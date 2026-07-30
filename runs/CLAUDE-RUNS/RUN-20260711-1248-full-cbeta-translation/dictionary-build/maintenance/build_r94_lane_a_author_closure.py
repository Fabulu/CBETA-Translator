#!/usr/bin/env python3
"""Seal source-first semantic and actor decisions for R94 lane A."""

from __future__ import annotations

import hashlib
import json
import os
import time
from pathlib import Path


ROOT = Path(__file__).resolve().parent.parent
M = ROOT / "maintenance"
GATE = M / "non-iriya-v7-depth-regeneration-r94-timegate-root.json"
SELECTION = M / "non-iriya-v7-depth-regeneration-r94-selection-root.json"
EXTRACTION = M / "non-iriya-v7-depth-regeneration-r94-frozen-extraction-root.json"
SKELETON = M / "non-iriya-v7-depth-regeneration-r94-frozen-research-skeleton-root.json"
REVIEW = M / "r94-lane-a-cross-review-by-c.json"
OUT = M / "r94-lane-a-correction1-closure.json"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_exclusive(path: Path, value) -> None:
    encoded = (json.dumps(value, ensure_ascii=False, indent=2) + "\n").encode("utf-8")
    descriptor = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o644)
    try:
        os.write(descriptor, encoded)
        os.fsync(descriptor)
    finally:
        os.close(descriptor)


def linked(name: str, role: str = "utterer", **extra):
    return {"status": "linked", "actor": name, "role": role, **extra}


def unlinked(label: str, role: str = "utterer", **extra):
    return {
        "status": "identified-unlinked-master",
        "actor": label,
        "role": role,
        "rungsChecked": [
            "line",
            "expanded-context",
            "section-header",
            "book-title",
            "tei-header",
            "parallel-passage",
        ],
        **extra,
    }


