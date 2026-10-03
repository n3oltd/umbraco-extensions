using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.DataTypes;

public class RichTextDataTypeDesigner : DataTypeDesigner {
    private const string BlockExtension = "Umb.Tiptap.Block";
    private const string EssentialsExtension = "Umb.Tiptap.RichTextEssentials";

    private static readonly IEnumerable<string> DefaultExtensions = ["Umb.Tiptap.Anchor",
                                                                     "Umb.Tiptap.Blockquote",
                                                                     "Umb.Tiptap.Bold",
                                                                     "Umb.Tiptap.BulletList",
                                                                     "Umb.Tiptap.CodeBlock",
                                                                     "Umb.Tiptap.Embed",
                                                                     "Umb.Tiptap.Figure",
                                                                     "Umb.Tiptap.Heading",
                                                                     "Umb.Tiptap.HorizontalRule",
                                                                     "Umb.Tiptap.HtmlAttributeClass",
                                                                     "Umb.Tiptap.HtmlAttributeDataset",
                                                                     "Umb.Tiptap.HtmlAttributeId",
                                                                     "Umb.Tiptap.HtmlAttributeStyle",
                                                                     "Umb.Tiptap.HtmlTagDiv",
                                                                     "Umb.Tiptap.HtmlTagSpan",
                                                                     "Umb.Tiptap.Image",
                                                                     "Umb.Tiptap.Italic",
                                                                     "Umb.Tiptap.Link",
                                                                     "Umb.Tiptap.MediaUpload",
                                                                     "Umb.Tiptap.OrderedList",
                                                                     "Umb.Tiptap.Strike",
                                                                     "Umb.Tiptap.Subscript",
                                                                     "Umb.Tiptap.Superscript",
                                                                     "Umb.Tiptap.Table",
                                                                     "Umb.Tiptap.TextAlign",
                                                                     "Umb.Tiptap.TextDirection",
                                                                     "Umb.Tiptap.TextIndent",
                                                                     "Umb.Tiptap.TrailingNode",
                                                                     "Umb.Tiptap.Underline",
                                                                     "Umb.Tiptap.WordCount"];
    private static readonly IEnumerable<IEnumerable<string>> DefaultToolbar = [["Umb.Tiptap.Toolbar.SourceEditor"],
                                                                               ["Umb.Tiptap.Toolbar.Bold",
                                                                                "Umb.Tiptap.Toolbar.Italic",
                                                                                "Umb.Tiptap.Toolbar.Underline"],
                                                                               ["Umb.Tiptap.Toolbar.TextAlignLeft",
                                                                                "Umb.Tiptap.Toolbar.TextAlignCenter",
                                                                                "Umb.Tiptap.Toolbar.TextAlignRight"],
                                                                               ["Umb.Tiptap.Toolbar.BulletList",
                                                                                "Umb.Tiptap.Toolbar.OrderedList"],
                                                                               ["Umb.Tiptap.Toolbar.Blockquote",
                                                                                "Umb.Tiptap.Toolbar.HorizontalRule"],
                                                                               ["Umb.Tiptap.Toolbar.Link",
                                                                                "Umb.Tiptap.Toolbar.Unlink"],
                                                                               ["Umb.Tiptap.Toolbar.MediaPicker",
                                                                                "Umb.Tiptap.Toolbar.EmbeddedMedia"]];

    private readonly IContentTypeService _contentTypeService;
    private readonly List<string> _blockElementTypeAliases = [];

    private IEnumerable<string> _extensions = DefaultExtensions;
    private bool _ignoreUserStartNodes;
    private Guid? _mediaParentKey;
    private IEnumerable<IEnumerable<string>> _toolbar = DefaultToolbar;

    public RichTextDataTypeDesigner(IDataTypeService dataTypeService,
                                    IDataTypeContainerService dataTypeContainerService,
                                    IContentTypeService contentTypeService,
                                    PropertyEditorCollection propertyEditors,
                                    IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) {
        _contentTypeService = contentTypeService;
    }

    public RichTextDataTypeDesigner AllowBlocks(params string[] elementTypeAliases) {
        _blockElementTypeAliases.AddRange(elementTypeAliases);

        return this;
    }

    public RichTextDataTypeDesigner Extensions(params string[] extensionAliases) {
        _extensions = extensionAliases;

        return this;
    }

    public RichTextDataTypeDesigner IgnoreUserStartNodes() {
        _ignoreUserStartNodes = true;

        return this;
    }

    public RichTextDataTypeDesigner MediaParent(Guid mediaFolderKey) {
        _mediaParentKey = mediaFolderKey;

        return this;
    }

    public RichTextDataTypeDesigner Toolbar(params IEnumerable<string>[] groups) {
        _toolbar = groups;

        return this;
    }

    protected override object BuildConfiguration(IDataType existing) {
        var configuration = new Dictionary<string, object>();

        configuration["extensions"] = BuildExtensions();
        configuration["ignoreUserStartNodes"] = _ignoreUserStartNodes;
        configuration["maxImageSize"] = 500;
        configuration["overlaySize"] = "medium";
        configuration["toolbar"] = new[] { _toolbar };

        if (_mediaParentKey.HasValue()) {
            configuration["mediaParentId"] = _mediaParentKey.GetValueOrThrow();
        }

        if (!_blockElementTypeAliases.None()) {
            configuration["blocks"] = _blockElementTypeAliases.Select(BuildBlock).ToArray();
        }

        return configuration;
    }

    protected override string EditorAlias => UmbracoPropertyEditors.Aliases.RichText;

    protected override string EditorUiAlias => "Umb.PropertyEditorUi.Tiptap";

    private RichTextConfiguration.RichTextBlockConfiguration BuildBlock(string elementTypeAlias) {
        var block = new RichTextConfiguration.RichTextBlockConfiguration();

        block.ContentElementTypeKey = _contentTypeService.GetOrThrow(elementTypeAlias).Key;

        return block;
    }

    private IReadOnlyList<string> BuildExtensions() {
        var extensions = new List<string>();

        extensions.Add(EssentialsExtension);
        extensions.AddRange(_extensions.Except([BlockExtension, EssentialsExtension]));

        if (!_blockElementTypeAliases.None()) {
            extensions.Add(BlockExtension);
        }

        return extensions;
    }
}
