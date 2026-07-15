# zc.py — shared Zen-corpus concordance toolkit for dictionary agents.
# Allowlist-scoped, tag-stripped, apparatus(<note>/<app>/<rdg>)-excluded, primary-edition lb aware.
#
# Usage from an agent (avoids CJK argv issues — pass Python strings):
#   import sys; sys.path.insert(0, r"C:\programmieren\MergeWorkCbeta\CBETA-Translator\runs\CLAUDE-RUNS\RUN-20260711-1248-full-cbeta-translation\dictionary-build")
#   import zc
#   zc.count("乾屎橛")                      -> {"hits":N, "files":M, "per_file":[(relpath,c),...]}
#   zc.verify("X/X80/X80n1565.xml", kwic)   -> {"ok":True, "fromLb":"0227a10", "toLb":"0227a10"}  (kwic = exact contiguous, tag/ws-normalized)
#   zc.find("X/X80/X80n1565.xml", "乾屎橛", ctx=16) -> [{"window":..., "fromLb":...}, ...]
#   zc.head("X/X80/X80n1565.xml", "0227a10")-> {"head":"...", "mulu":[...]}  (attribution: nearest preceding <head>/cb:mulu)
#   zc.title("X/X80/X80n1565.xml")          -> "五燈會元"
#
# Matching model: text is tag-stripped, apparatus removed, ALL whitespace removed (KWICs in entries are
# whitespace-free joined spans that may cross <lb/>). Counts + verify + find all operate on that normalized text.
import sys, os, re, json, hashlib, pickle, tempfile
from array import array
from bisect import bisect_right
from functools import lru_cache
from collections import Counter

_WINDOWS_CORPUS = r"C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5"
_WINDOWS_ALLOW = r"C:\programmieren\MergeWorkCbeta\CBETA-Translator\Assets\Data\zen-corpus.json"
_WSL_CORPUS = "/mnt/c/temp/NewTranslationrepos/CbetaZenTexts/xml-p5"
_WSL_ALLOW = "/mnt/c/programmieren/mergeworkcbeta/cbeta-translator/Assets/Data/zen-corpus.json"

# Keep the toolkit identical on native Windows and WSL so a guard cannot appear
# to pass only because a cached count hid an unusable hard-coded path.
CORPUS = os.environ.get("CBETA_ZEN_CORPUS") or (_WSL_CORPUS if os.path.isdir(_WSL_CORPUS) else _WINDOWS_CORPUS)
ALLOW = os.environ.get("CBETA_ZEN_ALLOWLIST") or (_WSL_ALLOW if os.path.isfile(_WSL_ALLOW) else _WINDOWS_ALLOW)

_TAG = re.compile(r"<[^>]+>")
_WS  = re.compile(r"\s+")
_cache = {}
_NORMALIZER_VERSION = "zc-apparatus-clean-v3"
_DISK_CACHE = os.environ.get("ZC_CACHE_DIR") or "/tmp/cbeta-zc-cache-v3"
_DISK_CACHE_ENABLED = os.environ.get("ZC_DISABLE_DISK_CACHE", "").lower() not in {"1", "true", "yes"}
_TRUST_FROZEN_CACHE = os.environ.get("ZC_TRUST_FROZEN_CACHE", "").lower() in {"1", "true", "yes"}


class _LbMap:
    """Compact primary-edition line map with list-compatible lookup operations."""
    __slots__ = ("starts", "values", "length")

    def __init__(self, starts, values, length):
        self.starts = starts
        self.values = values
        self.length = length

    def __len__(self):
        return self.length

    def __getitem__(self, index):
        if index < 0:
            index += self.length
        if index < 0 or index >= self.length:
            raise IndexError(index)
        run = bisect_right(self.starts, index) - 1
        return self.values[run] if run >= 0 else None

    def index(self, value):
        for start, current in zip(self.starts, self.values):
            if current == value:
                return start
        raise ValueError(value)


def _disk_cache_path(rel):
    key = hashlib.sha256(rel.replace("\\", "/").encode("utf-8")).hexdigest()
    return os.path.join(_DISK_CACHE, key + ".pickle")


def _read_disk_cache(rel, source):
    if not _DISK_CACHE_ENABLED:
        return None
    try:
        with open(_disk_cache_path(rel), "rb") as fh:
            payload = pickle.load(fh)
        if payload.get("version") != _NORMALIZER_VERSION:
            return None
        if not _TRUST_FROZEN_CACHE:
            stat = os.stat(source)
            if payload.get("size") != stat.st_size or payload.get("mtime_ns") != stat.st_mtime_ns:
                return None
        norm = payload["norm"]
        return norm, _LbMap(payload["lb_starts"], payload["lb_values"], len(norm))
    except (OSError, EOFError, pickle.PickleError, AttributeError, KeyError, TypeError, ValueError):
        return None


