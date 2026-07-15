#!/usr/bin/env python3
import copy, datetime, hashlib, json, os

BASE = os.path.dirname(os.path.dirname(os.path.dirname(__file__)))
ENTRIES = os.path.join(BASE, "fresh-build", "entries")
REVIEW = os.path.join(BASE, "fresh-build", "waves", "f001-laneA-independent-semantic-review.json")
LEDGER = os.path.join(BASE, "fresh-build", "waves", "f001-laneA-semantic-repairs.json")
UTC = "2026-07-15T04:10:00Z"

def path(i): return os.path.join(ENTRIES, i, "entry.v2.json")
def load(i):
    with open(path(i), encoding="utf-8") as f: return json.load(f)
def atomic_json(p, d):
    q=p+".tmp"
    with open(q,"w",encoding="utf-8",newline="\n") as f:
        json.dump(d,f,ensure_ascii=False,indent=2); f.write("\n")
    os.replace(q,p)
def sha(p):
    return hashlib.sha256(open(p,"rb").read()).hexdigest()
def cm(name, *roles): return {"MasterName":name,"Roles":list(roles)}
def replace_cm(o, name, roles):
    current=[x for x in o.get("ContextMasters",[]) if x.get("MasterName") != name]
    current.append(cm(name,*roles)); o["ContextMasters"]=current
def narrated(o, label, kind, evidence):
    o["MasterName"]=None
    o["ActorAttribution"]={"Status":"narrated","Kind":kind,"ActorLabel":label,"ActorRole":"compiler",
        "GrammarEvidence":evidence,"ReviewedBy":"Codex lane-A independent semantic repair; complete-case review",
        "ReviewedUtc":UTC}
def save(i,d,finding):
    atomic_json(path(i),d)
    ledger["entries"].append({"id":i,"term":d["SourceTerm"],"state":"repaired",
        "entrySha256":sha(path(i)),"findingAddressed":finding,"verified":"pending cohort exact gate"})
    ledger["completed"]=len(ledger["entries"]); ledger["updatedUtc"]=UTC; atomic_json(LEDGER,ledger)

review=json.load(open(REVIEW,encoding="utf-8"))
revises=[i for i,v in review["entries"].items() if v["verdict"]=="REVISE"]
ledger={"schemaVersion":1,"wave":"f001","lane":"A","assignment":"semantic repairs",
    "scope":revises,"completed":0,"updatedUtc":UTC,"entries":[],"cohortGate":None}
atomic_json(LEDGER,ledger)

# 末後句: a verbal final phrase and a death-performance are different things.
i="t_ab6276be6e08"; d=load(i); s=d["Senses"][0]; death=s["Occurrences"].pop(1)
s["Explanation"]=("The final phrase is a saying or verbal test claimed at the decisive end of an exchange or life. "
"Yantou Quanhuo says Xuefeng does not understand it and answers a request to identify it with ‘just this’ (只者是), after distinguishing shared birth from shared death. Tianning Xipu, Foyan Qingyuan, Chaozong Tongren, Sixin Wuxin, and Yuanwu Keqin use it in praise, criticism, first-and-last-phrase testing, and warnings that a claimed understanding still does not pass. No universally fixed sentence is supplied.")
s["Note"]="Six phrase/saying witnesses from six distinct works establish this verbal referent. The separately attested death-performance label is not merged into it."
s["SourceTexts"]=[o["RelPath"] for o in s["Occurrences"]]
d["Senses"].append({"SenseKey":None,"PreferredTarget":"the final act of dying seated or standing",
 "AlternateTargets":["death as the final phrase"],"SearchAliases":["final death act","seated or standing death"],
 "Status":"allowed","Explanation":"The same graphs also label a death-performance rather than a phrase. Zhongfeng Mingben explicitly reports that dying seated or standing is called the ‘final phrase’ (坐脫立亡喚作末後句). This one explicit witness establishes a different act-referent but does not make that later classification corpus-wide.",
 "Validation":"provisional","Note":"One explicit work; kept provisional and distinct from the verbal final phrase.",
 "Occurrences":[death],"SourceTexts":[death["RelPath"]],"RelatedMasters":["Zhongfeng Mingben"],"RelatedTerms":["坐脫","立亡"]})
