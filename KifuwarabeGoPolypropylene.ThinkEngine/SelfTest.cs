namespace KifuwarabeGoPolypropylene.ThinkEngine;

internal static class SelfTest
{
    public static void Run()
    {
        int count = 0;
        void Check(bool ok, string name)
        {
            if (!ok) throw new InvalidOperationException("FAIL: " + name);
            count++;
        }
        var board = new Board(3);
        Check(board.Play(2, 0), "place white");
        Check(board.Play(1, 1) && board.Play(1, 3) && board.At(0, 0) == 0, "capture");
        Check(!board.Play(2, 0), "suicide rejected");
        Check(!board.Play(2, 1) && board.At(1, 0) == 1, "occupied rejected without mutation");
        Check(board.Play(2, null), "pass");
        var ko = new Board(5);
        foreach (int p in new[] { 11, 13, 7 }) Check(ko.Play(1, p), "ko black setup");
        foreach (int p in new[] { 12, 16, 18, 22 }) Check(ko.Play(2, p), "ko white setup");
        Check(ko.Play(1, 17) && ko.At(2, 2) == 0, "ko capture");
        Check(!ko.Play(2, 12), "immediate ko rejected");
        Check(ko.Play(2, null) && ko.Play(1, null) && ko.Play(2, 12), "ko after passes");
        var full = new Board(2);
        Check(full.Play(1, 0) && full.Play(1, 1) && full.Play(1, 2), "fill board");
        Check(full.Generate(1) is null, "no legal move passes");
        string Exchange(string commands)
        {
            var output = new StringWriter();
            new GtpEngine().Run(new StringReader(commands), output);
            return output.ToString();
        }
        Check(Exchange("# comment\n1 protocol_version\nboardsize 9\nplay b D4\n2 genmove w\nquit\nname\n") == "=1 2\n\n=\n\n=\n\n=2 A1\n\n=\n\n", "GTP framing, ids, comments, generation, quit");
        Check(Exchange("boardsize 1\nplay x A1\nplay b I1\nunknown\n") == "? unacceptable size\n\n? invalid color\n\n? invalid vertex\n\n? unknown command\n\n", "GTP errors");
        Check(Exchange("boardsize 9\nplay b A1\nclear_board\ngenmove w\n").EndsWith("= A1\n\n"), "clear board");
        Check(Exchange("boardsize 19\nplay b T19\nplay w T19\n").EndsWith("? illegal move\n\n"), "19x19 coordinates");
        Console.WriteLine($"PASS: {count} checks");
    }
}
