using OpenNos.Core;
using OpenNos.DAL.EF;
using OpenNos.DAL.EF.Helpers;
using OpenNos.DAL.Interface;
using OpenNos.Data;
using OpenNos.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenNos.DAL.DAO
{
    public class CharacterTitlesDAO : ICharacterTitlesDAO
    {
        public IEnumerable<CharacterTitlesDTO> LoadByCharacterId(long characterId)
        {
            using (OpenNosContext context = DataAccessHelper.CreateContext())
            {
                List<CharacterTitlesDTO> result = new List<CharacterTitlesDTO>();
                foreach (CharacterTitles entity in context.CharacterTitles.Where(i => i.CharacterId == characterId))
                {
                    CharacterTitlesDTO output = new CharacterTitlesDTO();
                    Mapper.Mappers.CharacterTitlesMapper.ToCharacterTitlesDTO(entity, output);
                    result.Add(output);
                }
                return result;
            }
        }

        private static CharacterTitlesDTO insert(CharacterTitlesDTO CharacterTitles, OpenNosContext context)
        {
            CharacterTitles entity = new CharacterTitles();
            Mapper.Mappers.CharacterTitlesMapper.ToCharacterTitles(CharacterTitles, entity);
            context.CharacterTitles.Add(entity);
            context.SaveChanges();
            if (Mapper.Mappers.CharacterTitlesMapper.ToCharacterTitlesDTO(entity, CharacterTitles))
            {
                return CharacterTitles;
            }
            return null;
        }

        private static CharacterTitlesDTO update(CharacterTitles entity, CharacterTitlesDTO CharacterTitles, OpenNosContext context)
        {
            if (entity != null)
            {
                // State Updates should only occur upon deleting CharacterTitles, so outside of this method.
                long state = entity.TitleKey;
                Mapper.Mappers.CharacterTitlesMapper.ToCharacterTitles(CharacterTitles, entity);
                entity.TitleKey = state;

                context.SaveChanges();
            }

            if (Mapper.Mappers.CharacterTitlesMapper.ToCharacterTitlesDTO(entity, CharacterTitles))
            {
                return CharacterTitles;
            }

            return null;
        }

        public SaveResult InsertOrUpdate(ref CharacterTitlesDTO CharacterTitles)
        {
            try
            {
                using (OpenNosContext context = DataAccessHelper.CreateContext())
                {
                    long Titlekey = CharacterTitles.TitleKey;
                    CharacterTitles entity = context.CharacterTitles.FirstOrDefault(c => c.TitleKey.Equals(Titlekey));
                    if (entity == null)
                    {
                        CharacterTitles = insert(CharacterTitles, context);
                        return SaveResult.Inserted;
                    }
                    CharacterTitles = update(entity, CharacterTitles, context);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format(Language.Instance.GetMessageFromKey("INSERT_ERROR"), CharacterTitles, e.Message), e);
                return SaveResult.Error;
            }
        }
    }
}

