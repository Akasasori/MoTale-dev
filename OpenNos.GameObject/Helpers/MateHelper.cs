using System;
using System.Collections.Generic;
using System.Linq;
using OpenNos.GameObject.Networking;

namespace OpenNos.GameObject.Helpers
{
    public class MateHelper
    {
        #region Instantiation

        public MateHelper()
        {
            LoadConcentrate();
            LoadHpData();
            LoadMinDamageData();
            LoadMaxDamageData();
            LoadMpData();
            LoadXpData();
            LoadDefences();
            LoadPetSkills();
            LoadMateBuffs();
            LoadTrainerUpgradeHits();
            LoadTrainerUpRate();
            LoadTrainerDownRate();
        }

        #endregion

        #region Members

        #endregion

        #region Properties

        public int[,] MeleeDefenseData { get; private set; }

        public int[,] RangeDefenseData { get; private set; }

        public int[,] MagicDefenseData { get; private set; }

        public int[,] MeleeDefenseDodgeData { get; private set; }

        public int[,] RangeDefenseDodgeData { get; private set; }

        public short[] TrainerUpgradeHits { get; private set; }

        public short[] TrainerUpRate { get; private set; }

        public short[] TrainerDownRate { get; private set; }

        public int[,] Concentrate { get; private set; }

        public int[,] HpData { get; private set; }
        
        public int[,] MpData { get; private set; }

        public int[,] MinDamageData { get; private set; }

        public int[,] MaxDamageData { get; private set; }
        
        public double[] XpData { get; private set; }

        // Vnum - CardId
        public Dictionary<int, int> MateBuffs { get; set; }

        public List<int> PetSkills { get; set; }

        #endregion

        #region Methods

        #region PartnerSPSkills

        public short GetUpgradeType(short morph)
        {
            switch (morph)
            {
                case 2043:
                    return 1;
                case 2044:
                    return 2;
                case 2045:
                    return 3;
                case 2046:
                    return 4;
                case 2047:
                    return 5;
                case 2048:
                    return 6;
                case 2310:
                    return 7;
                case 2317:
                    return 8;
                case 2323:
                    return 9;
                case 2325:
                    return 10;
                case 2333:
                    return 11;
                case 2334:
                    return 12;
                case 2343:
                    return 13;
                case 2355:
                    return 14;
                case 2356:
                    return 15;
                case 2367:
                    return 16;
                case 2368:
                    return 17;
                case 2371:
                    return 18;
                case 2372:
                    return 19;
                case 2373:
                    return 20;
                case 2374:
                    return 21;
                case 2376:
                    return 22;
                case 2377:
                    return 23;
                case 2378:
                    return 24;
                case 2537:
                    return 25;
                case 2538:
                    return 26;
            }
            return -1;
        }

        #endregion
        
        #region Concentrate

        public void LoadConcentrate()
        {
            Concentrate = new int[3, 255];

            int baseValue = 0;

            for (int i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        baseValue = 424;
                        break;
                    case 1:
                        baseValue = 702;
                        break;
                    case 2:
                        baseValue = 702;
                        break;
                }

                for (int j = 0; j < 255; j++)
                {
                    Concentrate[i, j] = (short)(baseValue * j / 255);
                }
            }
        }

        #endregion

        #region HP

