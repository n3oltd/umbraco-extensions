angular.module("umbraco").run(["eventsService", function (eventsService) {
    const contentTypeAlias = "platformsCrowdfundingCampaign";

    eventsService.on("content.saved", function (name, args) {
        const content = args && args.content;

        if (!args.valid || !content || content.contentTypeAlias !== contentTypeAlias) {
            return;
        }

        const variant = content.variants.find(x => x.active);
        const properties = variant ? variant.tabs.flatMap(x => x.properties) : [];

        // The editor does not rebind the tabs a save answers with
        if (properties.length > 1) {
            return;
        }

        eventsService.emit("editors.content.reload", { node: { id: content.id } });
    });
}]);
