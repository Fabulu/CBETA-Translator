import json, sys
from pathlib import Path

root = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(root))
import zc

NOW = "2026-07-16T22:00:00Z"

def load(entry_id):
    path = root / "fresh-build/entries" / entry_id / "entry.v2.json"
    return path, json.loads(path.read_text(encoding="utf-8"))

def save(path, entry):
    path.write_text(json.dumps(entry, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

def cm(name, roles):
    return {"MasterName": name, "Roles": roles}

def unnamed(occ, label, role, explanation, links):
    occ.pop("MasterName", None)
    evidence = f"Source text ({zc.title(occ['RelPath'])}): {explanation}"
    occ["ActorAttribution"] = {
        "Status": "reviewed-unnamed", "Kind": "anonymous interlocutor",
        "ActorLabel": label, "ActorRole": role,
        "RungsChecked": ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"],
        "GrammarEvidence": evidence,
        "ReviewedBy": "Codex v6 full-case manual audit 066-075", "ReviewedUtc": NOW,
    }
    occ["ContextMasters"] = links
    occ["AttributionNote"] = evidence

def master(occ, name, explanation, links):
    occ["MasterName"] = name
    occ.pop("ActorAttribution", None)
    occ["ContextMasters"] = [cm(name, ["utterer"])] + links
    occ["AttributionNote"] = f"Source text ({zc.title(occ['RelPath'])}): The exact headword speaker is {name}. {explanation}"

# The title witness was formerly the ambiguous two-character title alone.
path, entry = load("t_f50f469aa43b")
o = entry["Senses"][0]["Occurrences"][0]
o["Kwic"] = "聯燈會要目次聯燈會要目次"
o["FromLb"] = "0001a04"; o["ToLb"] = "0001a04"
for o in entry["Senses"][0]["Occurrences"][1:]:
    o["AttributionNote"] = o["AttributionNote"].replace("the 聯燈會要", "The Essential Collection of the Linked Lamps (聯燈會要)")
save(path, entry)

# The selected 寮元 token belongs to Hefeng's direct cremation address, not
# the duplicated occasion heading that preceded it. Recut to the exact turn.
path, entry = load("t_f54129a637ae")
o = entry["Senses"][0]["Occurrences"][2]
o["Kwic"] = "小雪已去，大雪將來，寮元去住，不假安排"
o["FromLb"] = "0562a05"; o["ToLb"] = "0562a05"
o = entry["Senses"][0]["Occurrences"][3]
o["AttributionNote"] = "Source text (介石智朋禪師語錄): the impersonal occasion heading thanks the quarters supervisor before Jieshi Zhipeng's formal hall address."
save(path, entry)

# In both public questions the monk, not the answering section master, utters
# the headword. The old first-sense second witness illegally stored a role as a
# MasterName; the third had classified the question as compiler narration.
path, entry = load("t_18ec645f99f7")
occ = entry["Senses"][0]["Occurrences"]
unnamed(occ[1], "an unnamed monk", "questioner",
        "An unnamed monk asks 'What is host within guest?'; Fenyang Shanzhao answers 'Facing him, there is no companion.'",
        [cm("Fenyang Shanzhao", ["respondent"])])
unnamed(occ[2], "an unnamed monk", "questioner",
        "An unnamed monk asks the host-within-guest question; Xiaojin answers with the Xu You formula.",
        [cm("Xiaojin", ["respondent"])])
ledger = (" feedback-inference-verdict: the two school-specific referents remain split because the corpus explicitly distinguishes them; "
          "feedback-observations: Linji and Caodong records supply different four-position systems and formulas; "
          "feedback-falsification-searches: checked whether the identical graphs merely varied in wording within one system; "
          "feedback-counterexamples: variant answers within a single system do not create further senses; "
          "feedback-scope: corpus-attested Linji and Caodong deployments only; "
          "lookup-probes: host within guest, guest-host, Linji guest-host, Caodong guest-host; "
          "opening-interpretation-verdict: retained, with the school distinction stated before examples.")
if "feedback-inference-verdict:" not in entry["Senses"][0]["Note"]:
    entry["Senses"][0]["Note"] += ledger
save(path, entry)

# The biography names the lay interlocutor as Pang Yun before abbreviating him
# as 士 throughout this exchange.
path, entry = load("t_168078a96bd7")
master(entry["Senses"][0]["Occurrences"][8], "Pang Yun",
       "Pang Yun threatens to report the master's conduct to clear-eyed people; Guizong Zhichang is the interlocutor who then throws down the tea basket.",
       [cm("Guizong Zhichang", ["interlocutor"])])
save(path, entry)

# English-first and public-feedback ledger repairs for 向上機.
path, entry = load("t_22a3963b99da")
s = entry["Senses"][0]
s["Explanation"] = ("The further mechanism (向上機) prevents an answer—or even the claim to have gone beyond answers—from becoming a final resting place. "
                    "Tonghui Gui contrasts an initial mechanism with a further mechanism not yet trodden, making 'beyond' another public test rather than a hidden teaching. "
                    "Mingjue Cong and Zhean Jingfan display it through ordinary sounds, actions, and conduct, so it is not confined to an abstract formula. "
                    "Yuanwu Keqin calls its expanded form 'raising a sound to stop an echo'; Pin Jixiang calls it a rotten hemp rope; Panshan Liaozong says that even blocking passage between ordinary and sacred is still not it. "
                    "The records deploy the term and then turn it against anyone who settles on the term.")
s["Occurrences"][2]["AttributionNote"] = "Source text (圓悟佛果禪師語錄): Yuanwu Keqin utters the expanded compound, 'the further mechanism' (向上機關), and immediately undercuts treating that label as a stopping point."
ledger = (" feedback-inference-verdict: retained as a public-testing label rather than a hidden object; "
              "feedback-observations: six masters deploy it and several immediately undercut fixation on it; "
              "feedback-falsification-searches: checked whether it named a stable teaching formula or private attainment; "
              "feedback-counterexamples: ordinary sounds and actions are explicitly used to display it; "
              "feedback-scope: the six stored independent works; "
              "lookup-probes: further mechanism, beyond mechanism, upward mechanism, public test; "
          "opening-interpretation-verdict: revised English-first and grounded in the recorded contrasts.")
s["Note"] = s["Note"].replace("stable doctrine", "stable teaching formula")
if "feedback-inference-verdict:" not in s["Note"]:
    s["Note"] += ledger
save(path, entry)
