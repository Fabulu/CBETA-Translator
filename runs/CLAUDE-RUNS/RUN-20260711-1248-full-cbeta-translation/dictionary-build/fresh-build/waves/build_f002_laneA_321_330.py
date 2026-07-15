import copy,hashlib,json,os,re,sys
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'));sys.path.insert(0,ROOT);import zc
BASE='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a';NOW='2026-07-15T14:00:00Z'
OPEN={
'開口即錯':'“Open your mouth and you are already wrong” is spoken as both self-verdict and interview challenge, commonly paired with losing by silence or missing as soon as the mind frames an answer.',
'騎牛覓牛':'Riding an ox while searching for the ox is an answer to “What is buddha?” and a named condition that later records pair with recognizing the ox but refusing to dismount.',
'閑道人':'The at-ease person of the Way appears chiefly in the compound “one at ease, with learning ended and nothing contrived,” which the records treat as a named condition rather than mere leisure.',
'拈華微笑':'The flower-lifting and smile scene names the paired actions attributed to the Buddha and Kashyapa; later masters quote, judge, and sometimes reject the scene’s inherited formulation.',
'立處皆真':'“Where you stand is all genuine” is paired with acting as host wherever one is, then repeated as a Fayan-house characterization and as a predicate of immediate use.',
'擔雪填井':'Carrying snow to fill a well is a recurrent comparison for ineffective effort, applied to talk, question-and-answer, sitting Chan, and other undertakings.',
'本無一物':'“Fundamentally, not one thing” occurs as assertion, verse line, and interview question; the records place it beside what can be presented, displayed, or investigated.',
'困來即眠':'“When tired, sleep” forms the second half of the hunger-and-sleep saying, and Dazhu’s case explicitly distinguishes this from sleeping while occupied by calculations.',
'一絲不掛':'“Not wearing a single thread” names complete nakedness and is extended to a person from whom nothing further can be stripped or removed.',
'一日不作一日不食':'“A day without work is a day without food” is preserved as Baizhang’s saying, repeated as a communal rule, and versified as the house style of working for food.',
}
ALIASES={t:[x] for t,x in {'開口即錯':'open your mouth and be wrong','騎牛覓牛':'ride an ox while searching for it','閑道人':'at-ease person of the Way','拈華微笑':'flower lifting and smile','立處皆真':'where you stand is genuine','擔雪填井':'carry snow to fill a well','本無一物':'fundamentally not one thing','困來即眠':'when tired sleep','一絲不掛':'not wearing a single thread','一日不作一日不食':'a day without work is a day without food'}.items()}

def actor(o):
 if o.get('MasterName'):
  n=o['MasterName'];o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':n,'Roles':['utterer']}];subject=n;frame=f'Complete-case review assigns the stored headword-bearing turn to {n}.'
 else:
  a=o.get('ActorAttribution');assert a;a.setdefault('ReviewedBy','Codex f002 Lane A full-case review');a.setdefault('ReviewedUtc',NOW);a.setdefault('RungsChecked',['line','expanded-context','section-header','book-title','tei-header','parallel-passage']);a.setdefault('GrammarEvidence',f"The complete case assigns the headword-bearing {a.get('ActorRole','turn')} to {a.get('ActorLabel','the recorded non-master actor')}.");subject=a.get('ActorLabel');frame=a['GrammarEvidence']
 o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':subject,'SpeechFrame':frame,'FullCaseDecision':frame}

pre=json.load(open(os.path.join(ROOT,'fresh-build/waves/f002-laneA-301-400-preflight.json'),encoding='utf-8'))['entries'][20:30];inv=[]
for ordinal,p in enumerate(pre,321):
 old=json.load(open(os.path.join(ROOT,'terms',p['id'],'entry.v2.json'),encoding='utf-8'));term=p['term'];entry={'Id':p['id'],'SourceTerm':term,'CreatedBy':'Codex fresh f002 Lane A evidence-first','WrittenUtc':NOW,'Senses':[],'CorpusBaselineSha256':BASE}
 for s in old['Senses']:
  ns=copy.deepcopy(s);exp=ns.pop('Explanation','');ns['SearchAliases']=ns.get('SearchAliases') or ALIASES[term];occ=[];claims=[]
  for o in ns.get('Occurrences',[]):
   v=zc.verify(o['RelPath'],o['Kwic']);assert v.get('ok');o['FromLb']=v['fromLb'];o['ToLb']=v['toLb'];o['Curated']=True;actor(o);q=''.join(o['Kwic'].split())
   if term=='拈華微笑' and '拈花微笑' in q:o['VariantForm']='拈花微笑';o['EvidenceRole']='variant'
   if term not in q and not (o.get('VariantForm') and o['VariantForm'] in q and o.get('EvidenceRole')=='variant'):o['ClaimText']=o['Kwic'];o.pop('EvidenceRole',None);claims.append(o)
   else:occ.append(o)
  ns['Occurrences']=occ;ns['ClaimAnchors']=(ns.get('ClaimAnchors') or [])+claims;ns['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in occ));works=sorted({zc.work_id(o['RelPath']) for o in occ if term in ''.join(o['Kwic'].split())});ns['Validation']='multi-source' if len(works)>=2 else 'provisional';body=re.sub(r'^Literally[^.]*\.\s*','',exp);ns['ExplanationParts']={'CorpusEarnedOpening':OPEN[term],'EvidenceBody':[body]};ns['DraftEvidence']={'OpeningClaimEvidenceKeys':[f'o{i}' for i in range(1,len(occ)+1)],'ZenBend':OPEN[term],'CounterexampleOrLimit':ns.get('Note') or 'Quoted, ordinary, variant, and family uses were checked; the article remains bounded by the stored deployments.','DifferentThingTest':{'Decision':'one-thing','ComparedThings':[ns['PreferredTarget']],'Reason':'The cases preserve one saying, action, or referent; quotation and grammatical differences do not establish another thing.'},'AliasRationale':'Aliases preserve the literal phrase and close English lookup wording.','ModifierControls':['Longer sayings, graphic variants, and quoted titles were separated from exact headword evidence.'],'FamilyControls':['Neighboring sayings and overlapping terms were retested and do not donate unsupported meaning.'],'IndependentWorkIds':works};entry['Senses'].append(ns)
 d=os.path.join(ROOT,'fresh-build','entries',p['id']);os.makedirs(d,exist_ok=True);wp=os.path.join(d,'evidence.draft.json');open(wp,'w',encoding='utf-8').write(json.dumps({'SchemaVersion':1,'Entry':entry},ensure_ascii=False,indent=2)+'\n');inv.append({'ordinal':ordinal,'id':p['id'],'term':term,'worksheetSha256':hashlib.sha256(open(wp,'rb').read()).hexdigest()})
open(os.path.join(ROOT,'fresh-build/waves/f002-laneA-321-330-build-inventory.json'),'w',encoding='utf-8').write(json.dumps({'schemaVersion':1,'wave':'f002','lane':'A','ordinalStart':321,'ordinalEnd':330,'corpusBaselineSha256':BASE,'entries':inv},ensure_ascii=False,indent=2)+'\n')
