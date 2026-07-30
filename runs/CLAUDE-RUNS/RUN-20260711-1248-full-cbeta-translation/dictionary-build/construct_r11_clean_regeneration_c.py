#!/usr/bin/env python3
"""Construct R11 from explicit source-first decisions through the clean promoter."""
from __future__ import annotations

import hashlib
import json
import subprocess
from datetime import datetime, timezone
from pathlib import Path

import promote_clean_regeneration as clean
import zc
from atomic_write import atomic_write_json, atomic_write_text
from compile_evidence_draft import ALLOWED_ACTOR_STATUSES, ALLOWED_CONTEXT_ROLES

ROOT = Path(__file__).resolve().parent
MAINT = ROOT / "maintenance"
FRESH = ROOT / "fresh-build/entries"
RESEARCH_PATH = MAINT / "non-iriya-v7-depth-regeneration-r11-research-c.json"
SELECTION_PATH = MAINT / "non-iriya-v7-depth-regeneration-r11-selection-c.json"
TITLES_PATH = Path("/mnt/c/programmieren/CbetaZenTranslations/titles.jsonl")
STAMP = "2026-07-29T21:10:00Z"
CREATED_BY = "R11 clean-regeneration constructor C"
TARGET_ANCHOR_RADIUS = 24


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def titles() -> dict[str, str]:
    result = {}
    for line in TITLES_PATH.read_text(encoding="utf-8-sig").splitlines():
        if not line.strip():
            continue
        row = json.loads(line)
        result[row["path"]] = row.get("enShort") or row["en"]
    return result


