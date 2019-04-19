namespace Nomadic.ExtensionMethods
{
    using System.Collections.Generic;
    using System.Linq;
    using McMaster.Extensions.CommandLineUtils;

    static class CommandOptionExtensions {
        public static CommandOption GetOption(this List<CommandOption> options, string longName) {
            return options.SingleOrDefault(opt => opt.LongName == longName);
        }
    }
}
