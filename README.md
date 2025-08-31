<img width="1280" height="720" alt="Image" src="https://github.com/user-attachments/assets/3034cf0e-753f-49b7-8661-e72ede76c0c6" />

[English](#english) | [日本語](#japanese)

<a name="english"></a>
# English
## Table of Contents
- [What this tool does](#What_this_tool_does)
- [What it does better than other tools](#What_it_does_better_than_other_tools)
- [Installation](#Installation)
- [Usage](#Usage)
- [Notes](#Notes)

<a name="What_this_tool_does"></a>
## What this tool does
TilemapSplitter is a Unity editor extension that **classifies tiles by adjacency** on a given Tilemap and **reconstructs multiple Tilemaps per category** for distinct purposes(Operating environment is Unity 2023 or later)
* **Classify by number, direction of neighbors**
  * **Rect, Isometric**: Cross, T-Junction, Corner, VerticalEdge, HorizontalEdge, Isolate
  * **Hexagon**: Full(6), Junction5, Junction4, Junction3, Edge(2), Tip(1), Isolate(0)
* Per-category settings: Sorting Layer, Order in Layer, Tag, Preview color
* Preview the classification in Scene view(color overlay)
* **Options**:
  * **Attach Colliders**: add TilemapCollider2D + static Rigidbody2D + CompositeCollider2D to each generated Tilemap
  * **Merge VerticalEdge, HorizontalEdge**: combine both edge categories into one Tilemap(VerticalEdge settings take priority)
* Category merge rule(“Which obj to add to”): e.g., merge Cross into VerticalEdge or HorizontalEdge

<a name="What_it_does_better_than_other_tools"></a>
## What it does better than other tools
* **Post-processing at scale**: Works on an already drawn Tilemap to split by connectivity in one pass—complements RuleTile, auto-tiling(which focus on placement time)
* **One-screen workflow**: Configure per category → preview → execute, without custom scripts or juggling multiple windows
* **Collider-ready output**: Instantly produce physics-only or visual-only Tilemaps by toggling options
* **GUI-level merge rules**: Non-destructive category remapping (e.g., merge Cross into VerticalEdge) without code
> Note: Some of these are possible with custom scripts or complex setups in other tools, but TilemapSplitter focuses on making them **turn-key and repeatable** for finished maps

### Quick comparison (at a glance) 📊
| Scenario, Capability                              | RuleTile | Manual layer split | **TilemapSplitter** |
|----------------------------------------------------|-------------------------|--------------------|---------------------|
| Post-process finished maps                         | △                       | ○ (time-consuming) | **◎ (designed for it)** |
| Split by **connectivity categories**               | ×                       | △                  | **◎**              |
| **GUI-only** category merge rules (e.g., Cross→Edge)| ×                      | ×                  | **○**              |
| **Collider-ready** output per generated Tilemap    | ×      | △                  | **○**              |

### When TilemapSplitter is the better fit
- You already have a **finished single Tilemap** and need **role-based layers**(edges, corners, T-junctions, isolates)
- You want **GUI-only**
- You need a **physics-only** or **visual-only** Tilemap **right now**(auto add TilemapCollider2D, Rigidbody2D, CompositeCollider2D)

### When another tool is a better fit
- You want **placement-time** patterning or auto-replacement → keep using **RuleTile/Auto-tiling**
- You need **runtime** procedural generation (this tool targets **Editor-time** post-processing)
- You require **exact** cross-Tilemap draw order in Isometric (**Unity limitation**)

### Works great together with
- **RuleTile**: design at placement-time → **refactor after** with TilemapSplitter
- **Custom Brushes, Scripted importers**: bring content in → **normalize** layers via adjacency split

### Case studies (examples to copy) 💡
- **Outline, Glow for edges**: split Vertical, Horizontal edges → apply a distinct material, effect layer
- **Readable geometry**: isolate corners & T-junctions for decoration and level debugging
- **Physics separation**: generate a **collider-only** Tilemap while keeping visuals clean

<a name="Installation"></a>
## Installation
### Using UPM(Git URL)
1. Open Window → Package Manager
2. Plus Button → Add package from git URL…
3. Paste:
```
https://github.com/SunagimoOisii/TilemapSplitter.git?path=/Packages/com.sunagimo.tilemapsplitter
```

### Manual install
1. Clone the repo
2. Copy Packages/com.sunagimo.tilemapsplitter/Editor to your project (e.g. Assets/TilemapSplitter)
3. Restart Unity → Tools/TilemapSplitter appears

<a name="Usage"></a>
## Usage
1. Open Tools → TilemapSplitter
2. Assign your target Tilemap in Split Tilemap
3. Adjust per-category: Layer, Tag, Preview color
4. Optional:
  * Attach Colliders to generated Tilemaps
  * Merge VerticalEdge, HorizontalEdge into a single Tilemap
  * Use Which obj to add to to fold categories(e.g., Cross → VerticalEdge)
5. Click Execute Splitting �� Tilemaps are generated\n6. Use Reset(below Split Tilemap) to restore settings

### Script API
```csharp
using TilemapSplitter;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

// Returns a list of generated GameObjects
List<GameObject> created = TilemapSplitterApi.Split(
    source: someTilemap,
    canMergeEdges: false,      // Rect layout only: merge vertical/horizontal edges into one
    canAttachCollider: false,  // Attach TilemapCollider2D + Rigidbody2D(Static) + CompositeCollider2D
    progress: null,            // Optional IProgress<float> (0..1)
    isCancelled: null          // Optional Func<bool> to cancel classification
);
```


### Example (Preview & Result):
![Image](https://github.com/user-attachments/assets/8d28e9a7-9b0e-409a-85b8-d4f6afb715c4)

<a name="Notes"></a>
## Notes
* **Isometric, Isometric Z-as-Y**：
  * Unity sorts tiles per Tilemap, so fine-grained order between different Tilemaps cannot exactly match the original single-map appearance
  * After splitting, you can often get close by tuning TilemapRenderer → Mode, Order in Layer
- Before, After appearance (Isometric):
<img width="1035" height="430" alt="Image" src="https://github.com/user-attachments/assets/d9410b2b-746b-4034-9e93-6e92b319b529" />

---


<a name="japanese"></a>
# 日本語
## 目次
- [このツールでできること](#このツールでできること)
- [他ツールとの違い](#他ツールとの違い)
- [インストール](#インストール)
- [使い方](#使い方)
- [注意点](#注意点)

<a name="このツールでできること"></a>
## このツールでできること
TilemapSplitter は、対象の Tilemap 上のタイルを「隣接関係」で分類し、用途別に複数の Tilemap に再構成する Unity エディタ拡張です（動作環境: Unity 2023 以降）。
- 分類対象（隣接数・向き）
  - Rect, Isometric: Cross, T-Junction, Corner, VerticalEdge, HorizontalEdge, Isolate
  - Hexagon: Full(6), Junction5, Junction4, Junction3, Edge(2), Tip(1), Isolate(0)
- カテゴリごとの設定: Sorting Layer, Order in Layer, Tag, プレビュー色
- Scene ビューで分類プレビュー（色のオーバーレイ）
- オプション
  - Attach Colliders: 生成した各 Tilemap に TilemapCollider2D + 静的 Rigidbody2D + CompositeCollider2D を付与
  - Merge VerticalEdge, HorizontalEdge: 縦横エッジを 1 つの Tilemap に統合（VerticalEdge の設定を優先）
  - Which obj to add to: Cross を VerticalEdge/HorizontalEdge に寄せる等のカテゴリマージ規則

<a name="他ツールとの違い"></a>
## 他ツールとの違い
- 仕上げ後の一括後処理: 既に描いた Tilemap を接続カテゴリで一括分割（RuleTile/オートタイルは配置時が得意）
- 1 画面ワークフロー: GUI だけで「設定 → プレビュー → 実行」まで完結
- コライダー出力に即対応: 物理専用/見た目専用 Tilemap をすぐ用意
- GUI レベルのカテゴリマージ: コード不要で非破壊に再マッピング可能（例: Cross → VerticalEdge）

### TilemapSplitter が向いているケース
- 1 枚の完成 Tilemap を役割別レイヤー（エッジ/コーナー/T 字/孤立）に分けたい
- GUI だけで完結したい
- すぐに物理専用 or 見た目専用の Tilemap が欲しい（コライダー自動付与）

### 他ツールが向いているケース
- 配置時パターン適用や自動置換をしたい（RuleTile/オートタイル）
- 実行時の手続き的生成が必要（このツールはエディタ時の後処理向け）
- Isometric で分割後も完全に同一の描画順を再現したい（Unity の制約あり）

### 併用例
- RuleTile で作成 → TilemapSplitter で接続分割してレイヤーを整理
- カスタムブラシ/スクリプトインポート → 接続分割で正規化

### 事例
- ふち取り・発光: 縦横エッジを分割して専用マテリアル/エフェクトに
- 形状の読みやすさ: コーナー/T 字を分離して装飾やデバッグに
- 物理分離: 見た目と物理を分けるため、コライダー専用 Tilemap を生成

<a name="インストール"></a>
## インストール
### UPM（Git URL）
1. Window → Package Manager を開く
2. + ボタン → Add package from git URL…
3. 次を貼り付けて Add:
```
https://github.com/SunagimoOisii/TilemapSplitter.git?path=/Packages/com.sunagimo.tilemapsplitter
```

### 手動インストール
1. リポジトリをクローン
2. `Packages/com.sunagimo.tilemapsplitter/Editor` をプロジェクトへコピー（例: `Assets/TilemapSplitter`）
3. Unity を再起動 → `Tools/TilemapSplitter` が表示されます

<a name="使い方"></a>
## 使い方
1. Tools → TilemapSplitter を開く
2. Split Tilemap に分割対象の Tilemap を指定
3. 各カテゴリの Layer, Tag, プレビュー色を設定
4. 任意設定
   - Attach Colliders（コライダー付与）
   - Merge VerticalEdge, HorizontalEdge（縦横エッジ統合）
   - Which obj to add to（例: Cross を VerticalEdge へ）
5. Execute Splitting を押してカテゴリごとに Tilemap を生成
6. 設定を初期化したい場合は Reset（Split Tilemap 下）を使用

### スクリプト API
```csharp
using TilemapSplitter;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

// 生成された GameObject の一覧を返す
List<GameObject> created = TilemapSplitterApi.Split(
    source: someTilemap,
    canMergeEdges: false,      // 矩形レイアウトのみ: 縦/横エッジを 1 つに統合
    canAttachCollider: false,  // TilemapCollider2D + Rigidbody2D(Static) + CompositeCollider2D を付与
    progress: null,            // 任意の IProgress<float> (0..1)
    isCancelled: null          // 任意の Func<bool>（true で分類を中断）
);
```

### プレビュー例・出力例
![Image](https://github.com/user-attachments/assets/8d28e9a7-9b0e-409a-85b8-d4f6afb715c4)

<a name="注意点"></a>
## 注意点
- Isometric, Isometric Z-as-Y:
  - Unity は Tilemap 単位でソートするため、分割前の 1 枚と完全一致の描画順を再現できない場合があります
  - 分割後は TilemapRenderer の Mode や Order in Layer を調整すると近づけられることがあります
- Before/After（Isometric の例）:
<img width="1035" height="430" alt="Image" src="https://github.com/user-attachments/assets/d9410b2b-746b-4034-9e93-6e92b319b529" />
