#!/usr/bin/env node
// Local CLI over the website's ACTUAL v3 sharded search implementation.
// Discovery only: website text shards may retain XML apparatus, so every saved
// dictionary occurrence must still pass zc.verify against the source XML.

import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { performance } from 'node:perf_hooks';

const isWin = process.platform === 'win32';
const defaultSite = isWin ? 'C:/programmieren/ZenLinkPage' : '/mnt/c/programmieren/ZenLinkPage';
const defaultRepo = isWin
    ? 'C:/programmieren/MergeWorkCbeta/CBETA-Translator'
    : '/mnt/c/programmieren/mergeworkcbeta/cbeta-translator';

const args = process.argv.slice(2);
const queries = [];
let siteRoot = defaultSite;
let repoRoot = defaultRepo;
let exact = false;
let listFiles = false;
for (let i = 0; i < args.length; i++) {
    if (args[i] === '--site') siteRoot = args[++i];
    else if (args[i] === '--repo') repoRoot = args[++i];
    else if (args[i] === '--exact') exact = true;
    else if (args[i] === '--files') listFiles = true;
    else queries.push(args[i]);
}
if (!queries.length) {
    console.error('usage: node web_index_kwic.mjs [--exact] TERM [TERM ...]');
    process.exit(2);
}

const storage = new Map();
globalThis.sessionStorage = {
    getItem: key => storage.has(key) ? storage.get(key) : null,
    setItem: (key, value) => storage.set(key, String(value)),
    removeItem: key => storage.delete(key),
    clear: () => storage.clear(),
    key: index => [...storage.keys()][index] ?? null,
    get length() { return storage.size; },
};

// bigram-search.js uses browser-root URLs. Serve them straight from local files
// while preserving its own shard hashing, decoding, intersection, tf, and caches.
globalThis.fetch = async (input, init = {}) => {
    if (init.signal?.aborted) throw new DOMException('Aborted', 'AbortError');
    const raw = typeof input === 'string' ? input : input.url;
    const url = new URL(raw, 'http://local.readzen/');
    const rel = decodeURIComponent(url.pathname).replace(/^\/+/, '');
    const file = path.resolve(siteRoot, rel);
    const root = path.resolve(siteRoot) + path.sep;
    if (!file.startsWith(root)) return new Response('forbidden', { status: 403 });
    try {
        return new Response(await fs.promises.readFile(file), { status: 200 });
    } catch (error) {
        if (error?.code === 'ENOENT') return new Response('not found', { status: 404 });
        throw error;
    }
};

const allow = JSON.parse(
    fs.readFileSync(path.join(repoRoot, 'Assets/Data/zen-corpus.json'), 'utf8').replace(/^\uFEFF/, '')
);
const allowedIds = new Set(allow.texts.map(value =>
    value.replace(/\\/g, '/').split('/').pop().replace(/\.xml$/i, '')
));

const moduleUrl = pathToFileURL(path.join(siteRoot, 'lib/bigram-search.js')).href;
const { searchFulltext, metaForDocId, verifyDocPhrase, getManifestInfo } = await import(moduleUrl);
const output = {
    engine: 'ZenLinkPage/lib/bigram-search.js',
    manifest: await getManifestInfo(),
    allowlistSize: allowedIds.size,
    exactTextShardConfirmation: exact,
    queries: [],
};

for (const query of queries) {
    const started = performance.now();
    let stats = null;
    const rows = await searchFulltext(query, { onStats: value => { stats = value; } });
    const metas = await Promise.all(rows.map(row => metaForDocId(row.docId)));
    const kept = rows.map((row, index) => ({ row, meta: metas[index] }))
        .filter(item => item.meta && !item.meta.side && allowedIds.has(item.meta.fileId));
    let exactCounts = null;
    if (exact) {
        exactCounts = query.length === 1
            ? kept.map(item => item.row.hitCount) // unigram tf is exact
            : await Promise.all(kept.map(item => verifyDocPhrase(item.row.docId, query)));
    }
    output.queries.push({
        query,
        milliseconds: +(performance.now() - started).toFixed(1),
        indexVersion: stats?.indexVersion ?? null,
        allIndexCandidates: rows.length,
        allowlistedIndexCandidates: kept.length,
        allowlistedIndexTf: kept.reduce((sum, item) => sum + item.row.hitCount, 0),
        exactTextShardFiles: exactCounts ? exactCounts.filter(value => value > 0).length : null,
        exactTextShardHits: exactCounts ? exactCounts.reduce((sum, value) => sum + (value ?? 0), 0) : null,
        failedTextShardChecks: exactCounts ? exactCounts.filter(value => value === null).length : null,
        exactTextShardFileIds: exactCounts && listFiles
            ? kept.filter((item, index) => exactCounts[index] > 0).map(item => item.meta.fileId)
            : undefined,
        top: kept.slice(0, 5).map(item => ({ fileId: item.meta.fileId, indexTf: item.row.hitCount })),
    });
}

output.evidenceRule = 'discovery only; verify every saved KWIC with zc.verify against XML';
console.log(JSON.stringify(output, null, 2));
