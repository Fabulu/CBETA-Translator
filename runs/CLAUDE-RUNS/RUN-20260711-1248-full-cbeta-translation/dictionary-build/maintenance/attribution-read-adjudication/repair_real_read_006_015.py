import json
from pathlib import Path

BUILD = Path(__file__).resolve().parents[2]
p = BUILD / "fresh-build/entries/t_8d9558f7f8a5/entry.v2.json"
d = json.loads(p.read_text(encoding="utf-8"))
os = [o for s in d["Senses"] for o in s["Occurrences"]]
os[0].update(Kwic="師云隨他隨他去也僧無語", FromLb="0605a12", ToLb="0605b01")
os[1].update(Kwic="隋云。隨他去。大隋真如和尚", FromLb="0169a20", ToLb="0169a22")
os[2].update(Kwic="隨云。隨他去僧問龍濟。", FromLb="0247a10", ToLb="0247a11")
for s in d["Senses"]:
    s["SourceTexts"] = list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

p = BUILD / "fresh-build/entries/t_f9bb8b44b32f/entry.v2.json"
d = json.loads(p.read_text(encoding="utf-8"))
os = [o for s in d["Senses"] for o in s["Occurrences"]]
os[1].update(Kwic="延壽堂主看視病僧。", FromLb="1133a09", ToLb="1133a10")
os[4]["MasterName"] = "Mingjue Cong"
os[4].pop("ActorAttribution", None)
os[4]["ContextMasters"] = [{"MasterName": "Mingjue Cong", "Roles": ["utterer", "record-owner"]}]
os[4]["AttributionNote"] = "Source text (明覺聰禪師語錄): Mingjue Cong recounts returning to the infirmary with a grave illness and remaining there for three months."
d["Senses"][0]["Explanation"] = d["Senses"][0]["Explanation"].replace(
    "biographies place seriously ill masters there for months",
    "Mingjue Cong recounts spending three months there with a grave illness",
)
for s in d["Senses"]:
    s["SourceTexts"] = list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

p = BUILD / "fresh-build/entries/t_7f398fa1a823/entry.v2.json"
d = json.loads(p.read_text(encoding="utf-8"))
os = [o for s in d["Senses"] for o in s["Occurrences"]]
canonical = {
    0: "Fayun Faxiu",
    1: "Shuilao Heshang",
    3: "Tianyin Yuanxiu",
    4: "Tianyin Yuanxiu",
    5: "Fayan Wenyi",
    7: "Wuxie Lingmo",
}
for i, name in canonical.items():
    os[i]["MasterName"] = name
    os[i].pop("ActorAttribution", None)
    os[i]["ContextMasters"] = [{"MasterName": name, "Roles": ["utterer"]}]
os[0]["AttributionNote"] = "Source text (五燈全書): Fayun Faxiu asks his assembly whether they can identify the wind's color."
os[1]["AttributionNote"] = "Source text (古尊宿語錄): Shuilao Heshang rises laughing after Mazu's kick and says that innumerable meanings are all present on a hair-tip if one recognizes their source."
os[2]["MasterName"] = None
os[2]["ActorAttribution"] = {
    "Status": "reviewed-unnamed",
    "Kind": "unattributed capping verse",
    "ActorLabel": "the unattributed verse voice",
    "ActorRole": "verse-author",
    "RungsChecked": ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"],
    "GrammarEvidence": "The verse asking who recognizes spring's face follows Pingyang Min's comment in the 法華 section, but the source supplies no personal verse author.",
    "ReviewedBy": "Codex cohorts 1-3 v6 full-case hand read",
    "ReviewedUtc": "2026-07-15T22:30:00Z",
}
os[2]["ContextMasters"] = [{"MasterName": "Pingyang Min", "Roles": ["commentator", "case-figure"]}]
os[2]["AttributionNote"] = "Source text (宗鑑法林): an unattributed capping verse in the Lotus Scripture section asks who recognizes the face of spring; Pingyang Min's preceding comment is a separate turn."
os[3]["AttributionNote"] = "Source text (天隱修禪師語錄): Tianyin Yuanxiu raises his staff and asks the monk whether he recognizes it."
os[4]["AttributionNote"] = "Source text (天隱和尚語錄): Tianyin Yuanxiu raises his staff and asks the monk whether he recognizes it."
os[5]["AttributionNote"] = "Source text (五燈嚴統): Fayan Wenyi points to the bench and says that recognizing it leaves room all around."
os[7]["AttributionNote"] = "Source text (五燈會元): Wuxie Lingmo answers that no one recognizes what is greater than heaven and earth."
d["Senses"][0]["RelatedMasters"] = ["Fayun Faxiu", "Shuilao Heshang", "Pingyang Min", "Tianyin Yuanxiu", "Fayan Wenyi", "Yongjue Yuanxian", "Wuxie Lingmo"]
for s in d["Senses"]:
    s["SourceTexts"] = list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

