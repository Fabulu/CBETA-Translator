#!/usr/bin/env python3
"""Incremental exact-turn repairs for f004 lane B 1001-1010.

This file is deliberately resumable: REPAIRED records the rows whose full cases
have been adjudicated. Run only after extending REPAIRED and the matching branch.
"""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]
REVIEWER = "Codex f004 lane B 1001-1100 repair author"
WHEN = "2026-07-15T12:45:00Z"
REPAIRED = {
    "t_efa921d8f97a", "t_2ddd493fc9b0", "t_df2096b961c1", "t_486aaf7fbce8",
    "t_8beda961c75a", "t_1095b3f1544e", "t_7e7472becb31", "t_f54129a637ae",
    "t_420d43d8c61c", "t_da72db7aa635",
}


def root(d):
    return d.get("Entry", d)


def occurrences(d):
    return [o for s in root(d)["Senses"] for o in s["Occurrences"]]


def named(o, name, note, contexts=()):
    o.pop("ActorAttribution", None)
    o["MasterName"] = name
    o["ContextMasters"] = [{"MasterName": name, "Roles": ["utterer"]}]
    role_map = {"subject": "person-discussed", "earlier-quoted-voice": "case-figure", "later-commentator": "commentator"}
    for context_name, role in contexts:
        if context_name != name:
            o["ContextMasters"].append({"MasterName": context_name, "Roles": [role_map.get(role, role)]})
    o["AttributionNote"] = note
    o["DraftActorProof"] = {
        "ExactHeadwordClause": o["Kwic"],
        "SpeechFrame": note,
        "FullCaseDecision": f"The complete case assigns the headword-bearing utterance to {name}; contextual figures are stored separately.",
    }


def unnamed(o, label, role, note, contexts=()):
    o.pop("MasterName", None)
    role_map = {"subject": "person-discussed", "earlier-quoted-voice": "case-figure", "later-commentator": "commentator"}
    o["ContextMasters"] = [{"MasterName": n, "Roles": [role_map.get(r, r)]} for n, r in contexts]
    o["ActorAttribution"] = {
        "Status": "reviewed-unnamed",
        "Kind": "unnamed participant",
        "ActorLabel": label,
        "ActorRole": role,
        "RungsChecked": RUNGS,
        "GrammarEvidence": note,
        "ReviewedBy": REVIEWER,
        "ReviewedUtc": WHEN,
        "AuthoredVoiceRiskReviewed": True,
    }
    o["AttributionNote"] = note
    o["DraftActorProof"] = {"GrammaticalSubject": label, "FullCaseDecision": note}


def nonhuman(o, status, kind, label, role, note, contexts=()):
    o.pop("MasterName", None)
    role_map = {"subject": "person-discussed", "earlier-quoted-voice": "case-figure", "later-commentator": "commentator"}
    o["ContextMasters"] = [{"MasterName": n, "Roles": [role_map.get(r, r)]} for n, r in contexts]
    o["ActorAttribution"] = {
        "Status": status,
        "Kind": kind,
        "ActorLabel": label,
        "ActorRole": role,
        "RungsChecked": RUNGS,
        "GrammarEvidence": note,
        "ReviewedBy": REVIEWER,
        "ReviewedUtc": WHEN,
        "AuthoredVoiceRiskReviewed": True,
    }
    o["AttributionNote"] = note
    o["DraftActorProof"] = {"GrammaticalSubject": label, "FullCaseDecision": note}


