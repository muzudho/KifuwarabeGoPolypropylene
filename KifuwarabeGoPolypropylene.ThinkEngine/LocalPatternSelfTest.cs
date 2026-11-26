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