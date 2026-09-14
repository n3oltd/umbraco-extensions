using N3O.Umbraco.ContentTypes;
using System;
using System.Linq.Expressions;

namespace N3O.Umbraco.Extensions;

public static class PropertyContainerBuilderExtensions {
    public static BlockGridPropertyTypeBuilder BlockGrid(this IPropertyContainerBuilder builder, string propertyAlias) {
        return builder.Property<BlockGridPropertyTypeBuilder>(propertyAlias);
    }

    public static BlockGridPropertyTypeBuilder BlockGrid<T, TProperty>(this IPropertyContainerBuilder<T> builder,
                                                                       Expression<Func<T, TProperty>> expression) {
        return builder.Property<BlockGridPropertyTypeBuilder, TProperty>(expression);
    }

    public static BlockListPropertyTypeBuilder BlockList(this IPropertyContainerBuilder builder, string propertyAlias) {
        return builder.Property<BlockListPropertyTypeBuilder>(propertyAlias);
    }

    public static BlockListPropertyTypeBuilder BlockList<T, TProperty>(this IPropertyContainerBuilder<T> builder,
                                                                       Expression<Func<T, TProperty>> expression) {
        return builder.Property<BlockListPropertyTypeBuilder, TProperty>(expression);
    }

    public static CheckBoxListPropertyTypeBuilder CheckBoxList(this IPropertyContainerBuilder builder,
                                                               string propertyAlias) {
        return builder.Property<CheckBoxListPropertyTypeBuilder>(propertyAlias);
    }

    public static CheckBoxListPropertyTypeBuilder CheckBoxList<T, TProperty>(
        this IPropertyContainerBuilder<T> builder,
        Expression<Func<T, TProperty>> expression) {
        return builder.Property<CheckBoxListPropertyTypeBuilder, TProperty>(expression);
    }

    public static ColorPickerPropertyTypeBuilder ColorPicker(this IPropertyContainerBuilder builder,
                                                             string propertyAlias) {
        return builder.Property<ColorPickerPropertyTypeBuilder>(propertyAlias);
    }

    public static ColorPickerPropertyTypeBuilder ColorPicker<T, TProperty>(this IPropertyContainerBuilder<T> builder,
                                                                           Expression<Func<T, TProperty>> expression) {
        return builder.Property<ColorPickerPropertyTypeBuilder, TProperty>(expression);
    }

    public static ContentPickerPropertyTypeBuilder ContentPicker(this IPropertyContainerBuilder builder,
                                                                 string propertyAlias) {
        return builder.Property<ContentPickerPropertyTypeBuilder>(propertyAlias);
    }

    public static ContentPickerPropertyTypeBuilder ContentPicker<T, TProperty>(
        this IPropertyContainerBuilder<T> builder,
        Expression<Func<T, TProperty>> expression) {
        return builder.Property<ContentPickerPropertyTypeBuilder, TProperty>(expression);
    }

    public static DatePropertyTypeBuilder Date(this IPropertyContainerBuilder builder, string propertyAlias) {
        return builder.Property<DatePropertyTypeBuilder>(propertyAlias);
    }

    public static DatePropertyTypeBuilder Date<T, TProperty>(this IPropertyContainerBuilder<T> builder,
                                                             Expression<Func<T, TProperty>> expression) {
        return builder.Property<DatePropertyTypeBuilder, TProperty>(expression);
    }

    public static DateTimePropertyTypeBuilder DateTime(this IPropertyContainerBuilder builder, string propertyAlias) {
        return builder.Property<DateTimePropertyTypeBuilder>(propertyAlias);
    }

    public static DateTimePropertyTypeBuilder DateTime<T, TProperty>(this IPropertyContainerBuilder<T> builder,
                                                                     Expression<Func<T, TProperty>> expression) {
        return builder.Property<DateTimePropertyTypeBuilder, TProperty>(expression);
    }

