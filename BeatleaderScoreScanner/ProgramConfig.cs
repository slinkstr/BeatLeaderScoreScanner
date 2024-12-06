using System.Reflection;
using CommandLine;
using CommandLine.Text;

namespace BeatleaderScoreScanner
{
    internal class ProgramConfig
    {
        public static ProgramConfig? ArgsToConfig(string[] args)
        {
            var parser = new Parser(p => {
                p.HelpWriter = null;
                p.CaseInsensitiveEnumValues = true;
            });
            var parserResult = parser.ParseArguments<ProgramConfig>(args);

            ProgramConfig? config = null;
            parserResult
                .WithParsed((cfg) => config = cfg)
                .WithNotParsed((errs) =>
                {
                    if (errs.First() is VersionRequestedError)
                    {
                        DisplayVersion();
                    }
                    else if (errs.All(x => x is HelpRequestedError))
                    {
                        DisplayHelp(parserResult, false);
                    }
                    else
                    {
                        DisplayHelp(parserResult, true);
                    }
                });

            return config;
        }

        private static void DisplayHelp<T>(ParserResult<T> result, bool stderr)
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName();

            // such a pain in the ass to customize this
            var helpText = HelpText.AutoBuild(result, h =>
            {
                h.Heading = $"{assemblyName.Name} {assemblyName.Version}";
                h.Copyright = "";
                h.AdditionalNewLineAfterOption = false;
                h.AddEnumValuesToHelpText = true;
                h.AddNewLineBetweenHelpSections = true;
                h.AutoHelp = false;
                h.AutoVersion = false;
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);

            if(stderr)
            {
                Console.Error.WriteLine(helpText);
            }
            else
            {
                Console.WriteLine(helpText);
            }
        }

        private static void DisplayVersion()
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName();
            Console.WriteLine(assemblyName.Version);
        }

        [Option('o', "output-format", Default = Format.text, HelpText = "Changes output format.")]
        public Format              OutputFormat { get; private set; } = Format.text;

        [Option('s', "minimum-score", Default = 0, HelpText = "Only process replays that are above this percentage when scanning a profile. Valid values: 0.0 - 1.0")]
        public float               MinimumScore { get; private set; }

        [Option('f', "require-fc", HelpText = "Only process replays that full combo when scanning a profile.")]
        public bool                RequireFC    { get; private set; } = false;

        [Option('a', "allow-file", HelpText = "Allow reading replay files from disk.")]
        public bool                AllowFile    { get; private set;} = false;

        [Option('h', "help", HelpText = "Display this help text.")]
        public bool Help { get; set; } = false;

        [Option('v', "version", HelpText = "Display application version.")]
        public bool Version { get; set; } = false;

        [Value(0, Min = 1, MetaName = "input", HelpText = "Beatleader profile ID or URL, or replay URL.")]
        public IEnumerable<string> Inputs       { get; private set; } = [];

        public enum Format
        {
            text,
            json
        }

        [Usage(ApplicationAlias = "BeatleaderScoreScanner")]
        public static List<Example> Examples { get; private set; } =
        [
            new Example("Scan profile"    , new ProgramConfig { Inputs = new List<string>() { "<profile>" } }),
            new Example("Scan score"      , new ProgramConfig { Inputs = new List<string>() { "<replay_url>" } }),
        ];
    }
}
