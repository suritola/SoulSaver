# 무한 모드 최종 보스 배경음악 (1.1.2~): 「개발자」 · 「개발자 엄마」 — 웅장 · 공포 · 크게
#   Music/bgm_developer.wav       개발자전 (라 단조 · 150 BPM · 60마디 96.0초 · 끊김 없는 반복)
#   Music/bgm_developer_mom.wav   엄마전   (사 단조 · 168 BPM · 60마디 85.7초 · 같은 주제를 더 격렬하게)
# 스테레오 16비트 44.1kHz (다른 곡은 모노 32kHz — 합창 · 잔향의 공간감을 위해 올림. .meta 는 forceToMono 0)
#
# 악기는 전부 수식으로 합성: 포먼트 합창(라틴풍 음절 · 저음 남성 / 여성), 파이프 오르간, 금관, 낮은 현 오스티나토, 팀파니 · 대북,
# 쇠망치 · 종, 심장 박동 저음 펄스, 디지털 글리치 · 비트크러시(개발자 = 코드가 흐르는 노트북), 순환 컨볼루션 잔향(반복 이음새를 가로질러 꼬리가 이어짐)
# 실행: py -3.14 tools/audio/make_developer_bgm.py [dev|mom|all]   (numpy 필요 — 순수 math 로는 수십 성부 × 440만 샘플이 몇 시간)
# 끝나면 피크 · RMS · 길이 · 반복 이음새 불연속을 출력
import math, os, re, sys, time, uuid, wave
import numpy as np

RATE = 44100
TAU = 2 * math.pi
MUSIC = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', 'Resources', 'Music'))
rng = np.random.default_rng(20261015)


def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12.0)


def db(x):
    return 10 ** (x / 20.0)


def dbfs(x):
    return 20 * math.log10(max(x, 1e-9))


# ───────────────────────────────────────────── 기본 도구
def tline(n):
    return np.arange(n) / RATE


def env(n, a=0.01, r=0.05, curve=1.0):
    # 선형 어택 · 릴리스 (양 끝 0 — 딸깍 방지). curve>1 이면 어택이 느리게 시작
    t = tline(n)
    dur = n / RATE
    e = np.minimum(1.0, t / max(a, 1e-4)) ** curve
    rel = np.clip((dur - t) / max(r, 1e-4), 0.0, 1.0)
    return e * rel


def fft_filter(x, lo=None, hi=None, order=4):
    # 버터워스 모양의 진폭 마스크를 주파수 영역에서 곱함 (1차원 · 또는 (n,2) 스테레오, 축 0)
    n = x.shape[0]
    X = np.fft.rfft(x, axis=0)
    f = np.fft.rfftfreq(n, 1.0 / RATE)
    m = np.ones_like(f)
    if lo:
        m *= 1.0 / np.sqrt(1.0 + (lo / np.maximum(f, 1e-6)) ** (2 * order))
    if hi:
        m *= 1.0 / np.sqrt(1.0 + (f / hi) ** (2 * order))
    if x.ndim == 2:
        m = m[:, None]
    return np.fft.irfft(X * m, n=n, axis=0)


def noise(n):
    return rng.standard_normal(n)


def lookup(ext, ph):
    # 파형 테이블 선형 보간 (ext = 테이블 + 첫 샘플 반복)
    L = len(ext) - 1
    idx = (ph % 1.0) * L
    i = idx.astype(np.intp)
    fr = idx - i
    return ext[i] * (1.0 - fr) + ext[i + 1] * fr


def harm_table(weights, L=2048):
    # 배음 가중치 → 한 주기 파형 (RMS 1 로 정규화 — gain 이 곧 성부의 RMS)
    w = np.asarray(weights, dtype=float)
    k = np.arange(1, len(w) + 1)
    x = np.arange(L) / L
    phases = rng.uniform(0, TAU, len(w))
    tbl = (w[:, None] * np.sin(TAU * k[:, None] * x[None, :] + phases[:, None])).sum(0)
    rms = math.sqrt(float(np.mean(tbl * tbl))) or 1.0
    tbl /= rms
    return np.append(tbl, tbl[0])


def lowpass_w(k, f0, fc, order=2):
    return 1.0 / np.sqrt(1.0 + (k * f0 / fc) ** (2 * order))


def voice(f0, n, table, vib=(5.0, 0.004), detune=0.0, table2=None, morph=None, glide=None, vib_in=0.3):
    # 위상 누적 테이블 오실레이터: 비브라토 · 디튠 · 글라이드 · 두 테이블 사이 모핑
    t = tline(n)
    f = np.full(n, f0 * 2 ** (detune / 1200.0))
    if vib is not None:
        rate, depth = vib
        f = f * (1.0 + depth * np.sin(TAU * rate * t + rng.uniform(0, TAU)) * np.minimum(1.0, t / vib_in))
    if glide is not None:
        f = f * glide
    ph = np.cumsum(f) / RATE + rng.random()
    x = lookup(table, ph)
    if table2 is not None:
        x = x * (1.0 - morph) + lookup(table2, ph) * morph
    return x


# ───────────────────────────────────────────── 곡 버퍼 (순환 — 끝을 넘으면 처음으로)
class Song:
    def __init__(self, name, bpm, bars):
        self.name = name
        self.bpm = bpm
        self.beat = 60.0 / bpm
        self.bar = self.beat * 4
        self.bars = bars
        self.seconds = self.bar * bars
        self.n = int(round(self.seconds * RATE))
        self.dry = np.zeros((self.n, 2))
        self.send = np.zeros((self.n, 2))
        self.stats = {}
        # 균형 점검 (환경 변수 DEVBGM_GROUPS=1): 선율 · 타악 · 효과 그룹을 따로 쌓아 구간별 실제 합 RMS 를 비교
        self.groups = {g: np.zeros((self.n, 2)) for g in ('tonal', 'perc', 'fx')} if os.environ.get('DEVBGM_GROUPS') else None
        # 구간별 그룹 배율 (악절이 진행될수록 선율이 커지고 타악은 상대적으로 물러남 — 전주 → 절정의 고조)
        self.mul = {'tonal': 1.0, 'perc': 1.0, 'fx': 1.0}

    def section(self, tonal, perc, fx):
        self.mul = {'tonal': tonal, 'perc': perc, 'fx': fx}

    GROUP_OF = {'choir': 'tonal', 'brass': 'tonal', 'strings': 'tonal', 'organ': 'tonal', 'hi_strings': 'tonal', 'bell': 'tonal', 'sub': 'tonal',
                'perc': 'perc', 'perc_big': 'perc', 'metal': 'perc', 'crash': 'perc', 'heart': 'perc',
                'glitch': 'fx', 'whisper': 'fx', 'musicbox': 'fx', 'misc': 'fx'}

    def at(self, bar, beat=0.0):
        return bar * self.bar + beat * self.beat

    def loopf(self, f):
        # 곡 전체에 걸치는 지속음: 한 바퀴에 정수 주기가 되도록 살짝 보정 (이음새에서 위상이 맞음)
        return round(f * self.seconds) / self.seconds

    def add(self, t, sig, pan=0.0, gain=1.0, rev=0.3, bus='misc'):
        if sig.ndim == 1:
            a = (pan + 1.0) * math.pi / 4
            sig = np.stack([sig * math.cos(a), sig * math.sin(a)], 1)
        group = self.GROUP_OF.get(bus, 'fx')
        sig = sig * (gain * self.mul[group])
        st = self.stats.setdefault(bus, [0.0, 0, 0.0])
        st[0] += float(np.sum(sig * sig))
        st[1] += sig.shape[0]
        st[2] = max(st[2], float(np.max(np.abs(sig))))
        i0 = int(round(t * RATE)) % self.n
        m = sig.shape[0]
        wet = sig * rev
        grp = self.groups[group] if self.groups is not None else None
        pos = 0
        while pos < m:
            k = min(m - pos, self.n - i0)
            self.dry[i0:i0 + k] += sig[pos:pos + k]
            self.send[i0:i0 + k] += wet[pos:pos + k]
            if grp is not None:
                grp[i0:i0 + k] += sig[pos:pos + k]
            pos += k
            i0 = 0

    def report_groups(self):
        if self.groups is None:
            return
        seg = self.n // 12
        print('   그룹별 구간 RMS (드라이 · 1/12 씩, dBFS):')
        for g, buf in self.groups.items():
            print('     %-6s' % g, ' '.join('%6.1f' % dbfs(math.sqrt(float(np.mean(buf[i:i + seg] ** 2)))) for i in range(0, self.n - seg + 1, seg)))

    def region(self, t0, dur):
        i0 = int(round(t0 * RATE)) % self.n
        i1 = min(self.n, i0 + int(round(dur * RATE)))
        return i0, i1


# ───────────────────────────────────────────── 합창 (포먼트 합성)
# 모음별 포먼트 (Hz · dB · 대역폭) — 고전적인 가수 포먼트 표. m 은 자음 「ㅁ · ㄴ · ㄷ」 앞머리용 비음
FORMANTS = {
    'bass': {
        'a': ([600, 1040, 2250, 2450, 2750], [0, -7, -9, -9, -20], [60, 70, 110, 120, 130]),
        'e': ([400, 1620, 2400, 2800, 3100], [0, -12, -9, -12, -18], [40, 80, 100, 120, 120]),
        'i': ([250, 1750, 2600, 3050, 3340], [0, -30, -16, -22, -28], [60, 90, 100, 120, 120]),
        'o': ([400, 750, 2400, 2600, 2900], [0, -11, -21, -20, -40], [40, 80, 100, 120, 120]),
        'u': ([350, 600, 2400, 2675, 2950], [0, -20, -32, -28, -36], [40, 80, 100, 120, 120]),
        'm': ([250, 1000, 2200, 2800, 3300], [0, -26, -30, -36, -40], [60, 100, 120, 120, 120]),
    },
    'tenor': {
        'a': ([650, 1080, 2650, 2900, 3250], [0, -6, -7, -8, -22], [80, 90, 120, 130, 140]),
        'e': ([400, 1700, 2600, 3200, 3580], [0, -14, -12, -14, -20], [70, 80, 100, 120, 120]),
        'i': ([290, 1870, 2800, 3250, 3540], [0, -15, -18, -20, -30], [40, 90, 100, 120, 120]),
        'o': ([400, 800, 2600, 2800, 3000], [0, -10, -12, -12, -26], [70, 80, 100, 130, 135]),
        'u': ([350, 600, 2700, 2900, 3300], [0, -20, -17, -14, -26], [40, 60, 100, 120, 120]),
        'm': ([280, 1000, 2300, 2900, 3300], [0, -26, -30, -36, -40], [60, 100, 120, 120, 120]),
    },
    'alto': {
        'a': ([800, 1150, 2800, 3500, 4950], [0, -4, -20, -36, -60], [80, 90, 120, 130, 140]),
        'e': ([400, 1600, 2700, 3300, 4950], [0, -24, -30, -35, -60], [60, 80, 120, 150, 200]),
        'i': ([350, 1700, 2700, 3700, 4950], [0, -20, -30, -36, -60], [50, 100, 120, 150, 200]),
        'o': ([450, 800, 2830, 3500, 4950], [0, -9, -16, -28, -55], [70, 80, 100, 130, 135]),
        'u': ([325, 700, 2530, 3500, 4950], [0, -12, -30, -40, -64], [50, 60, 170, 180, 200]),
        'm': ([300, 1100, 2500, 3300, 4950], [0, -26, -32, -40, -60], [60, 100, 120, 150, 200]),
    },
    'soprano': {
        'a': ([800, 1150, 2900, 3900, 4950], [0, -6, -32, -20, -50], [80, 90, 120, 130, 140]),
        'e': ([350, 2000, 2800, 3600, 4950], [0, -20, -15, -40, -56], [60, 100, 120, 150, 200]),
        'i': ([270, 2140, 2950, 3900, 4950], [0, -12, -26, -26, -44], [60, 90, 100, 120, 120]),
        'o': ([450, 800, 2830, 3800, 4950], [0, -11, -22, -22, -50], [70, 80, 100, 130, 135]),
        'u': ([325, 700, 2700, 3800, 4950], [0, -16, -35, -40, -60], [50, 60, 170, 180, 200]),
        'm': ([320, 1100, 2600, 3600, 4950], [0, -26, -34, -40, -60], [60, 100, 120, 150, 200]),
    },
}
_vowel_cache = {}


