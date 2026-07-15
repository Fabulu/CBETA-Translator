#!/usr/bin/env python3
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

OPENINGS = {
    "t_ddab56ede4ef": "A bucket loses its bottom in the middle of use: contents spill, the vessel no longer holds, and the event can be seen. Chan records reuse that concrete collapse as a comparison after a blow or a phrase, as a direct answer to ‘What is buddha?’, and as an image whose literal kitchen occurrence remains visible.",
    "t_398a33955019": "The records use ‘silent illumination’ as a contested label. Hongzhi uses silence and illumination together affirmatively, while Dahui and later speakers attach the same name to a Chan they reject; the headword therefore names the disputed mode rather than settling the dispute in advance.",
    "t_824cfb1434b1": "In ordinary usage, ‘capture and release’ is a commander’s battlefield tactic: take the opponent, then let him go at will. Chan speakers bend that command pair toward control of an encounter, repeatedly coordinating it with rolling out and folding up or killing and giving life.",
    "t_47e7132eb361": "The phrase locates activity at the six sensory gateways—eye, ear, nose, tongue, body, and mind—where seeing, hearing, sensing, and thought meet their objects. Chan speakers point to these ‘gate-fronts’ as the immediate site of obstruction, light, responsiveness, or turning back, rather than as six architectural doors.",
    "t_3bf26be0cd43": "This is an opaque black bucket that can be broken, overturned, entered, lose its hoop, or serve as a blunt answer. The corpus makes darkness and enclosure do the visible work; it does not require that the bucket be made of lacquer or literally coated with it.",
    "t_b26bfa9e399e": "The imperative reverses the direction of illumination: instead of directing light outward at an object, the hearer is told to turn it around and shine back. Chan speakers place that optical action at the six faculties, amid daily activity, or against outward pursuit.",
    "t_2facdfa49dd9": "This is the last severe checkpoint in a sequence of testing, something speakers say must be reached, passed through, or personally arrived at even after an earlier understanding. ‘Final’ marks its position and ‘locked barrier’ its resistance; neither word by itself supplies the whole expression.",
    "t_5f1287817ebd": "Speakers use ‘wild-fox Chan’ as an adverse label for Chan they present as counterfeit, distorted, or merely clever. The phrase can allude to the Baizhang fox case, but its attested use also ranges beyond that case as a charge against other people’s teaching or performance.",
    "t_dec67da1f076": "Speakers use this paired verb phrase for a criticized arrest: sinking into emptiness and remaining stuck in stillness. Its surrounding clauses oppose such immobility to responsive use, seeing one’s nature, raising genuine doubt, or attending to living beings.",
    "t_12e8cba30de6": "‘Old monk’ is both an ordinary designation for an elderly monk and the conventional first-person self-reference a Chan teacher uses before an audience or interlocutor. The human role remains the same across narration, address, and self-reference; those grammatical settings do not create separate senses.",
}

