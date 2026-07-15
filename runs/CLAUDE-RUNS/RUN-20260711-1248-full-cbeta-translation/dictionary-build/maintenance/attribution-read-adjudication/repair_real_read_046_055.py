import json, hashlib
from pathlib import Path
BUILD=Path(__file__).resolve().parents[2]
reviewed="2026-07-16T09:00:00Z"

# 46 開山 — seven complete cases hand-read and entry-wide repaired.
p=BUILD/"fresh-build/entries/t_1853f433aff5/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));s1,s2=d["Senses"]
s1["Note"]="Five stored witnesses delimit this verbal sense; two further witnesses below delimit the different person-title sense."
s2["Note"]="Two stored witnesses delimit this person-title sense; grammatical number or a later speaker does not create another referent."
subjects=[
 (0,"Jifei Ruyi","The preface author narrates that Jifei Ruyi subsequently founded Guangshou; Jifei is the described performer, not the utterer."),
 (1,"Miaokan","The lamp biographer reports that Prince Wei asked Miaokan to found the monastery; Miaokan is the described performer."),
 (2,"Zhu'an Shigui","The Zhu'an Shigui biography reports an imperial appointment to found Nengren at Yandang; the inherited Hai Faxiu name was an adjacent-section leak."),
 (3,"Miaokan","The Miaokan biography reports that Prince Wei asked him to found the monastery; the inherited Guanghui Yuanlian name was an adjacent-section leak."),
 (4,"Yongming Yanshou","The Yongming Yanshou biography reports that King Zhongyi asked him to found the new Lingyin monastery; the inherited Foyan Qingyuan name was false."),
]
for i,name,evidence in subjects:
 o=s1["Occurrences"][i];o["MasterName"]=None;o["ContextMasters"]=[{"MasterName":name,"Roles":["action-performer","founding-abbot","person-described"]}]
 o["ActorAttribution"]={"Status":"narrated","Kind":"documentary narrator" if i==0 else "compiler narrative","ActorLabel":"the preface author" if i==0 else "the lamp-record biographer","ActorRole":"compiler","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":evidence,"ReviewedBy":"Codex cohorts 1-3 v6 full-case READ-AND-FIX","ReviewedUtc":reviewed}
 o["AttributionNote"]=evidence
o=s2["Occurrences"][0];o["ContextMasters"]=[{"MasterName":"Yuanjie Ying","Roles":["utterer","record-owner","dedicator"]},{"MasterName":"Ruibai Mingxue","Roles":["dedication-recipient","founding-abbot","predecessor"]}];o["AttributionNote"]="Yuanjie Ying's complete opening incense declaration: Yuanjie utters the headword while identifying his late teacher Ruibai Mingxue as Longhua Monastery's first founding abbot."
o=s2["Occurrences"][1];o["ContextMasters"]=[{"MasterName":"Yulin Tongxiu","Roles":["utterer","record-owner","dedicator"]},{"MasterName":"Tianyin Yuanxiu","Roles":["dedication-recipient","founding-abbot","predecessor"]}];o["AttributionNote"]="Yulin Tongxiu's complete Qingshan informal address: Yulin utters the headword in dedicating incense to his ordination and Dharma teacher, the founding abbot Tianyin Yuanxiu."
for s in d["Senses"]: s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired READ-AND-FIX entry 46")

