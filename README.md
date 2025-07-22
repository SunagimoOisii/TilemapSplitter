<img width="1280" height="720" alt="Image" src="https://github.com/user-attachments/assets/3034cf0e-753f-49b7-8661-e72ede76c0c6" />

[English](#english) | [日本語](#japanese)

<a name="english"></a>
# English
TilemapSplitter is a Unity editor extension that automatically classifies tiles in a Tilemap based on adjacency and reconstructs them into multiple Tilemaps for specific purposes.

## Table of Contents
- [Features](#features)
- [Installation](#installation)
  - [Using UPM](#using-upm)
  - [Manual Install](#manual-install)
- [Usage](#usage)
- [Notes on Isometric Layouts](#notes-on-isometric-layouts)
- [Requirements](#requirements)
- [License](#license)

<a name="features"></a>
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

<a name="installation"></a>
## Installation
<a name="using-upm"></a>
### Using UPM
1. Open **Window > Package Manager** in Unity
2. Click the **+** button and select **Add package from git URL...**
3. Enter the following URL:
```
https://github.com/SunagimoOisii/TilemapSplitter.git?path=/Packages/com.sunagimo.tilemapsplitter
```
4. Press **Add** to install the package.

<a name="manual-install"></a>
### Manual Install
1. Clone this repository and place the `TilemapSplitter` folder under your `Assets`
2. Restart Unity and **Tools/TilemapSplitter** will appear in the menu

<a name="usage"></a>
## Usage
1. Open the window via **Tools/TilemapSplitter**
2. Set the target Tilemap in `Split Tilemap`
3. Configure layer, tag and preview color for each category
4. Optionally enable `Merge VerticalEdge, HorizontalEdge`
   - When merging, settings for VerticalEdge take priority
5. Press `Execute Splitting` to generate new Tilemaps based on the classification
6. Use the button under `Split Tilemap` to reset settings

UseCase：

![Image](https://github.com/user-attachments/assets/8d28e9a7-9b0e-409a-85b8-d4f6afb715c4)

<a name="notes-on-isometric-layouts"></a>
## Notes on Isometric Layouts
Even when the Grid's Cell Layout is `Isometric` or `Isometric Z as Y`, this tool can be used.
However, because Unity sorts tiles per Tilemap, the fine ordering between Tilemaps cannot match the original Tilemap.
Therefore, the appearance after splitting often differs from before.<br>
It is possible to match **to some extent** by setting the Mode and Order In Layer of the TilemapRenderer as appropriate after splitting.

- SplitDifference
<img width="1035" height="430" alt="Image" src="https://github.com/user-attachments/assets/d9410b2b-746b-4034-9e93-6e92b319b529" />

<a name="requirements"></a>
## Requirements
- **Unity 2023** or later
- .NET Standard 2.1

<a name="license"></a>
## License
This repository is released under [MIT LICENSE](LICENSE).

---


<a name="japanese"></a>
# 日本語
Unity の `Tilemap` を接続関係に基づき自動で分類し、用途に応じた複数の Tilemap として再構成するエディタ拡張です。

## 目次
- [特徴](#特徴)
- [インストール](#インストール)
  - [UPM を利用する場合](#upm-を利用する場合)
  - [手動インストール](#手動インストール)
- [使い方](#使い方)
- [注意点](#注意点)
- [動作環境](#動作環境)
- [ライセンス](#ライセンス)

<a name="特徴"></a>
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

<a name="インストール"></a>
## インストール

<a name="upm-を利用する場合"></a>
### UPM を利用する場合
1. Unity メニューから **Window > Package Manager** を開きます
2. 左上の **+** ボタンで **Add package from git URL...** を選択します
3. 次の URL を入力して **Add** を押します
```
https://github.com/SunagimoOisii/TilemapSplitter.git?path=/Packages/com.sunagimo.tilemapsplitter
```
4. 取り込みが完了するとパッケージが利用可能になります

<a name="手動インストール"></a>
### 手動インストール
1. `TilemapSplitter` フォルダーを `Assets` 配下へ配置します
2. Unity を再起動するとメニューに **Tools/TilemapSplitter** が追加されます

<a name="使い方"></a>
## 使い方

1. メニューから **Tools/TilemapSplitter** を選択しウィンドウを開く
2. `Split Tilemap` 欄に分割対象の Tilemap を指定
3. 各カテゴリでレイヤー・タグ・プレビュー色を設定
4. 必要に応じて `Merge VerticalEdge, HorizontalEdge` を有効化
   - マージ時は VerticalEdge の設定が優先されます
5. `Execute Splitting` を押すと分類結果に応じた Tilemap が生成されます
6. 設定をリセットしたい場合は `Split Tilemap` の下にあるボタンを使用

使用例：

![Image](https://github.com/user-attachments/assets/8d28e9a7-9b0e-409a-85b8-d4f6afb715c4)

<a name="注意点"></a>
## 注意点
Grid の CellLayout が `Isometric` または `Isometric Z as Y` の場合でも本ツールは使用できます。
ただし Unity の仕様上、Tilemap 間で細かな並び替えができないため、分割後の見た目が元の Tilemap と異なる可能性が高いです。<br>
分割後に TilemapRenderer の Mode や Order In Layer を適宜設定することで**ある程度**一致させることはできます

- 分割前後のタイルの様子
<img width="1035" height="430" alt="Image" src="https://github.com/user-attachments/assets/d9410b2b-746b-4034-9e93-6e92b319b529" />

<a name="動作環境"></a>
## 動作環境

- **Unity 2023** 以降
- .NET Standard 2.1

## ライセンス

このリポジトリは [MIT LICENSE](LICENSE) の下で公開されています。
