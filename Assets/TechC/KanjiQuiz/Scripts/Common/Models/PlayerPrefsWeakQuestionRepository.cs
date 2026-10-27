using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 苦手な問題のリストを PlayerPrefs に保存する（改行区切りの文字列にする）
    /// </summary>
    public sealed class PlayerPrefsWeakQuestionRepository : IWeakQuestionRepository
    {
        private const string KeyPrefix = "WeakQuestions_";
        private const char Separator = '\n';

        public WeakQuestionList Load(string key)
        {
            string value = PlayerPrefs.GetString(KeyPrefix + key, string.Empty);
            if (string.IsNullOrEmpty(value)) return new WeakQuestionList();

            return new WeakQuestionList(value.Split(Separator));
        }

        public void Save(string key, WeakQuestionList list)
        {
            PlayerPrefs.SetString(KeyPrefix + key, string.Join(Separator.ToString(), list.Items));
            PlayerPrefs.Save();
        }
    }
}
