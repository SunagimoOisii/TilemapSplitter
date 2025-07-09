# TilemapSplitter
Unity 用 Tilemap 分割ツールです。タイルの接続状態を解析し、条件に応じて複数の Tilemap に分割します。

## 主な機能
- メニュー **Tools/TilemapSplitter** から専用ウィンドウを起動
- タイルの上下左右接続数に基づき以下のように分類
  - 十字形 (Cross)
  - T 字形 (T Junction)
  - 角 (Corner)
  - 孤立タイル (Isolate)
  - 垂直エッジ・水平エッジ
- 各分類ごとにレイヤー設定やプレビュー表示が可能
- 実行後、指定した分類ごとに新しい Tilemap オブジェクトを生成

## 使い方
1. Unity プロジェクトに本リポジトリを導入
2. メニューから **Tools/TilemapSplitter** を選択
3. `Tilemap` フィールドに分割したい元の Tilemap を指定
4. 各分類の設定やプレビュー色を調整
5. `Split` ボタンを押すと、分類に応じた Tilemap が生成されます

## 動作環境
- **Unity 2023** 以降
- .NET Standard 2.1

## ライセンス
このリポジトリは [CC0 1.0 Universal](LICENSE) の下で公開されています。
