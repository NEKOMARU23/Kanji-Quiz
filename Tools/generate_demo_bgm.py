"""
デモ用の BGM（WAV）を作る。

本番の曲を決めるまでの仮の曲。ほかの人の音源を使わず、計算で作る（クレジット不要）。標準ライブラリだけで動く。
曲の頭と終わりがつながるように、最後の音の余韻は曲の頭に重ねてある（ループしても切れ目が出ない）。
出力先: Assets/TechC/KanjiQuiz/Art/Audio/BGM/

    python Tools/generate_demo_bgm.py

- bgm_menu_demo.wav : メニュー用。ゆったりしたアルペジオとやさしいメロディ（BPM 72、8小節）
- bgm_quiz_demo.wav : クイズ用。軽いリズムで、考えるじゃまにならない（BPM 100、8小節）
"""

import math
import random
import struct
import wave
from pathlib import Path

SAMPLE_RATE = 44100
PEAK = 0.7  # 最大の音量（1.0 で割れる）
OUTPUT_DIR = Path(__file__).resolve().parent.parent / "Assets" / "TechC" / "KanjiQuiz" / "Art" / "Audio" / "BGM"


def midi_to_freq(note):
    return 440.0 * 2.0 ** ((note - 69) / 12.0)


class Track:
    """ループする1本の音。はみ出した余韻は曲の頭に回り込ませる"""

    def __init__(self, seconds):
        self.length = int(SAMPLE_RATE * seconds)
        self.samples = [0.0] * self.length

    def add(self, start, values, volume):
        """start 秒の位置に values を足す。曲の終わりを越えた分は頭に回り込む"""
        offset = int(SAMPLE_RATE * start)
        for i, value in enumerate(values):
            self.samples[(offset + i) % self.length] += value * volume


def pluck(note, duration, decay=0.35, harmonics=(1.0, 0.45, 0.2, 0.08)):
    """弦をはじいたような音（ピアノやマリンバに近い）"""
    freq = midi_to_freq(note)
    count = int(SAMPLE_RATE * duration)
    values = []
    for i in range(count):
        t = i / SAMPLE_RATE
        value = 0.0
        for n, amp in enumerate(harmonics, start=1):
            # 高い倍音ほど早く消える
            value += amp * math.sin(2 * math.pi * freq * n * t) * math.exp(-t * (n - 1) * 3.0 / duration)
        attack = min(1.0, t / 0.004)
        value *= attack * math.exp(-t / decay)
        values.append(value)
    return values


def pad(notes, duration, volume=1.0):
    """ゆっくり立ち上がり、ゆっくり消える和音"""
    count = int(SAMPLE_RATE * duration)
    values = [0.0] * count
    for note in notes:
        freq = midi_to_freq(note)
        for i in range(count):
            t = i / SAMPLE_RATE
            fade_in = min(1.0, t / (duration * 0.3))
            fade_out = min(1.0, (duration - t) / (duration * 0.3))
            values[i] += math.sin(2 * math.pi * freq * t) * fade_in * fade_out * volume / len(notes)
    return values


def kick(duration=0.18):
    """やわらかいバスドラム"""
    count = int(SAMPLE_RATE * duration)
    values = []
    phase = 0.0
    for i in range(count):
        t = i / SAMPLE_RATE
        freq = 45 + 80 * math.exp(-t * 30)
        phase += 2 * math.pi * freq / SAMPLE_RATE
        values.append(math.sin(phase) * math.exp(-t * 18))
    return values


def hat(rng, duration=0.05):
    """ハイハット（ノイズの高い成分だけを残す）"""
    count = int(SAMPLE_RATE * duration)
    values = []
    previous = 0.0
    for i in range(count):
        t = i / SAMPLE_RATE
        noise = rng.uniform(-1.0, 1.0)
        values.append((noise - previous) * math.exp(-t * 90))
        previous = noise
    return values


# コード：(ベース, 和音の構成音)。中央のドが 60
C = (36, (60, 64, 67, 72))
Am = (33, (57, 60, 64, 69))
F = (29, (53, 57, 60, 65))
G = (31, (55, 59, 62, 67))


