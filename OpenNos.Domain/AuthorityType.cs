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

namespace OpenNos.Domain
{
    public enum AuthorityType : short
    {
        //UNUSED
        BitchNiggerFaggot = -69,
        //DELETED
        Closed = -3,
        //BANNED
        Banned = -2,
        //NOT CONFIRMED ACCOUNT
        Unconfirmed = -1,
        //Player
        User = 0,
        //Donator
        Donator = 1,
        //Support
        GS = 20,
        TMOD = 30,
        MOD = 31,
        SMOD = 32,
        BA = 33,
        //Trial GM
        TGM = 50,
        //GM
        GM = 51,
        SGM = 52,
        GA = 53,
        //Team Manager
        TM = 60,
        //Community Manager
        CM = 70,
        DEV = 80,
        Owner = 100,
        //Administrator (Everything)
        Administrator = 666
    }
}