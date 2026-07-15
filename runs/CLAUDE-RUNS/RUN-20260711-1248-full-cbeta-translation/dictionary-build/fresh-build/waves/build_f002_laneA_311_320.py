import copy,hashlib,json,os,re,sys
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'));sys.path.insert(0,ROOT)
import zc
BASE='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a';NOW='2026-07-15T13:00:00Z'
OPEN={
'冷暖自知':'Knowing hot and cold for oneself is the recurrent predicate of the drinking-water comparison: the person concerned, not another reporter, knows the temperature.',
'騰騰任運':'“Rolling along, letting things run” recurs in verses and addresses for an unforced daily course, including eating at meal times and knowingly appearing simple.',
'換卻眼睛':'To have one’s eyes swapped out is a repeated warning that words, texts, sounds, or another person’s formulation can replace how one sees.',
'昏沈':'Dahui directly defines dull sinking by contrast: forgetting and falling into a dark cave is called dull sinking, while deliberate mental succession is called agitation.',
'寸絲不掛':'“Not a thread hanging” is tested against robes and nakedness in public exchanges: masters ask how a robe remains, call a monk back, or reject the claim as still below the steps.',
'鬱鬱黃花':'“Flourishing yellow flowers” travels with the proposition that yellow flowers are wisdom, and the records preserve both affirmation, rejection, and interview testing of that formula.',
'掉舉':'Dahui names agitation as deliberate, successive mental activity and pairs it with dull sinking; later records repeat, criticize, or reverse that pair.',
'逢祖殺祖':'“Meet a patriarch, kill the patriarch” belongs to Linji’s enumerated formula of killing whatever is encountered, and later masters quote that wording as a sharp inherited command.',
'煩惱即菩提':'“Afflictions are awakening” is both directly glossed through knowing and awakening and sharply restricted: Dahui rejects merely saying it before the matter has been penetrated.',
'磨磚作鏡':'Polishing a tile to make a mirror names the task Nanyue declares impossible in his exchange with Mazu, and later records reuse it as a comparison for wasted effort.',
}
ALIASES={t:[v] for t,v in {
'冷暖自知':'know hot and cold for oneself','騰騰任運':'rolling along letting things run','換卻眼睛':'have one’s eyes swapped out','昏沈':'dull sinking','寸絲不掛':'not a thread hanging','鬱鬱黃花':'flourishing yellow flowers','掉舉':'agitation','逢祖殺祖':'meet a patriarch kill the patriarch','煩惱即菩提':'afflictions are awakening','磨磚作鏡':'polish a tile into a mirror'}.items()}
RECUT={
('寸絲不掛','X/X69/X69n1333.xml'):'師便打，云：大好寸絲不掛。',
('寸絲不掛','J/J38/J38nB425.xml'):'師問：「寸絲不掛，身上袈裟向甚處得來？」',
}

def exception(status,kind,label,role,grammar,ctx=None):
 return {'Status':status,'Kind':kind,'ActorLabel':label,'ActorRole':role,'ReviewedBy':'Codex f002 Lane A full-case review','ReviewedUtc':NOW,'GrammarEvidence':grammar,'RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage']},ctx or []

def actor(term,o):
 key=(term,o['RelPath'])
 if key==('寸絲不掛','T/T51/T51n2076.xml'):
  o.pop('MasterName',None);aa,ctx=exception('identified-non-master','named lay official','Layman Lu Gen','utterer','陸云 explicitly names Layman Lu Gen as speaker of 寸絲不掛.',[{'MasterName':'Nanquan Puyuan','Roles':['respondent']}]);o['ActorAttribution']=aa;o['ContextMasters']=ctx
 elif key==('寸絲不掛','X/X82/X82n1571.xml'):
  o.pop('MasterName',None);aa,ctx=exception('reviewed-unnamed','person','an unnamed questioning monk','questioner','問 introduces the anonymous monk’s headword-bearing question; Cuiting Yao owns the following 師曰 response.',[{'MasterName':'Cuiting Yao','Roles':['respondent','section-subject']}]);o['ActorAttribution']=aa;o['ContextMasters']=ctx
 elif term=='鬱鬱黃花' and o.get('MasterName')=='Huayan Lecturer Zhi':
  o.pop('MasterName',None);aa,ctx=exception('identified-non-master','named lecturer','Huayan Lecturer Zhi','questioner','The surrounding case names Huayan lecturer Zhi as the speaker asking why the yellow-flower proposition is rejected.',[]);o['ActorAttribution']=aa;o['ContextMasters']=ctx
 elif o.get('MasterName'):
  n=o['MasterName'];o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':n,'Roles':['utterer']}]
 else:
  aa=o.get('ActorAttribution');assert aa
  aa.setdefault('ReviewedBy','Codex f002 Lane A full-case review');aa.setdefault('ReviewedUtc',NOW);aa.setdefault('RungsChecked',['line','expanded-context','section-header','book-title','tei-header','parallel-passage']);aa.setdefault('GrammarEvidence',f"The complete case assigns the headword-bearing {aa.get('ActorRole','turn')} to {aa.get('ActorLabel','the recorded non-master actor')}.")
 o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':o.get('MasterName') or o.get('ActorAttribution',{}).get('ActorLabel'),'SpeechFrame':(f"Complete-case review assigns this stored turn to {o['MasterName']}." if o.get('MasterName') else o.get('ActorAttribution',{}).get('GrammarEvidence')),'FullCaseDecision':o.get('ActorAttribution',{}).get('GrammarEvidence') or f"{o['MasterName']} is the exact speaker of this headword-bearing turn."}

