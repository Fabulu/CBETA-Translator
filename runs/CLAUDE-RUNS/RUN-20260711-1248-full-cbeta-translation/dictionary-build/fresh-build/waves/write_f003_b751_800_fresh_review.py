#!/usr/bin/env python3
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path

HERE = Path(__file__).resolve().parent
BUILD = HERE.parent
LEDGER = HERE / "f003-laneB-751-800-author-repair-round2-ledger.json"
FORMAL = HERE / "f003-laneB-751-800-formal-gate-author-repair-round2.json"
OUT = HERE / "f003-laneB-751-800-fresh-independent-exact-review.json"

findings = {
751: ("REVISE", "O4 is narrator-owned: 文遠侍者在佛殿禮拜次 reports Wenyuan in the hall; Nanquan speaks only after 師見. The building prose is useful, but the exact actor remains wrong."),
752: ("REVISE", "O3 and O5 still use generic compilation/preface labels where the section or preface owner must be recovered. A hard mechanical pass does not establish those exact documentary owners."),
753: ("KEEP", "The reusable demand formula is distinguished from its varying answers; all six selected clauses sit inside the attributed masters' own addresses or commentary turns, with no different referent found."),
754: ("REVISE", "O2 explicitly introduces 浪山嶼云 and O7 寶掌白云, but both rows are assigned to a generic compiler. Those named quoted speakers must own their exact turns."),
755: ("KEEP", "The verb 'found/open a monastery' is correctly separated from the institutional title 'founding abbot'. Biography narration is no longer assigned to its subjects, while the two memorial incense turns remain master speech."),
756: ("REVISE", "O4 is a duplicate of the named 南明 sermon preserved in a compilation, not anonymous compiler wording. The named sermon owner must be recovered before KEEP."),
757: ("REVISE", "O6 says 師問百丈從上宗乘如何指示於人: the section master asks Baizhang, so Baizhang is respondent, not the headword utterer."),
758: ("KEEP", "All three witnesses use Medicine King as the invoked figure in named master speech. O2 correctly belongs to Miyun Yuanwu, and the explanation remains limited to the Chan deployment of the burning-body episode."),
759: ("REVISE", "O3 is in T48n2001, Hongzhi Zhengjue's Extensive Record, yet is assigned to Dahui Zonggao. The stored title-owner attribution is factually wrong."),
760: ("REVISE", "O4 故禪者，誠去執之虛名 uses 禪者 as 'as for Chan/Chan', not a person who practices Chan. This is a different lexical referent, not evidence for the sole practitioner sense."),
761: ("REVISE", "O9 is the unnamed monk's question 維摩丈室…和尚丈室以何為明, not documentary narration. The respondent belongs only in ContextMasters."),
762: ("REVISE", "The sole physical-gate sense merges different things: 洞山門下 is Dongshan's lineage, 山門老宿 and 山門功德 use the monastery/community institution, while 山門之右 is the physical gate."),
763: ("KEEP", "The inherited company and its Chan uses in sermons, ritual assemblies, and the Guang'e case remain one referent; the selected turns and narration boundaries are coherent."),
764: ("REVISE", "O3 explicitly has 正覺云 before the headword and O5 places it in a named current sermon; both are mislabeled as compiler narration."),
765: ("REVISE", "O2 時翠巖真為首座，藏主問真 is narrator-owned office identification, while O3/O4/O8/O9 include invitation or thanks occasion labels. These are not utterances by the nearby presiding masters."),
766: ("REVISE", "O7 occurs inside the master's 乃云 speech as 臘八午夜…, not in an editorial 臘八，上堂 label. The blanket impersonal classification is false for that row."),
767: ("REVISE", "O2/O4/O7/O8 place 如何是第一句 in a monk's question but assign the respondent. O3 contains both Linji's statement and a monk's question in one KWIC, violating the one-actor-per-occurrence rule."),
768: ("REVISE", "O4 is a preface author's praise of Jifei, not Jifei's speech. O5 contains named Baiyun Duan and Huanglong Xin comments and cannot be left as a generic case narrator."),
769: ("REVISE", "O2 explicitly names Dawei Tai's rain sermon; O6 is Pindola's speech, O7 is the master's question, O8 is Chushi Fanqi's hall speech, and O9 contains quoted Pindola/Dawei turns. Their current owners are wrong."),
770: ("KEEP", "The final-move gloss is narrow, concrete, and survives the negative witness where a purported 'final move' is rejected. The six current turns have coherent owners."),
771: ("KEEP", "The building referent is consistently the public teaching hall; construction, entry, seating, and rule narration are correctly documentary rather than falsely assigned master speech."),
772: ("REVISE", "O4 is not Zilu: 沒髭鬚底胡子，路見不平 crosses the punctuation boundary between 胡子 and 路. It is an accidental string match and cannot evidence the named disciple."),
773: ("REVISE", "O3/O4/O7 mark Huineng with 祖曰/祖云 as the headword utterer, not Jiangxi Zhiche, Heze Shenhui, or Fada. O8 is a preface voice, not Yuanwu's own turn."),
774: ("REVISE", "O1 is the book-title/TOC string 列祖提綱錄, O2 is a questioner's turn, and O7 embeds 大溈喆云. Title, questioner, and quoted speaker remain flattened."),
775: ("REVISE", "O2-O7 use 浴佛，上堂 as an editorial occasion label rather than speech by the named sermon owner. O1 uniquely also contains spoken 天下精藍皆悉浴佛 and correctly belongs to Huanglong Huinan."),
776: ("REVISE", "O5 is the monk's question 三世諸佛出身處, not the respondent's turn. O7/O8 explicitly quote Yunfeng Wenyue's 出身之路 wording but are left as generic narration."),
777: ("REVISE", "O2 is a table-of-contents occurrence inside the longer office 聖僧侍者. It contradicts the worksheet's stated TOC/nested-compound exclusion and cannot buy depth for the sacred-monk image."),
778: ("REVISE", "The KWICs include both the original unnamed monk's question and later masters such as Langya Huijue repeating the identical full headword. Each row therefore contains multiple headword turns with different actors and must be recut."),
779: ("REVISE", "O4 is again T48n2001, Hongzhi Zhengjue's record, not Dahui. O2's verse owner and O7's named rain-sermon owner are also left generic despite recoverable contexts."),
780: ("REVISE", "O3 is continuous named-master sermon speech in Yinyuan Longqi's lineage record, not an anonymous 'assembly speaker'. The inscription and assembly provenance are not yet exact enough."),
781: ("REVISE", "O1's header names Dunan Zongyan, not Fozhao Deguang. O2 explicitly names Lingyan Chu, O4 Chengshan Qia, and O5 a recoverable sermon owner; all are misowned or left generic."),
782: ("KEEP", "The bowing cloth remains one concrete object across prostration, display, spreading, and the Caoqi land-measure story. All eight selected uses are correctly classified as encounter narration."),
783: ("REVISE", "O1/O3/O4 identify an alms officer in narration or an occasion heading; O5 is a thanks heading. The officeholder, record subject, or respondent is repeatedly assigned as if uttering 化主."),
784: ("KEEP", "All six exact headword turns are now correctly assigned to unnamed questioners, with Wuye, Muzhou Daoming, and Tiantai Deshao linked separately as respondents. The differing answers properly limit the entry."),
785: ("REVISE", "O9 is the layman's direct threat 待我一一舉向明眼人, not documentary narration. Other verse/commentary and sermon owners remain generic where their sections identify them."),
786: ("REVISE", "O3 is the questioner's 四天王天 phrase, not Langting Jingting's answer. O4 is a current named-master sermon rather than a generic assembly voice."),
787: ("REVISE", "The preferred target 'where it is used' misdefines 用處 as use, function, utility, or effective point. The nested 不用處定 occurrence is a distinct longer term and cannot support this single sense."),
788: ("REVISE", "O2/O3/O7 are TOC-only name listings despite the worksheet's claimed TOC exclusion. The opening's imported 'foremost in wisdom' identification is not established by the selected Chan deployments."),
789: ("REVISE", "O2/O3/O4/O6 place 德山入門便棒 in the monk's question, not in Yunfeng, Baofu, Zhimen, or Zeng Kai's response. The respondent-as-utterer defect survives."),
790: ("REVISE", "O3 explicitly introduces 寶峯弁云 before 把住要津 yet uses a generic later-commentator branch. The named quoted actor must be recovered and linked."),
791: ("REVISE", "O3/O6 are TOC-only listings. O8 places 須菩提 in the Buddha's quoted rebuke, not generic documentary narration, and the opening imports an emptiness association broader than these Chan deployments establish."),
792: ("REVISE", "O4 has 夾山…示眾云 immediately before the headword: Jiashan Shanhui owns the utterance, not a generic commentator on Jiashan."),
793: ("REVISE", "O3/O5 contain Hanshan and Zhaozhou turns rather than Fenggan speech; O4 embeds Linji's exclamation; O7 embeds Shitou's exclamation. Nearby record owners or later quoters are not the exact quoted actors."),
794: ("REVISE", "O6 is documentary date prose 辛酉佛誕日 inside a preface/letter, not an editorial occasion label. The actor kind and grammar proof are false even though most other rows are headings."),
795: ("REVISE", "O1/O2/O5/O6 put 如何是主中主 in questioner turns but assign respondents. O3 is prefatory narration, O7 embeds Dongshan's quoted definition, and O8 is a recoverable sermon owner."),
796: ("REVISE", "O1 is Nanquan's question to Huangbo; O3 is a monk's question to Fayan; O4/O6 are Zhizang's question; O7 is Maqiaoshan Benkong's sermon. O2 is biographical title narration and O9 a TOC, not the current actor states."),
797: ("KEEP", "All eight selected clauses are now correctly death-biography narration, with each deceased master only a person-described context. The years-since-ordination seniority referent is consistent."),
798: ("KEEP", "The greeting remains the same formal salutation across rule prose and encounter narration. Performers and respondents are no longer misrepresented as uttering the narrator's 問訊 wording."),
799: ("REVISE", "O2's 三要 is the woman's three requested things, not Linji's Three Essentials. That different lexical referent contaminates the sole Linji-category sense."),
800: ("KEEP", "The three referents are distinguishable and separately anchored: Fragrant Accumulation realm, Master Xiangji Zi, and Xiangji Monastery. O3/O4 correctly belong to Yunfeng Wenyue, and the proper-name rows are narration."),
}

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