def vowel_table(voice_type, vowel, f0, jitter=0.0):
    # 배음 k 의 세기 = 포먼트 응답(로렌츠 공명 합) × 성문 소리의 기울기 (1/k^0.6)
    key = (voice_type, vowel, round(f0, 1), round(jitter, 3))
    if key in _vowel_cache:
        return _vowel_cache[key]
    F, A, BW = FORMANTS[voice_type][vowel]
    fmax = 5200.0 if voice_type in ('bass', 'tenor') else 6500.0
    K = max(3, int(fmax / f0))
    k = np.arange(1, K + 1)
    f = k * f0
    H = np.zeros(K)
    for Fi, Ai, Bi in zip(F, A, BW):
        Fi = Fi * (1.0 + jitter)
        H += db(Ai) / (1.0 + ((f - Fi) / (Bi / 2.0)) ** 2)
    w = H / k ** 0.6
    tbl = harm_table(w)
    _vowel_cache[key] = tbl
    return tbl


def hiss(dur, lo=3500, hi=9000):
    n = int(dur * RATE)
    return fft_filter(noise(n), lo, hi, 2) * env(n, dur * 0.35, dur * 0.5) * 0.25


def click(dur=0.012):
    n = int(dur * RATE)
    return fft_filter(noise(n), 900, 5000, 2) * env(n, 0.001, dur * 0.8) * 0.5


def sing(song, t, dur, note, vtype, cons, vowel, vowel2=None, singers=4, gain=0.05, pan=0.0, spread=0.5, rev=0.55, attack=None, tail=None):
    # 한 음절: (자음) + 모음 (+ 끝자음). 여러 가수가 살짝 다른 음정 · 비브라토 · 포먼트 · 시작 시각 · 자리로 선다
    for s in range(singers):
        f0 = midi(note) * 2 ** (rng.normal(0, 7) / 1200.0)
        onset = rng.uniform(0.0, 0.06)
        rel = 0.22
        n = int((dur - onset + rel) * RATE)
        if n <= 0:
            continue
        jit = rng.normal(0, 0.035)
        tbl = vowel_table(vtype, vowel, f0, jit)
        tb2 = None
        morph = None
        tt = tline(n)
        if cons in 'mnd':
            tb2 = tbl
            tbl = vowel_table(vtype, 'm', f0, jit)
            morph = np.clip(tt / 0.09, 0.0, 1.0)
        elif vowel2 is not None:
            tb2 = vowel_table(vtype, vowel2, f0, jit)
            morph = np.clip((tt - dur * 0.25) / max(dur * 0.5, 0.01), 0.0, 1.0)
        a = attack if attack is not None else (0.015 if cons in 'kt' else 0.07)
        vib = (rng.uniform(4.6, 6.2), rng.uniform(0.0035, 0.006))
        x = voice(f0, n, tbl, vib=vib, table2=tb2, morph=morph, vib_in=0.35)
        e = env(n, a, rel, 1.4)
        # 길게 끄는 음은 중간에 조금 부풀어 오름
        if dur > 1.0:
            e *= 0.8 + 0.2 * np.sin(np.pi * np.clip(tt / dur, 0, 1))
        if cons == 'r':
            e *= 1.0 - 0.6 * np.exp(-tt / 0.07) * (0.5 + 0.5 * np.sin(TAU * 26 * tt))
        p = pan + rng.uniform(-spread, spread)
        song.add(t + onset, x * e, pan=p, gain=gain, rev=rev, bus='choir')
        if cons == 's':
            song.add(t + onset - 0.085, hiss(0.11), pan=p, gain=gain * 1.2, rev=rev * 0.6, bus='choir')
        elif cons in 'kt':
            song.add(t + onset - 0.008, click(), pan=p, gain=gain * 0.9, rev=rev * 0.5, bus='choir')
        if tail == 's':
            song.add(t + onset + dur - 0.03, hiss(0.12), pan=p, gain=gain * 1.0, rev=rev * 0.6, bus='choir')


def whisper(song, t, dur, vtype='alto', gain=0.02, pan=0.0):
    # 속삭임: 잡음을 모음 포먼트 모양으로 걸러 짧은 음절처럼
    n = int(dur * RATE)
    F, A, BW = FORMANTS[vtype][rng.choice(['a', 'e', 'i', 'o'])]
    X = np.fft.rfft(noise(n))
    f = np.fft.rfftfreq(n, 1.0 / RATE)
    H = np.zeros_like(f)
    for Fi, Ai, Bi in zip(F, A, BW):
        H += db(Ai) / (1.0 + ((f - Fi) / (Bi * 1.5)) ** 2)
    x = np.fft.irfft(X * H, n=n)
    x /= (np.sqrt(np.mean(x * x)) or 1.0)
    x *= env(n, dur * 0.3, dur * 0.4)
    song.add(t, x, pan=pan, gain=gain, rev=0.5, bus='whisper')


# ───────────────────────────────────────────── 현 · 금관 · 오르간
_tbl_cache = {}


def strings_table(f0, fc=3200):
    key = ('str', round(f0, 1), fc)
    if key not in _tbl_cache:
        K = max(3, min(200, int(9000 / f0)))
        k = np.arange(1, K + 1)
        _tbl_cache[key] = harm_table(lowpass_w(k, f0, fc, 2) / k)
    return _tbl_cache[key]


def brass_table(f0, fc):
    key = ('brs', round(f0, 1), fc)
    if key not in _tbl_cache:
        K = max(3, min(200, int(10000 / f0)))
        k = np.arange(1, K + 1)
        _tbl_cache[key] = harm_table(lowpass_w(k, f0, fc, 3) / k ** 0.55)
    return _tbl_cache[key]


def organ_table(f0, full=False):
    key = ('org', round(f0, 1), full)
    if key not in _tbl_cache:
        draw = {1: 1.0, 2: 0.75, 3: 0.45, 4: 0.55, 5: 0.12, 6: 0.3, 8: 0.28, 10: 0.08, 12: 0.12, 16: 0.1}
        if full:
            draw.update({3: 0.6, 4: 0.7, 6: 0.45, 8: 0.4, 12: 0.22, 16: 0.18, 20: 0.1, 24: 0.08})
        K = max(draw)
        w = np.zeros(K)
        for k, g in draw.items():
            if k * f0 < 14000:
                w[k - 1] = g
        _tbl_cache[key] = harm_table(w)
    return _tbl_cache[key]


def strings_bar(song, t0, notes, gain=0.045, step=None, accents=None, bright=3200, voices=3, octave_up=False):
    # 한 마디 오스티나토: notes = 16분음표마다 midi (None 이면 쉼), 3성부가 좌 · 중 · 우로 서서 살짝 어긋남
    step = step or song.beat / 4
    n_step = int(step * RATE)
    bar_n = n_step * len(notes)
    pans = np.linspace(-0.55, 0.55, voices)
    for v in range(voices):
        buf = np.zeros(bar_n + int(0.08 * RATE))
        det = rng.normal(0, 6)
        for i, nt in enumerate(notes):
            if nt is None:
                continue
            f0 = midi(nt + (12 if octave_up else 0))
            n = int(step * RATE * 1.15)
            e = env(n, 0.006, 0.04)
            e *= 0.55 + 0.45 * np.exp(-tline(n) / 0.06)          # 활을 끊어 긋는 느낌
            acc = accents[i] if accents else (1.0 if i % 4 == 0 else 0.75)
            x = voice(f0, n, strings_table(f0, bright), vib=(5.5, 0.002), detune=det + rng.normal(0, 2), vib_in=0.05)
            buf[i * n_step:i * n_step + n] += x * e * acc
        # 활 소음: 음표 머리마다 짧은 잡음
        bow = noise(bar_n) * 0.0
        for i, nt in enumerate(notes):
            if nt is None:
                continue
            m = int(0.02 * RATE)
            bow[i * n_step:i * n_step + m] = noise(m) * env(m, 0.002, 0.012)
        buf[:bar_n] += fft_filter(bow, 1500, 5000, 2) * 0.35
        song.add(t0, buf, pan=pans[v], gain=gain, rev=0.35, bus='strings')


def brass_note(song, t, dur, note, gain=0.06, pan=0.0, spread=0.3, voices=3, bright=4500, rev=0.42, swell=0.3, rip=1.5):
    for v in range(voices):
        f0 = midi(note)
        n = int((dur + 0.18) * RATE)
        tt = tline(n)
        glide = 2 ** (-rip / 12.0 * np.exp(-tt / 0.03))
        morph = 0.3 + 0.7 * (1.0 - np.exp(-tt / 0.07)) * (0.7 + 0.3 * np.exp(-tt / 0.6))
        if dur > 0.8:
            morph = np.minimum(1.0, morph * (0.9 + 0.35 * tt / dur))
        x = voice(f0, n, brass_table(f0, 1300), vib=(5.2, 0.0035), detune=rng.normal(0, 6), table2=brass_table(f0, bright), morph=morph, glide=glide, vib_in=0.5)
        e = env(n, 0.028, 0.16)
        e *= (1.0 + 0.25 * np.exp(-tt / 0.05))          # 어택 살짝 튐
        if dur > 0.8:
            e *= 1.0 + swell * np.clip(tt / dur, 0, 1)
        song.add(t, x * e, pan=pan + rng.uniform(-spread, spread), gain=gain / math.sqrt(voices), rev=rev, bus='brass')