TITLES = {
    "雪峰義存禪師語錄（真覺禪師語錄）": "Record of Xuefeng Yicun",
    "佛果圜悟禪師碧巖錄": "Blue Cliff Record",
    "五燈會元": "Five Lamps Meeting the Source",
    "宏智禪師廣錄": "Extensive Record of Hongzhi",
    "大慧普覺禪師語錄": "Record of Dahui Pujue",
    "續燈正統": "Continuation of the Orthodox Lamp",
    "三峰藏和尚語錄": "Record of Master Sanfeng Cang",
    "圓悟佛果禪師語錄": "Record of Yuanwu Foguo",
    "佛果克勤禪師心要": "Essentials of the Mind by Foguo Keqin",
    "列祖提綱錄": "Recorded Principles of the Lineage Patriarchs",
    "天目中峰廣錄": "Extensive Record of Zhongfeng of Tianmu",
    "古尊宿語錄": "Records of Ancient Venerable Masters",
    "百愚禪師語錄": "Record of Master Baiyu",
    "應菴曇華禪師語錄": "Record of Ying’an Tanhua",
    "如淨和尚語錄": "Record of Master Rujing",
    "廬山天然禪師語錄": "Record of Tianran of Mount Lu",
    "景德傳燈錄": "Jingde Transmission of the Lamp",
    "續傳燈錄": "Continued Transmission of the Lamp",
    "天聖廣燈錄": "Tiansheng Expanded Lamp Record",
    "汾陽無德禪師語錄": "Record of Fenyang Wude",
    "遠菴僼禪師語錄": "Record of Master Yuan’an",
    "天界覺浪盛禪師全錄": "Complete Record of Juelang Dasheng",
    "普濟玉琳國師語錄": "Record of National Teacher Yulin Tongxiu",
    "了菴清欲禪師語錄": "Record of Liao’an Qingyu",
    "入就瑞白禪師語錄": "Record of Ruibai Mingxue",
    "方融璽禪師語錄": "Record of Fangrong Tongxi",
    "蓮月禪師語錄": "Record of Lianyue Guangsi",
    "通天澹崖原禪師語錄": "Record of Tongtian Danya Yuan",
    "高峰原妙禪師語錄": "Record of Gaofeng Yuanmiao",
    "元叟行端禪師語錄": "Record of Yuansou Xingduan",
    "楚石梵琦禪師語錄": "Record of Chushi Fanqi",
    "趙州和尚語錄": "Record of Master Zhaozhou",
    "五燈嚴統": "Strict Compilation of the Five Lamps",
    "朝宗禪師語錄": "Record of Master Chaozong",
    "續古尊宿語要": "Continued Essential Sayings of Ancient Venerable Masters",
    "古庭禪師語錄輯略": "Selected Record of Master Guting",
}

def english_first(note):
    for zh, en in sorted(TITLES.items(), key=lambda item: -len(item[0])):
        note = note.replace("Source text " + zh + ":", f"Source text {en} ({zh}):")
    out, depth, i = [], 0, 0
    while i < len(note):
        ch = note[i]
        if ch in "(（":
            depth += 1; out.append(ch); i += 1; continue
        if ch in ")）":
            depth = max(0, depth - 1); out.append(ch); i += 1; continue
        if depth == 0 and re.match(r"[\u3400-\u9fff\uf900-\ufaff]", ch):
            j = i + 1
            while j < len(note) and re.match(r"[\u3400-\u9fff\uf900-\ufaff]", note[j]): j += 1
            out.extend(["(", note[i:j], ")"]); i = j; continue
        out.append(ch); i += 1
    return "".join(out)

