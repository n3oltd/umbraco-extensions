import {buildAssets} from '@n3oltd/asset-build';

await buildAssets({
    root: import.meta.dirname,
    outDir: 'wwwroot/assets/bundles',
    publicPath: '/assets/bundles',
    bundles: {
        main: {
            css: [{compile: 'assets-src/site.scss', out: 'css/site.css'}],
            js: [{adopt: 'wwwroot/assets/js/cta-box.js', out: 'js/site.js', module: false}]
        }
    }
});
