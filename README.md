# TilemapSplitter

Unity の `Tilemap` を接続関係に基づき自動で分類し、用途に応じた複数の Tilemap として再構成するエディタ拡張です。

## 特徴

- メニュー **Tools/TilemapSplitter** から専用ウィンドウを起動
- タイルの接続数に応じて以下のカテゴリへ分類
  - Cross（上下左右すべて接続）
  - T Junction（3 方向接続）
  - Corner（2 方向接続で角を形成）
  - Isolate（周囲に接続のない孤立タイル）
  - VerticalEdge / HorizontalEdge
- 各カテゴリごとにレイヤー・タグ・プレビュー色を設定可能
- Cross などの分類は `Which obj to add to` で VerticalEdge / HorizontalEdge
  へ統合することもできる
- 実行後、選択したカテゴリ別に新しい Tilemap オブジェクトを生成
- プレビューを有効にすると Scene 上で分類結果をカラー表示

## インストール

1. 本リポジトリをクローンし、`TilemapSplitter` フォルダーを `Assets` 配下へ配置します
2. Unity を再起動するとメニューに **Tools/TilemapSplitter** が追加されます

## 使い方

1. メニューから **Tools/TilemapSplitter** を選択しウィンドウを開く
2. `Split Tilemap` 欄に分割対象の Tilemap を指定
3. 各カテゴリでレイヤー・タグ・プレビュー色を設定
4. 必要に応じて `Merge VerticalEdge, HorizontalEdge` を有効化
   - マージ時は VerticalEdge の設定が優先されます
5. `Execute Splitting` を押すと分類結果に応じた Tilemap が生成されます

ツールウィンドウの例：

```
ここにツールウィンドウの画像を配置
```

分割結果の例：

```
ここに分割結果の画像を配置
```

## 動作環境

- **Unity 2023** 以降
- .NET Standard 2.1

## ライセンス

このリポジトリは [CC0 1.0 Universal](LICENSE) の下で公開されています。
