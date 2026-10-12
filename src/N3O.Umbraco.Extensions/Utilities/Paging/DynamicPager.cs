using Flurl;
using N3O.Umbraco.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Extensions;

namespace N3O.Umbraco.Utilities;

public partial class DynamicPager<T> {
    private readonly Uri _currentUrl;
    private int? _currentPageNumber;

    public DynamicPager(Uri currentUrl,
                        Func<int, int, IEnumerable<T>> getResults,
                        int totalResults,
                        int pageSize,
                        int? firstPageSize = null) {
        _currentUrl = currentUrl;
        PageSize = pageSize;
        FirstPageSize = firstPageSize ?? pageSize;

        TotalResults = totalResults;

        var pages = new List<ResultsPage>();
        pages.Add(new ResultsPage(getResults, 1, 0, FirstPageSize));

        var start = FirstPageSize;
        var pageNumber = 1;
        while (totalResults > start) {
            var num = Math.Min(PageSize, totalResults - start);

            pages.Add(new ResultsPage(getResults, pageNumber, start, num));

            pageNumber++;
            start += num;
        }

        Pages = pages;
    }

    public IReadOnlyList<ResultsPage> Pages { get; }

    public string NavigatePageUrl(int pageNumber) {
        var url = new Url(_currentUrl.GetAbsolutePathDecoded());

        url.Query = _currentUrl.Query;

        if (pageNumber == 1) {
            url.RemoveQueryParams(Pager.QueryString);
        } else {
            url.SetQueryParam(Pager.QueryString, pageNumber);
        }

        return url.ToString();
    }

    public string NavigateNextPageUrl() => NavigatePageUrl(CurrentPageNumber + 1);

    public string NavigatePreviousPageUrl() => NavigatePageUrl(CurrentPageNumber - 1);

    public int StartPage => Math.Max(1, CurrentPageNumber - 2);

    public int EndPage => Math.Min(TotalPages, StartPage + 4);

    public int TotalPages => Pages.Count;

    public bool HasNextPage => CurrentPageNumber != TotalPages;

    public bool HasPreviousPage => CurrentPageNumber != 1;

    public ResultsPage CurrentPage => Pages?.ElementAt(CurrentPageNumber - 1);

    public int CurrentPageNumber {
        get {
            if (_currentPageNumber == null) {
                var queryParams = new QueryParamCollection(_currentUrl.Query);

                var pageNumber = 1;

                var pageParam = queryParams.FirstOrDefault(x => x.Name == Pager.QueryString).Value?.ToString();

                if (int.TryParse(pageParam, out var requestedPageNumber)) {
                    pageNumber = requestedPageNumber;
                }

                _currentPageNumber = Math.Min(Math.Max(pageNumber, 1), TotalPages);
            }

            return _currentPageNumber.Value;
        }
    }

    public int FirstPageSize { get; }
    public int PageSize { get; }
    public int TotalResults { get; }
}