def brass_stab(song, t, notes, gain=0.07, dur=0.32):
    for i, nt in enumerate(notes):
        brass_note(song, t, dur, nt, gain=gain, pan=(i - (len(notes) - 1) / 2) * 0.25, spread=0.1, voices=2, bright=6000, swell=0.0, rip=2.0)


def organ_note(song, t, dur, note, gain=0.04, pan=0.0, full=False, rev=0.45, sub16=False):
    f0 = midi(note)
    n = int((dur + 0.15) * RATE)
    e = env(n, 0.05, 0.13)
    x = voice(f0, n, organ_table(f0, full), vib=None) + voice(f0, n, organ_table(f0, full), vib=None, detune=1.5) * 0.7
    if sub16:
        x += voice(f0 / 2, n, organ_table(f0 / 2, False), vib=None) * 0.9
    chiff = fft_filter(noise(int(0.03 * RATE)), 1200, 4000, 2) * env(int(0.03 * RATE), 0.003, 0.02) * 0.6
    x[:len(chiff)] += chiff
    song.add(t, x * e, pan=pan, gain=gain / (1.6 if sub16 else 1.2), rev=rev, bus='organ')


def tremolo_strings(song, t, dur, note, gain=0.02, pan=0.0):
    f0 = midi(note)
    n = int((dur + 0.1) * RATE)
    tt = tline(n)
    trem = 0.55 + 0.45 * np.sin(TAU * rng.uniform(11, 14) * tt + rng.uniform(0, TAU))
    x = voice(f0, n, strings_table(f0, 6000), vib=(6.0, 0.004), detune=rng.normal(0, 8))
    song.add(t, x * env(n, 0.3, 0.3) * trem, pan=pan, gain=gain, rev=0.5, bus='hi_strings')


# ───────────────────────────────────────────── 타악 · 종 · 효과
def drum(dur, f_hi, f_lo, decay, noise_amt=0.6, noise_dec=0.02, noise_hi=2500):
    n = int(dur * RATE)
    t = tline(n)
    fq = f_lo + (f_hi - f_lo) * np.exp(-t * 28)
    ph = np.cumsum(fq) / RATE
    body = np.sin(TAU * ph) * np.exp(-t / decay)
    nz = fft_filter(noise(n), None, noise_hi, 2) * np.exp(-t / noise_dec) * noise_amt
    return (body + nz) * env(n, 0.0015, 0.05)


def taiko(gain_var=0.0):
    return drum(0.9, 190, 62, 0.28, 0.8, 0.018, 3000) * (1.0 + gain_var)


def bass_drum():
    return drum(1.8, 110, 36, 0.55, 0.7, 0.03, 1200)


def heartbeat():
    n = int(0.9 * RATE)
    t = tline(n)

    def thump(x):
        on = x > 0
        x = np.maximum(x, 0.0)
        fq = (36 + 26 * np.exp(-x * 14)) * on          # 위상은 때리는 순간부터 쌓임 (시작 샘플 0 → 딸깍 없음)
        return np.sin(TAU * np.cumsum(fq) / RATE) * np.exp(-x * 9.0) * on
    y = thump(t) + 0.65 * thump(t - 0.24)
    return fft_filter(y, None, 220, 2) * env(n, 0.002, 0.1)


def metal_hit(base=820.0, dur=1.4):
    # 쇠망치 · 모루: 비조화 배음 + 날카로운 잡음 어택
    n = int(dur * RATE)
    t = tline(n)
    ratios = [1.0, 1.76, 2.41, 3.33, 4.2, 5.61, 6.9]
    out = np.zeros(n)
    for i, r in enumerate(ratios):
        out += np.sin(TAU * base * r * t + rng.uniform(0, TAU)) * np.exp(-t * (2.2 + i * 1.6)) / (1 + i * 0.6)
    out += fft_filter(noise(n), 2500, 11000, 2) * np.exp(-t / 0.012) * 1.2
    return out * env(n, 0.0008, 0.1) * 0.5


def bell(note, dur=4.5, bright=1.0):
    # 교회 종: 험 · 프라임 · 티어스 · 퀸트 · 노미널 … (들리는 음 = 노미널/2 = note)
    f = midi(note)
    n = int(dur * RATE)
    t = tline(n)
    parts = [(0.5, 1.0, 1.0), (1.0, 0.85, 1.4), (1.2, 0.55, 1.9), (1.5, 0.45, 2.2), (2.0, 0.7, 2.8), (2.5, 0.28, 3.6), (2.67, 0.22, 3.9), (3.0, 0.18, 4.4), (4.0, 0.1 * bright, 5.5), (5.33, 0.06 * bright, 6.5)]
    out = np.zeros(n)
    for r, g, d in parts:
        if f * r < 16000:
            out += g * np.sin(TAU * f * r * t + rng.uniform(0, TAU)) * np.exp(-t * d / (dur / 4.5))
    strike = fft_filter(noise(n), 1500, 8000, 2) * np.exp(-t / 0.01) * 0.8
    return (out + strike) * env(n, 0.001, 0.3) * 0.45


def tamtam(dur=6.0):
    n = int(dur * RATE)
    t = tline(n)
    out = np.zeros(n)
    for fr in rng.uniform(70, 900, 28):
        out += np.sin(TAU * fr * t + rng.uniform(0, TAU)) * np.exp(-t * rng.uniform(0.5, 1.8)) * rng.uniform(0.3, 1.0)
    out = out / 8.0 * (1.0 - 0.6 * np.exp(-t / 0.5))
    out += fft_filter(noise(n), 300, 6000, 2) * np.exp(-t / 0.6) * 0.5
    return out * env(n, 0.004, 0.5)


def crash(dur=2.4, lo=2800, reverse=False):
    n = int(dur * RATE)
    t = tline(n)
    x = fft_filter(np.stack([noise(n), noise(n)], 1), lo, 14000, 2)
    e = np.exp(-t / (dur / 3.5))
    if reverse:
        e = (t / dur) ** 2.5 * (1.0 - np.exp(-(dur - t) / 0.02))
    return x * e[:, None] * env(n, 0.002, 0.03)[:, None] * 0.5


def sub_drop(dur=1.6):
    n = int(dur * RATE)
    t = tline(n)
    fq = 30 + 70 * np.exp(-t * 3.0)
    return np.sin(TAU * np.cumsum(fq) / RATE) * np.exp(-t / 0.8) * env(n, 0.003, 0.2)


def glitch_tick(dur=None):
    # 데이터 틱: 몇 주기짜리 사각파를 4비트로 깨뜨림
    dur = dur or rng.uniform(0.012, 0.045)
    n = int(dur * RATE)
    f = rng.uniform(700, 5200)
    x = np.sign(np.sin(TAU * f * tline(n)))
    x = np.round(x * 7) / 7
    return fft_filter(x, 600, 9000, 2) * env(n, 0.001, 0.004) * 0.6


def data_stream(song, t0, dur, gain=0.012, rate=None):
    # 코드가 흐르는 소리: 1/32 박마다 무작위 음높이의 짧은 사각파 블립이 좌우로 튐
    step = rate or song.beat / 8
    k = 0
    t = t0
    scale = [0, 3, 5, 6, 10, 12, 15, 18]
    while t < t0 + dur:
        if rng.random() < 0.7:
            f = midi(74 + rng.choice(scale))
            n = int(step * 0.7 * RATE)
            x = np.sign(np.sin(TAU * f * tline(n)))
            x = np.round(x * 3) / 3 * env(n, 0.001, 0.01)
            song.add(t, fft_filter(x, 400, 7000, 2), pan=(-0.6 if k % 2 else 0.6), gain=gain * rng.uniform(0.5, 1.0), rev=0.15, bus='glitch')
        t += step
        k += 1


def music_box(song, t, dur, note, gain=0.035, pan=0.0):
    # 오르골: 맑은 금속성 배음, 살짝 어긋난 옥타브 (오싹한 자장가)
    f = midi(note)
    n = int(dur * RATE)
    tt = tline(n)
    parts = [(1.0, 1.0, 1.4), (2.004, 0.35, 2.5), (3.98, 0.22, 3.2), (6.1, 0.1, 4.0), (8.3, 0.05, 5.0)]
    x = sum(g * np.sin(TAU * f * r * tt + rng.uniform(0, TAU)) * np.exp(-tt * d) for r, g, d in parts if f * r < 16000)
    pluck = np.zeros(n)
    m = int(0.008 * RATE)
    pluck[:m] = fft_filter(noise(m), 2500, 9000, 2) * env(m, 0.0005, 0.005) * 0.6
    song.add(t, (x + pluck) * env(n, 0.001, 0.2) * 0.5, pan=pan, gain=gain, rev=0.6, bus='musicbox')


# ───────────────────────────────────────────── 후처리 효과 (드라이 · 센드 버퍼에 같이)
def _crush(seg, bits, hold):
    q = 2 ** (bits - 1)
    out = np.round(seg * q) / q
    if hold > 1:
        out = np.repeat(out[::hold], hold, axis=0)[:len(seg)]
    return out


def stutter(song, t, slice_dur, repeats, bits=6, hold=3, gain=1.0):
    # 직전 조각을 짧게 되감아 반복 (비트크러시) — 세계가 버벅이는 느낌
    for buf in (song.dry, song.send):
        i0, i1 = song.region(t, slice_dur)
        piece = _crush(buf[i0:i1].copy(), bits, hold)
        m = len(piece)
        fade = env(m, 0.002, 0.004)[:, None]
        j_end = i0
        for r in range(repeats):
            j0 = i0 + m * (r + 1)
            if j0 + m > song.n:
                break
            buf[j0:j0 + m] = buf[j0:j0 + m] * 0.25 + piece * fade * gain * (1.0 - 0.12 * r)
            j_end = j0 + m
        # 되감기가 끝나면 원래 소리가 0.25 → 1 로 4ms 동안 돌아옴 (계단 방지)
        k = min(int(0.004 * RATE), j_end - i0)
        if k > 0:
            buf[j_end - k:j_end] *= np.linspace(1.0, 4.0, k)[:, None]


