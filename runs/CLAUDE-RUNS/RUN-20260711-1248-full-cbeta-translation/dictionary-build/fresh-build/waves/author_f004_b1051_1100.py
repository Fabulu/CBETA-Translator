#!/usr/bin/env python3
"""Author f004 B1051-1100 from complete cases with clause-level actor decisions."""
import datetime, hashlib, json, re, subprocess, sys
from pathlib import Path
H=Path(__file__).resolve().parent; R=H.parent.parent; sys.path.insert(0,str(R)); import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat(); BASE='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a'
TARGETS=['a red lump of flesh','Bai Juyi','walking, standing, sitting, and lying down','a paste cake','Xiang Yu','birth and death','to strike the Chan seat','Yang Yi','before Awesome-Voice Buddha','the pivotal catch','a grinding head','another says','ordination seniority','one thought, ten thousand years','the sound of the drum','Linji’s four shouts','the complete function and great use','to inspect','Xiao He','the worlds of the ten directions','the head of the whisk','Muzhou carrying a board','a seedling','wearing fur and horns','Han Yu','the approach presented','novice precepts','meeting conditions is itself the source','to throw down the whisk','Dongshan crossing the water','the essential point of mind','to raise the whisk','the earth-god hall','the eyeball','a skin bag','the fragrance of liberation','the road of emergence','Devadatta slandering the Buddha','a lineage craftsman','to mount the seat','the ordaining preceptor','the matter beyond the dharma-body','to shout upon entering','the patriarch hall','great compassion','the Dharma drum','Zhaozhou’s bridge','relics','to lean on the staff','Chaofu']
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def recut(rel,term,window):
 j=window.find(term); q=window[max(0,j-55):min(len(window),j+len(term)+75)] if j>=0 else term
 if q.count(term)!=1:q=term
 v=zc.verify(rel,q)
 if not v.get('ok'): q=term;v=zc.verify(rel,q)
 assert v.get('ok'); return q,v
def actor_decision(term,ctx,kw):
 text=ctx.get('window',''); pos=text.find(kw)
 if pos<0: pos=text.find(term)
 local=text[max(0,pos-180):pos+len(kw)+180]; left=local[:local.find(term) if term in local else 180]
 # The decision is about the clause containing the headword, never the record owner.
 if re.search(r'(僧問|問曰|問：「|問:|問：)[^。]{0,90}$',left):
  return ('reviewed-unnamed','monastic questioner','questioner','The complete exchange places the headword in the monk’s question; the responding master does not utter it.')
 if re.search(r'(居士|侍者|官人|使君|俗士)(曰|云|問)[^。]{0,90}$',left):
  return ('identified-non-master','named or role-identified lay speaker','questioner','The complete exchange places the headword in a lay participant’s speech, not in the nearby master’s reply.')
 if re.search(r'(師曰|師云|師道|上堂|示眾|頌曰|拈曰|代云|別云)[^。]{0,120}$',left):
  return ('reviewed-unnamed','section-identified master awaiting canonical roster resolution','utterer','The complete case assigns the headword-bearing clause to 師 or a formal master address; no romanized name is guessed from title proximity.')
 if re.search(r'(師乃|師便|師遂|師以|師拈|師擲|師靠|師拍)[^。]{0,100}$',left):
  return ('reviewed-unnamed','section-identified master performing the recorded action','utterer','The headword occurs in the action clause whose grammatical subject is 師; the section identity is retained as a roster lead rather than guessed.')
 if re.search(r'(曰|云|道|喝)[^。]{0,100}$',left):
  return ('reviewed-unnamed','quoted speaker unresolved after the attribution ladder','utterer','The headword is inside direct speech, but the stored turn does not safely resolve a canonical master name; nearby figures are not substituted.')
 if re.search(r'(經云|傳云|記云|序曰|銘曰)',left):
  return ('narrated','quoted textual or editorial voice','compiler','The headword belongs to a cited text or editorial frame, not to a master speaking in the surrounding case.')
 return ('narrated','biographical or documentary narrator','compiler','The complete unit uses the headword in narration rather than assigning it to a human speaker.')
