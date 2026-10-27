using System;
using System.Collections.Generic;
using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// ゲームに同梱した問題セットの JSON（Data/ の TextAsset）から問題データを読む
    /// JSON の形：{ "id": "...", "questions": [ {...}, ... ] }
    /// </summary>
    /// <typeparam name="TEntry">問題データ1件の型。JsonUtility で読めるように [Serializable] にする</typeparam>
    public sealed class LocalQuestionRepository<TEntry> : IQuestionRepository<TEntry>
    {
        private readonly TEntry[] entries;

        /// <summary>問題セットの識別子（例：Elementary）</summary>
        public string SetId { get; }

        /// <param name="json">問題セットの JSON</param>
        public LocalQuestionRepository(string json)
        {
            if (string.IsNullOrEmpty(json)) throw new ArgumentException("JSON が空です。", nameof(json));

            var data = JsonUtility.FromJson<QuestionSetData>(json);
            if (data == null || data.questions == null)
            {
                throw new FormatException("問題セットの JSON の形式が正しくありません。");
            }
            SetId = data.id ?? string.Empty;
            entries = data.questions;
        }

        public IReadOnlyList<TEntry> LoadAll()
        {
            return entries;
        }

        [Serializable]
        private class QuestionSetData
        {
            public string id;
            public TEntry[] questions;
        }
    }
}
