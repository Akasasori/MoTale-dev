﻿/*
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

namespace OpenNos.GameObject
{
    public class Gift
    {
        #region Instantiation

        public Gift(short vnum, short amount, short design = 0, bool isRareRandom = false)
        {
            VNum = vnum;
            Amount = amount;
            IsRandomRare = isRareRandom;
            Design = design;
        }

        #endregion

        #region Properties

        public short Amount { get; set; }

        public short Design { get; set; }

        public bool IsRandomRare { get; set; }

        public short VNum { get; set; }

        public byte MinTeamSize { get; set; }

        public byte MaxTeamSize { get; set; }

        #endregion
    }
}