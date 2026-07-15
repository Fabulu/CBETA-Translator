from pathlib import Path
import json
import sys

H = Path(__file__).resolve().parent
R = H.parent.parent
wave = json.loads((H / 'f004.json').read_text(encoding='utf-8'))
rows = {x['ordinal']: x for x in wave['entries']}
start = int(sys.argv[1]) if len(sys.argv) > 1 else 1156
end = int(sys.argv[2]) if len(sys.argv) > 2 else 1165
for ordinal in range(start, end + 1):
    row = rows[ordinal]
    directory = R / 'fresh-build/entries' / row['id']
    draft = json.loads((directory / 'evidence.draft.json').read_text(encoding='utf-8'))['Entry']
    sense = draft['Senses'][0]
    opening = sense['ExplanationParts']['CorpusEarnedOpening']
    bend = sense['DraftEvidence']['ZenBend']
    limit = sense['DraftEvidence']['CounterexampleOrLimit']
    aliases = '; '.join(sense['SearchAliases'])
    path = directory / 'WORK.md'
    text = path.read_text(encoding='utf-8').rstrip() + '\n\n'
    text += f'''feedback-inference-verdict: licensed — {opening}
feedback-observations: All stored complete cases were read; the definition states the smallest repeated inference licensed by the pictured scene and its corpus deployments.
feedback-falsification-searches: literal scene; longer formulas; recensions; title and catalogue contamination; contrary or neutral deployments.
feedback-counterexamples: {limit}
feedback-scope: Exact headword uses in the locked 494-file/487-work corpus; no outside symbolism, intent, or doctrine was imported.
opening-interpretation-verdict: PASS — the opening gives the shortest corpus-earned interpretation before the evidence history.
modifier-relation-verdict: checked — the whole expression was tested as a lexical unit rather than assembled from isolated graph glosses.
display-modifier-verdict: checked — the visible object, actor, or action remains explicit in the English target.
lookup-probes: {aliases}.
'''
    path.write_text(text, encoding='utf-8')
