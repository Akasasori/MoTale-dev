using System;
using System.Collections.Generic;
using System.Linq;
using OpenNos.Core;
using OpenNos.DAL.EF;
using OpenNos.DAL.Interface;
using OpenNos.Data;
using OpenNos.Data.Enums;
using OpenNos.DAL.EF.Helpers;

namespace OpenNos.DAL.DAO
{
    public class CharacterQuestDAO : ICharacterQuestDAO
    {
        #region Methods

        public DeleteResult Delete(long characterId, long questId)
        {
            try
            {
                using (OpenNosContext context = DataAccessHelper.CreateContext())
                {
                    CharacterQuest charQuest = context.CharacterQuest.FirstOrDefault(i => i.CharacterId == characterId && i.QuestId == questId);
                    if (charQuest != null)
                    {
                        context.CharacterQuest.Remove(charQuest);
                        context.SaveChanges();
                    }
                    return DeleteResult.Deleted;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return DeleteResult.Error;
            }
        }

        public CharacterQuestDTO InsertOrUpdate(CharacterQuestDTO charQuest)
        {
            try
            {
                using (OpenNosContext context = DataAccessHelper.CreateContext())
                {
                    return InsertOrUpdate(context, charQuest);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Message: {e.Message}", e);
                return null;
            }
        }
        
        protected static CharacterQuestDTO InsertOrUpdate(OpenNosContext context, CharacterQuestDTO dto)
        {
            Guid primaryKey = dto.Id;
            CharacterQuest entity = context.Set<CharacterQuest>().FirstOrDefault(c => c.Id == primaryKey);
            if (entity == null)
            {
                return Insert(dto, context);
            }
            else
            {
                return Update(entity, dto, context);
            }
        }
        
        private static CharacterQuestDTO Insert(CharacterQuestDTO charQuest, OpenNosContext context)
        {
            CharacterQuest entity = new CharacterQuest();
            Mapper.Mappers.CharacterQuestMapper.ToCharacterQuest(charQuest, entity);
            context.CharacterQuest.Add(entity);
            context.SaveChanges();
            if (Mapper.Mappers.CharacterQuestMapper.ToCharacterQuestDTO(entity, charQuest))
            {
                return charQuest;
            }

            return null;
        }

        private static CharacterQuestDTO Update(CharacterQuest entity, CharacterQuestDTO charQuest, OpenNosContext context)
        {
            if (entity != null)
            {
                Mapper.Mappers.CharacterQuestMapper.ToCharacterQuest(charQuest, entity);
                context.SaveChanges();
            }

            if (Mapper.Mappers.CharacterQuestMapper.ToCharacterQuestDTO(entity, charQuest))
            {
                return charQuest;
            }

            return null;
        }

        public IEnumerable<CharacterQuestDTO> LoadByCharacterId(long characterId)
        {
            using (OpenNosContext context = DataAccessHelper.CreateContext())
            {
                List<CharacterQuestDTO> result = new List<CharacterQuestDTO>();
                foreach (CharacterQuest charQuest in context.CharacterQuest.Where(s => s.CharacterId == characterId))
                {
                    CharacterQuestDTO dto = new CharacterQuestDTO();
                    Mapper.Mappers.CharacterQuestMapper.ToCharacterQuestDTO(charQuest, dto);
                    result.Add(dto);
                }
                return result;
            }
        }

        public IEnumerable<Guid> LoadKeysByCharacterId(long characterId)
        {
            try
            {
                using (OpenNosContext context = DataAccessHelper.CreateContext())
                {
                    return context.CharacterQuest.Where(i => i.CharacterId == characterId).Select(c => c.Id).ToList();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        #endregion
    }
}