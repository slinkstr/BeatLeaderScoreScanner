using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReplayDecoder;

namespace BeatleaderScoreScanner
{
    internal class UnderswingDetector
    {
        private const float accThreshold = 0.00f;
        private const bool  requireFc = false;

        public static void PrintUnderswing(dynamic score, Replay replay)
        {
            int   maxScore          = score.leaderboard.difficulty.maxScore;
            int   underswing        = CalculateUnderswingPoints(replay);
            float underswingPercent = underswing / (float)maxScore;
            float fullSwingAcc      = ((int)score.baseScore + underswing) / (float)maxScore;

            DateTime dateTime       = Program.UnixToDateTime((long)score.timeset);
            string   printout       = $"{dateTime:yyyy-MM-dd} | {(float)score.accuracy * 100:0.00}% | Lost {underswing,6} points ({underswingPercent * 100:0.00}%), fullswing score: {fullSwingAcc * 100:0.00}% | {score.leaderboard.song.name} ({score.leaderboard.id})";

            Console.WriteLine(printout);
        }
        
        public static int CalculateUnderswingPoints(Replay replay, bool output = false)
        {
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

                int underPre  = 70 - note.score.pre_score;
                int underPost = 30 - note.score.post_score;

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
