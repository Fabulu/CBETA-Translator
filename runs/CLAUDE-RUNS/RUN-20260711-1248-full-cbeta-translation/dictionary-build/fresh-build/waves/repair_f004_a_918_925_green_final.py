#!/usr/bin/env python3
import json,subprocess,sys
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
def compile(i,fn):
 d=R/'fresh-build/entries'/i;p=d/'evidence.draft.json';x=json.loads(p.read_text());fn(x['Entry']);p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True)
def zhao(e):
 s=e['Senses'][0];s['Explanation']=s['Explanation'].replace('“Zhaozhou putting on his straw sandals”','Zhaozhou putting on his straw sandals');s['ExplanationParts']['CorpusEarnedOpening']=s['ExplanationParts']['CorpusEarnedOpening'].replace('“Zhaozhou putting on his straw sandals”','Zhaozhou putting on his straw sandals')
compile('t_bdc0cdca39d0',zhao)
def zux(e):
 o=e['Senses'][0]['Occurrences'][3];o.pop('ActorAttribution',None);o['MasterName']='Kefu Daozhe';o['ContextMasters']=[{'MasterName':'Kefu Daozhe','Roles':['utterer']}];o['AttributionNote']='Source text (天聖廣燈錄). Kefu Daozhe: Kefu answers the host-and-guest question with “raise the patriarchal seal high and use it at the occasion.”';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':'Kefu Daozhe','SpeechFrame':'The complete section is headed Chuzhou Kefu Daozhe; 師云 marks Kefu’s answer.','FullCaseDecision':'Kefu Daozhe utters the headword in his answer.'}
compile('t_c02887fbd979',zux)
def clean(e):
 for s in e['Senses']:
  for o in s['Occurrences']:
   o['AttributionNote']=o['AttributionNote'].replace('the author of the 覺浪和尚語錄 preface','the preface author').replace('the author of the preface to 增集續傳燈錄','the preface author').replace('the author of the 禪宗正脉 preface','the preface author')
compile('t_94f424853f5b',clean);compile('t_c9f69715e823',clean)
p=R/'fresh-build/pending-roster.json';d=json.loads(p.read_text());have={x.get('canonicalName') for x in d['candidates']}
if 'Kefu Daozhe' not in have:
 o=json.loads((R/'fresh-build/entries/t_c02887fbd979/entry.v2.json').read_text())['Senses'][0]['Occurrences'][3]
 d['candidates'].append({'canonicalName':'Kefu Daozhe','aliases':['剋符道者','剋符'],'evidence':[{k:o[k] for k in ('RelPath','FromLb','ToLb','Kwic')}],'reviewedBy':'Codex f004 A918-925 green repair author','reviewReport':'fresh-build/waves/f004-a-918-925-author-unique-pre-review-gate.json','status':'awaiting-roster-integration'});p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
