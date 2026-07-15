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

# 61 情知 — seven complete cases hand-read.
p=BUILD/"fresh-build/entries/t_5f6e8c98ffe7/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));s=d["Senses"][0];os=s["Occurrences"]
o=os[2];o["MasterName"]=None;o["ContextMasters"]=[{"MasterName":"Baizhang Le","Roles":["named-unrostered","utterer","commentator"]}];o["ActorAttribution"]={"Status":"named-unrostered","Kind":"master commentator","ActorLabel":"Baizhang Le (百丈泐)","ActorRole":"commentator","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"百丈泐云 introduces the complete comment through 情知你向者裏錯會; the inherited Jingfu name was not the speaker.","ReviewedBy":"Codex cohorts 1-3 v6 full-case READ-AND-FIX","ReviewedUtc":"2026-07-16T09:00:00Z"};o["AttributionNote"]="Collected Ancient Cases explicitly attributes the comment to the named-but-unrostered Baizhang Le: he anticipates that the reader misconstrues the case at the Buddha's silence."
o=os[3];o["MasterName"]=None;o["ContextMasters"]=[{"MasterName":"Langting Ting","Roles":["respondent","record-owner","case-teacher"]}];o["ActorAttribution"]={"Status":"reviewed-unnamed","Kind":"monastic interlocutor","ActorLabel":"the unnamed monk","ActorRole":"respondent","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"僧云：情知 explicitly assigns the compact reply to the unnamed monk; Langting answers in the next marked turn.","ReviewedBy":"Codex cohorts 1-3 v6 full-case READ-AND-FIX","ReviewedUtc":"2026-07-16T09:00:00Z"};o["AttributionNote"]="Complete Langting exchange: an unnamed monk replies 'I knew it'; Langting Ting answers that a fierce tiger does not eat prostrate flesh."
os[6]["Kwic"]="僧喝，師打云：情知汝這一喝"
s["Explanation"]="As I knew, sure enough, or I knew it marks prior expectation or recognition of the other turn. Ziman says he knew the monk was at a loss; Pingtian Puan says he knew Linji had seen an adept; Baizhang Le anticipates the reader's misconstrual; Shiyu Mingfang and Baichi Yuan mark the response they expected; Zhongfeng Mingben predicts Bodhidharma's 'I don't know.' An unnamed monk also uses the compact reply himself. These grammatical placements describe recognition of an ensuing or just-given turn, not a separate faculty of feeling-knowledge."
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in os));p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8");print("repaired READ-AND-FIX entry 61")

# 62 將錯就錯 — seven complete cases hand-read; all named utterers hold.
p=BUILD/"fresh-build/entries/t_64b296f04e9b/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));s=d["Senses"][0];os=s["Occurrences"]
os[4]["Kwic"]="一僧禮拜了退，師云：「將錯就錯。」"
os[6]["Kwic"]="趙州古佛因僧問，不將錯就錯。"
s["Explanation"]="To follow one error with another is to proceed from an already mistaken position by means of a further mistake. Gunanmen says Shakyamuni, pressed until no route of retreat remained, could only do this; Zhongfeng Mingben warns his assembly not to do it while asking who can speak for Linji; Sanshan Denglai issues the phrase as a verdict when a monk bows and withdraws. Tiebi Huiji asks what the error is and answers with eating tea or rice when each comes, while Jie Weizhou rejects preserving faulty writing under this excuse. Guting Shanjian can even prohibit the phrase while discussing Zhaozhou. Warning, case comment, and interview verdict differ in stance toward the same act, not in the phrase's sense."
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in os));p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8");print("repaired READ-AND-FIX entry 62")

