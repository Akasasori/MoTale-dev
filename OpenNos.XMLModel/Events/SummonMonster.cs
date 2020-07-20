﻿using System;
using System.Xml.Serialization;

namespace OpenNos.XMLModel.Events
{
    [Serializable]
    public class SummonMonster
    {
        #region Properties

        [XmlAttribute]
        public bool IsBoss { get; set; }

        [XmlAttribute]
        public bool IsHostile { get; set; }

        [XmlAttribute]
        public bool IsTarget { get; set; }

        [XmlAttribute]
        public bool Move { get; set; }

        [XmlAttribute]
        public bool IsBonus { get; set; }

        [XmlAttribute]
        public bool IsMeteorite { get; set; }

        [XmlAttribute]
        public short Damage { get; set; }

        [XmlElement]
        public OnDeath OnDeath { get; set; }

        [XmlElement]
        public OnNoticing OnNoticing { get; set; }

        [XmlAttribute]
        public byte NoticeRange { get; set; }

        [XmlAttribute]
        public short PositionX { get; set; }

        [XmlAttribute]
        public short PositionY { get; set; }

        [XmlElement]
        public Roam Roam { get; set; }

        [XmlAttribute]
        public short VNum { get; set; }

        [XmlAttribute]
        public short HasDelay { get; set; }

        [XmlElement]
        public SendMessage[] SendMessage { get; set; }

        [XmlElement]
        public UseSkillOnDamage[] UseSkillOnDamage { get; set; }

        [XmlElement]
        public Effect Effect { get; set; }

        [XmlElement]
        public RemoveAfter RemoveAfter { get; set; }

        #endregion
    }
}