import hashlib, json
from datetime import datetime, timezone
from pathlib import Path

BUILD = Path(__file__).resolve().parents[2]
STAMP = datetime.now(timezone.utc).isoformat()
IDS = [
    "t_e2c55f8feca0","t_e96268628f2c","t_e9847e6f41c9","t_eea2b5e58c24",
    "t_efc6a42814ee","t_f266d9e034ea","t_f47e0cb15568","t_f5f691fd0483",
    "t_f69bf9de345e","t_f7577f4c57c3",
]
LEDGER = []
RUNGS = ["line","expanded-context","section-header","book-title","tei-header","parallel-passage"]

def path(t): return BUILD/"fresh-build"/"entries"/t/"entry.v2.json"
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def load(t):
    p=path(t); return p,json.loads(p.read_text(encoding="utf-8")),digest(p)
def save(p,d,old,findings):
    p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    LEDGER.append({"Id":d["Id"],"SourceTerm":d["SourceTerm"],"oldSha256":old,"newSha256":digest(p),"findings":findings})
def ctx(*pairs): return [{"MasterName":n,"Roles":list(r)} for n,r in pairs]
def named(o,name,pairs,note):
    o["MasterName"]=name; o.pop("ActorAttribution",None); o["ContextMasters"]=ctx(*pairs); o["AttributionNote"]=note
def anon(o,status,kind,label,role,e,pairs,note):
    o["MasterName"]=None
    o["ActorAttribution"]={"Status":status,"Kind":kind,"ActorLabel":label,"ActorRole":role,
        "RungsChecked":RUNGS,"GrammarEvidence":e,"ReviewedBy":"Codex cohorts 1-3 096-105 v6 full-case READ-AND-FIX","ReviewedUtc":STAMP}
    o["ContextMasters"]=ctx(*pairs); o["AttributionNote"]=note

# 三寸 — all eight cases reread. The Linji travel case assigns the phrase to Fenglin, not Linji.
p,d,old=load(IDS[0]); a=d["Senses"][0]["Occurrences"]; b=d["Senses"][1]["Occurrences"]
anon(a[1],"reviewed-unnamed","monastic questioner","the unnamed monk","questioner","僧問 introduces the headword-bearing request; Cuiyan Lingcan's reply begins only at 師曰.",(("Cuiyan Lingcan",("respondent","section-subject")),),"Compendium of the Five Lamps (五燈會元), Cuiyan Lingcan section: an unnamed monk asks Cuiyan to speak without borrowing the three-inch tongue; Cuiyan answers next.")
anon(a[2],"reviewed-unnamed","monastic questioner","the unnamed monk","questioner","The omitted questioner says the old master's three inches are tightly guarded; Dahui's reply starts at 師曰.",(("Dahui Zonggao",("respondent","section-subject")),),"Complete Compendium of the Five Lamps (五燈全書(第34卷-第120卷)), Dahui Zonggao section: an unnamed monk calls Dahui's three-inch tongue tightly guarded; Dahui answers next.")
named(a[3],"Fenglin",(("Fenglin",("utterer","interlocutor")),("Linji Yixuan",("respondent","case-figure"))),"Old Recorded Sayings of Venerable Masters (古尊宿語錄), Linji travel case: Fenglin says that one may wield the three-inch tongue across heaven and earth; Linji answers next.")
save(p,d,old,["confirmed 8 full cases","corrected Fenglin as exact utterer","expanded anonymous-questioner context"])

# 萬法歸一 — seven full cases confirm the headword belongs to the asking monk(s).
p,d,old=load(IDS[1]); os=d["Senses"][0]["Occurrences"]
for o in os:
    if o.get("MasterName") is None and o.get("ActorAttribution"):
        o["ActorAttribution"]["GrammarEvidence"]="The complete case places the stock question before the separately marked master's answer; the record does not personally name the questioning monk."
        o["ActorAttribution"]["ReviewedBy"]="Codex cohorts 1-3 096-105 v6 full-case READ-AND-FIX"; o["ActorAttribution"]["ReviewedUtc"]=STAMP
