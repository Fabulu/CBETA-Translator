import json, subprocess, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT))
import zc

p = ROOT / "fresh-build/entries/t_fadc60d82192/evidence.draft.json"
d = json.loads(p.read_text(encoding="utf-8"))
s = d["Entry"]["Senses"][0]
for occurrence in s["Occurrences"][:3]:
    title = zc.title(occurrence["RelPath"])
    if title not in occurrence["AttributionNote"]:
        occurrence["AttributionNote"] = f"Source text ({title}): " + occurrence["AttributionNote"]
rel = "J/J27/J27nB192.xml"
kwic = "乃護法神，不許汝登三寶地，當精誠懺悔"
verified = zc.verify(rel, kwic)
assert verified["ok"]
name = "Daxiu Zhu"
note = "Source text (大休珠禪師語錄): In Daxiu Zhu's autobiographical address, he quotes a fellow monk telling him that a guardian deity barred him from the site of the Three Treasures and that he should repent sincerely; Daxiu owns the present retelling."
proof = "The complete autobiographical address assigns the headword-bearing retelling to Daxiu Zhu."
s["Occurrences"][3] = {
    "RelPath": rel, "FromLb": verified["fromLb"], "ToLb": verified["toLb"], "Kwic": kwic,
    "MasterName": name, "Curated": True, "AttributionNote": note,
    "ContextMasters": [{"MasterName": name, "Roles": ["utterer"]}],
    "DraftActorProof": {"ExactHeadwordClause": kwic, "GrammaticalSubject": name, "SpeechFrame": proof, "FullCaseDecision": proof},
}
s["SourceTexts"] = [o["RelPath"] for o in s["Occurrences"]]
s["RelatedMasters"] = ["Wuyi Yuanlai", "Yongjue Yuanxian", "Tianyuan Fuzhan", "Daxiu Zhu"]
s["DraftEvidence"]["IndependentWorkIds"] = ["work:X72n1435", "work:X72n1437", "work:X82n1571", "work:J27nB192"]
p.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
subprocess.run([sys.executable, str(ROOT / "compile_evidence_draft.py"), str(p)], check=True)
