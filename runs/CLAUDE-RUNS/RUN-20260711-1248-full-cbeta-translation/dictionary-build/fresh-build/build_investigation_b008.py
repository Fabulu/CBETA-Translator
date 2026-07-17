#!/usr/bin/env python3
"""Explicit evidence-first article for construction Lane B position 008: 把捉."""
import datetime, json, subprocess, sys
from pathlib import Path

DB = Path(__file__).resolve().parent.parent
ROOT = DB / "fresh-build"
sys.path.insert(0, str(DB))
import zc

TERM = "把捉"
BASE = "42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a"
NOW = datetime.datetime.now(datetime.timezone.utc).isoformat()
manifest = json.loads((DB / "maintenance/investigation-next300-construction-lane-b.json").read_text())
ID = next(x["id"] for x in manifest["rows"] if x["headword"] == TERM)

# Each row below is an explicit full-turn decision, not a template inference.
ROWS = [
    {"rel":"X/X79/X79n1557.xml","work":"work:X79n1557","kwic":"夢幻空花，徒勞把捉；心若不異，萬法一如。","label":"Joint Essentials of the Lamps","master":"Zhaozhou Congshen","decision":"Within Zhaozhou Congshen’s continuous address, he calls grasping dreamlike empty flowers wasted labor and immediately pairs this with the mind not differing and the many things being alike."},
    {"rel":"X/X79/X79n1557.xml","work":"work:X79n1557","kwic":"有一般瞎禿子，飽喫飯了，坐禪觀行，把捉念漏，不令放起，厭喧求靜，是外道法。","label":"Joint Essentials of the Lamps","master":"Linji Yixuan","decision":"Linji Yixuan rebukes people who seize and restrain arising thoughts while seeking quiet, explicitly classifying that conduct as an outsider’s teaching."},
    {"rel":"B/B25/B25n0145.xml","work":"work:B25n0145","kwic":"覺得昏沉擾擾散亂紛紛把捉不定處。初無一點外障。","label":"Extended Record of Zhongfeng","master":"Zhongfeng Mingben","decision":"Zhongfeng Mingben names the point where drowsiness and distraction cannot be held steady, then says there is no external obstruction at that point."},
    {"rel":"B/B25/B25n0145.xml","work":"work:B25n0145","kwic":"更須知道。只箇不勞把捉之說。早是墮他夢幻了也。","label":"Extended Record of Zhongfeng","master":"Zhongfeng Mingben","decision":"Zhongfeng Mingben warns that even fixing on the saying that grasping is needless has already fallen into the dreamlike construction under discussion."},
    {"rel":"X/X83/X83n1578.xml","work":"chan:zhiyue-lu","kwic":"夢幻空花，何勞把捉。得失是非，一時放却。","label":"Record Pointing at the Moon","master":"Sengcan","decision":"In Sengcan’s recorded verse, dreamlike empty flowers need no grasping, and gain and loss, right and wrong, are released together."},
    {"rel":"T/T47/T47n1998A.xml","work":"work:T47n1998A","kwic":"覺得昏怛沒巴鼻可把捉時。便是好消息也。莫怕落空。","label":"Recorded Sayings of Chan Master Dahui Pujue","master":"Dahui Zonggao","decision":"Writing to Lü Juren, Dahui Zonggao calls the point at which there is no handle available to grasp good news and tells him not to fear falling into emptiness."},
]

