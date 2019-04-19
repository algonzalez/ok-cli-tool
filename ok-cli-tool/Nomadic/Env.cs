namespace Nomadic
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    public class Env : IReadOnlyDictionary<string, string>
    {
        private const string DEFAULT_ENV_FILENAME = ".env";

        private bool _skipParseErrors;
        private Dictionary<string, string> _envMap = new Dictionary<string, string>();

        public static Env FromDotEnvFile(bool skipParseErrors = false)
            => From(Path.GetFullPath(DEFAULT_ENV_FILENAME), skipParseErrors);

        public static Env From(string filePath, bool skipParseErrors = false) {
            Env env = new Env(skipParseErrors);
            env.LoadFrom(filePath);
            return env;
        }
        public static Env From(params string[] filepaths) {
            Env env = new Env();
            env.LoadFrom(filepaths);
            return env;
        }

        public Env(bool skipParseErrors = false) {
            _skipParseErrors = skipParseErrors;
        }

        public Env LoadFrom(params string[] filepaths) {

            if (filepaths.Length == 0)
                throw new ArgumentException("At least one file path must be specified to load");

            var missingOrInvalidPaths = new List<string>();
            foreach (string filepath in filepaths) {
                if (filepath == null) {
                    missingOrInvalidPaths.Add("null");
                } else if (!File.Exists(Path.GetFullPath(filepath))) {
                    missingOrInvalidPaths.Add(filepath);
                }
            }

            if (missingOrInvalidPaths.Count > 0)
                throw new ArgumentException($"One or more file paths could not be found [{(string.Join(", ", missingOrInvalidPaths))}]");

            var parser = new EnvFileParser(_skipParseErrors);
            foreach (string filepath in filepaths) {
                foreach (var kvp in parser.Parse(filepath)) {
                    _envMap.Add(kvp.Key, ReplaceVars(kvp.Value));
                }
            }

            return this;
        }

        public Env Merge() {
            foreach (var kvp in this) {
                // skip if already set in the enironment
                if (Environment.GetEnvironmentVariable(kvp.Key) == null) {
                    Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
                }
            }
            return this;
        }

        public Env MergeWithOverride() {
            foreach (var kvp in this) {
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
            }
            return this;
        }

        private string ReplaceVars(string value) {
            var matches = Regex.Matches(value, @"\$\{(\w+)}");
            var varNames = matches.Select(m=>m.Groups[1].Value).ToList();
            foreach (var name in varNames) {
                if (!value.StartsWith("'")) {
                    var envValue = System.Environment.GetEnvironmentVariable(name) ?? this[name] ?? "";
                    value = value.Replace($"${{{name}}}", envValue);
                }
            }
            return value;
        }

        #region IReadOnlyCollection Implementation

        public string this[string key] { get { return _envMap[key]; } }

        public int Count { get { return _envMap.Count; } }
        public IEnumerable<string> Keys { get { return _envMap.Keys; } }
        public IEnumerable<string> Values { get { return _envMap.Values; } }

        public bool ContainsKey(string key)
            => _envMap.ContainsKey(key);

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            => _envMap.GetEnumerator();

        public bool TryGetValue(string key, out string value)
            => _envMap.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator()
            => _envMap.GetEnumerator();

        #endregion
    }
}
