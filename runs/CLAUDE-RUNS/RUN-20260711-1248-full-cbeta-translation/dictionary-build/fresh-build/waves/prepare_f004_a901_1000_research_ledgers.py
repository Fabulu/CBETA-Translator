import datetime,hashlib,json,re
from pathlib import Path
HERE=Path(__file__).resolve().parent;ROOT=HERE.parent.parent
pre=HERE/'f004-laneA-901-1000-preflight.json';wave=HERE/'f004.json'
p=json.loads(pre.read_text());w=json.loads(wave.read_text());ph=hashlib.sha256(pre.read_bytes()).hexdigest();now=datetime.datetime.now(datetime.timezone.utc).isoformat()
all_terms=[x['term'] for x in w['entries']]
catalogue=re.compile(r'(?:目錄|卷第|contents|catalog|序$|住.+語錄)')
titleish={'續傳燈錄','五燈會元','黃龍三關','十牛圖','圓覺經','金剛經','法華經'}
personish={'裴休','張無盡','孟子','陸亘大夫','黃帝','老子','孔子','龐居士'}
institution={'知事','法座','坐夏','開堂','入院','院主','維那','知客','堂頭和尚'}
def actor_candidate(window,term):
 pos=window.find(term);prex=window[max(0,pos-45):pos] if pos>=0 else window[:80]
 if re.search(r'(?:僧問|問[:：。])',prex):return {'candidate':'unnamed questioner','status':'requires-full-case','reason':'A 問/僧問 frame occurs before the headword; do not assign the following 師 reply.'}
 if catalogue.search(window):return {'candidate':'compiler/editorial narration','status':'catalogue-risk','reason':'The discovery window contains catalogue/title structure; exclude unless a full case supplies lexical use.'}
 if re.search(r'(?:師曰|師云|上堂|示眾)',prex):return {'candidate':'enclosing named master','status':'requires-full-case','reason':'A marked master/address frame precedes the headword; resolve the section and reconstruct the exact turn.'}
 return {'candidate':'unresolved actor or narrator','status':'requires-full-case','reason':'The KWIC window alone does not decide utterer versus narration.'}
def split_candidates(term):
 out=[]
 if term in personish:out.append('person/name versus ordinary graph or title use')
 if term in titleish:out.append('book title versus lexical phrase or quoted title')
 if term in institution:out.append('institutional office/object versus ordinary action or place')
 if len(term)<=2:out.append('bare graph/word versus compounds and named persons or places')
 if any(x in term for x in ('眼','門','堂','座','印','關','路','床','衣','鉢','金','銀')):out.append('literal object/place versus corpus-established institutional or lineage referent')
 return out or ['test whether apparently varied readings denote one thing or genuinely different referents']
rows=[]
for ordinal,e in enumerate(p['entries'],901):
 families=sorted({t for t in all_terms if t!=e['term'] and (e['term'] in t or t in e['term'])})[:20]
 works=[];actors=[];excluded=[]
 for cw in e.get('candidateWorks',[]):
  works.append({'workId':cw['workId'],'RelPath':cw['RelPath'],'title':cw.get('title'),'fileHits':cw.get('fileHits')})
  for wi,x in enumerate(cw.get('windows',[]),1):
   ac=actor_candidate(x.get('window',''),e['term']);actors.append({'workId':cw['workId'],'RelPath':cw['RelPath'],'fromLb':x.get('fromLb'),'windowNumber':wi,**ac})
   if ac['status']=='catalogue-risk':excluded.append({'RelPath':cw['RelPath'],'fromLb':x.get('fromLb'),'reason':'Catalogue/title/editorial discovery window; not admissible lexical evidence without a separate full-case use.'})
 row={'ordinal':ordinal,'id':e['id'],'term':e['term'],'source':'NEXT500_BUILD_PLAN.md','phase':'next500','isIriya':False,'preflightCounts':{'hits':e['hits'],'files':e['files'],'independentWorks':e['works'],'evidenceFloor':e['evidenceFloor']},'admissionState':'research-only-not-drafted','proseHygieneQuestions':[f"What ordinary scene, object, person, office, or utterance does {e['term']} name before any Chan redeployment?",f"Where do named Chan records bend, institutionalize, test, quote, or rebuke with {e['term']}?",'Which attractive inference fails against a counterexample or negative concordance result?','Can every eventual paragraph name its stored evidence and exact actor rather than say the records use it?','Could any proposed prose be pasted under another headword? If yes, reject it as template filler.'],'differentThingCandidates':split_candidates(e['term']),'modifierAndFamilyControls':families,'candidateIndependentWorks':works,'fullCaseActorCandidates':actors,'catalogueTitleExclusions':excluded,'admissionRequirements':['Read complete cases around selected windows; discovery KWIC is not final evidence.','Count distinct workId values, never files, for multi-source validation.','Run definition formulas and deployment-shape searches before drafting.','Verify every saved KWIC and exact FromLb/ToLb with zc.verify.','Resolve MasterName only to the utterer of the headword; actions, headings, questioners, narrators, and section owners are separate.','Test modifier compounds and overlapping family entries before deciding sense boundaries.'],'entryEdited':False}
 rows.append(row)
for start in range(901,1001,10):
 block=[x for x in rows if start<=x['ordinal']<=start+9]
 out={'schemaVersion':1,'generatedUtc':now,'wave':'f004','lane':'A','ordinals':[start,start+9],'state':'research-admission-only','immutablePreflight':{'path':str(pre.relative_to(ROOT)),'sha256':ph,'corpusBaselineSha256':p['corpusBaselineSha256']},'iriyaApplied':False,'reason':'All rows derive from NEXT500_BUILD_PLAN.md, not the Iriya queue.','entries':block,'draftsCreated':0,'entryFilesEdited':0,'supersedesF003':False,'siteTouched':False}
 (HERE/f'f004-laneA-{start}-{start+9}-research-admission-ledger.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
summary={'schemaVersion':1,'generatedUtc':now,'wave':'f004','lane':'A','ordinals':[901,1000],'state':'research-admission-prepared','immutablePreflight':{'path':str(pre.relative_to(ROOT)),'sha256':ph},'entries':100,'checkpoints':[f'fresh-build/waves/f004-laneA-{s}-{s+9}-research-admission-ledger.json' for s in range(901,1001,10)],'iriyaEntries':0,'next500Entries':100,'draftsCreated':0,'entryFilesEdited':0,'supersedesF003':False,'siteTouched':False}
(HERE/'f004-laneA-901-1000-research-admission-summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(summary,ensure_ascii=False))
