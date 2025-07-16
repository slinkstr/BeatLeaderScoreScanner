namespace BeatLeaderScoreScanner
{
    internal static class BeatLeaderDomain
    {
        public static string   Base   = "beatleader";
        public static string[] Tlds   = [ "com", "net", "xyz" ];
        public static string   Api    = $"https://api.{Base}.{Tlds[0]}";
        public static string   Replay = $"https://replay.{Base}.{Tlds[0]}";

        public static bool IsValid(string input)
        {
            if (!Uri.TryCreate(input, UriKind.Absolute, out Uri? uri) || uri == null)
            {
                throw new Exception("Input must be a valid URI.");
            }

            return IsValid(uri);
        }

        public static bool IsValid(Uri uri)
        {
            string[] hostSegments = uri.Host.Split(".");
            if (hostSegments.Length < 2)              { return false; }
            if (hostSegments[^2] != Base)             { return false; }
            if (Tlds.All(x => x != hostSegments[^1])) { return false; }

            return true;
        }

        public static bool IsReplay(Uri uri)
        {
            if (!IsValid(uri)) { return false; }

            string[] hostSegments = uri.Host.Split(".");
            if (hostSegments.Length != 3)    { return false; }
            if (hostSegments[0] != "replay") { return false; }

            return true;
        }

        public static bool IsReplayCdn(Uri uri)
        {
            if (!IsValid(uri)) { return false; }

            string[] hostSegments = uri.Host.Split(".");
            if (hostSegments.Length != 4)     { return false; }
            if (hostSegments[0] != "cdn")     { return false; }
            if (hostSegments[1] != "replays") { return false; }

            return true;
        }
    }
}
