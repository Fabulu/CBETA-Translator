import json
from pathlib import Path

BUILD=Path(__file__).resolve().parents[2]

# 56 速道 — all seven complete cases hand-read.
p=BUILD/"fresh-build/entries/t_4d4cbd834b80/entry.v2.json"
d=json.loads(p.read_text(encoding="utf-8"));s=d["Senses"][0];os=s["Occurrences"]
for i,k in {0:"道不得也叉下死。速道。學徒鮮有對者",1:"大悲千手眼那箇是正眼，速道",3:"不得下語，不得無語。速道！僧曰：請和尚放下竹篦",5:"去此二途，速道！曰：錯",6:"大悲千手眼，那箇是正眼？速道！"}.items():
 os[i]["Kwic"]=k
os[1]["ContextMasters"]=[{"MasterName":"Mayu Baoche","Roles":["addressee","respondent","action-performer"]},{"MasterName":"Linji Yixuan","Roles":["utterer","record-owner","challenger"]}]
os[1]["AttributionNote"]="Complete Linji–Mayu exchange: Linji repeats Mayu Baoche's thousand-hands question back to him and commands 'Speak, quickly!' Mayu then drags Linji from the seat."
os[4]["ContextMasters"]=[{"MasterName":"Linji Yixuan","Roles":["utterer","questioner","challenger"]},{"MasterName":"Xiangtian Heshang","Roles":["respondent","case-figure"]}]
os[4]["AttributionNote"]="Complete Xiangtian visit: Linji Yixuan asks Xiangtian to speak quickly without being ordinary or holy; Xiangtian answers and Linji shouts."
os[6]["ContextMasters"]=[{"MasterName":"Mayu Baoche","Roles":["addressee","respondent","action-performer"]},{"MasterName":"Linji Yixuan","Roles":["utterer","record-owner","challenger"]}]
os[6]["AttributionNote"]="Five Houses selection, complete parallel Linji–Mayu exchange: Linji repeats Mayu's question and commands him to speak quickly before Mayu pulls him from the seat."
s["Note"]="Speaker direction changes—master to monk or visitor to master—but the imperative denotes the same demanded act, so no sense split is warranted. Frozen-corpus concordance: 1592 exact hits in 249 storage files representing 244 independent works."
s["RelatedMasters"]=["Linji Yixuan","Mayu Baoche","Guishan Lingyou","Baizhang Huaihai","Dahui Zonggao"]
s["Explanation"]="Speak, quickly! is the timed command to produce an answer immediately in a public exchange. Mimi Cliff holds his fork at a visitor's neck while demanding it; Linji turns Mayu's question back on him and orders him to answer; Baizhang bars reliance on throat and lips before issuing the command; Dahui forbids both speaking and silence around the bamboo tally and then demands it. The direction can reverse: Linji asks Xiangtian to answer quickly. The surrounding pull from the seat, fork, bamboo tally, shout, or blow makes the phrase an enforced interview deadline, not merely a request to talk faster."
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in os))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired READ-AND-FIX entry 56")

