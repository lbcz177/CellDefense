"""Deterministic, original procedural SFX; no downloaded recordings or runtime edits."""
from __future__ import annotations

import base64
import hashlib
import html
import io
import json
import math
from pathlib import Path
import wave

import numpy as np


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "Assets/_Project/Audio/SFX/细胞音效试听_2026-10-22_v1"
REVIEW = ROOT / "Docs/Audio"
RATE = 48000
TAU = 2 * math.pi


def timeline(seconds):
    return np.arange(round(seconds * RATE), dtype=np.float64) / RATE


def envelope(t, attack, decay):
    return (1 - np.exp(-t / attack)) * np.exp(-t / decay)


def bubble(seconds, start, end, decay, fm=0.35):
    t = timeline(seconds)
    frequency = end + (start - end) * np.exp(-t / 0.028)
    phase = np.cumsum(frequency) * TAU / RATE
    modulation = fm * np.exp(-t / 0.035) * np.sin(phase * 1.43)
    return (np.sin(phase + modulation) + 0.12 * np.sin(2 * phase)) * envelope(t, 0.0025, decay)


def bell(seconds, frequency, decay=0.19):
    t = timeline(seconds)
    return (np.sin(TAU * frequency * t) * np.exp(-t / decay)
            + 0.20 * np.sin(TAU * frequency * 2.003 * t) * np.exp(-t / (decay * 0.42))
            + 0.045 * np.sin(TAU * frequency * 3.97 * t) * np.exp(-t / 0.04)) * (1 - np.exp(-t / 0.004))


def soft_noise(seconds, rng, cutoff, decay, attack=0.003):
    t = timeline(seconds)
    noise = rng.normal(size=len(t))
    # Smooth low-pass noise: tactile air / soft crackle, without white-noise hiss.
    bins = np.fft.rfftfreq(len(t), 1 / RATE)
    noise = np.fft.irfft(np.fft.rfft(noise) / (1 + (bins / cutoff) ** 4), n=len(t))
    noise /= max(1e-6, np.std(noise))
    return noise * envelope(t, attack, decay)


def layer(destination, source, start=0.0, gain=1.0):
    offset = round(start * RATE)
    length = min(len(source), len(destination) - offset)
    if length > 0:
        destination[offset:offset + length] += source[:length] * gain


