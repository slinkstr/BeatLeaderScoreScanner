using ReplayDecoder;

namespace BeatLeaderScoreScanner
{
    internal class ReplayAnalysis
    {
        public List<Frame>       JitterFrames          { get; private set; }
        public List<string>      JitterLinks           => JitterFrames.Select(f => ReplayTimestamp(ReplayUri, f.time)).ToList();
        public float             JittersPerMinute      => JitterFrames.Count / (Replay.frames.Last().time / 60f);
        public string            LeaderboardId         { get; private set; }
        public List<Frame>       OriginResetFrames     { get; private set; }
        public List<string>      OriginResetLinks      => OriginResetFrames.Select(f => ReplayTimestamp(ReplayUri, f.time)).ToList();
        public float             OriginResetsPerMinute => OriginResetFrames.Count / (Replay.frames.Last().time / 60f);
        public Replay            Replay                { get; private set; }
        public Uri               ReplayUri             { get; private set; }
        public string?           ScoreId               => BeatLeaderDomain.IsReplayCdn(ReplayUri) ? ReplayUri.Segments.LastOrDefault()?.Split("-")?[0] : null;
        public UnderswingSummary Underswing            { get; private set; }

        public ReplayAnalysis(Replay replay, bool requireScoreLoss, Uri replayUri, string leaderboardId)
        {
            JitterFrames      = JitterDetector.Jitters(replay, requireScoreLoss);
            LeaderboardId     = leaderboardId;
            OriginResetFrames = JitterDetector.OriginResets(replay);
            Replay            = replay;
            ReplayUri         = replayUri;
            Underswing        = new UnderswingSummary(replay);
        }

        public DateTime Date()
        {
            return DateTimeOffset.FromUnixTimeSeconds(long.Parse(Replay.info.timestamp)).UtcDateTime;
        }

        public string SysInfo()
        {
            string[] hmdSplit = Replay.info.hmd.Split("\"", StringSplitOptions.RemoveEmptyEntries);
            return $"{string.Join(' ', hmdSplit)}, {Replay.info.trackingSystem}, {Replay.info.gameVersion}, {Replay.info.version}";
        }

        public override string ToString()
        {
            return $"{Date():yyyy-MM-dd} | " +
                   $"{SysInfo()} | " +
                   $"{Underswing.Acc * 100:0.00}% | " +
                   $"Lost {Underswing.LostScore} points ({Underswing.LostAcc * 100:0.00}%), fullswing acc: {Underswing.FullSwingAcc * 100:0.00}% | " +
                   $"{JitterFrames.Count} jitters ({JittersPerMinute:F2}/min) | " +
                   $"{OriginResetFrames.Count} origin resets ({OriginResetsPerMinute:F2}/min) | " +
                   $"{Replay.info.songName}" + (string.IsNullOrWhiteSpace(LeaderboardId) ? "" : $" ({LeaderboardId})");
        }

        private static string ReplayTimestamp(Uri replayUri, float time)
        {
            return $"{BeatLeaderDomain.Replay}/?link={replayUri}&speed=2&time={(int)(time * 1000) - 50}";
        }
    }
}