os[2]["Kwic"]="僧問萬法歸一一歸何處"; os[2]["FromLb"]="0831a21"; os[2]["ToLb"]="0831a21"
os[2]["AttributionNote"]="Linked Collection of Chan Verses on Ancient Cases (禪宗頌古聯珠通集), Wenshu Yingzhen section: an unnamed monk asks where the one returns; Wenshu's answer follows separately."
os[3]["ContextMasters"]=ctx(("Baizhang Mingzhao An",("respondent","section-subject")))
os[3]["AttributionNote"]="Compendium of the Five Lamps (五燈會元), Baizhang Mingzhao An section: an unnamed monk asks where the one returns; Baizhang Mingzhao An answers that no one has failed to ask."
d["Senses"][0]["ClaimAnchors"]=[x for x in d["Senses"][0].get("ClaimAnchors",[]) if x.get("ClaimText")!="黃河九曲"]+[{
    "RelPath":"C/C078/C078n1720.xml","FromLb":"0831a21","ToLb":"0831a22","Kwic":"師曰黃河九曲。","ClaimText":"黃河九曲","Curated":True,
    "MasterName":"Wenshu Yingzhen","ContextMasters":ctx(("Wenshu Yingzhen",("utterer","section-subject"))),
    "AttributionNote":"Linked Collection of Chan Verses on Ancient Cases (禪宗頌古聯珠通集), Wenshu Yingzhen section: Wenshu Yingzhen answers the stock question with 'the Yellow River has nine bends.'"}]
save(p,d,old,["confirmed all 7 exact question turns","no actor reversal required"])

# 腳跟下 — the Tushita questioner is not a master; Faxi Yin owns the final answer.
p,d,old=load(IDS[2]); os=d["Senses"][0]["Occurrences"]
anon(os[5],"reviewed-unnamed","monastic questioner","the unnamed monk","questioner","僧問 assigns 'the matter under the heels' to an unnamed monk; Bulin Jian's answer starts at 師云.",(("Bulin Jian",("respondent","record-owner")),),"Recorded Sayings of Bulin Jian (兜率不磷堅禪師語錄): an unnamed monk asks Bulin about the matter right under the heels; Bulin answers 'putting a head atop the head.'")
named(os[7],"Faxi Yin",(("Faxi Yin",("utterer","record-owner")),),"Recorded Sayings of Faxi Yin (法璽印禪師語錄): in an emancipation-day address, Faxi Yin says that thirty blows would be fitting right under the questioner's heels.")
save(p,d,old,["confirmed all 8 full cases","removed invalid anonymous string from MasterName","recovered Faxi Yin exact utterer"])

# 院主 — distinguish narration, an official's direct question, and named later quotation.
p,d,old=load(IDS[3]); os=d["Senses"][0]["Occurrences"]
os[0]["Kwic"]="既而示疾院主問和尚近日尊候如何"; os[0]["FromLb"]="0617a14"; os[0]["ToLb"]="0617a15"
anon(os[1],"reviewed-unnamed","government official","the unnamed official","questioner","因問院主曰 assigns the headword-bearing question to an unnamed official; the steward's response starts at 主曰.",(),"Mirror of the Lineage Dharma Grove (宗鑑法林): an unnamed official asks the monastery steward which merit the protector image embodies.")
named(os[2],"Zhongfeng Mingben",(("Zhongfeng Mingben",("utterer","commentator")),("Danxia Tianran",("case-figure","person-discussed"))),"Record Pointing at the Moon (指月錄): Zhongfeng Mingben's cited explanatory verse says that Danxia burned the wooden buddha and made the monastery steward lose his eyebrows.")
os[4]["Kwic"]="院主曰：糶得盡。"; os[4]["FromLb"]="0248b01"; os[4]["ToLb"]="0248b01"
save(p,d,old,["confirmed all 7 cases","official question separated from steward reply","recovered Zhongfeng's authored comment"])

