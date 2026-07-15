import datetime, json, subprocess, sys
from pathlib import Path

R = Path(__file__).resolve().parents[2]
E = R / "fresh-build" / "entries"
sys.path.insert(0, str(R))
import zc

NOW = datetime.datetime.now(datetime.timezone.utc).isoformat()


def compile_entry(eid, mutate):
    p = E / eid
    draft = p / "evidence.draft.json"
    data = json.loads(draft.read_text())
    mutate(data["Entry"])
    draft.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n")
    subprocess.run(
        [sys.executable, str(R / "compile_evidence_draft.py"), str(draft),
         "--output", str(p / "entry.v2.json"), "--report", str(p / "round3-final-report.json")],
        check=True,
    )


def name_occurrence(entry, rel, old_lb, name, rationale, find_query=None, find_index=0):
    for sense in entry["Senses"]:
        for occ in sense["Occurrences"]:
            if occ["RelPath"] != rel or (occ["FromLb"] != old_lb and occ.get("MasterName") != name):
                continue
            if find_query:
                hit = zc.find(rel, find_query, ctx=120, limit=20)[find_index]
                verified = zc.verify(rel, hit["window"])
                occ.update(FromLb=verified["fromLb"], ToLb=verified["toLb"], Kwic=hit["window"])
            occ["MasterName"] = name
            occ["ContextMasters"] = [{"MasterName": name, "Roles": ["utterer"]}]
            # ActorAttribution is reserved for MasterName=null outcomes. Named
            # utterers carry the same human decision in DraftActorProof.
            occ.pop("ActorAttribution", None)
            occ["AttributionNote"] = f"Source text ({zc.title(rel)}; {rel}). Exact actor: {name}. {rationale}"
            occ["DraftActorProof"] = {
                "ExactHeadwordClause": occ["Kwic"], "GrammaticalSubject": name,
                "SpeechFrame": rationale, "FullCaseDecision": rationale,
            }
            return
    raise RuntimeError((entry["SourceTerm"], rel, old_lb))


# Replace the anonymous imperial verse with Changsha's explicitly headed public address.
def fix_world(entry):
    sense = entry["Senses"][0]
    sense["Occurrences"] = [o for o in sense["Occurrences"] if o["RelPath"] != "X/X68/X68n1319.xml"]
    hit = zc.find("X/X68/X68n1319.xml", "盡十方世界是沙門眼", ctx=150, limit=10)[0]
    verified = zc.verify("X/X68/X68n1319.xml", hit["window"])
    rationale = "The section heading names Changsha Jingcen; a public-address heading opens his address, and he utters the headword in the repeated formula."
    sense["Occurrences"].append({
        "RelPath": "X/X68/X68n1319.xml", "FromLb": verified["fromLb"], "ToLb": verified["toLb"],
        "Kwic": hit["window"], "MasterName": "Changsha Jingcen", "Curated": True,
        "ContextMasters": [{"MasterName": "Changsha Jingcen", "Roles": ["utterer"]}],
        "AttributionNote": f"Source text ({zc.title('X/X68/X68n1319.xml')}; X/X68/X68n1319.xml). Exact actor: Changsha Jingcen. {rationale}",
        "DraftActorProof": {"ExactHeadwordClause": hit["window"], "GrammaticalSubject": "Changsha Jingcen", "SpeechFrame": rationale, "FullCaseDecision": rationale},
    })
    sense["SourceTexts"] = list(dict.fromkeys(o["RelPath"] for o in sense["Occurrences"]))


compile_entry("t_43f57213c34e", fix_world)
compile_entry("t_7c5f24652dfa", lambda e: name_occurrence(e, "X/X71/X71n1412.xml", "0216b04", "Shushan Kuangren", "Gulin raises Shushan's saying as a case; the explicit Shushan-said frame directly assigns the headword formula to Shushan Kuangren."))
compile_entry("t_897abeb2436c", lambda e: (
    name_occurrence(e, "C/C077/C077n1710.xml", "0624b21", "Baizhang Huaihai", "The full personal-record section is Baizhang Huaihai's discourse, and this uninterrupted clause remains his direct address."),
    name_occurrence(e, "J/J33/J33nB280.xml", "0266c20", "Shending Yunwai Ze", "The record heading names Shending Yunwai Ze; after the cited formula, the explicit master-said frame introduces his own headword-bearing challenge.", "披毛戴角作麼", 0),
))


def add_skin_depth(entry):
    sense = entry["Senses"][0]
    rel = "T/T48/T48n2004.xml"
    hit = zc.find(rel, "天童却道卸却這皮袋", ctx=120, limit=10)[0]
    verified = zc.verify(rel, hit["window"])
    rationale = "Wansong's commentary explicitly contrasts Shitou and Tiantong and utters the headword in his own adjudicating question."
    sense["Occurrences"] = [o for o in sense["Occurrences"] if not (o["RelPath"] == rel and o["FromLb"] == verified["fromLb"])]
    sense["Occurrences"].append({
        "RelPath": rel, "FromLb": verified["fromLb"], "ToLb": verified["toLb"], "Kwic": hit["window"],
        "MasterName": "Wansong Xingxiu", "Curated": True,
        "ContextMasters": [{"MasterName": "Wansong Xingxiu", "Roles": ["utterer"]}],
        "AttributionNote": f"Source text ({zc.title(rel)}; {rel}). Exact actor: Wansong Xingxiu. {rationale}",
        "DraftActorProof": {"ExactHeadwordClause": hit["window"], "GrammaticalSubject": "Wansong Xingxiu", "SpeechFrame": rationale, "FullCaseDecision": rationale},
    })
    sense["SourceTexts"] = list(dict.fromkeys(o["RelPath"] for o in sense["Occurrences"]))
    sense["OpeningClaimEvidenceKeys"] = [f"o{i}" for i in range(1, len(sense["Occurrences"]) + 1)]
    sense["DraftEvidence"]["OpeningClaimEvidenceKeys"] = sense["OpeningClaimEvidenceKeys"]
    sense["DraftEvidence"]["IndependentWorkIds"] = [zc.work_id(x) for x in sense["SourceTexts"]]


compile_entry("t_085b87d75535", add_skin_depth)
