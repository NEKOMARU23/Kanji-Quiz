"""
結果画面の仮の効果音（WAV）を作る。

正式な素材に差し替えるまでの仮の音。標準ライブラリだけで動く。
出力先: Assets/TechC/KanjiQuiz/Art/Audio/SE/

    python Tools/generate_placeholder_se.py
"""

import math
import random
import struct
import wave
from pathlib import Path

SAMPLE_RATE = 44100
OUTPUT_DIR = Path(__file__).resolve().parent.parent / "Assets" / "TechC" / "KanjiQuiz" / "Art" / "Audio" / "SE"


def tone(freq, duration, wave_type="sine", attack=0.005, decay=None, volume=1.0):
    """1つの音を作る。decay を指定すると指数関数的に減衰する"""
    count = int(SAMPLE_RATE * duration)
    samples = []
    for i in range(count):
        t = i / SAMPLE_RATE
        phase = freq * t
        if wave_type == "sine":
            value = math.sin(2 * math.pi * phase) + 0.3 * math.sin(4 * math.pi * phase)
        elif wave_type == "square":
            value = 1.0 if (phase % 1.0) < 0.5 else -1.0
        elif wave_type == "saw":
            value = 2.0 * (phase % 1.0) - 1.0
        else:
            raise ValueError(wave_type)

        envelope = min(1.0, t / attack) if attack > 0 else 1.0
        if decay is not None:
            envelope *= math.exp(-t / decay)
        # 終わりのプツッという音を防ぐ
        release = min(1.0, (count - i) / (SAMPLE_RATE * 0.01))
        samples.append(value * envelope * release * volume)
    return samples


def mix(*tracks):
    length = max(len(t) for t in tracks)
    result = [0.0] * length
    for track in tracks:
        for i, value in enumerate(track):
            result[i] += value
    return result


def concat(*tracks):
    result = []
    for track in tracks:
        result.extend(track)
    return result


def silence(duration):
    return [0.0] * int(SAMPLE_RATE * duration)


def lowpass(samples, strength):
    """簡単なローパス。strength が大きいほどこもる（0〜1）"""
    result = []
    prev = 0.0
    for value in samples:
        prev = prev * strength + value * (1 - strength)
        result.append(prev)
    return result


def normalize(samples, peak=0.8):
    max_value = max(abs(v) for v in samples) or 1.0
    return [v / max_value * peak for v in samples]


def save(name, samples):
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    path = OUTPUT_DIR / name
    with wave.open(str(path), "wb") as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(SAMPLE_RATE)
        f.writeframes(b"".join(struct.pack("<h", int(max(-1.0, min(1.0, v)) * 32767)) for v in samples))
    print(f"wrote {path}")


def correct():
    """正解：ピンポン"""
    return normalize(concat(
        tone(987.8, 0.14, decay=0.25),
        tone(784.0, 0.5, decay=0.18),
    ))


def incorrect():
    """不正解：ブッブー"""
    buzz_short = lowpass(tone(130, 0.12, "square", attack=0.003), 0.6)
    buzz_long = lowpass(tone(130, 0.4, "square", attack=0.003), 0.6)
    return normalize(concat(buzz_short, silence(0.06), buzz_long), peak=0.6)


def roulette_loop():
    """ルーレット：カチカチという音。ループしてもつなぎ目が出ないよう、ちょうど 8 回分の長さにする"""
    tick_interval = 0.0625
    tick = tone(2200, 0.012, attack=0.0005, decay=0.004)
    track = []
    for _ in range(8):
        track.extend(tick)
        track.extend(silence(tick_interval - len(tick) / SAMPLE_RATE))
    return normalize(track, peak=0.5)


def score_reveal():
    """点数の発表：ジャン！（和音とシンバル風のノイズ）"""
    random.seed(0)
    chord = [523.25, 659.25, 783.99, 1046.5]
    brass = mix(*[lowpass(tone(f, 1.2, "saw", attack=0.01, decay=0.45, volume=0.6), 0.5) for f in chord])
    noise = [random.uniform(-1, 1) * math.exp(-i / (SAMPLE_RATE * 0.25)) * 0.5 for i in range(int(SAMPLE_RATE * 1.0))]
    return normalize(mix(brass, noise))


if __name__ == "__main__":
    save("se_correct.wav", correct())
    save("se_incorrect.wav", incorrect())
    save("se_roulette_loop.wav", roulette_loop())
    save("se_score_reveal.wav", score_reveal())
