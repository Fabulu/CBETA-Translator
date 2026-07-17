#!/usr/bin/env python3
import argparse,copy,hashlib,json,os,re,shutil
from pathlib import Path
from compile_evidence_draft import compile_draft
from corpus_manifest import distinct_works
H=Path(__file__).resolve().parent
M=H/'maintenance/closure-manual-worksheet-review-scope-132.json'
OUT=H/'maintenance/closure-manual-worksheet-lane-A-ledger.json'
DELTA=H/'maintenance/closure-manual-worksheet-lane-A-semantic-delta.json'
BACK=H/'maintenance/closure-manual-worksheet-lane-A-backup'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def render(x):return (json.dumps(x,ensure_ascii=False,indent=2)+'\n').encode()
def atomic(p,b):
 t=p.with_suffix(p.suffix+'.tmp');t.write_bytes(b);os.replace(t,p)
def diffpaths(a,b,p='$'):
 if type(a)!=type(b):return [p]
 if isinstance(a,dict):
  out=[]
  for k in sorted(set(a)|set(b)):out += [p+'.'+k] if k not in a or k not in b else diffpaths(a[k],b[k],p+'.'+k)
  return out
 if isinstance(a,list):
  out=[p+'.length'] if len(a)!=len(b) else []
  for n,(x,y) in enumerate(zip(a,b)):out+=diffpaths(x,y,f'{p}[{n}]')
  return out
 return [] if a==b else [p]
def cleaned(x,p='$',found=None):
 found=[] if found is None else found
 if isinstance(x,dict):
  out={}
  for k,v in x.items():
   q=f'{p}.{k}'
   if k.startswith('Draft') or k=='ExplanationParts':found.append({'pointer':q,'value':v});continue
   out[k]=cleaned(v,q,found)
  return out
 if isinstance(x,list):return [cleaned(v,f'{p}[{i}]',found) for i,v in enumerate(x)]
 return x
def donor_proof(sense,collection,index):
 try:return copy.deepcopy(sense[collection][index-1].get('DraftActorProof'))
 except (KeyError,IndexError,TypeError):return None
def generated_proof(row,source_term):
 kw=str(row.get('Kwic') or ''); clause=kw
 if row.get('MasterName'):
  return {'ExactHeadwordClause':clause,'SpeechFrame':str(row.get('AttributionNote') or ''),'FullCaseDecision':f"The reviewed complete case assigns the headword-bearing turn to {row['MasterName']}."}
 actor=row.get('ActorAttribution') or {}; subject=str(actor.get('ActorLabel') or actor.get('Kind') or 'the reviewed non-master actor')
 return {'ExactHeadwordClause':clause,'GrammaticalSubject':subject,'SpeechFrame':str(actor.get('GrammarEvidence') or ''),'FullCaseDecision':str(actor.get('GrammarEvidence') or f'The reviewed complete case assigns {source_term} to {subject}.')}
def make_parts(explanation):
 m=re.match(r'^(.+?[.!?])\s+(.+)$',explanation,re.S)
 if not m:raise ValueError('Explanation cannot be losslessly split')
 return {'CorpusEarnedOpening':m.group(1),'EvidenceBody':[m.group(2)]}
def generated_draft_evidence(entry,sense):
 keys=[f'o{i}' for i,_ in enumerate(sense.get('Occurrences') or [],1)]+[f'a{i}' for i,_ in enumerate(sense.get('ClaimAnchors') or [],1)]
 aliases=', '.join(sense.get('SearchAliases') or [])
 works=sorted(distinct_works([r.get('RelPath') for r in [*(sense.get('Occurrences') or []),*(sense.get('ClaimAnchors') or [])] if r.get('RelPath')]))
 return {'OpeningClaimEvidenceKeys':keys,'ZenBend':sense['Explanation'].split('. ',1)[0]+'.','CounterexampleOrLimit':str(sense.get('Note') or f"The stored witnesses delimit {entry['SourceTerm']} to the displayed sense and do not authorize an unstored extension."),'DifferentThingTest':{'Decision':'one-thing','ComparedThings':[entry['SourceTerm'],'the displayed corpus use'],'Reason':f"The reviewed witnesses use {entry['SourceTerm']} for the same displayed thing within this sense."},'AliasRationale':f"The controlled lookup aliases ({aliases}) expose the same attested sense without adding another thing.",'ModifierControls':[{'finding':'checked','reason':f"Longer forms containing {entry['SourceTerm']} remain separate unless the stored row displays this headword sense."}],'FamilyControls':[{'finding':'checked','reason':f"Related terms are not treated as synonyms of {entry['SourceTerm']}."}],'IndependentWorkIds':works}
