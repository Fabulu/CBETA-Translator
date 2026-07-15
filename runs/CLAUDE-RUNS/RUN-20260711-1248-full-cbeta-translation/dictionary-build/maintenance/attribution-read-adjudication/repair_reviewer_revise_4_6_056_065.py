import json
from pathlib import Path
BUILD=Path(__file__).resolve().parents[2]; STAMP="2026-07-16T12:30:00Z"
def load(t):
 p=BUILD/"fresh-build/entries"/t/"entry.v2.json"; return p,json.loads(p.read_text(encoding="utf-8"))
def save(p,d): p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
def aa(status,kind,label,role,evidence): return {"Status":status,"Kind":kind,"ActorLabel":label,"ActorRole":role,"RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":evidence,"ReviewedBy":"Codex grouped 4-6 independent-review full-unit REVISE","ReviewedUtc":STAMP}

# 拂袖: retain narrator ownership of bodily actions, but name the visible actors.
p,d=load("t_efbed6116e24");s=d["Senses"][0];o=s["Occurrences"]
o[0]["ContextMasters"]=[{"MasterName":"Xitang Zhizang","Roles":["questioner","case-figure"]},{"MasterName":"Baizhang Huaihai","Roles":["questioner","case-figure"]},{"MasterName":"Nanquan Puyuan","Roles":["case-figure"]}]
o[1]["ContextMasters"]=[{"MasterName":"Dirghanakha","Roles":["named-unrostered","action-performer","questioner"]},{"MasterName":"Shakyamuni Buddha","Roles":["respondent","case-teacher"]}]
o[1]["AttributionNote"]="Complete long-clawed Brahmin case: the narrator reports Dirghanakha sweeping his sleeves and leaving after Shakyamuni's question; Dirghanakha performs the action but does not utter its label."
o[2]["ContextMasters"]=[{"MasterName":"Xingjiao Ming","Roles":["named-unrostered","action-performer","questioner"]}]
o[2]["AttributionNote"]="Complete exchange: the narrator reports the named-but-unrostered Xingjiao Ming sweeping his sleeves and leaving after the master's answer."
o[4]["ContextMasters"]=[{"MasterName":"Yunmen Cheng","Roles":["named-unrostered","utterer","instructor"]}]
o[4]["MasterName"]=None;o[4]["ActorAttribution"]=aa("named-unrostered","master's hypothetical instruction","Yunmen Cheng (雲門澄)","utterer","雲門澄云 introduces the complete prescription ending in a shout and sweeping the sleeves; this is direct instruction by a named but unrostered master.")
o[4]["AttributionNote"]="Yunmen Cheng directly prescribes the response: shout, sweep the sleeves, and leave; he is named in the source but not yet rostered."
o[5]["MasterName"]="Dahui Zonggao";o[5]["ContextMasters"]=[{"MasterName":"Dahui Zonggao","Roles":["utterer","commentator"]}];o[5].pop("ActorAttribution",None);o[5]["AttributionNote"]="Dahui Zonggao directly criticizes contrived enactments of the woman-leaving-absorption case, including sleeve-sweeping, as shameful on cold inspection."
o[6]["ContextMasters"]=[{"MasterName":"Dirghanakha","Roles":["named-unrostered","action-performer","questioner"]},{"MasterName":"Shakyamuni Buddha","Roles":["respondent","case-teacher"]}]
o[6]["AttributionNote"]="Parallel long-clawed Brahmin case: the narrator reports Dirghanakha sweeping his sleeves and leaving; Shakyamuni is the respondent."
s["SourceTexts"]=list(dict.fromkeys(x["RelPath"] for x in o));save(p,d)

