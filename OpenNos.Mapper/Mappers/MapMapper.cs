using OpenNos.DAL.EF;
using OpenNos.Data;

namespace OpenNos.Mapper.Mappers
{
    public static class MapMapper
    {
        #region Methods

        public static bool ToMap(MapDTO input, Map output)
        {
            if (input == null)
            {
                return false;
            }

            output.Data = input.Data;
            output.MapId = input.MapId;
            output.SecondMapId = input.SecondMapId;
            output.Music = input.Music;
            output.Name = input.Name;
            output.ShopAllowed = input.ShopAllowed;
            output.XpRate = input.XpRate;

            return true;
        }

        public static bool ToMapDTO(Map input, MapDTO output)
        {
            if (input == null)
            {
                return false;
            }

            output.Data = input.Data;
            output.MapId = input.MapId;
            output.SecondMapId = input.SecondMapId;
            output.Music = input.Music;
            output.Name = input.Name;
            output.ShopAllowed = input.ShopAllowed;
            output.XpRate = input.XpRate;

            return true;
        }

        #endregion
    }
}