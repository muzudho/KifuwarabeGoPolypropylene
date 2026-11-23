namespace KifuwarabeGoPolypropylene.ThinkEngine;

public sealed class Board(int size)
{
    public int Size { get; } = size;
    private int[] cells = new int[size * size];
    private int[]? previous;
    private readonly HashSet<string> history = new() { new string('0', size * size) };
    public BattleLessons Lessons { get; init; } = new();
    private readonly Dictionary<string, string> failedCandidates = new();
    private static string Key(int[] position) => string.Concat(position.Select(c => (char)('0' + c)));
    private Board Copy() => new(Size) { cells = (int[])cells.Clone(), previous = previous };
    public int At(int x, int y) => cells[y * Size + x];
    public string PositionKey => Key(cells);
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
        history.Add(Key(cells));
        return true;
    }
    private bool IsEye(int color, int p)
    {
        if (cells[p] != 0 || Neighbors(p).Any(n => cells[n] != color)) return false;
        int x = p % Size, y = p / Size, enemy = 0, outside = 0;
        foreach (int dx in new[] { -1, 1 })
            foreach (int dy in new[] { -1, 1 })
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || nx >= Size || ny < 0 || ny >= Size) outside++;
                else if (At(nx, ny) == 3 - color) enemy++;
            }
        return enemy <= (outside > 0 ? 0 : 1);
    }
    // 合法性と戦訓は分離する。相手の合法手を戦訓で拒否しない。
    public int? Generate(int color)
    {
        if (color is not (1 or 2)) throw new ArgumentException("invalid color");
        for (int p = 0; p < cells.Length; p++)
        {
            string experience = $"{Key(cells)}:{(previous is null ? "" : Key(previous))}:{color}:{p}";
            if (failedCandidates.TryGetValue(experience, out string? learned))
            {
                Lessons.Avoid(learned);
                continue;
            }
            void Observe(string id, string title)
            {
                Lessons.Observe(id, title);
                Lessons.RecordExample(new(id, Size, Key(cells), color, p));
                failedCandidates[experience] = id;
            }
            var trial = Copy();
            if (!trial.Play(color, p)) continue;
            if (history.Contains(Key(trial.cells)))
            {
                Observe("repeat-position", "過去の盤面に戻った");
                continue;
            }
            if (IsEye(color, p))
            {
                Observe("fill-eye", "自分の眼を埋めた");
                continue;
            }
            bool captured = trial.cells.Count(c => c == 3 - color) < cells.Count(c => c == 3 - color);
            // 実際に相手の一手を試し、置いた石が消えることを観測する。
            // 取りを伴う手は、取り返しやコウの可能性があるので今回の対象外。
            var replies = trial.NeighborsOfGroup(p).Take(2).ToArray();
            if (!captured && replies.Length == 1 && replies.Any(reply =>
                {
                    var response = trial.Copy();
                    return response.Play(3 - color, reply) && response.cells[p] == 0;
                }))
            {
                Observe("immediate-capture", "置いた石を次の一手で取られた");
                continue;
            }
            Play(color, p);
            return p;
        }
        Play(color, null);
        return null;
    }
    private IEnumerable<int> NeighborsOfGroup(int p) => Group(cells, p).stones
        .SelectMany(Neighbors).Where(n => cells[n] == 0).Distinct();
}
