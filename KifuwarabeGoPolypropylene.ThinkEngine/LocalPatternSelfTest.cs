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