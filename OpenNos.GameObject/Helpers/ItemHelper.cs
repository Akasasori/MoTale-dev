using OpenNos.Domain;
using System.Collections.Generic;

namespace OpenNos.GameObject.Helpers
{
    public class ItemHelper
    {
        #region Properties

        public static readonly byte[] RareRate = new byte[] { 100, 80, 70, 50, 30, 15, 5, 1 };

        public static readonly byte[] BuyCraftRareRate = new byte[] { 100, 100, 63, 48, 35, 24, 14, 6 };

        public static readonly byte[] RarifyRate = new byte[] { 80, 70, 60, 40, 30, 15, 10, 5, 3, 2, 1 };

        public static readonly byte[] SpUpFailRate = new byte[] { 20, 25, 30, 40, 50, 60, 65, 70, 75, 80, 90, 93, 95, 97, 99 };

        public static readonly byte[] SpDestroyRate = new byte[] { 0, 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 70 };

        public static readonly byte[] ItemUpgradeFixRate = new byte[] { 0, 0, 10, 15, 20, 20, 20, 20, 15, 14 };

        public static readonly byte[] ItemUpgradeFailRate = new byte[] { 0, 0, 0, 5, 20, 40, 60, 70, 80, 85 };

        public static readonly byte[] R8ItemUpgradeFixRate = new byte[] { 50, 40, 70, 65, 80, 90, 95, 97, 98, 99 };

        public static readonly byte[] R8ItemUpgradeFailRate = new byte[] { 50, 40, 60, 50, 60, 70, 75, 77, 83, 89 };


        #region Rates Event

        public static readonly byte[] SPUpLuckRateEvent = new byte[] { 100, 100, 100, 90, 75, 60, 51, 45, 47, 30, 15, 10, 7, 4, 2 };

        public static readonly byte[] ItemUpgradeFailRateEvent = new byte[] { 0, 0, 0, 0, 0, 0, 20, 40, 60, 70 };

        public static readonly byte[] R8ItemUpgradeFailRateEvent = new byte[] { 0, 0, 20, 0, 20, 40, 50, 54, 76, 78 };

        public static readonly byte[] RarifyRateEvent = new byte[] { 100, 100, 90, 60, 45, 30, 20, 10, 6, 4, 2 };



        #endregion
        #endregion

        #region Singleton

        private static PassiveSkillHelper _instance;

        public static PassiveSkillHelper Instance => _instance ?? (_instance = new PassiveSkillHelper());

        #endregion
    }
}