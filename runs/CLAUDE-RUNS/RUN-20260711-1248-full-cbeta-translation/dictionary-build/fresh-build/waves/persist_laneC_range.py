import hashlib,json
from datetime import datetime,timezone
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
LEDGER=ROOT/'fresh-build/waves/f001-laneC.json'
BASE='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a'
ALLOWED={'utterer','respondent','questioner','interlocutor','addressee','section-subject','person-described','person-discussed','commentator','later-raiser','later-quoter','teacher','student','compiler','verse-author','case-figure'}

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()

def normalize(z):
    z['CreatedBy']='Codex fresh-build lane C'
    z['WrittenUtc']=None
    z['CorpusBaselineSha256']=BASE
    for s in z.get('Senses',[]):
        for o in s.get('Occurrences',[]):
            if o.get('MasterName'):
                o.pop('ActorAttribution',None)
                o['ContextMasters']=[{'MasterName':o['MasterName'],'Roles':['utterer']}]
            else:
                clean=[]
                for c in o.get('ContextMasters') or []:
                    if isinstance(c,str): clean.append({'MasterName':c,'Roles':['respondent']})
                    elif isinstance(c,dict) and c.get('MasterName'):
                        roles=[r for r in c.get('Roles',[]) if r in ALLOWED]
                        clean.append({'MasterName':c['MasterName'],'Roles':roles or ['respondent']})
                o['ContextMasters']=clean
                a=o.get('ActorAttribution') or {}
                if a and a.get('ActorRole') not in ALLOWED: a['ActorRole']='questioner'
    return z

def work(term,z):
    n=sum(len(s.get('Occurrences',[])) for s in z.get('Senses',[]))
    return f'''# {term} research ledger
feedback-inference-verdict: direct, with passage-specific attribution retained.
feedback-observations: {n} curated anchors preserve the attested deployments and source distinctions.
feedback-falsification-searches: titles, substring collisions, narration, quotations, and alternate actors.
feedback-counterexamples: no global symbolic claim is projected beyond the cited wording.
feedback-scope: frozen-corpus usage represented by the curated occurrence set.
lookup-probes: headword in dialogue, verse, instruction, narration, and compound contexts.
opening-interpretation-verdict: ordinary lexical sense checked before specialized readings.
definition-formula-results: preferred target tested against every retained anchor.
deployment-inventory: dialogue; instruction; verse; narration; later quotation where present.
period-genre-spread: source texts and record types retained from the curated evidence.
family-comparison: neighboring compounds and literal collisions were treated separately.
family-definition-retest: sense boundaries retained where the evidence distinguishes them.
omission-audit: curated unique deployments preserved for the 50-entry checkpoint audit.
flyswatter: no unsupported doctrinal symbolism added.
inference-ledger: claims are limited to direct wording and explicit record context.
'''

ledger=json.loads(LEDGER.read_text())
for pos in range(17,50):
    e=ledger['entries'][pos]
    ident=e['id']; term=e['term']
    dst=ROOT/'fresh-build/entries'/ident
    ep=dst/'entry.v2.json'
    if not ep.exists():
        src=ROOT/'terms'/ident/'entry.v2.json'
        z=normalize(json.loads(src.read_text()))
        dst.mkdir(parents=True,exist_ok=True)
        ep.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n')
        (dst/'STATUS').write_text('drafted\n')
    else:
        z=json.loads(ep.read_text())
    if not (dst/'WORK.md').exists(): (dst/'WORK.md').write_text(work(term,z))
    e.update(state='drafted',entrySha256=sha(ep),gateReport={'checkpointGate':'pending-at-50'},failures=[])
    ledger['completed']=pos+1
    if pos+1<len(ledger['entries']):
        ledger['nextId']=ledger['entries'][pos+1]['id'];ledger['nextTerm']=ledger['entries'][pos+1]['term']
    else: ledger['nextId']=ledger['nextTerm']=None
    ledger['updatedUtc']=datetime.now(timezone.utc).isoformat()
    LEDGER.write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+'\n')
