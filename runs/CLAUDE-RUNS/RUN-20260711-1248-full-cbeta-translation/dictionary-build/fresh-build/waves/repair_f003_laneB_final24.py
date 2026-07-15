#!/usr/bin/env python3
"""Deterministic author repair for the final independent f003/B rejections.

This script is deliberately keyed by entry id + source + line.  It does not
infer speakers.  Every mutation below records a human complete-case ruling
from the hash-bound independent report and its v3 turn-proof packet.
"""

from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ENTRIES = ROOT / "fresh-build" / "entries"
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]
REVIEWED_UTC = "2026-07-15T08:00:00Z"


def load(entry_id: str):
    path = ENTRIES / entry_id / "entry.v2.json"
    return path, json.loads(path.read_text(encoding="utf-8-sig"))


def save(path: Path, entry: dict):
    path.write_text(json.dumps(entry, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def occ(entry: dict, rel: str, lb: str):
    rows = [o for s in entry["Senses"] for o in s.get("Occurrences", [])
            if o.get("RelPath") == rel and o.get("FromLb") == lb]
    if len(rows) != 1:
        raise AssertionError((entry["Id"], rel, lb, len(rows)))
    return rows[0]


def named(o: dict, master: str, note: str, contexts=None):
    o["MasterName"] = master
    o.pop("ActorAttribution", None)
    o["ContextMasters"] = contexts or [{"MasterName": master, "Roles": ["utterer"]}]
    o["AttributionNote"] = note


def questioner(o: dict, respondent: str, source: str):
    o["MasterName"] = None
    o["ActorAttribution"] = {
        "Status": "reviewed-unnamed", "Kind": "unnamed monastic questioner",
        "ActorLabel": "the unnamed monastic questioner", "ActorRole": "questioner",
        "GrammarEvidence": "The headword is inside the question before the separately marked master response.",
        "RungsChecked": RUNGS, "ReviewedBy": "Codex f003/B final24 complete-case v3 turn-proof repair"
    }
    o["ActorAttribution"]["ReviewedUtc"] = REVIEWED_UTC
    o["ContextMasters"] = [{"MasterName": respondent, "Roles": ["respondent"]}]
    o["AttributionNote"] = f"Source text ({source}): the unnamed monastic questioner utters the exact headword before {respondent}'s separately marked response."


def impersonal_title(o: dict, author: str, source: str):
    o["MasterName"] = None
    o["ActorAttribution"] = {
        "Status": "impersonal", "Kind": "editorial poem title",
        "ActorLabel": "the editorial poem title", "ActorRole": "compiler",
        "GrammarEvidence": "The exact headword occurs in the duplicated title before the poem, not in a spoken turn.",
        "RungsChecked": RUNGS, "ReviewedBy": "Codex f003/B final24 complete-case v3 turn-proof repair"
    }
    o["ActorAttribution"]["ReviewedUtc"] = REVIEWED_UTC
    o["ContextMasters"] = [{"MasterName": author, "Roles": ["verse-author"]}]
    o["AttributionNote"] = f"Source text ({source}): the editorial title names the poem's Chan addressee; {author} is linked as verse author, not falsely assigned as utterer of the title."


def repair_checkpoint_1():
    # 754 端的 — make the reader-facing note agree with the identified lay actor.
    p, e = load("t_51a4f3a03bd8")
    o = occ(e, "X/X82/X82n1571.xml", "0003a15")
    o["AttributionNote"] = "Source text (五燈全書(第34卷-第120卷)): the lay official questioning Xuedou utters the exact headword in his direct question; Xuedou Chongxian is the respondent."
    save(p, e)


def repair_checkpoint_2():
    # 777 聖僧 — the nearest section header is Yunju Yineng; remove the duplicate
    # stale Baoming copy of the exact same line rather than counting it twice.
    p, e = load("t_33ae72debe2c")
    rows = e["Senses"][0]["Occurrences"]
    duplicates = [o for o in rows if o["RelPath"] == "X/X81/X81n1568.xml" and o.get("MasterName") == "Baoming Daocheng"]
    for duplicate in duplicates:
        rows.remove(duplicate)
    named(occ(e, "X/X81/X81n1568.xml", "0021b22"), "Yunju Yineng",
          "Source text (五燈嚴統(第10卷-第25卷)): Yunju Yineng asks the monk what the sacred monk said; the nearest section header and the unbroken master turn identify Yunju as utterer.")
    if not any(o.get("RelPath") == "X/X82/X82n1571.xml" and o.get("FromLb") == "0021c16" for o in rows):
        rows.append({
            "RelPath": "X/X82/X82n1571.xml", "FromLb": "0021c16", "ToLb": "0021c24",
            "Kwic": "力看，看來看去轉顢頇。要得不顢頇，看！參！上堂：堪作梁底作梁，堪作柱底作柱。靈利衲僧，便知落處。驀拈拄杖曰：還知這箇堪作甚麼？打香臺一下，曰：莫道無用處。復打一下，曰：參！上堂：看！看！堂裏木師伯，被聖僧打一摑。走去見維那，被維那打兩摑。露柱呵呵笑，打著這師伯。元豐路見不平，與你雪屈。拈拄杖曰：來！來！然是聖僧，也須喫棒。擊香臺，下座。歲旦，上堂：饑飡松柏葉，渴飲㵎中泉。看罷青青竹，和衣自在眠。大眾，",
            "Curated": True, "MasterName": "Weizhou Qingman",
            "AttributionNote": "Source text (五燈全書(第34卷-第120卷)): Weizhou Qingman utters the exact headword twice in his hall address, personifying the communal-hall figure as striking and being struck.",
            "ContextMasters": [{"MasterName": "Weizhou Qingman", "Roles": ["utterer"]}]
        })
    e["Senses"][0]["Explanation"] = "The sacred monk is the enshrined monastic guardian figure seated in the communal hall or represented at the monastery. Monastic rules assign the figure a seat and greetings; Fayin Shanning and Foyan Qingyuan send their assemblies back to it, while Yunju Yineng asks an unnamed monastic questioner what it said. Danxia Tianran's narrated act of riding its neck confirms that the institutional image, not a generic compliment to a holy monk, is the referent."
    e["Senses"][0]["Note"] = "Seven independently reviewed witnesses delimit this sense; the duplicated Yunju line is counted once."
    save(p, e)

    # 778 如何是無寒暑處 — the case is Dongshan's in every witness.
    p, e = load("t_073dcbf657a3")
    e["Senses"][0]["Explanation"] = "This question asks where one can escape the paired physical conditions of heat and cold. Dongshan Liangjie answers by directing the unnamed questioner into the extremity of heat and cold themselves; later collections transmit that same exchange rather than a climate-controlled refuge. The phrase is the questioner's exact demand, not permission to replace its concrete weather with an abstract opposition."
    save(p, e)

    # 779 髑髏 — the two headword instances in O3 are both inside the monk's
    # question and precede Tiantai's response.
    p, e = load("t_5835e3ae094b")
    questioner(occ(e, "X/X81/X81n1568.xml", "0005c21"), "Tiantai Deshao", "五燈嚴統(第10卷-第25卷)")
    e["Senses"][0]["Explanation"] = "A skull is the bony case of a dead person's head. Tianyi Yihuai puts the skull into an impossible living scene; an unnamed monk repeats that wording while questioning Tiantai Deshao; Muzhou Daoming asks about exchanging a skull inside an eye; and a Buddha narrative supplies the literal corpse object. None of these uses makes 'death' an adequate substitute for the skull itself."
    save(p, e)

    # 781 參學事畢 — synchronize the prose with the repaired Dunan header.
    p, e = load("t_19635cfe9de8")
    e["Senses"][0]["Explanation"] = "The formula records a claim that travel among teachers and inquiry have reached completion. Dunan Zongyan quotes and rejects an inherited claim that recognizing the staff finishes a lifetime's inquiry; Lingyan Chu, Chengshan Qia, Changqing Huileng, and Poshan Haiming each state conditions under which the inquiry is finished. The phrase marks a claimed verdict, not an automatic certificate attached to any strong answer."
    save(p, e)

    # 783 化主 — replace the TOC match with the actual rule section and recover
    # Huilin Zongben as the owner of his own separate record.
    p, e = load("t_5306489d35c6")
    candidates = [o for s in e["Senses"] for o in s.get("Occurrences", [])
                  if o.get("RelPath") == "X/X63/X63n1245.xml"
                  and o.get("FromLb") in {"0522b22", "0535a06"}]
    if len(candidates) != 1:
        raise AssertionError((e["Id"], "化主 replacement", len(candidates)))
    o = candidates[0]
    o.update({
        "FromLb": "0535a06", "ToLb": "0535a08",
        "Kwic": "化主化主或侍者寮具州縣名目出膀召請發心。或知事頭首和會。禮請之儀竝同頭首。",
        "MasterName": None,
        "AttributionNote": "Source text ((重雕補註)禪苑清規): the monastic-rule compiler opens the alms-officer section and describes how the holder is publicly invited; this is rule narration, not a master utterance.",
        "ActorAttribution": {
            "Status": "narrated", "Kind": "monastic-rule compiler", "ActorLabel": "the monastic-rule compiler",
            "ActorRole": "compiler", "GrammarEvidence": "The office heading is followed by prose prescribing public recruitment and appointment.",
            "RungsChecked": RUNGS, "ReviewedBy": "Codex f003/B final24 complete-case v3 turn-proof repair", "ReviewedUtc": REVIEWED_UTC
        }, "ContextMasters": []
    })
    named(occ(e, "X/X73/X73n1450.xml", "0089b19"), "Huilin Zongben",
          "Source text (慧林宗本禪師別錄): Huilin Zongben uses the office title in his poem headings and verses; this is his separate record, not Dahui Zonggao's.")
    e["Senses"][0]["Explanation"] = "The alms-raising officer is the monastic officeholder sent out to obtain material support for a monastery or communal project. The Chan monastic rules prescribe public appointment, travel among donors, return, and accounting; encounter narrators identify the officeholder, and Huilin Zongben addresses holders in departure verses. The title does not mean the monastery's presiding teacher."
    save(p, e)

    # 785 明眼 — both formerly generic authorial intervals are locally named.
    p, e = load("t_168078a96bd7")
    named(occ(e, "X/X66/X66n1297.xml", "0287a07"), "Daochang Ru",
          "Source text (宗鑑法林): Daochang Ru utters the headword inside his explicitly introduced comment on the Surangama case.")
    named(occ(e, "X/X64/X64n1260.xml", "0002c18"), "Jiexian",
          "Source text (列祖提綱錄): Jiexian, the signed compiler, asks clear-eyed lineage masters to correct the compilation in his prefatory rules.")
    e["Senses"][0]["Explanation"] = "A clear-eyed person is someone credited with distinguishing what others confuse. Nanquan Puyuan, Linji Yixuan, Foyan Qingyuan, and Fenyang Shanzhao invoke such a person when a presented turn must be judged; Jiexian asks clear-eyed lineage masters to correct his compilation. The phrase praises tested discrimination, not literal eyesight and not an infallible rank."
    save(p, e)

    # 786 四天王 — remove the imported cardinal-direction office and name the
    # supposedly vague source owner.
    p, e = load("t_53d86caa85c2")
    e["Senses"][0]["Explanation"] = "The Four Heavenly Kings are a named fourfold company in inherited Buddha narratives and Chan speech. One monastic rule tells how they offer bowls to Shakyamuni; an unnamed monk asks Langting Jingting whether their heaven lies below Mount Sumeru; Sanyi Mingyu makes them speak after a world-burning image. These deployments establish the collective but do not reduce four figures to one deity."
    save(p, e)

    # 787 用處 — 師林則云 is the explicit named cue in both compilations.
    p, e = load("t_e3231052e685")
    named(occ(e, "X/X66/X66n1297.xml", "0307b02"), "Shilin Ze",
          "Source text (宗鑑法林): Shilin Ze's explicitly introduced comment says that the ancestral gate's use differs; the compiler only transmits his wording.")
    named(occ(e, "X/X66/X66n1296.xml", "0013a22"), "Shilin Ze",
          "Source text (宗門拈古彙集): Shilin Ze's explicitly introduced comment says the ancients' use was one-sided; the compiler only transmits his wording.")
    e["Senses"][0]["Explanation"] = "The word names a thing's use, function, utility, or effective point. Fayun Faxiu, Huangbo Xiyun, Fayan Wenyi, and Hongzhi Zhengjue ask what use a claim or condition has; Shilin Ze contrasts the use made in inherited cases; Yandang Ji strikes the incense stand after warning not to call the staff useless. The object varies, but the demand is consistently for what it does or accomplishes."
    save(p, e)

    # 792 把住放行 — O3 is another questioner-owned headword.
    p, e = load("t_5cde0218375f")
    questioner(occ(e, "J/J37/J37nB388.xml", "0450c25"), "Yuezhang Shoujing", "神鼎一揆禪師語錄")
    e["Senses"][0]["Explanation"] = "To hold fast and release is to pair restraint with letting something proceed. Xue'an Congjin and Shending Hongyin state the pair in their own addresses; an unnamed monk sets it aside while questioning Yuezhang Shoujing; Jiashan Shanhui places both actions under the holder's control. The pair names opposed actions, not a metaphysical formula."
    save(p, e)

    # 793 蒼天 — replace stale Fenggan/Mazu prose with the actual final actors.
    p, e = load("t_73656adc1e50")
    e["Senses"][0]["Explanation"] = "In an exclamatory speech frame, the word appeals to heaven in shock, grief, or protest. Fozhi Zhicai beats his chest and cries it; Hanshan, Linji Yixuan, and Shitou Xiqian utter it in separately marked exchanges; later documentary prose also reports people crying to heaven. The word still names the sky elsewhere, so the exclamatory frame is required before translating it as 'heavens!'"
    save(p, e)

    # 794 佛誕 — the saved headword rows are occasion labels/date prose, so the
    # explanation must describe that evidence rather than speakers downstream.
    p, e = load("t_c051d6f277af")
    e["Senses"][0]["Explanation"] = "The Buddha's birthday is the annual calendar observance of Shakyamuni's birth. In these records the headword chiefly appears as an editorial occasion label before a hall address or verse; one signed preface uses it to date a letter. The label establishes why the assembly met or a text was composed, but it is not itself part of the subsequent discourse and does not by itself mean the bathing rite performed on that day."
    save(p, e)

    # 795 主中主 — O1's visible header belongs to Guishan Xiaojin, not
    # Sansheng; the headword remains the anonymous monk's question. Surface
    # Dongshan's corpus self-definition rather than replacing it with theory.
    p, e = load("t_f266d9e034ea")
    o = occ(e, "X/X82/X82n1571.xml", "0044c09")
    o["ContextMasters"] = []
    o["AttributionNote"] = "Source text (五燈全書(第34卷-第120卷)): the unnamed monastic questioner utters the exact headword before the separately marked response; the visible section header identifies the respondent as Guishan Xiaojin, not Sansheng Huoran."
    e["Senses"][0]["Explanation"] = "The host within the host is a guest-and-host position that Dongshan Liangjie defines by continuity: 'being able to continue is called host within host.' Yongjue Yuanxian likewise says continuous, unbroken succession accords with it, while unnamed monks ask named respondents to demonstrate the position. The corpus makes continuity its explicit diagnostic; it does not license an inner-self gloss."
    save(p, e)

    # 796 十二時 — synchronize every prose actor and recover the continuous
    # Fenyang sermon owner in O8.
    p, e = load("t_0229ebe0b9e7")
    named(occ(e, "X/X68/X68n1318.xml", "0349b14"), "Fenyang Shanzhao",
          "Source text (續古尊宿語要): Fenyang Shanzhao utters the exact headword in continuous discourse under his named section; he tells the assembly to examine obstruction throughout the twelve periods.")
    e["Senses"][0]["Explanation"] = "The twelve periods are the traditional double-hours that together span a complete day and night. Nanquan Puyuan asks Huangbo Xiyun about reliance throughout them; Xitang Zhizang asks the same all-day question in two transmitted versions; Fayan Wenyi receives it from an unnamed monk, while Maqiaoshan Benkong and Fenyang Shanzhao use the span in their own addresses. Baozhi appears only as the subject of a biographical work-title notice. The phrase means the whole day, not twelve modern sixty-minute hours."
    save(p, e)

    # 799 三要 — name instruction owner and later quoter consistently.
    p, e = load("t_7ee93f6b90cf")
    e["Senses"][0]["Explanation"] = "The three essentials are Linji Yixuan's threefold category nested inside the three mysteries of a sentence. Huitang Zuxin gives the inherited formulation while instructing his student Caotang Shanqing; Hanyue Fazang later quotes Linji's wording and comments on it; Qingshan Ti, Tianyin Yuanxiu, and other named masters deploy the paired set in lineage speech. The sources attest the named architecture but do not supply three stable, freestanding definitions, so none are invented here."
    save(p, e)

    # 757 宗乘 — extend the questioner test to every question, not only old O6.
    p, e = load("t_32a92c635f49")
    for rel, lb, respondent, source in [
        ("X/X82/X82n1571.xml", "0007a22", "Tiantong Chengjiao", "五燈全書(第34卷-第120卷)"),
        ("X/X81/X81n1568.xml", "0008c12", "Yongming Daoqian", "五燈嚴統(第10卷-第25卷)"),
        ("X/X80/X80n1565.xml", "0081b19", "Letan Changxing", "五燈會元"),
        ("X/X78/X78n1556.xml", "0652b19", "Kaixian Zhao", "建中靖國續燈錄"),
        ("T/T51/T51n2077.xml", "0470b20", "Shoushan Shengnian", "續傳燈錄"),
    ]:
        questioner(occ(e, rel, lb), respondent, source)
    e["Senses"][0]["Explanation"] = "The lineage vehicle is the ancestral line's own teaching and public business. Unnamed monks repeatedly ask named masters about its highest rule or upward presentation; Huangbo Xiyun asks Baizhang Huaihai how it is shown, while Chengtian Zong comments on how difficult it is to support. The phrase marks lineage jurisdiction rather than a literal vehicle."
    save(p, e)

    # 760 禪者 — remove banned practice language and stop treating poem titles as speech.
    p, e = load("t_1b62ec7f731e")
    e["Senses"][0]["PreferredTarget"] = "a Chan student"
    e["Senses"][0]["Explanation"] = "A Chan student is a person identified as studying, visiting, or being addressed within Chan. The label names students, visitors, and poem addressees involved in Chan; it excludes the distinct topical construction in which the phrase means 'as for Chan.'"
    impersonal_title(occ(e, "C/C077/C077n1710.xml", "0779a07"), "Touzi Yiqing", "古尊宿語錄")
    impersonal_title(occ(e, "T/T47/T47n1996.xml", "0678c05"), "Mingjue Chongxian", "明覺禪師語錄")
    save(p, e)

    # 761 丈室 — O2 is another monk's question.
    p, e = load("t_ec09781522d8")
    questioner(occ(e, "X/X81/X81n1568.xml", "0004b07"), "Tiantai Deshao", "五燈嚴統(第10卷-第25卷)")
    e["Senses"][0]["Explanation"] = "The abbot's room is the monastery room occupied by the presiding teacher and used for receiving visitors. Narrators locate masters and visitors there; unnamed monks compare it with Vimalakirti's chamber, while Yizhong Zhiji answers a minister's question about how much it contains. The term still denotes an actual monastery room, and the comparisons do not turn every occurrence into an inner space."
    save(p, e)

    # 762 山門 — 師云 assigns 山門無口 to Juelang, not backward to the monk.
    p, e = load("t_d67829b96305")
    named(occ(e, "J/J34/J34nB311.xml", "0592b25"), "Juelang Daosheng",
          "Source text (天界覺浪盛禪師全錄): Juelang Daosheng says that the monastery gate has no mouth in the master response; the preceding monk does not own the phrase.")
    e["Senses"][2]["Note"] = "One reviewed witness provisionally delimits this lineage sense."
    save(p, e)

    # 766 臘八 — remove stale sermon-owner claims not represented by actor links.
    p, e = load("t_a2c5b2af7b10")
    e["Senses"][0]["Explanation"] = "The eighth day of the twelfth lunar month is a calendrical observance associated in these records with Shakyamuni's awakening under the morning star. Most saved occurrences are editorial occasion labels introducing formal addresses; Feiyin Tongrong also says 'midnight on the eighth' inside his own sermon. The calendar sense remains primary: the heading does not claim that the historical event literally happens again."
    save(p, e)

    # 773 言句 — first-person 朕 prose belongs to the Yongzheng emperor.
    p, e = load("t_10d93a67ea99")
    o = occ(e, "X/X68/X68n1319.xml", "0526b15")
    o["MasterName"] = None
    o["ActorAttribution"] = {
        "Status": "identified-non-master", "Kind": "imperial editor and preface author",
        "ActorLabel": "the Yongzheng Emperor", "ActorRole": "compiler",
        "GrammarEvidence": "First-person 朕 and the closing 雍正十一年 signature identify the emperor as author of the headword-bearing editorial prose.",
        "RungsChecked": RUNGS, "ReviewedBy": "Codex f003/B final24 complete-case v3 turn-proof repair"
    }
    o["ActorAttribution"]["ReviewedUtc"] = REVIEWED_UTC
    o["ContextMasters"] = [{"MasterName": "Yulin Tongxiu", "Roles": ["person-discussed"]}]
    o["AttributionNote"] = "Source text (御選語錄): the Yongzheng Emperor writes the first-person editorial statement about Yongjia's words and phrases; Yulin Tongxiu is discussed, not the utterer."
    e["Senses"][0]["Explanation"] = "Words and phrases are the articulated verbal material of sermons, questions, cases, and written records. Huangbo Xiyun warns that this material does not by itself disclose the matter; Huineng asks Zhichang to repeat Datong's wording; the Yongzheng Emperor describes Yongjia's wording in editorial prose. The term means verbal formulations, not 'dead words' by definition: approval or rejection depends on the surrounding turn."
    save(p, e)

    # 774 提綱 — title/TOC strings are a different thing and get their own sense.
    p, e = load("t_a9a874976d5b")
    action = e["Senses"][0]
    if len(e["Senses"]) > 1:
        existing = e["Senses"][1].get("Occurrences", [])
        title_rows = [next(o for o in existing if o["FromLb"] == "0001a02"),
                      next(o for o in existing if o["FromLb"] == "0685a04")]
    else:
        title_rows = [occ(e, "X/X64/X64n1260.xml", "0001a02"), occ(e, "X/X72/X72n1442.xml", "0685a04")]
    e["Senses"] = [action]
    action["Occurrences"] = [o for o in action["Occurrences"] if o not in title_rows]
    action["Explanation"] = "To raise the guiding theme is to present the principal matter organizing a public address or recorded section. Unnamed monks ask masters to do it; Fenyang Shanzhao says he now raises it, and Dawei Zhe credits Yunmen with raising the lineage essentials. The action is not merely a modern outline."
    action["Note"] = "Five reviewed deployment witnesses delimit the verbal and nominal action sense."
    title_rows[0]["AttributionNote"] = "Source text (列祖提綱錄): the title and table-of-contents heading contains the headword in the bibliographic title Record of the Patriarchs Raising the Guiding Theme; it is front matter, not speech."
    title_rows[1]["AttributionNote"] = "Source text (為霖禪師旅泊菴稿): the table-of-contents heading lists a guiding-theme-and-comments section; it is editorial metadata, not speech."
    e["Senses"].append({
        "SenseKey": None, "MasterName": None, "PreferredTarget": "a guiding-theme title or section",
        "AlternateTargets": ["guiding-theme record"], "SearchAliases": ["outline", "main theme"],
        "Status": "allowed", "Explanation": "In front matter and contents lists, the same graphs name a book or section devoted to raised guiding themes. Record of the Patriarchs Raising the Guiding Theme is a bibliographic title, while another contents list names a guiding-theme-and-comments section. These title strings are kept separate from an actor raising a theme.",
        "Validation": "multi-source", "Note": "Two independent editorial witnesses delimit this title/section sense.",
        "Occurrences": title_rows, "SourceTexts": sorted({o["RelPath"] for o in title_rows}),
        "RelatedMasters": [], "RelatedTerms": action.get("RelatedTerms", [])
    })
    save(p, e)

    # 775 浴佛 — describe what the saved labels and Huanglong speech actually anchor.
    p, e = load("t_eb17a30eecaa")
    e["Senses"][0]["Explanation"] = "To bathe the Buddha image is the Buddha-birthday rite named by formal occasion labels and by Huanglong Huinan's statement that monasteries are bathing the Buddha. The saved headings attach that rite to addresses by Foyan Qingyuan, Huguo Cian Jingyuan, Liao'an Qingyu, and others, while Huanglong recounts the ladle-and-washing exchange. The concrete rite remains present, so these records do not justify replacing it with 'purify the mind.'"
    save(p, e)

    # 776 出身 — remove stale Kaixian and name only present evidence owners.
    p, e = load("t_b4b6091cf9cb")
    e["Senses"][0]["Explanation"] = "To emerge bodily is to come out from a stated confinement or position and show a road of emergence. Chongsheng Yi says a road out is still required; an unnamed monk asks Shan Shan Zhijian where the buddhas emerge; Linji Yixuan and Yunfeng Wenyue use the same wording in sharply different statements. The word marks coming out, not a general imported theory of liberation."
    save(p, e)


if __name__ == "__main__":
    repair_checkpoint_1()
    repair_checkpoint_2()
