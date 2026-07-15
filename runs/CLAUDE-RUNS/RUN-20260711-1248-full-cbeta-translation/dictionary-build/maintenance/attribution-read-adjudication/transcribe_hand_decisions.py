#!/usr/bin/env python3
"""Losslessly serialize already hand-authored Markdown decisions; makes no verdicts."""
import json, re, sys
from pathlib import Path

src, dst = map(Path, sys.argv[1:3])
rows=[]; entry_id=term=None; row=None
for line in src.read_text(encoding="utf-8").splitlines():
    m=re.match(r"## \d+\. (.+) \(`([^`]+)`\)",line)
    if m: term,entry_id=m.groups(); continue
    m=re.match(r"### S(\d+)/O(\d+) — (.+)",line)
    if m:
        if row: rows.append(row)
        s,o,v=m.groups(); row={"entryId":entry_id,"term":term,"sense":int(s),"occurrence":int(o),"verdict":v,"decisionAuthored":True}
        continue
    if not row: continue
    for prefix,key in [
        ("- Headword clause: ","exactHeadwordClause"),
        ("- Turn/name evidence: ","ownershipEvidence"),
        ("- Actor conclusion: ","adjudicatedActor"),
        ("- Validator ruling: ","reason"),
        ("- Prose/definition impact: ","definitionProseImpact"),
    ]:
        if line.startswith(prefix): row[key]=line[len(prefix):]; break
if row: rows.append(row)
for r in rows:
    # Role remains the author's complete actor/role sentence rather than a guessed enum.
    r["adjudicatedRole"]=r.get("adjudicatedActor")
payload={"schema":"hand-read-attribution-decisions-v1","sourceMarkdown":src.name,"rows":rows}
dst.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print(json.dumps({"rows":len(rows),"output":str(dst)},ensure_ascii=False))