# 藥師: narrated rites/biography retain their named people and ceremonial roles.
p,d=load("t_f74516e0ba71");s=d["Senses"][0];o=s["Occurrences"]
o[0]["ContextMasters"]=[{"MasterName":"Yulin Tongxiu","Roles":["person-described","record-owner"]}];o[0]["AttributionNote"]="Yulin Tongxiu's biographer reports disciples keeping the name of Medicine Master Lapis-Lazuli Buddha and saying Yulin had manifested from that realm; the biographer owns the wording."
o[1]["ContextMasters"]=[{"MasterName":"Yongzheng Emperor","Roles":["named-unrostered","ceremony-patron"]}];o[1]["AttributionNote"]="Ritual narration in the Imperial Selection records officiants reciting the Medicine Master spell among the listed spells, circumambulating, repenting, vowing, and dedicating merit."
o[2]["ContextMasters"]=[{"MasterName":"Zhongfeng Mingben","Roles":["address-speaker","ceremony-master"]},{"MasterName":"Qü Tingfa","Roles":["named-unrostered","deceased-subject"]}]
o[2]["AttributionNote"]="Editorial ceremony heading: Zhongfeng Mingben gives a small-group address before the spirit at the Medicine Master observance for the deceased transport commissioner Qü Tingfa."
o[4]["ContextMasters"]=[{"MasterName":"Gaofeng San Shanlai","Roles":["named-unrostered","text-author"]}];o[4]["AttributionNote"]="Gaofeng San Shanlai's ritual-formulary heading identifies a Medicine Master memorial; the headword is a ceremony/title label rather than dialogue."
o[5]["ContextMasters"]=[{"MasterName":"Puming","Roles":["record-owner","hall-speaker"]}];o[5]["AttributionNote"]="Complete Puming record: the narrator labels the opening of a Medicine Master observance and the following hall address; Puming conducts the event but does not utter the heading."
s["SourceTexts"]=list(dict.fromkeys(x["RelPath"] for x in o));save(p,d)

# 雪山童子: first exchange repeats the same monk-question structure as the third.
p,d=load("t_fa1b42d25280");s=d["Senses"][0];o=s["Occurrences"][0];o["MasterName"]=None;o["ContextMasters"]=[{"MasterName":"Yexian Guixing","Roles":["respondent","case-teacher"]}];o["ActorAttribution"]=aa("reviewed-unnamed","monastic questioner","the unnamed monk","questioner","問雪山童子 introduces the unnamed monk's headword-bearing question; Yexian's answer begins after 師云.");o["AttributionNote"]="Complete Yexian exchange: an unnamed monk asks about the Snow Mountain Youth's self-sacrifice; Yexian Guixing gives the separately marked answer.";s["SourceTexts"]=list(dict.fromkeys(x["RelPath"] for x in s["Occurrences"]));save(p,d)

# 薦取: restore direct speakers named by the encompassing records.
p,d=load("t_fe7e2066672d");s=d["Senses"][0];o=s["Occurrences"]
o[0]["MasterName"]="Xuedou Chongxian";o[0]["ContextMasters"]=[{"MasterName":"Xuedou Chongxian","Roles":["utterer","instructor"]}];o[0].pop("ActorAttribution",None);o[0]["AttributionNote"]="Complete Mingjue/Xuedou biography: Xuedou Chongxian directly flicks a finger and tells the official to recognize it just so."
o[3]["MasterName"]="Fenyang Shanzhao";o[3]["ContextMasters"]=[{"MasterName":"Fenyang Shanzhao","Roles":["utterer","hall-speaker","instructor"]}];o[3].pop("ActorAttribution",None);o[3]["AttributionNote"]="Complete Fenyang discourse: Fenyang Shanzhao says that beyond the three phrases there are the three essentials and directly commands the assembly to recognize them."
o[5]["MasterName"]="Feiyin Tongrong";o[5]["ContextMasters"]=[{"MasterName":"Feiyin Tongrong","Roles":["utterer","hall-speaker"]}];o[5].pop("ActorAttribution",None);o[5]["AttributionNote"]="Complete Feiyin record: after a shout, Feiyin Tongrong directly commands the assembly to recognize the move that gets ahead."
# The long ordination address is unmistakably direct but its extracted packet does not
# preserve a safe personal heading; keep reviewed-unnamed rather than inventing one.
o[4]["MasterName"]=None;o[4]["ActorAttribution"]=aa("reviewed-unnamed","ordination-address speaker","the unnamed ordination speaker","utterer","The first-person public address directly commands listeners to recognize it before speech or intention; the extracted complete unit does not safely preserve a personal heading.");o[4]["AttributionNote"]="Direct ordination address: the unnamed speaker commands the newly shaven assembly to recognize it before speech, intention, beings, or buddhas arise; no personal name is safely recoverable in the supplied unit."
s["SourceTexts"]=list(dict.fromkeys(x["RelPath"] for x in o));save(p,d)
print("repaired independent-review REVISE entries 056-065")