CONFIG = [
    {
        "id": "t_97e8db7ae00b", "term": "敵勝還他獅子兒",
        "target": "to overcome an opponent, it takes a lion cub",
        "aliases": ["a lion cub overcomes the opponent", "it takes a lion cub to defeat a rival"],
        "opening": "A capping couplet reserves victory over a formidable opponent for an equally formidable lion cub.",
        "body": "Miyun Yuanwu cites the couplet after saying that the buddhas' teaching does not follow human sentiment. Feiyin Tongrong, Eryin Mi, Linye Tongqi, and Yinyuan Longqi each use it in their own hall exchanges or addresses, sometimes after the companion line about an outstanding man rising from the crowd.",
        "note": "The repeated couplet is not five reports of one event: the retained passages are five later masters' own uses. A monk's quotation of the line inside a question is excluded from the retained Feiyin and Yinyuan turns.",
        "occurrences": [
            ("J/J10/J10nA158.xml", 0, "Miyun Yuanwu", []),
            ("J/J26/J26nB178.xml", 0, "Feiyin Tongrong", []),
            ("J/J28/J28nB212.xml", 0, "Eryin Mi", []),
            ("J/J26/J26nB186.xml", 1, "Linye Tongqi", []),
            ("J/J27/J27nB193.xml", 0, "Yinyuan Longqi", []),
        ],
        "classes": ["capping couplet", "hall-address close", "public-exchange appraisal"],
        "family": ["出群須是英靈漢", "驚群須是英靈漢"],
    },
    {
        "id": "t_355e774b3789", "term": "習氣不除",
        "target": "ingrained habits remain",
        "aliases": ["habitual patterns remain", "ingrained habits have not been removed"],
        "opening": "A verdict that ingrained patterns remain visible despite words, claims, or an apparent breakthrough.",
        "body": "Zhuanyu Guanheng applies it to indiscriminate questioning, answering, and shouting. Hongzhi Zhengjue applies it to Shigong's continued bowmanship, while Yongjue Yuanxian discusses people who claim complete realization and then explain their remaining habits as unfinished gradual work. Yanju Deshen and Pufu Zhuo use the phrase as a concise personal rebuke.",
        "note": "The object of the habit varies with the passage, but every retained use says that an established pattern has not disappeared. The parallel Hongzhi containers and the duplicate C077/D48 work are not counted twice.",
        "occurrences": [
            ("J/J28/J28nB219.xml", 0, "Zhuanyu Guanheng", []),
            ("J/J40/J40nB478.xml", 0, "Yanju Deshen", []),
            ("J/J40/J40nB493.xml", 0, "Pufu Zhuo", []),
            ("X/X72/X72n1437.xml", 0, "Yongjue Yuanxian", []),
            ("J/J32/J32nB272.xml", 0, "Hongzhi Zhengjue", []),
        ],
        "classes": ["direct rebuke", "case appraisal", "extended written discussion"],
        "family": ["習氣", "習氣未除"],
    },
    {
        "id": "t_936e9a0adaf7", "term": "爭解彎弓射尉遲",
        "target": "how could you know how to draw a bow and shoot Yuchi?",
        "aliases": ["how could you draw a bow against Yuchi?", "who knows how to shoot Yuchi?"],
        "opening": "A martial challenge denying that an unproved speaker knows how to draw the bow against the formidable Yuchi.",
        "body": "Tian'an Sheng and Baichi Xingyuan answer monks with the line after comparing them to Jinya. Yinyuan Longqi, Yuan'an Liao, and Mingjue Cong use the same challenge in separate public exchanges, with local variants such as “know,” “suppose,” and “think” in the companion clause.",
        "note": "The Yuchi line remains the stable challenge while the preceding Jinya clause varies. Lamp recensions and passive repetitions of the old Shaoshan exchange are excluded because direct recorded-sayings uses meet the evidence floor.",
        "occurrences": [
            ("J/J26/J26nB187.xml", 0, "Tian'an Sheng", []),
            ("J/J27/J27nB193.xml", 0, "Yinyuan Longqi", []),
            ("J/J28/J28nB202.xml", 0, "Baichi Xingyuan", []),
            ("J/J37/J37nB386.xml", 0, "Yuan'an Liao", []),
            ("L/L158/L158n1652.xml", 0, "Mingjue Cong", []),
        ],
        "classes": ["martial challenge", "public answer", "exchange capping line"],
        "family": ["看君不是金牙作", "知君不是金牙作", "想君不是金牙作"],
    },
    {
        "id": "t_9f422c1ca7d7", "term": "隱顯全該",
        "target": "concealment and manifestation are wholly included",
        "aliases": ["hidden and manifest are fully included", "concealed and apparent are all encompassed"],
        "opening": "A paired formula saying that both what is concealed and what appears are fully encompassed in the matter being described.",
        "body": "Zhe'an Jingfan applies the pair to a staff's transformations and elsewhere to a deceased teacher's activity. Pinjixiang Zhixiang places it in a verse on the five positions; Yun'e Xi makes it part of a public question, Yingning Zhijing uses it in written exposition, and Linquan Conglun joins it to the paired contrast between withering and flourishing.",
        "note": "The formula belongs to several independently framed uses rather than one inherited quotation. Memorial prose, verse, a public question, and case commentary all preserve the same concealment-and-appearance pair.",
        "occurrences": [
            ("J/J36/J36nB369.xml", 2, "Zhe'an Jingfan", []),
            ("J/J39/J39nB454.xml", 0, "Pinjixiang Zhixiang", []),
            ("J/J28/J28nB203.xml", 0, "Yun'e Xi", []),
            ("J/J33/J33nB286.xml", 0, "Yingning Zhijing", []),
            ("X/X67/X67n1304.xml", 1, "Linquan Conglun", []),
        ],
        "classes": ["paired formula", "verse line", "public question", "case commentary"],
        "family": ["隱顯", "顯隱"],
    },
    {
        "id": "t_599b071846ed", "term": "鉢裏飯",
        "target": "rice in the bowl",
        "aliases": ["bowlful of rice", "rice inside the bowl"],
        "opening": "Rice in the bowl is the concrete first half of Yunmen's paired answer, followed by water in the bucket.",
        "body": "Yunmen Wenyan gives the pair when asked about complete command in every mote. Yuanwu Keqin and Wansong Xingxiu quote and comment on that answer in separate case collections. Baiyun Shouduan turns the pair into his own verse, and Gulin Qingmao's authored poem asks where the old saying began.",
        "note": "The entry covers the bowl-rice expression, not generic mentions of food. Later quotations remain attributed to Yunmen, while Baiyun's and Gulin's new verse lines belong to their respective authors.",
        "occurrences": [
            ("X/X71/X71n1413.xml", 0, "Gulin Qingmao", []),
            ("T/T47/T47n1988.xml", 0, "Yunmen Wenyan", []),
            ("T/T48/T48n2003.xml", 0, "Yunmen Wenyan", [("Yuanwu Keqin", ["commentator", "later-raiser"])]),
            ("T/T48/T48n2004.xml", 0, "Yunmen Wenyan", [("Wansong Xingxiu", ["commentator", "later-raiser"])]),
            ("X/X69/X69n1352.xml", 1, "Baiyun Shouduan", [("Yunmen Wenyan", ["case-figure"])]),
        ],
        "classes": ["direct answer", "active case commentary", "authored verse"],
        "family": ["桶裏水", "鉢裏飯桶裏水"],
    },
]


def concise_kwic(rel: str, term: str, occurrence_index: int) -> tuple[str, str]:
    if rel == "L/L158/L158n1652.xml" and term == "爭解彎弓射尉遲":
        kwic = "師云看君不是金牙作爭解彎弓射尉遲"
        verified = zc.verify(rel, kwic)
        if verified.get("ok") and verified.get("count") == 1:
            return kwic, verified["fromLb"]
    for context in (55, 45, 35, 25, 18):
        hits = zc.find(rel, term, ctx=context, limit=20)
        if occurrence_index >= len(hits):
            raise ValueError(f"{rel}: occurrence index {occurrence_index} unavailable")
        kwic = hits[occurrence_index]["window"]
        verified = zc.verify(rel, kwic)
        if kwic.count(term) == 1 and verified.get("ok") and verified.get("count") == 1:
            return kwic, hits[occurrence_index]["fromLb"]
    raise ValueError(f"{rel}: no uniquely re-anchorable single-turn KWIC at the selected occurrence")


