namespace KifuwarabeGoPolypropylene.ThinkEngine;

public sealed class Board(int size)
{
    public const int MoveLimit = 400;
    public int Size { get; } = size;
    private int[] cells = new int[size * size];
    private int[]? previous;
    private readonly HashSet<string> history = new() { new string('0', size * size) };
    public BattleLessons Lessons { get; init; } = new();
    private readonly List<TacticApplication> tacticApplications = new();
    public IReadOnlyList<TacticApplication> TacticApplications => tacticApplications.AsReadOnly();
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
    // 観測専用。同じ連の二眼、または既に二眼の味方だけが支える独立一目眼二つ。
    // この認識を未確認数・地保護・着手選択へはまだ組み込まない。
    public bool HasTwoEyesWithSupport(int color, int stone)
    {
        if (color is not (1 or 2)) throw new ArgumentException("invalid color");
        if (stone < 0 || stone >= cells.Length || cells[stone] != color) return false;
        if (HasTwoEyes(color, stone)) return true;
        var group = Group(cells, stone).stones;
        var anchors = new Dictionary<int, bool>();
        bool SupportedBy(int p)
        {
            if (group.Contains(p)) return true;
            if (!anchors.TryGetValue(p, out bool alive))
            {
                // 拡張した認識を再帰に使わず、元の二眼だけを支えの証拠にする。
                alive = HasTwoEyes(color, p);
                foreach (int n in Group(cells, p).stones) anchors[n] = alive;
            }
            return alive;
        }
        return group.SelectMany(Neighbors).Where(p => cells[p] == 0).Distinct()
            .Count(p => Neighbors(p).All(n => cells[n] == color && SupportedBy(n))) >= 2;
    }

    // 旧基準では未確認だが支援二眼で認識できる石だけを別指標として数える。
    public int SupportedOnlyStoneCount(int color)
    {
        if (color is not (1 or 2)) throw new ArgumentException("invalid color");
        var seen = new HashSet<int>();
        int count = 0;
        for (int p = 0; p < cells.Length; p++)
        {
            if (cells[p] != color || seen.Contains(p)) continue;
            var group = Group(cells, p).stones; seen.UnionWith(group);
            if (!HasTwoEyes(color, p) && HasTwoEyesWithSupport(color, p)) count += group.Count;
        }
        return count;
    }

    // 支援二眼でも未確認の石のうち共有一点眼で追加確認できる数。既存指標と重複しない。
    public int SharedAdditionalStoneCount(int color)
    {
        var recognized = FindSharedPointEyeStones(color);
        var seen = new HashSet<int>();
        int count = 0;
        foreach (int p in recognized)
        {
            if (seen.Contains(p)) continue;
            var group = Group(cells, p).stones; seen.UnionWith(group);
            if (!HasTwoEyesWithSupport(color, p)) count += group.Count;
        }
        return count;
    }

    // 未確認は死石を意味しない。終局研究で早いパスを見つけるための観測値。
    public int UnconfirmedStoneCount(int color)
    {
        if (color is not (1 or 2)) throw new ArgumentException("invalid color");
        var seen = new HashSet<int>();
        int count = 0;
        for (int p = 0; p < cells.Length; p++)
        {
            if (cells[p] != color || seen.Contains(p)) continue;
            var stones = Group(cells, p).stones;
            seen.UnionWith(stones);
            if (!HasTwoEyes(color, p)) count += stones.Count;
        }
        return count;
    }

