using System.Collections.Generic;
using R3;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// モード選択画面（時間割と黒板）の表示
    /// 時間割でモードを選び、[この授業を受ける] で黒板を広げて難易度を選ぶ
    /// </summary>
    public interface IModeSelectView
    {
        /// <summary>時間割の行がクリックされたとき。値は行の番号（0 始まり）</summary>
        Observable<int> OnModeClicked { get; }

        /// <summary>時間割の選択を上下に動かすキーが押されたとき。値は -1（上）か 1（下）</summary>
        Observable<int> OnModeMoved { get; }

        /// <summary>[この授業を受ける] が押されたとき（Enter を含む）</summary>
        Observable<Unit> OnAttendClicked { get; }

        /// <summary>[← タイトルへ] が押されたとき（時間割で Esc を含む）</summary>
        Observable<Unit> OnBackToTitleClicked { get; }

        /// <summary>黒板の難易度の列がクリックされたとき。値は列の番号（0 始まり）</summary>
        Observable<int> OnDifficultyClicked { get; }

        /// <summary>黒板の難易度の選択を左右に動かすキーが押されたとき。値は -1（左）か 1（右）</summary>
        Observable<int> OnDifficultyMoved { get; }

        /// <summary>[○○ではじめる] が押されたとき（黒板で Enter を含む）</summary>
        Observable<Unit> OnStartClicked { get; }

        /// <summary>[← 時間割へ] が押されたとき（黒板で Esc を含む）</summary>
        Observable<Unit> OnBackToTimetableClicked { get; }

        /// <summary>時間割の横の黒板に字をチョークで書き始めたとき（true）と、書き終えた・飛ばした・止めたとき（false）</summary>
        Observable<bool> OnChalkWriting { get; }

        /// <summary>モード選択画面を表示するかどうか。隠すときは黒板をタイトルの大きさに戻す</summary>
        void SetVisible(bool visible);

        /// <summary>時間割に行を並べる</summary>
        void SetTimetable(IReadOnlyList<ModeSummary> modes);

        /// <summary>時間割の行を選び、右の黒板にそのモードの内容をチョークで書く</summary>
        /// <param name="canAttend">[この授業を受ける] を押せるかどうか（準備中なら false）</param>
        void SelectMode(int index, ModeSummary mode, bool canAttend);

        /// <summary>黒板を画面いっぱいに広げ、難易度ごとの記録を書く</summary>
        void ShowDifficultyBoard(ModeSummary mode);

        /// <summary>黒板の難易度の列を選ぶ</summary>
        /// <param name="startLabel">はじめるボタンの文字（例：常識ではじめる）</param>
        void SelectDifficulty(int index, string startLabel);

        /// <summary>黒板を時間割の横の大きさにして、時間割を見せる</summary>
        /// <param name="fromTitle">タイトルから来たかどうか。true なら黒板が縮んで時間割表が貼られる演出をする</param>
        void ShowTimetable(bool fromTitle);
    }
}