def repair_1001(d):
    sense = root(d)["Senses"][0]
    sense["PreferredTarget"] = "one's own household treasure"
    sense["AlternateTargets"] = ["one's own treasure", "the treasure at home"]
    sense["SearchAliases"] = ["household treasure", "one's own treasure", "home treasure", "family treasure"]
    sense["Explanation"] = (
        "One's own household treasure is what is already one's own, rather than something carried in through another person's gate. "
        "Zhenjing Kewen quotes the contrast 'what enters through the gate is not household treasure' and immediately asks what the treasure is; "
        "Baichi Yuanshuo says he brings it out in a public address, while Zhongfeng Mingben calls sights, sounds, and the whole field before a person one's own household treasure. "
        "Other records test it with an unpolished jade and warn that inherited words can leave a person counting somebody else's wealth. The domestic image therefore marks direct possession, not money or a concealed jewel."
    )
    sense["Note"] = "Seven witnesses across independent works include direct definition, public deployment, verse, interview, and later explanation."
    o = occurrences(d)
    o[0]["Kwic"] = "古德道，從門入者，不是家珍。又作麼生是家珍？驀拈拄杖，召大眾曰：還見麼？"
    named(o[0], "Zhenjing Kewen", "Source text (五燈全書): Zhenjing Kewen quotes the inherited contrast and himself asks what 家珍 is in an上堂 address.")
    named(o[1], "Buhui", "Source text (不會禪師語錄): Buhui says that host and guest open the treasury face to face and values recognizing one's own household treasure.")
    named(o[2], "Liao'an Qingyu", "Source text (列祖提綱錄): the unit explicitly identifies Liao'an Qingyu before his上堂; he says Mingjue opened the treasury and brought out the household treasure.")
    nonhuman(o[3], "narrated", "compiled verse", "the verse compiler", "compiler", "Source text (宗鑑法林): 家珍 occurs in an unattributed compiled verse following the Linji dossier; the stored unit does not name a verse author.", (("Linji Yixuan", "subject"),))
    named(o[4], "Baichi Yuanshuo", "Source text (百癡禪師語錄): Baichi Yuanshuo says he brings out the household treasure in an上堂 address.")
    o[5]["Kwic"] = "問：抱璞投師時如何？師曰：不是自家珍。曰：如何是自家珍？師曰：不琢不成器。"
    o[5]["FromLb"] = "0079b20"
    unnamed(o[5], "an unnamed monk", "questioner", "Source text (五燈嚴統): an unnamed monk utters both headword-bearing questions; Caoshan Qianghui Zhiju answers them.", (("Caoshan Qianghui Zhiju", "respondent"),))
    named(o[6], "Zhongfeng Mingben", "Source text (天目中峰廣錄): Zhongfeng Mingben says mountains, rivers, sights, and sounds are all one's own household treasure.")


def repair_1003(d):
    sense = root(d)["Senses"][0]
    sense["PreferredTarget"] = "the mechanism of the iron ox"
    sense["AlternateTargets"] = ["the iron ox's mechanism", "the working of the iron ox"]
    sense["SearchAliases"] = ["iron ox mechanism", "iron bull mechanism", "Fengxue iron ox"]
    sense["Explanation"] = (
        "The mechanism of the iron ox is Fengxue Yanzhao's image for the ancestral seal as a working that cannot be handled by choosing motion or rest. "
        "His case says that when it goes the seal holds fast, when it stays the seal breaks, and then asks what happens when it neither goes nor stays. "
        "Yuanwu Keqin calls this mechanism impossible to cage; later masters quote the case in public interviews and demand an answer beyond merely repeating its terms. "
        "The iron ox supplies apparent immovability, while 機 makes that image an active test rather than a static object."
    )
    sense["Note"] = "Six witnesses distinguish Fengxue's source case, later quotation, direct questioning, and teaching-seat redeployment."
    o = occurrences(d)
    named(o[0], "Mian Xianjie", "Source text (列祖提綱錄): Mian Xianjie delivers the imperial-opening上堂 and utters the iron-ox formulation.")
    named(o[1], "Lingyan Chongque", "Source text (五燈全書): Lingyan Chongque utters the formulation in his explicitly headed上堂.")
    named(o[2], "Yuanwu Keqin", "Source text (圓悟佛果禪師語錄): Yuanwu Keqin says the iron-ox mechanism cannot be caged.")
    named(o[3], "Fengxue Yanzhao", "Source text (天岸昇禪師語錄): Tian'an Sheng quotes Fengxue Yanzhao's complete iron-ox case; Fengxue is the historical utterer.", (("Tian'an Sheng", "later-quoter"),))
    o[4]["Kwic"] = "進云記得風穴抂郢州官衙陞座云祖師心印狀似鐵牛之機去即印住住即印破意旨如何師云不是知音者徒勞話歲寒"
    o[4]["FromLb"] = "0163b02"
    unnamed(o[4], "an unnamed monk", "questioner", "Source text (弘覺忞禪師語錄): an unnamed monk quotes Fengxue's line while asking its meaning; Hongjue Min answers.", (("Fengxue Yanzhao", "earlier-quoted-voice"), ("Hongjue Min", "respondent")))
    named(o[5], "Fengxue Yanzhao", "Source text (碧巖錄): the case explicitly introduces Fengxue Yanzhao's上堂 and quotes his iron-ox formulation; Yuanwu Keqin is the later commentator.", (("Yuanwu Keqin", "later-commentator"),))


