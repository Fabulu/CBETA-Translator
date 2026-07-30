#!/usr/bin/env python3
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import maintenance.generic_manifest_publisher as publisher_module
from maintenance.generic_manifest_publisher import (
    ManifestPublisher, canonical_entry_sha, sha, write,
)

ROOT = Path(__file__).resolve().parent.parent


class PublisherFixture:
    def __init__(self, base: Path, modes: list[str]):
        self.base = base
        self.terms = base / "terms"
        self.public = base / "public"
        self.stage = base / "stage"
        self.terms.mkdir()
        self.public.mkdir()
        self.products = []
        baseline = []
        for number, mode in enumerate(modes, 1):
            identity = f"t_fixture_{number}"
            old = self.entry(identity, f"term-{number}", f"old-{number}")
            new = self.entry(identity, f"term-{number}", f"new-{number}")
            if mode == "replace":
                target = self.terms / identity
                target.mkdir()
                write(target / "entry.v2.json", old)
                (target / "WORK.md").write_text("old\n", encoding="utf-8")
                (target / "STATUS").write_text("done\n", encoding="utf-8")
                baseline.append(old)
                baseline_sha = sha(target / "entry.v2.json")
            else:
                baseline_sha = None
            source = base / "sources" / identity
            source.mkdir(parents=True)
            write(source / "entry.v2.json", new)
            write(source / "evidence.draft.json", {"id": identity})
            (source / "WORK.md").write_text("new\n", encoding="utf-8")
            self.products.append({
                "id": identity,
                "sourceDir": str(source.relative_to(base)),
                "entrySha256": sha(source / "entry.v2.json"),
                "worksheetSha256": sha(source / "evidence.draft.json"),
                "workSha256": sha(source / "WORK.md"),
                "termsMode": mode,
                "termsBaselineSha256": baseline_sha,
            })
        # Include one unaffected published entry to prove preservation.
        baseline.append(self.entry("t_unaffected", "unaffected", "keep"))
        self.write_public(baseline)
        roster = base / "roster.json"
        write(roster, {"rows": []})
        review = base / "review.json"
        write(review, {
            "cohort": "FIXTURE",
            "verdict": "PASS",
            "boundProducts": [
                {"id": row["id"], "sha256": row["entrySha256"]}
                for row in self.products
            ],
        })
        node_script = base / "merge.js"
        node_script.write_text("// fixture\n", encoding="utf-8")
        authority = base / "authority.json"
        write(authority, {
            "cohort": "FIXTURE",
            "releaseAuthorized": True,
            "products": [
                {"id": row["id"], "entrySha256": row["entrySha256"]}
                for row in self.products
            ],
        })
        self.manifest = base / "manifest.json"
        write(self.manifest, {
            "schemaVersion": "generic-dictionary-publication-manifest.v1",
            "cohort": "FIXTURE",
            "buildRoot": ".",
            "termsRoot": "terms",
            "publicRoot": str(self.public),
            "stageRoot": "stage",
            "installReceipt": "install-receipt.json",
            "authority": {
                "path": "authority.json", "sha256": sha(authority),
            },
            "reviews": [{
                "path": "review.json", "sha256": sha(review),
            }],
            "products": self.products,
            "roster": {"path": "roster.json", "sha256": sha(roster)},
            "node": {
                "windowsNodeExe": "node.exe",
                "script": {
                    "path": "merge.js", "sha256": sha(node_script),
                },
                "cwd": str(base),
                "status": "fixture-ready",
            },
        })

    @staticmethod
    def entry(identity, term, gloss):
        return {
            "Id": identity,
            "SourceTerm": term,
            "Senses": [{
                "SenseKey": None,
                "PreferredTarget": gloss,
                "AlternateTargets": [],
                "Status": "preferred",
                "Validation": "multi-source",
                "Note": "",
                "Occurrences": [],
            }],
        }

    def write_public(self, entries):
        entries = sorted(entries, key=lambda row: row["SourceTerm"])
        write(self.public / "termbase.v2.json", {
            "SchemaVersion": 2, "Entries": entries,
        })
        write(self.public / "termbase.json", [
            {
                "SourceTerm": row["SourceTerm"],
                "PreferredTarget": row["Senses"][0]["PreferredTarget"],
            } for row in entries
        ])
        write(self.public / "termbase.index.json", {
            "SchemaVersion": 2,
            "Terms": [
                [row["SourceTerm"], row["Senses"][0]["PreferredTarget"]]
                for row in entries
            ],
            "Aliases": {},
        })
        write(self.public / "termbase/000.json", {
            "SchemaVersion": 2, "Entries": entries,
        })

    def fake_node(self, command, cwd):
        values = {
            item.split("=", 1)[0]: item.split("=", 1)[1]
            for item in command if item.startswith("--") and "=" in item
        }
        terms = Path(values["--terms-dir"])
        output = Path(values["--out"])
        rich = json.loads(
            (output / "termbase.v2.json").read_text(encoding="utf-8"))
        by_id = {row["Id"]: row for row in rich["Entries"]}
        for path in terms.glob("*/entry.v2.json"):
            entry = json.loads(path.read_text(encoding="utf-8"))
            by_id[entry["Id"]] = entry
        live_public = self.public
        try:
            self.public = output
            self.write_public(list(by_id.values()))
        finally:
            self.public = live_public


