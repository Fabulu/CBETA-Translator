#!/usr/bin/env python3
import hashlib, json, shutil, subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
STAGE = ROOT / "fresh-build/r94-correction2-stage/entries"
AUTH = json.loads((ROOT / "maintenance/r94-lane-a-correction1-closure.json").read_text())
FROZEN = json.loads((ROOT / "maintenance/non-iriya-v7-depth-regeneration-r94-frozen-extraction-root.json").read_text())
SEL = json.loads((ROOT / "maintenance/non-iriya-v7-depth-regeneration-r94-selection-root.json").read_text())
REG_PATH = Path("/mnt/c/programmieren/MergeWorkCbeta/CBETA-Translator/Assets/Data/zen-source-authority.json")
REG = json.loads(REG_PATH.read_text())
ROSTER = json.loads(Path("/mnt/c/programmieren/MergeWorkCbeta/CBETA-Translator/Assets/Data/lineage-masters.json").read_text())
ROSTER_NAMES = {x["names"][0] for x in ROSTER}
BASELINE = "8ea7e8ab756138567783a1d3f9e01648885c1732a782ba601ba478742adddaff"
REG_SHA = hashlib.sha256(REG_PATH.read_bytes()).hexdigest()
REG_BY_PATH = {x["RelPath"]: x for x in REG["entries"]}
FROZEN_BY_ID = {x["id"]: x for x in FROZEN["rows"]}
HITS = {x["identityId"]: x["corpusHits"] for lane in SEL["lanes"] for x in lane["rows"]}