def _write_disk_cache(rel, source, norm, starts, values):
    if not _DISK_CACHE_ENABLED:
        return
    try:
        os.makedirs(_DISK_CACHE, exist_ok=True)
        stat = os.stat(source)
        payload = {
            "version": _NORMALIZER_VERSION,
            "size": stat.st_size,
            "mtime_ns": stat.st_mtime_ns,
            "norm": norm,
            "lb_starts": starts,
            "lb_values": values,
        }
        target = _disk_cache_path(rel)
        fd, temporary = tempfile.mkstemp(prefix="zc-", suffix=".tmp", dir=_DISK_CACHE)
        try:
            with os.fdopen(fd, "wb") as fh:
                pickle.dump(payload, fh, protocol=pickle.HIGHEST_PROTOCOL)
            os.replace(temporary, target)
        finally:
            if os.path.exists(temporary):
                os.unlink(temporary)
    except OSError:
        # The cache is an optimization only; source-backed behavior must survive
        # an unwritable or full temporary filesystem.
        return

def _allow():
    if "allow" not in _cache:
        z = json.load(open(ALLOW, encoding="utf-8"))
        _cache["allow"] = [t for t in z["texts"]]
        _cache["allowset"] = set(_cache["allow"])
    return _cache["allow"]


def work_id(rel):
    """Return the manifest's independent-work identity for a corpus file."""
    if "work_ids" not in _cache:
        z = json.load(open(ALLOW, encoding="utf-8"))
        _cache["work_ids"] = z.get("work_ids") or {}
    try:
        return _cache["work_ids"][rel]
    except KeyError as exc:
        raise KeyError(f"allowlisted file lacks work_id: {rel}") from exc

def is_allowed(rel):
    _allow(); return rel in _cache["allowset"]

def _abs(rel):
    return os.path.join(CORPUS, rel.replace("/", os.sep))

def _load(rel):
    """Return (norm_text, idx2lb) where norm_text is tag-stripped, apparatus-removed, whitespace-removed;
    idx2lb[j] = primary-edition lb n-value governing the j-th char of norm_text."""
    if rel in _cache.get("files", {}):
        return _cache["files"][rel]
    source = _abs(rel)
    cached = _read_disk_cache(rel, source)
    if cached is not None:
        _cache.setdefault("files", {})[rel] = cached
        return cached
    raw = open(source, encoding="utf-8").read()
    m = re.search(r"<body\b[^>]*>(.*)</body>", raw, re.S)
    body = m.group(1) if m else raw
    # Preserve raw offsets while excluding apparatus; deletion would shift every
    # following position and could make a head/case lookup inspect the wrong unit.
    body = re.sub(r"<note\b[^>]*>.*?</note>", lambda m: " " * len(m.group(0)), body, flags=re.S)
    body = re.sub(r"<app\b[^>]*>.*?</app>", lambda m: " " * len(m.group(0)), body, flags=re.S)
    # primary edition: prefer X, else most common ed on lb tags
    eds = re.findall(r'<lb\b[^>]*\bed="([^"]+)"', body)
    primary = "X" if "X" in eds else (Counter(eds).most_common(1)[0][0] if eds else None)
    norm = []           # normalized chars
    idx2lb = []         # lb governing each normalized char
    cur_lb = None
    i = 0
    for mt in _TAG.finditer(body):
        seg = body[i:mt.start()]
        for ch in seg:
            if not ch.isspace():
                norm.append(ch); idx2lb.append(cur_lb)
        tag = mt.group(0)
        if tag.startswith("<lb"):
            em = re.search(r'ed="([^"]+)"', tag); nm = re.search(r'n="([^"]+)"', tag)
            ed = em.group(1) if em else None; n = nm.group(1) if nm else None
            if n and (ed == primary or primary is None):
                cur_lb = n
        i = mt.end()
    for ch in body[i:]:
        if not ch.isspace():
            norm.append(ch); idx2lb.append(cur_lb)
    value = "".join(norm)
    starts = []
    lb_values = []
    previous = object()
    for index, lb in enumerate(idx2lb):
        if lb != previous:
            starts.append(index)
            lb_values.append(lb)
            previous = lb
    compact = _LbMap(starts, lb_values, len(value))
    res = (value, compact)
    _write_disk_cache(rel, source, value, starts, lb_values)
    _cache.setdefault("files", {})[rel] = res
    return res