save(i,d,"Split the explicit seated/standing-death referent from the phrase/saying referent.")

# 僧問: retain recorder as actor; resolve all retained respondent masters.
i="t_67bff0d0e5d3"; d=load(i); os_=d["Senses"][0]["Occurrences"]
for idx,name,extra in [(2,"Oxhead Zhiwei",[cm("Xuanting", "case-figure")]),(3,"Helin Xuansu",[]),(7,"Budai Qici",[]),(8,"Tianyi Yihuai",[])]:
    replace_cm(os_[idx],name,["respondent"]); os_[idx]["ContextMasters"] += extra
    os_[idx]["AttributionNote"] += f" Complete-case and section review identifies {name} as the respondent; the recorder, not the monk or respondent, utters the formula 僧問."
d["Senses"][0]["RelatedMasters"] += ["Oxhead Zhiwei","Xuanting","Helin Xuansu","Budai Qici","Tianyi Yihuai"]
d["Senses"][0]["Explanation"] += " The four previously orphaned respondents are Oxhead Zhiwei, Helin Xuansu, Budai Qici, and Tianyi Yihuai; they answer the questions but do not utter the recorder formula."
save(i,d,"Resolved every retained respondent while preserving compiler ownership of the recorder formula.")

# 和尚: remove compound-only 和尚子 sense and correct remaining prose/source inventories.
i="t_8f7b20536cb6"; d=load(i); d["Senses"]=d["Senses"][:2]
s=d["Senses"][0]
s["Explanation"]=("In Chan records, 和尚 is the respectful title used to address or identify a senior monastic master. Huike addresses Bodhidharma; Huang Tingjian addresses a teacher; Guishan Lingyou and Zhaozhou Congshen address their teachers; Linji Yixuan and Huangbo Xiyun speak of the old masters under heaven. Compiler narration also attaches the title to Wuzu Fayan, Budai Qici, Master An of Songshan, and Wuzu Jie. Direct address, honorific naming, and collective description retain the same senior-master referent; a narrator’s title is not an utterance by its bearer.")
s["Note"]="Ten exact witnesses from ten distinct works cover address, title, collective description, and named designation. The nested compound 和尚子 was excluded under the substring gate and supplies no bare-headword plural-vocative sense."
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
s2=d["Senses"][1]
s2["Explanation"]=("In explicit precept-reception and ordination frames, 和尚 names the preceptor from whom ordination was received. The records use 得戒和尚 as a heading, say that He Yizi received precepts from Yulin Tongxiu, ask Ruogan Heshang to serve as ordination master, and offer incense to an ordination preceptor. These office frames distinguish the ordination role from ordinary direct address.")
s2["Note"]="Four explicit office-context witnesses from four distinct works establish this narrower role. Wuzu Jie’s non-ordination title occurrence remains only in the general master sense."
save(i,d,"Removed compound contamination, preserved narrator/utterer distinctions, and cleaned ordination evidence.")

# 如何是佛: resolve every answerer while keeping unnamed monks as exact utterers.
i="t_e4d6ebff1bb2"; d=load(i); os_=d["Senses"][0]["Occurrences"]
answers={2:"Baizhang Huaihai",3:"Guyin Yuncong",4:"Baizhang Huaihai",5:"Tianyi Yihuai",7:"Zhu'an Shigui"}
for idx,name in answers.items():
    replace_cm(os_[idx],name,["respondent"])
    os_[idx]["AttributionNote"] += f" Full section and case review identifies {name} as respondent; the unnamed monk alone utters the headword question."
