import datetime, json, subprocess, sys
from pathlib import Path

R=Path(__file__).resolve().parents[2]
REV=json.loads((R/'fresh-build/waves/f002-laneA-351-400-independent-semantic-current-review.json').read_text(encoding='utf8'))
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat()
RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']

# Only names established by the record title/section and the complete turn are used here.
NAMED={354:'Juelang Daosheng',360:'Tianyin Yuanxiu',365:'Feiyin Tongrong',368:'Wuyi Yuanlai',
       369:'Shending Yunwai Ze',375:'Wuyi Yuanlai',377:'Dahui Zonggao',379:'Linji Yixuan',
       382:'Linji Yixuan',383:'Linji Yixuan',385:'Wuyi Yuanlai',388:'Tianyi Yihuai',
       392:'Dahui Zonggao',397:'Huanyou Zhengchuan',399:'Minshu Zhiche'}

def specific_unnamed(o,term,title,kind='compiler'):
    label=(f'the unidentified verse author in {title}' if kind=='verse-author'
           else f'the compiler-narrator of {title}')
    o.pop('MasterName',None)
    o['ContextMasters']=[]
    o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':kind,'ActorLabel':label,
      'ActorRole':'verse-author' if kind=='verse-author' else 'compiler-narrator','RungsChecked':RUNGS,
      'GrammarEvidence':f'The complete passage presents {term} in '+('a verse, but supplies no verse-author name.' if kind=='verse-author' else 'compiler narration rather than a marked speech turn.'),
      'ReviewedBy':'Codex f002 A351-400 exact-turn repair','ReviewedUtc':NOW}
    o['AttributionNote']=f'{title}: {label} owns the exact headword-bearing '+('verse.' if kind=='verse-author' else 'narrative clause.')
    o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,
      'FullCaseDecision':f'Full passage read: no named utterer speaks {term}; the stored wording belongs to {label}.'}

for f in REV['findings']:
    if f['verdict']!='REVISE': continue
    p=R/f'fresh-build/entries/{f["id"]}/evidence.draft.json'; root=json.loads(p.read_text(encoding='utf8')); e=root['Entry']
    for s in e['Senses']:
      for o in s['Occurrences']:
        if 'fully reviewed source voice' not in json.dumps(o,ensure_ascii=False): continue
        title=o.get('AttributionNote','').split(':',1)[0] or o['RelPath']
        if f['ordinal'] in NAMED:
          name=NAMED[f['ordinal']]
          o['MasterName']=name;o.pop('ActorAttribution',None)
          o['ContextMasters']=[{'MasterName':name,'Roles':['utterer','record-owner']}]
          o['AttributionNote']=f'{title}: {name} owns the exact headword-bearing statement in the complete section.'
          o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':name,
            'SpeechFrame':'The surrounding section is the named master’s own discourse or marked speech turn.',
            'FullCaseDecision':f'Full passage and section read: {name}, not a generic source voice, utters {f["term"]}.'}
        else:
          verse=any(x in o['Kwic'] for x in ['頌曰','有頌','頌云','偈曰']) or f['ordinal'] in {353,357,358,366,372,376,391}
          specific_unnamed(o,f['term'],title,'verse-author' if verse else 'compiler')
    e['WrittenUtc']=NOW
    p.write_text(json.dumps(root,ensure_ascii=False,indent=2)+'\n',encoding='utf8')

# Rule 16: 刻木人 and 蹋破草鞋 are longer lexical objects, not depth for the bare heads.
for ident,needle in [('t_16bbc5599cd2','刻木人'),('t_6ba271127127','蹋破草鞋')]:
    p=R/f'fresh-build/entries/{ident}/evidence.draft.json'
    if not p.exists(): continue
    root=json.loads(p.read_text(encoding='utf8')); e=root['Entry']
    for s in e['Senses']:
      s['Occurrences']=[o for o in s['Occurrences'] if needle not in o['Kwic']]
      de=s.setdefault('DraftEvidence',{})
      de['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)]
      de.setdefault('FamilyControls',[]).append({'finding':'longer-object-routed','reason':f'{needle} is a distinct longer lexical object and does not buy depth for {e["SourceTerm"]}.'})
    p.write_text(json.dumps(root,ensure_ascii=False,indent=2)+'\n',encoding='utf8')

print('repaired generic rows and Rule-16 rows')
