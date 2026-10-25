# Reproducible Unity AssetBundle generator

This Unity 2021.3 LTS project creates original, synthetic AssetBundles for UABS
parser and metadata testing. It never reads game data. The fixed configuration
seed controls names, values, dependency selection, duplicate-name cases, and
Unicode cases. Unity's own deterministic bundle option is enabled; the
manifest records the exact Unity version and output hashes because byte-for-byte
identity across different Unity versions or host platforms is not promised.

Generation and publication are deliberately separate operations. `generate`
does not invoke Git. Publishing can occur only from a later, explicit command.

## Prerequisites

- Unity Editor `2021.3.45f2` (the generator accepts the Unity 2021.3 LTS line,
  while recording the exact version in every manifest)
- Python 3.9 or newer; the tooling uses only the standard library
- Git and network access only for publication preflight/push, not generation

No Unity license, activation token, generated `Library/`, or local logs belong
in Git. The repository `.gitignore` excludes Unity state and working output.

## Generate locally

First validate a checked-in profile without launching Unity:

```bash
python3 test-fixtures/unity/generator/tools/fixturectl.py generate \
  --profile smoke --validate-only
```

Then supply the local Unity executable:

```bash
python3 test-fixtures/unity/generator/tools/fixturectl.py generate \
  --profile smoke --unity /path/to/Unity
```

On Windows PowerShell, for example:

```powershell
python test-fixtures/unity/generator/tools/fixturectl.py generate `
  --profile smoke `
  --unity "C:/Program Files/Unity/Hub/Editor/2021.3.45f2/Editor/Unity.exe"
```

`fixturectl` launches Unity in graphics-enabled batch mode and passes the
configured standalone target through Unity's `-buildTarget` argument. Do not
add `-nographics`: the smoke test deliberately reopens every generated bundle,
including Material and Animation content, and treats disabled runtime modules
or a null graphics device as a failed validation. Run the command in a Windows
desktop session with the Windows Build Support module installed.

The generator also pins `EditorUserBuildSettings.standaloneBuildSubtarget` to
`Player` before building. In Unity 2021.3, the public `BuildAssetBundles`
overload inherits the active subtarget independently of `BuildTarget`. This
prevents an accidental Dedicated Server build context, but Windows testing has
shown that selecting `Player` alone does not resolve the disabled-module
failure. Managed assembly stripping and `PlayerSettings.stripEngineCode` are
logged for diagnosis but are not changed during normal generation.

### Engine-module diagnostic

If Unity reports disabled runtime modules or cannot reopen a bundle, run the
opt-in controlled diagnostic before changing project stripping settings:

```powershell
python test-fixtures/unity/generator/tools/fixturectl.py diagnose-modules `
  --profile smoke `
  --unity "C:/Program Files/Unity/Hub/Editor/2021.3.45f2/Editor/Unity.exe"
```

The command starts two clean Unity processes. Each builds the same forced-rebuild,
LZ4-compressed, TextAsset-only bundle for the same target and `Player`
subtarget. One process temporarily enables `PlayerSettings.stripEngineCode`;
the other temporarily disables it. Each must pass `AssetBundle.LoadFromFile`,
and the original project setting is restored in `finally`. Reports and separate
logs are written below `Generated/working/module-diagnostic/` and remain
ignored. This command diagnoses the setting; it does not change normal fixture
generation or treat a failed reopen as success.

Windows Unity 2021.3.45f2 testing produced identical 442-byte, unloadable
UnityFS bundles with engine-code stripping enabled and disabled. That result
rules out `PlayerSettings.stripEngineCode` as the causal variable and does not
justify a permanent stripping-setting change.

### Native pipeline diagnostic

To isolate the project package configuration from the installed Editor, run:

```powershell
python test-fixtures/unity/generator/tools/fixturectl.py diagnose-native-pipeline `
  --unity "C:/Program Files/Unity/Hub/Editor/2021.3.45f2/Editor/Unity.exe"
