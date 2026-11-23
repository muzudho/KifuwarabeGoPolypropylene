using System.Globalization;
namespace KifuwarabeGoPolypropylene.ThinkEngine;

public sealed class GtpEngine
{
    private const string Columns = "ABCDEFGHJKLMNOPQRST";
    private static readonly string[] Commands = ["protocol_version", "name", "version", "known_command", "list_commands", "boardsize", "clear_board", "komi", "play", "genmove", "showboard", "kfw-lessons", "quit"];
    private readonly BattleLessons lessons = new();
    private Board board = new(19);
    public GtpEngine() { board = new Board(19) { Lessons = lessons }; }
    public void Run(TextReader input, TextWriter output)
    {
        while (input.ReadLine() is { } line)
        {
            var words = line.Split('#')[0].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (words.Count == 0) continue;
            string id = "";
            if (words[0].All(char.IsAsciiDigit)) { id = words[0]; words.RemoveAt(0); }
            string command = words.Count > 0 ? words[0] : "";
            try
            {
                string result = Execute(command, words.Skip(1).ToArray());
                output.Write($"={id}{(result.Length > 0 ? " " + result : "")}\n\n");
            }
            catch (ArgumentException e) { output.Write($"?{id} {e.Message}\n\n"); }
            output.Flush();
            if (command == "quit" && words.Count == 1) break;
        }
    }
    private string Execute(string command, string[] args)
    {
        int expected = command switch { "boardsize" or "komi" or "genmove" or "known_command" => 1, "play" => 2, _ => 0 };
        if (!Commands.Contains(command)) throw new ArgumentException("unknown command");
        if (args.Length != expected) throw new ArgumentException("invalid arguments");
        switch (command)
        {
            case "protocol_version": return "2";
            case "name": return "KifuwarabeGoPolypropylene";
            case "version": return "0.2.0";
            case "kfw-lessons": return System.Text.Json.JsonSerializer.Serialize(board.Lessons.Reports);
            case "known_command": return Commands.Contains(args[0]) ? "true" : "false";
            case "list_commands": return string.Join('\n', Commands);
            case "boardsize":
                if (!int.TryParse(args[0], out int size) || size < 2 || size > 19) throw new ArgumentException("unacceptable size");
                board = new Board(size) { Lessons = lessons }; return "";
            case "clear_board": board = new Board(board.Size) { Lessons = lessons }; return "";
            case "komi":
                if (!double.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double komi) || !double.IsFinite(komi)) throw new ArgumentException("invalid komi");
                // 評価関数・得点計算は未実装。コミは着手に影響しない。
                return "";
            case "play":
                if (!board.Play(Color(args[0]), ParseVertex(args[1]))) throw new ArgumentException("illegal move");
                return "";
            case "genmove": return FormatVertex(board.Generate(Color(args[0])));
            case "showboard":
                return "\n" + string.Join('\n', Enumerable.Range(0, board.Size).Reverse().Select(y => $"{y + 1,2} " + string.Join(' ', Enumerable.Range(0, board.Size).Select(x => ".XO"[board.At(x, y)])))) + "\n   " + string.Join(' ', Columns.Take(board.Size));
            default: return "";
        }
    }
    private static int Color(string text) => text.ToLowerInvariant() switch
    {
        "b" or "black" => 1, "w" or "white" => 2, _ => throw new ArgumentException("invalid color")
    };
    private int? ParseVertex(string text)
    {
        if (text.Equals("pass", StringComparison.OrdinalIgnoreCase)) return null;
        int x = text.Length > 0 ? Columns.IndexOf(char.ToUpperInvariant(text[0])) : -1;
        if (x < 0 || x >= board.Size || !int.TryParse(text.AsSpan(1), out int y) || y < 1 || y > board.Size) throw new ArgumentException("invalid vertex");
        return (y - 1) * board.Size + x;
    }
    private string FormatVertex(int? point) => point is int p ? $"{Columns[p % board.Size]}{p / board.Size + 1}" : "pass";
}
