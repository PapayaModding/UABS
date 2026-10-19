using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace UABS.UnityAssets.Packages.Memos;

public enum PackageMemoPersistenceStatus
{
    Saved,
    Unchanged,
    PackageMissing,
    AssetNotFound,
    AmbiguousIdentity,
    RevisionConflict,
    PackageEditorActive,
    WriteFailure,
    VerificationFailure,
    CatalogPublicationFailure,
    CancelledBeforeCommit
}

public sealed record PackageMemoPersistenceRequest(
    PackageMemoIdentity Identity,
    string? DesiredMemo,
    string? ExpectedOriginalMemo,
    string? ExpectedPackageRevision = null);

public sealed record PackageMemoPersistenceResult(
    PackageMemoPersistenceStatus Status,
    PackageMemoPersistenceRequest Request,
    string Diagnostic,
    string? PackageRevision = null,
    PackageDocument? PersistedPackage = null)
{
    public bool IsSuccess => Status is PackageMemoPersistenceStatus.Saved or PackageMemoPersistenceStatus.Unchanged;
    public bool ChangedDisk => Status is PackageMemoPersistenceStatus.Saved
        or PackageMemoPersistenceStatus.VerificationFailure
        or PackageMemoPersistenceStatus.CatalogPublicationFailure;
}

public delegate Task PackageMemoCatalogPublisher(
    PackageDocument persistedPackage,
    CancellationToken cancellationToken);

public interface IPackageMemoPersistenceService
{
    Task<string?> CaptureRevisionAsync(
        string packagePath,
        CancellationToken cancellationToken = default);

    Task<PackageMemoPersistenceResult> PersistAsync(
        PackageMemoPersistenceRequest request,
        CancellationToken cancellationToken = default);
}

internal interface IPackageRevisionProvider
{
    Task<string?> CaptureAsync(string packagePath, CancellationToken cancellationToken);
}

public sealed class PackageMemoPersistenceService : IPackageMemoPersistenceService
{
    private readonly IPackageService _packageService;
    private readonly IPackageWriteCoordinator _writeCoordinator;
    private readonly IPackageMemoService _memoService;
    private readonly IPackageEditorSessionGuard? _editorSessionGuard;
    private readonly PackageMemoCatalogPublisher? _catalogPublisher;
    private readonly IPackageRevisionProvider _revisionProvider;

    public PackageMemoPersistenceService(
        IPackageService packageService,
        IPackageWriteCoordinator writeCoordinator,
        IPackageMemoService? memoService = null,
        IPackageEditorSessionGuard? editorSessionGuard = null,
        PackageMemoCatalogPublisher? catalogPublisher = null)
        : this(
            packageService,
            writeCoordinator,
            memoService,
            editorSessionGuard,
            catalogPublisher,
            new PackageRevisionProvider())
    {
    }

    internal PackageMemoPersistenceService(
        IPackageService packageService,
        IPackageWriteCoordinator writeCoordinator,
        IPackageMemoService? memoService,
        IPackageEditorSessionGuard? editorSessionGuard,
        PackageMemoCatalogPublisher? catalogPublisher,
        IPackageRevisionProvider revisionProvider)
    {
        _packageService = packageService ?? throw new ArgumentNullException(nameof(packageService));
        _writeCoordinator = writeCoordinator ?? throw new ArgumentNullException(nameof(writeCoordinator));
        _memoService = memoService ?? new PackageMemoService();
        _editorSessionGuard = editorSessionGuard;
        _catalogPublisher = catalogPublisher;
        _revisionProvider = revisionProvider ?? throw new ArgumentNullException(nameof(revisionProvider));
    }

    public Task<string?> CaptureRevisionAsync(
        string packagePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
        return _revisionProvider.CaptureAsync(packagePath, cancellationToken);
    }

    public async Task<PackageMemoPersistenceResult> PersistAsync(
        PackageMemoPersistenceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Identity);
        string? packagePath = request.Identity.PackagePath;
        if (string.IsNullOrWhiteSpace(packagePath))
            return Result(PackageMemoPersistenceStatus.PackageMissing, request, "The memo identity does not contain a package path.");

