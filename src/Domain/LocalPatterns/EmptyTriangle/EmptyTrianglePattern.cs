namespace KifuwarabeGoPolypropylene.Domain.LocalPatterns.EmptyTriangle;

/// <summary>
///     <pre>
///         ［空き三角］局所パターン
///
///             - 例（x が自石。C3 に打ってできる形）
///                   +-------------------+
///                 9 | . . . . . . . . . |
///                 8 | . . . . . . . . . |
///                 7 | . . . . . . . . . |
///                 6 | . . . . . . . . . |
///                 5 | . . . . . . . . . |
///                 4 | . . x . . . . . . |
///                 3 | . . x x . . . . . |
///                 2 | . . . . . . . . . |
///                 1 | . . . . . . . . . |
///                   +-------------------+
///                     A B C D E F G H J
///
///             - 着手前の T を原点に、次の二点だけを八方向で確認する。
///                 型1: 自石(+1, 0)、自石(+1, +1)
///                 型2: 自石(+1, 0)、自石( 0, +1)
///                 指定していない点は空点・自石・相手石のどれでもよい。
///                 その点も自石なら団子石の検出と同時に一致し得る。
///
///             - 一致は確認対象という意味。捕獲・救出・必要な連絡など、
///               今回この形を作ってよいかは着手評価側で別に判断する。
///     </pre>
/// </summary>
public static class EmptyTrianglePattern
{
    /// <summary>空点の着手候補が指定の形を作るか、着手前の盤面で調べる。</summary>
    public static bool Matches(ReadOnlySpan<int> cells, int size, int color, int target)
    {
        if (!LocalPatternMatcher.IsTargetValid(cells, size, color, target)) return false;
        for (int symmetry = 0; symmetry < 8; symmetry++)
        {
            if (!LocalPatternMatcher.HasStone(cells, size, target, 1, 0, color, symmetry)) continue;
            if (LocalPatternMatcher.HasStone(cells, size, target, 1, 1, color, symmetry)
                || LocalPatternMatcher.HasStone(cells, size, target, 0, 1, color, symmetry)) return true;
        }
        return false;
    }
}