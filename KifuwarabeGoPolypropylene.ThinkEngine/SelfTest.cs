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
        // 戦術#1: 完成形の認識だけを検証し、着手優先はまだ変更しない。
        foreach (int color in new[] { 1, 2 })
        foreach (int rotation in Enumerable.Range(0, 4))
        foreach (bool mirror in new[] { false, true })
        foreach (int location in Enumerable.Range(0, 3))
        {
            const int size = 9;
            int Point(int x, int y)
            {
                if (mirror) x = size - 1 - x;
                for (int r = 0; r < rotation; r++) (x, y) = (size - 1 - y, x);
                return y * size + x;
            }
            // 隅6石、辺8石、中央11石。最小性の証明ではない。
            (int x, int y)[] shape = location switch
            {
                0 => new[] { (1, 0), (3, 0), (0, 1), (1, 1), (2, 1), (3, 1) },
                1 => new[] { (2, 0), (4, 0), (6, 0), (2, 1), (3, 1), (4, 1), (5, 1), (6, 1) },
                _ => new[] { (3, 2), (4, 2), (5, 2), (2, 3), (4, 3), (6, 3), (2, 4), (3, 4), (4, 4), (5, 4), (6, 4) }
            };
            var alive = new Board(size);
            foreach (var (x, y) in shape) Check(alive.Play(color, Point(x, y)), "tactic#1 shape setup");
            int first = Point(shape[0].x, shape[0].y);
            Check(alive.HasTwoEyes(color, first), "tactic#1 same-chain two eyes across transformations");
            (int x, int y)[] eyePoints = location switch
            {
                0 => new[] { (0, 0), (2, 0) },
                1 => new[] { (3, 0), (5, 0) },
                _ => new[] { (3, 3), (5, 3) }
            };
            foreach (var (x, y) in eyePoints)
                Check(!alive.Play(3 - color, Point(x, y)), "tactic#1 opponent cannot enter either eye");
            Check(alive.Play(color, Point(eyePoints[0].x, eyePoints[0].y)), "tactic#1 legal own-eye fill");
            Check(!alive.HasTwoEyes(color, first), "tactic#1 one remaining eye is not two");
            var broken = new Board(size);
            foreach (var (x, y) in shape.Skip(1)) Check(broken.Play(color, Point(x, y)), "tactic#1 broken boundary setup");
            Check(!broken.HasTwoEyes(color, Point(shape[^1].x, shape[^1].y)), "tactic#1 open boundary rejected");
            string beforeCandidates = broken.PositionKey;
            int beforeCount = broken.MoveCount;
            var completing = broken.FindTwoEyeCompletingMoves(color);
            Check(completing.Contains(Point(shape[0].x, shape[0].y)), "tactic#1 find missing boundary move");
            Check(broken.PositionKey == beforeCandidates && broken.MoveCount == beforeCount && broken.Lessons.Reports.Count == 0,
                "tactic#1 probing leaves board and reports unchanged");
            foreach (int candidate in completing)
            {
                var proof = new Board(size);
                foreach (var (x, y) in shape.Skip(1)) proof.Play(color, Point(x, y));
                Check(proof.Play(color, candidate) && proof.HasTwoEyes(color, candidate), "tactic#1 every proposed move completes same-chain eyes");
            }
        }
        var twoPieces = new Board(9);
        foreach (int p in new[] { 2, 5, 9, 10, 11, 12, 13, 14 }) Check(twoPieces.Play(1, p), "two two-point pieces setup");
        Check(twoPieces.HasTwoEyes(1, 2), "two independent two-point regions are eyes without filling");
        Check(twoPieces.ProtectedTerritoryOwners()[0] == 1 && twoPieces.ProtectedTerritoryOwners()[3] == 1,
            "preserve multi-point pieces as land");
        Check(twoPieces.Play(2, 0) && !twoPieces.HasTwoEyes(1, 2), "invaded piece is unsettled, not automatically alive or dead");
        var onePiece = new Board(9);
        foreach (int p in new[] { 5, 9, 10, 11, 12, 13, 14 }) onePiece.Play(1, p);
        Check(!onePiece.HasTwoEyes(1, 5), "one connected multi-point region is not two eyes");
        var cornerTerritory = new Board(9);
        foreach (int point in new[] { 1, 3, 9, 10, 11, 12 }) cornerTerritory.Play(1, point);
        var territoryOwners = cornerTerritory.ProtectedTerritoryOwners();
        Check(territoryOwners[0] == 1 && territoryOwners[2] == 1, "pancake protects corner eye pieces");
        Check(territoryOwners[80] == 0, "pancake does not claim four-edge outside as protected land");
        Check(cornerTerritory.Play(2, 80), "opponent can invade outside of alive corner");
        var separateEyes = new Board(9);
        foreach (int p in new[] { 1, 9, 10, 7, 17, 16 }) Check(separateEyes.Play(1, p), "tactic#1 separate groups setup");
        Check(!separateEyes.HasTwoEyes(1, 1) && !separateEyes.HasTwoEyes(1, 7), "tactic#1 never sum eyes of separate chains");
        Check(!separateEyes.HasTwoEyes(1, -1) && !separateEyes.HasTwoEyes(1, 81) && !separateEyes.HasTwoEyes(2, 1), "tactic#1 invalid or wrong stone rejected");
        Check(new Board(9).FindTwoEyeCompletingMoves(1).Count == 0, "tactic#1 no one-move two eyes on empty board");
        var finishedTactic = new Board(3);
        for (int ply = 0; ply < Board.MoveLimit; ply++) finishedTactic.Play(ply % 2 + 1, null);
        Check(finishedTactic.FindTwoEyeCompletingMoves(1).Count == 0, "tactic#1 no candidate after move limit");
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
        Check(ko.StonesLostToImmediateReply(1, 17) == 0, "illegal ko recapture not treated as losing capture");
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
        Check(eyes.Generate(1) is null && eyes.CountStones().Black == 7, "pancake passes instead of filling own two-eye territory");
        Check(eyes.ProtectedTerritoryOwners()[0] == 1 && eyes.ProtectedTerritoryOwners()[4] == 1, "pancake protects both own eyes");
        Check(eyes.Generate(1) is null, "pass only when no legal move remains");
        var externalEye = new Board(3);
        foreach (int p in new[] { 1, 2, 3, 5, 6, 7, 8 }) externalEye.Play(1, p);
        Check(externalEye.Play(1, 0), "policy does not reject externally supplied legal move");
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
            int repeats = 0;
            while (!game.IsFinished)
            {
                int? move = game.Generate(moves % 2 + 1);
                passes = move is null ? passes + 1 : 0;
                if (move is not null)
                {
                    string state = string.Concat(Enumerable.Range(0, size * size).Select(p => game.At(p % size, p / size)));
                    if (!seen.Add(state)) repeats++;
                }
                moves++;
            }
            Check(passes == 2 || moves == Board.MoveLimit, $"{size}x{size} self-play ends within 400 plies");
            Check(game.MoveCount == moves, "generated moves and passes counted exactly once");
            Console.WriteLine($"Self-play {size}x{size}: {moves} moves, {repeats} repeats, {game.CountStones().Result}");
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
        Check(Exchange("boardsize 9\nkomi 7.5\nplay b pass\nplay w pass\nfinal_score\n").EndsWith("= W+7.5\n\n"), "territory rule counts komi");
        Check(Exchange("final_score\n").StartsWith("? game not finished"), "no final score before passes");
        Check(area.CountStones() == new StoneScore(4, 0), "count stones instead of territory");
        Check(board.CountStones() == new StoneScore(2, 0), "captured stones not counted");
        Check(new StoneScore(2, 3).Result == "W+1" && new StoneScore(3, 2).Result == "B+1" && new StoneScore(2, 2).Result == "0", "stone wins and draw");
        Check(board.BlackPrisoners == 1 && board.WhitePrisoners == 0, "capture increments prisoners");
        Check(area.ScoreTerritory(0).BlackTerritory == 5, "territory excludes stones");
        Check(new TerritoryScore(4, 3, 2, 1, 0.5).Result == "B+1.5", "territory prisoners and komi");
        Check(new Board(9).ProtectedTerritoryOwners().All(c => c == 0), "empty board not territory");
        Check(eyes.Play(1, 0), "policy permits external legal play");
        Check(Exchange("boardsize 9\nkomi 2.5\nclear_board\nplay b pass\nplay w pass\nfinal_score\n").EndsWith("= W+2.5\n\n"), "clear preserves komi");
        var mixedRegion = new Board(5);
        mixedRegion.Play(1, 0); mixedRegion.Play(2, 24);
        Check(mixedRegion.ProtectedTerritoryOwners().All(c => c == 0), "mixed boundary not own territory");
        var resetCapture = new Board(3);
        resetCapture.Play(2, 0); resetCapture.Play(1, 1); resetCapture.Play(1, 3);
        int capturedBeforeIllegal = resetCapture.BlackPrisoners;
        Check(!resetCapture.Play(2, 0) && resetCapture.BlackPrisoners == capturedBeforeIllegal, "suicide leaves prisoners unchanged");
        Check(new Board(3).BlackPrisoners == 0 && new Board(3).WhitePrisoners == 0, "new board resets prisoners");
        var limit = new Board(9);
        for (int ply = 0; ply < 398; ply++) Check(limit.Play(ply % 2 + 1, null), "count pass");
        Check(!limit.Play(1, -1) && limit.MoveCount == 398, "illegal move not counted");
        Check(limit.Play(1, 0) && limit.MoveCount == 399 && !limit.ReachedMoveLimit, "399th move accepted");
        Check(limit.Generate(2) is not null && limit.MoveCount == 400 && limit.IsFinished, "400th generated move finishes game");
        string finalPosition = limit.PositionKey;
        Check(limit.Generate(1) is null && !limit.Play(1, 2) && !limit.Play(1, null) && limit.MoveCount == 400 && limit.PositionKey == finalPosition, "400-ply board frozen");
        string atLimit = string.Concat(Enumerable.Repeat("play b pass\n", 399)) + "play b A1\nfinal_score\nplay w B1\ngenmove w\n";
        Check(Exchange(atLimit).EndsWith("= B+360\n\n? game finished at 400 moves\n\n= pass\n\n"), "GTP provisional territory at 400th position");
        Check(Exchange(atLimit + "clear_board\nplay b A1\n").EndsWith("=\n\n=\n\n"), "clear resets move limit");
        // SEED2026112321 / game0 の290手目直後。H9で1石取り、H8で11石失う。
        const string reversalMoves = "0,1,2,3,4,5,6,7,9,8,10,11,1,12,13,14,15,16,18,17,19,20,21,22,23,4,24,13,25,27,26,7,16,28,0,1,8,9,19,10,29,18,30,31,32,33,34,36,35,37,38,39,40,41,42,43,33,44,45,46,47,48,49,50,51,52,54,53,55,56,29,21,38,30,57,47,58,29,59,60,41,61,63,62,64,65,66,67,68,69,70,71,73,74,75,72,54,45,55,63,76,64,77,54,78,79,7,0,17,2,50,67,6,7,15,8,16,23,17,24,7,25,26,32,33,34,40,35,41,8,6,7,16,15,42,6,49,17,50,51,57,58,68,59,40,33,41,42,75,49,76,50,77,40,66,78,66,57,76,68,75,77,75,66,-,76,-,16,-,19,-,26,-,38,-,41,-,55,-,70,-,73,-,75,80,0,1,2,3,4,5,6,7,9,8,10,11,1,12,13,14,15,16,18,17,19,20,21,22,23,4,24,13,25,27,26,7,16,28,0,1,8,9,19,10,29,18,30,31,32,33,34,36,35,37,38,39,40,41,42,43,33,44,45,46,47,48,49,50,51,52,54,53,55,56,29,21,38,30,57,47,58,29,59,60,41,61,63,62,64,65,66,67,68,69,70,71,73,74,75,72,54,45,55,63,76,64,77,54,78";
        Board ReplayReversal()
        {
            var replay = new Board(9);
            int ply = 0;
            foreach (string vertex in reversalMoves.Split(','))
                Check(replay.Play(ply++ % 2 + 1, vertex == "-" ? null : int.Parse(vertex)), "capture reversal replay");
            return replay;
        }
        var original = ReplayReversal();
        int originalWhite = original.CountStones().White;
        Check(original.Play(1, 79) && original.CountStones().White == originalWhite - 1, "reproduced one-stone capture");
        Check(original.StonesLostToImmediateReply(1, 79) == 11, "predict exact recapture loss");
        int beforeRecapture = original.CountStones().Black;
        Check(original.Play(2, 70) && original.CountStones().Black == beforeRecapture - 11, "reproduced eleven-stone loss");
        var avoided = ReplayReversal();
        Check(avoided.Generate(1) != 79, "pancake policy avoids reproduced losing capture");
        var lastPlyCapture = ReplayReversal();
        while (lastPlyCapture.MoveCount < 399) lastPlyCapture.Play(1, null);
        Check(lastPlyCapture.Play(1, 79) && lastPlyCapture.StonesLostToImmediateReply(1, 79) == 0, "400th capture has no 401st reply");
        Check(board.StonesLostToImmediateReply(1, 3) == 0, "safe capture not treated as losing capture");
        Console.WriteLine($"PASS: {count} checks");
    }
}