d["Senses"][0]["RelatedMasters"] += list(dict.fromkeys(answers.values()))
d["Senses"][0]["Explanation"]=("This direct public-interview question asks ‘What is Buddha?’ The question remains stable while named respondents answer differently. Mazu Daoyi answers Damei Fachang with ‘this very mind is Buddha’; Xuefeng Yicun cries ‘blue sky’; Baizhang Huaihai asks the monk’s identity; Guyin Yuncong names Qiongzhou’s nine-jointed staffs; Tianyi Yihuai says ‘spread hair over mud, lay the body across the ground’; Dongshan Shouchu says ‘three pounds of flax’; and Zhu'an Shigui names the stone tortoise at Huayang cave. The recurring question belongs to its named or reviewed-unnamed questioner, never automatically to the respondent, and no one answer becomes the definition of the question.")
save(i,d,"Resolved all retained responding masters and named them in the prose without changing questioner ownership.")

# 序: headings are metadata; audit all rows and make provenance explicit.
i="t_6f47a97d45b0"; d=load(i); s=d["Senses"][0]
s["Explanation"]=("序 names a preface: prose placed before a record, collection, or scripture to introduce its compilation, transmission, author, or publication. The corpus supplies repeated title headings, named preface writers, first-person prefaces, and requests that someone compose a preface. Xu Fu, Xiong Kaiyuan, Yang Yi, Yunqi Zhuhong, Yongjue Yuanxian, and Sanfeng Hanyue Fazang are established passage by passage as writers or speakers; the repeated Yongjue and Linquan headings are impersonal metadata. A record owner named in a heading is contextual and is not thereby the utterer of 序.")
s["Note"]="Eight exact witnesses across six distinct works. All heading rows were re-audited: metadata has impersonal actor status; named prose or requests retain their actual writer/speaker."
save(i,d,"Re-audited all heading rows and stated the metadata-versus-author distinction explicitly.")

# 金鎖玄路: modifier study changes material-looking English and records controls.
i="t_5ddde30711a4"; d=load(i); s=d["Senses"][0]
s["PreferredTarget"]="the gold-lock barrier on the dark road"
s["AlternateTargets"]=["gold-lock obstruction on the hidden road"]
s["SearchAliases"]=["gold lock dark road","gold lock hidden road","golden lock dark road","golden lock hidden road"]
s["Explanation"]=("The gold-lock compound names an obstruction on the dark or hidden road and is repeatedly tied to ordinary feelings and holy views. Caoshan Benji equates those feelings and views with 金鎖玄路 and calls for reciprocal interpenetration; Yongjue Yuanxian repeats the equation and says earlier sages winnowed it. A Dongshan-linked verse places it beside intertwined light and dark, difficulty after merit is complete, and exhausted advance and retreat. An unnamed monk asks for it and Langting Ting answers ‘this side, that side.’ Gold modifies the lock-name here; none of the four passages describes a lock made from metal.")
s["Note"]="Modifier-control audit: exact 金鎖 has 397 hits/164 files and 黃金鎖 49/41 in the frozen corpus. Controls include barrier, gate, lock, opening, and restraint frames, not descriptions of manufacture, weight, possession, or precious-metal hardware. The entry therefore keeps gold as a lexical modifier but avoids the materially misleading English ‘golden lock.’ Four headword witnesses from four distinct works support the compound."
save(i,d,"Completed 金鎖/黃金鎖 modifier controls and replaced materially misleading English.")

