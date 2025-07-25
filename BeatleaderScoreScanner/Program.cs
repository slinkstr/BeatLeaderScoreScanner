﻿using System.Reflection;
using System.Web;
using BeatLeaderScoreScanner;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ReplayDecoder;

internal class Program
{
    private static HttpClient         _httpClient = new();
    private static ProgramConfig?     _config;

    private static async Task Main(string[] args)
    {
        _config = ProgramConfig.ArgsToConfig(args);
        if (_config == null)
        {
            // Output already handled
            Environment.Exit(0);
        }

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("BeatLeaderScoreScanner (+https://github.com/slinkstr/BeatLeaderScoreScanner/)");

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
            if (result.IsFile)
            {
                if (!config.AllowFile) { throw new Exception("Unable to read file, pass --allow-file to allow."); }
                tasks.AddRange(await AnalyzeFromFile(input, config));
            }
            else if (BeatLeaderDomain.IsValid(result) && result.Segments.Length > 1 && result.Segments[1] == "u/")
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
        analyses = analyses.Select(analysis =>
        {
            if (analysis is null)                              { return null; }
            if (config.RequireFC && analysis.Mistakes > 0)     { return null; }
            if (config.MinimumScore > analysis.Underswing.Percent) { return null; }
            return analysis;
        }).ToArray();

        string output = "";
        foreach (var analysis in analyses)
        {
            output += OutputAnalysis(analysis, config.OutputFormat) + "\n";
        }
        output = output.TrimEnd('\n');

        if(config.OutputFormat == ProgramConfig.Format.json)
        {
            var outputLines = output.Split('\n');
            output = "[" + string.Join(',', outputLines) + "]";
        }

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

            if (analysis.Jitter.Events.Count > 0)
            {
                output += "\nJITTER LINKS:\n";
                foreach (var link in analysis.JitterLinks())
                {
                    output += $"\t{link}\n";
                }
                output = output.TrimEnd('\n');
            }

            if (analysis.OriginReset.Events.Count > 0)
            {
                output += "\nORIGIN RESET LINKS:\n";
                foreach (var link in analysis.OriginResetLinks())
                {
                    output += $"\t{link}\n";
                }
                output = output.TrimEnd('\n');
            }

            if (analysis.Underswing.Events.Count > 0)
            {
                output += "\nUNDERSWING LINKS:\n";
                foreach (var link in analysis.UnderswingLinks())
                {
                    output += $"\t{link}\n";
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
                ContractResolver = new IgnorePropertiesResolver([
                    "frames", "heights", "notes", "pauses", "walls", // Replay
                    "normalized", // Vector3
                    "fps", "head", "leftHand", "rightHand", // Frame
                    "noteCutInfo", // NoteEvent
                ]),
            });
        }

        return output;
    }

    public static async Task<List<Task<ReplayAnalysis?>>> AnalyzeFromProfileId(string playerId, ProgramConfig config)
    {
        List<Task<ReplayAnalysis?>> analysisTasks = [];

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
        return await ScanReplay(replay, config.JitterRequireScoreLoss, new Uri(replayUrl));
    }

    public static async Task<List<Task<ReplayAnalysis?>>> AnalyzeFromFile(string filepath, ProgramConfig config)
    {
        List<Task<ReplayAnalysis?>> analysisTasks = [];
        if (Directory.Exists(filepath))
        {
            var files = Directory.GetFiles(filepath);
            foreach (var file in files)
            {
                if (!file.EndsWith(".bsor")) { continue; }

                var replay = await ReplayFetch.FromFile(file);
                analysisTasks.Add(ScanReplay(replay, config.JitterRequireScoreLoss, new Uri(file)));
            }
        }
        else if (File.Exists(filepath))
        {
            var replay = await ReplayFetch.FromFile(filepath);
            analysisTasks.Add(ScanReplay(replay, config.JitterRequireScoreLoss, new Uri(filepath)));
        }
        else
        {
            throw new Exception("File not found: " + filepath);
        }

        return analysisTasks;
    }

    private static async Task<ReplayAnalysis> ScanScore(dynamic score, ProgramConfig config)
    {
        // required for linking leaderboards/replays
        var scoreReplayUrl     = new Uri((string)score.replay);
        var scoreLeaderboardId = (string)score.leaderboardId;

        var replay = await ReplayFetch.FromUri((string)score.replay);
        var analysis = await ScanReplay(replay, config.JitterRequireScoreLoss, scoreReplayUrl, scoreLeaderboardId);
        return analysis;
    }

    private static async Task<ReplayAnalysis> ScanReplay(Replay replay, bool requireScoreLoss, Uri replayUrl, string leaderboardId = "")
    {
        ReplayAnalysis analysis = await Task.Run(() => new ReplayAnalysis(replay, requireScoreLoss, replayUrl, leaderboardId));
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
