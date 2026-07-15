import json
import sys
from pathlib import Path

root = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(root))
import zc

NOW = "2026-07-16T13:30:00Z"
changed = set()


def load(eid):
    p = root / "fresh-build/entries" / eid / "entry.v2.json"
    return p, json.loads(p.read_text(encoding="utf-8"))


def save(p, e):
    p.write_text(json.dumps(e, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    changed.add(p)


def cm(name, roles):
    return {"MasterName": name, "Roles": roles}


def sn(o, note):
    return f"Source text ({zc.title(o['RelPath'])}): {note}"


def master(o, name, roles, note, extra=None):
    o["MasterName"] = name
    o.pop("ActorAttribution", None)
    links = [cm(name, roles)]
    for link in extra or []:
        if link["MasterName"] not in {x["MasterName"] for x in links}:
            links.append(link)
    o["ContextMasters"] = links
    o["AttributionNote"] = sn(o, f"The exact headword speaker is {name}. {note}")


def actor(o, label, role, status, kind, note, links=None, rungs=False):
    o.pop("MasterName", None)
    if status == "reviewed-unnamed":
        prefix = f"The exact headword actor is {label}; the source does not name this person. "
    elif status in {"narrated", "impersonal"}:
        prefix = f"The exact headword actor is {label}, in narration or editorial text. "
    else:
        prefix = f"The exact headword actor is {label}. "
    evidence = sn(o, prefix + note)
    block = {
        "Status": status, "Kind": kind, "ActorLabel": label, "ActorRole": role,
        "ReviewedBy": "Codex v6-bound full-case manual audit 026-035",
        "ReviewedUtc": NOW, "GrammarEvidence": evidence,
    }
    if rungs:
        block["RungsChecked"] = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]
    o["ActorAttribution"] = block
    o["ContextMasters"] = links or []
    o["AttributionNote"] = evidence


# 法眼宗: o6's bound occurrence is in the layman's first five questions,
# before the text explicitly starts the monk's second series.
p, e = load("t_baaf8fde82d2")
O = e["Senses"][0]["Occurrences"]
actor(O[0], "the unnamed monastic questioner", "questioner", "reviewed-unnamed", "dialogue turn",
      "The monk asks (如何是法眼宗); Cian Jingyuan's answer begins at (師曰).",
      [cm("Cian Jingyuan", ["respondent", "record-owner"])], True)
actor(O[1], "the anthology heading compiler", "compiler", "impersonal", "editorial heading",
      "The bound text is the section label (南嶽下八世（法眼宗）), not a spoken turn.",
      [cm("Fayan Wenyi", ["section-subject", "case-figure"])])
actor(O[3], "the lineage-manual compiler", "compiler", "narrated", "biographical heading",
      "The manual writes the heading (法眼宗) and then identifies Fayan Wenyi; neither occurrence is a quoted utterance.",
      [cm("Fayan Wenyi", ["person-described", "section-subject"])])
actor(O[4], "the unnamed monastic questioner", "questioner", "reviewed-unnamed", "dialogue turn",
      "The monk asks (如何是法眼宗); Cian Jingyuan replies (箭鋒相直不相饒).",
      [cm("Cian Jingyuan", ["respondent", "record-owner"])], True)
actor(O[5], "the unnamed lay questioner", "questioner", "reviewed-unnamed", "dialogue turn",
      "The full exchange starts (居士問) and the bound first (如何是法眼宗) precedes the later marker (僧問); it belongs to the unnamed layman.",
      [cm("Langting Jingting", ["respondent", "record-owner"])], True)
save(p, e)


# 如意 was under-split: an implement, a wish-fulfilling jewel, and a monastery
# name are different things.  Retain every verified quote under the right sense.
p, e = load("t_baf1c6b9db60")
old = e["Senses"][0]
all_occurrences = [o for sense in e["Senses"] for o in sense["Occurrences"]]
by_rel = {o["RelPath"]: o for o in all_occurrences}
O = [by_rel[rel] for rel in [
    "J/J34/J34nB311.xml", "X/X82/X82n1571.xml", "J/J25/J25nB174.xml",
    "T/T48/T48n2016.xml", "J/J27/J27nB191.xml", "J/J28/J28nB208.xml",
    "C/C077/C077n1710.xml",
]]
actor(O[0], "the record compiler", "compiler", "narrated", "narrated teaching-seat action",
      "The compiler narrates (師乃舉如意); Juelang raises the scepter and then speaks.",
      [cm("Juelang Daosheng", ["action-performer", "record-owner"])])
actor(O[2], "the record compiler", "compiler", "narrated", "narrated teaching-seat action",
      "The stage direction (以如意畫○) narrates Juelang drawing a circle with the scepter.",
      [cm("Juelang Daosheng", ["action-performer", "record-owner"])])
actor(O[4], "the record compiler", "compiler", "narrated", "narrated teaching-seat action",
      "The stage direction (以如意畫圓相) narrates Xiangtian Jinian drawing a circle with the scepter.",
      [cm("Xiangtian Jinian", ["action-performer", "record-owner"])])
master(O[5], "Guxue Zhe", ["utterer", "record-owner"],
       "Guxue personifies his scepter as (如意子), gives it a congratulatory speech, and answers it in the same hall address.")
master(O[3], "Yongming Yanshou", ["utterer", "record-owner"],
       "Yongming's authorial exposition says a good friend seeks the one mind (為如意寶), a wish-fulfilling jewel.")
master(O[6], "Baizhang Huaihai", ["utterer", "record-owner"],
       "Baizhang's continuous address says what lies beyond the three clauses (是名如意寶).")
actor(O[1], "the lamp-record compiler", "compiler", "narrated", "biographical place-name",
      "The biography says the master lived at (黃山如意院); this is the proper name of a monastery, not a scepter.",
      [cm("Huangshan Faan", ["person-described", "record-owner"])])

base = {k: v for k, v in old.items() if k not in {"PreferredTarget", "AlternateTargets", "Explanation", "Note", "Occurrences", "RelatedMasters", "Validation"}}
implement = dict(base)
implement.update({
    "PreferredTarget": "ceremonial scepter",
    "AlternateTargets": ["teaching scepter", "ruyi scepter"],
    "Explanation": "A ceremonial scepter used from the teaching seat: the records raise it, draw a circle with it, or personify it as an interlocutor before the assembly. This is a handled implement, distinct from the wish-fulfilling jewel compound and from the monastery name Huangshan Ruyi Cloister.",
    "Note": "The full-case reading separates the action-stage directions from Guxue Zhe's personification of the implement.",
    "Occurrences": [O[0], O[2], O[4], O[5]],
    "RelatedMasters": ["Juelang Daosheng", "Xiangtian Jinian", "Guxue Zhe"],
    "Validation": "multi-source",
})
jewel = dict(base)
jewel.update({
    "PreferredTarget": "wish-fulfilling jewel",
    "AlternateTargets": ["jewel that grants what is wished"],
    "Explanation": "In the compound for a wish-fulfilling jewel (如意寶), the word belongs to that jewel image. Yongming Yanshou and Baizhang Huaihai use the jewel-name in expositions; neither passage refers to the handheld teaching scepter.",
    "Note": "This is a different referent, not a different reading of the implement.",
    "Occurrences": [O[3], O[6]],
    "RelatedMasters": ["Yongming Yanshou", "Baizhang Huaihai"],
    "Validation": "multi-source",
})
place = dict(base)
place.update({
    "PreferredTarget": "Ruyi Cloister",
    "AlternateTargets": ["Ruyi Monastery"],
    "Explanation": "In the name Huangshan Ruyi Cloister (黃山如意院), Ruyi is the proper name of a monastery. The biography uses it as a place-name and supplies no evidence that the cloister is the ceremonial implement or the wish-fulfilling jewel.",
    "Note": "Proper-name sense; one work, therefore provisional.",
    "Occurrences": [O[1]],
    "RelatedMasters": ["Huangshan Faan"],
    "Validation": "provisional",
})
e["Senses"] = [implement, jewel, place]
save(p, e)


# 機鋒 o3 is the named manual compiler's own explanatory prose.
p, e = load("t_c1af3ecba987")
master(e["Senses"][0]["Occurrences"][2], "Huiyan Zhizhao", ["utterer", "compiler"],
       "Huiyan Zhizhao's manual explains the circle exchanges as (師資辨難，互換機鋒); this is authorial prose, not anonymous event narration.")
save(p, e)


# 秦鏡 o4 is an undifferentiated joint compiler voice, not a fictitious master
# named 'Jing and Yun'.
p, e = load("t_c298daf8fd94")
actor(e["Senses"][0]["Occurrences"][3], "the joint Zutang ji compiler voice", "compiler", "narrated", "compiler commentary",
      "After narrating Xuefeng's exchange, the joint compiler voice comments (斯謂：面臨秦鏡); the edition does not assign this sentence to one individual compiler.",
      [cm("Xuefeng Yicun", ["person-discussed", "case-figure"])])
save(p, e)


# 豎拂子: every headword is a stage direction.  Name the master who performs
# each public action, while keeping MasterName null because nobody says 豎拂子.
p, e = load("t_c552c63de77f")
O = e["Senses"][0]["Occurrences"]
performers = [
    ("Fushan Dexuan", "The compiler records Dexuan raising the whisk before asking (還見麼)."),
    ("Zhean Jingfan", "The stage direction follows Zhean's inaugural address and precedes his verse."),
    ("Yushan Ming", "The record says (驀豎拂子) before Yushan asks whether the assembly sees."),
    ("Sanyi Yu", "The stage direction places Sanyi's raised whisk before his claim about the sentence."),
    ("Yinyuan Longqi", "The first and later stage directions both have Yinyuan raise the whisk during his exchange."),
    ("Tianran Hanshi", "The record says Tianran suddenly raises the whisk and asks whether the assembly understands."),
    ("Yune Xiyue", "The small address says Yune suddenly raises the whisk, tests its naming, and later throws it down."),
]
for o, (name, note) in zip(O, performers):
    actor(o, "the record compiler", "compiler", "narrated", "narrated teaching-seat action", note,
          [cm(name, ["action-performer", "record-owner"])])
e["Senses"][0]["RelatedMasters"] = [x[0] for x in performers]
save(p, e)


# 心印 title sense remains an editorial heading, but say so specifically.
p, e = load("t_c968268a64d1")
o = e["Senses"][1]["Occurrences"][0]
actor(o, "the lamp-record heading compiler", "compiler", "impersonal", "biographical heading",
      "The repeated dossier heading names Kaixian Mind-Seal Chan Master (廬山開先心印禪師); no one utters the title wording.",
      [cm("Kaixian Zhixun", ["section-subject", "person-described"])])
save(p, e)


# Existing prose names these case figures; make the reader-facing links explicit.
for eid, si, oi, name, roles in [
    ("t_c1af3ecba987", 1, 2, "Xuefeng Yicun", ["person-discussed", "case-figure"]),
    ("t_c698ab3d0cf9", 1, 1, "Gaofeng Yuanmiao", ["student", "case-figure"]),
    ("t_c968268a64d1", 1, 5, "Bodhidharma", ["case-figure"]),
    ("t_ccae22e8375d", 1, 1, "Zhaozhou Congshen", ["later-quoter"]),
    ("t_cd9e5485fbe1", 1, 3, "Baizhang Huaihai", ["case-figure"]),
]:
    path, entry = load(eid)
    occ = entry["Senses"][si - 1]["Occurrences"][oi - 1]
    links = occ.setdefault("ContextMasters", [])
    if name not in {x.get("MasterName") for x in links}:
        links.append(cm(name, roles))
    save(path, entry)


# The anonymous old worthy supplies the quoted saying; Tianyin is the present
# named raiser in both recensions.
p, e = load("t_b7fd3f3a1395")
for o in e["Senses"][0]["Occurrences"][:2]:
    for link in o.get("ContextMasters", []):
        if link.get("MasterName") == "Tianyin Yuanxiu":
            link["Roles"] = sorted(set(link.get("Roles", [])) | {"later-quoter", "later-raiser"})
save(p, e)


# Frozen-corpus count refreshes.
p, e = load("t_c968268a64d1")
e["Senses"][0]["Note"] = e["Senses"][0]["Note"].replace(
    "336 times in 145 texts", "368 times in 162 texts"
)
save(p, e)
p, e = load("t_ccae22e8375d")
e["Senses"][0]["Explanation"] = e["Senses"][0]["Explanation"].replace(
    "具一隻眼, 381 occurrences", "具一隻眼, 476 occurrences"
)
save(p, e)


# 一隻眼 o6 is Jingfu's signed first-person preface voice, not anonymous
# compiler narration; Jingfu is a named non-master writer.
p, e = load("t_ccae22e8375d")
o = e["Senses"][0]["Occurrences"][5]
actor(o, "Jingfu", "compiler", "identified-non-master", "signed preface author",
      "Jingfu speaks in first person as (符) and hopes a later reader will (別具一隻眼); the source names the writer but does not identify him as a roster master.")
save(p, e)


print(json.dumps({"changed": [str(p.relative_to(root)) for p in sorted(changed)]}, ensure_ascii=False, indent=2))
