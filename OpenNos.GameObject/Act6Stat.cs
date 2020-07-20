using System;

namespace OpenNos.GameObject
{
    public class Act6Stat
    {
        #region Members

        private DateTime _latestUpdate;

        private int _percentage;

        private short _totalTime;

        #endregion

        #region Instantiation

        public Act6Stat()
        {
            _latestUpdate = DateTime.Now;
        }

        #endregion

        #region Properties

        public short CurrentTimeErenia => IsBossErenia ? (short)(_latestUpdate.AddSeconds(_totalTime) - DateTime.Now).TotalSeconds : (short)0;
        public short CurrentTimeZenas => IsBossZenas ? (short)(_latestUpdate.AddSeconds(_totalTime) - DateTime.Now).TotalSeconds : (short)0;
        public bool IsBossErenia { get; set; }
        public bool IsBossZenas { get; set; }

        public byte Mode { get; set; }

        public int Percentage
        {
            get => _percentage;
            set => _percentage = value;
        }

        public short TotalTime
        {
            get => _totalTime;
            set
            {
                _latestUpdate = DateTime.Now;
                _totalTime = value;
            }
        }

        #endregion
    }
}