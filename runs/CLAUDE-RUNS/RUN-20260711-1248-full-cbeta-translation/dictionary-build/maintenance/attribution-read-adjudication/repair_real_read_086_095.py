import json
from pathlib import Path
BUILD=Path(__file__).resolve().parents[2]; STAMP="2026-07-16T14:00:00Z"
def load(t):
 p=BUILD/"fresh-build/entries"/t/"entry.v2.json"; return p,json.loads(p.read_text(encoding="utf-8"))
def save(p,d): p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
def aa(status,kind,label,role,e):
 return {"Status":status,"Kind":kind,"ActorLabel":label,"ActorRole":role,"RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":e,"ReviewedBy":"Codex cohorts 1-3 v6 full-unit READ-AND-FIX","ReviewedUtc":STAMP}

# 86 禪床
p,d=load("t_c212062774f9");s=d["Senses"][0];os=s["Occurrences"]
ctx={1:[("Zhaozhou Congshen",["case-figure","action-performer"])],4:[("Shishuang Chuyuan",["action-performer","hall-speaker"])],5:[("Niutou Huizhong",["action-performer","person-described"])],7:[("Dahui Zonggao",["action-performer","hall-speaker"])],9:[("Magu Baoche",["action-performer","questioner"]),("Nanyang Huizhong",["respondent","case-teacher"])]}
for i,pairs in ctx.items(): os[i]["ContextMasters"]=[{"MasterName":n,"Roles":r} for n,r in pairs]
for i in range(1,10):
 if i==6: continue
 os[i]["MasterName"]=None;os[i]["ActorAttribution"]=aa("narrated","stage-direction or case narration","the case narrator","narrator","The headword labels a Chan seat or an action involving it in narration; quoted speech begins separately.");os[i]["AttributionNote"]="Complete unit read: the narrator owns the seat/action wording; the named participant performs or frames the action but does not utter the headword."
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in os));save(p,d)

# 87 洗鉢盂
p,d=load("t_c4a694970a12");s=d["Senses"][0];os=s["Occurrences"]
os[0]["MasterName"]=None;os[0]["ActorAttribution"]=aa("narrated","biographical narration","the biographer","compiler","The biographer says the master used the wash-your-bowl case as his standing room question; this is not an interlocutor's utterance.");os[0]["AttributionNote"]="The biography reports a master's recurring room question about the wash-your-bowl case."
os[1]["ContextMasters"]=[{"MasterName":"Wuzu Fayan","Roles":["instructor","case-teacher"]},{"MasterName":"Zhaozhou Congshen","Roles":["case-source"]}];os[1]["AttributionNote"]="The biographer reports Wuzu Fayan assigning Zhaozhou's wash-your-bowl case and later testing it."
for i in (3,4):
 os[i]["MasterName"]="Zhaozhou Congshen";os[i].pop("ActorAttribution",None);os[i]["Kwic"]="州云：喫粥了也未？云：喫粥了。州云：洗鉢盂去。";os[i]["ContextMasters"]=[{"MasterName":"Zhaozhou Congshen","Roles":["quoted-utterer","case-source"]}]+([{"MasterName":"Dahui Zonggao","Roles":["later-raiser","commentator"]}] if i==3 else []);os[i]["AttributionNote"]="The quoted case directly assigns 'wash your bowl' to Zhaozhou Congshen; the surrounding work transmits or comments on that turn."
os[5]["MasterName"]=None;os[5]["ActorAttribution"]=aa("named-unrostered","hall speaker","Qingshan Shoulong of Hangzhou (杭州慶善守隆)","utterer","The record heading names Qingshan Shoulong, who directly asks how to understand the wash-your-bowl phrase.");os[5]["AttributionNote"]="Qingshan Shoulong directly asks the assembly how to understand the wash-your-bowl phrase; roster normalization remains pending."
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in os));save(p,d)

# 88 家風
p,d=load("t_c728f3a8e02b");s=d["Senses"][0];os=s["Occurrences"];o=os[3]
o["MasterName"]=None;o["ContextMasters"]=[{"MasterName":"Feiyin Tongrong","Roles":["respondent","hall-speaker","case-teacher"]}];o["ActorAttribution"]=aa("reviewed-unnamed","monastic questioner","the unnamed monk","questioner","僧問 assigns the headword-bearing question about Linji's house style to an unnamed monk; Feiyin's answer follows after 師云.");o["AttributionNote"]="An unnamed monk contrasts Linji's house style with the Caodong purport; Feiyin Tongrong answers next."
s["SourceTexts"]=list(dict.fromkeys(x["RelPath"] for x in os));save(p,d)