def write_wav(path, track):
    peak = max(abs(v) for v in track.samples) or 1.0
    scale = PEAK / peak
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(SAMPLE_RATE)
        wav.writeframes(b"".join(struct.pack("<h", int(v * scale * 32767)) for v in track.samples))
    print(f"{path.name}: {track.length / SAMPLE_RATE:.1f} 秒")


def make_menu():
    bpm = 72
    beat = 60.0 / bpm
    bars = [C, Am, F, G, C, Am, F, G]
    track = Track(beat * 4 * len(bars))

    # 1〜4小節はアルペジオだけ、5〜8小節はやさしいメロディをのせる
    melody = {
        4: [(0, 76, 1.5), (1.5, 74, 0.5), (2, 72, 2.0)],
        5: [(0, 72, 1.0), (1, 69, 1.0), (2, 71, 1.0), (3, 72, 1.0)],
        6: [(0, 77, 1.5), (1.5, 76, 0.5), (2, 72, 2.0)],
        7: [(0, 74, 1.0), (1, 71, 1.0), (2, 74, 1.0), (3, 79, 1.0)],
    }

    for bar_index, (bass, chord) in enumerate(bars):
        start = bar_index * beat * 4
        track.add(start, pad(chord[:3], beat * 4.2, 1.0), 0.16)
        track.add(start, pluck(bass + 12, beat * 3.5, decay=0.9), 0.55)

        # 8分音符のアルペジオ：根音 → 3度 → 5度 → 上のオクターブ → 5度 → 3度 …
        order = [0, 1, 2, 3, 2, 1, 2, 1]
        for step, index in enumerate(order):
            track.add(start + step * beat / 2, pluck(chord[index], beat * 1.2, decay=0.5), 0.22)

        for offset, note, length in melody.get(bar_index, []):
            track.add(start + offset * beat, pluck(note, beat * length * 1.4, decay=0.6), 0.34)
    return track


def make_quiz():
    bpm = 100
    beat = 60.0 / bpm
    bars = [Am, F, C, G, Am, F, G, C]
    track = Track(beat * 4 * len(bars))
    rng = random.Random(7)
    kick_sound = kick()

    # 小節ごとのメロディ（拍の位置, 音, 長さ）。短くはねる音でリズムを作る
    melody = [
        [(0, 76), (0.5, 79), (1.5, 76), (2.5, 74), (3, 72)],
        [(0, 77), (0.5, 81), (1.5, 77), (2.5, 76), (3, 74)],
        [(0, 79), (0.5, 76), (1.5, 72), (2, 76), (3, 79)],
        [(0, 79), (1, 74), (1.5, 76), (2.5, 74), (3.5, 71)],
        [(0, 76), (0.5, 79), (1.5, 81), (2.5, 79), (3, 76)],
        [(0, 77), (0.5, 76), (1.5, 72), (2.5, 77), (3, 81)],
        [(0, 79), (1, 76), (1.5, 74), (2.5, 71), (3, 74)],
        [(0, 72), (1, 76), (2, 79), (2.5, 76), (3, 72)],
    ]

    for bar_index, (bass, chord) in enumerate(bars):
        start = bar_index * beat * 4
        # ベースは1拍目と、3拍目の裏
        track.add(start, pluck(bass + 12, beat * 1.2, decay=0.4, harmonics=(1.0, 0.3)), 0.55)
        track.add(start + beat * 2.5, pluck(bass + 12, beat * 1.0, decay=0.3, harmonics=(1.0, 0.3)), 0.4)

        # 和音は裏拍のコードストローク（短く）
        for off in (1.5, 3.5):
            for note in chord[:3]:
                track.add(start + off * beat, pluck(note, beat * 0.8, decay=0.18), 0.12)

        # バスドラ（1・3拍）とハイハット（8分）
        for off in (0, 2):
            track.add(start + off * beat, kick_sound, 0.5)
        for step in range(8):
            track.add(start + step * beat / 2, hat(rng), 0.10 if step % 2 == 0 else 0.06)

        for off, note in melody[bar_index]:
            track.add(start + off * beat, pluck(note, beat * 0.9, decay=0.22, harmonics=(1.0, 0.2, 0.05)), 0.30)
    return track


def main():
    write_wav(OUTPUT_DIR / "bgm_menu_demo.wav", make_menu())
    write_wav(OUTPUT_DIR / "bgm_quiz_demo.wav", make_quiz())


if __name__ == "__main__":
    main()
