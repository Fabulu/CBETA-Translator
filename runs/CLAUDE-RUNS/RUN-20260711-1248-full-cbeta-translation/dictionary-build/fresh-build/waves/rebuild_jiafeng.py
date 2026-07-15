import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT))
import zc
old = json.loads((ROOT / "terms/t_c728f3a8e02b/entry.v2.json").read_text())
occs = old["Senses"][0]["Occurrences"]

for o in occs:
    if o.get("MasterName"):
        o["ContextMasters"] = [{"MasterName": o["MasterName"], "Roles": ["utterer"]}]
    else:
        o["ContextMasters"] = [
            {"MasterName": x if isinstance(x, str) else x["MasterName"], "Roles": ["respondent"]}
            for x in (o.get("ContextMasters") or [])
        ]
        o["ActorAttribution"]["ActorRole"] = "questioner"

new = [
    ("J/J36/J36nB359.xml", "野老家風興不孤，茅簷竹徑曉霜鋪，等閒懶話朝堂事，醉裏何妨笑語麤？", "Baiyu Jingsi"),
    ("J/J40/J40nB494.xml", "釋迦未出世，鷲嶺有家風；達磨一西來，少林無妙訣。", "Yushan Shangsi"),
    ("J/J28/J28nB202.xml", "二尊宿，一人道有、一人道無，雖則各展家風，要且未能截斷這僧舌頭在。", "Baichi Yuanshuo"),
]
for rel, kwic, name in new:
    v = zc.verify(rel, kwic)
    assert v["ok"], (rel, v)
    title = zc.title(rel)
    occs.append({
        "RelPath": rel, "FromLb": v["fromLb"], "ToLb": v["toLb"], "Kwic": kwic,
        "MasterName": name, "ContextMasters": [{"MasterName": name, "Roles": ["utterer"]}],
        "Curated": True,
        "AttributionNote": f"{title}: full-case review identifies {name}, the record owner speaking in his own hall address, as the utterer of the headword."
    })

entry = {
    "Id": "t_c728f3a8e02b", "SourceTerm": "家風", "CreatedBy": "Codex fresh-build lane C",
    "WrittenUtc": None,
    "CorpusBaselineSha256": "42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a",
    "Senses": [{
        "SenseKey": None,
        "PreferredTarget": "a house's characteristic style",
        "AlternateTargets": ["house style", "lineage style", "family manner"],
        "SearchAliases": ["house style", "lineage style", "teaching style", "family manner"],
        "Status": "preferred",
        "Explanation": "A house's characteristic style is the recognizable manner displayed in its teaching, conduct, and encounter replies. Speakers ask about the style of a named teacher, monastery, inherited line, or older model; answers commonly demonstrate it rather than define it. The same expression also describes preserving, extending, or failing an inherited manner. These uses share one referent: the characteristic way associated with a teaching house.",
        "Validation": "multi-source",
        "Note": "The frozen 494-file corpus has 4,138 exact hits in 364 files representing 359 independent works. Eight standalone anchors cover direct questions, displayed replies, inherited lineage style, appraisal, and later hall-address use across eight independent works. Parallel editions and nested-only strings were excluded from the depth count.",
        "Occurrences": occs,
        "SourceTexts": sorted({o["RelPath"] for o in occs}),
        "RelatedMasters": sorted({o["MasterName"] for o in occs if o.get("MasterName")}),
        "RelatedTerms": ["宗旨", "門風", "家法"]
    }]
}
out = ROOT / "fresh-build/entries/t_c728f3a8e02b"
out.mkdir(parents=True, exist_ok=True)
(out / "entry.v2.json").write_text(json.dumps(entry, ensure_ascii=False, indent=2) + "\n")
(out / "STATUS").write_text("drafted\n")
