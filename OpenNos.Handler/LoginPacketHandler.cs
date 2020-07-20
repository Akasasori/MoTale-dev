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
using OpenNos.DAL;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject;
using OpenNos.GameObject.Packets.ClientPackets;
using OpenNos.Master.Library.Client;
using System;
using System.Configuration;
using System.Linq;

namespace OpenNos.Handler
{
    public class LoginPacketHandler : IPacketHandler
    {
        #region Members

        private readonly ClientSession _session;

        #endregion

        #region Instantiation

        public LoginPacketHandler(ClientSession session) => _session = session;

        #endregion

        #region Methods

        string BuildServersPacket(string username, int sessionId, bool ignoreUserName, string regionCode)
        {
            string channelPacket = CommunicationServiceClient.Instance.RetrieveRegisteredWorldServers( username, sessionId, ignoreUserName, regionCode);

            if (channelPacket?.Contains(':') != true)
            {
                // no need for this as in release the debug is ignored eitherway
                //if (ServerManager.Instance.IsDebugMode)
                Logger.Debug("Could not retrieve Worldserver groups. Please make sure they've already been registered.");

                // find a new way to display this message
                //Session.SendPacket($"fail {Language.Instance.GetMessageFromKey("NO_WORLDSERVERS")}");
                _session.SendPacket("failc 1");
            }

            return channelPacket;
        }

        /// <summary>
        /// login packet
        /// </summary>
        /// <param name="loginPacket"></param>
        public void VerifyLogin(LoginPacket loginPacket)
        {
            UserDTO user = new UserDTO
            {
                Name = loginPacket.Name,
                Password = ConfigurationManager.AppSettings["UseOldCrypto"] == "true" ? CryptographyBase.Sha512(LoginCryptography.GetPassword(loginPacket.Password)).ToUpper() : loginPacket.Password
            };
            if (user == null || user.Name == null || user.Password == null)
            {
                return;
            }
            AccountDTO loadedAccount = DAOFactory.AccountDAO.LoadByName(user.Name);
            if (loadedAccount != null && loadedAccount.Name != user.Name)
            {
                _session.SendPacket($"failc {(byte)LoginFailType.WrongCaps}");
                return;
            }
            if (loadedAccount?.Password.ToUpper().Equals(user.Password) == true)
            {
                string ipAddress = _session.IpAddress;
                DAOFactory.AccountDAO.WriteGeneralLog(loadedAccount.AccountId, ipAddress, null, GeneralLogType.Connection, "LoginServer");

                //check if the account is connected
                if (!CommunicationServiceClient.Instance.IsAccountConnected(loadedAccount.AccountId))
                {
                    AuthorityType type = loadedAccount.Authority;
                    PenaltyLogDTO penalty = DAOFactory.PenaltyLogDAO.LoadByAccount(loadedAccount.AccountId).FirstOrDefault(s => s.DateEnd > DateTime.Now && s.Penalty == PenaltyType.Banned);
                    if (penalty != null)
                    {
                        // find a new way to display date of ban
                        _session.SendPacket($"fail {(Language.Instance.GetMessageFromKey("BANNED"), penalty.Reason, penalty.DateEnd.ToString("yyyy-MM-dd-HH:mm"))}");
                    }
                    //    if (loginPacket.ClientVersion != "0.9.4.3127")
                    //      {
                    //  _session.SendPacket($"failc {(byte)LoginFailType.OldClient}");
                    // return;
                    //   }
                    else
                    {
                        switch (type)
                        {
                            case AuthorityType.Unconfirmed:
                                {
                                    _session.SendPacket($"fail {Language.Instance.GetMessageFromKey("NOTVALIDATE")}");
                                }
                                break;

                            case AuthorityType.Banned:
                                {
                                    _session.SendPacket($"failc {(byte)LoginFailType.Banned}");
                                }
                                break;

                            case AuthorityType.Closed:
                                {
                                    _session.SendPacket($"failc {(byte)LoginFailType.CantConnect}");
                                }
                                break;

                            default:
                                {
                                    if (loadedAccount.Authority == AuthorityType.User || loadedAccount.Authority == AuthorityType.BitchNiggerFaggot)
                                    {
                                        MaintenanceLogDTO maintenanceLog = DAOFactory.MaintenanceLogDAO.LoadFirst();
                                        if (maintenanceLog != null)
                                        {
                                            // find a new way to display date and reason of maintenance
                                            _session.SendPacket($"failc {(byte)LoginFailType.Maintenance}");
                                            return;
                                        }
                                    }
                                    if (loadedAccount.Authority == AuthorityType.User || loadedAccount.Authority == AuthorityType.BitchNiggerFaggot)
                                    {                                     
                                            // find a new way to display date and reason of maintenance
                                            _session.SendPacket($"failc {(byte)LoginFailType.CantConnect}");
                                            return;                                        
                                    }

                                    int newSessionId = SessionFactory.Instance.GenerateSessionId();

                                    Logger.Debug(string.Format(Language.Instance.GetMessageFromKey("CONNECTION"), user.Name, newSessionId));
                                    try
                                    {
                                        ipAddress = ipAddress.Substring(6, ipAddress.LastIndexOf(':') - 6);
                                        CommunicationServiceClient.Instance.RegisterAccountLogin(loadedAccount.AccountId, newSessionId, ipAddress);
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error("General Error SessionId: " + newSessionId, ex);
                                    }
                                    string[] clientData = loginPacket.ClientData.Split('.');
                                    bool ignoreUserName = clientData.Length < 3 ? false : short.TryParse(clientData[3], out short clientVersion) && (clientVersion < 3126 || ConfigurationManager.AppSettings["UseOldCrypto"] == "true");
                                    _session.SendPacket(BuildServersPacket(user.Name, newSessionId, ignoreUserName, loginPacket.LangData.Split((char)11)[0]));
                                }
                                break;
                        }
                    }
                }
                else
                {
                    _session.SendPacket($"failc {(byte)LoginFailType.AlreadyConnected}");
                }
            }
            else
            {
                _session.SendPacket($"failc {(byte)LoginFailType.AccountOrPasswordWrong}");
            }
        }

        #endregion
    }
}