# 영상 음악 (직접 합성 → 저작권 걱정 없음) · 48kHz 스테레오 16비트
#  - 하이라이트 (record.ps1 -Mode highlight): 140 BPM · 라단조 (Dm - Bb - Gm - A), 타이코 북 · 현악 스타카토 · 금관 화음 · 합창 · 낮은 지속음
#    영상 장면이 바뀌는 순간 · 필살기가 터지는 순간 · 결계가 깨지는 순간 · 로고에 충격음을 맞춤 (GameplayCapture.HighlightReel 의 프레임 수 기준)
#  - 상점 트레일러 (record.ps1 기본 -Mode trailer, --trailer): 아래 「상점 트레일러 곡」 · CUE 표 (GameplayCapture.Capture 의 프레임 수 기준)
# 실행: py tools/trailer/make_trailer_music.py <출력 wav> [--trailer]
import sys, wave
import numpy as np

ARGS = [a for a in sys.argv[1:] if not a.startswith("--")]
TRAILER = "--trailer" in sys.argv[1:]

SR = 48000
DUR = 47.2 if TRAILER else 44.5
N = int(SR * DUR)
rng = np.random.default_rng(7)
L = np.zeros(N)
R = np.zeros(N)
EL = np.zeros(N)                # 트레일러 끝 버스 (로고 타격 · 정적 구간에서 눌리지 않음)
ER = np.zeros(N)

BPM = 140.0
BEAT = 60.0 / BPM
BAR = BEAT * 4

# 하이라이트 영상 타이밍 (초): 장면이 바뀌는 순간 · 필살기가 터지는 순간 · 결계가 깨지는 순간 · 로고
CUTS = [2.87, 6.37, 9.77, 15.47, 19.27, 22.57, 26.27, 31.97, 35.47]
ULTS = [0.6, 2.87 + 1.27, 6.37 + 1.17, 15.47 + 1.57, 19.27 + 1.07, 22.57 + 1.47, 31.97 + 1.27, 35.47 + 1.27]
BREAKS = [9.77 + 3.47, 26.27 + 3.47]
LOGO = 39.3


def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12.0)


def put(sig, start, gain=1.0, pan=0.0, bus=0):
    i0 = int(start * SR)
    if i0 >= N or i0 + len(sig) <= 0:
        return
    s = sig[: max(0, N - i0)] * gain
    gl, gr = np.sqrt(0.5 * (1 - pan)), np.sqrt(0.5 * (1 + pan))
    bl, br = (EL, ER) if bus else (L, R)
    bl[i0:i0 + len(s)] += s * gl
    br[i0:i0 + len(s)] += s * gr


def tt(sec):
    return np.arange(int(sec * SR)) / SR


def env(sec, a, r, sustain=1.0):
    t = tt(sec)
    e = np.minimum(1.0, t / max(a, 1e-4)) * sustain
    tail = np.clip((sec - t) / max(r, 1e-4), 0.0, 1.0)
    return e * tail


def lowpass(x, cutoff):
    # FFT 로 간단히 깎음 (짧은 소리용)
    X = np.fft.rfft(x)
    f = np.fft.rfftfreq(len(x), 1 / SR)
    X *= 1.0 / (1.0 + (f / cutoff) ** 4)
    return np.fft.irfft(X, len(x))


def highpass(x, cutoff):
    return x - lowpass(x, cutoff)


def saw(freq, sec, harm=12, rolloff=1.0, detune=0.0):
    t = tt(sec)
    s = np.zeros_like(t)
    for k in range(1, harm + 1):
        if freq * k > SR * 0.45:
            break
        s += np.sin(2 * np.pi * freq * k * (1 + detune) * t + k * 0.3) / k ** rolloff
    return s


# ---------------- 악기
def taiko(big=1.0):
    sec = 0.9
    t = tt(sec)
    f = 46 + 70 * np.exp(-t * 18)
    body = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 4.5)
    skin = lowpass(rng.standard_normal(len(t)), 900) * np.exp(-t * 28) * 0.8
    return (body * 1.4 + skin) * big


def tom(pitch=1.0):
    sec = 0.35
    t = tt(sec)
    f = (120 + 90 * np.exp(-t * 25)) * pitch
    body = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 11)
    hit = lowpass(rng.standard_normal(len(t)), 2500) * np.exp(-t * 40) * 0.5
    return body + hit


