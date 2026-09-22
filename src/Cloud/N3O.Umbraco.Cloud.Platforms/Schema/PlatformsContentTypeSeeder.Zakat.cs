using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.ContentTypes;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Allocations.Lookups;
using N3O.Umbraco.Giving.Allocations.Models;
using System;
using System.Linq;
using System.Linq.Expressions;
using Folders = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Folders;
using Groups = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Groups;
using Shared = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.SharedDataTypes;

namespace N3O.Umbraco.Cloud.Platforms;

public partial class PlatformsContentTypeSeeder {
    private string BlockContentDataType() {
        return new[] { Shared.PageBlockGrid, Shared.PerplexBlocks }.FirstOrDefault(HasDataType);
    }

    private void SeedZakatCalculatorField() {
        var designer = _contentTypeEditor.NewDocument<ZakatCalculatorFieldSettingsContent>();

        designer.SetName("Zakat Calculator Field");
        designer.InFolder(Folders.Platforms, Folders.Zakat, Folders.Calculator);
        designer.WithDeterministicId();

        var general = designer.Group(Groups.General);

        general.TextBox(x => x.Alias).DataType(Shared.TextBox).Mandatory();
        general.ContentmentDataList(x => x.Classification).DataType(Shared.ZakatFieldClassification).Mandatory();
        general.TextBox(x => x.Tooltip).DataType(Shared.TextBox);
        general.ContentmentDataList(x => x.Type).DataType(Shared.ZakatFieldType).Mandatory();
        general.ContentmentDataList(x => x.Metal).DataType(Shared.Metal);

        var blockContent = BlockContentDataType();

        if (blockContent.HasValue()) {
            general.BlockGrid(x => x.Content).DataType(blockContent);
        }

        designer.Save();
    }

    private void SeedZakatCalculatorSection() {
        var designer = _contentTypeEditor.NewDocument<ZakatCalculatorSectionSettingsContent>();

        designer.SetName("Zakat Calculator Section");
        designer.InFolder(Folders.Platforms, Folders.Zakat, Folders.Calculator);
        designer.WithDeterministicId();
        designer.AllowChildren(PlatformsConstants.Zakat.Settings.Calculator.Field.Alias);

        var general = designer.Group(Groups.General);

        general.TextBox(x => x.Alias).DataType(Shared.TextBox).Mandatory();

        var blockContent = BlockContentDataType();

        if (blockContent.HasValue()) {
            general.BlockGrid(x => x.Content).DataType(blockContent);
        }

        designer.Save();
    }

    private void SeedZakatCalculatorSettings() {
        var designer = _contentTypeEditor.NewDocument<ZakatCalculatorSettingsContent>();

        designer.SetName("Zakat Calculator");
        designer.InFolder(Folders.Platforms, Folders.Zakat, Folders.Calculator);
        designer.WithDeterministicId();
        designer.AllowChildren(PlatformsConstants.Zakat.Settings.Calculator.Section.Alias);

        var defaults = designer.Group(Groups.Defaults);

        defaults.MultiNodeTreePicker(x => x.Offering).DataType(Shared.OfferingPicker).Mandatory();
        defaults.ContentmentDataList(x => x.DefaultNisabType).DataType(Shared.NisabType).Mandatory();

        var blockContent = BlockContentDataType();

        if (blockContent.HasValue()) {
            defaults.BlockGrid(x => x.DefaultContent).DataType(blockContent).Mandatory();
        }

        designer.Group(Groups.Settings)
                .TextBox(x => x.EmailCompositionId)
                .DataType(Shared.TextBox)
                .Mandatory();

        var filters = designer.Group(Groups.Filters);

        SeedZakatFundDimension(filters, x => x.FundDimension1, Shared.FundDimension1);
        SeedZakatFundDimension(filters, x => x.FundDimension2, Shared.FundDimension2);
        SeedZakatFundDimension(filters, x => x.FundDimension3, Shared.FundDimension3);
        SeedZakatFundDimension(filters, x => x.FundDimension4, Shared.FundDimension4);

        designer.Save();
    }

    private void SeedZakatFundDimension(IPropertyContainerBuilder<ZakatCalculatorSettingsContent> container,
                                        Expression<Func<ZakatCalculatorSettingsContent, IFundDimensionValue>> expression,
                                        string dataTypeName) {
        if (HasDataType(dataTypeName)) {
            container.ContentmentDataList(expression).DataType(dataTypeName);
        }
    }
}
