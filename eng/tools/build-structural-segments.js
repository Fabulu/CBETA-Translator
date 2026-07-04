#!/usr/bin/env node
// eng/tools/build-structural-segments.js
//
// Structural segmentation: groups <lb> markers by parent <p>/<lg> elements.
// Produces a .segments.jsonl file for each input XML pair (source + translation).
//
// Usage:
//   node eng/tools/build-structural-segments.js \
//     --source-dir C:/Programmieren/CbetaZenTexts/xml-p5 \
//     --trans-dir  C:/Programmieren/CbetaZenTranslations/xml-p5t \
//     --out-dir    C:/Programmieren/CbetaZenTranslations/segments \
//     [--file T48n2005]   # optional: process single file
//
// Processes all files in trans-dir (since those are our translated texts).
// For each, finds the matching source in source-dir, parses both, and
// produces a segment map JSONL grouping lbs by <p>/<lg> parent.

const { readFileSync, writeFileSync, mkdirSync, readdirSync, existsSync } = require('fs');
const { join, dirname, basename, relative } = require('path');

// Parse args
const args = process.argv.slice(2);
function arg(name) {
    const i = args.indexOf('--' + name);
    return i >= 0 && i + 1 < args.length ? args[i + 1] : null;
}

const SOURCE_DIR = arg('source-dir') || 'C:/Programmieren/CbetaZenTexts/xml-p5';
const TRANS_DIR = arg('trans-dir') || 'C:/Programmieren/CbetaZenTranslations/xml-p5t';
const OUT_DIR = arg('out-dir') || 'C:/Programmieren/CbetaZenTranslations/segments';
const SINGLE_FILE = arg('file'); // e.g., "T48n2005"

/**
 * Detect the primary lb edition of a body by most-frequent `ed` attribute.
 * Audit R2 §segmentation: the old code hardcoded a T|X|J|B whitelist, so ZW/C/P/U
 * collection files (whose lbs carry ed="ZW" etc.) collected NO lbs and produced
 * empty lb_range — silently degrading the reader's page/reading toggle. We now
 * accept lbs of the file's own primary edition (plus lbs with no ed attribute).
 * Returns the most frequent ed string, or null when no lb carries an ed attribute
 * (in which case all lbs are accepted).
 */
function detectPrimaryEdition(body) {
    const counts = new Map();
    const lbRe = /<lb\b[^>]*\bed="([^"]+)"/gi;
    let m;
    while ((m = lbRe.exec(body)) !== null) {
        counts.set(m[1], (counts.get(m[1]) || 0) + 1);
    }
    let best = null, bestN = 0;
    for (const [ed, n] of counts) {
        if (n > bestN) { best = ed; bestN = n; }
    }
    return best;
}

/** True when a start-tag `n`-value belongs to the file's primary edition. */
function lbEditionAccepted(tag, primaryEd) {
    const edMatch = tag.match(/\bed="([^"]+)"/);
    if (!edMatch) return true;                 // no ed → accept (belongs to this text)
    if (primaryEd === null) return true;       // file has no ed attrs anywhere → accept all
    return edMatch[1] === primaryEd;           // otherwise only the primary edition
}

/**
 * Extract segments from a TEI XML string.
 * Returns array of { lbIds: string[], text: string, type: string, xmlId: string }
 * Each segment = one <p> or <lg> element's content.
 */
