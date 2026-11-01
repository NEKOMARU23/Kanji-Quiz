using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// タイトル画面の表示（黒板にチョークで書くタイトル）
    /// </summary>
    public interface ITitleView
    {
        /// <summary>[はじめる] が押されたとき（Enter を含む）</summary>
        Observable<Unit> OnStartClicked { get; }

        /// <summary>[おわる] が押されたとき</summary>
        Observable<Unit> OnQuitClicked { get; }

        /// <summary>[設定] が押されたとき</summary>
        Observable<Unit> OnSettingsClicked { get; }

        /// <summary>[漢字帳] が押されたとき</summary>
        Observable<Unit> OnKanjiBookClicked { get; }

        /// <summary>[クレジット] が押されたとき</summary>
        Observable<Unit> OnCreditsClicked { get; }

        /// <summary>演出の中で効果音を鳴らしてほしいとき（ロゴの1画を書き始めた、黒板消しを引き始めた）</summary>
        Observable<SeType> OnEffectSound { get; }

        /// <summary>タイトル画面を表示するかどうか</summary>
        void SetVisible(bool visible);

        /// <summary>
        /// ロゴを黒板にチョークで1画ずつ書き、書き終わったらメニューのマグネットを貼る
        /// クリックか Enter・スペースで飛ばすと、すぐに書き終えた状態にする
        /// </summary>
        /// <param name="logoStrokePaths">ロゴの字ごとの画のパスデータ（KanjiVG の d 属性、書き順の順）。空の字は線で書けないので、字の形のまま出す</param>
        UniTask PlayIntroAsync(IReadOnlyList<IReadOnlyList<string>> logoStrokePaths, CancellationToken ct);

        /// <summary>
        /// ロゴを書き終えてマグネットを貼った状態で、すぐにタイトルを出す（黒板に板書した画面から戻るとき）
        /// </summary>
        void ShowWithoutIntro(IReadOnlyList<IReadOnlyList<string>> logoStrokePaths);

        /// <summary>
        /// マグネットを外し、黒板消しでタイトルを消す
        /// </summary>
        /// <param name="pressed">押されたメニュー（押した手応えを見せる）</param>
        /// <returns>途中で飛ばされたら true（続く演出も飛ばすのに使う）</returns>
        UniTask<bool> PlayExitAsync(TitleMenuItem pressed, CancellationToken ct);
    }
}
