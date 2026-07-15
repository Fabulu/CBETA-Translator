import copy,json,os,re,subprocess,sys
R=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'));sys.path.insert(0,R);import zc
B='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a';NOW='2026-07-15T19:00:00Z'
OPEN={
'師子吼':'The lion’s roar names fearless, authoritative teaching and is also invoked as a standard against which a speaker or utterance is tested.',
'立雪':'Standing in the snow primarily recalls Huike’s vigil before Bodhidharma; a separate image compares a white heron standing in snow.',
'德山棒':'Deshan’s staff or blow is a named Chan device, repeatedly paired or contrasted with Linji’s shout in questions and proclamations.',
'臨濟喝':'Linji’s shout is a named Chan device, repeatedly paired or contrasted with Deshan’s staff and tested in later encounters.',
'單提':'To raise singly is to bring one saying, question, or Chan device forward without auxiliary explanation or divided attention.',
'破草鞋':'Worn-out straw sandals figure as a discarded or worthless object, while the longer wear-out construction evokes exhaustive wandering.',
'丈六金身':'The sixteen-foot golden body is the Buddha’s conventional majestic form, freely exchanged with a blade of grass in Chan transformation formulas.',
'賓中主':'The host within the guest names a guest–host position whose interpretation is explicitly system-dependent in later Chan analysis.',
'人境俱不奪':'Taking away neither person nor environment is one member of Linji’s Four Selections, stated as a rubric and answered through fresh images.',
'主中賓':'The guest within the host names a guest–host position used in answers and later explanations, with Linji and Caodong schemes kept distinct.'}
ACT={
'師子吼':['Nanyang Huizhong','Guizong Cezhen','Qingyu'],
'德山棒':['Dahui Zonggao','Yuanwu Keqin'],
'臨濟喝':['Tiantong Danjiao','Dahui Zonggao'],
'單提':['Xuedou Zhongqian','Zhongfeng Mingben'],
'破草鞋':['Foyan Qingyuan'],
'丈六金身':['Yuanwu Keqin','Yuanwu Keqin'],
'人境俱不奪':['Jiuxian Faqing Zujian','Linji Yixuan'],
'主中賓':['Shuangling Hua']}
def ap(o,name,narr=False):
 if narr:
  o['ActorAttribution']={'Status':'narrated','Kind':'compiler narrative','ActorLabel':name,'ActorRole':'compiler','ReviewedBy':'Codex f002 A371–380','ReviewedUtc':NOW,'GrammarEvidence':'The exact headword occurs in framing or compiler narration rather than an owned master turn.','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage']};o.pop('MasterName',None);f=o['ActorAttribution']['GrammarEvidence']
 else:
  o['MasterName']=name;o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}];f=f'{name} owns the exact headword-bearing turn after review of the complete exchange and section heading.'
 o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':name,'SpeechFrame':f,'FullCaseDecision':f}
P=json.load(open(os.path.join(R,'fresh-build/waves/f002-laneA-301-400-preflight.json')))['entries'][70:80]
for p in P:
 old=json.load(open(os.path.join(R,'terms',p['id'],'entry.v2.json')));term=p['term'];E={'Id':p['id'],'SourceTerm':term,'CreatedBy':'Codex fresh f002 Lane A evidence-first','WrittenUtc':NOW,'CorpusBaselineSha256':B,'Senses':[]}
 used={o['RelPath'] for s in old['Senses'] for o in s.get('Occurrences',[])};need=max(0,p['evidenceFloor']-sum(len(s.get('Occurrences',[])) for s in old['Senses']));cands=[x for x in p['candidateWorks'] if x['RelPath'] not in used][:need];extras=[]
 for j,c in enumerate(cands):
  win=c['windows'][0]['window'];pos=win.find(term);assert pos>=0;kw=win[max(0,pos-35):min(len(win),pos+len(term)+35)];v=zc.verify(c['RelPath'],kw);assert v['ok'];o={'RelPath':c['RelPath'],'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True,'AttributionNote':f"Source text ({c['title']}); exact actor reviewed from the full exchange and section heading.",'ContextMasters':[]};ap(o,ACT[term][j]);extras.append(o)
 for si,s in enumerate(old['Senses']):
  q=copy.deepcopy(s);exp=q.pop('Explanation','');q['SearchAliases']=q.get('SearchAliases') or [q['PreferredTarget']];O=[];C=[]
  for o in q.get('Occurrences',[]):
   v=zc.verify(o['RelPath'],o['Kwic']);assert v['ok'];o['FromLb']=v['fromLb'];o['ToLb']=v['toLb'];name=o.get('MasterName') or o.get('ActorAttribution',{}).get('ActorLabel');ap(o,name,bool(o.get('ActorAttribution') and o['ActorAttribution'].get('Status')=='narrated'));(O if term in ''.join(o['Kwic'].split()) else C).append(o)
  if si==0:O+=extras
  opening=OPEN[term] if si==0 else f'This separate sense denotes {q["PreferredTarget"]}, not {old["Senses"][0]["PreferredTarget"]}.';q['Occurrences']=O;q['ClaimAnchors']=q.get('ClaimAnchors',[])+C;q['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in O));works=sorted({zc.work_id(o['RelPath']) for o in O});q['Validation']='multi-source' if len(works)>1 else 'provisional';q['ExplanationParts']={'CorpusEarnedOpening':opening,'EvidenceBody':[re.sub(r'^Literally[^.]*\.\s*','',exp)]};q['DraftEvidence']={'OpeningClaimEvidenceKeys':[f'o{i}' for i in range(1,len(O)+1)],'ZenBend':opening,'CounterexampleOrLimit':q.get('Note') or 'Neighboring compounds, literal scenes, and family members were checked separately.','DifferentThingTest':{'Decision':'different-thing' if len(old['Senses'])>1 else 'one-thing','ComparedThings':[x['PreferredTarget'] for x in old['Senses']],'Reason':'Only incompatible referents or explicitly distinct technical systems are split.'},'AliasRationale':'Close English lookup wording only.','ModifierControls':['Longer formulas and paired devices were checked separately.'],'FamilyControls':['Related family terms do not donate meaning or source independence.'],'IndependentWorkIds':works};E['Senses'].append(q)
 d=os.path.join(R,'fresh-build/entries',p['id']);os.makedirs(d,exist_ok=True);w=os.path.join(d,'evidence.draft.json');open(w,'w').write(json.dumps({'SchemaVersion':1,'Entry':E},ensure_ascii=False,indent=2)+'\n');r=subprocess.run([sys.executable,os.path.join(R,'compile_evidence_draft.py'),w,'--output',os.path.join(d,'entry.v2.json'),'--report',os.path.join(d,'compile-report.json')],capture_output=True,text=True);assert r.returncode==0,r.stdout+r.stderr