def system_crash(song, bar_t, bar_len, pattern, bits=5, hold=6, pitch_fall=True):
    # 한 마디가 통째로 깨짐: 거친 양자화 · 샘플 홀드 · 16분 게이트, 마디 끝은 점점 더 느린 홀드(음정이 무너지는 느낌)
    for buf in (song.dry, song.send):
        i0, i1 = song.region(bar_t, bar_len)
        seg = buf[i0:i1].copy()
        slots = len(pattern)
        m = (i1 - i0) // slots
        out = np.zeros_like(seg)
        for s in range(slots):
            if not pattern[s]:
                continue
            h = hold
            if pitch_fall and s >= slots - 4:
                h = hold * 2 ** (s - slots + 5)
            piece = _crush(seg[s * m:(s + 1) * m], bits, h)
            out[s * m:s * m + len(piece)] = piece * env(len(piece), 0.001, 0.006)[:, None]
        buf[i0:i1] = out * 1.15
        k = min(int(0.004 * RATE), song.n - i1)
        if k > 0:
            buf[i1:i1 + k] *= np.linspace(0.0, 1.0, k)[:, None]      # 다음 마디가 0 에서 올라옴 (계단 방지)


# ───────────────────────────────────────────── 잔향 · 마스터
def make_ir(seconds=3.4, rt_low=3.6, rt_mid=2.9, rt_high=1.5, predelay=0.022):
    n = int(seconds * RATE)
    t = tline(n)
    ir = np.zeros((n, 2))
    for ch in range(2):
        w = noise(n)
        for band, rt in ((fft_filter(w, None, 260, 2), rt_low), (fft_filter(w, 260, 2600, 2), rt_mid), (fft_filter(w, 2600, None, 2), rt_high)):
            ir[:, ch] += band * np.exp(-6.908 * t / rt)
    ir *= (1.0 - np.exp(-t / 0.012))[:, None]
    # 초기 반사
    for d, g, p in ((0.009, 0.7, -0.6), (0.016, 0.55, 0.7), (0.024, 0.45, -0.3), (0.033, 0.4, 0.4), (0.047, 0.3, -0.8), (0.061, 0.25, 0.5)):
        i = int(d * RATE)
        a = (p + 1) * math.pi / 4
        ir[i, 0] += g * math.cos(a) * 3.0
        ir[i, 1] += g * math.sin(a) * 3.0
    pre = np.zeros((int(predelay * RATE), 2))
    ir = np.concatenate([pre, ir])
    ir /= math.sqrt(float(np.sum(ir * ir)) / 2)      # 단위 에너지 (채널 평균)
    return ir


def convolve_loop(x, ir):
    # 순환 컨볼루션: 곡 끝의 잔향 꼬리가 곡 머리로 이어짐 (반복 이음새가 끊기지 않음)
    n = x.shape[0]
    X = np.fft.rfft(x, n=n, axis=0)
    H = np.fft.rfft(ir, n=n, axis=0)
    return np.fft.irfft(X * H, n=n, axis=0)


def block_gain(x, block, fn, attack_coef, release_coef):
    # 블록 단위 게인 (압축기 · 리미터 공용): fn(블록) → 목표 게인, 어택 · 릴리스로 매끈하게, 샘플로 보간
    n = x.shape[0]
    nb = (n + block - 1) // block
    want = np.ones(nb)
    for b in range(nb):
        want[b] = fn(x[b * block:(b + 1) * block])
    g = np.ones(nb)
    cur = 1.0
    for b in range(nb):
        w = want[b]
        cur = w + (cur - w) * (attack_coef if w < cur else release_coef)
        g[b] = cur
    centers = (np.arange(nb) + 0.5) * block
    return np.interp(np.arange(n), centers, g)


def master(mix, target_rms_db=-15.0, ceiling=0.95):
    # 1) 25Hz 아래 제거  2) 완만한 압축 (2:1, -13dBFS 위 — 절정만 살짝 눌러 전주→절정 약 8dB 의 고조는 남김)  3) RMS 를 기준에  4) 룩어헤드 리미터  5) 안전 소프트클립
    mix = fft_filter(mix, 25, None, 2)
    thr = db(-13.0)

    def comp(blk):
        r = math.sqrt(float(np.mean(blk * blk))) + 1e-9
        if r <= thr:
            return 1.0
        over = 20 * math.log10(r / thr)
        return db(-over * (1.0 - 1.0 / 2.0))
    g = block_gain(mix, 256, comp, math.exp(-256 / RATE / 0.015), math.exp(-256 / RATE / 0.3))
    mix = mix * g[:, None]
    rms = math.sqrt(float(np.mean(mix * mix)))
    mix *= db(target_rms_db) / rms
    pk = float(np.max(np.abs(mix)))
    print('   압축 뒤 RMS %.1f dBFS → 기준 %.1f, 리미터 전 피크 %.2f (%.1f dBFS)' % (dbfs(rms), target_rms_db, pk, dbfs(pk)))

    def lim(blk):
        p = float(np.max(np.abs(blk))) + 1e-9
        return min(1.0, ceiling / p)
    g = block_gain(mix, 64, lim, 0.0, math.exp(-64 / RATE / 0.09))
    # 룩어헤드: 게인을 한 블록 앞당김
    g = np.concatenate([g[64:], np.full(64, g[-1])])
    mix = mix * g[:, None]
    knee = 0.9
    a = np.abs(mix)
    soft = np.where(a > knee, np.sign(mix) * (knee + (1.0 - knee) * np.tanh((a - knee) / (1.0 - knee))), mix)
    return soft


def analyze(path):
    with wave.open(path, 'rb') as w:
        ch, sw, sr, n = w.getnchannels(), w.getsampwidth(), w.getframerate(), w.getnframes()
        data = np.frombuffer(w.readframes(n), dtype='<i2').astype(float).reshape(-1, ch) / 32768.0
    peak = float(np.max(np.abs(data)))
    rms = math.sqrt(float(np.mean(data * data)))
    mono = data.mean(1)
    # 이음새: 마지막 → 첫 샘플의 계단을 보통 인접 샘플 차이(끝 · 처음 0.1초의 95% 분위)와 비교
    seam = float(np.max(np.abs(data[0] - data[-1])))
    around = np.concatenate([np.abs(np.diff(mono[:int(0.1 * sr)])), np.abs(np.diff(mono[-int(0.1 * sr):]))])
    typical = float(np.percentile(around, 95))
    # 8마디(≈12.8초)마다의 RMS
    secs = n / sr
    print('   %s: %d채널 %dbit %dHz · %.1f초 · 피크 %.3f (%.2f dBFS) · RMS %.1f dBFS · 크레스트 %.1f dB' % (os.path.basename(path), ch, sw * 8, sr, secs, peak, dbfs(peak), dbfs(rms), dbfs(peak) - dbfs(rms)))
    print('   반복 이음새 계단 %.4f FS (주변 95%% 인접 샘플 차 %.4f → %s)' % (seam, typical, '정상' if seam <= typical * 2.5 else '주의'))
    seg = int(sr * secs / 12)
    bands = ['%.1f' % dbfs(math.sqrt(float(np.mean(data[i:i + seg] ** 2)))) for i in range(0, n - seg + 1, seg)]
    print('   구간 RMS (1/12 씩, dBFS):', ' '.join(bands))
    # 대역별 에너지 비율 (저음 과잉 · 고음 쏠림 점검) · 좌우 상관(스테레오 폭)
    X = np.abs(np.fft.rfft(mono)) ** 2
    f = np.fft.rfftfreq(n, 1.0 / sr)
    edges = [(0, 60, '초저음'), (60, 250, '저음'), (250, 1000, '중저음'), (1000, 4000, '중고음'), (4000, 22050, '고음')]
    tot = float(X.sum())
    print('   대역 에너지:', ' · '.join('%s %.0f%%' % (nm, 100 * float(X[(f >= lo) & (f < hi)].sum()) / tot) for lo, hi, nm in edges))
    corr = float(np.corrcoef(data[:, 0], data[:, 1])[0, 1])
    print('   좌우 상관 %.2f (1 = 모노 · 0.3~0.8 = 넓은 스테레오)' % corr)
    return {'peak': peak, 'rms': rms, 'seam': seam, 'secs': secs}


def save(song, mix, path):
    data = np.clip(mix, -1.0, 1.0)
    pcm = (data * 32767.0).astype('<i2')
    with wave.open(path, 'wb') as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(pcm.tobytes())
    meta = path + '.meta'
    if not os.path.exists(meta):
        # 보스 곡 .meta 그대로 (스트리밍 · Vorbis 0.7 · 백그라운드 로드), guid 만 새로 · 스테레오라 forceToMono 0
        tpl = open(os.path.join(MUSIC, 'bgm_boss.wav.meta'), encoding='utf-8').read()
        tpl = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, tpl, count=1)
        tpl = tpl.replace('forceToMono: 1', 'forceToMono: 0')
        open(meta, 'w', encoding='utf-8', newline='\n').write(tpl)
    print('wrote', path, '%.1fs' % (song.n / RATE))


def finish(song, wet=0.33, target_db=-15.0):
    t0 = time.time()
    print('   버스별 RMS (활동 구간 평균) · 피크:')
    for bus, (e, cnt, pk) in sorted(song.stats.items()):
        print('     %-11s %6.1f dBFS  피크 %.3f' % (bus, dbfs(math.sqrt(e / max(cnt, 1))), pk))
    song.report_groups()
    ir = make_ir()
    wetsig = fft_filter(convolve_loop(song.send, ir), 140, None, 2)
    mix = song.dry + wetsig * wet
    out = master(mix, target_db)
    print('   잔향 · 마스터 %.1f초' % (time.time() - t0))
    return out


# ───────────────────────────────────────────── 화음 자료
def chord(root, kind):
    # kind: m 단3화음 · M 장3화음 · 7 딸림7 · dim7 감7 · m9 단3화음+반음 위(공포 클러스터)
    table = {'m': [0, 3, 7], 'M': [0, 4, 7], '7': [0, 4, 7, 10], 'dim7': [0, 3, 6, 9], 'm9': [0, 1, 3, 7]}
    return root, [root + i for i in table[kind]]


# 현 오스티나토 (근음 기준 반음 · 16분음표 16개): 반음 · 단3도 · 삼전음이 번갈아
OSTI_I = [0, 0, 12, 0, 0, 0, 11, 0, 0, 0, 12, 0, 3, 6, 3, 1]
OSTI_II = [0, 12, 0, 12, 0, 11, 0, 11, 0, 12, 0, 12, 3, 6, 7, 6]
OSTI_C = [0, 12, 0, 12, 0, 11, 0, 11, 0, 12, 0, 12, 6, 7, 6, 1]
ACC_C = [1.0, 0.6, 0.8, 0.6, 1.0, 0.6, 0.8, 0.6, 1.0, 0.6, 0.8, 0.6, 1.1, 0.9, 1.0, 0.9]

# 금관 주제 (midi, 박) 8마디 — 「진노의 날」을 닮은 하강 선율, 끝은 삼전음(라 단조의 솔♯)으로 떨어짐
THEME = [(62, 3), (65, 1), (64, 2), (62, 2), (60, 2), (62, 2), (57, 4), (58, 3), (57, 1), (56, 2), (57, 2), (61, 2), (62, 2), (68, 4)]

