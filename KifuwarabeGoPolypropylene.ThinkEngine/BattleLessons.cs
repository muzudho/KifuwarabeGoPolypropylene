namespace KifuwarabeGoPolypropylene.ThinkEngine;

public sealed record LessonReport(string Id, string Title, int Observations, int Avoided);
public sealed record LessonEvidence(string Id, int Size, string Position, int Color, int Point);

// 候補手の仮想実行で失敗を観測し、その対局プロセス内で戦訓を取得する。
public sealed class BattleLessons
{
    private readonly Dictionary<string, LessonReport> learned = new();
    private readonly Dictionary<string, LessonEvidence> examples = new();
    public IReadOnlyCollection<LessonReport> Reports => learned.Values;
    public IReadOnlyCollection<LessonEvidence> Examples => examples.Values;
    internal void CopyFrom(BattleLessons source)
    {
        learned.Clear();
        examples.Clear();
        foreach (var report in source.Reports) learned.Add(report.Id, report);
        foreach (var example in source.Examples) examples.Add(example.Id, example);
    }
    public void RecordExample(LessonEvidence evidence) => examples.TryAdd(evidence.Id, evidence);
    public void Avoid(string id)
    {
        var old = learned[id];
        learned[id] = old with { Avoided = old.Avoided + 1 };
    }
    public void Observe(string id, string title)
    {
        learned.TryGetValue(id, out var old);
        learned[id] = new(id, title, (old?.Observations ?? 0) + 1, (old?.Avoided ?? 0) + 1);
    }
}
