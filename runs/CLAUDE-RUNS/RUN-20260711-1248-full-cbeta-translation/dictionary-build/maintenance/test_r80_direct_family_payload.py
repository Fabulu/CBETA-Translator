#!/usr/bin/env python3
import copy
import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from compile_evidence_draft import compile_draft
from maintenance.r80_direct_family_spec import BODY, USES
from maintenance.source_authority_binding import authority_registry_sha256


ROOT = Path(__file__).resolve().parent.parent
TERM = "雞向五更啼"


class R80DirectFamilyPayloadTest(unittest.TestCase):
    def test_direct_family_spec_has_no_yunju_quotation_actors(self):
        actors = [row[1] for row in USES]
        self.assertEqual(
            actors,
            ["Miyin Zhenchuan", "Hengshan Dengbing", "Shanyi Ruchun", "Huanxi Weiyi"],
        )
        self.assertNotIn("Qianyan Yuanzhang", actors)
        self.assertNotIn("Dahui Zonggao", actors)
        self.assertEqual(4, len({row[2] for row in USES}))

    def test_revised_payload_passes_real_pipeline_compiler(self):
        config = json.loads(
            (ROOT / "maintenance/non-iriya-v7-depth-regeneration-r79-constructor-config-b.json")
            .read_text(encoding="utf-8")
        )
        extraction = json.loads(
            (ROOT / "maintenance/non-iriya-v7-depth-regeneration-r79-extraction-output-b.json")
            .read_text(encoding="utf-8")
        )
        payload = copy.deepcopy(config["entries"][1]["evidenceDraft"])
        candidates = {
            row["relPath"]: row
            for row in extraction["rows"][1]["sourceCandidates"]
        }
        occurrences = []
        authority = []
        paths = []
        masters = []
        for index, (rel, master, family, grammar) in enumerate(USES, 1):
            candidate = candidates[rel]
            context = candidate["context"]
            offset = context.index(TERM)
            kwic = context[max(0, offset - 150):offset + len(TERM) + 150]
            note = f"Source record ({rel}). {grammar}"
            occurrences.append({
                "RelPath": rel,
                "FromLb": candidate["fromLb"],
                "ToLb": candidate["toLb"],
                "Kwic": kwic,
                "MasterName": master,
                "Curated": True,
                "ContextMasters": [{"MasterName": master, "Roles": ["utterer"]}],
                "ContextActors": [],
                "AttributionNote": note,
                "DraftActorProof": {
                    "ExactHeadwordClause": TERM,
                    "GrammaticalSubject": master,
                    "SpeechFrame": note,
                    "FullCaseDecision": (
                        f"{master} is the exact actor at the headword-bearing clause."
                    ),
                },
            })
            authority.append({
                "EvidenceKey": f"o{index}",
                "RelPath": rel,
                "WorkId": candidate["workId"],
                "Tier": 2,
                "SourceClass": "recorded-sayings",
                "AuthorityReason": "A named master's complete recorded-sayings turn.",
                "WitnessFamilyId": family,
                "DeploymentRole": "original-use",
            })
            paths.append(rel)
            masters.append(master)
        sense = payload["Entry"]["Senses"][0]
        sense["Occurrences"] = occurrences
        sense["SourceTexts"] = paths
        sense["RelatedMasters"] = masters
        sense["DraftAcceptedDerivedFields"] = {
            "SourceTexts": paths, "RelatedMasters": masters
        }
        sense["ExplanationParts"]["EvidenceBody"] = [BODY]
        sense["Explanation"] = sense["ExplanationParts"]["CorpusEarnedOpening"] + " " + BODY
        sense["DraftEvidence"]["SourceAuthorityRows"] = authority
        sense["DraftEvidence"]["IndependentWorkIds"] = [
            row["WorkId"] for row in authority
        ]
        payload["EvidenceTransport"]["SourceAuthorityManifestSha256"] = (
            authority_registry_sha256(ROOT)
        )
        with tempfile.TemporaryDirectory() as temporary:
            dossier = Path(temporary) / "source-dossier.json"
            dossier.write_text(
                json.dumps(
                    config["entries"][1]["sourceDossier"],
                    ensure_ascii=False,
                    indent=2,
                ) + "\n",
                encoding="utf-8",
            )
            payload["EvidenceTransport"]["DossierSha256"] = hashlib.sha256(
                dossier.read_bytes()
            ).hexdigest()
            worksheet = Path(temporary) / "evidence.draft.json"
            worksheet.write_text(
                json.dumps(payload, ensure_ascii=False, indent=2) + "\n",
                encoding="utf-8",
            )
            _, errors = compile_draft(
                payload, require_pipeline_v2=True, worksheet_path=worksheet
            )
        self.assertEqual([], errors)


if __name__ == "__main__":
    unittest.main()
