using OpenNos.DAL.EF;
using OpenNos.Data;
using OpenNos.Domain;

namespace OpenNos.Mapper.Mappers
{
    public static class CharacterTitlesMapper
    {
        #region Methods

        public static bool ToCharacterTitles(CharacterTitlesDTO input, CharacterTitles output)
        {
            if (input == null)
            {
                return false;
            }

            output.TitleKey = input.TitleKey;
            output.CharacterId = input.CharacterId;
            output.TitleId = input.TitleId;

            return true;
        }

        public static bool ToCharacterTitlesDTO(CharacterTitles input, CharacterTitlesDTO output)
        {
            if (input == null)
            {
                return false;
            }

            output.TitleKey = input.TitleKey;
            output.CharacterId = input.CharacterId;
            output.TitleId = input.TitleId;

            return true;
        }

        #endregion
    }
}