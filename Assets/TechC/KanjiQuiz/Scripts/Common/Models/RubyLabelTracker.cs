using System;
using System.Collections.Generic;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 画面の文字（ラベル）のうち、読みの印（{漢字|よみ}）がついたものを覚えておき、
    /// 漢字のままか、ひらがなかを合わせる
    /// ラベルが何かは知らない（Unity に依存しない）。ラベルごとに、今の文字を渡して「書き直す文字」を聞く
    /// </summary>
    /// <typeparam name="TKey">ラベルを区別するもの（TMP_Text など）</typeparam>
    public sealed class RubyLabelTracker<TKey> where TKey : class
    {
        private sealed class Entry
        {
            public string Marked;
            public string Rendered;
            public bool UseHiragana;
        }

        private readonly Dictionary<TKey, Entry> entries = new Dictionary<TKey, Entry>();
        private bool useHiragana;

        /// <summary>今、ひらがなにしているかどうか</summary>
        public bool UseHiragana => useHiragana;

        /// <summary>覚えているラベルの数</summary>
        public int Count => entries.Count;

        /// <summary>ひらがなにするかどうかを変える。書き直しは Sync で、ラベルごとにする</summary>
        public void SetUseHiragana(bool value)
        {
            useHiragana = value;
        }

        /// <summary>覚えているラベルの一覧（Sync しながら回すときのために、写しを返す）</summary>
        public List<TKey> Keys() => new List<TKey>(entries.Keys);

        /// <summary>ラベルを忘れる（消えたラベルなど）</summary>
        public void Forget(TKey key)
        {
            if (key != null) entries.Remove(key);
        }

        /// <summary>
        /// ラベルの今の文字を見て、書き直す文字を返す
        /// - 印がついた文字が新しく入っていたら、今の設定で書き直す
        /// - 前に書き直した文字のままで、設定が変わっていたら、書き直す
        /// - ほかのコードが文字を入れ替えていたら（前に書き直した文字と違う）、その文字を新しいものとして見る
        /// </summary>
        /// <returns>書き直す文字。直すところがなければ null</returns>
        public string Sync(TKey key, string currentText)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            if (entries.TryGetValue(key, out var entry))
            {
                if (currentText == entry.Rendered)
                {
                    if (entry.UseHiragana == useHiragana) return null;
                    entry.Rendered = RubyText.Render(entry.Marked, useHiragana);
                    entry.UseHiragana = useHiragana;
                    return entry.Rendered;
                }
                entries.Remove(key);
            }

            if (!RubyText.HasMark(currentText)) return null;

            string rendered = RubyText.Render(currentText, useHiragana);
            entries[key] = new Entry { Marked = currentText, Rendered = rendered, UseHiragana = useHiragana };
            return rendered;
        }
    }
}
