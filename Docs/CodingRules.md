# 漢字クイズ コーディング規約

| 項目 | 内容 |
|---|---|
| 状態 | 確定 |
| 最終更新 | 2026-10-24 |
| 関連 | [Architecture.md](Architecture.md) |

既存のコードの書き方をもとに、迷いやすい点だけを決める。

---

## 1. C# の命名規則

| 対象 | 書き方 | 例 |
|---|---|---|
| 名前空間 | `TechC.KanjiQuiz.<フォルダ>`（3章） | `TechC.KanjiQuiz.Modes.StrokeOrder` |
| クラス・構造体・enum | PascalCase | `QuizSession`, `StrokeData`, `Difficulty` |
| インターフェース | `I` + PascalCase | `IQuestionRepository` |
| public メソッド・プロパティ | PascalCase | `RecordAnswer()`, `CurrentIndex` |
| private メソッド | PascalCase | `SetupQuestion()` |
| private フィールド | camelCase（接頭辞なし） | `correctAnswerIndex` |
| SerializeField のフィールド | private + camelCase | `[SerializeField] private Button nextButton;` |
| 定数 | PascalCase | `const int MaxChoices = 4;` |
| ローカル変数・引数 | camelCase | `selectedIndex` |
| bool | `is` / `has` / `can` で始める | `isCorrect`, `hasAnswered` |
| 非同期メソッド | 末尾に `Async` | `ShowQuestionAsync()` |
| イベント | `On` + 過去形・名詞 | `OnAnswered` |
| イベントを受けるメソッド | `On` + 対象 + 動詞の過去分詞 | `OnNextButtonClicked()` |

- 英語で書く。モード名などの和名は使わない（「四字熟語」は `FourCharIdiom`）。
- 例外として、テストメソッドの名前は日本語でよい（例：`選択するまで次へを押せない()`）。テストの意図を読みやすくするため。

### 1.1 MVP のクラス名

| 役割 | 名前の付け方 | 例 |
|---|---|---|
| Model | 内容を表す名詞（接尾辞なし） | `QuizSession`, `StrokeOrderQuestion` |
| View | 末尾に `View` | `QuizFrameView`, `ChoiceItemView` |
| View のインターフェース | `I` + View 名 | `IQuizFrameView` |
| Presenter | 末尾に `Presenter` | `StrokeOrderPresenter` |
| 組み立て役 | 末尾に `EntryPoint` | `StrokeOrderEntryPoint` |
| データの読み込み口 | 末尾に `Repository` | `LocalQuestionRepository` |

### 1.2 R3 の命名

| 対象 | 書き方 | 例 |
|---|---|---|
| View が公開するイベント | `On` + 名詞 + 過去分詞、型は `Observable<T>` | `Observable<Unit> OnNextClicked` |
| Model が公開する値 | 名詞、型は `ReadOnlyReactiveProperty<T>` | `ReadOnlyReactiveProperty<int> Score` |
| Model 内部の値 | private + camelCase、型は `ReactiveProperty<T>` | `private readonly ReactiveProperty<int> score` |
- 略語は一般的なものだけ使う（`UI`, `Id`, `Svg` など）。2文字の略語も PascalCase の中では先頭だけ大文字にする（`SvgPath`）。

### 1.3 今のコードで直すもの

- `StrokeOrderQuizMode.ConfirmationScreen` は camelCase にする（`confirmationScreen`）。
  - SerializeField の名前を変えると Inspector の参照が外れる。`[FormerlySerializedAs("ConfirmationScreen")]` をつけて変える。
- `GameContext` は Core フォルダにあるのに名前空間が `Data` になっている。フォルダに合わせる。

---

## 2. コードの書き方

- 1ファイルに1クラス。ファイル名とクラス名を同じにする。
- 名前空間はフォルダ構成と一致させる。
- 他のオブジェクトへの参照は EntryPoint の SerializeField で Inspector から渡す。`GameObject.Find` や `FindObjectOfType` は使わない。
- 調整したい数値は SerializeField か定数にする。コードに直接数字を書かない。
- `Random` は `UnityEngine.Random` に統一する。シャッフルは `ShuffleUtility` を使う。
- シングルトンにする MonoBehaviour は `SingletonMonoBehaviour<T>` を継承する。Awake・OnDestroy の代わりに `OnAwake`・`OnRelease` を上書きする。Presenter からはインターフェース（`IAudioPlayer` など）経由で使い、`Instance` を直接参照するのは EntryPoint だけにする。
- キーボードの入力は、その操作を持つ View が `Keyboard.current` で受け取る。中断メニューを開いている間などに止まるよう、`InputGate.IsBlocked` を確かめる。EventSystem の決定・移動・キャンセルは使わない（外してある）。
- 新しいモードのシーンはシーンテンプレート「漢字クイズのモード」から作り、Canvas は Screen Space - Camera にする（GameSpec.md 4.6）。モード名・問題数・保存のキーはモードの定義（`QuizModeDefinition`）に書き、EntryPoint に直接書かない。
- イベントは R3 の Observable で受け渡す（View の入力、Model の値の変化）。全体で共有するイベントの管理クラスは、複数の画面で同じイベントを受け取る必要が出たときに作る。
- 非同期処理は UniTask で書き、`CancellationToken` を必ず受け取る。
- `async void` は使わない。投げっぱなしにする場合は `UniTaskVoid` と `.Forget()` を使う。

### 2.1 MVP のルール

- View に判断を書かない。「正解かどうか」「次に何をするか」は Presenter が決める。
- View は Presenter と Model を参照しない。入力は Observable で公開するだけにする。
- Model は UnityEngine の UI や MonoBehaviour を使わない。
- Presenter と Model は MonoBehaviour にしない。作成と破棄は EntryPoint が行う。
- View を参照するのは EntryPoint の SerializeField だけ。Presenter にはコンストラクタで渡す。

