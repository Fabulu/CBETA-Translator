import json
from pathlib import Path

BUILD=Path(__file__).resolve().parents[2]
reviewed="2026-07-16T04:20:00Z"
p=BUILD/"fresh-build/entries/t_850d52f97185/entry.v2.json"
d=json.loads(p.read_text(encoding="utf-8")); s1,s2=d["Senses"]
rows={
  0:(s1["Occurrences"][0],"compiler of Wuzu Fayan's recorded sayings",[{"MasterName":"Wuzu Fayan","Roles":["person-described","record-owner"]}],"乃拈起法衣云 is a recorder-supplied action-and-speech marker; Wuzu's quoted description begins after 云.","Old Recorded Sayings of Venerable Masters (古尊宿語錄), complete Wuzu Fayan hall address: the compiler narrates Wuzu lifting the Dharma robe before Wuzu comments on its color."),
  1:(s1["Occurrences"][1],"compiler of Yangqi Fanghui's lamp-record biography",[{"MasterName":"Yangqi Fanghui","Roles":["person-described","record-owner"]}],"受請日，拈法衣示眾曰 is biographical action narration; Yangqi's words begin after 曰.","Complete Book of the Five Lamps (五燈全書), complete Yangqi Fanghui section: the compiler narrates Yangqi taking the Dharma robe on the day he accepts the invitation and showing it to the assembly before quoting him."),
}
for _,(o,label,contexts,evidence,note) in rows.items():
 o["MasterName"]=None;o["ContextMasters"]=contexts
 o["ActorAttribution"]={"Status":"narrated","Kind":"compiler narrative","ActorLabel":label,"ActorRole":"compiler","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":evidence,"ReviewedBy":"Codex cohorts 1-3 v6 full-case hand read","ReviewedUtc":reviewed}
 o["AttributionNote"]=note
for s in d["Senses"]: s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired hand-read entry 26")

# 27 拄杖子 — fourteen complete cases hand-read.
p=BUILD/"fresh-build/entries/t_87cc840b8f33/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));os=d["Senses"][0]["Occurrences"]
recuts={0:"爾有拄杖子。",1:"拄杖子化為龍。",2:"拄杖子卻辯得。",4:"不得喚作拄杖子",5:"接取拄杖子",8:"欲知常住性。當觀拄杖子。",9:"這箇是拄杖子。",13:"這箇拄杖子是三昧"}
recuts[3]="但喚作拄杖子。"
for i,q in recuts.items(): os[i]["Kwic"]=q
# Yuanwu is the voice quoting Yunmen in O4.
o=os[3];o["MasterName"]="Yuanwu Keqin";o["ContextMasters"]=[{"MasterName":"Yuanwu Keqin","Roles":["utterer","record-owner","later-raiser"]},{"MasterName":"Yunmen Wenyan","Roles":["quoted-source","case-figure"]}]
o["AttributionNote"]="Blue Cliff Record, complete Yuanwu commentary: Yuanwu Keqin quotes Yunmen's instruction to call a staff simply a staff. Yuanwu is the utterer of the stored wording; Yunmen is its quoted source."
# O3 recut to Boshan's direct words following the stage direction.
o=os[2];o["MasterName"]="Boshan Yuanlai";o["ContextMasters"]=[{"MasterName":"Boshan Yuanlai","Roles":["utterer","record-owner"]}];o.pop("ActorAttribution",None)
o["AttributionNote"]="Boshan's Recorded Sayings, complete hall address: after the recorder narrates Boshan planting the staff, Boshan directly says, ‘The staff can discern it.’"
for s in d["Senses"]:s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired hand-read entry 27")

# 28 拈出 — nine complete cases hand-read; existing actor assignments survive.
p=BUILD/"fresh-build/entries/t_8cf244d2d802/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"))
for s in d["Senses"]:s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("gated hand-read entry 28")

# 29 飯頭 — seven complete cases hand-read.
p=BUILD/"fresh-build/entries/t_901f410fce73/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));os=d["Senses"][0]["Occurrences"]
os[0]["Kwic"]="黃檗因入厨次問飯頭作什麼"
for s in d["Senses"]:s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired hand-read entry 29")

