PRAGMA application_id = 0x55414232; -- UAB2, distinct from legacy/spike
PRAGMA user_version = 20000;       -- major * 10000 + minor
PRAGMA foreign_keys = ON;
BEGIN;

CREATE TABLE package (
    singleton INTEGER PRIMARY KEY CHECK(singleton = 1),
    package_uuid BLOB NOT NULL UNIQUE CHECK(length(package_uuid) = 16),
    generation_uuid BLOB NOT NULL CHECK(length(generation_uuid) = 16),
    schema_major INTEGER NOT NULL CHECK(schema_major = 2),
    schema_minor INTEGER NOT NULL CHECK(schema_minor = 0),
    display_name TEXT NOT NULL,
    created_utc TEXT NOT NULL,
    indexer_version TEXT NOT NULL,
    text_contract TEXT NOT NULL CHECK(text_contract = 'dotnet-ordinal-v1'),
    derived_text_profile TEXT NOT NULL,
    catalog_revision INTEGER NOT NULL DEFAULT 0 CHECK(catalog_revision >= 0),
    index_status TEXT NOT NULL CHECK(index_status IN ('complete','partial'))
) STRICT;

CREATE TABLE source_root (
    root_id INTEGER PRIMARY KEY,
    label TEXT NOT NULL,
    original_base_path TEXT NOT NULL,
    path_policy TEXT NOT NULL CHECK(path_policy IN ('windows-lexical-v1','posix-lexical-v1'))
) STRICT;

CREATE TABLE source (
    source_id INTEGER PRIMARY KEY,
    source_uuid BLOB NOT NULL UNIQUE CHECK(length(source_uuid) = 16),
    source_revision_uuid BLOB NOT NULL CHECK(length(source_revision_uuid) = 16),
    root_id INTEGER NOT NULL REFERENCES source_root(root_id) ON DELETE RESTRICT,
    relative_path TEXT NOT NULL,
    locator_key TEXT NOT NULL,
    original_path TEXT NOT NULL,
    original_name TEXT NOT NULL,
    index_directory TEXT NOT NULL DEFAULT '',
    content_sha256 BLOB CHECK(content_sha256 IS NULL OR length(content_sha256) = 32),
    byte_size INTEGER CHECK(byte_size IS NULL OR byte_size >= 0),
    original_mtime_utc TEXT,
    kind TEXT NOT NULL CHECK(kind IN ('bundle','serialized','unknown')),
    index_status TEXT NOT NULL CHECK(index_status IN ('complete','partial','failed','unsupported')),
    dependency_status TEXT NOT NULL CHECK(dependency_status IN ('complete','partial','unavailable')),
    UNIQUE(root_id, locator_key),
    CHECK(index_status <> 'complete' OR content_sha256 IS NOT NULL)
) STRICT;

CREATE TABLE logical_file (
    file_id INTEGER PRIMARY KEY,
    source_id INTEGER NOT NULL REFERENCES source(source_id) ON DELETE CASCADE,
    directory_ordinal INTEGER NOT NULL CHECK(directory_ordinal >= 0),
    logical_name TEXT,
    cab_name TEXT,
    unity_version TEXT,
    serialized_version INTEGER CHECK(serialized_version IS NULL OR serialized_version >= 0),
    has_type_tree INTEGER CHECK(has_type_tree IS NULL OR has_type_tree IN (0,1)),
    declared_asset_count INTEGER CHECK(declared_asset_count IS NULL OR declared_asset_count >= 0),
    index_status TEXT NOT NULL CHECK(index_status IN ('complete','partial','failed')),
    UNIQUE(source_id, directory_ordinal)
) STRICT;

CREATE TABLE asset_type (
    type_id INTEGER PRIMARY KEY,
    file_id INTEGER NOT NULL REFERENCES logical_file(file_id) ON DELETE CASCADE,
    local_type_index INTEGER CHECK(local_type_index IS NULL OR local_type_index >= 0),
    class_id INTEGER CHECK(class_id IS NULL OR class_id BETWEEN -2147483648 AND 2147483647),
    script_type_index INTEGER,
    type_name TEXT,
    resolution_status TEXT NOT NULL CHECK(resolution_status IN ('resolved','fallback','unavailable')),
    UNIQUE(file_id, local_type_index),
    UNIQUE(type_id, file_id)
) STRICT;