### 2.2 R3 のルール

- Presenter の購読は `CompositeDisposable` にまとめ、`Dispose` で解除する。
- View の中の購読は `AddTo(this)` で、GameObject の破棄と一緒に解除する。
- `ReactiveProperty` は外に公開しない。公開するときは `ReadOnlyReactiveProperty` にする。
- 時間を待つ処理は R3 ではなく UniTask で書く。

### 2.3 コメントとログ

- public なクラスとメソッドには、日本語で `/// <summary>` を書く。
- ログには `[クラス名]` を先頭につける。
  - 例：`Debug.Log("[QuizSession] 回答を記録しました");`
- 開発用のログはリリース前に整理する。

### 2.4 メンバーの並び順

1. SerializeField のフィールド
2. private フィールド
3. プロパティ
4. Unity のイベント関数（`Awake` → `OnEnable` → `Start` → `Update` → `OnDisable` → `OnDestroy`）
5. public メソッド
6. private メソッド

---

## 3. フォルダ構成

```
Assets/
├─ StreamingAssets/
│   └─ KanjiVG/              … 書き順の SVG
└─ TechC/KanjiQuiz/
    ├─ Art/                  … 画像、フォント、音
    ├─ Data/
    │   ├─ KanjiSets/        … 出題範囲の漢字セット JSON（Tools で生成）
    │   └─ Questions/        … 問題セット JSON（予定）
    ├─ Prefabs/
    │   ├─ Common/           … 全モードで使う UI
    │   └─ Modes/<モード名>/
    ├─ Scenes/
    ├─ Scripts/
    │   ├─ Common/           … 全モード共通
    │   │   ├─ Models/       … QuizSession、AnswerRecord、Repository
    │   │   ├─ Views/        … QuizFrameView、ChoiceListView
    │   │   ├─ Presenters/   … QuizPresenterBase、ChoiceQuizPresenter
    │   │   └─ Utility/      … SceneLoader、ShuffleUtility
    │   ├─ Modes/<モード名>/
    │   │   ├─ Models/
    │   │   ├─ Views/
    │   │   ├─ Presenters/
    │   │   └─ <モード名>EntryPoint.cs
    │   ├─ Screens/<画面名>/ … Title、ModeSelect、Result（同じく MVP）
    │   └─ DebugTools/       … テスト用の機能（エディタと開発ビルドのみ）
    └─ Tests/EditMode/
        ├─ Fakes/            … テスト用の偽物の View など
        └─ <対象>/           … StrokeOrder、Result、Review など
Tools/                       … データを作るスクリプト（Unity の外で実行）
```

- モード名のフォルダは `StrokeOrder`, `RealKanji`, `ReplyMessage`, `FourCharIdiom`, `ImageName` とする。
- 名前空間は `TechC.KanjiQuiz.Modes.StrokeOrder` のように、`Models` などの1つ上の階層までにする。

---

## 4. アセットの命名

| 種類 | 書き方 | 例 |
|---|---|---|
| シーン | PascalCase | `ModeSelect.unity` |
| プレハブ | PascalCase（役割がわかる名前） | `ChoiceButton.prefab` |
| 画像 | `種類_名前` | `bg_title.png`, `icon_correct.png` |
| 問題セット | `モード_内容_番号` | `ReplyMessage_Basic_001.json` |
| ヒエラルキーの GameObject | PascalCase | `NextButton`, `ChoiceContainer` |

---

## 5. Git の運用

### 5.1 ブランチ

| ブランチ | 用途 |
|---|---|
| `main` | 動く状態だけを置く |
| `<種類>/kono/<内容>` | 作業用。既存の `refactor/kono/program` と同じ形 |

例：`feature/kono/quiz-session`、`fix/kono/stroke-cancel`

### 5.2 コミットメッセージ

`<種類>:<日本語の内容>` の形で書く。既存のコミットと同じ形にする。

| 種類 | 使う場面 |
|---|---|
| `feat` | 機能の追加 |
| `fix` | 不具合の修正 |
| `refactor` | 動きを変えないコードの整理（今までの `logic` もこれに含める） |
| `chore` | 設定、パッケージ、アセットの追加など |
| `docs` | ドキュメント |
| `test` | テストの追加・修正 |

例：`feat:問題の進行を管理するQuizSessionを作成`

### 5.3 プルリクエスト（PR）

作業用ブランチは PR を作ってから `main` にマージする。

**題名**

- コミットメッセージと同じ `<種類>:<日本語の内容>` の形で書く。
- 何が変わるのかが題名だけでわかるように書く。「修正」「対応」だけの題名にしない。

| 良い例 | 悪い例 |
|---|---|
| `feat:全モード共通の出題・回答の流れを作成` | `feat:作業` |
| `fix:問題を切り替えたときに前の書き順の描画が残る不具合を修正` | `fix:バグ修正` |

**説明**

[.github/pull_request_template.md](../.github/pull_request_template.md) の項目を埋める。GitHub で PR を作ると自動で入る。

| 項目 | 書くこと |
|---|---|
| 概要 | この PR で何をしたか、なぜ必要かを2〜3文で |
| 変更内容 | 主な変更を箇条書きで |
| 動作確認 | 確認した手順と結果。確認できていないことも書く |
| スクリーンショット | 画面が変わる場合は変更前後の画像や動画 |
| 関連 | 対応する仕様書の章や Issue |
| 残課題 | この PR でやらなかったこと、次にやること |

- 1つの PR には1つの目的だけを入れる。大きくなりそうなら分ける。
- シーンやプレハブを変えた場合は、どのシーン・プレハブを変えたかを書く（差分が読みにくいため）。
