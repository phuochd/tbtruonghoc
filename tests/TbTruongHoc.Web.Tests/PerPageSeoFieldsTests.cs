using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Piranha;
using Piranha.Models;
using TbTruongHoc.Web.Data;
using TbTruongHoc.Web.Models;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Covers Story 1.2 (per-page SEO fields): confirms the platform capability
/// Piranha's <c>Page&lt;T&gt;</c>/<c>Post&lt;T&gt;</c> base and the scaffolded
/// <c>Views/Cms/*.cshtml</c> already provide - editable Title/Meta
/// Description/Slug rendered into <c>&lt;title&gt;</c>/<c>&lt;meta
/// name="description"&gt;</c> and resolved at the edited slug's URL, with a
/// post-publish slug change reflected both in the page's own URL and in
/// Piranha's own Sitemap (the mechanism <c>_Layout.cshtml</c>'s nav reads,
/// never a hardcoded link) - actually works end to end against the real
/// MariaDB-backed <see cref="IApi"/> and the real HTTP pipeline. No
/// production code is exercised here beyond what Story 1.1 already
/// scaffolded; this is verification, not new capability.
/// </summary>
[Collection(PiranhaAppCollection.Name)]
public class PerPageSeoFieldsTests
{
    /// <summary>
    /// Piranha treats whichever top-level page holds the lowest
    /// <c>SortOrder</c> for a site as that site's home page - its
    /// <c>Permalink</c> becomes "/" regardless of its own <c>Slug</c>. Every
    /// throwaway top-level page/blog this test suite creates must stay off
    /// that value (the real scaffold default, untouched by these tests) so
    /// it is never mistaken for the site's home page.
    /// </summary>
    private const int NonStartPageSortOrder = 1;

    private readonly PiranhaWebApplicationFactory _factory;

    public PerPageSeoFieldsTests(PiranhaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Published_Page_Renders_MetaTitle_And_MetaDescription_And_Resolves_At_Its_Slug()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetTbTruongHocSiteAsync(api);

        var suffix = Guid.NewGuid().ToString("N");
        var metaTitle = $"SEO Meta Title {suffix}";
        var metaDescription = $"SEO meta description for automated test {suffix}.";

        var page = await api.Pages.CreateAsync<StandardPage>();
        page.SiteId = site.Id;
        page.SortOrder = NonStartPageSortOrder;
        page.Title = $"SEO Test Page {suffix}";
        page.MetaTitle = metaTitle;
        page.MetaDescription = metaDescription;
        page.Slug = $"seo-test-page-{suffix}";
        page.Published = DateTime.Now;
        await api.Pages.SaveAsync(page);

        try
        {
            var saved = await api.Pages.GetByIdAsync<StandardPage>(page.Id);
            Assert.NotNull(saved);

            var html = await GetHtmlAsync(saved!.Permalink, HostnameOf(site));

            Assert.Contains($"<title>{metaTitle}</title>", html);
            Assert.Contains(MetaDescriptionTag(metaDescription), html);

            // Story 1.4 (FR-3): the quote-request form and its JS handler must
            // actually render on every published Page - not just compile - so
            // this feature can't silently disappear from the page while every
            // other test here stays green.
            Assert.Contains("data-quote-request-form", html);
            Assert.Contains("lead-form.js", html);
        }
        finally
        {
            await api.Pages.DeleteAsync(page.Id);
        }
    }

    [Fact]
    public async Task Published_Page_With_Html_Special_Characters_Is_Escaped_In_Title_And_MetaDescription()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetTbTruongHocSiteAsync(api);

        var suffix = Guid.NewGuid().ToString("N");
        // Deliberately includes characters that must be HTML-encoded
        // (&, <, >, ") - an unescaped '"' here would break out of the
        // <meta content="..."> attribute, which a plain substring match
        // would not catch. Regression test for the encoding bug in
        // Piranha's own WebApp.MetaTags(Model) helper (unfixed as of
        // Piranha 12.0-12.2) that Views/Shared/_MetaTags.cshtml now
        // replaces.
        var metaTitle = $"SEO <Test> \"Title\" & More {suffix}";
        var metaDescription = $"SEO & <meta> \"description\" test {suffix}.";

