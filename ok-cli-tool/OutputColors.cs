namespace Gonzal.OK.Cli.Tool
{
    using System;
    using ExtensionMethods;

    public class OutputColors
    {
        private readonly ConsoleColor originalForegroundColor = Console.ForegroundColor;

        public ConsoleColor Command {
            get { return GetColorFromString("_OK_C_COMMAND", originalForegroundColor); }
        }

        public ConsoleColor Comment {
            get { return GetColorFromString("_OK_C_COMMENT", ConsoleColor.Blue); }
        }

        public ConsoleColor Heading {
            get { return GetColorFromString("_OK_C_HEADING", ConsoleColor.Red); }
        }

        public ConsoleColor Number {
            get { return GetColorFromString("_OK_C_NUMBER", ConsoleColor.Cyan); }
        }

        public ConsoleColor Original {
            get { return originalForegroundColor; }
        }

        public ConsoleColor Prompt {
            get { return GetColorFromString("_OK_C_PROMPT", ConsoleColor.Cyan); }
        }

        private ConsoleColor GetColorFromString(string envVarKey, ConsoleColor defaultColor)
        {
            string colorName = Environment.GetEnvironmentVariable(envVarKey);
            return colorName.ToEnum(defaultColor);
        }
    }
}