    // 戦術#1の二眼候補。戦術#23では9路の着手選択にも使用する。
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
    // 戦術#9: 候補抽出のみ。二呼吸点の小連を三呼吸点以上へ伸ばす。
    // 三呼吸点は生きの証明ではなく、着手優先・地保護には使わない。
    public IReadOnlyList<int> FindPreAtariEscapeMoves(int color)
    {
        if (color is not (1 or 2)) throw new ArgumentException("invalid color");
        // 着手直後の相手応手まで400手内で検査できる局面に限る。
        if (IsFinished || MoveCount >= MoveLimit - 1) return Array.Empty<int>();
        var result = new HashSet<int>(); var seen = new HashSet<int>();
        var owners = ProtectedTerritoryOwners();
        for (int p = 0; p < cells.Length; p++)
        {
            if (cells[p] != color || seen.Contains(p)) continue;
            var group = Group(cells, p).stones; seen.UnionWith(group);
            if (group.Count > 3 || HasTwoEyesWithSupport(color, p)) continue;
            var liberties = NeighborsOfGroup(p).Take(3).ToArray();
            if (liberties.Length != 2) continue;
            foreach (int candidate in liberties)
            {
                if (owners[candidate] == color || IsEye(color, candidate)) continue;
                var trial = Copy();
                if (!trial.Play(color, candidate) || history.Contains(trial.PositionKey)
                    || trial.NeighborsOfGroup(p).Take(3).Count() < 3
                    || trial.StonesLostToImmediateReply(color, candidate) > 0) continue;
                result.Add(candidate);
            }
        }
        return result.Order().ToArray();
    }
    // 戦術#9の限定候補。既に基礎二眼の味方へ連絡して確認済みとなる伸びだけ。
    // 全盤の利益・他連の安全は保証せず、Generateの優先には使わない。
    public IReadOnlyList<int> FindPreAtariLivingConnectionMoves(int color)
    {
        var result = new List<int>();
        foreach (int candidate in FindPreAtariEscapeMoves(color))
        {
            if (!Neighbors(candidate).Any(n => cells[n] == color && HasTwoEyes(color, n))) continue;
            var trial = Copy();
            if (trial.Play(color, candidate) && trial.HasTwoEyesWithSupport(color, candidate)) result.Add(candidate);
        }
        return result;
    }
    // 戦術#11の観測のみ。共有する独立一点眼を二つ以上保つ連の集合。
    // 非認識は死石を意味しない。大きな眼・セキ・正式終局の判定は対象外。
    public IReadOnlyList<int> FindSharedPointEyeStones(int color)
    {
        if (color is not (1 or 2)) throw new ArgumentException("invalid color");
        var ids = Enumerable.Repeat(-1, cells.Length).ToArray();
        var groups = new List<HashSet<int>>();
        for (int p = 0; p < cells.Length; p++)
        {
            if (cells[p] != color || ids[p] >= 0) continue;
            var group = Group(cells, p).stones;
            foreach (int stone in group) ids[stone] = groups.Count;
            groups.Add(group);
        }
        var eyes = Enumerable.Range(0, cells.Length)
            .Where(p => cells[p] == 0 && Neighbors(p).All(n => cells[n] == color))
            .Select(p => Neighbors(p).Select(n => ids[n]).Distinct().ToArray()).ToArray();
        var remaining = Enumerable.Range(0, groups.Count).ToHashSet();
        while (true)
        {
            // 除外された境界連に依存する眼も外す。単なる循環支援では認定しない。
            var validEyes = eyes.Where(boundary => boundary.All(remaining.Contains)).ToArray();
            var remove = remaining.Where(g => validEyes.Count(boundary => boundary.Contains(g)) < 2).ToArray();
            if (remove.Length == 0) break;
            remaining.ExceptWith(remove);
        }
        return remaining.SelectMany(g => groups[g]).Order().ToArray();
    }

    // 戦術#10の観測読み。別手→相手の任意の合法一手→同じ点での捕獲を検査。
    // trueはこの3手の保持だけ。falseは未確認を含み、死活・最善順位を表さない。
    public bool CanDeferSmallCapture(int color, int capturePoint, int alternativePoint)
    {
        if (color is not (1 or 2) || capturePoint < 0 || capturePoint >= cells.Length
            || alternativePoint < 0 || alternativePoint >= cells.Length
            || IsFinished || MoveCount > MoveLimit - 3 || capturePoint == alternativePoint) return false;
        var immediate = Copy();
        if (!immediate.Play(color, capturePoint) || history.Contains(immediate.PositionKey)) return false;
        var target = Enumerable.Range(0, cells.Length)
            .Where(p => cells[p] == 3 - color && immediate.cells[p] != 3 - color).ToArray();
        if (target.Length is < 1 or > 3 || ProtectedTerritoryOwners()[alternativePoint] == color
            || IsEye(color, alternativePoint)) return false;
        var after = Copy();
        if (!after.Play(color, alternativePoint) || history.Contains(after.PositionKey)
            || after.StonesLostToImmediateReply(color, alternativePoint) > 0) return false;
        string afterPosition = after.PositionKey;
        foreach (int? reply in Enumerable.Range(0, cells.Length).Select(p => (int?)p).Append(null))
        {
            var trial = after.Copy();
            // 相手の合法性はPlayの自殺手・単純コウで検査。自分の既出回避を相手へ課さない。
            if (!trial.Play(3 - color, reply)) continue;
            string replyPosition = trial.PositionKey;
            if (!trial.Play(color, capturePoint) || target.Any(p => trial.cells[p] == 3 - color)) return false;
            string capturedPosition = trial.PositionKey;
            if (history.Contains(capturedPosition) || capturedPosition == afterPosition || capturedPosition == replyPosition) return false;
        }
        return true;
    }
    // 戦術#6候補抽出のみ。囲まれた未確認侵入の唯一の一手二眼完成を妨げる。
    public IReadOnlyList<int> FindTwoEyeDenialMoves(int color)
    {
        if (color is not (1 or 2)) throw new ArgumentException("invalid color");
        if (IsFinished) return Array.Empty<int>();
        var threats = FindTwoEyeCompletingMoves(3 - color);
        if (threats.Count != 1) return Array.Empty<int>();
        int point = threats[0];
        if (ProtectedTerritoryOwners()[point] == color || IsEye(color, point)) return Array.Empty<int>();
        var region = new HashSet<int> { point };
        var wall = new HashSet<int>();
        var pending = new Stack<int>(); pending.Push(point);
        while (pending.TryPop(out int p))
            foreach (int n in Neighbors(p))
                if (cells[n] == color) wall.Add(n);
                else if (region.Add(n)) pending.Push(n);
        // 外側や既に生きた敵を攻める候補は、この限定形の対象外。
        if (region.Any(p => p % Size == 0 || p % Size == Size - 1 || p < Size || p >= cells.Length - Size)
            || wall.Count == 0 || wall.Any(p => !HasTwoEyes(color, p))
            || !region.Any(p => cells[p] == 3 - color)
            || region.Any(p => cells[p] == 3 - color && HasTwoEyes(3 - color, p)))
            return Array.Empty<int>();
        var trial = Copy();
        if (!trial.Play(color, point) || history.Contains(trial.PositionKey)
            || trial.StonesLostToImmediateReply(color, point) > 0
            || trial.FindTwoEyeCompletingMoves(3 - color).Count != 0)
            return Array.Empty<int>();
        return new[] { point };
    }

