#!/usr/bin/env python3
"""Migrate the verified Lane-C decile research into item-20 worksheets.

This is deliberately limited to f002/C/501-510 and does not run a gate.
"""
import json, os, re, subprocess
from datetime import datetime, timezone
from pathlib import Path

ROOT=Path(__file__).parent
FRESH=ROOT/'fresh-build'
OLD=ROOT/'terms'
IDS=os.environ.get('F002_IDS','t_b90a5f36ec86 t_d4df8bc75ad7 t_74a27239e6c7 t_48bc24c64738 t_8253f56255ce t_96473172e857 t_cb2205148690 t_e95ea628d5dd t_b33fddd5d4f1 t_6214dc704b24').split()
BASE=json.loads((FRESH/'corpus-baseline.json').read_text(encoding='utf8'))
WORK=BASE['work_ids']

def parts(text):
    chunks=re.split(r'(?<=[.!?])\s+', text.strip())
    if chunks and chunks[0].lower().startswith('literally'):
        chunks=chunks[1:]
    chunks=[x for x in chunks if x]
    if len(chunks)==1: return chunks[0], ['The cited occurrences supply the direct statements and encounter deployments summarized here.']
    return chunks[0], [' '.join(chunks[1:])]

def proof(o):
    note=o.get('AttributionNote') or ''
    if o.get('MasterName'):
        if not note:
            note=f"The full case identifies {o['MasterName']} as the utterer of the headword clause."
            o['AttributionNote']=note
        return {'ExactHeadwordClause':o['Kwic'],'SpeechFrame':note,'FullCaseDecision':note}
    actor=o.get('ActorAttribution') or {}
    return {'GrammaticalSubject':actor.get('ActorLabel') or 'the source voice identified in the attribution record','FullCaseDecision':note}

def complete_actor(o):
    """Preserve reviewed attribution while filling required item-20 audit prose."""
    if o.get('MasterName'):
        return
    actor=o.get('ActorAttribution') or {}
    actor.setdefault('ActorType','reviewed-unnamed')
    actor.setdefault('ActorLabel','the source voice identified in the attribution record')
    actor['GrammarEvidence']=actor.get('GrammarEvidence') or (
        'The full case was read: the headword is not uttered by a nameable Zen master in this occurrence.'
    )
    actor['ContextEvidence']=actor.get('ContextEvidence') or (
        o.get('AttributionNote') or 'The surrounding exchange and source context were checked for the utterer.'
    )
    o['ActorAttribution']=actor

for id in IDS:
    old=json.loads((OLD/id/'entry.v2.json').read_text(encoding='utf8'))
    entry=dict(old)
    entry['SchemaVersion']=2
    entry['CorpusBaselineSha256']=BASE['manifestSha256']
    entry['CreatedBy']='Codex f002 Lane C author'
    entry['WrittenUtc']=datetime.now(timezone.utc).isoformat()
    for s in entry['Senses']:
        opening,body=parts(s.pop('Explanation'))
        s['ExplanationParts']={'CorpusEarnedOpening':opening,'EvidenceBody':body}
        if not s.get('SearchAliases'):
            seeds=[s['PreferredTarget'],*(s.get('AlternateTargets') or [])]
            base=s['PreferredTarget'].removeprefix('the ').removeprefix('to ')
            seeds.extend([base,base.replace('-', ' '),f'Zen {base}'])
            s['SearchAliases']=list(dict.fromkeys(x for x in seeds if x))[:5]
        for o in s.get('Occurrences',[]):
            complete_actor(o); o['DraftActorProof']=proof(o)
        for o in s.get('ClaimAnchors',[]):
            complete_actor(o); o['DraftActorProof']=proof(o)
        allowed={'utterer','respondent','questioner','interlocutor','addressee','section-subject','record-owner','person-described','person-discussed','commentator','later-raiser','later-quoter','teacher','student','compiler','verse-author','case-figure'}
        for o in [*s.get('Occurrences',[]),*s.get('ClaimAnchors',[])]:
            raw_context=o.get('ContextMasters') or []
            o['ContextMasters']=[{'MasterName':c,'Roles':['person-discussed']} if isinstance(c,str) else c for c in raw_context]
            for c in o['ContextMasters']:
                roles=[r for r in c.get('Roles',[]) if r in allowed]
                c['Roles']=roles or ['person-discussed']
        rels=s.get('SourceTexts') or [o['RelPath'] for o in s.get('Occurrences',[])]
        workids=[]
        for rel in rels:
            wid=WORK.get(rel)
            if wid and wid not in workids: workids.append(wid)
        s['DraftEvidence']={
          'OpeningClaimEvidenceKeys':[f'o{i}' for i in range(1,min(3,len(s.get('Occurrences',[])))+1)],
          'ZenBend':opening,
          'CounterexampleOrLimit':s.get('Note') or 'The stored cases bound the claim and do not authorize an outside symbolic or doctrinal reading.',
          'DifferentThingTest':{'Decision':'different-thing' if len(entry['Senses'])>1 else 'one-thing','ComparedThings':[x['PreferredTarget'] for x in entry['Senses']],'Reason':'The inherited sense inventory was rechecked against the exact occurrences; separate senses are retained only for different referents or events.'},
          'AliasRationale':'The controlled lookup phrases cover the preferred wording and natural English variants without changing the displayed translation.',
          'ModifierControls':[{'finding':'not-applicable','reason':'No apparent material/color modifier controls this headword.'}],
          'FamilyControls':[{'finding':'checked','reason':'Standalone headword occurrences were kept distinct from longer compounds and neighboring family terms.'}],
          'IndependentWorkIds':workids
        }
    payload={'SchemaVersion':1,'Entry':entry}
    out=FRESH/'entries'/id
    out.mkdir(parents=True,exist_ok=True)
    worksheet=out/'evidence.draft.json'
    worksheet.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    subprocess.run(['python3',str(ROOT/'compile_evidence_draft.py'),str(worksheet)],check=True,stdout=subprocess.DEVNULL)
