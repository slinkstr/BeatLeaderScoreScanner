using System;
using System.Web;
using BeatleaderScoreScanner;
using Newtonsoft.Json;
using ReplayDecoder;

internal class Program
{
    private static Dictionary<long, long> BeatleaderUnderswings = new()
    {
        { 19336178, 10186 },
        { 19336147, 538   },
        { 19296026, 1456  },
        { 19295964, 1156  },
        { 19295897, 2520  },
        { 19295716, 1328  },
        { 19295660, 1376  },
        { 19295620, 159   },
        { 19295391, 3162  },
        { 19257510, 23670 },

        { 19257484, 136   },
        { 19257455, 7680  },
        { 19257442, 396   },
        { 19257428, 1052  },
        { 19257378, 0     },
        { 19257344, 0     },
        { 19257322, 0     },
        { 19257304, 1856  },
        { 19257283, 256   },
        { 19257249, 0     },

        { 19257207, 112   },
        { 19257180, 96    },
        { 19257126, 0     },
        { 18989377, 17526 },
        { 18989200, 7104  },
        { 18989122, 9832  },
        { 18989032, 648   },
        { 18988920, 9228  },
        { 18988866, 3160  },
        { 18988795, 5808  },

        { 18988598, 1312  },
        { 18875243, 12043 },
        { 18875207, 4820  },
        { 18875175, 4240  },
        { 18875072, 410   },
        { 18834128, 13758 },
        { 18834087, 13112 },
        { 18834057, 7948  },
        { 18834001, 6992  },
        { 18833829, 13840 },

        { 18833658, 1490  },
        { 18833619, 1296  },
        { 18766658, 39793 },
        { 18766502, 8908  },
        { 18766215, 2912  },
        { 18672659, 1664  },
        { 18672632, 4304  },
        { 18672521, 664   },
        { 18672508, 1444  },
        { 18672437, 406   },

        { 18672392, 537   },
        { 18630663, 8050  },
        { 18630638, 2440  },
        { 18630587, 2178  },
        { 18630538, 2456  },
        { 18630471, 1612  },
        { 18630381, 5856  },
        { 18630328, 1368  },
        { 18630266, 664   },
        { 18630225, 309   },

        { 18514540, 1264  },
        { 18514378, 672   },
        { 18514241, 11705 },
        { 18514158, 232   },
        { 18514109, 21    },
        { 18379371, 19904 },
        { 18379249, 8740  },
        { 18379172, 2604  },
        { 18379044, 1088  },
        { 18378995, 5310  },

        { 18378936, 560   },
        { 18378812, 1976  },
        { 18378768, 1045  },
        { 18294139, 13060 },
        { 18293991, 2364  },
        { 18293967, 1216  },
        { 18293876, 720   },
        { 18293808, 704   },
        { 18293534, 1288  },
        { 18293461, 1096  },
    };

    /* easier to scrape pages with this
    (async () => {

    let scoreIds = [];
    let replayButtons = document.querySelectorAll(".song .desktop-and-up span[slot='default_buttons'] > .button:nth-child(5)");
    replayButtons.forEach((elm) => { scoreIds.push(elm.href.split("scoreId=")[1]); });
    console.log(scoreIds.join("\n"));

    let expandArrows = document.querySelectorAll(".score-options-section.tablet-and-up > .beat-savior-reveal:not(.opened)");
    expandArrows.forEach((elm) => { elm.click(); });

    await new Promise(r => setTimeout(r, 10000));
    let underswingPage = document.querySelectorAll(".details-box.chart > .compact-pagination > .pagination-button:nth-child(2)");
    underswingPage.forEach((elm) => { elm.click(); });

    let details = document.querySelectorAll("section.details");
    details.forEach((elm) => {
	    elm.children[3].remove();
	    elm.children[2].remove();
	    elm.children[0].remove();
	    });

    })();
    */

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
                        score = await GetScore(scoreId);
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
                else if (Path.GetExtension(result.AbsoluteUri) == ".bsor")
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
        var diff = score.leaderboard?.difficulty ?? score.difficulty ?? throw new Exception("Error parsing difficulty.");

        // compare these against replay analysis
        var scoreDate          = DateTimeOffset.FromUnixTimeSeconds((long)score.timeset).UtcDateTime;
        var scoreBase          = (long)score.baseScore;
        var scoreAcc           = (float)score.accuracy;
        var scoreMax           = (long)diff.maxScore;

        // required for linking leaderboards/replays
        var scoreId            = (string)score.id;
        var scoreLeaderboardId = (string)score.leaderboardId;

        var replay = await ReplayFetch.FromUrl((string)score.replay);
        await ScanReplay(replay, scoreId, scoreLeaderboardId);
    }

    private static async Task ScanReplay(Replay replay, string scoreId = "", string leaderboardId = "")
    {
        var analysis = new ReplayAnalysis(replay, scoreId, leaderboardId);
        await Console.Out.WriteLineAsync(analysis.ToString());

        if(BeatleaderUnderswings.TryGetValue(long.Parse(scoreId), out long under))
        {
            if(under != analysis.Underswing.Underswing)
            {
                await Console.Out.WriteLineAsync($"Calculated underswing did not match Beatleader. Calc: {analysis.Underswing.Underswing}, BL: {under}");
            }
        }
        //else
        //{
        //    await Console.Out.WriteLineAsync("Did not have BL value for " + scoreId);
        //}

        var links = analysis.JitterLinks();
        if (links.Count > 0)
        {
            foreach (var link in links)
            {
                await Console.Out.WriteLineAsync(link);
            }
        }
        else
        {
            foreach (var time in analysis.JitterTimes())
            {
                await Console.Out.WriteLineAsync(time);
            }
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
}
