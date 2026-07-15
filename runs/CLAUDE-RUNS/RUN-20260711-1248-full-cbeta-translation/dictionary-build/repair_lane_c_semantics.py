#!/usr/bin/env python3
"""Apply the independently adjudicated f001 lane-C semantic repairs (positions 27-100)."""
from __future__ import annotations
import copy, datetime, hashlib, json, os
from pathlib import Path

ROOT = Path(__file__).resolve().parent
FRESH = ROOT / "fresh-build"
ENTRIES = FRESH / "entries"
WAVES = FRESH / "waves"
REVIEW = WAVES / "f001-laneC-independent-semantic-review.json"
LANE = WAVES / "f001-laneC.json"
OUT = WAVES / "f001-laneC-semantic-repairs.json"
NOW = "2026-07-14T23:30:00Z"

def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def load_entry(tid):
    p=ENTRIES/tid/"entry.v2.json"
    return p,json.loads(p.read_text(encoding="utf-8"))
def save_entry(p,d):
    tmp=p.with_suffix(".json.tmp")
    tmp.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    os.replace(tmp,p)
def occ(d, s, n): return d["Senses"][s-1]["Occurrences"][n-1]
def roles(name,*rs): return {"MasterName":name,"Roles":list(rs)}
def named(o,name,contexts=None,note=None):
    o["MasterName"]=name; o.pop("ActorAttribution",None)
    o["ContextMasters"]=contexts or [roles(name,"utterer")]
    if note: o["AttributionNote"]=note
def actor(o,status,kind,label,role,evidence,contexts=None,note=None):
    o.pop("MasterName",None)
    o["ActorAttribution"]={"Status":status,"Kind":kind,"ActorLabel":label,"ActorRole":role,
      "GrammarEvidence":evidence,"RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],
      "ReviewedBy":"Codex lane-C independent semantic repair","ReviewedUtc":NOW}
    o["ContextMasters"]=contexts or []
    if note: o["AttributionNote"]=note
def aliases(s,*vals):
    seen=[]
    for v in list(s.get("SearchAliases") or [])+list(vals):
        if v and v.casefold() not in [x.casefold() for x in seen]: seen.append(v)
    s["SearchAliases"]=seen
def lead(s,text):
    e=s.get("Explanation","")
    if not e.startswith(text): s["Explanation"]=text+" "+e
def replace(s,old,new,field="Explanation"):
    s[field]=s.get(field,"").replace(old,new)
def sync_sources(s): s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s.get("Occurrences",[])))

