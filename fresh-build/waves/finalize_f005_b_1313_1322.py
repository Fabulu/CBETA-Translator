from pathlib import Path
import datetime,hashlib,json,os,tempfile
W=Path(__file__).resolve().parents[2]
R=W/"runs/CLAUDE-RUNS/RUN-20260711-1248-full-cbeta-translation/dictionary-build"
def sha(p):return hashlib.sha256((R/p).read_bytes()).hexdigest()
def put(p,x):
 p=R/p;fd,t=tempfile.mkstemp(prefix=p.name+".",dir=p.parent)
 with os.fdopen(fd,"w",encoding="utf-8") as h:json.dump(x,h,ensure_ascii=False,indent=2);h.write("\n")
 os.replace(t,p)
rows=json.loads((R/"fresh-build/waves/f005-laneB-1313-1322-author-rows.json").read_text())["rows"]
pre=json.loads((R/"fresh-build/waves/f005-laneB-1313-1322-pre-review.json").read_text())
risk=json.loads((R/"fresh-build/waves/f005-laneB-1313-1322-authoring-risk.json").read_text())
assert pre["hardPass"] and pre["exactKwic"]["verified"]==80 and not risk["flagged"]
assert sha("fresh-build/entries/t_df028fd6bd35/entry.v2.json")=="525987c476729e770717f41d5bf51d85884790666025262fe375dc2d1b414de8"
assert sha("fresh-build/entries/t_705aabe99572/entry.v2.json")=="4ca3ee6bdd4affd19b44d8f146148ebb4171f9e4ba84eac3cf14a243351ba305"
now=datetime.datetime.now(datetime.timezone.utc).isoformat()
payload={"schemaVersion":"f005-laneB-1313-1322-author-checkpoint-v1","generatedUtc":now,"ordinals":[1313,1322],"entries":10,"occurrences":sum(x["occurrences"] for x in rows),"rows":rows,"acceptedCanariesPreservedByteIdentical":True,"authoringRisk":{"path":"fresh-build/waves/f005-laneB-1313-1322-authoring-risk.json","sha256":sha("fresh-build/waves/f005-laneB-1313-1322-authoring-risk.json"),"passing":10,"flagged":0},"preReview":{"path":"fresh-build/waves/f005-laneB-1313-1322-pre-review.json","sha256":sha("fresh-build/waves/f005-laneB-1313-1322-pre-review.json"),"hardPass":True},"compositeGate":{"path":"fresh-build/waves/f005-laneB-1313-1322-pre-review.json","sha256":sha("fresh-build/waves/f005-laneB-1313-1322-pre-review.json"),"hardPass":True,"exactKwic":80,"exactFailures":0},"pendingRoster":{"path":"fresh-build/waves/f005-laneB-1313-1322-pending-roster.json","sha256":sha("fresh-build/waves/f005-laneB-1313-1322-pending-roster.json"),"promoted":False},"semanticReviewRequired":True,"selfReview":False,"promoted":False,"merged":False,"published":False}
put("fresh-build/waves/f005-laneB-1313-1322-author-checkpoint.json",payload)
lp=R/"fresh-build/waves/f005-laneB.json";lane=json.loads(lp.read_text());by={x["ordinal"]:x for x in rows}
for x in lane["entries"]:
 if x["ordinal"] in by:x["state"]="drafted-awaiting-independent-review";x["entrySha256"]=by[x["ordinal"]]["entrySha256"];x["gateReport"]={"authorCheckpoint":"f005-laneB-1313-1322-author-checkpoint.json"};x["failures"]=[]
lane["updatedUtc"]=now;lane["nextId"]=next(x["id"] for x in lane["entries"] if x["ordinal"]==1323);lane["nextTerm"]=next(x["term"] for x in lane["entries"] if x["ordinal"]==1323)
put("fresh-build/waves/f005-laneB.json",lane)
print(json.dumps({"checkpointSha256":sha("fresh-build/waves/f005-laneB-1313-1322-author-checkpoint.json"),"entries":10,"occurrences":payload["occurrences"],"exactKwic":80,"next":1323},indent=2))
