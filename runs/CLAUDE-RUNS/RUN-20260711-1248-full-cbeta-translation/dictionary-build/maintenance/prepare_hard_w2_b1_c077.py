#!/usr/bin/env python3
"""Write the human-reviewed C077 hard-bundle overrides; makes no entry edits."""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SHEET = ROOT / "maintenance/hard-bundle-inputs/w2-b1/decisions-C077n1710.json"
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]
UTC = "2026-07-14T07:15:00Z"
TITLE = "Old Recorded Sayings of Venerable Masters (古尊宿語錄)"


def master(name: str, note: str, context: list[tuple[str, list[str]]] | None = None) -> dict:
    d = {"MasterName": name, "AttributionNote": f"{TITLE}: {note}"}
    if context:
        d["ContextMasters"] = [{"MasterName": n, "Roles": roles} for n, roles in context]
    return d


def unnamed(kind: str, label: str, role: str, note: str, context: list[tuple[str, list[str]]] | None = None) -> dict:
    d = {
        "ActorAttribution": {
            "Status": "reviewed-unnamed", "Kind": kind, "ActorLabel": label,
            "ActorRole": role, "RungsChecked": RUNGS, "ReviewedBy": "Codex hard-w2-b1",
            "ReviewedUtc": UTC,
        },
        "AttributionNote": f"{TITLE}: {note} The line, expanded context, section header, book title, TEI header, and parallel-passage search do not name the {label}.",
    }
    if context:
        d["ContextMasters"] = [{"MasterName": n, "Roles": roles} for n, roles in context]
    return d