# 합창 음절 (시작 박 · 길이 박 · 자음 · 모음 · 끝자음) — 두 마디(8박) 단위 라틴풍 「Sanc-tus Do-mi-ne」
CHANT = [(0, 1.5, 's', 'a', None), (1.5, 0.5, 'k', 'u', 's'), (2, 1.0, 't', 'u', None), (3, 1.0, 's', 'o', None), (4, 1.5, 'd', 'o', None), (5.5, 0.5, 'm', 'i', None), (6, 2.0, 'n', 'e', None)]
# 절정의 빠른 음절 (한 마디 4박) 「Ky-ri-e  Rex · Mor-tis De-us」
CHANT_FAST = [[(0, 0.5, 'k', 'i', None), (0.5, 0.5, 'r', 'i', None), (1, 1.0, '-', 'e', None), (2, 1.5, 'r', 'e', 's')],
              [(0, 0.5, 'm', 'o', 'r'), (0.5, 0.5, 't', 'i', 's'), (1, 1.0, 'd', 'e', None), (2, 1.5, '-', 'u', 's')]]


# ───────────────────────────────────────────── 1. 개발자전 (라 단조 · 150 BPM · 60마디 = 96초)
def compose_dev():
    S = Song('bgm_developer', 150, 60)
    B, bar = S.beat, S.bar
    D = 50  # 라(D3)
    Dm, Bb, A7, Eb, Gs, Gm = chord(D, 'm'), chord(D - 4, 'M'), chord(D - 5, '7'), chord(D + 1, 'M'), chord(D - 6, 'dim7'), chord(D - 7, 'm')
    PH_A, PH_B, PH_C, PH_D = [Dm, Dm, Bb, A7], [Dm, Eb, Gs, A7], [Dm, Bb, Eb, Gs], [Dm, Gm, Eb, A7]
    # 마디 8~55 의 화음 (두 마디마다 하나)
    prog = {}
    for p, phrase in enumerate([PH_A, PH_B, PH_A, PH_B, PH_C, PH_D]):
        for c, ch in enumerate(phrase):
            prog[8 + p * 8 + c * 2] = ch
    prog[56] = chord(D, 'm9')

    def chord_at(bar_i):
        return prog[bar_i - (bar_i % 2)] if bar_i >= 8 else Dm

    # ── 0. 첫 타격 (반복 지점이기도 함)
    S.section(1.0, 0.9, 6.5)
    S.add(0, tamtam(6.5), gain=0.5, rev=0.5, bus='perc_big')
    S.add(0, bass_drum(), gain=0.9, rev=0.2, bus='perc_big')
    S.add(0.005, taiko(), pan=-0.2, gain=0.7, rev=0.3, bus='perc')
    S.add(0, sub_drop(1.8), gain=0.55, rev=0.0, bus='sub')
    S.add(0, metal_hit(620, 2.5), pan=0.35, gain=0.45, rev=0.55, bus='metal')
    S.add(0, bell(D - 12, 6.0), pan=-0.3, gain=0.5, rev=0.6, bus='bell')
    S.add(0.0, crash(3.0, 2200), gain=0.45, rev=0.5, bus='crash')
    brass_stab(S, 0.0, [D - 24, D - 12, D, D + 3, D + 1], gain=0.09, dur=0.8)       # 라1 · 라2 · 라3 · 다3 · 라♯3(반음 클러스터)
    for nt, vt in ((D - 12, 'bass'), (D - 5, 'bass'), (D, 'tenor'), (D + 1, 'tenor')):
        sing(S, 0.0, 0.55, nt, vt, 'h', 'a', singers=4, gain=0.045 if vt == 'bass' else 0.035, attack=0.012, spread=0.8)
    for _ in range(5):
        S.add(rng.uniform(0.05, 0.7), glitch_tick(), pan=rng.uniform(-0.9, 0.9), gain=0.05, rev=0.2, bus='glitch')

    # ── 전주 (0~7 마디): 저음 드론 · 심장 박동 · 낮은 합창 「우—아」 · 삼전음 종 · 드문 데이터 틱
    S.section(0.8, 0.75, 6.5)
    n = S.n
    tt = tline(n)
    f1 = S.loopf(midi(D - 24))
    pulse = 0.65 + 0.35 * np.sin(TAU * S.loopf(S.bpm / 60.0 / 2) * tt - math.pi / 2)
    drone = (np.sin(TAU * f1 * tt) + 0.35 * np.sin(TAU * 2 * f1 * tt + 1.0) + 0.12 * np.sin(TAU * 3 * f1 * tt)) * pulse
    # 전주와 끝에서는 조용히, 본론에서 크게 (경계는 둥글게)
    # 전주와 끝에서는 조용히, 본론에서 크게 (양 끝 0 → 이음새가 이어짐)
    lvl = np.interp(tt, [0, 1.5, S.at(7.9), S.at(8), S.at(55.5), S.at(56.5), S.at(59.5), S.seconds], [0.0, 0.45, 0.6, 1.0, 1.0, 0.55, 0.4, 0.0])
    S.add(0, drone * lvl, gain=0.11, rev=0.05, bus='sub')
    for k in range(2, 16):
        S.add(S.at(0, k * 2) + 0.02, heartbeat(), gain=0.5 + 0.02 * k, rev=0.08, bus='heart')
    sing(S, S.at(1), S.at(7, 2.5) - S.at(1), D - 12, 'bass', '-', 'u', 'a', singers=5, gain=0.055, pan=-0.25, spread=0.5, attack=1.4)      # 라2
    sing(S, S.at(1, 1), S.at(7, 2.5) - S.at(1, 1), D - 5, 'bass', '-', 'u', 'a', singers=4, gain=0.04, pan=0.3, spread=0.5, attack=1.6)    # 마2
    sing(S, S.at(4), S.at(7, 2.5) - S.at(4), D - 11, 'bass', '-', 'o', 'a', singers=3, gain=0.03, pan=0.0, spread=0.6, attack=1.2)        # 반음 위(라♯2) 클러스터
    sing(S, S.at(5, 2), S.at(7, 2.5) - S.at(5, 2), D + 6, 'tenor', '-', 'o', 'a', singers=3, gain=0.022, pan=0.1, spread=0.6, attack=1.0)  # 삼전음(레♯3)
    organ_note(S, S.at(1), S.at(7, 2.5) - S.at(1), D - 12, gain=0.03, pan=0.0, sub16=True, rev=0.5)
    S.add(S.at(4), bell(D - 12, 5.0), pan=0.3, gain=0.3, rev=0.65, bus='bell')
    S.add(S.at(6), bell(D - 6, 5.0, 1.3), pan=-0.4, gain=0.34, rev=0.65, bus='bell')
    for _ in range(14):
        S.add(rng.uniform(S.at(1), S.at(7)), glitch_tick(), pan=rng.uniform(-0.9, 0.9), gain=0.045, rev=0.25, bus='glitch')
    for _ in range(6):
        whisper(S, rng.uniform(S.at(3), S.at(7)), rng.uniform(0.18, 0.4), 'alto', gain=0.014, pan=rng.uniform(-0.8, 0.8))
    # 7마디: 팀파니 몰아치기 → 8마디 첫 박 (3박째는 반 박 숨 고르기)
    for i in range(8):
        S.add(S.at(7, i * 0.25), taiko(), pan=(i % 2 - 0.5) * 0.4, gain=0.3 + 0.07 * i, rev=0.3, bus='perc')
    for i in range(6):
        S.add(S.at(7, 2.5 + i * 0.25), taiko(), pan=(i % 2 - 0.5) * 0.5, gain=0.7 + 0.08 * i, rev=0.3, bus='perc')
    S.add(S.at(6, 2), crash(2.4, 2500, reverse=True), gain=0.45, rev=0.4, bus='crash')
    S.add(S.at(7, 3.75), sub_drop(0.6), gain=0.3, rev=0.0, bus='sub')

    # ── 본론 I (8~23) · 본론 II (24~39) · 절정 (40~55)
    for b in range(8, 56):
        root, tones = chord_at(b)
        sec = 1 if b < 24 else 2 if b < 40 else 3
        phrase_start = (b - 8) % 8 == 0
        chord_start = b % 2 == 0
        S.section(*{1: (1.15, 0.6, 6.5), 2: (1.45, 0.68, 7.0), 3: (1.8, 0.8, 8.0)}[sec])
        if sec == 1:
            for _ in range(rng.integers(1, 3)):
                S.add(S.at(b, rng.uniform(0, 4)), glitch_tick(), pan=rng.uniform(-0.9, 0.9), gain=0.04, rev=0.25, bus='glitch')
        # 현 오스티나토 (낮은 현 · 절정에는 비올라 옥타브 위 겹침)
        patt = OSTI_I if sec == 1 else OSTI_II if sec == 2 else OSTI_C
        osti = [root - 12 + s for s in patt]
        if b == 55:
            osti = [root - 12 + s for s in [0, 0, 12, 0, 0, 0, 11, 0, 0, 12, 12, 12, 6, 6, 1, 1]]
        strings_bar(S, S.at(b), osti, gain=0.05 if sec == 1 else 0.06 if sec == 2 else 0.068, accents=ACC_C if sec == 3 else None, bright=3000 + 600 * sec)
        if sec == 3:
            strings_bar(S, S.at(b), osti, gain=0.03, accents=ACC_C, bright=5000, voices=2, octave_up=True)
        # 오르간 페달 (2마디) · 손건반 화음 (본론 II 부터) · 절정은 믹스처 풀 스톱
        if chord_start:
            organ_note(S, S.at(b), 2 * bar - 0.05, root - 12, gain=0.045 if sec < 3 else 0.055, sub16=True, full=sec == 3)
            if sec >= 2:
                for i, nt in enumerate(tones):
                    organ_note(S, S.at(b), 2 * bar - 0.05, nt, gain=0.022 if sec == 2 else 0.028, pan=(i - 1) * 0.35, full=sec == 3)
                if sec == 3:
                    organ_note(S, S.at(b), 2 * bar - 0.05, tones[0] + 12, gain=0.016, pan=0.0, full=True)
        # 저음 펄스 — 심장 박동이 본론 I 에서 조용히 이어짐
        if sec == 1:
            S.add(S.at(b) + 0.02, heartbeat(), gain=0.3, rev=0.05, bus='heart')
            S.add(S.at(b, 2) + 0.02, heartbeat(), gain=0.3, rev=0.05, bus='heart')
        # 타악
        if sec == 1:
            hits = [(0, 1.0), (2, 0.85), (3.75, 0.6)]
        elif sec == 2:
            hits = [(0, 1.0), (1.5, 0.7), (2, 0.9), (3, 0.75), (3.5, 0.7)]
        else:
            hits = [(i * 0.5, 1.0 if i % 2 == 0 else 0.7) for i in range(8)]
        for beat, g in hits:
            S.add(S.at(b, beat), taiko(rng.uniform(-0.05, 0.05)), pan=rng.uniform(-0.3, 0.3), gain=0.55 * g * (1.0 + 0.12 * (sec - 1)), rev=0.28, bus='perc')
        if chord_start or sec == 3:
            S.add(S.at(b), bass_drum(), gain=0.75 if sec < 3 else 0.9, rev=0.15, bus='perc_big')
        if sec == 3 and b % 2 == 1:
            S.add(S.at(b, 2), bass_drum(), gain=0.6, rev=0.15, bus='perc_big')
        # 쇠망치
        metal_steps = [] if sec == 1 and b % 2 == 0 else [3] if sec == 1 else [1, 3] if sec == 2 else [0.5, 1.5, 2.5, 3.5]
        for st in metal_steps:
            S.add(S.at(b, st), metal_hit(rng.choice([620, 740, 820, 980]), 1.1), pan=rng.uniform(-0.7, 0.7), gain=0.18 if sec < 3 else 0.22, rev=0.5, bus='metal')
        # 종: 화음이 바뀔 때 근음 (절정은 마디마다)
        if (chord_start and sec >= 2) or sec == 3:
            S.add(S.at(b), bell(root - 12 if sec < 3 else root, 4.0, 1.0 if sec < 3 else 1.3), pan=rng.uniform(-0.5, 0.5), gain=0.22 if sec < 3 else 0.26, rev=0.6, bus='bell')
        # 크래시: 악절 머리
        if phrase_start and sec >= 2:
            S.add(S.at(b), crash(2.6, 2600), gain=0.4, rev=0.45, bus='crash')
        # 금관: 본론 I 뒷부분 스탭 → 본론 II 호른 주제 → 절정 트럼펫 옥타브 위 + 호른
        if sec == 1 and b >= 16 and chord_start:
            brass_stab(S, S.at(b), [root - 12, tones[1], tones[2]], gain=0.065, dur=0.45)
        if sec >= 2 and phrase_start:
            t = S.at(b)
            for nt, beats in THEME:
                d = beats * B - 0.06
                brass_note(S, t, d, nt, gain=0.075 if sec == 2 else 0.085, pan=0.0, spread=0.35, voices=3, bright=4200 if sec == 2 else 5200)
                if sec == 3:
                    brass_note(S, t, d, nt + 12, gain=0.05, pan=0.0, spread=0.5, voices=3, bright=7000, rip=1.0)
                    brass_note(S, t, d, nt - 12, gain=0.045, pan=0.0, spread=0.3, voices=2, bright=2600, rip=1.0)
                t += beats * B
        if sec == 2 and chord_start and not phrase_start:
            brass_stab(S, S.at(b, 3.5), [root - 12, tones[0], tones[1]], gain=0.05, dur=0.2)
        # 합창
        if chord_start:
            parts = [(root - 12, 'bass', 0.055, -0.4), (tones[2] - 12, 'bass', 0.04, 0.35), (tones[1], 'tenor', 0.028 if sec == 1 else 0.034, 0.0)]
            if sec >= 2:
                parts.append((tones[0], 'tenor', 0.03, 0.15))
            if sec == 1 or sec == 2:
                for beat, dur, c, v, tail in CHANT:
                    for nt, vt, g, pn in parts:
                        sing(S, S.at(b, beat), dur * B - 0.04, nt, vt, c, v, singers=4 if vt == 'bass' else 3, gain=g, pan=pn, spread=0.45, tail=tail)
            if sec == 2:
                # 길게 받쳐 주는 「오—아」
                for nt, vt, g, pn in parts[:2]:
                    sing(S, S.at(b), 2 * bar - 0.1, nt, vt, '-', 'o', 'a', singers=3, gain=g * 0.7, pan=pn, spread=0.5, attack=0.6)
            if sec == 3:
                for nt, vt, g, pn in parts:
                    sing(S, S.at(b), 2 * bar - 0.1, nt, vt, '-', 'a', 'o', singers=4, gain=g * 1.1, pan=pn, spread=0.55, attack=0.25)
                sing(S, S.at(b), 2 * bar - 0.1, tones[0] + 12, 'tenor', '-', 'a', singers=4, gain=0.03, pan=-0.1, spread=0.6, attack=0.3)
                # 높은 현 트레몰로
                for i, nt in enumerate(tones):
                    tremolo_strings(S, S.at(b), 2 * bar, nt + 24, gain=0.018, pan=(i - 1) * 0.5)
        if sec == 3:
            fast = CHANT_FAST[(b // 2) % 2]
            for beat, dur, c, v, tail in fast:
                sing(S, S.at(b, beat), dur * B - 0.03, tones[1], 'tenor', c, v, singers=4, gain=0.036, pan=0.2, spread=0.5, tail=tail)
                sing(S, S.at(b, beat), dur * B - 0.03, root - 12, 'bass', c, v, singers=4, gain=0.04, pan=-0.3, spread=0.5, tail=tail)
        # 데이터 스트림 (본론 I 뒤 절반부터)
        if (sec == 1 and b >= 16) or sec == 2:
            data_stream(S, S.at(b, 2 if sec == 1 else 0), 2 * B if sec == 1 else bar, gain=0.011 if sec == 1 else 0.014)
        if sec == 3:
            data_stream(S, S.at(b), bar, gain=0.016, rate=B / 16)
        # 악절 끝 팀파니 몰아치기 · 전환
        if (b - 8) % 8 == 7 and b != 55:
            for i in range(8):
                S.add(S.at(b, 2 + i * 0.25), taiko(), pan=(i % 2 - 0.5) * 0.5, gain=0.45 + 0.07 * i, rev=0.3, bus='perc')
        if b == 38:
            S.add(S.at(b), crash(3.2, 2200, reverse=True), gain=0.5, rev=0.4, bus='crash')
            for i in range(16):
                S.add(S.at(39, i * 0.25), metal_hit(980 + 40 * i, 0.5), pan=(i % 2 - 0.5) * 0.8, gain=0.14 + 0.012 * i, rev=0.45, bus='metal')
        if b == 55:
            S.add(S.at(55, 2), crash(2.0, 3000, reverse=True), gain=0.5, rev=0.4, bus='crash')
            for i in range(8):
                S.add(S.at(55, 2 + i * 0.25), taiko(), pan=(i % 2 - 0.5) * 0.6, gain=0.6 + 0.08 * i, rev=0.3, bus='perc')

    # ── 56~59: 마지막 일격 → 가라앉음 → (반복) 첫 타격
    root, tones = prog[56]
    S.section(1.3, 0.75, 6.5)
    S.add(S.at(56), bass_drum(), gain=1.0, rev=0.2, bus='perc_big')
    S.add(S.at(56), tamtam(5.0), gain=0.45, rev=0.5, bus='perc_big')
    S.add(S.at(56), crash(3.0, 2400), gain=0.5, rev=0.5, bus='crash')
    S.add(S.at(56), bell(D - 12, 6.0), pan=0.2, gain=0.45, rev=0.65, bus='bell')
    S.add(S.at(56), metal_hit(560, 3.0), pan=-0.4, gain=0.4, rev=0.55, bus='metal')
    for i, nt in enumerate([D - 24, D - 12, D, D + 3, D + 7, D + 1]):
        brass_note(S, S.at(56), 2 * bar, nt, gain=0.07, pan=(i - 2.5) * 0.22, spread=0.15, voices=3, bright=4000, swell=-0.5, rip=1.2)
    for i, (nt, vt) in enumerate([(D - 12, 'bass'), (D - 5, 'bass'), (D - 11, 'bass'), (D + 3, 'tenor'), (D + 7, 'tenor')]):
        sing(S, S.at(56), S.at(59, 2.5) - S.at(56), nt, vt, '-', 'a', 'u', singers=4, gain=0.05 if vt == 'bass' else 0.03, pan=(i - 2) * 0.3, spread=0.4, attack=0.08)
    organ_note(S, S.at(56), S.at(58) - S.at(56), D - 12, gain=0.04, sub16=True, full=True)
    for k in range(1, 8):
        S.add(S.at(57, k * 2 - 1.5), heartbeat(), gain=0.42, rev=0.08, bus='heart')
    S.add(S.at(58), bell(D - 6, 5.0, 1.2), pan=-0.3, gain=0.26, rev=0.7, bus='bell')
    for _ in range(10):
        S.add(rng.uniform(S.at(57), S.at(59, 3.5)), glitch_tick(), pan=rng.uniform(-0.9, 0.9), gain=0.045, rev=0.25, bus='glitch')
    for _ in range(4):
        whisper(S, rng.uniform(S.at(57), S.at(59, 2)), rng.uniform(0.2, 0.4), 'alto', gain=0.014, pan=rng.uniform(-0.8, 0.8))
    S.add(S.at(59, 3.5), crash(0.9, 3500, reverse=True), gain=0.3, rev=0.3, bus='crash')

    # ── 글리치 후처리: 본론 II 악절 끝 되감기, 절정 중간(48마디) 「시스템 붕괴」
    for b in (27, 31, 35):
        stutter(S, S.at(b, 3), B / 4, 3, bits=6, hold=3)
    for b in (43, 51):
        stutter(S, S.at(b, 3.5), B / 8, 3, bits=5, hold=4)
    system_crash(S, S.at(48), bar, [1, 0, 1, 1, 0, 1, 0, 1, 1, 1, 0, 1, 1, 1, 1, 1], bits=5, hold=6)
    return S


# ───────────────────────────────────────────── 2. 엄마전 (사 단조 · 168 BPM · 60마디 = 85.7초)
def compose_mom():
    S = Song('bgm_developer_mom', 168, 60)
    B, bar = S.beat, S.bar
    G = 55  # 사(G3) — 개발자 주제보다 완전4도 위
    Gm, Eb, D7, Ab, Cs, Cm = chord(G, 'm'), chord(G - 4, 'M'), chord(G - 5, '7'), chord(G + 1, 'M'), chord(G - 6, 'dim7'), chord(G - 7, 'm')
    PH_A, PH_B, PH_C, PH_D = [Gm, Gm, Eb, D7], [Gm, Ab, Cs, D7], [Gm, Eb, Ab, Cs], [Gm, Cm, Ab, D7]
    prog = {}
    for p, phrase in enumerate([PH_A, PH_B, PH_A, PH_B, PH_C, PH_D]):
        for c, ch in enumerate(phrase):
            prog[8 + p * 8 + c * 2] = ch
    prog[56] = chord(G, 'm9')

    def chord_at(bar_i):
        return prog[bar_i - (bar_i % 2)] if bar_i >= 8 else Gm
    theme = [(n + 5, d) for n, d in THEME]

    # ── 0. 첫 타격: 더 크게 — 대북 둘 · 유리 깨지는 금속 · 여성 합창 외침 「하!」
    S.section(1.0, 0.8, 6.5)
    S.add(0, tamtam(6.0), gain=0.55, rev=0.5, bus='perc_big')
    S.add(0, bass_drum(), gain=1.0, rev=0.2, bus='perc_big')
    S.add(0.012, bass_drum(), gain=0.6, rev=0.2, bus='perc_big')
    S.add(0.004, taiko(), pan=-0.3, gain=0.75, rev=0.3, bus='perc')
    S.add(0.004, taiko(), pan=0.3, gain=0.75, rev=0.3, bus='perc')
    S.add(0, sub_drop(1.8), gain=0.6, rev=0.0, bus='sub')
    for i, base in enumerate([520, 880, 1270]):
        S.add(0.002 * i, metal_hit(base, 2.5), pan=(i - 1) * 0.5, gain=0.3, rev=0.55, bus='metal')
    S.add(0, bell(G - 12, 6.0, 1.3), pan=-0.3, gain=0.5, rev=0.6, bus='bell')
    S.add(0, crash(3.2, 2000), gain=0.5, rev=0.5, bus='crash')
    brass_stab(S, 0.0, [G - 24, G - 12, G, G + 3, G + 1], gain=0.09, dur=0.8)
    for nt, vt in ((G - 12, 'bass'), (G, 'tenor'), (G + 3, 'alto'), (G + 12, 'soprano'), (G + 13, 'soprano')):
        sing(S, 0.0, 0.5, nt, vt, 'h', 'a', singers=4, gain=0.04 if vt in ('bass', 'tenor') else 0.03, attack=0.01, spread=0.8)
    for _ in range(8):
        S.add(rng.uniform(0.05, 0.8), glitch_tick(), pan=rng.uniform(-0.9, 0.9), gain=0.055, rev=0.2, bus='glitch')

    # ── 전주 (0~7): 오르골 자장가(주제) · 빠른 심장 박동 · 속삭임 · 여성 합창 「우—」 · 드론
    S.section(0.8, 0.75, 6.5)
    n = S.n
    tt = tline(n)
    f1 = S.loopf(midi(G - 24))
    pulse = 0.65 + 0.35 * np.sin(TAU * S.loopf(S.bpm / 60.0 / 2) * tt - math.pi / 2)
    drone = (np.sin(TAU * f1 * tt) + 0.35 * np.sin(TAU * 2 * f1 * tt + 1.0) + 0.12 * np.sin(TAU * 3 * f1 * tt)) * pulse
    lvl = np.interp(tt, [0, 1.5, S.at(7.9), S.at(8), S.at(55.5), S.at(56.5), S.at(59.5), S.seconds], [0.0, 0.4, 0.55, 1.0, 1.0, 0.5, 0.35, 0.0])
    S.add(0, drone * lvl, gain=0.12, rev=0.05, bus='sub')
    t = S.at(1)
    for nt, beats in theme:
        music_box(S, t, min(beats * B, 1.6) + 1.2, nt + 12, gain=0.03, pan=rng.uniform(-0.3, 0.3))
        if beats >= 2:
            music_box(S, t + B, 1.4, nt + 24 - 12 + rng.choice([0, 3, 7]), gain=0.014, pan=rng.uniform(-0.6, 0.6))
        t += beats * B
    # 7마디 끝의 삼전음 (도♯) 위에서 오르골이 「망가짐」
    for i in range(6):
        music_box(S, S.at(7, 1.5 + i * 0.25), 0.5, 73 + 12 - i * 2 + rng.integers(-1, 2), gain=0.02, pan=(i % 2 - 0.5))
    for k in range(2, 32):
        S.add(S.at(0, k) + 0.01, heartbeat(), gain=0.38 + 0.01 * k, rev=0.08, bus='heart')
    sing(S, S.at(2), S.at(7, 2) - S.at(2), G, 'alto', '-', 'u', 'o', singers=5, gain=0.035, pan=-0.3, spread=0.5, attack=1.3)           # 사3
    sing(S, S.at(2, 2), S.at(7, 2) - S.at(2, 2), G - 5, 'alto', '-', 'u', 'o', singers=4, gain=0.03, pan=0.3, spread=0.5, attack=1.5)   # 라3
    sing(S, S.at(4), S.at(7, 2) - S.at(4), G + 1, 'alto', '-', 'o', 'a', singers=3, gain=0.022, pan=0.0, spread=0.6, attack=1.2)       # 반음 위(사♯3)
    sing(S, S.at(5, 2), S.at(7, 2) - S.at(5, 2), G + 18, 'soprano', '-', 'o', 'a', singers=3, gain=0.018, pan=0.1, spread=0.6, attack=1.0)  # 삼전음(다♯5)
    organ_note(S, S.at(1), S.at(7, 2) - S.at(1), G - 12, gain=0.03, sub16=True, rev=0.5)
    S.add(S.at(4), bell(G - 12, 5.0), pan=0.3, gain=0.3, rev=0.65, bus='bell')
    S.add(S.at(6), bell(G - 6, 5.0, 1.3), pan=-0.4, gain=0.34, rev=0.65, bus='bell')
    for _ in range(18):
        S.add(rng.uniform(S.at(1), S.at(7)), glitch_tick(), pan=rng.uniform(-0.9, 0.9), gain=0.05, rev=0.25, bus='glitch')
    for _ in range(12):
        whisper(S, rng.uniform(S.at(1), S.at(7)), rng.uniform(0.15, 0.35), rng.choice(['alto', 'soprano']), gain=0.018, pan=rng.uniform(-0.9, 0.9))
    for i in range(8):
        S.add(S.at(7, i * 0.25), taiko(), pan=(i % 2 - 0.5) * 0.4, gain=0.35 + 0.08 * i, rev=0.3, bus='perc')
    for i in range(6):
        S.add(S.at(7, 2.5 + i * 0.25), taiko(), pan=(i % 2 - 0.5) * 0.5, gain=0.75 + 0.08 * i, rev=0.3, bus='perc')
        S.add(S.at(7, 2.5 + i * 0.25), metal_hit(900 + 90 * i, 0.4), pan=(0.5 - i % 2) * 0.7, gain=0.16, rev=0.4, bus='metal')
    S.add(S.at(6, 2), crash(2.2, 2500, reverse=True), gain=0.5, rev=0.4, bus='crash')
    S.add(S.at(7, 3.75), sub_drop(0.6), gain=0.35, rev=0.0, bus='sub')

    # ── 본론 I (8~23) · 본론 II (24~39) · 절정 (40~55): 개발자 곡의 구조 위에 더 촘촘한 타악 · 여성 합창 · 글리치
    for b in range(8, 56):
        root, tones = chord_at(b)
        sec = 1 if b < 24 else 2 if b < 40 else 3
        phrase_start = (b - 8) % 8 == 0
        chord_start = b % 2 == 0
        S.section(*{1: (1.15, 0.55, 6.5), 2: (1.45, 0.58, 7.0), 3: (1.85, 0.66, 8.0)}[sec])
        if sec == 1:
            for _ in range(rng.integers(1, 4)):
                S.add(S.at(b, rng.uniform(0, 4)), glitch_tick(), pan=rng.uniform(-0.9, 0.9), gain=0.045, rev=0.25, bus='glitch')
        patt = OSTI_I if sec == 1 else OSTI_II if sec == 2 else OSTI_C
        osti = [root - 12 + s for s in patt]
        if b == 55:
            osti = [root - 12 + s for s in [0, 0, 12, 0, 0, 0, 11, 0, 0, 12, 12, 12, 6, 6, 1, 1]]
        strings_bar(S, S.at(b), osti, gain=0.052 if sec == 1 else 0.062 if sec == 2 else 0.07, accents=ACC_C if sec == 3 else None, bright=3200 + 600 * sec)
        if sec >= 2:
            strings_bar(S, S.at(b), osti, gain=0.026 if sec == 2 else 0.034, accents=ACC_C, bright=5500, voices=2, octave_up=True)
        if sec == 3:
            # 바이올린 32분음표 떨림 (한 옥타브 더 위) — 격렬함
            hi = [root + 12 + s for s in [0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 12, 0, 6, 6, 7, 6]]
            strings_bar(S, S.at(b), hi, gain=0.018, bright=7000, voices=2)
        if chord_start:
            organ_note(S, S.at(b), 2 * bar - 0.05, root - 12, gain=0.045 if sec < 3 else 0.056, sub16=True, full=sec == 3)
            if sec >= 2:
                for i, nt in enumerate(tones):
                    organ_note(S, S.at(b), 2 * bar - 0.05, nt, gain=0.022 if sec == 2 else 0.03, pan=(i - 1) * 0.35, full=sec == 3)
                if sec == 3:
                    organ_note(S, S.at(b), 2 * bar - 0.05, tones[0] + 12, gain=0.018, full=True)
        if sec == 1:
            for beat in (0, 1, 2, 3):
                S.add(S.at(b, beat) + 0.01, heartbeat(), gain=0.26, rev=0.05, bus='heart')
        # 타악: 전부 한 단계 촘촘히 · 팀파니 둘(좌우)
        if sec == 1:
            hits = [(0, 1.0), (1.5, 0.6), (2, 0.9), (3.5, 0.6), (3.75, 0.7)]
        elif sec == 2:
            hits = [(0, 1.0), (1, 0.7), (1.5, 0.75), (2, 0.95), (3, 0.8), (3.5, 0.75), (3.75, 0.6)]
        else:
            hits = [(i * 0.5, 1.0 if i % 2 == 0 else 0.75) for i in range(8)] + [(1.75, 0.6), (3.75, 0.7)]
        for beat, g in hits:
            S.add(S.at(b, beat), taiko(rng.uniform(-0.05, 0.05)), pan=-0.35, gain=0.5 * g * (1.0 + 0.15 * (sec - 1)), rev=0.28, bus='perc')
            S.add(S.at(b, beat) + 0.006, taiko(rng.uniform(-0.05, 0.05)), pan=0.35, gain=0.42 * g * (1.0 + 0.15 * (sec - 1)), rev=0.28, bus='perc')
        S.add(S.at(b), bass_drum(), gain=0.8 if sec < 3 else 0.95, rev=0.15, bus='perc_big')
        if sec >= 2:
            S.add(S.at(b, 2), bass_drum(), gain=0.55 if sec == 2 else 0.7, rev=0.15, bus='perc_big')
        metal_steps = [3] if sec == 1 else [1, 3, 3.5] if sec == 2 else [0.5, 1, 1.5, 2.5, 3, 3.5]
        for st in metal_steps:
            S.add(S.at(b, st), metal_hit(rng.choice([700, 880, 1040, 1270]), 1.0), pan=rng.uniform(-0.8, 0.8), gain=0.19 if sec < 3 else 0.23, rev=0.5, bus='metal')
        if sec == 3:
            for i in range(16):      # 쇠 16분음표 (하이햇 같은 금속 틱)
                S.add(S.at(b, i * 0.25), fft_filter(noise(int(0.03 * RATE)), 5000, 14000, 2) * env(int(0.03 * RATE), 0.001, 0.02), pan=0.5 - (i % 2), gain=0.09 if i % 2 else 0.05, rev=0.2, bus='metal')
        if (chord_start and sec >= 2) or sec == 3:
            S.add(S.at(b), bell(root - 12 if sec < 3 else root, 4.0, 1.3), pan=rng.uniform(-0.5, 0.5), gain=0.24 if sec < 3 else 0.28, rev=0.6, bus='bell')
        if phrase_start or (sec == 3 and b % 4 == 0):
            S.add(S.at(b), crash(2.6, 2600), gain=0.42, rev=0.45, bus='crash')
        # 금관
        if sec == 1 and b >= 14 and chord_start:
            brass_stab(S, S.at(b), [root - 12, tones[1], tones[2]], gain=0.07, dur=0.4)
            brass_stab(S, S.at(b, 3.5), [root - 12, tones[0]], gain=0.05, dur=0.18)
        if sec >= 2 and phrase_start:
            t = S.at(b)
            for nt, beats in theme:
                d = beats * B - 0.05
                brass_note(S, t, d, nt, gain=0.08 if sec == 2 else 0.09, spread=0.35, voices=3, bright=4600 if sec == 2 else 5600)
                brass_note(S, t, d, nt + 12, gain=0.04 if sec == 2 else 0.055, spread=0.5, voices=3, bright=7500, rip=1.0)
                if sec == 3:
                    brass_note(S, t, d, nt - 12, gain=0.05, spread=0.3, voices=2, bright=2800, rip=1.0)
                t += beats * B
        if sec == 2 and chord_start and not phrase_start:
            brass_stab(S, S.at(b, 3.5), [root - 12, tones[0], tones[1]], gain=0.055, dur=0.2)
        # 합창: 알토 · 소프라노가 위에, 남성은 아래 받침
        if chord_start:
            parts = [(root - 12, 'bass', 0.045, -0.4), (root, 'tenor', 0.03, -0.3), (tones[1], 'alto', 0.034, -0.15), (tones[2], 'alto', 0.03, 0.2), (tones[0] + 12, 'soprano', 0.026 if sec == 1 else 0.032, 0.35)]
            if sec >= 2:
                parts.append((tones[1] + 12, 'soprano', 0.024, 0.0))
            if sec <= 2:
                for beat, dur, c, v, tail in CHANT:
                    for nt, vt, g, pn in parts:
                        sing(S, S.at(b, beat), dur * B - 0.03, nt, vt, c, v, singers=4 if vt in ('alto', 'soprano') else 3, gain=g, pan=pn, spread=0.45, tail=tail)
            if sec == 2:
                sing(S, S.at(b), 2 * bar - 0.1, tones[0] + 24, 'soprano', '-', 'o', 'a', singers=4, gain=0.024, pan=0.1, spread=0.6, attack=0.5)
            if sec == 3:
                for nt, vt, g, pn in parts:
                    sing(S, S.at(b), 2 * bar - 0.1, nt, vt, '-', 'a', 'e', singers=4, gain=g * 1.1, pan=pn, spread=0.55, attack=0.2)
                sing(S, S.at(b), 2 * bar - 0.1, tones[0] + 24, 'soprano', '-', 'a', singers=5, gain=0.032, pan=0.0, spread=0.7, attack=0.25)
                for i, nt in enumerate(tones):
                    tremolo_strings(S, S.at(b), 2 * bar, nt + 24, gain=0.02, pan=(i - 1) * 0.5)
        if sec == 3:
            fast = CHANT_FAST[(b // 2) % 2]
            for beat, dur, c, v, tail in fast:
                sing(S, S.at(b, beat), dur * B - 0.03, tones[1] + 12, 'soprano', c, v, singers=4, gain=0.03, pan=0.25, spread=0.5, tail=tail)
                sing(S, S.at(b, beat), dur * B - 0.03, tones[1], 'alto', c, v, singers=4, gain=0.034, pan=-0.1, spread=0.5, tail=tail)
                sing(S, S.at(b, beat), dur * B - 0.03, root - 12, 'bass', c, v, singers=3, gain=0.04, pan=-0.35, spread=0.5, tail=tail)
        # 데이터 스트림 · 오르골 조각(절정에 주제 윗소리로)
        if sec == 1 and b >= 12:
            data_stream(S, S.at(b, 2), 2 * B, gain=0.013)
        elif sec == 2:
            data_stream(S, S.at(b), bar, gain=0.016)
        elif sec == 3:
            data_stream(S, S.at(b), bar, gain=0.018, rate=B / 16)
        if sec == 3 and phrase_start:
            t = S.at(b)
            for nt, beats in theme:
                music_box(S, t, 1.2, nt + 24, gain=0.022, pan=rng.uniform(-0.5, 0.5))
                t += beats * B
        if (b - 8) % 8 == 7 and b != 55:
            for i in range(8):
                S.add(S.at(b, 2 + i * 0.25), taiko(), pan=-0.3, gain=0.5 + 0.07 * i, rev=0.3, bus='perc')
                S.add(S.at(b, 2 + i * 0.25) + 0.006, taiko(), pan=0.3, gain=0.45 + 0.07 * i, rev=0.3, bus='perc')
        if b == 38:
            S.add(S.at(b), crash(3.0, 2200, reverse=True), gain=0.5, rev=0.4, bus='crash')
            for i in range(16):
                S.add(S.at(39, i * 0.25), metal_hit(1100 + 50 * i, 0.5), pan=(i % 2 - 0.5) * 0.8, gain=0.15 + 0.012 * i, rev=0.45, bus='metal')
        if b == 55:
            S.add(S.at(55, 2), crash(1.8, 3000, reverse=True), gain=0.5, rev=0.4, bus='crash')
            for i in range(8):
                S.add(S.at(55, 2 + i * 0.25), taiko(), pan=(i % 2 - 0.5) * 0.6, gain=0.65 + 0.08 * i, rev=0.3, bus='perc')

    # ── 56~59: 마지막 일격 → 오르골 조각 · 심장 박동 → (반복) 첫 타격
    S.section(1.3, 0.75, 6.5)
    S.add(S.at(56), bass_drum(), gain=1.0, rev=0.2, bus='perc_big')
    S.add(S.at(56), tamtam(5.0), gain=0.5, rev=0.5, bus='perc_big')
    S.add(S.at(56), crash(3.0, 2400), gain=0.5, rev=0.5, bus='crash')
    S.add(S.at(56), bell(G - 12, 6.0, 1.3), pan=0.2, gain=0.45, rev=0.65, bus='bell')
    for i, base in enumerate([560, 940]):
        S.add(S.at(56), metal_hit(base, 3.0), pan=(i - 0.5) * 0.8, gain=0.35, rev=0.55, bus='metal')
    for i, nt in enumerate([G - 24, G - 12, G, G + 3, G + 7, G + 1]):
        brass_note(S, S.at(56), 2 * bar, nt, gain=0.07, pan=(i - 2.5) * 0.2, spread=0.15, voices=3, bright=4200, swell=-0.5, rip=1.2)
    for i, (nt, vt) in enumerate([(G - 12, 'bass'), (G - 5, 'tenor'), (G + 1, 'alto'), (G + 3, 'alto'), (G + 15, 'soprano'), (G + 19, 'soprano')]):
        sing(S, S.at(56), S.at(58, 3) - S.at(56), nt, vt, '-', 'a', 'u', singers=4, gain=0.045 if vt in ('bass', 'tenor') else 0.03, pan=(i - 2.5) * 0.3, spread=0.4, attack=0.08)
    organ_note(S, S.at(56), S.at(58) - S.at(56), G - 12, gain=0.04, sub16=True, full=True)
    for k in range(0, 10):
        S.add(S.at(57, 1 + k), heartbeat(), gain=0.4, rev=0.08, bus='heart')
    t = S.at(58)
    for nt, beats in theme[:7]:
        music_box(S, t, 1.0, nt + 12, gain=0.026, pan=rng.uniform(-0.4, 0.4))
        t += beats * B * 0.5
    S.add(S.at(58, 2), bell(G - 6, 4.0, 1.2), pan=-0.3, gain=0.26, rev=0.7, bus='bell')
    for _ in range(12):
        S.add(rng.uniform(S.at(57), S.at(59, 3.5)), glitch_tick(), pan=rng.uniform(-0.9, 0.9), gain=0.05, rev=0.25, bus='glitch')
    for _ in range(6):
        whisper(S, rng.uniform(S.at(57), S.at(59, 2.5)), rng.uniform(0.15, 0.35), rng.choice(['alto', 'soprano']), gain=0.016, pan=rng.uniform(-0.9, 0.9))
    S.add(S.at(59, 3.5), crash(0.8, 3500, reverse=True), gain=0.3, rev=0.3, bus='crash')

    # ── 글리치 후처리: 더 자주 · 두 번의 「시스템 붕괴」
    for b in (25, 27, 29, 31, 33, 35, 37):
        stutter(S, S.at(b, 3), B / 4, 3, bits=6, hold=3)
    for b in range(41, 56, 2):
        if b not in (47, 51):
            stutter(S, S.at(b, 3.5), B / 8, 3, bits=5, hold=4)
    system_crash(S, S.at(31), bar, [1, 0, 1, 1, 0, 1, 1, 0, 1, 1, 0, 1, 1, 1, 1, 1], bits=5, hold=5)
    system_crash(S, S.at(48), bar, [1, 0, 1, 1, 0, 1, 0, 1, 1, 1, 0, 1, 1, 1, 1, 1], bits=4, hold=6)
    return S


def build(which):
    t0 = time.time()
    S = compose_dev() if which == 'dev' else compose_mom()
    print('%s: 합성 %.1f초 (%d마디 · %d BPM · %.1f초)' % (S.name, time.time() - t0, S.bars, S.bpm, S.seconds))
    out = finish(S, wet=0.5, target_db=-15.0)
    path = os.path.join(MUSIC, S.name + '.wav')
    save(S, out, path)
    analyze(path)


if __name__ == '__main__':
    which = sys.argv[1] if len(sys.argv) > 1 else 'all'
    if which in ('dev', 'all'):
        build('dev')
    if which in ('mom', 'all'):
        rng = np.random.default_rng(20261016)
        build('mom')