def main():
    occurrences=[]
    for r in ROWS:
        v=zc.verify(r["rel"],r["kwic"])
        assert v.get("ok"),(r["rel"],v)
        occurrences.append({
            "RelPath":r["rel"],"FromLb":v["fromLb"],"ToLb":v["toLb"],"Kwic":r["kwic"],
            "MasterName":r["master"],"Curated":True,
            "AttributionNote":f"Source record ({r['rel']}). {r['label']}: {r['decision']}",
            "ContextMasters":[{"MasterName":r["master"],"Roles":["utterer"]}],
            "DraftActorProof":{"ExactHeadwordClause":r["kwic"],"GrammaticalSubject":r["master"],"SpeechFrame":r["decision"],"FullCaseDecision":r["decision"]},
        })
    sense={
        "SenseKey":None,"MasterName":None,"PreferredTarget":"to grasp",
        "AlternateTargets":["to seize hold of","to hold under control","to keep a grip on"],
        "SearchAliases":["grasp","seize","hold under control","keep hold of","get a grip on"],
        "Status":"preferred","Validation":"multi-source",
        "Note":"Fresh concordance: 171 exact hits in 91 files representing 87 independent works. Six full turns from four independent works anchor distinct deployments; literal physical seizure was tested and did not establish a second referent in the selected Chan uses.",
        "Occurrences":occurrences,"ClaimAnchors":[],
        "SourceTexts":list(dict.fromkeys(r["rel"] for r in ROWS)),
        "RelatedMasters":["Zhaozhou Congshen","Linji Yixuan","Zhongfeng Mingben","Sengcan","Dahui Zonggao"],
        "RelatedTerms":["住心觀靜","放却","沒巴鼻"],
        "ExplanationParts":{
            "CorpusEarnedOpening":"To grasp is to seize something mentally or keep it under control; Chan records characteristically bring the verb to the point where no stable handle can be held. They also criticize the deliberate attempt to restrain thoughts, so the word names both the attempted grip and its failure without becoming a second thing.",
            "EvidenceBody":[
                "Zhaozhou Congshen calls grasping dreamlike empty flowers wasted labor. Linji Yixuan gives the attempt a sharper institutional setting: he rebukes people who seize and restrain arising thoughts while seeking quiet.",
                "Zhongfeng Mingben records being unable to hold steady amid drowsiness and distraction, yet locates no external obstruction there. Elsewhere he warns that turning 'no need to grasp' into a fixed explanation is itself another dreamlike construction.",
                "Sengcan’s verse says that dreamlike empty flowers need no grasping and pairs this with releasing gain and loss, right and wrong. Dahui Zonggao calls the moment when no handle is available to grasp 'good news.'",
                "The corpus therefore does not make grasping a literal seizure in these witnesses. It repeatedly uses the ordinary action of taking or controlling for attempts to secure thoughts, sayings, or an intelligible foothold, and it records the failure of that attempt.",
            ],
        },
        "DraftEvidence":{
            "OpeningClaimEvidenceKeys":["o1","o2","o3","o4","o5","o6"],
            "ZenBend":"The ordinary verb for taking hold becomes a recurrent test of whether thoughts, sayings, or an explanatory foothold can be kept under control; masters both expose the failed grip and rebuke forced restraint.",
            "CounterexampleOrLimit":"The evidence does not establish that every occurrence means attachment, nor that the word itself names a doctrine. The selected literal-seizure control did not supply a distinct physical referent.",
            "DifferentThingTest":{"Decision":"one-thing","ComparedThings":["attempted mental or verbal grasp","failure to keep that grasp"],"Reason":"Success and failure are states of the same grasping action, not different lexical objects; no selected witness denotes a separate physical seizure."},
            "AliasRationale":"Grasp, seize, hold under control, keep hold, and get a grip cover the English action and its control frame without importing a doctrinal label.",
            "ModifierControls":[{"finding":"not-applicable","reason":"The unmodified verb contains no material or color component."}],
            "FamilyControls":[{"finding":"checked","reason":"把捉不定, 沒把捉處, 無把捉, and 不勞把捉 were compared as frames of the same verb; none supplied a different referent."}],
            "IndependentWorkIds":["work:X79n1557","work:B25n0145","chan:zhiyue-lu","work:T47n1998A"],
        },
    }
    data={"SchemaVersion":1,"Entry":{"Id":ID,"SourceTerm":TERM,"CorpusBaselineSha256":BASE,"CreatedBy":"Codex investigation-next300 Lane B explicit author","WrittenUtc":NOW,"Senses":[sense]}}
    out=ROOT/"entries"/ID;out.mkdir(parents=True,exist_ok=True)
    draft=out/"evidence.draft.json";draft.write_text(json.dumps(data,ensure_ascii=False,indent=2)+"\n")
    (out/"WORK.md").write_text("""# 把捉 — construction Lane B position 008

- Fresh exact count: 171 hits, 91 files, 87 independent works.
- Read the complete governing turns for Zhaozhou Congshen, Linji Yixuan, Zhongfeng Mingben, Sengcan, and Dahui Zonggao.
- Six selected witnesses cover wasted grasping, forced thought-restraint, unstable control, doctrinal re-grasping, nowhere-to-grasp, and no-handle deployments.
- Literal physical seizure was tested as the negative control; it did not establish a second referent in the selected Chan concordance.

feedback-inference-verdict: The word names an attempt to seize or control a thought, saying, or foothold, and the recurrent Chan bend is the point where that grip cannot be secured.
feedback-observations: Zhaozhou's wasted labor; Linji's rebuke of restraining thoughts; Zhongfeng's unstable and reified grips; Sengcan's release formula; Dahui's missing handle.
feedback-falsification-searches: literal seizure; 把捉不定; 沒把捉處; 無把捉; 不勞把捉; contradictory praise of maintaining a fixed grip.
feedback-counterexamples: inability and prohibition do not make separate senses; they predicate the same grasping action. No imported doctrine of attachment is asserted.
feedback-scope: Corpus-wide lexical action across the frozen 494-file / 487-work corpus.
lookup-probes: grasp; seize; hold under control; keep hold of; get a grip on.
opening-interpretation-verdict: licensed by the six explicit predicates and narrowed by the literal-seizure and doctrine controls.
sense-target-distinguishability: one sense; success, failure, and criticism are readings or states of the same action, not different things.
""")
    p=subprocess.run([sys.executable,str(DB/"compile_evidence_draft.py"),str(draft),"--output",str(out/"entry.v2.json"),"--report",str(out/"evidence-compile-report.json")],text=True,capture_output=True)
    assert p.returncode==0,p.stdout+p.stderr

if __name__=="__main__":
    main()
