namespace KifuwarabeGoPolypropylene.Domain.LocalPatterns;

/// <summary>
///     <pre>
///         局所パターンの共通処理。
///         T は着手予定点。盤面全体を走査せず、指定された相対位置だけを読む。
///         色は 1 が黒、2 が白、0 が空点。盤外の点を含む型は一致しない。
///         検出は着手前の幾何だけを扱い、合法性や着手の利益は判定しない。
///     </pre>
/// </summary>
internal static class LocalPatternMatcher
{
    internal static bool IsTargetValid(ReadOnlySpan<int> cells, int size, int color, int target) =>
        size > 0 && (long)size * size == cells.Length && color is 1 or 2
        && (uint)target < (uint)cells.Length && cells[target] == 0;

    // 左右反転の有無と四回転を組み合わせ、八つの対称形を作る。
    internal static (int x, int y) Transform(int x, int y, int symmetry)
    {
        if (symmetry >= 4) x = -x;
        for (int i = 0; i < symmetry % 4; i++) (x, y) = (y, -x);
        return (x, y);
    }

    internal static bool HasStone(ReadOnlySpan<int> cells, int size, int target,
        int dx, int dy, int color, int symmetry)
    {
        (dx, dy) = Transform(dx, dy, symmetry);
        int x = target % size + dx, y = target / size + dy;
        return (uint)x < (uint)size && (uint)y < (uint)size && cells[y * size + x] == color;
    }
}