def make_sound(kind, variant=0):
    rng = np.random.default_rng(221022 + variant * 337 + sum(map(ord, kind)))
    detune = (0.97, 1.0, 1.035)[variant % 3]
    duration = {
        "click": .105, "build": .38, "upgrade": .72, "sell": .36,
        "emission": .15, "impact": .20, "engulf": .53, "wall_break": .66,
        "wave": .84, "boss": 1.40, "core": .58, "victory": 2.05, "defeat": 1.85,
    }[kind]
    out = np.zeros(round(duration * RATE))
    if kind == "click":
        layer(out, bubble(.09, 1100, 780, .014), gain=.60)
        layer(out, bell(.07, 1568, .015), .012, .12)
    elif kind == "build":
        layer(out, bubble(.20, 310, 155, .055), gain=.85)
        layer(out, bubble(.17, 690, 420, .038), .036, .44)
        layer(out, bell(.24, 1046.5, .07), .105, .22)
        layer(out, soft_noise(.12, rng, 900, .022), gain=.055)
    elif kind == "upgrade":
        for start, freq in zip((0, .085, .17, .27), (523.25, 659.25, 783.99, 1046.5)):
            layer(out, bell(.42, freq, .115), start, .48 if start < .27 else .58)
        layer(out, bubble(.25, 220, 440, .06), gain=.22)
    elif kind == "sell":
        layer(out, bubble(.16, 780, 450, .035), gain=.32)
        layer(out, bell(.28, 987.77, .075), .055, .50)
        layer(out, bell(.20, 659.25, .055), .13, .30)
    elif kind == "emission":
        layer(out, bubble(.15, 830 * detune, 360 * detune, .028, .45), gain=.78)
        layer(out, bubble(.09, 230 * detune, 145 * detune, .019), .003, .30)
        layer(out, soft_noise(.10, rng, 1750, .012), gain=.055)
    elif kind == "impact":
        layer(out, bubble(.19, 430 * detune, 120 * detune, .041, .70), gain=.83)
        layer(out, bubble(.12, 1450 * detune, 660 * detune, .014), .007, .18)
        layer(out, soft_noise(.16, rng, 1550, .026), gain=.12)
    elif kind == "engulf":
        layer(out, bubble(.34, 260, 96, .09, .9), gain=.70)
        for start, freq in ((.07, 460), (.13, 630), (.22, 340)):
            layer(out, bubble(.19, freq, freq * .38, .038, .6), start, .28)
        layer(out, soft_noise(.43, rng, 650, .11, .04), gain=.10)
    elif kind == "wall_break":
        layer(out, bubble(.40, 180, 57, .09, .8), gain=.75)
        for i in range(15):
            at = float(rng.uniform(.006, .22))
            layer(out, soft_noise(.20, rng, float(rng.uniform(900, 2600)), .025), at, .075 * (1 - at))
            layer(out, bell(.19, float(rng.uniform(280, 780)), .029), at, .035)
        layer(out, soft_noise(.55, rng, 620, .105), gain=.16)
    elif kind == "wave":
        for start, freq in ((0, 392), (.13, 523.25), (.27, 783.99)):
            layer(out, bell(.55, freq, .13), start, .50)
        layer(out, bubble(.30, 190, 290, .08), gain=.19)
    elif kind == "boss":
        for start in (0, .37, .74):
            layer(out, bubble(.48, 155, 88, .11), start, .68)
            layer(out, bell(.38, 392, .075), start + .04, .23)
            layer(out, bell(.34, 369.99, .075), start + .16, .20)
    elif kind == "core":
        layer(out, bubble(.50, 178, 63, .10, .5), gain=.85)
        layer(out, bell(.30, 311.13, .075), .015, .21)
        layer(out, bell(.28, 207.65, .08), .10, .17)
        layer(out, soft_noise(.25, rng, 800, .045), gain=.09)
    elif kind == "victory":
        for start, freq in ((0, 523.25), (.15, 659.25), (.30, 783.99), (.52, 1046.5)):
            layer(out, bell(1.1, freq, .25), start, .42)
        for freq in (261.63, 329.63, 392):
            layer(out, bell(1.35, freq, .34), .59, .15)
        layer(out, bell(.72, 1567.98, .17), .77, .075)
    elif kind == "defeat":
        for start, freq in ((0, 392), (.22, 311.13), (.47, 261.63)):
            layer(out, bell(1.1, freq, .25), start, .38)
        layer(out, bell(1.15, 130.81, .32), .54, .20)
    return finish(out)