def count(term, limit=0):
    """Allowlist-scoped count with both storage-file and independent-work spread."""
    term = _WS.sub("", term)
    per = []
    total = 0; nfiles = 0
    for rel in _allow():
        try:
            norm, _ = _load(rel)
        except Exception:
            continue
        c = norm.count(term)
        if c:
            per.append((rel, c)); total += c; nfiles += 1
    per.sort(key=lambda x: -x[1])
    if limit: per = per[:limit]
    works = {work_id(rel) for rel, _ in per}
    return {"hits": total, "files": nfiles, "works": len(works), "per_file": per}

def verify(rel, kwic):
    """Is kwic an exact contiguous (tag/ws-normalized) substring of rel's body? Report fromLb/toLb."""
    if not is_allowed(rel):
        return {"ok": False, "error": "NOT_IN_ALLOWLIST"}
    norm, idx2lb = _load(rel)
    q = _WS.sub("", kwic)
    j = norm.find(q)
    if j < 0:
        return {"ok": False, "fromLb": None, "toLb": None}
    return {"ok": True, "fromLb": idx2lb[j], "toLb": idx2lb[min(j+len(q)-1, len(idx2lb)-1)], "count": norm.count(q)}

def find(rel, term, ctx=16, limit=12):
    """List occurrences of term in rel with a context window + governing lb."""
    norm, idx2lb = _load(rel)
    q = _WS.sub("", term)
    out = []; start = 0
    while len(out) < limit:
        j = norm.find(q, start)
        if j < 0: break
        a = max(0, j-ctx); b = min(len(norm), j+len(q)+ctx)
        out.append({"window": norm[a:b], "fromLb": idx2lb[j]})
        start = j + len(q)
    return out

def head(rel, lb):
    """Attribution helper: nearest preceding <head> text + cb:mulu chain for a given lb n-value."""
    raw = open(_abs(rel), encoding="utf-8").read()
    pos = None
    m = re.search(r'<lb\b[^>]*\bn="%s"' % re.escape(lb), raw)
    if m: pos = m.start()
    if pos is None: return {"head": None, "mulu": [], "error": "LB_NOT_FOUND"}
    pre = raw[:pos]
    heads = re.findall(r"<head\b[^>]*>(.*?)</head>", pre, re.S)
    last_head = _WS.sub("", _TAG.sub("", heads[-1])) if heads else None
    mulus = re.findall(r'<cb:mulu\b[^>]*?(?:n|type|level)="[^"]*"[^>]*>', pre)[-6:]
    return {"head": last_head, "mulu": mulus}

def context(rel, lb, chars=500, kwic=None):
    """Return normalized context around the first character governed by lb.

    This implements rung 2 of the attribution ladder. Call successively with
    chars=500, 2000, and 10000; do not infer the speaker from a narrow KWIC.
    """
    norm, idx2lb = _load(rel)
    if kwic:
        pos = norm.find(_WS.sub("", kwic))
        if pos < 0:
            return {"window": None, "error": "KWIC_NOT_FOUND"}
    else:
        try:
            pos = idx2lb.index(lb)
        except ValueError:
            return {"window": None, "error": "LB_NOT_FOUND"}
    a = max(0, pos - chars)
    b = min(len(norm), pos + chars)
    return {"window": norm[a:b], "fromLb": idx2lb[a], "toLb": idx2lb[b-1]}

def _raw_position_index(raw):
    """Build normalized body text plus its raw-character position map."""
    body_match = re.search(r"<body\b[^>]*>(.*)</body>", raw, re.S)
    body = body_match.group(1) if body_match else raw
    body_offset = body_match.start(1) if body_match else 0
    # Preserve raw offsets while excluding apparatus.  Deleting a note/app
    # shifts every later character and makes the returned index point into the
    # wrong case even though normalized KWIC verification still succeeds.
    # Equal-length blanking keeps positions aligned with the original XML.
    def blank_apparatus(match):
        return re.sub(r"[^\r\n]", " ", match.group(0))

    body = re.sub(r"<note\b[^>]*>.*?</note>", blank_apparatus, body, flags=re.S)
    body = re.sub(r"<app\b[^>]*>.*?</app>", blank_apparatus, body, flags=re.S)
    chars, raw_positions = [], array("I")
    i = 0
    for match in _TAG.finditer(body):
        for offset, char in enumerate(body[i:match.start()]):
            if not char.isspace():
                chars.append(char)
                raw_positions.append(body_offset + i + offset)
        i = match.end()
    for offset, char in enumerate(body[i:]):
        if not char.isspace():
            chars.append(char)
            raw_positions.append(body_offset + i + offset)
    return "".join(chars), raw_positions


