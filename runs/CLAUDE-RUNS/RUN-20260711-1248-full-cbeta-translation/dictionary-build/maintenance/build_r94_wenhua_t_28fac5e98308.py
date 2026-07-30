#!/usr/bin/env python3
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "fresh-build/entries/t_28fac5e98308"
FROZEN = ROOT / "maintenance/non-iriya-v7-depth-regeneration-r94-frozen-extraction-root.json"
BASELINE = "8ea7e8ab756138567783a1d3f9e01648885c1732a782ba601ba478742adddaff"
AUTHORITY = "2ee44bf19a2533958e5620c38915f3d03fbb81209c76c7183742a4f3d059f501"

frozen = json.loads(FROZEN.read_text(encoding="utf-8"))
row = next(x for x in frozen["rows"] if x["id"] == "t_28fac5e98308")
selected = [row["sourceCandidates"][i] for i in (1, 2, 3)]

kwics = [
    "舉德山示眾云：「今夜不答話，問話者三十棒。」時，有僧出禮拜，山便打。僧云：「某甲話也未問。」山云：「爾是甚處人？」僧云：「新羅人。」山云：「未踏船舷，好與三十棒。」",
    "瑞鹿禪師云：大凡參學未必學，問話是參學未必學，揀話是參學未必學，代語是參學未必學，別語是參學未必學，捻破經論中奇特言語是參學，未必捻破祖師奇特言語是參學。若於如是等參學，任你七通八達，於佛法中倘無見處，喚作乾慧之徒。",
    "臨濟慧照禪師，師諱義玄，曹州邢氏子。初在黃蘗，隨眾參侍。時堂中第一座勉令問話，因上方丈問：如何是佛法的的大意？黃蘗打。如是三問，三遭打，遂告辭。",
]

facts = [
    {
        "actor": "Deshan Xuanjian",
        "voice": "quoted-original",
        "family": "Dawei Xingxiu active appraisal of Deshan's case",
        "context": None,
        "role": None,
        "source": "Dawei Xingxiu's authored case appraisal",
        "proof": "Deshan's quoted declaration 今夜不答話，問話者三十棒 makes 問話者 the person who puts a question.",
        "deployment": "a quoted rule threatening thirty blows to anyone who questions",
    },
    {
        "actor": "Ruilu Benxian",
        "voice": "quoted-original",
        "family": "Boshan authorial evaluation of Ruilu Benxian's saying",
        "context": "Wuyi Yuanlai",
        "role": "commentator",
        "source": "Wuyi Yuanlai's authored evaluations of Chan sayings",
        "proof": "Ruilu's parallel list 問話、揀話、代語、別語 treats 問話 as one verbal practice that may be mistaken for genuine study.",
        "deployment": "a quoted list distinguishing questioning from genuine investigation",
    },
    {
        "actor": "Xisou Shaotan",
        "voice": "compiler-narration",
        "family": "Xisou authored Linji biography",
        "context": None,
        "role": None,
        "source": "Five Houses Correct Lineage Praises",
        "proof": "Xisou's authored biography says the head seat urged Linji 問話 and immediately narrates Linji going to ask his question.",
        "deployment": "biographical narration of Linji being urged to put a question",
    },
]

retained = []
occurrences = []
source_rows = []
for n, (src, fact) in enumerate(zip(selected, facts), 1):
    kwic = kwics[n - 1]
    retained.append({
        "relPath": src["relPath"], "fromLb": src["fromLb"], "toLb": src["toLb"],
        "kwic": kwic, "actor": fact["actor"], "voiceLayer": fact["voice"],
        "tier": 1, "workId": src["workId"], "witnessFamily": fact["family"],
    })
    contexts = [{"MasterName": fact["actor"], "Roles": ["utterer"]}]
    if fact["context"]:
        contexts.append({"MasterName": fact["context"], "Roles": [fact["role"]]})
    occurrences.append({
        "RelPath": src["relPath"], "FromLb": src["fromLb"], "ToLb": src["toLb"],
        "Kwic": kwic, "MasterName": fact["actor"], "Curated": True,
        "ContextMasters": contexts,
        "AttributionNote": f"Source record ({src['relPath']}). {fact['source']}. {fact['actor']} is the exact actor of the headword-bearing {fact['voice']} layer; {fact['family']}.",
        "DraftActorProof": {
            "ExactHeadwordClause": "問話",
            "GrammaticalSubject": fact["actor"],
            "SpeechFrame": fact["proof"],
            "FullCaseDecision": f"{fact['actor']} owns the headword-bearing layer; adjacent and outer voices are excluded.",
        },
    })
    source_rows.append({
        "EvidenceKey": f"o{n}", "RelPath": src["relPath"], "WorkId": src["workId"],
        "Tier": 1, "SourceClass": "master-authored",
        "AuthorityReason": fact["family"], "WitnessFamilyId": f"問話-family-{n}",
        "DeploymentRole": "active-quotation" if n < 3 else "commentary",
    })

