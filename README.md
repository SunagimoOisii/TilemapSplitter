# TilemapSplitter
TilemapSplitter is a Unity editor extension that automatically classifies tiles in a Tilemap based on adjacency and reconstructs them into multiple Tilemaps for specific purposes.

## Features
- Launch the dedicated window via **Tools/TilemapSplitter**
- Classify tiles by number of neighbors into the following categories:
  - Cross (connected in all four directions)
  - T Junction (three connections)
  - Corner (two connections forming a corner)
  - Isolate (no connections)
  - VerticalEdge / HorizontalEdge
- Each category allows configuring layer, tag and preview color
- Categories like Cross can be merged into VerticalEdge or HorizontalEdge via `Which obj to add to`
- After execution, new Tilemap objects are created per category
- Enable preview to visualize classification results in the Scene
- Settings persist via EditorPrefs even after closing the window
- A reset button is available below the Split Tilemap field

## Installation
1. Clone this repository and place the `TilemapSplitter` folder under your `Assets`
2. Restart Unity and **Tools/TilemapSplitter** will appear in the menu

## Usage
1. Open the window via **Tools/TilemapSplitter**
2. Set the target Tilemap in `Split Tilemap`
3. Configure layer, tag and preview color for each category
4. Optionally enable `Merge VerticalEdge, HorizontalEdge`
   - When merging, settings for VerticalEdge take priority
5. Press `Execute Splitting` to generate new Tilemaps based on the classification
6. Use the button under `Split Tilemap` to reset settings

Example of the tool window:

```
Place an image of the tool window here
```

Example of the split result:

```
Place an image of the split result here
```

## Requirements
- **Unity 2023** or later
- .NET Standard 2.1

## License
This repository is released under [CC0 1.0 Universal](LICENSE).

---


# TilemapSplitter 日本語版
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
- 設定は EditorPrefs を介して保存され、ウィンドウを閉じても維持
- `Split Tilemap` 欄の下にリセットボタンを配置

## インストール
1. `TilemapSplitter` フォルダーを `Assets` 配下へ配置します
2. Unity を再起動するとメニューに **Tools/TilemapSplitter** が追加されます

## 使い方

1. メニューから **Tools/TilemapSplitter** を選択しウィンドウを開く
2. `Split Tilemap` 欄に分割対象の Tilemap を指定
3. 各カテゴリでレイヤー・タグ・プレビュー色を設定
4. 必要に応じて `Merge VerticalEdge, HorizontalEdge` を有効化
   - マージ時は VerticalEdge の設定が優先されます
5. `Execute Splitting` を押すと分類結果に応じた Tilemap が生成されます
6. 設定をリセットしたい場合は `Split Tilemap` の下にあるボタンを使用

ツールウィンドウの例：
![Image](https://github.com/user-attachments/assets/8d28e9a7-9b0e-409a-85b8-d4f6afb715c4)

分割結果の例：

```
ここに分割結果の画像を配置
```

## 動作環境

- **Unity 2023** 以降
- .NET Standard 2.1

## ライセンス

このリポジトリは [CC0 1.0 Universal](LICENSE) の下で公開されています。