# 63 正令 — seven complete cases hand-read; questions belong to their unnamed monks.
p=BUILD/"fresh-build/entries/t_68835cda6c3f/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));s=d["Senses"][0];os=s["Occurrences"]
cuts=["問：如何是正令行時句？師云：千里特來呈舊面。","師云：正令已行。","承天確禪師示眾：正令提綱，猶是揑窠造偽。","若遇雲門行正令，管教棒下辨龍虵。","師云：「主寰中正令，握閫外威權」","菩提珍云：世尊握閫外威權，全提正令。","問：師登寶座，壁立千仞。正令當行，十方坐斷。未審將何為人？"]
for o,k in zip(os,cuts): o["Kwic"]=k
for i,respondent in [(0,"Fenyang Shanzhao"),(6,"Sanzu Chonghui")]:
 o=os[i];o["MasterName"]=None;o["ContextMasters"]=[{"MasterName":respondent,"Roles":["respondent","section-subject","case-teacher"]}]
 o["ActorAttribution"]={"Status":"reviewed-unnamed","Kind":"monastic questioner","ActorLabel":"the unnamed monk","ActorRole":"questioner","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"The explicit 問 frame assigns the headword-bearing question to an unnamed monk; the named master's reply begins after 師云/師曰.","ReviewedBy":"Codex cohorts 1-3 v6 full-case READ-AND-FIX","ReviewedUtc":"2026-07-16T09:00:00Z"}
 o["AttributionNote"]=f"Complete public exchange: an unnamed monk utters the headword in his question; {respondent} gives the separately marked reply."
s["Explanation"]="The authoritative command is an order presented as being exercised, fully raised, or already in force, often through governmental and military language on the public teaching seat. An unnamed monk asks Fenyang Shanzhao for the line used when it is carried out; Longtan Yuan declares that it has already been carried out; Rui'an Sengyin says Yunmen's command distinguishes dragons from snakes under the staff. Baichi Yuan describes commanding within the realm while holding authority beyond the gate, and Bodhi Zhen says Shakyamuni fully raised it. Chengtian Que also calls even raising its guiding principle the manufacture of a false nest. The corpus can enact or criticize the command without changing what the term names."
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in os));p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8");print("repaired READ-AND-FIX entry 63")