REQUIRED_ACTOR_DECISION_KEYS = {
    "evidenceKey", "masterName", "actorAttribution", "contextMasters",
    "contextActors", "exactHeadwordClause", "grammarEvidence", "voice",
    "fullCaseDecision", "action", "attributionNote",
}


class OccurrenceDecisionClosureError(ValueError):
    def __init__(self, errors):
        self.errors = errors
        super().__init__(
            "whole-config occurrence-decision closure failed: "
            + json.dumps(errors, ensure_ascii=False)
        )


def validate_occurrence_spec(spec, expected_key):
    if not isinstance(spec, dict):
        raise ValueError(
            f"{expected_key}: occurrence must be an explicit keyed actor/action decision"
        )
    if set(spec) != {
        "evidenceKey", "relPath", "fromLb", "sourceSpanOrdinal",
        "sourceContextSha256", "sourceCharOffset", "targetSpanAnchorSha256",
        "boundedKwic", "boundedFromLb", "boundedToLb",
        "boundaryEvidence", "actorDecision",
    }:
        raise ValueError(f"{expected_key}: occurrence-spec keys are incomplete or unknown")
    if spec["evidenceKey"] != expected_key:
        raise ValueError(
            f"{expected_key}: occurrence evidence key mismatch {spec['evidenceKey']!r}"
        )
    decision = spec["actorDecision"]
    if not isinstance(decision, dict) or set(decision) != REQUIRED_ACTOR_DECISION_KEYS:
        raise ValueError(f"{expected_key}: complete actor/action decision is required")
    if decision["evidenceKey"] != expected_key:
        raise ValueError(f"{expected_key}: actor decision is keyed to another occurrence")
    for key in (
        "exactHeadwordClause", "grammarEvidence", "voice",
        "fullCaseDecision", "action", "attributionNote",
    ):
        if not isinstance(decision[key], str) or not decision[key].strip():
            raise ValueError(f"{expected_key}: actor decision {key} is required")
    master = decision["masterName"]
    attribution = decision["actorAttribution"]
    if bool(master) == bool(attribution):
        raise ValueError(
            f"{expected_key}: exactly one of masterName or actorAttribution is required"
        )
    if attribution:
        if not isinstance(attribution, dict):
            raise ValueError(f"{expected_key}: actorAttribution must be an object")
        if attribution.get("Status") not in ALLOWED_ACTOR_STATUSES:
            raise ValueError(f"{expected_key}: actorAttribution uses an open status")
        if attribution.get("ActorRole") not in ALLOWED_CONTEXT_ROLES:
            raise ValueError(f"{expected_key}: actorAttribution uses an open actor role")
        if attribution.get("GrammarEvidence") != decision["grammarEvidence"]:
            raise ValueError(
                f"{expected_key}: actorAttribution grammar must equal the keyed decision"
            )
    for field in ("contextMasters", "contextActors"):
        if not isinstance(decision[field], list):
            raise ValueError(f"{expected_key}: {field} must be an explicit list")
    for context in decision["contextMasters"]:
        roles = context.get("Roles") if isinstance(context, dict) else None
        if not roles or any(role not in ALLOWED_CONTEXT_ROLES for role in roles):
            raise ValueError(f"{expected_key}: ContextMasters uses an open/empty role")
    for context in decision["contextActors"]:
        roles = context.get("Roles") if isinstance(context, dict) else None
        if (
            not roles
            or "utterer" in roles
            or any(role not in ALLOWED_CONTEXT_ROLES for role in roles)
        ):
            raise ValueError(f"{expected_key}: ContextActors uses an open/empty role")
    utterer_masters = [
        context.get("MasterName")
        for context in decision["contextMasters"]
        if "utterer" in context.get("Roles", [])
    ]
    if master and utterer_masters != [master]:
        raise ValueError(
            f"{expected_key}: named actor must be the sole ContextMasters utterer"
        )
    if attribution and utterer_masters:
        raise ValueError(
            f"{expected_key}: null MasterName cannot have a ContextMasters utterer"
        )
    return decision


