namespace KUKULCAN.SharedKernel.i18n.Application.Common.Pagination;

/// <summary>Specifies the direction of a sort operation.</summary>
/// <example>
/// <code>
/// var req = new PaginationRequest(Page: 1, PageSize: 20,
///     SortBy: "Name", SortOrder: SortOrder.Descending);
/// </code>
/// </example>
public enum SortOrder
{
    /// <summary>Sort in ascending order (A → Z, 0 → 9, earliest → latest).</summary>
    Ascending = 0,

    /// <summary>Sort in descending order (Z → A, 9 → 0, latest → earliest).</summary>
    Descending = 1,
}
