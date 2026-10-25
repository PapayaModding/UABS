# UABS Unity fixtures

This tree separates local fixture generation from reviewed, repository-owned
fixtures. Unity Editor is needed only to create a collection. Consumers in
Codex Cloud and CI read committed bundles and their JSON manifests directly;
they do not need Unity Editor or a Unity runtime.

- `generator/` is the reproducible Unity project and guarded command-line tool.
- `published/<collection>/` is reserved for explicitly approved collections.
- `manifest.json` indexes approved collections. An empty `collections` array
  means that no generated collection has been published yet.
- `manifest.schema.json` describes each collection manifest.

## Validate a published collection

From the repository root, choose a collection listed in `manifest.json` and
run:

```bash
python3 test-fixtures/unity/generator/tools/fixturectl.py verify \
  --manifest test-fixtures/unity/published/<collection>/fixture-manifest.json
jq '.collections[] | {name, manifestSha256, bundleCount, totalBytes}' \
  test-fixtures/unity/manifest.json
jq '.bundles[] | {file, sizeBytes, sha256}' \
  test-fixtures/unity/published/<collection>/fixture-manifest.json
sha256sum test-fixtures/unity/published/<collection>/bundles/*
```

`fixturectl verify` is the authoritative convenience check: it rejects unsafe
paths and symlinks, then verifies every listed size and SHA-256 digest. The
`jq` and `sha256sum` commands are useful for inspection and independent spot
checks.

The existing headless UABS regression suite remains:

```bash
dotnet build Avalonia/Avalonia.sln -c Debug --no-restore
dotnet test Avalonia/Avalonia.sln -c Debug --no-build --no-restore \
  --logger 'console;verbosity=minimal' --verbosity minimal
```

For parser-focused coverage, use:

```bash
dotnet test Avalonia/UABS.UnityAssets.Tests/UABS.UnityAssets.Tests.csproj \
  -c Debug --no-build --no-restore \
  --filter 'FullyQualifiedName~AssetSessionTests|FullyQualifiedName~V2MetadataExtractionTests'
```

Those tests are supported without Unity installed. A generated collection is
not silently injected into the suite: a test must explicitly select a bundle
from its manifest so additions remain reviewable and deterministic.

See [generator/README.md](generator/README.md) for generation, dry-run, and
publication controls.
