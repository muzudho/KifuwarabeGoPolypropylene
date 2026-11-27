using KifuwarabeGoPolypropylene.Domain.LocalPatterns.EmptyTriangle;
using KifuwarabeGoPolypropylene.Domain.LocalPatterns.DumplingStones;
using KifuwarabeGoPolypropylene.Domain.LocalPatterns.SplitShape;

namespace KifuwarabeGoPolypropylene.ThinkEngine;

internal static class LocalPatternSelfTest
{
    internal static void Run(Action<bool, string> check)
    {
        // 反転・回転、黒白、盤サイズ、指定外点の三状態を実際の配列で照合する。
        foreach (int size in new[] { 9, 19 })
        foreach (int color in new[] { 1, 2 })
        for (int symmetry = 0; symmetry < 8; symmetry++)
        {
            int target = 4 * size + 4;
            int Map(int dx, int dy)
            {
                if (symmetry >= 4) dx = -dx;
                for (int r = 0; r < symmetry % 4; r++) (dx, dy) = (dy, -dx);
                return (4 + dy) * size + 4 + dx;
            }
            foreach (bool diagonal in new[] { false, true })
            foreach (int wildcard in new[] { 0, 1, 2 })
            {
                var cells = new int[size * size];
                cells[Map(1, 0)] = color;
                cells[Map(diagonal ? 1 : 0, 1)] = color;
                cells[Map(diagonal ? 0 : 1, 1)] = wildcard;
                string before = string.Join(',', cells);
                check(EmptyTrianglePattern.Matches(cells, size, color, target) == (wildcard == 0), "local empty triangle requires empty fourth point");
                check(DumplingStonesPattern.Matches(cells, size, color, target) == (wildcard == color), "local dumpling distinguished from triangle");
                check(string.Join(',', cells) == before, "local pattern reads do not mutate board");
            }
            // 一間の両端と相手の支え、まだ空いている割込み点を一緒に調べる。
            foreach (int gap in new[] { 0, 1, 2 })
            {
                var cells = new int[size * size];
                cells[Map(2, 0)] = color;
                cells[Map(1, 1)] = 3 - color;
                cells[Map(1, 0)] = gap;
                check(SplitShapePattern.Matches(cells, size, color, target) == (gap == 0),
                    "local one-space split requires open cut point");
                cells[Map(1, 1)] = 0;
                check(!SplitShapePattern.Matches(cells, size, color, target),
                    "local one-space jump without enemy support is not split");
            }
            foreach (int enemyY in new[] { 0, 1 })
            {
                var cells = new int[size * size];
                cells[Map(2, 1)] = color;
                cells[Map(1, enemyY)] = 3 - color;
                check(SplitShapePattern.Matches(cells, size, color, target), "local split two templates eight symmetries");
                // 味方が他にも存在しても、幾何検出段階で必要性を決めつけない。
                cells[Map(0, 1)] = color;
                check(SplitShapePattern.Matches(cells, size, color, target), "local split wildcard accepts extra support");
            }
        }
        // 一間の割込み後、切った二石を取るまでに相手が他所へ三手打てる提示例。
        foreach (int size in new[] { 9, 19 })
        foreach (int color in new[] { 1, 2 })
        for (int symmetry = 0; symmetry < 8; symmetry++)
        {
            int Map(int x, int y)
            {
                if (symmetry >= 4) x = size - 1 - x;
                for (int r = 0; r < symmetry % 4; r++) (x, y) = (y, size - 1 - x);
                return y * size + x;
            }
            var board = new Board(size);
            (int color, int x, int y)[] sequence =
            [
                (color, 2, 3), (3 - color, 3, 4), // C4、D5
                (color, 4, 3), (3 - color, 3, 3), // E4、D4の割込み
                (color, 3, 2), (3 - color, 2, 2), // D3、C3の切り
                (color, 1, 2), (3 - color, 2, 1), // B3、C2へ伸びる
                (color, 1, 1), (3 - color, 2, 6), // B2、他所のC7
                (color, 3, 1), (3 - color, 6, 6), // D2、他所のG7
                (color, 2, 0), (3 - color, 6, 2)  // C1で二石取り、他所のG3
            ];
            foreach (var move in sequence)
                check(board.Play(move.color, Map(move.x, move.y)), "one-space sacrifice sequence legal");
            check(board.At(Map(2, 1) % size, Map(2, 1) / size) == 0
                && board.At(Map(2, 2) % size, Map(2, 2) / size) == 0,
                "one-space sacrifice removes exactly the two cutting stones");
            check((color == 1 ? board.BlackPrisoners : board.WhitePrisoners) == 2,
                "one-space sacrifice prisoner count");
            foreach (var point in new[] { (2, 6), (6, 6), (6, 2) })
                check(board.At(Map(point.Item1, point.Item2) % size,
                    Map(point.Item1, point.Item2) / size) == 3 - color,
                    "one-space sacrifice permits three plays elsewhere");
        }
        // 敵のつながった二石をまたぐ割かれ形。候補相対で色反転と八対称を調べる。
        foreach (int size in new[] { 9, 19 })
        foreach (int color in new[] { 1, 2 })
        for (int symmetry = 0; symmetry < 8; symmetry++)
        {
            int Map(int x, int y)
            {
                if (symmetry >= 4) x = size - 1 - x;
                for (int r = 0; r < symmetry % 4; r++) (x, y) = (size - 1 - y, x);
                return y * size + x;
            }
            Board Root(int missing = -1)
            {
                var board = new Board(size);
                (int x, int y, int c)[] stones = [(6, 3, color), (5, 4, 3 - color), (6, 4, 3 - color)];
                for (int i = 0; i < stones.Length; i++)
                    if (i != missing) check(board.Play(stones[i].c, Map(stones[i].x, stones[i].y)), "blocked split setup legal");
                return board;
            }
            int target = Map(6, 5);
            var root = Root();
            string before = root.PositionKey;
            check(SplitShapePattern.MatchesBlockedOneSpace(before.Select(c => c - '0').ToArray(), size, color, target), "blocked split detected");
            check(root.ShouldDeferOneSpaceSplit(color, target), "unsupported blocked split deferred");
            check(root.PositionKey == before, "blocked split evaluation does not mutate board");
            for (int missing = 0; missing < 3; missing++)
                check(!SplitShapePattern.MatchesBlockedOneSpace(Root(missing).PositionKey.Select(c => c - '0').ToArray(), size, color, target), "blocked split requires every stone");
            var supported = Root();
            foreach (var q in new[] { (7, 3), (7, 4), (7, 5) })
                check(supported.Play(color, Map(q.Item1, q.Item2)), "blocked split support legal");
            check(!supported.ShouldDeferOneSpaceSplit(color, target), "already connected support is not deferred");
            check(Root().Generate(color) != target, "blocked split generator prefers alternative");
            // 二石取りが成立する支援付きの形は、形一致だけで禁止しない。
            var captureSplit = Root();
            foreach (var q in new[] { (4, 4), (5, 3), (7, 4), (5, 5) })
                check(captureSplit.Play(color, Map(q.Item1, q.Item2)), "blocked split capture setup legal");
            check(!captureSplit.ShouldDeferOneSpaceSplit(color, target), "blocked split capture remains available");
            check(captureSplit.Play(color, target), "blocked split capture legal");
            check((color == 1 ? captureSplit.BlackPrisoners : captureSplit.WhitePrisoners) == 2, "blocked split captures enemy pair");
        }
        // CGOS1706627白56C1。一眼でも五手の攻めで元連が取れるなら優先しない。
        (int color, int? point)[] oneEyeHistory = [(1, 50), (2, 29), (1, 24), (2, 47), (1, 21), (2, 39), (1, 31), (2, 57), (1, 58), (2, 20), (1, 66), (2, 23), (1, 32), (2, 22), (1, 14), (2, 13), (1, 12), (2, 4), (1, 3), (2, 42), (1, 56), (2, 33), (1, 48), (2, 38), (1, 5), (2, 51), (1, 46), (2, 60), (1, 37), (2, 59), (1, 40), (2, 41), (1, 28), (2, 49), (1, 57), (2, 50), (1, 19), (2, 10), (1, 68), (2, 11), (1, 69), (2, 16), (1, 25), (2, 15), (1, 70), (2, 64), (1, 55), (2, 65), (1, 34), (2, 0), (1, 53), (2, 8), (1, 43), (2, 9), (1, 61)];
        foreach (bool flip in new[] { false, true })
        for (int symmetry = 0; symmetry < 8; symmetry++)
        {
            int Map(int p)
            {
                int x = p % 9, y = p / 9;
                if (symmetry >= 4) x = 8 - x;
                for (int r = 0; r < symmetry % 4; r++) (x, y) = (8 - y, x);
                return y * 9 + x;
            }
            var board = new Board(9);
            foreach (var move in oneEyeHistory)
                check(board.Play(flip ? 3 - move.color : move.color,
                    move.point is int p ? Map(p) : null), "three-lib eye actual history legal");
            int color = flip ? 1 : 2, target = Map(2);
            string before = board.PositionKey;
            check(board.FormsSinglePointEye(color, target), "three-lib eye still recognizes formation");
            check(!board.ShouldPrioritizeSinglePointEye(color, target), "three-lib captured eye loses priority");
            check(board.PositionKey == before, "three-lib eye reading preserves root");
            check(board.Generate(color) != target, "three-lib eye chooses another candidate");
        }
        (int color, int? point)[] futileHistory = [(1, 29), (2, 22), (1, 33), (2, 58), (1, 47), (2, 39), (1, 51), (2, 41), (1, 21), (2, 12), (1, 20), (2, 42), (1, 23), (2, 31), (1, 38), (2, 43), (1, 48), (2, 49), (1, 50), (2, 65), (1, 10), (2, 55), (1, 67), (2, 66), (1, 0), (2, 36), (1, 1), (2, 18), (1, 4), (2, 13), (1, 3), (2, 5), (1, 2), (2, 14), (1, 24), (2, 19), (1, 15), (2, 17), (1, 6), (2, 35), (1, 63), (2, 54), (1, 64), (2, 73), (1, 76), (2, 68), (1, 52), (2, 59), (1, 70), (2, 69), (1, 53), (2, 44), (1, 71), (2, 79), (1, 61), (2, 78), (1, 75), (2, 72)];
        (int color, int? point)[] eyePreparationHistory = [(1, 29), (2, 57), (1, 33), (2, 50), (1, 21), (2, 32), (1, 39), (2, 24), (1, 47), (2, 42), (1, 34), (2, 23), (1, 22), (2, 56), (1, 58), (2, 49), (1, 59), (2, 60), (1, 14), (2, 15), (1, 13), (2, 68), (1, 67), (2, 66), (1, 76), (2, 48), (1, 38), (2, 69), (1, 25), (2, 16), (1, 43), (2, 46), (1, 52), (2, 51), (1, 61), (2, 17), (1, 64), (2, 55), (1, 65), (2, 70), (1, 63), (2, 75), (1, 73), (2, 37), (1, 54), (2, 28), (1, 10), (2, 45), (1, 11), (2, 40), (1, 20), (2, 26), (1, 31), (2, 35), (1, 0), (2, 27), (1, 1), (2, 74), (1, 4), (2, 72), (1, 80), (2, 44), (1, 71), (2, 79), (1, 5), (2, 62), (1, 6), (2, 7), (1, 9), (2, 19), (1, 18), (2, 77), (1, 58), (2, 64), (1, 67), (2, 59), (1, null), (2, 76), (1, null), (2, 63), (1, null), (2, 58), (1, null), (2, 3)];
        // 実戦6651の詰まった伸びと、6624の二眼準備を色反転・八対称で区別する。
        foreach (bool flip in new[] { false, true })
        for (int symmetry = 0; symmetry < 8; symmetry++)
        {
            int Map(int p)
            {
                int x = p % 9, y = p / 9;
                if (symmetry >= 4) x = 8 - x;
                for (int r = 0; r < symmetry % 4; r++) (x, y) = (8 - y, x);
                return y * 9 + x;
            }
            Board Replay((int color, int? point)[] moves)
            {
                var board = new Board(9);
                foreach (var move in moves)
                    check(board.Play(flip ? 3 - move.color : move.color,
                        move.point is int p ? Map(p) : null), "futile extension actual history legal");
                return board;
            }
            int color = flip ? 2 : 1;
            var root = Replay(futileHistory);
            string before = root.PositionKey;
            check(root.ShouldDeferFutileExtension(color, Map(80)), "captured J9 extension deferred");
            check(root.ShouldDeferFutileExtension(color, Map(60)), "captured G7 extension deferred");
            check(root.PositionKey == before, "futile extension reading preserves root");
            var chosen = root.Generate(color);
            check(chosen != Map(80) && chosen != Map(60), "futile extension prefers elsewhere");
            var preparation = Replay(eyePreparationHistory);
            check(!preparation.ShouldDeferFutileExtension(color, Map(12)), "two-eye preparation D2 retained");
        }
        (int color, int? point)[] twoLibRescueHistory = [(1, 29), (2, 50), (1, 33), (2, 32), (1, 47), (2, 24), (1, 21), (2, 23), (1, 39), (2, 58), (1, 22), (2, 42), (1, 34), (2, 66), (1, 56), (2, 43), (1, 60), (2, 68), (1, 51), (2, 13), (1, 64), (2, 69), (1, 15), (2, 14), (1, 25), (2, 16), (1, 37), (2, 12), (1, 10), (2, 11), (1, 70), (2, 52), (1, 61), (2, 79), (1, 0), (2, 40), (1, 6), (2, 17), (1, 5), (2, 19), (1, 4), (2, 20), (1, 30), (2, 28), (1, 3), (2, 2), (1, 36), (2, 7), (1, 27), (2, 18), (1, 54), (2, 65), (1, 72), (2, 74), (1, 76), (2, 59), (1, 75), (2, 67), (1, 31), (2, 48), (1, 38), (2, 73), (1, 57), (2, 55), (1, 49), (2, 63)];
        (int color, int? point)[] competingAtariHistory = [(1, 48), (2, 29), (1, 33), (2, 51), (1, 41), (2, 21), (1, 50), (2, 31), (1, 60), (2, 23), (1, 24), (2, 38), (1, 47), (2, 42), (1, 43), (2, 52), (1, 61), (2, 53), (1, 46), (2, 57), (1, 32), (2, 22), (1, 58), (2, 56), (1, 64), (2, 15), (1, 16), (2, 14), (1, 55), (2, 67), (1, 68), (2, 4), (1, 25), (2, 8), (1, 37), (2, 7), (1, 28), (2, 10), (1, 6), (2, 5), (1, 44), (2, 12), (1, 19), (2, 0), (1, 39), (2, 20), (1, 40), (2, 2), (1, 17)];
        // 二呼吸点救出の正例と、別のアタリ救出を先に残す競合例を検証する。
        foreach (bool flip in new[] { false, true })
        for (int symmetry = 0; symmetry < 8; symmetry++)
        {
            int Map(int p)
            {
                int x = p % 9, y = p / 9;
                if (symmetry >= 4) x = 8 - x;
                for (int r = 0; r < symmetry % 4; r++) (x, y) = (8 - y, x);
                return y * 9 + x;
            }
            Board Replay((int color, int? point)[] moves)
            {
                var board = new Board(9);
                foreach (var move in moves)
                    check(board.Play(flip ? 3 - move.color : move.color,
                        move.point is int p ? Map(p) : null), "two-lib rescue actual history legal");
                return board;
            }
            int color = flip ? 2 : 1;
            var board = Replay(twoLibRescueHistory);
            string before = board.PositionKey;
            var rescues = board.TwoLibertyRescuePoints(color);
            check(rescues.Contains(Map(71)), "two-lib J8 rescue detected");
            check(board.PositionKey == before, "two-lib reading preserves root");
            check(board.Generate(color) is int selected && rescues.Contains(selected), "two-lib rescue chosen before one-stone capture");
            var competing = Replay(competingAtariHistory);
            int white = 3 - color;
            check(competing.TwoLibertyRescuePoints(white).Contains(Map(66)), "two-lib D8 rescue detected alongside atari");
            check(competing.Generate(white) == Map(6), "existing atari rescue G1 remains first");
        }
        // 実戦1706083の白70J5。H5の黒石が残るので空き三角ではない。
        int[] realPosition = "010102010101011122012112111012122222122110110021001012021111120021222112201020210"
            .Select(c => c - '0').ToArray();
        check(realPosition[43] == 1 && !EmptyTrianglePattern.Matches(realPosition, 9, 2, 44),
            "real1706083 J5 with black H5 is not empty triangle");
        realPosition[43] = 0;
        check(EmptyTrianglePattern.Matches(realPosition, 9, 2, 44),
            "real1706083 J5 becomes empty triangle when H5 is empty");
        var empty = new int[81];
        check(!EmptyTrianglePattern.Matches(empty, 9, 1, 40), "local empty board has no triangle");
        check(!DumplingStonesPattern.Matches(empty, 9, 1, 40), "local empty board has no dumpling");
        check(!SplitShapePattern.Matches(empty, 9, 1, 40), "local empty board has no split");
        check(!EmptyTrianglePattern.Matches(empty, 9, 0, 40), "local invalid color");
        check(!DumplingStonesPattern.Matches(empty, 8, 1, 40), "local size mismatch");
        check(!SplitShapePattern.Matches(empty, 9, 1, -1), "local off-board target");
        // 配列添字だけで右端から次行へ回り込んだ形を拾わない。
        empty[9] = 1; empty[18] = 1;
        check(!EmptyTrianglePattern.Matches(empty, 9, 1, 8), "local edge does not wrap rows");
        empty[8] = 1;
        check(!EmptyTrianglePattern.Matches(empty, 9, 1, 8), "local occupied target excluded");

        // 捕獲のある提示例も形は認識する。作ってよい理由は検出の後で扱う。
        var capture = new Board(9);
        foreach (int p in new[] { 29, 21, 22, 24, 33, 40, 41, 14 }) capture.Play(1, p);
        foreach (int p in new[] { 31, 32, 23 }) capture.Play(2, p);
        int[] position = Enumerable.Range(0, 81).Select(p => capture.At(p % 9, p / 9)).ToArray();
        check(EmptyTrianglePattern.Matches(position, 9, 1, 30), "local triangle capture exception remains detected");
        check(capture.Play(1, 30) && capture.BlackPrisoners == 3, "local triangle example captures three stones");
    }
}