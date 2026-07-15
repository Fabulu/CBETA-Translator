import json, sys
from pathlib import Path

root = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(root))
import zc

NOW = "2026-07-16T19:00:00Z"

def load(entry_id):
    path = root / "fresh-build/entries" / entry_id / "entry.v2.json"
    return path, json.loads(path.read_text(encoding="utf-8"))

def save(path, entry):
    path.write_text(json.dumps(entry, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

def cm(name, roles):
    return {"MasterName": name, "Roles": roles}

def actor(occ, label, role, kind, explanation, links):
    occ.pop("MasterName", None)
    evidence = f"Source text ({zc.title(occ['RelPath'])}): {explanation}"
    unnamed = "unnamed" in label or "anonymous" in label
    impersonal = "heading" in kind
    occ["ActorAttribution"] = {
        "Status": "impersonal" if impersonal else ("reviewed-unnamed" if unnamed else "identified-non-master"),
        "Kind": kind,
        "ActorLabel": label,
        "ActorRole": role,
        "GrammarEvidence": evidence,
        "ReviewedBy": "Codex v6 full-case manual audit 056-065",
        "ReviewedUtc": NOW,
    }
    if unnamed:
        occ["ActorAttribution"]["RungsChecked"] = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]
    occ["ContextMasters"] = links
    occ["AttributionNote"] = evidence

def master(occ, name, explanation, links):
    occ["MasterName"] = name
    occ.pop("ActorAttribution", None)
    occ["ContextMasters"] = [cm(name, ["utterer"])] + links
    occ["AttributionNote"] = f"Source text ({zc.title(occ['RelPath'])}): The exact headword speaker is {name}. {explanation}"

# 猊座: in both public exchanges the headword occurs in the monk's question.
# The section master answers afterward and therefore belongs only in context.
path, entry = load("t_6ce996d17a55")
occ = entry["Senses"][0]["Occurrences"]
actor(occ[0], "an unnamed monk", "questioner", "anonymous interlocutor",
      "An unnamed monk asks how the master's ascent of the lion seat benefits beings; Huailian's answer begins after the question.",
      [cm("Huailian", ["respondent"])])
actor(occ[2], "an unnamed monk", "questioner", "anonymous interlocutor",
      "An unnamed monk asks about the moment the master mounts the lion seat; Feiyin Tongrong replies after the headword-bearing question.",
      [cm("Feiyin Tongrong", ["respondent"])])
save(path, entry)

# English-first prose hygiene caught by the depth gate after full-case review.
path, entry = load("t_6275f20a3f87")
sense = entry["Senses"][0]
sense["Explanation"] = sense["Explanation"].replace("answer 胡餅", "flatbread answer (胡餅)")
sense["Occurrences"][2]["AttributionNote"] = "Source text (五燈全書(第34卷-第120卷)): Xuefeng Sihui pairs Yunmen's flatbread with Zhaozhou's tea in a formal hall address."
save(path, entry)

path, entry = load("t_7e7472becb31")
sense = entry["Senses"][0]
sense["Note"] = "Seven witnesses include direct public warning, verse, named case criticism, and two Dahui deployments; the shared construction means 'to make a livelihood in a ghost cave' (鬼窟裏作活計)."
sense["Occurrences"][0]["AttributionNote"] = "Source text (五燈全書(第34卷-第120卷)): Zhongji Kezun says in a formal hall address that even the three-thousand-great-thousand world is only a ghost cave."
sense["Occurrences"][1]["AttributionNote"] = "Source text (宗鑑法林): the compiler places the headword in a verse on Complete Awakening (圓覺) that contrasts the darkest ghost cave with opening the skylight."
save(path, entry)

path, entry = load("t_8cc557911096")
sense = entry["Senses"][0]
sense["Explanation"] = sense["Explanation"].replace("鋒鋩 is", "The cutting edge or point (鋒鋩) is")
sense["Occurrences"][0]["AttributionNote"] = "Source text (五燈全書(第34卷-第120卷)): Letan Yingqian uses the term in his explicitly introduced death verse."
save(path, entry)

path, entry = load("t_b7fa9548f704")
sense = entry["Senses"][0]
for index in (0, 2, 3):
    sense["Occurrences"][index]["AttributionNote"] = sense["Occurrences"][index]["AttributionNote"].replace("in his上堂", "in his formal hall address").replace("in the recorded上堂", "in the recorded formal hall address")
save(path, entry)

# 鬼窟: the governing biography changes from Letan Xiang to his heir Hongfu
# Desheng before this sermon. The full case explicitly identifies Desheng and
# then gives his uninterrupted address.
path, entry = load("t_7e7472becb31")
occ = entry["Senses"][0]["Occurrences"]
master(occ[6], "Hongfu Desheng",
       "Hongfu Desheng says that neither going nor coming is itself 'making a livelihood in a ghost cave'; Letan Xiang appears only in the preceding lineage heading.", [])
save(path, entry)

# 雲門胡餅: the final witness is a case heading. The subsequent prose quotes
# Yunmen's original answer and comments on it, but the bound token itself is
# the impersonal title, not speech.
path, entry = load("t_6275f20a3f87")
occ = entry["Senses"][0]["Occurrences"]
actor(occ[4], "the case heading", "compiler", "impersonal heading",
      "The bound phrase is the title 'Case forty-two: Yunmen's flatbread'; the case then quotes Yunmen Wenyan's separate answer and later commentary.",
      [cm("Yunmen Wenyan", ["case-figure"])])
save(path, entry)

# 第二月: three parallel Yunyan cases preserve different interlocutor labels.
# Keep the exact headword turn distinct from the master's answering gesture.
path, entry = load("t_dcb8d664b64f")
occ = entry["Senses"][0]["Occurrences"]
actor(occ[6], "the temple director", "interlocutor", "identified non-master interlocutor",
      "The temple director asks where there could be a second moon; Yunyan Tansheng answers by raising the broom.",
      [cm("Yunyan Tansheng", ["respondent"])])
save(path, entry)
