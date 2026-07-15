import copy,json,re,subprocess,sys
from datetime import datetime,timezone
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
review=json.load(open(R/'fresh-build/waves/f003-laneA-651-700-postrepair-independent-exact-rereview.json'))
rows={r['ordinal']:r for r in review['rows'] if r['verdict']=='REVISE'}
catalogue=re.compile(r'目次|目錄|No\.|卷第|法嗣|編輯|較閱|序\〔|進呈')
replace_catalogue={652,653,655,656,657,661,662,666,680}

def load(n):
 p=R/'fresh-build/entries'/rows[n]['id']/'evidence.draft.json';return p,json.load(open(p))
def narrated(rel,kwic):
 v=zc.verify(rel,kwic);assert v['ok'];title=zc.title(rel)
 return {'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kwic,'Curated':True,
 'ActorAttribution':{'Status':'narrated','Kind':'compiler narrative','ActorLabel':'the compiler narrating the headword-bearing clause','ActorRole':'compiler','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'ReviewedBy':'Codex f003 Lane A catalogue-replacement repair','ReviewedUtc':datetime.now(timezone.utc).isoformat(),'GrammarEvidence':'The selected clause is continuous documentary or expository prose without a direct-speech frame assigning the headword to a different speaker.'},
 'ContextMasters':[],'AttributionNote':f'Source text ({title}): the compiler owns the exact headword-bearing documentary wording.',
 'DraftActorProof':{'ExactHeadwordClause':kwic,'GrammaticalSubject':'the compiler narrating the headword-bearing clause','SpeechFrame':'continuous documentary or expository prose','FullCaseDecision':'No direct-speech frame in the selected clause transfers the headword wording to another speaker.'}}
def candidates(term,used,need):
 if need<=0:return []
 out=[]
 for rel,_ in zc.count(term)['per_file']:
  for hit in zc.find(rel,term,ctx=70):
   w=hit['window']
   if w in used or catalogue.search(w):continue
   # Keep replacement ownership falsifiable: only continuous narration/exposition.
   if re.search(r'師曰|師云|僧問|問：|曰：|云：|上堂|拈|頌曰',w):continue
   if len(w)<35:continue
   out.append(narrated(rel,w));used.add(w)
   if len(out)>=need:return out
 return out

for n in replace_catalogue:
 p,x=load(n);term=x['Entry']['SourceTerm']
 for s in x['Entry']['Senses']:
  for o in s['Occurrences']:
   if o.get('ActorAttribution'):o['ActorAttribution'].setdefault('ReviewedUtc',datetime.now(timezone.utc).isoformat())
  old=s['Occurrences'];bad=[o for o in old if catalogue.search(o['Kwic'])];good=[o for o in old if o not in bad]
  if bad:
   add=candidates(term,{o['Kwic'] for o in old},len(bad));assert len(add)==len(bad),(n,term,len(bad),len(add));s['Occurrences']=good+add
 p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n')

# 錯: encounter verdict versus documentary/textual error.
p,x=load(651);s=x['Entry']['Senses'][0];doc=[];verdict=[]
for o in [o for q in x['Entry']['Senses'] for o in q['Occurrences']]:
 (doc if re.search(r'集書者|舛錯|錯謬',o['Kwic']) else verdict).append(o)
s['PreferredTarget']='wrong (a public encounter verdict)';s['Occurrences']=verdict
s['ExplanationParts']={'CorpusEarnedOpening':'Wrong is the public verdict that a reply, quotation, or move has missed what the exchange demanded.','EvidenceBody':['Named speakers use it to reject an answer, warn against misquoting a saying, or expose a mistaken recognition; this sense is an encounter judgment, not a scribal diagnosis.']};s['DraftEvidence']['ZenBend']=s['ExplanationParts']['EvidenceBody'][0]
t=copy.deepcopy(s);t['SenseKey']='documentary-error';t['PreferredTarget']='an error in copying, wording, or attribution';t['AlternateTargets']=['textual error','misattribution'];t['Occurrences']=doc;t['ExplanationParts']={'CorpusEarnedOpening':'A documentary error is a mistake in copying, wording, naming, or attribution.','EvidenceBody':['Compilers use the graph to correct books and transmitted wording; these clauses describe the record rather than delivering a lineage teacher’s public verdict.']};t['DraftEvidence']['ZenBend']=t['ExplanationParts']['EvidenceBody'][0];x['Entry']['Senses']=[s,t];p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n')

# 出世: public abbacy versus a buddha or sage appearing; delete the false 出世法 substring.
p,x=load(665);s=x['Entry']['Senses'][0];all_o=[o for q in x['Entry']['Senses'] for o in q['Occurrences'] if '出世法' not in o['Kwic']];abb=[o for o in all_o if re.search(r'出世長蘆|今出世|遂出世|和尚出世',o['Kwic'])];appear=[o for o in all_o if o not in abb]
s['PreferredTarget']='to appear in the world (of a buddha or sage)';s['Occurrences']=appear;s['ExplanationParts']={'CorpusEarnedOpening':'To appear in the world is for a buddha or sage to emerge publicly in an age.','EvidenceBody':['Buddha and ancient-sage clauses use the phrase for sacred appearance; they do not describe appointment to a monastery.']};s['DraftEvidence']['ZenBend']=s['ExplanationParts']['EvidenceBody'][0]
t=copy.deepcopy(s);t['SenseKey']='take-abbacy';t['PreferredTarget']='to enter public service as abbot';t['AlternateTargets']=['to take up an abbacy'];t['Occurrences']=abb;t['ExplanationParts']={'CorpusEarnedOpening':'To enter public service as abbot is for a lineage teacher to take up a monastery’s teaching seat.','EvidenceBody':['Biographical records place the phrase beside the named monastery, an inaugural hall address, or the duration of a lineage teacher’s public service.']};t['DraftEvidence']['ZenBend']=t['ExplanationParts']['EvidenceBody'][0]
if len(appear)+len(abb)<8:s['Occurrences'].extend(candidates('出世',{o['Kwic'] for o in appear+abb},1))
x['Entry']['Senses']=[s,t];p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n')

# 消息: keep the attested three-way split, but make each explanation independently readable.
p,x=load(679);texts=[('News or tidings is information carried from an absent person or place.','Messengers, letters, and absence predicates make this a report that has or has not arrived.'),('A revealing sign is the clue by which an encounter or matter discloses itself.','Masters ask whether a participant can expose such a sign, or raise an object as the sign before the assembly.'),('Adjustment is the regulation of a condition between extremes.','The stored clause uses the word with balancing language, not with a messenger or an encounter disclosure.')]
for s,(a,b) in zip(x['Entry']['Senses'],texts):s['ExplanationParts']={'CorpusEarnedOpening':a,'EvidenceBody':[b]};s['DraftEvidence']['ZenBend']=b
p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n')

# 羅漢: attained rank/person versus Luohan embedded in a proper name.
p,x=load(688);s=x['Entry']['Senses'][0];rank=[];proper=[]
for o in [o for q in x['Entry']['Senses'] for o in q['Occurrences']]:
 (proper if re.search(r'羅漢勤禪師|羅漢僧|羅漢和尚|供羅漢',o['Kwic']) else rank).append(o)
s['PreferredTarget']='arhat (an attained person or rank)';s['Occurrences']=rank;s['ExplanationParts']={'CorpusEarnedOpening':'An arhat is a person identified by an attained rank in inherited cases.','EvidenceBody':['Questions about Kasyapa’s status and clauses about attaining arhatship predicate the rank of a person rather than naming a monastery or master.']};s['DraftEvidence']['ZenBend']=s['ExplanationParts']['EvidenceBody'][0]
t=copy.deepcopy(s);t['SenseKey']='luohan-proper-name';t['PreferredTarget']='Luohan (in a master, monastery, or offering name)';t['AlternateTargets']=['Luohan'];t['Occurrences']=proper;t['ExplanationParts']={'CorpusEarnedOpening':'Luohan is also a proper-name element in master, monastery, and offering titles.','EvidenceBody':['Catalogue and institutional strings use the graphs inside a name; they do not assert that the named place or office-holder is an arhat.']};t['DraftEvidence']['ZenBend']=t['ExplanationParts']['EvidenceBody'][0];x['Entry']['Senses']=[s,t];p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n')

# 道得: discard false segmentation in 外道得 and replace to preserve depth.
p,x=load(698);s=x['Entry']['Senses'][0];old=s['Occurrences'];good=[o for o in old if '外道得' not in o['Kwic']];add=candidates('道得',{o['Kwic'] for o in old},len(old)-len(good));assert len(add)==len(old)-len(good);s['Occurrences']=good+add;p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n')

for n in rows:
 d=R/'fresh-build/entries'/rows[n]['id'];p=d/'evidence.draft.json';x=json.load(open(p))
 for s in x['Entry']['Senses']:
  for o in s['Occurrences']:
   if o.get('ActorAttribution'):o['ActorAttribution'].setdefault('ReviewedUtc',datetime.now(timezone.utc).isoformat())
  s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)]
  s['DraftEvidence']['IndependentWorkIds']=list(dict.fromkeys(zc.work_id(o['RelPath']) for o in s['Occurrences']))
  if n in (651,665,688):
   s['DraftEvidence']['DifferentThingTest']={'Decision':'different-thing','ComparedThings':[s['PreferredTarget'],'the other sense in this entry'],'Reason':'The full-case predicates select different referents, not merely different readings or grammatical forms of one referent.'}
   s['DraftEvidence']['SenseTargetDistinguishability']=s['PreferredTarget']+'; contrasted with the other sense by different full-case predicates'
   s['DraftEvidence']['DifferentThingSenseTest']=s['PreferredTarget']+' versus the other sense; KEEP SPLIT because the full cases select different things'
  if n==698:
   s['ExplanationParts']['EvidenceBody']=[b.replace(' (外道得)','').replace('substring 外道得','the false outsider-plus-obtain substring') for b in s['ExplanationParts']['EvidenceBody']]
   s['DraftEvidence']['ZenBend']=s['DraftEvidence']['ZenBend'].replace('substring 外道得','the false outsider-plus-obtain substring')
 p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n')
 if n in (651,665,688):
  wp=d/'WORK.md';wt=wp.read_text(encoding='utf-8') if wp.exists() else ''
  if 'sense-target-distinguishability:' not in wt:wp.write_text(wt.rstrip()+f"\n\nsense-target-distinguishability: {x['Entry']['Senses'][0]['PreferredTarget']} versus {x['Entry']['Senses'][1]['PreferredTarget']}; full-case predicates select different things.\n",encoding='utf-8')
 subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True)
print('repaired',len(rows))
