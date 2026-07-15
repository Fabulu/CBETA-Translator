# -*- coding: utf-8 -*-
import re, json, sys
sys.stdout.reconfigure(encoding="utf-8")
CJK = re.compile(r'[㐀-鿿]{2,}')
BOLD = re.compile(r'\*\*([^*]+?)\*\*')

# --- candidate extraction from the 5 discovery files ---
cands = {}   # headword -> first source line snippet
for n in range(1,6):
    try: txt = open(f"DISCOVERY_{n}.md", encoding="utf-8").read()
    except FileNotFoundError: continue
    for line in txt.splitlines():
        for b in BOLD.findall(line):
            # split variant forms, take the first CJK token as headword
            for piece in re.split(r'[／/·|,，、]', b):
                m = CJK.findall(piece)
                if m:
                    hw = m[0]
                    if hw not in cands: cands[hw] = f"D{n}: {line.strip()[:70]}"
                    break
# --- the "already have / already queued" universe ---
wave = open("WAVE_PLAN.md", encoding="utf-8").read()
req  = open("REQUESTED_TERMS.md", encoding="utf-8").read()
tb = json.load(open(r"C:/temp/NewTranslationrepos/CbetaZenTranslations/termbase.v2.json", encoding="utf-8"))
tbset = set(e["SourceTerm"] for e in tb["Entries"])
roster = json.load(open(r"C:/programmieren/MergeWorkCbeta/CBETA-Translator/Assets/Data/master-dates.json", encoding="utf-8"))
rtext = json.dumps(roster, ensure_ascii=False)

new, dup = [], []
for hw, src in cands.items():
    reason = None
    if hw in tbset: reason="termbase"
    elif hw in wave: reason="WAVE_PLAN"
    elif hw in req: reason="REQUESTED"
    elif hw in rtext: reason="ROSTER(master)"
    if reason: dup.append((hw, reason))
    else: new.append((hw, src))

print(f"candidates extracted: {len(cands)} | NEW gaps: {len(new)} | already-covered: {len(dup)}")
print("\n=== ALREADY COVERED (dropped) ===")
print(", ".join(f"{h}[{r}]" for h,r in sorted(dup)))
print("\n=== NEW GAP CANDIDATES ===")
for h,s in new: print(f"  {h}   ({s})")
# save the NEW list
open("DISCOVERY_COMPILED_NEW.txt","w",encoding="utf-8").write("\n".join(h for h,_ in new))