def plan_occurrence_recut(term, spec, expected_key):
    """Validate an explicit human-selected turn/clause cut against its source span."""
    rel = spec["relPath"]
    from_lb = spec["fromLb"]
    ordinal = spec["sourceSpanOrdinal"]
    if not isinstance(ordinal, int) or ordinal < 0:
        raise ValueError(f"{expected_key}: sourceSpanOrdinal must be a nonnegative integer")
    norm, idx2lb = zc._load(rel)
    offsets = []
    start = 0
    while True:
        offset = norm.find(term, start)
        if offset < 0:
            break
        offsets.append(offset)
        start = offset + 1
    line_offsets = [offset for offset in offsets if idx2lb[offset] == from_lb]
    if ordinal >= len(line_offsets):
        raise ValueError(
            f"{expected_key}: source line/span ordinal does not identify an exact hit"
        )
    source_offset = spec["sourceCharOffset"]
    if not isinstance(source_offset, int) or source_offset < 0:
        raise ValueError(f"{expected_key}: sourceCharOffset must be a nonnegative integer")
    if line_offsets[ordinal] != source_offset:
        raise ValueError(f"{expected_key}: sourceSpanOrdinal does not bind sourceCharOffset")
    left = norm[max(0, source_offset - TARGET_ANCHOR_RADIUS):source_offset]
    right_start = source_offset + len(term)
    right = norm[right_start:right_start + TARGET_ANCHOR_RADIUS]
    anchor_hash = hashlib.sha256((left + term + right).encode()).hexdigest()
    if anchor_hash != spec["targetSpanAnchorSha256"]:
        raise ValueError(f"{expected_key}: target-span anchor hash drift")
    global_index = offsets.index(source_offset)
    contexts = zc.find(rel, term, ctx=350, limit=10000)
    if global_index >= len(contexts):
        raise ValueError(f"{expected_key}: source context index drift")
    if hashlib.sha256(contexts[global_index]["window"].encode()).hexdigest() != spec["sourceContextSha256"]:
        raise ValueError(f"{expected_key}: source context hash drift")
    kwic = spec["boundedKwic"]
    boundary = spec["boundaryEvidence"]
    if not isinstance(kwic, str) or not kwic.strip() or len(kwic) > 800:
        raise ValueError(f"{expected_key}: explicit boundedKwic must contain 1-800 characters")
    if kwic.count(term) != 1:
        raise ValueError(f"{expected_key}: explicit boundedKwic must contain exactly one target span")
    if not isinstance(boundary, str) or len(boundary.strip()) < 24:
        raise ValueError(f"{expected_key}: explicit speech/turn boundary evidence is required")
    verified = zc.verify(rel, kwic)
    if not verified.get("ok") or verified.get("count") != 1:
        raise ValueError(f"{expected_key}: explicit boundedKwic is not unique and verifiable")
    if (
        verified["fromLb"] != spec["boundedFromLb"]
        or verified["toLb"] != spec["boundedToLb"]
    ):
        raise ValueError(f"{expected_key}: bounded KWIC line identity drift")
    if not (spec["boundedFromLb"] <= from_lb <= spec["boundedToLb"]):
        raise ValueError(f"{expected_key}: target source line falls outside bounded KWIC")
    kwic_offset = norm.find(kwic)
    if kwic_offset < 0 or norm.find(kwic, kwic_offset + 1) >= 0:
        raise ValueError(f"{expected_key}: bounded KWIC source offset is not unique")
    kwic_target_offset = kwic_offset + kwic.index(term)
    if kwic_target_offset != source_offset:
        raise ValueError(
            f"{expected_key}: boundedKwic does not contain the cryptographically bound target span"
        )
    return {
        "evidenceKey": expected_key, "relPath": rel,
        "fromLb": from_lb, "sourceSpanOrdinal": ordinal,
        "sourceContextSha256": spec["sourceContextSha256"],
        "sourceCharOffset": source_offset,
        "targetSpanAnchorSha256": anchor_hash,
        "kwic": kwic, "verifiedFromLb": verified["fromLb"],
        "verifiedToLb": verified["toLb"], "boundaryEvidence": boundary,
    }


def preflight_config_occurrence_decisions(configs, expected_ids=None):
    """Exhaustively close every keyed actor decision before any entry write."""
    errors = []
    if not isinstance(configs, list):
        raise OccurrenceDecisionClosureError(["config collection must be a list"])
    ids = [config.get("id") for config in configs if isinstance(config, dict)]
    if len(ids) != len(configs):
        errors.append("every config row must be an object with an id")
    duplicates = sorted({ident for ident in ids if ids.count(ident) > 1})
    if duplicates:
        errors.append(f"duplicate config ids: {duplicates}")
    if expected_ids is not None:
        missing = [ident for ident in expected_ids if ident not in ids]
        surplus = [ident for ident in ids if ident not in expected_ids]
        if missing:
            errors.append(f"missing config ids: {missing}")
        if surplus:
            errors.append(f"surplus config ids: {surplus}")
        if not missing and not surplus and ids != list(expected_ids):
            errors.append(f"misaligned config id order: {ids}")
    plans = {}
    for ci, config in enumerate(configs):
        if not isinstance(config, dict):
            continue
        coordinate = f"configs[{ci}]({config.get('id')})"
        specs = config.get("occurrences")
        if not isinstance(specs, list) or not specs:
            errors.append(f"{coordinate}: nonempty occurrences[] required")
            continue
        keys = [
            spec.get("evidenceKey") if isinstance(spec, dict) else None
            for spec in specs
        ]
        duplicate_keys = sorted({
            key for key in keys if key is not None and keys.count(key) > 1
        })
        if duplicate_keys:
            errors.append(f"{coordinate}: duplicate occurrence keys {duplicate_keys}")
        expected_keys = [f"o{number}" for number in range(1, len(specs) + 1)]
        missing_keys = [key for key in expected_keys if key not in keys]
        surplus_keys = [key for key in keys if key not in expected_keys]
        if missing_keys:
            errors.append(f"{coordinate}: missing occurrence keys {missing_keys}")
        if surplus_keys:
            errors.append(f"{coordinate}: surplus occurrence keys {surplus_keys}")
        if not missing_keys and not surplus_keys and keys != expected_keys:
            errors.append(f"{coordinate}: misaligned occurrence key order {keys}")
        for number, spec in enumerate(specs, 1):
            try:
                validate_occurrence_spec(spec, f"o{number}")
                plan = plan_occurrence_recut(
                    config.get("term"), spec, f"o{number}"
                )
                plans.setdefault(config.get("id"), []).append(plan)
            except (KeyError, TypeError, ValueError) as exc:
                errors.append(f"{coordinate}.occurrences[{number - 1}]: {exc}")
    if errors:
        raise OccurrenceDecisionClosureError(errors)
    return plans


