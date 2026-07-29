#!/usr/bin/env python3

import hashlib
import json
import tempfile
import unittest
from pathlib import Path
from unittest import mock

import stage_authorized_closures as staging


class AuthorizedReplacementStagingTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.fresh = self.root / "fresh"
        self.fresh.mkdir()
        self.predecessor_entry = {
            "Id": "t_existing",
            "SourceTerm": "舊語",
            "Senses": [{"PreferredTarget": "old"}],
        }
        self.predecessor = self.root / "termbase.v2.json"
        self.write_json(self.predecessor, {
            "SchemaVersion": 2, "Entries": [self.predecessor_entry]
        })
        self.product = self.fresh / "t_existing" / "entry.v2.json"
        self.product.parent.mkdir()
        self.write_json(self.product, {
            "Id": "t_existing",
            "SourceTerm": "舊語",
            "Senses": [{"PreferredTarget": "repaired"}],
        })

    def tearDown(self):
        self.temp.cleanup()

    @staticmethod
    def write_json(path, value):
        path.write_text(
            json.dumps(value, ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8",
        )

    @staticmethod
    def file_sha(path):
        return hashlib.sha256(path.read_bytes()).hexdigest()

    def replacement(self):
        return {
            "hardPass": True,
            "releaseAuthorized": True,
            "closureKind": "replacement-of-existing-stable-ids",
            "predecessorAggregate": {
                "path": str(self.predecessor),
                "sha256": self.file_sha(self.predecessor),
                "entryCount": 1,
            },
            "replacementCount": 1,
            "postReplacementEntryCount": 1,
            "exclusions": {"ids": [], "sourceTerms": []},
            "entries": [{
                "id": "t_existing",
                "sourceTerm": "舊語",
                "predecessor": {
                    "id": "t_existing",
                    "sourceTerm": "舊語",
                    "objectSha256": staging.object_sha(self.predecessor_entry),
                },
                "entrySha256": self.file_sha(self.product),
            }],
        }

    def write_closure(self, name, value):
        path = self.root / name
        self.write_json(path, value)
        return path

    def test_replacement_stages_exact_authorized_product(self):
        closure = self.write_closure("replacement.json", self.replacement())
        out = self.root / "out"
        with mock.patch.object(staging, "FRESH", self.fresh):
            self.assertEqual(0, staging.stage([closure], out))
        staged = out / "t_existing" / "entry.v2.json"
        self.assertEqual(self.product.read_bytes(), staged.read_bytes())
        self.assertEqual("done\n", (staged.parent / "STATUS").read_text())

    def test_stale_predecessor_aggregate_is_rejected(self):
        value = self.replacement()
        value["predecessorAggregate"]["sha256"] = "0" * 64
        closure = self.write_closure("stale.json", value)
        with mock.patch.object(staging, "FRESH", self.fresh):
            with self.assertRaisesRegex(SystemExit, "stale predecessor aggregate"):
                staging.stage([closure], self.root / "out-stale")

    def test_excluded_replacement_is_rejected(self):
        value = self.replacement()
        value["exclusions"]["ids"] = ["t_existing"]
        closure = self.write_closure("excluded.json", value)
        with mock.patch.object(staging, "FRESH", self.fresh):
            with self.assertRaisesRegex(SystemExit, "intersects exclusion"):
                staging.stage([closure], self.root / "out-excluded")

    def test_changed_post_replacement_count_is_rejected(self):
        value = self.replacement()
        value["postReplacementEntryCount"] = 2
        closure = self.write_closure("count.json", value)
        with mock.patch.object(staging, "FRESH", self.fresh):
            with self.assertRaisesRegex(SystemExit, "replacement count"):
                staging.stage([closure], self.root / "out-count")

    def test_legacy_novel_add_closure_still_stages(self):
        novel = self.fresh / "t_novel" / "entry.v2.json"
        novel.parent.mkdir()
        self.write_json(novel, {"Id": "t_novel", "SourceTerm": "新語", "Senses": []})
        closure = self.write_closure("novel.json", {
            "hardPass": True,
            "releaseAuthorized": True,
            "entries": [{
                "id": "t_novel",
                "sourceTerm": "新語",
                "entrySha256": self.file_sha(novel),
            }],
        })
        out = self.root / "out-novel"
        with mock.patch.object(staging, "FRESH", self.fresh):
            self.assertEqual(0, staging.stage([closure], out))
        self.assertEqual(novel.read_bytes(), (out / "t_novel" / "entry.v2.json").read_bytes())

    def test_superseded_sealed_closure_rejects_predecessor_object_drift(self):
        closure = (
            Path(__file__).resolve().parent
            / "maintenance/non-iriya-v7-depth-regeneration-r01a-authorized3-replacement-closure-a.json"
        )
        self.assertEqual(
            "50c8763f9062e8efdbdb9d83027f9b500582d0a172a82205dd1f5ba4b1305a49",
            self.file_sha(closure),
        )
        value = json.loads(closure.read_text(encoding="utf-8"))
        out = self.root / "sealed-canary"
        # The live aggregate legitimately advances after a closure is sealed.
        # Isolate this schema/product canary from aggregate staleness while
        # retaining the real row-level predecessor-object and product guards.
        real_sha = staging.sha
        sealed_predecessor = Path(value["publicPredecessor"]["path"])
        with mock.patch.object(
            staging,
            "sha",
            side_effect=lambda path: (
                value["publicPredecessor"]["sha256"]
                if Path(path) == sealed_predecessor
                else real_sha(path)
            ),
        ):
            with self.assertRaisesRegex(
                SystemExit, "predecessor object hash drift"
            ):
                staging.stage([closure], out)

    def test_exact_r04_final_manifests_binding_canary_stages_two(self):
        predecessor_entries = [
            {
                "Id": "t_r04_one",
                "SourceTerm": "慶無不宜",
                "Senses": [{"PreferredTarget": "sealed predecessor one"}],
            },
            {
                "Id": "t_r04_two",
                "SourceTerm": "北禪分歲",
                "Senses": [{"PreferredTarget": "sealed predecessor two"}],
            },
        ]
        sealed_predecessor = self.root / "r04-termbase.v2.json"
        self.write_json(
            sealed_predecessor,
            {"SchemaVersion": 2, "Entries": predecessor_entries},
        )
        products = []
        for entry_id, source_term, target in [
            ("t_r04_one", "慶無不宜", "sealed replacement one"),
            ("t_r04_two", "北禪分歲", "sealed replacement two"),
        ]:
            product = self.fresh / entry_id / "entry.v2.json"
            product.parent.mkdir()
            self.write_json(
                product,
                {
                    "Id": entry_id,
                    "SourceTerm": source_term,
                    "Senses": [{"PreferredTarget": target}],
                },
            )
            products.append(product)
        value = {
            "schemaVersion": "non-iriya-v7-depth-regeneration-replacement-closure.v1",
            "cohort": "R04-sealed-canary",
            "closureKind": "replacement-of-existing-stable-ids",
            "hardPass": True,
            "releaseAuthorized": True,
            "publicPredecessor": {
                "path": str(sealed_predecessor),
                "sha256": self.file_sha(sealed_predecessor),
                "entryCount": 2,
            },
            "rows": [
                {
                    "id": predecessor["Id"],
                    "term": predecessor["SourceTerm"],
                    "operation": "REPLACE_EXISTING",
                    "publicPredecessorCanonicalObjectSha256": staging.object_sha(
                        predecessor
                    ),
                    "product": {
                        "path": str(product),
                        "sha256": self.file_sha(product),
                    },
                    "verdict": "AUTHORIZE",
                }
                for predecessor, product in zip(predecessor_entries, products)
            ],
            "explicitExclusions": [],
            "verification": {
                "authorizedRows": 2,
                "authorizedProductHashesMatchFinalManifests": 2,
                "stableIdsExistExactlyOnceInPublicPredecessor": 2,
                "sourceTermsMatchPublicPredecessor": 2,
            },
        }
        closure = self.write_closure("r04-sealed-canary.json", value)
        self.assertNotIn(
            "authorizedProductHashesMatchCorrectionManifest",
            value["verification"],
        )
        self.assertEqual(
            len(value["rows"]),
            value["verification"]["authorizedProductHashesMatchFinalManifests"],
        )
        out = self.root / "r04-sealed-canary"
        with mock.patch.object(staging, "FRESH", self.fresh):
            self.assertEqual(0, staging.stage([closure], out))
        staged = sorted(path.name for path in out.iterdir() if path.is_dir())
        self.assertEqual(sorted(row["id"] for row in value["rows"]), staged)
        self.assertEqual(2, len(staged))
        for row in value["rows"]:
            source = Path(row["product"]["path"])
            target = out / row["id"] / "entry.v2.json"
            self.assertEqual(source.read_bytes(), target.read_bytes())


if __name__ == "__main__":
    unittest.main()