CREATE TABLE asset (
    asset_id INTEGER PRIMARY KEY,
    file_id INTEGER NOT NULL REFERENCES logical_file(file_id) ON DELETE CASCADE,
    table_ordinal INTEGER NOT NULL CHECK(table_ordinal >= 0),
    path_id INTEGER,
    class_id INTEGER CHECK(class_id IS NULL OR class_id BETWEEN -2147483648 AND 2147483647),
    type_id INTEGER,
    byte_offset INTEGER CHECK(byte_offset IS NULL OR byte_offset >= 0),
    byte_size INTEGER CHECK(byte_size IS NULL OR byte_size >= 0),
    identity_status TEXT NOT NULL CHECK(identity_status IN ('addressable','ambiguous','incomplete')),
    name TEXT,
    name_status TEXT NOT NULL CHECK(name_status IN ('present','absent','failed','budgeted','unattempted')),
    name_ascii_key TEXT,
    memo TEXT,
    memo_revision INTEGER NOT NULL DEFAULT 0 CHECK(memo_revision >= 0),
    memo_nonblank INTEGER NOT NULL DEFAULT 0 CHECK(memo_nonblank IN (0,1)),
    UNIQUE(file_id, table_ordinal),
    FOREIGN KEY(type_id, file_id) REFERENCES asset_type(type_id, file_id) ON DELETE RESTRICT,
    CHECK(identity_status <> 'addressable' OR path_id IS NOT NULL),
    CHECK((name_status = 'present' AND name IS NOT NULL) OR
          (name_status <> 'present' AND name IS NULL)),
    CHECK(name_ascii_key IS NULL OR name IS NOT NULL)
) STRICT;

CREATE TABLE dependency (
    dependency_id INTEGER PRIMARY KEY,
    file_id INTEGER NOT NULL REFERENCES logical_file(file_id) ON DELETE CASCADE,
    relation_kind TEXT NOT NULL CHECK(relation_kind IN ('external_file','assetbundle_dependency')),
    declaration_ordinal INTEGER NOT NULL CHECK(declaration_ordinal >= 0),
    declaring_asset_path_id INTEGER,
    raw_path TEXT,
    raw_virtual_path TEXT,
    raw_cab TEXT,
    guid_bytes BLOB CHECK(guid_bytes IS NULL OR length(guid_bytes) = 16),
    external_type INTEGER,
    UNIQUE(file_id, relation_kind, declaration_ordinal)
) STRICT;

CREATE TABLE indexing_issue (
    issue_id INTEGER PRIMARY KEY,
    source_id INTEGER REFERENCES source(source_id) ON DELETE CASCADE,
    file_id INTEGER REFERENCES logical_file(file_id) ON DELETE CASCADE,
    asset_id INTEGER REFERENCES asset(asset_id) ON DELETE CASCADE,
    phase TEXT NOT NULL CHECK(phase IN ('discovery','fingerprint','file','table','name','dependency','validation')),
    code TEXT NOT NULL,
    severity TEXT NOT NULL CHECK(severity IN ('warning','error')),
    message TEXT NOT NULL,
    CHECK((source_id IS NOT NULL) + (file_id IS NOT NULL) + (asset_id IS NOT NULL) <= 1)
) STRICT;

CREATE INDEX ix_file_cab ON logical_file(cab_name) WHERE cab_name IS NOT NULL;
CREATE INDEX ix_source_name ON source(original_name);
CREATE INDEX ix_asset_identity ON asset(file_id, path_id);
CREATE UNIQUE INDEX ux_addressable_path ON asset(file_id, path_id)
    WHERE identity_status = 'addressable';
CREATE INDEX ix_asset_class ON asset(class_id, asset_id);
CREATE INDEX ix_asset_path ON asset(path_id, asset_id) WHERE path_id IS NOT NULL;
CREATE INDEX ix_asset_type_file ON asset(type_id, file_id);
CREATE INDEX ix_asset_ascii_name ON asset(name_ascii_key, asset_id)
    WHERE name_ascii_key IS NOT NULL;
CREATE INDEX ix_asset_with_memo ON asset(asset_id) WHERE memo_nonblank = 1;
CREATE INDEX ix_dependency_guid ON dependency(guid_bytes) WHERE guid_bytes IS NOT NULL;
CREATE INDEX ix_dependency_cab ON dependency(raw_cab) WHERE raw_cab IS NOT NULL;
CREATE INDEX ix_issue_source ON indexing_issue(source_id);
CREATE INDEX ix_issue_file ON indexing_issue(file_id);
CREATE INDEX ix_issue_asset ON indexing_issue(asset_id);

CREATE TRIGGER asset_initial_memo AFTER INSERT ON asset BEGIN
    UPDATE asset SET memo_nonblank = uabs_has_text_v1(NEW.memo)
    WHERE asset_id = NEW.asset_id;
END;
CREATE TRIGGER asset_memo_changed AFTER UPDATE OF memo ON asset
WHEN NEW.memo IS NOT OLD.memo BEGIN
    UPDATE asset SET memo_revision = OLD.memo_revision + 1,
        memo_nonblank = uabs_has_text_v1(NEW.memo) WHERE asset_id = NEW.asset_id;
    UPDATE package SET catalog_revision = catalog_revision + 1 WHERE singleton = 1;
END;
COMMIT;
