using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 画面の文字（TextMeshPro）のうち、読みの印（{漢字|よみ}）がついたものを、漢字かひらがなにして出す
    /// シーンやプレハブの文字にも、コードが入れる文字にも、印をつけておくだけでよい
    /// 毎フレームの最後（描く前）に、出ている文字を見て書き直すので、印のついた文字が一瞬でも画面に出ることはない
    /// どのシーンにも置いていない。起動時に <see cref="RubyLabelBootstrap"/> が1つ作ってつなぐ
    /// </summary>
    public sealed class RubyLabelView : MonoBehaviour, IRubyLabelView
    {
        private readonly RubyLabelTracker<TMP_Text> tracker = new RubyLabelTracker<TMP_Text>();
        private readonly List<TMP_Text> labelBuffer = new List<TMP_Text>();

        private static RubyLabelView instance;

        private void Awake()
        {
            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        /// <summary>
        /// 毎フレームの最後を待たずに、このラベルをすぐ書き直す
        /// 文字の数や位置を読む処理（チョークで1字ずつ書くなど）が、印のついた文字を読まないようにするために使う
        /// </summary>
        public static void Flush(TMP_Text label)
        {
            if (instance != null && label != null) instance.Apply(label);
        }

        /// <inheritdoc />
        public void SetHiragana(bool useHiragana)
        {
            tracker.SetUseHiragana(useHiragana);

            // 今は消してある（出ていない）ラベルも、あとで出たときに合っているように書き直す
            foreach (var label in tracker.Keys())
            {
                if (label == null)
                {
                    tracker.Forget(label);
                    continue;
                }
                Apply(label);
            }
        }

        private void LateUpdate()
        {
            var labels = FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            labelBuffer.Clear();
            labelBuffer.AddRange(labels);
            foreach (var label in labelBuffer) Apply(label);
        }

        private void Apply(TMP_Text label)
        {
            string next = tracker.Sync(label, label.text);
            if (next != null) label.text = next;
        }
    }
}