def snare():
    sec = 0.25
    t = tt(sec)
    n = highpass(rng.standard_normal(len(t)), 1500) * np.exp(-t * 22)
    return n * 0.7 + np.sin(2 * np.pi * 190 * t) * np.exp(-t * 30) * 0.4


def crash(sec=2.6):
    t = tt(sec)
    n = highpass(rng.standard_normal(len(t)), 4000)
    return n * np.exp(-t * 2.2) * 0.55


def boom(size=1.0):
    # 트레일러 충격음: 아주 낮게 떨어지는 사인 + 묵직한 타격 + 심벌
    sec = 3.2
    t = tt(sec)
    f = 28 + 60 * np.exp(-t * 6)
    sub = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 1.4)
    thud = lowpass(rng.standard_normal(len(t)), 300) * np.exp(-t * 9)
    out = np.tanh((sub * 1.6 + thud * 1.2) * 1.5) * size
    c = crash(sec)
    out[: len(c)] += c * 0.9 * size
    return out


def riser(sec):
    # 장면 앞에 차오르는 소리: 거꾸로 커지는 바람 + 올라가는 음
    t = tt(sec)
    k = (t / sec) ** 2.2
    n = lowpass(rng.standard_normal(len(t)), 6000) * k * 0.6
    f = 200 + 1400 * k
    tone = np.sin(2 * np.pi * np.cumsum(f) / SR) * k * 0.25
    return n + tone


def string_note(note, sec=BEAT / 4 * 0.9):
    s = saw(midi(note), sec, harm=14, rolloff=1.1) + saw(midi(note), sec, harm=14, rolloff=1.1, detune=0.004)
    return s * env(sec, 0.006, 0.04) * 0.35


def brass(notes, sec):
    out = np.zeros(int(sec * SR))
    for n in notes:
        for d in (-0.003, 0.003):
            out += saw(midi(n), sec, harm=16, rolloff=1.25, detune=d)
    e = env(sec, 0.035, 0.25)
    # 금관다운 숨: 앞이 살짝 부풀었다 가라앉음
    e *= 1.0 + 0.4 * np.exp(-tt(sec) * 8)
    return out * e / len(notes) * 0.9


def choir(notes, sec):
    t = tt(sec)
    out = np.zeros_like(t)
    for j, n in enumerate(notes):
        for k, g in ((1, 1.0), (2, 0.5), (3, 0.3), (4, 0.15)):
            vib = 1 + 0.004 * np.sin(2 * np.pi * (4.5 + j * 0.3) * t + j)
            out += g * np.sin(2 * np.pi * midi(n) * k * vib * t + j)
    return out * env(sec, 0.6, 0.8) / len(notes) * 0.5


def drone(note, sec):
    t = tt(sec)
    return (np.sin(2 * np.pi * midi(note) * t) + 0.5 * np.sin(2 * np.pi * midi(note + 12) * t)) * env(sec, 1.5, 2.0) * 0.5