    // 戦術#7候補抽出のみ。小侵入をアタリにし、同じ領域の空点を残す。
    public IReadOnlyList<int> FindSmallInvasionAtariMoves(int color)
    {
        if (color is not (1 or 2)) throw new ArgumentException("invalid color");
        var result = new Dictionary<int, int>();
        if (IsFinished) return Array.Empty<int>();
        var protectedOwners = ProtectedTerritoryOwners();
        var seen = new HashSet<int>();
        for (int p = 0; p < cells.Length; p++)
        {
            if (cells[p] != 3 - color || seen.Contains(p)) continue;
            var group = Group(cells, p).stones; seen.UnionWith(group);
            if (group.Count > 3 || HasTwoEyes(3 - color, p)) continue;
            var liberties = NeighborsOfGroup(p).Take(3).ToArray();
            if (liberties.Length != 2) continue;
            var region = new HashSet<int> { p };
            var wall = new HashSet<int>();
            var pending = new Stack<int>(); pending.Push(p);
            while (region.Count <= 9 && pending.TryPop(out int q))
                foreach (int n in Neighbors(q))
                    if (cells[n] == color) wall.Add(n);
                    else if (region.Add(n)) pending.Push(n);
            if (region.Count > 9 || wall.Count == 0
                || wall.Any(n => !HasTwoEyes(color, n))
                || region.Any(n => n % Size == 0 || n % Size == Size - 1 || n < Size || n >= cells.Length - Size))
                continue;
            int before = region.Count(n => protectedOwners[n] == color);
            foreach (int candidate in liberties)
            {
                if (protectedOwners[candidate] == color || IsEye(color, candidate)) continue;
                var trial = Copy();
                if (!trial.Play(color, candidate) || history.Contains(trial.PositionKey)
                    || trial.cells[p] != 3 - color || trial.NeighborsOfGroup(p).Take(2).Count() != 1
                    || !trial.HasTwoEyes(color, candidate)
                    || trial.StonesLostToImmediateReply(color, candidate) > 0) continue;
                var after = trial.ProtectedTerritoryOwners();
                int gain = region.Count(n => after[n] == color) - before;
                if (gain > 0) result[candidate] = Math.Max(result.GetValueOrDefault(candidate), gain);
            }
        }
        return result.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key).Select(pair => pair.Key).ToArray();
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
    // 戦術#3の限定形: 生きた味方の壁の内側にある小さなアタリの侵入石。
    private HashSet<int> SmallInvasionCapturePoints(int color)
    {
        var result = new HashSet<int>();
        var seen = new HashSet<int>();
        for (int p = 0; p < cells.Length; p++)
        {
            if (cells[p] != 3 - color || seen.Contains(p)) continue;
            var group = Group(cells, p).stones; seen.UnionWith(group);
            if (group.Count > 3) continue;
            var boundary = group.SelectMany(Neighbors).Where(n => !group.Contains(n)).Distinct().ToArray();
            var liberties = boundary.Where(n => cells[n] == 0).ToArray();
            if (liberties.Length != 1) continue;
            var wall = boundary.Where(n => cells[n] == color).ToArray();
            if (wall.Length > 0 && wall.All(n => HasTwoEyes(color, n))) result.Add(liberties[0]);
        }
        return result;
    }

    // 戦術#5: 今すぐ取られる複数石を、安全な二呼吸点以上へ救出する。
    private Dictionary<int, int> AtariRescuePoints(int color)
    {
        var result = new Dictionary<int, int>(); var seen = new HashSet<int>();
        for (int p = 0; p < cells.Length; p++)
        {
            if (cells[p] != color || seen.Contains(p)) continue;
            var group = Group(cells, p).stones; seen.UnionWith(group);
            if (group.Count < 2) continue;
            var liberties = group.SelectMany(Neighbors).Where(n => cells[n] == 0).Distinct().Take(2).ToArray();
            if (liberties.Length != 1) continue;
            var threat = Copy();
            if (!threat.Play(3 - color, liberties[0]) || threat.cells[p] != 0) continue;
            var candidates = new HashSet<int> { liberties[0] };
            foreach (int enemy in group.SelectMany(Neighbors).Where(n => cells[n] == 3 - color).Distinct())
            {
                var enemyLiberties = NeighborsOfGroup(enemy).Take(2).ToArray();
                if (enemyLiberties.Length == 1) candidates.Add(enemyLiberties[0]);
            }
            foreach (int candidate in candidates)
            {
                var trial = Copy();
                if (!trial.Play(color, candidate) || trial.cells[p] != color
                    || trial.NeighborsOfGroup(p).Take(2).Count() < 2
                    || trial.StonesLostToImmediateReply(color, candidate) > 0) continue;
                result[candidate] = Math.Max(result.GetValueOrDefault(candidate), group.Count);
            }
        }
        return result;
    }

