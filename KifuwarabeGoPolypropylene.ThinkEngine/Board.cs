namespace KifuwarabeGoPolypropylene.ThinkEngine;

public sealed class Board(int size)
{
    public const int MoveLimit = 400;
    public int Size { get; } = size;
    private int[] cells = new int[size * size];
    private int[]? previous;
    private readonly HashSet<string> history = new() { new string('0', size * size) };
    public BattleLessons Lessons { get; init; } = new();
    private readonly Dictionary<string, string> failedCandidates = new();
    private static string Key(int[] position) => string.Concat(position.Select(c => (char)('0' + c)));
    private Board Copy() => new(Size) { cells = (int[])cells.Clone(), previous = previous, MoveCount = MoveCount, ConsecutivePasses = ConsecutivePasses, BlackPrisoners = BlackPrisoners, WhitePrisoners = WhitePrisoners };
    public int At(int x, int y) => cells[y * Size + x];
    public string PositionKey => Key(cells);
    public int ConsecutivePasses { get; private set; }
    public int MoveCount { get; private set; }
    public int BlackPrisoners { get; private set; }
    public int WhitePrisoners { get; private set; }
    public bool ReachedMoveLimit => MoveCount >= MoveLimit;
    public bool IsFinished => ConsecutivePasses >= 2 || ReachedMoveLimit;
    public StoneScore CountStones() => new(cells.Count(c => c == 1), cells.Count(c => c == 2));

