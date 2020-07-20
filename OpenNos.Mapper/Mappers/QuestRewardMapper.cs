using OpenNos.DAL.EF;
using OpenNos.Data;

namespace OpenNos.Mapper.Mappers
{
    public static class QuestRewardMapper
    {
        #region Methods

        public static bool ToQuestReward(QuestRewardDTO input, QuestReward output)
        {
            if (input == null)
            {
                return false;
            }

            output.QuestRewardId = input.QuestRewardId;
            output.RewardType = input.RewardType;
            output.Data = input.Data;
            output.Design = input.Design;
            output.Rarity = input.Rarity;
            output.Upgrade = input.Upgrade;
            output.Amount = input.Amount;
            output.QuestId = input.QuestId;

            return true;
        }

        public static bool ToQuestRewardDTO(QuestReward input, QuestRewardDTO output)
        {
            if (input == null)
            {
                return false;
            }

            output.QuestRewardId = input.QuestRewardId;
            output.RewardType = input.RewardType;
            output.Data = input.Data;
            output.Design = input.Design;
            output.Rarity = input.Rarity;
            output.Upgrade = input.Upgrade;
            output.Amount = input.Amount;
            output.QuestId = input.QuestId;

            return true;
        }

        #endregion
    }
}