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
            counter++;
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
            totalScore     += note.score.value * multiplierCounter.Multiplier;

            if(counter > 100)
            {
                // do nothing
            }
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
        List<float> timestamps = new();

        float angleThresholdDegrees = 45f;
        int   scanRangeTicks        = 5;

        string identifier = $"{replay.info.hmd}, {replay.info.trackingSystem}, {replay.info.gameVersion}, {replay.info.version}";

        foreach (var frame in replay.frames)
        {
            // fucking quaternions....
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