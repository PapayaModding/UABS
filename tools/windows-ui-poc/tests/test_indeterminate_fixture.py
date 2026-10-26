"""Independent read-only coverage proof; no Search API, Unity claim or SQL mutation."""
import argparse
import copy
import hashlib
import json
import sqlite3
import unittest
import uuid
from pathlib import Path


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def validate(config):
    assert config['acceptance'] == 'checkpoint21-indeterminate' and config['keyword'] == 'g5_known_asset'
    fixtures = config['indeterminateFixtures']
    assert len(fixtures) == 2 and {f['partial'] for f in fixtures} == {False, True}
    ids, paths = [], []
    for f in fixtures:
        prefix = 'partial' if f['partial'] else 'complete'
        assert f['package'] == 'G5 ' + prefix + ' synthetic metadata'
        path, source = Path(f['path']), Path(f['sourcePath'])
        assert path.is_absolute() and path.resolve() == path and not path.is_symlink()
        assert source.is_absolute() and source.resolve() == source and not source.is_symlink()
        assert path.name == 'G5-' + prefix + '.uabs.sqlite' and source.name == 'g5-' + prefix + '.metadata.json'
        assert digest(path) == f['sha256'] and digest(source) == f['sourceSha256']
        assert not Path(str(path) + '-wal').exists() and not Path(str(path) + '-shm').exists()
        evidence = json.loads(source.read_text())
        assert evidence == dict(Provenance='synthetic IV2MetadataExtractor contract; not Unity-extracted data', Partial=f['partial'], Asset='g5_known_asset', Declared=2 if f['partial'] else 1, Captured=1)
        with sqlite3.connect(path.as_uri() + '?mode=ro', uri=True) as sql:
            package = sql.execute('SELECT display_name,index_status,package_uuid,generation_uuid FROM package').fetchall()
            assert len(package) == 1 and package[0][:2] == (f['package'], prefix)
            sources = sql.execute('SELECT original_path,original_name,kind,index_status,content_sha256,byte_size,source_uuid,source_revision_uuid FROM source').fetchall()
            assert len(sources) == 1
            s = sources[0]
            assert Path(s[0]) == source and s[1:4] == (source.name, 'bundle', prefix)
            assert bytes(s[4]).hex() == f['sourceSha256'] and s[5] == source.stat().st_size
            for key, blob in zip(('packageId', 'generationId', 'sourceId', 'sourceRevision'), package[0][2:] + s[6:]):
                identity = uuid.UUID(f[key]); assert identity.int and identity == uuid.UUID(bytes=blob); ids.append(identity)
            files = sql.execute('SELECT declared_asset_count,index_status FROM logical_file').fetchall()
            assert files == [(2 if f['partial'] else 1, prefix)]
            assert sql.execute('SELECT name,name_status FROM asset').fetchall() == [('g5_known_asset', 'present')]
            issues = sql.execute('SELECT code FROM indexing_issue').fetchall()
            assert issues == ([('g5-deliberate-incomplete',)] if f['partial'] else [])
        paths.append(path)
    assert len(set(paths)) == 2 and len(set(ids)) == 8
    complete = next(f for f in fixtures if not f['partial'])
    assert config['package'] == complete['path'] and config['packageSha256'] == complete['sha256']
    assert config['expectedRows'] == [dict(name='g5-complete.metadata.json', package='G5 complete synthetic metadata')]


class CoverageFixtureTests(unittest.TestCase):
    def test_raw_eligible_partial_and_complete_contract(self):
        validate(CONFIG)

    def test_rejects_tampered_expectations(self):
        for field, value in [('partial', False), ('sourceSha256', '0' * 64), ('sourceId', str(uuid.uuid4())), ('package', 'Smoke')]:
            changed = copy.deepcopy(CONFIG)
            next(f for f in changed['indeterminateFixtures'] if f['partial'])[field] = value
            with self.subTest(field=field), self.assertRaises(AssertionError):
                validate(changed)

    def test_rejects_partial_as_success_baseline(self):
        changed = copy.deepcopy(CONFIG)
        changed['expectedRows'][0]['name'] = 'g5-partial.metadata.json'
        with self.assertRaises(AssertionError):
            validate(changed)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--prepared', required=True)
    args = parser.parse_args()
    CONFIG = json.loads((Path(args.prepared) / 'scenario.local.json').read_text())
    unittest.main(argv=['test_indeterminate_fixture'])
