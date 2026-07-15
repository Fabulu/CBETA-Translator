#!/usr/bin/env python3
import json,subprocess
from pathlib import Path
import zc
R=Path(__file__).parent
p=R/'fresh-build/entries/t_b986851dcdd8/evidence.draft.json';d=json.loads(p.read_text(encoding='utf8'))
for s in d['Entry']['Senses']:
 for a in s.get('ClaimAnchors',[]):
  actor=a.get('ActorAttribution') or {}
  if actor.get('ActorRole')=='metadata':actor['ActorRole']='compiler'
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8');subprocess.run(['python3',str(R/'compile_evidence_draft.py'),str(p)],check=True,stdout=subprocess.DEVNULL)
p=R/'fresh-build/entries/t_f1b933473387/evidence.draft.json';d=json.loads(p.read_text(encoding='utf8'))
for s in d['Entry']['Senses']:
 for a in s.get('ClaimAnchors',[]):
  if '葛藤公案' in a.get('Kwic',''):
   a['AttributionNote']='Blue Cliff Record by Yuanwu Keqin (佛果圜悟禪師碧巖錄): Yuanwu Keqin is the exact speaker who calls these vine-tangle cases and challenges a clear-eyed person to explain them.'
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8');subprocess.run(['python3',str(R/'compile_evidence_draft.py'),str(p)],check=True,stdout=subprocess.DEVNULL)