        try
        {
            return await _writeCoordinator.ExecutePackageWriteAsync(
                packagePath,
                token => PersistCoordinatedAsync(request, packagePath, token),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result(
                PackageMemoPersistenceStatus.CancelledBeforeCommit,
                request,
                "The memo save was cancelled before package replacement began.");
        }
        catch (DirectoryNotFoundException exception)
        {
            return Result(PackageMemoPersistenceStatus.PackageMissing, request, exception.Message);
        }
        catch (Exception exception) when (exception is PackageException
                                           or IOException
                                           or UnauthorizedAccessException
                                           or System.Security.SecurityException
                                           or ArgumentException
                                           or NotSupportedException)
        {
            return Result(PackageMemoPersistenceStatus.WriteFailure, request, exception.Message);
        }
    }

    private async Task<PackageMemoPersistenceResult> PersistCoordinatedAsync(
        PackageMemoPersistenceRequest request,
        string packagePath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_editorSessionGuard?.IsPackageOpen(packagePath) == true)
        {
            return Result(
                PackageMemoPersistenceStatus.PackageEditorActive,
                request,
                "Memo-only persistence is unavailable while this package is open in Package Editor.");
        }

        string? loadedRevision = await _revisionProvider.CaptureAsync(packagePath, cancellationToken).ConfigureAwait(false);
        if (loadedRevision is null)
            return Result(PackageMemoPersistenceStatus.PackageMissing, request, "The package directory no longer exists.");
        if (request.ExpectedPackageRevision is not null
            && !string.Equals(request.ExpectedPackageRevision, loadedRevision, StringComparison.Ordinal))
        {
            return Result(
                PackageMemoPersistenceStatus.RevisionConflict,
                request,
                "The package changed after the memo draft was loaded.",
                loadedRevision);
        }

