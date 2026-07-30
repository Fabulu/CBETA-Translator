#!/usr/bin/env python3
import copy
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

from atomic_write import atomic_write_json
from maintenance.generic_30_rebuild_pipeline import sha, verify_bundle
from maintenance.generic_bounded_constructor import run, validate_batch_plan

ROOT = Path(__file__).resolve().parent.parent
FIXTURE = "t_1c2e34e1abb7"


def plan(ids):
    return {
        "mode": "thirty-entry-rebuild",
        "expectedEntryCount": 30,
        "lanes": [
            {
                "lane": lane + 1,
                "ids": ids[lane * 10:(lane + 1) * 10],
                "semanticAuthor": f"author-{lane + 1}",
                "crossReviewer": f"reviewer-{(lane + 1) % 3 + 1}",
            }
            for lane in range(3)
        ],
    }


def minimal_entries():
    return [
        {
            "id": f"t_batch_{number:02d}",
            "term": f"batch-{number:02d}",
            "sourceDossier": {
                "id": f"t_batch_{number:02d}",
                "term": f"batch-{number:02d}",
            },
            "evidenceDraft": {
                "Entry": {
                    "Id": f"t_batch_{number:02d}",
                    "SourceTerm": f"batch-{number:02d}",
                },
                "EvidenceTransport": {},
            },
        }
        for number in range(1, 31)
    ]


def write_doc(path, value):
    atomic_write_json(path, value)
    return {"path": str(path), "sha256": sha(path)}


class BundleFixture:
    def __init__(self, base):
        self.base = base
        self.ids = [f"t_bundle_{number:02d}" for number in range(1, 31)]
        self.plan = plan(self.ids)
        self.bindings = {}
        self.bindings["artifactZero"] = write_doc(base / "zero.json", {
            "artifactZero": True, "entryCount": 30,
        })
        rows = [{"id": identity} for identity in self.ids]
        self.bindings["selection"] = write_doc(
            base / "selection.json", {"rows": rows})
        self.bindings["extraction"] = write_doc(
            base / "extraction.json", {"rows": rows})
        self.bindings["constructionPreclosure"] = write_doc(
            base / "preclosure.json",
            {"hardPass": True, "ids": self.ids},
        )
        manifest_rows = [
            {
                "id": identity,
                "dossierSha256": f"d{number:063d}",
                "worksheetSha256": f"w{number:063d}",
                "productSha256": f"p{number:063d}",
            }
            for number, identity in enumerate(self.ids, 1)
        ]
        product_hashes = {
            row["id"]: row["productSha256"] for row in manifest_rows
        }
        self.product_hashes = product_hashes
        self.bindings["constructionManifest"] = write_doc(
            base / "manifest.json",
            {"rows": manifest_rows, "batchPlan": self.plan},
        )
        lane_closures = [
            {
                "lane": lane["lane"],
                "ids": lane["ids"],
                "productSha256s": [
                    product_hashes[identity] for identity in lane["ids"]
                ],
                "semanticAuthor": lane["semanticAuthor"],
                "crossReviewer": lane["crossReviewer"],
            }
            for lane in self.plan["lanes"]
        ]
        self.bindings["constructionClosure"] = write_doc(
            base / "closure.json",
            {
                "hardPass": True,
                "manifestSha256":
                    self.bindings["constructionManifest"]["sha256"],
                "preclosureSha256":
                    self.bindings["constructionPreclosure"]["sha256"],
                "ids": self.ids,
                "productCount": 30,
                "batchPlan": self.plan,
                "laneClosures": lane_closures,
            },
        )
        reviews = []
        by_id = {row["id"]: row for row in manifest_rows}
        for lane in self.plan["lanes"]:
            reviews.append(write_doc(
                base / f"review-{lane['lane']}.json",
                {
                    "verdict": "PASS",
                    "crossReviewer": lane["crossReviewer"],
                    "rows": [
                        {
                            "id": identity,
                            "verdict": "PASS",
                            "dossierSha256":
                                by_id[identity]["dossierSha256"],
                            "worksheetSha256":
                                by_id[identity]["worksheetSha256"],
                            "productSha256":
                                by_id[identity]["productSha256"],
                        }
                        for identity in lane["ids"]
                    ],
                },
            ))
        self.bindings["semanticLaneReviews"] = reviews
        publication = write_doc(base / "publication.json", {
            "products": [
                {"id": identity, "entrySha256": product_hashes[identity]}
                for identity in self.ids
            ],
        })
        self.bindings["publicationManifest"] = publication
        stage = write_doc(base / "stage.json", {
            "hardPass": True,
            "manifestSha256": publication["sha256"],
        })
        self.bindings["publicationStageReceipt"] = stage
        install = write_doc(base / "install.json", {
            "hardPass": True,
            "manifestSha256": publication["sha256"],
            "stageReceiptSha256": stage["sha256"],
            "products": product_hashes,
        })
        self.bindings["publicationInstallReceipt"] = install
        release = write_doc(base / "release.json", {
            "hardPass": True,
            "products": product_hashes,
            "publicationManifestSha256": publication["sha256"],
            "stageReceiptSha256": stage["sha256"],
            "installReceiptSha256": install["sha256"],
            "constructionClosureSha256":
                self.bindings["constructionClosure"]["sha256"],
        })
        self.bindings["releaseReceipt"] = release
        predecessor = write_doc(
            base / "prior.json", {"uniqueIds": ["t_prior"]})
        result = write_doc(
            base / "result.json", {"uniqueIds": ["t_prior"] + self.ids})
        self.bindings["predecessorUnion"] = predecessor
        self.bindings["resultUnion"] = result
        ledger = write_doc(base / "ledger.json", {
            "publishedIds": self.ids,
            "entryCount": 30,
            "productSha256s": product_hashes,
            "releaseReceiptSha256": release["sha256"],
            "installReceiptSha256": install["sha256"],
            "publicationManifestSha256": publication["sha256"],
            "constructionClosureSha256":
                self.bindings["constructionClosure"]["sha256"],
            "predecessorUnionSha256": predecessor["sha256"],
            "resultUnionSha256": result["sha256"],
        })
        self.bindings["ledger"] = ledger
        self.bundle = base / "bundle.json"
        self.rewrite_bundle()

    def rewrite_bundle(self):
        atomic_write_json(self.bundle, {
            "schemaVersion": "generic-thirty-rebuild-bundle.v1",
            "buildRoot": str(self.base),
            "orderedIds": self.ids,
            "batchPlan": self.plan,
            **self.bindings,
            "rosterMutation": False,
        })

    def replace_binding(self, key, value):
        path = Path(self.bindings[key]["path"])
        self.bindings[key] = write_doc(path, value)
        self.rewrite_bundle()


