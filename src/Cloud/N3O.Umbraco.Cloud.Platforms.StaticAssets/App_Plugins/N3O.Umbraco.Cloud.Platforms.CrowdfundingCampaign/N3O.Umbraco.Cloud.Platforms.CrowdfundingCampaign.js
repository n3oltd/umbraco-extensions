angular.module("umbraco").run(["eventsService", function (eventsService) {
    const contentTypeAlias = "platformsCrowdfundingCampaign";

    eventsService.on("content.saved", function (name, args) {
        const content = args && args.content;

        if (!args.valid || !content || content.contentTypeAlias !== contentTypeAlias) {
            return;
        }

        const variant = content.variants.find(x => x.active);
        const properties = variant ? variant.tabs.flatMap(x => x.properties) : [];

        // A create is sent carrying the campaign alone, and the editor does not rebind the tabs it is
        // answered with, so the properties populated from the campaign need it to load the node again
        if (properties.length > 1) {
            return;
        }

        eventsService.emit("editors.content.reload", { node: { id: content.id } });
    });
}]);