# ================================================================= 상점 트레일러 곡 (record.ps1 기본 -Mode trailer · GameplayCapture.Capture)
# 120 BPM(한 박 0.5초 · 영웅 컷 1초 = 2박 · 네 세계 컷 1.5초 = 3박) · 라단조
# 0~4초 낮은 지속음 · 시계 초침 · 심장 박동 → 4.1초 드롭 → 장면마다 충격음 · 휘익 → 네 세계 · 개발자에서 가장 웅장하게(합창 · 금관)
# → 스킬 진화에서 한 번 숨을 고르고 → 영혼 트리 · 결계 깨짐 직전 상승음 → 정적(끝 카드 빛줄기) → 로고가 박히는 순간 거대한 타격 → 잔향 · 페이드
#
# 장면 시각 (초 = 녹화 프레임 / 30) — Capture() 의 장면 함수 프레임 수로 계산하고 지난 촬영 영상(1391프레임판)의 장면 전환 프레임으로 맞춤
# 끝 카드가 바뀌면 SLAM(EndCard 의 slam = RoundToInt(0.75 * 30) = 22프레임) · END 만 고치면 됨
T_BEAT = 0.5
T_STEP = T_BEAT / 4
# 2026-10-18 실제 녹화 [CUE] 로그(capture.log)로 맞춤: 결계 컷부터 +5프레임, 끝 카드는 GameplayCapture.EndcardFrame(1246)에 고정
CUE = {
    "first_shot": 0.62,      # 첫 조준 사격 (Fade 10 + AimShot 8프레임)
    "drop": 4.10,            # 123  유령 컷 (영웅 1초 컷 시작 · 비트가 들어옴)
    "hero_ult1": 4.33,       # 130  유령 필살기
    "hero2": 5.10,           # 153  시계공
    "hero_ult2": 5.33,       # 160  시계공 필살기
    "hero3": 6.10,           # 183  궁수 「완벽!」
    "barrier": 7.27,         # 213  킹 슬라임 결계 (BarrierKill)
    "barrier_kill": 8.47,    # 249  결계째 처치 + 슬로모 0.5초
    "evolve_pick": 9.77,     # 288  진화 카드 고름
    "evolve_ok": 10.27,      # 303  진화 확정
    "weak": 10.34,           # 305  리치 왕 빈틈 (WeakCut 78프레임)
    "memory": 12.94,         # 383  지옥의 군주 기억 (MemoryCut 90프레임)
    "memory_shield": 13.24,  # 392  기억 발동 → 방패
    "memory_ult": 14.77,     # 438  필살기로 깸
    "mirror": 15.94,         # 473  거울의 군주 (검은 화면에서 밝아짐 · 134프레임)
    "mirror_ult": 16.57,     # 492  거울이 내 필살기를 흉내
    "mirror_counter": 19.20, # 571  내 필살기
    "world1": 20.40,         # 607  초원 · 쌍권총 (WorldCut 45프레임씩)
    "world2": 21.90,         # 652  지하 묘역
    "world3": 23.40,         # 697  불타는 지옥
    "world4": 24.90,         # 742  영혼의 심연
    "developer": 26.40,      # 787  개발자 (120프레임)
    "dev_ult": 27.84,        # 830  개발자 앞에서 필살기
    "skill_evo": 30.40,      # 907  스킬 진화 (189프레임)
    "skill_fuse": 32.40,     # 967  마지막 재료 확정 → 진화 연출
    "skill_flash": 34.14,    # 1019 진화 연출이 가장 밝게 번쩍이는 순간
    "tree": 36.70,           # 1096 영혼 트리 (63프레임 · 0.4초 뒤부터 0.267초마다 구매 6번)
    "shatter": 38.80,        # 1159 연금술사 · 결계 (BarrierShatter)
    "shatter_break": 39.50,  # 1180 결계 깨짐 슬로모
    "fade_black": 40.90,     # 1201 검게 어두워짐 (0.4초)
    "endcard": 41.53,        # 1214 끝 카드 시작 (빛줄기가 번짐)
    "slam": 42.27,           # 1236 로고 「쾅」
    "end": 46.73,            # 1370 영상 끝 (끝 카드 156프레임)
}
TREE_BUYS = [CUE["tree"] + 0.40 + k * 8 / 30.0 for k in range(6)]
T_DUR = 47.2

# 박자 기준점: 4.1초(드롭)부터 0.5초 격자 · 거울 장면(15.77초, 검은 화면)에서 격자를 다시 맞춤 → 뒤쪽 컷이 거의 박 위에 떨어짐
ANCHORS = [(0.10, 0.0), (15.77, 15.77)]

# 화성: 평소 Dm - Bb - Gm - A · 네 세계(가장 웅장하게) Dm - Bb - F - C
T_CHORDS = [[50, 53, 57, 62], [46, 50, 53, 58], [43, 50, 55, 58], [45, 49, 52, 57]]
T_ROOTS = [38, 34, 31, 33]
EPIC_CHORDS = [[50, 53, 57, 62], [46, 50, 53, 58], [41, 48, 53, 57], [48, 52, 55, 60]]
EPIC_ROOTS = [38, 34, 41, 36]
T_OST = [0, 0, 12, 0, 7, 0, 3, 0, 0, 0, 12, 0, 7, 3, 2, 0]


def tick(hi=True):
    # 시계 초침: 짧은 딸깍
    t = tt(0.06)
    n = highpass(rng.standard_normal(len(t)), 3000) * np.exp(-t * 160)
    return n * 0.6 + np.sin(2 * np.pi * (3100 if hi else 2300) * t) * np.exp(-t * 90) * 0.35


def heartbeat():
    t = tt(0.45)
    f = 38 + 40 * np.exp(-t * 20)
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 9) * 1.2