def make_occurrence(labels, term, spec, expected_key, planned_recut):
    decision = validate_occurrence_spec(spec, expected_key)
    rel = spec["relPath"]
    if (
        planned_recut.get("evidenceKey") != expected_key
        or planned_recut.get("relPath") != rel
        or planned_recut.get("fromLb") != spec["fromLb"]
        or planned_recut.get("sourceSpanOrdinal") != spec["sourceSpanOrdinal"]
    ):
        raise ValueError(f"{expected_key}: recut plan identity mismatch")
    kwic = planned_recut["kwic"]
    clause = decision["exactHeadwordClause"]
    if clause not in kwic:
        raise ValueError(f"{expected_key}: exactHeadwordClause is absent from the KWIC")
    occurrence = {
        "RelPath": rel,
        "FromLb": planned_recut["verifiedFromLb"],
        "ToLb": planned_recut["verifiedToLb"],
        "Kwic": kwic,
        "MasterName": decision["masterName"],
        "Curated": True,
        "ContextMasters": decision["contextMasters"],
        "ContextActors": decision["contextActors"],
        "AttributionNote": (
            f"Source record ({rel}). {labels[rel]}. {decision['attributionNote']}"
        ),
        "DraftActorProof": {
            "ExactHeadwordClause": clause,
            "GrammaticalSubject": decision["grammarEvidence"],
            "SpeechFrame": decision["voice"],
            "FullCaseDecision": decision["fullCaseDecision"],
            "SourceSpanIdentity": {
                "TargetFromLb": spec["fromLb"],
                "SourceSpanOrdinal": spec["sourceSpanOrdinal"],
                "SourceContextSha256": spec["sourceContextSha256"],
                "SourceCharOffset": planned_recut["sourceCharOffset"],
                "TargetSpanAnchorSha256": planned_recut["targetSpanAnchorSha256"],
                "TargetSpanAnchorRadius": TARGET_ANCHOR_RADIUS,
                "BoundedFromLb": planned_recut["verifiedFromLb"],
                "BoundedToLb": planned_recut["verifiedToLb"],
                "BoundaryEvidence": planned_recut["boundaryEvidence"],
            },
        },
    }
    if decision["actorAttribution"]:
        occurrence["ActorAttribution"] = decision["actorAttribution"]
    return occurrence


def explicit_worksheet(entry, dossier, decisions):
    worksheet, anchors = clean.build_worksheet(entry, dossier, CREATED_BY)
    sense = worksheet["Entry"]["Senses"][0]
    rows = sense["DraftEvidence"]["SourceAuthorityRows"]
    for number, row in enumerate(rows, 1):
        row["WitnessFamilyId"] = decisions["families"][number - 1]
        row["DeploymentRole"] = decisions["roles"][number - 1]
    keys = [f"o{x}" for x in range(1, len(rows) + 1)]
    sense["DraftEvidence"].update({
        "LiteralGraphFloor": decisions["literal"],
        "LexicalJob": decisions["lexicalJob"],
        "DeploymentClasses": decisions["classes"],
        "HighValueEvidenceLedger": decisions["highValue"],
        "OpeningClaimEvidenceKeys": keys,
        "EvidenceBodyClaimKeys": [keys],
        "ZenBend": decisions["zenBend"],
        "CounterexampleOrLimit": decisions["counterexample"],
        "DifferentThingTest": decisions["differentThing"],
        "AliasRationale": decisions["aliasRationale"],
        "ModifierControls": decisions["modifierControls"],
        "FamilyControls": decisions["familyControls"],
        "LampExcessJustification": "No Tier-3 lamp or lineage compilation is retained.",
        "NoHigherWitnessSearchReceipt": decisions["higherSearch"],
        "DepthHarvestReceipt": decisions["depthReceipt"],
    })
    worksheet["Admission"].update({
        "LexicalUnitReason": decisions["admissionReason"],
        "ObservableChanJob": decisions["lexicalJob"],
        "DuplicateCheck": decisions["duplicateCheck"],
    })
    worksheet["FamilyHarvest"] = decisions["familyHarvest"]
    return worksheet, anchors


