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
        { 22380709, 7522   },
        { 22380659, 156    },
        { 22380630, 136    },
        { 22380608, 88     },
        { 22380476, 16     },
        { 22380411, 24     },
        { 22380323, 240    },
        { 22313318, 6608   },
        { 22313233, 3116   },
        { 22313132, 1464   },
        // 2
        { 22313047, 10536  },
        { 22313001, 3616   },
        { 22312914, 14491  },
        { 22312775, 2688   },
        { 22312727, 24301  },
        { 22312655, 608    },
        { 22312614, 8      },
        { 22312590, 2268   },
        { 22312524, 80     },
        { 22236892, 16504  },
        // 3
        { 22236751 , 1976   },
        { 22236592 , 64     },
        { 22236550 , 0      },
        { 22236510 , 40     },
        { 22236473 , 16     },
        { 22236449 , 0      },
        { 22236407 , 24     },
        { 22236368 , 24     },
        { 22144077 , 11410  },
        { 22143839 , 10825  },
        // 4
        { 22143659 , 10419  },
        { 22143555 , 673733 },
        { 22043048 , 1416   },
        { 22042904 , 2889   },
        { 22042848 , 428    },
        { 22042824 , 832    },
        { 22042645 , 1016   },
        { 22042597 , 80     },
        { 22042556 , 0      },
        { 22042525 , 424    },
        // 5
        { 22042497, 344     },
        { 22042428, 1289    },
        { 21999235, 9819    },
        { 21999088, 4312    },
        { 21998992, 3120    },
        { 21998834, 1248    },
        { 21998771, 368     },
        { 21998742, 128     },
        { 21998673, 56      },
        { 21998646, 28      },
        // 6
        { 21998602, 128     },
        { 21998526, 248     },
        { 21937672, 3776    },
        { 21919847, 19392   },
        { 21919768, 5458    },
        { 21919630, 2776    },
        { 21919575, 136     },
        { 21919462, 0       },
        { 21919389, 0       },
        { 21919327, 16      },
        // 7
        { 21876431, 66106   },
        { 21876256, 1412    },
        { 21876211, 0       },
        { 21876130, 0       },
        { 21751570, 1744    },
        { 21751204, 8301    },
        { 21751088, 5672    },
        { 21751049, 1240    },
        { 21751019, 104     },
        { 21750981, 0       },
        // 8
        { 21750944, 0       },
        { 21750915, 8       },
        { 21750870, 16      },
        { 21680501, 4158    },
        { 21680372, 1956    },
        { 21680352, 2524    },
        { 21680313, 1556    },
        { 21680271, 1672    },
        { 21680246, 0       },
        { 21680239, 0       },
        // 9
        { 21680198, 32      },
        { 21490881, 2384    },
        { 21490810, 13392   },
        { 21490765, 2156    },
        { 21490695, 16      },
        { 21490667, 8       },
        { 21317915, 25310   },
        { 21317704, 0       },
        { 21317652, 0       },
        { 21317617, 0       },
        // 10
        { 21317569, 0       },
        { 21317530, 104     },
        { 21172887, 4166    },
        { 21172779, 944     },
        { 21172731, 28447   },
        { 21172684, 112     },
        { 21172627, 8       },
        { 21172592, 0       },
        { 21172554, 24      },
        { 21095534, 15421   },
        // 11
        { 21095457, 3668    },
        { 21095135, 0       },
        { 21095068, 0       },
        { 20979209, 6720    },
        { 20979144, 920     },
        { 20978988, 512     },
        { 20978920, 10168   },
        { 20978867, 6448    },
        { 20978810, 43572   },
        { 20978748, 4298    },
        // 12
        { 20978648, 301     },
        { 20978557, 88      },
        { 20927768, 24771   },
        { 20927691, 3288    },
        { 20927618, 7498    },
        { 20927557, 13744   },
        { 20927500, 2999    },
        { 20927439, 945     },
        { 20927413, 40      },
        { 20927384, 0       },
        // 13
        { 20927360, 40      },
        { 20927322, 472     },
        { 20927262, 240     },
        { 20927212, 136     },
        { 20927157, 248     },
        { 20927118, 8       },
        { 20754446, 7769    },
        { 20754308, 1424    },
        { 20754213, 28      },
        { 20754156, 8       },
        // 14
        { 20682124, 4148    },
        { 20681894, 2712    },
        { 20681775, 6704    },
        { 20681722, 1120    },
        { 20681684, 10036   },
        { 20681610, 30454   },
        { 20593733, 3199    },
        { 20593713, 8196    },
        { 20593573, 9488    },
        { 20593520, 4696    },
        // 15
        { 20593422, 232     },
        { 20547468, 7594    },
        { 20547189, 2608    },
        { 20547123, 672     },
        { 20546999, 5580    },
        { 20546949, 32      },
        { 20546888, 72      },
        { 20546830, 0       },
        { 20546795, 32      },
        { 20503768, 624     },
        // 16
        { 20503682, 2152    },
        { 20503620, 1720    },
        { 20503595, 5156    },
        { 20503539, 52      },
        { 20503485, 132     },
        { 20423030, 2664    },
        { 20422752, 464     },
        { 20422632, 24      },
        { 20422557, 0       },
        { 20422480, 232     },
        // 17
        { 20259582, 184     },
        { 20259429, 8       },
        { 20259388, 208     },
        { 20259320, 350     },
        { 20248142, 32      },
        { 20247709, 0       },
        { 20247635, 32      },
        { 20247440, 40      },
        { 20247362, 48      },
        { 20247285, 108     },
        // 18
        { 20237246, 80      },
        { 20237101, 24      },
        { 20237056, 128     },
        { 20236991, 0       },
        { 20236928, 0       },
        { 20236882, 32      },
        { 20236842, 40      },
        { 20236801, 0       },
        { 20214074, 1224    },
        { 20214052, 1608    },
        // 19
        { 20214021, 32      },
        { 20213979, 0       },
        { 20213944, 0       },
        { 20168817, 15361   },
        { 20168595, 8796    },
        { 20168542, 2436    },
        { 20168521, 194     },
        { 20168393, 0       },
        { 20168350, 0       },
        { 20168320, 0       },
        // 20
        { 20077113, 32424   },
        { 20077013, 3560    },
        { 20076936, 830     },
        { 20076910, 4412    },
        { 20076884, 1384    },
        { 20076805, 240     },
        { 20076770, 80      },
        { 20076741, 360     },
        { 20076708, 9985    },
        { 20076686, 32      },
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
            // Output already handled
            Environment.Exit(0);
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
