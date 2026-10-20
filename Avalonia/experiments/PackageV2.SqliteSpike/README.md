# Experimental Package V2 SQLite spike

Not production persistence, not in `Avalonia.sln`. Requires .NET 9 runtime; current SDK may be newer.

```powershell
dotnet build experiments/PackageV2.SqliteSpike -c Debug
dotnet run --project experiments/PackageV2.SqliteSpike -c Debug --no-build -- test
dotnet run --project experiments/PackageV2.SqliteSpike -c Debug --no-build -- bench 10000
dotnet run --project experiments/PackageV2.SqliteSpike -c Debug --no-build -- bench 100000
```

`bench 1000000` is optional. Each invocation writes a fresh ignored `artifacts/uabs-v2-spike-*` directory inside this experiment, never a real package destination. Run sequentially. `test` includes read-only enumeration of committed Unity fixtures. Outputs are retained for inspection; original fixtures stay untouched.

`test` runs deterministic executable assertions (not a replacement production xUnit suite). `probe.ps1 -ArtifactDirectory <completed-benchmark-directory>` records three process-cold/OS-cache-warm observations and bounded scan/FTS page comparisons. Native calls are synchronous and execute without a desktop UI. Exact-name indexes require the registered .NET collation on every connection; this is experimental, not a generic portable SQLite format contract.

See the [validation report](../../docs/05_audit/package-format-v2-dotnet-sqlite-spike.md). Linux/macOS deployment is not proven by a Windows build.