# 上堂: merge the spurious physical-action sense into the event sense.
i="t_4f7bd98ad40f"; d=load(i); s=d["Senses"][0]; physical=d["Senses"][1]["Occurrences"][0]
s["Occurrences"].append(physical); s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
s["Explanation"]=("A formal teaching-hall address or observance, conventionally headed ‘ascend the hall’ (上堂). Chan turns the ordinary ascent phrase into the standard name of the abbot’s public address and its recorded discourse. It is requested by officials or command, scheduled in monastic codes, announced by placard, delegated to a head monk holding the whisk, suspended and resumed, or introduced by occasion headings. Linji Yixuan’s record separately says officials requested 升座 and then records 師上堂云: 升座 supplies the physical seat-taking while 上堂 introduces the same formal address event. Headings, requests, scheduling, and verbal narration therefore remain one institutional referent.")
s["Note"]="One formal hall-address sense. The former physical split was rejected because its sole line uses separate 升座 for taking the seat and 上堂 for the address. Thirteen witnesses represent eleven distinct works."
d["Senses"]=[s]
save(i,d,"Merged the unsupported physical-action split into the formal hall-address event.")

# Utility for adding an exact, fully reviewed occurrence.
def add_occ(s, rel, lb, kwic, master, note):
    s["Occurrences"].append({"RelPath":rel,"FromLb":lb,"ToLb":lb,"Kwic":kwic,"MasterName":master,
      "ContextMasters":[cm(master,"utterer")],"Curated":True,"AttributionNote":note})
    if rel not in s["SourceTexts"]: s["SourceTexts"].append(rel)
    if master not in s["RelatedMasters"]: s["RelatedMasters"].append(master)

# 無事: broaden beyond the Linji/Huangbo cluster with independent later deployments.
i="t_f6dadadcbef5"; d=load(i); s=d["Senses"][0]
add_occ(s,"X/X69/X69n1357.xml","0458b06","到無為無事大達之場，乃為種草。","Yuanwu Keqin","Essentials of Mind by Chan Master Foguo Keqin (佛果克勤禪師心要): Yuanwu Keqin directly places the phrase in a statement about reaching an open field; full section review confirms his turn.")
add_occ(s,"T/T47/T47n1998A.xml","0821b05","喚作無事人。更須知有向上一竅在。師云。潑油救火渾閑事。","Dahui Zonggao","Recorded Sayings of Chan Master Dahui Pujue (大慧普覺禪師語錄): Dahui Zonggao quotes the designation and immediately comments on it in his own hall turn.")
add_occ(s,"X/X72/X72n1437.xml","0398b01","汝但無事於心，無心於事則虗而靈、寂而玅。","Yongjue Yuanxian","Extended Record of Chan Master Yongjue Yuanxian (永覺元賢禪師廣錄): Yongjue Yuanxian quotes Deshan’s paired wording in his own address; the exact headword occurs in Yongjue’s quoted evidence and is attributed to the quoted master in prose.")
s["Explanation"] += " Later independent records broaden the evidence: Yuanwu Keqin names an open field of nonaction and nothing-to-do, Dahui Zonggao quotes the ‘person with nothing to do’ only to demand an upward aperture beyond it, and Yongjue Yuanxian preserves the paired saying ‘nothing to do in the mind, no mind in affairs.’ These later uses confirm that the term can be asserted, qualified, and quoted rather than functioning only as an early Linji-school slogan."
s["Note"]="Eleven exact witnesses from seven distinct works now cover early formulas, ordinary activity, later quotation, criticism/inversion, investigation, and institutional address."
save(i,d,"Expanded a high-frequency keystone from four to seven distinct works and re-tested the opening.")

# 作家: add later independent adept/real-hand witnesses and retain literal-author falsification result.
i="t_dab856504b69"; d=load(i); s=d["Senses"][0]
add_occ(s,"X/X69/X69n1357.xml","0455c02","若無本分作家手段，未免賺悞方來。","Yuanwu Keqin","Essentials of Mind by Chan Master Foguo Keqin (佛果克勤禪師心要): Yuanwu Keqin requires the real hand’s means in an institutional teaching-seat discussion.")
add_occ(s,"L/L158/L158n1652.xml","0017b06","師云莊宗作家君王興化明眼宗師。","Mingjue Cong","Recorded Sayings of Chan Master Mingjue Cong (明覺聰禪師語錄): complete-case review identifies Mingjue Cong as the utterer who calls Emperor Zhuangzong an adept ruler.")
s["Explanation"] += " Independent later witnesses apply it to the means required of a teaching-seat master and even to Emperor Zhuangzong as an ‘adept ruler’; the competence follows the supplied role. A corpus-wide control for 撰, 著, 編, 作者, and 作家詩客 found author/composer grammar named by other words, while 作家 continues to predicate demonstrated capability."
s["Note"]="Ten exact witnesses across six distinct works. Occupational/author controls were actively tested; no curated line requires modern ‘author/writer’ as the bare-headword referent."
save(i,d,"Broadened source spread and recorded the literal author/maker falsification.")