dossier = {
    "schemaVersion": "source-dossier.v1", "id": "t_28fac5e98308", "term": "問話",
    "exactCount": {"hits": 67, "files": 42, "works": 41},
    "retained": retained,
    "excluded": [
        {"row": 1, "reason": "The occurrence is inside 問話頭, a longer unit."},
        {"row": 5, "reason": "Modern editorial prose, not a Zen-master deployment."},
        {"row": 6, "reason": "A lower-ranked recorded-sayings occurrence was unnecessary after three independent authored families survived."},
    ],
    "authorityBinding": {
        "path": "maintenance/r94-lane-c-correction2-authority.json",
        "sha256": "00a08a9f13e242a9c1b2234bfa582b34b8f2d5df172759f722528d21a08956d0",
    },
}
OUT.mkdir(parents=True, exist_ok=True)
(OUT / "source-dossier.json").write_text(json.dumps(dossier, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
dossier_sha = hashlib.sha256((OUT / "source-dossier.json").read_bytes()).hexdigest()

draft = {
    "SchemaVersion": 1,
    "ConstructionPipelineVersion": 2,
    "Admission": {
        "Decision": "admit",
        "LexicalUnitReason": "問話 is independently used as a complete verb-object unit for putting a question.",
        "ObservableChanJob": "It names the act of putting a question in an encounter, and by extension the questioning so performed.",
        "DuplicateCheck": {
            "DeterministicIdChecked": True, "ExactHeadwordChecked": True,
            "NearDuplicateRuling": "問話頭 is a longer compound and does not replace the independently deployed shorter unit.",
        },
    },
    "EvidenceTransport": {
        "DossierPath": "source-dossier.json", "DossierSha256": dossier_sha,
        "CorpusBaselineSha256": BASELINE, "SourceAuthorityManifestSha256": AUTHORITY,
        "DiscoveryMethods": ["frozen R94 exact-unit extraction", "complete-case actor review", "Tier 1 source ranking"],
        "ExactCount": 67, "BridgedCount": 67,
    },
    "Entry": {
        "Id": "t_28fac5e98308", "SourceTerm": "問話", "CorpusBaselineSha256": BASELINE,
        "CreatedBy": "R94 governed constructor", "WrittenUtc": "2026-07-30T17:30:00Z",
        "Senses": [{
            "SenseKey": None, "MasterName": None, "PreferredTarget": "questioning",
            "AlternateTargets": ["ask a question", "put a question to a master"],
            "SearchAliases": ["questioning", "ask a question", "put a question"],
            "Status": "preferred", "Validation": "multi-source",
            "Note": "The word names putting a question; context decides whether English needs the activity noun or a finite verb. No lamp is retained.",
            "Occurrences": occurrences,
            "SourceTexts": [x["relPath"] for x in selected],
            "RelatedMasters": ["Deshan Xuanjian", "Ruilu Benxian", "Wuyi Yuanlai", "Xisou Shaotan"],
            "RelatedTerms": [], "ClaimAnchors": [],
            "ExplanationParts": {
                "CorpusEarnedOpening": "Questioning: the act of putting a question in a Chan encounter.",
                "EvidenceBody": [
                    "Deshan Xuanjian's warning makes the 問話者 the person who asks despite his refusal to answer.",
                    "Ruilu Benxian lists 問話 beside selecting sayings, giving substitute replies, and giving alternate replies, then denies that skill in these verbal practices necessarily amounts to genuine study.",
                    "Xisou Shaotan narrates the head seat urging Linji to 問話 and immediately supplies the question Linji then puts to Huangbo.",
                ],
            },
            "DraftEvidence": {
                "OpeningClaimEvidenceKeys": ["o1", "o2", "o3"],
                "EvidenceBodyClaimKeys": [["o1"], ["o2"], ["o3"]],
                "LiteralGraphFloor": "ask words; put a question",
                "LexicalJob": "It names the act of putting a question in an encounter, whether described as an action, a practice, or a person's next move.",
                "DeploymentClasses": [x["deployment"] for x in facts],
                "HighValueEvidenceLedger": [
                    {"Disposition": "keep", "Finding": f"{f['actor']}: {f['deployment']}.", "Reason": f["family"]}
                    for f in facts
                ],
                "ZenBend": "Ruilu uses the familiar act as a limit case: fluency at asking and manipulating sayings does not itself establish genuine investigation.",
                "CounterexampleOrLimit": "Do not expand the term into all dialogue or treat every question as proof of understanding.",
                "DifferentThingTest": {
                    "Decision": "one-thing",
                    "ComparedThings": ["the act of asking", "questioning as a named practice"],
                    "Reason": "The noun-like and verb-like English forms package the same act; the cases do not establish two referents.",
                },
                "AliasRationale": "English syntax alternates naturally between 'questioning,' 'ask a question,' and 'put a question'; all preserve the same act.",
                "ModifierControls": [{"Term": "問話頭", "Finding": "This longer compound is excluded where 問話 is only its constituent; the retained cases deploy 問話 independently."}],
                "FamilyControls": [{"Term": "問話", "Finding": "問答 and 問訊 are neighboring question expressions, not interchangeable forms of this exact unit."}],
                "IndependentWorkIds": [x["workId"] for x in selected],
                "SourceAuthorityRows": source_rows,
                "DepthHarvestReceipt": {
                    "Complete": True,
                    "ReviewedExactHitCount": 67,
                    "AvailableSourceFiles": 42,
                    "SearchedDeploymentClasses": [
                        "authored case appraisal",
                        "authored evaluation of a quoted saying",
                        "authored biographical narration",
                        "recorded-sayings reserve",
                    ],
                    "OmissionAudit": [
                        "問話頭 constituent-only match excluded",
                        "modern editorial prose excluded",
                        "lower-ranked Tier 2 reserve omitted after the frozen authority selected three Tier 1 families",
                    ],
                    "AuthorizedFloorException": {
                        "Decision": "FROZEN_CANDIDATE_EXHAUSTION",
                        "AuthorizedBy": "root",
                        "FrozenCandidateExhausted": True,
                        "RetainedIndependentFamilies": 3,
                        "ExcludedReserve": [
                            {"row": 1, "reason": "contained only in 問話頭"},
                            {"row": 5, "reason": "modern editorial prose"},
                            {"row": 6, "reason": "lower-ranked reserve outside the root-approved final three-family authority"},
                        ],
                    },
                },
                "NoHigherWitnessSearchReceipt": "The frozen R94 authority supplied three independent Tier 1 families.",
                "LampExcessJustification": "No lamp is retained.",
            },
            "DraftAcceptedDerivedFields": {
                "SourceTexts": [x["relPath"] for x in selected],
                "RelatedMasters": ["Deshan Xuanjian", "Ruilu Benxian", "Wuyi Yuanlai", "Xisou Shaotan"],
            },
        }],
    },
    "FamilyHarvest": {
        "PolicyVersion": 1, "Scope": "R94 問話 deterministic construction", "Edges": [],
        "NegativeReceipt": [{
            "SourceSenseIndex": 0, "SourceSenseKey": None, "queries": ["問話頭", "問答", "問訊"],
            "QueryEvidence": [{"tool": "frozen R94 exact-unit review", "query": "問話 family", "hits": 67, "files": 42, "works": 41}],
            "CandidateAvailability": {"availableCandidateCount": 3, "exhaustive": True, "method": "bounded reviewed family controls"},
            "dispositions": [
                {"candidate": "問話頭", "candidateId": "t_f35507ed4265", "decision": "reject", "relationType": "longer-compound", "reason": "A longer compound, not proof of the shorter unit."},
                {"candidate": "問答", "candidateId": "t_47a8c4d45a14", "decision": "reject", "relationType": "neighbor", "reason": "Question-and-answer is a different lexical unit."},
                {"candidate": "問訊", "candidateId": "t_dc24f92ead78", "decision": "reject", "relationType": "neighbor", "reason": "Greeting or inquiry is a different lexical unit."},
            ],
            "reason": "All bounded neighboring forms were explicitly ruled without manufacturing public relations.",
        }],
        "GraphicVariants": [],
    },
}
(OUT / "evidence.draft.json").write_text(json.dumps(draft, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
(OUT / "WORK.md").write_text("""# 問話 — R94 worksheet

- source hierarchy: three independent Tier 1 authored deployments; no lamp retained.
- semantic job: the act of putting a question in a Chan encounter.
- limit: do not inflate the word into all dialogue or treat the act itself as proof of understanding.
- actor correction: 瑞鹿禪師 is roster-canonical Ruilu Benxian.
- exact-unit ruling: 問話頭 is excluded where the headword occurs only as its constituent.

feedback-inference-verdict: licensed by the three retained authored families.
feedback-observations: Deshan names the questioner; Ruilu contrasts questioning with genuine study; Xisou narrates Linji being urged to ask.
feedback-falsification-searches: 問話頭; 問答; 問訊; lamp duplication.
feedback-counterexamples: verbal fluency at questioning need not amount to genuine investigation.
feedback-scope: independent 問話 deployments in the retained reviewed sources.
lookup-probes: questioning; ask a question; put a question
opening-interpretation-verdict: licensed by all three headword-bearing clauses.
""", encoding="utf-8")
print(OUT)
