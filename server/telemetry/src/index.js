// Soul Saver 플레이 데이터 수집 Worker (빌드 단계 없는 ES module)
//
//   POST /v1/run     판 기록 한 줄 저장 (게임 → 서버)
//   POST /v1/delete  anon_id 의 모든 기록 삭제 (게임 설정 「내 데이터 삭제 요청」)
//   GET  /v1/export  기록 내려받기 (개발자 CLI · Authorization: Bearer ADMIN_TOKEN)
//   크론            받은 지 2년 지난 기록 삭제
//
// 개인정보: IP · User-Agent · CF-* 헤더는 읽지도 저장하지도 않고, 로그에도 남기지 않음
// CORS 헤더 없음 (브라우저에서 부르는 용도가 아님)

const MAX_BODY = 16 * 1024;          // 본문 16KB
const MAX_FIELDS = 200;              // 칸 수 상한
const MAX_KEY_LEN = 64;              // 칸 이름 길이 상한
const MAX_STR_LEN = 4096;            // 값 문자열 길이 상한 (피해 출처 목록 등이 길 수 있음)
const DAILY_LIMIT = 100;             // anon_id 당 하루(UTC) 저장 상한
const EXPORT_PAGE = 1000;            // 내려받기 한 쪽 행 수
const RETENTION_DAYS = 730;          // 보유 기간 2년

const UUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const KEY_RE = /^[a-z0-9_]+$/;
const VERSION_RE = /^[0-9A-Za-z.\-]{1,32}$/;

function json(status, obj) {
  return new Response(JSON.stringify(obj), {
    status,
    headers: { "Content-Type": "application/json; charset=utf-8", "Cache-Control": "no-store" },
  });
}

// 본문을 16KB 까지만 읽음 (Content-Length 를 믿지 않고 실제로 센다)
async function readBody(request) {
  const len = Number(request.headers.get("Content-Length") || "0");
  if (len > MAX_BODY) return null;
  if (!request.body) return "";
  const reader = request.body.getReader();
  const chunks = [];
  let total = 0;
  for (;;) {
    const { done, value } = await reader.read();
    if (done) break;
    total += value.byteLength;
    if (total > MAX_BODY) { try { await reader.cancel(); } catch (_) {} return null; }
    chunks.push(value);
  }
  const all = new Uint8Array(total);
  let off = 0;
  for (const c of chunks) { all.set(c, off); off += c.byteLength; }
  return new TextDecoder("utf-8", { fatal: true }).decode(all);
}

// JSON 본문 읽기 · 공통 검사 (Content-Type · 크기 · 형식) → { obj } 또는 { error: Response }
async function readJson(request) {
  const ct = (request.headers.get("Content-Type") || "").toLowerCase();
  if (!ct.startsWith("application/json")) return { error: json(415, { error: "content-type" }) };
  let text;
  try { text = await readBody(request); } catch (_) { return { error: json(400, { error: "body" }) }; }
  if (text === null) return { error: json(413, { error: "too_large" }) };
  let obj;
  try { obj = JSON.parse(text); } catch (_) { return { error: json(400, { error: "json" }) }; }
  if (!obj || typeof obj !== "object" || Array.isArray(obj)) return { error: json(400, { error: "json" }) };
  return { obj };
}

// 길이가 달라도 시간이 같도록 SHA-256 으로 맞춘 뒤 전부 비교
async function safeEqual(a, b) {
  const enc = new TextEncoder();
  const [ha, hb] = await Promise.all([
    crypto.subtle.digest("SHA-256", enc.encode(a)),
    crypto.subtle.digest("SHA-256", enc.encode(b)),
  ]);
  const x = new Uint8Array(ha), y = new Uint8Array(hb);
  let diff = 0;
  for (let i = 0; i < x.length; i++) diff |= x[i] ^ y[i];
  return diff === 0;
}

// 게임 공개 키 (설정됐을 때만 검사)
async function checkIngestKey(request, env) {
  const want = env.INGEST_KEY || "";
  if (!want) return true;
  return safeEqual(request.headers.get("X-Ingest-Key") || "", want);
}

function str(v) {
  return typeof v === "string" ? v : (typeof v === "number" && Number.isFinite(v) ? String(v) : "");
}