# 89 陞座
p,d=load("t_cbf868f557e2");s=d["Senses"][0];os=s["Occurrences"]
perform={2:"Fayan Wenyi",3:"Shakyamuni Buddha",4:"Huangbo Xiyun",5:"Huqiu Shaolong",6:"Yushan Shangsi",7:"Yulin Tongxiu"}
for i,name in perform.items():
 os[i]["MasterName"]=None;os[i]["ContextMasters"]=[{"MasterName":name,"Roles":["action-performer","hall-speaker"]}];os[i]["ActorAttribution"]=aa("narrated","event narration","the record narrator","narrator","The narrator or heading reports the named figure ascending the seat; the event label is not uttered by its performer.");os[i]["AttributionNote"]=f"The narrator reports {name} ascending the teaching seat; {name} performs the event but does not utter its label."
os[1]["ActorAttribution"]=aa("impersonal","preface terminology","the preface author","compiler","The preface discusses the genre of collected seat-ascending addresses, not a speech turn.")
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in os));save(p,d)

# 90 石人
p,d=load("t_d0a4a5271135");s=d["Senses"][0];os=s["Occurrences"]
os[1]["ContextMasters"]=[{"MasterName":"Yongming Yanshou","Roles":["respondent","hall-speaker"]}];os[1]["AttributionNote"]="An unnamed monk quotes the line about stone and wooden people answering together; Yongming Yanshou answers next."
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in os));save(p,d)

# 91 第一句
p,d=load("t_d0d82a2681a0");s=d["Senses"][0];os=s["Occurrences"]
os[3]["ContextMasters"]=[{"MasterName":"Jingqing Daofu","Roles":["respondent","case-teacher"]}];os[3]["AttributionNote"]="An unnamed monk asks Jingqing Daofu for the first phrase; Jingqing answers next."
os[4]["MasterName"]=None;os[4]["ActorAttribution"]=aa("reviewed-unnamed","master questioner","the unnamed master","questioner","師問居士 assigns the headword-bearing question about Bodhidharma's first phrase to the master; the unit does not preserve his safe personal name.");os[4]["AttributionNote"]="An unnamed master asks the layman for Bodhidharma's first phrase; the layman's reply is separate."
os[7]["ContextMasters"]=[{"MasterName":"Linji Yixuan","Roles":["respondent","case-teacher"]}];os[7]["AttributionNote"]="An unnamed monk asks for the first phrase; Linji Yixuan gives the separately marked answer."
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in os));save(p,d)

# 92 代云
p,d=load("t_d8868082a16c");s=d["Senses"][0];os=s["Occurrences"]
for i in (1,2):
 os[i]["MasterName"]="Yunmen Wenyan";os[i].pop("ActorAttribution",None);os[i]["ContextMasters"]=[{"MasterName":"Yunmen Wenyan","Roles":["utterer","substitute-answerer"]}];os[i]["AttributionNote"]="In the Yunmen record, 師代云 marks Yunmen Wenyan directly supplying the substitute response."
os[4]["MasterName"]=None;os[4]["ActorAttribution"]=aa("named-unrostered","master's direct speech","Lingrui (靈瑞)","utterer","自代云 marks the title master Lingrui directly supplying his own substitute response.");os[4]["AttributionNote"]="Lingrui directly supplies the substitute answer; roster normalization is pending."
os[5]["MasterName"]="Fenyang Shanzhao";os[5].pop("ActorAttribution",None);os[5]["ContextMasters"]=[{"MasterName":"Fenyang Shanzhao","Roles":["utterer","substitute-answerer"]}];os[5]["AttributionNote"]="In Fenyang's record, 代云 introduces Fenyang Shanzhao's direct substitute response."
os[6]["MasterName"]=None;os[6]["ContextMasters"]=[{"MasterName":"Chushi Fanqi","Roles":["respondent","hall-speaker"]},{"MasterName":"Xuedou Chongxian","Roles":["person-discussed","substitute-answerer"]}];os[6]["ActorAttribution"]=aa("reviewed-unnamed","monastic interlocutor","the unnamed monk","questioner","進云 assigns 'Later Xuedou supplied...' to the unnamed monk; Chushi's evaluation follows after 師云.");os[6]["AttributionNote"]="An unnamed monk reports Xuedou's later substitute response and asks whether it accords; Chushi Fanqi answers."
os[7]["MasterName"]="Fayan Wenyi";os[7].pop("ActorAttribution",None);os[7]["ContextMasters"]=[{"MasterName":"Fayan Wenyi","Roles":["utterer","substitute-answerer"]}];os[7]["AttributionNote"]="The source explicitly says Fayan Wenyi supplied the substitute response 'Precisely habit-energy.'"
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in os));save(p,d)