def _raw_pos_for_kwic(raw, kwic):
    """Map a normalized KWIC to its raw-string start after apparatus removal."""
    normalized, raw_positions = _raw_position_index(raw)
    pos = normalized.find(_WS.sub("", kwic))
    return raw_positions[pos] if pos >= 0 else None


@lru_cache(maxsize=1)
def _raw_position_index_for_rel(rel):
    """Build the expensive raw-position map once per source in a batch process."""
    with open(_abs(rel), encoding="utf-8") as handle:
        return _raw_position_index(handle.read())


def _raw_pos_for_rel_kwic(rel, kwic):
    normalized, raw_positions = _raw_position_index_for_rel(rel)
    pos = normalized.find(_WS.sub("", kwic))
    return raw_positions[pos] if pos >= 0 else None


def _normalized_positions_for_rel_kwic(rel, kwic):
    """Return every normalized-text start for ``kwic`` in ``rel``.

    Attribution review cannot assume the first identical KWIC in a source is
    the saved occurrence.  Keep this helper overlap-aware so repeated matches
    are explicit rather than silently collapsed by ``str.find``.
    """
    normalized, _ = _raw_position_index_for_rel(rel)
    needle = _WS.sub("", kwic)
    if not needle:
        return []
    starts, offset = [], 0
    while True:
        found = normalized.find(needle, offset)
        if found < 0:
            return starts
        starts.append(found)
        offset = found + 1


def _raw_pos_for_rel_kwic_lb(rel, kwic, from_lb):
    """Bind an identical KWIC to its saved primary-edition start line.

    Returns ``(raw_position, metadata)``.  A unique KWIC+lb match is selected;
    zero or multiple lb matches fail closed instead of returning the first
    textual match.  Callers may surface the metadata to a human reviewer.
    """
    _, raw_positions = _raw_position_index_for_rel(rel)
    _, idx2lb = _load(rel)
    starts = _normalized_positions_for_rel_kwic(rel, kwic)
    lb_starts = [start for start in starts if start < len(idx2lb) and idx2lb[start] == from_lb]
    metadata = {
        "kwicMatchCountInSource": len(starts),
        "kwicFromLbMatchCount": len(lb_starts),
        "normalizedMatchStarts": starts,
        "normalizedFromLbMatchStarts": lb_starts,
        "occurrenceIdentityStatus": "unique-kwic-fromlb" if len(lb_starts) == 1 else (
            "kwic-fromlb-not-found" if not lb_starts else "ambiguous-kwic-fromlb"
        ),
    }
    if len(lb_starts) != 1:
        return None, metadata
    start = lb_starts[0]
    metadata["selectedNormalizedStart"] = start
    return raw_positions[start], metadata


def heads(rel, lb, limit=12, kwic=None):
    """Return preceding TEI head texts, nearest first, for ladder rung 3."""
    raw = open(_abs(rel), encoding="utf-8").read()
    pos = _raw_pos_for_rel_kwic(rel, kwic) if kwic else None
    if pos is None:
        m = re.search(r'<lb\b[^>]*\bn="%s"' % re.escape(lb), raw)
        pos = m.start() if m else None
    if pos is None:
        return {"heads": [], "error": "LB_NOT_FOUND"}
    values = []
    for hm in re.finditer(r"<head\b[^>]*>(.*?)</head>", raw[:pos], re.S):
        value = _WS.sub("", _TAG.sub("", hm.group(1)))
        if value:
            values.append(value)
    return {"heads": list(reversed(values[-limit:]))}

@lru_cache(maxsize=None)
def title(rel):
    # The level=m title is in teiHeader; do not reread multi-megabyte bodies for
    # every occurrence during the attribution gate.
    with open(_abs(rel), encoding="utf-8") as fh:
        raw = fh.read(200000)
    m = re.search(r'<title\b[^>]*level="m"[^>]*>(.*?)</title>', raw, re.S)
    return _WS.sub("", _TAG.sub("", m.group(1))) if m else None

if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8")  # Windows console prints CJK
    except Exception:
        pass
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "selftest":
        r = verify("X/X80/X80n1565.xml", "無位真人是甚麼乾屎橛。巖頭不覺吐舌。雪峯曰。")
        print("verify exemplar:", r, "(expect ok=True fromLb=0227a10)")
        print("count 乾屎橛:", {k: v for k, v in count("乾屎橛").items() if k != "per_file"})
        print("title X80n1565:", title("X/X80/X80n1565.xml"))
        print("head @0227a10:", head("X/X80/X80n1565.xml", "0227a10"))
        print("apparatus guard — 四賓主 main-text vs footnote in T47n1985:", verify("T/T47/T47n1985.xml", "四賓主"))
    else:
        print("use: import zc  (see header). CLI: python zc.py selftest")
