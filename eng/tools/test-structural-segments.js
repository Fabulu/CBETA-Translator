#!/usr/bin/env node
// eng/tools/test-structural-segments.js
// Regression tests for the structural segment parser.
// Run: node --test eng/tools/test-structural-segments.js

const test = require('node:test');
const assert = require('node:assert');
const { extractSegments } = require('./build-structural-segments.js');

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