ADDITIONS = {
    "t_ddab56ede4ef": {
        "RelPath": "J/J36/J36nB369.xml", "FromLb": "0962a06", "ToLb": "0962a07",
        "Kwic": "白汗出過幾身，桶底脫落一番，洞見釋迦心肝", "MasterName": "Zhe'an Jingfan",
        "AttributionNote": "Source text Recorded Sayings of Zhe’an Jingfan (蔗菴範禪師語錄): Zhe’an Jingfan tells practitioners that after repeated exertion, ‘the bucket-bottom falls away once,’ followed in his sentence by seeing Shakyamuni’s heart and guts.",
        "ContextMasters": [{"MasterName": "Zhe'an Jingfan", "Roles": ["utterer", "record-owner"]}],
        "DraftActorProof": {"ExactHeadwordClause": "桶底脫落一番", "SpeechFrame": "Zhe’an Jingfan’s own record places the clause inside a marked instruction to the assembly.", "FullCaseDecision": "Zhe’an Jingfan is the continuing speaker throughout the complete instruction; no embedded quotation intervenes."},
        "WorkId": "work:J36nB369"
    },
    "t_398a33955019": {
        "RelPath": "J/J29/J29nB223.xml", "FromLb": "0029c19", "ToLb": "0029c20",
        "Kwic": "其或似亡若存，必落枯禪默照，盧扁難治", "MasterName": "Shanhui",
        "AttributionNote": "Source text Recorded Sayings of Shanhui (山暉禪師語錄): Shanhui warns that a condition ‘as if absent, as if present’ necessarily falls into ‘withered Chan and silent illumination,’ which even the famed physicians Lu and Bian would struggle to treat.",
        "ContextMasters": [{"MasterName": "Shanhui", "Roles": ["utterer", "record-owner"]}],
        "DraftActorProof": {"ExactHeadwordClause": "必落枯禪默照", "SpeechFrame": "The clause occurs in Shanhui’s continuous address in the section recording his teaching at Wanshou Chan Cloister.", "FullCaseDecision": "Shanhui is the current speaker and uses the headword critically; it is not an embedded older quotation."},
        "WorkId": "work:J29nB223"
    },
    "t_824cfb1434b1": {
        "RelPath": "X/X69/X69n1357.xml", "FromLb": "0457b17", "ToLb": "0457b18",
        "Kwic": "卷舒擒縱皆據本分，綿綿的的到風穴、興化", "MasterName": "Yuanwu Keqin",
        "AttributionNote": "Source text Essentials of the Mind by Foguo Keqin (佛果克勤禪師心要): Yuanwu Keqin says that rolling up, unfolding, capturing, and releasing all rest on one’s own basis, and traces that continuous deployment to Fengxue and Xinghua.",
        "ContextMasters": [{"MasterName": "Yuanwu Keqin", "Roles": ["utterer", "record-owner"]}],
        "DraftActorProof": {"ExactHeadwordClause": "卷舒擒縱皆據本分", "SpeechFrame": "Yuanwu’s signed instruction to secretary Gao contains the clause in his own expository voice.", "FullCaseDecision": "Yuanwu Keqin is the exact writer-speaker; Fengxue and Xinghua are people discussed, not speakers of this clause."},
        "WorkId": "work:X69n1357"
    },
    "t_47e7132eb361": {
        "RelPath": "J/J28/J28nB219.xml", "FromLb": "0661b08", "ToLb": "0661b09",
        "Kwic": "又則六根門頭以無分別心念佛，能念即不動智", "MasterName": "Zhuanyu Guanheng",
        "AttributionNote": "Source text Recorded Sayings of Zhuanyu Guanheng (紫竹林顓愚衡和尚語錄): Zhuanyu Guanheng places recollection of buddha ‘at the gates of the six faculties’ and specifies an undiscriminating mind there.",
        "ContextMasters": [{"MasterName": "Zhuanyu Guanheng", "Roles": ["utterer", "record-owner"]}],
        "DraftActorProof": {"ExactHeadwordClause": "六根門頭以無分別心念佛", "SpeechFrame": "The headword clause is part of Zhuanyu Guanheng’s continuous instruction to the assembly.", "FullCaseDecision": "Zhuanyu Guanheng is the exact speaker; the sentence contains no embedded attribution to another person."},
        "WorkId": "work:J28nB219"
    }
}

def named(rel, start, end, kwic, master, title_en, title_zh, decision, work):
    clause = kwic
    return {"RelPath": rel, "FromLb": start, "ToLb": end, "Kwic": kwic, "MasterName": master,
            "AttributionNote": f"Source text {title_en} ({title_zh}): {decision}",
            "ContextMasters": [{"MasterName": master, "Roles": ["utterer"]}],
            "DraftActorProof": {"ExactHeadwordClause": clause, "SpeechFrame": decision,
                                "FullCaseDecision": decision}, "WorkId": work}

