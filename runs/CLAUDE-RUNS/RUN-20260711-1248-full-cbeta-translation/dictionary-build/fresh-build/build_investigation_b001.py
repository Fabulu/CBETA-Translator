#!/usr/bin/env python3
"""Explicit evidence-first article for Lane B position 001: 活潑潑."""

from __future__ import annotations

import datetime
import json
import subprocess
import sys
from pathlib import Path

DB = Path(__file__).resolve().parent.parent
ROOT = DB / "fresh-build"
sys.path.insert(0, str(DB))
import zc  # noqa: E402

TERM = "活潑潑"
ID = "t_3ad5ae4da39d"
BASE = "42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a"
NOW = datetime.datetime.now(datetime.timezone.utc).isoformat()

# Each row is an explicit full-turn decision made after reading the source unit.
EVIDENCE = [
    {
        "rel": "J/J27/J27nB193.xml",
        "work": "work:J27nB193",
        "kwic": "問：「如何是佛？」師云：「圓陀陀。」「如何是法？」師云：「活潑潑。」「如何是僧？」師云：「任騰騰。」",
        "master": "Yinyuan Longqi",
        "label": "Recorded Sayings of Chan Master Yinyuan",
        "decision": "Within Yinyuan Longqi’s own hall exchange, the anonymous monk asks three questions and Yinyuan utters the headword as his answer to the question about the teaching.",
    },
    {
        "rel": "J/J26/J26nB188.xml",
        "work": "work:J26nB188",
        "kwic": "師云：「若論此事，無人不具，無剎不彰，圓陀陀包括虛空，活潑潑遍呈萬象，露裸裸全無滲漏，赤條條永絕周遮，左右逢源，隨流得妙。」",
        "master": "Ruibai Mingxue",
        "label": "Recorded Sayings of Chan Master Ruibai Mingxue",
        "decision": "The complete inaugural hall address assigns this continuous sentence to Ruibai Mingxue; the preceding questioner does not utter the stored clause.",
    },
    {
        "rel": "J/J28/J28nB202.xml",
        "work": "work:J28nB202",
        "kwic": "若是衲僧活計，端的不在裏許。何也？拄杖如龍活潑潑，風軒竹徑任逍遙。",
        "master": "Baichi Xingyuan",
        "label": "Recorded Sayings of Chan Master Baichi Xingyuan",
        "decision": "The winter-solstice hall-address frame remains open through this sentence, so Baichi Xingyuan, not a quoted earlier speaker, utters the staff comparison.",
    },
    {
        "rel": "J/J37/J37nB386.xml",
        "work": "work:J37nB386",
        "kwic": "宗師家垂下一言半句、點出一機一境，正如箇錦標兒相似，擲向波間，東看成西、南觀成北，活潑潑地、轉轆轆地，無你近傍處、無你捉摸處。",
        "master": "Yuan'an Liao",
        "label": "Recorded Sayings of Chan Master Yuan’an Liao",
        "decision": "This clause lies inside Yuan'an Liao’s uninterrupted instruction; no embedded quotation or interlocutor takes over the headword-bearing sentence.",
    },
    {
        "rel": "J/J39/J39nB471.xml",
        "work": "work:J39nB471",
        "kwic": "山僧者莖拄杖生在荊棘叢中，用斧砍來，燒炮刮削，幾番琢磨，即今在山僧手裏活潑潑而來，激勵人天。",
        "master": "Konggu Daocheng",
        "label": "Recorded Sayings of Chan Master Konggu Daocheng",
        "decision": "Konggu Daocheng identifies himself as ‘this mountain monk’ and describes the staff now coming alive in his own hand; he is the exact utterer.",
    },
    {
        "rel": "J/J26/J26nB178.xml",
        "work": "work:J26nB178",
        "kwic": "結夏上堂，僧問：「個個現成活潑潑，因何特地又為牢？」",
        "master": None,
        "label": "Recorded Sayings of Chan Master Feiyin Tongrong",
        "decision": "The summer-retreat hall record explicitly assigns the exact headword-bearing question to an unnamed monk; Feiyin Tongrong is the respondent and record owner, not the utterer of this occurrence.",
        "actor": {
            "Status": "reviewed-unnamed",
            "Kind": "questioning monk",
            "ActorLabel": "an unnamed questioning monk",
            "ActorRole": "questioner",
            "RungsChecked": ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"],
            "GrammarEvidence": "The explicit frame 僧問 assigns the quoted question containing 活潑潑 to a monk. The full exchange, section, record title, TEI header, and parallel search do not name that monk.",
            "ReviewedBy": "Codex investigation-next300 Lane B explicit author",
            "ReviewedUtc": NOW,
        },
        "context": [{"MasterName": "Feiyin Tongrong", "Roles": ["respondent", "record-owner"]}],
    },
]