    // 中国式の面積計算。盤上に残る石を生き石として扱い、混在境界の空点は中立。
    // 死石の自動判定・合意による除去は未実装。争いがあれば打ち続けて解消する。
    public AreaScore Score(double komi)
    {
        if (!double.IsFinite(komi)) throw new ArgumentException("invalid komi");
        int black = cells.Count(c => c == 1), white = cells.Count(c => c == 2), neutral = 0;
        var visited = new HashSet<int>();
        for (int p = 0; p < cells.Length; p++)
        {
            if (cells[p] != 0 || !visited.Add(p)) continue;
            var pending = new Stack<int>();
            pending.Push(p);
            int count = 0, boundary = 0;
            while (pending.TryPop(out int q))
            {
                count++;
                foreach (int n in Neighbors(q))
                    if (cells[n] == 0) { if (visited.Add(n)) pending.Push(n); }
                    else boundary |= cells[n];
            }
            if (boundary == 1) black += count;
            else if (boundary == 2) white += count;
            else neutral += count;
        }
        return new(black, white, neutral, komi);
    }
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
        if (color is not (1 or 2) || ReachedMoveLimit) return false;
        if (point is null) { previous = (int[])cells.Clone(); ConsecutivePasses++; MoveCount++; return true; }
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
        int capturedCount = cells.Count(c => c == 3 - color) - next.Count(c => c == 3 - color);
        if (color == 1) BlackPrisoners += capturedCount; else WhitePrisoners += capturedCount;
        previous = cells;
        cells = next;
        ConsecutivePasses = 0;
        MoveCount++;
        history.Add(Key(cells));
        return true;
    }
    // 戦術#1: 同じ連で囲む独立領域が二つあり、各空点がその連の呼吸点である保守的な生き判定。
    // false は死石を意味しない。この条件を満たさない広い眼やセキはこの判定の対象外。
    public bool HasTwoEyes(int color, int stone)
    {
        if (color is not (1 or 2)) throw new ArgumentException("invalid color");
        if (stone < 0 || stone >= cells.Length || cells[stone] != color) return false;
        var stones = Group(cells, stone).stones;
        var visited = new HashSet<int>();
        int eyes = 0;
        foreach (int liberty in stones.SelectMany(Neighbors).Where(p => cells[p] == 0).Distinct())
        {
            if (!visited.Add(liberty)) continue;
            var pending = new Stack<int>(); pending.Push(liberty);
            bool enclosed = true, vital = true;
            while (pending.TryPop(out int q))
            {
                var neighbors = Neighbors(q).ToArray();
                if (!neighbors.Any(stones.Contains)) vital = false;
                foreach (int n in neighbors)
                    if (cells[n] == 0) { if (visited.Add(n)) pending.Push(n); }
                    else if (!stones.Contains(n)) enclosed = false;
            }
            // 独立した領域を数える。同じ一領域内の空点を複数の眼と数えない。
            if (enclosed && vital && ++eyes >= 2) return true;
        }
        return false;
    }
    // 戦術#1候補。一手で完成する二眼を調べる。まだ着手選択には適用しない。
    public IReadOnlyList<int> FindTwoEyeCompletingMoves(int color)
    {
        if (color is not (1 or 2)) throw new ArgumentException("invalid color");
        var result = new List<int>();
        if (ReachedMoveLimit) return result;
        for (int p = 0; p < cells.Length; p++)
        {
            if (cells[p] != 0) continue;
            // 既に生きている連を延ばしただけの手は「完成」に数えない。
            if (Neighbors(p).Any(n => cells[n] == color && HasTwoEyes(color, n))) continue;
            var trial = Copy();
            if (trial.Play(color, p) && !history.Contains(trial.PositionKey) && trial.HasTwoEyes(color, p))
                result.Add(p);
        }
        return result;
    }
    // 純碁ホットケーキ: 境界が同色の二眼確認済み連だけから成る空点のピース。
    // 大領域への侵入・死石・セキの完全な証明ではなく、着手方針用の認識。
    public int[] ProtectedTerritoryOwners()
    {
        var owners = new int[cells.Length];
        var visited = new HashSet<int>();
        var alive = new Dictionary<int, bool>();
        bool Alive(int p)
        {
            if (!alive.TryGetValue(p, out bool value))
            {
                value = HasTwoEyes(cells[p], p);
                foreach (int stone in Group(cells, p).stones) alive[stone] = value;
            }
            return value;
        }
        for (int p = 0; p < cells.Length; p++)
        {
            if (cells[p] != 0 || !visited.Add(p)) continue;
            var region = new List<int>();
            var boundary = new HashSet<int>();
            var pending = new Stack<int>(); pending.Push(p);
            while (pending.TryPop(out int q))
            {
                region.Add(q);
                foreach (int n in Neighbors(q))
                    if (cells[n] == 0) { if (visited.Add(n)) pending.Push(n); }
                    else boundary.Add(n);
            }
            // 四辺すべてへ通じる外側は、石の壁で切り出した地としない。
            int edges = 0;
            foreach (int q in region)
            {
                if (q % Size == 0) edges |= 1;
                if (q % Size == Size - 1) edges |= 2;
                if (q < Size) edges |= 4;
                if (q >= cells.Length - Size) edges |= 8;
            }
            if (boundary.Count == 0 || edges == 15) continue;
            int color = cells[boundary.First()];
            if (boundary.All(n => cells[n] == color && Alive(n)))
                foreach (int q in region) owners[q] = color;
        }
        return owners;
    }
    // 地+アゲハマ+コミ。死石除去・セキの合意処理は未実装の暫定集計。
    public TerritoryScore ScoreTerritory(double komi)
    {
        var area = Score(komi);
        var stones = CountStones();
        return new(area.Black - stones.Black, area.White - stones.White,
            BlackPrisoners, WhitePrisoners, komi);
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
        if (ReachedMoveLimit) return null;
        var protectedOwners = ProtectedTerritoryOwners();
        for (int p = 0; p < cells.Length; p++)
        {
            if (protectedOwners[p] == color) continue;
            var trial = Copy();
            if (!trial.Play(color, p)) continue;
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
            int captured = cells.Count(c => c == 3 - color) - trial.cells.Count(c => c == 3 - color);
            // 取りを伴う場合も、合法な取り返しの損失を観測する。
            // 単純コウで禁止される取り返し、400手目後の存在しない応手は含めない。
            int lost = trial.StonesLostToImmediateReply(color, p);
            if (captured == 0 && lost > 0)
            {
                Observe("immediate-capture", "置いた石を次の一手で取られた");
                continue;
            }
            if (captured > 0 && lost > captured)
            {
                Observe("capture-reversal", "石を取った直後に、それより多くの味方を取られた");
                continue;
            }
            Play(color, p);
            return p;
        }
        // 自分の地は候補とフォールバックから除外する。残りの合法手がなければパス。
        Play(color, null);
        return null;
    }
    private IEnumerable<int> NeighborsOfGroup(int p) => Group(cells, p).stones
        .SelectMany(Neighbors).Where(n => cells[n] == 0).Distinct();

    internal int StonesLostToImmediateReply(int color, int p)
    {
        var replies = NeighborsOfGroup(p).Take(2).ToArray();
        if (replies.Length != 1) return 0;
        var response = Copy();
        if (!response.Play(3 - color, replies[0]) || response.cells[p] != 0) return 0;
        return cells.Count(c => c == color) - response.cells.Count(c => c == color);
    }
}
