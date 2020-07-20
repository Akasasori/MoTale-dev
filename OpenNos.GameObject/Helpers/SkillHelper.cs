using OpenNos.GameObject.Networking;
using System;

namespace OpenNos.GameObject.Helpers
{
    public class SkillHelper
    {
        public static bool IsCausingChance(short skillVNum)
        {
            switch (skillVNum)
            {
                case 1131: // Blink
                case 1149: // Blink
                case 1158: // Vengeful Spirit Pendulum 1
                case 1165: // Vengeful Spirit Pendulum 2
                    return true;
            }

            return false;
        }

        public static bool IsSelfAttack(short skillVNum)
        {
            switch (skillVNum)
            {
                case 1304: // No name (Laurena's Thunderbolt)
                case 1306: // No name (Laurena's Buff)
                    return true;
            }

            return false;
        }

        public static bool IsManagedSkill(short skillVNum)
        {
            switch (skillVNum)
            {
                case 1306: // No name (Laurena Buff)
                    return true;
            }

            return false;
        }

        public static Skill GetOriginalSkill(Skill skill)
        {
            switch (skill?.SkillVNum)
            {
                case 1113: // Double Lightning
                case 1445: // Lightning Storm
                    return ServerManager.GetSkill(1106);
                case 1125: // Rotating Arrow – Level 1
                case 1126: // Rotating Arrow – Level 2
                    return ServerManager.GetSkill(1122);
                case 1139: // Blade Changer
                case 1140: // Blade Changer
                    return ServerManager.GetSkill(1136);
                case 1165: // Vengeful Spirit Pendulum 2
                case 1166: // Vengeful Spirit Pendulum 3
                    return ServerManager.GetSkill(1158);
            }

            return skill;
        }

        public static bool CalculateNewPosition(MapInstance mapInstance, short x, short y, short cells, ref short mapX, ref short mapY)
        {
            short deltaX = (short)(mapX - x);
            short deltaY = (short)(mapY - y);

            if (cells == 0 || (deltaX == 0 && deltaY == 0))
            {
                return false;
            }

            if (cells > 0)
            {
                double distance = Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));

                while (cells > 0)
                {
                    double scalar = (distance + cells--) / distance;

                    mapX = (short)(x + (deltaX * scalar));
                    mapY = (short)(y + (deltaY * scalar));

                    if (!mapInstance.Map.IsBlockedZone(mapX, mapY))
                    {
                        return true;
                    }
                }
            }
            else
            {
                cells *= -1;

                short velocityX = 0;
                short velocityY = 0;

                if (deltaX != 0)
                {
                    velocityX = deltaX > 0 ? (short)1 : (short)-1;
                }

                if (deltaY != 0)
                {
                    velocityY = deltaY > 0 ? (short)1 : (short)-1;
                }

                velocityX *= cells;
                velocityY *= cells;

                mapX = (short)(x + velocityX);
                mapY = (short)(y + velocityY);

                if (!mapInstance.Map.IsBlockedZone(mapX, mapY))
                {
                    return true;
                }
            }

            return false;
        }
    }
}