def repair_1004(d):
    sense = root(d)["Senses"][0]
    sense["PreferredTarget"] = "the monastery supervisor"
    sense["AlternateTargets"] = ["monastic administrator", "monastery superintendent"]
    sense["SearchAliases"] = ["monastery supervisor", "monastery administrator", "superintendent", "jiansi"]
    sense["Explanation"] = (
        "The monastery supervisor is the administrator who records, allocates, and safeguards communal property and practical business. "
        "A dying elder orders the supervisor to record the division of robes and bowls; public records thank incoming and outgoing supervisors, and named officeholders request formal addresses or receive verses and memorial rites. "
        "The office belongs to the monastery's administrative side: being 監寺 does not by itself make its holder the presiding teacher."
    )
    sense["Note"] = "Seven witnesses cover property accounting, public office transitions, commissioned addresses, a birthday verse, and a named officeholder."
    o = occurrences(d)
    named(o[0], "Daoji", "Source text (濟顛道濟禪師語錄): Daoji instructs that the monastery supervisor record the division of his robes and bowls.")
    nonhuman(o[1], "impersonal", "occasion heading", "the impersonal occasion heading", "compiler", "Source text (即非禪師全錄): the heading records that supervisor Tan Sui requested the上堂; Jifei Ruyi speaks in the ensuing address.", (("Jifei Ruyi", "record-owner"),))
    # Replace the table-of-contents witness with the same work's substantive office formula.
    o[2]["Kwic"] = "圓悟勤禪師謝監寺，上堂：滴水氷生，百了千當。鐵作脊梁骨，金鑄堅實心。荷負叢林，贊弼知識。"
    o[2]["FromLb"] = "0250b04"; o[2]["ToLb"] = "0250b06"
    named(o[2], "Yuanwu Keqin", "Source text (列祖提綱錄): the unit explicitly identifies Yuanwu Keqin before his上堂 thanking the monastery supervisor and describing the office's support of the community.")
    nonhuman(o[3], "impersonal", "occasion heading", "the impersonal occasion heading", "compiler", "Source text (希叟紹曇禪師廣錄): the heading thanks the former and new monastery supervisors before Xisou Shaotan's上堂.", (("Xisou Shaotan", "record-owner"),))
    o[4]["Kwic"] = "和監寺四旬和監寺四旬四十稱強仕，風光汝自知，倚笻何以贈？千古襲楊岐。"
    named(o[4], "Baichi Yuanshuo", "Source text (百癡禪師語錄): Baichi Yuanshuo addresses a monastery supervisor in a fortieth-birthday verse.")
    nonhuman(o[5], "impersonal", "occasion heading", "the impersonal occasion heading", "compiler", "Source text (山暉禪師語錄): the heading records that supervisor Shiqin requested the上堂; Shanhui speaks in the following exchange.", (("Shanhui", "record-owner"),))
    o[6]["Kwic"] = "雨樹愚監寺雨樹愚監寺不薦當陽句，徒誇鷁萬里，蓬吞四海風，纜拽千江水"
    named(o[6], "Zhangxue Tongzui", "Source text (昭覺丈雪醉禪師語錄): Zhangxue Tongzui titles and addresses his verse to Yushu Yu, identified as monastery supervisor.")


def repair_1005(d):
    sense = root(d)["Senses"][0]
    sense["PreferredTarget"] = "Tao Yuanming, the poet who returned home"
    sense["AlternateTargets"] = ["Tao Yuanming", "Tao Qian"]
    sense["SearchAliases"] = ["Tao Yuanming", "Tao Qian", "poet who returned home", "chrysanthemum poet"]
    sense["Explanation"] = (
        "Tao Yuanming is the poet whom these records recognize through wine, chrysanthemums, and his decision to return home. "
        "Masters make that literary figure work in public speech: 'if it were Tao Yuanming, he would frown and go home' dismisses a labored formulation, while other addresses pair his return with withdrawal from a crowded or defiled scene. "
        "He is therefore a pre-Chan figure as deployed by Chan speakers, not a lineage master and not merely a biographical name."
    )
    sense["Note"] = "Five independent witnesses show the return-home formula, wine and chrysanthemums, and named deployment in public addresses."
    o = occurrences(d)
    named(o[0], "Baofang Jin", "Source text (五燈全書): Baofang Jin invokes Tao Yuanming and his Return Home rhapsody in his own recorded speech.")
    named(o[1], "Langting Jingting", "Source text (雲溪俍亭挺禪師語錄): Langting Jingting says Tao Yuanming frowns and returns home in a formal address.")
    named(o[2], "Shengfa Fa", "Source text (宗鑑法林): the compiler explicitly introduces Shengfa Fa before the saying 'if it were Tao Yuanming, he would frown and go home.'")
    named(o[3], "Shending Yikui", "Source text (神鼎一揆禪師語錄): Shending Yikui utters the Tao Yuanming line in an上堂 address.")
    named(o[4], "Sanyi Mingyu", "Source text (三宜盂禪師語錄): Sanyi Mingyu invokes Tao Yuanming drinking wine in an上堂 address.")


