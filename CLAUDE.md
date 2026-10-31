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
| `Docs/Progress.md` | 進行状況、スケジュール、次にやること、PR の履歴 |
| `Docs/BugFixes.md` | バグ修正の記録（症状・原因・調べ方・直し方・結果・学んだこと）。就活で説明できるように、直した不具合をここに書く |

設計や仕様を変えたら、対応する Docs も同じ作業の中で更新する。
作業が終わったら（PR を作るとき）、`Docs/Progress.md` のチェック・PR の履歴・次にやること・最終更新日も更新する。

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
- 問題データは JSON。`IQuestionRepository` 経由で読む（同梱の JSON は `LocalQuestionRepository<TEntry>`。`TEntry` は問題1件の [Serializable] な型）。
- 非同期は UniTask。問題ごとに `CancellationTokenSource` を作り直す。`async void` 禁止。

## 現在の状態（2026-10-30 時点）

- **遊べるのは書き順クイズと本物の漢字さがし**。タイトル（黒板にチョークで書くロゴ）、モード選択（教室の時間割表）、難易度選択（黒板）もできた。ほかの3モードは未着手。次にやることは `Docs/Progress.md` の9章。返信文クイズは後回し（正しい返信文を作るのが難しいため）。
- MVP の共通部分は `Assets/TechC/KanjiQuiz/Scripts/Common/`（`QuizSession`, `QuizPresenterBase`, `ChoiceQuizPresenter`, `ChoiceQuizReviewPresenter`, `QuizFrameView`, `ChoiceListView`, `QuizReviewView` など）。
  - KanjiVG の SVG を読む部品（`KanjiSvgParser`, `StrokePathParser`, `StrokeData`, `StreamingAssetsKanjiStrokeSource`）と、字を線で描く `KanjiGlyphRenderer`、熟語の辞書 `KanjiWordDictionary` も Common にある。
  - ゲーム本体は `TechC.KanjiQuiz` アセンブリ、EditMode テストは `Assets/TechC/KanjiQuiz/Tests/EditMode/`（`TechC.KanjiQuiz.Tests`、198 件）。
- 書き順クイズは `Assets/TechC/KanjiQuiz/Scripts/Modes/StrokeOrder/`。
  - シーンの `GameManager` にある `StrokeOrderEntryPoint` が組み立て役。難易度・難易度ごとの漢字セット・選択肢の数・振り返り用の View もここで設定する。
  - 難易度はモード選択画面の黒板で選び、`QuizLaunchOptions.Difficulty` で受け取る（なければ EntryPoint の Inspector の値）。F1 キーで表示されるテスト用ボタン（`Scripts/DebugTools/DebugDifficultySwitcher`、エディタと開発ビルドのみ）で切り替えられる。シーンに保存する難易度は Easy にしておく。
  - 出題範囲は `Assets/TechC/KanjiQuiz/Data/KanjiSets/` の JSON（簡単＝小学漢字 `Elementary`、常識＝小学校で学ばない常用漢字のうちよく使う半分 `JoyoCommon`、困難＝あまり使わない半分 `JoyoRare`）。**人名用漢字はほかのモードに混ぜない**（数が多く何でもありになるため）。常用漢字以外の `Hyogai` は、あとで作る人名漢字のモード用に残しているだけで、どの難易度にも使わない。`Tools/generate_kanji_sets.py` で KANJIDIC2 から作る。手で編集しない。
  - 不正解の作り方：簡単は全画シャッフル、常識は連続した30%の画を必ず動かす、困難は隣り合う2画を入れ替える（`WrongOrderGenerator`）。
  - 書き順データは `Assets/StreamingAssets/KanjiVG/` の SVG（基本の字形のみ 6,701 ファイル）を UnityWebRequest で読む。
  - 書き順の図は1段階ごとに黒い四角で囲む。枠の太さ・色は選択肢プレハブ（`ProblemDisplay`）の `CumulativeStrokeRenderer` で設定する。
