namespace KifuwarabeGoPolypropylene.Domain.LocalPatterns.DumplingStones;

/// <summary>
///     <pre>
///         ［団子石］局所パターン（最小の2×2充填）
///
///             - 例（x が自石。D4 に打ってできる形）
///                   +-------------------+
///                 9 | . . . . . . . . . |
///                 8 | . . . . . . . . . |
///                 7 | . . . . . . . . . |
///                 6 | . . . . . . . . . |
///                 5 | . . . . . . . . . |
///                 4 | . . x x . . . . . |
///                 3 | . . x x . . . . . |
///                 2 | . . . . . . . . . |
///                 1 | . . . . . . . . . |
///                   +-------------------+
///                     A B C D E F G H J
///
///             - T を含む最大四つの2×2だけで、残り三点が自石か確認する。
///               空き三角を埋めた結果を、無条件に良形と扱わないための検出。
///               より広い密集形のすべてや、連全体の眼・死活は判定しない。
///               捕獲・救出などで必要な着手かどうかは別に判断する。
///     </pre>
/// </summary>
public static class DumplingStonesPattern
{
    /// <summary>着手候補を埋めると自石の2×2が完成するか調べる。</summary>
    public static bool Matches(ReadOnlySpan<int> cells, int size, int color, int target)
    {
        if (!LocalPatternMatcher.IsTargetValid(cells, size, color, target)) return false;
        for (int dx = -1; dx <= 1; dx += 2)
            for (int dy = -1; dy <= 1; dy += 2)
                if (LocalPatternMatcher.HasStone(cells, size, target, dx, 0, color, 0)
                    && LocalPatternMatcher.HasStone(cells, size, target, 0, dy, color, 0)
                    && LocalPatternMatcher.HasStone(cells, size, target, dx, dy, color, 0)) return true;
        return false;
    }
}