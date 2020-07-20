using OpenNos.Data;
using OpenNos.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNos.DAL.Interface
{
    public interface ICharacterTitlesDAO
    {
        IEnumerable<CharacterTitlesDTO> LoadByCharacterId(long characterId);

        SaveResult InsertOrUpdate(ref CharacterTitlesDTO character);
    }
}