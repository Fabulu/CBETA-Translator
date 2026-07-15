#!/usr/bin/env python3
"""Replace stale concordance prose with frozen-preflight facts for B401-450."""
import json, re
from pathlib import Path

R = Path(__file__).resolve().parents[2]
rows = json.loads((R/'fresh-build/waves/f002-laneB-401-500-preflight.json').read_text())['entries'][:50]
# Count claims are deliberately recognizable and distinguish storage files from works.
count_sentence = re.compile(
    r'(?:(?<=^)|(?<=[.!?])\s+)[^.!?]*(?:\b(?:occurs?|occurred|attested|headword has|exact headword occurs|allowlist count|allowlisted count|compound occurs|expression occurs|formula occurs|term occurs|phrase occurs|action occurs|compact headword occurs)\b)[^.!?]*\b(?:times|hits?)\b[^.!?]*(?:[.!?]|$)',
    re.I,
)
any_count_clause = re.compile(r'[^.!?]*(?:\b\d[\d,]*\s+(?:times|hits?|files|texts|works)\b)[^.!?]*(?:[.!?]|$)', re.I)
bare_occurrence_count = re.compile(r'[^.!?]*\boccurs?\s+\d[\d,]*\s+times\s+in\s+\d[\d,]*[^.!?]*(?:[.!?]|$)', re.I)

def scrub(value):
    if isinstance(value, str):
        if 'Frozen-corpus concordance:' in value:
            # Keep the controlled current fact, remove any other numeric concordance clause.
            chunks=[]
            for sentence in re.split(r'(?<=[.!?])\s+',value):
                if re.search(r'\b\d[\d,]*\s+(?:times|hits?|files|texts|works)\b',sentence,re.I) and 'Frozen-corpus concordance:' not in sentence:
                    continue
                chunks.append(sentence)
            return bare_occurrence_count.sub('', ' '.join(chunks)).strip()
        return re.sub(r'\s{2,}',' ',bare_occurrence_count.sub('',any_count_clause.sub('',value))).strip()
    if isinstance(value,list): return [scrub(x) for x in value]
    if isinstance(value,dict): return {k:scrub(v) for k,v in value.items()}
    return value

for n,row in enumerate(rows,401):
    p=R/'fresh-build/entries'/row['id']/'evidence.draft.json'; payload=json.loads(p.read_text()); e=payload['Entry']
    fact=f"Frozen-corpus concordance: {row['hits']} exact hits in {row['files']} storage files representing {row['works']} independent works."
    replaced=0
    for s in e['Senses']:
        parts=s['ExplanationParts']
        for key in ('CorpusEarnedOpening',):
            old=parts[key]; new,nsub=count_sentence.subn(fact,old); parts[key]=new; replaced+=nsub
        bodies=[]
        for old in parts['EvidenceBody']:
            new,nsub=count_sentence.subn(fact if not replaced else '',old); replaced+=nsub; bodies.append(re.sub(r'\s{2,}',' ',new).strip())
        parts['EvidenceBody']=bodies
        if s.get('Note'):
            old=s['Note']; new,nsub=count_sentence.subn(fact if not replaced else '',old); replaced+=nsub; s['Note']=re.sub(r'\s{2,}',' ',new).strip()
        if not replaced:
            # The counts are useful provenance, but they are not allowed to crowd the definition.
            s['Note']=(s.get('Note','').rstrip()+' '+fact).strip()
            replaced=1
        # Draft admission prose copied from the historical entry must not retain
        # a second, stale count claim either.
        cleaned=scrub(s)
        s.clear();s.update(cleaned)
        if fact not in (s.get('Note') or ''):
            s['Note']=(s.get('Note','').rstrip()+' '+fact).strip()
    p.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n')
    print(n,e['SourceTerm'],fact)
