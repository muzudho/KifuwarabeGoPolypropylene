using System.Globalization;
namespace KifuwarabeGoPolypropylene.ThinkEngine;

// 日本式の計算要素を使う暫定判定。死石・セキの完全な日本ルール判定ではない。
public sealed record TerritoryScore(int BlackTerritory, int WhiteTerritory,
    int BlackPrisoners, int WhitePrisoners, double Komi)
{
    public double Difference => BlackTerritory + BlackPrisoners - WhiteTerritory - WhitePrisoners - Komi;
    public string Result => Difference == 0 ? "0" :
        $"{(Difference > 0 ? "B" : "W")}+{Math.Abs(Difference).ToString("0.################", CultureInfo.InvariantCulture)}";
}