def finish(samples):
    samples = samples.copy()
    # Remove DC and any residual discontinuity at either boundary.
    samples -= samples.mean(axis=0)
    fade = min(round(.006 * RATE), len(samples) // 4)
    shape = .5 - .5 * np.cos(np.linspace(0, math.pi, fade))
    if samples.ndim == 2:
        shape = shape[:, None]
    samples[:fade] *= shape
    samples[-fade:] *= shape[::-1]
    return samples


def wav_bytes(samples):
    buffer = io.BytesIO()
    with wave.open(buffer, "wb") as output:
        output.setnchannels(1 if samples.ndim == 1 else samples.shape[1])
        output.setsampwidth(2)
        output.setframerate(RATE)
        output.writeframes(np.round(samples * 32767).astype("<i2").tobytes())
    return buffer.getvalue()


def preserve_write(path, content):
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        if path.read_bytes() == content:
            return
        raise FileExistsError(f"Existing different asset preserved: {path}")
    with path.open("xb") as output:
        output.write(content)


SPECS = [
    ("click", "操作", "轻触", "短促的软质点击，用于菜单确认。", .27, 1),
    ("build", "操作", "建造", "落点有重量，随后一小声细胞成形。", .43, 1),
    ("upgrade", "操作", "升级", "逐步上扬的清亮音，表达强化完成。", .43, 1),
    ("sell", "操作", "出售", "轻轻回收，以两声柔和提示收尾。", .32, 1),
    ("emission", "战斗", "细胞发射", "轻弹性发射，三种微变体供轮换。", .28, 3),
    ("impact", "战斗", "黏液命中", "软质低频与少量气泡声，三种微变体。", .34, 3),
    ("engulf", "战斗", "巨噬吞噬", "下沉的吸入感，伴随短气泡。", .42, 1),
    ("wall_break", "战斗", "壁垒破裂", "纤维松裂与低沉散开，区别于金属爆炸。", .47, 1),
    ("wave", "状态", "开始波次", "简短向上提示，进入战斗。", .40, 1),
    ("boss", "状态", "Boss 警报", "三次低脉冲和双音警示，不用尖锐蜂鸣。", .49, 1),
    ("core", "状态", "核心受击", "更低沉、明确的受损反馈。", .54, 1),
    ("victory", "结算", "胜利", "短上行旋律与温暖和弦。", .49, 1),
    ("defeat", "结算", "失败", "收敛的下行提示，保留再次挑战的空间。", .43, 1),
]


def main():
    records, signals = [], {}
    for index, (kind, group, label, description, peak, variations) in enumerate(SPECS, 1):
        for variant in range(variations):
            signal = make_sound(kind, variant)
            signal *= peak / max(1e-9, np.max(np.abs(signal)))
            suffix = "_" + "abc"[variant] if variations > 1 else ""
            name = f"{index:02}_{kind}{suffix}.wav"
            data = wav_bytes(signal)
            preserve_write(OUTPUT / name, data)
            signals.setdefault(kind, []).append(signal)
            records.append(dict(kind=kind, group=group, label=label, description=description,
                                file=name, variant=variant + 1, seconds=round(len(signal) / RATE, 3),
                                peak_dbfs=round(20 * np.log10(np.max(np.abs(signal))), 2),
                                rms_dbfs=round(20 * np.log10(np.sqrt(np.mean(signal ** 2))), 2),
                                sha256=hashlib.sha256(data).hexdigest(), bytes=len(data)))

    # A quiet gap between each category makes the full reel easy to audition.
    sections = [np.zeros(round(.25 * RATE))]
    cue, cursor = [], .25
    for kind, group, label, *_ in SPECS:
        signal = signals[kind][0]
        cue.append(dict(label=label, at=round(cursor, 3)))
        sections += [signal, np.zeros(round(.38 * RATE))]
        cursor += len(signal) / RATE + .38
    reel = np.concatenate(sections)

    # Illustrative mix only: these are not edits to in-game voice handling.
    mix = np.zeros((round(8 * RATE), 2))
    rng = np.random.default_rng(8844)
    for index, start in enumerate(np.arange(.2, 6.8, .155)):
        shot = signals["emission"][index % 3]
        hit = signals["impact"][(index + 1) % 3]
        pan = float(rng.uniform(-.6, .6))
        stereo = np.array([math.sqrt((1 - pan) / 2), math.sqrt((1 + pan) / 2)])
        layer(mix, shot[:, None] * stereo, float(start), .40)
        layer(mix, hit[:, None] * stereo[::-1], float(start + .13), .43)
    for kind, start, gain in (("build", .08, .52), ("engulf", 2.2, .60), ("wall_break", 4.1, .53), ("core", 6.5, .65)):
        layer(mix, np.repeat(signals[kind][0][:, None], 2, axis=1), start, gain)
    mix = finish(mix)
    if np.max(np.abs(mix)) > .70:
        mix *= .70 / np.max(np.abs(mix))
    preserve_write(OUTPUT / "试听~" / "00_全组顺序试听.wav", wav_bytes(reel))
    preserve_write(OUTPUT / "试听~" / "01_密集战斗混音.wav", wav_bytes(mix))
    manifest = dict(sample_rate=RATE, bit_depth=16, channels=1, seed=221022,
                    style="柔软、弹性、轻微电子感；程序合成试听 v1", integrated=False,
                    files=records, reel_cues=cue)
    preserve_write(OUTPUT / "试听~" / "manifest.json", json.dumps(manifest, ensure_ascii=False, indent=2).encode())
    create_review(records, reel, mix)
    print(json.dumps(dict(output=str(OUTPUT), review=str(REVIEW / "2026-10-22_细胞音效试听.html"),
                          sounds=len(records), types=len(SPECS), reel_seconds=round(len(reel)/RATE, 2)), ensure_ascii=False))


def audio_uri(data):
    return "data:audio/wav;base64," + base64.b64encode(data).decode("ascii")


def create_review(records, reel, mix):
    cards = []
    for kind, group, label, description, _, _ in SPECS:
        choices = [record for record in records if record["kind"] == kind]
        controls = []
        for record in choices:
            file = record["file"]
            tag = "" if len(choices) == 1 else f' · 变体 {record["variant"]}'
            controls.append(f'<label class="take">{html.escape(label + tag)}<audio controls preload="none" src="{audio_uri((OUTPUT / file).read_bytes())}"></audio></label>')
        cards.append(f'<article><div class="group">{group} / {choices[0]["seconds"]:.2f}s</div><h2>{label}</h2><p>{description}</p>{"".join(controls)}</article>')
    document = r'''<!doctype html>
<html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>胞卫战 · 音效试听 v1</title>
<style>
*{box-sizing:border-box}body{margin:0;background:#0b191c;color:#e7f3ed;font:16px/1.65 system-ui,"Microsoft YaHei",sans-serif}main{max-width:1100px;margin:auto;padding:48px 24px}small,.group{color:#83c5b2;letter-spacing:.08em}h1{font-size:clamp(30px,5vw,48px);line-height:1.2;margin:12px 0}header p{max-width:720px;color:#b5cdc7}section{margin-top:30px}.players{display:grid;grid-template-columns:1fr 1fr;gap:18px}.featured,article{border:1px solid #294846;border-radius:18px;padding:22px;background:#10262a}h2{margin:5px 0;font-size:21px}p{margin:8px 0 17px;color:#b5cdc7;font-size:14px}audio{display:block;width:100%;height:38px;margin:9px 0 14px;accent-color:#8be2c4}.grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:16px}.take{display:block;font-size:12px;color:#c2dad0}.toolbar{display:flex;align-items:center;gap:16px;flex-wrap:wrap;margin:24px 0}input{accent-color:#92dec7}button{font:inherit;background:#9de9c9;color:#102420;border:0;padding:9px 20px;border-radius:24px;cursor:pointer}button:hover{background:#c4f6e0}footer{color:#91ada5;font-size:13px;margin-top:34px}article:has(audio[data-playing]){border-color:#9de9c9;box-shadow:0 0 0 1px #9de9c9}@media(max-width:850px){.grid{grid-template-columns:repeat(2,minmax(0,1fr))}}@media(max-width:560px){.grid,.players{grid-template-columns:1fr}main{padding:28px 16px}}
</style>
<main><header><small>CELL DEFENSE / SOUND STUDY 01</small><h1>胞卫战 · 细胞音效试听</h1><p>13 类用途，17 个独立音效。柔软的弹射、气泡和轻电子音色；战斗高频音效保持短促，警报与结算保留辨识度。本页可离线播放，音频已嵌入。</p></header>
<div class="toolbar"><label>试听音量 <input id="volume" type="range" min="0" max="1" value="0.65" step="0.01" aria-label="试听音量"></label><button id="stop" type="button">停止播放</button><span id="status" aria-live="polite">点击下方播放按钮试听</span></div>
<section class="players"><div class="featured"><small>推荐先听</small><h2>整组顺序试听</h2><p>顺序：轻触 → 建造 → 升级 → 出售 → 发射 → 命中 → 吞噬 → 破墙 → 开波 → Boss → 核心受击 → 胜利 → 失败。</p><audio controls preload="none" src="__REEL__"></audio></div><div class="featured"><small>叠加听感</small><h2>密集战斗示例 · 8 秒</h2><p>连续发射与命中，穿插吞噬、破墙和核心受击。用于判断声音密度与疲劳感，是编排示例。</p><audio controls preload="none" src="__MIX__"></audio></div></section>
<section class="grid">__CARDS__</section><footer>48 kHz / 16-bit PCM WAV · 单个音效为单声道，战斗示例为双声道。<br>本批为原创程序合成试听素材，尚未替换游戏内音效。独立 WAV 位于 Assets/_Project/Audio/SFX/细胞音效试听_2026-10-22_v1/。</footer></main>
<script>
const players=[...document.querySelectorAll('audio')],volume=document.querySelector('#volume'),status=document.querySelector('#status');
for(const audio of players){audio.volume=+volume.value;audio.addEventListener('play',()=>{for(const other of players){if(other!==audio){other.pause();other.removeAttribute('data-playing')}}audio.setAttribute('data-playing','');status.textContent='正在试听：'+(audio.closest('article,.featured').querySelector('h2').textContent)});audio.addEventListener('ended',()=>{audio.removeAttribute('data-playing');status.textContent='播放结束'});audio.addEventListener('pause',()=>audio.removeAttribute('data-playing'))}
volume.addEventListener('input',()=>players.forEach(audio=>audio.volume=+volume.value));document.querySelector('#stop').addEventListener('click',()=>{for(const audio of players){audio.pause();audio.currentTime=0;audio.removeAttribute('data-playing')}status.textContent='已停止'});
</script></html>'''
    document = document.replace("__CARDS__", "\n".join(cards)).replace("__REEL__", audio_uri(wav_bytes(reel))).replace("__MIX__", audio_uri(wav_bytes(mix)))
    preserve_write(REVIEW / "2026-10-22_细胞音效试听.html", document.encode("utf-8"))


if __name__ == "__main__":
    main()
