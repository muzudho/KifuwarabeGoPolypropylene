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
        var eyes = new Board(3);
        foreach (int p in new[] { 1, 2, 3, 5, 6, 7, 8 }) Check(eyes.Play(1, p), "two-eye setup");
        Check(eyes.Lessons.Reports.Count == 0, "lessons initially unlearned");
        Check(eyes.Generate(1) is null && eyes.At(0, 0) == 0 && eyes.At(1, 1) == 0, "avoid filling own eyes");
        Check(eyes.Lessons.Reports.Any(r => r.Id == "fill-eye" && r.Observations > 0), "automatically learn eye loss");
        eyes.Generate(1); // パスで previous が変化するため、この回は別の経験。
        int observed = eyes.Lessons.Reports.Single().Observations;
        eyes.Generate(1);
        Check(eyes.Lessons.Reports.Single().Observations == observed, "reuse learned experience without resimulation");
        Check(eyes.Play(1, 0), "policy does not reject externally supplied legal move");
        var trap = new Board(3);
        Check(trap.Play(2, 1) && trap.Play(2, 4), "capture trap setup");
        trap.Generate(1);
        Check(trap.At(0, 0) == 0 && trap.Lessons.Reports.Any(r => r.Id == "immediate-capture"), "learn from simulated opponent capture");
        var cycle = new Board(5);
        foreach (int p in new[] { 11, 13, 7 }) Check(cycle.Play(1, p), "cycle black setup");
        foreach (int p in new[] { 12, 16, 18, 22 }) Check(cycle.Play(2, p), "cycle white setup");
        for (int p = 0; p < 12; p++)
            if (cycle.At(p % 5, p / 5) == 0) Check(cycle.Play(1, p), "cycle scan prefix setup");
        Check(cycle.Play(1, 17) && cycle.Play(2, null) && cycle.Play(1, null), "cycle capture and passes");
        Check(cycle.Generate(2) != 12 && cycle.Lessons.Reports.Any(r => r.Id == "repeat-position"), "learn and avoid repeated board after ko passes");
        foreach (int size in new[] { 3, 5, 9, 19 })
        {
            var game = new Board(size);
            var seen = new HashSet<string>();
            int passes = 0, moves = 0;
            while (passes < 2 && moves < size * size * 8 + 100)
            {
                int? move = game.Generate(moves % 2 + 1);
                passes = move is null ? passes + 1 : 0;
                if (move is not null)
                {
                    string state = string.Concat(Enumerable.Range(0, size * size).Select(p => game.At(p % size, p / size)));
                    Check(seen.Add(state), "self-play never repeats board");
                }
                moves++;
            }
            Check(passes == 2, $"{size}x{size} self-play ends with two passes");
            Console.WriteLine($"Self-play {size}x{size}: {moves} moves, {game.Lessons.Reports.Sum(r => r.Avoided)} avoided");
        }
        Check(Exchange("kfw-lessons\n").Contains("[]"), "GTP lesson reports");
        var empty = new Board(9);
        Check(empty.Score(7.5) == new AreaScore(0, 0, 81, 7.5), "empty board is neutral");
        Check(empty.Score(7.5).Result == "W+7.5", "white komi");
        var area = new Board(3);
        foreach (int p in new[] { 1, 3, 5, 7 }) Check(area.Play(1, p), "area setup");
        Check(area.Score(0) == new AreaScore(9, 0, 0, 0), "stones plus surrounded empties");
        Check(area.Score(9).Result == "0", "draw");
        var mixed = new Board(3);
        Check(mixed.Play(1, 0) && mixed.Play(2, 8), "neutral setup");
        Check(mixed.Score(0) == new AreaScore(1, 1, 7, 0), "mixed boundary neutral");
        Check(mixed.Play(1, null) && mixed.Play(2, null) && mixed.IsFinished, "two passes finish");
        Check(mixed.Play(1, 1) && !mixed.IsFinished, "dispute can resume play");
        Check(Exchange("boardsize 9\nkomi 7.5\nplay b pass\nplay w pass\nfinal_score\n").EndsWith("= W+7.5\n\n"), "GTP Chinese area and komi");
        Check(Exchange("final_score\n").StartsWith("? game not finished"), "no final score before passes");
        Console.WriteLine($"PASS: {count} checks");
    }
}