# 64 莊主 — six complete source units hand-read; office labels and narrative mentions are not utterances.
p=BUILD/"fresh-build/entries/t_708bab84a958/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));s=d["Senses"][0];os=s["Occurrences"]
cuts=["謝莊主上堂云：一不做，二不休。","園頭莊主廨院主","南泉因至莊所，莊主預設迎奉。","師曰：汝去問莊主。","既稱名，則知為舒州太平才莊主矣。","南泉因到莊所，莊主預𬾨迎奉。"]
for o,k in zip(os,cuts): o["Kwic"]=k;o["MasterName"]=None
os[0]["ActorAttribution"]={"Status":"impersonal","Kind":"editorial heading","ActorLabel":"the hall-address heading","ActorRole":"none","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"謝莊主上堂 is a heading for a hall address thanking an estate steward; 莊主 labels the recipient's office rather than an utterer.","ReviewedBy":"Codex cohorts 1-3 v6 full-case READ-AND-FIX","ReviewedUtc":"2026-07-16T09:00:00Z"};os[0]["AttributionNote"]="The editorial heading announces a hall address thanking an unnamed estate steward; the headword is an office label, not spoken dialogue."
os[1]["ActorAttribution"]={"Status":"impersonal","Kind":"table-of-contents office label","ActorLabel":"the monastic-code contents list","ActorRole":"none","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"莊主 appears in an enumerated list of monastery offices, between 園頭 and 廨院主; there is no human utterer.","ReviewedBy":"Codex cohorts 1-3 v6 full-case READ-AND-FIX","ReviewedUtc":"2026-07-16T09:00:00Z"};os[1]["AttributionNote"]="Monastic-code contents list: 莊主 is one institutional office among other officers, with no speaking actor."
for i in (2,5):
 os[i]["ContextMasters"]=[{"MasterName":"Nanquan Puyuan","Roles":["case-teacher","person-described"]}]
 os[i]["ActorAttribution"]={"Status":"narrated","Kind":"case narration","ActorLabel":"the case narrator","ActorRole":"narrator","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"The narrator identifies an unnamed estate steward who prepares for Nanquan's visit; neither participant utters the office title.","ReviewedBy":"Codex cohorts 1-3 v6 full-case READ-AND-FIX","ReviewedUtc":"2026-07-16T09:00:00Z"};os[i]["AttributionNote"]="Nanquan case narration: an unnamed estate steward prepares to receive Nanquan Puyuan; the narrator supplies the office title."
os[3]["ActorAttribution"]={"Status":"reviewed-unnamed","Kind":"quoted ancient master","ActorLabel":"the unnamed ancient master","ActorRole":"utterer","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"師曰 directly marks a master telling someone to ask the estate steward, but the source unit identifies him only generically as an ancient worthy.","ReviewedBy":"Codex cohorts 1-3 v6 full-case READ-AND-FIX","ReviewedUtc":"2026-07-16T09:00:00Z"};os[3]["AttributionNote"]="An ancient master directly says, 'Go ask the estate steward'; the complete source unit does not supply his personal name."
os[4]["ContextMasters"]=[{"MasterName":"Longya Zhicai","Roles":["person-described","office-holder"]}];os[4]["ActorAttribution"]={"Status":"narrated","Kind":"biographical narration","ActorLabel":"the biographer","ActorRole":"compiler","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"The biographer identifies Longya Zhicai by his former office as the Taiping estate steward; Longya does not utter the title.","ReviewedBy":"Codex cohorts 1-3 v6 full-case READ-AND-FIX","ReviewedUtc":"2026-07-16T09:00:00Z"};os[4]["AttributionNote"]="Biographical identification: Longya Zhicai had been known as the Taiping estate steward; the compiler supplies the office label."
s["Explanation"]="The estate steward is the monastery officer responsible for an outlying estate or farm and its material business. A monastic code lists the office among the garden superintendent and other administrators; a biography identifies Longya Zhicai by his former service as the Taiping estate steward. In the Nanquan case, the steward prepares to receive Nanquan Puyuan at the estate and answers for that preparation. The office can therefore identify both an institutional post and its holder, but these are the same role rather than separate senses."
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in os));p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8");print("repaired READ-AND-FIX entry 64")

# 65 木人舞 — five complete source units hand-read; capping-verse authors remain unnamed where the source does.
p=BUILD/"fresh-build/entries/t_7114caf4b0ec/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));s=d["Senses"][0];os=s["Occurrences"]
cuts=["師云：「木人舞袖歸來晚。」","頌曰：滿鉢盛來一物無，豈同香積變珍蘇？日月並輪長不照，木人舞袖向紅爐。","頌曰：滿鉢盛來一物無，豈同香積變珍蘇；日月並輪長不照，木人舞袖向紅爐。","石女奏無生之曲，木人舞虞舜之韶。","師乃云：「琉璃殿上玉女拋梭，明月堂前木人舞袖。」"]
for o,k in zip(os,cuts): o["Kwic"]=k
o=os[1];o["MasterName"]=None;o["ContextMasters"]=[{"MasterName":"Linquan Conglun","Roles":["later-commentator","record-owner"]}];o["ActorAttribution"]={"Status":"reviewed-unnamed","Kind":"capping-verse author","ActorLabel":"the unnamed verse author","ActorRole":"quoted-author","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"頌曰 introduces the headword inside a cited verse; Linquan Conglun's own comment begins only afterward at 師云, so he is not the utterer of this occurrence.","ReviewedBy":"Codex cohorts 1-3 v6 full-case READ-AND-FIX","ReviewedUtc":"2026-07-16T09:00:00Z"};o["AttributionNote"]="The headword belongs to a capping verse introduced by 頌曰; Linquan Conglun comments only after the verse, and the complete unit does not name its author."
o=os[2];o["MasterName"]=None;o["ActorAttribution"]={"Status":"reviewed-unnamed","Kind":"capping-verse author","ActorLabel":"the unnamed verse author","ActorRole":"quoted-author","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"頌曰 explicitly introduces the headword-bearing verse, but the complete source unit does not name the verse's author.","ReviewedBy":"Codex cohorts 1-3 v6 full-case READ-AND-FIX","ReviewedUtc":"2026-07-16T09:00:00Z"};o["AttributionNote"]="The occurrence is in an explicitly marked capping verse whose author is not named in the complete source unit."
s["Explanation"]="A wooden figure dances is a deliberately impossible animation of an inert carved person. Ruibai Mingxue makes it return late waving its sleeves; two capping verses make it dance toward a red furnace; Yezhu Fusheng pairs its dance to Shun's music with a stone woman playing an unborn melody; Yunsou Zhu places it waving its sleeves before the bright-moon hall. The recurrent wooden body, dance, sleeves, and impossible companions establish one image-family, not separate senses for each choreography."
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in os));p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8");print("repaired READ-AND-FIX entry 65")
