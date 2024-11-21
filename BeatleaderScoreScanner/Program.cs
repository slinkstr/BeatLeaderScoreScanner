using ReplayDecoder;
using Newtonsoft.Json;
using BeatleaderScoreScanner;

internal class Program
{
    private static HttpClient _httpClient = new();
    private static AsyncReplayDecoder _decoder = new();

    private static async Task Main(string[] args)
    {
        await foreach (var page in GetPlayerScores("76561198871467199"))
        {
            foreach (var score in page.data)
            {
                //if (!score.fullCombo)      { continue; }
                //if (score.accuracy < 0.95) { continue; }

                // this might be interesting
                string platform = score.platform;

                var replay = await ReplayFetch.FromUrl((string)score.replay);
                await PrintUnderswingStats(score, replay);
                var jitters = DetectSwingJitter(replay);
            }
        }
    }

    private static async Task PrintUnderswingStats(dynamic score, Replay? replay = null)
    {
        if (replay == null)
        {
            replay = await ReplayFetch.FromUrl((string)score.replay);
        }

        int   maxScore          = score.leaderboard.difficulty.maxScore;
        int   underswing        = CalculateUnderswingLoss(replay);
        float underswingPercent = underswing / (float)maxScore;
        float fullSwingAcc      = ((int)score.baseScore + underswing) / (float)maxScore;

        DateTime dateTime = UnixToDateTime((long)score.timeset);
        string printout = $"{dateTime:yyyy-MM-dd} | {(float)score.accuracy * 100:0.00}% | Lost {underswing,6} points ({underswingPercent * 100:0.00}%), fullswing score: {fullSwingAcc * 100:0.00}% | {score.leaderboard.song.name} ({score.leaderboard.id})";

        await Console.Out.WriteLineAsync(printout);
    }

    private static int CalculateUnderswingLoss(Replay replay, bool debugLog = false)
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

            if ((underPre > 0 || underPost > 0) && debugLog)
            {
                Console.WriteLine($"Underswing note at {note.eventTime} | " +
                    $"Pre: {underPre}, Post: {underPost} | " +
                    $"X={note.noteParams.lineIndex}, Y={note.noteParams.noteLineLayer}, color={(note.noteParams.colorType == 0 ? "red" : "blue")}");
            }

            totalUnderPre  += underPre * multiplierCounter.Multiplier;
            totalUnderPost += underPost * multiplierCounter.Multiplier;
            /**/totalScore += note.score.value * multiplierCounter.Multiplier;
        }

        if (debugLog)
        {
            Console.WriteLine("underPre:  " + totalUnderPre);
            Console.WriteLine("underPost: " + totalUnderPost);
        }

        return totalUnderPre + totalUnderPost;
    }

    private static float[] DetectSwingJitter(Replay replay)
    {
        float[] Trajectories(Frame frameFrom, Frame frameTo)
        {
            var leftTrajectory  = Vector3.Angle(frameFrom.leftHand.position , frameTo.leftHand.position);
            var rightTrajectory = Vector3.Angle(frameFrom.rightHand.position, frameTo.rightHand.position);
            var headTrajectory  = Vector3.Angle(frameFrom.head.position     , frameTo.head.position);

            // Console.WriteLine($"Trajectories: [ {leftTrajectory,13:F8}, {rightTrajectory,13:F8}, {headTrajectory,13:F8} ]");
            return [ leftTrajectory, rightTrajectory, headTrajectory ];
        }

        List<float> timestamps = new();
        string identifier = $"{replay.info.hmd}, {replay.info.trackingSystem}, {replay.info.gameVersion}, {replay.info.version}";
        float[]? lastTrajectories = null;

        // skip 1st because it has nothing to compare against
        for (int i = 1; i < replay.frames.Count; i++)
        {
            float[] trajectories = Trajectories(replay.frames[i - 1], replay.frames[i]);

            if (lastTrajectories != null)
            {
                float[] difference = [ (trajectories[0] - lastTrajectories[0]), (trajectories[1] - lastTrajectories[1]), (trajectories[2] - lastTrajectories[2]), ];
                //Console.WriteLine($"Difference:   [ {difference[0],13:F8}, {difference[1],13:F8}, {difference[2],13:F8} ]");

                if (difference.Any(diff => diff > 5))
                {
                    Console.WriteLine($"Found abnormal difference at frame {i} ({replay.frames[i].time:F2}s), [ {difference[0],13:F8}, {difference[1],13:F8}, {difference[2],13:F8} ]");
                }
            }

            lastTrajectories = trajectories;
        }

        return timestamps.ToArray();
    }

    private static async IAsyncEnumerable<dynamic> GetPlayerScores(string playerId, int pages = 10, int pageSize = 10)
    {
        string endpoint = $"https://api.beatleader.xyz/player/{playerId}/scores";
        for (int currentPage = 1; currentPage <= pages; currentPage++)
        {
            string args = $"?sortBy=date&page={currentPage}&count={pageSize}";
            var response = await _httpClient.GetAsync(endpoint + args);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            dynamic json = JsonConvert.DeserializeObject(content) ?? throw new Exception("Unable to parse JSON");
            yield return json;
        }
    }

    private static DateTime UnixToDateTime(long unixTimestamp)
    {
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimestamp);
        return dateTime;
    }
}