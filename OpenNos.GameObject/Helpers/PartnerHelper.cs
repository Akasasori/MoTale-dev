using OpenNos.Domain;
using System.Collections.Generic;
using System.Linq;

namespace OpenNos.GameObject.Helpers
{
    public class PartnerHelper
    {
        private static readonly Dictionary<AttackType, short[]> Map = new Dictionary<AttackType, short[]>
        {
            { AttackType.Melee, new short[] { 318, 319, 2617, 2618 } }, // Tom, Kliff, Frigg, Ragnar
            { AttackType.Range, new short[] { 317, 822, 2640, 2641  } }, // Bob, Leona, Jennifer, Wingless Amora
            { AttackType.Magical, new short[] { 417, 2557, 2620, 2673 } } // Princess Sakura, Graham, Erdimien, Yertirand
        };

        private static AttackType GetAttackType(short itemVNum)
        {
            switch (itemVNum)
            {
                case 4326: // Bone Warrior Ragnar
                case 4343: // Mad Professor Macavity
                case 4349: // Archdaemon Amon
                case 4800: // Aegir the Angry
                case 4804: // Shinobi the Silent
                case 4807: // Foxy
                case 4808: // Maru
                case 4809: // Maru in Mother's Fur
                case 4814: // Amon
                case 4815: // Lucy Lopea﻿rs
                case 4818: // Fiona
                case 4822: // Palina Puppet Master
                case 4825: // Little Pri﻿ncess Venus
                    //RE- Add missing partners
                    return AttackType.Melee;

                case 4324: // Guardian Lucifer
                case 4325: // Sheriff Chloe
                case 4413: // Amora
                case 4417: // Mad March Hare
                case 4802: // Barni the Clever
                case 4805: // Lotus the Graceful
                case 4812: // Lucifer
                case 4817: // Cowgirl Chloe
                case 4821: // Daniel Ducats
                case 4824: // Nelia Nymph
                    return AttackType.Range;

                case 4405: // Magic Student Yuna
                case 4803: // Freya the Fateful
                case 4806: // Orkani the Turbulent
                case 4810: // Hongbi
                case 4811: // Cheongbi
                case 4813: // Witch Laurena
                case 4819: // Jinn
                case 4820: // Ice Princess Eliza
                case 4823: // Harlequin
                    return AttackType.Magical;
            }

            return AttackType.None;
        }

        public static bool CanWearSp(short partnerVNum, short itemVNum)
        {
            AttackType attackType = GetAttackType(itemVNum);

            return Map.ContainsKey(attackType) && Map[attackType].Contains(partnerVNum);
        }
    }
}