def repair_1002(d):
    sense = root(d)["Senses"][0]
    sense["PreferredTarget"] = "Continuation of the Lamp Record"
    sense["AlternateTargets"] = ["Continuation Lamp Record", "a continuation of a lamp record"]
    sense["SearchAliases"] = ["Continuation of the Lamp Record", "continued lamp record", "later lamp history"]
    sense["Explanation"] = (
        "A ‘Continuation of the Lamp Record’ is a later lineage history that extends earlier lamp records with additional "
        "masters, cases, and succession claims. The title names the Song compilation submitted by Fogu Weibai, while Guxue "
        "Zhe describes compiling Yuan- and Ming-period masters into a later continuation. Shuijian Huihai and Langting Jingting "
        "cite or dispute what such a record says about Xuedou Chongxian’s lineage. In these records the title is therefore not "
        "only bibliographic: its contents are evidence contested in public lineage adjudication."
    )
    sense["Note"] = "Five witnesses from five independent works cover the title, a compiler biography, a first-person compilation project, and two lineage disputes."
    o = occurrences(d)
    nonhuman(o[0], "impersonal", "bibliographic title heading", "the impersonal work-title heading", "compiler", "Source text (建中靖國續燈錄): the exact headword appears in the work's bibliographic title heading; no human actor utters it.")
    nonhuman(o[1], "narrated", "lamp-record biography", "the Jiatai lamp-record compiler", "compiler", "Source text (嘉泰普燈錄): the lamp-record compiler narrates that Fogu Weibai submitted the thirty-fascicle 宗門續燈錄 and received an imperial preface.")
    named(o[2], "Guxue Zhe", "Source text (古雪哲禪師語錄): Guxue Zhe uses the headword in his first-person account of compiling Yuan- and Ming-period masters into a continuation record.")
    named(o[3], "Shuijian Huihai", "Source text (天王水鑑海和尚六會錄): Shuijian Huihai cites Fogu Weibai's 續燈錄 while arguing Xuedou Chongxian's lineage placement.")
    named(o[4], "Langting Jingting", "Source text (雲溪俍亭挺禪師語錄): Langting Jingting names and disputes the claimed contents of Fogu Weibai's 續燈錄 in his own extended argument.")


def repair_1009(d):
    sense = root(d)["Senses"][0]
    sense["PreferredTarget"] = "I am not it now"
    sense["AlternateTargets"] = ["I am not that now", "I am now not it"]
    sense["SearchAliases"] = [
        "I am not it now",
        "it is now exactly me",
        "Dongshan's reflection verse",
        "Dongshan water reflection",
    ]
    sense["Explanation"] = (
        "“I am not it now” is the second half of Dongshan Liangjie’s paired line, “It is now exactly me; "
        "I am not it now” (渠今正是我，我今不是渠), in his verse on seeing his reflection while crossing water. "
        "The records repeatedly quote the line, ask what it means, and use it to test later speakers: an unnamed monk "
        "asks Yingning Jing directly about it, while Zhean Fan and Zongbao Daodu raise Dongshan’s verse in later addresses. "
        "The entry therefore keeps the first-person contrast and its case deployment without turning ‘it’ into an imported doctrine."
    )
    sense["Note"] = "Five exact witnesses preserve Dongshan's line as quotation, direct question, or later case citation; parallel recensions are not treated as different sayings."
    o = occurrences(d)
    named(o[0], "Dongshan Liangjie", "Source text (廬山天然禪師語錄): the record master explicitly quotes Dongshan Liangjie’s reflection verse; Dongshan is the exact historical utterer of 我今不是渠.")
    unnamed(o[1], "an unnamed monk", "questioner", "Source text (攖寧靜禪師語錄): an unnamed monk asks the exact headword-bearing question; Yingning Jing answers in the following 師云 turn.", (("Yingning Jing", "respondent"),))
    named(o[2], "Dongshan Liangjie", "Source text (蔗菴範禪師語錄): Zhean Fan raises Xiaoshan’s quotation of Dongshan Liangjie’s exact line; Dongshan is the quoted historical utterer.", (("Zhean Fan", "later-quoter"),))
    named(o[3], "Dongshan Liangjie", "Source text (宗寶道獨禪師語錄): Zongbao Daodu quotes Dongshan Liangjie’s complete reflection verse and comments on it; Dongshan is the quoted historical utterer.", (("Zongbao Daodu", "later-quoter"),))
    named(o[4], "Dongshan Liangjie", "Source text (五燈全書(第1卷-第33卷)): the biography explicitly introduces Dongshan’s water-reflection verse and quotes 我今不是渠; Dongshan Liangjie is the historical utterer.")


