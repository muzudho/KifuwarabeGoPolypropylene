using System.Diagnostics;
using System.Text.Json;

namespace KifuwarabeGoPolypropylene.ThinkEngine;

// 対局の証拠を収集する。得点計算がないため勝率や棋力は評価しない。
internal static class LessonCycle
{
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
        var random = new Random(seed);
        int completed = 0, moveLimits = 0, timedOut = 0, repeated = 0;
        using var log = new StreamWriter(Path.Combine(directory, "games.jsonl"), false) { AutoFlush = true };
        for (int gameIndex = 0; gameIndex < games; gameIndex++)
        {
            if (timer.Elapsed.TotalSeconds >= seconds) { timedOut++; break; }
            int size = new[] { 3, 5, 9, 19 }[gameIndex % 4];
            var board = new Board(size);
            var moves = new List<int?>();
            var seen = new HashSet<string> { board.PositionKey };
            int passes = 0, repeats = 0;
            string outcome = "move-limit";
            // ランダムな序盤と、ランダム対戦相手の有無で観測局面を変える。
            bool randomOpponent = gameIndex % 2 == 1;
            int openingLength = gameIndex % 5;
            for (int ply = 0; ply < size * size * 8 + 100; ply++)
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
            log.WriteLine(JsonSerializer.Serialize(new {
                gameIndex, seed, size, randomOpponent, openingLength, outcome,
                repeats, moves, lessons = board.Lessons.Reports, evidence = board.Lessons.Examples
            }));
            if (outcome == "timeout") break;
        }
        var summary = new {
            version = 1, seed, requestedGames = games, completed, moveLimits, timedOut,
            repeatedPositions = repeated, elapsedSeconds = timer.Elapsed.TotalSeconds,
            engineVersion = "0.2.0", note = "Convergence observations only; not a strength or win-rate evaluation."
        };
        string json = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(directory, "summary.json"), json);
        Console.WriteLine(json);
        return timedOut > 0 ? 2 : 0;
    }
}
