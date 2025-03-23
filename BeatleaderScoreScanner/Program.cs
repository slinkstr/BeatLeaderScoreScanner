using System.Reflection;
using System.Web;
using BeatLeaderScoreScanner;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ReplayDecoder;

internal class Program
{

#if DEBUG
    // to debug inconsistencies between program and bl
    private static Dictionary<long, long> BeatLeaderUnderswings = new()
    {
        // 1
        { 19673962, 10136 },
        { 19673722, 7764  },
        { 19673336, 15036 },
        { 19673256, 1520  },
        { 19609190, 4470  },
        { 19609148, 9484  },
        { 19608960, 12902 },
        { 19608876, 1624  },
        { 19608834, 640   },
        { 19608806, 2427  },
        // 2
        { 19544641, 15235 },
        { 19544037, 44608 },
        { 19543745, 1096  },
        { 19543593, 6116  },
        { 19336178, 10186 },
        { 19336147, 538   },
        { 19296026, 1456  },
        { 19295964, 1156  },
        { 19295897, 2520  },
        { 19295716, 1328  },
        // 3
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
        // 4
        { 19257322, 0     },
        { 19257304, 1856  },
        { 19257283, 256   },
        { 19257249, 0     },
        { 19257207, 112   },
        { 19257180, 96    },
        { 19257126, 0     },
        { 18989200, 7104  },
        { 18989122, 9832  },
        { 18989032, 648   },
        // 5
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
        // 6
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
        // 7
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
        // 8
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
        // 9
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
        // 10
        { 18293808, 704   },
        { 18293534, 1288  },
        { 18293461, 1096  },
        { 18293440, 858   },
        { 18293392, 206   },
        { 18144669, 3504  },
        { 18144514, 4340  },
        { 18144477, 464   },
        { 18144363, 2400  },
        { 18144324, 3600  },
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
#endif

    private static HttpClient         _httpClient = new();
    private static AsyncReplayDecoder _decoder    = new();
    private static ProgramConfig?     _config;

    private static async Task Main(string[] args)
    {
        _config = ProgramConfig.ArgsToConfig(args);
        if (_config == null)
        {
            throw new Exception("Unable to parse config.");
        }

        List<Task<string>> inputTasks = new();
        foreach (string input in _config.Inputs)
        {
            inputTasks.Add(ProcessInput(input, _config));
        }
        await Task.WhenAll(inputTasks);
        foreach(var task in inputTasks)
        {
            await Console.Out.WriteLineAsync(task.Result);
        }
    }

    public static async Task<string> ProcessInput(string input, ProgramConfig config)
    {
        List<Task<ReplayAnalysis?>> tasks = new();

        if (Uri.TryCreate(HttpUtility.UrlDecode(input), UriKind.Absolute, out var result))
        {
            if (result.IsFile && !config.AllowFile) { throw new Exception("Unable to read file, pass --allow-file to allow."); }

            if (BeatLeaderDomain.IsValid(result) && result.Segments.Length > 1 && result.Segments[1] == "u/")
            {
                tasks.AddRange(await AnalyzeFromProfileId(result.Segments[2].TrimEnd('/'), config));
            }
            else if (BeatLeaderDomain.IsReplay(result))
            {
                var queryParams = HttpUtility.ParseQueryString(result.Query);
                var scoreId = queryParams.Get("scoreId");
                var link = queryParams.Get("link");

                if (scoreId != null)
                {
                    tasks.Add(AnalyzeFromScoreId(scoreId, config));
                }
                else if (link != null)
                {
                    tasks.Add(AnalyzeFromBsorUrl(link, config));
                }
                else
                {
                    throw new Exception("Unable to find replay from BeatLeader URL: " + input);
                }
            }
            else
            {
                tasks.Add(AnalyzeFromBsorUrl(result.ToString(), config));
            }
        }
        else
        {
            tasks.AddRange(await AnalyzeFromProfileId(input, config));
        }

        var analyses = await Task.WhenAll(tasks);

#if DEBUG
        foreach (var analysis in analyses)
        {
            if (analysis == null) { continue; }

            if (!string.IsNullOrWhiteSpace(analysis.ScoreId) && BeatLeaderUnderswings.TryGetValue(long.Parse(analysis.ScoreId), out long under))
            {
                if (under != analysis.Underswing.LostScore)
                {
                    await Console.Out.WriteLineAsync($"{analysis.ScoreId} | {analysis.Replay.info.songName} underswing did not match BeatLeader. Calc: {analysis.Underswing.LostScore}, BL: {under} ({under - analysis.Underswing.LostScore})");
                }
            }
            else
            {
                await Console.Out.WriteLineAsync("Did not have BL value for " + analysis.Replay.info.songName);
            }
        }
#endif

        string output = "";
        foreach (var analysis in analyses)
        {
            output += OutputAnalysis(analysis, config.OutputFormat) + "\n";
        }
        output = output.TrimEnd('\n');
        return output;
    }

    public static string OutputAnalysis(ReplayAnalysis? analysis, ProgramConfig.Format format)
    {
        string output = "";
        if (format == ProgramConfig.Format.text)
        {
            if (analysis == null)
            {
                return "Analysis skipped.";
            }
            
            output = analysis.ToString();

            if (analysis.JitterFrames.Count > 0)
            {
                output += "\nJITTERS:\n";
                foreach (var time in analysis.JitterLinks)
                {
                    output += $"\t{time}\n";
                }
                output = output.TrimEnd('\n');
            }
            
            if (analysis.OriginResetFrames.Count > 0)
            {
                output += "\nORIGIN RESETS:\n";
                foreach (var time in analysis.OriginResetLinks)
                {
                    output += $"\t{time}\n";
                }
                output = output.TrimEnd('\n');
            }
        }
        else if (format == ProgramConfig.Format.json)
        {
            if (analysis == null)
            {
                return "null";
            }

            output = JsonConvert.SerializeObject(analysis, new JsonSerializerSettings()
            {
                ContractResolver = new IgnorePropertiesResolver(["frames", "heights", "notes", "pauses", "walls", "fps", "head", "leftHand", "rightHand"]),
            });
        }

        return output;
    }

    public static async Task<List<Task<ReplayAnalysis?>>> AnalyzeFromProfileId(string playerId, ProgramConfig config)
    {
        List<Task<ReplayAnalysis?>> analysisTasks = new();

        var scores = await GetPlayerScores(playerId, config.Count, config.Page);
        foreach (var score in scores)
        {
            if (config.MinimumScore > (float)score.accuracy)
            {
                analysisTasks.Add(Task.FromResult<ReplayAnalysis?>(null));
            }
            else if (config.RequireFC && !(bool)score.fullCombo)
            {
                analysisTasks.Add(Task.FromResult<ReplayAnalysis?>(null));
            }
            else
            {
                analysisTasks.Add(ScanScore(score, config));
            }
        }

        return analysisTasks;
    }

    public static async Task<ReplayAnalysis?> AnalyzeFromScoreId(string scoreId, ProgramConfig config)
    {
        var score = await GetScore(scoreId);
        return await ScanScore(score, config);
    }

    public static async Task<ReplayAnalysis?> AnalyzeFromBsorUrl(string replayUrl, ProgramConfig config)
    {
        var replay = await ReplayFetch.FromUri(replayUrl);
        return await ScanReplay(replay, config.RequireScoreLoss, new Uri(replayUrl));
    }

    private static async Task<ReplayAnalysis> ScanScore(dynamic score, ProgramConfig config)
    {
        // required for linking leaderboards/replays
        var scoreReplayUrl     = new Uri((string)score.replay);
        var scoreLeaderboardId = (string)score.leaderboardId;

        var replay = await ReplayFetch.FromUri((string)score.replay);
        var analysis = await ScanReplay(replay, config.RequireScoreLoss, scoreReplayUrl, scoreLeaderboardId);
        return analysis;
    }

    private static async Task<ReplayAnalysis> ScanReplay(Replay replay, bool requireScoreLoss, Uri replayUrl, string leaderboardId = "")
    {
        ReplayAnalysis analysis = await Task.Run(() => { return new ReplayAnalysis(replay, requireScoreLoss, replayUrl, leaderboardId); });
        return analysis;
    }

    private static async Task<dynamic> GetScore(string scoreId)
    {
        string endpoint = $"{BeatLeaderDomain.Api}/score/{scoreId}";
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        dynamic json = JsonConvert.DeserializeObject(content) ?? throw new Exception("Unable to parse JSON");
        return json;
    }

    private static async Task<IEnumerable<dynamic>> GetPlayerScores(string playerId, int count, int page)
    {
        if (count < 1)   { throw new ArgumentException("Count must be a positive integer.", nameof(count)); }
        if (count > 100) { throw new ArgumentException("Count can't be greater than 100." , nameof(count)); }

        string endpoint = $"{BeatLeaderDomain.Api}/player/{playerId}/scores";
        string args = $"?sortBy=date&page={page}&count={count}";

        var response = await _httpClient.GetAsync(endpoint + args);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return [];
        }
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        dynamic json = JsonConvert.DeserializeObject(content) ?? throw new Exception("Unable to parse JSON");
        return json.data;
    }

    //short helper class to ignore some properties from serialization
    private class IgnorePropertiesResolver : DefaultContractResolver
    {
        private readonly HashSet<string> ignoreProps;
        public IgnorePropertiesResolver(IEnumerable<string> propNamesToIgnore)
        {
            this.ignoreProps = new HashSet<string>(propNamesToIgnore);
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            if (this.ignoreProps.Contains(property.PropertyName!))
            {
                property.ShouldSerialize = _ => false;
            }
            return property;
        }
    }
}