        public void LoadHpData()
        {
            HpData = new int[3, 255];

            int baseValue = 0;

            for (int i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        baseValue = 22628;
                        break;
                    case 1:
                        baseValue = 12728;
                        break;
                    case 2:
                        baseValue = 32573;
                        break;
                }

                for (int j = 0; j < 255; j++)
                {
                    HpData[i, j] = (short)(baseValue * j / 255);
                }
            }
        }

        #endregion

        #region Damage

        public void LoadMinDamageData()
        {
            MinDamageData = new int[3, 255];

            int baseValue = 0;

            for (int i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        baseValue = 542;
                        break;
                    case 1:
                        baseValue = 726;
                        break;
                    case 2:
                        baseValue = 932;
                        break;
                }

                for (int j = 0; j < 255; j++)
                {
                    MinDamageData[i, j] = (short)(baseValue * j / 255);
                }
            }
        }

        public void LoadMaxDamageData()
        {
            MaxDamageData = new int[3, 255];

            int baseValue = 0;

            for (int i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        baseValue = 719;
                        break;
                    case 1:
                        baseValue = 945;
                        break;
                    case 2:
                        baseValue = 1161;
                        break;
                }

                for (int j = 0; j < 255; j++)
                {
                    MaxDamageData[i, j] = (short)(baseValue * j / 255);
                }
            }
        }

        #endregion

        #region List

        public void LoadPetSkills()
        {
            PetSkills = new List<int>
            {
                1513, // Purcival 
                1514, // Baron scratch ?
                1515, // Amiral (le chat chelou) 
                1516, // roi des pirates pussifer 
                1524, // Miaou fou
                1575, // Marié Bouhmiaou 
                1576 // Marie Bouhmiaou 
            };
        }

        private void LoadMateBuffs()
        {
            MateBuffs = new Dictionary<int, int>
            {
                { 536, 162 }, // LUCKY PIG
                { 178, 108 }, // LUCKY PIG
                { 670, 374 }, // FIBI
                { 829, 377 }, // RUDY ROWDY
                { 830, 377 }, // RUDY ROWDY
                { 836, 381 }, // FLUFFY BALLY
                { 838, 385 }, // NAVY BUSHTAIL
                { 840, 442 }, // LEO THE COWARD
                { 841, 394 }, // RATUFU NINJA
                { 842, 399 }, // INDIAN BUSHTAIL
                { 843, 403 }, // VIKING BUSHTAIL
                { 844, 391 }, // COWBOY BUSHTAIL
                { 2105, 383 }, // INFERNO
                { 2704, 710 },
                { 2709, 711 },
                { 683, 742 }, // NUTRIA
            };
        }

        #endregion

        #region MP

        public void LoadMpData()
        {
            MpData = new int[3, 255];

            int baseValue = 0;

            for (int i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        baseValue = 7071;
                        break;
                    case 1:
                        baseValue = 9900;
                        break;
                    case 2:
                        baseValue = 18385;
                        break;
                }

                for (int j = 0; j < 255; j++)
                {
                    MpData[i, j] = (short)(baseValue * j / 255);
                }
            }
        }

        #endregion

        #region XP

        public void LoadXpData()
        {
            // Load XpData
            XpData = new double[256];
            double[] v = new double[256];
            double var = 1;
            v[0] = 540;
            v[1] = 960;
            XpData[0] = 300;
            for (int i = 2; i < v.Length; i++)
            {
                v[i] = v[i - 1] + 420 + 120 * (i - 1);
            }

            for (int i = 1; i < XpData.Length; i++)
            {
                if (i < 79)
                {
                    switch (i)
                    {
                        case 14:
                            var = 6 / 3d;
                            break;
                        case 39:
                            var = 19 / 3d;
                            break;
                        case 59:
                            var = 70 / 3d;
                            break;
                    }

                    XpData[i] = Convert.ToInt64(XpData[i - 1] + var * v[i - 1]);
                }

                if (i < 79)
                {
                    continue;
                }

                switch (i)
                {
                    case 79:
                        var = 5000;
                        break;
                    case 82:
                        var = 9000;
                        break;
                    case 84:
                        var = 13000;
                        break;
                }

                XpData[i] = Convert.ToInt64(XpData[i - 1] + var * (i + 2) * (i + 2));
            }
        }

        #endregion

        #region Defences
        
        public void LoadDefences()
        {
            MeleeDefenseData = new int[3, 255];
            int baseValue = 0;
            for (int i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        baseValue = 340;
                        break;
                    case 1:
                        baseValue = 382;
                        break;
                    case 2:
                        baseValue = 340;
                        break;
                }

                for (int j = 0; j < 255; j++)
                {
                    MeleeDefenseData[i, j] = (short)(baseValue * j / 255);
                }
            }

            RangeDefenseData = new int[3, 255];
            baseValue = 0;
            for (int i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        baseValue = 612;
                        break;
                    case 1:
                        baseValue = 510;
                        break;
                    case 2:
                        baseValue = 425;
                        break;
                }

                for (int j = 0; j < 255; j++)
                {
                    RangeDefenseData[i, j] = (short)(baseValue * j / 255);
                }
            }

            MagicDefenseData = new int[3, 255];
            baseValue = 0;
            for (int i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        baseValue = 510;
                        break;
                    case 1:
                        baseValue = 425;
                        break;
                    case 2:
                        baseValue = 680;
                        break;
                }

                for (int j = 0; j < 255; j++)
                {
                    MagicDefenseData[i, j] = (short)(baseValue * j / 255);
                }
            }

            MeleeDefenseDodgeData = new int[3, 255];
            baseValue = 0;
            for (int i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        baseValue = 540;
                        break;
                    case 1:
                        baseValue = 945;
                        break;
                    case 2:
                        baseValue = 475;
                        break;
                }

                for (int j = 0; j < 255; j++)
                {
                    MeleeDefenseDodgeData[i, j] = (short)(baseValue * j / 255);
                }
            }

            RangeDefenseDodgeData = new int[3, 255];
            baseValue = 0;
            for (int i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        baseValue = 540;
                        break;
                    case 1:
                        baseValue = 945;
                        break;
                    case 2:
                        baseValue = 475;
                        break;
                }

                for (int j = 0; j < 255; j++)
                {
                    RangeDefenseDodgeData[i, j] = (short)(baseValue * j / 255);
                }
            }
        }

        #endregion

        #region Trainers

        public void LoadTrainerUpgradeHits()
        {
            TrainerUpgradeHits = new short[10];

            short baseValue = 0;

            for (int i = 0; i < 10; i++)
            {
                baseValue += 50;
                TrainerUpgradeHits[i] = baseValue;
            }
        }
        
        public void LoadTrainerUpRate()
        {
            TrainerUpRate = new short[] { 67, 67, 44, 34, 22, 15, 14, 8, 1, 0 };
        }

        public void LoadTrainerDownRate()
        {
            TrainerDownRate = new short[] { 0, 7, 13, 16, 28, 29, 33, 36, 50, 60 };
        }

        #endregion

        #endregion

        #region Singleton

        private static MateHelper _instance;

        public static MateHelper Instance => _instance ?? (_instance = new MateHelper());

        #endregion
    }
}