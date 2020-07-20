/*
 * This file is part of the OpenNos Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using OpenNos.Core;
using OpenNos.Core.Handling;
using OpenNos.Core.Networking.Communication.Scs.Communication.Messages;
using OpenNos.DAL;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Networking;
using OpenNos.Master.Library.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;

namespace OpenNos.GameObject
{
    public class ClientSession
    {
        #region Members

        public bool HealthStop;

        private static CryptographyBase _encryptor;

        private readonly INetworkClient _client;

        private readonly Random _random;

        private readonly ConcurrentQueue<byte[]> _receiveQueue;

        private readonly object _receiveQueueObservable;

        private readonly IList<string> _waitForPacketList = new List<string>();

        private Character _character;

        private IDictionary<string[], HandlerMethodReference> _handlerMethods;

        private int _lastPacketId;

        // private byte countPacketReceived;

        private long _lastPacketReceive;

        private int? _waitForPacketsAmount;

        #endregion

        #region Instantiation

        public ClientSession(INetworkClient client)
        {
            // set the time of last received packet
            _lastPacketReceive = DateTime.Now.Ticks;

            // lag mode
            _random = new Random((int)client.ClientId);

            // initialize network client
            _client = client;

            // absolutely new instantiated Client has no SessionId
            SessionId = 0;

            // register for NetworkClient events
            _client.MessageReceived += OnNetworkClientMessageReceived;

            // start observer for receiving packets
            _receiveQueue = new ConcurrentQueue<byte[]>();
            _receiveQueueObservable = Observable.Interval(new TimeSpan(0, 0, 0, 0, 10)).Subscribe(x => HandlePackets());

            #region Anti-Cheat Heartbeat

            if (ServerManager.Instance.IsWorldServer)
            {
                RegisterAntiCheat();
            }

            #endregion
        }

        #endregion

        #region Properties

        public static ThreadSafeGenericLockedList<string> UserLog { get; set; } = new ThreadSafeGenericLockedList<string>();

        public bool IsAntiCheatAlive { get; set; }

        public IDisposable AntiCheatHeartbeatDisposable { get; set; }

        public string LastAntiCheatData { get; set; }

        public string Crc32 { get; set; }

        public Account Account { get; private set; }

        public Character Character
        {
            get
            {
                if (_character == null || !HasSelectedCharacter)
                {
                    // cant access an
                    Logger.Warn("An uninitialized character should not be accessed.");
                }

                return _character;
            }
            private set => _character = value;
        }

        public long ClientId => _client.ClientId;

        public MapInstance CurrentMapInstance { get; set; }

        public IDictionary<string[], HandlerMethodReference> HandlerMethods
        {
            get => _handlerMethods ?? (_handlerMethods = new Dictionary<string[], HandlerMethodReference>());
            private set => _handlerMethods = value;
        }

        public bool HasCurrentMapInstance => CurrentMapInstance != null;

        public bool HasSelectedCharacter { get; private set; }

        public bool HasSession => _client != null;

        public string IpAddress => _client.IpAddress;

        public string CleanIpAddress
        {
            get
            {
                string cleanIp = _client.IpAddress.Replace("tcp://", "");
                return cleanIp.Substring(0, cleanIp.LastIndexOf(":") > 0 ? cleanIp.LastIndexOf(":") : cleanIp.Length);
            }
            set { }
        }

        public bool IsAuthenticated { get; private set; }

        public bool IsConnected => _client.IsConnected;

        public bool IsDisposing
        {
            get => _client.IsDisposing;
            internal set => _client.IsDisposing = value;
        }

        public bool IsLocalhost => IpAddress.Contains("127.0.0.1");// i dont know because this is here

        public bool IsOnMap => CurrentMapInstance != null;
        
        public DateTime RegisterTime { get; internal set; }

        public int SessionId { get; private set; }

        #endregion

        #region Methods

        #region Anti-Cheat

        public void RegisterAntiCheat()
        {
            if (!AntiCheatHelper.IsAntiCheatEnabled)
            {
                return;
            }

            IsAntiCheatAlive = true;

            AntiCheatHeartbeatDisposable = Observable.Interval(TimeSpan.FromMinutes(1))
                .Subscribe(observer =>
                {
                    if (IsAntiCheatAlive)
                    {
                        LastAntiCheatData = AntiCheatHelper.GenerateData(64);
                        SendPacket($"ntcp_ac 0 {LastAntiCheatData} {AntiCheatHelper.Sha512(LastAntiCheatData + AntiCheatHelper.ClientKey)}");
                        IsAntiCheatAlive = false;
                    }
                    else
                    {
                        if (Account?.Authority != AuthorityType.Administrator)
                        {
                            Disconnect();
                        }
                    }
                });
        }

        public void BanForCheatUsing(int detectionCode)
        {
            bool isFirstTime = !DAOFactory.PenaltyLogDAO.LoadByAccount(Account.AccountId).Any(s => s.AdminName == "Anti-Cheat")
                && !DAOFactory.PenaltyLogDAO.LoadByIp(IpAddress).Any(s => s.AdminName == "Anti-Cheat");

            PenaltyLogDTO penaltyLog = new PenaltyLogDTO
            {
                AccountId = Account.AccountId,
                Reason = $"Cheat Using ({detectionCode})",
                Penalty = PenaltyType.Banned,
                DateStart = DateTime.Now,
                DateEnd = isFirstTime ? DateTime.Now.AddHours(24) : DateTime.Now.AddYears(15),
                AdminName = "Anti-Cheat"
            };

            Character.InsertOrUpdatePenalty(penaltyLog);
            Disconnect();
        }

        #endregion

        public void ClearLowPriorityQueue() => _client.ClearLowPriorityQueueAsync();

        public void Destroy()
        {
            #region Anti-Cheat Heartbeat

            AntiCheatHeartbeatDisposable?.Dispose();

            #endregion

            // unregister from WCF events
            CommunicationServiceClient.Instance.CharacterConnectedEvent -= OnOtherCharacterConnected;
            CommunicationServiceClient.Instance.CharacterDisconnectedEvent -= OnOtherCharacterDisconnected;

            // do everything necessary before removing client, DB save, Whatever
            if (HasSelectedCharacter)
            {
                Logger.LogUserEvent("CHARACTER_LOGOUT", GenerateIdentity(), "");

                long characterId = Character.CharacterId;

                Character.Dispose();

                // disconnect client
                CommunicationServiceClient.Instance.DisconnectCharacter(ServerManager.Instance.WorldId, characterId);

                // unregister from map if registered
                if (CurrentMapInstance != null)
                {
                    CurrentMapInstance.UnregisterSession(characterId);
                    CurrentMapInstance = null;
                }

                ServerManager.Instance.UnregisterSession(characterId);
            }

            if (Account != null)
            {
                CommunicationServiceClient.Instance.DisconnectAccount(Account.AccountId);
            }

            ClearReceiveQueue();
        }

        public void Disconnect() => _client.Disconnect();

        public string GenerateIdentity()
        {
            if (Character != null)
            {
                return $"Character: {Character.Name}";
            }
            return $"Account: {Account.Name}";
        }

        public void Initialize(CryptographyBase encryptor, Type packetHandler, bool isWorldServer)
        {
            _encryptor = encryptor;
            _client.Initialize(encryptor);

            // dynamically create packethandler references
            GenerateHandlerReferences(packetHandler, isWorldServer);
        }

        public void InitializeAccount(Account account, bool crossServer = false)
        {
            Account = account;
            if (crossServer)
            {
                CommunicationServiceClient.Instance.ConnectAccountCrossServer(ServerManager.Instance.WorldId, account.AccountId, SessionId);
            }
            else
            {
                CommunicationServiceClient.Instance.ConnectAccount(ServerManager.Instance.WorldId, account.AccountId, SessionId);
            }
            IsAuthenticated = true;
        }

        public void ReceivePacket(string packet, bool ignoreAuthority = false)
        {
            string header = packet.Split(' ')[0];
            TriggerHandler(header, $"{_lastPacketId} {packet}", false, ignoreAuthority);
            _lastPacketId++;
        }

        public void SendPacket(string packet, byte priority = 10)
        {
            if (!IsDisposing)
            {
                _client.SendPacket(packet, priority);
                if (packet != null && _character != null && HasSelectedCharacter && !packet.StartsWith("cond ") && !packet.StartsWith("mv ")) SendPacket(Character.GenerateCond());
            }
        }

        public void SendPacket(PacketDefinition packet, byte priority = 10)
        {
            if (!IsDisposing)
            {
                _client.SendPacket(PacketFactory.Serialize(packet), priority);
            }
        }

        public void SendPacketAfter(string packet, int milliseconds)
        {
            if (!IsDisposing)
            {
                Observable.Timer(TimeSpan.FromMilliseconds(milliseconds)).Subscribe(o => SendPacket(packet));
            }
        }

        public void SendPacketFormat(string packet, params object[] param)
        {
            if (!IsDisposing)
            {
                _client.SendPacketFormat(packet, param);
            }
        }

        public void SendPackets(IEnumerable<string> packets, byte priority = 10)
        {
            if (!IsDisposing)
            {
                _client.SendPackets(packets, priority);
                if (_character != null && HasSelectedCharacter) SendPacket(Character.GenerateCond());
            }
        }

        public void SendPackets(IEnumerable<PacketDefinition> packets, byte priority = 10)
        {
            if (!IsDisposing)
            {
                packets.ToList().ForEach(s => _client.SendPacket(PacketFactory.Serialize(s), priority));
            }
        }

        public void SetCharacter(Character character)
        {
            Character = character;
            HasSelectedCharacter = true;

            Logger.LogUserEvent("CHARACTER_LOGIN", GenerateIdentity(), "");

            // register CSC events
            CommunicationServiceClient.Instance.CharacterConnectedEvent += OnOtherCharacterConnected;
            CommunicationServiceClient.Instance.CharacterDisconnectedEvent += OnOtherCharacterDisconnected;

            // register for servermanager
            ServerManager.Instance.RegisterSession(this);
            ServerManager.Instance.CharacterScreenSessions.Remove(character.AccountId);
            Character.SetSession(this);
        }

        private void ClearReceiveQueue()
        {
            while (_receiveQueue.TryDequeue(out byte[] outPacket))
            {
                // do nothing
            }
        }

        private void GenerateHandlerReferences(Type type, bool isWorldServer)
        {
            IEnumerable<Type> handlerTypes = !isWorldServer ? type.Assembly.GetTypes().Where(t => t.Name.Equals("LoginPacketHandler")) // shitty but it works
                                                            : type.Assembly.GetTypes().Where(p =>
                                                            {
                                                                Type interfaceType = type.GetInterfaces().FirstOrDefault();
                                                                return interfaceType != null && !p.IsInterface && interfaceType.IsAssignableFrom(p);
                                                            });

            // iterate thru each type in the given assembly
            foreach (Type handlerType in handlerTypes)
            {
                IPacketHandler handler = (IPacketHandler)Activator.CreateInstance(handlerType, this);

                // include PacketDefinition
                foreach (MethodInfo methodInfo in handlerType.GetMethods().Where(x => x.GetCustomAttributes(false).OfType<PacketAttribute>().Any() || x.GetParameters().FirstOrDefault()?.ParameterType.BaseType == typeof(PacketDefinition)))
                {
                    List<PacketAttribute> packetAttributes = methodInfo.GetCustomAttributes(false).OfType<PacketAttribute>().ToList();

                    // assume PacketDefinition based handler method
                    if (packetAttributes.Count == 0)
                    {
                        HandlerMethodReference methodReference = new HandlerMethodReference(DelegateBuilder.BuildDelegate<Action<object, object>>(methodInfo), handler, methodInfo.GetParameters().FirstOrDefault()?.ParameterType);
                        HandlerMethods.Add(methodReference.Identification, methodReference);
                    }
                    else
                    {
                        // assume string based handler method
                        foreach (PacketAttribute packetAttribute in packetAttributes)
                        {
                            HandlerMethodReference methodReference = new HandlerMethodReference(DelegateBuilder.BuildDelegate<Action<object, object>>(methodInfo), handler, packetAttribute);
                            HandlerMethods.Add(methodReference.Identification, methodReference);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handle the packet received by the Client.
        /// </summary>
        private void HandlePackets()
        {
            try
            {
                while (_receiveQueue.TryDequeue(out byte[] packetData))
                {
                    // determine first packet
                    if (_encryptor.HasCustomParameter && SessionId == 0)
                    {
                        string sessionPacket = _encryptor.DecryptCustomParameter(packetData);

                        string[] sessionParts = sessionPacket.Split(' ');

                        if (sessionParts.Length == 0)
                        {
                            return;
                        }

                        if (!int.TryParse(sessionParts[0], out int packetId))
                        {
                            Disconnect();
                        }

                        _lastPacketId = packetId;

                        // set the SessionId if Session Packet arrives
                        if (sessionParts.Length < 2)
                        {
                            return;
                        }

                        if (int.TryParse(sessionParts[1].Split('\\').FirstOrDefault(), out int sessid))
                        {
                            SessionId = sessid;
                            Logger.Debug(string.Format(Language.Instance.GetMessageFromKey("CLIENT_ARRIVED"), SessionId));

                            if (!_waitForPacketsAmount.HasValue)
                            {
                                TriggerHandler("OpenNos.EntryPoint", "", false);
                            }
                        }

                        return;
                    }

                    string packetConcatenated = _encryptor.Decrypt(packetData, SessionId);

                    foreach (string packet in packetConcatenated.Split(new[] { (char)0xFF }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string packetstring = packet.Replace('^', ' ');
                        string[] packetsplit = packetstring.Split(' ');

                        if (_encryptor.HasCustomParameter)
                        {
                            string nextRawPacketId = packetsplit[0];

                            if (!int.TryParse(nextRawPacketId, out int nextPacketId) && nextPacketId != _lastPacketId + 1)
                            {
                                Logger.Error(string.Format(Language.Instance.GetMessageFromKey("CORRUPTED_KEEPALIVE"), _client.ClientId));
                                _client.Disconnect();
                                return;
                            }

                            if (nextPacketId == 0)
                            {
                                if (_lastPacketId == ushort.MaxValue)
                                {
                                    _lastPacketId = nextPacketId;
                                }
                            }
                            else
                            {
                                _lastPacketId = nextPacketId;
                            }

                            if (_waitForPacketsAmount.HasValue)
                            {
                                _waitForPacketList.Add(packetstring);

                                string[] packetssplit = packetstring.Split(' ');

                                if (packetssplit.Length > 3 && packetsplit[1] == "DAC")
                                {
                                    _waitForPacketList.Add("0 CrossServerAuthenticate");
                                }

                                if (_waitForPacketList.Count == _waitForPacketsAmount)
                                {
                                    _waitForPacketsAmount = null;
                                    string queuedPackets = string.Join(" ", _waitForPacketList.ToArray());
                                    string header = queuedPackets.Split(' ', '^')[1];
                                    TriggerHandler(header, queuedPackets, true);
                                    _waitForPacketList.Clear();
                                    return;
                                }
                            }
                            else if (packetsplit.Length > 1)
                            {
                                if (packetsplit[1].Length >= 1 && (packetsplit[1][0] == '/' || packetsplit[1][0] == ':' || packetsplit[1][0] == ';'))
                                {
                                    packetsplit[1] = packetsplit[1][0].ToString();
                                    packetstring = packet.Insert(packet.IndexOf(' ') + 2, " ");
                                }

                                if (packetsplit[1] != "0")
                                {
                                    TriggerHandler(packetsplit[1].Replace("#", ""), packetstring, false);
                                }
                            }
                        }
                        else
                        {
                            string packetHeader = packetstring.Split(' ')[0];

                            // simple messaging
                            if (packetHeader[0] == '/' || packetHeader[0] == ':' || packetHeader[0] == ';')
                            {
                                packetHeader = packetHeader[0].ToString();
                                packetstring = packet.Insert(packet.IndexOf(' ') + 2, " ");
                            }

                            TriggerHandler(packetHeader.Replace("#", ""), packetstring, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Invalid packet (Crash Exploit)", ex);
                Disconnect();
            }
        }

        /// <summary>
        /// This will be triggered when the underlying NetworkClient receives a packet.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnNetworkClientMessageReceived(object sender, MessageEventArgs e)
        {
            ScsRawDataMessage message = e.Message as ScsRawDataMessage;
            if (message == null)
            {
                return;
            }
            if (message.MessageData.Length > 0 && message.MessageData.Length > 2)
            {
                _receiveQueue.Enqueue(message.MessageData);
            }
            _lastPacketReceive = e.ReceivedTimestamp.Ticks;
        }

        private void OnOtherCharacterConnected(object sender, EventArgs e)
        {
            if (Character?.IsDisposed != false)
            {
                return;
            }

            Tuple<long, string> loggedInCharacter = (Tuple<long, string>)sender;

            if (Character.IsFriendOfCharacter(loggedInCharacter.Item1) && Character != null && Character.CharacterId != loggedInCharacter.Item1)
            {
                _client.SendPacket(Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("CHARACTER_LOGGED_IN"), loggedInCharacter.Item2), 10));
                _client.SendPacket(Character.GenerateFinfo(loggedInCharacter.Item1, true));
            }

            FamilyCharacter chara = Character.Family?.FamilyCharacters.Find(s => s.CharacterId == loggedInCharacter.Item1);

            if (chara != null && loggedInCharacter.Item1 != Character?.CharacterId)
            {
                _client.SendPacket(Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("CHARACTER_FAMILY_LOGGED_IN"), loggedInCharacter.Item2, Language.Instance.GetMessageFromKey(chara.Authority.ToString().ToUpper())), 10));
            }
        }

        private void OnOtherCharacterDisconnected(object sender, EventArgs e)
        {
            if (Character?.IsDisposed != false)
            {
                return;
            }

            Tuple<long, string> loggedOutCharacter = (Tuple<long, string>)sender;

            if (Character.IsFriendOfCharacter(loggedOutCharacter.Item1) && Character != null && Character.CharacterId != loggedOutCharacter.Item1)
            {
                _client.SendPacket(Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("CHARACTER_LOGGED_OUT"), loggedOutCharacter.Item2), 10));
                _client.SendPacket(Character.GenerateFinfo(loggedOutCharacter.Item1, false));
            }
        }

        private void TriggerHandler(string packetHeader, string packet, bool force, bool ignoreAuthority = false)
        {
            if (ServerManager.Instance.InShutdown)
            {
                return;
            }
            if (!IsDisposing)
            {
                if (Account?.Name != null && UserLog.Contains(Account.Name))
                {
                    try
                    {
                        File.AppendAllText($"C:\\{Account.Name.Replace(" ", "")}.txt", packet + "\n");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }
                }

                string[] key = HandlerMethods.Keys.FirstOrDefault(s => s.Any(m => string.Equals(m, packetHeader, StringComparison.CurrentCultureIgnoreCase)));
                HandlerMethodReference methodReference = key != null ? HandlerMethods[key] : null;
                if (methodReference != null)
                {
                    if (methodReference.HandlerMethodAttribute != null && !force && methodReference.HandlerMethodAttribute.Amount > 1 && !_waitForPacketsAmount.HasValue)
                    {
                        // we need to wait for more
                        _waitForPacketsAmount = methodReference.HandlerMethodAttribute.Amount;
                        _waitForPacketList.Add(packet != "" ? packet : $"1 {packetHeader} ");
                        return;
                    }
                    try
                    {
                        if (HasSelectedCharacter || methodReference.ParentHandler.GetType().Name == "CharacterScreenPacketHandler" || methodReference.ParentHandler.GetType().Name == "LoginPacketHandler")
                        {
                            // call actual handler method
                            if (methodReference.PacketDefinitionParameterType != null)
                            {
                                //check for the correct authority
                                if (!IsAuthenticated
                                    || Account.Authority.Equals(AuthorityType.Administrator)
                                    || methodReference.Authorities.Any(a => a.Equals(Account.Authority))
                                    || methodReference.Authorities.Any(a => a.Equals(AuthorityType.User)) && Account.Authority >= AuthorityType.User
                                    || methodReference.Authorities.Any(a => a.Equals(AuthorityType.TMOD)) && Account.Authority >= AuthorityType.TMOD && Account.Authority <= AuthorityType.BA
                                    || methodReference.Authorities.Any(a => a.Equals(AuthorityType.MOD)) && Account.Authority >= AuthorityType.MOD && Account.Authority <= AuthorityType.BA
                                    || methodReference.Authorities.Any(a => a.Equals(AuthorityType.SMOD)) && Account.Authority >= AuthorityType.SMOD && Account.Authority <= AuthorityType.BA
                                    || methodReference.Authorities.Any(a => a.Equals(AuthorityType.TGM)) && Account.Authority >= AuthorityType.TGM
                                    || methodReference.Authorities.Any(a => a.Equals(AuthorityType.GM)) && Account.Authority >= AuthorityType.GM
                                    || methodReference.Authorities.Any(a => a.Equals(AuthorityType.SGM)) && Account.Authority >= AuthorityType.SGM
                                    || methodReference.Authorities.Any(a => a.Equals(AuthorityType.GA)) && Account.Authority >= AuthorityType.GA
                                    || methodReference.Authorities.Any(a => a.Equals(AuthorityType.TM)) && Account.Authority >= AuthorityType.TM
                                    || methodReference.Authorities.Any(a => a.Equals(AuthorityType.CM)) && Account.Authority >= AuthorityType.CM
                                    || Account.Authority == AuthorityType.BitchNiggerFaggot && methodReference.Authorities.Any(a => a.Equals(AuthorityType.User))
                                    || ignoreAuthority)
                                {
                                    PacketDefinition deserializedPacket = PacketFactory.Deserialize(packet, methodReference.PacketDefinitionParameterType, IsAuthenticated);
                                    if (deserializedPacket != null || methodReference.PassNonParseablePacket)
                                    {
                                        /*if (ServerManager.Instance.Configuration != null && ServerManager.Instance.Configuration.UseLogService && Character != null)
                                        {
                                            try
                                            {
                                                string message = "";
                                                string[] valuesplit = deserializedPacket.OriginalContent?.Split(' ');
                                                if (valuesplit == null)
                                                {
                                                    return;
                                                }

                                                if (valuesplit[1] != null && valuesplit[1] != "walk" && valuesplit[1] != "ncif" && valuesplit[1] != "say" && valuesplit[1] != "preq")
                                                {
                                                    for (int i = 0; i < valuesplit.Length; i++)
                                                    {
                                                        if (i > 0)
                                                        {
                                                            message += valuesplit[i] + " ";
                                                        }
                                                    }
                                                    message = message.Substring(0, message.Length - 1); // Remove the last " "
                                                    try
                                                    {
                                                        LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                                                        {
                                                            Sender = Character.Name,
                                                            SenderId = Character.CharacterId,
                                                            PacketType = LogType.Packet,
                                                            Packet = message
                                                        });
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Logger.Error("PacketLog Error. SessionId: " + SessionId, ex);
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Logger.Error("PacketLog Error. SessionId: " + SessionId, ex);
                                            }
                                        }*/
        methodReference.HandlerMethod(methodReference.ParentHandler, deserializedPacket);
                                    }
                                    else
                                    {
                                        Logger.Warn(string.Format(Language.Instance.GetMessageFromKey("CORRUPT_PACKET"), packetHeader, packet));
                                    }
                                }
                            }
                            else
                            {
                                methodReference.HandlerMethod(methodReference.ParentHandler, packet);
                            }
                        }
                    }
                    catch (DivideByZeroException ex)
                    {
                        // disconnect if something unexpected happens
                        Logger.Error("Handler Error SessionId: " + SessionId, ex);
                        Disconnect();
                    }
                }
                else
                {
                    Logger.Warn($"{ string.Format(Language.Instance.GetMessageFromKey("HANDLER_NOT_FOUND"), packetHeader)} From IP: {_client.IpAddress}");
                }
            }
            else
            {
                Logger.Warn(string.Format(Language.Instance.GetMessageFromKey("CLIENTSESSION_DISPOSING"), packetHeader));
            }
        }

        #endregion
    }
}