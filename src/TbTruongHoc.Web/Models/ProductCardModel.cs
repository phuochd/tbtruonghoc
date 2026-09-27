namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 2.2: input for <c>Views/Shared/_ProductCard.cshtml</c> - the product
/// plus its eyebrow (the owning archive's title).
/// </summary>
public sealed record ProductCardModel(ProductPost Post, string Eyebrow);