- 本物の漢字さがしは `Assets/TechC/KanjiQuiz/Scripts/Modes/RealKanji/`（仕様は `Docs/GameSpec.md` 2.1）。
  - 本物1つと、同じ部首を残して部首以外を入れ替えた偽物3つの4択。2×2 に並べ、字の下に読みを出す。難易度は書き順クイズと同じく出題範囲で変える。
  - 問題データは `Data/RealKanji/`（`Tools/generate_real_kanji_fakes.py`）、振り返りに出す熟語は `Data/KanjiWords/`（`Tools/generate_kanji_words.py`、JMdict から）の JSON。どちらも手で編集しない。
  - 偽物は `FakeKanjiComposer` が、本物の SVG の部首のグループの画に、借りた部品の画を問題データの `box`（部品を置く枠）に合わせて足して作る。`box` は Python が、部首と部品の線が重ならないように求めたもの（`Docs/BugFixes.md` 1章）。4つとも `KanjiGlyphRenderer` で線で描く（本物だけフォントにすると見抜けるため）。
  - シーンは `RealKanji.unity`（ひな形から作った）、組み立て役は `GameManager` の `RealKanjiEntryPoint`、選択肢は `Prefabs/Modes/RealKanji/RealKanjiChoice`、モードの定義は `Mode_RealKanji`。保存のキーは `BestRecord_RealKanji_<難易度>`・`WeakQuestions_RealKanji_<難易度>`（Play で確かめたあとは消す）。
- タイトルとモード選択は `Assets/TechC/KanjiQuiz/Scripts/Screens/Title/`（`Title.unity`、組み立て役は `TitleEntryPoint`）。タイトル → 時間割 → 黒板を1つのシーンで切り替える（仕様は `Docs/ScreenDesign.md` 3.1〜3.3）。
  - 時間割は `QuizModeCatalog` の順に並べる。まだ作っていないモード（四字熟語づくり・画像で名前当て・返信文クイズ・人名漢字 `JinmeiKanji`）は、モードの定義の `isAvailable` を外して「自習」と出す。モードを作ったら `isAvailable` を入れる。
  - 中断メニューの [やめる] と結果画面の [モード選択へ] は `TitleLaunchOptions.OpenModeSelect` を立てて Title を開く（時間割から始まる）。
  - 黒板（Board）は1枚をタイトル・時間割の横・画面いっぱいで使い回す。ロゴは KanjiVG の書き順で `ChalkStrokeGraphic`（UI の線）に書く。時間割の横の黒板の字は `ChalkTextWriter` で1字ずつ出す。演出はクリックか Enter・スペースで飛ばせる（`SkipInput`）。
- シーンは `Assets/TechC/KanjiQuiz/Scenes/` の `Title.unity`（シーンリストの先頭）、`StrokeOrder.unity`、`RealKanji.unity` と `Result.unity`（全モード共通の結果画面）。
  - 全問回答すると `QuizResultStore` に結果、`QuizReviewStore` に問題と回答を置いて Result へ移る。
  - 結果画面の [問題を振り返る]（または一覧の問題）で、モードのシーンを振り返りとして開き直す。
  - 結果画面は、1問ずつ ○×（ハンコの画像）→ 正解数のルーレット → 正解数 → 級のハンコ → 全問正解なら花丸 → 自己ベスト、の順に効果音つきで見せる（クリックで飛ばせる。振り返りから戻ったときは演出しない）。
  - 級は `KanjiRank`、自己ベストは `QuizResult.UpdateBestRecord` と `PlayerPrefsBestRecordRepository`（キー `BestRecord_StrokeOrder_<難易度>`）。Play で確かめたあとは、残った自己ベストを `PlayerPrefs.DeleteKey` で消す。
  - プレイ中は、選択・ページめくりの音と、問題を切り替えるときのスライド（`QuizFrameView` の `transitionTargets`）がある。
  - 振り返りでは、出題する漢字の右にお手本の書き順アニメーション（`StrokeAnimationView`）を出す。
  - 間違えた漢字は `WeakQuestionList`（PlayerPrefs のキー `WeakQuestions_StrokeOrder_<難易度>`）に残し、次のプレイで問題数の半分まで優先して出す。出たときは「苦手」のハンコを出す。Play で確かめたあとは、このキーも `PlayerPrefs.DeleteKey` で消す。
