import json
from pathlib import Path

BUILD = Path(__file__).resolve().parents[2]
p = BUILD / "fresh-build/entries/t_a805d0c76bbd/entry.v2.json"
d = json.loads(p.read_text(encoding="utf-8"))
os = [o for s in d["Senses"] for o in s["Occurrences"]]
reviewed = "2026-07-16T02:10:00Z"

# The original installation-address KWIC contained two ritual repetitions. Retain the
# second complete action marker so the stored evidence has exactly one headword token.
os[4]["Kwic"] = "復拈香云此一瓣香佛祖齅之腦裂天人覷之眼枯󲇊向爐中"

rows = [
    ("the compiler of Konggu Daocheng's record", "拈香云 is the narrative action marker before Konggu's direct dedication; the quoted words begin after 云.", [{"MasterName":"Konggu Daocheng","Roles":["person-described","record-owner"]}], "Complete installation address in Konggu Daocheng's Recorded Sayings (空谷道澄禪師語錄): the compiler narrates Konggu taking up incense, then quotes his successive dedications."),
    ("the compiler narrating Guo Xiangzheng's ceremony", "公趨前拈香曰 narrates the lay official Guo Xiangzheng stepping forward and taking incense; his direct words begin after 曰.", [], "Complete biographical sequence in the Complete Lamp Collection (五燈全書): the compiler narrates the lay official Guo Xiangzheng stepping forward and taking incense before his direct dedication at South Chan monastery."),
    ("the compiler of Shimen Yuncong's record", "師開堂拈香云 narrates Shimen's opening and incense action; the direct lineage statement begins after 云.", [{"MasterName":"Shimen Yuncong","Roles":["person-described","record-owner"]}], "Complete opening ceremony in Shimen Yuncong's Recorded Sayings (石門禪師語錄): the compiler narrates Shimen taking up incense before quoting his statement about transmission after the Sixth Patriarch."),
    ("the editor of disciple Yu's written question", "弟子裕拈香啟問 is the formal epistolary preamble naming disciple Yu's action before his written question; it is not a master's spoken token.", [], "Complete letter-question appended to the L154 record: the editorial preamble says disciple Yu takes up incense and respectfully asks his Bathing the Buddha questions; no master utters the headword."),
    ("the compiler of Yulin Tongxiu's record", "遂陞拈香云 narrates Yulin ascending and taking incense; his quoted dedication begins only after 云.", [{"MasterName":"Yulin Tongxiu","Roles":["person-described","record-owner"]}], "Complete installation address in Yulin Tongxiu's Recorded Sayings (普濟玉琳國師語錄): the compiler narrates Yulin ascending and taking incense before quoting his dedications to benefactors and Tianyin Yuanxiu."),
    ("the compiler of Feiyin Tongrong's record", "伽藍堂，拈香云 narrates Feiyin's ritual action at the monastery-protector hall; his direct question begins after 云.", [{"MasterName":"Feiyin Tongrong","Roles":["person-described","record-owner"]}], "Complete entry ceremony in Feiyin Tongrong's Recorded Sayings (費隱禪師語錄): the compiler narrates Feiyin taking up incense in the monastery-protector hall before quoting his question and dedication."),
    ("the compiler of Meixi Fudu's record", "遂拈香，云 narrates Meixi Fudu taking up incense after surveying the hall; his compact verdict begins after 云.", [{"MasterName":"Meixi Fudu","Roles":["person-described","record-owner"]}], "Complete hall-entry action in Meixi Fudu's Recorded Sayings (東山梅溪禪師語錄): the compiler narrates Meixi taking up incense, after which Meixi says, ‘Two wins, one contest.’"),
    ("the compiler of Pin Jixiang's record", "師拈香入爐中，曰 narrates Pin Jixiang taking incense and placing it in the burner; his direct words begin after 曰.", [{"MasterName":"Pin Jixiang","Roles":["person-described","record-owner"]}], "Complete memorial address in Pin Jixiang's Recorded Sayings (頻吉祥禪師語錄): the compiler narrates Pin taking incense and placing it in the burner before quoting his arrow challenge for Baiyan Wei."),
]
for o,(label,evidence,contexts,note) in zip(os,rows):
    o["MasterName"] = None
    o["ContextMasters"] = contexts
    o["ActorAttribution"] = {"Status":"narrated","Kind":"compiler narrative","ActorLabel":label,"ActorRole":"compiler","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":evidence,"ReviewedBy":"Codex cohorts 1-3 v6 full-case hand read","ReviewedUtc":reviewed}
    o["AttributionNote"] = note
