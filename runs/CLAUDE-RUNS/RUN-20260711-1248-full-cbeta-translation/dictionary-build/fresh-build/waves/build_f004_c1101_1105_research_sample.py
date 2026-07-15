#!/usr/bin/env python3
"""Create the mandatory five-entry pre-prose evidence sample for f004 lane C."""
from __future__ import annotations
import datetime, json, re, sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
sys.path.insert(0, str(ROOT))
import zc  # noqa: E402

def main() -> None:
    pre = json.loads((HERE / "f004-laneC-1101-1200-preflight.json").read_text(encoding="utf-8"))
    sample=[]
    for ordinal,e in enumerate(pre["entries"][:5],1101):
        selected=[]
        for work in e.get("candidateWorks", []):
            if not work.get("windows") or len(selected) >= max(5, e["evidenceFloor"]):
                continue
            win=work["windows"][0]
            verification=zc.verify(work["RelPath"],win["window"])
            assert verification.get("ok")
            context=zc.context(work["RelPath"],verification["fromLb"],chars=2000,kwic=win["window"])
            head=zc.head(work["RelPath"],verification["fromLb"])
            before=win["window"].split(e["term"],1)[0][-120:]
            if re.search(r"(?:僧問|問[:：])",before): hint="questioner-before-reply; full turn must be read"
            elif re.search(r"(?:師云|師曰|上堂|示眾)",before): hint="enclosing master/address candidate; resolve canonical owner"
            else: hint="utterer versus narrator unresolved until full-case adjudication"
            selected.append({"workId":work["workId"],"RelPath":work["RelPath"],"title":work.get("title"),
                             "FromLb":verification["fromLb"],"ToLb":verification.get("toLb"),"Kwic":win["window"],
                             "zcVerified":True,"sectionHead":head.get("head"),"completeContext":context,"actorResearchHint":hint,
                             "canonicalRosterDecision":None,"exactTurnDecision":None,"admitted":False})
        sample.append({"ordinal":ordinal,"id":e["id"],"term":e["term"],"evidenceFloor":e["evidenceFloor"],
                       "selectedCandidateWorks":len(selected),"verifiedCandidates":selected,
                       "inferenceLedger":{"observation":[],"minimalInference":None,"ordinaryBridge":None,
                                          "falsificationSearches":[],"counterexamples":[],"scope":None,"verdict":None},
                       "differentThingDecision":None,"proseBlocked":True,"compileState":"blocked-before-prose",
                       "gateState":"pending-exact-turn-and-roster"})
    payload={"schemaVersion":1,"generatedUtc":datetime.datetime.now(datetime.timezone.utc).isoformat(),
             "wave":"f004","lane":"C","ordinals":[1101,1105],"purpose":"required representative early-five evidence sample",
             "allCandidateKwicsVerified":all(x["zcVerified"] for r in sample for x in r["verifiedCandidates"]),
             "entryCompilationAttempted":False,"reasonCompilationBlocked":"Guide §8c.11-13 and §8c.11 production order forbid prose/entry compilation before complete-case actor, roster, work, sense, and inference adjudication.",
             "bulkAuthoringAllowed":False,"entries":sample,"f003Touched":False,"otherLanesTouched":False,
             "promotion":False,"merge":False,"siteTouched":False}
    out=HERE/"f004-laneC-1101-1105-early-sample-evidence-packets.json"
    out.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    gate={"schemaVersion":1,"generatedUtc":payload["generatedUtc"],"hardPass":False,"ordinals":[1101,1105],
          "checks":{"entries":5,"candidateKwicsVerified":sum(len(r["verifiedCandidates"]) for r in sample),
                    "exactKwicFailures":0,"completeContextsStored":sum(len(r["verifiedCandidates"]) for r in sample),
                    "exactTurnDecisions":0,"canonicalRosterDecisions":0,"senseDecisions":0,"inferenceLedgersComplete":0,
                    "compiledEntries":0},
          "blockers":["full-case exact-turn adjudication incomplete","canonical roster adjudication incomplete",
                      "different-things and inference ledgers incomplete","no entry may compile until these evidence-identity gates pass"],
          "bulkAuthoringAllowed":False,"rootCauseRepeatStopRuleActive":True}
    (HERE/"f004-laneC-1101-1105-early-sample-gate.json").write_text(json.dumps(gate,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(gate,ensure_ascii=False))

if __name__ == "__main__": main()
