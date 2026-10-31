using System;
using System.Collections.Generic;
using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 1つのゲームモードの設定（名前、シーン、難易度ごとの問題数など）
    /// モードごとにアセットを作り、そのモードの EntryPoint と、モード選択画面が読む
    /// </summary>
    [CreateAssetMenu(menuName = "KanjiQuiz/モードの定義", fileName = "Mode_")]
    public sealed class QuizModeDefinition : ScriptableObject
    {
        [Tooltip("モードを表す ID。保存のキーに使うので、一度決めたら変えない（例：StrokeOrder）")]
        [SerializeField] private string modeId;
        [Tooltip("画面に出す名前（例：書き順クイズ）")]
        [SerializeField] private string displayName;
        [Tooltip("モード選択画面に出す説明")]
        [TextArea]
        [SerializeField] private string description;
        [Tooltip("プレイ画面のシーンの名前。Build Profiles のシーンリストに登録しておく")]
        [SerializeField] private string sceneName;
        [Tooltip("遊べるかどうか。まだ作っていないモードは外し、モード選択画面で「準備中」として出す")]
        [SerializeField] private bool isAvailable = true;
        [Tooltip("難易度を選べるかどうか")]
        [SerializeField] private bool hasDifficulty = true;
        [Header("1回のプレイの問題数")]
        [Min(1)] [SerializeField] private int easyQuestionCount = 5;
        [Min(1)] [SerializeField] private int normalQuestionCount = 7;
        [Min(1)] [SerializeField] private int hardQuestionCount = 7;
        [Header("出題範囲の説明（モード選択画面に出す）")]
        [SerializeField] private string easyRange = "小学漢字";
        [SerializeField] private string normalRange = "常用漢字（よく使う）";
        [SerializeField] private string hardRange = "常用漢字（あまり使わない）";

        private static readonly Difficulty[] AllDifficulties = { Difficulty.Easy, Difficulty.Normal, Difficulty.Hard };
        private static readonly Difficulty[] EasyOnly = { Difficulty.Easy };

        /// <summary>モードを表す ID（保存のキーに使う）</summary>
        public string ModeId => modeId;

        /// <summary>画面に出す名前</summary>
        public string DisplayName => displayName;

        /// <summary>モード選択画面に出す説明</summary>
        public string Description => description;

        /// <summary>プレイ画面のシーンの名前</summary>
        public string SceneName => sceneName;

        /// <summary>遊べるかどうか（false ならモード選択画面で「準備中」）</summary>
        public bool IsAvailable => isAvailable;

        /// <summary>難易度を選べるかどうか</summary>
        public bool HasDifficulty => hasDifficulty;

        /// <summary>選べる難易度。難易度がないモードは Easy だけ</summary>
        public IReadOnlyList<Difficulty> Difficulties => hasDifficulty ? AllDifficulties : EasyOnly;


        /// <summary>出題範囲の説明（例：小学漢字）</summary>
        public string Range(Difficulty difficulty)
        {
            if (!hasDifficulty) return easyRange;

            switch (difficulty)
            {
                case Difficulty.Normal: return normalRange;
                case Difficulty.Hard: return hardRange;
                default: return easyRange;
            }
        }

        /// <summary>1回のプレイの問題数</summary>
        public int QuestionCount(Difficulty difficulty)
        {
            if (!hasDifficulty) return easyQuestionCount;

            switch (difficulty)
            {
                case Difficulty.Normal: return normalQuestionCount;
                case Difficulty.Hard: return hardQuestionCount;
                default: return easyQuestionCount;
            }
        }

        /// <summary>
        /// 1回のプレイで優先して出す苦手な問題の数の上限（問題数の半分）
        /// 苦手な問題ばかりにならないように、残りは新しい問題にする
        /// </summary>
        public int WeakQuestionCount(Difficulty difficulty)
        {
            return QuestionCount(difficulty) / 2;
        }

        /// <summary>画面と結果に出す名前（例：書き順クイズ（簡単））</summary>
        public string Title(Difficulty difficulty)
        {
            return hasDifficulty ? $"{displayName}（{difficulty.ToDisplayName()}）" : displayName;
        }

        /// <summary>自己ベストと苦手な問題を保存するときのキー。難易度ごとに分ける（例：StrokeOrder_Easy）</summary>
        public string RecordKey(Difficulty difficulty)
        {
            return hasDifficulty ? $"{modeId}_{difficulty}" : modeId;
        }

        /// <summary>
        /// コードから作る（テスト用）。ゲームではアセットを作って Inspector で設定する
        /// </summary>
        public static QuizModeDefinition Create(
            string modeId, string displayName, string sceneName, bool hasDifficulty = true,
            int easyQuestionCount = 5, int normalQuestionCount = 7, int hardQuestionCount = 7,
            bool isAvailable = true, string description = "")
        {
            if (string.IsNullOrEmpty(modeId)) throw new ArgumentException("ID が空です。", nameof(modeId));

            var definition = CreateInstance<QuizModeDefinition>();
            definition.modeId = modeId;
            definition.displayName = displayName;
            definition.description = description;
            definition.sceneName = sceneName;
            definition.isAvailable = isAvailable;
            definition.hasDifficulty = hasDifficulty;
            definition.easyQuestionCount = easyQuestionCount;
            definition.normalQuestionCount = normalQuestionCount;
            definition.hardQuestionCount = hardQuestionCount;
            return definition;
        }
    }
}
