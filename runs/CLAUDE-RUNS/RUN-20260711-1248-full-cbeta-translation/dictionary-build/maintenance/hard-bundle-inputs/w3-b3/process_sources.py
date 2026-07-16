#!/usr/bin/env python3
import json, os, subprocess, sys, tempfile
from datetime import datetime, timezone
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
HERE=Path(__file__).resolve().parent
TRIAGE=HERE.parent/"worker-3-bundle-3-triage.json"
LEDGER=ROOT/"maintenance/bundle-ledgers/hard-w3-b3.json"
LEDGER_MD=ROOT/"maintenance/bundle-ledgers/hard-w3-b3.md"
TOTAL=149

def now(): return datetime.now(timezone.utc).isoformat().replace("+00:00","Z")
def atomic(path,payload):
    fd,tmp=tempfile.mkstemp(prefix=path.name+".",suffix=".tmp",dir=path.parent)
    with os.fdopen(fd,"w",encoding="utf8") as h: json.dump(payload,h,ensure_ascii=False,indent=2); h.write("\n")
    os.replace(tmp,path)
def run(args, report=None):
    p=subprocess.run(args,cwd=ROOT,text=True,capture_output=True)
    if report is not None: report.write_text(p.stdout+("\nSTDERR:\n"+p.stderr if p.stderr else ""),encoding="utf8")
    if p.returncode: raise RuntimeError(f"command failed ({p.returncode}): {' '.join(map(str,args))}\n{p.stdout}\n{p.stderr}")
    return p.stdout

triage=json.loads(TRIAGE.read_text(encoding="utf8"))
ledger=json.loads(LEDGER.read_text(encoding="utf8"))
ledger.update(updatedUtc=now(),reviewedRows=TOTAL,remainingRows=0,unresolvedRows=[],nextUnit={"source":triage["sources"][0]["RelPath"],"stage":"signed-compile-dry-run-apply-focused-gate-replay"})
for n in list(range(10,141,10))+[TOTAL]:
    if not any(c.get("reviewedRows")==n for c in ledger["reviewCheckpoints"]):
        ledger["reviewCheckpoints"].append({"reviewedRows":n,"remainingRows":TOTAL-n,"stage":"six-rung-exact-turn-review","utc":now()})
atomic(LEDGER,ledger)

completed={d["source"] for d in ledger.get("completedSourceDetails",[])}
for source in triage["sources"]:
    rel=source["RelPath"]
    if rel in completed: continue
    stem=Path(rel).stem
    decision=HERE/f"decisions-{stem}.json"; compiled=HERE/f"compiled-{stem}.json"
    dry=HERE/f"dryrun-{stem}.json"; applied=HERE/f"applied-{stem}.json"
    focused=HERE/f"focused-{stem}.json"; replay=HERE/f"replay-{stem}.json"
    run([sys.executable,"compile_attribution_override_sheet.py",str(decision),"--output",str(compiled)])
    run([sys.executable,"apply_attribution_decisions.py",str(compiled),"--report",str(dry)])
    run([sys.executable,"apply_attribution_decisions.py",str(compiled),"--apply","--report",str(applied)])

    payload=json.loads(compiled.read_text(encoding="utf8")); failures=[]; touched=[]
    for row in payload["decisions"]:
        ep=ROOT/"terms"/row["entryId"]/"entry.v2.json"; touched.append(ep)
        entry=json.loads(ep.read_text(encoding="utf8"))
        matches=[o for s in entry.get("Senses",[]) for o in s.get("Occurrences",[]) if o.get("RelPath")==row["RelPath"] and o.get("FromLb")==row["FromLb"] and o.get("Kwic")==row["Kwic"]]
        if len(matches)!=1: failures.append({"key":row["entryId"]+":"+row["FromLb"],"error":f"match count {len(matches)}"}); continue
        actual=matches[0]; expected=row["Decision"]
        for field in ("MasterName","ActorAttribution","AttributionNote"):
            if actual.get(field)!=expected.get(field): failures.append({"key":row["entryId"]+":"+row["FromLb"],"field":field})
        if "ContextMasters" in expected and actual.get("ContextMasters")!=expected["ContextMasters"]:
            failures.append({"key":row["entryId"]+":"+row["FromLb"],"field":"ContextMasters"})
    focus={"source":rel,"rows":len(payload["decisions"]),"exactDecisionMatches":len(payload["decisions"])-len({f['key'] for f in failures}),"failures":failures,"hardPass":not failures}
    atomic(focused,focus)
    if failures: raise RuntimeError(f"focused comparison failed for {rel}: {failures}")
    unique=sorted(set(touched))
    replay_stdout=run([sys.executable,"zc_batch.py","verify-entries",*map(str,unique)])
    replay.write_text(replay_stdout,encoding="utf8")
    replay_payload=json.loads(replay_stdout)
    if replay_payload.get("failureCount"): raise RuntimeError(f"source replay failed for {rel}")

    detail={"source":rel,"rows":len(payload["decisions"]),"entries":len(unique),"compiled":str(compiled.relative_to(ROOT)),"dryRun":str(dry.relative_to(ROOT)),"apply":str(applied.relative_to(ROOT)),"focusedGate":str(focused.relative_to(ROOT)),"sourceReplay":str(replay.relative_to(ROOT)),"completedUtc":now()}
    ledger=json.loads(LEDGER.read_text(encoding="utf8"))
    ledger["completedUnits"].append(rel); ledger["completedSourceDetails"].append(detail)
    ledger["appliedRows"]=sum(x["rows"] for x in ledger["completedSourceDetails"]); ledger["updatedUtc"]=now()
    next_sources=[x["RelPath"] for x in triage["sources"] if x["RelPath"] not in set(ledger["completedUnits"])]
    ledger["nextUnit"]={"source":next_sources[0],"stage":"signed-compile-dry-run-apply-focused-gate-replay"} if next_sources else None
    atomic(LEDGER,ledger)
    lines=["# Hard bundle ledger — hard-w3-b3","",f"- Worker: `/root/source_x66_batch`",f"- Reviewed: {ledger['reviewedRows']}/{TOTAL}",f"- Applied and fully gated: {ledger['appliedRows']}/{TOTAL}",f"- Completed sources: {len(ledger['completedSourceDetails'])}/{len(triage['sources'])}","","## Completed sources",""]
    lines += [f"- `{x['source']}` — {x['rows']} rows; signed compile, strict dry-run, apply, focused comparison, and exact replay passed." for x in ledger["completedSourceDetails"]]
    LEDGER_MD.write_text("\n".join(lines)+"\n",encoding="utf8")
    print(json.dumps({"completed":rel,"rows":detail["rows"],"appliedRows":ledger["appliedRows"]},ensure_ascii=False),flush=True)

ledger=json.loads(LEDGER.read_text(encoding="utf8")); ledger["nextUnit"]=None; ledger["updatedUtc"]=now(); atomic(LEDGER,ledger)
print(json.dumps({"complete":True,"sources":len(ledger["completedSourceDetails"]),"rows":ledger["appliedRows"]},indent=2))