p = BUILD / "fresh-build/entries/t_ba4b06d70a44/entry.v2.json"
d = json.loads(p.read_text(encoding="utf-8"))
os = [o for s in d["Senses"] for o in s["Occurrences"]]
reviewed = "2026-07-15T23:10:00Z"

# The original witness fused two separate 坐具 actions by Fuchang Xin.
os[0].update(Kwic="師提起坐具，僧曰：雲生片片，雨點霏霏。", FromLb="0009a08", ToLb="0009a09")
os[0]["AttributionNote"] = "Complete case in the Complete Lamp Collection (五燈全書), Fuchang Xin section: the compiler narrates Fuchang Xin lifting the sitting cloth before the monk answers; Fuchang does not utter the headword."
os[0]["ActorAttribution"].update(
    ActorLabel="the compiler of Fuchang Xin's record",
    GrammarEvidence="師提起坐具 is third-person case narration: 師 refers to Fuchang Xin, while the compiler supplies the headword-bearing clause.",
    ReviewedBy="Codex cohorts 1-3 v6 full-case hand read",
    ReviewedUtc=reviewed,
)

os[1]["ContextMasters"] = [{"MasterName": "Fayan Wenyi", "Roles": ["person-described", "record-owner"]}]
os[1]["AttributionNote"] = "Complete case in the Strict Lamp Collection (五燈嚴統), Fayan Wenyi section: the compiler narrates Fayan Wenyi striking with a sitting cloth after recalling the monk who had failed to bow; Fayan does not utter 坐具."
os[1]["ActorAttribution"].update(
    ActorLabel="the compiler of Fayan Wenyi's record",
    GrammarEvidence="眼摵一坐具 is third-person narration of Fayan Wenyi's action, not wording spoken by Fayan or the monk.",
    ReviewedBy="Codex cohorts 1-3 v6 full-case hand read",
    ReviewedUtc=reviewed,
)

# The old KWIC fused Huineng's speech, Chen Yaxian's reply, and two narrated actions.
os[2].update(Kwic="遂謁里人陳亞仙曰：老僧欲乞檀那一坐具地。", FromLb="0450b04", ToLb="0450b05", MasterName="Huineng")
os[2].pop("ActorAttribution", None)
os[2]["ContextMasters"] = [{"MasterName": "Huineng", "Roles": ["utterer", "record-owner"]}]
os[2]["AttributionNote"] = "Complete land-expansion case in the Records of Pointing at the Moon (指月錄): Huineng directly asks the layman Chen Yaxian for one sitting-cloth's area of land; Chen's reply and the compiler's later cloth-spreading narration are separate turns."

os[3]["ContextMasters"] = [{"MasterName": "Baizhang Huaihai", "Roles": ["teacher", "record-owner"]}, {"MasterName": "Zhangjing Huaihui", "Roles": ["case-figure", "addressee"]}]
os[3]["AttributionNote"] = "Complete case in the Recorded Sayings of the Ancient Worthies (古尊宿語錄), Baizhang Huaihai section: the compiler narrates Baizhang instructing an unnamed monk to spread his sitting cloth when Zhangjing Huaihui takes the teaching seat; the instruction is reported rather than marked as a direct quotation."
os[3]["ActorAttribution"].update(
    ActorLabel="the compiler of Baizhang Huaihai's record",
    GrammarEvidence="師令僧去章敬處…便展開坐具 reports Baizhang's instruction in third-person narrative; the unnamed monk is the intended cloth-spreader and Zhangjing is the recipient.",
    ReviewedBy="Codex cohorts 1-3 v6 full-case hand read",
    ReviewedUtc=reviewed,
)

os[4]["ContextMasters"] = [{"MasterName": "Baizhang Huaihai", "Roles": ["teacher", "case-figure"]}, {"MasterName": "Zhangjing Huaihui", "Roles": ["respondent", "record-owner"]}]
os[4]["AttributionNote"] = "Complete case in the Five Lamps Meeting at the Source (五燈會元), Zhangjing Huaihui section: the compiler narrates the unnamed monk sent by Baizhang Huaihai spreading the sitting cloth before Zhangjing's teaching seat; neither master utters 坐具."
os[4]["ActorAttribution"].update(
    ActorLabel="the compiler of Zhangjing Huaihui's record",
    GrammarEvidence="百丈和尚令僧來候。師上堂次。展坐具 is third-person narration; the unnamed visiting monk performs the cloth action, and Zhangjing speaks only afterward.",
    ReviewedBy="Codex cohorts 1-3 v6 full-case hand read",
    ReviewedUtc=reviewed,
)

for i in (5, 6, 7):
    os[i]["ContextMasters"] = [{"MasterName": "Danxia Tianran", "Roles": ["person-described", "case-figure"]}, {"MasterName": "Nanyang Huizhong", "Roles": ["respondent", "record-owner"]}]
    os[i]["ActorAttribution"].update(
        ActorLabel="the compiler narrating the Danxia–Nanyang case",
        GrammarEvidence="丹霞來，纔展坐具 is third-person narration of Danxia Tianran beginning to spread the cloth; Nanyang Huizhong's 不用 response is a later turn.",
        ReviewedBy="Codex cohorts 1-3 v6 full-case hand read",
        ReviewedUtc=reviewed,
    )
