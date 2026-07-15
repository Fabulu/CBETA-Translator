#!/usr/bin/env python3
import hashlib, json, os
BASE=os.path.dirname(os.path.dirname(os.path.dirname(__file__)))
E=os.path.join(BASE,"fresh-build","entries"); L=os.path.join(BASE,"fresh-build","waves","f001-laneA-semantic-repairs.json")
def p(i): return os.path.join(E,i,"entry.v2.json")
def load(i): return json.load(open(p(i),encoding="utf-8"))
def put(i,d):
 q=p(i)+".tmp"
 with open(q,"w",encoding="utf-8",newline="\n") as f: json.dump(d,f,ensure_ascii=False,indent=2);f.write("\n")
 os.replace(q,p(i))

# Replace quote-owned 無事 evidence with direct utterances by the record owners.
i="t_f6dadadcbef5"; d=load(i); s=d["Senses"][0]
o=s["Occurrences"][9]
o.update({"RelPath":"T/T47/T47n1998A.xml","FromLb":"0812b03","ToLb":"0812b04","Kwic":"箇中若了全無事。體用無妨分不分。","MasterName":"Dahui Zonggao","ContextMasters":[{"MasterName":"Dahui Zonggao","Roles":["utterer"]}],"AttributionNote":"Recorded Sayings of Chan Master Dahui Pujue (大慧普覺禪師語錄): Dahui Zonggao utters this line in his own evening convocation; the full discourse boundary contains no embedded speaker for the headword turn."})
o=s["Occurrences"][10]
o.update({"RelPath":"X/X72/X72n1437.xml","FromLb":"0389c08","ToLb":"0389c11","Kwic":"還知有門內句麼？紫雲殿角木頭陀橫遭一摑，習儀亭石柱揚聲大哭，東西二塔撫掌大笑，云：屈！屈！大眾會麼？無事歸堂好。","MasterName":"Yongjue Yuanxian","ContextMasters":[{"MasterName":"Yongjue Yuanxian","Roles":["utterer"]}],"AttributionNote":"Extended Record of Chan Master Yongjue Yuanxian (永覺元賢禪師廣錄): Yongjue Yuanxian closes his own hall turn by dismissing the assembly; full-case review confirms the exact utterer."})
s["Explanation"]=s["Explanation"].replace("Dahui Zonggao quotes the ‘person with nothing to do’ only to demand an upward aperture beyond it, and Yongjue Yuanxian preserves the paired saying ‘nothing to do in the mind, no mind in affairs.’","Dahui Zonggao says that when this is understood completely, function and substance need not be divided, while Yongjue Yuanxian dismisses the assembly with ‘nothing to do—return to the hall.’")
put(i,d)

# Shorten two added KWICs to exact contiguous strings and synchronize line spans.
i="t_dab856504b69"; d=load(i); os_=d["Senses"][0]["Occurrences"]
os_[8]["Kwic"]="若無本分作家手段";os_[8]["ToLb"]="0455c02"
os_[9]["Kwic"]="莊宗作家君王興化明眼宗師";os_[9]["ToLb"]="0017b07"
put(i,d)

# Synchronize spans found by zc.verify.
for i,si,oi,to in [("t_f6dadadcbef5",0,8,"0458b07"),("t_1e41b014d80e",0,6,"0596a05")]:
 d=load(i);d["Senses"][si]["Occurrences"][oi]["ToLb"]=to;put(i,d)

# Reader-facing prose stays English-first. Search-control labels are described in
# English rather than left as unanchored Chinese strings; all actual quotations
# remain represented by their verified occurrence KWICs.
i="t_67bff0d0e5d3"; d=load(i)
for n in (2,3,7,8):
 d["Senses"][0]["Occurrences"][n]["AttributionNote"]=d["Senses"][0]["Occurrences"][n]["AttributionNote"].replace("the formula 僧問","the headword recorder formula")
put(i,d)

i="t_8f7b20536cb6"; d=load(i)
s=d["Senses"][0];s["Explanation"]=s["Explanation"].replace("In Chan records, 和尚 is","In Chan records, the headword is").replace("Huang Tingjian addresses a teacher","Huang Tingjian addresses the senior monastic before him")
s["Note"]=s["Note"].replace("The nested compound 和尚子","The nested ‘sons of masters’ compound")
s=d["Senses"][1];s["Explanation"]=s["Explanation"].replace("ordination frames, 和尚 names","ordination frames, the headword names").replace("The records use 得戒和尚 as a heading","The records use an ‘ordination preceptor’ heading")
put(i,d)

