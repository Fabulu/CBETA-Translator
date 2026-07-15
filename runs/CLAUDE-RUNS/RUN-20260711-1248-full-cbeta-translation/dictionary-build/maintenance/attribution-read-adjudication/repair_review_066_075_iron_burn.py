import hashlib
import json
import sys
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT))
import zc
OUT = ROOT / "maintenance/attribution-read-adjudication/cohorts-7-9-066-075-review-repair-ledger.json"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def save(path, data):
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


rows = []

# Reviewer correction: repair defective English grammar only; evidence and sense hold.
p = ROOT / "fresh-build/entries/t_1e2ad50d18fe/entry.v2.json"
old = sha(p)
d = json.loads(p.read_text(encoding="utf-8"))
s = d["Senses"][0]
s["Explanation"] = s["Explanation"].replace(
    "The iron tree blossoms is the pictured impossibility of flowers opening from iron.",
    "The iron tree blossoming pictures the impossibility of flowers opening from iron.",
)
save(p, d)
rows.append({"id": d["Id"], "term": d["SourceTerm"], "oldSha256": old,
             "newSha256": sha(p), "changes": ["Repaired the grammatically defective opening; no evidence or sense change."]})

# Reviewer correction: add the strongest frozen-corpus master deployment and state the
# requested Gaofeng–Shiwu limitation explicitly rather than implying it is attested here.
p = ROOT / "fresh-build/entries/t_32289452a85b/entry.v2.json"
old = sha(p)
d = json.loads(p.read_text(encoding="utf-8"))
s = d["Senses"][0]
kw = "大士告眾曰：今世界眾災不息，人民困劇，誰能苦行，燒指為燒？普為一切，供養三寶，請佛住世，普度羣生。"
v = zc.verify("X/X69/X69n1335.xml", kw)
assert v["ok"] and v.get("count") == 1, v
if not any(o.get("Kwic") == kw for o in s["Occurrences"]):
    s["Occurrences"].append({
        "RelPath": "X/X69/X69n1335.xml",
        "FromLb": v["fromLb"],
        "ToLb": v["toLb"],
        "Kwic": kw,
        "Curated": True,
        "MasterName": "Shanhui Dashi",
        "ContextMasters": [{"MasterName": "Shanhui Dashi", "Roles": ["utterer"]}],
        "AttributionNote": "Record of Shanhui Dashi (善慧大士錄): Shanhui Dashi addresses the assembly during a time of public calamity and asks who can undertake the austerity of burning fingers as an offering to the Three Treasures and a plea that the buddhas remain in the world.",
    })
s["Explanation"] = (
    "To burn a finger is literal bodily burning, but the frozen corpus also shows how the act is made into a public demand. "
    "Shanhui Dashi addresses the assembly amid public calamities and asks who can undertake finger burning as an offering and a plea that the buddhas remain in the world. "
    "Elsewhere named petitioners say they cut their ears and burn fingers while urgently pressing Shanhui to remain; two further sources place finger burning in austerity and disciplinary lists. "
    "The often-retold Gaofeng–Shiwu exchange in which fingers are to be burned like incense was searched under its relevant exact and variant forms but is not attested in this frozen corpus, so this entry does not use that story as evidence."
)
s["Note"] = (
    "Four exact witnesses from three independent works. The Shanhui address supplies the master-deployed public demand; "
    "the unavailable Gaofeng–Shiwu wording is explicitly excluded from the evidence rather than silently imported."
)
save(p, d)
rows.append({"id": d["Id"], "term": d["SourceTerm"], "oldSha256": old,
             "newSha256": sha(p), "changes": [
                 "Added and zc-verified Shanhui Dashi's direct public demand for finger burning.",
                 "Rewrote the opening to surface the Chan/public deployment.",
                 "Explicitly recorded that the Gaofeng–Shiwu burn-fingers-like-incense exchange is unattested in the frozen corpus."
             ]})

save(OUT, {"generatedUtc": datetime.now(timezone.utc).isoformat(), "rows": rows})
print(json.dumps({"ledger": str(OUT), "rows": rows}, ensure_ascii=False, indent=2))
