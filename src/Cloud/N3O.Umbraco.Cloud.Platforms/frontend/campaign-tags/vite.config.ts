import { n3oPluginConfig } from '@repo/build-config';

export default n3oPluginConfig({
    entries: {
        'N3O.Umbraco.Cloud.Platforms.CampaignTags/campaign-tags': 'src/campaign-tags.ts',
    },
    outDir: 'dist',
    react: true,
    additionalExternals: ['@n3oltd/backoffice-core'],
});
