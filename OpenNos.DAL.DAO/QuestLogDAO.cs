using System;
using System.Collections.Generic;
using System.Linq;
using OpenNos.Core;
using OpenNos.DAL.EF;
using OpenNos.DAL.Interface;
using OpenNos.Data;
using OpenNos.Data.Enums;
using OpenNos.DAL.EF.Helpers;
using System.Data.Entity;

namespace OpenNos.DAL.DAO
{
    public class QuestLogDAO : IQuestLogDAO
    {
        public SaveResult InsertOrUpdate(ref QuestLogDTO quest)
        {
            try
            {
                using (OpenNosContext context = DataAccessHelper.CreateContext())
                {
                    long questId = quest.QuestId;
                    long characterId = quest.CharacterId;
                    QuestLog entity = context.QuestLog.FirstOrDefault(c => c.QuestId.Equals(questId) && c.CharacterId.Equals(characterId));

                    if (entity == null)
                    {
                        quest = Insert(quest, context);
                        return SaveResult.Inserted;
                    }

                    quest.QuestId = entity.QuestId;
                    quest = Update(entity, quest, context);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return SaveResult.Error;
            }
        }

        public QuestLogDTO Insert(QuestLogDTO questLog, OpenNosContext context)
        {
            try
            {
                QuestLog entity = new QuestLog();
                Mapper.Mappers.QuestLogMapper.ToQuestLog(questLog, entity);
                context.QuestLog.Add(entity);
                context.SaveChanges();
                if (Mapper.Mappers.QuestLogMapper.ToQuestLogDTO(entity, questLog))
                {
                    return questLog;
                }

                return null;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public QuestLogDTO Update(QuestLog old, QuestLogDTO replace, OpenNosContext context)
        {
            if (old != null)
            {
                Mapper.Mappers.QuestLogMapper.ToQuestLog(replace, old);
                context.Entry(old).State = EntityState.Modified;
                context.SaveChanges();
            }
            if (Mapper.Mappers.QuestLogMapper.ToQuestLogDTO(old, replace))
            {
                return replace;
            }

            return null;
        }

        public QuestLogDTO LoadById(long id)
        {
            try
            {
                using (OpenNosContext context = DataAccessHelper.CreateContext())
                {
                    QuestLogDTO dto = new QuestLogDTO();
                    if (Mapper.Mappers.QuestLogMapper.ToQuestLogDTO(context.QuestLog.FirstOrDefault(i => i.Id.Equals(id)), dto))
                    {
                        return dto;
                    }

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public IEnumerable<QuestLogDTO> LoadByCharacterId(long characterId)
        {
            using (OpenNosContext context = DataAccessHelper.CreateContext())
            {
                List<QuestLogDTO> result = new List<QuestLogDTO>();
                foreach (QuestLog questLog in context.QuestLog.Where(s => s.CharacterId == characterId))
                {
                    QuestLogDTO dto = new QuestLogDTO();
                    Mapper.Mappers.QuestLogMapper.ToQuestLogDTO(questLog, dto);
                    result.Add(dto);
                }
                return result;
            }
        }
    }
}