/*
 * This file is part of the OpenNos Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using OpenNos.Data;
using OpenNos.GameObject.Networking;
using System;

namespace OpenNos.GameObject
{
    public class NpcMonsterSkill : NpcMonsterSkillDTO
    {
        #region Members

        private Skill _skill;

        #endregion

        #region Instantiation

        public NpcMonsterSkill()
        {
        }

        public NpcMonsterSkill(NpcMonsterSkillDTO input)
        {
            NpcMonsterSkillId = input.NpcMonsterSkillId;
            NpcMonsterVNum = input.NpcMonsterVNum;
            Rate = input.Rate;
            SkillVNum = input.SkillVNum;
        }

        #endregion

        #region Properties

        public short Hit { get; set; }

        public DateTime LastSkillUse
        {
            get; set;
        }

        public Skill Skill => _skill ?? (_skill = ServerManager.GetSkill(SkillVNum));

        public bool CanBeUsed() => Skill != null && LastSkillUse.AddMilliseconds(Skill.Cooldown * 100) < DateTime.Now;

        #endregion
    }
}