def repair_1006(d):
    sense = root(d)["Senses"][0]
    sense["PreferredTarget"] = "Fenggan"
    sense["AlternateTargets"] = ["Master Fenggan", "Fenggan of Tiantai"]
    sense["SearchAliases"] = ["Fenggan", "Feng-kan", "Tiantai Fenggan", "Fenggan Hanshan Shide"]
    sense["Explanation"] = (
        "Fenggan is the Tiantai eccentric whom Chan records place with Hanshan and Shide. His dossier shows him answering Hanshan's question about an unpolished mirror, travelling toward Wutai, riding a tiger, and identifying Hanshan and Shide when Lüqiu Yin asks for instruction. "
        "Later masters invoke the trio in public addresses, questions, and portrait verses—sometimes treating Fenggan's additional words as needless speech. This is Fenggan as a repeatedly deployed Chan figure, not a general saint's biography or a lineage-master claim."
    )
    sense["Note"] = "Six witnesses span lamp biography, inherited case, public question, compiler narration, and a later portrait verse."
    o = occurrences(d)
    named(o[0], "Guizong Huitong", "Source text (五燈全書): Guizong Huitong raises the scene of Hanshan and Shide bowing to Fenggan in an上堂 address.")
    nonhuman(o[1], "narrated", "case compiler", "the case compiler", "compiler", "Source text (古尊宿語錄): the compiler narrates Lüqiu Yin's question to Fenggan while introducing a verse on the three mysteries.", (("Fenggan", "subject"),))
    unnamed(o[2], "an unnamed monk", "questioner", "Source text (翼菴禪師語錄): an unnamed monk asks why Fenggan must add more words; Yian Xingtao answers.", (("Yian Xingtao", "respondent"), ("Fenggan", "subject")))
    nonhuman(o[3], "narrated", "lamp-record compiler", "the Jingde lamp-record compiler", "compiler", "Source text (景德傳燈錄): the compiler lists Fenggan among the recorded responsive figures and begins the biographical dossier.", (("Fenggan", "subject"),))
    o[4]["Kwic"] = "天台豐干禪師天台豐干禪師者不知何許人也居天台山國清寺剪髮齊眉人或問佛理止荅隨時二字常誦唱道歌乘虎入松門"
    nonhuman(o[4], "narrated", "lamp-record biography", "the lamp-record compiler", "compiler", "Source text (傳燈玉英集): the compiler introduces Fenggan's Tiantai dossier, his terse replies, songs, and tiger-riding.", (("Fenggan", "subject"),))
    o[5]["Kwic"] = "豐干豐干這阿師，嬾似傲，帝王來也不起，佛祖薄而不做。謂是豐干耶，何異眼中著屑？"
    named(o[5], "Jifei Ruyi", "Source text (即非禪師全錄): Jifei Ruyi addresses and questions the received image of Fenggan in a portrait verse.", (("Fenggan", "subject"),))


