using OpenNos.Domain;

namespace OpenNos.GameObject.Helpers
{
    public class MonsterHelper
    {
        public static bool IsKamikaze(short monsterVNum)
        {
            switch (monsterVNum)
            {
                case 854: // Black Meteorite
                case 945: // Bomb
                case 946: // Fire Mine
                case 974: // SP2AM Lotus Flower
                case 1382: // Pumpkin Bomb
                case 1436: // Mobile Trap
                case 1439: // Giant Swirl
                case 2015: // Valakus' Hatchling
                case 2048: // ???
                case 2112: // Summoned Dark Clone (Fire)
                case 2113: // Summoned Dark Clone (Ice)
                case 2114: // Summoned Dark Clone (Light)
                case 2115: // Summoned Dark Clone (Dark)
                    return true;
            }

            return false;
        }

        public static bool IsSelfAttack(short monsterVNum)
        {
            switch (monsterVNum)
            {
                case 2328: // No name (Laurena's Thunderbolt)
                    return true;
            }

            return false;
        }

        public static bool IsNamaju(short monsterVNum)
        {
            switch (monsterVNum)
            {
                case 414: // Namaju
                case 428: // Angry Namaju
                case 429: // Angry Namaju
                case 430: // Angry Namaju
                case 431: // Angry Namaju
                case 432: // Angry Namaju
                    return true;
            }

            return false;
        }

        public static bool UseOwnerEntity(short monsterVNum)
        {
            switch (monsterVNum)
            {
                case 416: // Mini Jajamaru
                case 945: // SP3A Bomb
                case 946: // SP3A Fire Mine
                case 974: // SP2AM Lotus Flower
                case 2112: // Summoned Dark Clone (Fire)
                case 2113: // Summoned Dark Clone (Ice)
                case 2114: // Summoned Dark Clone (Light)
                case 2115: // Summoned Dark Clone (Dark)
                    return true;
            }

            return false;
        }

        public static FactionType GetFaction(short monsterVNum)
        {
            switch (monsterVNum)
            {
                case 679:
                case 972:
                case 965:
                case 967:
                    return FactionType.Angel;

                case 680:
                case 973:
                case 966:
                case 968:
                    return FactionType.Demon;
            }

            return FactionType.None;
        }
    }
}