- 背景は学校のプリントのような画像（各 Canvas の Background）。シーンの切り替えは `SceneTransitionView`（`Prefabs/Common/SceneTransition`、プリントを回収して次を配る）で、`new SceneLoader(SceneTransitionView.Instance)` のように EntryPoint で渡す。
- **新しいモードのシーンは File > New Scene のテンプレート「漢字クイズのモード」から作る**（`Scenes/Templates/`、手順は `Docs/GameSpec.md` 4.6）。共通の部品は `Prefabs/Common/` のプレハブ（QuizFrame、PauseMenu、PrintBackground、AudioManager、SceneTransition）。
- モード名・シーン名・難易度ごとの問題数・保存のキーは `Data/Modes/` のモードの定義（`QuizModeDefinition`）に書く。書き順クイズは `Mode_StrokeOrder`、本物の漢字さがしは `Mode_RealKanji`（各 EntryPoint の `mode` に設定）。
- 中断メニュー（Esc か右上の [メニュー]）で、続ける・最初からやり直す・やめる（時間割へ）・効果音の音量。音量は PlayerPrefs の `Volume_Se`。キーボード操作は数字キー・Enter・← →・Esc。メニューを開いている間とシーンの切り替え中は `InputGate` でキー操作を止める。
- 切り替えはカメラの映像をテクスチャに描いて使うので、**新しいシーンの Canvas は Screen Space - Camera にする**（Overlay の UI はプリントに映らない）。Title・StrokeOrder・RealKanji・Result はどれも Camera にしてある。
- 効果音は `AudioManager`（`Prefabs/Common/AudioManager`、Title・StrokeOrder・RealKanji・Result のシーンに置いている）。シングルトンは `SingletonMonoBehaviour<T>` を継承して作る。今の効果音と画像は `Tools/generate_placeholder_se.py`・`Tools/generate_placeholder_ui.py` で作った仮のもの。
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
- エディタが起動していなければ（`unity status` が `STATUS_NO_INSTANCES`）、`unity open .` で開き、`unity status --until-ready` で待つ。
- シーン・プレハブ・アセットを変更する前に `unity status` で接続を確認する。接続できるなら、`.unity` / `.prefab` の YAML を直接編集せずエディタ経由（`unity command eval` で SerializedObject を使う）で操作する。
- `unity command` には `--caller plugin --skill <スキル名>` をつける。
- パッケージ追加やコンパイルの直後はエディタが応答しないことがある。`unity status --until-ready` で待ってから次の操作をする。
- コンパイルエラーの確認：`unity command console_status` の `compilationFailed` と、`unity command console --level error`。
- テストの実行：`unity command run_tests --mode EditMode --filter TechC.KanjiQuiz.Tests`。
- **エディタのダイアログで CLI が止まる**：ブランチを切り替えてシーンファイルが書き換わると、「The open scene(s) have been modified externally」ダイアログが出てメインスレッドが止まる。`unity status` は ready のままなのに `unity command` がすべてタイムアウトする場合はこれを疑い、ユーザーに Unity の画面を確認してもらう。ブランチは `git checkout -b <新ブランチ> origin/main` のように、作業ツリーが変わらない形で切ると起きにくい。
- `eval` では `using` が効かない。型は完全修飾名で書き、拡張メソッドは静的メソッドとして呼ぶ（例：`R3.ObservableSubscribeExtensions.Subscribe(p, x => ...)`）。
- 長い `eval` のコードはファイルに書いてから `"$(cat ファイル)"` で渡す。Bash のヒアドキュメントに複雑な引用符を入れると解析に失敗することがある。

