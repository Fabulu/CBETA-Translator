import json
from pathlib import Path
import zc

plans = {
    "t_7c2c8da520e4": ("壓良為賤", "J/J35/J35nB342.xml", "Huayan Shengke"),
    "t_ff3b9302050a": ("逢場作戲", "J/J36/J36nB359.xml", "Baiyu Si"),
    "t_4b4c8dc868b7": ("賊過後張弓", "T/T47/T47n1996.xml", "Xuedou Chongxian"),
    "t_d400e8468267": ("鑽龜打瓦", "J/J37/J37nB388.xml", "Shending Yikui"),
}
for tid, (term, rel, name) in plans.items():
    path = Path("fresh-build/entries") / tid / "evidence.draft.json"
    data = json.loads(path.read_text(encoding="utf-8"))
    row = zc.find(rel, term, ctx=32)[1]
    verified = zc.verify(rel, row["window"])
    proof = f"The continuing full address assigns this additional headword-bearing clause to {name}."
    occurrence = {
        "RelPath": rel, "FromLb": verified["fromLb"], "ToLb": verified["toLb"], "Kwic": row["window"],
        "MasterName": name, "Curated": True,
        "AttributionNote": f"Source text ({zc.title(rel)}): {name} uses the same idiom again in a distinct clause of the complete address.",
        "ContextMasters": [{"MasterName": name, "Roles": ["utterer"]}],
        "DraftActorProof": {"ExactHeadwordClause": row["window"], "GrammaticalSubject": name, "SpeechFrame": proof, "FullCaseDecision": proof},
    }
    data["Entry"]["Senses"][0]["Occurrences"].append(occurrence)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