# 57 上堂 — thirteen complete passages hand-read; event performer is not the heading's utterer.
p=BUILD/"fresh-build/entries/t_4f7bd98ad40f/entry.v2.json"
d=json.loads(p.read_text(encoding="utf-8"));s=d["Senses"][0];os=s["Occurrences"]
performers={0:"Yunmen Wenyan",1:"Hongzhi Zhengjue",4:"Yulin Tongxiu",6:"Hongzhi Zhengjue",7:"Foyan Qingyuan",8:"Feiyin Tongrong",10:"Yongjue Yuanxian",12:"Linji Yixuan"}
for i,name in performers.items():
 o=os[i];o["MasterName"]=None;o["ContextMasters"]=[{"MasterName":name,"Roles":["action-performer","hall-speaker","record-owner"]}]
 if i in (0,1,4,6,7,8):
  o["ActorAttribution"]={"Status":"impersonal","Kind":"editorial heading","ActorLabel":"the textual hall-address heading","ActorRole":"none","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":f"The heading or request labels {name}'s formal hall-address event; the master's quoted speech begins only after the following speech marker.","ReviewedBy":"Codex cohorts 1-3 v6 full-case READ-AND-FIX","ReviewedUtc":"2026-07-16T09:00:00Z"}
 elif i==10:
  o["ActorAttribution"]={"Status":"narrated","Kind":"biographical narration","ActorLabel":"the biographer","ActorRole":"compiler","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"The biographer reports that Yongjue Yuanxian had prohibited hall addresses and later ascended the seat; Yongjue performs the event but does not utter the headword.","ReviewedBy":"Codex cohorts 1-3 v6 full-case READ-AND-FIX","ReviewedUtc":"2026-07-16T09:00:00Z"}
 else:
  o["ActorAttribution"]={"Status":"narrated","Kind":"event narration","ActorLabel":"the record narrator","ActorRole":"narrator","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":f"The narrator states that {name} ascends the hall; the master's quoted words begin after the speech marker.","ReviewedBy":"Codex cohorts 1-3 v6 full-case READ-AND-FIX","ReviewedUtc":"2026-07-16T09:00:00Z"}
  
 o["AttributionNote"]=f"Complete source unit: the textual heading or narrator owns the word 上堂, while {name} is the named master who performs the formal hall address."
os[9]["ContextMasters"]=[{"MasterName":"Yinyuan Longqi","Roles":["action-performer","whisk-holder","head-monk"]},{"MasterName":"Feiyin Tongrong","Roles":["respondent","record-owner","case-teacher"]}]
os[9]["AttributionNote"]="Complete Feiyin record: the compiler reports former front-hall head monk Yinyuan Longqi holding the whisk and ascending the hall; Feiyin Tongrong supplies the marked replies. Yinyuan performs the event but does not utter its title."
os[10]["ContextMasters"]=[{"MasterName":"Yongjue Yuanxian","Roles":["action-performer","hall-speaker","person-described"]}]
os[12]["ContextMasters"]=[{"MasterName":"Linji Yixuan","Roles":["action-performer","hall-speaker","record-owner"]}]
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in os))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired READ-AND-FIX entry 57")

# 58 懸崖撒手 — seven complete cases hand-read.
p=BUILD/"fresh-build/entries/t_586bc9a3f0a8/entry.v2.json"
d=json.loads(p.read_text(encoding="utf-8"));s=d["Senses"][0];os=s["Occurrences"]
for i,respondent in [(0,"Xingyang Xiyin"),(2,"Luopu Yuan'an")]:
 o=os[i];o["MasterName"]=None;o["ContextMasters"]=[{"MasterName":respondent,"Roles":["respondent","record-owner","case-teacher"]}]
 o["ActorAttribution"]={"Status":"reviewed-unnamed","Kind":"monastic questioner","ActorLabel":"the unnamed monk","ActorRole":"questioner","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"The explicit 僧問/問 frame assigns the headword-bearing question to an unnamed monk; the named master's answer is a separate turn.","ReviewedBy":"Codex cohorts 1-3 v6 full-case READ-AND-FIX","ReviewedUtc":"2026-07-16T09:00:00Z"}
 o["AttributionNote"]=f"Complete public exchange: an unnamed monk utters the cliff-release phrase in his question; {respondent} supplies the marked answer."
os[6]["Kwic"]="悟謂曰：不見道：懸崖撒手，自肯承當；絕後再蘇，欺君不得"
o=os[6];o["MasterName"]="Yuanwu Keqin";o["ContextMasters"]=[{"MasterName":"Yuanwu Keqin","Roles":["quoted-utterer","teacher","case-source"]},{"MasterName":"Dahui Zonggao","Roles":["addressee","person-described","student"]}];o.pop("ActorAttribution",None);o["AttributionNote"]="Complete Dahui biography: Yuanwu Keqin directly tells Dahui that he remains dead and quotes the paired cliff-release and revival formula. Dahui is the addressed student, not the utterer."
s["Explanation"]="Release your grip over a sheer drop is the corpus's danger-image for abandoning the last support where loss of life is explicitly at stake. An unnamed monk asks Xingyang Xiyin for this named line; Yong'an Jingwu makes it his answer about leaving home; another monk asks Luopu how to avoid losing his life after doing it. Yuanwu Keqin repeatedly pairs the release with accepting for oneself and reviving after extinction, and in a letter compares it with giving up one's life before a dead person revives. Wanfeng Shiwei uses it in his death verse. These witnesses establish danger, relinquishment, and the recurring death-and-revival sequence without turning that sequence into an unstated doctrine."
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in os))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired READ-AND-FIX entry 58")