function extractSegments(xml) {
    const segments = [];

    // Find <body> content
    const bodyMatch = xml.match(/<body[^>]*>([\s\S]*?)<\/body>/i);
    if (!bodyMatch) return segments;
    const body = bodyMatch[1];
    const primaryEd = detectPrimaryEdition(body);

    // Strategy: walk through the body tracking which <p>/<lg> we're inside.
    // Collect <lb> n-values and text per parent element.

    // Split body into tokens: tags and text
    const tokens = [];
    const tagRe = /<\/?[^>]+>/g;
    let lastIdx = 0;
    let m;
    while ((m = tagRe.exec(body)) !== null) {
        if (m.index > lastIdx) {
            tokens.push({ kind: 'text', value: body.slice(lastIdx, m.index) });
        }
        tokens.push({ kind: 'tag', value: m[0] });
        lastIdx = m.index + m[0].length;
    }
    if (lastIdx < body.length) {
        tokens.push({ kind: 'text', value: body.slice(lastIdx) });
    }

    // State machine.
    // pDepth counts nesting of content containers (<p>/<cell>/<item>). It replaces a
    // boolean `inP` that a nested inner </p> cleared while still inside an outer
    // <item>, dropping the item's tail text (audit R2-M1). A segment boundary is the
    // OUTERMOST container open/close; nested containers flow into the same segment.
    let pDepth = 0, inLg = false, inHead = false;
    // Suppress <note>/<app> CONTENT: their inner text bled into text_zh, disagreeing
    // with what TeiRenderer renders and poisoning downstream classification (R2-M1).
    let noteAppDepth = 0;
    let headDepth = 0;
    let currentLbs = [];
    let currentText = '';
    let currentXmlId = '';
    let currentType = 'prose';
    let segCounter = 0;

    function flushSegment() {
        // Clean whitespace but preserve ideographic space U+3000 (caesura in verse).
        const cleanText = currentText
            .replace(/[\t\n\r ]+/g, ' ')
            .trim();
        // Accept segments with text OR lbs (not requiring both).
        // Bug fix: short <p> elements with text but no internal <lb> were being dropped.
        if (currentLbs.length === 0 && cleanText.length === 0) return;

        segCounter++;
        segments.push({
            lbIds: [...currentLbs],
            text: cleanText,
            type: currentType,
            xmlId: currentXmlId
        });
        currentLbs = [];
        currentText = '';
        currentXmlId = '';
        currentType = 'prose';
    }

    for (const tok of tokens) {
        if (tok.kind === 'tag') {
            const tag = tok.value;

            // Skip <head> content entirely
            if (/<head[\s>]/i.test(tag) && !tag.startsWith('</')) {
                inHead = true;
                headDepth = 1;
                continue;
            }
            if (inHead) {
                if (/<head[\s>]/i.test(tag) && !tag.startsWith('</')) headDepth++;
                if (/<\/head>/i.test(tag)) {
                    headDepth--;
                    if (headDepth <= 0) inHead = false;
                }
                continue;
            }

            // <note>/<app> — suppress their CONTENT (audit R2-M1). Track depth so
            // nested notes and self-closing <note/>/<app/> behave; <rdg>/<lem> live
            // inside <app> so app-depth already covers them.
            if (/^<(note|app)[\s>]/i.test(tag) && !tag.startsWith('</')) {
                if (!/\/>\s*$/.test(tag)) noteAppDepth++;   // ignore self-closing <note/>/<app/>
                continue;
            }
            if (/^<\/(note|app)>/i.test(tag)) {
                noteAppDepth = Math.max(0, noteAppDepth - 1);
                continue;
            }

            // <p>, <cell>, <item> open — content containers. Carry pending lbs INTO
            // the new segment (in CBETA XML the <lb> often appears on the same line
            // BEFORE the <p> tag, so it logically belongs to the paragraph that
            // follows). Only the OUTERMOST open starts a new segment; nested opens
            // just deepen so their text flows into the same segment.
            if (/^<(p|cell|item)[\s>]/i.test(tag) && !tag.startsWith('</')) {
                if (pDepth === 0) {
                    // Flush any TEXT from the previous context, but keep currentLbs.
                    const savedLbs = [...currentLbs];
                    const savedText = currentText;
                    currentLbs = [];
                    currentText = '';
                    // Only flush if there was text outside a <p> (rare — metadata, etc.)
                    if (savedText.replace(/[\t\n\r ]+/g, '').trim().length > 0) {
                        currentLbs = savedLbs;
                        currentText = savedText;
                        flushSegment();
                    }
                    // Start the new paragraph with any carried-over lbs
                    currentLbs = savedLbs.length > 0 && savedText.replace(/[\t\n\r ]+/g, '').trim().length === 0
                        ? savedLbs : [];
                    currentText = '';
                    const idMatch = tag.match(/xml:id="([^"]+)"/);
                    currentXmlId = idMatch ? idMatch[1] : '';
                    currentType = 'prose';
                }
                pDepth++;
                continue;
            }
            // </p>, </cell>, </item> — content container closers. Only the OUTERMOST
            // close flushes the segment (nested closers just un-nest).
            if (/^<\/(p|cell|item)>/i.test(tag)) {
                pDepth = Math.max(0, pDepth - 1);
                if (pDepth === 0) flushSegment();
                continue;
            }

            // <lg> open
            if (/^<lg[\s>]/i.test(tag) && !tag.startsWith('</')) {
                flushSegment();
                inLg = true;
                const idMatch = tag.match(/xml:id="([^"]+)"/);
                currentXmlId = idMatch ? idMatch[1] : '';
                currentType = 'verse';
                continue;
            }
            // </lg>
            if (/^<\/lg>/i.test(tag)) {
                flushSegment();
                inLg = false;
                continue;
            }

            // <lb> — collect n-value (skip lbs inside <note>/<app>; accept only the
            // file's primary edition, or any lb with no ed attribute).
            if (/^<lb[\s]/i.test(tag)) {
                if (noteAppDepth === 0) {
                    const nMatch = tag.match(/\bn="([^"]+)"/);
                    if (nMatch && lbEditionAccepted(tag, primaryEd)) {
                        currentLbs.push(nMatch[1]);
                    }
                }
                continue;
            }

            // Skip remaining structural/metadata tags — but NOT <cb:div> which is a
            // content container. (The broad `cb:` prefix match used to eat <cb:div>,
            // causing ZW/ZS canons to produce 0 segments and J-canon texts to miss
            // their main body. <note>/<app> are handled above.)
            if (/^<(anchor|figure|rdg|lem|cb:mulu|cb:juan|cb:jhead|cb:tt|cb:coloph)/i.test(tag)) continue;
            if (/^<\/(anchor|figure|rdg|lem|cb:mulu|cb:juan|cb:jhead|cb:tt|cb:coloph)/i.test(tag)) continue;

            // <caesura/> → emit ideographic space U+3000 (verse pause marker)
            if (/^<caesura/i.test(tag)) {
                if ((pDepth > 0 || inLg) && noteAppDepth === 0) currentText += '　';
                continue;
            }

            // Ignore other tags (l, g, etc.) — their text still flows
        } else {
            // Text token
            if (inHead) continue;
            if (noteAppDepth > 0) continue;   // inside <note>/<app>: content suppressed
            if (pDepth > 0 || inLg) {
                currentText += tok.value;
            }
            // Text outside <p>/<lg> is silently ignored.
            // Bug fix: the previous fallback (currentLbs.length > 0) vacuumed
            // text between </p> and the next <p>, merging separate paragraphs.
        }
    }
    flushSegment(); // flush last segment if any

    // Post-process: detect dialogue type by content patterns
    for (const seg of segments) {
        if (seg.type !== 'verse') {
            if (/[云曰問答]/.test(seg.text) && /[師僧祖]/.test(seg.text)) {
                seg.type = 'dialogue';
            }
            // Detect commentary (e.g., "無門曰" pattern in Wumenguan)
            if (/^無門曰/.test(seg.text) || /^師[云曰]/.test(seg.text.replace(/^\s+/, ''))) {
                // Keep as dialogue if it has Q&A patterns, otherwise commentary
                if (seg.text.startsWith('無門曰')) {
                    seg.type = 'commentary';
                }
            }
        }
    }

    return segments;
}

