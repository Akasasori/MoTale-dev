namespace OpenNos.Data
{
    public class BoxItemDTO
    {
        #region Properties

        public long BoxItemId { get; set; }

        public short OriginalItemVNum { get; set; }

        public short OriginalItemDesign { get; set; }

        public short ItemGeneratedAmount { get; set; }

        public short ItemGeneratedVNum { get; set; }

        public short ItemGeneratedDesign { get; set; }

        public byte ItemGeneratedRare { get; set; }

        public byte ItemGeneratedUpgrade { get; set; }

        public byte Probability { get; set; }

        #endregion
    }
}