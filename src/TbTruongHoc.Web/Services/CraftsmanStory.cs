using Piranha;
using Piranha.Models;
using TbTruongHoc.Web.Data;
using TbTruongHoc.Web.Models;

namespace TbTruongHoc.Web.Services;

/// <summary>What the trust-block and footer need from the story page.</summary>
/// <param name="Permalink">The story page's URL.</param>
/// <param name="Quote">The trimmed quote, or null when it or the attribution is blank.</param>
/// <param name="Attribution">The trimmed attribution, or null when it or the quote is blank.</param>
public sealed record CraftsmanStoryLink(string Permalink, string Quote, string Attribution);

/// <summary>
/// Story 2.5: finds a site's craftsman story page for the trust-block and
/// the Site B footer link. The one place for the lookup rule: the site's
/// first <see cref="CraftsmanStoryPage"/> in sitemap order that is published
/// and not future-dated (hidden-from-nav pages count). Site B only - every
/// other site gets null. Scoped: the result is memoized per request, since
/// the trust-block and the footer both ask.
/// </summary>
public class CraftsmanStory
{
    private readonly IApi _api;
    private readonly Dictionary<Guid, Task<CraftsmanStoryLink>> _cache = new();

    public CraftsmanStory(IApi api)
    {
        _api = api;
    }

    /// <summary>The site's published story page, or null when there is none.</summary>
    public Task<CraftsmanStoryLink> GetPublishedAsync(Guid siteId)
    {
        if (!_cache.TryGetValue(siteId, out var task))
        {
            task = LookupAsync(siteId);
            _cache[siteId] = task;
        }
        return task;
    }

    private async Task<CraftsmanStoryLink> LookupAsync(Guid siteId)
    {
        if (siteId == Guid.Empty)
        {
            return null;
        }

        var site = await _api.Sites.GetByIdAsync(siteId);
        if (site?.InternalId != SiteSeed.TrongDoiTamInternalId)
        {
            return null;
        }

        var sitemap = await _api.Sites.GetSitemapAsync(siteId, onlyPublished: true);
        var now = DateTime.Now;
        // Cheap pre-filter on the sitemap's type name, so PageInfo is only
        // fetched for likely story pages; TypeId stays the real check.
        var typeTitle = App.PageTypes.GetById(nameof(CraftsmanStoryPage))?.Title;

        foreach (var item in Flatten(sitemap))
        {
            if (!item.Published.HasValue || item.Published.Value > now
                || (typeTitle != null && item.PageTypeName != typeTitle))
            {
                continue;
            }

            var info = await _api.Pages.GetByIdAsync<PageInfo>(item.Id);
            if (info == null || info.TypeId != nameof(CraftsmanStoryPage))
            {
                continue;
            }

            var page = await _api.Pages.GetByIdAsync<CraftsmanStoryPage>(item.Id);
            if (page == null)
            {
                continue;
            }

            var quote = page.QuoteText;
            var attribution = page.QuoteAttributionText;
            return quote != null && attribution != null
                ? new CraftsmanStoryLink(item.Permalink, quote, attribution)
                : new CraftsmanStoryLink(item.Permalink, null, null);
        }

        return null;
    }

    /// <summary>Depth-first, each level in sort order - the sitemap order.</summary>
    private static IEnumerable<SitemapItem> Flatten(IEnumerable<SitemapItem> items)
    {
        foreach (var item in items.OrderBy(i => i.SortOrder))
        {
            yield return item;
            foreach (var child in Flatten(item.Items))
            {
                yield return child;
            }
        }
    }
}
