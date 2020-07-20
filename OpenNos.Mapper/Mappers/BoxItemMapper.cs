using OpenNos.DAL.EF;
using OpenNos.Data;

namespace OpenNos.Mapper.Mappers
{
    public static class BoxItemMapper
    {
        #region Methods

        public static bool ToBoxItem(BoxItemDTO input, BoxItem output)
        {
            if (output == null || input == null)
            {
                return false;
            }

            output.BoxItemId = input.BoxItemId;
            output.OriginalItemVNum = input.OriginalItemVNum;
            output.OriginalItemDesign = input.OriginalItemDesign;
            output.ItemGeneratedAmount = input.ItemGeneratedAmount;
            output.ItemGeneratedVNum = input.ItemGeneratedVNum;
            output.ItemGeneratedDesign = input.ItemGeneratedDesign;
            output.ItemGeneratedRare = input.ItemGeneratedRare;
            output.ItemGeneratedUpgrade = input.ItemGeneratedUpgrade;
            output.Probability = input.Probability;

            return true;
        }

        public static bool ToBoxItemDTO(BoxItem input, BoxItemDTO output)
        {
            if (input == null || output == null)
            {
                return false;
            }

            output.BoxItemId = input.BoxItemId;
            output.OriginalItemVNum = input.OriginalItemVNum;
            output.OriginalItemDesign = input.OriginalItemDesign;
            output.ItemGeneratedAmount = input.ItemGeneratedAmount;
            output.ItemGeneratedVNum = input.ItemGeneratedVNum;
            output.ItemGeneratedDesign = input.ItemGeneratedDesign;
            output.ItemGeneratedRare = input.ItemGeneratedRare;
            output.ItemGeneratedUpgrade = input.ItemGeneratedUpgrade;
            output.Probability = input.Probability;

            return true;
        }

        #endregion
    }
}