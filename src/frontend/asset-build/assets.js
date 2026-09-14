import fs from 'node:fs';
import path from 'node:path';
import * as sass from 'sass';
import {bundle as bundleCss, transform as transformCss} from 'lightningcss';
import * as esbuild from 'esbuild';

// An entry either compiles a source file or adopts an artifact another tool already produced. Adopt
// exists because ten sites build their JS with Rollup and Terser already; re-bundling that output
// would change its semantics for no gain.
export async function produceAsset(entry, kind, options) {
    if (entry.adopt) {
        return adopt(entry, options);
    }

    if (kind === 'css') {
        return compileCss(entry, options);
    }

    return compileJs(entry, options);
}

function adopt(entry, options) {
    const source = path.resolve(options.root, entry.adopt);
    const content = fs.readFileSync(source);
    const mapSource = `${source}.map`;
    let map = null;

    if (options.sourceMaps && fs.existsSync(mapSource)) {
        map = fs.readFileSync(mapSource);
    }

    return {content, map};
}

function compileCss(entry, options) {
    const source = path.resolve(options.root, entry.compile);
    let code;
    let inputMap;

    // Lightning CSS transforms CSS; it does not understand Sass, so .scss goes through dart-sass first.
    if (source.endsWith('.scss') || source.endsWith('.sass')) {
        const compiled = sass.compile(source, {sourceMap: options.sourceMaps, style: 'expanded'});

        code = Buffer.from(compiled.css);
        inputMap = compiled.sourceMap ? Buffer.from(JSON.stringify(compiled.sourceMap)) : undefined;

        const result = transformCss({
            filename: source,
            code: code,
            minify: true,
            sourceMap: options.sourceMaps,
            inputSourceMap: inputMap ? inputMap.toString() : undefined,
            targets: options.targets
        });

        return {content: Buffer.from(result.code), map: result.map ? Buffer.from(result.map) : null};
    }

    // A plain .css entry point may use @import, which bundle() resolves and inlines.
    const result = bundleCss({
        filename: source,
        minify: true,
        sourceMap: options.sourceMaps,
        targets: options.targets
    });

    return {content: Buffer.from(result.code), map: result.map ? Buffer.from(result.map) : null};
}

async function compileJs(entry, options) {
    const source = path.resolve(options.root, entry.compile);

    const result = await esbuild.build({
        entryPoints: [source],
        bundle: true,
        minify: true,
        format: entry.module === false ? 'iife' : 'esm',
        sourcemap: options.sourceMaps ? 'external' : false,
        write: false,
        logLevel: 'silent'
    });

    let content = null;
    let map = null;

    for (const file of result.outputFiles) {
        if (file.path.endsWith('.map')) {
            map = Buffer.from(file.contents);
        } else {
            content = Buffer.from(file.contents);
        }
    }

    return {content, map};
}