decisions = [
    {
        "ordinal": 1,
        "id": "t_223c2f6ade25",
        "term": "一大事因緣",
        "preferredTarget": "the occasion of the one great matter",
        "alternates": ["the causal occasion of the one great matter"],
        "opening": "The occasion or causal circumstance of the one great matter.",
        "body": (
            "Guifeng Zongmi actively quotes the Buddha's formulation while explaining why "
            "the teaching appeared; Yuanwu Keqin and Zhongfeng Mingben independently use "
            "the phrase for the matter for which buddhas and patriarchs appeared."
        ),
        "note": (
            "The headword contains 因緣 as well as 一大事; translating only 'the one great "
            "matter' drops the phrase's final two graphs."
        ),
        "uses": [
            (
                "T/T48/T48n2015.xml",
                unlinked(
                    "Shakyamuni Buddha",
                    "quoted-original",
                    outerActor="Guifeng Zongmi",
                    deploymentRole="active-quotation",
                ),
                "great-matter:zongmi-active-lotus-quotation",
            ),
            (
                "X/X69/X69n1357.xml",
                linked("Yuanwu Keqin"),
                "great-matter:yuanwu-letter",
            ),
            (
                "B/B25/B25n0145.xml",
                linked("Zhongfeng Mingben"),
                "great-matter:zhongfeng-discourse",
            ),
        ],
        "finiteUncertainty": (
            "The phrase is inherited from the Lotus formulation; the three families are "
            "independent Chan deployments, not three claims of lexical coinage."
        ),
    },
    {
        "ordinal": 2,
        "id": "t_22885135d39e",
        "term": "一塵",
        "preferredTarget": "one speck of dust",
        "alternates": ["a single dust mote"],
        "opening": "One speck of dust, used as the smallest named particle in a scene or comparison.",
        "body": (
            "Xuefeng Huikong writes of gathering the great thousand-world without one speck; "
            "Micang Daokai actively quotes Yongjia Xuanjue's line about one speck entering "
            "concentration; Dawei Jinglun makes one speck encompass or equal worlds."
        ),
        "note": (
            "The retained evidence does not establish a separate lexical thing called a "
            "'thought-speck'; contextual comparisons do not change the noun."
        ),
        "uses": [
            ("D/D50/D50n8945.xml", linked("Xuefeng Huikong", "verse-author"), "dust:xuefeng-verse"),
            (
                "J/J23/J23nB118.xml",
                linked(
                    "Yongjia Xuanjue",
                    "quoted-original",
                    outerActor="Micang Daokai",
                    deploymentRole="active-quotation",
                ),
                "dust:micang-active-yongjia-quotation",
            ),
            ("J/J25/J25nB165.xml", unlinked("Dawei Jinglun", "author"), "dust:dawei-writing"),
        ],
        "finiteUncertainty": "Later compounds may lexicalize separately; they are not imported into 一塵.",
    },
    {
        "ordinal": 3,
        "id": "t_229d6fd2a889",
        "term": "仁者見之謂之仁",
        "preferredTarget": "the humane see it and call it humane",
        "alternates": ["the benevolent see it and call it benevolent"],
        "opening": (
            "The first half of the paired saying: the humane see it and call it humane, "
            "while the wise call it wise."
        ),
        "body": (
            "Dahui Zonggao quotes the formula in a case later appraised by Tianyin Yuanxiu; "
            "Tian'an Sheng applies it after his own hall remarks; an unnamed layman puts its "
            "paired wording to Sanyi Mingyu as a question."
        ),
        "note": "仁者 and 仁 deliberately repeat the same graph; preserve that repetition in English.",
        "uses": [
            (
                "J/J25/J25nB171.xml",
                linked(
                    "Dahui Zonggao",
                    "quoted-original",
                    outerActor="Tianyin Yuanxiu",
                    deploymentRole="active-quotation",
                ),
                "humane:dahui-quotation-tianyin-appraisal",
            ),
            ("J/J26/J26nB187.xml", linked("Tian'an Sheng"), "humane:tianan-hall-use"),
            (
                "J/J27/J27nB189.xml",
                {
                    "status": "reviewed-unnamed-non-master",
                    "actor": "an unnamed layman",
                    "role": "questioner",
                    "outerActor": "Sanyi Mingyu",
                    "rungsChecked": [
                        "line",
                        "expanded-context",
                        "section-header",
                        "book-title",
                        "tei-header",
                        "parallel-passage",
                    ],
                },
                "humane:sanyi-lay-question",
            ),
        ],
        "finiteUncertainty": (
            "The inherited source of the maxim is not treated as another Chan deployment family."
        ),
    },
    {
        "ordinal": 4,
        "id": "t_22b4a92f2919",
        "term": "收歸上科",
        "preferredTarget": "refer it back to the preceding category",
        "alternates": ["collect it under the foregoing heading"],
        "opening": (
            "A closing verdict that gathers the just-listed alternatives back under the "
            "preceding category or heading."
        ),
        "body": (
            "Feiyin Tongrong closes three different sections with it; Sanyi Mingyu applies "
            "it after 'two parts differ'; Baichi Xingyuan applies it after three unsuccessful answers."
        ),
        "note": (
            "上科 is the foregoing category or heading, not a claim that someone passed a "
            "modern examination."
        ),
        "uses": [
            ("J/J26/J26nB178.xml", linked("Feiyin Tongrong"), "upper-category:feiyin-letter"),
            ("J/J27/J27nB189.xml", linked("Sanyi Mingyu"), "upper-category:sanyi-hall"),
            ("J/J28/J28nB202.xml", linked("Baichi Xingyuan"), "upper-category:baichi-verdict"),
        ],
        "finiteUncertainty": "The examination-register wordplay remains visible but does not create a second sense.",
    },
    {
        "ordinal": 5,
        "id": "t_2310fbae5dc4",
        "term": "把手共行",
        "preferredTarget": "walk together hand in hand",
        "alternates": ["join hands and walk together"],
        "opening": "To join hands and walk together, used as a verdict of keeping company on equal footing.",
        "body": (
            "Jinul says a reader with trust and understanding can walk hand in hand with the "
            "ancients; Gulin Qingmao uses it in verse; Zhongfeng Mingben offers to walk hand "
            "in hand before names for staff and shout arise."
        ),
        "note": "The phrase names joint movement; it does not by itself establish agreement on every claim.",
        "uses": [
            ("T/T48/T48n2020.xml", linked("Jinul", "author"), "hand-in-hand:jinul-writing"),
            ("X/X71/X71n1413.xml", linked("Gulin Qingmao", "verse-author"), "hand-in-hand:gulin-verse"),
            ("B/B25/B25n0145.xml", linked("Zhongfeng Mingben"), "hand-in-hand:zhongfeng-discourse"),
        ],
        "finiteUncertainty": "Quoted historical figures in the surrounding cases are context, not headword actors.",
    },
    {
        "ordinal": 6,
        "id": "t_23204fbd253c",
        "term": "玄旨",
        "preferredTarget": "profound purport",
        "alternates": ["subtle purport"],
        "opening": "A profound or subtle purport that the surrounding speaker says must be recognized or penetrated.",
        "body": (
            "Fushan Benzhi writes of opening the profound purport; Yeyun Ying says Fengxue "
            "thoroughly penetrated it; Sengcan's attributed inscription warns that failing "
            "to recognize it makes quieting thoughts futile."
        ),
        "note": "玄 marks depth or subtlety here; it does not supply a separate occult doctrine.",
        "uses": [
            ("J/J25/J25nB166.xml", unlinked("Fushan Benzhi", "verse-author"), "profound-purport:fushan-verse"),
            ("J/J40/J40nB484.xml", unlinked("Yeyun Ying", "verse-author"), "profound-purport:yeyun-lineage-verse"),
            ("T/T48/T48n2010.xml", linked("Sengcan", "attributed-author"), "profound-purport:sengcan-inscription"),
        ],
        "finiteUncertainty": "The traditional attribution of the Inscription on Faith in Mind remains historically cautious.",
    },
    {
        "ordinal": 7,
        "id": "t_2325720f94cd",
        "term": "強作主宰",
        "preferredTarget": "force oneself to act as master",
        "alternates": ["forcibly make oneself the one in control"],
        "opening": "To force or contrive oneself into the role of master or controller.",
        "body": (
            "Wuyi Yuanlai, Zhongfeng Mingben, and Yulin Tongxiu independently criticize this "
            "contrived control when it is manufactured in stillness, imposed on circumstances, "
            "or mistaken for direct command."
        ),
        "note": "The phrase is pejorative in all three retained deployments; 強 marks the forcing.",
        "uses": [
            ("X/X63/X63n1257.xml", linked("Wuyi Yuanlai", "author"), "forced-master:wuyi-admonition"),
            ("B/B25/B25n0145.xml", linked("Zhongfeng Mingben"), "forced-master:zhongfeng-warning"),
            ("B/B27/B27n0152.xml", linked("Yulin Tongxiu"), "forced-master:yulin-critique"),
        ],
        "finiteUncertainty": "Other occurrences may contrast genuine command, but that contrast does not neutralize 強.",
    },
    {
        "ordinal": 8,
        "id": "t_2354ad61810c",
        "term": "貪瞋癡",
        "preferredTarget": "greed, anger, and delusion",
        "alternates": ["greed, anger, and ignorance"],
        "opening": "The three-item list greed, anger, and delusion.",
        "body": (
            "Yongjia Xuanjue places the triad among states to be absent; Weilin Daopei names "
            "the three while vowing not to let them run loose; "
            "the attributed Straight Talk on the True Mind uses it for reactions to agreeable, "
            "adverse, and neutral circumstances."
        ),
        "note": "The entry names the three words as used; it does not import an Abhidharma system as their definition.",
        "uses": [
            ("T/T48/T48n2013.xml", linked("Yongjia Xuanjue", "author"), "three-poisons:yongjia-vow"),
            ("X/X72/X72n1442.xml", linked("Weilin Daopei", "author"), "three-poisons:weilin-vow"),
            ("T/T48/T48n2019A.xml", linked("Jinul", "attributed-author"), "three-poisons:true-mind-treatise"),
        ],
        "finiteUncertainty": "The alternate 'ignorance' is lexical; no doctrinal expansion is licensed.",
    },
    {
        "ordinal": 9,
        "id": "t_2385e8874684",
        "term": "遞相鈍置",
        "preferredTarget": "take turns making fools of one another",
        "alternates": ["successively put one another at a disadvantage"],
        "opening": "To pass the disadvantage back and forth, taking turns making fools of one another.",
        "body": (
            "Zhongfeng Mingben applies it to the inherited succession; Tianyin Yuanxiu uses "
            "it for guest and host repeatedly constraining one another; Poshan Haiming uses "
            "it while criticizing Yunmen's response to the Buddha."
        ),
        "note": "鈍置 is an interpersonal putting-down or disadvantaging here, not literal loss of sharpness.",
        "uses": [
            ("B/B25/B25n0145.xml", linked("Zhongfeng Mingben"), "mutual-disadvantage:zhongfeng-succession"),
            ("J/J25/J25nB171.xml", linked("Tianyin Yuanxiu"), "mutual-disadvantage:tianyin-host-guest"),
            ("J/J26/J26nB177.xml", linked("Poshan Haiming"), "mutual-disadvantage:poshan-yunmen"),
        ],
        "finiteUncertainty": "The sharper 'make fools of' rendering is contextual; the alternate preserves the broader floor.",
    },
    {
        "ordinal": 10,
        "id": "t_23e82e80e367",
        "term": "渴飲饑餐",
        "preferredTarget": "drink when thirsty, eat when hungry",
        "alternates": ["thirsty, drink; hungry, eat"],
        "opening": "To drink when thirsty and eat when hungry.",
        "body": (
            "Puming places the pair in an oxherding verse; Huangbo Wunian uses it while "
            "describing his return to the mountains; Shiqi Tongyun calls it part of ordinary conduct."
        ),
        "note": "The phrase states two ordinary responses in parallel; no added meditation or present-moment gloss is needed.",
        "uses": [
            ("J/J23/J23nB128.xml", unlinked("Puming", "verse-author"), "thirst-hunger:puming-verse"),
            ("J/J20/J20nB098.xml", unlinked("Huangbo Wunian", "letter-writer"), "thirst-hunger:wunian-letter"),
            ("J/J26/J26nB183.xml", linked("Shiqi Tongyun"), "thirst-hunger:shiqi-verse"),
        ],
        "finiteUncertainty": "The composite oxherding collection requires the local verse heading for authorship, which is retained.",
    },
]


