﻿using System;

namespace OpenNos.Data
{
    [Serializable]
    public class QuestDTO
    {
        #region Properties

        public long QuestId { get; set; }

        public int QuestType { get; set; }

        public byte LevelMin { get; set; }

        public byte LevelMax { get; set; }

        public int? StartDialogId { get; set; }

        public int? EndDialogId { get; set; }

        public int? DialogNpcVNum { get; set; }

        public int? DialogNpcId { get; set; }

        public short? TargetMap { get; set; }

        public short? TargetX { get; set; }

        public short? TargetY { get; set; }

        public int InfoId { get; set; }

        public long? NextQuestId { get; set; }

        public bool IsDaily { get; set; }

        #endregion
    }
}