def clang(base=420.0):
    # 쇠 방패 · 결계에 부딪는 소리 (조화롭지 않은 배음)
    t = tt(1.6)
    out = np.zeros_like(t)
    for k, g, d in ((1.0, 1.0, 3.0), (2.76, 0.6, 4.5), (5.40, 0.4, 6.0), (8.93, 0.25, 8.0)):
        out += g * np.sin(2 * np.pi * base * k * t) * np.exp(-t * d)
    hit = highpass(rng.standard_normal(len(t)), 2000) * np.exp(-t * 40) * 0.5
    return (out * 0.5 + hit) * 0.8


def bell(note, sec=1.4):
    t = tt(sec)
    f = midi(note)
    out = np.sin(2 * np.pi * f * t) + 0.5 * np.sin(2 * np.pi * f * 2.0 * t) * np.exp(-t * 2) + 0.3 * np.sin(2 * np.pi * f * 3.01 * t) * np.exp(-t * 4)
    return out * np.exp(-t * 3.0) * np.minimum(1.0, t / 0.003) * 0.35


def whoosh(sec=0.45):
    # 장면 전환 직전 휘익: 짧게 차오르는 바람 (끝에서 뚝 끊김 → 충격음이 이어 받음)
    t = tt(sec)
    k = (t / sec) ** 3
    n = highpass(lowpass(rng.standard_normal(len(t)), 7000), 500) * k
    f = 300 + 2200 * k
    return n * 0.55 + np.sin(2 * np.pi * np.cumsum(f) / SR) * k * 0.12


def revcrash(sec=0.6):
    return crash(sec * 2)[: int(sec * SR)][::-1] * 1.1


def bass(note, sec=0.2):
    s = saw(midi(note), sec, harm=10, rolloff=1.2)
    return lowpass(s, 420) * env(sec, 0.004, 0.05) * 0.9


def braam(notes, sec):
    # 트레일러 「브람」: 낮은 금관을 세게 찌그러뜨림
    b = brass(notes, sec)
    return np.tanh(b * 3.0) * env(sec, 0.02, sec * 0.6) * 0.8


def cello_swell(note, sec):
    t = tt(sec)
    s = saw(midi(note), sec, harm=10, rolloff=1.3) + saw(midi(note), sec, harm=10, rolloff=1.3, detune=0.003)
    trem = 1 + 0.25 * np.sin(2 * np.pi * 6.0 * t)
    return lowpass(s, 900) * trem * env(sec, sec * 0.7, 0.4) * 0.25


def grid_anchor(x):
    a = ANCHORS[0][1]
    for t0, an in ANCHORS:
        if x >= t0 - 1e-6:
            a = an
    return a


def groove(t0, t1, strings=1.0, drums=2, octave=False, brass_on=0.0, choir_on=0.0, bass_on=0.8, epic=False, gain=1.0):
    # t0 ~ t1 동안 박자 격자 위에 현악 스타카토 16분음표 · 베이스 · 북 · 금관 · 합창을 깔음
    anchor = grid_anchor(t0)
    chords, roots = (EPIC_CHORDS, EPIC_ROOTS) if epic else (T_CHORDS, T_ROOTS)
    i = int(np.ceil((t0 - anchor) / T_STEP - 1e-6))
    while True:
        x = anchor + i * T_STEP
        if x >= t1 - 1e-6:
            break
        bar, s = divmod(i, 16)
        ci = bar % 4
        root, chord = roots[ci], chords[ci]
        if strings > 0:
            put(string_note(root + 12 + T_OST[s], T_STEP * 0.9), x, gain * strings * (1.15 if s % 4 == 0 else 0.8), pan=-0.35)
            if octave:
                put(string_note(root + 24 + T_OST[s], T_STEP * 0.9), x, gain * strings * 0.45, pan=0.35)
        if bass_on > 0 and s % 2 == 0:
            put(bass(root - 12 if root > 36 else root, T_STEP * 1.6), x, gain * bass_on * (1.0 if s % 4 == 0 else 0.7))
        if drums >= 1 and s in (0, 8):
            put(taiko(1.0), x, gain * 0.95)
        if drums >= 2:
            if s in (4, 12):
                put(taiko(0.9), x, gain * 0.75)
                put(snare(), x, gain * 0.55, pan=0.1)
            if s % 4 == 2:
                put(tom(1.0), x, gain * 0.5, pan=-0.2)
        if drums >= 3:
            if s in (6, 14):
                put(taiko(0.8), x, gain * 0.6)
            if s in (13, 15):
                put(tom(1.3), x, gain * 0.45, pan=0.25)
            if s % 2 == 1:
                put(tick(s % 4 == 1), x, gain * 0.18, pan=0.4)            # 하이햇 대신 딸깍 (긴박하게)
        if s == 0:
            if brass_on > 0:
                put(brass(chord, T_BEAT * 1.6), x, gain * brass_on, pan=0.15)
            if choir_on > 0:
                put(choir([n + 12 for n in chord], T_BEAT * 4 + 0.4), x, gain * choir_on)
        if s == 10 and brass_on > 0:
            put(brass(chord, T_BEAT * 0.7), x, gain * brass_on * 0.7, pan=0.15)
        i += 1


