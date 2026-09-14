import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import {produceAsset} from './assets.js';

// Written next to the emitted assets and read at runtime by N3O.Umbraco.Bundling's IAssetManifest.
// The tag helpers cannot know a content hash at authoring time; this file is how they learn it.
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

    // Only after the manifest names the new files, so a request arriving mid-build is never pointed at
    // something already deleted.
    pruneStale(outDir, emitted);

    return manifest;
}

// The running site watches this file and reparses it on change, so a partial write would be read as
// truncated JSON. Rename is atomic within a volume, so a reader sees either the old file or the new one.
function writeAtomic(target, contents) {
    const temp = `${target}.${process.pid}.tmp`;

    fs.writeFileSync(temp, contents);
    fs.renameSync(temp, target);
}

// Every content change produces a new filename, so previous ones would otherwise accumulate forever
// wherever wwwroot survives between builds. Only content-hashed artefacts are considered, so pointing
// outDir at a directory that also holds hand-managed assets cannot delete them.
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

    // The sourceMappingURL comment cannot be hashed with the content that names the file it points at,
    // so the map itself goes into the hash instead. Without it, toggling sourcemaps would leave the
    // filename unchanged while the served bytes changed, and a cached copy would fail its integrity check.
    const emitMap = sourceMaps && produced.map != null;

    // An adopted artefact may already end with a sourceMappingURL naming its own map. Ours is appended
    // below and would win, but the stale one would be left pointing at a file that is never emitted.
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

    // Rollup's iife output must load as a classic script; loading it as a module would defer it, scope
    // it, and force strict mode. Only an entry that says so is emitted with type="module".
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
