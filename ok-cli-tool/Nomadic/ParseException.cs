namespace Nomadic
{
    using System;

    public class ParseException : Exception
    {
        public int LineNo { get; private set; } = -1;
        public int CharIndex { get; private set; } = -1;

        public ParseException(int charIndex, int lineNo)
            : this(charIndex, lineNo, "parse attempt failed")
        {}

        public ParseException(string message)
            : base(message)
        {}

        public ParseException(int charIndex, int lineNo, string message)
            : base(message)
        {
            CharIndex = charIndex;
            LineNo = lineNo;
        }

        public ParseException(int charIndex, int lineNo, string message, Exception innerException)
            : base(message, innerException)
        {
            CharIndex = charIndex;
            LineNo = lineNo;
        }
    }
}
