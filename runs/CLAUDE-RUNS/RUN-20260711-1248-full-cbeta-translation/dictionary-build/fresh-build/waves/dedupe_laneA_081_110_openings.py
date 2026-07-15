import json
from pathlib import Path
R=Path(__file__).resolve().parents[2]
preflights=['f001-laneA-076-100-preflight.json','f001-laneA-101-110-preflight.json']
ids=[]
for name in preflights:
 d=json.loads((R/'fresh-build/waves'/name).read_text());ids += [x['id'] for x in (d['entries'][5:] if '076-100' in name else d['entries'])]
for eid in ids:
 p=R/'fresh-build/entries'/eid/'evidence.draft.json';d=json.loads(p.read_text())
 for s in d['Entry']['Senses']:
  parts=s.get('ExplanationParts') or {};opening=parts.get('CorpusEarnedOpening');body=parts.get('EvidenceBody') or []
  if body and body[0].strip()==(opening or '').strip():parts['EvidenceBody']=body[1:]
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')

# 四照用: the heading is compiler narration; Linji is the school/master context.
p=R/'fresh-build/entries/t_4e10d7c80fbc/evidence.draft.json';d=json.loads(p.read_text());o=d['Entry']['Senses'][0]['Occurrences'][0]
o['MasterName']=None
o['ContextMasters']=[{'MasterName':'Linji Yixuan','Roles':['person-discussed','section-subject']}]
o['AttributionNote']='Compiler narration by Huiyan Zhizhao in Eyes of Humans and Devas (人天眼目) supplies the Four Illumination-and-Function heading and definition in the Linji-house section; Linji Yixuan is the contextual master, not a quoted speaker of this clause.'
o['ActorAttribution']={'Status':'narrated','Kind':'compiler heading and exposition','ActorLabel':'Huiyan Zhizhao','ActorRole':'compiler','GrammarEvidence':'The headword and definition occur in compiler exposition under the Linji-house heading, without a live speech marker assigning the clause to a participant.','ReviewedBy':'Codex f001 lane A semantic rereview repair','ReviewedUtc':'2026-07-15T00:00:00Z'}
o['DraftActorProof']={'GrammaticalSubject':'Huiyan Zhizhao’s compiler exposition','FullCaseDecision':'Huiyan Zhizhao owns the narrated heading and definition; Linji Yixuan is the contextual school master.'}
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')

# 四料揀, Yongming sense: spell out the four named pairs.
p=R/'fresh-build/entries/t_81147ad4e8bf/evidence.draft.json';d=json.loads(p.read_text());s=d['Entry']['Senses'][1]
opening='Yongming’s Four Selections contrast four pairs: Chan with Pure Land, Pure Land without Chan, Chan without Pure Land, and neither Chan nor Pure Land; the cited scheme remains distinct from Linji’s encounter selections.'
s['ExplanationParts']['CorpusEarnedOpening']=opening
s['ExplanationParts']['EvidenceBody']=['Hengshan Dengbing introduces Yongming as the cited source, while Yongjue Yuanxian later characterizes the same fourfold scheme as praise and encouragement to return to Pure Land.']
s['DraftEvidence']['ZenBend']=opening
s['DraftEvidence']['CounterexampleOrLimit']=s['ExplanationParts']['EvidenceBody'][-1]
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
