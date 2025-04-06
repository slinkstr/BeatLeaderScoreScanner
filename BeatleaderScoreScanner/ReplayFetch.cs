using System.Security.Cryptography;
using System.Text;
using ReplayDecoder;

namespace BeatLeaderScoreScanner
{
    internal static class ReplayFetch
    {
        private static string _cachePath = Path.Combine(Path.GetTempPath(), "BeatLeaderScoreScanner");
        private static HttpClient _httpClient = new();

        static ReplayFetch()
        {
            Directory.CreateDirectory(_cachePath);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("BeatLeaderScoreScanner (+https://github.com/slinkstr/BeatleaderScoreScanner/)");
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

            string filename   = GetCachedFilename(uri);
            string cachedFile = Path.Combine(_cachePath, filename);

            if (File.Exists(cachedFile))
            {
                return await FromFile(cachedFile);
            }

            var response = await _httpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            var stream = response.Content.ReadAsStream();
            Replay replay = await DecodeStream(stream);
            stream.Seek(0, SeekOrigin.Begin);
            using (var fileStream = File.Create(cachedFile))
            {
                await stream.CopyToAsync(fileStream);
            }
            return replay;
        }

        public static async Task<Replay> FromFile(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                return await DecodeStream(stream);
            }
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

        private static string GetCachedFilename(Uri uri)
        {
            byte[] hash;
            using (MD5 md5 = MD5.Create())
            {
                md5.Initialize();
                md5.ComputeHash(Encoding.UTF8.GetBytes(uri.AbsoluteUri));
                hash = md5.Hash ?? throw new Exception("Unable to calculate hash.");
            }
            return Convert.ToHexString(hash) + ".bsor";
        }
    }
}
