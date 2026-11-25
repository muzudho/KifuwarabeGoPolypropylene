using System.Diagnostics;
using System.Text.Json;

namespace KifuwarabeGoPolypropylene.ThinkEngine;

// 対局の証拠を収集する。研究ルールの勝敗を記録するが大会ルールの棋力とは区別する。
internal static class LessonCycle
{
    internal static int GameSeed(int seed, int gameIndex) => unchecked(seed + gameIndex * 1_000_003);

    public static int Run(string[] args)
    {
        string Option(string name, string fallback)
        {
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
        }
        int seed = int.Parse(Option("--seed", "1"));
        int games = int.Parse(Option("--games", "32"));
        int seconds = int.Parse(Option("--time-limit", "120"));
        string directory = Path.GetFullPath(Option("--output", "."));
        if (games < 1 || seconds < 1) throw new ArgumentException("games and time-limit must be positive");
        Directory.CreateDirectory(directory);
        var timer = Stopwatch.StartNew();

        int completed = 0, moveLimits = 0, timedOut = 0, repeated = 0;
        var bySize = new Dictionary<int, int[]>();
        using var log = new StreamWriter(Path.Combine(directory, "games.jsonl"), false) { AutoFlush = true };
        for (int gameIndex = 0; gameIndex < games; gameIndex++)
        {
            if (timer.Elapsed.TotalSeconds >= seconds) { timedOut++; break; }
            // 9路・19路を優先し、各サイズで自己対局とランダム相手の両方を行う。
            int size = new[] { 9, 19, 9, 19, 3, 5, 3, 5 }[gameIndex % 8];
            // 前の局の長さが変わっても、この局の乱数列は変えない。
            int gameSeed = GameSeed(seed, gameIndex);
            var random = new Random(gameSeed);
            var board = new Board(size);
            var moves = new List<int?>();
            var seen = new HashSet<string> { board.PositionKey };
            int passes = 0, repeats = 0;
            string outcome = "move-limit";
            // ランダムな序盤と、ランダム対戦相手の有無で観測局面を変える。
            bool randomOpponent = gameIndex % 4 >= 2;
            int openingLength = gameIndex % 5;
            for (int ply = 0; ply < Board.MoveLimit; ply++)
            {
                if (timer.Elapsed.TotalSeconds >= seconds) { outcome = "timeout"; break; }
                int color = ply % 2 + 1;
                int? move = null;
                if (ply < openingLength || randomOpponent && color == 2)
                {
                    // 乱数比較用: 合法手を無作為に試す。失敗した試行は盤面を変えない。
                    foreach (int point in Enumerable.Range(0, size * size).OrderBy(_ => random.Next()))
                        if (board.Play(color, point)) { move = point; break; }
                    if (move is null) board.Play(color, null);
                }
                else move = board.Generate(color);
                moves.Add(move);
                passes = move is null ? passes + 1 : 0;
                if (move is not null && !seen.Add(board.PositionKey)) repeats++;
                if (passes >= 2) { outcome = "two-passes"; break; }
            }
            if (outcome == "two-passes") completed++;
            else if (outcome == "timeout") timedOut++;
            else moveLimits++;
            repeated += repeats;
            if (!bySize.ContainsKey(size)) bySize[size] = new int[4];
            bySize[size][0]++;
            bySize[size][1] += outcome == "two-passes" ? 1 : 0;
            bySize[size][2] += outcome == "move-limit" ? 1 : 0;
            bySize[size][3] += repeats;
            log.WriteLine(JsonSerializer.Serialize(new {
                gameIndex, seed, gameSeed, randomStream = "per-game-v1", size, randomOpponent, openingLength, outcome,
                repeats, moves, moveCount = board.MoveCount,
                territoryScore = outcome == "timeout" ? null : board.ScoreTerritory(0),
                stoneCount = board.CountStones(),
                unconfirmedBlack = board.UnconfirmedStoneCount(1),
                unconfirmedWhite = board.UnconfirmedStoneCount(2),
                supportedOnlyBlack = board.SupportedOnlyStoneCount(1),
                supportedOnlyWhite = board.SupportedOnlyStoneCount(2),
                scoreStatus = outcome == "timeout" ? "unscored" : "provisional-not-proof-of-settlement",
                lessons = board.Lessons.Reports, evidence = board.Lessons.Examples,
                tactics = board.TacticApplications
            }));
            if (outcome == "timeout") break;
        }
        var summary = new {
            version = 6, seed, randomStream = "per-game-v1", requestedGames = games, completed, moveLimits, timedOut,
            provisionallyScoredGames = completed + moveLimits, moveLimit = Board.MoveLimit,
            repeatedPositions = repeated, elapsedSeconds = timer.Elapsed.TotalSeconds,
            byBoardSize = bySize.Select(pair => new { size = pair.Key, games = pair.Value[0], completed = pair.Value[1], moveLimits = pair.Value[2], repeats = pair.Value[3] }),
            engineVersion = "0.4.0", rules = "Pure Go Pancake: avoid own protected territory; territory plus prisoners and komi; simple ko; 400 plies",
            komi = 0, note = "Two passes or 400 plies: provisional territory score. Dead-stone removal and seki adjudication are not implemented. Not full Japanese rules. Timeout is not adjudicated."
        };
        string json = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(directory, "summary.json"), json);
        Console.WriteLine(json);
        return timedOut > 0 ? 2 : 0;
    }
}