MORE_ROWS = {
 "t_398a33955019": [
  ("anchor", named("T/T48/T48n2001.xml","0075b02","0075b02","默默照處天宇澄秋。照無照功。光影斯斷。","Hongzhi Zhengjue","Extensive Record of Hongzhi","宏智禪師廣錄","Hongzhi Zhengjue speaks directly in a teaching passage: ‘in the place of silent shining, the sky is clear in autumn’; the following clauses deny a separately grasped illuminating achievement.","work:T48n2001")),
  ("anchor", {"RelPath":"T/T47/T47n1998A.xml","FromLb":"0885a18","ToLb":"0885a19","Kwic":"和尚因甚麼。却力排默照。以為邪非。","AttributionNote":"Source text Record of Dahui Pujue (大慧普覺禪師語錄): the identified visiting monastic Shangming asks Dahui why he forcefully rejects silent illumination as deviant; the clause is the visitor’s question, not Dahui’s utterance.","ActorAttribution":{"Status":"identified-non-master","Kind":"identified visiting monastic","ActorLabel":"Shangming","ActorRole":"questioner","GrammarEvidence":"The surrounding exchange identifies the visitor as Shangming and the clause addresses Dahui as 和尚 before Dahui replies 妙喜曰.","ReviewedBy":"Codex lane-B full-case repair","ReviewedUtc":"2026-07-15T00:00:00Z"},"ContextMasters":[{"MasterName":"Dahui Zonggao","Roles":["addressee","respondent"]}],"DraftActorProof":{"GrammaticalSubject":"the identified visiting monastic Shangming","FullCaseDecision":"Shangming asks the headword-bearing question; the following 妙喜曰 separately opens Dahui Zonggao’s reply."},"WorkId":"work:T47n1998A"}),
 ],
 "t_2facdfa49dd9": [
  ("anchor", named("B/B27/B27n0152.xml","0521b14","0521b15","故云末後一句始到牢關若一見不再見始覺即合本覺者自然透脫無疑","Yulin Tongxiu","Record of National Teacher Yulin Tongxiu","普濟玉琳國師語錄","Yulin Tongxiu speaks directly in sustained instruction and cites the formula ‘only with the final phrase does one reach the locked barrier’ before testing premature certainty.","work:B27n0152")),
  ("occurrence", named("X/X72/X72n1437.xml","0414c20","0414c21","若識得此一問，便明最初一句，亦明末後一關，百千諸佛、百千戒法，盡從脚跟下流出。","Yongjue Yuanxian","Extensive Record of Yongjue Yuanxian","永覺元賢禪師廣錄","Yongjue Yuanxian speaks directly in a general address, pairing the first phrase with ‘the final barrier’ and saying that recognizing the question makes both clear.","work:X72n1437")),
  ("occurrence", named("J/J26/J26nB182.xml","0464b14","0464b15","若是末後一關，正未透在。眾中還有透得末後一關底麼？","Wanru Tongwei","Recorded Sayings of Wanru","萬如禪師語錄","Wanru Tongwei speaks from the hall, says earlier figures had not yet passed the final barrier, and asks whether anyone present has passed it.","work:J26nB182")),
  ("anchor", named("B/B27/B27n0152.xml","0517b15","0517b16","透脫末後牢關雲庵正罵洞達歷祖綱宗妙喜猶呵","Yulin Tongxiu","Record of National Teacher Yulin Tongxiu","普濟玉琳國師語錄","Yulin Tongxiu directly says that even passing through the final locked barrier draws Yun’an’s rebuke, while mastering the lineage principles still draws Miaoxi’s scolding.","work:B27n0152")),
 ],
 "t_3bf26be0cd43": [
  ("occurrence", named("J/J36/J36nB369.xml","0903a24","0903a25","黑漆桶底未曾脫落，彼此男兒莫生退屈，堅確精神討箇分曉。","Zhe'an Jingfan","Recorded Sayings of Zhe’an Jingfan","蔗菴範禪師語錄","Zhe’an Jingfan directly says that while the bottom of the opaque black bucket has not fallen away, the men present should not retreat but investigate firmly.","work:J36nB369")),
  ("anchor", named("J/J38/J38nB406.xml","0146c29","0146c30","要人向黑漆桶裏橫衝直撞，撞來撞去，撞到差別境界尚不知非","Tianran Hanshi","Record of Tianran of Mount Lu","廬山天然禪師語錄","Tianran Hanshi speaks directly in a general address, criticizing people made to charge around inside the opaque black bucket without recognizing differentiated conditions.","work:J38nB406")),
 ],
 "t_5f1287817ebd": [
  ("anchor", named("X/X71/X71n1414.xml","0377b05","0377b05","倒拈苕帚柄，痛掃野狐禪。","Lia'an Qingyu","Record of Liao’an Qingyu","了菴清欲禪師語錄","Liao’an Qingyu’s authored verse turns the broom-handle around and ‘painstakingly sweeps away wild-fox Chan.’","work:X71n1414")),
  ("occurrence", named("X/X69/X69n1367.xml","0707b23","0707b24","師室中常舉百丈野狐話問僧，對者多不契。一日自云：百丈野狐，野狐百丈，埋作一坑，伏惟尚享。","Xiaoyin Daxin","Recorded Sayings of Xiaoyin Daxin","笑隱大訢禪師語錄","The record narrates Xiaoyin Daxin’s room questioning and then explicitly marks his own words; he reverses ‘Baizhang’s wild fox’ to ‘wild-fox Baizhang’ and buries both in one pit.","work:X69n1367")),
  ("anchor", named("J/J29/J29nB249.xml","0820a28","0820a28","師云：「野狐禪逞甚伎倆？」","Fangrong Tongxi","Record of Fangrong Tongxi","方融璽禪師語錄","Fangrong Tongxi is explicitly marked as speaker and asks, ‘what tricks is wild-fox Chan showing off?’","work:J29nB249")),
 ],
 "t_b26bfa9e399e": [
  ("occurrence", named("X/X70/X70n1402.xml","0732b22","0732b22","回光返照四字，是獨脫凡情，超入大悟之域底境界。","Zhongfeng Mingben","Miscellaneous Record of Tianmu Mingben","天目明本禪師雜錄","Zhongfeng Mingben directly singles out the four characters of the 回 graph variant and describes the condition named by them.","work:X70n1402")),
  ("occurrence", named("L/L154/L154n1640.xml","0556a01","0556a02","返照回光直下觀無干東北與西南十方世界全身現一切人天正眼看","Miyun Yuanwu","Recorded Sayings of Miyun Yuanwu","密雲悟禪師語錄","Miyun Yuanwu’s authored instruction verse reverses the verb order—‘shine back, turn the light’—and commands immediate looking independent of direction.","chan:miyun-wu-yulu")),
 ],
 "t_dec67da1f076": [
  ("anchor", named("T/T51/T51n2077.xml","0654b21","0654b22","嘗示眾曰。一法若有重重鐵壁銀山。萬法若無處處沈空滯寂。","Yuwang Duanyu","Continued Transmission of the Lamp","續傳燈錄","The record explicitly introduces Yuwang Duanyu’s saying; he contrasts treating one thing as present like layered iron walls and silver mountains with treating the many things as absent and sinking into emptiness.","work:T51n2077")),
  ("anchor", named("X/X71/X71n1419.xml","0546b14","0546b16","惟其沈空滯寂，只知自了，不顧度生，迦文老人所以深所訶責。","Yuansou Xingduan","Recorded Sayings of Yuansou Xingduan","元叟行端禪師語錄","Yuansou Xingduan’s authored prose on an arhat painting says that sinking into emptiness leaves one concerned only with oneself and neglecting living beings, which is why Shakyamuni strongly rebukes it.","work:X71n1419")),
  ("occurrence", named("J/J39/J39nB453.xml","0575b12","0575b13","不可如存若亡，不可沉空滯寂。莫坐一知半解，莫以得少為足。","Yuanjie Ying","Recorded Sayings of Yuanjie Ying","元潔瑩禪師語錄","Yuanjie Ying speaks directly from the hall, forbidding an indeterminate half-presence and the 沉 graph variant ‘sink into emptiness and stick in stillness.’","work:J39nB453")),
  ("occurrence", named("J/J26/J26nB188.xml","0759b23","0759b24","不可高標聖境、不可希圖悟門、不可沉空滯寂、不可流浪前塵","Ruibai Mingxue","Recorded Sayings of Ruibai Mingxue","入就瑞白禪師語錄","Ruibai Mingxue speaks directly in an evening instruction, placing the 沉 graph variant among four explicitly forbidden moves.","work:J26nB188")),
 ],
}