os[5]["AttributionNote"] = "Complete Danxia–Nanyang case in the Linked Lamps Essential Record (聯燈會要): the compiler narrates Danxia Tianran beginning to spread his sitting cloth; Nanyang Huizhong then says, ‘No need.’"
os[6]["AttributionNote"] = "Complete Danxia–Nanyang case in the Essentials of the Chan Lineage (宗門統要正續集): the compiler narrates Danxia Tianran beginning to spread his sitting cloth; Nanyang Huizhong then says, ‘No need.’"
os[7]["AttributionNote"] = "Complete Danxia–Nanyang case in the Chan Grove of Ancestral Mirrors (宗鑑法林): the compiler narrates Danxia Tianran beginning to spread his sitting cloth; Nanyang Huizhong then says, ‘No need.’"

for s in d["Senses"]:
    s["SourceTexts"] = list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

p = BUILD / "fresh-build/entries/t_26ea593a58e2/entry.v2.json"
d = json.loads(p.read_text(encoding="utf-8"))
os = [o for s in d["Senses"] for o in s["Occurrences"]]
reviewed = "2026-07-15T23:35:00Z"

def narrated(i, label, evidence, contexts, note):
    o = os[i]
    o["MasterName"] = None
    o["ContextMasters"] = contexts
    o["ActorAttribution"] = {
        "Status": "narrated",
        "Kind": "compiler narrative",
        "ActorLabel": label,
        "ActorRole": "compiler",
        "RungsChecked": ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"],
        "GrammarEvidence": evidence,
        "ReviewedBy": "Codex cohorts 1-3 v6 full-case hand read",
        "ReviewedUtc": reviewed,
    }
    o["AttributionNote"] = note