/**
 * Build a map from lb n-value to text content for a parsed XML.
 */
function buildLbTextMap(xml) {
    const map = new Map();
    const bodyMatch = xml.match(/<body[^>]*>([\s\S]*?)<\/body>/i);
    if (!bodyMatch) return map;
    const body = bodyMatch[1];
    const primaryEd = detectPrimaryEdition(body);

    // Walk tokens (same approach as extractSegments) to properly skip <head>
    const tokens = [];
    const tagRe = /<\/?[^>]+>/g;
    let lastIdx = 0;
    let m;
    while ((m = tagRe.exec(body)) !== null) {
        if (m.index > lastIdx)
            tokens.push({ kind: 'text', value: body.slice(lastIdx, m.index) });
        tokens.push({ kind: 'tag', value: m[0] });
        lastIdx = m.index + m[0].length;
    }
    if (lastIdx < body.length)
        tokens.push({ kind: 'text', value: body.slice(lastIdx) });

    let currentLb = null;
    let inHead = false;
    let headDepth = 0;
    let noteAppDepth = 0;

    for (const tok of tokens) {
        if (tok.kind === 'tag') {
            const tag = tok.value;
            // Skip <head> content
            if (/<head[\s>]/i.test(tag) && !tag.startsWith('</')) {
                inHead = true; headDepth = 1; continue;
            }
            if (inHead) {
                if (/<head[\s>]/i.test(tag) && !tag.startsWith('</')) headDepth++;
                if (/<\/head>/i.test(tag)) { headDepth--; if (headDepth <= 0) inHead = false; }
                continue;
            }
            // <note>/<app> — suppress their content from the EN map too (parity with
            // extractSegments; both parsers unify in P3.1c).
            if (/^<(note|app)[\s>]/i.test(tag) && !tag.startsWith('</')) {
                if (!/\/>\s*$/.test(tag)) noteAppDepth++;
                continue;
            }
            if (/^<\/(note|app)>/i.test(tag)) {
                noteAppDepth = Math.max(0, noteAppDepth - 1);
                continue;
            }
            // <lb> — track current lb (primary edition, or no-ed lbs)
            if (/^<lb[\s]/i.test(tag)) {
                if (noteAppDepth === 0) {
                    const nMatch = tag.match(/\bn="([^"]+)"/);
                    if (nMatch && lbEditionAccepted(tag, primaryEd)) {
                        currentLb = nMatch[1];
                    }
                }
            }
        } else {
            // Text token
            if (inHead) continue;
            if (noteAppDepth > 0) continue;
            if (currentLb) {
                const text = tok.value.replace(/[\t\n\r ]+/g, ' ').trim();
                if (text) {
                    const prev = map.get(currentLb) || '';
                    map.set(currentLb, prev ? prev + ' ' + text : text);
                }
            }
        }
    }
    return map;
}

