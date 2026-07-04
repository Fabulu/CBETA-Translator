#!/usr/bin/env node
// eng/tools/test-structural-segments.js
// Regression tests for the structural segment parser.
// Run: node --test eng/tools/test-structural-segments.js

const test = require('node:test');
const assert = require('node:assert');
const { extractSegments, detectPrimaryEdition, sourceContentHash } = require('./build-structural-segments.js');

test('consecutive <p> elements produce separate segments', () => {
    const xml = `<TEI><body>
        <p xml:id="p1"><lb n="001a" ed="T"/>first para</p>
        <p xml:id="p2"><lb n="002a" ed="T"/>second para</p>
    </body></TEI>`;
    const segs = extractSegments(xml);
    assert.equal(segs.length, 2, 'should produce 2 segments, not 1');
    assert.deepStrictEqual(segs[0].lbIds, ['001a']);
    assert.ok(segs[0].text.includes('first para'));
    assert.deepStrictEqual(segs[1].lbIds, ['002a']);
    assert.ok(segs[1].text.includes('second para'));
});

test('short <p> without internal <lb> is preserved', () => {
    const xml = `<TEI><body>
        <p><lb n="001a" ed="T"/>text with lb</p>
        <p>no lb tag here</p>
    </body></TEI>`;
    const segs = extractSegments(xml);
    assert.equal(segs.length, 2, 'short <p> without lb should still produce a segment');
    assert.ok(segs[1].text.includes('no lb tag here'));
});

test('<cb:div> wrapped content produces segments', () => {
    const xml = `<TEI><body>
        <cb:div type="jing">
            <p xml:id="p1"><lb n="001a" ed="T"/>wrapped text</p>
        </cb:div>
    </body></TEI>`;
    const segs = extractSegments(xml);
    assert.equal(segs.length, 1, '<cb:div> is a content container, not metadata — should not be skipped');
    assert.ok(segs[0].text.includes('wrapped text'));
});

test('nested <cb:div> elements segment correctly', () => {
    const xml = `<TEI><body>
        <cb:div type="juan">
            <cb:div type="jing">
                <p><lb n="001a" ed="T"/>inner text</p>
            </cb:div>
        </cb:div>
    </body></TEI>`;
    const segs = extractSegments(xml);
    assert.equal(segs.length, 1);
    assert.ok(segs[0].text.includes('inner text'));
});

test('<lg> verse inside <cb:div> is typed correctly', () => {
    const xml = `<TEI><body>
        <cb:div type="other">
            <lg xml:id="v1"><l><lb n="001a" ed="T"/>verse line one</l><l><lb n="001b" ed="T"/>verse line two</l></lg>
        </cb:div>
    </body></TEI>`;
    const segs = extractSegments(xml);
    assert.equal(segs.length, 1);
    assert.equal(segs[0].type, 'verse');
    assert.ok(segs[0].text.includes('verse line'));
});

test('<head> content is excluded from segments', () => {
    const xml = `<TEI><body>
        <p><lb n="001a" ed="T"/>before</p>
        <head>Chapter Title</head>
        <p><lb n="002a" ed="T"/>after</p>
    </body></TEI>`;
    const segs = extractSegments(xml);
    assert.equal(segs.length, 2);
    assert.ok(!segs[0].text.includes('Chapter'));
    assert.ok(!segs[1].text.includes('Chapter'));
});

test('<caesura/> produces U+3000 ideographic space', () => {
    const xml = `<TEI><body>
        <lg><l><lb n="001a" ed="T"/>phrase one<caesura/>phrase two</l></lg>
    </body></TEI>`;
    const segs = extractSegments(xml);
    assert.equal(segs.length, 1);
    assert.ok(segs[0].text.includes('\u3000'), 'should contain ideographic space U+3000');
});

test('ZW-style <cb:div> with multiple <p> elements', () => {
    const xml = `<TEI><body>
        <cb:div type="other">
            <p xml:id="zw1"><lb n="001a" ed="ZW"/>first para</p>
            <p xml:id="zw2"><lb n="001b" ed="ZW"/>second para</p>
        </cb:div>
    </body></TEI>`;
    const segs = extractSegments(xml);
    assert.equal(segs.length, 2, 'ZW-style <cb:div> wrapping should produce 2 segments');
    assert.ok(segs[0].text.includes('first para'));
    assert.ok(segs[1].text.includes('second para'));
});

// ---------------------------------------------------------------
// P3.1a Fix 1 — nested content containers preserve tail text (audit R2-M1)
// ---------------------------------------------------------------

test('nested <p> inside <item> does not drop the item tail text', () => {
    // The inner </p> used to clear the boolean inP while still inside <item>,
    // dropping "tail" from text_zh. A container-depth counter fixes it.
    const xml = `<TEI><body>
        <item><lb n="001a" ed="T"/><p>inner</p>tail text</item>
    </body></TEI>`;
    const segs = extractSegments(xml);
    assert.equal(segs.length, 1, 'the <item> is one segment');
    assert.ok(segs[0].text.includes('inner'), 'inner <p> text present');
    assert.ok(segs[0].text.includes('tail text'), 'item tail text after nested </p> must be preserved');
});