narrated(0, "the compiler of Shakyamuni Buddha's case record", "世尊便下座 is third-person narrative after Manjusri's direct 白椎 declaration; Shakyamuni does not utter 下座.", [{"MasterName":"Shakyamuni Buddha","Roles":["person-described","case-figure"]},{"MasterName":"Manjusri","Roles":["utterer","case-figure"]}], "Complete case in the Five Lamps Meeting at the Source (五燈會元): the compiler narrates Shakyamuni Buddha descending after Manjusri gives the sounding-block declaration; neither man's direct speech contains 下座.")
narrated(1, "the compiler of Shakyamuni Buddha's case record", "世尊曰 marks only the preceding refusal; 便下座 is the compiler's following action clause.", [{"MasterName":"Shakyamuni Buddha","Roles":["person-described","case-figure"]}], "Complete case in the Five Lamps Meeting at the Source (五燈會元): after Shakyamuni Buddha directly refuses to preach to the two vehicles, the compiler narrates that he descends from the seat.")
narrated(2, "the compiler of Tianyi Yihuai's record", "良久曰 marks Tianyi's verse, 喝一喝 his shout, and 下座 the compiler's unquoted closing action.", [{"MasterName":"Tianyi Yihuai","Roles":["person-described","record-owner"]}], "Complete case in the Complete Lamp Collection (五燈全書), Tianyi Yihuai section: the compiler narrates Tianyi shouting and descending after his verse; 下座 is not part of the quoted verse.")
narrated(3, "the compiler of Feiyin Tongrong's record", "The quotation closes after 法王法如是; the following 下座 is narrative, while 上首 is the chief seat who gives the declaration.", [{"MasterName":"Feiyin Tongrong","Roles":["person-described","record-owner"]}], "Complete formal address in Feiyin Tongrong's Recorded Sayings (費隱禪師語錄): after the chief seat's closing declaration, the compiler narrates Feiyin descending from the teaching seat.")
narrated(4, "the compiler of Langting Jingting's record", "師云 introduces only 過去了也; 便下座 follows outside the quotation as narration.", [{"MasterName":"Langting Jingting","Roles":["person-described","record-owner"]}], "Complete one-line address in Langting Jingting's Recorded Sayings (雲溪俍亭挺禪師語錄): Langting says that the geese have passed, and the compiler then narrates his descent.")
narrated(5, "the compiler of the Linji–Mayu case", "麻谷拽師下座 is third-person narration: Mayu Baoche pulls Linji Yixuan from the seat; neither utters the headword.", [{"MasterName":"Mayu Baoche","Roles":["person-described","case-figure"]},{"MasterName":"Linji Yixuan","Roles":["case-figure","record-owner"]}], "Complete Linji–Mayu exchange in the Selected Records of the Five Houses (五家語錄): the compiler narrates Mayu Baoche pulling Linji Yixuan down and taking the seat.")
narrated(6, "the compiler of the Linji–Mayu case", "師亦拽麻谷下座 is third-person narration: 師 is Linji, who pulls Mayu down; neither utters the headword.", [{"MasterName":"Linji Yixuan","Roles":["person-described","record-owner"]},{"MasterName":"Mayu Baoche","Roles":["case-figure"]}], "Complete Linji–Mayu exchange in the Selected Records of the Five Houses (五家語錄): the compiler narrates Linji Yixuan pulling Mayu Baoche down and retaking the seat.")
narrated(7, "the compiler of the Linji–Mayu case", "師便下座 is third-person narration after Mayu leaves; 師 is Linji, who does not speak this clause.", [{"MasterName":"Linji Yixuan","Roles":["person-described","record-owner"]},{"MasterName":"Mayu Baoche","Roles":["case-figure"]}], "Complete Linji–Mayu exchange in the Selected Records of the Five Houses (五家語錄): after Mayu Baoche leaves, the compiler narrates Linji Yixuan descending.")
narrated(8, "the compiler quoting the Yaoshan case", "山陞座良久便下座 is case narration; Yaoshan's direct reply begins only later at 山云.", [{"MasterName":"Yaoshan Weiyan","Roles":["person-described","case-figure"]},{"MasterName":"Wansong Xingxiu","Roles":["commentator","record-owner"]}], "Complete raised case in Wansong Xingxiu's Book of Serenity (從容錄): the case narrator says Yaoshan Weiyan mounts the seat, remains silent, descends, and returns to his quarters; Yaoshan speaks only afterward.")
narrated(9, "the compiler quoting the Nanquan–Zhaozhou case", "師下座歸方丈 is third-person case narration after Zhaozhou's request; Nanquan supplies no direct verbal answer here.", [{"MasterName":"Nanquan Puyuan","Roles":["person-described","respondent"]},{"MasterName":"Zhaozhou Congshen","Roles":["questioner","case-figure"]}], "Complete Nanquan–Zhaozhou case in the Linked Pearls Collection of Verse Comments (禪宗頌古聯珠通集): the compiler narrates Nanquan Puyuan descending and returning to his quarters instead of verbally answering Zhaozhou Congshen.")
narrated(10, "the compiler of Feiyin Tongrong's record", "The direct question closes at 點卻不到; 卓拄杖便下座 is subsequent action narration.", [{"MasterName":"Feiyin Tongrong","Roles":["person-described","record-owner"]}], "Complete opening-the-furnace address in Feiyin Tongrong's Recorded Sayings (費隱禪師語錄): after Feiyin's shouted question, the compiler narrates him planting the staff and descending.")
narrated(11, "the compiler of Juelang Daosheng's record", "將下座 is third-person narration after Juelang's direct verse and before Huang Gong resumes questioning.", [{"MasterName":"Juelang Daosheng","Roles":["person-described","record-owner"]}], "Complete lay interview in Juelang Daosheng's Complete Record (天界覺浪盛禪師全錄): the compiler says Juelang was about to descend after his verse when Huang Gong asked another question.")

# The two genuinely spoken tokens remain assigned to their non-master interlocutors.
os[12]["ActorAttribution"].update(ReviewedBy="Codex cohorts 1-3 v6 full-case hand read", ReviewedUtc=reviewed, GrammarEvidence="士云 directly marks Huang Gong's request 請下座; Juelang Daosheng answers only in the following 師云 turn.")
os[12]["AttributionNote"] = "Complete lay interview in Juelang Daosheng's Complete Record (天界覺浪盛禪師全錄): Huang Gong directly tells Juelang, ‘Please descend from the seat’; Juelang's response is the next turn."
os[13]["ActorAttribution"].update(ReviewedBy="Codex cohorts 1-3 v6 full-case hand read", ReviewedUtc=reviewed, GrammarEvidence="公曰 directly marks Censor Wang's paired question 還是上座下座; the master answers only at 師曰.")
os[13]["AttributionNote"] = "Complete encounter in the Complete Lamp Collection (五燈全書): Censor Wang directly asks whether it is the upper or lower seat; the master answers that neither end adheres and the middle is not secure."

for s in d["Senses"]:
    s["SourceTexts"] = list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

p = BUILD / "fresh-build/entries/t_4deccd09a5d0/entry.v2.json"
d = json.loads(p.read_text(encoding="utf-8"))
os = [o for s in d["Senses"] for o in s["Occurrences"]]
reviewed = "2026-07-16T00:05:00Z"

