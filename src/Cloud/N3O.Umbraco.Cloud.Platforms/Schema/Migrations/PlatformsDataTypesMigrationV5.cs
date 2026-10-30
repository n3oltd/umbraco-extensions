using N3O.Umbraco.DataTypes;
using N3O.Umbraco.Extensions;
using Umbraco.Cms.Infrastructure.Migrations;
using DataTypeNames = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.DataTypes;

namespace N3O.Umbraco.Cloud.Platforms;

public class PlatformsDataTypesMigrationV5 : MigrationBase {
    private readonly IDataTypeEditor _dataTypeEditor;

    public PlatformsDataTypesMigrationV5(IMigrationContext context, IDataTypeEditor dataTypeEditor)
        : base(context) {
        _dataTypeEditor = dataTypeEditor;
    }

    protected override void Migrate() {
        EscapeElementEmbedCode();
    }

    private void EscapeElementEmbedCode() {
        var designer = _dataTypeEditor.NewContentmentTemplatedLabel(DataTypeNames.ElementEmbedCodeLabel);

        designer.Template("<label>{{ model.value | escape }}</label>");
        designer.WithDeterministicId(DataTypeNames.ElementEmbedCodeLabel);

        designer.Save();
    }
}