class GenericThirtyRebuildTest(unittest.TestCase):
    def test_exact_ordered_thirty_ids_partition_three_lanes(self):
        entries = minimal_entries()
        ids = [row["id"] for row in entries]
        config = {"entries": entries, "batchPlan": plan(ids)}
        validate_batch_plan(config)
        with self.assertRaisesRegex(ValueError, "ordinary bounded config"):
            validate_batch_plan({"entries": entries[:4]})
        with self.assertRaisesRegex(ValueError, "exact batchPlan and 30"):
            validate_batch_plan({
                "entries": entries[:29],
                "batchPlan": plan(ids),
            })
        drift = copy.deepcopy(config)
        drift["batchPlan"]["lanes"][2]["ids"][9] = ids[0]
        with self.assertRaisesRegex(
            ValueError, "exactly partition the 30 ordered"
        ):
            validate_batch_plan(drift)

    def test_invalid_late_entry_fails_before_any_write(self):
        source = ROOT / "fresh-build/entries" / FIXTURE
        original_worksheet = json.loads(
            (source / "evidence.draft.json").read_text())
        original_dossier = json.loads(
            (source / "source-dossier.json").read_text())
        entries = []
        for number in range(1, 31):
            identity = f"t_late_{number:02d}"
            term = f"late-{number:02d}"
            worksheet = copy.deepcopy(original_worksheet)
            dossier = copy.deepcopy(original_dossier)
            worksheet["Entry"]["Id"] = identity
            worksheet["Entry"]["SourceTerm"] = term
            worksheet["Admission"]["DuplicateCheck"]["NearDuplicateRuling"] = (
                "No exact or punctuation-normalized collision is admitted."
            )
            dossier["id"] = identity
            dossier["term"] = term
            entries.append({
                "id": identity,
                "term": term,
                "sourceDossier": dossier,
                "evidenceDraft": worksheet,
            })
        entries[-1]["sourceDossier"].pop("requiredFloor", None)
        config = {
            "startedEpoch": 0,
            "entries": entries,
            "batchPlan": plan([row["id"] for row in entries]),
        }
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            base = Path(raw)
            output = base / "output"
            config_path = base / "config.json"
            atomic_write_json(config_path, config)
            with (
                patch(
                    "maintenance.generic_bounded_constructor.verify_authority",
                    return_value={"outputRoot": output},
                ),
                patch(
                    "maintenance.generic_bounded_constructor.verify_actor_closure"
                ),
            ):
                with self.assertRaisesRegex(
                    ValueError, "requiredFloor must be a positive integer"
                ):
                    run(config_path, base)
            self.assertFalse(
                output.exists(),
                "one invalid 30th entry must prevent every output write",
            )

    def test_exact_thirty_product_manifest_and_closure(self):
        entries = minimal_entries()
        ids = [row["id"] for row in entries]
        config = {
            "cohort": "BATCH30",
            "startedEpoch": 0,
            "entries": entries,
            "batchPlan": plan(ids),
        }
        validate_batch_plan(config)
        projections = {
            identity: {"Id": identity, "SourceTerm": f"batch-{index:02d}"}
            for index, identity in enumerate(ids, 1)
        }
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            base = Path(raw)
            output = base / "output"
            paths = {
                "outputRoot": output,
                "firstProductReceipt": base / "first.json",
                "preclosure": base / "preclosure.json",
                "manifest": base / "manifest.json",
                "closure": base / "closure.json",
                "governedDeadlines": {
                    "firstProduct": 100,
                    "construction": 200,
                },
            }
            config_path = base / "config.json"
            atomic_write_json(config_path, config)

            def compile_product(command, **kwargs):
                output_path = Path(command[command.index("--output") + 1])
                report_path = Path(command[command.index("--report") + 1])
                identity = output_path.parent.name
                atomic_write_json(output_path, projections[identity])
                atomic_write_json(report_path, {"hardPass": True})

            with (
                patch(
                    "maintenance.generic_bounded_constructor.verify_authority",
                    return_value=paths,
                ),
                patch(
                    "maintenance.generic_bounded_constructor.verify_actor_closure"
                ),
                patch(
                    "maintenance.generic_bounded_constructor."
                    "verify_whole_config_preclosure"
                ),
                patch(
                    "maintenance.generic_bounded_constructor."
                    "canonical_compile_prewrite",
                    return_value=projections,
                ),
                patch(
                    "maintenance.generic_bounded_constructor.subprocess.run",
                    side_effect=compile_product,
                ),
                patch(
                    "maintenance.generic_bounded_constructor.load_preclosure_row",
                    return_value={},
                ),
                patch(
                    "maintenance.generic_bounded_constructor.validate_preclosure",
                    return_value=[],
                ),
            ):
                result = run(config_path, base, now=lambda: 1)
            manifest = json.loads((base / "manifest.json").read_text())
            closure = json.loads((base / "closure.json").read_text())
            self.assertEqual(30, result["completed"])
            self.assertEqual(ids, [row["id"] for row in manifest["rows"]])
            self.assertEqual(config["batchPlan"], manifest["batchPlan"])
            self.assertEqual(ids, closure["ids"])
            self.assertEqual(30, closure["productCount"])
            self.assertEqual(config["batchPlan"], closure["batchPlan"])
            self.assertEqual(
                [10, 10, 10],
                [len(lane["productSha256s"]) for lane in closure["laneClosures"]],
            )
            self.assertTrue(closure["hardPass"])

    def test_release_bundle_exact_chain_passes(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            fixture = BundleFixture(Path(raw))
            self.assertEqual(30, verify_bundle(fixture.bundle)["productCount"])

    def test_substituted_construction_closure_is_rejected(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            fixture = BundleFixture(Path(raw))
            closure = json.loads(
                Path(fixture.bindings["constructionClosure"]["path"]).read_text())
            closure["manifestSha256"] = "0" * 64
            fixture.replace_binding("constructionClosure", closure)
            with self.assertRaisesRegex(ValueError, "construction closure"):
                verify_bundle(fixture.bundle)

    def test_stale_same_id_lane_review_is_rejected(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            fixture = BundleFixture(Path(raw))
            binding = fixture.bindings["semanticLaneReviews"][2]
            path = Path(binding["path"])
            review = json.loads(path.read_text())
            review["rows"][-1]["productSha256"] = "0" * 64
            fixture.bindings["semanticLaneReviews"][2] = write_doc(path, review)
            fixture.rewrite_bundle()
            with self.assertRaisesRegex(ValueError, "cross-review"):
                verify_bundle(fixture.bundle)

    def test_foreign_release_receipt_is_rejected(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            fixture = BundleFixture(Path(raw))
            release = json.loads(
                Path(fixture.bindings["releaseReceipt"]["path"]).read_text())
            release["installReceiptSha256"] = "f" * 64
            fixture.replace_binding("releaseReceipt", release)
            with self.assertRaisesRegex(ValueError, "release receipt"):
                verify_bundle(fixture.bundle)

    def test_unbound_same_id_ledger_is_rejected(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            fixture = BundleFixture(Path(raw))
            ledger = json.loads(
                Path(fixture.bindings["ledger"]["path"]).read_text())
            ledger["resultUnionSha256"] = "e" * 64
            fixture.replace_binding("ledger", ledger)
            with self.assertRaisesRegex(ValueError, "ledger"):
                verify_bundle(fixture.bundle)


if __name__ == "__main__":
    unittest.main()
