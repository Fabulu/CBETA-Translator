#!/usr/bin/env python3
"""Allowlist-first, resumable non-Iriya frequency discovery.

Discovery only. Produces two navigation reservoirs (2 graphs and 3-8 graphs),
then exact apparatus-clean zc counts. It never edits an authority queue, entry,
registry, or lineage file.
"""
from __future__ import annotations

import hashlib, json, re, unicodedata, datetime as dt
from collections import Counter, defaultdict
from pathlib import Path

import zc

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[3]
INDEX = REPO / "bin/Debug/net8.0/index/CbetaZenTexts"
ALLOW = REPO / "Assets/Data/zen-corpus.json"
MAINT = HERE / "maintenance"
OUT = MAINT / "non-iriya-frequency-reservoir-v2-20260718.json"
RECEIPT = MAINT / "non-iriya-frequency-reservoir-v2-receipt-20260718.md"
SHORT_CP = MAINT / "non-iriya-frequency-reservoir-v2-shortlist.checkpoint.json"
EXACT_CP = MAINT / "non-iriya-frequency-reservoir-v2-exact.checkpoint.json"

CJK_RUN = re.compile(r"[\u3400-\u9fff]{2,}")
TARGET_PER_BAND = 5000
SHORTLIST_PER_BAND = 16000
LOCAL_KEEP = {2: 2200, 3: 1400, 4: 900, 5: 650, 6: 500, 7: 400, 8: 350}

# Explicit authority manifest: no wildcard over historical/rejected packets.
AUTHORITY = [
    "WAVE_PLAN.md", "REQUESTED_TERMS.md", "REQUESTED_BUILD_PLAN.md",
    "NEXT500_TERMS.md", "NEXT500_BUILD_PLAN.md", "NEXT500_CANDIDATES_A.md",
    "NEXT500_CANDIDATES_B.md", "NEXT500_RELATED_POOL.tsv",
    "NEXT100_BUILD_PLAN.md", "NEXT100_SAYINGS_CANDIDATES.md",
    "RELATED_INVESTIGATION_BACKLOG.md", "IRIYA_SAYINGS_QUEUE.md",
    "IRIYA_FINAL_BUILD_PLAN.md", "maintenance/iriya-trusted-registry.json",
    "maintenance/investigation-next300-final-semantic-admission.json",
    "fresh-build/queue.json",
]


