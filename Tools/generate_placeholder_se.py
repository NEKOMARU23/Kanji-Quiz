"""
効果音（WAV）を作る。

製品版でも使う自作の音（ほかの人の音源を使わず、計算で作る）。標準ライブラリだけで動く。
チョークの音（se_chalk_writing.mp3）だけは On-Jin ～音人～ の音源なので、このスクリプトでは作らない。
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


def select():
    """選択肢を選んだ：コッという短い木の音"""
    return normalize(mix(
        tone(1400, 0.06, attack=0.001, decay=0.012),
        tone(700, 0.06, attack=0.001, decay=0.02, volume=0.6),
    ), peak=0.6)


def page_turn():
    """次の問題へ：紙をめくるサッという音"""
    random.seed(1)
    count = int(SAMPLE_RATE * 0.22)
    noise = []
    for i in range(count):
        t = i / count
        envelope = math.sin(math.pi * t) ** 2
        noise.append(random.uniform(-1, 1) * envelope)
    # こもらせてから高い音を少し足し、紙らしい音にする
    soft = lowpass(noise, 0.75)
    return normalize(mix(soft, [v * 0.25 for v in noise]), peak=0.45)


def stamp():
    """ハンコを押す：ポンという低い音"""
    random.seed(2)
    body = []
    count = int(SAMPLE_RATE * 0.25)
    for i in range(count):
        t = i / SAMPLE_RATE
        freq = 180 * math.exp(-t * 8) + 70
        body.append(math.sin(2 * math.pi * freq * t) * math.exp(-t / 0.06))
    click = [random.uniform(-1, 1) * math.exp(-i / (SAMPLE_RATE * 0.004)) * 0.6 for i in range(int(SAMPLE_RATE * 0.03))]
    return normalize(mix(body, lowpass(click, 0.4)), peak=0.85)


def hanamaru_draw():
    """花丸を書く：ペンでキュキュッと書く音（花丸の画像が出る時間に合わせて約 1.2 秒）"""
    random.seed(3)
    count = int(SAMPLE_RATE * 1.2)
    track = []
    phase = 0.0
    for i in range(count):
        t = i / SAMPLE_RATE
        # 音の高さを揺らして、ペン先がこすれる感じにする
        freq = 2600 + 900 * math.sin(2 * math.pi * 7 * t)
        phase += freq / SAMPLE_RATE
        squeak = math.sin(2 * math.pi * phase) * 0.25
        scratch = random.uniform(-1, 1) * 0.5
        envelope = (0.6 + 0.4 * abs(math.sin(2 * math.pi * 3.5 * t))) * min(1.0, (count - i) / (SAMPLE_RATE * 0.05))
        track.append((squeak + scratch) * envelope)
    return normalize(lowpass(track, 0.55), peak=0.4)


def new_record():
    """自己ベスト更新：キラキラと上がっていく音"""
    notes = [1046.5, 1318.5, 1568.0, 2093.0]
    track = []
    for i, f in enumerate(notes):
        track = mix(track or [0.0], silence(0.07 * i) + tone(f, 0.5, decay=0.15, volume=0.7))
    return normalize(track, peak=0.6)


def eraser():
    """黒板消しで1回引く：ザッという柔らかい音（1回引く秒数 0.33 秒に合わせる）"""
    random.seed(5)
    count = int(SAMPLE_RATE * 0.33)
    track = []
    for i in range(count):
        t = i / count
        # 引き始めと引き終わりは弱く、真ん中で一番こする
        envelope = math.sin(math.pi * t) ** 1.5 * (0.85 + 0.15 * math.sin(2 * math.pi * 18 * t))
        track.append(random.uniform(-1, 1) * envelope)
    # フェルトでこするので、紙をめくる音よりこもらせる
    return normalize(lowpass(lowpass(track, 0.85), 0.6), peak=0.4)


if __name__ == "__main__":
    save("se_correct.wav", correct())
    save("se_incorrect.wav", incorrect())
    save("se_roulette_loop.wav", roulette_loop())
    save("se_score_reveal.wav", score_reveal())
    save("se_select.wav", select())
    save("se_page_turn.wav", page_turn())
    save("se_stamp.wav", stamp())
    save("se_hanamaru.wav", hanamaru_draw())
    save("se_new_record.wav", new_record())
    save("se_eraser.wav", eraser())