# 59 答話 — seven complete cases hand-read.
p=BUILD/"fresh-build/entries/t_589f52acc0b0/entry.v2.json"
d=json.loads(p.read_text(encoding="utf-8"));s=d["Senses"][0];os=s["Occurrences"]
o=os[0];o["MasterName"]=None;o["ContextMasters"]=[{"MasterName":"Caoshan Baoji Xiong","Roles":["respondent","record-owner","case-teacher"]}];o["ActorAttribution"]={"Status":"reviewed-unnamed","Kind":"monastic interlocutor","ActorLabel":"the unnamed monk","ActorRole":"respondent","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"曰：謝師答話 assigns the headword-bearing thanks to the unnamed monk after Caoshan's answer; the inherited Wanshan name came from an adjacent section.","ReviewedBy":"Codex cohorts 1-3 v6 full-case READ-AND-FIX","ReviewedUtc":"2026-07-16T09:00:00Z"};o["AttributionNote"]="Complete Caoshan Baoji Xiong exchange: an unnamed monk thanks Caoshan for the reply; Caoshan answers that a clear-eyed person is hard to deceive."
o=os[2];o["MasterName"]="Tiantai Deshao";o["ContextMasters"]=[{"MasterName":"Tiantai Deshao","Roles":["utterer","record-owner","instructor"]}];o.pop("ActorAttribution",None);o["AttributionNote"]="Complete Tiantai Deshao hall instruction: Deshao says that even reply-discrimination flowing like a suspended river produces inverted understanding if prized in itself. The inherited Nanyang Huizhong attribution was an adjacent-section leak."
for i,respondent,label in [(4,"Mengxi Heshang","the unnamed monk"),(5,"Shuangquan Qiong","the unnamed questioner")]:
 o=os[i];o["MasterName"]=None;o["ContextMasters"]=[{"MasterName":respondent,"Roles":["respondent","record-owner","case-teacher"]}]
 o["ActorAttribution"]={"Status":"reviewed-unnamed","Kind":"monastic questioner","ActorLabel":label,"ActorRole":"questioner","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"請師答話 occurs in the unnamed monastic's marked request; the local master's response is a separate turn.","ReviewedBy":"Codex cohorts 1-3 v6 full-case READ-AND-FIX","ReviewedUtc":"2026-07-16T09:00:00Z"};o["AttributionNote"]=f"Complete exchange: an unnamed monastic asks {respondent} to give a reply; {respondent} supplies the following marked answer."
s["Explanation"]="A reply is an answering turn treated as an object of request, thanks, criticism, or discrimination inside an exchange. An unnamed monk thanks Caoshan Baoji Xiong for one; other monks ask Mengxi and Shuangquan to provide one. Luohan Ji cites critics who call a particular reply an added fetter, Tiantai Deshao warns that fluent discrimination among replies can still produce inverted understanding, and Dongchan Qi asks whether another reply is still required. The term names the response-turn and the corpus's scrutiny of it, not proof that the response is adequate."
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in os))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired READ-AND-FIX entry 59")