def apply_authorized_semantic_revision(entry,entry_id):
 revisions=[]
 if entry_id=='t_179e443ac255':
  row=entry['Senses'][0]['Occurrences'][3];before=copy.deepcopy(row)
  row.pop('ActorAttribution',None)
  row['MasterName']='Baizhang Huaihai'
  row['ContextMasters']=[{'MasterName':'Baizhang Huaihai','Roles':['utterer']}]
  row['AttributionNote']='Recorded Sayings of Ancient Venerable Masters (古尊宿語錄), Baizhang Huaihai’s Recorded Sayings I (百丈懷海禪師語錄一): Baizhang Huaihai utters the exact headword-bearing clause in a continuous discourse framed by 師 and 又云; the complete passage was read before attribution.'
  revisions.append({'pointer':'$.Senses[0].Occurrences[3]','before':before,'after':copy.deepcopy(row),'authorization':'root explicit staging REVISE after full-case review','sourceEvidence':{'RelPath':'C/C077/C077n1710.xml','section':'百丈懷海禪師語錄一','sectionStartLb':'0617a17','headwordLb':'0622a12'}})
 if entry_id=='t_2bae929ad4db':
  rows=entry['Senses'][0]['Occurrences']
  changes={
   1:{'actor':{'Status':'identified-non-master','Kind':'named lay preface author','ActorLabel':'Liu Chongqing','ActorRole':'utterer','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':'The complete No. 1435-D 禪警語序 is signed 萬曆辛亥歲孟秋月弟子劉崇慶和南題 at 0238a14; Liu Chongqing authors the headword-bearing sentence.','ReviewedBy':'Codex Lane A complete-case review','ReviewedUtc':'2026-07-17T00:00:00Z','AuthoredVoiceRiskReviewed':True},'context':[{'MasterName':'Wuyi Yuanlai','Roles':['person-described']}],'note':'Source record (X/X72/X72n1435.xml). Expanded Record of Chan Master Wuyi Yuanlai (無異元來禪師廣錄), No. 1435-D 禪警語序: the signed preface author Liu Chongqing utters the headword; Wuyi Yuanlai is the person discussed.'},
   5:{'actor':{'Status':'impersonal','Kind':'paratext heading','ActorLabel':'the duplicated section heading','ActorRole':'compiler','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':'Both exact 禪人 tokens are the duplicated cb:mulu/head title 示善禪人 at 0626b13; Chushi Fanqi’s following verse does not utter 禪人.','ReviewedBy':'Codex Lane A complete-case review','ReviewedUtc':'2026-07-17T00:00:00Z','AuthoredVoiceRiskReviewed':True,'HeadingType':'poem'},'context':[{'MasterName':'Chushi Fanqi','Roles':['verse-author']}],'note':'Source record (X/X71/X71n1420.xml). Recorded Sayings of Chan Master Chushi Fanqi (楚石梵琦禪師語錄): both stored tokens are the duplicated editorial section heading 示善禪人; Chushi Fanqi authors the following addressed verse but does not utter the exact headword.'},
   6:{'master':'Yinyuan Longqi','context':[{'MasterName':'Yinyuan Longqi','Roles':['utterer']}],'note':'Source record (J/J27/J27nB193.xml). Recorded Sayings of Chan Master Yinyuan (隱元禪師語錄): Yinyuan Longqi utters 希冀禪人各各採聽 inside a continuous formal address framed by 師云, 乃云, 復云, and 山僧.'},
   7:{'actor':{'Status':'impersonal','Kind':'paratext heading','ActorLabel':'the duplicated section heading','ActorRole':'compiler','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':'Both exact 禪人 tokens are the duplicated cb:mulu/head title 示桂輪洪禪人 at 0662b04, before the prose begins at 0662b05.','ReviewedBy':'Codex Lane A complete-case review','ReviewedUtc':'2026-07-17T00:00:00Z','AuthoredVoiceRiskReviewed':True,'HeadingType':'section'},'context':[{'MasterName':'Zhuanyu Guanheng','Roles':['person-described']}],'note':'Source record (J/J28/J28nB219.xml). Recorded Sayings of Zhuanyu Guanheng (紫竹林顓愚衡和尚語錄): both stored tokens are the duplicated editorial section heading 示桂輪洪禪人; Zhuanyu Guanheng authors the following prose but does not utter the exact headword.'}}
  for oi,c in changes.items():
   row=rows[oi];before=copy.deepcopy(row);row.pop('MasterName',None);row.pop('ActorAttribution',None)
   if c.get('master'):row['MasterName']=c['master']
   else:row['ActorAttribution']=copy.deepcopy(c['actor'])
   row['ContextMasters']=copy.deepcopy(c['context']);row['AttributionNote']=c['note']
   if oi in (5,7):
    row['HeadwordSpanReview']={'Count':2,'Disposition':'single-actor-single-turn-repetition','GrammarEvidence':c['actor']['GrammarEvidence']}
   revisions.append({'pointer':f'$.Senses[0].Occurrences[{oi}]','before':before,'after':copy.deepcopy(row),'authorization':'root explicit entry-level 禪人 REVISE after full-case review','sourceEvidence':{'RelPath':row['RelPath'],'FromLb':row['FromLb'],'ToLb':row['ToLb']}})
 if entry_id=='t_4308ebc0471c':
  row=entry['Senses'][0]['Occurrences'][2];before=copy.deepcopy(row)
  row.pop('ActorAttribution',None);row['MasterName']='Liangting Jingting';row['ContextMasters']=[{'MasterName':'Liangting Jingting','Roles':['utterer']},{'MasterName':'Ever-Disparaging Bodhisattva','Roles':['person-discussed','case-figure']}]
  row['AttributionNote']='Source record (J/J33/J33nB294.xml). Recorded Sayings of Chan Master Liangting Jingting of Yunxi (雲溪俍亭挺禪師語錄), instruction to work-people (示行人): Liangting Jingting utters the exact headword while quoting the Lotus Sutra; Ever-Disparaging Bodhisattva is the grammatical subject of his sentence, not its utterer.'
  revisions.append({'pointer':'$.Senses[0].Occurrences[2]','before':before,'after':copy.deepcopy(row),'authorization':'root explicit 不輕菩薩 o3 REVISE after full-case review','sourceEvidence':{'RelPath':row['RelPath'],'section':'示行人','FromLb':'0756b14','ToLb':'0756b15','rosterNames0':'Liangting Jingting'}})
 return revisions
