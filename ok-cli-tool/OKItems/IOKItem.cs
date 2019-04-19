namespace OK.Cli.Tool.OKItems
{
    public interface IOKItem {
        int CommandNumber { get; }
        string CommandText { get; }
        string CommentText { get; }

        bool HasComment { get; }
    }
}