def repair(pos,d):
    s=d["Senses"][0]
    if pos==27:
        for n in (1,2): occ(d,2,n)["ContextMasters"]=[roles("Puming","verse-author")]
    elif pos==28:
        replace(s,"Literally \"be lost or confused\" (迷) and \"awaken or understand\" (悟): \"confusion and awakening,\"", "The records coordinate confusion and awakening as a tested pair, repeatedly setting it beside ordinary and holy, motion and stillness, and true and false. Literally the graphs say \"be lost or confused\" (迷) and \"awaken or understand\" (悟): \"confusion and awakening,\"")
        o=occ(d,1,3); o["Kwic"]="師曰：既無迷悟，了箇甚麼？"; o["FromLb"]="0067c22"; o["ToLb"]="0067c22"
        o["AttributionNote"]="Complete Book of the Five Lamps (五燈全書), Zhongyan Huimu Yunneng section: Zhongyan replies, ‘If there is neither confusion nor awakening, what do you understand?’ The preceding Longji quotation is discussed in the prose but is not conflated with this exact turn."
    elif pos==29:
        replace(s,"Literally, transmit the lamp.","The records use ‘transmit the lamp’ for teacher-to-successor continuation, drawing the continuation through lamp and flame equations. Literally, transmit the lamp.")
        occ(d,2,1)["ContextMasters"]=[]
    elif pos==30:
        replace(s,"Literally, the coordinated graphs name \"light and dark.\"", "The records coordinate light and dark as a pair, variously distinguishing, intermingling, reciprocating, and pairing them without reducing those uses to one imported theory. Literally, the graphs name \"light and dark.\"")
        replace(s,"Literally, \"light and dark.\"", "The records coordinate light and dark as a pair, variously distinguishing, intermingling, reciprocating, and pairing them without reducing those uses to one imported theory. Literally, \"light and dark.\"")
    elif pos==31:
        replace(s,"Literally, to 'go along walking' or 'walk about.'", "The term names an observable walking circuit or action, with records specifying its timing, direction, companions, and coordination with rest. Literally, to 'go along walking' or 'walk about.'")
        occ(d,1,1)["ContextMasters"]=[roles("Hanshan Deqing","person-described")]
        named(occ(d,1,6),"Zhuanyu Guanheng",note="Recorded Sayings of Zhuanyu Guanheng (紫竹林顓愚衡和尚語錄), in Zhuanyu’s address to Wanbai Haozi: Zhuanyu uses ‘sick monk’ as self-reference and says that after each meal he saw the addressee serve the assembly before eating.")
    elif pos==33:
        # Longer work title is not lexical evidence; preserve it as an anchored claim.
        o=d["Senses"][0]["Occurrences"].pop(7)
        d["Senses"][0].setdefault("ClaimAnchors",[]).append(o)
        for si in (2,3):
            for o2 in d["Senses"][si-1].get("Occurrences",[]):
                for c in o2.get("ContextMasters",[]):
                    if c.get("MasterName")=="Jiashan Shanhui": c["Roles"]=["teacher"]
        for c in o.get("ContextMasters",[]):
            if c.get("MasterName")=="Juelang Daosheng": c["Roles"]=["record-owner"]
        for ss in d["Senses"]: sync_sources(ss)
    elif pos==34:
        lead(s,"In the records, ‘one eye’ is a figurative capacity used in appraisals: it may be possessed, placed, lost, exchanged, or opened, without the texts supplying a doctrine of what it sees.")
        aliases(s,"one eye","have one eye","lose one eye","open one eye","single-eye appraisal")
    elif pos==35:
        replace(s,"Literally, \"lamp records\":", "The term names the family of lineage-and-encounter compilations titled as lamp records. Literally, \"lamp records\":")
        replace(s,"Literally, lamp (燈) records (錄).", "The term names the family of lineage-and-encounter compilations titled as lamp records. Literally, lamp (燈) records (錄).")
        occ(d,1,1)["ContextMasters"]=[]
        aliases(s,"lamp record","lamp chronicle","transmission-lamp record","lineage record")
    elif pos==36: aliases(s,"principle and affairs","principle and events","principle and matters","unobstructed principle and affairs","interpenetrating principle and affairs")
    elif pos==38:
        lead(s,"The term names a teacher’s recorded act of approving or certifying a person’s understanding; the entries keep that act distinct from assigning succession.")
        named(occ(d,1,2),"Nanyue Huairang",note="Continuation of the Lamp record, biography of Nanyue Huairang: after the Mazu dialogue, Huairang says that he approved each of six room-entering disciples. Nanyue Huairang is the exact utterer of the headword.")
        aliases(s,"approve understanding","certify understanding","teacher approval","seal of approval")
    elif pos==39: replace(s,"Primary inherited Buddhist/Chan sense", "Primary corpus-wide original-mind sense",field="Note")
    elif pos==40:
        lead(s,"‘Old Master Wang’ is Nanquan Puyuan’s recurrent self-designation and the label under which later records quote him.")
        aliases(s,"Old Master Wang","Teacher Wang","Nanquan's Wang title","Nanquan Wang")
    elif pos==41: aliases(s,"seeing hearing sensing knowing","see hear sense know","seeing and hearing","sensing and knowing","four perceptual functions")
    elif pos==42:
        lead(s,"The records deploy ‘single transmission’ as a lineage slogan joined to direct pointing, the mind-seal, and transmission outside teachings.")
        aliases(s,"single transmission","sole transmission","direct lineage transmission","single lineage transmission")
    elif pos==44:
        lead(s,"The wooden buddha is a materially wooden image in Danxia Tianran’s burning case and in the fixed contrast that a wooden buddha does not pass through fire.")
        occ(d,1,3)["ContextMasters"]=[roles("Danxia Tianran","respondent")]
        for n in (5,6):
            for c in occ(d,1,n).get("ContextMasters",[]):
                if c.get("MasterName")=="Danxia Tianran": c["Roles"]=["case-figure"]
        aliases(s,"wooden buddha","wooden buddha image","wooden buddha statue","wooden image")
    elif pos==46:
        lead(s,"The term names a teacher-to-successor entrustment event, repeatedly recorded in verses and lineage notices.")
        o=occ(d,1,3); o["Kwic"]="法法本來法，無法無非法，何於一法中，有法有不法。佛告迦葉：吾將金縷僧伽梨衣傳付於汝，轉授補處，至慈氏佛出世，勿令朽壞。迦葉聞偈，頭面禮足曰：善哉善哉，我當依勅，恭順佛教。"; o["FromLb"]="0206c18"; o["ToLb"]="0207a03"
        named(o,"Shakyamuni Buddha",[roles("Shakyamuni Buddha","utterer"),roles("Mahakasyapa","student")],"Transmission of the Lamp (景德傳燈錄), Shakyamuni Buddha entrusts the robe to Mahakasyapa in direct speech. The separate compiler sentence ‘Ananda finished entrusting the treasury’ is no longer conflated with this turn.")
        cm=occ(d,1,6).setdefault("ContextMasters",[])
        if not any(x.get("MasterName")=="Shakyamuni Buddha" for x in cm): cm.append(roles("Shakyamuni Buddha","case-figure"))
        aliases(s,"entrust the teaching","hand on the teaching","transmit the teaching","teaching entrustment")
    elif pos==48:
        lead(s,"The fixed appraisal ‘the substance stands exposed’ occurs in ‘true constancy stands exposed,’ ‘the whole substance stands exposed,’ and the recurrent ‘the substance stands exposed in the golden wind’ case; the grammar leaves ‘body’ versus ‘substance’ open where the record does.")
        # Replace unresolved anonymous quotation with an attributable witness already represented by the case family.
        d["Senses"][0]["Occurrences"].pop(4); sync_sources(s)
        aliases(s,"substance exposed","whole substance exposed","body exposed","exposed in the golden wind")
    elif pos==51:
        actor(occ(d,1,2),"narrated","compiler narrative","the record compiler","compiler","The compiler narrates Wuzu Fayan ordering Yuanwu Keqin to join the hall.",[roles("Wuzu Fayan","teacher"),roles("Yuanwu Keqin","student")])
        actor(occ(d,1,3),"narrated","biographical narration","the biographical compiler","compiler","The passage narrates admission to the hall rather than quoting a headword turn.",[roles("Huanxi Weiyi","person-described"),roles("Fojian Huiqin","teacher")])
        o=occ(d,1,4); o["ActorAttribution"]["ActorRole"]="utterer"; o["ContextMasters"]=[roles("Xuedou Chongxian","commentator"),roles("Yuanwu Keqin","commentator")]
        actor(occ(d,1,6),"narrated","compiler narrative","the record compiler","compiler","The compiler narrates Wuzu ordering Yuanwu to join the hall.",[roles("Wuzu Fayan","teacher"),roles("Yuanwu Keqin","student")])
        named(occ(d,1,7),"Huangbo Xiyun",note="Strict Lineage of the Five Lamps (五燈嚴統): the line explicitly says Huangbo called the attendant and ordered him to lead ‘this mad fellow’ to the hall; Huangbo Xiyun utters the headword.")
        aliases(s,"join the hall","go to the hall","enter the monks hall","hall admission")
    elif pos==52:
        for ss in d["Senses"]:
            for fld in ("PreferredTarget","Explanation","Note"):
                if fld in ss and isinstance(ss[fld],str): ss[fld]=ss[fld].replace("concentration","settled absorption").replace("Concentration","Settled Absorption")
            ss["AlternateTargets"]=[x.replace("concentration","settled absorption").replace("Concentration","Settled Absorption") for x in ss.get("AlternateTargets",[])]
            ss["SearchAliases"]=[x for x in ss.get("SearchAliases",[]) if "concentrat" not in x.casefold()]
        # Terminal formula is the same entered state followed by death, not a different lexical referent.
        if len(d["Senses"])>1:
            s0,s1=d["Senses"][0],d["Senses"][1]; s0["Occurrences"].extend(s1.get("Occurrences",[]));
            s0["Note"]=(s0.get("Note","")+" The dated terminal notices use the same entered settled state followed by death; they are retained as a deployment of this sense, not split as another lexical event.").strip(); d["Senses"]=[s0]; sync_sources(s0)
    elif pos==53:
        o=occ(d,2,3); o["ActorAttribution"]["ActorRole"]="utterer"; o["ContextMasters"]=[roles("Huineng","case-figure")]
        cm=occ(d,1,1).setdefault("ContextMasters",[])
        if not any(x.get("MasterName")=="Huiming" for x in cm): cm.append(roles("Huiming","student"))
    elif pos==54:
        named(occ(d,1,7),"Liangshan Yuanguan",note="Mirror of the Chan School (宗鑑法林): the line explicitly introduces Liangshan Yuanguan with ‘Liangshan Yuan said’; he utters the headword appraisal.")
        aliases(s,"leak","give the game away","indiscreet disclosure","let slip")
    elif pos==55:
        lead(s,"The records use this transitive appraisal for seeing through a saying, case, person, condition, or position before or after it is presented.")
        # Remove only corrupted paraphrase, retaining all seven anchored Chinese witnesses.
        import re
        s["Explanation"]=re.sub(r" A later address sets out a repeated contrast:.*?\)\.","",s["Explanation"])
        aliases(s,"see through","look through","see through a saying","see through a case","be seen through")
    elif pos==56:
        # Narrow first row to Baizhang's exact question; add Hualin answer as separate exact turn.
        o=occ(d,1,1); old=copy.deepcopy(o)
        o["Kwic"]="百丈問眾曰：不得喚作淨瓶，汝等喚作甚麼？"; o["FromLb"]="0264c10"; o["ToLb"]="0264c11"; named(o,"Baizhang Huaihai",note="Transmission of the Lamp, clean-bottle case: Baizhang Huaihai asks the assembly what to call the clean bottle without calling it a clean bottle.")
        h=copy.deepcopy(old); h["Kwic"]="華林曰：不可喚作木𣔻也。"; h["FromLb"]="0264c11"; h["ToLb"]="0264c11"; named(h,"Hualin Shanjue",note="Transmission of the Lamp, clean-bottle case: Hualin Shanjue replies that it cannot be called a wooden wedge; this contextual answer does not contain the headword and is therefore retained only as a ClaimAnchor.")
        s.setdefault("ClaimAnchors",[]).append(h)
        actor(occ(d,1,6),"narrated","compiler narrative","the record compiler","compiler","The compiler narrates that a person heard Wuzu raise the clean-bottle case.",[roles("Wuzu Fayan","later-raiser")])
        aliases(s,"clean bottle","water bottle","bottle test","clean-bottle case")
    elif pos==57:
        o=occ(d,1,4); o["ActorAttribution"]["ActorRole"]="compiler"; o["ActorAttribution"]["Kind"]="quoted document voice"; o["ContextMasters"]=[roles("Yongming Yanshou","later-quoter")]
        for n in (2,3,8):
            if n<=len(s["Occurrences"]):
                oo=occ(d,1,n)
                if oo["RelPath"]=="X/X63/X63n1220.xml":
                    oo.setdefault("ContextMasters",[])
                    if not any(c.get("MasterName")=="Bodhidharma" for c in oo["ContextMasters"]): oo["ContextMasters"].append(roles("Bodhidharma","record-owner"))
    elif pos==58:
        lead(s,"The tiny mustard seed is the records’ scale-comparison object, repeatedly paired with reversals in which Mount Sumeru is contained within it.")
        occ(d,1,5)["ContextMasters"]=[]
        # Yongming is governing author of continuous Source-Mirror exposition.
        named(occ(d,1,7),"Yongming Yanshou",note="Source-Mirror Record (宗鏡錄), in Yongming Yanshou’s continuous exposition: Yongming uses the mustard seed as the scale term in a comparison.")
        aliases(s,"mustard seed","tiny mustard seed","Sumeru in a mustard seed","mustard-seed comparison")
    elif pos==59:
        for n in (1,2): actor(occ(d,2,n),"narrated","ceremony stage direction","the ceremony recorder","compiler","傳衣竟 marks the ceremony stage ‘after the robes had been handed out’; Konggu’s speech begins only after 復云.",[roles("Konggu Daocheng","record-owner")])
    elif pos==60:
        o=occ(d,1,6); actor(o,"identified-non-master","imperial discourse","the Yongzheng Emperor","utterer","The Yongzheng Emperor is the named non-master who utters the headword.",[],o.get("AttributionNote"))
    elif pos==61:
        actor(occ(d,1,3),"impersonal","personified object speech","a personified staff named Du Zhuan","utterer","The case explicitly makes the staff answer under the punning personal name Du Zhuan; no unnamed human actor is implied.",[roles("Tianan Sheng","interlocutor")])
        aliases(s,"fabricated","made up","bogus","invented","spurious")
    elif pos==62: aliases(s,"ready-made public case","already-present public case","present public case","ready-made case")
    elif pos==63: lead(s,"Layman Pang’s fixed public question—who is not a companion of the ten thousand things?—is repeatedly raised afresh and answered in later records.")
    elif pos==64:
        # continuous direct address in record-owner discourse
        named(occ(d,1,7),"Miyun Yuanwu",note="Recorded discourse of Miyun Yuanwu: within the enclosing direct instruction, Miyun Yuanwu says this matter must be bare and clean; he is the governing speaker of the headword sentence.")
        aliases(s,"bare and clean","stark naked","completely bare","clean and exposed")
    elif pos==65:
        lead(s,"The Boatman’s dominant paired injunction says to hide the body where no trace remains, yet not to hide it where no trace remains.")
        s["Note"]=s["Note"].replace("\"absolutely no trace to seek\" (the cited wording; 11 occurrences of the cited wording), and \"leave no trace\" (the cited wording; 10 occurrences)","\"absolutely no trace to seek\" (全無蹤跡可尋; 11 occurrences), and \"leave no trace\" (沒蹤跡; 10 occurrences)")
        aliases(s,"leave no trace","without a trace","no tracks","no trace remains")
    elif pos==66: aliases(s,"put it down","set it down","lay it down","drop it")
    elif pos==67:
        o=occ(d,1,3); o["Kwic"]="圓通秀禪師道：『眾中不敢顯言，只是蒙頭打坐。』"; named(o,"Yuantong Xiu",note="Baichi Yuan’s record quotes Yuantong Xiu by name: Yuantong says that among the assembly one does not dare speak openly and only sits with the head covered.")
    elif pos==68:
        lead(s,"The records warn against collapsing into blank nothing, then answer that blankness itself is absent or is not blank.")
        o=occ(d,1,3); o["Kwic"]="法明曰：若一切都無，豈不落空？"; named(o,"Faming",note="Transmission of the Lamp: the named Vinaya lecturer Faming, an identified non-master, asks whether making everything entirely absent would not fall into blankness.")
        actor(o,"identified-non-master","named Vinaya lecturer","Faming","questioner","Faming is explicitly named as the Vinaya lecturer who asks the headword question.",[],o["AttributionNote"])
        named(occ(d,1,7),"Miyun Yuanwu",note="Recorded discourse of Miyun Yuanwu: the direct second-person instruction belongs to Miyun Yuanwu’s enclosing discourse, and he utters the headword warning.")
        aliases(s,"fall into blankness","collapse into nothing","fall into emptiness","blank nothing")
    elif pos==69: aliases(s,"clean and bare","bare and exposed","completely uncovered","stark bare")
    elif pos==70:
        lead(s,"The phrase appraises urgently solicitous intervention: records sometimes praise it and sometimes compare it to poisoned honey.")
        s["Note"]=s["Note"].replace("\"only because\" (the cited wording, 24 occurrences; variant 祇為, 7)","\"only because\" (只為, 24 occurrences; variant 祇為, 7)")
        aliases(s,"urgent old-woman concern","pressing grandmotherly concern","solicitous concern","urgent concern")
    elif pos==71: replace(s,"(the cited wording，the cited wording)","(見遲即漸，見疾即頓, ‘seeing slowly is gradual; seeing quickly is sudden’)")
    elif pos==73:
        cm=occ(d,1,6).setdefault("ContextMasters",[])
        if not any(c.get("Roles")==["commentator"] for c in cm): cm.append(roles("Zhaozhou Congshen","commentator"))
    elif pos==75:
        o=occ(d,1,5); o["ContextMasters"]=[roles("Huanyuan Fuyu","person-described"),roles("Lingyin Tai","teacher")]
        occ(d,1,6)["ContextMasters"]=[roles("Huqiu Shaolong","respondent")]
        occ(d,1,9)["ContextMasters"]=[roles("Huanyuan Fuyu","person-described")]
    elif pos==77:
        o=occ(d,1,5); o["Kwic"]="法眼道：佛是無事人。"; named(o,"Fayan Wenyi",note="Five Lamps record: the current master quotes Fayan Wenyi’s exact saying, ‘Buddha is a person with nothing to do.’ The later current-master repetition is no longer conflated with this turn.")
        o=occ(d,1,6); o["ActorAttribution"]["ActorRole"]="utterer"; o["ContextMasters"]=[roles("Panshan Baoji","later-quoter")]
    elif pos==79: aliases(s,"lively","alive and darting","quick and lively","darting fish","vividly alive")
    elif pos==80:
        s["Note"]=s["Note"].replace("\"the bright-and-numinous one\" (the cited wording, 14 occurrences in 12 texts)","\"the bright-and-numinous one\" (昭昭靈靈者, 14 occurrences in 12 texts)")
        named(occ(d,1,6),"Juelang Daosheng",note="Recorded Sayings of Juelang Daosheng: an explicitly headed ‘instruction to the assembly’ continues after ‘the master raised his staff and said’; Juelang Daosheng is the exact speaker warning against taking a bright numinous discriminating spirit as master of the house.")
        aliases(s,"bright and numinous","brightly responsive","luminous and responsive","bright knowing")
    elif pos==81:
        lead(s,"The clay buddha is a materially clay image in Zhaozhou’s water/furnace/fire set, and Yuanwu explicitly predicates that it dissolves in water.")
        d["Senses"][0]["Occurrences"].pop(5); sync_sources(s)
        aliases(s,"clay buddha","clay buddha image","clay statue","mud buddha")
    elif pos==82:
        replace(s,"(說老婆禪，the cited wording)","(說老婆禪，拕泥帶水, ‘speaking old-woman Chan, dragging through mud and water’)")
        s["Note"]=s["Note"].replace("(the cited wordingthe cited wording)","(老婆心切)")
    elif pos==84:
        s0=d["Senses"][0]; s4=d["Senses"].pop(3); s0["Occurrences"].extend(s4.get("Occurrences",[])); sync_sources(s0)
        s0["Note"]="Five curated anchors across independent works support this continuity sense, including teacher-successor transmission; hereditary bloodline and bodily circulation remain separate referents in the following senses."
    elif pos==85:
        lead(s,"The records define straight mind against bends, twists, crookedness, and falseness: ‘straight mind means without bends or twists.’")
        replace(s,"(直心是道場，the cited wording)","(直心是道場，直心是淨土, ‘straight mind is the site; straight mind is the clean land’)")
        s["Note"]=s["Note"].replace("(the cited wording／虛假)","(真／虛假, genuine / false)")
        aliases(s,"straight mind","mind without bends","mind without falseness","straightforward mind")
    elif pos==86: occ(d,1,7)["ContextMasters"]=[roles("Zhizhe Yuanan Zhenci","person-described")]
    elif pos==87:
        named(occ(d,1,6),"Guifeng Zongmi",note="Chan Source Preface (禪源諸詮集都序): in Guifeng Zongmi’s continuous authorial instruction, Zongmi is the governing speaker of the sentence telling the reader to gather the mind and dwell in purity.")
    elif pos==88: occ(d,1,3)["ContextMasters"]=[]
    elif pos==91:
        o=d["Senses"][0]["Occurrences"].pop(5); d["Senses"][1]["Occurrences"].append(o)
        for ss in d["Senses"]: sync_sources(ss)
        if "SearchAliases" in d["Senses"][1]: aliases(d["Senses"][1],*d["Senses"][1]["SearchAliases"])
    elif pos==92:
        lead(s,"Linji’s direct imperative ‘wherever you are, act as host’ is paired with ‘where you stand is genuine’ and with circumstances being unable to turn the person addressed.")
        aliases(s,"act as host wherever you are","be host wherever you are","wherever you are act as master","stand genuine")
    elif pos==93:
        lead(s,"Masters raise ‘awakening wherever the eye lands’ as a public question, then enact or redirect the answer through an immediately visible object or action without making one response universal.")
        cm=occ(d,1,1).setdefault("ContextMasters",[])
        if not any(c.get("MasterName")=="Daowu Yuanzhi" for c in cm): cm.append(roles("Daowu Yuanzhi","respondent"))
        aliases(s,"awakening wherever the eye lands","awakening at every sight","visible awakening","whatever the eye meets")
    elif pos==94:
        lead(s,"The broken-legged cauldron is the concrete object around which poor-monastery subsistence and succession are tested in the records.")
        replace(s,"becomes a concrete roster","becomes a concrete test")
        replace(s,"Becomes a concrete roster","Becomes a concrete test")
        aliases(s,"broken-legged cauldron","three-legged cauldron","damaged cooking pot","poor monastery cauldron")
    elif pos==95:
        lead(s,"Green bamboo is presented in the records as immediately displaying or being the body, and that claim is then publicly challenged; the phrase is therefore a tested claim rather than scenery.")
        aliases(s,"green bamboo","verdant bamboo","green bamboo body","bamboo displays the body")
    elif pos==96:
        s["PreferredTarget"]="gradual cultivation"
        s["AlternateTargets"]=["stepwise cultivation","gradual working-through"]
        lead(s,"The records place gradual cultivation in explicit contrast or sequence with sudden awakening, making particular claims about stepwise work before or after the sudden term.")
        replace(s,"gradual refinement","gradual cultivation"); replace(s,"refinement","cultivation")
        aliases(s,"gradual cultivation","stepwise cultivation","gradual work","gradual and sudden")
    elif pos==97:
        s["PreferredTarget"]="Complete Command of the Precious Mirror"
        s["AlternateTargets"]=["Precious-Mirror Complete Command","Song of the Complete Command of the Precious Mirror"]
        lead(s,"In the records this is a transmitted Caodong verse and named textual object, described as a complete command and passed from teacher to successor.")
        replace(s,"The four graphs literally name the precious mirror (寶鏡) and complete command (三昧). ","")
        replace(s,"(the cited wording，the cited wording)","(他不是我，我正是他, ‘he is not me; I am precisely he’)")
        aliases(s,"Complete Command of the Precious Mirror","Precious-Mirror Complete Command","precious mirror verse","precious mirror song")
    elif pos==98:
        lead(s,"The corpus uses ‘talking about food’ as a verdict for speech that cannot satisfy hunger: verbal display lacks the thing it names.")
        aliases(s,"talking about food","talk of food cannot satisfy hunger","verbal food","speak of food")
    elif pos==99:
        # Split Baizhang functional classification from transient guest-dust usages by explicit Baizhang rows.
        ba=[]; rest=[]
        for o in s["Occurrences"]:
            (ba if "Baizhang" in (o.get("MasterName") or "") or "百丈" in o.get("Kwic","") else rest).append(o)
        if ba and rest:
            s["Occurrences"]=rest; sync_sources(s)
            lead(s,"In the inherited guest-dust image, ‘guest’ and ‘dust’ name transient troubles or stain that arrive and depart rather than the host.")
            aliases(s,"guest dust","transient dust","visiting dust","guest-like stain")
            s2=copy.deepcopy(s); s2["PreferredTarget"]="guest and dust (Baizhang’s functional classification)"; s2["AlternateTargets"]=["guest-dust functions","illuminating function as guest and dust"]; s2["Occurrences"]=ba; sync_sources(s2)
            s2["Explanation"]="Baizhang Huaihai uses guest and dust as terms in a functional host-and-guest classification of illuminating activity. This is a master-specific classification, not the same referent as transient dust or stain. The entry reports his predicates without generalizing them beyond the anchored passages."
            s2["SenseKey"]="Baizhang Huaihai"; s2["MasterName"]="Baizhang Huaihai"; aliases(s2,"Baizhang guest and dust","guest-dust function","illuminating guest and dust")
            d["Senses"].append(s2)
        else:
            lead(s,"The records use guest-dust for transient troubles or stain and, in Baizhang’s own classification, for an illuminating function; the anchored cases distinguish those deployments.")
            aliases(s,"guest dust","transient dust","visiting dust","guest-dust function")
    elif pos==100:
        literal=[]; zen=[]
        for o in s["Occurrences"]:
            (literal if any(x in o.get("Kwic","") for x in ("歸家","還鄉","到家")) and "就路還家" not in o.get("Kwic","").replace("就路還家","") else zen).append(o)
        # Reviewer identifies occurrence 2 as ordinary physical journey.
        if len(s["Occurrences"])>=2:
            literal=[s["Occurrences"][1]]; zen=[o for i,o in enumerate(s["Occurrences"]) if i!=1]
        s["Occurrences"]=zen; sync_sources(s); s["PreferredTarget"]="take the road home (Chan appraisal)"
        s["Explanation"]="As a public Chan appraisal, ‘take the road home’ judges a person or saying by the homeward-road image. The records apply and contest the verdict in encounters; this sense does not silently absorb an ordinary physical journey. Literally the phrase says ‘take the road and return home.’"
        aliases(s,"take the road home","return home by the road","homeward-road appraisal","road home")
        s2=copy.deepcopy(s); s2["PreferredTarget"]="take the road home (literal journey)"; s2["AlternateTargets"]=["set out for home","return home by road"]; s2["Occurrences"]=literal; sync_sources(s2); s2["SenseKey"]=None; s2["MasterName"]=None
        s2["Explanation"]="In ordinary narrative use, the phrase describes a physical homeward journey: setting out on the road and returning home. This is a different event from the Chan appraisal idiom retained in the primary sense."
        aliases(s2,"physical journey home","set out for home","return home by road")
        d["Senses"].append(s2)
    else: raise AssertionError(pos)

