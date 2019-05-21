namespace OK.Cli.Tool
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Text;
    using McMaster.Extensions.CommandLineUtils;
    using Nomadic.ExtensionMethods;
    using OK.Cli.Tool.OKItems;

    using static System.Runtime.InteropServices.RuntimeInformation;

    class Program
    {
        // Exit Codes
        const int OK = 0;
        const int UNEXPECTED_ERROR = 1;
        const int INVALID_ARGUMENT = 2;

        Config _config = new Config();
        OKItemList _okItemList;

        static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                Console.WriteLine($"Unexpected Error: {eventArgs.ExceptionObject.ToString()}");
                Environment.Exit(UNEXPECTED_ERROR);
            };

            var orginalColor = Console.ForegroundColor;
            Console.CancelKeyPress  += (sender, eventArgs) => {
                Console.ForegroundColor = orginalColor;
            };

            return new Program().Run(args);
        }

        public int Run(string[] args)
        {
            var options = CommandLineOptions.Parse(args, _config);
            if (options.WasHelpRequested)
                return ShowHelp();
            if (options.WasInfoRequested)
                return ShowInfo();
            if (options.WasVersionRequestd)
                return ShowVersion();
            if (!File.Exists(options.FileName))
                return OK;

            _okItemList = OKItemList.FromFile(options.FileName);

            if (options.RemainingArgs.Count == 0) {
                _okItemList.WriteList(_config);
                Console.ForegroundColor = _config.Colors.Prompt;
                Console.Write(_config.Prompt);
                var input = Console.ReadLine().Trim();
                if (string.IsNullOrEmpty(input)) {
                    Console.ForegroundColor = _config.Colors.Original;
                    return OK;
                }

                var inputParts = input.Split(' ');
                options.RemainingArgs.Add(inputParts[0]);
                if (inputParts.Length > 1)
                    options.RemainingArgs.Add(input.Substring(inputParts[0].Length).TrimStart());
            } else if (options.WasListRequested) {
                _okItemList.WriteList(_config);
            }

            if (!int.TryParse(options.RemainingArgs[0], out int commandNumber)) {
                _okItemList.WriteList(_config);
                return OK;
            }

            int maxCommandNumber = _okItemList.MaxCommandNumber;
            if (commandNumber < 1 || commandNumber > maxCommandNumber) {
                Console.WriteLine($"Command number '{commandNumber}' is out of range. Must be between 1 and {maxCommandNumber}");
                return INVALID_ARGUMENT;
            }

            var commandItem = _okItemList.FindCommandByNumber(commandNumber);
            if (_config.VerbosityLevel != VerbosityLevel.Quiet)
            {
                Console.ForegroundColor = _config.Colors.Prompt;
                Console.Write($"{_config.Prompt}{commandItem.CommandText}");
                if (commandItem.HasComment)
                {
                    Console.ForegroundColor = _config.Colors.Comment;
                    Console.Write($" {commandItem.CommentText}");
                }
                Console.ForegroundColor = _config.Colors.Original;
                Console.WriteLine();
            }

            return RunCommand(commandItem, options);
        }

        private int RunCommand(OKCommandItem commandItem, CommandLineOptions options) {
            // TODO: for Linux/Mac (NEEDS TESTING)
            var sbArgs = new StringBuilder(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? $"/C {commandItem.CommandText}"
                : $"-c {commandItem.CommandText}");
            for (int i = 1; i < options.RemainingArgs.Count; i++)
            {
                sbArgs.Append(" ").Append(options.RemainingArgs[i]);
            }

            var proc = new Process();
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            // TODO: for Linux/Mac (NEEDS TESTING)
            proc.StartInfo.FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "cmd.exe"
                : "/bin/bash";
            proc.StartInfo.Arguments = sbArgs.ToString();
            proc.Start();
            string output = proc.StandardOutput.ReadToEnd();
            string errors = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (output.Length > 0)
                Console.WriteLine(output);
            if (_config.VerbosityLevel != VerbosityLevel.Quiet && errors.Length > 0)
                Console.WriteLine(errors);

            return proc.ExitCode;
        }

        private int ShowHelp()
        {
            var appAssembly = typeof(Program).Assembly;

            Console.ForegroundColor = _config.Colors.Comment;
            Console.WriteLine(
$@"{appAssembly.GetTitle()} v{appAssembly.GetVersion()}
{appAssembly.GetCopyright()}
{appAssembly.GetDescription()}
");
            Console.ForegroundColor = _config.Colors.Heading;
            Console.Write("Usage: ");
            Console.ForegroundColor = _config.Colors.Command;
            Console.WriteLine("ok [options] <number> [script-arguments...]");
            Console.WriteLine("       ok <command> [options]");
            Console.WriteLine();
            Console.ForegroundColor = _config.Colors.Heading;
            Console.WriteLine("number:");
            WriteKeyValue("  1..commandCount       ", "Run <number>th command from the '.ok' file.");
            Console.WriteLine();
            Console.ForegroundColor = _config.Colors.Heading;
            Console.WriteLine("script-arguments:");
            WriteKeyValue("  ...                   ", "These are passed through, when a line is executed");
            Console.WriteLine();
            Console.ForegroundColor = _config.Colors.Heading;
            Console.WriteLine("command:");
            WriteKeyValue("  l, list               ", "Show the list from the '.ok' file.");
            WriteKeyValue("                        ", "Default command when none are specified.");
            WriteKeyValue("  h, help               ", "Show this usage information.");
            Console.WriteLine();
            Console.ForegroundColor = _config.Colors.Heading;
            Console.WriteLine("options:");
            WriteKeyValue("  -c, --comment_align N ", "Level of comment alignment.");
            WriteKeyValue("                        ", "0=no alignment, 1=align consecutive lines (Default),");
            WriteKeyValue("                        ", "2=including whitespace, 3 align all.");
            WriteKeyValue("  -f, --file            ", "Use a custom file instead of '.ok'.");
            WriteKeyValue("  -h, --help            ", "Show this usage information.");
            WriteKeyValue("  -i, --info            ", "Show tool and runtime information.");
            WriteKeyValue("  -v, --verbose         ", "Show more output, mostly errors.");
            WriteKeyValue("                        ", "Will also show environment-variables in this screen.");
            WriteKeyValue("  -q, --quiet           ", "Show less output.");
            WriteKeyValue("  -V  --version         ", "Show version # and exit.");
            if (_config.VerbosityLevel == VerbosityLevel.Verbose) {
                Console.WriteLine();
                Console.ForegroundColor = _config.Colors.Heading;
                Console.WriteLine("environment variables (used for colored output, see System.ConsoleColor):");
                WriteKeyValue("  _OK_C_HEADING         ", "Color for lines starting with a comment (heading).");
                WriteKeyValue("                        ", "Defaults to red.");
                WriteKeyValue("  _OK_C_NUMBER          ", "Color for numbering. Defaults to cyan.");
                WriteKeyValue("  _OK_C_COMMENT         ", "Color for comments. Defaults to blue.");
                WriteKeyValue("  _OK_C_COMMAND         ", "Color for commands.");
                WriteKeyValue("                        ", "Defaults to standard terminal color.");
                WriteKeyValue("  _OK_C_PROMPT          ", "Color for prompt. Defaults to cyan.");
                Console.ForegroundColor = _config.Colors.Heading;
                Console.WriteLine("environment variables (other configuration):");
                WriteKeyValue("  _OK_COMMENT_ALIGN     ", "Level of comment alignment. 0=no alignment,");
                WriteKeyValue("                        ", "1=align consecutive lines (Default),");
                WriteKeyValue("                        ", "2=including whitespace, 3 align all.");
                WriteKeyValue("  _OK_PROMPT            ", "String used as prompt. Defaults to '> '.");
                WriteKeyValue("  _OK_VERBOSE           ", "Level of feedback ok cli provides.");
                WriteKeyValue("                        ", "0=quiet, 1=normal (Default),");
                WriteKeyValue("                        ", "2=verbose. Can be overriden with --verbose or --quiet.");
                Console.WriteLine();
                Console.ForegroundColor = _config.Colors.Original;
                Console.WriteLine($"NOTE: environment variables may also be set in {_config.DotEnvConfigFilePath}");
            }
            Console.ForegroundColor = _config.Colors.Original;

            return OK;
        }

        private int ShowInfo()
        {
            var programType = typeof(Program);
            var appAssembly = programType.Assembly;
            string gitCommit;

            using (var resource = appAssembly.GetManifestResourceStream($"{programType.Namespace}.gitcommit.txt"))
            using (var sr = new StreamReader(resource))
                gitCommit = sr.EndOfStream ? "[Not Specified]" : sr.ReadLine().Substring(0, 8);

            var osPlatform
                = IsOSPlatform(OSPlatform.Linux) ? "Linux"
                    : IsOSPlatform(OSPlatform.OSX) ? "macOS"
                    : IsOSPlatform(OSPlatform.Windows) ? "Windows"
                    : "Unknown";

            Console.ForegroundColor = _config.Colors.Heading;
            Console.WriteLine($@"{appAssembly.GetName().Name}:");
            WriteKeyValue("  Version:   ", appAssembly.GetVersion());
            WriteKeyValue("  Commit #:  ", gitCommit);
            Console.WriteLine();
            Console.ForegroundColor = _config.Colors.Heading;
            Console.WriteLine("Runtime Environment:");
            WriteKeyValue("  .NET Framework:   ", Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName);
            WriteKeyValue("  OS Name:          ", OSDescription);
            WriteKeyValue("  OS Platform:      ", $"{osPlatform} {(Environment.Is64BitOperatingSystem ? 64 : (IntPtr.Size * 8))}-bit");
            WriteKeyValue("  Base Path:        ", AppContext.BaseDirectory);
            WriteKeyValue("  .env Config File: ", _config.DotEnvConfigFilePath);
            Console.WriteLine();
            Console.ForegroundColor = _config.Colors.Heading;
            Console.WriteLine("Configuration:");
            WriteKeyValue("  _OK_COMMENT_ALIGN: ", _config.CommentAlignment.ToString());
            WriteKeyValue("  _OK_PROMPT:        ", $"\"{_config.Prompt}\"");
            WriteKeyValue("  _OK_VERBOSE:       ", _config.VerbosityLevel.ToString());
            Console.WriteLine();
            Console.ForegroundColor = _config.Colors.Heading;
            Console.WriteLine("Configuration Colors:");
            WriteKeyValue("  _OK_C_HEADING: ", _config.Colors.Heading.ToString());
            WriteKeyValue("  _OK_C_NUMBER:  ", _config.Colors.Number.ToString());
            WriteKeyValue("  _OK_C_COMMENT: ", _config.Colors.Comment.ToString());
            WriteKeyValue("  _OK_C_COMMAND: ", _config.Colors.Command.ToString());
            WriteKeyValue("  _OK_C_PROMPT : ", _config.Colors.Prompt.ToString());
            Console.ForegroundColor = _config.Colors.Original;

            return OK;
        }

        private int ShowVersion()
        {
            Console.WriteLine(typeof(Program).Assembly.GetVersion());
            return OK;
        }

        private void WriteKeyValue(string key, string value)
        {
            Console.ForegroundColor = _config.Colors.Number;
            Console.Write(key);
            Console.ForegroundColor = _config.Colors.Command;
            Console.WriteLine(value);
        }
    }
}
