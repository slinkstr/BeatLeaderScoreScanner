using ReplayDecoder;

namespace BeatLeaderScoreScanner
{
    internal class UnderswingSummary
    {
        public int    MaxScore       { get; set; }
        public int    Score          { get; set; }
        public int    FullSwingScore { get; set; }
        public double Acc            => Score / (double)MaxScore;
        public double FullSwingAcc   => FullSwingScore / (double)MaxScore;
        public int    LostScore      => FullSwingScore - Score;
        public double LostAcc        => FullSwingAcc - Acc;

        public UnderswingSummary(Replay replay)
        {
            var processed = ReplayStatistic.ProcessReplay(replay);
            if (processed.Item2 != null) { throw new Exception(processed.Item2); }
            ScoreStatistic stats = processed.Item1!;
            int maxScore = stats.winTracker.maxScore;

            int underswing = CalculateUnderswingPoints(replay);

            MaxScore       = maxScore;
            Score          = replay.info.score;
            FullSwingScore = replay.info.score + underswing;
        }

        private static int CalculateUnderswingPoints(Replay replay)
        {
            int totalUnderPre = 0;
            int totalUnderPost = 0;
            MultiplierCounter multiplierCounter = new();

            var comboEvents = replay.notes.Concat<object>(replay.walls).ToList().OrderBy(x =>
            {
                if (x is NoteEvent n) { return n.eventTime; }
                if (x is WallEvent w) { return w.time;      }
                throw new Exception("Encountered unknown type in comboEvents");
            });
            
            foreach (var comboEvent in comboEvents)
            {
                if (comboEvent is WallEvent)
                {
                    multiplierCounter.Decrease();
                    continue;
                }
                
                var note = comboEvent as NoteEvent ?? throw new Exception("Unable to cast comboEvent to NoteEvent");
                if (note.eventType != NoteEventType.good)
                {
                    multiplierCounter.Decrease();
                    continue;
                }

                multiplierCounter.Increase();

                int targetPre;
                int targetPost;

                switch(note.noteParams.scoringType)
                {
                    case ScoringType.BurstSliderHead:
                        targetPre  = 70;
                        targetPost = 0;
                        break;
                    case ScoringType.BurstSliderElement:
                        targetPre  = 0;
                        targetPost = 0;
                        break;
                    default:
                        targetPre  = 70;
                        targetPost = 30;
                        break;
                }

                int underPre  = targetPre  - note.score.pre_score;
                int underPost = targetPost - note.score.post_score;

                totalUnderPre  += underPre * multiplierCounter.Multiplier;
                totalUnderPost += underPost * multiplierCounter.Multiplier;
            }

            return totalUnderPre + totalUnderPost;
        }
    }
}
