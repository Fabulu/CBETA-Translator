#!/usr/bin/env python3
"""zc.py - local ground-truth phrase search over the CBETA corpus.

Strips XML tags + whitespace (the same normalization the search indexer applies)
so a phrase interrupted by inline <lb/>/<pb/>/<note>… still matches. Caches the
stripped per-doc text so repeat queries are fast.

Usage:
  python zc.py 鄰床損腳            # search the CBETA source corpus
  python zc.py --trans 無念西堂     # also include translations (xml-p5t)
  python zc.py --rebuild           # rebuild the stripped-text cache
  python zc.py --rebuild 鄰床損腳   # rebuild then search

Corpus dirs (override via env CBETA_XML_DIR / CBETA_TRANS_DIR):
  C:/programmieren/CbetaZenTexts/xml-p5        (originals)
  C:/programmieren/CbetaZenTranslations/xml-p5t (translations)
"""
import sys, os, re, json, glob, argparse, time

CBETA = os.environ.get("CBETA_XML_DIR", r"C:/programmieren/CbetaZenTexts/xml-p5")
TRANS = os.environ.get("CBETA_TRANS_DIR", r"C:/programmieren/CbetaZenTranslations/xml-p5t")
CACHE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "zc.cache.jsonl")

_TAG = re.compile(r"<[^>]+>")
_WS = re.compile(r"\s+")


def strip(xml: str) -> str:
    """Remove XML tags then all whitespace -> contiguous text (indexer normalization)."""
    return _WS.sub("", _TAG.sub("", xml))


def build_cache(dirs):
    t = time.time()
    n = 0
    with open(CACHE, "w", encoding="utf-8") as out:
        for d in dirs:
            if not os.path.isdir(d):
                print(f"  (skip, not found: {d})", file=sys.stderr)
                continue
            for f in glob.iglob(os.path.join(d, "**", "*.xml"), recursive=True):
                try:
                    with open(f, encoding="utf-8") as fh:
                        xml = fh.read()
                except Exception:
                    continue
                rel = os.path.relpath(f, d).replace("\\", "/")
                out.write(json.dumps({"rel": rel, "t": strip(xml)}, ensure_ascii=False) + "\n")
                n += 1
    print(f"built cache: {n} docs in {time.time()-t:.1f}s -> {CACHE}", file=sys.stderr)


def search(phrase: str):
    q = strip(phrase)
    hits = []
    with open(CACHE, encoding="utf-8") as fh:
        for line in fh:
            d = json.loads(line)
            c = d["t"].count(q)
            if c:
                hits.append((d["rel"], c))
    return q, hits


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("phrase", nargs="?")
    ap.add_argument("--rebuild", action="store_true")
    ap.add_argument("--trans", action="store_true", help="also index translations")
    a = ap.parse_args()

    dirs = [CBETA] + ([TRANS] if a.trans else [])
    if a.rebuild or not os.path.exists(CACHE):
        build_cache(dirs)
    if not a.phrase:
        return

    q, hits = search(a.phrase)
    hits.sort(key=lambda x: -x[1])
    total = sum(c for _, c in hits)
    print(f'query "{a.phrase}"  (normalized "{q}"):  {len(hits)} text(s), {total} occurrence(s)')
    for rel, c in hits[:60]:
        print(f"  {c:>4}  {rel}")


if __name__ == "__main__":
    main()