    public static DecimalPropertyTypeBuilder Decimal(this IPropertyContainerBuilder builder, string propertyAlias) {
        return builder.Property<DecimalPropertyTypeBuilder>(propertyAlias);
    }

    public static DecimalPropertyTypeBuilder Decimal<T, TProperty>(this IPropertyContainerBuilder<T> builder,
                                                                   Expression<Func<T, TProperty>> expression) {
        return builder.Property<DecimalPropertyTypeBuilder, TProperty>(expression);
    }

    public static DropdownPropertyTypeBuilder Dropdown(this IPropertyContainerBuilder builder, string propertyAlias) {
        return builder.Property<DropdownPropertyTypeBuilder>(propertyAlias);
    }

    public static DropdownPropertyTypeBuilder Dropdown<T, TProperty>(this IPropertyContainerBuilder<T> builder,
                                                                     Expression<Func<T, TProperty>> expression) {
        return builder.Property<DropdownPropertyTypeBuilder, TProperty>(expression);
    }

    public static EmailPropertyTypeBuilder Email(this IPropertyContainerBuilder builder, string propertyAlias) {
        return builder.Property<EmailPropertyTypeBuilder>(propertyAlias);
    }

    public static EmailPropertyTypeBuilder Email<T, TProperty>(this IPropertyContainerBuilder<T> builder,
                                                               Expression<Func<T, TProperty>> expression) {
        return builder.Property<EmailPropertyTypeBuilder, TProperty>(expression);
    }

    public static EyeDropperPropertyTypeBuilder EyeDropper(this IPropertyContainerBuilder builder,
                                                           string propertyAlias) {
        return builder.Property<EyeDropperPropertyTypeBuilder>(propertyAlias);
    }

    public static EyeDropperPropertyTypeBuilder EyeDropper<T, TProperty>(this IPropertyContainerBuilder<T> builder,
                                                                         Expression<Func<T, TProperty>> expression) {
        return builder.Property<EyeDropperPropertyTypeBuilder, TProperty>(expression);
    }

    public static ImageCropperPropertyTypeBuilder ImageCropper(this IPropertyContainerBuilder builder,
                                                               string propertyAlias) {
        return builder.Property<ImageCropperPropertyTypeBuilder>(propertyAlias);
    }

    public static ImageCropperPropertyTypeBuilder ImageCropper<T, TProperty>(
        this IPropertyContainerBuilder<T> builder,
        Expression<Func<T, TProperty>> expression) {
        return builder.Property<ImageCropperPropertyTypeBuilder, TProperty>(expression);
    }

    public static LabelPropertyTypeBuilder Label(this IPropertyContainerBuilder builder, string propertyAlias) {
        return builder.Property<LabelPropertyTypeBuilder>(propertyAlias);
    }

    public static LabelPropertyTypeBuilder Label<T, TProperty>(this IPropertyContainerBuilder<T> builder,
                                                               Expression<Func<T, TProperty>> expression) {
        return builder.Property<LabelPropertyTypeBuilder, TProperty>(expression);
    }

    public static MarkdownPropertyTypeBuilder Markdown(this IPropertyContainerBuilder builder, string propertyAlias) {
        return builder.Property<MarkdownPropertyTypeBuilder>(propertyAlias);
    }

    public static MarkdownPropertyTypeBuilder Markdown<T, TProperty>(this IPropertyContainerBuilder<T> builder,
                                                                     Expression<Func<T, TProperty>> expression) {
        return builder.Property<MarkdownPropertyTypeBuilder, TProperty>(expression);
    }

    public static MediaPickerPropertyTypeBuilder MediaPicker(this IPropertyContainerBuilder builder,
                                                             string propertyAlias) {
        return builder.Property<MediaPickerPropertyTypeBuilder>(propertyAlias);
    }

    public static MediaPickerPropertyTypeBuilder MediaPicker<T, TProperty>(this IPropertyContainerBuilder<T> builder,
                                                                           Expression<Func<T, TProperty>> expression) {
        return builder.Property<MediaPickerPropertyTypeBuilder, TProperty>(expression);
    }