O = {
"t_7887dc8d449f:0615a01:1:1": {
    "ActorAttribution": {"Status": "impersonal", "Kind": "document title and contents list", "ActorLabel": "document title and contents list", "ActorRole": "metadata", "GrammarEvidence": "The headword occurs in the volume title 古尊宿語錄 and its contents list, not in a speech turn.", "ReviewedBy": "Codex hard-w2-b1", "ReviewedUtc": UTC},
    "AttributionNote": f"{TITLE}: the document title and contents list use 尊宿 impersonally while listing Nanyue Huairang, Mazu Daoyi, and Baizhang Huaihai.",
    "ContextMasters": [{"MasterName": "Nanyue Huairang", "Roles": ["listed-record-subject"]}, {"MasterName": "Mazu Daoyi", "Roles": ["listed-record-subject"]}, {"MasterName": "Baizhang Huaihai", "Roles": ["listed-record-subject"]}],
},
"t_dfd1dbffe9f2:0616c02:1:2": unnamed("monk", "unnamed monk", "questioner", "in Mazu Daoyi's record, an unnamed monk asks why Mazu says 'this mind is Buddha'; Mazu answers that it stops a small child's crying.", [("Mazu Daoyi", ["respondent", "record-subject"])]),
"t_d1e06fd225fa:0617a10:1:5": master("Mazu Daoyi", "Mazu Daoyi is the grammatical subject of the biographical notice saying that his chamber-entering disciples numbered 139."),
"t_8bd6933e6de3:0617c01:1:7": master("Baizhang Huaihai", "Baizhang Huaihai says that Master Ma once gave him one shout and that he was deaf for three days.", [("Mazu Daoyi", ["source-of-quoted-shout"])]),
"t_43ecdacadde0:0617c11:1:5": master("Yunyan Tansheng", "Yunyan Tansheng is named immediately before 問 and asks Baizhang Huaihai for whom he busies himself every day.", [("Baizhang Huaihai", ["respondent", "record-subject"])]),
"t_63ca7d059ee8:0618a08:1:2": unnamed("monastic assembly", "unnamed monastic assembly", "nonresponding group", "in Baizhang Huaihai's record, the assembled monks are the exact grammatical subject of 眾無語 after Baizhang tests them with the sauce jars.", [("Baizhang Huaihai", ["questioner", "record-subject"])]),
"t_f04c29743e77:0630a17:1:1": master("Huangbo Xiyun", "Huangbo Xiyun is the continuing speaker in his named record and calls the old exemplar an idle person of the Way who has ended learning."),
"t_77821881a767:0667a20:1:4": master("Shoushan Xingnian", "Shoushan Xingnian says in his own named record that even old Shakyamuni arriving here would receive thirty blows."),
"t_eedf4100b3d7:0667b14:1:4": unnamed("monk", "unnamed monk", "questioner", "in Shoushan Xingnian's record, an unnamed monk asks what happens on meeting a lion's roar; Shoushan answers and the monk shouts.", [("Shoushan Xingnian", ["respondent", "record-subject"])]),
"t_1793c3514a69:0669b20:1:4": unnamed("monk", "unnamed monk", "questioner", "in Shoushan Xingnian's record, an unnamed monk raises Deshan's stick and Linji's shout as a question; Shoushan answers.", [("Shoushan Xingnian", ["respondent", "record-subject"]), ("Deshan Xuanjian", ["named-case-master"]), ("Linji Yixuan", ["named-case-master"])]),
"t_e5259ce8bbf5:0669c04:2:3": unnamed("monk", "unnamed monk", "questioner", "in Shoushan Xingnian's record, an unnamed monk requests that the master offer an indication; Shoushan answers 'under the staff a shooting star bursts.'", [("Shoushan Xingnian", ["respondent", "record-subject"])]),
"t_961b548d6462:0670b15:1:2": unnamed("monk", "unnamed monk", "questioner", "in Shoushan Xingnian's record, an unnamed monk states the before-the-word and after-the-phrase contrast and asks for a device; Shoushan raises the whisk.", [("Shoushan Xingnian", ["respondent", "record-subject"])]),
"t_36aa29eb1287:0672c18:1:8": master("Shoushan Xingnian", "Shoushan Xingnian asks an unnamed monk whether the water buffalo is at ease; the monk answers and Shoushan continues the test."),
"t_b8d2633b12ef:0673b02:1:1": unnamed("monk", "unnamed monk", "questioner", "in Shoushan Xingnian's record, an unnamed monk asks what exposed fault he has after Shoushan calls his answer wrong.", [("Shoushan Xingnian", ["respondent", "record-subject"])]),
"t_936118ea496c:0676a06:1:2": master("Shimen Yuncong", "Shimen Yuncong, identified in the section as Shimen Cizhao Yuncong (石門山慈照禪師蘊聦), answers 'eat gruel, eat rice' when asked what is non-Dharma."),
"t_4cf045deab37:0682a12:1:2": master("Fenyang Shanzhao", "Fenyang Shanzhao is the named subject and speaker who says that he has long been a gruel-and-rice monk and that transmitting the Buddha-mind lineage is no small matter."),
"t_84e490b1773f:0691b05:1:5": unnamed("monk", "unnamed monk", "interlocutor", "in Ciming Chuyuan's record, an unnamed monk makes the palm-clapping action; Ciming rebukes and drives him from the hall.", [("Ciming Chuyuan", ["respondent", "record-subject"])]),
"t_8bd6933e6de3:0692a11:1:10": master("Ciming Chuyuan", "Ciming Chuyuan completes his own teaching-seat address with one shout and leaves the seat."),
"t_2d4525b4b123:0732b03:1:5": unnamed("monk", "unnamed monk", "quoted questioner", "in a case raised and commented on by Yunmen Wenyan, an unnamed monk asks Shishuang Qingzhu for the one phrase transmitted outside the teaching.", [("Shishuang Qingzhu", ["quoted-respondent"]), ("Yunmen Wenyan", ["later-raiser", "commentator", "record-subject"])]),
"t_35c3fb655630:0788c03:1:4": master("Yexian Guixing", "Yexian Guixing speaks from the teaching seat in his Guangjiao record and calls for someone to answer the testing barrier, intercepting function, and enlivening phrase."),
"t_5854f7c24ddf:0801b05:1:5": master("Dayu Shouzhi", "Dayu Shouzhi, the named owner of the Cuiyan Monastery record, strikes the Chan seat once during his exchange with an unnamed monk."),
"t_78f95517a347:0808a20:1:2": unnamed("monk", "unnamed monk", "questioner", "in Fahua Quanju's record, an unnamed monk says that birth and death are a great matter and asks the master to save him.", [("Fahua Quanju", ["respondent", "record-subject"])]),
"t_8bd6933e6de3:0808c13:1:9": unnamed("monk", "unnamed monk", "interlocutor", "in Fahua Quanju's record, an unnamed monk gives the exact shout after Fahua challenges his claim about the great function.", [("Fahua Quanju", ["respondent", "record-subject"])]),
"t_ab6276be6e08:0817a16:1:8": master("Foyan Qingyuan", "Foyan Qingyuan gives the memorial-morning address for his late teacher and says that the teacher's final phrase picked something out from beneath people's skin.", [("Wuzu Fayan", ["deceased-teacher", "subject-of-memorial"])]),
"t_6edb551acb53:0917a14:1:1": master("Huineng", "inside a case raised in Yunfeng Wenyue's record, Huineng is the quoted patriarch who says that the novice will become only a follower of an understanding-based lineage.", [("Yunfeng Wenyue", ["later-raiser", "commentator", "record-subject"])]),
"t_4cf045deab37:0938c04:2:1": master("Zhenjing Kewen", "Zhenjing Kewen tells those who have reached Guizong that each must pass with an empty mind and must not become a long-term gruel-and-rice monk."),
"t_c0a6177c9c44:0948a13:2:1": master("Zhenjing Kewen", "Zhenjing Kewen is the author of the verse on the Xuefeng turtle-nosed-snake case that says the old tune is not a musical measure."),
}

d = json.loads(SHEET.read_text(encoding="utf-8"))
keys = [r["key"] for r in d["rows"]]
assert set(keys) == set(O), (set(keys) - set(O), set(O) - set(keys))
for row in d["rows"]:
    row["Override"] = O[row["key"]]
d["reviewedAllCases"] = True
d["reviewer"] = "Codex hard-w2-b1"
d["reviewedUtc"] = UTC
SHEET.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(f"wrote {len(d['rows'])} reviewed overrides to {SHEET}")
