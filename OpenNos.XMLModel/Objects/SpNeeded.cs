using System;
using System.Xml.Serialization;

namespace OpenNos.XMLModel.Objects
{
    [Serializable]
    public class SpNeeded
    {
        #region Properties

        [XmlAttribute]
        public int Adventurer { get; set; }

        [XmlAttribute]
        public int Swordman { get; set; }

        [XmlAttribute]
        public int Archer { get; set; }

        [XmlAttribute]
        public int Magician { get; set; }

        [XmlAttribute]
        public int MartialArtist { get; set; }

        #endregion
    }
}