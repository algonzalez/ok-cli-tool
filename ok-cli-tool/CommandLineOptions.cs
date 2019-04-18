namespace Gonzal.OK.Cli.Tool
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Gonzal.ExtensionMethods;
    using McMaster.Extensions.CommandLineUtils;

    class CommandLineOptions
    {
        public static readonly string DefaultFileName = ".ok";

        public string FileName { get; private set; }
        public List<string> RemainingArgs { get; private set; }

        public bool WasHelpRequested { get; private set; }
        public bool WasInfoRequested { get; private set; }
        public bool WasListRequested { get; private set; }
        public bool WasVersionRequestd { get; private set; }

        public static CommandLineOptions Parse(string[] args, Config config)
        {
            var options = new CommandLineOptions();
            var app = options.CreateCommandLineApplication();
            if (app.Execute(args) != 0) return null;

            options.RemainingArgs = new List<string>(app.RemainingArguments);

            options.FileName = app.Options.GetOption("file").Values.FirstOrDefault() ?? DefaultFileName;
            options.WasHelpRequested = app.Options.GetOption("help").HasValue();
            options.WasInfoRequested = app.Options.GetOption("info").HasValue();
            options.WasVersionRequestd = app.Options.GetOption("version").HasValue();

            CommandOption opt;
            if ((opt = app.Options.GetOption("verbose")) != null && opt.HasValue()) {
                config.VerbosityLevel = VerbosityLevel.Verbose;
            }
            else if ((opt = app.Options.GetOption("quiet")) != null && opt.HasValue()) {
                config.VerbosityLevel = VerbosityLevel.Quiet;
            }
            if ((opt = app.Options.GetOption("comment_align")) != null && opt.HasValue()) {
                int alignment;
                if (int.TryParse(opt.Value(), out alignment))
                    config.CommentAlignment = alignment.ToEnum(CommentAlignment.ByGroup);
            }

            return options;
        }

        private CommandLineApplication CreateCommandLineApplication()
        {
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

            Action<CommandLineApplication> showListAction = c => c.OnExecute(() => WasListRequested = true);
            app.Command("l", showListAction);
            app.Command("ls", showListAction);
            app.Command("list", showListAction);

            Action<CommandLineApplication> showHelpAction = c => c.OnExecute(() => WasHelpRequested = true);
            app.Command("h", showHelpAction);
            app.Command("help", showHelpAction);

            app.Option("-c|--comment_align", "Level of comment alignment", CommandOptionType.SingleValue);
            app.Option("-f|--file", "Use a custom file instead of '.ok'.", CommandOptionType.SingleValue);
            app.Option("-h|--help", "Show this usage information.", CommandOptionType.NoValue);
            app.Option("-i|--info", "Show tool and runtime information.", CommandOptionType.NoValue);
            app.Option("-v|--verbose", "Show more output, mostly errors. Will also show environment-variables in this screen.", CommandOptionType.NoValue);
            app.Option("-q|--quiet", "Show less output.", CommandOptionType.NoValue);
            app.Option("-V|--version", "Show version # and exit.", CommandOptionType.NoValue);

            return app;
        }
    }
}