    // 戦術#15: 単石は三呼吸点以上へ回復し、他の味方も直後に取られない場合だけ救う。
    internal IReadOnlySet<int> FindSafeSingleStoneRescueMoves(int color)
    {
        if (color is not (1 or 2)) throw new ArgumentException("invalid color");
        var result = new HashSet<int>();
        if (Size < 9 || MoveCount >= MoveLimit - 1 || ConsecutivePasses >= 2) return result;
        var protectedOwners = ProtectedTerritoryOwners();
        for (int p = 0; p < cells.Length; p++)
        {
            if (cells[p] != color || Group(cells, p).stones.Count != 1) continue;
            var liberties = NeighborsOfGroup(p).Take(2).ToArray();
            if (liberties.Length != 1) continue;
            var threat = Copy();
            if (!threat.Play(3 - color, liberties[0]) || threat.cells[p] == color) continue;
            var candidates = new HashSet<int> { liberties[0] };
            foreach (int enemy in Neighbors(p).Where(n => cells[n] == 3 - color))
            {
                var enemyLiberties = NeighborsOfGroup(enemy).Take(2).ToArray();
                if (enemyLiberties.Length == 1) candidates.Add(enemyLiberties[0]);
            }
            foreach (int candidate in candidates)
            {
                if (protectedOwners[candidate] == color || IsEye(color, candidate)) continue;
                var trial = Copy();
                if (!trial.Play(color, candidate) || history.Contains(trial.PositionKey)
                    || trial.cells[p] != color || trial.NeighborsOfGroup(p).Take(3).Count() < 3
                    || trial.HasImmediateFriendlyLoss(color)) continue;
                result.Add(candidate);
            }
        }
        return result;
    }

    private bool HasImmediateFriendlyLoss(int color)
    {
        var seen = new HashSet<int>();
        for (int p = 0; p < cells.Length; p++)
        {
            if (cells[p] != color || !seen.Add(p)) continue;
            seen.UnionWith(Group(cells, p).stones);
            var liberties = NeighborsOfGroup(p).Take(2).ToArray();
            if (liberties.Length != 1) continue;
            var reply = Copy();
            if (reply.Play(3 - color, liberties[0]) && reply.cells[p] != color) return true;
        }
        return false;
    }
    // 戦術#13: 小目の内側は予定地。確定地や死活判定には使わない。
    private IEnumerable<(int anchor, int[] inside, int[] corner)> CornerPlans()
    {
        if (Size < 9) yield break;
        foreach (bool swap in new[] { false, true })
        foreach (bool top in new[] { false, true })
        foreach (bool right in new[] { false, true })
        {
            int Map(int x, int y)
            {
                if (swap) (x, y) = (y, x);
                if (right) x = Size - 1 - x;
                if (top) y = Size - 1 - y;
                return y * Size + x;
            }
            yield return (Map(2, 3),
                Enumerable.Range(0, 12).Select(i => Map(i % 3, i / 3)).ToArray(),
                Enumerable.Range(0, 16).Select(i => Map(i % 4, i / 4)).ToArray());
        }
    }

    internal IReadOnlyList<int> FindOpenCornerKomokuMoves() => CornerPlans()
        .Where(plan => plan.corner.All(p => cells[p] == 0)).Select(plan => plan.anchor).ToArray();

