namespace KUKULCAN.SharedKernel.i18n.Application.Common.Pagination;

/// <summary>
/// Wraps a single page of results together with total-count metadata.
/// This is the standard return type for all paginated ATLAS queries.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <example>
/// <code>
/// // In a QueryHandler:
/// var totalCount = await query.CountAsync(ct);
/// var items      = await query
///     .Skip(pagination.Skip)
///     .Take(pagination.PageSize)
///     .Select(c => CustomerDto.FromEntity(c))
///     .ToListAsync(ct);
///
/// return PagedResult&lt;CustomerDto&gt;.Create(items, totalCount, pagination);
///
/// // Consuming in an API controller:
/// var page = await mediator.Send(new GetCustomerListQuery(pagination), ct);
/// Console.WriteLine(page.TotalPages);     // e.g. 5
/// Console.WriteLine(page.HasNextPage);    // true
/// </code>
/// </example>
public sealed class PagedResult<T>
{
    private PagedResult(IReadOnlyList<T> items, long totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    /// <summary>Gets the items on the current page.</summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>Gets the total record count across all pages.</summary>
    public long TotalCount { get; }

    /// <summary>Gets the current 1-based page number.</summary>
    public int Page { get; }

    /// <summary>Gets the maximum items per page.</summary>
    public int PageSize { get; }

    /// <summary>Gets the total number of pages required to display all results.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>Gets a value indicating whether a next page exists.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Gets a value indicating whether a previous page exists.</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>Gets the 1-based index of the first item on this page (0 when empty).</summary>
    public long FirstItemIndex => Items.Count == 0 ? 0 : (long)(Page - 1) * PageSize + 1;

    /// <summary>Gets the 1-based index of the last item on this page.</summary>
    public long LastItemIndex => Items.Count == 0 ? 0 : Math.Min(FirstItemIndex + Items.Count - 1, TotalCount);

    // ── Factory methods ──────────────────────────────────────────────────────

    /// <summary>Creates a populated <see cref="PagedResult{T}"/>.</summary>
    /// <param name="items">The items on the current page.</param>
    /// <param name="totalCount">Total records matching the query.</param>
    /// <param name="pagination">The pagination parameters used to retrieve this page.</param>
    public static PagedResult<T> Create(IReadOnlyList<T> items, long totalCount, PaginationRequest pagination)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(pagination);
        return new PagedResult<T>(items, totalCount, pagination.Page, pagination.PageSize);
    }

    /// <summary>Returns an empty page with zero items and zero total count.</summary>
    public static PagedResult<T> Empty(PaginationRequest pagination)
    {
        ArgumentNullException.ThrowIfNull(pagination);
        return new PagedResult<T>([], 0, pagination.Page, pagination.PageSize);
    }

    /// <summary>Projects each item to a new type using <paramref name="mapper"/>.</summary>
    public PagedResult<TOut> Map<TOut>(Func<T, TOut> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return new PagedResult<TOut>(Items.Select(mapper).ToList(), TotalCount, Page, PageSize);
    }
}