for s in d["Senses"]:
    s["SourceTexts"] = list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired hand-read entry 16")

# 17 方丈 — all nine complete structural cases hand-read.
p = BUILD / "fresh-build/entries/t_becc0a1ea8cb/entry.v2.json"
d = json.loads(p.read_text(encoding="utf-8"))
s1, s2 = d["Senses"]

# Keep exactly one headword token in the stored KWIC; the full contrast remains in the
# explanation and full-case adjudication ledger.
s1["Occurrences"][3]["Kwic"] = "老僧喚作方丈室。"

# S1/O2 is compiler narration of Mazu's movement, not a token uttered by Mazu.
o = s1["Occurrences"][1]
o["MasterName"] = None
o["ContextMasters"] = [
    {"MasterName":"Mazu Daoyi","Roles":["person-described","record-owner"]},
    {"MasterName":"Layman Pang","Roles":["case-figure"]},
]
o["ActorAttribution"] = {"Status":"narrated","Kind":"compiler narrative","ActorLabel":"compiler of the Five Lamps Meeting the Source","ActorRole":"compiler","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"師歸方丈 is an unquoted narrative clause: Mazu is the subject of 歸, while the compiler supplies the words.","ReviewedBy":"Codex cohorts 1-3 v6 full-case hand read","ReviewedUtc":reviewed}
o["AttributionNote"] = "Five Lamps Meeting the Source (五燈會元), complete Mazu Daoyi section: the compiler narrates Mazu returning to the abbot's quarters after Layman Pang bows; Pang then follows him."

# S1/O3 likewise narrates the unnamed head monk's action.
o = s1["Occurrences"][2]
o["MasterName"] = None
o["ContextMasters"] = [{"MasterName":"Linji Yixuan","Roles":["person-described","record-owner"]}]
o["ActorAttribution"] = {"Status":"narrated","Kind":"compiler narrative","ActorLabel":"compiler of Linji Yixuan's supplementary record","ActorRole":"compiler","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"首座隨後上方丈 is unquoted narration. The unnamed head monk performs the movement; the compiler utters the headword.","ReviewedBy":"Codex cohorts 1-3 v6 full-case hand read","ReviewedUtc":reviewed}
o["AttributionNote"] = "Old Recorded Sayings of Venerable Masters (古尊宿語錄), complete encounter in Linji Yixuan's supplementary record: the compiler narrates the unnamed head monk following Linji up to the abbot's quarters and bowing."

# S1/O6 occurs inside Sihui Miaozhan's address: he raises the Yaoshan case.
o = s1["Occurrences"][5]
o["MasterName"] = None
o["ContextMasters"] = [{"MasterName":"Yaoshan Weiyan","Roles":["person-discussed","case-figure"]}]
o["ActorAttribution"] = {"Status":"named-unrostered","Kind":"master","ActorLabel":"Xuefeng Sihui Miaozhan","ActorRole":"utterer","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"Under the heading 福州雪峰思慧妙湛禪師, the sentence is part of Sihui Miaozhan's continuing address; he raises the earlier Yaoshan event and then comments on present assemblies.","ReviewedBy":"Codex cohorts 1-3 v6 full-case hand read","ReviewedUtc":reviewed}
o["AttributionNote"] = "Complete Book of the Five Lamps (五燈全書), full address under Fuzhou Xuefeng Sihui Miaozhan: Sihui Miaozhan raises the case that Yaoshan returned to the abbot's quarters as soon as the assembly gathered, then contrasts it with contemporary daily hall addresses. Sihui is named in the source but is not yet linkable through the current roster."

# S2/O1 follows the explicit marker 璨隱山亦云; it is Canyin Shan's prose, not an anonymous compiler's.
o = s2["Occurrences"][0]
o["MasterName"] = None
o["ContextMasters"] = []
o["ActorAttribution"] = {"Status":"named-unrostered","Kind":"master","ActorLabel":"Canyin Shan (璨隱山)","ActorRole":"utterer","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"璨隱山亦云 explicitly introduces the ensuing admonition; 今之踞方丈者 remains within that quoted prose.","ReviewedBy":"Codex cohorts 1-3 v6 full-case hand read","ReviewedUtc":reviewed}
o["AttributionNote"] = "Chan Grove Admonitions (禪林寶訓), full preface paragraph: the explicit marker 'Canyin Shan also said' (璨隱山亦云) introduces the admonition in which Canyin criticizes those who now occupy the abbot's seat. He is named by the text but is not yet linkable through the current roster."

for s in d["Senses"]:
    s["SourceTexts"] = list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired hand-read entry 17")

# 18 竪拂 — all nine complete structural cases hand-read.
p = BUILD / "fresh-build/entries/t_df3e128ab4c1/entry.v2.json"
d = json.loads(p.read_text(encoding="utf-8"))
s = d["Senses"][0]
os = s["Occurrences"]
os[1]["Kwic"] = "竪拂子云。只這是。"

stage_rows = {
    0: ("compiler of Nanta Guangyong's lamp-record section", [{"MasterName":"Nanta Guangyong","Roles":["person-described","record-owner","respondent"]}], "師竪拂子示之 is a recorder-supplied action clause between the monk's question and response.", "Jingde Record of the Transmission of the Lamp (景德傳燈錄), complete Nanta Guangyong encounter: the compiler narrates Nanta raising the whisk to show it after the monk asks about Manjusri's teacher."),
    1: ("compiler of Yuanwu Keqin's recorded sayings", [{"MasterName":"Yuanwu Keqin","Roles":["person-described","record-owner"]}], "竪拂子云 is the action-and-speech marker; the headword is in the recorder's stage direction, while Yuanwu's quoted words begin with 只這是.", "Recorded Sayings of Yuanwu Foguo (圓悟佛果禪師語錄), complete Vesak informal address: the compiler records Yuanwu raising the whisk, after which Yuanwu says, ‘Just this is it.’"),
    2: ("compiler of Luopu Yuanan's lamp-record section", [{"MasterName":"Luopu Yuanan","Roles":["person-described","record-owner","respondent"]}], "師竪拂子 is unquoted action narration. The surrounding section heading is 澧州洛浦山元安禪師, not Zhaozhou.", "Five Lamps Meeting the Source (五燈會元), complete Luopu Yuanan section: the compiler narrates Luopu raising the whisk in response to the monk's question about receiving one who sets the realm in order. The prior attribution to Zhaozhou was a section-identification error."),
    5: ("compiler of Baiyu Jingsi's recorded sayings", [{"MasterName":"Baiyu Jingsi","Roles":["person-described","record-owner"]}], "豎拂子，云 is the recorder's action-and-speech marker; Baiyu's quoted question begins after 云.", "Recorded Sayings of Chan Master Baiyu (百愚禪師語錄), complete hall address: the compiler narrates Baiyu raising the whisk; Baiyu then asks whether the assembly sees and gives a verse."),
    6: ("compiler of Yinyuan Longqi's recorded sayings", [{"MasterName":"Yinyuan Longqi","Roles":["person-described","record-owner","respondent"]}], "師豎拂子云 is a stage direction. Yinyuan performs the action, but his quoted token begins with 會麼.", "Recorded Sayings of Yinyuan (隱元禪師語錄), complete opening encounter: the compiler narrates Yinyuan raising the whisk in response to the new-command question; Yinyuan then asks whether the monk understands."),
    7: ("compiler of Xueyan Zuqin's recorded sayings", [{"MasterName":"Xueyan Zuqin","Roles":["person-described","record-owner"]}], "竪拂子云 is a recorder-supplied stage direction; Xueyan's quoted verdict follows 云.", "Recorded Sayings of Xueyan Zuqin (雪巖祖欽禪師語錄), complete hall address: the compiler narrates Xueyan raising the whisk before Xueyan says that eyes are like blindness while affective dust remains unfreed."),
    8: ("compiler of Xueyan Zuqin's recorded sayings", [{"MasterName":"Xueyan Zuqin","Roles":["person-described","record-owner"]}], "竪拂子，云 is a recorder-supplied stage direction; Xueyan's moon verse follows 云.", "Recorded Sayings of Xueyan Zuqin (雪巖祖欽禪師語錄), complete mid-autumn address: the compiler narrates Xueyan raising the whisk before Xueyan gives the line about the round moon emerging from the clouds."),
}
for i,(label,contexts,evidence,note) in stage_rows.items():
    o=os[i]; o["MasterName"]=None; o["ContextMasters"]=contexts
    o["ActorAttribution"]={"Status":"narrated","Kind":"compiler narrative","ActorLabel":label,"ActorRole":"compiler","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":evidence,"ReviewedBy":"Codex cohorts 1-3 v6 full-case hand read","ReviewedUtc":reviewed}
    o["AttributionNote"]=note
for s0 in d["Senses"]:
    s0["SourceTexts"] = list(dict.fromkeys(o["RelPath"] for o in s0["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired hand-read entry 18")

# 19 家珍 — all seven complete structural cases hand-read.
p = BUILD / "fresh-build/entries/t_efa921d8f97a/entry.v2.json"
d = json.loads(p.read_text(encoding="utf-8"))
os = d["Senses"][0]["Occurrences"]
# Both old spans fused separate tokens/turns. Retain one actor-pure token.
os[0]["Kwic"] = "古德道，從門入者，不是家珍。"
os[5]["Kwic"] = "師曰：不是自家珍。"
os[5]["MasterName"] = "Caoshan Qianghui Zhiju"
os[5]["ContextMasters"] = [{"MasterName":"Caoshan Qianghui Zhiju","Roles":["utterer","respondent","record-owner"]}]
os[5].pop("ActorAttribution",None)
os[5]["AttributionNote"] = "Strict Lineage of the Five Lamps (五燈嚴統), complete Caoshan Qianghui Zhiju encounter: Caoshan answers the unnamed monk's jewel question, ‘It is not one's own household treasure.’ The KWIC is recut to Caoshan's single actor-pure turn."
for s0 in d["Senses"]:
    s0["SourceTexts"] = list(dict.fromkeys(o["RelPath"] for o in s0["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired hand-read entry 19")

# 20 昭昭靈靈 — all six complete structural cases hand-read.
p = BUILD / "fresh-build/entries/t_faf30cf1fb87/entry.v2.json"
d = json.loads(p.read_text(encoding="utf-8"))
os = d["Senses"][0]["Occurrences"]
os[0]["Kwic"] = "我今問汝：汝若認昭昭靈靈是汝真實"
os[1]["Kwic"] = "我向汝道：昭昭靈靈，祇因前塵色聲香等法而有分別"
os[2]["Kwic"] = "病在認目前昭昭靈靈以為是"
os[5]["MasterName"] = "Yulin Tongxiu"
os[5]["ContextMasters"] = [{"MasterName":"Yulin Tongxiu","Roles":["utterer","record-owner"]}]
os[5]["AttributionNote"] = "Recorded Sayings of National Teacher Yulin (大覺普濟玉林禪師語錄), complete instruction under the record's own heading: Yulin Tongxiu warns against disorderly outsiders taking the bright-and-aware consciousness-spirit as the master within. The prior attribution to Juelang Daosheng was false."
for s0 in d["Senses"]:
    s0["SourceTexts"] = list(dict.fromkeys(o["RelPath"] for o in s0["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired hand-read entry 20")

# 21 五位 — all fifteen complete structural cases hand-read.
p = BUILD / "fresh-build/entries/t_ff50c6974a36/entry.v2.json"
d = json.loads(p.read_text(encoding="utf-8"))
s1,s2=d["Senses"]; os=s1["Occurrences"]
# O6: the compiler introduces Dongshan's composition; the headword is in that introduction.
o=os[5]; o["MasterName"]=None; o["ContextMasters"]=[{"MasterName":"Dongshan Liangjie","Roles":["person-described","attributed-author"]}]
o["ActorAttribution"]={"Status":"narrated","Kind":"compiler narrative","ActorLabel":"compiler introducing Dongshan Liangjie's verse","ActorRole":"compiler","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"師作五位君臣頌云 is the compiler's attribution formula; Dongshan's quoted verse begins with 正中偏.","ReviewedBy":"Codex cohorts 1-3 v6 full-case hand read","ReviewedUtc":reviewed}
o["AttributionNote"]="The cited Caodong source (T47n1986B), complete composition unit: the compiler says that Dongshan Liangjie composed a Five Ranks lord-and-minister verse; Dongshan is the attributed verse author, but the headword occurs in the compiler's introduction."
# O7: the unnamed monk, not Caoshan, utters the stored headword-bearing question.
o=os[6]; o["MasterName"]=None; o["ContextMasters"]=[{"MasterName":"Caoshan Benji","Roles":["respondent","record-owner"]}]
o["ActorAttribution"]={"Status":"reviewed-unnamed","Kind":"monk","ActorLabel":"the unnamed questioning monk","ActorRole":"questioner","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"因有僧問五位君臣旨訣 assigns the headword-bearing question to 僧; Caoshan's answer begins after 師曰.","ReviewedBy":"Codex cohorts 1-3 v6 full-case hand read","ReviewedUtc":reviewed}
o["AttributionNote"]="Recorded Sayings of Caoshan (曹山大師語錄), complete question-and-answer: an unnamed monk asks for the key to the Five Ranks of lord and minister; Caoshan Benji gives the extended answer."
# O8 and O11 carried repeated diagram/list tokens; retain one token without changing sense.
os[7]["Kwic"]="示以偏正五位。"
os[10]["Kwic"]="五位功勳圖正中偏君位向黑白未變時"
o=os[10]; o["MasterName"]=None; o["ContextMasters"]=[]
o["ActorAttribution"]={"Status":"narrated","Kind":"compiled diagram heading","ActorLabel":"compiler of the Five Ranks of merit diagram","ActorRole":"compiler","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"五位功勳圖 is a repeated editorial diagram heading, not direct speech by Huiyan Zhizhao.","ReviewedBy":"Codex cohorts 1-3 v6 full-case hand read","ReviewedUtc":reviewed}
o["AttributionNote"]="The cited compiled Caodong material (T48n2006), complete diagram unit: the editor labels the Five Ranks of merit diagram before its rank descriptions. The headword is an editorial heading, not speech by Huiyan Zhizhao."
for s0 in d["Senses"]:
    s0["SourceTexts"] = list(dict.fromkeys(o["RelPath"] for o in s0["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired hand-read entry 21")

# 22 禮拜 — all thirteen complete structural cases hand-read.
p = BUILD / "fresh-build/entries/t_1d3473614976/entry.v2.json"
d = json.loads(p.read_text(encoding="utf-8")); os=d["Senses"][0]["Occurrences"]
# The Mazu/Daliang span contained the narrator's action token and Mazu's quoted repetition.
os[1]["Kwic"]="豁然大悟禮拜。"
# The final passage is Poshan's own hall retelling, not anonymous source narration.
o=os[12]; o["MasterName"]="Poshan Haiming"; o["ContextMasters"]=[{"MasterName":"Poshan Haiming","Roles":["utterer","record-owner","later-raiser"]},{"MasterName":"Deshan Xuanjian","Roles":["case-figure","person-discussed"]}]
o.pop("ActorAttribution",None)
o["AttributionNote"]="Recorded Sayings of Chan Master Poshan (破山禪師語錄), full hall address: Poshan Haiming retells the Deshan–Longtan lamp case and says that Zhou (Deshan Xuanjian) greatly awakened and bowed. Deshan is the case figure; Poshan is the utterer of the stored headword."
for s0 in d["Senses"]:
    s0["SourceTexts"] = list(dict.fromkeys(o["RelPath"] for o in s0["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired hand-read entry 22")

# 23 白居易 — all five complete structural cases hand-read.
p = BUILD / "fresh-build/entries/t_4db0d950f314/entry.v2.json"
d = json.loads(p.read_text(encoding="utf-8")); os=d["Senses"][0]["Occurrences"]
o=os[3]; o["MasterName"]="Chuiwan Guangzhen"; o["ContextMasters"]=[{"MasterName":"Chuiwan Guangzhen","Roles":["utterer","record-owner","later-raiser"]},{"MasterName":"Weikuan","Roles":["case-figure","respondent"]}]
o.pop("ActorAttribution",None)
o["AttributionNote"]="Recorded Sayings of Chuiwan Guangzhen (吹萬禪師語錄), complete hall address: Chuiwan Guangzhen names Bai Juyi while raising Bai's extended exchange with Weikuan. Bai and Weikuan are case figures; Chuiwan is the headword utterer."
for s0 in d["Senses"]:
    s0["SourceTexts"] = list(dict.fromkeys(o["RelPath"] for o in s0["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired hand-read entry 23")

# 24 拈拄杖 — all eight complete structural cases hand-read.
p = BUILD / "fresh-build/entries/t_68303eb8b076/entry.v2.json"
d = json.loads(p.read_text(encoding="utf-8")); os=d["Senses"][0]["Occurrences"]
stage = {
  0:("compiler of Shengzong of Chengxin's section",[],"拈拄杖曰 is the recorder's action-and-speech marker; Shengzong's quoted question begins after 曰.","Complete Book of the Five Lamps (五燈全書), full Chengxin Shengzong hall address: the compiler narrates Shengzong taking up the staff before Shengzong asks which is the buddha-seed."),
  3:("compiler of Shancui of Dingzhou's section",[],"師陞座，拈拄杖曰 is unquoted stage narration; Shancui's words begin after 曰.","Strict Lineage of the Five Lamps (五燈嚴統), complete Dingzhou Shancui encounter: the compiler narrates Shancui ascending the seat and taking up the staff before quoting his warning."),
  4:("compiler of Dahui Zonggao's lamp-record section",[{"MasterName":"Dahui Zonggao","Roles":["person-described","record-owner"]}],"驀拈拄杖，云 is a recorder-supplied stage direction; Dahui's verse begins after 云.","Extended Lamp Record (續傳燈錄), full Dahui Zonggao section: the compiler narrates Dahui suddenly taking up the staff before quoting the sword verse."),
  7:("compiler of Muzhou Daoming's lamp-record section",[{"MasterName":"Muzhou Daoming","Roles":["person-described","record-owner"]}],"師拈拄杖打曰 is unquoted action narration. The nearest section heading is 睦州陳尊宿; it is not Huangbo's section.","Five Lamps Meeting the Source (五燈會元), complete Muzhou Daoming section: the compiler narrates Muzhou taking up the staff and striking a monk. The prior Huangbo Xiyun attribution was a section-identification error."),
}
for i,(label,contexts,evidence,note) in stage.items():
 o=os[i]; o["MasterName"]=None; o["ContextMasters"]=contexts
 o["ActorAttribution"]={"Status":"narrated","Kind":"compiler narrative","ActorLabel":label,"ActorRole":"compiler","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":evidence,"ReviewedBy":"Codex cohorts 1-3 v6 full-case hand read","ReviewedUtc":reviewed}
 o["AttributionNote"]=note
for s0 in d["Senses"]:
    s0["SourceTexts"] = list(dict.fromkeys(o["RelPath"] for o in s0["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired hand-read entry 24")

# 25 開堂 — all eight complete structural cases hand-read.
p = BUILD / "fresh-build/entries/t_804dc8bdce55/entry.v2.json"
d = json.loads(p.read_text(encoding="utf-8")); os=d["Senses"][0]["Occurrences"]
o=os[4]; o["MasterName"]=None; o["ContextMasters"]=[{"MasterName":"Dongsi Ruhui","Roles":["respondent","record-owner"]}]
o["ActorAttribution"]={"Status":"reviewed-unnamed","Kind":"petitioner","ActorLabel":"the unnamed petitioner","ActorRole":"questioner","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"某甲擬請和尚開堂得否 is the petitioner's direct question; Dongsi Ruhui answers after 師曰.","ReviewedBy":"Codex cohorts 1-3 v6 full-case hand read","ReviewedUtc":reviewed}
o["AttributionNote"]="Five Lamps Meeting the Source (五燈會元), complete Dongsi Ruhui section: an unnamed petitioner asks whether he may invite Dongsi to open the hall. Dongsi is the respondent, not the headword utterer."
for s0 in d["Senses"]:
    s0["SourceTexts"] = list(dict.fromkeys(o["RelPath"] for o in s0["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired hand-read entry 25")
