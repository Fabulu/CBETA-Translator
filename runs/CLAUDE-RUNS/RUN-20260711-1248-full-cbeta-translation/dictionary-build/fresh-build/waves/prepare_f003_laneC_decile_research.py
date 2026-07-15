import argparse, datetime, hashlib, json, os, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
sys.path.insert(0, ROOT)
import zc

ap = argparse.ArgumentParser(); ap.add_argument("start", type=int); args = ap.parse_args()
assert 801 <= args.start <= 891 and (args.start - 801) % 10 == 0
source = "fresh-build/waves/f003-laneC-801-900-preflight.json"
packet = json.load(open(os.path.join(ROOT, source), encoding="utf-8"))
offset = args.start - 801; rows = []
for ordinal, item in zip(range(args.start, args.start + 10), packet["entries"][offset:offset + 10]):
    chosen=[]; works=set()
    for candidate in item["candidateWorks"]:
        if candidate["workId"] in works: continue
        lead=next((w for w in candidate.get("windows",[]) if item["term"] in w["window"]),None)
        if not lead: continue
        found=zc.find(candidate["RelPath"],item["term"],ctx=500)
        match=next((h for h in found if h["fromLb"]==lead.get("fromLb")),found[0] if found else None)
        if not match: continue
        verify=zc.verify(candidate["RelPath"],match["window"])
        if not verify.get("ok"): continue
        works.add(candidate["workId"]); chosen.append({"workId":candidate["workId"],"RelPath":candidate["RelPath"],"title":zc.title(candidate["RelPath"]),"fromLb":verify["fromLb"],"toLb":verify["toLb"],"expandedWindow":match["window"],"zcVerifyOk":True,"headingContext":zc.head(candidate["RelPath"],verify["fromLb"])})
        if len(chosen)>=max(item["evidenceFloor"],4): break
    rows.append({"ordinal":ordinal,"id":item["id"],"term":item["term"],"hits":item["hits"],"files":item["files"],"works":item["works"],"evidenceFloor":item["evidenceFloor"],"selectedDistinctWorks":len(chosen),"workIdUnique":len(works)==len(chosen),"allExpandedWindowsVerified":all(x["zcVerifyOk"] for x in chosen),"witnesses":chosen})
out={"generatedUtc":datetime.datetime.now(datetime.timezone.utc).isoformat(),"wave":"f003","lane":"C","ordinals":[args.start,args.start+9],"corpusBaselineSha256":packet["corpusBaselineSha256"],"sourcePreflight":source,"formalGateRun":False,"siteTouched":False,"state":"verified-research-ready-for-full-turn-attribution","entries":rows}
target=os.path.join(ROOT,f"fresh-build/waves/f003-laneC-{args.start:03d}-{args.start+9:03d}-research-ledger.json")
with open(target,"w",encoding="utf-8") as f: json.dump(out,f,ensure_ascii=False,indent=2);f.write("\n")
print(json.dumps({"output":os.path.relpath(target,ROOT),"entries":len(rows),"witnesses":sum(len(x["witnesses"]) for x in rows),"underFloor":[x["ordinal"] for x in rows if x["selectedDistinctWorks"]<x["evidenceFloor"]],"sha256":hashlib.sha256(open(target,"rb").read()).hexdigest()}))