def sha(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(8 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def cjk_terms(value: str) -> set[str]:
    return {x for x in CJK_RUN.findall(value) if 2 <= len(x) <= 32}


def authority_terms() -> tuple[set[str], list[dict]]:
    covered: set[str] = set()
    manifest: list[dict] = []
    installed = HERE / "fresh-build/merged/termbase.v2.json"
    root = json.loads(installed.read_text(encoding="utf-8-sig"))
    for e in root.get("Entries", []):
        for v in [e.get("SourceTerm")]:
            if isinstance(v, str): covered.add(v.strip())
        for s in e.get("Senses", []):
            for v in s.get("SearchAliases") or []:
                if isinstance(v, str) and CJK_RUN.fullmatch(v.strip()): covered.add(v.strip())
    manifest.append({"path": str(installed.relative_to(HERE)), "sha256": sha(installed), "role": "installed"})

    field_re = re.compile(r'"(?:term|headword|sourceTerm|SourceTerm|query)"\s*:\s*"([^"\\]+)"')
    for name in AUTHORITY:
        p = HERE / name
        if not p.exists():
            manifest.append({"path": name, "missing": True, "role": "planned-authority"})
            continue
        text = p.read_text(encoding="utf-8-sig", errors="replace")
        found: set[str] = set()
        if p.suffix == ".json":
            for m in field_re.finditer(text): found.update(cjk_terms(m.group(1)))
        elif p.suffix == ".tsv":
            for line in text.splitlines():
                for cell in line.split("\t"): found.update(cjk_terms(cell))
        else:
            for line in text.splitlines():
                if "|" in line:
                    for cell in line.split("|"):
                        cell = cell.strip().strip("`* ")
                        if CJK_RUN.fullmatch(cell): found.add(cell)
                for v in re.findall(r"`([\u3400-\u9fff]{2,32})`", line): found.add(v)
        covered.update(found)
        manifest.append({"path": name, "sha256": sha(p), "termsParsed": len(found), "role": "planned-authority"})
    return covered, manifest


def index_rows():
    allow = json.loads(ALLOW.read_text(encoding="utf-8-sig"))
    allowed = {str(x).replace("\\", "/") for x in allow["texts"]}
    work_ids = {str(k).replace("\\", "/"): v for k, v in allow["work_ids"].items()}
    manifest = json.loads((INDEX / "search.text.manifest.json").read_text(encoding="utf-8-sig"))
    rows = [r for r in manifest["Entries"] if int(r.get("Side", 0)) == 0 and str(r["RelPath"]).replace("\\", "/") in allowed]
    return rows, work_ids


def shortlist_signature(authority_manifest: list[dict]) -> str:
    payload = {
        "allow": sha(ALLOW), "textManifest": sha(INDEX / "search.text.manifest.json"),
        "textBin": sha(INDEX / "search.text.bin"), "authority": authority_manifest,
        "localKeep": LOCAL_KEEP, "shortlist": SHORTLIST_PER_BAND,
    }
    return hashlib.sha256(json.dumps(payload, sort_keys=True).encode()).hexdigest()


def build_shortlist(covered: set[str], authority_manifest: list[dict]):
    signature = shortlist_signature(authority_manifest)
    resume = None
    if SHORT_CP.exists():
        cp = json.loads(SHORT_CP.read_text())
        if cp.get("signature") == signature and cp.get("complete"):
            return cp
        if cp.get("signature") == signature and cp.get("approx"):
            resume = cp
    rows, _work_ids = index_rows()
    approx = {n: Counter(dict(resume["approx"].get(str(n), []))) if resume else Counter()
              for n in range(2, 9)}
    processed = int(resume.get("processedFiles", 0)) if resume else 0
    with (INDEX / "search.text.bin").open("rb") as fh:
        for row in rows[processed:]:
            fh.seek(int(row["TextOffset"])); text = fh.read(int(row["TextLengthBytes"])).decode("utf-8")
            runs = CJK_RUN.findall(text)
            for n in range(2, 9):
                local = Counter()
                for run in runs:
                    local.update(run[i:i+n] for i in range(max(0, len(run)-n+1)))
                approx[n].update(dict(local.most_common(LOCAL_KEEP[n])))
            processed += 1
            if processed % 50 == 0:
                partial = {str(n): approx[n].most_common(SHORTLIST_PER_BAND * 2) for n in range(2, 9)}
                SHORT_CP.write_text(json.dumps({"schemaVersion":"shortlist-partial-v1","signature":signature,"processedFiles":processed,"complete":False,"approx":partial}, ensure_ascii=False), encoding="utf-8")
    two = [(t, c) for t, c in approx[2].most_common() if t not in covered][:SHORTLIST_PER_BAND]
    long_all = []
    for n in range(3, 9): long_all.extend((t, c, n) for t, c in approx[n].most_common(SHORTLIST_PER_BAND) if t not in covered)
    long_all.sort(key=lambda x: (-x[1], -x[2], x[0]))
    seen = set(); long = []
    for t, c, n in long_all:
        key = unicodedata.normalize("NFKC", t)
        if key in seen: continue
        seen.add(key); long.append((t, c, n))
        if len(long) >= SHORTLIST_PER_BAND: break
    cp = {"schemaVersion":"shortlist-v2","signature":signature,"processedFiles":processed,"complete":True,"two":two,"long":long}
    SHORT_CP.write_text(json.dumps(cp, ensure_ascii=False), encoding="utf-8")
    return cp


def exact_counts(short_cp: dict, authority_manifest: list[dict]):
    terms = {t for t, _ in short_cp["two"]} | {t for t, _, _ in short_cp["long"]}
    signature = hashlib.sha256((short_cp["signature"] + "|zc-apparatus-clean-nonoverlap-v4|" + str(len(terms))).encode()).hexdigest()
    resume = None
    if EXACT_CP.exists():
        cp = json.loads(EXACT_CP.read_text())
        if cp.get("signature") == signature and cp.get("complete"):
            return cp
        if cp.get("signature") == signature:
            resume = cp
    by_len = defaultdict(set)
    for t in terms: by_len[len(t)].add(t)
    hits = Counter(resume.get("hits", {})) if resume else Counter()
    files = Counter(resume.get("files", {})) if resume else Counter()
    works = defaultdict(set, {k:set(v) for k,v in resume.get("works", {}).items()}) if resume else defaultdict(set)
    allow = json.loads(ALLOW.read_text(encoding="utf-8-sig")); work_ids = allow["work_ids"]
    done = int(resume.get("processedFiles", 0)) if resume else 0
    for rel in allow["texts"][done:]:
        text, _lb = zc._load(rel); seen = set()
        for run in CJK_RUN.findall(text):
            last_end = {}
            for n, candidates in by_len.items():
                for i in range(max(0, len(run)-n+1)):
                    term = run[i:i+n]
                    if term in candidates and i >= last_end.get(term, -1):
                        hits[term] += 1; seen.add(term); last_end[term] = i + n
        for term in seen: files[term] += 1; works[term].add(work_ids[rel])
        zc._cache.get("files", {}).pop(rel, None)
        done += 1
        if done % 25 == 0:
            EXACT_CP.write_text(json.dumps({"schemaVersion":"exact-partial-v1","signature":signature,"processedFiles":done,"complete":False,"hits":hits,"files":files,"works":{k:sorted(v) for k,v in works.items()}}, ensure_ascii=False), encoding="utf-8")
    cp = {"schemaVersion":"exact-v2","signature":signature,"processedFiles":done,"complete":True,"hits":hits,"files":files,"works":{k:sorted(v) for k,v in works.items()}}
    EXACT_CP.write_text(json.dumps(cp, ensure_ascii=False), encoding="utf-8")
    return cp


def row_flags(term: str, parents: list[str], children: list[str]) -> list[str]:
    out = ["DISCOVERY-ONLY; FULL-CASE-SEMANTIC-REVIEW-REQUIRED"]
    grammar = set("之其而以於為者也所不無有是此彼何如若則乃尚可未已將欲能故又或與及")
    if any(c in grammar for c in term): out.append("triage:generic-character-present")
    if parents: out.append("family:substring-of-covered-term")
    if children: out.append("family:contains-covered-term")
    return out


def main():
    covered, authority_manifest = authority_terms()
    short = build_shortlist(covered, authority_manifest)
    exact = exact_counts(short, authority_manifest)
    nav_two = dict(short["two"]); nav_long = {t:c for t,c,n in short["long"]}
    candidates = set(nav_two) | set(nav_long)
    parent_map = defaultdict(set)
    for parent in covered:
        for n in range(2, min(8, len(parent) - 1) + 1):
            for i in range(len(parent) - n + 1):
                child = parent[i:i+n]
                if child in candidates:
                    parent_map[child].add(parent)
    child_map = defaultdict(set)
    for term in candidates:
        for n in range(2, len(term)):
            for i in range(len(term) - n + 1):
                child = term[i:i+n]
                if child in covered:
                    child_map[term].add(child)
    def make_row(term, band):
        ws = exact["works"].get(term, [])
        parents = sorted(parent_map.get(term, ()))[:12]
        children = sorted(child_map.get(term, ()))[:12]
        return {"term":term,"graphs":len(term),"band":band,"indexNavigationHits":(nav_two if band=="2" else nav_long).get(term,0),"zcExactHits":int(exact["hits"].get(term,0)),"zcExactFiles":int(exact["files"].get(term,0)),"zcExactDistinctWorks":len(ws),"flags":row_flags(term,parents,children),"substringParents":parents,"coveredChildren":children,"status":"DISCOVERY-ONLY; AWAITING-MANUAL-SEMANTIC-VETTING"}
    two = [make_row(t,"2") for t,_ in short["two"]]
    long = [make_row(t,"3-8") for t,_,_ in short["long"]]
    for rows in (two,long): rows.sort(key=lambda r:(-r["zcExactHits"],-r["zcExactDistinctWorks"],-r["graphs"],r["term"]))
    two=two[:TARGET_PER_BAND];long=long[:TARGET_PER_BAND]
    artifact_hashes = {name:sha(INDEX/name) for name in ["search.text.bin","search.text.manifest.json","search.corpusfreq.bin","search.corpusfreq.manifest.json"]}
    text_manifest=json.loads((INDEX/"search.text.manifest.json").read_text(encoding="utf-8-sig"));freq_manifest=json.loads((INDEX/"search.corpusfreq.manifest.json").read_text(encoding="utf-8-sig"))
    out={"schemaVersion":"non-iriya-frequency-reservoir-v2","generatedUtc":dt.datetime.now(dt.timezone.utc).isoformat(),"scope":"allowlist-first discovery only; not accepted entries","frozen":{"allowlistPath":str(ALLOW.relative_to(REPO)),"allowlistSha256":sha(ALLOW),"textIndexStamp":text_manifest.get("IndexStamp"),"corpusFrequencyIndexStamp":freq_manifest.get("IndexStamp"),"artifactSha256":artifact_hashes},"authoritySources":authority_manifest,"coveredExactTerms":len(covered),"method":{"navigation":"per-allowlisted-file heavy-hitter shortlist from search.text.bin; never whole-index preselection","finalCounts":"apparatus-clean exact counts from zc normalized source XML","nested":"exact covered terms excluded; substrings retained and family-flagged","normalization":"exact graph strings retained; NFKC used only to dedupe shortlist identities"},"checkpoints":[str(SHORT_CP.relative_to(HERE)),str(EXACT_CP.relative_to(HERE))],"bands":{"2":{"target":TARGET_PER_BAND,"rows":two},"3-8":{"target":TARGET_PER_BAND,"rows":long}},"counts":{"two":len(two),"long":len(long),"total":len(two)+len(long)},"authorityMutation":False,"entriesBuilt":False,"registryMutation":False,"lineageMutation":False}
    OUT.write_text(json.dumps(out,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    RECEIPT.write_text("# Non-Iriya frequency reservoir v2 receipt\n\nDiscovery only; no authority mutation.\n\n"+f"- Output: `{OUT.relative_to(HERE)}` SHA-256 `{sha(OUT)}`\n- Shortlist checkpoint: `{SHORT_CP.relative_to(HERE)}` SHA-256 `{sha(SHORT_CP)}`\n- Exact checkpoint: `{EXACT_CP.relative_to(HERE)}` SHA-256 `{sha(EXACT_CP)}`\n- 2-graph rows: {len(two)}\n- 3-8-graph rows: {len(long)}\n- Frozen allowlist: `{sha(ALLOW)}`\n- Text index stamp: `{text_manifest.get('IndexStamp')}`\n- Corpus-frequency index stamp: `{freq_manifest.get('IndexStamp')}`\n\nAll rows require manual full-case semantic vetting.\n",encoding="utf-8")
    print(json.dumps({"output":str(OUT),"two":len(two),"long":len(long),"sha256":sha(OUT)},ensure_ascii=False))


if __name__ == "__main__": main()