# 60 作禮 — eight complete cases hand-read; all are narrator-owned action reports.
p=BUILD/"fresh-build/entries/t_5cade7a4f4ba/entry.v2.json"
d=json.loads(p.read_text(encoding="utf-8"));s=d["Senses"][0];os=s["Occurrences"]
os[4]["Kwic"]="須臾，罔明大士從地涌出，作禮世尊"
os[6]["Kwic"]="罔明大士從地涌出，作禮世尊；世尊敕罔明出女人定"
contexts=[
 [{"MasterName":"Wangming Bodhisattva","Roles":["named-unrostered","action-performer","case-figure"]},{"MasterName":"Shakyamuni Buddha","Roles":["recipient","case-teacher"]}],
 [{"MasterName":"Cishou Huaishen","Roles":["respondent","record-owner","case-teacher"]}],
 [{"MasterName":"Mahakasyapa","Roles":["action-performer","case-figure"]},{"MasterName":"Shakyamuni Buddha","Roles":["recipient","case-teacher"]}],
 [{"MasterName":"Wangming Bodhisattva","Roles":["named-unrostered","action-performer","case-figure"]},{"MasterName":"Shakyamuni Buddha","Roles":["recipient","case-teacher"]}],
 [{"MasterName":"Wangming Bodhisattva","Roles":["named-unrostered","action-performer","case-figure"]},{"MasterName":"Shakyamuni Buddha","Roles":["recipient","case-teacher"]}],
 [{"MasterName":"Shakyamuni Buddha","Roles":["action-performer","case-teacher"]}],
 [{"MasterName":"Wangming Bodhisattva","Roles":["named-unrostered","action-performer","case-figure"]},{"MasterName":"Shakyamuni Buddha","Roles":["recipient","case-teacher"]},{"MasterName":"Yulin Tongxiu","Roles":["later-raiser","record-owner","commentator"]}],
 [{"MasterName":"Shakyamuni Buddha","Roles":["recipient","case-teacher"]}],
]
evidence=[
 "The case narrator says Wangming emerges, pays respect to Shakyamuni, and is then commanded to bring the woman out of absorption.",
 "僧作禮 is a stage direction reporting the unnamed monk's bow after Cishou's answer; neither participant utters the headword.",
 "The nirvana-case narrator reports Mahakasyapa paying respect before requesting cremation.",
 "The parallel case narrator reports Wangming paying respect to Shakyamuni before the command.",
 "The parallel case narrator reports Wangming paying respect to Shakyamuni before the command.",
 "The case narrator reports Shakyamuni seeing an ancient buddha's stupa and paying respect to it.",
 "Yulin raises the old case; inside the quotation its narrator reports Wangming paying respect to Shakyamuni. Yulin comments afterward.",
 "The case narrator reports the unnamed outsider paying respect and leaving after praising Shakyamuni's silence.",
]
for i,o in enumerate(os):
 o["MasterName"]=None;o["ContextMasters"]=contexts[i]
 o["ActorAttribution"]={"Status":"narrated","Kind":"case narration","ActorLabel":"the case narrator","ActorRole":"narrator","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":evidence[i],"ReviewedBy":"Codex cohorts 1-3 v6 full-case READ-AND-FIX","ReviewedUtc":"2026-07-16T09:00:00Z"}
 o["AttributionNote"]=evidence[i]
s["Explanation"]="To pay respect is to perform a formal bodily salutation before a person, image, or memorial. The records narrate Wangming bowing to Shakyamuni before receiving a command, Mahakasyapa bowing before requesting the Buddha's cremation, Shakyamuni himself bowing to an ancient buddha's stupa, an unnamed monk bowing after a reply, and an outsider bowing before leaving an exchange. Chan preserves who bows, to whom, and at what turn; the action is public and timed, not an inferred inward attitude."
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in os))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired READ-AND-FIX entry 60")