COMPILER_ROWS = {
 "t_824cfb1434b1": [
  ("occurrence", {"RelPath":"T/T48/T48n2006.xml","FromLb":"0311b08","ToLb":"0311b10","Kwic":"臨濟宗者。大機大用。脫羅籠出窠臼。虎驟龍奔。星馳電激。轉天關斡地軸。負衝天意氣。用格外提持。卷舒擒縱殺活自在。","AttributionNote":"Source text Eyes of Humans and Devas (人天眼目): compiler Huiyan Zhizhao’s expository Linji-school section joins rolling out and folding up, capturing and releasing, killing and giving life as freely deployed actions; it is not presented as a quotation from Linji Yixuan.","ActorAttribution":{"Status":"narrated","Kind":"compiler exposition","ActorLabel":"Huiyan Zhizhao","ActorRole":"compiler","GrammarEvidence":"The headword appears in continuous expository prose beneath the Linji-school heading, without a speech marker or quotation assigning this clause to Linji Yixuan.","ReviewedBy":"Codex lane-B full-case repair","ReviewedUtc":"2026-07-15T00:00:00Z"},"ContextMasters":[{"MasterName":"Linji Yixuan","Roles":["person-discussed"]}],"DraftActorProof":{"GrammaticalSubject":"the compiler’s characterization of the Linji school","FullCaseDecision":"Huiyan Zhizhao compiles the expository characterization; Linji Yixuan is the school figure discussed, not the exact utterer."},"WorkId":"work:T48n2006"}),
 ]
}

