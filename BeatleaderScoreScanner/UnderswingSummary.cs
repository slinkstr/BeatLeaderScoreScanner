using ReplayDecoder;

namespace BeatleaderScoreScanner
{
    internal class UnderswingSummary
    {
        public int    MaxScore      { get; set; }
        public int    Score         { get; set; }
        public int    FullScore     { get; set; }
        public double Acc           => Score / (double)MaxScore;
        public double FullAcc       => FullScore / (double)MaxScore;
        public int    Underswing    => FullScore - Score;
        public double UnderswingAcc => FullAcc - Acc;

        public UnderswingSummary(Replay replay)
        {
            var processed = ReplayStatistic.ProcessReplay(replay);
            if(processed.Item2 != null) { throw new Exception(processed.Item2); }
            ScoreStatistic stats = processed.Item1!;
            int maxScore = stats.winTracker.maxScore;

            int underswing = CalculateUnderswingPoints(replay);

            MaxScore  = maxScore;
            Score     = replay.info.score;
            FullScore = replay.info.score + underswing;
        }

        private static int CalculateUnderswingPoints(Replay replay)
        {
            /**/bool output = false;
            /**/int totalScore = 0;
            /**/int counter = 0;
            int totalUnderPre = 0;
            int totalUnderPost = 0;
            MultiplierCounter multiplierCounter = new();

            foreach (var note in replay.notes)
            {
                /**/counter++;
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

                if ((underPre > 0 || underPost > 0) && output)
                {
                    Console.WriteLine($"Underswing note at {note.eventTime} | " +
                        $"Pre: {underPre}, Post: {underPost} | " +
                        $"X={note.noteParams.lineIndex}, Y={note.noteParams.noteLineLayer}, color={(note.noteParams.colorType == 0 ? "red" : "blue")}");
                }

                totalUnderPre  += underPre * multiplierCounter.Multiplier;
                totalUnderPost += underPost * multiplierCounter.Multiplier;
                /**/totalScore += note.score.value * multiplierCounter.Multiplier;
            }

            if (output)
            {
                Console.WriteLine("underPre:  " + totalUnderPre);
                Console.WriteLine("underPost: " + totalUnderPost);
            }

            return totalUnderPre + totalUnderPost;
        }
    }
}
