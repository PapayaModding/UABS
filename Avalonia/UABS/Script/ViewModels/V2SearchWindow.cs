using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UABS.PackageV2.Search;

namespace UABS.ViewModels;

/// <summary>Bounded adjacent window over one immutable keyset session. No count scan,
/// OFFSET, new Search admission or materialized prefix is needed for backward replay.</summary>
public sealed class V2SearchWindow
{
    public const int PageSize = 100;
    public const int MaximumRows = 600;
    public const int MaximumCheckpoints = 128;
    public const int PrefetchRows = 20;
    private readonly Func<SearchCursor?, CancellationToken, Task<SearchPage>> _fetch;
    private readonly SortedDictionary<long, SearchCursor?> _checkpoints = new() { [0] = null };
    private readonly SemaphoreSlim _fetchGate = new(1, 1);
    private long _stride = 1;
    public V2SearchWindow(Func<SearchCursor?, CancellationToken, Task<SearchPage>> fetch) => _fetch = fetch;
    public ObservableCollection<SearchResultItemViewModel> Rows { get; } = new();
    public long StartPage { get; private set; }
    public long StartIndex => StartPage * PageSize;
    public long EndPage => StartPage + (Rows.Count + PageSize - 1) / PageSize - 1;
    public long SeenRows { get; private set; }
    public long? ExactCount { get; private set; }
    public int CheckpointCount => _checkpoints.Count;
    public long FetchedPages { get; private set; }
    public bool CanForward => Rows.Count > 0 && (ExactCount is null || StartIndex + Rows.Count < ExactCount);
    public bool CanBackward => StartPage > 0;
    public bool IsChanging { get; private set; }
    public event Action? Changing;
    public event Action? Changed;

    public async Task<SearchPage> ReadAsync(long pageIndex, CancellationToken token)
    {
        if (pageIndex < 0) throw new ArgumentOutOfRangeException(nameof(pageIndex));
        await _fetchGate.WaitAsync(token);
        try
        {
            var checkpoint = _checkpoints.Last(c => c.Key <= pageIndex);
            for (long index = checkpoint.Key; ; index++)
            {
                token.ThrowIfCancellationRequested();
                var page = await _fetch(checkpoint.Value, token); FetchedPages++;
                token.ThrowIfCancellationRequested();
                if (page.Status != SearchStatus.Success) return page;
                if (page.Rows.Count > PageSize || page.HasMore != (page.NextCursor is not null)
                    || (page.HasMore && page.Rows.Count != PageSize))
                    throw new InvalidDataException("Invalid keyset page boundary; no window change published.");
                if (page.NextCursor is not null) Remember(index + 1, page.NextCursor);
                if (index == pageIndex) return page;
                if (page.NextCursor is null) throw new InvalidDataException("Checkpoint replay exhausted before the requested window.");
                checkpoint = new(index + 1, page.NextCursor);
            }
        }
        finally { _fetchGate.Release(); }
    }

    private void Remember(long index, SearchCursor cursor)
    {
        _checkpoints[index] = cursor;
        while (_checkpoints.Count > MaximumCheckpoints)
        {
            _stride = checked(_stride * 2);
            foreach (long key in _checkpoints.Keys.Where(k => k != 0 && k % _stride != 0
                && (k < StartPage - 2 || k > EndPage + 2)).ToArray()) _checkpoints.Remove(key);
        }
    }

    public void Apply(long pageIndex, SearchPage page,
        Func<SearchResult, SearchRevision, SearchResultItemViewModel>? map = null)
    {
        map ??= (r, v) => new(r, v);
        if (page.Status != SearchStatus.Success) throw new InvalidOperationException("Cannot publish a failed page.");
        bool first = Rows.Count == 0 && SeenRows == 0;
        bool before = !first && pageIndex == StartPage - 1;
        if (!first && !before && pageIndex != EndPage + 1) throw new InvalidOperationException("Only adjacent windows may be published.");
        if (first && pageIndex != 0) throw new InvalidOperationException("First window starts at the actual first result.");
        var keys = Rows.Select(r => r.V2Result!.Key).ToHashSet();
        var added = page.Rows.Select(r => {
            if (!keys.Add(r.Key)) throw new InvalidDataException("Duplicate immutable result identity in keyset continuation.");
            return map(r, page.Revisions.Single(v => v.PackageId == r.Key.PackageId && v.GenerationId == r.Key.GenerationId));
        }).ToArray();
        IsChanging = true;
        try
        {
            Changing?.Invoke();
            if (before)
            {
                StartPage = pageIndex;
                for (int i = 0; i < added.Length; i++) Rows.Insert(i, added[i]);
                while (Rows.Count > MaximumRows) Rows.RemoveAt(Rows.Count - 1);
            }
            else
            {
                foreach (var row in added) Rows.Add(row);
                if (Rows.Count > MaximumRows)
                {
                    int remove = Rows.Count - MaximumRows;
                    // Every evicted leading page is full; only the terminal page can be short.
                    remove = ((remove + PageSize - 1) / PageSize) * PageSize;
                    for (int i = 0; i < remove; i++) Rows.RemoveAt(0);
                    StartPage += remove / PageSize;
                }
            }
            SeenRows = Math.Max(SeenRows, pageIndex * PageSize + added.Length);
            if (!page.HasMore) ExactCount = pageIndex * PageSize + added.Length;
        }
        finally { IsChanging = false; Changed?.Invoke(); }
    }
}