for entry_id, opening in OPENINGS.items():
    path = ROOT / "fresh-build" / "entries" / entry_id / "evidence.draft.json"
    data = json.loads(path.read_text(encoding="utf-8"))
    data["Entry"]["Senses"][0]["ExplanationParts"]["CorpusEarnedOpening"] = opening
    if entry_id == "t_3bf26be0cd43":
        sense = data["Entry"]["Senses"][0]
        sense["PreferredTarget"] = "an opaque black bucket"
        sense["AlternateTargets"] = ["a pitch-black bucket", "the black bucket"]
        sense["SearchAliases"] = ["an opaque black bucket", "a pitch-black bucket", "the black bucket", "black lacquer bucket", "black lacquered bucket"]
        draft = sense["DraftEvidence"]
        draft["ModifierControls"] = [
            "黑 directly supplies the bucket's dark appearance.",
            "漆 is attested inside the fixed modifier 黑漆, but the stored cases do not predicate lacquer manufacture, lacquer coating, or gold-like material composition of the bucket.",
            "The bucket's repeatedly controlled properties are opacity and enclosure: it has a bottom and hoop and is broken, entered, overturned, or made to leap.",
            "The reader-facing target therefore translates the corpus-visible appearance; conventional ‘black lacquer bucket’ remains only a search alias pending direct material evidence."
        ]
        draft["ModifierStudy"] = {
            "Modifier": "黑漆",
            "Decision": "appearance, not demonstrated material",
            "DirectPredication": "No stored complete case says that the bucket is made from lacquer or coated by a named act of lacquering.",
            "MaterialControl": "桶底 and 篐子 establish bucket morphology; 打破, 入, 翻, and 跳 establish actions. These controls support an opaque container image but do not establish its manufacture.",
            "TranslationConsequence": "Translate the visible darkness in the preferred target and retain the conventional lacquer wording for lookup only."
        }
    if entry_id == "t_ddab56ede4ef":
        occ = data["Entry"]["Senses"][0]["Occurrences"][3]
        occ["AttributionNote"] = "Source text 五燈會元: the compiler narrates that a noodle bucket suddenly loses its bottom; the assembly cries out, and Zhenxie Qingliao then says that the event itself should occasion joy before conceding the noodles are lost."
        occ["ActorAttribution"] = {
            "Status": "narrated",
            "Kind": "narrated mechanical event",
            "ActorLabel": "the noodle bucket's bottom",
            "ActorRole": "compiler",
            "GrammarEvidence": "In 忽桶底脫, 桶底 is the grammatical subject of 脫; the following 師曰 separately introduces Zhenxie Qingliao's comment.",
            "ReviewedBy": "Codex lane-B full-case repair",
            "ReviewedUtc": "2026-07-15T00:00:00Z"
        }
        occ["DraftActorProof"] = {
            "GrammaticalSubject": "the noodle bucket's bottom",
            "FullCaseDecision": "五燈會元 narrates the mechanical event first and marks Zhenxie Qingliao's subsequent speech with 師曰; the master is commentator, not actor of 桶底脫."
        }
    if entry_id == "t_12e8cba30de6":
        data["Entry"]["Senses"][0]["ExplanationParts"]["CorpusEarnedOpening"] = "‘Old monk’ is both an ordinary designation for an elderly monk and the conventional first-person self-reference used by named teachers such as Baizhang Huaihai, Linji Yixuan, Zhaozhou Congshen, and Yunju Daoqi. The human role remains the same across narration, address, and self-reference; those grammatical settings do not create separate senses."
        data["Entry"]["Senses"][0]["ExplanationParts"]["EvidenceBody"] = ["Its characteristic Chan deployment is humble first-person self-reference in monastic speech, conventionally translated ‘this old monk’ and grammatically serving as ‘I’ or ‘me.’ Baizhang Huaihai recalls receiving Mazu Daoyi’s shout; Linji Yixuan says he sits securely and distinguishes the visitors who come; Zhaozhou Congshen redirects a public interview by saying, ‘this old monk is hard of hearing; ask loudly’; and Yunju Daoqi addresses the assembly at his final meeting. The same noun phrase also refers in narration to an old monk: Wuzhuo meets one, a raised case begins with an old monk carried by two disciples, and a record introduces ‘an old monk named Puhui.’ Speaker self-reference, third-person reference, and definiteness are contextual readings of the same human-role noun, not different things."]
    if entry_id == "t_5f1287817ebd":
        body = data["Entry"]["Senses"][0]["ExplanationParts"]["EvidenceBody"][0]
        data["Entry"]["Senses"][0]["ExplanationParts"]["EvidenceBody"][0] = body.replace("that the speaker rejects", "that Lia’an Qingyu, Ruibai Mingxue, Fangrong Tongxi, or another named speaker rejects")
        sense = data["Entry"]["Senses"][0]
        sense["Note"] = sense["Note"].replace("逞伎倆", "逞甚伎倆")
        sense["DraftEvidence"]["CounterexampleOrLimit"] = sense["DraftEvidence"]["CounterexampleOrLimit"].replace("逞伎倆", "逞甚伎倆")
    if entry_id == "t_dec67da1f076":
        for row in data["Entry"]["Senses"][0]["Occurrences"]:
            row["AttributionNote"] = row["AttributionNote"].replace("one dharma", "one thing").replace("no dharmas", "the many things as absent")
    if entry_id in ADDITIONS:
        sense = data["Entry"]["Senses"][0]
        addition = dict(ADDITIONS[entry_id])
        work_id = addition.pop("WorkId")
        if not any(o.get("RelPath") == addition["RelPath"] and o.get("Kwic") == addition["Kwic"] for o in sense["Occurrences"]):
            sense["Occurrences"].append(addition)
        if addition["RelPath"] not in sense["SourceTexts"]: sense["SourceTexts"].append(addition["RelPath"])
        if work_id not in sense["DraftEvidence"]["IndependentWorkIds"]: sense["DraftEvidence"]["IndependentWorkIds"].append(work_id)
        sense["DraftEvidence"]["OpeningClaimEvidenceKeys"] = [f"o{n}" for n in range(1, len(sense["Occurrences"]) + 1)]
    for kind, raw in [*MORE_ROWS.get(entry_id, []), *COMPILER_ROWS.get(entry_id, [])]:
        sense = data["Entry"]["Senses"][0]
        row = dict(raw); work_id = row.pop("WorkId")
        field = "Occurrences" if kind == "occurrence" else "ClaimAnchors"
        if not any(o.get("RelPath") == row["RelPath"] and o.get("Kwic") == row["Kwic"] for o in sense.get(field, [])):
            sense.setdefault(field, []).append(row)
        if row["RelPath"] not in sense["SourceTexts"]: sense["SourceTexts"].append(row["RelPath"])
        if work_id not in sense["DraftEvidence"]["IndependentWorkIds"]: sense["DraftEvidence"]["IndependentWorkIds"].append(work_id)
        sense["DraftEvidence"]["OpeningClaimEvidenceKeys"] = [f"o{n}" for n in range(1, len(sense["Occurrences"]) + 1)]
    claim_texts = {
        "默默照處天宇澄秋。照無照功。光影斯斷。": "默默照處天宇澄秋",
        "和尚因甚麼。却力排默照。以為邪非。": "排默照",
        "故云末後一句始到牢關若一見不再見始覺即合本覺者自然透脫無疑": "末後一句始到牢關",
        "透脫末後牢關雲庵正罵洞達歷祖綱宗妙喜猶呵": "透脫末後牢關",
        "要人向黑漆桶裏橫衝直撞，撞來撞去，撞到差別境界尚不知非": "黑漆桶裏",
        "倒拈苕帚柄，痛掃野狐禪。": "痛掃野狐禪",
        "師云：「野狐禪逞甚伎倆？」": "逞甚伎倆",
        "嘗示眾曰。一法若有重重鐵壁銀山。萬法若無處處沈空滯寂。": "一法若有重重鐵壁銀山",
        "惟其沈空滯寂，只知自了，不顧度生，迦文老人所以深所訶責。": "不顧度生",
    }
    sense = data["Entry"]["Senses"][0]
    for anchor in sense.get("ClaimAnchors", []):
        if anchor["Kwic"] in claim_texts: anchor["ClaimText"] = claim_texts[anchor["Kwic"]]
    if entry_id == "t_5f1287817ebd":
        sense["ExplanationParts"]["EvidenceBody"] = [part.replace("逞伎倆", "逞甚伎倆") for part in sense["ExplanationParts"]["EvidenceBody"]]
    # Related forms are supporting evidence, not headword-frequency rows.
    supporting = {
        "t_2facdfa49dd9": {"若識得此一問，便明最初一句，亦明末後一關，百千諸佛、百千戒法，盡從脚跟下流出。":"末後一關",
                              "若是末後一關，正未透在。眾中還有透得末後一關底麼？":"末後一關"},
        "t_5f1287817ebd": {"師室中常舉百丈野狐話問僧，對者多不契。一日自云：百丈野狐，野狐百丈，埋作一坑，伏惟尚享。":"百丈野狐"},
    }.get(entry_id, {})
    kept = []
    for row in sense["Occurrences"]:
        if row["Kwic"] in supporting:
            row["ClaimText"] = supporting[row["Kwic"]]
            if not any(a.get("Kwic") == row["Kwic"] for a in sense.setdefault("ClaimAnchors", [])):
                sense["ClaimAnchors"].append(row)
        else: kept.append(row)
    sense["Occurrences"] = kept
    # Claim-support rows that contain the exact headword are full Occurrences;
    # ClaimText remains as the explicit prose-anchor declaration.
    anchors_kept = []
    for row in sense.get("ClaimAnchors", []):
        if data["Entry"]["SourceTerm"] in row["Kwic"]:
            if not any(o.get("RelPath") == row["RelPath"] and o.get("Kwic") == row["Kwic"] for o in sense["Occurrences"]):
                sense["Occurrences"].append(row)
        else: anchors_kept.append(row)
    sense["ClaimAnchors"] = anchors_kept
    for row in sense["Occurrences"]:
        if entry_id == "t_b26bfa9e399e" and ("回光返照" in row["Kwic"] or "返照回光" in row["Kwic"]):
            row["EvidenceRole"] = "variant"; row["VariantForm"] = "回光返照" if "回光返照" in row["Kwic"] else "返照回光"
        if entry_id == "t_dec67da1f076" and "沉空滯寂" in row["Kwic"]:
            row["EvidenceRole"] = "variant"; row["VariantForm"] = "沉空滯寂"
    if entry_id == "t_824cfb1434b1":
        sense["ClaimAnchors"] = [a for a in sense.get("ClaimAnchors", []) if a.get("ClaimText") != "卷舒擒縱"]
        for row in sense["Occurrences"]:
            if row.get("ActorAttribution", {}).get("ActorLabel") == "Huiyan Zhizhao":
                tail = row["AttributionNote"].split(": ", 1)[-1]
                tail = re.sub(r"^(?:Huiyan Zhizhao is the exact textual actor as compiler\. )?(?:This is compiler narration under the Linji-school heading\. )?", "", tail)
                row["AttributionNote"] = "Source text Eyes of Humans and Devas (人天眼目): This compiler narration under the Linji-school heading is Huiyan Zhizhao’s textual statement. " + tail
    for row in [*sense.get("Occurrences", []), *sense.get("ClaimAnchors", [])]:
        if row.get("MasterName") and "is the exact speaker" not in row["AttributionNote"]:
            row["AttributionNote"] = row["AttributionNote"].replace(": ", f": {row['MasterName']} is the exact speaker. ", 1)
    sense["DraftEvidence"]["OpeningClaimEvidenceKeys"] = [f"o{n}" for n in range(1, len(sense["Occurrences"]) + 1)]
    for sense in data["Entry"]["Senses"]:
        for row in [*(sense.get("Occurrences") or []), *(sense.get("ClaimAnchors") or [])]:
            row["AttributionNote"] = english_first(row["AttributionNote"])
            proof = row.get("DraftActorProof") or {}
            for field in ("SpeechFrame", "FullCaseDecision"):
                if proof.get(field): proof[field] = english_first(proof[field])
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