def impact(at, size=1.0, whoosh_sec=0.4, cym=True):
    # 장면 전환: 휘익 → 쾅
    if whoosh_sec > 0:
        put(whoosh(whoosh_sec), at - whoosh_sec, 0.6 * size)
    put(boom(size), at, 0.9)
    if cym:
        put(crash(1.4), at, 0.35 * size, pan=0.3)


def hit(at, size=1.0):
    # 필살기 · 처치 같은 화면 속 큰 타격
    put(taiko(1.3), at, size)
    put(crash(1.2), at, 0.4 * size, pan=-0.3)


def trailer():
    c = CUE
    # ---- 0~4.1초: 긴장 — 낮은 지속음 · 첼로가 서서히 · 시계 초침 · 심장 박동, 첫 사격에 작은 타격, 2초부터 차오름
    put(drone(26, c["fade_black"] + 0.3), 0.0, 0.75)
    put(cello_swell(38, c["drop"]), 0.0, 1.0)
    for k in range(4):
        put(heartbeat(), 0.10 + k * 1.0, 0.5 + 0.12 * k)
        put(heartbeat(), 0.10 + k * 1.0 + 0.2, 0.3 + 0.08 * k)
    x, k = 0.10, 0
    while x < c["drop"] - 0.05:
        put(tick(k % 2 == 0), x, 0.22 + 0.25 * x / c["drop"], pan=0.25 if k % 2 == 0 else -0.25)
        x += 0.5 if x < 2.1 else 0.25                                   # 2.1초부터 초침이 두 배로 빨라짐
        k += 1
    put(taiko(0.5), c["first_shot"], 0.6)
    put(crash(0.8), c["first_shot"], 0.15)
    groove(2.10, c["drop"] - 0.12, strings=0.35, drums=0, bass_on=0.0)  # 현악이 작게 먼저 들어옴
    put(riser(2.0), c["drop"] - 2.0, 0.8)

    # ---- 4.1~7.1초: 영웅 1초 컷 — 비트가 들어옴
    impact(c["drop"], 1.0, 0)
    put(braam([26, 38, 45], 1.4), c["drop"], 0.8)
    groove(c["drop"], c["barrier"], strings=0.7, drums=2, bass_on=0.8, gain=0.6)
    for k in ("hero_ult1", "hero_ult2"):
        hit(c[k], 0.75)
    for k in ("hero2", "hero3"):
        impact(c[k], 0.6, 0.3)

    # ---- 7.1~10.2초: 킹 슬라임 결계 → 결계째 처치(슬로모) → 진화 카드
    impact(c["barrier"], 0.9, 0.35)
    put(braam([26, 38, 41, 45], 1.8), c["barrier"], 1.0)
    groove(c["barrier"], c["barrier_kill"] - 0.05, strings=0.8, drums=1, bass_on=0.9, gain=0.7)
    put(revcrash(0.5), c["barrier_kill"] - 0.5, 0.5)
    put(boom(1.2), c["barrier_kill"], 1.0)
    put(clang(330), c["barrier_kill"], 0.6)
    put(choir([62, 65, 69], 1.6), c["barrier_kill"], 0.7)
    groove(c["barrier_kill"] + 0.55, c["weak"] - 0.1, strings=0.8, drums=2, bass_on=0.8, gain=0.65)
    for j, n in enumerate((74, 77, 81, 86)):
        put(bell(n), c["evolve_pick"] + j * 0.06, 0.55, pan=-0.3 + j * 0.2)
    put(bell(86, 2.0), c["evolve_ok"], 0.6)

    # ---- 10.2~15.8초: 빈틈 · 기억 — 북이 꽉 차고 합창이 낮게
    impact(c["weak"], 0.8, 0.4)
    groove(c["weak"], c["mirror"] - 0.25, strings=1.0, drums=3, octave=True, choir_on=0.35, bass_on=0.9, gain=0.75)
    impact(c["memory"], 0.8, 0.4)
    put(clang(420), c["memory_shield"], 0.9)
    put(clang(560), c["memory_shield"] + 0.02, 0.4, pan=0.4)
    hit(c["memory_ult"], 1.1)
    put(riser(1.2), c["mirror"] - 1.2, 0.9)

    # ---- 15.8~20.2초: 거울의 군주 — 금관이 들어옴
    impact(c["mirror"], 1.1, 0)
    put(choir([50, 57, 62, 65], 2.4), c["mirror"], 0.8)
    groove(c["mirror"], c["mirror"] + 2.0, strings=1.0, drums=2, octave=True, brass_on=0.6, choir_on=0.5, gain=0.85)
    groove(c["mirror"] + 2.0, c["world1"] - 0.2, strings=1.0, drums=3, octave=True, brass_on=0.75, choir_on=0.6, gain=0.9)
    put(revcrash(0.6), c["mirror_ult"] - 0.6, 0.6)
    put(boom(0.7), c["mirror_ult"], 0.8)
    hit(c["mirror_counter"], 1.2)
    put(riser(1.3), c["world1"] - 1.3, 1.0)

    # ---- 20.2~26.2초: 네 세계 — 가장 웅장하게 (Dm - Bb - F - C · 합창 · 금관 · 북 두 배)
    impact(c["world1"], 1.25, 0)
    put(brass([38, 45, 50, 57, 62], 1.6), c["world1"], 1.0)
    groove(c["world1"], c["developer"] - 0.2, strings=1.0, drums=3, octave=True, brass_on=1.0, choir_on=0.9, epic=True)
    for k in ("world2", "world3", "world4"):
        impact(c[k], 0.85, 0.35)
    for k in ("world1", "world2", "world3", "world4"):
        hit(c[k] + 0.60, 1.1)                                          # 무리를 쓸어버리는 필살기 (Hit · 슬로모)

    # ---- 26.2~30.2초: 개발자 — 더 어둡고 무겁게, 크기는 그대로 최고
    impact(c["developer"], 1.35, 0.5)
    put(braam([26, 38, 45, 50], 2.2), c["developer"], 1.1)
    put(choir([62, 65, 69, 74], 4.0), c["developer"], 1.0)
    groove(c["developer"], c["skill_evo"] - 0.15, strings=1.0, drums=3, octave=True, brass_on=1.0, choir_on=1.0)
    put(boom(0.9), c["dev_ult"], 0.9)
    hit(c["dev_ult"], 1.2)

    # ---- 30.2~36.5초: 스킬 진화 — 한 번 숨을 고르고 진화 연출에서 다시 쌓음
    impact(c["skill_evo"], 0.9, 0.3)
    groove(c["skill_evo"], c["skill_flash"] - 0.05, strings=0.6, drums=1, bass_on=0.7, choir_on=0.3, gain=0.85)
    for j, n in enumerate((69, 74, 77, 81, 86)):
        put(bell(n), c["skill_fuse"] + j * 0.07, 0.6, pan=-0.4 + j * 0.2)
    put(riser(1.7), c["skill_flash"] - 1.7, 0.7)
    impact(c["skill_flash"], 1.0, 0)
    put(brass([38, 45, 50, 57, 62], 1.8), c["skill_flash"], 0.9)
    groove(c["skill_flash"] + 0.05, c["tree"] - 0.05, strings=0.85, drums=2, octave=True, brass_on=0.5, choir_on=0.5)

    # ---- 36.5~39.3초: 영혼 트리 → 결계 — 북 연타가 빨라지며 상승음
    impact(c["tree"], 0.7, 0.3)
    groove(c["tree"], c["shatter_break"] - 0.2, strings=0.9, drums=0, octave=True, bass_on=0.9, choir_on=0.5)
    for j, at in enumerate(TREE_BUYS):
        put(bell([74, 76, 77, 79, 81, 84][j]), at, 0.5, pan=-0.5 + j * 0.2)
    x = c["tree"]
    while x < c["shatter_break"] - 0.08:
        k = (x - c["tree"]) / (c["shatter_break"] - c["tree"])
        put(snare(), x, 0.15 + 0.5 * k, pan=0.1)
        if k > 0.5:
            put(tom(1.0 + 0.4 * k), x, 0.35 * k, pan=-0.2)
        x += 0.25 - 0.19 * k                                            # 0.25초 → 0.06초 간격
    impact(c["shatter"], 0.8, 0.35)
    put(riser(2.8), c["shatter_break"] - 2.8, 1.1)
    put(revcrash(0.7), c["shatter_break"] - 0.7, 0.6)

    # ---- 39.3초: 결계 깨짐 — 큰 충격 · 잔향 (40초 뒤에는 정적으로 눌림)
    impact(c["shatter_break"], 1.3, 0)
    put(clang(300), c["shatter_break"], 0.8)
    put(braam([26, 38, 45, 50], 1.2), c["shatter_break"], 1.0)
    put(choir([62, 65, 69, 74], 1.2), c["shatter_break"], 0.9)

    # ---- 40.5~41.2초: 끝 카드 빛줄기 — 거의 무음, 박히기 직전 0.3초 숨 들이쉬듯 짧게 거꾸로 차오름 (끝 버스)
    put(revcrash(0.3), c["slam"] - 0.3, 0.35, bus=1)
    put(riser(0.3), c["slam"] - 0.3, 0.2, bus=1)
    # ---- 41.2초: 로고 「쾅」 — 거대한 타격 + 금관 · 합창 화음 + 낮은 지속음, 잔향 꼬리
    s = c["slam"]
    put(boom(1.6), s, 1.3, bus=1)
    put(taiko(1.6), s, 1.2, bus=1)
    put(clang(260), s, 0.6, bus=1)
    put(braam([26, 38, 45, 50], 3.2), s, 1.1, bus=1)
    put(brass([38, 45, 50, 53, 57, 62], 4.2), s, 1.0, bus=1)
    put(choir([62, 65, 69, 74], 4.6), s, 1.2, bus=1)
    put(drone(26, 4.8), s, 0.8, bus=1)
    put(crash(3.5), s, 0.6, pan=0.2, bus=1)
    put(taiko(1.2), s + 1.0, 0.45, bus=1)                              # 잦아드는 여운 북 두 번
    put(taiko(1.0), s + 2.0, 0.3, bus=1)