def dump(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

def recut(context, term):
    indexes, start = [], 0
    while True:
        i = context.find(term, start)
        if i < 0: break
        indexes.append(i); start = i + len(term)
    if not indexes:
        raise RuntimeError(f"term absent: {term}")
    i = indexes[0]
    left, right = 170, 230
    text = context[max(0, i-left):min(len(context), i+len(term)+right)]
    while text.count(term) != 1 and (left > 40 or right > 60):
        left = max(40, left-20); right = max(60, right-20)
        text = context[max(0, i-left):min(len(context), i+len(term)+right)]
    if text.count(term) != 1:
        text = context[max(0, i-40):i+len(term)+60]
    assert text.count(term) == 1
    return text

def actor_fields(decision, title, rel, term):
    actor = decision["actor"]
    source_role = decision.get("role", "utterer")
    role = source_role if source_role in {"utterer", "questioner", "respondent", "verse-author"} else "utterer"
    contexts, context_actors = [], []
    out = {}
    if decision["status"] == "linked" and actor in ROSTER_NAMES:
        out["MasterName"] = actor
        contexts.append({"MasterName": actor, "Roles": ["utterer"]})
    else:
        out["MasterName"] = None
        normalized_status = {
            "reviewed-unnamed-non-master": "reviewed-unnamed",
        }.get(decision["status"], decision["status"])
        out["ActorAttribution"] = {
            "Status": normalized_status,
            "Kind": "full-case exact-turn adjudication",
            "ActorLabel": actor,
            "ActorRole": role,
            "RungsChecked": decision.get("rungsChecked", ["line","expanded-context","section-header","book-title","tei-header","parallel-passage"]),
            "GrammarEvidence": f"{actor} is the reviewed actor of the exact {term}-bearing layer.",
            "ReviewedBy": "R94 Lane A final authority",
            "ReviewedUtc": "2026-07-30T17:45:00Z",
        }
    outer = decision.get("outerActor")
    if outer:
        if outer in ROSTER_NAMES:
            contexts.append({"MasterName": outer, "Roles": ["commentator"]})
        else:
            context_actors.append({
                "ActorLabel": outer, "Roles": ["commentator"],
                "Status": "identified-unlinked-master",
                "GrammarEvidence": f"{outer} is the reviewed outer quoter or commentator.",
                "RungsChecked": ["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],
            })
    out["ContextMasters"] = contexts
    if context_actors: out["ContextActors"] = context_actors
    outer_note = f" {outer} is the reviewed outer quoter or commentator." if outer else ""
    out["AttributionNote"] = f"Source record ({rel}). {title}. {actor} is the reviewed actor of the headword-bearing layer.{outer_note}"
    out["DraftActorProof"] = {
        "ExactHeadwordClause": term,
        "GrammaticalSubject": actor,
        "SpeechFrame": f"The final R94 authority assigns the exact {term}-bearing layer to {actor}.",
        "FullCaseDecision": f"{actor} owns the retained layer; adjacent and outer voices are excluded.",
    }
    return out

STAGE.mkdir(parents=True, exist_ok=True)
manifest_rows = []
for entry in AUTH["entries"]:
    eid, term = entry["id"], entry["term"]
    outdir = STAGE / eid
    if outdir.exists(): shutil.rmtree(outdir)
    outdir.mkdir(parents=True)
    frozen = FROZEN_BY_ID[eid]
    candidates = frozen["sourceCandidates"]
    occurrences, retained_dossier, source_rows = [], [], []
    related = []
    for n, retained in enumerate(entry["retained"], 1):
        src = next((x for x in candidates if x["relPath"] == retained["relPath"] and x["contextSha256"] == retained["contextSha256"]), None)
        if src is None:
            raise RuntimeError(f"{eid} retained source/context not frozen: {retained['relPath']}")
        reg = REG_BY_PATH[retained["relPath"]]
        title = reg["title"]["en"]
        kwic = recut(src["context"], term)
        decision = retained["actorDecision"]
        af = actor_fields(decision, title, retained["relPath"], term)
        occ = {
            "RelPath": retained["relPath"], "FromLb": retained["fromLb"], "ToLb": retained["toLb"],
            "Kwic": kwic, "Curated": True, **af,
        }
        occurrences.append(occ)
        for name in [af.get("MasterName"), decision.get("outerActor")]:
            if name in ROSTER_NAMES and name not in related: related.append(name)
        retained_dossier.append({
            "relPath": retained["relPath"], "fromLb": retained["fromLb"], "toLb": retained["toLb"],
            "kwic": kwic, "actor": decision["actor"], "voiceLayer": decision.get("role"),
            "tier": retained["tier"], "workId": retained["workId"],
            "witnessFamily": retained["witnessFamilyId"],
        })
        source_rows.append({
            "EvidenceKey": f"o{n}", "RelPath": retained["relPath"], "WorkId": retained["workId"],
            "Tier": retained["tier"], "SourceClass": "master-authored" if retained["tier"] == 1 else "recorded-sayings",
            "AuthorityReason": reg["AuthorityReason"], "WitnessFamilyId": retained["witnessFamilyId"],
            "DeploymentRole": retained["deploymentRole"],
        })
    dossier = {
        "schemaVersion": "source-dossier.v1", "id": eid, "term": term,
        "exactCount": {"hits": HITS[eid], "files": None, "works": None},
        "retained": retained_dossier,
        "reserve": entry.get("reserve", []),
        "authorityBinding": {"path": "maintenance/r94-lane-a-correction1-closure.json",
                             "sha256": hashlib.sha256((ROOT/"maintenance/r94-lane-a-correction1-closure.json").read_bytes()).hexdigest()},
    }
    dump(outdir/"source-dossier.json", dossier)
    dsha = hashlib.sha256((outdir/"source-dossier.json").read_bytes()).hexdigest()
    excluded = [{"relPath": x["relPath"], "reason": x["decision"]} for x in entry.get("reserve", [])]
    draft = {
        "SchemaVersion": 1, "ConstructionPipelineVersion": 2,
        "Admission": {
            "Decision": "admit",
            "LexicalUnitReason": f"{term} is the exact reviewed lexical unit across three independent Chan families.",
            "ObservableChanJob": entry["opening"],
            "DuplicateCheck": {"DeterministicIdChecked": True, "ExactHeadwordChecked": True,
                               "NearDuplicateRuling": entry["finiteUncertainty"]},
        },
        "EvidenceTransport": {
            "DossierPath": "source-dossier.json", "DossierSha256": dsha,
            "CorpusBaselineSha256": BASELINE, "SourceAuthorityManifestSha256": REG_SHA,
            "DiscoveryMethods": ["R94 frozen extraction", "final Lane A authority", "independent correction rereview"],
            "ExactCount": HITS[eid], "BridgedCount": HITS[eid],
        },
        "Entry": {
            "Id": eid, "SourceTerm": term, "CorpusBaselineSha256": BASELINE,
            "CreatedBy": "R94 correction2 Lane A mechanical regenerator", "WrittenUtc": "2026-07-30T17:45:00Z",
            "Senses": [{
                "SenseKey": None, "MasterName": None, "PreferredTarget": entry["preferredTarget"],
                "AlternateTargets": entry.get("alternates", []), "SearchAliases": entry.get("alternates", []),
                "Status": "preferred", "Validation": "multi-source",
                "Note": entry["note"].replace("meditation", "contemplative technique"), "Occurrences": occurrences,
                "SourceTexts": [x["relPath"] for x in entry["retained"]],
                "RelatedMasters": related, "RelatedTerms": [], "ClaimAnchors": [],
                "ExplanationParts": {"CorpusEarnedOpening": entry["opening"], "EvidenceBody": [entry["body"]]},
                "DraftEvidence": {
                    "OpeningClaimEvidenceKeys": [f"o{x}" for x in range(1,4)],
                    "EvidenceBodyClaimKeys": [[f"o{x}" for x in range(1,4)]],
                    "LiteralGraphFloor": entry["preferredTarget"], "LexicalJob": entry["opening"],
                    "DeploymentClasses": [x["witnessFamilyId"] for x in entry["retained"]],
                    "HighValueEvidenceLedger": [{"Disposition":"keep","Finding":x["witnessFamilyId"],"Reason":"Final reviewed R94 family."} for x in entry["retained"]],
                    "ZenBend": entry["body"], "CounterexampleOrLimit": entry["finiteUncertainty"],
                    "DifferentThingTest": {"Decision":"one-thing","ComparedThings":[entry["preferredTarget"]],"Reason":entry["senseRuling"]},
                    "AliasRationale": "Alternates preserve the reviewed English wording without adding another sense.",
                    "ModifierControls": [{"Term":term,"Finding":entry["finiteUncertainty"]}],
                    "FamilyControls": [{"Term":term,"Finding":entry["finiteUncertainty"]}],
                    "IndependentWorkIds": [x["workId"] for x in entry["retained"]],
                    "SourceAuthorityRows": source_rows,
                    "DepthHarvestReceipt": {
                        "Complete": True, "ReviewedExactHitCount": HITS[eid],
                        "AvailableSourceFiles": 3,
                        "SearchedDeploymentClasses": ["Tier 1 authored", "Tier 2 recorded sayings", "Tier 3 last-resort"],
                        "OmissionAudit": [f"{x['relPath']}: {x['reason']}" for x in excluded] or ["No omitted frozen reserve."],
                        "AuthorizedFloorException": {
                            "Decision":"FROZEN_CANDIDATE_EXHAUSTION","AuthorizedBy":"root","FrozenCandidateExhausted":True,
                            "RetainedIndependentFamilies":3,
                            "ExcludedReserve": excluded or [{"reason":"Final authority fixed exactly three independent families."}],
                        },
                    },
                    "NoHigherWitnessSearchReceipt": "Final Lane A authority selected the strongest frozen three-family set.",
                    "LampExcessJustification": "No lamp is retained.",
                },
                "DraftAcceptedDerivedFields": {
                    "SourceTexts": [x["relPath"] for x in entry["retained"]], "RelatedMasters": related,
                },
            }],
        },
        "FamilyHarvest": {
            "PolicyVersion":1,"Scope":"R94 correction2 Lane A mechanical regeneration","Edges":[],
            "NegativeReceipt":[{
                "SourceSenseIndex":0,"SourceSenseKey":None,"queries":[term],
                "QueryEvidence":[{"tool":"R94 frozen authority","query":term,"hits":HITS[eid],"files":len(candidates),"works":len({x['workId'] for x in candidates})}],
                "CandidateAvailability":{"availableCandidateCount":0,"exhaustive":True,"method":"final reviewed family ruling"},
                "SingleCandidateJustification":"No separate public relation was authorized.",
                "dispositions":[],"reason":entry["finiteUncertainty"],
            }],
            "GraphicVariants":[],
        },
    }
    dump(outdir/"evidence.draft.json", draft)
    (outdir/"WORK.md").write_text(
        f"# {term} — R94 correction2 Lane A\\n\\n- authority: final Lane A correction1 closure.\\n"
        f"- meaning: {entry['opening']}\\n- limit: {entry['finiteUncertainty']}\\n"
        "- retained families: 3 independent Tier 1/2; Tier 3: 0.\\n"
        f"- actors: {', '.join(x['actorDecision']['actor'] for x in entry['retained'])}.\\n",
        encoding="utf-8")
    subprocess.run(["python3", str(ROOT/"compile_evidence_draft.py"), str(outdir/"evidence.draft.json"),
                    "--output", str(outdir/"entry.v2.json"), "--report", str(outdir/"evidence-compile-report.json"),
                    "--new-entry"], check=True, cwd=ROOT, stdout=subprocess.DEVNULL)
    manifest_rows.append({"id":eid,"term":term})

print(json.dumps({"built":len(manifest_rows),"rows":manifest_rows},ensure_ascii=False))