# 葛藤: retain one loaded sense only after explicit literal-plant controls.
i="t_3a0a4e68cf13"; d=load(i); s=d["Senses"][0]
s["Explanation"] += " A literal-plant control searched plant predicates and collocations such as 生, 蔓, 枝, 根, 纏, 遍地, and 滿地. In the retained Chan uses these extend the same speech-and-case tangle—lineage talk sprouts, sayings branch, cases cover the ground, or words bind a participant—rather than describing cultivated or botanical vines. The selected evidence therefore supports one loaded tangle referent, not a hidden second plant article."
s["Note"]="Literal vegetation/concrete-entanglement controls were run across all 2,876 exact hits. Apparent plant grammar repeatedly governs discourse, sayings, cases, or lineage proliferation; no independently active botanical referent survived for this entry. Eight witnesses from five distinct works remain sufficient for the loaded sense."
save(i,d,"Ran and documented mandatory literal vegetation controls; no second thing survived.")

# 正法眼藏: polished title target and heading actor.
i="t_8ece09f6b91a"; d=load(i); s=d["Senses"][1]
s["PreferredTarget"]="the book Treasury of the True Eye of the Teaching"
s["AlternateTargets"]=["Treasury of the True Eye of the Teaching (book)"]
o=s["Occurrences"][1]; narrated(o,"the preface writer","preface narrative","Zhanran Yuancheng narrates that Dahui compiled and titled the book; the title itself is not an utterance by Zhanran or Dahui.")
o["ContextMasters"]=[cm("Zhanran Yuancheng","compiler"),cm("Dahui Zonggao","person-described")]
o["AttributionNote"]="Preface to the recut Treasury of the True Eye of the Teaching (重刻正法眼藏序): Zhanran Yuancheng narrates Dahui Zonggao’s compilation and its title; neither is stored as exact utterer of the impersonal title string."
save(i,d,"Made title target independently readable and corrected the narrated title occurrence.")

# 下語: compiler owns narrator-governed verbs; participants remain contextual.
i="t_7182bedf65d1"; d=load(i); s=d["Senses"][0]; os_=s["Occurrences"]
for idx,label,evidence in [(0,"the compiler","The compiler narrates that the student assembly submitted responses."),(2,"the compiler","The compiler narrates that many people submitted responses; the participants do not utter the recorder-governed verb."),(3,"the compiler","The compiler narrates Yulin ordering the assembly and the assembly completing responses."),(4,"the compiler","The compiler narrates that an unnamed senior monk submitted ninety-six responses.")]:
    narrated(os_[idx],label,"compiler narrative",evidence)
for idx in [0,2,3,4]: os_[idx]["AttributionNote"] += " Exact-actor repair classifies 下語 as narrator-governed; response participants remain contextual non-master actors."
o=os_[6]; replace_cm(o,"Nanyue Huairang",["person-described"]); o["AttributionNote"] += " Nanyue Huairang is the person described, not the utterer of the compiler’s narrative verb."
s["Explanation"]=("下語 names the act and product of submitting an on-the-spot response for judgment on a case or challenge. Compiler narration records assemblies, many people, or a senior monk submitting responses; those participants perform the response but do not utter the recorder-governed verb. Langye Huijue gives his own final response, while other records narrate failed group responses, ordered responses, ninety-six unsuccessful attempts, and Nanyue Huairang’s eventual answer ‘to call it a thing misses.’ Acceptance, rejection, replacement, and renewed challenge concern one public response operation rather than separate noun and verb senses.")
save(i,d,"Corrected four narrator-governed rows and Nanyue’s person-described role.")

