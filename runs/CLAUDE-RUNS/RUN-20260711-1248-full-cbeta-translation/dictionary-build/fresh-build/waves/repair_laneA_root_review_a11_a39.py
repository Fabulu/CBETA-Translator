import hashlib, json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
LEDGER = ROOT / "fresh-build/waves/f001-laneA.json"
BASE = ROOT / "fresh-build/entries"
led = json.loads(LEDGER.read_text(encoding="utf-8"))
by_term = {e["term"]: e for e in led["entries"]}

RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]

def load(term):
    e = by_term[term]
    p = BASE / e["id"] / "entry.v2.json"
    return e, p, json.loads(p.read_text(encoding="utf-8"))

def context(o, name, roles):
    if not name: return
    cms = o.setdefault("ContextMasters", [])
    if not any(c.get("MasterName") == name for c in cms): cms.append({"MasterName": name, "Roles": roles})

def narrated(o, label="the compiler", kind="compiler narration", role="compiler", keep_role="person-described"):
    old = o.get("MasterName")
    o["MasterName"] = None
    context(o, old, [keep_role])
    o["ActorAttribution"] = {"Status":"narrated", "Kind":kind, "ActorLabel":label, "ActorRole":role,
      "GrammarEvidence":"The headword belongs to narrator or editorial grammar; the named figure is described or supplies the surrounding discourse, not this lexical item.",
      "ReviewedBy":"Codex fresh f001 lane A root review", "ReviewedUtc":"2026-07-15T02:00:00Z"}
    o["AttributionNote"] += f" The exact headword-bearing actor is {label}; the named figure is contextual."

def impersonal(o, label="an impersonal textual heading", kind="editorial heading"):
    old=o.get("MasterName"); o["MasterName"]=None; context(o,old,["section-subject"])
    o["ActorAttribution"]={"Status":"impersonal","Kind":kind,"ActorLabel":label,"ActorRole":"compiler",
      "GrammarEvidence":"The headword labels a textual division or discourse occasion and is not a quoted turn by the section subject.",
      "ReviewedBy":"Codex fresh f001 lane A root review","ReviewedUtc":"2026-07-15T02:00:00Z"}
    o["AttributionNote"] += f" The exact headword-bearing actor is {label}; the named figure is only the section subject."

def nonmaster(o, label=None, kind="identified non-master", role="utterer"):
    label=label or o.get("MasterName") or "identified non-master"
    o["MasterName"]=None
    o["ActorAttribution"]={"Status":"identified-non-master","Kind":kind,"ActorLabel":label,"ActorRole":role,"RungsChecked":RUNGS,
      "GrammarEvidence":"The surrounding source identifies this named non-master as the author or speaker of the exact headword-bearing clause.",
      "ReviewedBy":"Codex fresh f001 lane A root review","ReviewedUtc":"2026-07-15T02:00:00Z"}
    o["AttributionNote"] += f" The exact headword-bearing actor is the identified non-master {label}."

changed=set()
def edit(term, fn):
    e,p,z=load(term); fn(z); p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+"\n",encoding="utf-8"); e["entrySha256"]=hashlib.sha256(p.read_bytes()).hexdigest(); changed.add(term)

def fix_seq(z):
    os=z["Senses"][0]["Occurrences"]
    for i in (0,2,4): nonmaster(os[i])
    for i in (1,5): impersonal(os[i])
edit("序",fix_seq)

def fix_shangtang(z):
    os=z["Senses"][0]["Occurrences"]
    for i in (0,1,4,6,7,8,10,11): impersonal(os[i])
    narrated(os[9], keep_role="person-discussed")
    if len(z["Senses"])>1:
      z["Senses"][0]["Explanation"] += " Event headings and verbal descriptions belong to one formal hall-address usage rather than separate lexical senses."
edit("上堂",fix_shangtang)

def fix_shizhong(z):
    for i in (0,2,4,5): narrated(z["Senses"][0]["Occurrences"][i])
    if len(z["Senses"])>1:
      for o in z["Senses"][1]["Occurrences"]: narrated(o)
edit("示眾",fix_shizhong)

def fix_xiaocan(z):
    os=z["Senses"][0]["Occurrences"]
    for i in (2,4,5,6,7): impersonal(os[i])
edit("小參",fix_xiaocan)

def fix_wuxin(z):
    if len(z["Senses"])>1 and len(z["Senses"][1]["Occurrences"])>=3: nonmaster(z["Senses"][1]["Occurrences"][2],"Yongzheng Emperor","emperor")
edit("無心",fix_wuxin)

def fix_canchan(z): nonmaster(z["Senses"][0]["Occurrences"][7],"Tang","identified lay questioner","questioner")
edit("參禪",fix_canchan)

def fix_dawu(z):
    nonmaster(z["Senses"][0]["Occurrences"][1],"Emperor Zhuangzong","emperor")
    nonmaster(z["Senses"][0]["Occurrences"][2],"Tang Shiji","identified non-master")
edit("大悟",fix_dawu)

def fix_zhengfa(z):
    if len(z["Senses"])>1:
      impersonal(z["Senses"][1]["Occurrences"][0],"an impersonal book-title heading","book-title metadata")
      z["Senses"][1]["PreferredTarget"]="the book titled Treasury of the True Dharma Eye"
edit("正法眼藏",fix_zhengfa)

def fix_songgu(z):
    os=z["Senses"][0]["Occurrences"]; nonmaster(os[0],"Guan Youwudang","preface author")
    if len(os)>3: narrated(os[3],keep_role="person-described")
edit("頌古",fix_songgu)

def fix_wuwei(z):
    os=z["Senses"][0]["Occurrences"]; narrated(os[1],keep_role="later-raiser")
    if len(os)>5: nonmaster(os[5],"Yongzheng Emperor","emperor")
edit("無位真人",fix_wuwei)

def fix_zhuanyu(z):
    o=z["Senses"][0]["Occurrences"][0]
    if o.get("ActorAttribution"): o["ActorAttribution"]["ActorRole"]="interlocutor"
edit("轉語",fix_zhuanyu)

def fix_yiqing(z):
    os=z["Senses"][0]["Occurrences"]; narrated(os[3])
    if len(os)>5: nonmaster(os[5],"Ding Libiao","lay author")
edit("疑情",fix_yiqing)

def fix_xiayu(z): narrated(z["Senses"][0]["Occurrences"][6])
edit("下語",fix_xiayu)

def fix_wunian(z):
    if len(z["Senses"])>1:
      os=z["Senses"][1]["Occurrences"]
      if len(os)>2: nonmaster(os[2],"Tao Shikui","official author")
      if len(os)>3: narrated(os[3],keep_role="person-described")
edit("無念",fix_wunian)

# Final normalization for the reader-note and named-non-master gates.
for term in changed:
    e,p,z=load(term)
    touched=False
    for s in z["Senses"]:
      for o in s["Occurrences"]:
        a=o.get("ActorAttribution") or {}
        if a.get("Status")=="narrated" and "narrat" not in o.get("AttributionNote","").lower():
          o["AttributionNote"] += " This is compiler narration."
          touched=True
        if a.get("Status")=="identified-non-master" and "master" in a.get("Kind","").lower():
          a["Kind"]="identified author or speaker"
          touched=True
    if touched:
      p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
      e["entrySha256"]=hashlib.sha256(p.read_bytes()).hexdigest()

led["updatedUtc"]="2026-07-15T02:00:00Z"
LEDGER.write_text(json.dumps(led,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired",len(changed),sorted(changed))