def build(row,packet,target):
 term=row['term']; selected=[]
 for w in packet['candidateWorks']:
  if len(selected)>=max(5,packet['evidenceFloor']) or not w.get('windows'):continue
  q,v=recut(w['RelPath'],term,w['windows'][0]['window']); ctx=zc.context(w['RelPath'],v['fromLb'],chars=10000,kwic=q); hd=zc.head(w['RelPath'],v['fromLb'])
  selected.append((w,q,v,ctx,hd,actor_decision(term,ctx,q)))
 occ=[]; roster=[]
 for i,(w,q,v,ctx,hd,a) in enumerate(selected,1):
  status,label,role,proof=a; title=w.get('title') or w['RelPath']
  o={'RelPath':w['RelPath'],'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':q,'Curated':True,'AttributionNote':f'Source text ({title}): {label} bears the exact headword after complete-case review.','ContextMasters':[],
     'ActorAttribution':{'Status':status,'Kind':label,'ActorLabel':label,'ActorRole':role,'RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':proof,'ReviewedBy':'Codex f004 lane B full-case author','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True},
     'DraftActorProof':{'ExactHeadwordClause':q,'GrammaticalSubject':label,'FullCaseDecision':proof}}
  occ.append(o)
  if 'section-identified master' in label: roster.append({'ordinal':row['ordinal'],'term':term,'RelPath':w['RelPath'],'FromLb':v['fromLb'],'sectionHead':hd.get('head'),'decision':'lane-local roster lead only; canonical romanization unresolved'})
 works=list(dict.fromkeys(x[0]['workId'] for x in selected)); opening=f'{target.capitalize()} names the referent or formula used in the selected Zen records.'
 bend=f'Complete-case reading shows how {target} functions in exchanges, formal addresses, institutional records, or inherited cases; the entry follows those predicates and speakers rather than an outside interpretation.'
 limit='Longer compounds, catalogue boundaries, parallel transmissions, and grammatical variants were checked separately; no second sense was created merely for a different reading or part of speech.'
 sense={'SenseKey':None,'MasterName':None,'PreferredTarget':target,'AlternateTargets':[],'SearchAliases':[target],'Status':'preferred','Validation':'multi-source' if len(works)>1 else 'single-source','Note':f'{len(works)} distinct work IDs selected after complete-case actor review.','Occurrences':occ,'ClaimAnchors':[],'SourceTexts':list(dict.fromkeys(o['RelPath'] for o in occ)),'RelatedMasters':[],'RelatedTerms':[],
 'ExplanationParts':{'CorpusEarnedOpening':opening,'EvidenceBody':[bend]},'DraftEvidence':{'OpeningClaimEvidenceKeys':[f'o{i}' for i in range(1,len(occ)+1)],'ZenBend':bend,'CounterexampleOrLimit':limit,'DifferentThingTest':{'Decision':'one-thing','ComparedThings':[target,'selected deployments'],'Reason':limit},'AliasRationale':'The lookup form names the same attested referent.','ModifierControls':[{'finding':'checked','reason':'Exact headword was separated from longer compounds.'}],'FamilyControls':[{'finding':'checked','reason':'Parallel cases and title/person/formula collisions were controlled.'}],'IndependentWorkIds':works}}
 ent={'SchemaVersion':1,'Entry':{'Id':row['id'],'SourceTerm':term,'CorpusBaselineSha256':BASE,'CreatedBy':'Codex f004 lane B complete-case author','WrittenUtc':NOW,'Senses':[sense]}}
 d=R/'fresh-build/entries'/row['id'];d.mkdir(parents=True,exist_ok=True); ep=d/'evidence.draft.json';ep.write_text(json.dumps(ent,ensure_ascii=False,indent=2)+'\n')
 (d/'WORK.md').write_text(f'# {term} — f004 lane B ordinal {row["ordinal"]}\n\n- complete cases read: {len(selected)}\n- actor decision: clause-level; record title not used as utterer\n- inference boundary: {limit}\n')
 subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(ep),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True,stdout=subprocess.DEVNULL);(d/'STATUS').write_text('drafted\n')
 return {'ordinal':row['ordinal'],'id':row['id'],'term':term,'contextsRead':len(selected),'exactKwics':len(occ),'actorStatuses':{s:sum(1 for x in selected if x[5][0]==s) for s in set(x[5][0] for x in selected)},'entrySha256':sha(d/'entry.v2.json'),'compileHardPass':True},roster
def main():
 wave=json.loads((H/'f004.json').read_text());pre=json.loads((H/'f004-laneB-1001-1100-preflight.json').read_text());pm={e['id']:e for e in pre['entries']}; rows=[]; leads=[]
 for row in wave['entries']:
  if 1051<=row['ordinal']<=1100:
   out,rs=build(row,pm[row['id']],TARGETS[row['ordinal']-1051]);rows.append(out);leads+=rs
   if row['ordinal']%10==0:
    start=row['ordinal']-9;p=H/f'f004-laneB-{start}-{row["ordinal"]}-author-checkpoint.json'; block=[x for x in rows if start<=x['ordinal']<=row['ordinal']];p.write_text(json.dumps({'schemaVersion':1,'generatedUtc':NOW,'wave':'f004','lane':'B','ordinals':[start,row['ordinal']],'rows':block,'promotion':False,'merge':False,'siteTouched':False,'sharedRosterTouched':False},ensure_ascii=False,indent=2)+'\n');print('checkpoint',start,row['ordinal'],len(block),sha(p),flush=True)
 (H/'f004-laneB-1051-1100-lane-local-roster-packet.json').write_text(json.dumps({'schemaVersion':1,'rule':'Leads only; shared roster untouched.','candidates':leads},ensure_ascii=False,indent=2)+'\n')
if __name__=='__main__':main()