# 30 盡大地 — eight complete cases hand-read.
p=BUILD/"fresh-build/entries/t_9199b9a31645/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));os=d["Senses"][0]["Occurrences"]
o=os[0];o["MasterName"]="Yuanwu Keqin";o["ContextMasters"]=[{"MasterName":"Yuanwu Keqin","Roles":["utterer","commentator"]},{"MasterName":"Xuefeng Yicun","Roles":["quoted-source","case-figure"]}];o["AttributionNote"]="Blue Cliff Record, complete commentary: Yuanwu Keqin raises and explains Xuefeng Yicun's saying that the whole earth is gathered into a grain. Yuanwu utters the stored wording; Xuefeng is its quoted source."
for i,ctx,note in [(1,[{"MasterName":"Zhaozhou Congshen","Roles":["respondent"]},{"MasterName":"Xuefeng Yicun","Roles":["quoted-source"]}],"Linked-Lamp Compendium, complete Zhaozhou exchange: an unnamed monk quotes Xuefeng's saying that the whole earth is a monk's single eye; Zhaozhou answers."),(2,[{"MasterName":"Huqiu Shaolong","Roles":["respondent","record-owner"]},{"MasterName":"Xuefeng Yicun","Roles":["quoted-source"]}],"Complete Book of the Five Lamps, complete Huqiu exchange: an unnamed monk quotes Xuefeng's saying that the whole earth is a gate of release; Huqiu answers.")]:
 o=os[i];o["MasterName"]=None;o["ContextMasters"]=ctx;o["ActorAttribution"]={"Status":"reviewed-unnamed","Kind":"monk","ActorLabel":"the unnamed questioning monk","ActorRole":"questioner","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"The headword occurs in the monk's direct quotation-question; the named master answers after 師云/師曰.","ReviewedBy":"Codex cohorts 1-3 v6 full-case hand read","ReviewedUtc":reviewed};o["AttributionNote"]=note
os[4]["Kwic"]="所謂盡大地是清涼解脫之場"
os[6]["Kwic"]="盡大地是解脫門把手牽不入"
for s in d["Senses"]:s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired hand-read entry 30")

# 31 無生 — sixteen complete cases hand-read.
p=BUILD/"fresh-build/entries/t_ac4749b5b609/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));os=d["Senses"][0]["Occurrences"]
os[7]["Kwic"]="如今要見無生麼？"
os[12]["Kwic"]="這箇不是無生。"
os[14]["Kwic"]="忍可此法無生"
o=os[11];o["MasterName"]="Xutang Zhiyu";o["ContextMasters"]=[{"MasterName":"Xutang Zhiyu","Roles":["utterer","record-owner","later-raiser"]},{"MasterName":"Liangshan Yuanguan","Roles":["quoted-source","case-figure"]}]
o["AttributionNote"]="Recorded Sayings of Xutang Zhiyu (虛堂和尚語錄), complete hall address: Xutang raises Liangshan Yuanguan's exchange and recites Liangshan's answer about banishment to the country of no-arising. Xutang is the utterer; Liangshan is the quoted source."
for s in d["Senses"]:s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired hand-read entry 31")

# 32 如何是佛法大意 — eight complete cases hand-read.
p=BUILD/"fresh-build/entries/t_bc7bbb4299f1/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));os=d["Senses"][0]["Occurrences"]
os[3]["Kwic"]="上堂僧問如何是佛法大意師豎起拂子"
o=os[7];o["MasterName"]=None;o["ContextMasters"]=[{"MasterName":"Linji Yixuan","Roles":["respondent","record-owner"]}]
o["ActorAttribution"]={"Status":"reviewed-unnamed","Kind":"monk","ActorLabel":"the unnamed questioning monk","ActorRole":"questioner","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"又有僧問 introduces the headword-bearing question; Linji responds with a shout after 師便喝.","ReviewedBy":"Codex cohorts 1-3 v6 full-case hand read","ReviewedUtc":reviewed}
o["AttributionNote"]="Record of Linji (臨濟錄), complete hall exchange: an unnamed monk asks, ‘What is the fundamental meaning of the teaching?’ Linji Yixuan responds with a shout. The prior Linji MasterName conflated respondent with questioner."
for s in d["Senses"]:s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired hand-read entry 32")

# 33 祖印 — seven complete cases hand-read; current actor rulings survive.
p=BUILD/"fresh-build/entries/t_c02887fbd979/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"))
for s in d["Senses"]:s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("gated hand-read entry 33")

# 34 佛法大意 — seven complete cases hand-read.
p=BUILD/"fresh-build/entries/t_cc68e32cf1b4/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));os=d["Senses"][0]["Occurrences"]
for i in (2,4):
 o=os[i];o["MasterName"]="Bai Juyi";o["ContextMasters"]=[{"MasterName":"Bai Juyi","Roles":["utterer","questioner"]},{"MasterName":"Niaoke Daolin","Roles":["respondent","case-figure"]}];o.pop("ActorAttribution",None)
 o["AttributionNote"]="Complete Niaoke Daolin biography: Bai Juyi asks Niaoke, ‘What is the fundamental meaning of the teaching?’ Niaoke answers that one should do no evil and carry out every good. Bai is the utterer; Niaoke is the respondent."
for s in d["Senses"]:s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired hand-read entry 34")

# 35 好手 — seven complete cases hand-read.
p=BUILD/"fresh-build/entries/t_d247a2ea7cc3/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));os=d["Senses"][0]["Occurrences"]
named=[(0,"Chongyuan Wenhui",None,"師曰：跳得出是好手 assigns the phrase to Chongyuan Wenhui in his own section."),(1,"Tiantong Hua",None,"天童華云 explicitly introduces Tiantong Hua's comment."),(3,"Shoushan Shengnian","Shoushan Shengnian","師曰：好手不張名 is Shoushan Shengnian's direct reply in his own section."),(4,"Head Seat Cilang",None,"舉慈朗首座上堂 introduces Cilang's address; the phrase occurs in that address."),(5,"Baichi Yuanshuo","Baichi Yuanshuo","師云 directly assigns the evaluation to Baichi Yuanshuo in his own record."),(6,"Zhuyu",None,"萸曰 introduces Zhuyu's direct answer, 覩對聲色不是好手.")]
for i,label,master,evidence in named:
 o=os[i];o["MasterName"]=master;o["ContextMasters"]=([{"MasterName":master,"Roles":["utterer","record-owner"]}] if master else [])
 o["ActorAttribution"]={"Status":"named-unrostered" if not master else "named","Kind":"master or named officeholder","ActorLabel":label,"ActorRole":"utterer","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":evidence,"ReviewedBy":"Codex cohorts 1-3 v6 full-case hand read","ReviewedUtc":reviewed}
 o["AttributionNote"]=f"Complete structural case: {label} is the exact utterer of the headword-bearing statement."
for s in d["Senses"]:s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired hand-read entry 35")

# Independent rereview hygiene correction: retain the frozen-corpus concordance once.
p=BUILD/"fresh-build/entries/t_8cf244d2d802/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"))
sentence="Frozen-corpus concordance: 2781 exact hits in 365 storage files representing 360 independent works."
note=d["Senses"][0]["Note"]
while note.count(sentence)>1: note=note.replace(" "+sentence,"",1)
d["Senses"][0]["Note"]=note
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired independent-review prose duplication in entry 28 拈出")