    internal IReadOnlySet<int> FindPlannedCornerPoints(int color)
    {
        if (color is not (1 or 2)) throw new ArgumentException("invalid color");
        return CornerPlans().Where(plan => cells[plan.anchor] == color
                && plan.corner.All(p => cells[p] != 3 - color))
            .SelectMany(plan => plan.inside).Where(p => cells[p] == 0).ToHashSet();
    }
    // 合法性と戦訓は分離する。相手の合法手を戦訓で拒否しない。
    // 戦術#20：9路の通常同順位候補で、2×2の詰め込みを後回しにする。
    // 形だけで禁止せず、実捕獲と安全なウッテガエシは従来どおり許す。
    // 必要な連絡や長期の損得すべてを判定するものではない。
    internal bool ShouldDeferDumpling(int color, int point)
    {
        if (Size != 9 || !KifuwarabeGoPolypropylene.Domain.LocalPatterns.DumplingStones.DumplingStonesPattern.Matches(cells, Size, color, point)) return false;
        var trial = Copy();
        if (!trial.Play(color, point)
            || trial.cells.Count(c => c == 3 - color) < cells.Count(c => c == 3 - color)) return false;
        return !(trial.StonesLostToImmediateReply(color, point) > 0
            && trial.HasSafeSnapback(color, point, history));
    }
    // 戦術#16：候補点の相対八方向だけを調べ、空き三角を通常候補の後ろへ回す。
    // 全盤の形の走査はしない。禁止ではなく、代替候補がなければ着手できる。
    // 捕獲と安全なウッテガエシは除外し、緊急救出等の順位もこの判定より先に保つ。
    // 戦術#27：二呼吸点の連を、別連へつないで空間を増やす空き三角を区別する。
    internal bool IsLibertyImprovingConnection(int color, int point)
    {
        if (Size != 9 || cells[point] != 0) return false;
        var seen = new HashSet<int>();
        int count = 0, minimum = int.MaxValue, maximum = 0;
        foreach (int p in Neighbors(point))
        {
            if (cells[p] != color || seen.Contains(p)) continue;
            var group = Group(cells, p);
            seen.UnionWith(group.stones);
            int liberties = NeighborsOfGroup(p).Count();
            minimum = Math.Min(minimum, liberties);
            maximum = Math.Max(maximum, liberties);
            count++;
        }
        if (count < 2 || minimum != 2 || maximum < 3) return false;
        var trial = Copy();
        return trial.Play(color, point)
            && trial.NeighborsOfGroup(point).Count() >= 4
            && trial.StonesLostToImmediateReply(color, point) == 0;
    }
    // 戦術#18：二呼吸点の斜めの連へ二つの橋を残し、両方の切断へ連絡できる候補。
    // 二つの指定応手後の四呼吸点・直後損失0だけを確認。長期生きや全盤利益ではない。
    internal bool IsLibertyImprovingKosumiConnection(int color, int point)
    {
        if (Size != 9 || (uint)point >= (uint)cells.Length || cells[point] != 0) return false;
        Board Clone(Board b) { var c=b.Copy(); c.history.UnionWith(b.history); return c; }
        var after=Clone(this);
        if(!after.Play(color,point) || after.cells.Count(c=>c==3-color)!=cells.Count(c=>c==3-color)
            || after.NeighborsOfGroup(point).Count()<4 || after.StonesLostToImmediateReply(color,point)!=0) return false;
        var main=after.Group(after.cells,point).stones;
        for(int dx=-1;dx<=1;dx+=2)for(int dy=-1;dy<=1;dy+=2)
        {
            int x=point%Size+dx,y=point/Size+dy;
            if(x<0||x>=Size||y<0||y>=Size)continue;
            int diagonal=x+y*Size;
            if(cells[diagonal]!=color||main.Contains(diagonal))continue;
            int bridge1=x+point/Size*Size,bridge2=point%Size+y*Size;
            if(cells[bridge1]!=0||cells[bridge2]!=0)continue;
            var weak=Group(cells,diagonal).stones;
            if(!NeighborsOfGroup(diagonal).ToHashSet().SetEquals(new[]{bridge1,bridge2}))continue;
            var original=main.Concat(weak).ToHashSet();bool joined=true;
            foreach(var pair in new[]{(bridge1,bridge2),(bridge2,bridge1)})
            {
                var reply=Clone(after);
                if(!reply.Play(3-color,pair.Item1)||reply.IsFinished||!reply.Play(color,pair.Item2)
                    ||original.Any(p=>reply.cells[p]!=color)
                    ||!original.All(reply.Group(reply.cells,pair.Item2).stones.Contains)
                    ||reply.NeighborsOfGroup(pair.Item2).Count()<4
                    ||reply.StonesLostToImmediateReply(color,pair.Item2)!=0){joined=false;break;}
            }
            if(joined)return true;
        }
        return false;
    }
    internal bool ShouldDeferEmptyTriangle(int color, int point)
    {
        if (!KifuwarabeGoPolypropylene.Domain.LocalPatterns.EmptyTriangle.EmptyTrianglePattern.Matches(cells, Size, color, point)) return false;
        if (IsLibertyImprovingConnection(color, point) || IsLibertyImprovingKosumiConnection(color, point)) return false;
        // 二眼の生き壁に接する局所防御は、従来の侵入対処を維持する。
        if (Neighbors(point).Any(p => cells[p] == color && HasTwoEyes(color, p))) return false;
        var trial = Copy();
        if (!trial.Play(color, point)
            || trial.cells.Count(c => c == 3 - color) < cells.Count(c => c == 3 - color)) return false;
        return !(trial.StonesLostToImmediateReply(color, point) > 0
            && trial.HasSafeSnapback(color, point, history));
    }
    // 戦術#17：一間の割込みを許す形を9路の通常候補だけ後回しにする。
    // 救出等の既存優先順位は維持し、捕獲・生き壁の局所防御は除外する。
    // 戦術#18：コスミで二呼吸点連を守り、割込み後も別点で物理連絡できる候補に限定。
    internal bool HasRecoverableSplitWithKosumi(int color,int point)
    {
        if(!KifuwarabeGoPolypropylene.Domain.LocalPatterns.EmptyTriangle.EmptyTrianglePattern.Matches(cells,Size,color,point)
            ||!IsLibertyImprovingKosumiConnection(color,point))return false;
        Board Clone(Board b){var c=b.Copy();c.history.UnionWith(b.history);return c;}
        var after=Clone(this);if(!after.Play(color,point))return false;
        bool matched=false;
        foreach(var axis in new[]{(1,0),(-1,0),(0,1),(0,-1)})
        for(int side=-1;side<=1;side+=2)
        {
            int x=point%Size,y=point/Size;
            int fx=x+2*axis.Item1,fy=y+2*axis.Item2;
            int mx=x+axis.Item1,my=y+axis.Item2;
            int ex=mx-side*axis.Item2,ey=my+side*axis.Item1;
            if(fx<0||fx>=Size||fy<0||fy>=Size||ex<0||ex>=Size||ey<0||ey>=Size)continue;
            int far=fx+fy*Size,middle=mx+my*Size;
            if(cells[far]!=color||cells[middle]!=0||cells[ex+ey*Size]!=3-color)continue;
            matched=true;
            var original=after.Group(after.cells,point).stones.Concat(after.Group(after.cells,far).stones).ToHashSet();
            var cut=Clone(after);if(!cut.Play(3-color,middle))continue;
            if(cut.IsFinished||original.Any(p=>cut.cells[p]!=color))return false;
            bool recovered=false;
            foreach(int response in cut.NeighborsOfGroup(point).Intersect(cut.NeighborsOfGroup(far)).ToArray())
            {
                var reply=Clone(cut);
                if(reply.Play(color,response)&&original.All(p=>reply.cells[p]==color)
                    &&original.All(reply.Group(reply.cells,response).stones.Contains)
                    &&reply.NeighborsOfGroup(response).Count()>=4
                    &&reply.StonesLostToImmediateReply(color,response)==0){recovered=true;break;}
            }
            if(!recovered)return false;
        }
        return matched;
    }
    internal bool ShouldDeferOneSpaceSplit(int color, int point)
    {
        if (Size != 9 || !KifuwarabeGoPolypropylene.Domain.LocalPatterns.SplitShape.SplitShapePattern
                .MatchesOneSpace(cells, Size, color, point)) return false;
        if (HasRecoverableSplitWithKosumi(color, point)) return false;
        if (Neighbors(point).Any(p => cells[p] == color && HasTwoEyes(color, p))) return false;
        var trial = Copy();
        if (!trial.Play(color, point)
            || trial.cells.Count(c => c == 3 - color) < cells.Count(c => c == 3 - color)) return false;
        return !(trial.StonesLostToImmediateReply(color, point) > 0
            && trial.HasSafeSnapback(color, point, history));
    }
    // 戦術#21：敵に接する二呼吸点の孤立単石を、9路の通常候補だけ後回しにする。
    // すぐアタリを掛けられて救出に追われる形を減らす試み。捨て石を禁止しない。
    internal bool ShouldDeferExposedSingle(int color, int point)
    {
        if (Size != 9 || (uint)point >= (uint)cells.Length || cells[point] != 0) return false;
        var adjacent = Neighbors(point).ToArray();
        if (adjacent.Any(p => cells[p] == color)
            || !adjacent.Any(p => cells[p] == 3 - color)
            || adjacent.Count(p => cells[p] == 0) != 2) return false;
        var trial = Copy();
        return trial.Play(color, point)
            && trial.cells.Count(c => c == 3 - color) == cells.Count(c => c == 3 - color);
    }
    // 戦術#22：候補の最大四隣に新しい一目眼を同一連で囲う手を9路で試す。
    // 一眼は生きの証明ではない。既存の地・眼・合法性・取り返し検査は維持する。
    internal bool FormsSinglePointEye(int color, int point)
    {
        if (Size != 9 || (uint)point >= (uint)cells.Length || cells[point] != 0) return false;
        if (Neighbors(point).Any(q => cells[q] == color && HasTwoEyes(color, q))) return false;
        bool LocalEye(Board board, int eye)
        {
            if (board.cells[eye] != 0) return false;
            var boundary = board.Neighbors(eye).ToArray();
            if (boundary.Any(q => board.cells[q] != color)) return false;
            var group = board.Group(board.cells, boundary[0]).stones;
            if (boundary.Any(q => !group.Contains(q))) return false;
            int enemies = 0;
            for (int dx = -1; dx <= 1; dx += 2)
                for (int dy = -1; dy <= 1; dy += 2)
                {
                    int x = eye % Size + dx, y = eye / Size + dy;
                    if (x >= 0 && x < Size && y >= 0 && y < Size && board.At(x, y) == 3 - color) enemies++;
                }
            bool edge = eye % Size == 0 || eye % Size == Size - 1 || eye / Size == 0 || eye / Size == Size - 1;
            return enemies <= (edge ? 0 : 1);
        }
        var after = Copy();
        if (!after.Play(color, point)) return false;
        return Neighbors(point).Any(q => !LocalEye(this, q) && LocalEye(after, q));
    }
    // 戦術#25：捕獲しながら別の敵連をアタリにする候補。強制捕獲や全盤の利益は保証しない。
    internal int CaptureAndAtariPressure(int color, int point)
    {
        if (Size != 9 || (uint)point >= (uint)cells.Length || cells[point] != 0) return 0;
        int enemy = 3 - color;
        var adjacent = Neighbors(point).Where(p => cells[p] == enemy).ToArray();
        if (!adjacent.Any(p => NeighborsOfGroup(p).Take(2).Count() == 1)) return 0;
        var trial = Copy();
        if (!trial.Play(color, point) || trial.cells.Count(c => c == enemy) >= cells.Count(c => c == enemy)) return 0;
        var seen = new HashSet<int>();
        int pressure = 0;
        foreach (int p in adjacent)
        {
            if (seen.Contains(p)) continue;
            var before = Group(cells, p);
            seen.UnionWith(before.stones);
            if (NeighborsOfGroup(p).Take(2).Count() < 2 || trial.cells[p] != enemy) continue;
            if (trial.NeighborsOfGroup(p).Take(2).Count() == 1) pressure += before.stones.Count;
        }
        return pressure;
    }

