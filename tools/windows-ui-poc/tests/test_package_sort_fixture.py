"""Read-only independent fixture checks; corrupt expectations, never source files."""
import argparse
import copy
import hashlib
import json
import sqlite3
import unittest
import uuid
from pathlib import Path

NAMES = [f"uabs-smoke-{i:04d}.bundle" for i in range(3)]
ASC = [(NAMES[1], "Alpha"), (NAMES[2], "Alpha"), (NAMES[0], "Zeta")]
DESC = [(NAMES[0], "Zeta"), (NAMES[1], "Alpha"), (NAMES[2], "Alpha")]


def digest(path):
    with Path(path).open("rb") as stream:
        result = hashlib.sha256()
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            result.update(block)
        return result.hexdigest()


def validate(repo, config):
    assert config["acceptance"] == "checkpoint21-package-sort" and config["keyword"] == "asset"
    assert [(r["name"], r["package"]) for r in config["expectedRows"]] == ASC
    fixtures = config["packageSortFixtures"]
    assert len(fixtures) == 2 and {f["package"] for f in fixtures} == {"Alpha", "Zeta"}
    assert len({str(Path(f["path"]).resolve()) for f in fixtures}) == 2
    assert config["package"] == next(f["path"] for f in fixtures if f["package"] == "Alpha")
    assert config["packageSha256"] == digest(config["package"])
    root = repo / "test-fixtures/unity/collections/smoke"
    manifest = json.loads((root / "fixture-manifest.json").read_text(encoding="utf-8"))
    assert manifest["profile"] == "smoke" and manifest["seed"] == 20211031
    inputs = {b["name"]: b for b in manifest["bundles"]}
    assert sorted(inputs) == NAMES
    all_ids = []
    observed = []
    for fixture in fixtures:
        path = Path(fixture["path"])
        assert path.is_absolute() and str(path.resolve()) == str(path) and not path.is_symlink()
        assert path.name == fixture["package"] + ".uabs.sqlite" and digest(path) == fixture["sha256"]
        allocation = NAMES[1:] if fixture["package"] == "Alpha" else NAMES[:1]
        assert sorted(s["name"] for s in fixture["sources"]) == allocation
        with sqlite3.connect(path.as_uri() + "?mode=ro", uri=True) as sql:
            row = sql.execute("SELECT display_name,package_uuid,generation_uuid,index_status FROM package").fetchone()
            assert row[0] == fixture["package"] and row[3] == "complete"
            for value, blob in zip((fixture["packageId"], fixture["generationId"]), row[1:3]):
                assert uuid.UUID(value) == uuid.UUID(bytes=blob) and uuid.UUID(value).int != 0
                all_ids.append(uuid.UUID(value))
            assert sql.execute("SELECT count(*) FROM indexing_issue").fetchone()[0] == 0
            sources = sql.execute("SELECT source_id,original_name,original_path,kind,index_status,content_sha256,source_uuid,source_revision_uuid,index_directory FROM source").fetchall()
            assert len(sources) == len(allocation)
            for source in fixture["sources"]:
                matches = [r for r in sources if r[1] == source["name"]]
                assert len(matches) == 1
                r = matches[0]
                original = (root / "bundles" / source["name"]).resolve()
                assert Path(source["originalPath"]) == original and Path(r[2]) == original
                assert not original.is_symlink() and r[3:5] == ("bundle", "complete") and r[8] == ""
                assert source["sha256"] == inputs[source["name"]]["sha256"] == digest(original) == bytes(r[5]).hex()
                assert original.stat().st_size == inputs[source["name"]]["sizeBytes"]
                for value, blob in zip((source["sourceId"], source["sourceRevision"]), r[6:8]):
                    assert uuid.UUID(value) == uuid.UUID(bytes=blob) and uuid.UUID(value).int != 0
                    all_ids.append(uuid.UUID(value))
                witnesses = [name for (name,) in sql.execute("SELECT a.name FROM asset a JOIN logical_file f ON f.file_id=a.file_id WHERE f.source_id=? AND a.name IS NOT NULL", (r[0],)) if "asset" in name.lower()]
                assert witnesses and sorted(witnesses) == sorted(source["witnesses"])
                observed.append((source["name"], fixture["package"]))
    assert len(all_ids) == len(set(all_ids)) == 10 and sorted(observed) == sorted(ASC)


class FixtureTests(unittest.TestCase):
    def test_real_independently_built_databases(self):
        validate(REPO, CONFIG)

    def test_rejects_corrupted_expectations_without_mutating_databases(self):
        mutations = [
            lambda c: c["expectedRows"].reverse(),
            lambda c: c["packageSortFixtures"][0].update(package="Zeta"),
            lambda c: c["packageSortFixtures"][1].update(packageId=c["packageSortFixtures"][0]["packageId"]),
            lambda c: c["packageSortFixtures"][0].update(sha256="0" * 64),
            lambda c: c["packageSortFixtures"][0]["sources"][0].update(witnesses=[]),
            lambda c: c["packageSortFixtures"][0]["sources"][0].update(sourceId=str(uuid.uuid4())),
            lambda c: c["packageSortFixtures"][0]["sources"][0].update(originalPath=str(REPO / "wrong.bundle")),
        ]
        for mutation in mutations:
            with self.subTest(mutation=mutation):
                changed = copy.deepcopy(CONFIG)
                mutation(changed)
                with self.assertRaises(AssertionError):
                    validate(REPO, changed)

    def test_independent_tie_oracle(self):
        assert DESC != list(reversed(ASC))
        assert [(n, p) for n, p in ASC if p == "Alpha"] == [(n, p) for n, p in DESC if p == "Alpha"]
        report = json.loads((PREPARED / "preparation.json").read_text(encoding="utf-8"))
        assert [(r["name"], r["package"]) for r in report["ascending"]] == ASC
        assert [(r["name"], r["package"]) for r in report["descending"]] == DESC


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--repository", required=True, type=Path)
    parser.add_argument("--prepared", required=True, type=Path)
    args = parser.parse_args()
    REPO, PREPARED = args.repository.resolve(), args.prepared.resolve()
    CONFIG = json.loads((PREPARED / "scenario.local.json").read_text(encoding="utf-8"))
    unittest.main(argv=[__file__])
