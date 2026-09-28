using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Piranha.Extend;
using Piranha.Extend.Fields;

namespace TbTruongHoc.Web.Models;

/// <summary>
/// Story 4.2 (FR-11): "Bảng cấu hình" - a reusable block group an editor
/// places among a page's blocks (ProductArchive, ProductPost, LandingPage).
/// An optional heading plus <see cref="ConfigRowBlock"/> rows, each with an
/// editor-chosen input type. Rendered by
/// <c>Views/Cms/DisplayTemplates/ConfigBlock.cshtml</c>; the visitor's
/// selections are prepended to the page's one quote form's message
/// (sb-config.js + lead-form.js). Never a price, cart or configurator.
/// Replaces Story 4.1's DrumConfig regions on <see cref="ProductArchive"/>.
/// </summary>
[BlockGroupType(Name = "Bảng cấu hình", Category = "Sản phẩm", Icon = "fas fa-sliders-h")]
[BlockItemType(typeof(ConfigRowBlock))]
public class ConfigBlock : BlockGroup
{
    /// <summary>The h2 when the editor leaves the heading blank.</summary>
    public const string DefaultHeading = "Bảng cấu hình";

    /// <summary>The disclaimer shown once under the rows.</summary>
    public const string Disclaimer = "Bảng tham khảo, không tính giá tự động";

    /// <summary>
    /// ViewData key a host view sets before its block loop so the template
    /// renders at all. Any value = "feed the page's existing quote form".
    /// </summary>
    public const string HostViewDataKey = "ConfigBlockHost";

    /// <summary>
    /// ViewData key an archive host sets (to the form's prefilled product) so
    /// the block brings its own quote form.
    /// </summary>
    public const string OwnFormViewDataKey = "ConfigBlockOwnForm";

    // Fields start non-null: Piranha's block serializer throws on a null
    // field, e.g. a block built in code without setting every field.
    [Field(Title = "Tiêu đề", Placeholder = DefaultHeading, Description = "Chỉ hiển thị trên trang Danh mục sản phẩm, trang Sản phẩm và Landing page. Để trống thì hiện \"Bảng cấu hình\".")]
    public StringField Heading { get; set; } = new();

    /// <summary>The heading, trimmed, or <see cref="DefaultHeading"/> when blank.</summary>
    public string HeadingText =>
        string.IsNullOrWhiteSpace(Heading?.Value) ? DefaultHeading : Heading.Value.Trim();

    /// <summary>Valid rows, in editor order (see <see cref="ConfigRow.From"/>).</summary>
    public IReadOnlyList<ConfigRow> ValidRows =>
        (Items ?? new List<Block>())
            .OfType<ConfigRowBlock>()
            .Select(ConfigRow.From)
            .Where(r => r != null)
            .ToList();

    /// <summary>
    /// The block that renders on a page: the first config block with valid
    /// rows, or null. Only top-level blocks count - Piranha never nests a
    /// block group inside another group.
    /// </summary>
    public static ConfigBlock FirstRenderable(IEnumerable<Block> blocks) =>
        (blocks ?? Enumerable.Empty<Block>())
            .OfType<ConfigBlock>()
            .FirstOrDefault(b => b.ValidRows.Count > 0);
}

/// <summary>Story 4.2: the input type an editor picks for a config row.</summary>
// Text is first (0) so a new row, or an unreadable stored value, defaults to
// a valid row rather than a hidden Chỉ hiển thị row with no value.
public enum ConfigInputType
{
    [Display(Description = "Text")]
    Text,

    [Display(Description = "Chỉ hiển thị")]
    Display,

    [Display(Description = "Option")]
    Option,

    [Display(Description = "Check")]
    Check,

    [Display(Description = "Multi-option")]
    MultiOption
}

/// <summary>Story 4.2: one config row inside a <see cref="ConfigBlock"/>.</summary>
[BlockType(Name = "Dòng cấu hình", Category = "Sản phẩm", Icon = "fas fa-list", IsUnlisted = true, IsGeneric = true, ListTitle = "Label")]
public class ConfigRowBlock : Block
{
    [Field(Title = "Hạng mục", Description = "Ví dụ \"Kích thước\", \"Loại\", \"Bánh xe\". Để trống thì dòng bị ẩn.")]
    public StringField Label { get; set; } = new();

    [Field(Title = "Kiểu nhập", Description = "Chỉ hiển thị: chỉ đọc. Text: ô nhập chữ. Option: chọn một. Check: có/không. Multi-option: chọn nhiều.")]
    public SelectField<ConfigInputType> InputType { get; set; } = new();

    [Field(Title = "Lựa chọn / Nội dung", Description = "Option, Multi-option: mỗi dòng một lựa chọn (cần ít nhất một). Chỉ hiển thị: nội dung hiển thị (bắt buộc). Text, Check: bỏ trống.")]
    public TextField Choices { get; set; } = new();
}

/// <summary>
/// Story 4.2: a validated, trimmed config row ready to render. Views and
/// tests share this so the validity rules live in one place.
/// </summary>
public sealed class ConfigRow
{
    public string Label { get; private init; }

    public ConfigInputType Type { get; private init; }

    /// <summary>Choice lines (Option/Multi-option), trimmed, non-blank, editor order.</summary>
    public IReadOnlyList<string> Choices { get; private init; } = Array.Empty<string>();

    /// <summary>The read-only value (Chỉ hiển thị), trimmed.</summary>
    public string Value { get; private init; }

    /// <summary>
    /// The valid row for <paramref name="block"/>, or null when it is invalid:
    /// blank label; Chỉ hiển thị with a blank value; Option/Multi-option with
    /// no non-blank choice.
    /// </summary>
    public static ConfigRow From(ConfigRowBlock block)
    {
        var label = block?.Label?.Value;
        if (string.IsNullOrWhiteSpace(label))
        {
            return null;
        }

        var type = block.InputType?.Value ?? default;
        if (!Enum.IsDefined(type))
        {
            return null;
        }

        var raw = block.Choices?.Value ?? "";
        var row = new ConfigRow { Label = label.Trim(), Type = type };

        switch (type)
        {
            case ConfigInputType.Display:
                return string.IsNullOrWhiteSpace(raw) ? null : new ConfigRow { Label = row.Label, Type = type, Value = raw.Trim() };
            case ConfigInputType.Option:
            case ConfigInputType.MultiOption:
                var choices = raw.Split('\n')
                    .Select(c => c.Trim())
                    .Where(c => c.Length > 0)
                    .ToList();
                return choices.Count == 0 ? null : new ConfigRow { Label = row.Label, Type = type, Choices = choices };
            default:
                return row;
        }
    }
}
