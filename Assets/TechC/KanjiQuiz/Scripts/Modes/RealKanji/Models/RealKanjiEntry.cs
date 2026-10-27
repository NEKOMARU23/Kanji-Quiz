using System;

namespace TechC.KanjiQuiz.Modes.RealKanji
{
    /// <summary>
    /// 本物の漢字さがしの問題データ1件（Data/RealKanji の JSON の questions の要素）
    /// Tools/generate_real_kanji_fakes.py で作る。JsonUtility で読むため、フィールド名は JSON に合わせる
    /// </summary>
    [Serializable]
    public class RealKanjiEntry
    {
        /// <summary>本物の漢字</summary>
        public string kanji;

        /// <summary>本物の読み（音読み。なければ訓読み）</summary>
        public string reading;

        /// <summary>部首（例：氵）</summary>
        public string radical;

        /// <summary>部首の位置（KanjiVG の kvg:position。left・right・top・bottom・kamae・tare・nyo）</summary>
        public string position;

        /// <summary>本物の SVG で残す部首のグループの id</summary>
        public string[] radicalGroups;

        /// <summary>偽物の候補</summary>
        public RealKanjiFakeEntry[] fakes;
    }

    /// <summary>
    /// 偽物の候補1つ。本物の部首以外の部分を、part に入れ替えたもの
    /// </summary>
    [Serializable]
    public class RealKanjiFakeEntry
    {
        /// <summary>入れ替える部品の漢字（例：舎）</summary>
        public string part;

        /// <summary>部品を借りる漢字（例：捨）</summary>
        public string donor;

        /// <summary>donor の SVG で借りるグループの id。空なら donor の画をすべて使う</summary>
        public string[] groups;

        /// <summary>偽物の読み（part の音読み）</summary>
        public string reading;
    }
}