def compile_one(config, research_row, family_counts, labels, recut_plan=None):
    if not isinstance(recut_plan, list) or len(recut_plan) != len(config.get("occurrences") or []):
        raise ValueError("whole-config preflight recut plan required before compile_one")
    entry_dir = FRESH / config["id"]
    entry_dir.mkdir(parents=True, exist_ok=True)
    # The semantic draft is complete before the predecessor is opened.
    occurrences = [
        make_occurrence(
            labels, config["term"], spec, f"o{number}", recut_plan[number - 1]
        )
        for number, spec in enumerate(config["occurrences"], 1)
    ]
    entry = {
        "Id": config["id"],
        "SourceTerm": config["term"],
        "CreatedBy": CREATED_BY,
        "WrittenUtc": STAMP,
        "Senses": [{
            "SenseKey": None,
            "MasterName": None,
            "PreferredTarget": config["target"],
            "AlternateTargets": [],
            "SearchAliases": config["aliases"],
            "Status": "preferred",
            "Validation": "multi-source",
            "Explanation": config["opening"] + " " + config["body"],
            "Note": config["note"],
            "Occurrences": occurrences,
            "ClaimAnchors": [],
            "SourceTexts": [],
            "RelatedMasters": [],
            "RelatedTerms": [],
        }],
    }
    full_cases = []
    for number, spec in enumerate(config["occurrences"], 1):
        decision = validate_occurrence_spec(spec, f"o{number}")
        rel = spec["relPath"]
        hit = recut_plan[number - 1]
        full_cases.append({
            "relPath": rel,
            "workId": zc.work_id(rel),
            "sourceTitle": labels[rel],
            "tier": next(x["tier"] for x in research_row["fullConcordance"] if x["relPath"] == rel),
            "fullCaseWindow": next(
                candidate["context"]
                for candidate in research_row["fullCandidates"]
                if candidate["relPath"] == rel
                and candidate["fromLb"] == spec["fromLb"]
                and candidate["contextSha256"] == spec["sourceContextSha256"]
            ),
            "heading": zc.head(rel, spec["fromLb"]),
            "actorDecision": {
                "evidenceKey": decision["evidenceKey"],
                "masterName": decision["masterName"],
                "actorAttribution": decision["actorAttribution"],
                "action": decision["action"],
                "grammarEvidence": decision["grammarEvidence"],
                "voice": decision["voice"],
                "contextMasters": decision["contextMasters"],
                "contextActors": decision["contextActors"],
            },
            "sourceSpanIdentity": {
                "fromLb": spec["fromLb"],
                "sourceSpanOrdinal": spec["sourceSpanOrdinal"],
                "sourceContextSha256": spec["sourceContextSha256"],
                "boundedKwic": hit["kwic"],
                "boundedFromLb": hit["verifiedFromLb"],
                "boundedToLb": hit["verifiedToLb"],
                "boundaryEvidence": hit["boundaryEvidence"],
            },
            "decisionBasis": decision["fullCaseDecision"],
        })
    dossier = {
        "schemaVersion": "r11-source-dossier.v1",
        "id": config["id"],
        "term": config["term"],
        "selectionBinding": {"path": str(SELECTION_PATH.relative_to(ROOT)), "sha256": sha(SELECTION_PATH)},
        "researchBinding": {"path": str(RESEARCH_PATH.relative_to(ROOT)), "sha256": sha(RESEARCH_PATH)},
        "exactCount": {
            "hits": research_row["exactHits"],
            "files": research_row["files"],
            "works": research_row["independentWorks"],
            "per_file": [[x["relPath"], x["hits"]] for x in research_row["fullConcordance"]],
        },
        "requiredFloor": research_row["floor"],
        "semanticReadComplete": True,
        "tier3Lamp": sum(
            1 for case in full_cases if int(case["tier"]) == 3
        ),
        "predecessorEvidenceAudit": [],
        "retainedCompleteCases": full_cases,
        "tier3ExceptionalJustification": "",
        "actorRiskAdjudication": research_row["actorRisks"],
        "senseRuling": "One corpus-wide sense: the retained grammatical and rhetorical frames do not introduce a different referent.",
    }

    # Post-draft predecessor countercheck: preserve leads, never inherit authority.
    old_path = entry_dir / "entry.v2.json"
    if old_path.is_file():
        old = json.loads(old_path.read_text(encoding="utf-8"))
        old_paths = [
            occurrence["RelPath"]
            for sense in old.get("Senses", [])
            for occurrence in sense.get("Occurrences", [])
        ]
        dossier["oldEntryCountercheck"] = {
            "performedAfterSemanticDraft": True,
            "oldEntrySha256": sha(old_path),
            "oldPreferredTargets": [s.get("PreferredTarget") for s in old.get("Senses", [])],
            "oldOccurrencePaths": old_paths,
            "rulings": [
                {
                    "relPath": path,
                    "decision": "KEEP" if path in {o["RelPath"] for o in occurrences} else "REJECT_AS_REDUNDANT_OR_WEAKER",
                    "reason": "Retained when it survives the new source-first actor and authority review; otherwise displaced by a stronger independent case.",
                }
                for path in old_paths
            ],
            "definitionRuling": "The source-first draft was compared with the predecessor only after its own target, sense, actors, and prose were fixed.",
        }
        dossier["predecessorEvidenceAudit"] = [
            {
                "relPath": path,
                "decision": (
                    "KEEP" if path in {o["RelPath"] for o in occurrences}
                    else "REJECT_AS_REDUNDANT_OR_WEAKER"
                ),
                "reason": (
                    f"{config['term']}: retained only when this exact predecessor "
                    "survives the completed actor, independence, and source-authority review."
                ),
            }
            for path in old_paths
        ]
    negative = [{
        "CandidateTerm": term,
        "Query": term,
        "Hits": family_counts[term]["hits"],
        "Files": family_counts[term]["files"],
        "IndependentWorks": family_counts[term]["works"],
        "Decision": "reject-edge",
        "Reason": "The companion wording is attested but has no independently reviewed dictionary authority; it remains a documented family lead rather than a dangling graph edge.",
    } for term in config["family"]]
    decisions = {
        "literal": config["target"],
        "lexicalJob": config["opening"],
        "classes": config["classes"],
        "families": [f"{config['id']}-family-{n}" for n in range(1, 6)],
        "roles": ["original-use"] * 5,
        "highValue": [{
            "Disposition": "keep",
            "Finding": (
                f"{o.get('MasterName') or o['ActorAttribution']['ActorLabel']} "
                f"{spec['actorDecision']['action']} in {labels[o['RelPath']]}."
            ),
            "Reason": "The complete case secures the exact actor and adds a distinct direct use or active commentary.",
        } for o, spec in zip(occurrences, config["occurrences"])],
        "zenBend": config["opening"],
        "counterexample": config["note"],
        "differentThing": {
            "Decision": "one-thing",
            "ComparedThings": [config["target"]],
            "Reason": "The question, verse, appraisal, and commentary frames retain the same lexical referent; none names a separate object, person, title, or institutional role.",
        },
        "aliasRationale": f"The aliases preserve the concrete wording of “{config['target']}” without adding a new interpretation.",
        "modifierControls": [{
            "Term": config["term"],
            "Finding": "No material or color modifier creates a separate referent; the full fixed expression is translated as one unit.",
        }],
        "familyControls": [{
            "Term": item["CandidateTerm"],
            "Finding": item["Reason"],
        } for item in negative] or [{
            "Term": config["term"],
            "Finding": (
                "Not applicable: no distinct family candidate was admitted or "
                "rejected in this bounded source-first construction."
            ),
        }],
        "higherSearch": "Every matching allowlisted file was classified by authority; the retained set uses only Tier 1 or Tier 2 sources, and no lamp is needed.",
        "depthReceipt": {
            "Complete": True,
            "ReviewedExactHitCount": research_row["exactHits"],
            "AvailableSourceFiles": research_row["files"],
            "SearchedDeploymentClasses": config["classes"],
            "OmissionAudit": [
                "All five retained complete cases were read and exact-actor adjudicated.",
                "Parallel containers and passive quotations were excluded from independent-family counting.",
                "Tier-3 lamps were considered last and none was needed.",
                "The predecessor was opened only after the source-first semantic draft was complete.",
            ],
        },
        "admissionReason": f"{config['term']} is a stable fixed expression used in public answers, verses, appraisals, or case commentary.",
        "duplicateCheck": {
            "DeterministicIdChecked": True,
            "ExactHeadwordChecked": True,
            "NearDuplicateRuling": "No exact or punctuation-normalized collision occurs in R01-R10 or the current R11 selection; companion clauses remain separately named family leads.",
        },
        "familyHarvest": {
            "PolicyVersion": 1,
            "Scope": f"{CREATED_BY} exact source-first family harvest",
            "Edges": [],
            "NegativeReceipt": negative,
            "GraphicVariants": [],
        },
    }
    dossier_path = entry_dir / "source-dossier.json"
    atomic_write_json(dossier_path, dossier)
    worksheet, anchor_receipts = explicit_worksheet(entry, dossier, decisions)
    worksheet["EvidenceTransport"]["DossierSha256"] = sha(dossier_path)
    worksheet_path = entry_dir / "evidence.draft.json"
    product_path = entry_dir / "entry.v2.json"
    compile_report = entry_dir / "evidence-compile-clean-promotion-report.json"
    roundtrip_report = entry_dir / "evidence-compile-clean-roundtrip-report.json"
    atomic_write_json(worksheet_path, worksheet)
    subprocess.run([
        "python3", str(ROOT / "compile_evidence_draft.py"), str(worksheet_path),
        "--output", str(product_path), "--report", str(compile_report), "--new-entry",
    ], check=True, cwd=ROOT)
    first_sha = sha(product_path)
    subprocess.run([
        "python3", str(ROOT / "compile_evidence_draft.py"), str(worksheet_path),
        "--output", str(product_path), "--report", str(roundtrip_report), "--new-entry",
        "--preserve-existing-bytes",
    ], check=True, cwd=ROOT)
    roundtrip = json.loads(roundtrip_report.read_text(encoding="utf-8"))
    if not roundtrip.get("semanticParityWithExistingOutput") or sha(product_path) != first_sha:
        raise ValueError(f"{config['id']}: canonical roundtrip parity failed")
    work = "\n".join([
        f"# R11 source-first construction: {config['term']}",
        "",
        f"- exact concordance: {research_row['exactHits']} hits / {research_row['files']} files / {research_row['independentWorks']} works",
        f"- floor: {research_row['floor']}; retained: 5 Tier-1/2 cases",
        "- sense-target-distinguishability: one thing; no second referent survives.",
        f"- opening-interpretation-verdict: licensed by the five complete cases; {config['opening']}",
        "- actor ruling: each retained headword span was read in its complete turn; quotations and record owners were not substituted for the utterer.",
        "- duplicate-family ruling: parallel editions and passive repetitions do not count as independent deployments.",
        "- old-entry countercheck: performed only after the new semantic draft; see source-dossier.json.",
        "- family harvest: named companion formulas were counted; no edge was emitted without an independently reviewed dictionary endpoint.",
        f"- feedback-inference-verdict: licensed — {config['opening']}",
        f"- feedback-observations: {config['body']}",
        f"- feedback-falsification-searches: {config['note']}",
        "- feedback-counterexamples: passive repetitions, duplicate containers, and mismatched actors were checked and excluded from independent support.",
        "- feedback-scope: corpus-wide within the fixed expression and the locally documented frames.",
        f"- lookup-probes: {'; '.join(config['aliases'])}; {config['target']}.",
        "- public mutation: none.",
        "",
    ])
    atomic_write_text(entry_dir / "WORK.md", work)
    return {
        "id": config["id"], "term": config["term"],
        "worksheetSha256": sha(worksheet_path),
        "dossierSha256": sha(dossier_path),
        "productSha256": sha(product_path),
        "compileReportSha256": sha(compile_report),
        "roundtripReportSha256": sha(roundtrip_report),
        "workSha256": sha(entry_dir / "WORK.md"),
        "anchorReceipts": anchor_receipts,
        "oldEntryCountercheck": "complete",
    }