for i, name, note in [
    (0, "Nanyuan Huiyong", "Complete exchange in the Complete Lamp Collection (五燈全書), Nanyuan Huiyong section: Nanyuan lifts his staff and directly says, ‘Acceptance of the unborn under the staff; at the encounter do not yield to the teacher.’"),
    (1, "Tianning Qi", "Complete chain of comments in the Chan Grove of Ancestral Mirrors (宗鑑法林): Tianning Qi directly introduces and quotes the staff formula while commenting on Mahakasyapa's proposed expulsion of Manjusri."),
    (2, "Nanyuan Huiyong", "Complete exchange in the Recorded Sayings of the Ancient Worthies (古尊宿語錄), Nanyuan Huiyong section: Nanyuan lifts his staff and directly gives the formula in answer to Fengxue Yanzhao's question about the local staff."),
]:
    os[i]["MasterName"] = name
    os[i].pop("ActorAttribution", None)
    os[i]["ContextMasters"] = [{"MasterName": name, "Roles": ["utterer"]}]
    os[i]["AttributionNote"] = note

os[2].update(Kwic="師拈拄杖云棒下無生忍臨機不見師", FromLb="0663c16", ToLb="0663c17")

os[3].update(Kwic="行咨問棒下無生忍行咨知恩報恩", FromLb="0506a16", ToLb="0506a17", MasterName=None)
os[3]["ActorAttribution"] = {
    "Status": "reviewed-nonmaster", "Kind": "named monastic questioner", "ActorLabel": "the monk Xingzi (行咨)", "ActorRole": "questioner",
    "RungsChecked": ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"],
    "GrammarEvidence": "行咨問 explicitly names Xingzi as the monk who begins the headword-bearing question; Yulin Tongxiu answers only at the following 師便打 action.",
    "ReviewedBy": "Codex cohorts 1-3 v6 full-case hand read", "ReviewedUtc": reviewed,
}
os[3]["ContextMasters"] = [{"MasterName":"Yulin Tongxiu","Roles":["respondent","record-owner"]}]
os[3]["AttributionNote"] = "Complete twelfth-month address in Yulin Tongxiu's Recorded Sayings (普濟玉琳國師語錄): the named monk Xingzi (行咨) begins his question with the staff formula; Yulin responds by striking him."

os[4].update(Kwic="頌曰。色自色𠔃聲自聲新鶯啼處柳𤇆輕門門有路通京國三島斜橫海月明聲出虛色生無聲前色後轉塗糊間不容髮安可名模堂堂圓應沒錙銖巧張爐鞴費分踈爭如棒下無生忍聞見馨香滿道途", FromLb="0792a16", ToLb="0792a21", MasterName=None)
os[4]["ActorAttribution"] = {
    "Status": "reviewed-unnamed", "Kind": "unattributed capping verse", "ActorLabel": "the unattributed verse voice", "ActorRole": "verse-author",
    "RungsChecked": ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"],
    "GrammarEvidence": "頌曰 introduces a verse appended to the Baishui Benren case, but none of the six source rungs supplies a personal author for this verse.",
    "ReviewedBy": "Codex cohorts 1-3 v6 full-case hand read", "ReviewedUtc": reviewed,
}
os[4]["ContextMasters"] = []
os[4]["AttributionNote"] = "Complete Baishui Benren case and appended verse in the Linked Pearls Collection of Verse Comments (禪宗頌古聯珠通集): an unattributed verse voice compares elaborate verbal work with ‘acceptance of the unborn under the staff’; the source does not name the verse author."

os[5].update(Kwic="進云：「恁麼則棒下無生忍，臨機不見師。」", FromLb="0318b15", ToLb="0318b16", MasterName=None)
os[5]["ActorAttribution"] = {
    "Status": "reviewed-unnamed", "Kind": "monastic questioner", "ActorLabel": "the unnamed monk questioning Guxue Zhe", "ActorRole": "questioner",
    "RungsChecked": ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"],
    "GrammarEvidence": "進云 continues the unnamed monk's marked turn; Guxue Zhe answers only at 師云 after the quoted formula.",
    "ReviewedBy": "Codex cohorts 1-3 v6 full-case hand read", "ReviewedUtc": reviewed,
}
os[5]["ContextMasters"] = [{"MasterName":"Guxue Zhe","Roles":["respondent","record-owner"]}]
os[5]["AttributionNote"] = "Complete birthday address in Guxue Zhe's Recorded Sayings (古雪哲禪師語錄): an unnamed monk offers the staff formula as an inference; Guxue answers that the shout is present but the phrase of the unborn is not yet there."

for s in d["Senses"]:
    s["SourceTexts"] = list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