# 東坡居士 — all five occurrences are name/reference uses; current actor divisions hold.
p,d,old=load(IDS[4]); os=d["Senses"][0]["Occurrences"]
os[0]["Kwic"]="適東坡居士到"; os[0]["FromLb"]="0005b16"; os[0]["ToLb"]="0005b17"
os[3]["Kwic"]="適東坡居士到面前"; os[3]["FromLb"]="0137b04"; os[3]["ToLb"]="0137b05"
save(p,d,old,["confirmed all 5 full cases","paratext/narrator/quoted-master distinctions hold","recut two mixed-turn narrator spans"])

# 主中主 — all eight cases; preserve questioners and three named expositors.
p,d,old=load(IDS[5]); os=d["Senses"][0]["Occurrences"]
os[1]["Kwic"]="問如何是主中主\U000f223a云磨礱三尺劒待斬不平人"; os[1]["FromLb"]="0671c24"; os[1]["ToLb"]="0672a01"
anon(os[2],"reviewed-unnamed","preface author","the unnamed signed preface author","compiler","The signed preface author uses 主中主 while praising Juelang Daosheng; the available signature preserves only 旦 and does not recover a full personal name.",(("Juelang Daosheng",("person-described",)),),"Complete Record of Juelang Daosheng (天界覺浪盛禪師全錄), signed preface: an unnamed preface author describes Juelang through the host-within-host phrase.")
save(p,d,old,["confirmed all 8 full cases","questioner/respondent divisions hold","corrected role-only identified actor to reviewed unnamed preface author"])

# 末後一著 — first witness is Yu'an Ji's embedded saying; third is an unnamed questioner.
p,d,old=load(IDS[6]); os=d["Senses"][0]["Occurrences"]
named(os[0],"Yu'an Ji",(("Yu'an Ji",("utterer","case-figure")),("Langting Jingting",("later-quoter","record-owner"))),"Recorded Sayings of Langting Jingting (雲溪俍亭挺禪師語錄): Langting quotes his late teacher Yu'an Ji saying that the final move first reaches the tight barrier.")
anon(os[2],"reviewed-unnamed","named-by-office monastic questioner","the unnamed West Hall Wude","questioner","西堂無得問 names the questioner by office and style but not by a rostered personal name; Yinyuan's answer begins at 師云.",(("Yinyuan Longqi",("respondent","record-owner")),),"Recorded Sayings of Yinyuan Longqi (隱元禪師語錄): the unnamed West Hall Wude asks Yinyuan Longqi to substantiate the final move; Yinyuan tells him to bow at once.")
os[3]["Kwic"]="妄說未來禍福，師資相傳，謂之末後一著，心中疑信相半"; os[3]["FromLb"]="0232a20"; os[3]["ToLb"]="0232a21"
os[4]["Kwic"]="師曰：末後一著始到牢關。"; os[4]["FromLb"]="0801a19"; os[4]["ToLb"]="0801a19"
save(p,d,old,["confirmed all 6 full cases","reassigned embedded quote from Langting to Yu'an Ji","removed Yinyuan from questioner's exact-actor field"])

# 犀牛 — isolate the verse-presenting monk's exact turn so one occurrence never spans two utterers.
p,d,old=load(IDS[7]); os=d["Senses"][0]["Occurrences"]
os[2]["Kwic"]='僧呈題扇偈曰：「鹽官錯喚作犀牛。」'
os[2]["ToLb"]="0115a02"
anon(os[2],"reviewed-unnamed","verse-presenting monk","the unnamed monk","verse-author","僧呈題扇偈曰 assigns the headword-bearing verse line to an unnamed monk; Shiyu Mingfang's reply begins at 師曰.",(("Shiyu Mingfang",("respondent","record-owner")),("Yanguan Qian",("case-figure","person-discussed"))),"Shiyu Mingfang's Dharma Altar (石雨禪師法檀): an unnamed monk presents the verse 'Yanguan wrongly called it a rhinoceros'; Shiyu's challenge follows separately.")
os[6]["ContextMasters"]=ctx(("Pin Jixiang",("utterer","record-owner")),("Yanguan Qian",("case-figure","person-discussed")),("Yunmen Wenyan",("case-figure","person-discussed")))
save(p,d,old,["confirmed all 7 full cases","recut mixed-turn KWIC to the monk's exact verse turn"])