```

The command creates two disposable projects below the ignored
`Generated/working/native-pipeline-diagnostic/` directory. Both receive the
same tracked `ProjectSettings`, target (`StandaloneWindows64`), Player
subtarget, probe source, TextAsset, and build options. Their package manifests
differ in exactly one dependency: `module-omitted` matches this generator's
current manifest, while `module-explicit` adds Unity's built-in
`com.unity.modules.assetbundle` package at `1.0.0`. The command does not edit
the tracked manifest or normal generator assets.

For each emitted bundle, the summary decodes the UnityFS format version,
serialized Unity version and revision, declared and actual size, block-info
sizes, flags, and SHA-256. It also records the Editor reopen result and every
log line that reports a disabled module. Interpret the controlled result as
follows:

- omitted fails and explicit succeeds: the package manifest disabled the
  native AssetBundle module; test an explicit Animation module separately
  before correcting the full generator manifest;
- both fail: the cause is independent of the AssetBundle package declaration,
  so inspect the common Editor installation/build-support modules and decoded
  UnityFS metadata;
- both succeed: this isolated native path is healthy, and another difference
  in the full fixture project or generated asset composition remains.

Unity 2021.3 documents `com.unity.modules.*` dependencies as
[built-in packages](https://docs.unity3d.com/2021.3/Documentation/Manual/pack-build-in.html)
that a project can
[disable](https://docs.unity3d.com/2021.3/Documentation/Manual/upm-ui-disable.html).
The tracked generator manifest currently declares only
`com.unity.2d.sprite`; this diagnostic provides the controlled confirmation
required before any permanent package change.

The standard profiles are:

| Profile | Bundles | Assets per bundle | Purpose |
| --- | ---: | ---: | --- |
| `smoke` | 3 | 8 | Fast parser sanity checks |
| `regression` | 18 | 12 | Mixed types, names, and dependencies |
| `stress` | 250 | 40 | Local scale experiments; excluded from publication by default |

Each run writes only below
`test-fixtures/unity/generator/Generated/working/<profile>/`. The output
contains `bundles/`, `fixture-manifest.json`, and a generator marker. The Unity
project writes an adjacent `<profile>.unity-generator.log` and uses ignored
`Assets/GeneratedFixtures/` source assets. A non-empty output directory without
the generator marker is never overwritten.

Configuration controls the seed, bundle/asset counts, compression, standalone
build target, dependency density, duplicate-name frequency, Unicode cases,
asset-type selection, and output directory. Supported generated types include
textures, sprites, materials, prefabs, text assets, ScriptableObjects,
animation clips, and sprite atlases. Source assignments and Unity-reported
bundle contents/dependencies are recorded. The generator does not claim that
source assignments enumerate every internal serialized object or Path ID;
those claims require UABS extraction and separate validation.

Validate output without Unity:

```bash
python3 test-fixtures/unity/generator/tools/fixturectl.py verify \
  --manifest test-fixtures/unity/generator/Generated/working/smoke/fixture-manifest.json
```

## Preview publication

Copy the checked-in template to the ignored local allowlist directory, then
edit it. Every bundle must be named explicitly and must also exist in the
selected manifest:

```bash
mkdir -p test-fixtures/unity/generator/Generated/publication-allowlists
cp test-fixtures/unity/generator/config/publish-allowlist.example.json \
  test-fixtures/unity/generator/Generated/publication-allowlists/regression.json
```

This ignored location lets the required Git checkout remain clean. A reviewed,
already committed allowlist under `config/` is also accepted. Previewing is
read-only with respect to Git:

```bash
python3 test-fixtures/unity/generator/tools/fixturectl.py publish --dry-run \
  --manifest test-fixtures/unity/generator/Generated/working/regression/fixture-manifest.json \
  --allowlist test-fixtures/unity/generator/Generated/publication-allowlists/regression.json
```

The preview prints every source and destination path, size, SHA-256 hash, total
size, and proposed Git status. It writes an ignored immutable plan under
`generator/Generated/publication-plans/`; it does not copy files, stage,
commit, or push.

Even a dry-run refuses unless all publication invariants hold:

- the checkout is clean and the attached branch is exactly `v2-recovery`;
- `HEAD` exactly matches the live `refs/heads/v2-recovery` hash from `origin`;
- `origin` is exactly the GitHub repository `PapayaModding/UABS` over supported
  HTTPS or SSH syntax;
- the manifest is below `Generated/working/`, and the allowlist is either a
  checked-in `config/` file or an ignored local file below
  `Generated/publication-allowlists/`;
- all paths are normalized descendants, regular files, and free of symlinks;
- the destination has no unexpected files or conflicts.

The conservative defaults are at most 30 bundles, 20 MiB per bundle, and
200 MiB total. An allowlist can lower or deliberately configure these positive
limits, but the preview always displays them. Stress collections require both
`"allowStress": true` in the reviewed allowlist and `--approve-stress` on the
dry-run. They are otherwise rejected.

The tool does not enable or configure Git LFS.

## Publish an approved plan

Creating a plan is not authorization to publish. After reviewing its complete
output, run the separate command in an interactive terminal:

```bash
python3 test-fixtures/unity/generator/tools/fixturectl.py publish \
  --plan test-fixtures/unity/generator/Generated/publication-plans/<plan-id>.json
```

The tool rebuilds the plan and rejects any changed input, checkout, remote
commit, destination, or hash. It then requires the exact phrase
`publish <plan-id>`. Only after that confirmation does it copy the allowlisted
files, write the collection manifest/index, and stage those explicit paths.
It never uses `git add .`. It compares the staged path list to the plan before
committing and pushes only `HEAD:refs/heads/v2-recovery`, without force. It
never addresses `main`.

After a push, the tool resolves the remote recovery ref and reads the committed
manifest back from that exact commit, checking its hash against the approved
plan. Independently confirm availability from a fresh Codex Cloud checkout
with the validation commands in `../README.md`; the committed index and
collection files require no Unity installation.

If any preflight, copy, commit, push, or remote check fails, stop and inspect
the checkout. Do not broaden the staged set or bypass the plan checks. No Git
publication command is part of normal generator testing.
