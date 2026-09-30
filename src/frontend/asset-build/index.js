import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import {produceAsset} from './assets.js';

export async function buildAssets(config) {
    const root = config.root ?? process.cwd();
    const outDir = path.resolve(root, config.outDir);
    const publicPath = trimTrailingSlash(config.publicPath ?? '/assets');
    const manifestPath = path.resolve(root, config.manifest ?? path.join(config.outDir, 'assets-manifest.json'));
    const manifest = {};
    const emitted = new Set();

    for (const [bundleName, bundle] of Object.entries(config.bundles)) {
        manifest[bundleName] = {
            css: await emitAll(bundle.css ?? [], 'css', bundle, {root, outDir, publicPath, config, emitted}),
            js: await emitAll(bundle.js ?? [], 'js', bundle, {root, outDir, publicPath, config, emitted})
        };
    }

    fs.mkdirSync(path.dirname(manifestPath), {recursive: true});
    writeAtomic(manifestPath, `${JSON.stringify(manifest, null, 2)}\n`);

    pruneStale(outDir, emitted);

    return manifest;
}

function writeAtomic(target, contents) {
    const temp = `${target}.${process.pid}.tmp`;

    fs.writeFileSync(temp, contents);
    fs.renameSync(temp, target);
}

const HASHED_ARTEFACT = /\.[0-9a-f]{8}\.[^.\\/]+(\.map)?$/;

function pruneStale(outDir, emitted) {
    if (!fs.existsSync(outDir)) {
        return;
    }

    for (const relative of fs.readdirSync(outDir, {recursive: true})) {
        const full = path.join(outDir, relative);

        if (!HASHED_ARTEFACT.test(full) || emitted.has(full) || fs.statSync(full).isDirectory()) {
            continue;
        }

        fs.rmSync(full);
    }
}

async function emitAll(entries, kind, bundle, context) {
    const references = [];

    for (const entry of entries) {
        references.push(await emit(entry, kind, bundle, context));
    }

    return references;
}

async function emit(entry, kind, bundle, context) {
    const {root, outDir, publicPath, config, emitted} = context;
    const sourceMaps = (entry.sourcemaps ?? bundle.sourcemaps ?? config.sourcemaps ?? 'none') === 'external';
    const wantsIntegrity = entry.integrity ?? bundle.integrity ?? config.integrity ?? false;
    const targets = entry.targets ?? bundle.targets ?? config.targets;

    const produced = await produceAsset(entry, kind, {root, sourceMaps, targets});

    const emitMap = sourceMaps && produced.map != null;

    const base = emitMap ? stripSourceMappingUrl(produced.content) : produced.content;
    const hashInput = emitMap ? Buffer.concat([base, produced.map]) : base;

    const hash = crypto.createHash('sha256').update(hashInput).digest('hex').slice(0, 8);
    const outName = hashName(entry.out, hash);
    const outPath = path.resolve(outDir, outName);

    fs.mkdirSync(path.dirname(outPath), {recursive: true});

    let content = base;
    const url = `${publicPath}/${outName.split(path.sep).join('/')}`;

    if (emitMap) {
        const mapName = `${path.basename(outName)}.map`;

        fs.writeFileSync(`${outPath}.map`, produced.map);
        emitted.add(`${outPath}.map`);

        const comment = kind === 'css' ? `\n/*# sourceMappingURL=${mapName} */\n`
                                       : `\n//# sourceMappingURL=${mapName}\n`;

        content = Buffer.concat([content, Buffer.from(comment)]);
    }

    fs.writeFileSync(outPath, content);
    emitted.add(outPath);

    const reference = {url: url, integrity: null};

    if (wantsIntegrity) {
        reference.integrity = `sha384-${crypto.createHash('sha384').update(content).digest('base64')}`;
    }

    if (kind === 'js') {
        reference.module = entry.module ?? false;
    }

    return reference;
}

function hashName(out, hash) {
    const dir = path.dirname(out);
    const ext = path.extname(out);
    const stem = path.basename(out, ext);
    const name = `${stem}.${hash}${ext}`;

    return dir === '.' ? name : path.join(dir, name);
}

function stripSourceMappingUrl(content) {
    const text = content.toString('utf8');
    const stripped = text.replace(/\s*(?:\/\/#|\/\*#)\s*sourceMappingURL=[^\r\n*]*(?:\s*\*\/)?\s*$/, '');

    return stripped.length === text.length ? content : Buffer.from(stripped, 'utf8');
}

function trimTrailingSlash(value) {
    return value.endsWith('/') ? value.slice(0, -1) : value;
}
