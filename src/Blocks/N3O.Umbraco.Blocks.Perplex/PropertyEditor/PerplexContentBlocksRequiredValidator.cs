using N3O.Umbraco.Extensions;
using Perplex.ContentBlocks.PropertyEditor.ModelValue;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Umbraco.Cms.Core.PropertyEditors.Validators;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Blocks.Perplex;

public class PerplexContentBlocksRequiredValidator : RequiredValidator {
    private readonly ContentBlocksModelValueDeserializer _deserializer;

    public PerplexContentBlocksRequiredValidator(ILocalizedTextService textService,
                                                 ContentBlocksModelValueDeserializer deserializer)
        : base(textService) {
        _deserializer = deserializer;
    }

    // Content Blocks stores a versioned envelope, so a property holding nothing still reads as
    // {"version":3,"header":null,"blocks":[]}, and the base validator counts only "{}" and "[]" as empty JSON.
    public override IEnumerable<ValidationResult> ValidateRequired(object value, string valueType) {
        return base.ValidateRequired(IsEmpty(value) ? "{}" : value, valueType);
    }

    private bool IsEmpty(object value) {
        var modelValue = _deserializer.Deserialize(value?.ToString());

        if (modelValue == null) {
            return false;
        }

        return modelValue.Header == null && modelValue.Blocks.None();
    }
}