    // 戦術#31：一眼形成後、攻め二手以内の捕獲を証明できた場合だけ一眼優先を外す。
    // 4096ノード打切り/未検出は不明として元の優先を維持。通常候補自体は除外しない。
    internal bool ShouldPrioritizeSinglePointEye(int color, int point)
    {
        if (!FormsSinglePointEye(color, point)) return false;
        Board Clone(Board b) { var c = b.Copy(); c.history.UnionWith(b.history); return c; }
        var after = Clone(this);
        if (!after.Play(color, point)) return true;
        if (after.cells.Count(c => c == 3-color) != cells.Count(c => c == 3-color)
            || after.HasTwoEyes(color, point)) return true;
        var original = after.Group(after.cells, point).stones;
        if (after.NeighborsOfGroup(point).Take(3).Count() != 2) return true;
        int nodes=0;
        bool Captured(Board b, int side, int depth)
        {
            if (++nodes > 4096) return false;
            if (original.All(p => b.cells[p] != color)) return true;
            if (depth == 0 || b.IsFinished || b.cells[point] != color) return false;
            int?[] moves = side == color
                ? Enumerable.Range(0,cells.Length).Select(p => (int?)p).Append(null).ToArray()
                : b.NeighborsOfGroup(point).Distinct().Order().Select(p => (int?)p).ToArray();
            bool any=false;
            foreach (int? p in moves)
            {
                var next=Clone(b);
                if (!next.Play(side,p)) continue;
                any=true;
                bool forced=Captured(next,3-side,depth-1);
                if (side != color && forced) return true;
                if (side == color && !forced) return false;
            }
            return side == color && any;
        }
        return !Captured(after,3-color,3);
    }