        var page = await api.Pages.CreateAsync<StandardPage>();
        page.SiteId = site.Id;
        page.SortOrder = NonStartPageSortOrder;
        page.Title = $"SEO Escaping Test Page {suffix}";
        page.MetaTitle = metaTitle;
        page.MetaDescription = metaDescription;
        page.Slug = $"seo-escaping-test-page-{suffix}";
        page.Published = DateTime.Now;
        await api.Pages.SaveAsync(page);

        try
        {
            var saved = await api.Pages.GetByIdAsync<StandardPage>(page.Id);
            Assert.NotNull(saved);

            var html = await GetHtmlAsync(saved!.Permalink, HostnameOf(site));

            Assert.Contains($"<title>{System.Net.WebUtility.HtmlEncode(metaTitle)}</title>", html);
            Assert.Contains(MetaDescriptionTag(System.Net.WebUtility.HtmlEncode(metaDescription)), html);
            // The raw, unescaped characters must never appear literally where
            // they would break the surrounding markup/attribute.
            Assert.DoesNotContain($"content=\"{metaDescription}\"", html);
        }
        finally
        {
            await api.Pages.DeleteAsync(page.Id);
        }
    }

    [Fact]
    public async Task Published_Page_Without_MetaTitle_Falls_Back_To_Title()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetTbTruongHocSiteAsync(api);

        var suffix = Guid.NewGuid().ToString("N");
        var title = $"Fallback Title {suffix}";

        var page = await api.Pages.CreateAsync<StandardPage>();
        page.SiteId = site.Id;
        page.SortOrder = NonStartPageSortOrder;
        page.Title = title;
        // MetaTitle deliberately left empty - Views/Cms/Page.cshtml must fall
        // back to Model.Title rather than rendering an empty <title>.
        page.Slug = $"seo-fallback-{suffix}";
        page.Published = DateTime.Now;
        await api.Pages.SaveAsync(page);

        try
        {
            var saved = await api.Pages.GetByIdAsync<StandardPage>(page.Id);
            Assert.NotNull(saved);

            var html = await GetHtmlAsync(saved!.Permalink, HostnameOf(site));

            Assert.Contains($"<title>{title}</title>", html);
        }
        finally
        {
            await api.Pages.DeleteAsync(page.Id);
        }
    }

    [Fact]
    public async Task Changing_Slug_After_Publish_Moves_The_Page_And_Updates_The_Sitemap()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetTbTruongHocSiteAsync(api);

        var suffix = Guid.NewGuid().ToString("N");
        var title = $"Slug Change Test {suffix}";
        var oldSlug = $"seo-old-slug-{suffix}";
        var newSlug = $"seo-new-slug-{suffix}";

        var page = await api.Pages.CreateAsync<StandardPage>();
        page.SiteId = site.Id;
        page.SortOrder = NonStartPageSortOrder;
        page.Title = title;
        page.Slug = oldSlug;
        page.Published = DateTime.Now;
        await api.Pages.SaveAsync(page);

        try
        {
            var afterFirstPublish = await api.Pages.GetByIdAsync<StandardPage>(page.Id);
            Assert.NotNull(afterFirstPublish);
            var oldPermalink = afterFirstPublish!.Permalink;

            var oldHtml = await GetHtmlAsync(oldPermalink, HostnameOf(site));
            Assert.Contains($"<title>{title}</title>", oldHtml);

            // Editor changes the slug post-publish.
            afterFirstPublish.Slug = newSlug;
            await api.Pages.SaveAsync(afterFirstPublish);

            var afterSlugChange = await api.Pages.GetByIdAsync<StandardPage>(page.Id);
            Assert.NotNull(afterSlugChange);
            var newPermalink = afterSlugChange!.Permalink;
            Assert.NotEqual(oldPermalink, newPermalink);

            var newHtml = await GetHtmlAsync(newPermalink, HostnameOf(site));
            Assert.Contains($"<title>{title}</title>", newHtml);

            // The story's AC only requires the new slug to resolve and internal
            // links (Sitemap-driven, never hardcoded) to keep resolving - it
            // does not require the old slug to stop working, and this repo has
            // no redirect/alias mechanism yet (that's Epic 8/AD-4's scope, not
            // this story's). What the old slug itself does now is out of scope
            // here and deliberately not asserted.

            // Piranha's own Sitemap (the mechanism _Layout.cshtml's nav reads,
            // never a hardcoded link) must reflect the new URL, not the old one.
            var sitemap = await api.Sites.GetSitemapAsync(site.Id, false);
            var sitemapItem = FindById(sitemap, page.Id);
            Assert.NotNull(sitemapItem);
            Assert.Equal(newPermalink, sitemapItem!.Permalink);
        }
        finally
        {
            await api.Pages.DeleteAsync(page.Id);
        }
    }

    [Fact]
    public async Task Published_Post_Renders_MetaTitle_And_MetaDescription_And_Resolves_At_Its_Slug()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetTbTruongHocSiteAsync(api);

        var suffix = Guid.NewGuid().ToString("N");

        var metaTitle = $"SEO Post Meta Title {suffix}";
        var metaDescription = $"SEO post meta description for automated test {suffix}.";

        StandardArchive? blog = null;
        StandardPost? post = null;

        try
        {
            blog = await CreatePublishedBlogAsync(api, site, suffix);

            post = await api.Posts.CreateAsync<StandardPost>();
            post.BlogId = blog.Id;
            post.Category = "General";
            post.Title = $"SEO Test Post {suffix}";
            post.MetaTitle = metaTitle;
            post.MetaDescription = metaDescription;
            post.Slug = $"seo-test-post-{suffix}";
            post.Published = DateTime.Now;
            await api.Posts.SaveAsync(post);

            var saved = await api.Posts.GetByIdAsync<StandardPost>(post.Id);
            Assert.NotNull(saved);

            var html = await GetHtmlAsync(saved!.Permalink, HostnameOf(site));

            Assert.Contains($"<title>{metaTitle}</title>", html);
            Assert.Contains(MetaDescriptionTag(metaDescription), html);

            // Story 1.4 (FR-3): same on-page-delivery guard as the Page test
            // above, for StandardPost's own template.
            Assert.Contains("data-quote-request-form", html);
            Assert.Contains("lead-form.js", html);
        }
        finally
        {
            await DeleteBlogAndPostAsync(api, blog, post);
        }
    }

    [Fact]
    public async Task Published_Post_Without_MetaTitle_Falls_Back_To_Title()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetTbTruongHocSiteAsync(api);

        var suffix = Guid.NewGuid().ToString("N");

        var title = $"Post Fallback Title {suffix}";

        StandardArchive? blog = null;
        StandardPost? post = null;

        try
        {
            blog = await CreatePublishedBlogAsync(api, site, suffix);

            post = await api.Posts.CreateAsync<StandardPost>();
            post.BlogId = blog.Id;
            post.Category = "General";
            post.Title = title;
            // MetaTitle deliberately left empty - Views/Cms/Post.cshtml must
            // fall back to Model.Title, same as Page.cshtml.
            post.Slug = $"seo-post-fallback-{suffix}";
            post.Published = DateTime.Now;
            await api.Posts.SaveAsync(post);

            var saved = await api.Posts.GetByIdAsync<StandardPost>(post.Id);
            Assert.NotNull(saved);

            var html = await GetHtmlAsync(saved!.Permalink, HostnameOf(site));

            Assert.Contains($"<title>{title}</title>", html);
        }
        finally
        {
            await DeleteBlogAndPostAsync(api, blog, post);
        }
    }

    [Fact]
    public async Task Changing_Post_Slug_After_Publish_Moves_The_Post()
    {
        using var scope = _factory.Services.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IApi>();
        var site = await GetTbTruongHocSiteAsync(api);

        var suffix = Guid.NewGuid().ToString("N");

        var title = $"Post Slug Change Test {suffix}";
        var oldSlug = $"seo-post-old-slug-{suffix}";
        var newSlug = $"seo-post-new-slug-{suffix}";

        StandardArchive? blog = null;
        StandardPost? post = null;

        try
        {
            blog = await CreatePublishedBlogAsync(api, site, suffix);

            post = await api.Posts.CreateAsync<StandardPost>();
            post.BlogId = blog.Id;
            post.Category = "General";
            post.Title = title;
            post.Slug = oldSlug;
            post.Published = DateTime.Now;
            await api.Posts.SaveAsync(post);

            var afterFirstPublish = await api.Posts.GetByIdAsync<StandardPost>(post.Id);
            Assert.NotNull(afterFirstPublish);

            // Editor changes the slug post-publish.
            afterFirstPublish!.Slug = newSlug;
            await api.Posts.SaveAsync(afterFirstPublish);

            var afterSlugChange = await api.Posts.GetByIdAsync<StandardPost>(post.Id);
            Assert.NotNull(afterSlugChange);
            var newPermalink = afterSlugChange!.Permalink;
            Assert.NotEqual(afterFirstPublish.Permalink, newPermalink);

            var newHtml = await GetHtmlAsync(newPermalink, HostnameOf(site));
            Assert.Contains($"<title>{title}</title>", newHtml);
        }
        finally
        {
            await DeleteBlogAndPostAsync(api, blog, post);
        }
    }

    private static async Task<StandardArchive> CreatePublishedBlogAsync(IApi api, Site site, string suffix)
    {
        var blog = await api.Pages.CreateAsync<StandardArchive>();
        blog.SiteId = site.Id;
        blog.SortOrder = NonStartPageSortOrder;
        blog.Title = $"SEO Test Blog {suffix}";
        blog.Slug = $"seo-test-blog-{suffix}";
        blog.Published = DateTime.Now;
        await api.Pages.SaveAsync(blog);
        return blog;
    }

    /// <summary>
    /// Post deleted before its parent blog Page - Piranha's own FK
    /// relationship between a Post and its owning blog archive Page. Either
    /// may be null when creation failed partway through.
    /// </summary>
    private static async Task DeleteBlogAndPostAsync(IApi api, StandardArchive? blog, StandardPost? post)
    {
        if (post != null)
        {
            await api.Posts.DeleteAsync(post.Id);
        }
        if (blog != null)
        {
            await api.Pages.DeleteAsync(blog.Id);
        }
    }

    private static string MetaDescriptionTag(string metaDescription) =>
        $"<meta name=\"description\" content=\"{metaDescription}\">";

    private static async Task<Site> GetTbTruongHocSiteAsync(IApi api)
    {
        var site = await api.Sites.GetByInternalIdAsync(SiteSeed.TbTruongHocInternalId);
        Assert.NotNull(site);
        return site!;
    }

    /// <summary>
    /// Reads the site's *current* configured hostname rather than assuming
    /// the originally-seeded value, since other tests in this shared,
    /// real-database test run (e.g. SiteSeedIdempotencyTests) are allowed to
    /// mutate a Site's Hostnames.
    /// </summary>
    private static string HostnameOf(Site site)
    {
        var hostname = site.Hostnames?.Split(',').FirstOrDefault()?.Trim();
        Assert.False(string.IsNullOrEmpty(hostname));
        return hostname!;
    }

    private async Task<string> GetHtmlAsync(string permalink, string hostname)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, permalink);
        request.Headers.Host = hostname;

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.Content.ReadAsStringAsync();
    }

    private static SitemapItem? FindById(IEnumerable<SitemapItem> items, Guid id)
    {
        foreach (var item in items)
        {
            if (item.Id == id)
            {
                return item;
            }

            var found = FindById(item.Items, id);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