# 47 喝 — eighteen complete cases hand-read and entry-wide repaired.
p=BUILD/"fresh-build/entries/t_193632bffe7b/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));s=d["Senses"][0];os=s["Occurrences"]
# Recut every multi-token KWIC to one exact headword token without changing its source anchor.
recuts={0:"師問僧：「有時一喝如金剛王寶劍",2:"一喝分賓主者。",7:"喝一",11:"師便喝，僧禮拜",12:"諸上座不得盲喝",14:"棒喝若是禪",16:"三喝"}
for i,k in recuts.items(): os[i]["Kwic"]=k
o=os[0];o["MasterName"]="Linji Yixuan";o["ContextMasters"]=[{"MasterName":"Linji Yixuan","Roles":["utterer","questioner","record-owner"]}];o.pop("ActorAttribution",None);o["AttributionNote"]="Record of Linji, complete examination: Linji Yixuan asks how the monk understands a shout as sword, lion, probing pole or shadowing grass. The recut stores one exact headword token from Linji's speech; his later performed shout remains in the full case."
o=os[6];o["MasterName"]=None;o["ContextMasters"]=[];o["ActorAttribution"]={"Status":"reviewed-unnamed","Kind":"monastic interlocutor","ActorLabel":"the unnamed monk","ActorRole":"shouter","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"\u50e7\u4fbf\u559d assigns the shout to the unnamed monk; \u5e2b\u4fbf打 assigns only the following strike to the master.","ReviewedBy":"Codex cohorts 1-3 v6 full-case READ-AND-FIX","ReviewedUtc":reviewed};o["AttributionNote"]="Complete Sanjiao exchange: the unnamed monk gives the shout and the master strikes afterward. The prior generic narrator label obscured the explicit performer."
# Narrated action still requires the explicitly identified acting master in context.
for i,name in [(4,"Mazu Daoyi"),(5,"Mengxi Heshang"),(7,"Zhenjing Kewen"),(11,"Linji Yixuan")]:
 o=os[i];o["ContextMasters"]=[{"MasterName":name,"Roles":["action-performer","shouter","case-figure"]}]
 o["AttributionNote"]=f"Complete case: the record narrator owns the headword token while explicitly reporting {name} as the master who performs the shout."
# Make the compact explanation identify the corpus's own positive and negative deployment.
s["Explanation"]="A shout is a voiced public-interview action whose placement can expose guest and host or illumination and function. Linji classifies and performs shouts; other records describe monks and masters exchanging them. The corpus also distinguishes this use from blind imitation: Shoushan forbids random shouting, while Songshan Yezhu asks whether ordinary street brawling would count if stick-and-shout itself were Chan. These opposed deployments define the entry without making every loud cry a Zen device."
for ss in d["Senses"]: ss["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in ss["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired READ-AND-FIX entry 47")

# 48 燈籠 — seven complete cases hand-read and entry-wide repaired.
p=BUILD/"fresh-build/entries/t_1f6124388d25/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));s=d["Senses"][0];os=s["Occurrences"]
os[1]["Kwic"]="燈籠是色。那箇是心"
os[2]["Kwic"]="師曰。大好燈籠"
os[5]["Kwic"]="拈燈籠向佛殿裏"
o=os[0];o["MasterName"]=None;o["ContextMasters"]=[{"MasterName":"Dizang Guichen","Roles":["action-performer","respondent","case-figure"]}];o["ActorAttribution"]={"Status":"narrated","Kind":"lamp-record narrator","ActorLabel":"the lamp-record biographer","ActorRole":"compiler","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"\u85cf\u6307\u71c8\u7c60\u66f0 narrates Dizang pointing at the lantern; the object token is in the narrator's frame, while Dizang's quoted word is only 見麼.","ReviewedBy":"Codex cohorts 1-3 v6 full-case READ-AND-FIX","ReviewedUtc":reviewed};o["AttributionNote"]="Complete Qingliang Xiufu biography: the biographer narrates Dizang Guichen pointing to the lantern and asking whether Xiufu sees it. Dizang performs the pointing but does not utter the headword."
o=os[1];o["MasterName"]=None;o["ContextMasters"]=[];o["ActorAttribution"]={"Status":"reviewed-unnamed","Kind":"monastic questioner","ActorLabel":"the unnamed monk","ActorRole":"questioner","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"\u50e7\u554f introduces the recut statement 燈籠是色 and its question; the later master reply is a separate turn.","ReviewedBy":"Codex cohorts 1-3 v6 full-case READ-AND-FIX","ReviewedUtc":reviewed};o["AttributionNote"]="Complete Yanqing Chuanyin exchange: an unnamed monk calls the lantern form and asks which is mind. The respondent later says the lantern is mind; the recut stores one token from the monk's turn."
o=os[5];o["MasterName"]="Yunmen Wenyan";o["ContextMasters"]=[{"MasterName":"Yunmen Wenyan","Roles":["utterer","record-owner","questioner"]}];o.pop("ActorAttribution",None);o["AttributionNote"]="Essentials of Yunmen Kuangzhen, complete chamber prompt: Yunmen Wenyan asks what to make of taking the lantern into the Buddha hall and bringing the three gates atop it. The inherited Huitang attribution was a source-ownership error."
o=os[6];o["MasterName"]="Fachang Yu";o["ContextMasters"]=[{"MasterName":"Fachang Yu","Roles":["quoted-utterer","case-source"]}];o.pop("ActorAttribution",None);o["AttributionNote"]="Recorded Sayings of Wenmu Nian, complete retreat-opening address: Wenmu Nian raises a quoted hall address by Fachang Yu. Fachang is the utterer of the stored headword-bearing line about the lantern exposing sealed-mouth sitters; Wenmu is the later raiser, not its utterer."
s["Explanation"]="A lantern is the monastery's visible light-holder, repeatedly bent into a public-stage object and impossible participant. Dizang points to one; an unnamed monk calls it form and asks which is mind; Guishan answers a question about the ancestral teacher by saying, 'A very good lantern.' Elsewhere it dozes while the exposed pillar is alert, dances while pillars knit their brows, is carried into the Buddha hall beneath the three gates, and exposes silent sitters. These literal, interrogated, and personified deployments still refer to the same lantern; the corpus supplies no hidden substance behind it."
for ss in d["Senses"]: ss["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in ss["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired READ-AND-FIX entry 48")

# 49 卓拄杖 — eight complete cases hand-read and entry-wide repaired.
p=BUILD/"fresh-build/entries/t_2282e557069b/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));s=d["Senses"][0];os=s["Occurrences"]
os[0]["Kwic"]="卓拄杖一下，云：一時粉碎"
performers=["Xuzhou Pudu","Xuanming Foyin","Zisheng Shengqin","Dahui Zonggao","Hanyue Fazang","Buyan Le","Xueyan Zuqin","Yuanwu Keqin"]
for o,name in zip(os,performers):
 o["MasterName"]=None;o["ContextMasters"]=[{"MasterName":name,"Roles":["action-performer","record-owner"]}]
 o["ActorAttribution"]={"Status":"narrated","Kind":"stage-direction narrator","ActorLabel":"the record narrator","ActorRole":"narrator","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":f"The stage direction 卓拄杖 assigns the physical staff strike to {name}; it does not put the headword in the master's quoted speech.","ReviewedBy":"Codex cohorts 1-3 v6 full-case READ-AND-FIX","ReviewedUtc":reviewed}
 o["AttributionNote"]=f"Complete hall unit: the record narrator reports {name} striking or planting the staff before the following words or descent from the seat. {name} is the action performer, not the utterer of the headword."
s["Explanation"]="To strike or plant the staff on the floor is a recurrent teaching-seat action. The records count it once, twice, or three times and place it inside public exchanges, after a pause, beside a shout, before stepping down, or immediately before the master's spoken answer. Xuzhou Pudu distinguishes his two strikes as 'all at once smashed to pieces' and 'the words remain'; Xuanming Foyin is challenged for striking after a question; Zisheng Shengqin strikes before leaving the seat; Buyan Le calls two strikes the staff's explanation. These observable placements surface the Chan deployment without assigning an unspoken doctrine to the blow. Raising, pointing with, and throwing the staff remain different actions. Frozen-corpus concordance: 5662 exact hits in 309 storage files representing 304 independent works."
for ss in d["Senses"]: ss["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in ss["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired READ-AND-FIX entry 49")

# 50 佛祖 — nine complete cases hand-read; two apparent hits are cross-boundary 佛 + 祖云.
p=BUILD/"fresh-build/entries/t_279cf2b97244/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));s=d["Senses"][0];os=s["Occurrences"]
# In both deleted records the question ends with 佛 and the following narrator cue begins 祖云:
# neither passage contains the lexical unit 佛祖. Preserve the seven genuine paired-authority witnesses.
s["Occurrences"]=[o for i,o in enumerate(os) if i not in (6,7)]
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
s["Explanation"]="Buddhas and patriarchs is the corpus's paired authority formula, not a claim that the two are historically identical. Rui'an Sengyin opposes the pair to a demonic army in his death verse; Baichi Yuanshuo and Mingjue Cong invoke their inherited teaching; Huqiu Shaolong says their arrangements have no place in his gate; and Yulin Tongxiu speaks of treading on their crowns. The phrase thus names the established religious and lineage authorities that speakers inherit, invoke, reject as an arrangement, or publicly overtop. Two former witnesses were removed because their text was actually 佛 followed across a turn boundary by 祖云, 'the patriarch said,' not the headword 佛祖."
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired READ-AND-FIX entry 50")

# 51 佛向上事 — seven complete cases hand-read and attribution/prose repaired.
p=BUILD/"fresh-build/entries/t_32f0847e5d1e/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));s=d["Senses"][0];os=s["Occurrences"]
o=os[0];o["Kwic"]="舉洞山云須知有佛向上事";o["MasterName"]="Dongshan Liangjie";o["ContextMasters"]=[{"MasterName":"Dongshan Liangjie","Roles":["quoted-utterer","case-source"]}];o.pop("ActorAttribution",None);o["AttributionNote"]="Old Worthies' Recorded Sayings, complete raised case: the compiler's 舉 introduces Dongshan Liangjie saying that one must know the matter beyond a buddha. The KWIC is recut to that one quoted occurrence; the following monk's question and the later local master's comment are separate turns."
o=os[1];o["MasterName"]="Dongshan Liangjie";o["ContextMasters"]=[{"MasterName":"Dongshan Liangjie","Roles":["utterer","record-owner"]}];o.pop("ActorAttribution",None);o["AttributionNote"]="Complete Dongshan Liangjie biography: 師有時曰 directly assigns to Dongshan the statement that grasping the matter beyond a buddha gives one some share in speech."
o=os[2];o["MasterName"]="Yushan Shangsi";o["ContextMasters"]=[{"MasterName":"Yushan Shangsi","Roles":["utterer","record-owner","questioner"]},{"MasterName":"Dongshan Liangjie","Roles":["quoted-source"]},{"MasterName":"Yunmen Wenyan","Roles":["quoted-commentator"]}];o.pop("ActorAttribution",None);o["AttributionNote"]="Recorded Sayings of Yushan, complete instruction: Yushan Shangsi says that if one grasps the matter beyond a buddha the many medicinal prohibitions exhaust themselves, then asks what that matter is and quotes Dongshan and Yunmen."
o=os[3];o["MasterName"]="Dahui Zonggao";o["ContextMasters"]=[{"MasterName":"Dahui Zonggao","Roles":["utterer","later-commentator","action-performer"]}];o.pop("ActorAttribution",None);o["AttributionNote"]="Collected Ancient Cases of the Chan School explicitly introduces 徑山杲云: Dahui Zonggao says to set the matter beyond a buddha aside for the moment, asks it anew, and answers by dragging over the staff to strike."
for i in (5,6):
 o=os[i];o["MasterName"]=None;o["ContextMasters"]=[{"MasterName":"Xuefeng Yicun","Roles":["respondent","record-owner","action-performer"]}]
 o["ActorAttribution"]={"Status":"named-nonmaster","Kind":"monastic officer","ActorLabel":"Qi, the chief cook (栖典座)","ActorRole":"questioner","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"栖典座問 assigns the headword-bearing quotation of the old saying and the ensuing question to Qi; Xuefeng responds by grabbing and knocking him down.","ReviewedBy":"Codex cohorts 1-3 v6 full-case READ-AND-FIX","ReviewedUtc":reviewed}
 o["AttributionNote"]="Complete Xuefeng exchange: Qi, the chief cook, quotes the old formula 'know the matter beyond a buddha' while asking about speech. Xuefeng Yicun is the respondent and action performer, not the utterer of the stored headword."
s["Explanation"]="The matter beyond a buddha is a stock public-interview formula for what remains when even 'buddha' is treated as a position one can ask beyond. Dongshan Liangjie says it must be known before one has a share in speech and answers a request for it with 'not buddha.' Yushan Shangsi repeats that consequence, Dahui Zonggao tells his audience to set the formula aside before enacting a staff answer, and named and unnamed monastics pose it as a question. The witnesses show a deliberately recurrent question-position and divergent responses; they do not license a single hidden answer."
for ss in d["Senses"]: ss["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in ss["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired READ-AND-FIX entry 51")
