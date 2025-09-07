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

### Case studies
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
- [何ができるのか](#何ができるのか)
- [他のツールより何が優れているのか](#他のツールより何が優れているのか)
- [導入方法](#導入方法)
- [使用方法](#使用方法)
- [注意点](#注意点)

<a name="何ができるのか"></a>
## このツールでできること
TilemapSplitter は、指定 Tilemap のタイルを**接続関係**で自動的に分類し、**カテゴリごとに Tilemap を再構成**する Unity エディタ拡張です(動作環境は Unity 2023 以降)
* **接続数にもとづく分類**
  * **Rect, Isometric**：Cross, T字, Corner, VerticalEdge, HorizontalEdge, Isolate
  * **Hexagon**：Full(6), Junction5, Junction4, Junction3, Edge(2), Tip(1), Isolate(0)
* 各カテゴリ単位で Sorting Layer, Order, Tag, プレビューの色 を設定
* プレビューでシーンビューに分類結果を色分け表示
* **オプション**
  * **Which obj to add to**：Cross を VerticalEdge へ統合などの再分類ルールを設定
  * **Attach Colliders**：生成 Tilemap に TilemapCollider2D + Rigidbody2D(BodyType：static) + CompositeCollider2D を付与
  * **Merge VerticalEdge, HorizontalEdge**：縦横エッジを1枚の Tilemap に統合（VerticalEdge の設定が優先）

<a name="他のツールより何が優れているのか"></a>
## 他ツールとの違い
* **後処理に強い**：描き終えた Tilemap を、接続カテゴリごとに一括分割。配置時の自動置換（RuleTile 等）を補完します
* **1画面で完結**：カテゴリ設定 → プレビュー → 実行までノーコードでスムーズ
* **コライダ用出力が即時**：物理専用／視覚専用の Tilemap をオプション切り替えだけで生成
* **GUIで統合ルール**：Cross を VerticalEdge に吸収、縦横エッジを結合などを非破壊で設定可能
> 補足：スクリプトや他ツールの応用で実現できる場合もありますが、TilemapSplitter は 完成済みマップの再編を簡単で反復可能な手順に特化しています

### 他ツールとの違い（比較）
| 想定ケース, 機能                                | RuleTile | 手作業レイヤー分割 | **TilemapSplitter** |
|-----------------------------------------------|-------------------------|--------------------|---------------------|
| **完成済み**マップの後処理                      | △        | ○(工数大)        | **◎(主戦場)**     |
| **接続カテゴリ**(エッジ, 角…)で分割              | ×        | △                  | **◎**               |
| **GUIだけ**で統合ルール(例：Cross → Edge)       | ×        | ×                  | **○**               |
| 生成 Tilemap ごとの**コライダ即時付与**          | ×       | △                  | **○**               |

### TilemapSplitter が向いているケース
- **1枚の完成Tilemap**を、**別レイヤーに一括分割**したい 
- **物理専用**や**視覚専用**の Tilemap を**すぐ作りたい**(TilemapCollider2D, Rigidbody2D, CompositeCollider2D 自動付与)

### 他ツールが向いているケース
- **配置時**の自動置換やパターン適用が目的
- **ランタイム**での自動生成が必要(本ツールは**エディタ時後処理**に特化)
- Isometric で **Tilemap間の厳密な前後一致**が必須(**Unityの仕様**で困難)

### 併用例
- **RuleTile**で配置 → **TilemapSplitter**で後処理(レイヤー再編, 物理専用 Tilemap 分離)
- **カスタムブラシ, インポーター**で投入 → **接続分解**でレイヤー標準化

### 事例
- **縁取り, グロー演出**：Vertical, Horizontal に位置するタイルだけ分離して別マテリアルに
- **レベル形状の可読性向上**：角, T字だけを抽出して装飾やデバッグに活用
- **物理と描画の責務分離**：視覚用は軽量化、**コライダ専用**レイヤーを高速生成

<a name="導入方法"></a>
## インストール
### UPM(Git URL)
1. Window → Package Manager
2. プラスのボタン → Add package from git URL…
3. 次を貼り付けて Add：
```
https://github.com/SunagimoOisii/TilemapSplitter.git?path=/Packages/com.sunagimo.tilemapsplitter
```

### 手動インストール
1. リポジトリをクローン
2. Packages/com.sunagimo.tilemapsplitter/Editor をプロジェクトへコピー(例：Assets/TilemapSplitter)
3. Unity 再起動 → Tools/TilemapSplitter がメニューに表示

<a name="使用方法"></a>
## 使い方
1. Tools → TilemapSplitter を開く
2. Split Tilemap に分割対象の Tilemap を指定
3. 各カテゴリの Layer, Tag, プレビュー色 を調整
4. 任意設定：
  * Attach Colliders(コライダ等の自動付与)
  * Merge VerticalEdge, HorizontalEdge(縦横エッジの結合)
  * Which obj to add to(例：Cross → VerticalEdge へ統合)
5. Execute Splitting を押下 → カテゴリ別の Tilemap が生成

### スクリプト API
```csharp
using TilemapSplitter;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

//生成されたGameObjectのリストを返す
List<GameObject> created = TilemapSplitterApi.Split(
    source: someTilemap,
    canMergeEdges: false,      //矩形レイアウトのみ：縦, 横エッジを1つにまとめる
    canAttachCollider: false,  //TilemapCollider2D + Rigidbody2D(Static) + CompositeCollider2D をアタッチ
    progress: null,            //IProgress<float>
    isCancelled: null          //Func<bool>
);
```

6. 設定を初期化したい場合は Reset(Split Tilemap 下)を使用

### プレビュー例・出力例
![Image](https://github.com/user-attachments/assets/8d28e9a7-9b0e-409a-85b8-d4f6afb715c4)

<a name="注意点"></a>
## 注意点
* **Isometric, Isometric Z-as-Y**：
  * Unity では Tilemap 単位でソートされるため、分割前後でタイル同士の微妙な前後関係を完全一致させることはできません
  * 分割後に TilemapRenderer の Mode, Order in Layer を調整することである程度近づけられます
- 分割前後の見え方(Isometric 例)：
<img width="1035" height="430" alt="Image" src="https://github.com/user-attachments/assets/d9410b2b-746b-4034-9e93-6e92b319b529" />


