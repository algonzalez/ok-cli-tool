namespace Gonzal.OK.Cli.Tool.OKItems
{
    public abstract class OKItem : IOKItem
    {
        protected OKItem(int commandNumber = 0, string commandText = "", string commentText = "")
        {
            this.CommandNumber = commandNumber;
            this.CommandText = commandText ?? "";
            this.CommentText = commentText ?? "";
        }

        public int CommandNumber { get; }
        public string CommandText { get; }
        public string CommentText { get; }

        public bool HasComment => !string.IsNullOrEmpty(CommentText);

        public override string ToString() {
            var sb = new System.Text.StringBuilder();
            if (CommandNumber > 0)
                sb.Append($"{CommandNumber}. ");
            if (!string.IsNullOrWhiteSpace(CommandText))
                sb.Append($"{CommandText} ");
            if (!string.IsNullOrEmpty(CommentText))
                sb.Append(CommentText);
            return sb.ToString().TrimEnd();
        }
    }
}