ledger = json.loads(LEDGER.read_text(encoding="utf-8"))
rows = []
for source in ledger["entries"]:
    ordinal = source["ordinal"]
    entry_path = BUILD / "entries" / source["id"] / "entry.v2.json"
    draft_path = BUILD / "entries" / source["id"] / "evidence.draft.json"
    entry = json.loads(entry_path.read_text(encoding="utf-8"))
    occurrence_count = sum(len(s.get("Occurrences", [])) for s in entry["Senses"])
    verdict, finding = findings[ordinal]
    current = sha(entry_path)
    if current != source["entrySha256"]:
        raise SystemExit(f"hash drift for {ordinal}: ledger={source['entrySha256']} current={current}")
    rows.append({
        "ordinal": ordinal,
        "id": source["id"],
        "term": entry["SourceTerm"],
        "entrySha256": current,
        "worksheetSha256": sha(draft_path),
        "verdict": verdict,
        "occurrencesRead": occurrence_count,
        "finding": finding,
    })

summary = {"KEEP": sum(r["verdict"] == "KEEP" for r in rows), "REVISE": sum(r["verdict"] == "REVISE" for r in rows)}
report = {
    "schemaVersion": "f003-fresh-independent-exact-review-v1",
    "reviewType": "read-only fresh semantic, sense, prose, and full-case exact-actor review",
    "wave": "f003",
    "lane": "B",
    "ordinals": [751, 800],
    "generatedUtc": datetime.now(timezone.utc).isoformat(),
    "reviewer": "Codex fresh independent reviewer /root/f003_b751_800_fresh_review",
    "readOnly": True,
    "entriesEdited": 0,
    "promotionOrMergePerformed": False,
    "siteTouched": False,
    "repairLedger": "fresh-build/waves/" + LEDGER.name,
    "repairLedgerSha256": sha(LEDGER),
    "formalGate": "fresh-build/waves/" + FORMAL.name,
    "formalGateSha256": sha(FORMAL),
    "formalGateHardPass": json.loads(FORMAL.read_text(encoding="utf-8"))["hardPass"],
    "currentHashesVerified": True,
    "occurrencesReadInFullCaseContext": sum(r["occurrencesRead"] for r in rows),
    "summary": summary,
    "structuralFinding": "Formal mechanics pass 355/355, but fresh reading still finds respondent-as-utterer, biography/office, embedded-quotation, TOC/nested-term, multi-actor-KWIC, and different-referent defects. Mechanics do not establish semantic or exact-turn ownership.",
    "decileCheckpoints": [
        {"range": f"{start}-{start+9}", "KEEP": sum(r["verdict"] == "KEEP" for r in rows if start <= r["ordinal"] <= start+9), "REVISE": sum(r["verdict"] == "REVISE" for r in rows if start <= r["ordinal"] <= start+9)}
        for start in range(751, 801, 10)
    ],
    "rows": rows,
}
OUT.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"out": str(OUT), "summary": summary, "occurrences": report["occurrencesReadInFullCaseContext"]}, ensure_ascii=False))
