namespace Gonzal.OK.Cli.Tool.OKItems
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    class OKItemList : List<IOKItem>
    {
        public static readonly string[] InlineCommentSeparators = new [] { "#" };

        public static OKItemList FromFile(string fileName)
        {
            int commandNumber = 0;
            var okItemList = new OKItemList();

            using (StreamReader reader = File.OpenText(fileName))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine().Trim();

                    IOKItem okItem;
                    if (line.Length == 0) {
                        okItem = new OKEmptyItem();
                    } else if (line.StartsWith("#")) {
                        okItem = new OKCommentItem(line);
                    } else {
                        commandNumber++;
                        var results = line.Split(InlineCommentSeparators, StringSplitOptions.None);
                        okItem = (results.Length == 1)
                            ? new OKCommandItem(commandNumber, line)
                            : new OKCommandItem(commandNumber, results[0].Trim(), line.Substring(results[0].Length).Trim());
                    }

                    okItemList.Add(okItem);
                }
            }

            return okItemList;
        }

        public void WriteList(Config config) {
            int groupIndex = 0;
            int groupWidth = 0;
            var groupWidths = new Dictionary<int, int>();

            foreach (var okItem in this) {
                if (okItem is OKCommentItem
                    || (okItem is OKEmptyItem && config.CommentAlignment == CommentAlignment.ByGroup))
                {
                    groupWidths[groupIndex] = groupWidth;
                    groupIndex++;
                    groupWidth = 0;
                } else if (okItem is OKCommandItem) {
                    groupWidth = Math.Max(groupWidth, okItem.CommandText.Length);
                }
            }
            if (groupWidth > 0)
                groupWidths[groupIndex] = groupWidth;

            int maxWidth = groupWidths.Values.DefaultIfEmpty(0).Max();
            groupIndex = 0;
            foreach (var okItem in this)
            {
                if (okItem is OKCommandItem)
                {
                    Console.ForegroundColor = config.Colors.Number;
                    Console.Write($"{okItem.CommandNumber}. ");
                    Console.ForegroundColor = config.Colors.Command;

                    switch (config.CommentAlignment) {
                        case CommentAlignment.None:
                            Console.Write(okItem.CommandText);
                            break;
                        case CommentAlignment.ByGroup:
                        case CommentAlignment.ByGroupIgnoringBlankLines:
                            Console.Write(okItem.CommandText.PadRight(groupWidths[groupIndex]));
                            break;
                        case CommentAlignment.All:
                            Console.Write(okItem.CommandText.PadRight(maxWidth));
                            break;
                        default:
                            Console.Write(okItem.CommandText.PadRight(groupWidths[groupIndex]));
                            break;
                    }
                    if (!string.IsNullOrWhiteSpace(okItem.CommentText))
                    {
                        Console.ForegroundColor = config.Colors.Comment;
                        Console.Write(" " + okItem.CommentText);
                    }
                }
                else
                {
                    if (okItem is OKCommentItem
                        || (okItem is OKEmptyItem && config.CommentAlignment == CommentAlignment.ByGroup))
                    {
                        groupIndex++;
                    }
                    Console.ForegroundColor = config.Colors.Heading;
                    Console.Write(okItem.CommentText);
                }
                Console.ForegroundColor = config.Colors.Original;
                Console.WriteLine();
            }
        }

        public int MaxCommandNumber
            => this
                .Where(x=>x is OKCommandItem)
                .Select(x => x.CommandNumber).Max();

        public OKCommandItem FindCommandByNumber(int commandNumber)
            => this
                .Where(x=>x is OKCommandItem)
                .Where(x => x.CommandNumber == commandNumber)
                .Cast<OKCommandItem>()
                .FirstOrDefault();
    }
}
