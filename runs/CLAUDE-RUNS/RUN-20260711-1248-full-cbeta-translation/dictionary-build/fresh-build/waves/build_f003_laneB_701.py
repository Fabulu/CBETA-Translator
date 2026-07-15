import datetime
import hashlib
import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT))
import zc

BASE = "42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a"
NOW = datetime.datetime.now(datetime.timezone.utc).isoformat()
OUT = ROOT / "fresh-build/entries/t_8fab213d600f"
OUT.mkdir(parents=True, exist_ok=True)


def occ(rel, kwic, name, note, roles=None):
    proof = zc.verify(rel, kwic)
    assert proof["ok"], (rel, kwic, proof)
    roles = roles or ["utterer", "record-owner"]
    return {
        "RelPath": rel,
        "FromLb": proof["fromLb"],
        "ToLb": proof["toLb"],
        "Kwic": kwic,
        "MasterName": name,
        "Curated": True,
        "AttributionNote": note,
        "ContextMasters": [{"MasterName": name, "Roles": roles}],
        "DraftActorProof": {
            "ExactHeadwordClause": kwic,
            "SpeechFrame": note,
            "FullCaseDecision": f"{name} owns the exact headword-bearing wording in this complete case.",
        },
    }


person = [
    occ(
        "T/T48/T48n2016.xml",
        "周易為真玄。老子為虛玄。莊子為談玄",
        "Yongming Yanshou",
        "In the Record of the Source-Mirror (宗鏡錄), Yongming Yanshou distinguishes the three Chinese ‘mysteries’ and names Laozi as the representative of the empty mystery.",
        ["utterer", "commentator", "record-owner"],
    ),
    occ(
        "J/J26/J26nB185.xml",
        "老子曰：『聖人抱一為天下式。』",
        "Fushi Tongxian",
        "In the Recorded Sayings of Fushi (浮石禪師語錄), Fushi Tongxian juxtaposes attributed sayings of Confucius, Laozi, and Gautama; Fushi owns the comparative framing and quotes Laozi by name.",
        ["utterer", "later-quoter", "record-owner"],
    ),
    occ(
        "J/J28/J28nB208.xml",
        "老子道：『抱一為天下式。』",
        "Guxue Zhe",
        "In the Recorded Sayings of Guxue Zhe (古雪哲禪師語錄), Guxue Zhe places a saying attributed to Laozi beside Confucius’s formulation while addressing the assembly.",
        ["utterer", "later-quoter", "record-owner"],
    ),
    occ(
        "X/X69/X69n1368.xml",
        "老子化胡成佛",
        "Yanxi Guangwen",
        "In the Recorded Sayings of Yanxi Guangwen (偃溪廣聞禪師語錄), Yanxi Guangwen asks why the story says Laozi transformed the foreigners into buddhas rather than Daoists.",
        ["utterer", "record-owner"],
    ),
]

epithet = [
    occ(
        "J/J34/J34nB301.xml",
        "三千大千世界被這老子一時搖動",
        "Nanyue Jiqi Hongchu",
        "In the Recorded Sayings of Nanyue Jiqi (南嶽繼起和尚語錄), Nanyue Jiqi Hongchu comments after quoting Xuefeng and calls him ‘this old fellow.’",
        ["utterer", "commentator", "record-owner"],
    ),
    occ(
        "M/M59/M59n1540.xml",
        "這老子果然出三門下坐",
        "Dahui Zonggao",
        "In Dahui Pujue’s General Addresses (大慧普覺禪師普說), Dahui Zonggao narrates confronting his teacher and familiarly calls him ‘this old fellow.’",
        ["utterer", "record-owner"],
    ),
    occ(
        "J/J27/J27nB193.xml",
        "請出為者老子雪屈",
        "Yinyuan Longqi",
        "In the Recorded Sayings of Yinyuan (隱元禪師語錄), Yinyuan Longqi invites anyone who objects to come vindicate ‘this old fellow,’ referring to Shakyamuni in the address.",
        ["utterer", "record-owner"],
    ),
    occ(
        "J/J36/J36nB369.xml",
        "三箇老子尋常氣陵今古",
        "Zhean Jingfan",
        "In the Recorded Sayings of Zhean Jingfan (蔗菴範禪師語錄), Zhean Jingfan calls Dongshan, Shishuang, and Ming’an ‘three old fellows’ while comparing their comments.",
        ["utterer", "commentator", "record-owner"],
    ),
]


def draft(sense, opening, body, bend, limit, different, aliases, works):
    sense["ExplanationParts"] = {"CorpusEarnedOpening": opening, "EvidenceBody": [body]}
    sense["DraftEvidence"] = {
        "OpeningClaimEvidenceKeys": [f"o{i}" for i in range(1, len(sense["Occurrences"]) + 1)],
        "ZenBend": bend,
        "CounterexampleOrLimit": limit,
        "DifferentThingTest": different,
        "AliasRationale": aliases,
        "ModifierControls": [{"finding": "not-applicable", "reason": "No material or color modifier controls the headword."}],
        "FamilyControls": [{"finding": "checked", "reason": "Longer forms such as 釋迦老子, 黃面老子, 達磨老子, and 閻老子 were inventoried and were not allowed to pad standalone occurrence depth."}],
        "IndependentWorkIds": works,
    }
    return sense


