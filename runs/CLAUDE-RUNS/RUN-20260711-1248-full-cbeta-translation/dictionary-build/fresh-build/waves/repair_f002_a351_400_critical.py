import datetime,json,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
N=datetime.datetime.now(datetime.timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
def load(i):
 p=R/f'fresh-build/entries/{i}/evidence.draft.json';return p,json.loads(p.read_text(encoding='utf8'))
def unnamed(o,label,role='questioner'):
 o.pop('MasterName',None)
 for cm in o.get('ContextMasters',[]): cm['Roles']=[r for r in cm.get('Roles',[]) if r!='utterer'] or ['respondent']
 o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'monk' if role=='questioner' else 'person','ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':'The complete exchange places the headword before 師曰/師云 in the interlocutor’s marked question; the named master owns only the reply.','ReviewedBy':'Codex f002 A351-400 exact-turn repair','ReviewedUtc':N};o['AttributionNote']=f'{label} utters the exact headword-bearing question; the named respondent speaks only after it.';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':'問 introduces the headword-bearing turn; 師曰/師云 introduces the response.','FullCaseDecision':o['AttributionNote']}
def named(o,name,roles=['utterer']):
 o['MasterName']=name;o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':name,'Roles':roles}];o['AttributionNote']=f'The complete case identifies {name} as utterer of the exact headword-bearing clause.';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':name,'SpeechFrame':'The complete marked turn or named section identifies the speaker.','FullCaseDecision':o['AttributionNote']}

# 師子吼: repair literal fake name and question/answer ownership.
p,d=load('t_eedf4100b3d7');O=d['Entry']['Senses'][0]['Occurrences'];unnamed(O[3],'the unnamed monk who asks what happens on meeting a lion’s roar');unnamed(O[5],'the unnamed monk asking Guizong Cezhen about the lion’s roar');p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')

# 臨濟喝: later citation is not Linji utterance; questions belong to their questioners.
p,d=load('t_1403ddf1e83b');O=d['Entry']['Senses'][0]['Occurrences']
for i,label in [(2,'the unnamed monk asking about Deshan’s staff and Linji’s shout'),(3,'the unnamed monk asking what lies beyond Deshan’s staff and Linji’s shout'),(4,'the unnamed monk addressing Chongfan Yu about Linji’s shout')]: unnamed(O[i],label)
named(O[6],'Xiangyan Zhixian');p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')

# 透網金鱗: Baohua responds only after the monk’s exact question.
p,d=load('t_d95b944e0749');O=d['Entry']['Senses'][0]['Occurrences'];unnamed(O[5],'the unnamed monk asking Baohua why a golden-scaled fish beyond the net still lingers in water');p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')

# 千手眼: narrow the two mixed-turn rows to Magu's question.
for ident,idx,kw in [('t_bf67613e4573',5,'麻谷問：大悲千手眼，那箇是正眼？'),('t_bf67613e4573',6,'麻谷出問大悲千手眼那箇是正眼')]:
 p,d=load(ident);o=d['Entry']['Senses'][0]['Occurrences'][idx];v=zc.verify(o['RelPath'],kw);assert v['ok'];o['Kwic']=kw;o['FromLb']=v['fromLb'];o['ToLb']=v['toLb'];named(o,'Magu Baoche');p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')

# 南泉斬貓: exact questioner owns the Miyun turn; Guangfu owns his biographical question.
p,d=load('t_bd2caabef956');O=d['Entry']['Senses'][0]['Occurrences'];unnamed(O[0],'the unnamed monk asking Miyun Yuanwu why Nanquan cut the cat');named(O[6],'Guangfu Weishang');p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')

# Modifier hygiene: no material-looking “golden lock/chain” survives in public prose.
p,d=load('t_dda048ca832d')
for s in d['Entry']['Senses']:
 for part in ['CorpusEarnedOpening']:
  if part in s.get('ExplanationParts',{}): s['ExplanationParts'][part]=s['ExplanationParts'][part].replace('A golden lock','An ornate lock-barrier').replace('golden lock','ornate lock-barrier').replace('golden chain','ornate binding chain')
 s['ExplanationParts']['EvidenceBody']=[x.replace('A golden lock','An ornate lock-barrier').replace('golden lock','ornate lock-barrier').replace('golden chain','ornate binding chain') for x in s['ExplanationParts'].get('EvidenceBody',[])]
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
print('critical repairs written')