# 93 問訊
p,d=load("t_dc24f92ead78");s=d["Senses"][0];os=s["Occurrences"]
os[4]["ContextMasters"]=[{"MasterName":"Puhua","Roles":["action-performer","questioner"]},{"MasterName":"Linji Yixuan","Roles":["respondent","case-teacher"]}];os[4]["AttributionNote"]="The narrator reports Puhua making formal inquiry before asking Linji about the earlier statement."
os[5]["ContextMasters"]=[{"MasterName":"Fayan Wenyi","Roles":["respondent","person-described"]}];os[5]["AttributionNote"]="The narrator reports an unnamed monk making formal inquiry while Fayan Wenyi was ill; Fayan then speaks."
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in os));save(p,d)

# 94 法身邊事
p,d=load("t_e17068150613");s=d["Senses"][0];os=s["Occurrences"]
os[0]["MasterName"]="Shushan Kuangren";os[0].pop("ActorAttribution",None);os[0]["ContextMasters"]=[{"MasterName":"Shushan Kuangren","Roles":["utterer","questioner"]},{"MasterName":"Guishan Lingyou","Roles":["respondent","case-teacher"]}];os[0]["AttributionNote"]="Shushan Kuangren directly asks Guishan Lingyou about what remains merely a matter at the side of the Dharma-body."
os[1]["MasterName"]="Yunmen Wenyan";os[1].pop("ActorAttribution",None);os[1]["ContextMasters"]=[{"MasterName":"Yunmen Wenyan","Roles":["utterer","questioner"]},{"MasterName":"Shushan Kuangren","Roles":["respondent","case-teacher"]}];os[1]["AttributionNote"]="Yunmen Wenyan directly repeats Shushan's formula and asks for the Dharma-body-side matter."
os[2]["MasterName"]="Shushan Kuangren";os[2]["Kwic"]="疎山示眾云：病僧咸通年已前會得法身邊事，咸通年已後會得法身向上事。";os[2]["ContextMasters"]=[{"MasterName":"Shushan Kuangren","Roles":["quoted-utterer","case-source"]},{"MasterName":"Tianyin Yuanxiu","Roles":["later-raiser","commentator"]}];os[2]["AttributionNote"]="Tianyin Yuanxiu raises the old case; the stored headword token is Shushan Kuangren's explicitly quoted declaration."
os[3]["MasterName"]="Chushi Fanqi";os[3].pop("ActorAttribution",None);os[3]["Kwic"]="你道枯樁與非枯樁、法身邊事與法身向上事作麼生辨？";os[3]["ContextMasters"]=[{"MasterName":"Chushi Fanqi","Roles":["utterer","commentator"]},{"MasterName":"Shushan Kuangren","Roles":["case-source"]},{"MasterName":"Yunmen Wenyan","Roles":["case-figure"]}];os[3]["AttributionNote"]="Chushi Fanqi directly asks his audience to distinguish the Dharma-body-side matter from the matter beyond it."
os[4]["MasterName"]="Langye Huijue";os[4].pop("ActorAttribution",None);os[4]["AttributionNote"]="The section names Langye Huijue, who directly says that ruler and minister according is still a Dharma-body-side matter."
os[5]["MasterName"]=None;os[5]["ActorAttribution"]=aa("reviewed-unnamed","hall speaker","the unnamed master","utterer","The hall address directly states that ruler and minister according is still a Dharma-body-side matter, but the supplied unit does not safely preserve the speaker's name.");os[5]["AttributionNote"]="An unnamed master directly contrasts merit-side and Dharma-body-side matters in a hall address."
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in os));save(p,d)

# 95 徐六擔板 — all six named direct speakers hold after full reading.
p,d=load("t_e21288d0fefb");s=d["Senses"][0];s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]));save(p,d)
print("repaired/read-adjudicated cohorts 1-3 entries 086-095")
