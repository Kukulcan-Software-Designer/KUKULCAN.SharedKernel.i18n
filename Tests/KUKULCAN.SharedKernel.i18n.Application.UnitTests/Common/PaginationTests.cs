using KUKULCAN.SharedKernel.i18n.Application.Common.Pagination;

namespace KUKULCAN.SharedKernel.i18n.Application.UnitTests.Common;

[TestFixture]
public sealed class PaginationTests
{
    [Test]
    public void PaginationRequest_Create_ClampsPageAndPageSizeAndTrimsSearch()
    {
        var request = PaginationRequest.Create(0, 500, "Name", SortOrder.Descending, "  abc  ");

        Assert.That(request.Page, Is.EqualTo(1));
        Assert.That(request.PageSize, Is.EqualTo(200));
        Assert.That(request.SortBy, Is.EqualTo("Name"));
        Assert.That(request.SortOrder, Is.EqualTo(SortOrder.Descending));
        Assert.That(request.Search, Is.EqualTo("abc"));
        Assert.That(request.Skip, Is.Zero);
    }

    [Test]
    public void PaginationRequest_Skip_IsCalculatedFromPageAndPageSize()
    {
        var request = new PaginationRequest(3, 20);

        Assert.That(request.Skip, Is.EqualTo(40));
    }

    [Test]
    public void PagedResult_Create_CalculatesPageMetadata()
    {
        PagedResult<int> page = PagedResult<int>.Create([11, 12], 42, new PaginationRequest(2, 10));

        Assert.That(page.Items, Is.EqualTo(new[] { 11, 12 }));
        Assert.That(page.TotalCount, Is.EqualTo(42));
        Assert.That(page.TotalPages, Is.EqualTo(5));
        Assert.That(page.HasNextPage, Is.True);
        Assert.That(page.HasPreviousPage, Is.True);
        Assert.That(page.FirstItemIndex, Is.EqualTo(11));
        Assert.That(page.LastItemIndex, Is.EqualTo(12));
    }

    [Test]
    public void PagedResult_Empty_HasNoItemsAndNoPages()
    {
        PagedResult<int> page = PagedResult<int>.Empty(new PaginationRequest(2, 20));

        Assert.That(page.Items, Is.Empty);
        Assert.That(page.TotalCount, Is.Zero);
        Assert.That(page.TotalPages, Is.Zero);
        Assert.That(page.HasNextPage, Is.False);
        Assert.That(page.HasPreviousPage, Is.True);
        Assert.That(page.FirstItemIndex, Is.Zero);
        Assert.That(page.LastItemIndex, Is.Zero);
    }

    [Test]
    public void PagedResult_Map_ProjectsItemsAndPreservesMetadata()
    {
        PagedResult<int> page = PagedResult<int>.Create([1, 2, 3], 10, new PaginationRequest(2, 3));

        PagedResult<string> mapped = page.Map(value => value.ToString());

        Assert.That(mapped.Items, Is.EqualTo(new[] { "1", "2", "3" }));
        Assert.That(mapped.TotalCount, Is.EqualTo(10));
        Assert.That(mapped.Page, Is.EqualTo(2));
        Assert.That(mapped.PageSize, Is.EqualTo(3));
    }

    [Test]
    public void PagedResult_Create_NullItems_Throws()
    {
        Assert.That(() => PagedResult<int>.Create(null!, 0, new PaginationRequest()),
            Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void PagedResult_Create_NullPagination_Throws()
    {
        Assert.That(() => PagedResult<int>.Create([], 0, null!),
            Throws.TypeOf<ArgumentNullException>());
    }
}