p = BUILD / "fresh-build/entries/t_76466e7feed6/entry.v2.json"
d = json.loads(p.read_text(encoding="utf-8"))
os = [o for s in d["Senses"] for o in s["Occurrences"]]
reviewed = "2026-07-16T00:30:00Z"
owners = [
    ("Muchen Daomin", "the compiler excerpting Muchen Daomin's imperial-hall address", "弘覺忞禪師 and the immediately following imperial appointment make 師 Muchen Daomin; 師拈香祝聖 is third-person action narration before his direct address.", "Complete imperial-hall address in the Record of Ancestral Teaching Outlines (列祖提綱錄): the compiler narrates Muchen Daomin offering incense for the ruler before his questions and formal address."),
    ("Huaihai Yuanzhao", "the compiler of Huaihai Yuanzhao's record", "The Shuangta section of Huaihai Yuanzhao's own record narrates 次升座，拈香祝聖畢，就座 before marking his address at 乃云.", "Complete installation address in Huaihai Yuanzhao's Recorded Sayings (淮海原肇禪師語錄): the compiler narrates Huaihai ascending, offering incense for the ruler, and taking the seat before his direct address."),
    ("Shiyu Mingfang", "the compiler of Shiyu Mingfang's record", "遂陞座，拈香祝聖竟 is narration; Shiyu Mingfang's direct speech begins only at 師曰.", "Complete installation address in Shiyu Mingfang's Dharma Altar (石雨禪師法檀): the compiler narrates Shiyu ascending and offering incense for the ruler before Shiyu begins speaking."),
    ("Dabo Qian", "the compiler of Dabo Qian's record", "The 如來菴 installation section narrates 拈香祝聖罷; Dabo Qian's quoted lineage-incense words begin only after 次拈香云.", "Complete installation address in Dabo Qian's Recorded Sayings (大博乾禪師語錄): the compiler narrates Dabo's incense for the ruler, then separately quotes his lineage incense for Wanru Tongwei."),
    ("Feiyin Tongrong", "the compiler of Feiyin Tongrong's record", "元旦上堂，拈香祝聖畢 is narrative stage direction; Feiyin's direct speech begins at 師云.", "Complete New Year's address in Feiyin Tongrong's Recorded Sayings (費隱禪師語錄): the compiler narrates Feiyin offering incense for the ruler before the unrecorded questions and his address."),
    ("Chaozong Tongren", "the compiler of Chaozong Tongren's record", "師拈香祝聖罷，就座 narrates Chaozong Tongren's actions; his direct speech begins after the chief seat's declaration at 師云.", "Complete address in Chaozong Tongren's Recorded Sayings (朝宗禪師語錄): the compiler narrates Chaozong offering incense for the ruler and taking the seat before the chief seat's declaration and Chaozong's speech."),
    ("Yuanjie Ying", "the compiler of Yuanjie Ying's record", "The first address in Yuanjie Ying's own record says 上堂。拈香祝聖畢，乃曰; the ritual clause is narration and his direct address begins at 乃曰.", "Complete opening address in Yuanjie Ying's Recorded Sayings (元潔瑩禪師語錄): the compiler narrates Yuanjie offering incense for the ruler; Yuanjie's direct speech starts only afterward."),
]
for o, (name, label, evidence, note) in zip(os, owners):
    o["MasterName"] = None
    o["ContextMasters"] = [{"MasterName":name,"Roles":["person-described","record-owner"]}]
    o["ActorAttribution"] = {
        "Status":"narrated", "Kind":"compiler narrative", "ActorLabel":label, "ActorRole":"compiler",
        "RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],
        "GrammarEvidence":evidence, "ReviewedBy":"Codex cohorts 1-3 v6 full-case hand read", "ReviewedUtc":reviewed,
    }
    o["AttributionNote"] = note
for s in d["Senses"]:
    s["SourceTexts"] = list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

p = BUILD / "fresh-build/entries/t_7b7ca6f375b5/entry.v2.json"
d = json.loads(p.read_text(encoding="utf-8"))
os = [o for s in d["Senses"] for o in s["Occurrences"]]
reviewed = "2026-07-16T01:00:00Z"

def narrated(i, label, evidence, contexts, note):
    o=os[i];o["MasterName"]=None;o["ContextMasters"]=contexts
    o["ActorAttribution"]={"Status":"narrated","Kind":"compiler narrative","ActorLabel":label,"ActorRole":"compiler","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":evidence,"ReviewedBy":"Codex cohorts 1-3 v6 full-case hand read","ReviewedUtc":reviewed}
    o["AttributionNote"]=note

