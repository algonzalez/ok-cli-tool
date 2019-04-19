namespace Nomadic
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;

    public class EnvFileParser
    {
        private bool _skipParseErrors;

        public EnvFileParser(bool skipParseErrors = false) {
            _skipParseErrors = skipParseErrors;
        }

        public IEnumerable<KeyValuePair<string, string>> Parse(string filepath) {
            return Parse(filepath, UTF8Encoding.UTF8);
        }

        public IEnumerable<KeyValuePair<string, string>> Parse(string filepath, Encoding encoding) {
            if (String.IsNullOrWhiteSpace(filepath))
                throw new ArgumentException("A filepath must be specified", nameof(filepath));

            string fullpath = Path.GetFullPath(filepath);
            if (!File.Exists(fullpath))
                throw new ArgumentException($"The '{filepath}' file path could not be found");

            string[] lines = File.ReadAllLines(fullpath, encoding);
            return ParseLines(lines);
        }

        public IEnumerable<KeyValuePair<string, string>> ParseLines(string[] lines) {
            int lineNo = 0;
            foreach (string line in lines) {
                lineNo++;
                string trimmedLine = line.Trim();

                if (trimmedLine.Length > 0 && !trimmedLine.StartsWith("#")) {
                    KeyValuePair<string, string>? kvp = ParseLine(lineNo, trimmedLine);
                    if (kvp.HasValue)
                        yield return kvp.Value;
                }
            }
        }

        private KeyValuePair<string, string>? ParseLine(int lineNo, string line) {
            // allow, but ignore, 'export ' line prefix
            // to support files that can be loaded via source command in the shell
            if (line.StartsWith("export ")) {
                line = line.Substring(7).TrimStart();
            }

            LineParts parts = SplitLine(line);

            if (parts.SeparatorIndex < 0) {
                if (_skipParseErrors) return null;
                throw new ParseException(parts.SeparatorIndex, lineNo, "key/value separator (= or :) was not found");
            }

            if (parts.Key.Length == 0) {
                if (_skipParseErrors) return null;
                throw new ParseException(0, lineNo, "key name was not specified");
            }

            if (parts.IsQuoted() && parts.EndQuoteIndex <= parts.StartQuoteIndex) {
                if (_skipParseErrors) return null;
                throw new ParseException(parts.SeparatorIndex + 1, lineNo, $"value for key '{parts.Key}' is missing a closing quote");
            }

            if (parts.IsDoubleQuoted())
                parts.Value = Regex.Unescape(parts.Value);
            return new KeyValuePair<string, string>(parts.Key, parts.Value);
        }

        private LineParts SplitLine(string line) {
            var parts = new LineParts();

            // split key value pairs on '=' or ':'
            parts.SeparatorIndex = line.IndexOfAny(new [] {'=', ':'});
            if (parts.SeparatorIndex >= 0) {
                parts.SeparatorChar = line.Substring(parts.SeparatorIndex, 1)[0];

                parts.Key = line.Substring(0, parts.SeparatorIndex).Trim();
                parts.Value = line.Substring(parts.SeparatorIndex + 1).Trim();

                if (parts.Value.Length > 0) {
                    char startChar = parts.Value[0];
                    if ('"' == startChar || '\'' == startChar) {
                        parts.StartQuoteIndex = 0;
                        parts.QuoteChar = startChar;
                        parts.EndQuoteIndex = parts.Value.IndexOfAny(new [] {'"', '\''}, 1);
                        if (parts.EndQuoteIndex > 0)
                            parts.Value = parts.Value.Substring(1, parts.EndQuoteIndex - 1);
                    } else {
                        // strip trailing comment, if any, from value
                        int commentIndex = parts.Value.IndexOf('#');
                        if (commentIndex >= 0)
                            parts.Value = parts.Value.Substring(0, commentIndex).Trim();
                    }
                }
            }
            return parts;
        }

        private class LineParts {
            public int SeparatorIndex {get; set;} = -1;
            public int StartQuoteIndex {get; set;} = -1;
            public int EndQuoteIndex {get; set;} = -1;

            public char SeparatorChar {get; set;} = default(char);
            public char QuoteChar {get; set;} = default(char);

            public string Key {get; set;}
            public string Value {get; set;}

            public bool IsSingleQuoted() { return '\'' == QuoteChar; }
            public bool IsDoubleQuoted() { return '"' == QuoteChar; }
            public bool IsQuoted() { return IsSingleQuoted() || IsDoubleQuoted(); }
        }
    }
}
