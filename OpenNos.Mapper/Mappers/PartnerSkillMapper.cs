using OpenNos.DAL.EF;
using OpenNos.Data;

namespace OpenNos.Mapper.Mappers
{
    public static class PartnerSkillMapper
    {
        #region Methods

        public static bool ToPartnerSkill(PartnerSkillDTO input, PartnerSkill output)
        {
            if (input == null)
            {
                return false;
            }

            output.PartnerSkillId = input.PartnerSkillId;
            output.EquipmentSerialId = input.EquipmentSerialId;
            output.SkillVNum = input.SkillVNum;
            output.Level = input.Level;

            return true;
        }

        public static bool ToPartnerSkillDTO(PartnerSkill input, PartnerSkillDTO output)
        {
            if (input == null)
            {
                return false;
            }

            output.PartnerSkillId = input.PartnerSkillId;
            output.EquipmentSerialId = input.EquipmentSerialId;
            output.SkillVNum = input.SkillVNum;
            output.Level = input.Level;

            return true;
        }

        #endregion
    }
}