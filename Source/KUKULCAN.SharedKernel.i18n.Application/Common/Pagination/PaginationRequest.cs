namespace KUKULCAN.SharedKernel.i18n.Application.Common.Pagination;

// ReSharper disable NotAccessedPositionalProperty.Global
/// <summary>
/// Encapsulates the parameters for a paginated, sortable, searchable data query.
/// </summary>
/// <param name="Page">1-based page number (clamped to ≥ 1 by <see cref="Create"/>).</param>
/// <param name="PageSize">Records per page (clamped to 1–200 by <see cref="Create"/>).</param>
/// <param name="SortBy">
/// Property name to sort by (e.g., <c>"Name"</c>, <c>"CreatedOn"</c>).
/// <c>null</c> uses the default ordering.
/// </param>
/// <param name="SortOrder">Sort direction. Defaults to <see cref="SortOrder.Ascending"/>.</param>
/// <param name="Search">Optional free-text search term applied across searchable fields.</param>
/// <example>
/// <code>
/// // Safe construction — values are clamped automatically:
/// var pagination = PaginationRequest.Create(page: 0, pageSize: 500, sortBy: "Name");
/// Console.WriteLine(pagination.Page);     // 1   (clamped from 0)
/// Console.WriteLine(pagination.PageSize); // 200 (clamped from 500)
/// Console.WriteLine(pagination.Skip);     // 0
/// </code>
/// </example>
public sealed record PaginationRequest(int Page = 1, int PageSize = 20, string? SortBy = null, SortOrder SortOrder = SortOrder.Ascending, string? Search = null)
{
    /// <summary>Gets the 0-based record offset for SQL <c>OFFSET</c> / EF Core <c>Skip()</c>.</summary>
    public int Skip => (Page - 1) * PageSize;

    /// <summary>
    /// Creates a <see cref="PaginationRequest"/> with values clamped to safe bounds,
    /// preventing invalid page/size combinations from reaching the database.
    /// </summary>
    /// <param name="page">Clamped to ≥ 1.</param>
    /// <param name="pageSize">Clamped to [1, 200].</param>
    /// <param name="sortBy">Optional property name to sort by.</param>
    /// <param name="order">Sort direction (default: ascending).</param>
    /// <param name="search">Optional search term (trimmed automatically).</param>
    public static PaginationRequest Create(int page, int pageSize, string? sortBy = null, SortOrder order = SortOrder.Ascending, string? search = null)
        => new(
            Page: Math.Max(1, page),
            PageSize: Math.Clamp(pageSize, 1, 200),
            SortBy: sortBy,
            SortOrder: order,
            Search: search?.Trim());
}
// ReSharper restore NotAccessedPositionalProperty.Global
