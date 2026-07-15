import json, sys
from pathlib import Path

root = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(root))
import audit_attribution, zc
roster = audit_attribution.roster_names()
NOW = "2026-07-16T15:20:00Z"

def load(eid):
    p = root / "fresh-build/entries" / eid / "entry.v2.json"
    return p, json.loads(p.read_text(encoding="utf-8"))

def save(p, e):
    p.write_text(json.dumps(e, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

def cm(name, roles): return {"MasterName": name, "Roles": roles}

def source_note(o, note):
    return f"Source text ({zc.title(o['RelPath'])}): {note}"

def master(o, name, note, links=None):
    o["MasterName"] = name
    o.pop("ActorAttribution", None)
    o["ContextMasters"] = [cm(name, ["utterer"])] + (links or [])
    o["AttributionNote"] = source_note(o, f"The exact headword speaker is {name}. {note}")

def actor(o, label, role, status, kind, note, links=None, rungs=False):
    o.pop("MasterName", None)
    note = source_note(o, f"The exact headword actor is {label}. {note}")
    a = {"Status": status, "Kind": kind, "ActorLabel": label, "ActorRole": role,
         "ReviewedBy": "Codex v6 full-case manual audit 036-045", "ReviewedUtc": NOW,
         "GrammarEvidence": note}
    if rungs:
        a["RungsChecked"] = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]
    o["ActorAttribution"] = a
    o["ContextMasters"] = links or []
    o["AttributionNote"] = note

# 那畔: occurrence 6 belongs to the signed preface voice 埽道人, praising Yinyuan.
p,e=load("t_dc81acde25fd"); o=e["Senses"][0]["Occurrences"][5]
actor(o,"Saodao ren (the Sweeping Daoist)","compiler","identified-non-master","signed preface",
      "The full preface names the Sweeping Daoist as its author; he uses the headword while praising Yinyuan Longqi, who is the subject rather than the utterer.",
      [cm("Yinyuan Longqi",["person-discussed"])]); save(p,e)

# 直心: preserve explicit quotation sources and distinguish compiler narration.
p,e=load("t_dcd5468f5104"); O=e["Senses"][0]["Occurrences"]
actor(O[0],"the Awakening of Faith Treatise","compiler","impersonal","explicit textual citation",
      "The headword clause is explicitly introduced as a statement from the Awakening of Faith Treatise; Wuyi Yuanlai quotes it rather than originating the wording.",
      [cm("Wuyi Yuanlai",["later-quoter","record-owner"])])
actor(O[3],"Vimalakirti","utterer","identified-non-master","explicit scriptural quotation",
      "The full passage identifies Vimalakirti as the source of the claim that the straight mind is the site of awakening because it has no falsity; Yingning Jing is the present quoter.",
      [cm("Yingning Jing",["later-quoter","record-owner"])])
actor(O[5],"the biographical compiler","compiler","narrated","biographical narration",
      "The compiler says Miyun Yuanwu met people only with straight mind and straight conduct; this describes him rather than quoting him.",
      [cm("Miyun Yuanwu",["person-described","record-owner"])])
save(p,e)

# 疑情: the complete units identify two autobiographical public addresses, a
# named lay preface author, and Zhongfeng's letter-like instruction.
p,e=load("t_edabab064644"); O=e["Senses"][0]["Occurrences"]
master(O[3],"Gaofeng Yuanmiao","In his own public instruction, Gaofeng recounts how the doubt-mass suddenly arose and broke.")
master(O[4],"Xueyan Zuqin","In the complete general address, Xueyan recounts his own work with the headword; the prior attribution confused an embedded interlocutor with the address's speaker.")
actor(O[5],"Wang Xigun","compiler","identified-non-master","signed first-person preface narrative",
      "The signature names Wang Xigun, whose first-person preface calls his uncertainty about life and death a great feeling of doubt before recounting his dialogue with Juelang Daosheng.",
      [cm("Juelang Daosheng",["interlocutor","person-discussed"])])
master(O[6],"Zhongfeng Mingben","Zhongfeng writes the complete instruction to the summer assembly and describes the raised saying and the feeling of doubt remaining continuously bound together.")
save(p,e)

# 僧臘 is biographical metadata in every retained occurrence.  Nobody utters
# the headword.  Add rostered dossier subjects only when the complete case
# securely identifies them.
p,e=load("t_eeb3c71567a1"); O=e["Senses"][0]["Occurrences"]
subjects={5:"Chushi Fanqi",6:"Mazu Daoyi"}
for i,o in enumerate(O):
    links=[cm(subjects[i],["person-described"])] if i in subjects else []
    actor(o,"the biographical compiler","compiler","narrated","biographical narration",
          "The complete dossier reports a master's lifespan and monastic seniority (僧臘); this is compiler narration, not a spoken turn.",links)
save(p,e)

# Keep RelatedMasters linkable: non-roster people remain described in prose and
# attribution blocks, not in a field reserved for roster links.
for eid in ["t_d926adb80feb","t_dc81acde25fd","t_dcd5468f5104","t_dd2b39789323","t_dd3bf8dd507a","t_e6eb14b6c1ca","t_e7f672904614","t_ea138c7335d3","t_edabab064644","t_eeb3c71567a1"]:
    p,e=load(eid)
    for s in e.get("Senses",[]):
        s["RelatedMasters"]=[n for n in s.get("RelatedMasters",[]) if n in roster]
    save(p,e)

# Frozen-corpus count refresh.
p,e=load("t_dcd5468f5104")
e["Senses"][0]["Explanation"] = e["Senses"][0]["Explanation"].replace("22 times in 16 texts", "24 times in 17 texts")
save(p,e)

print("repaired checkpoint 036-045")