        PackageDocument latest = await _packageService.LoadAsync(
            packagePath,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        string? afterLoadRevision = await _revisionProvider.CaptureAsync(packagePath, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(loadedRevision, afterLoadRevision, StringComparison.Ordinal))
        {
            return Result(
                PackageMemoPersistenceStatus.RevisionConflict,
                request,
                "The package changed while its latest metadata was being loaded.",
                afterLoadRevision);
        }

        PackageMemoReadResult current = _memoService.Read(latest, request.Identity);
        if (current.Status == PackageMemoLookupStatus.NotFound)
            return Result(PackageMemoPersistenceStatus.AssetNotFound, request, current.Diagnostic, loadedRevision);
        if (current.Status == PackageMemoLookupStatus.Ambiguous)
            return Result(PackageMemoPersistenceStatus.AmbiguousIdentity, request, current.Diagnostic, loadedRevision);
        if (!string.Equals(current.Value, request.ExpectedOriginalMemo, StringComparison.Ordinal))
        {
            return Result(
                PackageMemoPersistenceStatus.RevisionConflict,
                request,
                "The target memo changed after this draft was created.",
                loadedRevision);
        }
        if (string.Equals(current.Value, request.DesiredMemo, StringComparison.Ordinal))
        {
            if (_catalogPublisher is not null)
            {
                try
                {
                    await _catalogPublisher(latest, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    return Result(
                        PackageMemoPersistenceStatus.CatalogPublicationFailure,
                        request,
                        $"The memo was already persisted, but catalog publication failed: {exception.Message}",
                        loadedRevision,
                        latest);
                }
            }

            return Result(
                PackageMemoPersistenceStatus.Unchanged,
                request,
                "The requested memo is already stored on the exact asset.",
                loadedRevision,
                latest);
        }

        PackageDocument candidate = latest.CloneForEditing();
        _memoService.SetLocalMemo(candidate, request.Identity, request.DesiredMemo);

        string? beforeCommitRevision = await _revisionProvider.CaptureAsync(packagePath, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(loadedRevision, beforeCommitRevision, StringComparison.Ordinal))
        {
            return Result(
                PackageMemoPersistenceStatus.RevisionConflict,
                request,
                "The package changed before the memo transaction could commit.",
                beforeCommitRevision);
        }

        cancellationToken.ThrowIfCancellationRequested();
        await _packageService.SaveAsync(
            candidate,
            packagePath,
            overwrite: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        try
        {
            string? savedRevision = await _revisionProvider.CaptureAsync(packagePath, CancellationToken.None).ConfigureAwait(false);
            PackageDocument persisted = await _packageService.LoadAsync(
                packagePath,
                cancellationToken: CancellationToken.None).ConfigureAwait(false);
            string? verifiedRevision = await _revisionProvider.CaptureAsync(packagePath, CancellationToken.None).ConfigureAwait(false);
            if (savedRevision is null || !string.Equals(savedRevision, verifiedRevision, StringComparison.Ordinal))
            {
                return Result(
                    PackageMemoPersistenceStatus.VerificationFailure,
                    request,
                    "The package changed while the saved memo was being verified.",
                    verifiedRevision);
            }

            PackageMemoReadResult verified = _memoService.Read(persisted, request.Identity);
            if (verified.Status != PackageMemoLookupStatus.Found
                || !string.Equals(verified.Value, request.DesiredMemo, StringComparison.Ordinal))
            {
                return Result(
                    PackageMemoPersistenceStatus.VerificationFailure,
                    request,
                    "The package writer completed, but the exact memo value could not be confirmed after reload.",
                    verifiedRevision);
            }

            if (_catalogPublisher is not null)
            {
                try
                {
                    await _catalogPublisher(persisted, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    return Result(
                        PackageMemoPersistenceStatus.CatalogPublicationFailure,
                        request,
                        $"The memo was saved and verified, but catalog publication failed: {exception.Message}",
                        verifiedRevision,
                        persisted);
                }
            }

            return Result(
                PackageMemoPersistenceStatus.Saved,
                request,
                "The memo was saved and verified against the exact asset identity.",
                verifiedRevision,
                persisted);
        }
        catch (Exception exception) when (exception is PackageException
                                           or IOException
                                           or UnauthorizedAccessException
                                           or System.Security.SecurityException
                                           or ArgumentException
                                           or NotSupportedException)
        {
            return Result(
                PackageMemoPersistenceStatus.VerificationFailure,
                request,
                $"The package writer completed, but verification failed: {exception.Message}");
        }
    }

    private static PackageMemoPersistenceResult Result(
        PackageMemoPersistenceStatus status,
        PackageMemoPersistenceRequest request,
        string diagnostic,
        string? revision = null,
        PackageDocument? persistedPackage = null) =>
        new(status, request, diagnostic, revision, persistedPackage);
}

internal sealed class PackageRevisionProvider : IPackageRevisionProvider
{
    public Task<string?> CaptureAsync(string packagePath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
        return Task.Run(() => Capture(packagePath, cancellationToken), cancellationToken);
    }

    private static string? Capture(string packagePath, CancellationToken cancellationToken)
    {
        string fullPath = Path.GetFullPath(packagePath);
        if (!Directory.Exists(fullPath))
            return null;

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
            IgnoreInaccessible = false,
            ReturnSpecialDirectories = false
        };
        string[] entries = Directory.EnumerateFileSystemEntries(fullPath, "*", options)
            .OrderBy(path => Path.GetRelativePath(fullPath, path), StringComparer.OrdinalIgnoreCase)
            .ThenBy(path => Path.GetRelativePath(fullPath, path), StringComparer.Ordinal)
            .ToArray();

        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] fileLength = new byte[8];
        foreach (string entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool isDirectory = Directory.Exists(entry);
            string relativePath = Path.GetRelativePath(fullPath, entry).Replace('\\', '/');
            Append(hash, isDirectory ? "D" : "F");
            Append(hash, relativePath);
            if (isDirectory)
                continue;

            using FileStream stream = new(
                entry,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                81920,
                FileOptions.SequentialScan);
            BinaryPrimitives.WriteInt64LittleEndian(fileLength, stream.Length);
            hash.AppendData(fileLength);
            byte[] buffer = new byte[81920];
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                hash.AppendData(buffer, 0, read);
            }
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static void Append(IncrementalHash hash, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(length, bytes.Length);
        hash.AppendData(length);
        hash.AppendData(bytes);
    }
}
