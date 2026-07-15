#!/usr/bin/env python3
import json,re,subprocess,sys,datetime,copy
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
G=json.loads((R/'fresh-build/waves/f003-laneB-751-800-formal-gate.json').read_text());ids={751+i:x['id'] for i,x in enumerate(G['entries'])}
ROLES=re.compile(r'^(?:compiler|documentary narrator|ceremony speaker|ritual compiler|preface author|assembly speaker|monastery-bell inscription author|monastic-rule|ceremony narrator|lineage narrator|lineage sermon speaker|case commentator|commentator|Fuxing monastery speaker)')
NONMASTER=re.compile(r'(?:questioning monk|practitioner|Songshan monk|Zhenzong monk)$')
def stamp():return datetime.datetime.now(datetime.timezone.utc).isoformat()
def actor(o,status,label,kind,role,grammar):
 old=o.pop('MasterName',None);o['ActorAttribution']={'Status':status,'Kind':kind,'ActorLabel':label,'ActorRole':role,'RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'ReviewedBy':'Codex f003 B751-800 repair author','ReviewedUtc':stamp(),'GrammarEvidence':grammar};o['ContextMasters']=[];o['AttributionNote']=f"Source text ({zc.title(o['RelPath'])}): {label} owns the exact headword-bearing wording; {grammar}";o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':grammar,'FullCaseDecision':grammar}
for n,eid in ids.items():
 d=R/'fresh-build/entries'/eid;p=d/'evidence.draft.json';x=json.loads(p.read_text())
 for s in x['Entry']['Senses']:
  for o in s['Occurrences']:
   name=o.get('MasterName','')
   if ROLES.search(name):actor(o,'narrated',name,'documentary narrator','compiler','The headword is governed by documentary, editorial, inscriptional, or ceremonial narration rather than a master utterance.')
   elif NONMASTER.search(name):actor(o,'reviewed-unnamed','the unnamed non-master participant','monastic participant','questioner','The full case identifies a non-master participant but supplies no personal name for the exact headword turn.')
  s['RelatedMasters']=sorted({o['MasterName'] for o in s['Occurrences'] if o.get('MasterName')})
 # Split founding action from founding-abbot title.
 if n==755 and len(x['Entry']['Senses'])==1:
  s=x['Entry']['Senses'][0];office_idx=[1,4];office=copy.deepcopy(s);office['PreferredTarget']='founding abbot';office['AlternateTargets']=['founding elder'];office['SearchAliases']=['founding abbot','founding elder','first abbot'];office['Occurrences']=[s['Occurrences'][i] for i in office_idx];s['Occurrences']=[o for i,o in enumerate(s['Occurrences']) if i not in office_idx];s['PreferredTarget']='found a monastery';s['SearchAliases']=['found a monastery','establish a monastery','open a monastic seat'];s['ExplanationParts']={'CorpusEarnedOpening':'To found a monastery is to establish a new monastic seat and begin its public institutional life.','EvidenceBody':['Biographies use the verb for invitations and appointments to open a named residence; this action is different from the person-title founding abbot.']};office['ExplanationParts']={'CorpusEarnedOpening':'A founding abbot is the first presiding holder of a newly established monastery.','EvidenceBody':['Commemorative and incense language names that first office-holder; it does not describe the later speaker as presently founding the place.']}
  for q in (s,office):
   q['DraftEvidence']['DifferentThingSenseTest']='founding action versus the person holding the first abbacy; KEEP SPLIT';q['DraftEvidence']['SenseTargetDistinguishability']='action: found a monastery; person/office: founding abbot';q['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(q['Occurrences'])+1)];q['DraftEvidence']['IndependentWorkIds']=sorted({zc.work_id(o['RelPath']) for o in q['Occurrences']});q['SourceTexts']=sorted({o['RelPath'] for o in q['Occurrences']});q['RelatedMasters']=sorted({o['MasterName'] for o in q['Occurrences'] if o.get('MasterName')})
  x['Entry']['Senses']=[s,office]
 # Remove out-of-scope generic ritual uses and add an in-scope Chan-record citation.
 if n==758:
  s=x['Entry']['Senses'][0];s['Occurrences']=[o for o in s['Occurrences'] if o['RelPath'] not in {'X/X63/X63n1232.xml','X/X65/X65n1277.xml'}]
  rel='T/T48/T48n2016.xml';kw='又藥王菩薩云。我捨兩臂。必當得佛金色之身。';v=zc.verify(rel,kw);assert v['ok']
  s['Occurrences'].append({'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True,'MasterName':'Yongming Yanshou','ContextMasters':[{'MasterName':'Yongming Yanshou','Roles':['utterer']}],'AttributionNote':f"Source text ({zc.title(rel)}): Yongming Yanshou quotes Medicine King's relinquishing both arms and then comments on the wording.",'DraftActorProof':{'ExactHeadwordClause':kw,'GrammaticalSubject':'Yongming Yanshou','SpeechFrame':'The sentence lies within Yongming Yanshou’s continuous authorial exposition.','FullCaseDecision':'Yongming Yanshou is the present exact headword utterer while quoting Medicine King.'}});s['SourceTexts']=sorted({o['RelPath'] for o in s['Occurrences']});s['RelatedMasters']=sorted({o['MasterName'] for o in s['Occurrences'] if o.get('MasterName')})
 # Remove Japanese-source contamination and retain six in-scope defining witnesses.
 if n==775:
  s=x['Entry']['Senses'][0];s['Occurrences']=[o for o in s['Occurrences'] if o.get('MasterName')!='Dogen' and o['RelPath']!='D/D51/D51n8948.xml'];s['SourceTexts']=sorted({o['RelPath'] for o in s['Occurrences']});s['RelatedMasters']=sorted({o['MasterName'] for o in s['Occurrences'] if o.get('MasterName')})
 # The headword is in the questioner's turn, never the respondent's answer.
 if n==784:
  s=x['Entry']['Senses'][0]
  # Drop the quoted 'an ancient said' row whose master cannot be resolved.
  s['Occurrences']=[o for o in s['Occurrences'] if o['RelPath']!='X/X81/X81n1568.xml']
  for o in s['Occurrences']:actor(o,'reviewed-unnamed','the unnamed non-master questioner','monastic questioner','questioner','The exact headword is inside the marked question before the separately marked master response; the record gives no personal name for the questioner.')
  rel='B/B25/B25n0144.xml';kw='問：「如何是禪？」師云：「露柱吞蝦蟆。」';v=zc.verify(rel,kw);assert v['ok']
  o={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True};actor(o,'reviewed-unnamed','the unnamed non-master questioner','monastic questioner','questioner','The explicit 問 marker assigns the headword question to an unnamed participant; the master begins only at 師云.');s['Occurrences'].append(o);s['SourceTexts']=sorted({o['RelPath'] for o in s['Occurrences']});s['RelatedMasters']=[]
 # Preserve Xiangji's three distinct referents; repair the two non-master syntax labels.
 if n==800:
  for s in x['Entry']['Senses']:
   s['RelatedMasters']=sorted({o['MasterName'] for o in s['Occurrences'] if o.get('MasterName')})
 for s in x['Entry']['Senses']:
  s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)]
  s['DraftEvidence']['IndependentWorkIds']=sorted({zc.work_id(o['RelPath']) for o in s['Occurrences']})
 p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True,stdout=subprocess.DEVNULL)
print('repaired',len(ids))
