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
using OpenNos.Core.Interfaces.Packets.ClientPackets;
using OpenNos.DAL;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject;
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Networking;
using OpenNos.Master.Library.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace OpenNos.Handler
{
    public class CharacterScreenPacketHandler : IPacketHandler
    {
        #region Instantiation

        public CharacterScreenPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        private ClientSession Session { get; }

        #endregion

        #region Methods

        /// <summary>
        ///     Title equip or unequip system
        /// </summary>
        /// <param name="titEqPacket"></param>

        public void TitleSystem(TitEqPacket titEqPacket)
        {

            if (DAOFactory.CharacterTitlesDAO.LoadByCharacterId(Session.Character.CharacterId).Any(s => s.TitleId == titEqPacket.TituloVNum))
            {
                switch (titEqPacket.Type)
                {
                    case 1:
                        if (Session.Character.VisTit == titEqPacket.TituloVNum)
                        {
                            Session.Character.VisTit = 0;
                        }
                        else if (Session.Character.VisTit != titEqPacket.TituloVNum)
                        {
                            Session.Character.VisTit = titEqPacket.TituloVNum;
                        }
                        break;
                    case 2:
                        if (Session.Character.EffTit == titEqPacket.TituloVNum)
                        {
                            Session.Character.EffTit = 0;
                        }
                        else if (Session.Character.EffTit != titEqPacket.TituloVNum)
                        {
                            Session.Character.EffTit = titEqPacket.TituloVNum;
                        }
                        break;
                }
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey($"TITLE_EQ_{titEqPacket.Type}")));
                Session.Character.GenerateTitInfo();
                Session.Character.GenerateTitle();
            }
        }

        public void CreateCharacterAction(ICharacterCreatePacket characterCreatePacket, ClassType classType)
        {
            if (Session.HasCurrentMapInstance)
            {
                return;
            }


            Logger.LogUserEvent("CREATECHARACTER", Session.GenerateIdentity(), $"[CreateCharacter]Name: {characterCreatePacket.Name} Slot: {characterCreatePacket.Slot} Gender: {characterCreatePacket.Gender} HairStyle: {characterCreatePacket.HairStyle} HairColor: {characterCreatePacket.HairColor}");

            if (characterCreatePacket.Slot <= 3
                && DAOFactory.CharacterDAO.LoadBySlot(Session.Account.AccountId, characterCreatePacket.Slot) == null
                && characterCreatePacket.Name != null
                && (characterCreatePacket.Gender == GenderType.Male || characterCreatePacket.Gender == GenderType.Female)
                && (characterCreatePacket.HairStyle == HairStyleType.HairStyleA || (classType != ClassType.MartialArtist && characterCreatePacket.HairStyle == HairStyleType.HairStyleB))
                && Enumerable.Range(0, 10).Contains((byte)characterCreatePacket.HairColor)
                && (characterCreatePacket.Name.Length >= 4 && characterCreatePacket.Name.Length <= 14))
            {
                if (classType == ClassType.MartialArtist)
                {
                    IEnumerable<CharacterDTO> characterDTOs = DAOFactory.CharacterDAO.LoadByAccount(Session.Account.AccountId);

                    if (!characterDTOs.Any(s => s.Level >= 80))
                    {
                        return;
                    }

                    if (characterDTOs.Any(s => s.Class == ClassType.MartialArtist))
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("MARTIAL_ARTIST_ALREADY_EXISTING")));
                        return;
                    }
                }

                Regex regex = new Regex(@"^[A-Za-z0-9_áéíóúÁÉÍÓÚäëïöüÄËÏÖÜ]+$");

                if (regex.Matches(characterCreatePacket.Name).Count != 1)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("INVALID_CHARNAME")));
                    return;
                }

                if (DAOFactory.CharacterDAO.LoadByName(characterCreatePacket.Name) != null)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("CHARNAME_ALREADY_TAKEN")));
                    return;
                }

                CharacterDTO characterDTO = new CharacterDTO
                {
                    AccountId = Session.Account.AccountId,
                    Slot = characterCreatePacket.Slot,
                    Class = classType,
                    Gender = characterCreatePacket.Gender,
                    HairStyle = characterCreatePacket.HairStyle,
                    HairColor = characterCreatePacket.HairColor,
                    Name = characterCreatePacket.Name,

                    Reputation = 250,
                    MapId = 1,
                    MapX = 79,
                    MapY = 120,
                    Gold = 30000,
                    MaxMateCount = 10,
                    MaxPartnerCount = 3,
                    SpPoint = 10000,
                    SpAdditionPoint = 0,
                    MinilandMessage = "Welcome",
                    State = CharacterState.Active,
                    MinilandPoint = 2000
                };

                switch (characterDTO.Class)
                {
                    case ClassType.MartialArtist:
                        {
                            characterDTO.Level = 81;
                            characterDTO.JobLevel = 1;
                            characterDTO.Hp = 700;
                            characterDTO.Mp = 221;
                        }
                        break;

                    default:
                        {
                            characterDTO.Level = 1;
                            characterDTO.JobLevel = 1;
                            characterDTO.Hp = 700;
                            characterDTO.Mp = 221;
                        }
                        break;
                }

                DAOFactory.CharacterDAO.InsertOrUpdate(ref characterDTO);

                if (classType != ClassType.MartialArtist)
                {
                    DAOFactory.CharacterQuestDAO.InsertOrUpdate(new CharacterQuestDTO
                    {
                        CharacterId = characterDTO.CharacterId,
                      //Your custom quest at the beginning.
                        QuestId = 1997,
                        IsMainQuest = true
                    });

                    //DAOFactory.QuicklistEntryDAO.InsertOrUpdate(new QuicklistEntryDTO
                    //{
                    //  CharacterId = characterDTO.CharacterId,
                    //Type = 1,
                    //Slot = 1,
                    //Pos = 1
                    //});

                    //DAOFactory.QuicklistEntryDAO.InsertOrUpdate(new QuicklistEntryDTO
                    //{
                    //    CharacterId = characterDTO.CharacterId,
                    //    Q2 = 1,
                    //    Slot = 2
                    //});

                    //DAOFactory.QuicklistEntryDAO.InsertOrUpdate(new QuicklistEntryDTO
                    //{
                    //    CharacterId = characterDTO.CharacterId,
                    //    Q2 = 8,
                    //    Type = 1,
                    //    Slot = 1,
                    //    Pos = 16
                    //});

                    //DAOFactory.QuicklistEntryDAO.InsertOrUpdate(new QuicklistEntryDTO
                    //{
                    //    CharacterId = characterDTO.CharacterId,
                    //    Q2 = 9,
                    //    Type = 1,
                    //    Slot = 3,
                    //    Pos = 1
                    //});

                    DAOFactory.CharacterSkillDAO.InsertOrUpdate(new CharacterSkillDTO { CharacterId = characterDTO.CharacterId, SkillVNum = 200 });
                    DAOFactory.CharacterSkillDAO.InsertOrUpdate(new CharacterSkillDTO { CharacterId = characterDTO.CharacterId, SkillVNum = 201 });
                    DAOFactory.CharacterSkillDAO.InsertOrUpdate(new CharacterSkillDTO { CharacterId = characterDTO.CharacterId, SkillVNum = 209 });

                    using (Inventory inventory = new Inventory(new Character(characterDTO)))
                    {

                        inventory.AddNewToInventory(1, 1, InventoryType.Wear, 8, 10);
                        inventory.AddNewToInventory(8, 1, InventoryType.Wear, 8, 10);
                        inventory.AddNewToInventory(12, 1, InventoryType.Wear, 8, 10);
                        inventory.AddNewToInventory(1011, 20, InventoryType.Main);
                        inventory.AddNewToInventory(2081, 50, InventoryType.Main);
                        inventory.AddNewToInventory(9300, 1, InventoryType.Main);
                        inventory.AddNewToInventory(5181, 1, InventoryType.Main);
                        inventory.AddNewToInventory(800, 1, InventoryType.Equipment);
                        inventory.AddNewToInventory(801, 1, InventoryType.Equipment);
                        inventory.AddNewToInventory(802, 1, InventoryType.Equipment);
                        inventory.AddNewToInventory(803, 1, InventoryType.Equipment);
                        inventory.ForEach(i => DAOFactory.ItemInstanceDAO.InsertOrUpdate(i));
                        LoadCharacters(characterCreatePacket.OriginalContent);
                    }
                }
                else
                {
                    // DAOFactory.CharacterQuestDAO.InsertOrUpdate(new CharacterQuestDTO
                    // {
                    //   CharacterId = characterDTO.CharacterId,
                    //   QuestId = 6275,
                    //   IsMainQuest = false
                    //     });

                    for (short skillVNum = 1525; skillVNum <= 1539; skillVNum++)
                    {
                        DAOFactory.CharacterSkillDAO.InsertOrUpdate(new CharacterSkillDTO
                        {
                            CharacterId = characterDTO.CharacterId,
                            SkillVNum = skillVNum
                        });
                    }

                    DAOFactory.CharacterSkillDAO.InsertOrUpdate(new CharacterSkillDTO { CharacterId = characterDTO.CharacterId, SkillVNum = 1565 });

                    using (Inventory inventory = new Inventory(new Character(characterDTO)))
                    {
                        inventory.AddNewToInventory(5832, 1, InventoryType.Main, 5);
                        inventory.ForEach(i => DAOFactory.ItemInstanceDAO.InsertOrUpdate(i));
                        LoadCharacters(characterCreatePacket.OriginalContent);
                    }
                }
            }
        }

        /// <summary>
        /// Char_NEW character creation character
        /// </summary>
        /// <param name="characterCreatePacket"></param>
        public void CreateCharacter(CharacterCreatePacket characterCreatePacket)
            => CreateCharacterAction(characterCreatePacket, ClassType.Adventurer);

        /// <summary>
        /// Char_NEW_JOB character creation character
        /// </summary>
        /// <param name="characterJobCreatePacket"></param>
        public void CreateCharacterJob(CharacterJobCreatePacket characterJobCreatePacket)
            => CreateCharacterAction(characterJobCreatePacket, ClassType.MartialArtist);

        /// <summary>
        /// Char_DEL packet
        /// </summary>
        /// <param name="characterDeletePacket"></param>
        public void DeleteCharacter(CharacterDeletePacket characterDeletePacket)
        {
            if (Session.HasCurrentMapInstance)
            {
                return;
            }

            if (characterDeletePacket.Password == null)
            {
                return;
            }

            Logger.LogUserEvent("DELETECHARACTER", Session.GenerateIdentity(),
                $"[DeleteCharacter]Name: {characterDeletePacket.Slot}");
            AccountDTO account = DAOFactory.AccountDAO.LoadById(Session.Account.AccountId);
            if (account == null)
            {
                return;
            }

            if (account.Password.ToLower() == CryptographyBase.Sha512(characterDeletePacket.Password))
            {
                CharacterDTO character =
                    DAOFactory.CharacterDAO.LoadBySlot(account.AccountId, characterDeletePacket.Slot);
                if (character == null)
                {
                    return;
                }
                if(Session.Character?.Family == null)
                {
                    DAOFactory.CharacterDAO.DeleteByPrimaryKey(account.AccountId, characterDeletePacket.Slot);
                    LoadCharacters("");
                }
                else if(Session.Character?.Family != null && Session.Character?.FamilyCharacter.Authority != FamilyAuthority.Head)
                {
                    return;
                }
            }
            else
            {
                Session.SendPacket($"info {Language.Instance.GetMessageFromKey("BAD_PASSWORD")}");
            }
        }

        /// <summary>
        /// Load Characters, this is the Entrypoint for the Client, Wait for 3 Packets.
        /// </summary>
        /// <param name="packet"></param>
        [Packet(3, "OpenNos.EntryPoint")]
        public void LoadCharacters(string packet)
        {
            string[] loginPacketParts = packet.Split(' ');
            bool isCrossServerLogin = false;

            // Load account by given SessionId
            if (Session.Account == null)
            {
                bool hasRegisteredAccountLogin = true;
                AccountDTO account = null;
                if (loginPacketParts.Length > 4)
                {
                    if (loginPacketParts.Length > 7 && loginPacketParts[4] == "DAC"
                        && loginPacketParts[9] == "CrossServerAuthenticate")
                    {
                        isCrossServerLogin = true;
                        account = DAOFactory.AccountDAO.LoadByName(loginPacketParts[5]);
                    }
                    else
                    {
                        account = DAOFactory.AccountDAO.LoadByName(loginPacketParts[4]);
                    }
                }

                try
                {
                    if (account != null)
                    {
                        if (isCrossServerLogin)
                        {
                            hasRegisteredAccountLogin =
                                CommunicationServiceClient.Instance.IsCrossServerLoginPermitted(account.AccountId,
                                    Session.SessionId);
                        }
                        else
                        {
                            hasRegisteredAccountLogin =
                                CommunicationServiceClient.Instance.IsLoginPermitted(account.AccountId,
                                    Session.SessionId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("MS Communication Failed.", ex);
                    Session.Disconnect();
                    return;
                }

                if (loginPacketParts.Length > 4 && hasRegisteredAccountLogin)
                {
                    if (account != null)
                    {
                        if (account.Password.ToLower().Equals(CryptographyBase.Sha512(loginPacketParts[8]))
                            || isCrossServerLogin)
                        {
                            Session.InitializeAccount(new Account(account), isCrossServerLogin);
                            ServerManager.Instance.CharacterScreenSessions[Session.Account.AccountId] = Session;
                        }
                        else
                        {
                            Logger.Debug($"Client {Session.ClientId} forced Disconnection, invalid Password.");
                            Session.Disconnect();
                            return;
                        }
                    }
                    else
                    {
                        Logger.Debug($"Client {Session.ClientId} forced Disconnection, invalid AccountName.");
                        Session.Disconnect();
                        return;
                    }
                }
                else
                {
                    Logger.Debug(
                        $"Client {Session.ClientId} forced Disconnection, login has not been registered or Account is already logged in.");
                    Session.Disconnect();
                    return;
                }
            }

            if (isCrossServerLogin)
            {
                if (byte.TryParse(loginPacketParts[6], out byte slot))
                {
                    SelectCharacter(new SelectPacket { Slot = slot });
                }
            }
            else
            {
                // TODO: Wrap Database access up to GO
                IEnumerable<CharacterDTO> characters = DAOFactory.CharacterDAO.LoadByAccount(Session.Account.AccountId);

                Logger.Info(string.Format(Language.Instance.GetMessageFromKey("ACCOUNT_ARRIVED"), Session.SessionId));

                // load characterlist packet for each character in CharacterDTO
                Session.SendPacket("clist_start 0");

                foreach (CharacterDTO character in characters)
                {
                    IEnumerable<ItemInstanceDTO> inventory =
                        DAOFactory.ItemInstanceDAO.LoadByType(character.CharacterId, InventoryType.Wear);

                    ItemInstance[] equipment = new ItemInstance[17];

                    foreach (ItemInstanceDTO equipmentEntry in inventory)
                    {
                        // explicit load of iteminstance
                        ItemInstance currentInstance = new ItemInstance(equipmentEntry);

                        if (currentInstance != null)
                        {
                            equipment[(short)currentInstance.Item.EquipmentSlot] = currentInstance;
                        }
                    }

                    string petlist = "";

                    List<MateDTO> mates = DAOFactory.MateDAO.LoadByCharacterId(character.CharacterId).ToList();

                    for (int i = 0; i < 26; i++)
                    {
                        //0.2105.1102.319.0.632.0.333.0.318.0.317.0.9.-1.-1.-1.-1.-1.-1.-1.-1.-1.-1.-1.-1
                        petlist += (i != 0 ? "." : "") + (mates.Count > i ? $"{mates[i].Skin}.{mates[i].NpcMonsterVNum}" : "-1");
                    }

                    // 1 1 before long string of -1.-1 = act completion
                    Session.SendPacket($"clist {character.Slot} {character.Name} 0 {(byte)character.Gender} {(byte)character.HairStyle} {(byte)character.HairColor} 0 {(byte)character.Class} {character.Level} {character.HeroLevel} {equipment[(byte)EquipmentType.Hat]?.ItemVNum ?? -1}.{equipment[(byte)EquipmentType.Armor]?.ItemVNum ?? -1}.{equipment[(byte)EquipmentType.WeaponSkin]?.ItemVNum ?? (equipment[(byte)EquipmentType.MainWeapon]?.ItemVNum ?? -1)}.{equipment[(byte)EquipmentType.SecondaryWeapon]?.ItemVNum ?? -1}.{equipment[(byte)EquipmentType.Mask]?.ItemVNum ?? -1}.{equipment[(byte)EquipmentType.Fairy]?.ItemVNum ?? -1}.{equipment[(byte)EquipmentType.CostumeSuit]?.ItemVNum ?? -1}.{equipment[(byte)EquipmentType.CostumeHat]?.ItemVNum ?? -1} {character.JobLevel}  1 1 {petlist} {(equipment[(byte)EquipmentType.Hat]?.Item.IsColored == true ? equipment[(byte)EquipmentType.Hat].Design : 0)} 0");
                }

                Session.SendPacket("clist_end");
            }
        }

        /// <summary>
        /// select packet
        /// </summary>
        /// <param name="selectPacket"></param>
        public void SelectCharacter(SelectPacket selectPacket)
        {
            try
            {
                #region Validate Session

                if (Session?.Account == null || ServerManager.GetAllMapInstances().Any(s => s.Sessions.Any(a => a.Account.Name == Session.Account.Name)))
                {
                    return;
                }

                #endregion

                #region Load Character

                CharacterDTO characterDTO = DAOFactory.CharacterDAO.LoadBySlot(Session.Account.AccountId, selectPacket.Slot);

                if (characterDTO == null)
                {
                    return;
                }

                Character character = new Character(characterDTO);

                #endregion

                #region Unban Character

                if (ServerManager.Instance.BannedCharacters.Contains(character.CharacterId))
                {
                    ServerManager.Instance.BannedCharacters.RemoveAll(s => s == character.CharacterId);
                }

                #endregion

                #region Initialize Character

                character.Initialize();

                character.MapInstanceId = ServerManager.GetBaseMapInstanceIdByMapId(character.MapId);
                character.PositionX = character.MapX;
                character.PositionY = character.MapY;
                character.Authority = Session.Account.Authority;

                Session.SetCharacter(character);

                #endregion

                #region Load General Logs

                character.GeneralLogs = new ThreadSafeGenericList<GeneralLogDTO>();
                character.GeneralLogs.AddRange(DAOFactory.GeneralLogDAO.LoadByAccount(Session.Account.AccountId)
                    .Where(s => s.LogType == "DailyReward" || s.CharacterId == character.CharacterId).ToList());

                #endregion

                #region Reset SpPoint

                if (!Session.Character.GeneralLogs.Any(s => s.Timestamp == DateTime.Now && s.LogData == "World" && s.LogType == "Connection"))
                {
                    Session.Character.SpAdditionPoint += (int)(Session.Character.SpPoint / 100D * 20D);
                    Session.Character.SpPoint = 10000;
                }

                #endregion

                #region Other Character Stuffs

                Session.Character.Respawns = DAOFactory.RespawnDAO.LoadByCharacter(Session.Character.CharacterId).ToList();
                Session.Character.StaticBonusList = DAOFactory.StaticBonusDAO.LoadByCharacterId(Session.Character.CharacterId).ToList();
                Session.Character.LoadInventory();
                Session.Character.LoadQuicklists();
                Session.Character.GenerateMiniland();

                #endregion

                #region Quests

                //if (!DAOFactory.CharacterQuestDAO.LoadByCharacterId(Session.Character.CharacterId).Any(s => s.IsMainQuest)
                //    && !DAOFactory.QuestLogDAO.LoadByCharacterId(Session.Character.CharacterId).Any(s => s.QuestId == 1997))
                //{
                //    CharacterQuestDTO firstQuest = new CharacterQuestDTO
                //    {
                //        CharacterId = Session.Character.CharacterId,
                //        QuestId = 1997,
                //        IsMainQuest = true
                //    };

                //    DAOFactory.CharacterQuestDAO.InsertOrUpdate(firstQuest);
                //}

                DAOFactory.CharacterQuestDAO.LoadByCharacterId(Session.Character.CharacterId).ToList()
                    .ForEach(qst => Session.Character.Quests.Add(new CharacterQuest(qst)));

                #endregion

                #region Fix Partner Slots

                if (character.MaxPartnerCount < 3)
                {
                    character.MaxPartnerCount = 3;
                }

                #endregion

                #region Load Mates

                DAOFactory.MateDAO.LoadByCharacterId(Session.Character.CharacterId).ToList().ForEach(s =>
                {
                    Mate mate = new Mate(s)
                    {
                        Owner = Session.Character
                    };

                    mate.GenerateMateTransportId();
                    mate.Monster = ServerManager.GetNpcMonster(s.NpcMonsterVNum);

                    Session.Character.Mates.Add(mate);
                });

                #endregion

                #region Load Permanent Buff

                Session.Character.LastPermBuffRefresh = DateTime.Now;

                #endregion

                #region CharacterLife

                Session.Character.Life = Observable.Interval(TimeSpan.FromMilliseconds(300))
                    .Subscribe(x => Session.Character.CharacterLife());

                #endregion

                #region Load Amulet

                Observable.Timer(TimeSpan.FromSeconds(1))
                    .Subscribe(o =>
                    {
                        ItemInstance amulet = Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Amulet, InventoryType.Wear);

                        if (amulet?.ItemDeleteTime != null || amulet?.DurabilityPoint > 0)
                        {
                            Session.Character.AddBuff(new Buff(62, Session.Character.Level), Session.Character.BattleEntity);
                        }
                    });

                #endregion

                #region Load Static Buff

                foreach (StaticBuffDTO staticBuff in DAOFactory.StaticBuffDAO.LoadByCharacterId(Session.Character.CharacterId))
                {
                    if (staticBuff.CardId != 319 /* Wedding */)
                    {
                        Session.Character.AddStaticBuff(staticBuff);
                    }
                }

                #endregion

                #region Enter the World

                Session.Character.GeneralLogs.Add(new GeneralLogDTO
                {
                    AccountId = Session.Account.AccountId,
                    CharacterId = Session.Character.CharacterId,
                    IpAddress = Session.IpAddress,
                    LogData = "World",
                    LogType = "Connection",
                    Timestamp = DateTime.Now
                });

                Session.SendPacket("OK");

                CommunicationServiceClient.Instance.ConnectCharacter(ServerManager.Instance.WorldId, character.CharacterId);

                character.Channel = ServerManager.Instance;

                #endregion
            }
            catch (Exception ex)
            {
                Logger.Error("Failed selecting the character.", ex);
            }
            finally
            {
                // Suspicious activity detected -- kick!
                if (Session != null && (!Session.HasSelectedCharacter || Session.Character == null))
                {
                    Session.Disconnect();
                }
            }
        }

        #endregion
    }
}