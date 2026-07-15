#!/usr/bin/env python3
"""Resumable exact-turn repairs for f004 lane B 1011-1020."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]
REVIEWER = "Codex f004 lane B 1001-1100 repair author"
WHEN = "2026-07-15T22:30:00Z"
REPAIRED = {"t_b7fa9548f704", "t_8cc557911096", "t_f50f469aa43b", "t_d468479c7729", "t_72bcb768449d", "t_f9bb8b44b32f", "t_2b9a5ab567cc", "t_88de22b8a40e", "t_6275f20a3f87", "t_baaf8fde82d2"}

def root(d): return d.get("Entry", d)
def occurrences(d): return [o for s in root(d)["Senses"] for o in s["Occurrences"]]

def named(o, name, note, contexts=()):
    o.pop("ActorAttribution", None); o["MasterName"] = name
    o["ContextMasters"] = [{"MasterName": name, "Roles": ["utterer"]}] + [{"MasterName": n, "Roles": [r]} for n, r in contexts if n != name]
    o["AttributionNote"] = note
    o["DraftActorProof"] = {"ExactHeadwordClause": o["Kwic"], "SpeechFrame": note, "FullCaseDecision": f"The complete case assigns this utterance to {name}."}

def unnamed(o,label,role,note,contexts=()):
    o.pop("MasterName",None);o["ContextMasters"]=[{"MasterName":n,"Roles":[r]} for n,r in contexts]
    o["ActorAttribution"]={"Status":"reviewed-unnamed","Kind":"unnamed participant","ActorLabel":label,"ActorRole":role,"RungsChecked":RUNGS,"GrammarEvidence":note,"ReviewedBy":REVIEWER,"ReviewedUtc":WHEN,"AuthoredVoiceRiskReviewed":True}
    o["AttributionNote"]=note;o["DraftActorProof"]={"GrammaticalSubject":label,"FullCaseDecision":note}

def nonhuman(o,status,kind,label,note,contexts=()):
    o.pop("MasterName",None);o["ContextMasters"]=[{"MasterName":n,"Roles":[r]} for n,r in contexts]
    o["ActorAttribution"]={"Status":status,"Kind":kind,"ActorLabel":label,"ActorRole":"compiler","RungsChecked":RUNGS,"GrammarEvidence":note,"ReviewedBy":REVIEWER,"ReviewedUtc":WHEN,"AuthoredVoiceRiskReviewed":True}
    o["AttributionNote"]=note;o["DraftActorProof"]={"GrammaticalSubject":label,"FullCaseDecision":note}

def sync(d):
    for s in root(d)["Senses"]:
        text=s["Explanation"].strip(); cut=text.find(". "); opening=text if cut<0 else text[:cut+1]; body=text if cut<0 else text[cut+2:]
        s["ExplanationParts"]={"CorpusEarnedOpening":opening,"EvidenceBody":[body]};s.setdefault("DraftEvidence",{})["ZenBend"]=body

def repair_1011(d):
    s=root(d)["Senses"][0]
    s["PreferredTarget"]="Sun Bin, the diviner who opens or closes shop"
    s["AlternateTargets"]=["Sun Bin the fortune-teller","Sun Bin's divination shop"]
    s["SearchAliases"]=["Sun Bin","Sun Pin","Sun Bin divination shop","fortune-teller closing shop"]
    s["Explanation"]=("Sun Bin is the named figure whom these records present as opening or closing a divination shop. "
        "Luopu Yuanan announces that 'Sun Bin has closed shop,' calls for another diviner, and then gives a monk a deliberately brutal fortune; Dongshan Fayan reverses the formula by saying Sun Bin opens shop and asks whether anyone born in the ox year wants a reading. "
        "Chan records use the recognizable shopkeeper-diviner to stage a public test of who can read whom; the entry does not import a complete biography of the historical strategist.")
    s["Note"]="Five witnesses preserve Luopu's case in three works, Dongshan Fayan's reversal, and later case commentary; parallel recensions are not counted as different events."
    o=occurrences(d)
    named(o[0],"Luopu Yuanan","Source text (五燈全書): Luopu Yuanan announces that Sun Bin has closed shop and calls for a diviner in his上堂.")
    named(o[1],"Dongshan Fayan","Source text (宗鑑法林): the section explicitly identifies Dongshan Fayan, who says Sun Bin opens shop and asks for ox-year clients.")
    named(o[2],"Luopu Yuanan","Source text (五燈會元): Luopu Yuanan announces that Sun Bin has closed shop in the recorded上堂.")
    named(o[3],"Luopu Yuanan","Source text (五燈嚴統): Luopu Yuanan announces that Sun Bin has closed shop in the recorded上堂.")
    named(o[4],"Luopu Yuanan","Source text (宗門拈古彙集): the compiler explicitly raises Luopu Yuanan's complete case; Luopu is the historical utterer.")

def repair_1012(d):
    s=root(d)["Senses"][0];s["PreferredTarget"]="the exposed cutting edge";s["AlternateTargets"]=["the blade's edge","a revealed point"]
    s["SearchAliases"]=["cutting edge","blade edge","exposed point","not showing the edge"]
    s["Explanation"]=("鋒鋩 is the cutting edge or point by which a blade shows its capacity to wound. Chan records make exposure itself the test: monks ask how to win without displaying the edge, masters warn against touching or avoiding it, and death verses say that not even a drop of it should be conceded. The word therefore joins sharp effectiveness to whether that effectiveness leaves a visible trace; it is not merely a decorative sword part.")
    s["Note"]="Seven witnesses span a death verse, public interviews, named addresses, and compiled verse; noun and verbal constructions remain one blade-edge sense."
    o=occurrences(d);named(o[0],"Letan Yingqian","Source text (五燈全書): Letan Yingqian utters 鋒鋩 in his explicitly introduced death verse.")
    nonhuman(o[1],"narrated","compiled verse","the verse compiler","Source text (宗鑑法林): the headword occurs in an unattributed compiled verse following the Niaoke Daolin dossier.",(("Niaoke Daolin","section-subject"),))
    named(o[2],"Foyuan","Source text (佛冤禪師語錄): Foyuan says that using the jeweled sword without touching its edge already loses the mechanism.")
    named(o[3],"Konggu Daocheng","Source text (空谷道澄禪師語錄): Konggu Daocheng challenges anyone unafraid of the edge to come forward.")
    named(o[4],"Linji Yixuan","Source text (五燈嚴統): Linji Yixuan himself asks Longguang how one can win without displaying the edge.",(("Longguang","interlocutor"),))
    unnamed(o[5],"an unnamed monk","questioner","Source text (五燈會元): an unnamed monk asks how one can know the tune without touching the edge; Anguo Hongtao answers.",(("Anguo Hongtao","respondent"),))
    named(o[6],"Baofeng Hongying","Source text (聯燈會要): Baofeng Hongying says that even proceeding without revealing edge or sign remains an encumbrance.")

def repair_1013(d):
    s=root(d)["Senses"][0];s["PreferredTarget"]="Essential Collection of the Linked Lamps";s["AlternateTargets"]=["Linked Lamps Essential Collection"]
    s["SearchAliases"]=["Essential Collection of the Linked Lamps","Linked Lamps collection","Liandeng huiyao"]
    s["Explanation"]=("The Essential Collection of the Linked Lamps is a lamp-history title, not a teaching phrase. Its own title heading names the work, while later lineage biographies repeatedly say that Huiweng Wuming compiled it and transmitted it in the monasteries. Those biographies use the book as evidence of a master's editorial work and the continuation of recorded lineages; parallel retellings of that fact do not create additional senses.")
    s["Note"]="Five witnesses include the work title and four later biographical notices; the notices are documentary narration rather than utterances by their section masters."
    o=occurrences(d);nonhuman(o[0],"impersonal","bibliographic title heading","the impersonal work-title heading","Source text (聯燈會要): the headword is the work's bibliographic title; no person utters it.")
    for i,title in [(1,"三山來禪師語錄"),(2,"高峰喬松億禪師語錄"),(3,"萬峰童真禪師語錄"),(4,"翠崖必禪師語錄")]:nonhuman(o[i],"narrated","lineage biography","the lineage biographer",f"Source text ({title}): the biographer narrates that Huiweng Wuming compiled the 聯燈會要; the nearby lineage master does not utter the title.",(("Huiweng Wuming","person-described"),))

def repair_1014(d):
    s=root(d)["Senses"][0];s["PreferredTarget"]="not thinking of good, not thinking of evil";s["AlternateTargets"]=["think neither good nor evil"]
    s["SearchAliases"]=["not thinking good or evil","think neither good nor evil","Huineng and Ming's original face"]
    s["Explanation"]=("“Not thinking of good, not thinking of evil” is Huineng's instruction to Ming at the moment he demands the robe and the teaching. The full case immediately asks, at just that moment, for Ming's original face; later masters quote the line to place the listener under the same demand, not to prescribe a general program of suppressing moral thought. The wording belongs to a public encounter and its later re-raising.")
    s["Note"]="Five witnesses preserve the source case and later quotations by named masters; Huineng remains the historical utterer where his words are quoted."
    o=occurrences(d)
    o[0]["Kwic"]="師云祖言不思善不思惡正恁麼時如何是本來面目"
    named(o[0],"Huineng","Source text (普濟玉琳國師語錄): Yulin Tongxiu quotes Huineng's exact instruction; Huineng is the historical utterer.",(("Yulin Tongxiu","later-quoter"),))
    named(o[1],"Huineng","Source text (天隱修禪師語錄): Tianyin Yuanxiu quotes Huineng's instruction to Ming; Huineng is the historical utterer.",(("Tianyin Yuanxiu","later-quoter"),))
    named(o[2],"Huineng","Source text (祖堂集): the compiler records Huineng giving the exact instruction to Ming in the inherited case.")
    named(o[3],"Huineng","Source text (大慧普覺禪師普說): Dahui Zonggao quotes Huineng's instruction and then presses its burden on his audience.",(("Dahui Zonggao","later-quoter"),))
    named(o[4],"Huineng","Source text (景德傳燈錄): the lamp biography records Huineng uttering the exact line to Ming.")

def repair_1016(d):
    s=root(d)["Senses"][0];s["PreferredTarget"]="the monastery infirmary";s["AlternateTargets"]=["the long-life hall","the sick hall"]
    s["SearchAliases"]=["monastery infirmary","infirmary","sick hall","long-life hall"]
    s["Explanation"]=("The 延壽堂, literally 'hall for extending life,' is the monastery infirmary where sick monastics are housed and tended. Rulebooks list its supervisor among the communal offices and describe appointing a conscientious person; biographies place seriously ill masters there for months, while Langting Jingting raises the case of a monk who dies in the hall. The auspicious name does not turn it into a longevity shrine.")
    s["Note"]="Five witnesses join two rulebooks, an office list, a public address, and a disease biography."
    o=occurrences(d);nonhuman(o[0],"narrated","table of monastic offices","the monastic-rule compiler","Source text (禪苑清規): the compiler lists the infirmary supervisor among monastery offices.")
    o[1]["Kwic"]="延壽堂主延壽堂主看視病僧。湯藥油燭炭火粥食五味常備供須。"
    nonhuman(o[1],"narrated","monastic rule","the monastic-rule compiler","Source text (勅修百丈清規): the rule defines the infirmary supervisor's care of sick monks, medicines, and food.")
    nonhuman(o[2],"narrated","monastic rule","the monastic-rule compiler","Source text (禪林備用清規): the rule lists appointing the infirmary supervisor among office transitions.")
    named(o[3],"Langting Jingting","Source text (雲溪俍亭挺禪師語錄): Langting Jingting raises a monk's death in the infirmary while discussing coming and going.")
    nonhuman(o[4],"narrated","master biography","the biographer","Source text (明覺聰禪師語錄): the biographer narrates Mingjue Cong's three-month grave illness in the infirmary.",(("Mingjue Cong","person-described"),))

def repair_1017(d):
    s=root(d)["Senses"][0];s["PreferredTarget"]="empty space shatters";s["AlternateTargets"]=["the void smashed to pieces"]
    s["SearchAliases"]=["empty space shatters","void shattered","space smashed to pieces","earth sinks and space shatters"]
    s["Explanation"]=("“Empty space shatters” is an impossible breakage used to describe a decisive collapse of the field that had seemed to contain everything. Accounts of Gaofeng's awakening pair it with the great earth sinking and self and objects being forgotten; public addresses then reuse the phrase as a challenge—before a saying space is packed full, after it space is shattered, so where is it between? The records present a reported event and interview formula, not physical cosmology.")
    s["Note"]="Six witnesses include awakening narration, public address, direct question, and later comparison."
    o=occurrences(d);named(o[0],"Yulin Tongxiu","Source text (普濟玉琳國師語錄): Yulin Tongxiu says that Gaofeng's realization made empty space shatter.")
    o[1]["Kwic"]="問：「燿古騰今即不問，天翻地覆時如何？」師云：「待虛空粉碎來向汝道。」"
    unnamed(o[1],"an unnamed monk","questioner","Source text (古雪哲禪師語錄): an unnamed monk asks what happens when empty space shatters; Guxue Zhe answers.",(("Guxue Zhe","respondent"),))
    named(o[2],"Chaozong Tongren","Source text (朝宗禪師語錄): Chaozong Tongren compares Gaofeng's realization to empty space shattering.")
    named(o[3],"Sanyi Mingyu","Source text (三宜盂禪師語錄): Sanyi Mingyu says Ushnisha's fire burns empty space to pieces in an上堂.")
    named(o[4],"Sanfeng Hanyue","Source text (三峰藏和尚語錄): Sanfeng Hanyue asks where empty space is between being packed full and shattered.")
    named(o[5],"Jiewei Zhou","Source text (介為舟禪師語錄): Jiewei Zhou says even earth sinking and empty space shattering provide no escape from the test.")

def repair_1015(d):
    e=root(d);old=occurrences(d);by={o["RelPath"]:o for o in old}
    # Three different things are attested: the implement, Xianglu Peak, and Xianglu Temple.
    obj=[by[p] for p in ("X/X82/X82n1571.xml","X/X81/X81n1568.xml","T/T51/T51n2077.xml","X/X64/X64n1260.xml","X/X66/X66n1297.xml")]
    peak=[by["J/J27/J27nB189.xml"]]; temple=[by["J/J37/J37nB392.xml"]]
    obj[2]["Kwic"]="大眾這箇是香爐子。如何是不見不行不知。百億恒沙世界諸佛。盡在香爐上放光動地說法度人。"
    obj[3]["Kwic"]="自家田地枯木生枝，古廟香爐寒灰再𦦨，莫不一切語言文字、資生產業皆與實相不相違背。"
    obj[3]["FromLb"]="0013b14"
    obj[4]["Kwic"]="鐘送黃昏鷄報曉，趙州何用閒煩惱？裂破虗空作兩邊，古廟香爐出芝艸。"
    obj[4]["FromLb"]="0397c05"
    temple[0]["Kwic"]="惟此香爐勝境，元非近代名藍；雲際禪林，盡是唐朝古剎。"
    temple[0]["FromLb"]="0562c19";temple[0]["ToLb"]="0562c20"
    named(obj[0],"Jingyin Weiyue","Source text (五燈全書): Jingyin Weiyue says the cold ash in an old-temple incense burner flames again.")
    nonhuman(obj[1],"narrated","case narration","the lamp-record compiler","Source text (五燈嚴統): the compiler narrates Daoxian pointing to an incense burner and asking whether the assembly sees it.",(("Yongming Daoqian","case-figure"),))
    named(obj[2],"Yunmen Wanshou","Source text (續傳燈錄): Yunmen Wanshou points out the incense burner and says the buddhas teach upon it.")
    named(obj[3],"Liao'an Qingyu","Source text (列祖提綱錄): Liao'an Qingyu uses the old-temple incense burner with cold ash flaming again in a formal address.")
    nonhuman(obj[4],"narrated","compiled verse","the verse compiler","Source text (宗鑑法林): the compiler preserves a verse in which fungus grows from an old-temple incense burner.")
    named(peak[0],"Sanyi Mingyu","Source text (三宜盂禪師語錄): Sanyi Mingyu names Xianglu Peak while describing an impossible measuring task in his address.")
    nonhuman(temple[0],"narrated","record preface","the record preface author","Source text (寒松操禪師語錄): the preface describes Xianglu as an old monastic site rather than merely listing it in the table of contents.",(("Hansong Xingcao","person-described"),))
    base=e["Senses"][0]
    def mk(key,target,alts,aliases,exp,note,occs):
        x=json.loads(json.dumps(base,ensure_ascii=False));x["SenseKey"]=key;x["PreferredTarget"]=target;x["AlternateTargets"]=alts;x["SearchAliases"]=aliases;x["Explanation"]=exp;x["Note"]=note;x["Occurrences"]=occs;x["SourceTexts"]=[o["RelPath"] for o in occs];x.setdefault("DraftEvidence",{})["OpeningClaimEvidenceKeys"]=[f"o{i}" for i in range(1,len(occs)+1)];return x
    e["Senses"]=[
      mk("implement","an incense burner",["incense brazier"],["incense burner","incense brazier","old-temple incense burner"],"An incense burner is the vessel in which incense and ash remain. Chan speakers point to the actual object, ask whether the assembly sees it, and use an old temple's cold burner flaming again as an image of renewed activity. The implement is concrete even when the sentence makes it work figuratively.","Five witnesses distinguish pointing, teaching-seat speech, and the recurring old-temple image.",obj),
      mk("peak","Xianglu Peak",["Incense-Burner Peak"],["Xianglu Peak","Incense Burner Peak","香爐峰"],"Xianglu Peak is a named mountain peak, not an incense vessel. Sanyi Mingyu invokes measuring its height while staging an impossible public task, preserving the place-name inside a Chan address.","One exact public-address witness supports the proper-place sense.",peak),
      mk("temple","Xianglu Temple",["Incense-Burner Temple"],["Xianglu Temple","Incense Burner Temple","香爐寺"],"Xianglu Temple is the named monastery at Baiyun Mountain. The record preface calls Xianglu an old monastic site and pairs it with Yunji; the proper name therefore denotes an institution, not the ritual implement.","One substantive preface witness replaces the former table-of-contents cut.",temple)]

def repair_1018(d):
    s=root(d)["Senses"][0];s["PreferredTarget"]="the person as originally present";s["AlternateTargets"]=["the original person"]
    s["SearchAliases"]=["original person","person as originally present","original human","recognize the original person"]
    s["Explanation"]=("The 'original person' is the person sought before acquired descriptions and discriminations settle the matter. Records ask how to accord with that person, say that all circumstances are it, or challenge a student to reveal it by breaking through present distinctions; Yuelin Jing even turns the search into a comic verse about catching its nose. The phrase names the person under examination, not a second hidden individual inside the body.")
    s["Note"]="Seven witnesses span interview, awakening verse, compiled verse, lay questioning, and public address."
    o=occurrences(d);named(o[0],"Fenyang Shanzhao","Source text (古尊宿語錄): Fenyang Shanzhao answers a woman disciple that when the long walls collapse the original person appears.")
    o[1]["Kwic"]="因參本來人有省，述偈曰：本來人，本來人，無腦無頭作麼尋？驀然揪著個鼻孔"
    named(o[1],"Yuelin Jing","Source text (五燈全書): Yuelin Jing utters the repeated headword in his explicitly introduced awakening verse.")
    unnamed(o[2],"an unnamed monk","questioner","Source text (五燈嚴統): an unnamed monk asks how, after sitting down right and wrong, one accords with the original person; Baoci Xingyan answers.",(("Baoci Xingyan","respondent"),))
    nonhuman(o[3],"narrated","compiled verse","the verse compiler","Source text (宗鑑法林): the headword occurs in an unattributed verse saying that when all conditions are illuminated the original person appears.")
    o[4]["Kwic"]="龐居士問。不昧本來人。請師高著眼。師直下󳬇。"
    o[4]["FromLb"]="0070b18"
    nonhuman(o[4],"reviewed-unnamed","identified lay participant","Layman Pang","Source text (五燈會元): Layman Pang, a named non-master participant, utters the request containing 本來人; the section master responds by looking down.")
    named(o[5],"Mingjue Cong","Source text (明覺聰禪師語錄): Mingjue Cong says the autumn wind opens the bamboo door and exposes the original person.")
    named(o[6],"Yinyuan Longqi","Source text (隱元禪師語錄): Yinyuan Longqi says every dust mote is the original person and immediately tests why the plainly present body disappears.")

def repair_1019(d):
    s=root(d)["Senses"][0];s["PreferredTarget"]="Yunmen's flatbread";s["AlternateTargets"]=["Yunmen's sesame cake"]
    s["SearchAliases"]=["Yunmen flatbread","Yunmen sesame cake","Yunmen's cake","Yunmen hubing"]
    s["Explanation"]=("“Yunmen's flatbread” names Yunmen Wenyan's answer 胡餅 when asked for talk that goes beyond buddhas and patriarchs. Later masters compress the whole exchange into this phrase, rank it beside Zhaozhou's tea, and test whether repeating the famous food answer already leaks the case. It is an inherited saying anchored in a public interview, not culinary information about Yunmen.")
    s["Note"]="Five witnesses include later public deployment, verse, and a case heading whose body preserves Yunmen's original answer."
    o=occurrences(d);named(o[0],"Sanyi Mingyu","Source text (三宜盂禪師語錄): Sanyi Mingyu raises Yunmen's flatbread among famous public cases.",(("Yunmen Wenyan","case-figure"),))
    named(o[1],"Zhean Fan","Source text (蔗菴範禪師語錄): Zhean Fan jokes that Yunmen's flatbread is excellent while testing his audience.",(("Yunmen Wenyan","case-figure"),))
    named(o[2],"Xuefeng Sihui","Source text (五燈全書): Xuefeng Sihui pairs Yunmen's flatbread with Zhaozhou's tea in an上堂.",(("Yunmen Wenyan","case-figure"),))
    o[3]["Kwic"]="雲門胡餅雲門胡餅雲門胡餅，針劄不入，未舉已前，早已漏逗。"
    named(o[3],"Xiangyan Xishui","Source text (香巖洗心水禪師語錄): Xiangyan Xishui comments in verse that Yunmen's flatbread cannot be pierced, yet leaks before it is raised.",(("Yunmen Wenyan","case-figure"),))
    o[4]["Kwic"]="第四十二則雲門胡餅第四十二則雲門胡餅示眾云：言言見諦，碓觜夜生花；句句超宗，磨盤秋結子。不涉離微，如何話會？舉：僧問雲門：如何是超佛越祖之談？門云"
    o[4]["FromLb"]="0291b21";o[4]["ToLb"]="0291b24"
    nonhuman(o[4],"impersonal","case heading","the impersonal case heading","Source text (空谷集): the heading names Yunmen's flatbread and the immediately following body supplies Yunmen's original flatbread exchange.",(("Yunmen Wenyan","case-figure"),))

def repair_1020(d):
    s=root(d)["Senses"][0];s["PreferredTarget"]="the Fayan lineage";s["AlternateTargets"]=["the Fayan school"]
    s["SearchAliases"]=["Fayan lineage","Fayan school","Fayan house","Fayan tradition"]
    s["Explanation"]=("The Fayan lineage is the Chan house traced through Fayan Wenyi. Lineage histories place Wenyi under Luohan Guichen, manuals summarize the house's succession and formulations, and public interviews ask 'what is the Fayan lineage?' alongside the other named houses. The term is institutional and classificatory: a master's answer may characterize the house, but the answer is not itself another dictionary sense of 法眼宗.")
    s["Note"]="Six witnesses cover direct questions, lineage headings, institutional history, and a master's exposition."
    o=occurrences(d);unnamed(o[0],"an unnamed monk","questioner","Source text (五燈全書): an unnamed monk asks what the Fayan lineage is; Cian Jingyuan answers with matching arrow points.",(("Cian Jingyuan","respondent"),))
    nonhuman(o[1],"impersonal","lineage heading","the impersonal lineage heading","Source text (教外別傳): the heading classifies the following Fayan Wenyi dossier under the Fayan lineage.",(("Fayan Wenyi","section-subject"),))
    named(o[2],"Sanfeng Hanyue","Source text (三峰藏和尚語錄): Sanfeng Hanyue says the Fayan lineage's purport is fully present in the six-aspect formulation.")
    o[3]["Kwic"]="法眼宗法眼宗師諱文益。餘杭魯氏子。得法於漳州羅漢琛禪師。初住撫州崇壽。次住建康清涼。"
    nonhuman(o[3],"narrated","lineage manual","the lineage-manual compiler","Source text (人天眼目): the compiler introduces the Fayan lineage through Fayan Wenyi and his succession from Luohan Guichen.",(("Fayan Wenyi","person-described"),))
    o[4]["Kwic"]="如何是法眼宗？師曰：箭鋒相直不相饒。"
    o[4]["FromLb"]="0222b05";o[4]["ToLb"]="0222b06"
    unnamed(o[4],"an unnamed monk","questioner","Source text (五燈嚴統): an unnamed monk asks what the Fayan lineage is; the section master answers with matching arrow points.")
    unnamed(o[5],"an unnamed monk","questioner","Source text (雲溪俍亭挺禪師語錄): an unnamed monk asks what the Fayan lineage is; Langting Jingting answers.",(("Langting Jingting","respondent"),))

for entry_id in sorted(REPAIRED):
    for filename in ("evidence.draft.json", "entry.v2.json"):
        path=ROOT/"fresh-build"/"entries"/entry_id/filename; data=json.loads(path.read_text(encoding="utf-8"))
        {"t_b7fa9548f704":repair_1011,"t_8cc557911096":repair_1012,"t_f50f469aa43b":repair_1013,"t_d468479c7729":repair_1014,"t_72bcb768449d":repair_1015,"t_f9bb8b44b32f":repair_1016,"t_2b9a5ab567cc":repair_1017,"t_88de22b8a40e":repair_1018,"t_6275f20a3f87":repair_1019,"t_baaf8fde82d2":repair_1020}[entry_id](data)
        sync(data);path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print(json.dumps({"repaired":len(REPAIRED),"ids":sorted(REPAIRED)}))
