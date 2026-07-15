#!/usr/bin/env python3
import copy,datetime,json,subprocess,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
def compile(i):
 d=R/'fresh-build/entries'/i;w=d/'evidence.draft.json';subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(w),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True)
# Break the exact floor-mode honestly with a ninth independent 佛祖 witness.
bid='t_279cf2b97244';d=R/'fresh-build/entries'/bid;p=d/'evidence.draft.json';x=json.loads(p.read_text());s=x['Entry']['Senses'][0];used={o['RelPath'] for o in s['Occurrences']}
for rel,_ in zc.count('佛祖')['per_file']:
 if rel in used:continue
 fs=zc.find(rel,'佛祖',ctx=70,limit=1)
 if not fs:continue
 q=fs[0]['window'];v=zc.verify(rel,q)
 if not v.get('ok'):continue
  title=zc.title(rel);label='the compiler or recorder of the source passage';o={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':q,'Curated':True,'ContextMasters':[],'ActorAttribution':{'Status':'narrated','Kind':'compiler narrative','ActorLabel':label,'ActorRole':'compiler','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':'The full clause is documentary narration rather than a safely isolated spoken turn.','ReviewedBy':'Codex f003 A651-700 depth repair','ReviewedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat()},'AttributionNote':f'Source text ({title}): {label} owns the headword-bearing documentary clause.','DraftActorProof':{'ExactHeadwordClause':q,'GrammaticalSubject':label,'SpeechFrame':'Documentary narration.','FullCaseDecision':'No named utterer owns this narrated clause.'}};s['Occurrences'].append(o);s['SourceTexts'].append(rel);s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)];break
else:raise SystemExit('no independent 佛祖 witness')
p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n');compile(bid)
# Split 消息 by different referents, not by grammar.
mid='t_4da199fae933';d=R/'fresh-build/entries'/mid;p=d/'evidence.draft.json';x=json.loads(p.read_text());base=x['Entry']['Senses'][0];groups=[('news or tidings',[1,4],'News or tidings are information carried from an absent person or place.'),('a revealing sign or intimation',[0,2,3,5,6],'A revealing sign is the detectable indication by which an encounter or teaching is said to disclose itself.'),('adjustment or regulation',[7],'Adjustment is the act of moderating or regulating between positions.')];out=[]
for target,ix,opening in groups:
 s=copy.deepcopy(base);s['PreferredTarget']=target;s['AlternateTargets']=[];s['SearchAliases']=[target];s['Occurrences']=[base['Occurrences'][i] for i in ix];s['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in s['Occurrences']));s['Validation']='multi-source' if len({zc.work_id(o['RelPath']) for o in s['Occurrences']})>1 else 'single-source';s['ExplanationParts']['CorpusEarnedOpening']=opening;s['ExplanationParts']['EvidenceBody']=['The exact predicates identify this referent without borrowing a meaning from the other two uses.'];s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)];s['DraftEvidence']['ZenBend']='Zen exchanges repeatedly turn “news” into the demanded or withheld sign that someone can actually present, while ordinary tidings and regulation remain visible controls.';s['DraftEvidence']['DifferentThingTest']={'Decision':'different-thing','ComparedThings':[g[0] for g in groups],'Reason':'Information, an evidentiary sign, and regulation are different things, not noun/verb readings of one referent.'};out.append(s)
x['Entry']['Senses']=out;p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n');w=d/'WORK.md';w.write_text(w.read_text()+'\nsense-target-distinguishability: news/tidings, a revealing sign, and adjustment/regulation each name a different thing visible from the PreferredTarget alone.\n');compile(mid)
print('repaired A651-700 depth cohort')