# 頓悟: remove book-title substring contamination from lexical depth.
i="t_ebb0995c99fc"; d=load(i); s=d["Senses"][0]; removed=s["Occurrences"].pop(6)
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
s["RelatedMasters"]=[x for x in s["RelatedMasters"] if x!="Dazhu Huihai"]
s["Explanation"]=s["Explanation"].replace(" A biography names Dazhu Huihai's Treatise on the Essential Gate of Entering the Way through Sudden Awakening.","")
s["Note"]="Seven exact lexical witnesses across six distinct works cover self-awakening, encounter narration, sudden/gradual contrast, a fourfold matrix, direct definition, and capacity test. The longer book title 頓悟入道要門論 was excluded under the substring gate and buys no bare-headword depth."
save(i,d,"Removed longer-title contamination from bare-headword depth and prose.")

# 向上一路: remove parallel duplicate and replace it with a distinct later institutional deployment.
i="t_1e41b014d80e"; d=load(i); s=d["Senses"][0]; s["Occurrences"].pop(1)
add_occ(s,"J/J34/J34nB311.xml","0596a04","須知向上一路尊貴自別。","Juelang Daosheng","Complete Record of Chan Master Juelang Daosheng (天界覺浪盛禪師全錄): Juelang Daosheng distinguishes the upward road as separately honored after a three-sentence classification.")
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
s["Explanation"]=s["Explanation"].replace(" A parallel lamp version preserves the same declaration.","") + " Juelang Daosheng later distinguishes the road as separately honored after a three-sentence classification, adding an institutional deployment rather than another copy of Panshan’s line."
s["Note"]="Seven exact witnesses from six distinct works. The duplicate Panshan parallel was retained only as attribution support outside curated depth and replaced by Juelang Daosheng’s distinct deployment."
save(i,d,"Replaced a parallel duplicate with a distinct deployment from another work.")

# 本分事: canonical roster spelling.
i="t_62044e7bbb87"; d=load(i)
raw=json.dumps(d,ensure_ascii=False).replace("Nansen Puyuan","Nanquan Puyuan")
d=json.loads(raw)
save(i,d,"Corrected Nansen to roster-canonical Nanquan everywhere in the entry.")

# 乾屎橛: obey controlling calibration without inventing construction.
i="t_ba841f6e11c8"; d=load(i); s=d["Senses"][0]
s["Explanation"]=("A dry shit-stick is the latrine wiping-tool named by the compound. Linji Yixuan asks what dry shit-stick the person of no rank is; Deshan Xuanjian predicates it of old Shakyamuni; and Yunmen Wenyan asks what dry shit-stick an assembly seeks to chew. In Yunmen’s famous case it answers ‘What is Buddha?’ Wumen Huikai preserves Yunmen as the answerer, Dahui Zonggao repeats the case in verse, and Zhongfeng Mingben places it beside the courtyard cypress and three catties of hemp as sayings with no route for explanatory boring. Wanpu Zhao later asks Miyun Yuanwu what it is and receives only ‘Look on the field ridge.’ The records deploy the named wiping-tool in questions, predicates, answers, case titles, and commentary; they assign it no further stable gloss, and neither does this entry.")
s["Note"]="Compound/object control follows the guide’s calibration: dry + excrement + stick names the latrine wiping-tool, while the corpus does not warrant claims about a particular material construction or a menu of symbolic readings. Seven exact witnesses from seven distinct works."
save(i,d,"Reconciled the object gloss with the controlling dry-shit-stick calibration.")

assert ledger["completed"]==16
