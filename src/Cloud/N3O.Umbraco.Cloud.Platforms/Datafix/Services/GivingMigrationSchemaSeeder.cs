using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.DataTypes;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Services;
using DataTypeNames = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.DataTypes;

namespace N3O.Umbraco.Cloud.Platforms;

// A site that already holds the platforms types was seeded before the donation form picker existed, and the schema
// component only runs where the platforms feature is switched on, so the migration cannot assume the picker is there.
// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public class GivingMigrationSchemaSeeder : IGivingMigrationSchemaSeeder {
    private static readonly string[] ElementTypeAliases = [
        PlatformsConstants.DonationFormItems.Campaign,
        PlatformsConstants.DonationFormItems.Offering
    ];

    private readonly IContentTypeService _contentTypeService;
    private readonly IPlatformsContentTypeSeeder _contentTypeSeeder;
    private readonly IDataTypeEditor _dataTypeEditor;
    private readonly IPlatformsDataTypeSeeder _dataTypeSeeder;

    public GivingMigrationSchemaSeeder(IContentTypeService contentTypeService,
                                       IPlatformsContentTypeSeeder contentTypeSeeder,
                                       IDataTypeEditor dataTypeEditor,
                                       IPlatformsDataTypeSeeder dataTypeSeeder) {
        _contentTypeService = contentTypeService;
        _contentTypeSeeder = contentTypeSeeder;
        _dataTypeEditor = dataTypeEditor;
        _dataTypeSeeder = dataTypeSeeder;
    }

    public GivingMigrationSeedRes Seed() {
        var res = new GivingMigrationSeedRes();

        res.DataTypeName = DataTypeNames.DonationFormList;
        res.AlreadyExisted = _dataTypeEditor.Find(res.DataTypeName) != null;

        // The order is the one the schema component uses: the element types hold properties bound to the data
        // lists by name and are skipped while those are absent, and the block list variant of the picker stores
        // the element types by key and so cannot be created before them
        _dataTypeSeeder.Seed();
        _contentTypeSeeder.Seed();
        _dataTypeSeeder.SeedDonationFormList();

        var dataType = _dataTypeEditor.Find(res.DataTypeName);

        res.Exists = dataType != null;
        res.PropertyEditorAlias = dataType?.EditorAlias;
        res.MissingElementTypes = GetMissingElementTypes();

        if (!res.Exists) {
            res.Message = "The donation form picker could not be seeded, so the properties holding a legacy form " +
                          "cannot be repointed";
        }

        return res;
    }

    private IReadOnlyList<string> GetMissingElementTypes() {
        return ElementTypeAliases.Where(x => _contentTypeService.Get(x) == null).ToList();
    }
}