class GenericManifestPublisherTest(unittest.TestCase):
    def publisher(self, fixture, failure_hook=None):
        publisher = ManifestPublisher(
            fixture.manifest,
            allowed_root=fixture.base,
            node_runner=fixture.fake_node,
            failure_hook=failure_hook,
        )
        publisher._windows_path = lambda path: str(path)
        return publisher

    def test_three_replacements_stage_and_install(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            fixture = PublisherFixture(Path(raw), ["replace"] * 3)
            publisher = self.publisher(fixture)
            publisher.stage()
            result = publisher.install()
            self.assertTrue(Path(result["receipt"]).is_file())
            for row in fixture.products:
                self.assertEqual(
                    row["entrySha256"],
                    sha(fixture.terms / row["id"] / "entry.v2.json"),
                )
            rich = json.loads(
                (fixture.public / "termbase.v2.json").read_text())
            self.assertIn("t_unaffected", {row["Id"] for row in rich["Entries"]})

    def test_maintenance_manifest_spans_build_tree_and_roster(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            fixture = PublisherFixture(Path(raw), ["replace"] * 3)
            nested = fixture.base / "dictionary-build/maintenance/manifest.json"
            nested.parent.mkdir(parents=True)
            manifest = json.loads(fixture.manifest.read_text())
            manifest["buildRoot"] = str(fixture.base)
            write(nested, manifest)
            fixture.manifest = nested
            state = self.publisher(fixture).validate()
            self.assertEqual(fixture.base / "roster.json", state["roster"])

    def test_relative_path_manifest_compatibility(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            fixture = PublisherFixture(Path(raw), ["replace"] * 3)
            self.assertEqual(
                fixture.terms,
                self.publisher(fixture).validate()["termsRoot"],
            )

    def test_parent_escape_is_rejected(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            fixture = PublisherFixture(Path(raw), ["replace"] * 3)
            build = fixture.base / "build"
            build.mkdir()
            manifest = json.loads(fixture.manifest.read_text())
            manifest["buildRoot"] = "build"
            manifest["termsRoot"] = "../terms"
            write(fixture.manifest, manifest)
            with self.assertRaises(ValueError):
                self.publisher(fixture)._path("../terms")

    def test_absolute_outside_allowed_root_is_rejected(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            fixture = PublisherFixture(Path(raw), ["replace"] * 3)
            manifest = json.loads(fixture.manifest.read_text())
            manifest["publicRoot"] = "/tmp/generic-publisher-outside"
            write(fixture.manifest, manifest)
            with self.assertRaises(ValueError):
                self.publisher(fixture).validate()

    def test_symlink_escape_is_rejected(self):
        with (
            tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw,
            tempfile.TemporaryDirectory() as outside_raw,
        ):
            fixture = PublisherFixture(Path(raw), ["replace"] * 3)
            link = fixture.base / "escape-link"
            link.symlink_to(Path(outside_raw), target_is_directory=True)
            manifest = json.loads(fixture.manifest.read_text())
            manifest["products"][0]["sourceDir"] = "escape-link"
            write(fixture.manifest, manifest)
            with self.assertRaises(ValueError):
                self.publisher(fixture).validate()

    def test_absolute_node_cwd_outside_allowed_root_is_rejected(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            fixture = PublisherFixture(Path(raw), ["replace"] * 3)
            manifest = json.loads(fixture.manifest.read_text())
            manifest["node"]["cwd"] = "/tmp"
            write(fixture.manifest, manifest)
            with self.assertRaises(ValueError):
                self.publisher(fixture).validate()

    def test_absolute_node_cwd_inside_allowed_sibling_is_accepted(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            fixture = PublisherFixture(Path(raw), ["replace"] * 3)
            build = fixture.base / "build"
            sibling = fixture.base / "node-sibling"
            build.mkdir()
            sibling.mkdir()
            manifest = json.loads(fixture.manifest.read_text())
            manifest["buildRoot"] = "build"
            write(fixture.manifest, manifest)
            publisher = self.publisher(fixture)
            self.assertEqual(sibling, publisher._path(str(sibling)))

    def test_symlink_node_cwd_escape_is_rejected(self):
        with (
            tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw,
            tempfile.TemporaryDirectory() as outside_raw,
        ):
            fixture = PublisherFixture(Path(raw), ["replace"] * 3)
            link = fixture.base / "node-cwd-link"
            link.symlink_to(Path(outside_raw), target_is_directory=True)
            manifest = json.loads(fixture.manifest.read_text())
            manifest["node"]["cwd"] = "node-cwd-link"
            write(fixture.manifest, manifest)
            with self.assertRaises(ValueError):
                self.publisher(fixture).validate()

    def test_mixed_replace_create_stage_and_install(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            fixture = PublisherFixture(
                Path(raw), ["replace", "create", "replace"])
            publisher = self.publisher(fixture)
            publisher.stage()
            publisher.install()
            self.assertTrue((fixture.terms / "t_fixture_2" / "STATUS").is_file())
            self.assertEqual(
                {row["id"] for row in fixture.products},
                {
                    row["Id"] for row in json.loads(
                        (fixture.public / "termbase.v2.json").read_text()
                    )["Entries"] if row["Id"].startswith("t_fixture_")
                },
            )

    def test_baseline_drift_fails_before_write(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            fixture = PublisherFixture(Path(raw), ["replace"] * 3)
            target = fixture.terms / "t_fixture_1" / "entry.v2.json"
            before_public = sha(fixture.public / "termbase.v2.json")
            write(target, {"drift": True})
            with self.assertRaisesRegex(ValueError, "terms baseline drift"):
                self.publisher(fixture).stage()
            self.assertFalse(fixture.stage.exists())
            self.assertEqual(
                before_public, sha(fixture.public / "termbase.v2.json"))

    def test_unauthorized_product_fails_before_write(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            fixture = PublisherFixture(Path(raw), ["replace"] * 3)
            authority_path = fixture.base / "authority.json"
            authority = json.loads(authority_path.read_text(encoding="utf-8"))
            authority["products"] = authority["products"][:2]
            write(authority_path, authority)
            manifest = json.loads(fixture.manifest.read_text(encoding="utf-8"))
            manifest["authority"]["sha256"] = sha(authority_path)
            write(fixture.manifest, manifest)
            with self.assertRaisesRegex(
                PermissionError, "authority product set differs"
            ):
                self.publisher(fixture).stage()
            self.assertFalse(fixture.stage.exists())

    def test_install_failure_rolls_back_terms_and_public(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            fixture = PublisherFixture(
                Path(raw), ["replace", "create", "replace"])
            publisher = self.publisher(fixture)
            publisher.stage()
            terms_before = {
                row["id"]: (
                    sha(fixture.terms / row["id"] / "entry.v2.json")
                    if row["termsMode"] == "replace" else None
                ) for row in fixture.products
            }
            public_before = sha(fixture.public / "termbase.v2.json")

            def fail(phase):
                if phase.startswith("public:"):
                    raise RuntimeError("injected install failure")

            publisher.failure_hook = fail
            with self.assertRaisesRegex(RuntimeError, "injected"):
                publisher.install()
            self.assertEqual(
                public_before, sha(fixture.public / "termbase.v2.json"))
            for row in fixture.products:
                target = fixture.terms / row["id"]
                if row["termsMode"] == "replace":
                    self.assertEqual(
                        terms_before[row["id"]],
                        sha(target / "entry.v2.json"),
                    )
                else:
                    self.assertFalse(target.exists())

    def test_foreign_or_stale_pass_review_fails_before_write(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            fixture = PublisherFixture(Path(raw), ["replace"] * 3)
            review_path = fixture.base / "review.json"
            review = json.loads(review_path.read_text())
            review["cohort"] = "FOREIGN"
            write(review_path, review)
            manifest = json.loads(fixture.manifest.read_text())
            manifest["reviews"][0]["sha256"] = sha(review_path)
            write(fixture.manifest, manifest)
            with self.assertRaisesRegex(
                PermissionError, "exact product set"
            ):
                self.publisher(fixture).stage()
            self.assertFalse(fixture.stage.exists())

    def test_low_level_partial_term_move_is_rollback_tracked(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            fixture = PublisherFixture(Path(raw), ["replace"] * 3)
            publisher = self.publisher(fixture)
            publisher.stage()
            before = {
                row["id"]: sha(
                    fixture.terms / row["id"] / "entry.v2.json")
                for row in fixture.products
            }
            real_replace = publisher_module.os.replace

            def fail_partial(source, target):
                if (
                    Path(source).name == "WORK.md"
                    and "prepared-terms" in str(source)
                ):
                    raise OSError("injected partial multi-move failure")
                return real_replace(source, target)

            with patch(
                "maintenance.generic_manifest_publisher.os.replace",
                side_effect=fail_partial,
            ):
                with self.assertRaisesRegex(OSError, "partial multi-move"):
                    publisher.install()
            self.assertEqual(
                before,
                {
                    row["id"]: sha(
                        fixture.terms / row["id"] / "entry.v2.json")
                    for row in fixture.products
                },
            )

    def test_equal_count_shard_or_index_divergence_is_rejected(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            fixture = PublisherFixture(Path(raw), ["replace"] * 3)

            def corrupt_graph(command, cwd):
                fixture.fake_node(command, cwd)
                values = {
                    item.split("=", 1)[0]: item.split("=", 1)[1]
                    for item in command
                    if item.startswith("--") and "=" in item
                }
                shard = Path(values["--out"]) / "termbase/000.json"
                document = json.loads(shard.read_text())
                document["Entries"][0]["SourceTerm"] = "same-count-drift"
                write(shard, document)

            publisher = ManifestPublisher(
                fixture.manifest,
                allowed_root=fixture.base,
                node_runner=corrupt_graph)
            publisher._windows_path = lambda path: str(path)
            with self.assertRaisesRegex(ValueError, "count parity failed"):
                publisher.stage()
            self.assertFalse(fixture.stage.exists())

    def test_roster_drift_is_restored_during_rollback(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            fixture = PublisherFixture(Path(raw), ["replace"] * 3)
            roster = fixture.base / "roster.json"
            roster_before = roster.read_bytes()
            publisher = self.publisher(fixture)
            publisher.stage()

            def mutate_roster(phase):
                if phase == "before-receipt":
                    write(roster, {"mutated": True})

            publisher.failure_hook = mutate_roster
            with self.assertRaisesRegex(RuntimeError, "lineage roster changed"):
                publisher.install()
            self.assertEqual(roster_before, roster.read_bytes())

    def test_receipt_write_failure_rolls_back_everything(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            fixture = PublisherFixture(
                Path(raw), ["replace", "create", "replace"])
            publisher = self.publisher(fixture)
            publisher.stage()
            public_before = sha(fixture.public / "termbase.v2.json")
            original_write = publisher_module.write

            def fail_receipt(path, value):
                if Path(path).name == "install-receipt.json":
                    raise OSError("injected receipt write failure")
                return original_write(path, value)

            with patch(
                "maintenance.generic_manifest_publisher.write",
                side_effect=fail_receipt,
            ):
                with self.assertRaisesRegex(OSError, "receipt write"):
                    publisher.install()
            self.assertEqual(
                public_before, sha(fixture.public / "termbase.v2.json"))
            self.assertFalse(fixture.base.joinpath("install-receipt.json").exists())
            self.assertFalse(fixture.terms.joinpath("t_fixture_2").exists())


if __name__ == "__main__":
    unittest.main()
