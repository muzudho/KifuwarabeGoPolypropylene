namespace KifuwarabeGoPolypropylene.ThinkEngine;

// 利用者指定の研究ルール: 空点・取った石・コミを加算せず、現在の石数だけで判定。
public sealed record StoneScore(int Black, int White)
{
    public int Difference => Black - White;
    public string Result => Difference == 0 ? "0" : $"{(Difference > 0 ? "B" : "W")}+{Math.Abs(Difference)}";
}
