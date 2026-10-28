import { n3oPluginConfig } from '@repo/build-config';

export default n3oPluginConfig({
    entries: {
        'N3O.Umbraco.Cloud.Platforms.Urls/entry-point': 'src/entry-point.ts',
        'N3O.Umbraco.Cloud.Platforms.Urls/platforms-urls-info-app': 'src/platforms-urls-info-app.ts',
    },
    outDir: 'dist',
    additionalExternals: ['@n3oltd/backoffice-core'],
});
