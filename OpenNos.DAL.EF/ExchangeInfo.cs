﻿using OpenNos.DAL.EF;
using System.Collections.Generic;

namespace OpenNos.GameObject
{
    public class ExchangeInfo
    {
        #region Instantiation

        public ExchangeInfo()
        {
            Confirm = false;
            Gold = 0;
            GoldBank = 0;
            TargetCharacterId = -1;
            ExchangeList = new List<ItemInstance>();
            Validate = false;
        }

        #endregion

        #region Properties

        public bool Confirm { get; set; }

        public List<ItemInstance> ExchangeList { get; set; }

        public long Gold { get; set; }

        public long GoldBank { get; set; }

        public long TargetCharacterId { get; set; }

        public bool Validate { get; set; }

        #endregion
    }
}