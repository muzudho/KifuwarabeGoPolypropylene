namespace KifuwarabeGoPolypropylene.Domain.LocalPatterns.SplitShape;

/// <summary>
///     <pre>
///         ［割かれ形］局所パターン
///
///             - 例（x が自石、o が相手石。D2 に打ってできる形）
///                   +-------------------+
///                 9 | . . . . . . . . . |
///                 8 | . . . . . . . . . |
///                 7 | . . . . . . . . . |
///                 6 | . . . . . . . . . |
///                 5 | . . . . . . . . . |
///                 4 | . . x . . . . . . |
///                 3 | . . . o . . . . . |
///                 2 | . . . x . . . . . |
///                 1 | . . . . . . . . . |
///                   +-------------------+
///                     A B C D E F G H J
///
///             - 着手前の T を原点に、次の二点だけを八方向で確認する。
///                 型1: 相手石(+1, +1)、自石(+2, +1)
///                 型2: 相手石(+1,  0)、自石(+2, +1)
///                 型3: 相手石(+1, +1)、自石(+2, 0)、空点(+1, 0)
///                       一間の間へ次に割り込まれる形（C4/白D5→E4など）。
///                 指定していない点は読まず、他の石の連結状態も条件にしない。
///
///             - 一致だけで悪手としない。捕獲・救出の働き、手番、
///               ケイマへの割込みに対する応手などを着手評価側で確認する。
///               このクラスは分断の強制成立や再連絡不能を証明しない。
///     </pre>
/// </summary>
public static class SplitShapePattern
{
    /// <summary>着手候補と二つの既存石が指定の相対配置になるか調べる。</summary>
    public static bool Matches(ReadOnlySpan<int> cells, int size, int color, int target)
    {
        if (!LocalPatternMatcher.IsTargetValid(cells, size, color, target)) return false;
        for (int symmetry = 0; symmetry < 8; symmetry++)
        {
            if (LocalPatternMatcher.HasStone(cells, size, target, 2, 0, color, symmetry)
                && LocalPatternMatcher.HasStone(cells, size, target, 1, 1, 3 - color, symmetry)
                && LocalPatternMatcher.HasStone(cells, size, target, 1, 0, 0, symmetry)) return true;
            if (!LocalPatternMatcher.HasStone(cells, size, target, 2, 1, color, symmetry)) continue;
            if (LocalPatternMatcher.HasStone(cells, size, target, 1, 1, 3 - color, symmetry)
                || LocalPatternMatcher.HasStone(cells, size, target, 1, 0, 3 - color, symmetry)) return true;
        }
        return false;
    }
}