# Regenerated after the no-heading fallback fix: 177-character paragraph-span, fully read.
os[0].update(Kwic="師默然僧罔措再問",FromLb="0226b05",ToLb="0226b05",MasterName=None)
os[0]["AttributionNote"] = "Complete Wenxi exchange in the surviving Transmission Lamp Jade Flowers Collection (傳燈玉英集): the compiler says an unnamed monk was at a loss after Hangzhou Wenxi remained silent; the monk asks again and Wenxi answers."
os[0]["ActorAttribution"] = {"Status":"narrated","Kind":"compiler narrative","ActorLabel":"the compiler of Hangzhou Wenxi's record","ActorRole":"compiler","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"師默然，僧罔措，再問 is third-person narration of the unnamed monk's reaction between Wenxi's silence and the monk's renewed question.","ReviewedBy":"Codex cohorts 1-3 v6 full-case hand read","ReviewedUtc":"2026-07-16T01:45:00Z"}
os[0]["ContextMasters"] = []
narrated(1,"the compiler of the Daizong–Nanyang case","帝罔措 narrates Emperor Daizong's response to Nanyang Huizhong's silence; neither man utters the headword.",[{"MasterName":"Nanyang Huizhong","Roles":["respondent","case-figure"]}],"Complete seamless-stupa case in the Collection from the Patriarchs' Hall (祖堂集): the compiler says Emperor Daizong was at a loss after Nanyang Huizhong remained silent; Huizhong then directs him to Danyuan Yingzhen.")
narrated(2,"the compiler narrating Li Ao's interview","侍郎罔措 and the later self-report 弟子罔措 both describe Li Ao's state after the master's silence; Li Ao does not utter the stored token.",[],"Complete Li Ao interview in the Collection from the Patriarchs' Hall (祖堂集): the compiler says Li Ao was at a loss after the master's silence; Li later uses the same words in explaining that the attendant's intervention gave him an entry.")
narrated(3,"the compiler of Muzhou Daozong's record","僧便喝…師拍手大笑…僧罔措 narrates the unnamed monk's state between Muzhou's laughter and blow.",[{"MasterName":"Muzhou Daozong","Roles":["respondent","record-owner"]}],"Complete short examination in the Recorded Sayings of the Ancient Worthies (古尊宿語錄), Muzhou Daozong section: the compiler says the unnamed monk was at a loss after Muzhou laughed, whereupon Muzhou struck him.")
os[4].update(Kwic="主罔措，師展手，云：「元來學弄虛的。」",FromLb="0029b02",ToLb="0029b03")
narrated(4,"the compiler of Miyun Yuanwu's record","主罔措 narrates the hall officer's state after Miyun asks what follows three or four shouts; Miyun speaks again only at 師展手云.",[{"MasterName":"Miyun Yuanwu","Roles":["questioner","record-owner"]}],"Complete examination in Miyun Yuanwu's Recorded Sayings (密雲禪師語錄): the compiler says the hall officer was at a loss after Miyun asked what follows repeated shouts; the KWIC is recut away from a second monk's later 罔措.")
narrated(5,"Tianyin Yuanxiu's raised-case narrator","後寶壽罔措 is narration inside the case raised by Tianyin Yuanxiu; Later Baoshou is the person described, not the utterer.",[{"MasterName":"Tianyin Yuanxiu","Roles":["later-raiser","record-owner"]},{"MasterName":"Later Baoshou","Roles":["person-described","case-figure"]}],"Complete raised case in Tianyin Yuanxiu's Recorded Sayings (天隱和尚語錄): Tianyin quotes the compiler's statement that Later Baoshou was at a loss when Earlier Baoshou asked for his original face.")
narrated(6,"the compiler of Linye Tongqi's record","公罔措 narrates Censor Wang's state after Linye Tongqi says he cannot distinguish the northern and southern bow.",[{"MasterName":"Linye Tongqi","Roles":["respondent","record-owner"]}],"Complete interview in the Complete Lamp Collection (五燈全書), Linye Tongqi section: the compiler says Censor Wang was at a loss after Linye's answer; Linye then asks him to bow.")
narrated(7,"the compiler of the Yaoshan–Shitou case","師罔措 describes Yaoshan Weiyan before awakening, after Shitou Xiqian asks what he can say when both ‘thus’ and ‘not thus’ fail.",[{"MasterName":"Yaoshan Weiyan","Roles":["person-described","student"]},{"MasterName":"Shitou Xiqian","Roles":["questioner","teacher"]}],"Complete early Yaoshan case in the Five Lamps Meeting at the Source (五燈會元): the compiler says Yaoshan Weiyan was at a loss before Shitou Xiqian and was sent on to Mazu; Shitou does not utter the headword.")
d["Senses"][0]["Explanation"] = "To be at a loss: unable to answer or know what move to make in an encounter. The records use the term for an unnamed monk after Hangzhou Wenxi remains silent, Emperor Daizong after Nanyang Huizhong remains silent, Li Ao after a master's silence, and another unnamed monk after Muzhou Daozong laughs. Miyun Yuanwu's hall officer, Later Baoshou, Censor Wang, and the still-studying Yaoshan Weiyan are likewise described as at a loss when a question or response leaves them with no answer. These cases ground the plain meaning in observable interview turns; they do not turn the word into an imported diagnosis of an unseen inner condition."
for s in d["Senses"]: s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")

p = BUILD / "fresh-build/entries/t_8bd6933e6de3/entry.v2.json"
d = json.loads(p.read_text(encoding="utf-8"))
os = [o for s in d["Senses"] for o in s["Occurrences"]]
reviewed = "2026-07-16T01:30:00Z"

for i,name,note in [
 (0,"Linji Yixuan","Complete examination in Linji Yixuan's Recorded Sayings (鎮州臨濟慧照禪師語錄): Linji directly asks the monk how he understands a single shout compared with sword, lion, sounding-pole grass, and a shout not functioning as a shout."),
 (1,"Linji Yixuan","Complete parallel examination in the Recorded Sayings of the Ancient Worthies (古尊宿語錄): Linji Yixuan directly gives the four comparisons and asks the monk how he understands them."),
 (2,"Zhongfeng Mingben","Complete instruction to the Japanese practitioner Kong in Zhongfeng Mingben's Extended Record (天目中峰廣錄): Zhongfeng directly says the boundless sea of lands and teachings is wholly gathered into this one staff and one shout."),
 (3,"Guyin Zhiyan","Complete public address in Tianyin Yuanxiu's Recorded Sayings (天隱和尚語錄): Tianyin explicitly quotes Guyin Zhiyan's verse, ‘One shout distinguishes guest and host’; the stored headword belongs to Guyin's quoted words."),
 (5,"Xinghua Cunjiang","Complete public examination in the Recorded Sayings of the Ancient Worthies (古尊宿語錄), Xinghua Cunjiang record: Xinghua directly says that Elder Minde understands how one shout does not function as a shout."),
 (6,"Baizhang Huaihai","Complete Baizhang–Huangbo exchange in the Recorded Sayings of the Ancient Worthies (古尊宿語錄): Baizhang Huaihai directly recounts that Mazu's single shout left him deaf for three days."),
 (7,"Tian'an Sheng","Complete interview in Tian'an Sheng's Recorded Sayings (天岸昇禪師語錄): after the monk shouts, Tian'an directly says, ‘A fine single shout,’ then praises the monk's two further shouts."),
]:
    o=os[i];o["MasterName"]=name;o.pop("ActorAttribution",None);o["ContextMasters"]=[{"MasterName":name,"Roles":["utterer"]}];o["AttributionNote"]=note
os[0].update(Kwic="師問僧：「有時一喝如金剛王寶劍",FromLb="0504a26",ToLb="0504a26")
os[1].update(Kwic="師問僧有時一喝如金剛王寶劍",FromLb="0650a16",ToLb="0650a16")
os[3]["ContextMasters"]=[{"MasterName":"Guyin Zhiyan","Roles":["utterer"]},{"MasterName":"Tianyin Yuanxiu","Roles":["later-quoter","record-owner"]}]
os[5].update(Kwic="為他旻德長老會一喝不作",FromLb="0653a21",ToLb="0653a22")
os[6]["ContextMasters"]=[{"MasterName":"Baizhang Huaihai","Roles":["utterer"]},{"MasterName":"Mazu Daoyi","Roles":["case-figure"]}]

o=os[4];o["MasterName"]=None;o["ContextMasters"]=[{"MasterName":"Yulin Tongxiu","Roles":["person-described","record-owner"]}];o["ActorAttribution"]={"Status":"narrated","Kind":"compiler narrative","ActorLabel":"the compiler of Yulin Tongxiu's record","ActorRole":"compiler","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"師一夕至東堂…震威一喝而出 narrates Yulin's action; his later direct discussion of the shout begins at 師云.","ReviewedBy":"Codex cohorts 1-3 v6 full-case hand read","ReviewedUtc":reviewed};o["AttributionNote"]="Complete evening interview in Yulin Tongxiu's Recorded Sayings (普濟玉琳國師語錄): the compiler narrates Yulin entering the east hall, looking around, giving one formidable shout, and leaving; Yulin later questions the assembly about that action."

o=os[8];o["MasterName"]=None;o["ContextMasters"]=[];o["ActorAttribution"]={"Status":"reviewed-unnamed","Kind":"monastic questioner","ActorLabel":"the unnamed monk questioning the Cuiyan master","ActorRole":"questioner","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"僧問…學人未遇大機請師一喝 directly assigns the only 一喝 token to the unnamed monk; the master answers at 師云.","ReviewedBy":"Codex cohorts 1-3 v6 full-case hand read","ReviewedUtc":reviewed};o["AttributionNote"]="Complete exchange in the Recorded Sayings of the Ancient Worthies (古尊宿語錄), Cuiyan record: an unnamed monk directly asks the master for a single shout; the master questions what capacity he means, and the monk himself then shouts."

o=os[9];o["MasterName"]=None;o["ContextMasters"]=[{"MasterName":"Shimen Yuncong","Roles":["person-described","record-owner"]}];o["ActorAttribution"]={"Status":"narrated","Kind":"compiler narrative","ActorLabel":"the compiler of Shimen Yuncong's record","ActorRole":"compiler","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"喝一喝下座 is unquoted action narration after Shimen's direct instruction about the long bench and meals.","ReviewedBy":"Codex cohorts 1-3 v6 full-case hand read","ReviewedUtc":reviewed};o["AttributionNote"]="Complete address in Shimen Yuncong's Recorded Sayings within the Recorded Sayings of the Ancient Worthies (古尊宿語錄): after speaking of the long bench, gruel, and rice, the compiler narrates Shimen giving one shout and descending."
for s in d["Senses"]: s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("repaired hand-read entries 6 and 8-15 (罔措 O1 deferred)")
