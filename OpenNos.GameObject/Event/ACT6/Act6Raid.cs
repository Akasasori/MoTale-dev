using System.Linq;
using OpenNos.Domain;
using OpenNos.Core;
using OpenNos.GameObject.Networking;

namespace OpenNos.GameObject.Event.ACT6
{
    public static class Act6Raid
    {
        #region Members

        public static MapInstance EntryMap;

        public static ScriptedInstance RaidInstance;

        #endregion

        #region Methods

        public static void GenerateRaid(FactionType raidType)
        {
            RaidInstance = null;
            if (ServerManager.Instance.Act6Raids.Count > 0)
            {
                switch (raidType)
                {
                    case FactionType.Angel:
                        foreach (ScriptedInstance instance in ServerManager.Instance.Act6Raids)
                        {
                            if (instance.Id == 23)
                            {
                                RaidInstance = instance;
                                break;
                            }
                        }
                        break;

                    case FactionType.Demon:
                        foreach (ScriptedInstance instance in ServerManager.Instance.Act6Raids)
                        {
                            if (instance.Id == 24)
                            {
                                RaidInstance = instance;
                                break;
                            }
                        }
                        break;
                }
            }

            if (RaidInstance == null)
            {
                Logger.Log.Info(Language.Instance.GetMessageFromKey("CANT_CREATE_RAIDS"));
                return;
            }

            EntryMap = ServerManager.GetMapInstance(
                ServerManager.GetBaseMapInstanceIdByMapId(RaidInstance.MapId));

            if (EntryMap == null)
            {
                Logger.Log.Info(Language.Instance.GetMessageFromKey("MAP_MISSING"));
                return;
            }

            EntryMap.CreatePortalTemp(new Portal
            {
                Type = (byte)PortalType.Raid,
                SourceMapId = RaidInstance.MapId,
                SourceX = RaidInstance.PositionX,
                SourceY = RaidInstance.PositionY
            }, 3600, true);
        }

        #endregion
    }
}
