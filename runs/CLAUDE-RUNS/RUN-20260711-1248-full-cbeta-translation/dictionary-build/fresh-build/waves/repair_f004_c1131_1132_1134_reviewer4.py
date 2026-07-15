#!/usr/bin/env python3
"""Focused author repair from reviewer4 rereview; resumable and idempotent."""
import json
from pathlib import Path
R=Path(__file__).resolve().parents[2]
IDS=['t_edfd0b2afa11','t_e251ef5cbc12','t_47b3313788e2']
def dedupe(text):
 parts=text.split('. ');out=[]
 for p in parts:
  if not out or p!=out[-1]:out.append(p)
 return '. '.join(out)
for ident in IDS:
 ep=R/'fresh-build/entries'/ident/'entry.v2.json';e=json.loads(ep.read_text())
 for s in e['Senses']:
  s['Explanation']=dedupe(s['Explanation'])
  if ident=='t_edfd0b2afa11': s['Explanation']='Receiving people and benefiting living beings names the public work of meeting those who come. The phrase appears in questions, biographies, and addresses where solitary understanding is contrasted with going out to meet people.'
  if s.get('ExplanationParts'):
   s['ExplanationParts']['CorpusEarnedOpening']=dedupe(s['ExplanationParts'].get('CorpusEarnedOpening',''))
   s['ExplanationParts']['EvidenceBody']=[dedupe(x) for x in s['ExplanationParts'].get('EvidenceBody',[])]
 if ident=='t_e251ef5cbc12':
  o=e['Senses'][0]['Occurrences'][6]
  o['ContextMasters']=[{'MasterName':'Huayan Zhizang','Roles':['person-described']},{'MasterName':'Linji Yixuan','Roles':['interlocutor']}]
  note='Source text (聯燈會要). The compiler narrates that Linji Yixuan visits Xiangzhou Huayan; Huayan Zhizang sees him and holds the staff crosswise in a sleeping posture. Huayan is the acting master and Linji the visiting interlocutor; neither utters the narrated headword.'
  o['AttributionNote']=note;o['ActorAttribution']['GrammarEvidence']=note
 e['CreatedBy']='Codex f004 lane C reviewer4 focused repair author';e['WrittenUtc']='2026-07-15T14:45:00Z'
 ep.write_text(json.dumps(e,ensure_ascii=False,indent=2)+'\n')
 (R/'fresh-build/entries'/ident/'evidence.draft.json').write_text(json.dumps({'SchemaVersion':1,'Entry':e},ensure_ascii=False,indent=2)+'\n')
# Source-proven temporary roster link; public roster itself remains untouched.
rp=R/'fresh-build/pending-roster.json';r=json.loads(rp.read_text())
if not any(x.get('canonicalName')=='Huayan Zhizang' for x in r['candidates']):
 o=json.loads((R/'fresh-build/entries/t_e251ef5cbc12/entry.v2.json').read_text())['Senses'][0]['Occurrences'][6]
 r['candidates'].append({'canonicalName':'Huayan Zhizang','aliases':['襄州華嚴','華嚴智藏'],'evidence':[{'RelPath':o['RelPath'],'FromLb':o['FromLb'],'ToLb':o['ToLb'],'Kwic':o['Kwic']}],'reviewedBy':'Codex f004 lane C reviewer4 focused repair author','reviewReport':'fresh-build/waves/f004-laneC-1131-1132-1134-reviewer4-rereview.json','status':'awaiting-roster-integration'})
 rp.write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'repaired':IDS,'deduplicatedExplanations':3,'correctedOccurrence':'1132-o7'}))