/**
 * Process one file pair and write the segment JSONL.
 * Returns { segments, emptyLbSegments } for the coverage metric.
 */
function processFile(workId, sourceXml, transXml, outPath) {
    const zhSegments = extractSegments(sourceXml);
    const enLbMap = buildLbTextMap(transXml);

    const lines = [];
    let segIdx = 0;
    let emptyLbSegments = 0;

    for (const seg of zhSegments) {
        segIdx++;
        const unitId = `${workId}_${String(segIdx).padStart(3, '0')}`;

        if (seg.lbIds.length === 0) emptyLbSegments++;

        // Build English text by concatenating translation for each lb in the range
        const enParts = [];
        for (const lb of seg.lbIds) {
            const enText = enLbMap.get(lb);
            if (enText) enParts.push(enText);
        }
        const textEn = enParts.join(' ').trim();

        const entry = {
            unit_id: unitId,
            lb_range: seg.lbIds,
            text_zh: seg.text,
            text_en: textEn,
            type: seg.type,
            confidence: 1.0
        };

        lines.push(JSON.stringify(entry));
    }

    // Ensure output directory exists
    mkdirSync(dirname(outPath), { recursive: true });
    writeFileSync(outPath, lines.join('\n') + '\n', 'utf-8');

    const totalLbs = zhSegments.reduce((n, s) => n + s.lbIds.length, 0);
    const emptyNote = emptyLbSegments > 0 ? `, ${emptyLbSegments} empty-lb` : '';
    console.log(`  ${workId}: ${zhSegments.length} segments, ${totalLbs} lbs${emptyNote} → ${outPath}`);

    return { segments: zhSegments.length, emptyLbSegments };
}

