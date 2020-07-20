using OpenNos.Core;

namespace OpenNos.GameObject.Helpers
{
    public class RewardsHelper
    {
        #region Methods

        public void GetLevelUpRewards(ClientSession session)
        {
            switch (session.Character.Level)
            {
                case 15:
                    session.Character.GiftAdd(1452, 1);
                    session.Character.AddQuest(1515);
                    break;

                case 20:
                    session.Character.GiftAdd(9301, 1);
                    break;

                case 30:
                    session.Character.GiftAdd(9302, 1);
                    break;

                case 25:
                    session.Character.GiftAdd(4129, 1);
                    break;

                case 32:
                    session.Character.GiftAdd(4130, 1);
                    break;
                case 34:
                    session.SendPacket(UserInterfaceHelper.GenerateModal("Lv. 34! You got   a gift.", 11));
                    session.Character.GiftAdd(1011, 10);
                    switch (session.Character.Class)
                    {
                        case Domain.ClassType.Swordsman:
                            session.Character.GiftAdd(22, 1, 2, 5);
                            session.Character.GiftAdd(73, 1, 2, 5);
                            session.Character.GiftAdd(99, 1, 2, 5);
                            break;
                        case Domain.ClassType.Archer:
                            session.Character.GiftAdd(36, 1, 2, 5);
                            session.Character.GiftAdd(81, 1, 2, 5);
                            session.Character.GiftAdd(112, 1, 2, 5);
                            break;
                        case Domain.ClassType.Magician:
                            session.Character.GiftAdd(50, 1, 2, 5);
                            session.Character.GiftAdd(89, 1, 2, 5);
                            session.Character.GiftAdd(125, 1, 2, 5);
                            break;
                    }
                    break;
                case 40:
                    session.Character.GiftAdd(1011, 10);
                    session.Character.GiftAdd(1363, 2);
                    session.Character.GiftAdd(2282, 30);
                    session.Character.GiftAdd(1030, 30);
                    session.Character.GiftAdd(9303, 1);
                    break;

                case 44:
                    session.Character.GiftAdd(4131, 1);
                    break;

                case 50:
                    session.Character.GiftAdd(1244, 30);
                    session.Character.GiftAdd(9304, 1);
                    break;
                case 55:
                    session.Character.GiftAdd(4132, 1);
                    break;
                case 58:
                    session.SendPacket(UserInterfaceHelper.GenerateModal("Lv. 58! You got a gift.", 11));
                    switch (session.Character.Class)
                    {
                        case Domain.ClassType.Swordsman:
                            session.Character.GiftAdd(140, 1, 3, 5);
                            session.Character.GiftAdd(77, 1, 3, 5);
                            session.Character.GiftAdd(297, 1, 3, 5);
                            break;
                        case Domain.ClassType.Archer:
                            session.Character.GiftAdd(147, 1, 3, 5);
                            session.Character.GiftAdd(85, 1, 3, 5);
                            session.Character.GiftAdd(295, 1, 3, 5);
                            break;
                        case Domain.ClassType.Magician:
                            session.Character.GiftAdd(154, 1, 3, 5);
                            session.Character.GiftAdd(93, 1, 3, 5);
                            session.Character.GiftAdd(271, 1, 3, 5);
                            break;
                    }
                    break;
                case 60:
                    session.Character.GiftAdd(1011, 10);
                    session.Character.GiftAdd(1363, 2);
                    session.Character.GiftAdd(9305, 1);
                    break;
                case 65:
                    session.SendPacket(UserInterfaceHelper.GenerateModal("Lv. 65! You got a gift.", 11));
                    switch (session.Character.Class)
                    {
                        case Domain.ClassType.Swordsman:
                            session.Character.GiftAdd(263, 1, 4, 5);
                            session.Character.GiftAdd(292, 1, 4, 5);
                            session.Character.GiftAdd(298, 1, 4, 5);
                            break;
                        case Domain.ClassType.Archer:
                            session.Character.GiftAdd(266, 1, 4, 5);
                            session.Character.GiftAdd(290, 1, 4, 5);
                            session.Character.GiftAdd(296, 1, 4, 5);
                            break;
                        case Domain.ClassType.Magician:
                            session.Character.GiftAdd(269, 1, 4, 5);
                            session.Character.GiftAdd(294, 1, 4, 5);
                            session.Character.GiftAdd(272, 1, 4, 5);
                            break;
                    }
                    break;
                case 70:
                    session.Character.GiftAdd(1244, 30);
                    session.Character.GiftAdd(1363, 3);
                    session.Character.GiftAdd(9306, 1);
                    session.Character.GiftAdd(1011, 10);
                    break;
                case 71:
                    session.SendPacket(UserInterfaceHelper.GenerateModal("Lv. 71! You got a gift.", 11));
                    switch (session.Character.Class)
                    {
                        case Domain.ClassType.Swordsman:
                            session.Character.GiftAdd(400, 1, 5, 5);
                            session.Character.GiftAdd(761, 1, 5, 5);
                            session.Character.GiftAdd(994, 1, 5, 5);
                            break;
                        case Domain.ClassType.Archer:
                            session.Character.GiftAdd(403, 1, 5, 5);
                            session.Character.GiftAdd(405, 1, 5, 5);
                            session.Character.GiftAdd(993, 1, 5, 5);
                            break;
                        case Domain.ClassType.Magician:
                            session.Character.GiftAdd(406, 1, 5, 5);
                            session.Character.GiftAdd(408, 1, 5, 5);
                            session.Character.GiftAdd(989, 1, 5, 5);
                            break;
                    }
                    break;
                case 79:
                    session.SendPacket(UserInterfaceHelper.GenerateModal("Lv. 79! You got a gift.", 11));
                    switch (session.Character.Class)
                    {
                        case Domain.ClassType.Swordsman:
                            session.Character.GiftAdd(401, 1, 6, 5);
                            session.Character.GiftAdd(4006, 1, 6, 5);
                            session.Character.GiftAdd(409, 1, 6, 5);
                            break;
                        case Domain.ClassType.Archer:
                            session.Character.GiftAdd(404, 1, 6, 5);
                            session.Character.GiftAdd(4008, 1, 6, 5);
                            session.Character.GiftAdd(410, 1, 6, 5);
                            break;
                        case Domain.ClassType.Magician:
                            session.Character.GiftAdd(407, 1, 6, 5);
                            session.Character.GiftAdd(4010, 1, 6, 5);
                            session.Character.GiftAdd(411, 1, 6, 5);
                            break;
                    }
                    break;  

                    case 80:
                    session.Character.GiftAdd(9307, 1);
                    break;

                case 85:
                    session.SendPacket(UserInterfaceHelper.GenerateModal("Lv. 85! You got a gift.", 11));
                    session.Character.GiftAdd(1244, 30);
                    switch (session.Character.Class)
                    {
                        case Domain.ClassType.Swordsman:
                            session.Character.GiftAdd(349, 1, 6, 5);
                            session.Character.GiftAdd(352, 1, 6, 5);
                            session.Character.GiftAdd(4012, 1, 6, 5);
                            break;
                        case Domain.ClassType.Archer:
                            session.Character.GiftAdd(353, 1, 6, 5);
                            session.Character.GiftAdd(351, 1, 6, 5);
                            session.Character.GiftAdd(4015, 1, 6, 5);
                            break;
                        case Domain.ClassType.Magician:
                            session.Character.GiftAdd(356, 1, 6, 5);
                            session.Character.GiftAdd(355, 1, 6, 5);
                            session.Character.GiftAdd(4018, 1, 6, 5);
                            break;
                        case Domain.ClassType.MartialArtist:
                            session.Character.GiftAdd(4724, 1, 6, 8);
                            session.Character.GiftAdd(4761, 1, 6, 8);
                            session.Character.GiftAdd(4742, 1, 6, 8);
                            break;
                    }
                    break;
                case 90:
                    session.Character.GiftAdd(1242, 20);
                    session.Character.GiftAdd(1364, 5);
                    session.Character.GiftAdd(9308, 1);
                    break;

                case 99:
                    session.Character.GiftAdd(9309, 1);
                    session.Character.GiftAdd(1364, 5);
                    break;
            }
        }
        public static int ArenaXpReward(byte characterLevel)
        {
            if (characterLevel <= 39)
            {
                // 50%
                return (int)(CharacterHelper.XPData[characterLevel] / 2);
            }

            if (characterLevel <= 55)
            {
                // 45%
                return (int)(CharacterHelper.XPData[characterLevel] / 3);
            }

            if (characterLevel <= 75)
            {
                // 20%
                return (int)(CharacterHelper.XPData[characterLevel] / 5);
            }

            if (characterLevel <= 79)
            {
                // 10%
                return (int)(CharacterHelper.XPData[characterLevel] / 10);
            }

            if (characterLevel <= 85)
            {
                // 4%
                return (int)(CharacterHelper.XPData[characterLevel] / 25);
            }

            if (characterLevel <= 90)
            {
                return (int)(CharacterHelper.XPData[characterLevel] / 40);
            }

            if (characterLevel <= 93)
            {
                return (int)(CharacterHelper.XPData[characterLevel] / 50);
            }

            if (characterLevel <= 99)
            {
                return (int)(CharacterHelper.XPData[characterLevel] / 125);
            }

            return 0;
        }

        #endregion


        #region Singleton

        private static RewardsHelper _instance;

        public static RewardsHelper Instance => _instance ?? (_instance = new RewardsHelper());

        #endregion
    }
}