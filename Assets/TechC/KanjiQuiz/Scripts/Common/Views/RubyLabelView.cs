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

        /// <summary>ひらがなのために「枠に収まるまで縮める」にしたラベルと、元の設定（漢字のときに戻すため）</summary>
        private readonly Dictionary<TMP_Text, ShrinkState> shrunkLabels = new Dictionary<TMP_Text, ShrinkState>();

        private readonly struct ShrinkState
        {
            public readonly bool WasAutoSizing;
            public readonly float FontSize;
            public readonly float FontSizeMin;
            public readonly float FontSizeMax;
            public readonly TextOverflowModes Overflow;
            public readonly Vector4 Margin;

            public ShrinkState(TMP_Text label)
            {
                WasAutoSizing = label.enableAutoSizing;
                FontSize = label.fontSize;
                FontSizeMin = label.fontSizeMin;
                FontSizeMax = label.fontSizeMax;
                Overflow = label.overflowMode;
                Margin = label.margin;
            }
        }

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

            // 消えたラベルを忘れる
            foreach (var label in new List<TMP_Text>(shrunkLabels.Keys))
            {
                if (label == null) shrunkLabels.Remove(label);
            }
        }

        private void LateUpdate()
        {
            var labels = FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude);
            labelBuffer.Clear();
            labelBuffer.AddRange(labels);
            foreach (var label in labelBuffer) Apply(label);
        }

        private void Apply(TMP_Text label)
        {
            string next = tracker.Sync(label, label.text);
            if (next == null) return;

            label.text = next;
            Fit(label, tracker.UseHiragana);
        }

        /// <summary>
        /// ひらがなのときは、元の大きさを上限に、枠に収まるまで文字を縮める（長くなって折り返したり、はみ出したりしないように）
        /// もともと自動で大きさを変える設定のラベルは、縮める下限だけを下げる
        /// 漢字に戻したら、すべて元の設定に戻す
        /// </summary>
        private void Fit(TMP_Text label, bool shrink)
        {
            if (shrink)
            {
                if (shrunkLabels.ContainsKey(label)) return;

                var state = new ShrinkState(label);
                shrunkLabels[label] = state;
                if (state.WasAutoSizing)
                {
                    label.fontSizeMin = Mathf.Min(state.FontSizeMin, RubyLabelFit.MinSize(state.FontSizeMin));
                }
                else
                {
                    label.fontSizeMax = state.FontSize;
                    label.fontSizeMin = RubyLabelFit.MinSize(state.FontSize);
                    label.enableAutoSizing = true;
                }

                bool noWrap = label.textWrappingMode == TextWrappingModes.NoWrap;
                // 折り返さない文字は、はみ出しを Overflow にしたままだと、幅では縮まない（高さだけで決まる）。幅を見るように切り替える
                if (noWrap && label.overflowMode == TextOverflowModes.Overflow)
                {
                    label.overflowMode = TextOverflowModes.Ellipsis;
                }
                // 折り返さず、中央にそろえた文字（ハンコ・ボタン）は、枠の線にかからないように、左右に余白をとる
                // 折り返す文字は、余白で幅が狭くなって行が増えるので、とらない
                if (noWrap && label.horizontalAlignment == HorizontalAlignmentOptions.Center)
                {
                    float pad = RubyLabelFit.SidePadding(state.WasAutoSizing ? state.FontSizeMin : state.FontSize);
                    var margin = label.margin;
                    label.margin = new Vector4(margin.x + pad, margin.y, margin.z + pad, margin.w);
                }
            }
            else if (shrunkLabels.TryGetValue(label, out ShrinkState state))
            {
                label.enableAutoSizing = state.WasAutoSizing;
                if (state.WasAutoSizing)
                {
                    label.fontSizeMin = state.FontSizeMin;
                    label.fontSizeMax = state.FontSizeMax;
                }
                else
                {
                    label.fontSize = state.FontSize;
                }
                label.overflowMode = state.Overflow;
                label.margin = state.Margin;
                shrunkLabels.Remove(label);
            }
        }
    }
}