### Play モードでの動作確認の手順

1. `unity command set_autotick` を一度実行し、`editor_play` で Play を始める。
2. Play 中に `eval` で `UnityEngine.Application.runInBackground = true;` を設定する（エディタが非アクティブだとフレームが進まないため）。
3. ボタンは `eval` で `onClick.Invoke()`、選択肢は `Toggle.isOn = true` で操作する。キー操作は `InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Digit2))` で押し、次の `eval` で `new KeyboardState()` を送って離す。クリックの当たり判定まで確かめるときは `EventSystem.RaycastAll` と `ExecuteEvents.ExecuteHierarchy` を使う。
4. 画面は `unity command capture_game_view -- --save_path "Temp/x.png"` で撮る。**保存先は `Assets/Temp/` になる**ので、確認後に `AssetDatabase.DeleteAsset("Assets/Temp")` で消す。
5. `editor_stop` で止め、`unity status --until-ready` で待つ。止めた直後のシーン編集は取り消されることがあるので、`EditorApplication.isPlayingOrWillChangePlaymode` が false になってから編集する。
6. 最後に TMP フォントアセットの文字データを消す（ハマりどころを参照）。

### コミットと PR

- 実装ごとにコミットを分ける（パッケージ、Model、View、Presenter、シーン、テスト、ドキュメントなど）。
- SSH の鍵が使えずプッシュできないときは、GitHub CLI の認証で HTTPS を使う（Git の設定は変えない）。

```bash
git -c credential.helper= -c "credential.helper=!gh auth git-credential" push https://github.com/NEKOMARU23/Knaji-Quiz.git <ブランチ>
```

## ハマりどころ

- **R3 と Microsoft.Bcl.AsyncInterfaces**：NuGetForUnity は Unity 6 で AsyncInterfaces を入れずにスキップする。`Assets/Packages/` の DLL を消さないこと。入れ直すときは `NugetForUnity.PackageRestorer.Restore(true)` を実行する。
- **TMP の Dynamic フォント**：Play するとエディタ上でフォントアセットに文字が追加され、差分が出る。コミット前に `ClearFontAssetData(true)` で消す。
- **Canvas は Screen Space - Camera**：書き順の線（LineRenderer）を UI より手前に出すため、`CumulativeStrokeRenderer` の `sortingOrder` を 1 以上にしている。UI 要素の z 座標が 0 以外だと遠近法で小さく見えるので注意。
- **Play モードの確認**：エディタが非アクティブだとフレームが進まない。`unity command set_autotick` と、Play 中に `Application.runInBackground = true` を設定する。
- **`GetComponent<T>() ?? AddComponent<T>()` が効かない**：エディタでは見つからないコンポーネントが「null に見えるオブジェクト」で返るので `??` で判定できない。`TryGetComponent` を使う。
- **コードの初期値を変えてもプレハブに反映されない**：エディタが読み込み済みのプレハブは古い値を持ち続ける。SerializedObject でプレハブに値を書き込んで保存する。
- **Play モードのままだとシーンを編集できない**：ユーザーが Play したままのことがある。`EditorApplication.isPlaying` を確認し、必要なら `editor_stop` してから編集する（止めたことはユーザーに伝える）。
- **`Scripts/Core/GameContext.cs`（未追跡）**：PR #3 で削除したファイルが、エディタで開いたままのタブから保存されて復活したもの。コミットしない。削除はユーザーに確認してから行う。
- **KanjiVG の SVG**：`Assets/StreamingAssets/KanjiVG/` に基本の字形だけを置いている。ファイル名はコードポイントを5桁の小文字16進数にしたもの（「漢」→ `06f22.svg`）。楷書体などの異体字ファイルは使わないので含めていない。