gate = read(GATE)
selection = read(SELECTION)
extraction = read(EXTRACTION)
selected_rows = selection["lanes"][0]["rows"]
if [(row["batchOrdinal"], row["identityId"], row["term"]) for row in selected_rows] != [
    (row["ordinal"], row["id"], row["term"]) for row in decisions
]:
    raise RuntimeError("lane A decision keys do not match the authoritative selection")
extracted = {row["id"]: row for row in extraction["rows"]}

entries = []
for decision in decisions:
    source_row = extracted[decision["id"]]
    candidates = {row["relPath"]: row for row in source_row["sourceCandidates"]}
    retained = []
    family_ids = set()
    for rel_path, actor, family in decision["uses"]:
        candidate = candidates[rel_path]
        if candidate["tier"] not in (1, 2):
            raise RuntimeError(f"{decision['id']}: Tier-3 evidence was selected")
        if family in family_ids:
            raise RuntimeError(f"{decision['id']}: duplicate family ID")
        family_ids.add(family)
        if decision["term"] not in candidate["context"]:
            raise RuntimeError(f"{decision['id']}: headword absent from retained context")
        retained.append(
            {
                "relPath": rel_path,
                "workId": candidate["workId"],
                "tier": candidate["tier"],
                "fromLb": candidate["fromLb"],
                "toLb": candidate["toLb"],
                "contextSha256": candidate["contextSha256"],
                "spanSha256": candidate["spanSha256"],
                "witnessFamilyId": family,
                "deploymentRole": actor.get("deploymentRole", "original-use"),
                "actorDecision": actor,
                "semanticDecision": "retain",
            }
        )
    if len(retained) != 3 or len({row["workId"] for row in retained}) != 3:
        raise RuntimeError(f"{decision['id']}: three-work floor not met")
    reserve = [
        {
            "relPath": row["relPath"],
            "workId": row["workId"],
            "tier": row["tier"],
            "contextSha256": row["contextSha256"],
            "decision": "reserve-not-needed-for-three-family-floor",
        }
        for row in source_row["sourceCandidates"]
        if row["relPath"] not in {use[0] for use in decision["uses"]}
    ]
    entries.append(
        {
            **{key: value for key, value in decision.items() if key != "uses"},
            "senseRuling": "one corpus-wide sense",
            "retained": retained,
            "reserve": reserve,
            "tierMix": {
                "tier1": sum(row["tier"] == 1 for row in retained),
                "tier2": sum(row["tier"] == 2 for row in retained),
                "tier3": 0,
            },
            "independentProofFamilies": 3,
            "lampFallbackRequired": False,
            "semanticReadComplete": True,
        }
    )

