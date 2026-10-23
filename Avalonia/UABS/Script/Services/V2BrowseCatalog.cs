using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using UABS.Models;
using UABS.PackageV2;

namespace UABS.Services;

public enum V2BrowseBoundary { BeforeAdmission, BeforeAdmissionCommit, BeforeRead }

/// <summary>Reader-only production adapter; never creates V1 documents, Search services or memo writers.</summary>
public sealed class V2BrowseCatalog : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly List<V2BrowseRegistration> _entries = new();
    private bool _disposed;
    private readonly Action<V2BrowseBoundary>? _observe;
    public V2BrowseCatalog(Action<V2BrowseBoundary>? observe = null) => _observe = observe;
    public async Task<V2BrowseRegistration> RegisterAsync(string path, CancellationToken token = default)
    {
        if (!path.EndsWith(".uabs.sqlite", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Select a .uabs.sqlite package; no conversion is performed.");
        string full = Path.GetFullPath(path);
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var existing = _entries.FirstOrDefault(r => string.Equals(r.Path, full, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
            if (existing is not null) { await existing.CheckAsync(token).ConfigureAwait(false); return existing; }
            // Deny rename/delete while admitted on Windows. Still permit cooperating SQLite writers.
            FileStream pin = new(full, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            PackageReader? reader = null;
            try
            {
                pin.Position = 18;
                if (pin.ReadByte() != 1 || pin.ReadByte() != 1)
                    throw new InvalidDataException("Only the approved rollback-journal V2 format is admitted; WAL/invalid headers are unsupported.");
                string evidence = await EvidenceAsync(full, token).ConfigureAwait(false);
                _observe?.Invoke(V2BrowseBoundary.BeforeAdmission);
                token.ThrowIfCancellationRequested();
                var admission = await PackageReader.OpenAsync(full, token).ConfigureAwait(false);
                reader = admission.Reader;
                token.ThrowIfCancellationRequested();
                if (reader is null) throw new InvalidDataException(admission.Message);
                if (evidence != await EvidenceAsync(full, token).ConfigureAwait(false)) throw new InvalidDataException("Package changed during admission; reopen explicitly.");
                if (_entries.Any(r => r.PackageId == reader.Info.PackageId)) throw new InvalidDataException("Package UUID already registered at another location; unload the original first.");
                var entry = new V2BrowseRegistration(full, reader, pin, evidence, _observe);
                token.ThrowIfCancellationRequested();
                _observe?.Invoke(V2BrowseBoundary.BeforeAdmissionCommit);
                _entries.Add(entry); return entry;
            }
            catch { reader?.Dispose(); pin.Dispose(); throw; }
        }
        finally { _gate.Release(); }
    }
    public async Task UnloadAsync(V2BrowseRegistration entry)
    {
        entry.Stop(); // cancel active/waiting reads before draining resources
        await _gate.WaitAsync().ConfigureAwait(false);
        try { _entries.Remove(entry); await entry.CloseAsync().ConfigureAwait(false); }
        finally { _gate.Release(); }
    }
    public void Dispose()
    {
        _gate.Wait();
        try { if (_disposed) return; _disposed = true; foreach (var e in _entries) e.Stop(); foreach (var e in _entries) e.CloseAsync().GetAwaiter().GetResult(); _entries.Clear(); }
        finally { _gate.Release(); }
    }
    internal static Task<string> EvidenceAsync(string path, CancellationToken token) => Task.Run(() => {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        string physical = "";
        if (OperatingSystem.IsWindows())
        {
            if (!GetFileInformationByHandle(stream.SafeFileHandle, out FileInformation info)) throw new IOException("Cannot establish package file identity.");
            physical = $"{info.Volume}:{info.IndexHigh}:{info.IndexLow}";
        }
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = new byte[65536]; int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0) { token.ThrowIfCancellationRequested(); hash.AppendData(buffer, 0, read); }
        token.ThrowIfCancellationRequested(); return physical + ":" + stream.Length + ":" + Convert.ToHexString(hash.GetHashAndReset());
    }, token);
    [StructLayout(LayoutKind.Sequential)]
    private struct FileInformation
    {
        public uint Attributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME Creation, Access, Write;
        public uint Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
    }
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle handle, out FileInformation info);
}

public sealed class V2BrowseRegistration
{
    private readonly PackageReader _reader;
    private readonly FileStream _pin;
    private readonly string _evidence;
    private readonly SemaphoreSlim _reads = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();
    private bool _closed;
    private bool _stale;
    private readonly Action<V2BrowseBoundary>? _observe;
    internal V2BrowseRegistration(string path, PackageReader reader, FileStream pin, string evidence, Action<V2BrowseBoundary>? observe)
    { Path = path; _reader = reader; _pin = pin; _evidence = evidence; _observe = observe; }
    public Guid Epoch { get; } = Guid.NewGuid();
    public string Path { get; }
    public Guid PackageId => _reader.Info.PackageId;
    public Guid GenerationId => _reader.Info.GenerationId;
    public string Name => _reader.Info.Name;
    public long AssetCount => _reader.Info.Assets;
    public long SourceCount => _reader.Info.Sources;
    public string Status => _stale || _closed || _lifetime.IsCancellationRequested ? "stale/unloaded" : _reader.Info.IndexStatus;
    public string Summary => $"V2 · {Name} · {Status} index · {AssetCount:N0} assets · {_reader.Info.Issues:N0} issues\nPackage {PackageId} · Generation {GenerationId}\nRead-only. No V2 Search, memo editing, structural saving or preview. "
        + (_reader.Info.DerivedFieldsCompatible ? "" : "Unknown derived profile: authoritative raw metadata only.");
    internal void Stop() => _lifetime.Cancel();
    internal async Task CloseAsync()
    {
        await _reads.WaitAsync().ConfigureAwait(false);
        try { if (_closed) return; _closed = true; _reader.Dispose(); _pin.Dispose(); }
        finally { _reads.Release(); }
    }
    internal Task CheckAsync(CancellationToken token) => Read(async ct => { await Task.CompletedTask; return true; }, token);
    private async Task<T> Read<T>(Func<CancellationToken, Task<T>> operation, CancellationToken token)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, _lifetime.Token);
        await _reads.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            if (_closed || _stale) throw new InvalidDataException("Registration is stale/unloaded. Remove and reopen the package.");
            if (_evidence != await V2BrowseCatalog.EvidenceAsync(Path, linked.Token).ConfigureAwait(false)) throw new InvalidDataException("Package bytes/location changed. Remove and reopen explicitly.");
            _observe?.Invoke(V2BrowseBoundary.BeforeRead); linked.Token.ThrowIfCancellationRequested();
            T result = await operation(linked.Token).ConfigureAwait(false);
            if (_evidence != await V2BrowseCatalog.EvidenceAsync(Path, linked.Token).ConfigureAwait(false)) throw new InvalidDataException("Package changed during read; no page published.");
            linked.Token.ThrowIfCancellationRequested(); return result;
        }
        catch (OperationCanceledException) { throw; }
        catch { _stale = true; throw; }
        finally { _reads.Release(); }
    }
    public Task<V2BrowsePage<V2AssetRow>> AssetsAsync(long after, CancellationToken token = default) => Read(async ct => {
        var rows = await _reader.ReadAssetsAsync(after, 101, ct).ConfigureAwait(false);
        var page = rows.Take(100).Select(a => new V2AssetRow(a.RowId,
            new(a.Key.PackageId, a.Key.GenerationId, a.Key.SourceId, a.Key.SourceRevision, a.Key.DirectoryOrdinal, a.Key.PathId, a.Key.TableOrdinal),
            a.Name, a.ClassId, a.TypeName, a.Memo, a.MemoRevision, a.IdentityStatus, a.ByteSize)).ToArray();
        return new V2BrowsePage<V2AssetRow>(page, page.Length == 0 ? after : page[^1].Cursor, rows.Count > 100);
    }, token);
    public Task<V2BrowsePage<V2MetadataItem>> MetadataAsync(V2MetadataKind kind, long after, CancellationToken token = default) => Read(async ct => {
        MetadataTable table = kind switch { V2MetadataKind.SourceRoots => MetadataTable.SourceRoots, V2MetadataKind.Sources => MetadataTable.Sources,
            V2MetadataKind.LogicalFiles => MetadataTable.LogicalFiles, V2MetadataKind.Dependencies => MetadataTable.Dependencies, _ => MetadataTable.Issues };
        var rows = await _reader.ReadMetadataAsync(table, after, 101, ct).ConfigureAwait(false);
        var page = rows.Take(100).Select(r => new V2MetadataItem(r.RowId, string.Join("\n", r.Values.Select(p => p.Key + ": " + Format(p.Value))))).ToArray();
        return new V2BrowsePage<V2MetadataItem>(page, page.Length == 0 ? after : page[^1].Cursor, rows.Count > 100);
    }, token);
    private static string Format(object? value) => value switch { null => "<NULL>", byte[] bytes => Convert.ToHexString(bytes), string s => V2AssetRow.Value(s), _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "<NULL>" };
    public Task<string> AssetEvidenceAsync(V2AssetRow asset, CancellationToken token = default) => Read(async ct => {
        var k = asset.Identity;
        var values = await _reader.ReadAssetEvidenceAsync(new(k.PackageId, k.GenerationId, k.SourceId, k.SourceRevision, k.DirectoryOrdinal, k.PathId, k.TableOrdinal), ct).ConfigureAwait(false);
        return string.Join("\n", values.Select(p => p.Key + ": " + Format(p.Value)));
    }, token);
}
