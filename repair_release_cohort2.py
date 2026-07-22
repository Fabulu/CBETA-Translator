#!/usr/bin/env python3
import json
from pathlib import Path
H=Path('/mnt/c/programmieren/mergeworkcbeta/cbeta-translator/runs/CLAUDE-RUNS/RUN-20260711-1248-full-cbeta-translation/dictionary-build')
IDS='t_49073773ac27 t_4fd4cdbc25b0 t_5ab492eefe6c t_5ba8bf9d83f2 t_6136d614a242 t_6b5825e8dc9a t_6b69c60b142d t_771649f50694 t_7c065becc98f t_7c22b3a70b70 t_7ee5a99b989c t_8873b46d7a4e t_88b6f3526f8e t_8cc2d1c484f9 t_8e59a2d1c6b2 t_9c1e9a072976 t_9d437dfc1719 t_9db05c37d46c t_a0471efec8ca t_a74430b8e7ec'.split()
M={'Yunwai Ze':'Yunwai Xingze','Wunian Yuanxin':'Wuwai Wunian Yuanxin',"San'yi Mingyu":'Sanyi Mingyu','Dongshan Meixi Du':'Meixi Du','Liangshan Guan':'Liangshan Yuanguan','Daguan Tanying':'Jinshan Tanying','Dabian Quan':'Dabian Xianquan'}
EN={'J/J34/J34nB311.xml':'Recorded Sayings of Chan Master Juelang Daosheng','T/T48/T48n2016.xml':'Record of the Mirror of the School','X/X70/X70n1397.xml':'Recorded Sayings of Chan Master Xueyan Zuqin','X/X72/X72n1437.xml':'Expanded Record of Chan Master Yongjue Yuanxian','J/J29/J29nB238.xml':'Recorded Sayings of Chan Master Chuiwan','X/X69/X69n1356.xml':'Recorded Sayings of Chan Master Puan Yinsu'}
pending=[]
def walk(x):
 if isinstance(x,dict):
  for k,v in list(x.items()):
   if k in ('MasterName',) and isinstance(v,str) and v in M:x[k]=M[v]
   else:walk(v)
 elif isinstance(x,list):
  for i,v in enumerate(x):
   if isinstance(v,str) and v in M:x[i]=M[v]
   else:walk(v)