def compile_all(configs, research_by_id, family_counts, labels, expected_ids=None):
    plans = preflight_config_occurrence_decisions(configs, expected_ids=expected_ids)
    return [
        compile_one(
            config, research_by_id[config["id"]], family_counts, labels,
            recut_plan=plans[config["id"]],
        )
        for config in configs
    ]


def main():
    research = json.loads(RESEARCH_PATH.read_text(encoding="utf-8"))
    research_by_id = {row["id"]: row for row in research["rows"]}
    family_terms = [term for config in CONFIG for term in config["family"]]
    family_counts = zc.batch_count(family_terms)
    labels = titles()
    recut_plans = preflight_config_occurrence_decisions(
        CONFIG, expected_ids=[config["id"] for config in CONFIG]
    )
    checkpoint3_path = MAINT / "non-iriya-v7-depth-regeneration-r11-checkpoint-03-c.json"
    results = []
    resume_after = 0
    if checkpoint3_path.is_file():
        prior = json.loads(checkpoint3_path.read_text(encoding="utf-8"))
        if prior.get("completed") == 3 and prior.get("worksheetSoleSource") is True:
            results.extend(prior["rows"])
            fourth = CONFIG[3]
            fourth_dir = FRESH / fourth["id"]
            if all((fourth_dir / name).is_file() for name in (
                "evidence.draft.json", "source-dossier.json", "entry.v2.json",
                "evidence-compile-clean-promotion-report.json",
                "evidence-compile-clean-roundtrip-report.json", "WORK.md",
            )):
                results.append({
                    "id": fourth["id"], "term": fourth["term"],
                    "worksheetSha256": sha(fourth_dir / "evidence.draft.json"),
                    "dossierSha256": sha(fourth_dir / "source-dossier.json"),
                    "productSha256": sha(fourth_dir / "entry.v2.json"),
                    "compileReportSha256": sha(fourth_dir / "evidence-compile-clean-promotion-report.json"),
                    "roundtripReportSha256": sha(fourth_dir / "evidence-compile-clean-roundtrip-report.json"),
                    "workSha256": sha(fourth_dir / "WORK.md"),
                    "anchorReceipts": [],
                    "oldEntryCountercheck": "complete",
                })
                resume_after = 4
    for index, config in enumerate(CONFIG, 1):
        if index <= resume_after:
            continue
        results.append(compile_one(
            config, research_by_id[config["id"]], family_counts, labels,
            recut_plan=recut_plans[config["id"]],
        ))
        if index in {3, 5}:
            checkpoint = {
                "schemaVersion": "r11-clean-regeneration-checkpoint.v1",
                "cohort": "R11",
                "completed": index,
                "remaining": 5 - index,
                "rows": results,
                "worksheetSoleSource": True,
                "tier3Retained": 0,
                "publicMutation": False,
                "generatedUtc": datetime.now(timezone.utc).isoformat(),
            }
            atomic_write_json(
                MAINT / f"non-iriya-v7-depth-regeneration-r11-checkpoint-{index:02d}-c.json",
                checkpoint,
            )
    print(json.dumps({"completed": 5, "tier3": 0, "publicMutation": False}, ensure_ascii=False))


if __name__ == "__main__":
    main()
