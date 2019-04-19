namespace OK.Cli.Tool
{
    using System;
    using System.IO;
    using Nomadic;
    using Nomadic.ExtensionMethods;

    partial class Config
    {
        private const string ENV_FILENAME = ".ok-cli-tool.env";

        public static readonly CommentAlignment _defaultCommentAlignment = CommentAlignment.ByGroup;
        public static readonly string _defaultPrompt = "> ";
        public static readonly VerbosityLevel _defaultVerbosityLevel = VerbosityLevel.Normal;

        private CommentAlignment? _commentAlignment;
        private string _prompt = null;
        private VerbosityLevel? _verbosityLevel;

        public Config() {
            string envPath;
            if (File.Exists(envPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ENV_FILENAME))) {
                Env.From(envPath, skipParseErrors: true).MergeWithOverride();
            }
            else if (File.Exists(envPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ENV_FILENAME))) {
                Env.From(envPath, skipParseErrors: true).MergeWithOverride();
            }

            Colors = new OutputColors();
        }

        public OutputColors Colors { get; }

        public CommentAlignment CommentAlignment {
            get {
                if (_commentAlignment.HasValue)
                    return _commentAlignment.Value;

                int alignment;
                return int.TryParse(Environment.GetEnvironmentVariable("_OK_COMMENT_ALIGN"), out alignment)
                    ? alignment.ToEnum(_defaultCommentAlignment)
                    : _defaultCommentAlignment;
            }
            set {
                _commentAlignment = value;
            }
        }

        public string Prompt {
            get {
                if (_prompt != null)
                    return _prompt;

                string prompt = Environment.GetEnvironmentVariable("_OK_PROMPT");
                return string.IsNullOrWhiteSpace(prompt)
                    ? _defaultPrompt
                    : prompt;
            }
            set {
                _prompt = value;
            }
        }

        public VerbosityLevel VerbosityLevel {
            get {
                if (_verbosityLevel.HasValue)
                    return _verbosityLevel.Value;

                int level;
                return int.TryParse(Environment.GetEnvironmentVariable("_OK_VERBOSE"), out level)
                    ? level.ToEnum(_defaultVerbosityLevel)
                    : _defaultVerbosityLevel;
            }
            set {
                _verbosityLevel = value;
            }
        }
    }
}