# ================================================================= 하이라이트 곡
def highlight():
    # ---------------- 화성 (한 마디에 하나): Dm - Bb - Gm - A
    CHORDS = [[50, 53, 57, 62], [46, 50, 53, 58], [43, 50, 55, 58], [45, 49, 52, 57]]
    ROOTS = [38, 34, 31, 33]
    OSTINATO = [0, 0, 12, 0, 7, 0, 3, 0, 0, 0, 12, 0, 7, 3, 2, 0]


    def section(t0, t1):
        return lambda x: t0 <= x < t1


    intro = section(0.0, 6.37)
    main = section(6.37, 26.27)
    calm = section(26.27, BREAKS[1])
    climax = section(BREAKS[1], LOGO)

    # 낮은 지속음 (처음부터 로고 뒤까지)
    put(drone(26, LOGO + 5.0), 0.0, 0.8)

    bar = 0
    tb = 0.0
    while tb < LOGO:
        ci = bar % 4
        root = ROOTS[ci]
        chord = CHORDS[ci]
        # 현악 스타카토 16분음표
        for i, step in enumerate(OSTINATO):
            x = tb + i * BEAT / 4
            if x >= LOGO:
                break
            lvl = 0.45 + 0.55 * min(1.0, x / 6.37) if intro(x) else (0.6 if calm(x) else 1.0)
            put(string_note(root + 12 + step), x, lvl * (1.15 if i % 4 == 0 else 0.85), pan=-0.35)
            if climax(x) or main(x):
                put(string_note(root + 24 + step), x, lvl * 0.45, pan=0.35)
        # 타이코 · 톰
        for b in range(4):
            x = tb + b * BEAT
            if x >= LOGO:
                break
            if intro(x):
                if b in (0, 2):
                    put(taiko(0.8), x, 0.9)
            elif calm(x):
                if b == 0:
                    put(taiko(0.9), x, 0.8)
            else:
                put(taiko(1.0), x, 1.0)
                if b in (1, 3):
                    put(snare(), x, 0.6, pan=0.1)
                put(tom(1.0), x + BEAT * 0.5, 0.55, pan=-0.2)
                if climax(x):
                    put(tom(1.3), x + BEAT * 0.25, 0.45, pan=0.25)
                    put(tom(0.8), x + BEAT * 0.75, 0.5, pan=-0.25)
        # 금관 화음 · 합창
        if main(tb) or climax(tb):
            put(brass(chord, BEAT * 1.5), tb, 1.0, pan=0.15)
            put(brass(chord, BEAT * 0.6), tb + BEAT * 2.5, 0.7, pan=0.15)
            if climax(tb):
                put(brass([n + 12 for n in chord[1:]], BEAT * 1.0), tb + BEAT * 2, 0.6, pan=-0.15)
        if not intro(tb):
            put(choir([n + 12 for n in chord], BAR + 0.4), tb, 0.9 if climax(tb) else 0.6)
        tb += BAR
        bar += 1

    # ---------------- 영상에 맞춘 충격음
    put(riser(2.8), 6.37 - 2.8, 0.9)                       # 본편 진입
    for c in CUTS:
        put(boom(0.75), c, 0.9)
    for u in ULTS:
        put(taiko(1.3), u, 1.0)
        put(crash(1.6), u, 0.5, pan=0.3)
    for b in BREAKS:
        put(riser(3.0), b - 3.0, 1.0)
        put(boom(1.2), b, 1.2)
        put(brass([38, 45, 50, 57, 62], 2.5), b, 1.1)
    put(riser(3.5), LOGO - 3.5, 1.1)
    put(boom(1.4), LOGO, 1.3)
    put(brass([38, 45, 50, 53, 57, 62], 4.5), LOGO, 1.0)
    put(choir([62, 65, 69, 74], 5.0), LOGO, 1.2)



