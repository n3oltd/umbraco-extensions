import { n3oPluginConfig } from '@repo/build-config';

export default n3oPluginConfig({
    entries: {
        'N3O.Umbraco.BackofficeCore/auth-fetch': 'src/auth-fetch.ts',
        'N3O.Umbraco.BackofficeCore/index': 'src/index.ts',
        'N3O.Umbraco.BackofficeCore/workspace-visibility-condition': 'src/workspace-visibility-condition.ts',
    },
    outDir: 'dist',
});
