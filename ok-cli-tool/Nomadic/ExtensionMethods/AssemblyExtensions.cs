namespace Nomadic.ExtensionMethods
{
    using System;
    using System.Reflection;

    public static class AssemblyExtensions {
        public static string GetCopyright(this Assembly assembly, string defaultValue = null) {
            return assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? defaultValue ?? ("Copyright " + DateTime.Now.ToString("yyyy"));
        }

        public static string GetDescription(this Assembly assembly, string defaultValue = "") {
            return assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? defaultValue;
        }

        public static string GetTitle(this Assembly assembly, string defaultValue = null) {
            return assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? defaultValue ?? assembly.GetName().Name;
        }

        public static string GetVersion(this Assembly assembly, string defaultValue = "n/a") {
            return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? defaultValue;
        }
    }
}
