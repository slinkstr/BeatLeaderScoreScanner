using System.Net.NetworkInformation;
using ReplayDecoder;

namespace BeatleaderScoreScanner
{
    internal class ReplayAnalysis
    {
        public UnderswingSummary Underswing { get; private set; }
        public List<Frame> JitterFrames { get; private set; }
        public Replay Replay { get; private set; }
        public string ScoreId { get; private set; }
        public string LeaderboardId { get; private set; }

        public ReplayAnalysis(Replay replay, string scoreId, string leaderboardId)
        {
            Replay        = replay;
            Underswing    = new UnderswingSummary(replay);
            JitterFrames  = JitterDetector.JitterTicks(replay);
            ScoreId       = scoreId;
            LeaderboardId = leaderboardId;
        }

        public DateTime Date()
        {
            return DateTimeOffset.FromUnixTimeSeconds(long.Parse(Replay.info.timestamp)).UtcDateTime;
        }

        public string Identifier()
        {
            string[] hmdSplit = Replay.info.hmd.Split("\"", StringSplitOptions.RemoveEmptyEntries);
            return $"{string.Join(' ', hmdSplit)}, {Replay.info.trackingSystem}, {Replay.info.gameVersion}, {Replay.info.version}";
        }

        public List<string> JitterLinks()
        {
            if(string.IsNullOrWhiteSpace(ScoreId) || string.IsNullOrWhiteSpace(LeaderboardId))
            {
                return [];
            }

            List<string> strings = [];
            foreach (Frame frame in JitterFrames)
            {
                var url = $"https://replay.beatleader.xyz/?scoreId={ScoreId}&time={(int)(frame.time * 1000) - 50}&speed=2";
                strings.Add(url);
            }
            return strings;
        }

        public string SongName()
        {
            return Replay.info.songName;
        }

        public override string ToString()
        {
            return $"{Date():yyyy-MM-dd} | " +
                   $"{Identifier()} | " +
                   $"{Underswing.Acc * 100:0.00}% | " +
                   $"Lost {Underswing.Underswing} points ({Underswing.UnderswingAcc * 100:0.00}%), fullswing acc: {Underswing.FullAcc * 100:0.00}% | " +
                   $"Found {JitterFrames.Count} jitters | " +
                   $"{SongName()}" + (string.IsNullOrWhiteSpace(LeaderboardId) ? "" : $" ({LeaderboardId})");
        }
    }
}