def main() -> None:
    occurrences = []
    for row in EVIDENCE:
        verified = zc.verify(row["rel"], row["kwic"])
        if not verified.get("ok"):
            raise SystemExit(f"zc.verify failed: {row['rel']} {verified}")
        occurrence = {
            "RelPath": row["rel"],
            "FromLb": verified["fromLb"],
            "ToLb": verified["toLb"],
            "Kwic": row["kwic"],
            "MasterName": row["master"],
            "Curated": True,
            "AttributionNote": f"Source record ({row['rel']}). {row['label']}: {row['decision']}",
            "ContextMasters": row.get("context", ([{"MasterName": row["master"], "Roles": ["utterer"]}] if row["master"] else [])),
            "DraftActorProof": {
                "ExactHeadwordClause": row["kwic"],
                "GrammaticalSubject": row["master"] or "an unnamed questioning monk",
                "SpeechFrame": row["decision"],
                "FullCaseDecision": row["decision"],
            },
        }
        if row.get("actor"):
            occurrence["ActorAttribution"] = row["actor"]
        occurrences.append(occurrence)

    entry = {
        "SchemaVersion": 1,
        "Entry": {
            "Id": ID,
            "SourceTerm": TERM,
            "CorpusBaselineSha256": BASE,
            "CreatedBy": "Codex investigation-next300 Lane B explicit author",
            "WrittenUtc": NOW,
            "Senses": [{
                "SenseKey": None,
                "MasterName": None,
                "PreferredTarget": "lively and active",
                "AlternateTargets": ["vividly active", "alive and responsive"],
                "SearchAliases": ["lively", "active", "alive", "responsive", "vividly active", "lively and active"],
                "Status": "preferred",
                "Validation": "multi-source",
                "Note": "Fresh concordance: 189 exact, apparatus-clean hits in 90 files representing 90 independent works; the punctuation-bridged discovery count is also 189.",
                "Occurrences": occurrences,
                "ClaimAnchors": [],
                "SourceTexts": [row["rel"] for row in EVIDENCE],
                "RelatedMasters": ["Yinyuan Longqi", "Ruibai Mingxue", "Baichi Xingyuan", "Yuan'an Liao", "Konggu Daocheng", "Feiyin Tongrong"],
                "RelatedTerms": ["圓陀陀", "轉轆轆"],
                "ExplanationParts": {
                    "CorpusEarnedOpening": "Lively and active is a favorable Chan appraisal for something presented as fully alive, freely moving, and difficult to seize.",
                    "EvidenceBody": [
                        "Yinyuan Longqi gives it as his direct answer to the question ‘what is the teaching?’, alongside ‘round and complete’ for buddha and ‘free and unhurried’ for the community.",
                        "Ruibai Mingxue pairs the expression with ‘round and complete’ and says it appears throughout the myriad forms; Yuan’an Liao joins it to ‘turning and rolling’ where there is no place to approach or grasp.",
                        "The adjective also operates on concrete teaching objects: Baichi Xingyuan calls his staff lively as a dragon, and Konggu Daocheng says the staff comes alive in his hand and rouses the assembly.",
                        "The explicitly unnamed questioner in Feiyin Tongrong’s record applies it to what is already complete in each person. Different objects receive the same appraisal; the evidence does not establish several different things or a hidden symbolic sense."
                    ],
                },
                "DraftEvidence": {
                    "OpeningClaimEvidenceKeys": ["o1", "o2", "o3", "o4", "o5", "o6"],
                    "ZenBend": "The records turn an ordinary word for liveliness into a public appraisal and even a direct answer about the teaching; staffs, sayings, and people can be judged by it.",
                    "CounterexampleOrLimit": "Some occurrences simply enliven scenery or prose. Those ordinary descriptive uses do not by themselves establish the Chan appraisal described here.",
                    "DifferentThingTest": {
                        "Decision": "one-thing",
                        "ComparedThings": ["a favorable appraisal of active liveliness", "the different people, sayings, and implements receiving that appraisal"],
                        "Reason": "The grammatical subjects vary, but 活潑潑 continues to predicate the same quality. The corpus does not use the word for a second object, title, or institutional role."
                    },
                    "AliasRationale": "‘Lively,’ ‘active,’ ‘alive,’ and ‘responsive’ are English retrieval words for the same attested quality; none introduces a separate reading.",
                    "ModifierControls": [
                        {"finding": "checked", "reason": "The intensified 活活潑潑 and adverbial 活潑潑地 were reviewed as grammatical forms of the same appraisal, not separate senses or padding for the exact headword count."}
                    ],
                    "FamilyControls": [
                        {"finding": "checked", "reason": "The recurring partners 圓陀陀 and 轉轆轆 were retained as related expressions; their hits were not counted as 活潑潑 unless the exact headword was present."}
                    ],
                    "IndependentWorkIds": [row["work"] for row in EVIDENCE],
                },
            }],
        },
    }

    out = ROOT / "entries" / ID
    out.mkdir(parents=True, exist_ok=True)
    draft = out / "evidence.draft.json"
    draft.write_text(json.dumps(entry, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    (out / "WORK.md").write_text("""# 活潑潑 — investigation-next300 Lane B position 001

- Frozen corpus: `42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a`.
- Fresh research: 189 exact hits / 90 independent works; bridged discovery also 189.
- Read the complete transported cases and expanded source turns for every selected occurrence.
- Definition probes and deployment inventory found: direct answer about the teaching; paired appraisal with 圓陀陀; no-approach/no-grasp instruction; staff/dragon comparison; staff-in-hand hall action; unnamed monk’s question.
- Ordinary scenic uses were retained as a counterexample, not promoted into a second sense.
- Different-thing decision: one quality predicated of several subjects, not several referents.
- Modifier/family review: 活活潑潑 and 活潑潑地 are grammatical forms; 圓陀陀 and 轉轆轆 are related terms, not evidence substitutes.
- Every stored Chinese span passed fresh `zc.verify`; independent support is counted by `work_id`.
- No lineage, roster, production `terms/`, install, merge, or publication file was changed.

feedback-inference-verdict: The opening is the smallest inference repeated by the six selected exact deployments.
feedback-observations: The word is a direct answer, a paired appraisal, and a predicate applied to teaching implements.
feedback-falsification-searches: Scenic description, intensified forms, neighboring collocations, titles, and duplicate works were checked.
feedback-counterexamples: Scenic liveliness alone does not establish a special Chan sense.
feedback-scope: Frozen 494-file / 487-work corpus only.
lookup-probes: lively; active; alive; responsive; vividly active.
opening-interpretation-verdict: The opening tells the reader what quality the records repeatedly predicate before naming the evidence.
""", encoding="utf-8")
    command = [sys.executable, str(DB / "compile_evidence_draft.py"), str(draft), "--output", str(out / "entry.v2.json"), "--report", str(out / "evidence-compile-report.json")]
    result = subprocess.run(command, text=True, capture_output=True)
    if result.returncode:
        raise SystemExit(result.stdout + result.stderr)
    print(result.stdout.strip())


if __name__ == "__main__":
    main()
