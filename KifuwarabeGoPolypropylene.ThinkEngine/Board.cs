namespace KifuwarabeGoPolypropylene.ThinkEngine;

public sealed class Board(int size)
{
    public int Size { get; } = size;
    private int[] cells = new int[size * size];
    private int[]? previous;
    public int At(int x, int y) => cells[y * Size + x];
    private IEnumerable<int> Neighbors(int p)
    {
        if (p % Size > 0) yield return p - 1;
        if (p % Size < Size - 1) yield return p + 1;
        if (p >= Size) yield return p - Size;
        if (p < cells.Length - Size) yield return p + Size;
    }
    private (HashSet<int> stones, bool liberty) Group(int[] position, int start)
    {
        var stones = new HashSet<int> { start };
        var pending = new Stack<int>();
        pending.Push(start);
        bool liberty = false;
        while (pending.TryPop(out int p))
            foreach (int n in Neighbors(p))
            {
                if (position[n] == 0) liberty = true;
                else if (position[n] == position[start] && stones.Add(n)) pending.Push(n);
            }
        return (stones, liberty);
    }
    public bool Play(int color, int? point)
    {
        if (color is not (1 or 2)) return false;
        if (point is null) { previous = (int[])cells.Clone(); return true; }
        int p = point.Value;
        if (p < 0 || p >= cells.Length || cells[p] != 0) return false;
        var next = (int[])cells.Clone();
        next[p] = color;
        foreach (int n in Neighbors(p))
            if (next[n] == 3 - color)
            {
                var group = Group(next, n);
                if (!group.liberty)
                    foreach (int captured in group.stones) next[captured] = 0;
            }
        if (!Group(next, p).liberty || (previous is not null && next.SequenceEqual(previous))) return false;
        previous = cells;
        cells = next;
        return true;
    }
    // 試作: 最初に見つかった合法手を実際に着手する。
    public int? Generate(int color)
    {
        for (int p = 0; p < cells.Length; p++)
            if (Play(color, p)) return p;
        Play(color, null);
        return null;
    }
}
