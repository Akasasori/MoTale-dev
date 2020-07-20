﻿using System;
using System.Xml.Serialization;

namespace OpenNos.XMLModel.Objects
{
    [Serializable]
    public class Item
    {
        #region Properties

        [XmlAttribute]
        public short Amount { get; set; }

        [XmlAttribute]
        public short Design { get; set; }

        [XmlAttribute]
        public bool IsRandomRare { get; set; }

        [XmlAttribute]
        public short VNum { get; set; }

        [XmlAttribute]
        public byte MinTeamSize { get; set; }

        [XmlAttribute]
        public byte MaxTeamSize { get; set; }

        #endregion
    }
}