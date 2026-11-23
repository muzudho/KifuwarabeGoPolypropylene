# KifuwarabeGoPolypropylene

コンピューター囲碁の思考エンジン専用リポジトリー。GUI・ロビー・大会通信などは KifuwarabeGo2026 側を利用する方針です。

現在は C# / .NET 10 の接続確認用試作です。標準入力で GTP 2 のコマンドを受け取り、標準出力で応答します。既存 KifuwarabeGo2026 との実接続は未検証です。

## 起動

```powershell
dotnet run --project .\KifuwarabeGoPolypropylene.ThinkEngine
```

起動後、例えば次を1行ずつ入力します。

```text
protocol_version
boardsize 9
komi 6.5
play b D4
genmove w
showboard
quit
```

`genmove w` は `= A1` と空行を返し、白石を実際に置きます。応答の `=` は成功、`?` はエラーです。コマンド番号と `#` コメントも扱えます。通常運転中の標準出力は GTP 応答専用です。

## 動作テスト

外部テストパッケージを追加せず、次のコマンドで石の取り・自殺手・単純コウ・パス・座標・GTP 応答を検査します。失敗時は終了コードが非ゼロになります。

```powershell
dotnet run --project .\KifuwarabeGoPolypropylene.ThinkEngine -- --self-test
```

## 試作の範囲

- 2～19 路盤。黒・白の指定、パス、石の取り、自殺手禁止、単純コウ。
- 着手選択は A1 から順に最初の合法手。合法手がなければパス。棋力を目指す探索・評価は今後実装します。
- コミは数値を受理しますが、得点計算と評価関数がないため着手に影響しません。
- 超コウ、終局判定、得点計算、時間管理、投了判断、置き石専用コマンド、undo、gRPC は未実装です。
- 対応コマンドは `list_commands` で確認できます。大会用としての完成品ではありません。

ソースは既存の ThinkEngine フォルダーに置いています。Board.cs は盤面と合法手判定、GtpEngine.cs は文字列の接続口、Program.cs は起動、SelfTest.cs は検証です。

## KifuwarabeGo2026 v4.0.9 のエントリー登録

EXE のコピーとは別に、GTP エンジン → computer プレイヤー → 接続先情報の登録が必要です。2026-11-23 のローカル環境では、エンジン登録だけ存在し、対応するプレイヤーがありませんでした。

ロビーを終了してから `scripts/Register-LocalEntry.ps1` を実行すると、既存の `Kifuwarabe Go Polypropylene` エンジン登録を利用し、欠けているプレイヤーと空の接続先情報を追加します。起動パスも配置先の絶対パスに更新します。変更前の3つの JSON は同じ場所に日時付き `.bak` として保存します。実行後はロビーを再起動してください。

```powershell
.\scripts\Register-LocalEntry.ps1
```

このスクリプトの既定値は今回のローカル環境用です。別環境では `-SettingsRoot` と `-EngineDirectory` で指定してください。CGOS のログイン情報は空のため、大会接続には別途設定が必要です。エントリーの参照整合性と配置した EXE の GTP 応答は検証済みですが、GUI での表示・対局は未確認です。