/**
 * Discover all translated files and process them.
 */
function main() {
    console.log('Structural segmentation: grouping <lb> markers by <p>/<lg> parents');
    console.log(`  Source:  ${SOURCE_DIR}`);
    console.log(`  Trans:   ${TRANS_DIR}`);
    console.log(`  Output:  ${OUT_DIR}`);
    console.log();

    // Find all translated XML files
    const files = [];
    function walk(dir) {
        for (const entry of readdirSync(dir, { withFileTypes: true })) {
            if (entry.isDirectory()) {
                walk(join(dir, entry.name));
            } else if (entry.name.endsWith('.xml')) {
                files.push(join(dir, entry.name));
            }
        }
    }
    walk(TRANS_DIR);

    let processed = 0;
    // Coverage metric: empty lb_range means the reader page/reading toggle silently
    // degrades for that segment (audit R2 §segmentation). Track per collection.
    const byCollection = new Map(); // collection -> { segments, emptyLbSegments, files }
    for (const transPath of files) {
        const rel = relative(TRANS_DIR, transPath); // e.g., T/T48/T48n2005.xml
        const workId = basename(transPath, '.xml');   // e.g., T48n2005

        if (SINGLE_FILE && workId !== SINGLE_FILE) continue;

        // Find matching source
        const sourcePath = join(SOURCE_DIR, rel);
        if (!existsSync(sourcePath)) {
            console.log(`  SKIP ${workId}: no source at ${sourcePath}`);
            continue;
        }

        // Build output path
        const outRel = rel.replace('.xml', '.segments.jsonl');
        const outPath = join(OUT_DIR, outRel);

        try {
            const sourceXml = readFileSync(sourcePath, 'utf-8');
            const transXml = readFileSync(transPath, 'utf-8');
            const stats = processFile(workId, sourceXml, transXml, outPath);
            processed++;

            const collection = rel.split(/[\\/]/)[0] || '?';
            const acc = byCollection.get(collection) || { segments: 0, emptyLbSegments: 0, files: 0 };
            acc.segments += stats.segments;
            acc.emptyLbSegments += stats.emptyLbSegments;
            acc.files += 1;
            byCollection.set(collection, acc);
        } catch (err) {
            console.log(`  ERROR ${workId}: ${err.message}`);
        }
    }

    console.log(`\nDone: ${processed} files processed.`);

    // Empty-lb_range coverage metric
    let totalSeg = 0, totalEmpty = 0;
    for (const acc of byCollection.values()) { totalSeg += acc.segments; totalEmpty += acc.emptyLbSegments; }
    if (totalSeg > 0) {
        const pct = ((totalEmpty / totalSeg) * 100).toFixed(2);
        console.log(`\nEmpty lb_range coverage: ${totalEmpty}/${totalSeg} segments (${pct}%) across ${processed} files`);
        const cols = [...byCollection.keys()].sort();
        for (const col of cols) {
            const acc = byCollection.get(col);
            const cpct = acc.segments > 0 ? ((acc.emptyLbSegments / acc.segments) * 100).toFixed(2) : '0.00';
            const flag = acc.emptyLbSegments > 0 ? '  <-- has empty lb_range' : '';
            console.log(`  ${col.padEnd(6)} ${acc.emptyLbSegments}/${acc.segments} (${cpct}%) in ${acc.files} files${flag}`);
        }
    }
}

// Export for testability; guard main() so tests can import without running.
module.exports = { extractSegments, buildLbTextMap, detectPrimaryEdition };
if (require.main === module) main();
