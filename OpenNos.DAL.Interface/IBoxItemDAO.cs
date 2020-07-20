using OpenNos.Data;
using System.Collections.Generic;

namespace OpenNos.DAL.Interface
{
    public interface IBoxItemDAO
    {
        #region Methods

        List<BoxItemDTO> LoadAll();

        #endregion
    }
}