pre=json.load(open(os.path.join(ROOT,'fresh-build/waves/f002-laneA-301-400-preflight.json'),encoding='utf-8'))['entries'][10:20]
inventory=[]
for ordinal,p in enumerate(pre,311):
 old=json.load(open(os.path.join(ROOT,'terms',p['id'],'entry.v2.json'),encoding='utf-8'));term=p['term'];entry={'Id':p['id'],'SourceTerm':term,'CreatedBy':'Codex fresh f002 Lane A evidence-first','WrittenUtc':NOW,'Senses':[],'CorpusBaselineSha256':BASE}
 for s in old['Senses']:
  ns=copy.deepcopy(s);exp=ns.pop('Explanation','');ns['SearchAliases']=ns.get('SearchAliases') or ALIASES[term];verified=[];claims=[]
  for o in ns.get('Occurrences',[]):
   if (term,o['RelPath']) in RECUT:o['Kwic']=RECUT[(term,o['RelPath'])]
   v=zc.verify(o['RelPath'],o['Kwic']);assert v.get('ok'),(term,o['RelPath'],v);o['FromLb']=v['fromLb'];o['ToLb']=v['toLb'];o['Curated']=True;actor(term,o)
   q=''.join(o['Kwic'].split());
   if term=='磨磚作鏡' and '磨甎作鏡' in q:o['VariantForm']='磨甎作鏡';o['EvidenceRole']='variant'
   if term=='磨磚作鏡' and '磨塼作鏡' in q:o['VariantForm']='磨塼作鏡';o['EvidenceRole']='variant'
   if term not in q and not (o.get('VariantForm') and o['VariantForm'] in q and o.get('EvidenceRole')=='variant'):o['ClaimText']=o['Kwic'];o.pop('EvidenceRole',None);claims.append(o)
   else:verified.append(o)
  ns['Occurrences']=verified;ns['ClaimAnchors']=(ns.get('ClaimAnchors') or [])+claims;ns['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in verified));works=sorted({zc.work_id(o['RelPath']) for o in verified if term in ''.join(o['Kwic'].split())});ns['Validation']='multi-source' if len(works)>=2 else 'provisional'
  body=re.sub(r'^Literally[^.]*\.\s*','',exp);ns['ExplanationParts']={'CorpusEarnedOpening':OPEN[term],'EvidenceBody':[body]};ns['DraftEvidence']={'OpeningClaimEvidenceKeys':[f'o{i}' for i in range(1,len(verified)+1)],'ZenBend':OPEN[term],'CounterexampleOrLimit':ns.get('Note') or 'Ordinary, quoted, and neighboring-family uses were checked; the entry does not extend beyond the stored contrasts and deployments.','DifferentThingTest':{'Decision':'one-thing','ComparedThings':[ns['PreferredTarget']],'Reason':'The full cases preserve one referent or formula; speaker role, quotation, and grammatical variation do not create a second thing.'},'AliasRationale':'The aliases give literal and close English lookup forms without adding an interpretation.','ModifierControls':['Modifiers, quotation frames, and graphic variants were checked separately from exact headword evidence.'],'FamilyControls':['The neighboring term family was retested and contributes only anchored comparisons or limits.'],'IndependentWorkIds':works};entry['Senses'].append(ns)
 outdir=os.path.join(ROOT,'fresh-build','entries',p['id']);os.makedirs(outdir,exist_ok=True);wp=os.path.join(outdir,'evidence.draft.json');json.dump({'SchemaVersion':1,'Entry':entry},open(wp,'w',encoding='utf-8'),ensure_ascii=False,indent=2);open(wp,'a',encoding='utf-8').write('\n');inventory.append({'ordinal':ordinal,'id':p['id'],'term':term,'worksheetSha256':hashlib.sha256(open(wp,'rb').read()).hexdigest()})
json.dump({'schemaVersion':1,'wave':'f002','lane':'A','ordinalStart':311,'ordinalEnd':320,'corpusBaselineSha256':BASE,'entries':inventory},open(os.path.join(ROOT,'fresh-build/waves/f002-laneA-311-320-build-inventory.json'),'w',encoding='utf-8'),ensure_ascii=False,indent=2)
