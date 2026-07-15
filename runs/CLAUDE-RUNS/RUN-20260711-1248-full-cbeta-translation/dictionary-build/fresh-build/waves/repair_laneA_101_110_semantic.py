import json,sys,re
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
P=json.loads((R/'fresh-build/waves/f001-laneA-101-110-preflight.json').read_text());ids={x['term']:x['id'] for x in P['entries']};RUNG=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
openings={'便喝':'Then shouted marks an immediate vocal action inside a public encounter; the stored cases distinguish who shouts and what follows rather than treating every shout as equivalent.','擬議':'To hesitate or deliberate marks loss of the encounter’s immediacy and is repeatedly paired with distance, error, a shout, or a blow, while direct warnings tell participants not to enter that pause.','目前':'Before the eyes names immediate presence, yet the stored sayings contrast that presence with treating it as an observable object; this bounded contrast is inferred from their own wording.','拄杖子':'The staff is a teaching and testing implement: named masters identify it, transfer it, raise or break it, and predicate impossible actions of it while disagreeing over whether recognizing it completes travel or leads to hell.','珍重':'As a closing formula, take care dismisses an assembly or participant at the end of an address or encounter.','承當':'To take on or own directly describes accepting what records say is already one’s own, while other turns warn that premature acceptance or refusal can itself miss the matter.','分別':'To distinguish names an act that some witnesses use for clear discrimination and others blame for constructing obstruction; the entry preserves both deployments without ruling that the act is inherently good or bad.','意旨如何':'What is the purport? is a follow-up interview question pressing for the point of an initial answer, gesture, or inherited case rather than merely requesting a lexical translation.','思量':'Deliberation is repeatedly said not to reach the matter, while Yaoshan’s paradoxical reply speaks of thinking what does not think; both claims remain attributed without importing a practice framework.','宗旨':'The defining purport is the governing point of a lineage or house as transmitted, established, entrusted, and tested in encounters.'}
def load(t):p=R/'fresh-build/entries'/ids[t]/'evidence.draft.json';return p,json.loads(p.read_text())
def save(p,d):p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
def narrated(o,name=None):
 title=zc.title(o['RelPath']);name=name or o.get('MasterName');o['MasterName']=None;o['ActorAttribution']={'Status':'narrated','Kind':'narrated action or biography','ActorLabel':'source narrator','ActorRole':'compiler','GrammarEvidence':'Narrative syntax reports the action or biographical fact rather than quoting the participant saying the headword.','ReviewedBy':'Codex f001 lane A independent-review repair','ReviewedUtc':'2026-07-15T00:00:00Z'};o['ContextMasters']=([{'MasterName':name,'Roles':['person-described']}] if name else []);o['AttributionNote']=f'Source narration in Source Record ({title}) owns the headword-bearing report.'+(f' {name} is the participant described.' if name else '');o['DraftActorProof']={'GrammaticalSubject':'the source narrator','FullCaseDecision':'The narrator owns the wording; the participant is retained as person described.'}
def question(o,respondent=None):
 title=zc.title(o['RelPath']);o['MasterName']=None;o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'monastic questioner','ActorLabel':'unnamed monastic questioner','ActorRole':'questioner','RungsChecked':RUNG,'GrammarEvidence':'The marked question assigns the exact wording to an unnamed monastic; the teacher responds afterward.','ReviewedBy':'Codex f001 lane A independent-review repair','ReviewedUtc':'2026-07-15T00:00:00Z'};o['ContextMasters']=([{'MasterName':respondent,'Roles':['respondent']}] if respondent else []);o['AttributionNote']=f'An unnamed monastic questioner in Source Record ({title}) owns the exact question.'+(f' {respondent} answers afterward.' if respondent else '');o['DraftActorProof']={'GrammaticalSubject':'the unnamed monastic questioner','FullCaseDecision':'The unnamed monastic owns the question; the respondent is context only.'}
def recut(o,term):
 c=zc.context(o['RelPath'],o['FromLb'],chars=220,kwic=o['Kwic']);w=c.get('window') or o['Kwic'];j=w.find(term)
 if j<0:return
 punct='。！？；\n';a=max([w.rfind(x,0,j) for x in punct]+[-1])+1;ends=[w.find(x,j+len(term)) for x in punct];ends=[x for x in ends if x>=0];b=(min(ends)+1 if ends else min(len(w),j+len(term)+35));kw=w[a:b].strip();v=zc.verify(o['RelPath'],kw)
 if v.get('ok'):o['Kwic']=kw;o['FromLb']=v['fromLb'];o['ToLb']=v['toLb']

