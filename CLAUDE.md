# CLAUDE.md

AI アシスタント向けのプロジェクト説明。作業を始める前に読むこと。

## プロジェクト概要

- 漢字をテーマにした5モードのクイズゲーム（PC 向け、余裕があればスマートフォン）。
- 1人開発の就活ポートフォリオ作品。完成目標は 2027年1月。
- フェーズ1：ゲームモード5種。フェーズ2（後回し）：問題作成モード、オンライン共有、アカウント。
- ユーザーとのやり取り、ドキュメント、コミットメッセージ、PR はすべて日本語。

## ドキュメント（正はこちら。このファイルと食い違ったら Docs を優先）

| ファイル | 内容 |
|---|---|
| `Docs/GameSpec.md` | 仕様。モード、ルール、未決定事項、既知の不具合 |
| `Docs/Architecture.md` | MVP 構成、クラス設計、データ設計、移行手順 |
| `Docs/ScreenDesign.md` | 画面レイアウトと遷移 |
| `Docs/CodingRules.md` | 命名規則、フォルダ構成、Git・PR の運用 |

設計や仕様を変えたら、対応する Docs も同じ作業の中で更新する。

## 技術スタック

- Unity 6000.4.3f1、URP、uGUI + TextMeshPro、Input System
- UniTask（非同期）、R3 1.3.1（イベント・値の通知）、NuGetForUnity 4.5.0
- VContainer は未導入（5モード完成後に検討）
- 書き順データは KanjiVG（CC BY-SA 3.0、クレジット表記が必要）

## アーキテクチャ（要点）

- **MVP**。Model と Presenter は普通の C# クラス、View だけが MonoBehaviour。
- View は判断を持たず、入力を R3 の `Observable` で公開するだけ。Presenter と Model を参照しない。
- 各シーンの `<Mode>EntryPoint`（MonoBehaviour）が Model・View・Presenter を作ってつなぐ。DI コンテナは使わない。
- 全モード共通の進行は `QuizPresenterBase<TQuestion>`、選択式4モードは `ChoiceQuizPresenter<TQuestion>` に置く（Template Method）。
- 問題データは JSON。`IQuestionRepository` 経由で読む。
- 非同期は UniTask。問題ごとに `CancellationTokenSource` を作り直す。`async void` 禁止。

## 現在の状態（2026-10-20 時点）

- MVP の共通部分は `Assets/TechC/KanjiQuiz/Scripts/Common/` にある（`QuizSession`, `QuizPresenterBase`, `ChoiceQuizPresenter`, `QuizFrameView`, `ChoiceListView` など）。
  - ゲーム本体は `TechC.KanjiQuiz` アセンブリ、EditMode テストは `Assets/TechC/KanjiQuiz/Tests/EditMode/`（`TechC.KanjiQuiz.Tests`）。
- 書き順クイズは MVP 構成に載せ替え済み（`Assets/TechC/KanjiQuiz/Scripts/Modes/StrokeOrder/`）。
  - シーンの `GameManager` にある `StrokeOrderEntryPoint` が組み立て役。出題する漢字・難易度・選択肢の数もここで設定する。
  - 書き順データは `Assets/StreamingAssets/KanjiVG/` の SVG（基本の字形のみ 6,701 ファイル）を UnityWebRequest で読む。
- シーンは `Assets/TechC/KanjiQuiz/Scenes/` の `StrokeOrder.unity` と `Result.unity`（全モード共通の結果画面）。全問回答すると `QuizResultStore` に結果を置いて Result へ移る。
- 文字は Klee One の TMP フォントアセット（Dynamic、TMP Settings の既定）で表示する。
- 未実装の機能と既知の不具合は `Docs/GameSpec.md` の8章を参照。

## コーディング規約（要点）

- private フィールドは camelCase、接頭辞なし。SerializeField も private。
- クラス名の接尾辞：`View` / `Presenter` / `EntryPoint` / `Repository`。Model は接尾辞なし。
- 名前空間は `TechC.KanjiQuiz.<フォルダ>`。モードは `TechC.KanjiQuiz.Modes.<ModeName>`。
- モード名：`StrokeOrder`, `RealKanji`, `ReplyMessage`, `FourCharIdiom`, `ImageName`。
- public メンバーには日本語の `/// <summary>`。ログは `[クラス名]` で始める。
- `GameObject.Find` / `FindObjectOfType` 禁止。シャッフルは共通化したものを使う。
- 詳細は `Docs/CodingRules.md`。

## Git と PR

- ブランチ：`<種類>/kono/<内容>`（例：`feature/kono/quiz-session`）。`main` に直接コミットしない。
- コミット：`<種類>:<日本語の内容>`。種類は `feat` / `fix` / `refactor` / `chore` / `docs` / `test`。
- PR の題名もコミットと同じ形式で、具体的に書く。説明は `.github/pull_request_template.md` の全項目を埋める。
- シーンやプレハブを変えたら、PR に変更したファイル名を書く。
- コミット・プッシュ・PR 作成はユーザーに頼まれたときだけ行う。

## Unity エディタの操作（Unity CLI）

- Unity CLI と Pipeline パッケージ（`com.unity.pipeline`）が導入済み。エディタが開いていれば CLI から操作できる。
- シーン・プレハブ・アセットを変更する前に `unity status` で接続を確認する。接続できるなら、`.unity` / `.prefab` の YAML を直接編集せずエディタ経由で操作する。
- `unity command` には `--caller plugin --skill <スキル名>` をつける。
- パッケージ追加やコンパイルの直後はエディタが応答しないことがある。`unity status --until-ready` で待ってから次の操作をする。
- コンパイルエラーの確認：`unity command console_status` の `compilationFailed` と、`unity command console --level error`。
- **エディタのダイアログで CLI が止まる**：ブランチを切り替えてシーンファイルが書き換わると、「The open scene(s) have been modified externally」ダイアログが出てメインスレッドが止まる。`unity status` は ready のままなのに `unity command` がすべてタイムアウトする場合はこれを疑い、ユーザーに Unity の画面を確認してもらう。
- `eval` では `using` が効かない。型は完全修飾名で書き、拡張メソッドは静的メソッドとして呼ぶ（例：`R3.ObservableSubscribeExtensions.Subscribe(p, x => ...)`）。

## ハマりどころ

- **R3 と Microsoft.Bcl.AsyncInterfaces**：NuGetForUnity は Unity 6 で AsyncInterfaces を入れずにスキップする。`Assets/Packages/` の DLL を消さないこと。入れ直すときは `NugetForUnity.PackageRestorer.Restore(true)` を実行する。
- **TMP の Dynamic フォント**：Play するとエディタ上でフォントアセットに文字が追加され、差分が出る。コミット前に `ClearFontAssetData(true)` で消す。
- **Canvas は Screen Space - Camera**：書き順の線（LineRenderer）を UI より手前に出すため、`CumulativeStrokeRenderer` の `sortingOrder` を 1 以上にしている。UI 要素の z 座標が 0 以外だと遠近法で小さく見えるので注意。
- **Play モードの確認**：エディタが非アクティブだとフレームが進まない。`unity command set_autotick` と、Play 中に `Application.runInBackground = true` を設定する。
- **KanjiVG の SVG**：`Assets/StreamingAssets/KanjiVG/` に基本の字形だけを置いている。ファイル名はコードポイントを5桁の小文字16進数にしたもの（「漢」→ `06f22.svg`）。楷書体などの異体字ファイルは使わないので含めていない。
