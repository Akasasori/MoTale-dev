using System.Collections.Generic;
using OpenNos.DAL.EF;
using OpenNos.DAL.EF.Helpers;
using OpenNos.DAL.Interface;
using OpenNos.Data;

namespace OpenNos.DAL.DAO
{
    public class BoxItemDAO : IBoxItemDAO
    {
        #region Methods

        public List<BoxItemDTO> LoadAll()
        {
            using (OpenNosContext context = DataAccessHelper.CreateContext())
            {
                List<BoxItemDTO> result = new List<BoxItemDTO>();

                foreach (BoxItem boxItem in context.BoxItem)
                {
                    BoxItemDTO dto = new BoxItemDTO();
                    Mapper.Mappers.BoxItemMapper.ToBoxItemDTO(boxItem, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        #endregion
    }
}
