namespace Gonzal.OK.Cli.Tool
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using Gonzal.ExtensionMethods;
    using Gonzal.OK.Cli.Tool.OKItems;
    using McMaster.Extensions.CommandLineUtils;

    class Program
    {
        // Exit Codes
        const int OK = 0;
        const int UNEXPECTED_ERROR = 1;
        const int INVALID_ARGUMENT = 2;

        static readonly string DefaultFileName = ".ok";

        Config _config;
        string _okFileName = DefaultFileName;
        OKItemList _okItemList;

        static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                Console.WriteLine($"Unexpected Error: {eventArgs.ExceptionObject.ToString()}");
                Environment.Exit(UNEXPECTED_ERROR);
            };

            return new Program().Run(args);
        }

        private EventHandler ShowListCommandEventHandler;

        private void ShowListCommand(object sender, EventArgs e)
        {
            if (_okItemList == null && File.Exists(_okFileName))
                _okItemList = OKItemList.FromFile(_okFileName);
            if (_okItemList != null)
                _okItemList.WriteList(_config);
            Environment.Exit(OK);
        }

        public int Run(string[] args)
        {
            _config = new Config();

            this.ShowListCommandEventHandler += ShowListCommand;

            var app = CreateCommandLineApplication();
            app.Execute(args);

            CommandOption opt;
            if ((opt = app.Options.GetOption("verbose")) != null && opt.HasValue()) {
                _config.VerbosityLevel = VerbosityLevel.Verbose;
            }
            else if ((opt = app.Options.GetOption("quiet")) != null && opt.HasValue()) {
                _config.VerbosityLevel = VerbosityLevel.Quiet;
            }
            if ((opt = app.Options.GetOption("comment_align")) != null && opt.HasValue()) {
                int alignment;
                if (int.TryParse(opt.Value(), out alignment))
                    _config.CommentAlignment = alignment.ToEnum(CommentAlignment.ByGroup);
            }
            if (app.Options.GetOption("help").HasValue()) {
                ShowHelp();
                return OK;
            }
            if (app.Options.GetOption("version").HasValue()) {
                Console.WriteLine(typeof(Program).Assembly.GetVersion());
                return OK;
            }
            _okFileName = app.Options.GetOption("file").Values.FirstOrDefault() ?? DefaultFileName;
            if (!File.Exists(_okFileName)) return 0;

            _okItemList = OKItemList.FromFile(_okFileName);

            if (app.RemainingArguments.Count == 0) {
                _okItemList.WriteList(_config);
                return OK;
            }

            int? commandNumber = null;
            if (app.RemainingArguments.Count > 0) {
                int number;
                if (int.TryParse(app.RemainingArguments[0], out number))
                    commandNumber = number;
            }

            if (!commandNumber.HasValue) {
                _okItemList.WriteList(_config);
                return OK;
            }

            int maxCommandNumber = _okItemList.MaxCommandNumber;
            if (commandNumber < 1 || commandNumber > maxCommandNumber) {
                Console.WriteLine($"Command number '{commandNumber}' is out of range. Must be between 1 and {maxCommandNumber}");
                return INVALID_ARGUMENT;
            }

            var okItem = _okItemList.FindCommandByNumber(commandNumber.Value);
            if (_config.VerbosityLevel != VerbosityLevel.Quiet)
            {
                Console.ForegroundColor = _config.Colors.Prompt;
                Console.Write($"{_config.Prompt}{okItem.CommandText}");
                if (okItem.HasComment)
                {
                    Console.ForegroundColor = _config.Colors.Comment;
                    Console.Write($" {okItem.CommentText}");
                }
                Console.ForegroundColor = _config.Colors.Original;
                Console.WriteLine();
            }

            // TODO: for Linux/Mac (NEEDS TESTING)
            //       does command need to be quoted???
            // var sbArgs = new StringBuilder($"-c {okLine.CommandText}");
            var sbArgs = new StringBuilder($"/C {okItem.CommandText}");
            for (int i = 1; i < app.RemainingArguments.Count; i++)
            {
                var arg = app.RemainingArguments[i];
                sbArgs.Append(" ");
                if (!arg.Contains(' '))
                    sbArgs.Append(arg);
                else
                    sbArgs.Append("\"").Append(arg).Append("\"");
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

        private CommandLineApplication CreateCommandLineApplication() {
            var appAssembly = typeof(Program).Assembly;
            var appVersion = appAssembly.GetVersion();
            var title = appAssembly.GetTitle();

            var app = new CommandLineApplication() {
                Name = title,
                FullName = title,
                Description = appAssembly.GetDescription().Replace("\\n", "\n"),
                ShortVersionGetter = () => appVersion,
                LongVersionGetter = () => "v" + appVersion,
                ThrowOnUnexpectedArgument = false,
            };

            Action<CommandLineApplication> showListAction = configCmd => {
                configCmd.OnExecute(() => ShowListCommandEventHandler.Invoke(this, null));
            };
            app.Command("l", showListAction);
            app.Command("ls", showListAction);
            app.Command("list", showListAction);

            Action<CommandLineApplication> showHelpAction = configCmd => {
                configCmd.OnExecute(() => {
                    ShowHelp();
                    Environment.Exit(OK);
                });
            };
            app.Command("h", showHelpAction);
            app.Command("help", showHelpAction);

            app.Option("-c|--comment_align", "Level of comment alignment", CommandOptionType.SingleValue);
            app.Option("-f|--file", "Use a custom file instead of '.ok'.", CommandOptionType.SingleValue);
            app.Option("-h|--help", "Show this usage information.", CommandOptionType.NoValue);
            app.Option("-v|--verbose", "Show more output, mostly errors. Will also show environment-variables in this screen.", CommandOptionType.NoValue);
            app.Option("-q|--quiet", "Show less output.", CommandOptionType.NoValue);
            app.Option("--version", "Show version # and exit.", CommandOptionType.NoValue);

            return app;
        }

        private void ShowHelp()
        {
            var appAssembly = typeof(Program).Assembly;

            Console.WriteLine(
$@"{appAssembly.GetTitle()} v{appAssembly.GetVersion()}
{appAssembly.GetCopyright()}
{appAssembly.GetDescription()}

Usage: ok [options] <number> [script-arguments...]
       ok <command> [options]

number:
  1..commandCount       Run <number>th command from the '.ok' file.

script-arguments:
  ...                   These are passed through, when a line is executed

command:
  l, list               Show the list from the '.ok' file.
                        Default command when none are specified.
  h, help               Show this usage information.

options:
  -c, --comment_align N Level of comment alignment.
                        0=no alignment, 1=align consecutive lines (Default),
                        2=including whitespace, 3 align all.
  -f, --file            Use a custom file instead of '.ok'.
  -h, --help            Show this usage information.
  -v, --verbose         Show more output, mostly errors.
                        Will also show environment-variables in this screen.
  -q, --quiet           Show less output.
      --version         Show version # and exit."
  + (_config.VerbosityLevel == VerbosityLevel.Verbose ?
@"

environment variables (used for colored output, see System.ConsoleColor):
  _OK_C_HEADING         Color for lines starting with a comment (heading).
                        Defaults to red.
  _OK_C_NUMBER          Color for numbering. Defaults to cyan.
  _OK_C_COMMENT         Color for comments. Defaults to blue.
  _OK_C_COMMAND         Color for commands.
                        Defaults to standard terminal color.
  _OK_C_PROMPT          Color for prompt. Defaults to cyan.
environment variables (other configuration):
  _OK_COMMENT_ALIGN     Level of comment alignment. 0=no alignment,
                        1=align consecutive lines (Default),
                        2=including whitespace, 3 align all.
  _OK_PROMPT            String used as prompt. Defaults to '> '.
  _OK_VERBOSE           Level of feedback ok cli provides.
                        0=quiet, 1=normal (Default),
                        2=verbose. Can be overriden with --verbose or --quiet.
" : "")
            );
        }
    }
}
