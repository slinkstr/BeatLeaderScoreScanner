using System;
using System.Web;
using BeatleaderScoreScanner;
using Newtonsoft.Json;
using ReplayDecoder;

internal class Program
{
    private static HttpClient _httpClient = new();
    private static AsyncReplayDecoder _decoder = new();

    private static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            await Console.Out.WriteLineAsync("Pass a user ID or replay URL as an argument to analyze.");
            Environment.Exit(0);
        }

        foreach (string arg in args)
        {
            IAsyncEnumerable<dynamic>? scores = null;
            dynamic?                   score  = null;
            Replay?                    replay = null;

            if (Uri.TryCreate(arg, UriKind.Absolute, out var result))
            {
                bool isBeatleader       = result.Host == "beatleader.xyz"             || result.Host == "beatleader.net";
                bool isBeatleaderReplay = result.Host == "replay.beatleader.xyz"      || result.Host == "replay.beatleader.net";
                bool isBeatleaderCdn    = result.Host == "cdn.replays.beatleader.xyz" || result.Host == "cdn.replays.beatleader.net";

                if (isBeatleader && result.Segments[1] == "u/")
                {
                    scores = GetPlayerScores(result.Segments[2].TrimEnd('/'));
                }
                else if (isBeatleaderReplay)
                {
                    var queryParams = HttpUtility.ParseQueryString(result.Query);
                    var scoreId = queryParams.Get("scoreId");
                    var link = queryParams.Get("link");

                    if (scoreId != null)
                    {
                        score = GetScore(scoreId);
                    }
                    else if (link != null)
                    {
                        replay = await ReplayFetch.FromUrl(link);
                    }
                    else
                    {
                        throw new Exception("Unable to find replay from BeatLeader URL: " + arg);
                    }
                }
                else if (Path.GetExtension(result.AbsoluteUri) == "bsor")
                {
                    replay = await ReplayFetch.FromUrl(arg);
                }
                else
                {
                    throw new Exception("Unable to find replay from URL: " + arg);
                }
            }
            else
            {
                scores = GetPlayerScores(arg);
            }

            if(scores != null)
            {
                await foreach (var s in scores)
                {
                    await ScanScore(s);
                }
            }
            else if (score != null)
            {
                await ScanScore(score);
            }
            else if (replay != null)
            {
                await ScanReplay(replay);
            }
            else
            {
                throw new Exception("No data to analyze.");
            }

            
        }
    }

    private static async Task ScanScore(dynamic score)
    {
        var scoreDate          = UnixToDateTime((int)score.timeset);
        var scoreBase          = (int)score.baseScore;
        var scoreAcc           = (float)score.accuracy;
        var scoreMax           = (int)score.leaderboard.difficulty.maxScore;
        var scoreId            = (string)score.id;
        var scoreLeaderboardId = (string)score.leaderboard.id;

        var replay = await ReplayFetch.FromUrl((string)score.replay);
        await ScanReplay(replay, scoreId, scoreLeaderboardId);
    }

    private static async Task ScanReplay(Replay replay, string scoreId = "", string leaderboardId = "")
    {
        UnderswingSummary underSummary = new(replay);
        var frames = JitterDetector.JitterTicks(replay);

        var replayIdentifier = $"{replay.info.hmd}, {replay.info.trackingSystem}, {replay.info.gameVersion}, {replay.info.version}";
        var replayDate = UnixToDateTime(long.Parse(replay.info.timestamp));

        string scoreSummary = $"{replayDate:yyyy-MM-dd} | " +
                              $"{replayIdentifier} | " +
                              $"{underSummary.Acc * 100:0.00}% | " +
                              $"Lost {underSummary.Underswing} points ({underSummary.UnderswingAcc * 100:0.00}%), fullswing acc: {underSummary.FullAcc * 100:0.00}% | " +
                              $"Found {frames.Count} jitters | " +
                              $"{replay.info.songName} ({leaderboardId})";

        await Console.Out.WriteLineAsync(scoreSummary);

        foreach (var frame in frames)
        {
            var url = $"https://replay.beatleader.xyz/?scoreId={scoreId}&time={(int)(frame.time * 1000) - 50}&speed=2";
            await Console.Out.WriteLineAsync(url);
        }
    }

    private static async Task<dynamic> GetScore(string scoreId)
    {
        string endpoint = $"https://api.beatleader.xyz/score/{scoreId}";
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        dynamic json = JsonConvert.DeserializeObject(content) ?? throw new Exception("Unable to parse JSON");
        return json;
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
            foreach (var score in json.data)
            {
                yield return score;
            }
        }
    }

    internal static DateTime UnixToDateTime(long unixTimestamp)
    {
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimestamp);
        return dateTime;
    }
}