# 漆桶 — replace batch-default compiler labels with the actual direct speakers/voices.
p,d,old=load(IDS[8]); os=d["Senses"][0]["Occurrences"]
named(os[0],"Fachang Yiyu",(("Fachang Yiyu",("utterer","record-owner")),),"Complete Compendium of the Five Lamps (五燈全書(第34卷-第120卷)), Fachang Yiyu section: Fachang Yiyu calls the monk a black-lacquer bucket after the monk draws a circle.")
named(os[1],"Dahui Zonggao",(("Dahui Zonggao",("utterer","record-owner")),("Yuanwu Keqin",("teacher","case-figure"))),"Dahui Zonggao's Formal Discourses (大慧普覺禪師普說): Dahui Zonggao says that Yuanwu Keqin's two-line response suddenly broke open his lacquer bucket.")
named(os[2],"Dahui Zonggao",(("Dahui Zonggao",("utterer","verse-author")),),"Mirror of the Lineage Dharma Grove (宗鑑法林) preserves Dahui Zonggao's verse ending 'bah, this lacquer bucket is not quick.'")
named(os[3],"Muzhou Daoming",(("Muzhou Daoming",("utterer","section-subject")),),"Old Recorded Sayings of Venerable Masters (古尊宿語錄), Muzhou Daoming section: Muzhou answers the question about meeting a blue-eyed man with 'a ghost fighting a lacquer bucket.'")
named(os[4],"Baoshou Xin",(("Baoshou Xin",("utterer","commentator")),("Bodhidharma",("case-figure","person-discussed"))),"Collected Ancient Cases of the Chan Gate (宗門拈古彙集): Baoshou Xin calls the participants a gang of black-lacquer buckets while commenting on Bodhidharma's skin-and-marrow case.")
named(os[5],"Yingan Tanhua",(("Yingan Tanhua",("utterer","record-owner")),),"Recorded Sayings of Yingan Tanhua (應菴曇華禪師語錄): Yingan ends his criticism of stock responses with 'far away, lacquer bucket.'")
named(os[6],"Dahui Zonggao",(("Dahui Zonggao",("utterer","verse-author")),),"Record Pointing at the Moon (指月錄) explicitly introduces this as a verse by Jingshan Gao, Dahui Zonggao.")
os[7]["Kwic"]="曰：這漆桶前後觸忤多少賢良！"; os[7]["FromLb"]="0028c09"; os[7]["ToLb"]="0028c10"
named(os[7],"Sansheng Huiran",(("Sansheng Huiran",("utterer","section-subject")),),"Strict Lineage of the Five Lamps (五燈嚴統(第10卷-第25卷)), Sansheng Huiran section: Sansheng Huiran directly calls the head monk a lacquer bucket.")
d["Senses"][0]["Explanation"]="A black-lacquer bucket is a blunt address for someone presented as unable to distinguish what is directly before them. Fachang Yiyu applies it after an unnamed questioner draws a circle; Sansheng Huiran applies it to a head monk; Muzhou Daoming and Yingan Tanhua use it in direct replies and rebukes. Dahui Zonggao also speaks of his own 'lacquer bucket' being broken open and repeats the phrase in verse. The concrete vessel supplies the insult's opaque, sealed image; the records themselves establish the recurrent public rebuke without assigning it a further doctrinal gloss."
save(p,d,old,["confirmed all 8 full cases","replaced 8 batch-default compiler attributions with exact speakers","named sources and speakers in prose"])

