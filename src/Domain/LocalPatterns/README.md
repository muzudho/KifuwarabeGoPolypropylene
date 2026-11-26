# 局所パターンのソース案内

着手前の空点Tを原点に、指定された近傍だけを読む幾何検出。クラスの日本語XMLコメントに盤面例、相対座標、対象外の判断を記載している。

- EmptyTriangle/EmptyTrianglePattern.cs：空き三角。指定外点はワイルドカード。団子石と同時一致する場合もある。
- DumplingStones/DumplingStonesPattern.cs：Tを含む2×2充填。最大四つの小領域だけを確認する。
- SplitShape/SplitShapePattern.cs：割かれ形。相手石一つ・自石一つの二点の型を八対称で確認する。
- LocalPatternMatcher.cs：入力確認、相対座標の対称変換、盤外確認を共有する。

各公開APIは `Matches(ReadOnlySpan<int> cells, int size, int color, int target)`。配列はA1から行ごとに並べ、0=空点、1=黒、2=白。対象が空点でない、色・寸法が不正、盤外にある場合はfalse。配列は変更しない。staticメソッドと値型/読み取り専用Spanを使用し、検出中のインスタンス生成・配列確保・盤面複製は行わない。実測速度の保証ではない。

一致は「作ってよいかを確認すべき形」の検出であり、合法性・捕獲・救出・必要な連絡・死活・着手順位は別の責務。敵接触を一律の例外にせず、まず形を検出する。着手順位への統合は検証中で、この整理ではGenerateの選択を変更していない。NASの旧ShapePenaltyは過去の順位試作として残しており、本番へコピーしていない。

ThinkEngineのcsprojからこのフォルダーのC#をコンパイル対象に含める。検証はKifuwarabeGoPolypropylene.ThinkEngine/LocalPatternSelfTest.csに集約し、SelfTestから呼ぶ。今回714チェックを追加し、全9061チェックと32局の研究検証に成功。ビルド・検証成果物はNASに保存。