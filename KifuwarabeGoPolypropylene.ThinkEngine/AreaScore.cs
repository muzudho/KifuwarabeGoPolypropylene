using System.Globalization;

namespace KifuwarabeGoPolypropylene.ThinkEngine;

public sealed record AreaScore(int Black, int White, int Neutral, double Komi)
{
    public double Difference => Black - White - Komi;
    public string Result => Difference == 0 ? "0" :
        $"{(Difference > 0 ? "B" : "W")}+{Math.Abs(Difference).ToString("0.################", CultureInfo.InvariantCulture)}";
}