    public int? Generate(int color)
    {
        if (color is not (1 or 2)) throw new ArgumentException("invalid color");
        if (ReachedMoveLimit) return null;
        var protectedOwners = ProtectedTerritoryOwners();
        var invasionCaptures = SmallInvasionCapturePoints(color);
        var rescues = AtariRescuePoints(color);
        var splits = FindSmallInvasionAtariMoves(color);
        var singleRescues = FindSafeSingleStoneRescueMoves(color);
        // 戦術#23：9路の二眼完成を通常の形回避より優先。既存緊急手と除外検査は維持。
        var twoEyeCompletions = Size == 9 ? FindTwoEyeCompletingMoves(color) : Array.Empty<int>();
        // 戦術#12: 空盤の初手だけ小目C4を優先し、通常の戦訓検査を通す。
        bool komokuOpening = Size >= 9 && MoveCount == 0 && cells.All(c => c == 0);
        int komoku = 3 * Size + 2;
        // 戦術#24：9路序盤は他所の接触だけで空き角への展開を解除しない。緊急順位は維持。
        bool cornerOpening = MoveCount < 8 && (Size == 9 || !Enumerable.Range(0, cells.Length)
            .Any(p => cells[p] != 0 && Neighbors(p).Any(n => cells[n] == 3 - cells[p])));
        var openCorners = cornerOpening ? FindOpenCornerKomokuMoves() : Array.Empty<int>();
        var plannedCorners = FindPlannedCornerPoints(color);
        // 戦術#14: 通常候補は三/四線から。手数だけで解除せず、生きた壁の局所防御は従来順を守る。
        bool expandOpening = Size >= 9
            && !Enumerable.Range(0, cells.Length).Any(q => cells[q] == color && HasTwoEyes(color, q));
        int LinePenalty(int p)
        {
            int edge = Math.Min(Math.Min(p % Size, Size - 1 - p % Size),
                Math.Min(p / Size, Size - 1 - p / Size));
            return Math.Min(Math.Abs(edge - 2), Math.Abs(edge - 3));
        }
        foreach (int p in Enumerable.Range(0, cells.Length)
            .OrderByDescending(p => rescues.GetValueOrDefault(p))
            .ThenByDescending(invasionCaptures.Contains)
            .ThenBy(p => splits.Contains(p) ? splits.ToList().IndexOf(p) : int.MaxValue)
            .ThenByDescending(singleRescues.Contains)
            .ThenByDescending(twoEyeCompletions.Contains)
            .ThenByDescending(p => CaptureAndAtariPressure(color, p))
            .ThenByDescending(p => ShouldPrioritizeSinglePointEye(color, p))
            .ThenBy(p => !rescues.ContainsKey(p) && !invasionCaptures.Contains(p)
                && !splits.Contains(p) && !singleRescues.Contains(p) && ShouldDeferEmptyTriangle(color, p))
            .ThenBy(p => !rescues.ContainsKey(p) && !invasionCaptures.Contains(p)
                && !splits.Contains(p) && !singleRescues.Contains(p) && ShouldDeferOneSpaceSplit(color, p))
            .ThenBy(p => !rescues.ContainsKey(p) && !invasionCaptures.Contains(p)
                && !splits.Contains(p) && !singleRescues.Contains(p) && ShouldDeferExposedSingle(color, p))
            .ThenByDescending(p => komokuOpening && p == komoku)
            .ThenBy(p => openCorners.Contains(p) ? openCorners.ToList().IndexOf(p) : int.MaxValue)
            .ThenBy(p => expandOpening ? LinePenalty(p) : 0)
            // 戦術#20：別の連の二眼成立後も、通常候補の団子回避を維持する。
            .ThenBy(p => ShouldDeferDumpling(color, p)))
        {
            if (protectedOwners[p] == color) continue;
            if (plannedCorners.Contains(p) && !rescues.ContainsKey(p)
                && !invasionCaptures.Contains(p) && !splits.Contains(p) && !singleRescues.Contains(p)) continue;
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
            bool snapback = captured == 0 && lost > 0 && trial.HasSafeSnapback(color, p, history);
            if (captured == 0 && lost > 0 && !snapback)
            {
                Observe("immediate-capture", "置いた石を次の一手で取られた");
                continue;
            }
            if (captured > 0 && lost > captured)
            {
                Observe("capture-reversal", "石を取った直後に、それより多くの味方を取られた");
                continue;
            }
            int captureAtariPressure = CaptureAndAtariPressure(color, p);
            bool completedTwoEyes = twoEyeCompletions.Contains(p);
            bool formedSinglePointEye = FormsSinglePointEye(color, p);
            Play(color, p);
            if (captureAtariPressure > 0) tacticApplications.Add(new("shared-liberty-pressure", MoveCount, color, p));
            if (completedTwoEyes) tacticApplications.Add(new("complete-two-eyes", MoveCount, color, p));
            if (formedSinglePointEye) tacticApplications.Add(new("form-single-point-eye", MoveCount, color, p));
            if (komokuOpening && p == komoku) tacticApplications.Add(new("komoku-opening", MoveCount, color, p));
            if (!komokuOpening && openCorners.Contains(p)) tacticApplications.Add(new("komoku-corner-plan", MoveCount, color, p));
            if (expandOpening && LinePenalty(p) == 0 && !openCorners.Contains(p)
                && !rescues.ContainsKey(p) && !invasionCaptures.Contains(p) && !splits.Contains(p) && !singleRescues.Contains(p))
                tacticApplications.Add(new("opening-lines", MoveCount, color, p));
            if (singleRescues.Contains(p)) tacticApplications.Add(new("safe-single-rescue", MoveCount, color, p));
            if (snapback) tacticApplications.Add(new("resolve-before-pass", MoveCount, color, p));
            if (invasionCaptures.Contains(p)) tacticApplications.Add(new("minimum-invasion-defense", MoveCount, color, p));
            if (rescues.ContainsKey(p)) tacticApplications.Add(new("rescue-atari-chain", MoveCount, color, p));
            if (splits.Contains(p)) tacticApplications.Add(new("split-small-invasion", MoveCount, color, p));
            return p;
        }
        // 自分の地は候補とフォールバックから除外する。残りの合法手がなければパス。
        Play(color, null);
        return null;
    }
    // 戦術#4の限定読み: 1石を取らせ、同じ点で2石以上を安全に取り返す。
    internal bool HasSafeSnapback(int color, int point, IReadOnlySet<string> seen)
    {
        var liberties = NeighborsOfGroup(point).Take(2).ToArray();
        if (liberties.Length != 1 || ReachedMoveLimit) return false;
        var response = Copy();
        int before = cells.Count(c => c == color);
        if (!response.Play(3 - color, liberties[0]) || response.cells[point] != 0
            || before - response.cells.Count(c => c == color) != 1
            || seen.Contains(response.PositionKey)) return false;
        var recapture = response.Copy();
        int enemyBefore = response.cells.Count(c => c == 3 - color);
        if (!recapture.Play(color, point) || seen.Contains(recapture.PositionKey)) return false;
        int taken = enemyBefore - recapture.cells.Count(c => c == 3 - color);
        return taken >= 2 && recapture.StonesLostToImmediateReply(color, point) == 0;
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