// ---------------------------------------------------------------
// P3.1a Fix 2 — <note>/<app> content is suppressed from text_zh (audit R2-M1)
// ---------------------------------------------------------------

test('<note> content does not bleed into text_zh', () => {
    const xml = `<TEI><body>
        <p><lb n="001a" ed="T"/>before<note>NOTECONTENT</note>after</p>
    </body></TEI>`;
    const segs = extractSegments(xml);
    assert.equal(segs.length, 1);
    assert.ok(!segs[0].text.includes('NOTECONTENT'), 'note content must be suppressed');
    assert.ok(segs[0].text.includes('before'));
    assert.ok(segs[0].text.includes('after'));
});

test('<app>/<rdg>/<lem> apparatus content does not bleed into text_zh', () => {
    const xml = `<TEI><body>
        <p><lb n="001a" ed="T"/>keep<app><lem>LEMMA</lem><rdg>READING</rdg></app>tail</p>
    </body></TEI>`;
    const segs = extractSegments(xml);
    assert.equal(segs.length, 1);
    assert.ok(!segs[0].text.includes('LEMMA'), 'lem content suppressed');
    assert.ok(!segs[0].text.includes('READING'), 'rdg content suppressed');
    assert.ok(segs[0].text.includes('keep'));
    assert.ok(segs[0].text.includes('tail'));
});

test('self-closing <note/> does not suppress following text', () => {
    const xml = `<TEI><body>
        <p><lb n="001a" ed="T"/>before<note place="inline"/>after</p>
    </body></TEI>`;
    const segs = extractSegments(xml);
    assert.equal(segs.length, 1);
    assert.ok(segs[0].text.includes('before'));
    assert.ok(segs[0].text.includes('after'), 'self-closing note must not open a suppression region');
});

// ---------------------------------------------------------------
// P3.1a Fix 3 — per-file lb edition (audit R2 §segmentation)
// ---------------------------------------------------------------

test('ZW lbs are collected into lb_range (per-file edition, not T|X|J|B whitelist)', () => {
    const xml = `<TEI><body>
        <cb:div type="other">
            <p xml:id="zw1"><lb n="001a" ed="ZW"/>first</p>
            <p xml:id="zw2"><lb n="001b" ed="ZW"/>second</p>
        </cb:div>
    </body></TEI>`;
    const segs = extractSegments(xml);
    assert.deepStrictEqual(segs[0].lbIds, ['001a'], 'ZW lb must be collected, not dropped');
    assert.deepStrictEqual(segs[1].lbIds, ['001b']);
});

test('detectPrimaryEdition picks the most frequent ed', () => {
    const body = '<lb n="1" ed="T"/><lb n="2" ed="T"/><lb n="3" ed="X"/>';
    assert.equal(detectPrimaryEdition(body), 'T');
    assert.equal(detectPrimaryEdition('<lb n="1" ed="ZW"/>'), 'ZW');
    assert.equal(detectPrimaryEdition('<lb n="1"/>'), null, 'no ed attrs → null');
});

test('secondary-edition lbs are excluded when a primary edition dominates', () => {
    // Primary is T (2 lbs); a stray X lb must NOT enter lb_range.
    const xml = `<TEI><body>
        <p><lb n="001a" ed="T"/>a<lb n="001b" ed="X"/>b<lb n="002a" ed="T"/>c</p>
    </body></TEI>`;
    const segs = extractSegments(xml);
    assert.deepStrictEqual(segs[0].lbIds, ['001a', '002a'], 'only primary-edition (T) lbs collected');
});

test('lbs with no ed attribute are always collected', () => {
    const xml = `<TEI><body>
        <p><lb n="001a"/>text</p>
    </body></TEI>`;
    const segs = extractSegments(xml);
    assert.deepStrictEqual(segs[0].lbIds, ['001a']);
});

// ---------------------------------------------------------------
// P3.1b — source content hash (staleness contract; C#/JS parity)
// ---------------------------------------------------------------

test('sourceContentHash is line-ending independent and matches the C# anchor', () => {
    // These hex anchors are pinned identically in the C# SegmentMapServiceTests so
    // the generator and the desktop loader agree byte-for-byte.
    assert.equal(
        sourceContentHash('<TEI><body><p>hi</p></body></TEI>'),
        '730c6fa790830ef1efbd219963d42c66be2aa49f8ba93a8f45430784b754068e');
    const crlf = sourceContentHash('<TEI>\r\n<body><p>hi</p></body>\r\n</TEI>');
    const lf = sourceContentHash('<TEI>\n<body><p>hi</p></body>\n</TEI>');
    assert.equal(crlf, lf, 'CRLF and LF must hash identically');
    assert.equal(crlf, 'eb0b16976a228903ae3643e755320fa35c6b65f9b6c35ae7c2c5e582c37ec097');
});
