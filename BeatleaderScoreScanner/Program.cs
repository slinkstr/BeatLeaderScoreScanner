using ReplayDecoder;
using Newtonsoft.Json;
using BeatleaderScoreScanner;
using static System.Net.WebRequestMethods;

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
                UnderswingDetector.PrintUnderswing(score, replay);
                JitterDetector.PrintJitter(replay);

                //var frames = JitterDetector.MovementDirectionJitter(replay);
                //foreach (var frame in frames)
                //{
                //    var url = $"https://replay.beatleader.xyz/?scoreId={score.id}&time={(int)(frame.time * 1000) - 50}&speed=2";
                //    await Console.Out.WriteLineAsync(url);
                //}
            }
        }
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

    internal static DateTime UnixToDateTime(long unixTimestamp)
    {
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimestamp);
        return dateTime;
    }
}