# 第二句 — every stored unit reread; seven are direct master/layman turns, the last a preface voice.
p,d,old=load(IDS[9]); os=d["Senses"][0]["Occurrences"]
named(os[0],"Yuanwu Keqin",(("Yuanwu Keqin",("utterer","section-subject")),),"Complete Compendium of the Five Lamps (五燈全書(第34卷-第120卷)), Yuanwu Keqin section: Yuanwu Keqin says that one who recommends in the second phrase makes humans and devas lose their nerve.")
named(os[1],"Foyan Qingyuan",(("Foyan Qingyuan",("utterer","record-owner")),),"Old Recorded Sayings of Venerable Masters (古尊宿語錄), Foyan Qingyuan record: Foyan states the first-, second-, and third-phrase rankings before answering a monk's questions.")
named(os[2],"Linji Yixuan",(("Linji Yixuan",("utterer","section-subject")),),"Strict Lineage of the Five Lamps (五燈嚴統(第10卷-第25卷)), Linji Yixuan section: Linji Yixuan says one who recommends within the second phrase can teach humans and devas.")
named(os[3],"Changsheng Jiaoran",(("Changsheng Jiaoran",("utterer","case-figure")),("Xuefeng Yicun",("teacher","case-figure"))),"Compendium of the Five Lamps (五燈會元), Changsheng Jiaoran section: after hearing Xuefeng's silence reported, Changsheng says, 'This is the second phrase.'")
named(os[4],"Hanyue Fazang",(("Hanyue Fazang",("utterer","record-owner")),),"Recorded Sayings of Hanyue Fazang (三峰藏和尚語錄): Hanyue gives a three-phrase ranking and says what happens when one recommends in the second phrase.")
named(os[5],"Pang Yun",(("Pang Yun",("utterer","case-figure")),("Danxia Tianran",("interlocutor","case-figure"))),"Record Pointing at the Moon (指月錄), Layman Pang Yun section: Pang asks Danxia to remain seated because there is still a second phrase.")
named(os[6],"Nanyue Jiqi",(("Nanyue Jiqi",("utterer","record-owner")),("Fengxue Yanzhao",("case-figure","person-discussed")),("Dahui Zonggao",("case-figure","person-discussed"))),"Recorded Sayings of Nanyue Jiqi (南嶽繼起和尚語錄): Jiqi sorts Fengxue's and Dahui's sayings through first, second, and third phrases.")
anon(os[7],"reviewed-unnamed","preface author","the unnamed preface author","compiler","The headword occurs in the author's first-person critical preface to the Combined Essentials of the Lamps; no master speech turn governs it and the supplied preface does not identify the writer by name.",(),"Combined Essentials of the Lamps (聯燈會要), anonymous preface: the unnamed preface author asks how there could be any so-called second phrase after claiming the earlier records showed not one word.")
d["Senses"][0]["Explanation"]="The second phrase is a ranked or sequential verbal position whose local force depends on its first-and-third-phrase scheme. Linji Yixuan says that one who recommends within it can teach humans and devas; Foyan Qingyuan and Hanyue Fazang place it inside their own three-phrase rankings. In another case Changsheng Jiaoran calls Xuefeng's silence 'the second phrase,' while Pang Yun tells Danxia that a second phrase still remains. Nanyue Jiqi deliberately reorders inherited first, second, and third phrases. These are distinct public classifications rather than one universal sentence occupying rank two."
save(p,d,old,["confirmed all 8 full cases","replaced 8 batch-default compiler labels","distinguished seven direct utterers from one anonymous preface voice"])

out=Path(__file__).with_name("cohorts-1-3-096-105-full-read-repair-ledger.json")
out.write_text(json.dumps({"generatedUtc":STAMP,"packetUnitsRead":72,"entries":LEDGER},ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print(json.dumps({"entries":len(LEDGER),"ledger":str(out),"hashes":[(x["SourceTerm"],x["oldSha256"],x["newSha256"]) for x in LEDGER]},ensure_ascii=False,indent=2))