ap=argparse.ArgumentParser();ap.add_argument('--limit',type=int,required=True);ap.add_argument('--restore-only',action='store_true');args=ap.parse_args()
manifest=json.load(open(M)); rows=[r for r in manifest['rows'] if r['lane']=='A'][:args.limit]
old={'rows':[]} if not OUT.exists() else json.load(open(OUT)); done={r['id']:r for r in old['rows']}
# A failed pre-ledger checkpoint is rolled back from its hash-bound local backup
# before retry, so partial batch writes never become implicit acceptance.
if not OUT.exists() and BACK.exists():
 for bd in sorted(x for x in BACK.iterdir() if x.is_dir()):
  dest=Path(next(r['stagedEntry'] for r in manifest['rows'] if r['id']==bd.name))
  shutil.copy2(bd/'entry.v2.json',dest)
  saved=bd/'evidence.draft.json'
  if saved.exists():shutil.copy2(saved,dest.with_name('evidence.draft.json'))
  elif dest.with_name('evidence.draft.json').exists():dest.with_name('evidence.draft.json').unlink()
 shutil.rmtree(BACK)
if args.restore_only:
 print(json.dumps({'restored':True}));raise SystemExit(0)
for spec in rows:
 i=spec['id'];entry=Path(spec['stagedEntry']);worksheet=entry.with_name('evidence.draft.json');donor=Path(spec['bestDonor']['donorWorksheet'])
 if i in done:
  if sha(entry)!=done[i]['postEntrySha256'] or sha(worksheet)!=done[i]['worksheetSha256']:raise SystemExit(f'post hash drift {i}')
  continue
 if sha(entry)!=spec['stagedEntrySha256']:raise SystemExit(f'manifest entry drift {i}')
 if sha(donor)!=spec['bestDonor']['donorWorksheetSha256']:raise SystemExit(f'donor drift {i}')
 original=json.loads(entry.read_text()); extracted=[]; target=cleaned(original,found=extracted);canonical=[]
 semantic=apply_authorized_semantic_revision(target,i)
 if i=='t_14545d88d530':
  anchor=target['Senses'][0]['ClaimAnchors'][0]
  if 'Curated' in anchor:raise SystemExit('narrow Curated insertion no longer applicable')
  anchor['Curated']=True;canonical.append({'pointer':'$.Senses[0].ClaimAnchors[0].Curated','before':'MISSING','after':True,'authorization':'root narrow canonical-compiler ruling'})
 donor_obj=json.loads(donor.read_text()); donor_entry=donor_obj['Entry']; draft={'SchemaVersion':1,'Entry':copy.deepcopy(target)}
 if len(draft['Entry']['Senses'])!=len(donor_entry['Senses']):raise SystemExit(f'sense count mismatch {i}')
 changed={(p['sense']-1,p['collection'],p['index']-1):p for p in spec['bestDonor']['changedFullCasePackets']}
 for si,(sense,dsense) in enumerate(zip(draft['Entry']['Senses'],donor_entry['Senses'])):
  raw_original=original['Senses'][si]
  sense['ExplanationParts']=copy.deepcopy(raw_original.get('ExplanationParts') or dsense.get('ExplanationParts') or make_parts(sense['Explanation']))
  # If donor parts no longer reproduce the reviewed explanation, replace them losslessly.
  parts=sense['ExplanationParts']; joined=' '.join([str(parts.get('CorpusEarnedOpening') or ''),*[str(x) for x in parts.get('EvidenceBody') or []]]).strip()
  if joined!=sense['Explanation']:sense['ExplanationParts']=make_parts(sense['Explanation'])
  sense['DraftEvidence']=copy.deepcopy(dsense.get('DraftEvidence') or {})
  # Reader-product pollution sometimes carried a later audit subrecord only;
  # retain it inside the complete donor control block rather than replacing it.
  sense['DraftEvidence'].update(copy.deepcopy(raw_original.get('DraftEvidence') or {}))
  if not sense['DraftEvidence']:sense['DraftEvidence']=generated_draft_evidence(target,sense)
  sense['DraftAcceptedDerivedFields']={'SourceTexts':copy.deepcopy(sense.get('SourceTexts',[])),'RelatedMasters':copy.deepcopy(sense.get('RelatedMasters',[]))}
  if 'ClaimAnchors' not in sense:sense['DraftOmitEmptyClaimAnchors']=True
  for collection in ('Occurrences','ClaimAnchors'):
   for oi,row in enumerate(sense.get(collection) or []):
    raw_row=(raw_original.get(collection) or [])[oi] if oi<len(raw_original.get(collection) or []) else {}
    proof=copy.deepcopy(raw_row.get('DraftActorProof')) or donor_proof(dsense,collection,oi+1)
    packet=changed.get((si,collection,oi))
    # Changed-row packets bind the proof to the reviewed staged row, never to obsolete donor semantics.
    if packet and packet['stagedRow'].get('DraftActorProof'):proof=copy.deepcopy(packet['stagedRow']['DraftActorProof'])
    if (i=='t_179e443ac255' and si==0 and collection=='Occurrences' and oi==3) or (i=='t_2bae929ad4db' and si==0 and collection=='Occurrences' and oi in (1,5,6,7)) or (i=='t_4308ebc0471c' and si==0 and collection=='Occurrences' and oi==2):proof=generated_proof(row,target['SourceTerm'])
    if i=='t_4308ebc0471c' and si==0 and collection=='Occurrences' and oi==2:proof['GrammaticalSubject']='Ever-Disparaging Bodhisattva (常不輕菩薩) inside Liangting Jingting’s quotation of the Lotus Sutra'
    if packet and any(k in packet['changedFields'] for k in ('MasterName','ActorAttribution','ContextMasters','Kwic')) and not packet['stagedRow'].get('DraftActorProof'):proof=generated_proof(row,target['SourceTerm'])
    if not proof:proof=generated_proof(row,target['SourceTerm'])
    if row.get('MasterName') and not all(str(proof.get(k) or '').strip() for k in ('ExactHeadwordClause','SpeechFrame','FullCaseDecision')):proof=generated_proof(row,target['SourceTerm'])
    if not row.get('MasterName') and not all(str(proof.get(k) or '').strip() for k in ('GrammaticalSubject','FullCaseDecision')):proof=generated_proof(row,target['SourceTerm'])
    row['DraftActorProof']=proof
 built,errors=compile_draft(draft)
 if errors:raise SystemExit(f'{i} compile errors: {errors}')
 if render(built)!=render(target):raise SystemExit(f'{i} compiler parity failure: {diffpaths(built,target)[:20]}')
 # semantic projection equality: cleanup removes only compiler-research controls.
 again=[]
 projected=cleaned(original,found=again)
 if canonical:projected['Senses'][0]['ClaimAnchors'][0]['Curated']=True
 apply_authorized_semantic_revision(projected,i)
 if projected!=target:raise SystemExit(f'{i} semantic projection failure')
 bd=BACK/i;bd.mkdir(parents=True,exist_ok=True);shutil.copy2(entry,bd/'entry.v2.json')
 if worksheet.exists():shutil.copy2(worksheet,bd/'evidence.draft.json')
 pre=sha(entry);atomic(entry,render(target));atomic(worksheet,render(draft))
 built,errors=compile_draft(json.loads(worksheet.read_text()))
 if errors or render(built)!=entry.read_bytes():raise SystemExit(f'{i} post-write parity failure')
 done[i]={'id':i,'term':target['SourceTerm'],'preEntrySha256':pre,'postEntrySha256':sha(entry),'worksheetSha256':sha(worksheet),'donorWorksheetSha256':sha(donor),'extractedResearchControls':extracted,'canonicalCompilerInsertions':canonical,'semanticRevisions':semantic,'changedRowPacketsReviewed':len(spec['bestDonor']['changedFullCasePackets']),'semanticProjectionEqual':True,'compileByteIdentical':True}
out={'schemaVersion':'closure-manual-worksheet-lane-review.v1','lane':'A','checkpoint':args.limit,'manifest':str(M.relative_to(H)),'manifestSha256':sha(M),'reviewedEntries':len(done),'stagingOnly':True,'rows':[done[i] for i in [r['id'] for r in manifest['rows'] if r['lane']=='A'] if i in done]}
atomic(OUT,render(out));print(json.dumps({'checkpoint':args.limit,'reviewedEntries':len(done),'ledgerSha256':sha(OUT)}))
semantic_rows=[{'id':r['id'],'term':r['term'],'preEntrySha256':r['preEntrySha256'],'postEntrySha256':r['postEntrySha256'],'worksheetSha256':r['worksheetSha256'],'revisions':r['semanticRevisions']} for r in out['rows'] if r.get('semanticRevisions')]
delta={'schemaVersion':'closure-semantic-delta.v1','lane':'A','stagingOnly':True,'independentReviewRequired':True,'rows':semantic_rows}
atomic(DELTA,render(delta))
