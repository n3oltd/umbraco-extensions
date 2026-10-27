import type { UmbEntryPointOnInit } from '@umbraco-cms/backoffice/extension-api';

const linksVisibility = {
    alias: 'N3O.Condition.WorkspaceVisibility',
    endpoint: '/umbraco/backoffice/api/PlatformsLinksVisibility',
};

export const onInit: UmbEntryPointOnInit = (_host, extensionRegistry) => {
    extensionRegistry.appendCondition('Umb.WorkspaceInfoApp.Document.Links', linksVisibility);
};
