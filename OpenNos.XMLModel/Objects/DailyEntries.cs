﻿using System;
using System.Xml.Serialization;

namespace OpenNos.XMLModel.Objects
{
    [Serializable]
    public class DailyEntries
    {
        #region Properties

        [XmlAttribute]
        public short Value { get; set; }

        #endregion
    }
}