using ReplayDecoder;

namespace BeatleaderScoreScanner
{
    internal static class ReplayFetch
    {
        private static string _cachePath = Path.Combine(Path.GetTempPath(), "BeatleaderScoreScanner");
        private static HttpClient _httpClient = new();

        static ReplayFetch()
        {
            Directory.CreateDirectory(_cachePath);
        }

        private static async Task<Replay> DecodeStream(Stream stream)
        {
            var (replayInfo, replayTask) = await new AsyncReplayDecoder().StartDecodingStream(stream);

            if (replayInfo == null || replayTask == null)
            {
                throw new Exception("Decoder returned null replay data.");
            }

            var replay = await replayTask;
            return replay ?? throw new Exception("Replay was null.");
        }

        public static async Task<Replay> FromUri(string uri)
        {
            return await FromUri(new Uri(uri));
        }

        public static async Task<Replay> FromUri(Uri uri)
        {
            if (uri.IsFile)
            {
                return await FromFile(uri.AbsolutePath);
            }

            string filename   = uri.Segments.LastOrDefault() ?? throw new Exception("Last segment was null.");
            string cachedFile = Path.Combine(_cachePath, filename);

            if (File.Exists(cachedFile))
            {
                return await FromFile(cachedFile);
            }

            var response = await _httpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            var stream = response.Content.ReadAsStream();

            using (var fileStream = File.Create(cachedFile))
            {
                await stream.CopyToAsync(fileStream);
            }
            stream.Seek(0, SeekOrigin.Begin);

            return await DecodeStream(stream);
        }

        public static async Task<Replay> FromFile(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                return await DecodeStream(stream);
            }
        }
    }
}
