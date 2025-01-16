// Lifted and modified from ReplayDecoder.ReplayStatistic.cs

namespace BeatLeaderScoreScanner
{
    internal class MultiplierCounter
    {
        public int Multiplier { get; private set; } = 1;
        public int Combo { get; private set; } = 0;

        private int _multiplierIncreaseProgress;
        private int _multiplierIncreaseMaxProgress = 2;

        public void Reset()
        {
            Multiplier = 1;
            _multiplierIncreaseProgress = 0;
            _multiplierIncreaseMaxProgress = 2;
        }

        public void Increase()
        {
            Combo++;
            if (Multiplier >= 8) return;

            if (_multiplierIncreaseProgress < _multiplierIncreaseMaxProgress)
            {
                ++_multiplierIncreaseProgress;
            }

            if (_multiplierIncreaseProgress >= _multiplierIncreaseMaxProgress)
            {
                Multiplier *= 2;
                _multiplierIncreaseProgress = 0;
                _multiplierIncreaseMaxProgress = Multiplier * 2;
            }
        }

        public void Decrease()
        {
            Combo = 0;
            if (_multiplierIncreaseProgress > 0)
            {
                _multiplierIncreaseProgress = 0;
            }

            if (Multiplier > 1)
            {
                Multiplier /= 2;
                _multiplierIncreaseMaxProgress = Multiplier * 2;
            }
        }
    }
}