def repair_1007(d):
    sense = root(d)["Senses"][0]
    sense["PreferredTarget"] = "a ghost cave"
    sense["AlternateTargets"] = ["the ghosts' cave", "a dead cave"]
    sense["SearchAliases"] = ["ghost cave", "ghosts' cave", "living in a ghost cave", "making a livelihood in a ghost cave"]
    sense["Explanation"] = (
        "A ghost cave is a dark, dead enclosure in which someone settles and tries to make a livelihood. Chan speakers bend the literal cave into a verdict on being trapped in blank stillness, fixed understanding, or the very attempt to lodge in neither coming nor going. "
        "Dahui Zonggao applies it to students who suppress movement and then sit unable to turn; other commentators say a formulation leads people into 'making a livelihood in a ghost cave.' The phrase condemns an enclosed dead end—it does not name a productive retreat."
    )
    sense["Note"] = "Seven witnesses include direct public warning, verse, named case criticism, and two Dahui deployments; the shared construction is 鬼窟裏作活計."
    o = occurrences(d)
    named(o[0], "Zhongji Kezun", "Source text (五燈全書): Zhongji Kezun says that even the three-thousand-great-thousand world is only a ghost cave in his上堂.")
    nonhuman(o[1], "narrated", "compiled verse", "the verse compiler", "compiler", "Source text (宗鑑法林): the compiler places the headword in a verse on 圓覺 that contrasts the darkest ghost cave with opening the skylight.")
    named(o[2], "Chengshan Qia", "Source text (宗門拈古彙集): Chengshan Qia says the earlier formulation leads people into making a livelihood in a ghost cave.")
    named(o[3], "Yisheng Ying", "Source text (指月錄): Yisheng Ying says Nanquan's answer makes a livelihood in a ghost cave.", (("Nanquan Puyuan", "subject"),))
    named(o[4], "Dahui Zonggao", "Source text (大慧普覺禪師普說): Dahui Zonggao says students who extinguish movement and sit in stillness are sitting in a ghost cave and cannot turn.")
    named(o[5], "Dahui Zonggao", "Source text (大慧普覺禪師語錄): Dahui Zonggao warns his audience not to calculate in a ghost cave.")
    named(o[6], "Letan Xiang", "Source text (五燈嚴統): Letan Xiang says that remaining in neither coming nor going is precisely ghost-cave livelihood.")


def repair_1008(d):
    sense = root(d)["Senses"][0]
    sense["PreferredTarget"] = "the quarters supervisor"
    sense["AlternateTargets"] = ["common-quarters supervisor", "dormitory supervisor"]
    sense["SearchAliases"] = ["quarters supervisor", "dormitory supervisor", "liaoyuan", "common hall supervisor"]
    sense["Explanation"] = (
        "The quarters supervisor is the officer responsible for the monks' common quarters. Monastic rules assign this officer the quarters' texts and goods, tea, fuel, cleaning, washing, grooming equipment, doors, boards, and attendance; one rule has the supervisor close the quarters door and report that the assembly has gone to seated dhyana. "
        "Records also name individual quarters supervisors as requesters or recipients of funeral rites. The title is an administrative office, not a teaching rank."
    )
    sense["Note"] = "Five witnesses join two institutional rulebooks with public thanks and rites for named officeholders."
    o = occurrences(d)
    nonhuman(o[0], "narrated", "monastic rule", "the monastic-rule compiler", "compiler", "Source text (禪林備用清規): the rule directs the quarters supervisor to close the common-quarters door and report attendance.")
    o[1]["Kwic"] = "寮元寮元掌眾寮之經文什物。茶湯柴炭。請給供需。洒掃浣濯。淨髮椸巾之類"
    nonhuman(o[1], "narrated", "monastic rule", "the monastic-rule compiler", "compiler", "Source text (勅修百丈清規): the rule defines the quarters supervisor's custody and service duties.")
    o[2]["Kwic"] = "為寮元古樸火為寮元古樸火舉火炬，云：「小雪已去，大雪將來，寮元去住，不假安排。」"
    named(o[2], "Hefeng Wucheng", "Source text (鶴峰禪師語錄): Hefeng Wucheng identifies Gupu as quarters supervisor and utters the headword during the cremation rite.")
    nonhuman(o[3], "impersonal", "occasion heading", "the impersonal occasion heading", "compiler", "Source text (介石智朋禪師語錄): the heading thanks the quarters supervisor before Jieshi Zhipeng's上堂.", (("Jieshi Zhipeng", "record-owner"),))
    o[4]["Kwic"] = "成唯寮元火。「生來死去原一貫，亙古亙今無間斷。放下許多閒念頭，全身證入空王殿。成唯寮元還會麼？"
    named(o[4], "Wanru Tongwei", "Source text (萬如禪師語錄): Wanru Tongwei identifies Chengwei as quarters supervisor and addresses him in the cremation rite.")