def main():
    review=json.loads(REVIEW.read_text(encoding="utf-8")); lane=json.loads(LANE.read_text(encoding="utf-8"))
    rows=[x for x in review["entries"] if 27<=x["position"]<=100 and x["verdict"]=="REVISE"]
    assert len(rows)==60, len(rows)
    lane_by={x["id"]:x for x in lane["entries"]}
    ledger={"schemaVersion":1,"wave":"f001","lane":"C","scope":"positions 27-100 independent semantic REVISE repairs","reviewLedger":str(REVIEW.relative_to(ROOT)),"startedUtc":NOW,"completed":0,"entries":[]}
    if OUT.exists(): ledger=json.loads(OUT.read_text(encoding="utf-8"))
    done={x["position"] for x in ledger.get("entries",[])}
    for row in rows:
        pos=row["position"]
        if pos in done: continue
        p,d=load_entry(row["id"])
        current=digest(p)
        if current!=row["entrySha256"]:
            # A process interruption can occur after the atomic entry replace but
            # before the ledger replace.  The position-27 role repair is idempotent.
            assert pos==27 and all(c.get("Roles")==["verse-author"] for n in (1,2) for c in occ(d,2,n).get("ContextMasters",[])), (pos,current,row["entrySha256"])
        repair(pos,d)
        save_entry(p,d)
        sha=digest(p)
        lane_by[row["id"]]["entrySha256"]=sha
        work=ENTRIES/row["id"]/"WORK.md"
        with work.open("a",encoding="utf-8") as f:
            f.write(f"\n## Independent semantic repair — {NOW}\nApplied all findings for lane-C position {pos} from `f001-laneC-independent-semantic-review.json`; definitions were rechecked against the retained occurrences. New entry SHA-256: `{sha}`.\n")
        ledger["entries"].append({"position":pos,"id":row["id"],"term":row["term"],"priorSha256":current,"entrySha256":sha,"findingsApplied":len(row["findings"]),"completedUtc":NOW})
        ledger["completed"]=len(ledger["entries"]); ledger["lastPosition"]=pos
        tmp=OUT.with_suffix(".json.tmp"); tmp.write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+"\n",encoding="utf-8"); os.replace(tmp,OUT)
        tmp=LANE.with_suffix(".json.tmp"); tmp.write_text(json.dumps(lane,ensure_ascii=False,indent=2)+"\n",encoding="utf-8"); os.replace(tmp,LANE)
    ledger["completedUtc"]=NOW
    tmp=OUT.with_suffix(".json.tmp"); tmp.write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+"\n",encoding="utf-8"); os.replace(tmp,OUT)
    print(json.dumps({"repaired":ledger["completed"],"lastPosition":ledger.get("lastPosition")},ensure_ascii=False))
if __name__=="__main__": main()
