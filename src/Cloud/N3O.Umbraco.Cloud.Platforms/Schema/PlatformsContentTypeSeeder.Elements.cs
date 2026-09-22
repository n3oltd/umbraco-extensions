using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Content;
using N3O.Umbraco.ContentTypes;
using N3O.Umbraco.Extensions;
using System;
using System.Linq.Expressions;
using DataTypeNames = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.DataTypes;
using Folders = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Folders;
using Groups = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Groups;
using Shared = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.SharedDataTypes;

namespace N3O.Umbraco.Cloud.Platforms;

public partial class PlatformsContentTypeSeeder {
    private static string Of<T, TProperty>(Expression<Func<T, TProperty>> expression) {
        return AliasHelper<T>.PropertyAlias(expression);
    }

    private void SeedDonationFormContent() {
        var designer = _contentTypeEditor.NewElement("Donation Form Content",
                                                     PlatformsConstants.DonationFormContent.CompositionAlias);

        designer.InFolder(Folders.Platforms, Folders.DonationForms);
        designer.WithDeterministicId();

        var general = designer.Group(Groups.General);

        general.MediaPicker(Of((DonationFormContentContent x) => x.Image))
               .DataType(Shared.ImageMediaPicker)
               .Mandatory();

        general.MediaPicker(Of((DonationFormContentContent x) => x.Icon))
               .DataType(Shared.IconMediaPicker)
               .Mandatory();

        general.Markdown(Of((DonationFormContentContent x) => x.Description))
               .DataType(Shared.Markdown)
               .Mandatory();

        general.Textarea(Of((DonationFormContentContent x) => x.Summary))
               .DataType(DataTypeNames.Summary)
               .Mandatory();

        designer.Save();
    }

    private void SeedDonationFormState() {
        var designer = _contentTypeEditor.NewElement("Donation Form State",
                                                     PlatformsConstants.DonationFormState.CompositionAlias);

        designer.InFolder(Folders.Platforms, Folders.DonationForms);
        designer.WithDeterministicId();

        designer.Group(Groups.Suggestions)
                .ContentmentDataList(Of((DonationFormStateContent x) => x.SuggestedGiftType))
                .DataType(Shared.GiftType);

        var options = designer.Group(Groups.Options);

        options.TextBox(Of((DonationFormStateContent x) => x.NotesLabel)).DataType(Shared.TextBox);
        options.ContentmentDataList(Of((DonationFormStateContent x) => x.Dimension1)).DataType(Shared.FundDimension1);
        options.ContentmentDataList(Of((DonationFormStateContent x) => x.Dimension2)).DataType(Shared.FundDimension2);
        options.ContentmentDataList(Of((DonationFormStateContent x) => x.Dimension3)).DataType(Shared.FundDimension3);

        if (HasDataType(Shared.FundDimension4)) {
            options.ContentmentDataList(Of((DonationFormStateContent x) => x.Dimension4))
                   .DataType(Shared.FundDimension4);
        }

        options.Textarea(Of((DonationFormStateContent x) => x.CustomFormState)).DataType(Shared.Textarea);

        designer.Save();
    }

    private void SeedFeedbackDonationFormState() {
        var designer = _contentTypeEditor.NewElement("Feedback Donation Form State",
                                                     PlatformsConstants.DonationFormState.Feedback);

        designer.InFolder(Folders.Platforms, Folders.DonationForms);
        designer.WithDeterministicId();

        designer.Group(Groups.General)
                .ContentmentDataList(Of((FeedbackDonationFormStateContent x) => x.Scheme))
                .DataType(Shared.FeedbackScheme)
                .Mandatory();

        designer.Save();
    }

    private void SeedFundDonationFormState() {
        var designer = _contentTypeEditor.NewElement("Fund Donation Form State",
                                                     PlatformsConstants.DonationFormState.Fund);

        designer.InFolder(Folders.Platforms, Folders.DonationForms);
        designer.WithDeterministicId();

        designer.Group(Groups.General)
                .ContentmentDataList(Of((FundDonationFormStateContent x) => x.DonationItem))
                .DataType(Shared.DonationItem)
                .Mandatory();

        var suggestions = designer.Group(Groups.Suggestions);

        suggestions.NestedContent(Of((FundDonationFormStateContent x) => x.OneTimeSuggestedAmounts))
                   .DataType(DataTypeNames.SuggestedAmounts);

        suggestions.NestedContent(Of((FundDonationFormStateContent x) => x.RecurringSuggestedAmounts))
                   .DataType(DataTypeNames.SuggestedAmounts);

        designer.Save();
    }

    private void SeedQurbaniDonationFormState() {
        var designer = _contentTypeEditor.NewElement("Qurbani Donation Form State",
                                                     PlatformsConstants.DonationFormState.Qurbani);

        designer.InFolder(Folders.Platforms, Folders.DonationForms);
        designer.WithDeterministicId();

        var general = designer.Group(Groups.General);

        general.ContentmentDataList(Of((QurbaniDonationFormStateContent x) => x.QurbaniItem))
               .DataType(DataTypeNames.QurbaniItem)
               .Mandatory();

        general.MultiNodeTreePicker(Of((QurbaniDonationFormStateContent x) => x.Categories))
               .DataType(DataTypeNames.QurbaniSeasonCategoryPicker)
               .Mandatory();

        designer.Save();
    }

    private void SeedSponsorshipDonationFormState() {
        var designer = _contentTypeEditor.NewElement("Sponsorship Donation Form State",
                                                     PlatformsConstants.DonationFormState.Sponsorship);

        designer.InFolder(Folders.Platforms, Folders.DonationForms);
        designer.WithDeterministicId();

        designer.Group(Groups.General)
                .ContentmentDataList(Of((SponsorshipDonationFormStateContent x) => x.Scheme))
                .DataType(Shared.SponsorshipScheme)
                .Mandatory();

        designer.Save();
    }

    private void SeedSuggestedAmount() {
        var designer = _contentTypeEditor.NewElement<DonationFormStateSuggestedAmountElement>();

        designer.SetName("Suggested Amount");
        designer.InFolder(Folders.Platforms, Folders.DonationForms);
        designer.WithDeterministicId();
        designer.SetIcon("icon-checkbox-dotted-active");

        var general = designer.Group(Groups.General);

        general.Decimal(x => x.Amount).DataType(Shared.Money).Mandatory();
        general.TextBox(x => x.Description).DataType(Shared.TextBox);

        designer.Save();
    }
}