elapsed = time.time() - gate["startedEpoch"]
payload = {
    "schemaVersion": "r94-lane-author-closure.v1",
    "cohort": "R94",
    "lane": "A",
    "author": "/root/source_tiers_b",
    "scope": {"ordinalFrom": 1, "ordinalTo": 10, "entryCount": 10},
    "bindings": {
        "artifactZero": {"path": str(GATE), "sha256": sha(GATE)},
        "selection": {"path": str(SELECTION), "sha256": sha(SELECTION)},
        "frozenExtraction": {"path": str(EXTRACTION), "sha256": sha(EXTRACTION)},
        "frozenSkeleton": {"path": str(SKELETON), "sha256": sha(SKELETON)},
        "crossReview": {"path": str(REVIEW), "sha256": sha(REVIEW)},
    },
    "correctionPass": {
        "reviewBound": True,
        "finiteDeltaCount": 4,
        "changedEntryIds": [
            "t_22885135d39e",
            "t_229d6fd2a889",
            "t_22b4a92f2919",
            "t_2354ad61810c",
        ],
        "dispositions": [
            "一塵: Yongjia Xuanjue quoted-original; Micang Daokai outer active quoter",
            "仁者見之謂之仁: unnamed layman closed as reviewed-unnamed non-master",
            "收歸上科: Baichi Xingyuan canonical roster link",
            "貪瞋癡: excluded T48n2016 preface and retained frozen X72n1442 Weilin Daopei authored vow",
        ],
        "newSearchPerformed": False,
        "lampPaddingAdded": False,
    },
    "sourcePolicy": {
        "priority": ["Tier 1 authored", "Tier 2 recorded sayings", "Tier 3 lamps"],
        "tier3Rule": "last-resort only",
        "minimumIndependentProofFamilies": 3,
        "contentRebuiltFromScratch": True,
    },
    "entries": entries,
    "summary": {
        "entriesAdjudicated": 10,
        "occurrencesRetained": 30,
        "tier1": sum(row["tierMix"]["tier1"] for row in entries),
        "tier2": sum(row["tierMix"]["tier2"] for row in entries),
        "tier3": 0,
        "lampFallbackEntries": [],
        "fewerThanThreeFamilies": [],
        "semanticBlockers": [],
    },
    "crossReviewAssignment": {
        "reviewer": "/root/source_tiers_b/r94_lane_c",
        "reviewScope": "all ten entries and all thirty retained coordinates",
        "selfReview": False,
    },
    "elapsedSeconds": elapsed,
    "deadlineSeconds": gate["deadlinesSeconds"]["adjudicatedConfig"],
    "withinDeadline": elapsed <= gate["deadlinesSeconds"]["adjudicatedConfig"],
    "publicMutationPerformed": False,
    "productMutationPerformed": False,
    "releaseAuthorized": False,
    "hardPass": False,
    "pending": "changed-coordinate independent rereview",
}
write_exclusive(OUT, payload)
print(json.dumps({"path": str(OUT), "sha256": sha(OUT), "summary": payload["summary"]}))