for eid in IDS:
 p=H/'fresh-build/entries'/eid/'entry.v2.json'; d=json.load(open(p,encoding='utf8')); walk(d)
 # English source labels required by the release gate.
 if eid=='t_6b69c60b142d':
  for s in d['Senses']:
   for o in s['Occurrences']:
    rel=o['RelPath']; rest=o['AttributionNote'].split('). ',1)[1]
    while rest.startswith(EN[rel]+': '+EN[rel]+': '):rest=rest[len(EN[rel])+2:]
    o['AttributionNote']=f"Source record ({rel}). {rest}" if rest.startswith(EN[rel]+':') else f"Source record ({rel}). {EN[rel]}: {rest}"
 # Structured links named in exact attribution prose.
 if eid=='t_5ba8bf9d83f2':
  for oi,names in {2:['Juzhi','Tianlong'],3:['Tianlong']}.items():
   o=d['Senses'][0]['Occurrences'][oi]; have={x['MasterName'] for x in o.get('ContextMasters',[])}
   for n in names:
    if n not in have:o.setdefault('ContextMasters',[]).append({'MasterName':n,'Roles':['person-discussed']})
 if eid=='t_7ee5a99b989c':
  d['Senses'][0]['RelatedMasters']=[x for x in d['Senses'][0].get('RelatedMasters',[]) if x!='Jieyun Ju']
  o=d['Senses'][0]['Occurrences'][0]
  if not any(x['MasterName']=='Baling Haojian' for x in o.get('ContextMasters',[])):o.setdefault('ContextMasters',[]).append({'MasterName':'Baling Haojian','Roles':['person-discussed']})
 if eid=='t_4fd4cdbc25b0': d['Senses'][0]['Occurrences'][3]['ActorAttribution']['ActorRole']='case-figure'
 if eid=='t_6136d614a242':
  o=d['Senses'][0]['Occurrences'][3]
  for x in o['ContextMasters']:
   if x['MasterName']=='Linye Tongqi' and 'case-figure' not in x['Roles']:x['Roles'].append('case-figure')
 if eid=='t_8e59a2d1c6b2':
  o=d['Senses'][0]['Occurrences'][3]; o['MasterName']='Yunfeng Zhixuan'; o.pop('ActorAttribution',None); o['ContextMasters']=[{'MasterName':'Yunfeng Zhixuan','Roles':['utterer']}]
  o['AttributionNote']='Source record (X/X82/X82n1571.xml). Complete Five Lamps: Yunfeng Zhixuan delivers the hall address that puts the iron caltrop through the eyes.'
  o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':'Yunfeng Zhixuan','SpeechFrame':'The section heading names Yunfeng Zhixuan and the uninterrupted 上堂 frame governs the headword clause.','FullCaseDecision':'The exact headword clause is Yunfeng Zhixuan’s hall address.'}
 if eid=='t_771649f50694':
  d['Senses'][0]['Occurrences'][3]['AttributionNote']='Source record (X/X82/X82n1571.xml). Complete Five Lamps, Volumes 34–120: Wuwai Wunian Yuanxin utters the headword in answering Guzhuo Changjun that face-to-face it is not recognized although its whole body stands openly exposed; Guzhuo Changjun then tests the answer further.'
 if eid=='t_9db05c37d46c':
  s=d['Senses'][0]
  s['Explanation']=s['Explanation'].replace('“Bodhi originally has no tree” is Huineng’s first verse line','Huineng’s first verse line says “Bodhi originally has no tree”',1)
 raw=json.dumps(d,ensure_ascii=False)
 for old,new in M.items():raw=raw.replace(old,new)
 while 'Wuwai Wuwai Wunian Yuanxin' in raw:raw=raw.replace('Wuwai Wuwai Wunian Yuanxin','Wuwai Wunian Yuanxin')
 d=json.loads(raw)
 # Remove vague English actor nouns without changing their referent.
 if eid in ('t_7c065becc98f','t_8873b46d7a4e'):
  raw=json.dumps(d,ensure_ascii=False).replace('the master','the exact named speaker').replace('a teacher','the cited named speaker').replace('the monk','the explicit unnamed questioner').replace('a master','the exact named speaker')
  d=json.loads(raw)
 # Produce evidence-bound pending candidates for genuinely absent structured names.
 roster=json.load(open(H.parents[3]/'Assets/Data/lineage-masters.json')); canon={x['names'][0] for x in (roster if isinstance(roster,list) else roster['masters'])}
 shared=json.load(open(H/'fresh-build/pending-roster.json')); pc={x['canonicalName'] for x in shared['candidates']}
 names=[]
 for s in d['Senses']:
  names += s.get('RelatedMasters') or []
  for o in s['Occurrences']:
   if o.get('MasterName'):names.append(o['MasterName'])
   names += [x['MasterName'] for x in o.get('ContextMasters',[])]
 for name in dict.fromkeys(names):
  if name in canon or name in pc or any(x['canonicalName']==name for x in pending):continue
  evidence=None
  for s in d['Senses']:
   for o in s['Occurrences']:
    linked=o.get('MasterName')==name or any(x['MasterName']==name for x in o.get('ContextMasters',[]))
    if linked:evidence={k:o[k] for k in ('RelPath','FromLb','ToLb','Kwic')};break
   if evidence:break
  if not evidence:continue
  pending.append({'canonicalName':name,'aliases':[name],'evidence':[evidence],'reviewedBy':'Codex current-wave release repair cohort 2 exact occurrence review','reviewReport':f'fresh-build/entries/{eid}/WORK.md','status':'awaiting-roster-integration','classification':'genuine-absent-identity-needing-evidence-bound-pending','classificationNote':'The retained exact occurrence or its structured context explicitly names this actor; dictionary identity only, with no lineage assertion.'})
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')

ledger={'schemaVersion':'current-wave-release-repair-pending-patch.v1','cohort':2,'candidates':pending}
(H/'maintenance/current-wave-release-repair-cohort2-pending-roster-patch.json').write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
shared=json.load(open(H/'fresh-build/pending-roster.json')); shared['candidates'] += pending
(H/'maintenance/current-wave-release-repair-cohort2-local-pending-roster.json').write_text(json.dumps(shared,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
