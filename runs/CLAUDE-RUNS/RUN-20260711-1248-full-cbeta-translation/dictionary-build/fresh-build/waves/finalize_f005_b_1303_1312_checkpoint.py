from pathlib import Path
import datetime, hashlib, json, os, tempfile

R=Path(__file__).resolve().parents[2]
ROWS=json.loads((R/"fresh-build/waves/f005-laneB-1303-1312-author-rows.json").read_text(encoding="utf-8"))["rows"]

def sha(rel): return hashlib.sha256((R/rel).read_bytes()).hexdigest()
def atomic(path,payload):
 fd,tmp=tempfile.mkstemp(prefix=path.name+".",dir=path.parent)
 try:
  with os.fdopen(fd,"w",encoding="utf-8") as h:json.dump(payload,h,ensure_ascii=False,indent=2);h.write("\n")
  os.replace(tmp,path)
 finally:
  if os.path.exists(tmp):os.unlink(tmp)

assert sha("fresh-build/entries/t_df028fd6bd35/entry.v2.json")=="525987c476729e770717f41d5bf51d85884790666025262fe375dc2d1b414de8"
assert sha("fresh-build/entries/t_705aabe99572/entry.v2.json")=="4ca3ee6bdd4affd19b44d8f146148ebb4171f9e4ba84eac3cf14a243351ba305"
payload={
 "schemaVersion":"f005-laneB-1303-1312-author-checkpoint-v1","generatedUtc":datetime.datetime.now(datetime.timezone.utc).isoformat(),
 "sourcePacket":{"path":"fresh-build/waves/f005-laneB-1303-1350-research-packets.json","sha256":sha("fresh-build/waves/f005-laneB-1303-1350-research-packets.json"),"usedAsLeadsOnly":True},
 "ordinals":[1303,1312],"entries":10,"occurrences":sum(r["occurrences"] for r in ROWS),"rows":ROWS,
 "acceptedCanariesPreservedByteIdentical":True,
 "authoringRisk":{"path":"fresh-build/waves/f005-laneB-1303-1312-authoring-risk.json","sha256":sha("fresh-build/waves/f005-laneB-1303-1312-authoring-risk.json"),"passing":10,"flagged":0},
 "preReview":{"path":"fresh-build/waves/f005-laneB-1303-1312-pre-review.json","sha256":sha("fresh-build/waves/f005-laneB-1303-1312-pre-review.json"),"hardPass":True},
 "compositeGate":{"path":"fresh-build/waves/f005-laneB-1303-1312-composite.json","sha256":sha("fresh-build/waves/f005-laneB-1303-1312-composite.json"),"hardPass":True,"exactKwic":76,"exactFailures":0},
 "pendingRoster":{"path":"fresh-build/waves/f005-laneB-1303-1312-pending-roster.json","sha256":sha("fresh-build/waves/f005-laneB-1303-1312-pending-roster.json"),"candidates":len(json.loads((R/"fresh-build/waves/f005-laneB-1303-1312-pending-roster.json").read_text())["candidates"]),"promoted":False},
 "semanticReviewRequired":True,"selfReview":False,"promoted":False,"merged":False,"published":False,
}
atomic(R/"fresh-build/waves/f005-laneB-1303-1312-author-checkpoint.json",payload)

lane_path=R/"fresh-build/waves/f005-laneB.json";lane=json.loads(lane_path.read_text(encoding="utf-8"));by={r["ordinal"]:r for r in ROWS}
for row in lane["entries"]:
 if row["ordinal"] in by:
  row["state"]="drafted-awaiting-independent-review";row["entrySha256"]=by[row["ordinal"]]["entrySha256"]
  row["gateReport"]={"authorCheckpoint":"f005-laneB-1303-1312-author-checkpoint.json"};row["failures"]=[]
lane["updatedUtc"]=payload["generatedUtc"];lane["nextId"]=next(r["id"] for r in lane["entries"] if r["ordinal"]==1313);lane["nextTerm"]=next(r["term"] for r in lane["entries"] if r["ordinal"]==1313)
atomic(lane_path,lane)
