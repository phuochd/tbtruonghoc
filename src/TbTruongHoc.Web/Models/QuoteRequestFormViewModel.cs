namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 1.4 (FR-3): the optional parameters <c>_QuoteRequestForm.cshtml</c>
/// accepts when included from a view, e.g.
/// <c>@await Html.PartialAsync("_QuoteRequestForm", new QuoteRequestFormViewModel { ProductOfInterest = "..." })</c>.
/// Both properties are optional - the partial defaults <see cref="FormType"/>
/// to "general" and leaves <see cref="ProductOfInterest"/> blank when the
/// caller (or the model itself, when included with no model at all) doesn't
/// supply one. Later epics' product/category page templates (Epic 2/3/6,
/// per this story's Boundaries) will pass a real product name here.
/// </summary>
public class QuoteRequestFormViewModel
{
    public string ProductOfInterest { get; set; }

    public string FormType { get; set; }
}