def repair_1010(d):
    sense = root(d)["Senses"][0]
    sense["PreferredTarget"] = "to hold fast and block off"
    sense["AlternateTargets"] = ["to bar completely", "to hold the crossing shut"]
    sense["SearchAliases"] = ["hold fast", "block off", "bar the pass", "hold the strategic crossing"]
    sense["Explanation"] = (
        "To 把斷 is to hold something fast so that passage is completely blocked. Its characteristic Chan construction is 把斷要津, 'hold the strategic crossing shut': masters use it for command of the critical point, while monks quote it in public interviews to ask whether any way of helping or escaping remains. "
        "Verses make the same pressure concrete by saying the heavy crossing is barred and difficult to pass. The word names the blocking action, not a separate metaphysical gate."
    )
    sense["Note"] = "Seven witnesses cover three live monk questions, public addresses, a named verse, and the inherited 'strategic crossing' construction."
    o = occurrences(d)
    unnamed(o[0], "an unnamed monk", "questioner", "Source text (圓悟佛果禪師語錄): an unnamed monk asks whether holding the strategic crossing shut still leaves any way to help people; Yuanwu Keqin answers.", (("Yuanwu Keqin", "respondent"),))
    named(o[1], "Baoen Tan", "Source text (五燈全書): Baoen Tan says one who gets it directs affairs and holds the strategic crossing shut.")
    named(o[2], "Mian Xianjie", "Source text (列祖提綱錄): Mian Xianjie says the forbidden city is held shut, sealed without a breath of passage, in an imperial occasion address.")
    named(o[3], "Liao'an Qingyu", "Source text (了菴清欲禪師語錄): Liao'an Qingyu pairs overturning sea and mountains with holding the strategic crossing shut.")
    unnamed(o[4], "an unnamed monk", "questioner", "Source text (廣福山勝覺寺密印禪師語錄): an unnamed monk quotes Yunmen holding the strategic crossing shut and says there is still no jumping out; Miyin answers.", (("Yunmen Wenyan", "earlier-quoted-voice"), ("Miyin", "respondent")))
    named(o[5], "Langya Huijue", "Source text (宗鑑法林): the compiler explicitly introduces Langya Huijue before his verse saying the heavy crossing is barred and hard to pass.")
    unnamed(o[6], "an unnamed monk", "questioner", "Source text (古林清茂禪師語錄): an unnamed monk says the strategic crossing is held shut without leakage; Gulin Qingmao answers.", (("Gulin Qingmao", "respondent"),))


def repair(d, entry_id):
    if entry_id == "t_efa921d8f97a":
        repair_1001(d)
    elif entry_id == "t_2ddd493fc9b0":
        repair_1002(d)
    elif entry_id == "t_df2096b961c1":
        repair_1003(d)
    elif entry_id == "t_486aaf7fbce8":
        repair_1004(d)
    elif entry_id == "t_8beda961c75a":
        repair_1005(d)
    elif entry_id == "t_1095b3f1544e":
        repair_1006(d)
    elif entry_id == "t_7e7472becb31":
        repair_1007(d)
    elif entry_id == "t_f54129a637ae":
        repair_1008(d)
    elif entry_id == "t_420d43d8c61c":
        repair_1009(d)
    elif entry_id == "t_da72db7aa635":
        repair_1010(d)
    else:
        raise KeyError(entry_id)
    # Keep the evidence-first worksheet fields aligned with the repaired public prose;
    # the compiler intentionally derives Explanation from these fields.
    for sense in root(d)["Senses"]:
        explanation = sense.get("Explanation", "").strip()
        if not explanation:
            continue
        cut = explanation.find(". ")
        opening = explanation if cut < 0 else explanation[:cut + 1]
        body = explanation if cut < 0 else explanation[cut + 2:]
        sense["ExplanationParts"] = {"CorpusEarnedOpening": opening, "EvidenceBody": [body]}
        sense.setdefault("DraftEvidence", {})["ZenBend"] = body


for entry_id in sorted(REPAIRED):
    for filename in ("entry.v2.json", "evidence.draft.json"):
        path = ROOT / "fresh-build" / "entries" / entry_id / filename
        data = json.loads(path.read_text(encoding="utf-8"))
        repair(data, entry_id)
        path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

print(json.dumps({"repaired": len(REPAIRED), "ids": sorted(REPAIRED)}))