# ---------------- 잔향 (짧은 홀) · 마스터
if TRAILER:
    trailer()
else:
    highlight()

ir_t = tt(1.6)
ir = rng.standard_normal(len(ir_t)) * np.exp(-ir_t * 3.2)
ir = lowpass(ir, 5000)
ir /= np.sqrt(np.sum(ir ** 2))


def reverb(x, wet=0.22):
    n = len(x) + len(ir)
    size = 1 << (n - 1).bit_length()
    y = np.fft.irfft(np.fft.rfft(x, size) * np.fft.rfft(ir, size), size)[: len(x)]
    return x + y * wet


L, R = reverb(L), reverb(R)
if TRAILER:
    # 끝 카드 빛줄기 동안 정적: 앞 장면 소리(잔향까지)를 40.1초부터 눌러 40.45초에 거의 무음 (-40dB)
    t = tt(DUR)
    duck = np.clip(1.0 - (t - (CUE["fade_black"] + 0.07)) / 0.35, 0.01, 1.0)
    # 로고 꼬리: 41.2초 타격 뒤 1.6초 머물다 3.4초 동안 사라짐 (영상 끝 45.67초 전에 거의 다 잦아듦)
    tail = np.clip(1.0 - (t - (CUE["slam"] + 1.6)) / 3.4, 0.0, 1.0) ** 1.5
    EL, ER = reverb(EL, 0.35), reverb(ER, 0.35)
    L, R = L * duck + EL * tail, R * duck + ER * tail
else:
    fade = np.clip((DUR - tt(DUR)) / 3.0, 0.0, 1.0)
    L, R = L * fade, R * fade
peak = max(np.abs(L).max(), np.abs(R).max()) or 1.0
drive = 1.6
L = np.tanh(L / peak * drive) / np.tanh(drive) * 0.93
R = np.tanh(R / peak * drive) / np.tanh(drive) * 0.93

out = ARGS[0] if ARGS else "TrailerMusic.wav"
pcm = (np.clip(np.stack([L, R], axis=1), -1.0, 1.0) * 32767).astype("<i2")
with wave.open(out, "w") as w:
    w.setnchannels(2)
    w.setsampwidth(2)
    w.setframerate(SR)
    w.writeframes(pcm.tobytes())
print("음악 저장: %s (%.1f초, %s)" % (out, DUR, "상점 트레일러" if TRAILER else "하이라이트"))
