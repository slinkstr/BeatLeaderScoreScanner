using System.Web;
using BeatLeaderScoreScanner.ReplayAnalyses;
using ReplayDecoder;

namespace BeatLeaderScoreScanner
{
    internal class ReplayAnalysis
    {
        public string            LeaderboardId         { get; private set; }
        public Replay            Replay                { get; private set; }
        public Uri               ReplayUri             { get; private set; }
        public string?           ScoreId               => BeatLeaderDomain.IsReplayCdn(ReplayUri) ? ReplayUri.Segments.LastOrDefault()?.Split("-")?[0] : null;
        public int               Mistakes              => Replay.walls.Count + Replay.notes.Count(x => x.eventType != NoteEventType.good);
        public float             PlayDuration          => Replay.frames.LastOrDefault()?.time ?? 0;
        public Jitter            Jitter                { get; private set; }
        public OriginReset       OriginReset           { get; private set; }
        public Underswing        Underswing            { get; private set; }

        public ReplayAnalysis(Replay replay, bool requireScoreLoss, Uri replayUri, string leaderboardId)
        {
            LeaderboardId = leaderboardId;
            Replay        = replay;
            ReplayUri     = replayUri;
            Jitter        = new Jitter(replay);
            OriginReset   = new OriginReset(replay);
            Underswing    = new Underswing(replay);

            if (requireScoreLoss)
            {
                Jitter.Events = Jitter.CausedScoreLoss(Underswing.Events.Select(x => x.Note).ToArray());
            }
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
            var jitterPerMinute = Jitter.Events.Count / (PlayDuration / 60f);
            var originResetPerMinute = OriginReset.Events.Count / (PlayDuration / 60f);
            var underswingPerMinute = Underswing.Events.Count / (PlayDuration / 60f);

            return $"{Date():yyyy-MM-dd} | " +
                   $"{SysInfo()} | " +
                   $"{Underswing.Percent * 100:0.00}% | " +
                   $"JITTERS: {Jitter.Events.Count} ({jitterPerMinute:F2}/min) | " +
                   $"ORIGIN RESETS: {OriginReset.Events.Count} ({originResetPerMinute:F2}/min) | " +
                   $"UNDERSWING: {Underswing.Events.Count} ({underswingPerMinute:F2}/min), {Underswing.ScoreLost} points ({Underswing.PercentLost * 100:0.00}%), fullswing: {Underswing.PercentFullSwing * 100:0.00}% | " +
                   $"{Replay.info.songName}" + (string.IsNullOrWhiteSpace(LeaderboardId) ? "" : $" ({LeaderboardId})");
        }

        public List<string> JitterLinks()
        {
            return Jitter.Events.Select(x => ReplayTimestamp(ReplayUri, x.Frame.time)).ToList();
        }

        public List<string> OriginResetLinks()
        {
            return OriginReset.Events.Select(x => ReplayTimestamp(ReplayUri, x.Frame.time)).ToList();
        }

        public List<string> UnderswingLinks()
        {
            return Underswing.Events.Select(x => ReplayTimestamp(ReplayUri, x.Note.eventTime)).ToList();
        }

        private static string ReplayTimestamp(Uri replayUri, float time)
        {
            if (replayUri.IsFile)
            {
                // For easy previewing.
                // Host a local webserver on port 8000 with CORS header set to "*" pointing to your replay folder.
                replayUri = new Uri("http://localhost:8000/" + HttpUtility.UrlEncode(replayUri.Segments.LastOrDefault()));
            }

            return $"{BeatLeaderDomain.Replay}/?link={replayUri}&speed=2&time={(int)(time * 1000) - 65}";
        }
    }
}