// 판 기록 검사 → 오류 문자열 또는 null
function validateRun(o) {
  const keys = Object.keys(o);
  if (keys.length === 0 || keys.length > MAX_FIELDS) return "fields";
  for (const k of keys) {
    if (k.length > MAX_KEY_LEN || !KEY_RE.test(k)) return "key";
    const v = o[k];
    if (v === null || typeof v === "boolean") continue;
    if (typeof v === "number") { if (!Number.isFinite(v)) return "value"; continue; }
    if (typeof v === "string") { if (v.length > MAX_STR_LEN) return "value"; continue; }
    return "value";      // 객체 · 배열은 받지 않음 (판 기록은 평평한 한 줄)
  }
  const schema = Number(o.schema);
  if (!Number.isInteger(schema) || schema < 1 || schema > 1000) return "schema";
  if (!UUID_RE.test(str(o.anon_id))) return "anon_id";
  if (!VERSION_RE.test(str(o.version))) return "version";
  const build = str(o.build);
  if (build !== "full" && build !== "demo") return "build";
  if (str(o.consent_ver) === "") return "consent_ver";
  const date = str(o.date);
  if (date && !/^\d{4}-\d{2}-\d{2}$/.test(date)) return "date";     // 날짜는 일 단위까지만
  return null;
}

async function handleRun(request, env) {
  if (!(await checkIngestKey(request, env))) return json(403, { error: "key" });
  const r = await readJson(request);
  if (r.error) return r.error;
  const o = r.obj;
  const bad = validateRun(o);
  if (bad) return json(422, { error: bad });

  const anon = str(o.anon_id).toLowerCase();
  o.anon_id = anon;
  const now = new Date();
  const dayStart = now.toISOString().slice(0, 10);      // "yyyy-MM-dd" — received_at 과 문자열 비교

  const cnt = await env.DB.prepare(
    "SELECT COUNT(*) AS n FROM runs WHERE anon_id = ?1 AND received_at >= ?2"
  ).bind(anon, dayStart).first();
  if (cnt && cnt.n >= DAILY_LIMIT) return json(429, { error: "daily_limit" });

  await env.DB.prepare(
    "INSERT INTO runs (received_at, anon_id, schema, version, build, data) VALUES (?1, ?2, ?3, ?4, ?5, ?6)"
  ).bind(now.toISOString(), anon, Number(o.schema), str(o.version), str(o.build), JSON.stringify(o)).run();
  return json(200, { ok: true });
}

async function handleDelete(request, env) {
  if (!(await checkIngestKey(request, env))) return json(403, { error: "key" });
  const r = await readJson(request);
  if (r.error) return r.error;
  const anon = str(r.obj.anon_id);
  if (!UUID_RE.test(anon)) return json(422, { error: "anon_id" });
  const res = await env.DB.prepare("DELETE FROM runs WHERE anon_id = ?1").bind(anon.toLowerCase()).run();
  return json(200, { ok: true, deleted: (res.meta && res.meta.changes) || 0 });
}

async function handleExport(request, env, url) {
  const admin = env.ADMIN_TOKEN || "";
  const auth = request.headers.get("Authorization") || "";
  const given = auth.startsWith("Bearer ") ? auth.slice(7) : "";
  // 토큰이 설정 안 됐으면 내려받기 자체를 막음
  if (!admin || !(await safeEqual(given, admin))) return json(401, { error: "auth" });

  let since = parseInt(url.searchParams.get("since") || "0", 10);
  if (!Number.isFinite(since) || since < 0) since = 0;
  const { results } = await env.DB.prepare(
    "SELECT id, received_at, anon_id, schema, version, build, data FROM runs WHERE id > ?1 ORDER BY id LIMIT ?2"
  ).bind(since, EXPORT_PAGE).all();
  const rows = results || [];
  const next = rows.length ? rows[rows.length - 1].id : since;
  return json(200, { rows, next, more: rows.length === EXPORT_PAGE });
}

export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    try {
      if (url.pathname === "/v1/run") {
        if (request.method !== "POST") return json(405, { error: "method" });
        return await handleRun(request, env);
      }
      if (url.pathname === "/v1/delete") {
        if (request.method !== "POST") return json(405, { error: "method" });
        return await handleDelete(request, env);
      }
      if (url.pathname === "/v1/export") {
        if (request.method !== "GET") return json(405, { error: "method" });
        return await handleExport(request, env, url);
      }
      return json(404, { error: "not_found" });
    } catch (_) {
      // 요청 내용 · 주소 · 헤더는 로그에 남기지 않음. 5xx 면 게임은 나중에 다시 보냄
      return json(500, { error: "server" });
    }
  },

  // 매일: 받은 지 2년 지난 기록 삭제
  async scheduled(event, env, ctx) {
    const cutoff = new Date(Date.now() - RETENTION_DAYS * 24 * 3600 * 1000).toISOString();
    ctx.waitUntil(env.DB.prepare("DELETE FROM runs WHERE received_at < ?1").bind(cutoff).run());
  },
};