s1 = draft({
    "SenseKey": "laozi-person",
    "MasterName": None,
    "PreferredTarget": "Laozi",
    "AlternateTargets": ["Lao Dan", "the Old Master"],
    "SearchAliases": ["Lao Tzu", "Lao-tzu", "author of the Daodejing", "Old Master Lao"],
    "Status": "preferred",
    "Validation": "multi-source",
    "Note": "This is the invoked pre-Chan figure, not the productive familiar epithet and not a claim that every longer compound containing 老子 names Laozi.",
    "Occurrences": person,
    "ClaimAnchors": [],
    "SourceTexts": [x["RelPath"] for x in person],
    "RelatedMasters": ["Yongming Yanshou", "Fushi Tongxian", "Guxue Zhe", "Yanxi Guangwen"],
    "RelatedTerms": ["老聃", "李耳", "道德經", "孔子", "莊子", "老子化胡"],
},
    "Laozi is the pre-Chan Chinese figure whom Chan records quote and place beside Confucius, Zhuangzi, and Gautama.",
    "Yongming Yanshou assigns Laozi to the ‘empty mystery’ in a threefold comparison; Fushi Tongxian and Guxue Zhe juxtapose his ‘holding to the one’ with sayings of Confucius and Gautama. Yanxi Guangwen turns the story that Laozi transformed the foreigners into buddhas into a public question: why did he not transform them into Daoists? The Chan bend is not a new biography of Laozi but his deployment as a named voice and case figure within comparative instruction and teaching-seat questioning.",
    "The records quote, compare, and question Laozi rather than merely listing him as outside background.",
    "Nested compounds such as 釋迦老子 name other people and therefore cannot evidence this person sense.",
    {"Decision": "different-thing", "ComparedThings": ["Laozi, the named pre-Chan figure", "old fellow, a productive familiar epithet"], "Reason": "A historical person and a noun or epithet applied to many different men are different lexical referents."},
    "Romanization, spacing, and the conventional English title are controlled retrieval forms for the same named person.",
    ["work:T48n2016", "work:J26nB185", "work:J28nB208", "work:X69n1368"],
)

s2 = draft({
    "SenseKey": "old-fellow-epithet",
    "MasterName": None,
    "PreferredTarget": "old fellow",
    "AlternateTargets": ["old man", "old master"],
    "SearchAliases": ["this old fellow", "these old fellows", "familiar master epithet", "irreverent old man"],
    "Status": "preferred",
    "Validation": "multi-source",
    "Note": "The appraisal ranges from familiar and vigorous to openly critical; ‘irreverent’ is therefore a lookup aid, not a fixed tone imposed on every occurrence.",
    "Occurrences": epithet,
    "ClaimAnchors": [],
    "SourceTexts": [x["RelPath"] for x in epithet],
    "RelatedMasters": ["Nanyue Jiqi Hongchu", "Dahui Zonggao", "Yinyuan Longqi", "Zhean Jingfan"],
    "RelatedTerms": ["釋迦老子", "黃面老子", "達磨老子", "閻老子", "老漢"],
},
    "As a productive noun or epithet, 老子 means an ‘old fellow’ and lets Chan speakers refer to teachers and case figures with striking familiarity.",
    "Nanyue Jiqi calls Xuefeng ‘this old fellow’ after raising his words; Dahui uses the same form for a teacher in a self-narrated confrontation. Yinyuan asks who will vindicate ‘this old fellow,’ meaning Shakyamuni, while Zhean groups Dongshan, Shishuang, and Ming’an as ‘three old fellows.’ The Chan bend lies in this flexible familiarity: revered lineage and pre-lineage figures can be praised, tested, blamed, or handled as participants in a present public exchange rather than kept at ceremonial distance.",
    "The epithet makes named authorities available for present comparison and criticism without fixing one appraisal.",
    "The corpus also forms many longer compounds, but this sense is anchored here by demonstrative and counted standalone noun phrases rather than substring counts alone.",
    {"Decision": "different-thing", "ComparedThings": ["old fellow, a productive epithet", "Laozi, the named person"], "Reason": "The epithet takes demonstratives and plural counting and applies to many figures; those grammatical facts distinguish it from the proper name."},
    "The aliases expose the noun’s ordinary and Chan-facing lookup forms while preserving variable tone.",
    ["work:J34nB301", "chan:dahui-pushuo", "work:J27nB193", "work:J36nB369"],
)

entry = {
    "Id": "t_8fab213d600f",
    "SourceTerm": "老子",
    "CorpusBaselineSha256": BASE,
    "CreatedBy": "Codex f003 Lane B evidence-first",
    "WrittenUtc": NOW,
    "Senses": [s1, s2],
}

worksheet = OUT / "evidence.draft.json"
worksheet.write_text(json.dumps({"SchemaVersion": 1, "Entry": entry}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
(OUT / "STATUS").write_text("researching\n", encoding="utf-8")
(OUT / "WORK.md").write_text((OUT / "WORK.md").read_text(encoding="utf-8") + "\n## Draft decision\n\nTwo senses retained after exact grammatical controls: named person versus productive epithet. Eight exact standalone headword witnesses across eight works are stored; longer compounds remain family evidence only.\n", encoding="utf-8")
subprocess.run([sys.executable, str(ROOT / "compile_evidence_draft.py"), str(worksheet), "--output", str(OUT / "entry.v2.json"), "--report", str(OUT / "compile-report.json")], check=True)
(OUT / "STATUS").write_text("drafted\n", encoding="utf-8")
print(json.dumps({"entry": str(OUT / "entry.v2.json"), "worksheetSha256": hashlib.sha256(worksheet.read_bytes()).hexdigest(), "entrySha256": hashlib.sha256((OUT / "entry.v2.json").read_bytes()).hexdigest()}))
