# Open Questions and Uncertainties

## Product behavior

1. Which game platforms must be supported: Windows, Android, iOS, WebGL, consoles, or all files regardless of host OS? This determines required texture codecs and native packaging.
2. Does “search by image” intentionally mean exact filename search, or is real visual/hash similarity expected?
3. Is reverse-dependent lookup (`DependentReader`) a shipped requirement, a planned feature, or dead code?
4. Is dump export (`ExportKind.Dump`) intended to ship? The enum/UI concept exists, but the manager only exports image assets.
5. Must Avalonia preserve the exact Unity layout/interaction, or only workflows and data behavior?
6. Are there known maximum bundle sizes and expected performance targets?

## Existing data compatibility

7. Are there production user packages/memos under `External/UABS_UserPackages` that must remain byte/schema compatible?
8. Can package JSON be upgraded in place, or must Avalonia support legacy and new schema versions concurrently?
9. Should absolute paths stored in packages be remappable when a game install moves?
10. What uniquely identifies an asset for memos when names duplicate: file ID + path ID, CAB + path ID, bundle relative path + path ID, or another stable key?
11. Is the bundle-name truncation before the last underscore in memo inheritance an intentional version-matching rule? What should happen when no underscore exists?

Milestone 7.5 resolved the former package-layout questions: target practical interoperability; preserve relative directories containing imported supported sources and all existing empty indexes; treat safe normal/extended drive and UNC path spellings as equivalent internally while retaining serialized text; and keep full CAB tokens canonical with bounded one-character legacy matching. Exact legacy export remains an optional future product decision.

## Parsing and dependencies

12. Which Unity engine/game versions have been successfully used with the current AssetsTools.NET 3.0.0 stack?
13. Are both Mono and IL2CPP games required? Are representative fixtures available for each?
14. Can a bundle contain multiple serialized assets files or multiple `AssetBundle` metadata objects in supported data? Code currently makes first-entry assumptions in some workflows.
15. Are extensionless bundles or Unity player data files in scope? Folder browsing currently only exposes `.bundle`, `.ab`, and `.assets`.
16. Beyond the verified exact/one-character-truncated rule, are other legacy CAB variants required? Multiple physical matches remain ambiguous.

## Texture/preview

17. Which texture formats are essential for the first release? BC1/3/5/6/7, ASTC, ETC/ETC2, crunched formats, and uncompressed formats have different decoder needs.
18. Is preserving Unity's vertical orientation/color behavior required byte-for-byte for exports?
19. Are rotated/tight-packed sprite atlas entries present? The current atlas crop logic appears rectangle-based only.
20. What should export do with duplicate/invalid asset names, existing files, or decode failures?

## Platform and architecture

21. Is the new application Windows-only? The legacy path code and native DLL strongly imply Windows, but Avalonia can be cross-platform.
22. Is .NET 9 a deliberate target, and what is the intended deployment model (framework-dependent, self-contained, single-file, installer)?
23. May third-party binaries be replaced by supported NuGet packages/upgraded versions, or must exact vendored versions be preserved initially?
24. Where should packages, cache, logs, and settings live, and should the user be allowed to choose them?

## Verification

25. Which legacy features are known by the user to work today versus merely present in source?
26. A small schema-faithful package fixture and real bundle/assets fixtures are committed. Is a production-created, redistributable legacy package available to broaden compatibility coverage?
27. What is the expected golden output for texture/sprite exports and memo inheritance?

Answers should be recorded as architectural decisions before the affected phase begins.