    public static MemberGroupPickerPropertyTypeBuilder MemberGroupPicker(this IPropertyContainerBuilder builder,
                                                                         string propertyAlias) {
        return builder.Property<MemberGroupPickerPropertyTypeBuilder>(propertyAlias);
    }

    public static MemberGroupPickerPropertyTypeBuilder MemberGroupPicker<T, TProperty>(
        this IPropertyContainerBuilder<T> builder,
        Expression<Func<T, TProperty>> expression) {
        return builder.Property<MemberGroupPickerPropertyTypeBuilder, TProperty>(expression);
    }

    public static MemberPickerPropertyTypeBuilder MemberPicker(this IPropertyContainerBuilder builder,
                                                               string propertyAlias) {
        return builder.Property<MemberPickerPropertyTypeBuilder>(propertyAlias);
    }

    public static MemberPickerPropertyTypeBuilder MemberPicker<T, TProperty>(
        this IPropertyContainerBuilder<T> builder,
        Expression<Func<T, TProperty>> expression) {
        return builder.Property<MemberPickerPropertyTypeBuilder, TProperty>(expression);
    }

    public static MultiNodeTreePickerPropertyTypeBuilder MultiNodeTreePicker(this IPropertyContainerBuilder builder,
                                                                             string propertyAlias) {
        return builder.Property<MultiNodeTreePickerPropertyTypeBuilder>(propertyAlias);
    }

    public static MultiNodeTreePickerPropertyTypeBuilder MultiNodeTreePicker<T, TProperty>(
        this IPropertyContainerBuilder<T> builder,
        Expression<Func<T, TProperty>> expression) {
        return builder.Property<MultiNodeTreePickerPropertyTypeBuilder, TProperty>(expression);
    }

    public static MultipleTextStringPropertyTypeBuilder MultipleTextString(this IPropertyContainerBuilder builder,
                                                                           string propertyAlias) {
        return builder.Property<MultipleTextStringPropertyTypeBuilder>(propertyAlias);
    }

    public static MultipleTextStringPropertyTypeBuilder MultipleTextString<T, TProperty>(
        this IPropertyContainerBuilder<T> builder,
        Expression<Func<T, TProperty>> expression) {
        return builder.Property<MultipleTextStringPropertyTypeBuilder, TProperty>(expression);
    }

    public static MultiUrlPickerPropertyTypeBuilder MultiUrlPicker(this IPropertyContainerBuilder builder,
                                                                   string propertyAlias) {
        return builder.Property<MultiUrlPickerPropertyTypeBuilder>(propertyAlias);
    }

    public static MultiUrlPickerPropertyTypeBuilder MultiUrlPicker<T, TProperty>(
        this IPropertyContainerBuilder<T> builder,
        Expression<Func<T, TProperty>> expression) {
        return builder.Property<MultiUrlPickerPropertyTypeBuilder, TProperty>(expression);
    }

    public static NumericPropertyTypeBuilder Numeric(this IPropertyContainerBuilder builder, string propertyAlias) {
        return builder.Property<NumericPropertyTypeBuilder>(propertyAlias);
    }

    public static NumericPropertyTypeBuilder Numeric<T, TProperty>(this IPropertyContainerBuilder<T> builder,
                                                                   Expression<Func<T, TProperty>> expression) {
        return builder.Property<NumericPropertyTypeBuilder, TProperty>(expression);
    }

    public static RadioButtonListPropertyTypeBuilder RadioButtonList(this IPropertyContainerBuilder builder,
                                                                     string propertyAlias) {
        return builder.Property<RadioButtonListPropertyTypeBuilder>(propertyAlias);
    }

    public static RadioButtonListPropertyTypeBuilder RadioButtonList<T, TProperty>(
        this IPropertyContainerBuilder<T> builder,
        Expression<Func<T, TProperty>> expression) {
        return builder.Property<RadioButtonListPropertyTypeBuilder, TProperty>(expression);
    }