for term in ids:
 p,d=load(term)
 for si,s in enumerate(d['Entry']['Senses']):
  op=openings[term]
  if term=='珍重' and si==1:op='To treasure or preserve actively describes valuing a teaching, record, or matter and keeping it in circulation; this differs from the dismissal formula.'
  s['ExplanationParts']={'CorpusEarnedOpening':op,'EvidenceBody':[op,'The exact rows retain their separate speakers, narrators, participants, and quoted layers; no single deployment is made to resolve all others.']};s['DraftEvidence']['ZenBend']=op;s['DraftEvidence']['CounterexampleOrLimit']=s['ExplanationParts']['EvidenceBody'][-1]
 save(p,d)

# Immediate shouts are narrated actions.
p,d=load('便喝')
for o in d['Entry']['Senses'][0]['Occurrences']:narrated(o)
save(p,d)
# Narrated hesitation only where the monk's event is reported.
p,d=load('擬議')
for o in d['Entry']['Senses'][0]['Occurrences']:
 if '僧擬議' in o['Kwic']:narrated(o)
save(p,d)
# Recut synthetic/context fragments to complete local sentences.
for term,indexes in {'目前':[6,7,8],'珍重':[5,6,7,8],'承當':[4,5,6,7,8],'分別':[4,5,6,7,8]}.items():
 p,d=load(term);s=d['Entry']['Senses'][0]
 for i in indexes:
  if i<len(s['Occurrences']):recut(s['Occurrences'][i],term)
 save(p,d)
# Staff gestures/actions are narrator-owned; direct sayings remain named.
p,d=load('拄杖子')
for o in d['Entry']['Senses'][0]['Occurrences']:
 if re.search(r'卓|拈起|接取|拗作|拈得|把得|舉|豎|擊|打',o['Kwic']):narrated(o)
save(p,d)
# Monk's closing action.
p,d=load('珍重');narrated(d['Entry']['Senses'][0]['Occurrences'][3]);save(p,d)
# Purport questions are questioner-owned; quoted Yangshan remains respondent/context.
p,d=load('意旨如何')
for o in d['Entry']['Senses'][0]['Occurrences']:question(o,'Yangshan Huiji' if '仰山' in o['Kwic'] else None)
save(p,d)
# Split both Yaoshan dialogues into actor-pure questions and replies.
p,d=load('思量');s=d['Entry']['Senses'][0];old=s['Occurrences'];new=[]
for base in old[:2]:
 for kw,who in [('思量甚麼',None),('思量箇不思量底','Yaoshan Weiyan'),('不思量底如何思量',None),('非思量','Yaoshan Weiyan')]:
  v=zc.verify(base['RelPath'],kw);o={'RelPath':base['RelPath'],'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'MasterName':who,'Curated':True}
  if who:o.update({'AttributionNote':f'Yaoshan Weiyan, in Source Record ({zc.title(base["RelPath"])}), owns the exact marked reply.','ContextMasters':[{'MasterName':who,'Roles':['utterer','respondent']}],'DraftActorProof':{'ExactHeadwordClause':kw,'SpeechFrame':'The marked teacher reply contains the headword.','FullCaseDecision':'Yaoshan Weiyan owns the exact reply.'}})
  else:question(o,'Yaoshan Weiyan')
  new.append(o)
s['Occurrences']=new+old[2:];s['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in s['Occurrences']));s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)];save(p,d)
# Huineng/Dongshan citation retains Huineng as quoted utterer and Dongshan as a distinct cited figure.
p,d=load('思量');o=d['Entry']['Senses'][0]['Occurrences'][-1];o['ContextMasters']=[{'MasterName':'Huineng','Roles':['utterer','case-figure']},{'MasterName':'Dongshan Liangjie','Roles':['person-discussed']}];save(p,d)
# Lineage-purport biography/question and Bodhidharma quotation layers.
p,d=load('宗旨');s=d['Entry']['Senses'][0];narrated(s['Occurrences'][2],'Shishuang Lin');question(s['Occurrences'][3]);
for i in [0,5,6]:
 o=s['Occurrences'][i];o['MasterName']='Bodhidharma';o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':'Bodhidharma','Roles':['utterer','case-figure']}];o['AttributionNote']=f'Bodhidharma, explicitly quoted in Source Record ({zc.title(o["RelPath"])}), owns the statement about entrusting the robe to establish the lineage purport.';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'SpeechFrame':'The passage explicitly supplies Bodhidharma’s quoted statement.','FullCaseDecision':'Bodhidharma owns the quoted headword wording; the later record transmits it.'}
save(p,d)