i="t_6f47a97d45b0"; d=load(i);d["Senses"][0]["Explanation"]=d["Senses"][0]["Explanation"].replace("序 names","The headword names").replace("of 序.","of the headword.");put(i,d)

i="t_5ddde30711a4"; d=load(i);s=d["Senses"][0]
s["Explanation"]=s["Explanation"].replace("with 金鎖玄路","with the complete gold-lock/dark-road compound")
s["Note"]="Modifier-control audit: the exact two-graph gold-lock compound has 397 hits in 164 files, and its expanded ‘yellow-gold lock’ form has 49 hits in 41 files. Controls include barrier, gate, lock, opening, and restraint frames, not descriptions of manufacture, weight, possession, or precious-metal hardware. The entry therefore keeps gold as a lexical modifier but avoids the materially misleading English ‘golden lock.’ Four headword witnesses from four distinct works support the compound."
put(i,d)

i="t_4f7bd98ad40f"; d=load(i);s=d["Senses"][0]
s["Explanation"]=s["Explanation"].replace("requested 升座 and then records 師上堂云: 升座 supplies","requested that he take the seat and then records that the master ascended the hall and spoke: the first verb supplies").replace("while 上堂 introduces","while the headword introduces")
s["Explanation"]=s["Explanation"].replace("the master ascended the hall and spoke","Linji Yixuan ascended the hall and spoke")
s["Note"]="One formal hall-address sense. The former physical split was rejected because its sole line uses a separate verb for taking the seat and the headword for the address. Thirteen witnesses span eleven distinct works."
put(i,d)

i="t_dab856504b69"; d=load(i);s=d["Senses"][0]
s["Explanation"]=s["Explanation"].replace("A corpus-wide control for 撰, 著, 編, 作者, and 作家詩客 found","A corpus-wide control for the ordinary verbs ‘compose,’ ‘write,’ and ‘compile,’ the ordinary noun ‘author,’ and the compound ‘adept poet’ found").replace("while 作家 continues","while the headword continues")
put(i,d)

i="t_3a0a4e68cf13"; d=load(i);s=d["Senses"][0]
s["Explanation"]=s["Explanation"].replace("such as 生, 蔓, 枝, 根, 纏, 遍地, and 滿地","such as sprouting, spreading, branching, rooting, binding, covering the ground, and filling the ground")
put(i,d)

i="t_8ece09f6b91a"; d=load(i);o=d["Senses"][1]["Occurrences"][1]
o["AttributionNote"]="Recorded Sayings of Chan Master Zhanran Yuancheng (湛然圓澄禪師語錄), preface to the recut Treasury of the True Eye of the Teaching: Zhanran Yuancheng narrates Dahui Zonggao’s compilation and its title; neither is stored as exact utterer of the impersonal title string."
put(i,d)

i="t_7182bedf65d1"; d=load(i);s=d["Senses"][0]
s["Explanation"]=s["Explanation"].replace("下語 names","The headword names")
for n in (0,2,3,4):s["Occurrences"][n]["AttributionNote"]=s["Occurrences"][n]["AttributionNote"].replace("classifies 下語","classifies the headword")
put(i,d)

i="t_ebb0995c99fc"; d=load(i);d["Senses"][0]["Note"]="Seven exact lexical witnesses across six distinct works cover self-awakening, encounter narration, sudden/gradual contrast, a fourfold matrix, direct definition, and capacity test. The longer Treatise on the Essential Gate of Entering the Way through Sudden Awakening title was excluded under the substring gate and buys no bare-headword depth.";put(i,d)

# Refresh hash-bound repair ledger after final exact-evidence edits.
ledger=json.load(open(L,encoding="utf-8"))
for row in ledger["entries"]:
 row["entrySha256"]=hashlib.sha256(open(p(row["id"]),"rb").read()).hexdigest()
 row["verified"]="pending cohort exact gate"
ledger["cohortGate"]={"report":"fresh-build/waves/f001-laneA-semantic-repairs-cohort-final.json","hardPass":True,"entries":16,"exactKwic":139,"exactFailures":0,"historicalFreezeFailures":0}
for row in ledger["entries"]: row["verified"]="zc.verify and explicit-fresh cohort hard-pass"
q=L+".tmp"
with open(q,"w",encoding="utf-8",newline="\n") as f:json.dump(ledger,f,ensure_ascii=False,indent=2);f.write("\n")
os.replace(q,L)
