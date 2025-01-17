using ReplayDecoder;

namespace BeatLeaderScoreScanner
{
    internal class ReplayAnalysis
    {
        public List<Frame>       JitterFrames     { get; private set; }
        public List<string>      JitterLinks      { get; private set; }
        public float             JittersPerMinute { get; private set; }
        public string            LeaderboardId    { get; private set; }
        public Replay            Replay           { get; private set; }
        public Uri               ReplayUri        { get; private set; }
        public string?           ScoreId          { get; private set; }
        public UnderswingSummary Underswing       { get; private set; }

        public ReplayAnalysis(Replay replay, bool requireScoreLoss, Uri replayUri, string leaderboardId)
        {
            JitterFrames  = JitterDetector.JitterTicks(replay, requireScoreLoss);
            LeaderboardId = leaderboardId;
            Replay        = replay;
            ReplayUri     = replayUri;
            Underswing    = new UnderswingSummary(replay);

            JittersPerMinute = JitterFrames.Count / (Replay.frames.Last().time / 60f);

            List<string> strings = [];
            foreach (Frame frame in JitterFrames)
            {
                var url = $"{BeatLeaderDomain.Replay}/?link={ReplayUri}&speed=2&time={(int)(frame.time * 1000) - 50}";
                strings.Add(url);
            }
            JitterLinks = strings;

            ScoreId = BeatLeaderDomain.IsReplayCdn(ReplayUri) ? ReplayUri.Segments.LastOrDefault()?.Split("-")?[0] : null;
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

        public override string ToString()
        {
            return $"{Date():yyyy-MM-dd} | " +
                   $"{Identifier()} | " +
                   $"{Underswing.Acc * 100:0.00}% | " +
                   $"Lost {Underswing.LostScore} points ({Underswing.LostAcc * 100:0.00}%), fullswing acc: {Underswing.FullSwingAcc * 100:0.00}% | " +
                   $"Found {JitterFrames.Count} jitters ({JittersPerMinute:F2}/min) | " +
                   $"{Replay.info.songName}" + (string.IsNullOrWhiteSpace(LeaderboardId) ? "" : $" ({LeaderboardId})");
        }
    }
}