    public static RichTextPropertyTypeBuilder RichText(this IPropertyContainerBuilder builder, string propertyAlias) {
        return builder.Property<RichTextPropertyTypeBuilder>(propertyAlias);
    }

    public static RichTextPropertyTypeBuilder RichText<T, TProperty>(this IPropertyContainerBuilder<T> builder,
                                                                     Expression<Func<T, TProperty>> expression) {
        return builder.Property<RichTextPropertyTypeBuilder, TProperty>(expression);
    }

    public static SliderPropertyTypeBuilder Slider(this IPropertyContainerBuilder builder, string propertyAlias) {
        return builder.Property<SliderPropertyTypeBuilder>(propertyAlias);
    }

    public static SliderPropertyTypeBuilder Slider<T, TProperty>(this IPropertyContainerBuilder<T> builder,
                                                                 Expression<Func<T, TProperty>> expression) {
        return builder.Property<SliderPropertyTypeBuilder, TProperty>(expression);
    }

    public static TagsPropertyTypeBuilder Tags(this IPropertyContainerBuilder builder, string propertyAlias) {
        return builder.Property<TagsPropertyTypeBuilder>(propertyAlias);
    }

    public static TagsPropertyTypeBuilder Tags<T, TProperty>(this IPropertyContainerBuilder<T> builder,
                                                             Expression<Func<T, TProperty>> expression) {
        return builder.Property<TagsPropertyTypeBuilder, TProperty>(expression);
    }

    public static TextareaPropertyTypeBuilder Textarea(this IPropertyContainerBuilder builder, string propertyAlias) {
        return builder.Property<TextareaPropertyTypeBuilder>(propertyAlias);
    }

    public static TextareaPropertyTypeBuilder Textarea<T, TProperty>(this IPropertyContainerBuilder<T> builder,
                                                                     Expression<Func<T, TProperty>> expression) {
        return builder.Property<TextareaPropertyTypeBuilder, TProperty>(expression);
    }

    public static TextBoxPropertyTypeBuilder TextBox(this IPropertyContainerBuilder builder, string propertyAlias) {
        return builder.Property<TextBoxPropertyTypeBuilder>(propertyAlias);
    }

    public static TextBoxPropertyTypeBuilder TextBox<T, TProperty>(this IPropertyContainerBuilder<T> builder,
                                                                   Expression<Func<T, TProperty>> expression) {
        return builder.Property<TextBoxPropertyTypeBuilder, TProperty>(expression);
    }

    public static TogglePropertyTypeBuilder Toggle(this IPropertyContainerBuilder builder, string propertyAlias) {
        return builder.Property<TogglePropertyTypeBuilder>(propertyAlias);
    }

    public static TogglePropertyTypeBuilder Toggle<T, TProperty>(this IPropertyContainerBuilder<T> builder,
                                                                 Expression<Func<T, TProperty>> expression) {
        return builder.Property<TogglePropertyTypeBuilder, TProperty>(expression);
    }

    public static UploadPropertyTypeBuilder Upload(this IPropertyContainerBuilder builder, string propertyAlias) {
        return builder.Property<UploadPropertyTypeBuilder>(propertyAlias);
    }

    public static UploadPropertyTypeBuilder Upload<T, TProperty>(this IPropertyContainerBuilder<T> builder,
                                                                 Expression<Func<T, TProperty>> expression) {
        return builder.Property<UploadPropertyTypeBuilder, TProperty>(expression);
    }

    public static UserPickerPropertyTypeBuilder UserPicker(this IPropertyContainerBuilder builder,
                                                           string propertyAlias) {
        return builder.Property<UserPickerPropertyTypeBuilder>(propertyAlias);
    }

    public static UserPickerPropertyTypeBuilder UserPicker<T, TProperty>(this IPropertyContainerBuilder<T> builder,
                                                                         Expression<Func<T, TProperty>> expression) {
        return builder.Property<UserPickerPropertyTypeBuilder, TProperty>(expression);
    }
}
