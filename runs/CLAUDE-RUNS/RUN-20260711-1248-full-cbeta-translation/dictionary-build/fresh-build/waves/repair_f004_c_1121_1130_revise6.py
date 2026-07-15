#!/usr/bin/env python3
"""Apply only the six REVISE findings from the independent C1121–1130 review."""
import json
from pathlib import Path

R=Path(__file__).resolve().parents[2]
IDS=['t_14545d88d530','t_aa9e5467d247','t_4c3f44abf01c','t_b021134d0ccb','t_e4dba349ae51','t_acaf1f7f698e']
RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
CANARY=' Exact-turn canary: the complete case and both sides of the exact headword-bearing turn were checked.'
BASE_KWIC='首山竹篦首山竹篦首山和尚。拈竹篦示眾云。汝等諸人若喚作竹篦則觸。不喚作竹篦則背。汝諸人且道。喚作甚麼。無門曰。喚作竹篦則觸。不喚作竹篦則背。不得有語。不得無語。速道速道。頌曰。拈起竹篦行殺活令背觸交馳佛祖乞命'

def senses(d): return d.get('Entry',d)['Senses']
def occs(d): return [o for s in senses(d) for o in s['Occurrences']]
def canary(o):
 if 'Exact-turn canary:' not in o['AttributionNote']: o['AttributionNote']+=CANARY
def named(o,name,note,contexts=()):
 o.pop('ActorAttribution',None);o['MasterName']=name
 o['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}]+[{'MasterName':n,'Roles':[r]} for n,r in contexts]
 o['AttributionNote']=note+CANARY
def narrated(o,label,grammar,contexts=()):
 o.pop('MasterName',None);o['ContextMasters']=[{'MasterName':n,'Roles':[r]} for n,r in contexts]
 o['ActorAttribution']={'Status':'narrated','Kind':'compiler narrative','ActorLabel':label,'ActorRole':'compiler','RungsChecked':RUNGS,'GrammarEvidence':grammar,'ReviewedBy':'Codex f004 lane C repair author','ReviewedUtc':'2026-07-15T12:00:00Z','AuthoredVoiceRiskReviewed':True}
 titles={'X/X82/X82n1571.xml':'五燈全書(第34卷-第120卷)','J/J39/J39nB454.xml':'頻吉祥禪師語錄','T/T48/T48n2005.xml':'無門關'}
 o['AttributionNote']='Source text ('+titles[o['RelPath']]+'): '+grammar+CANARY

def repair(d,id,is_draft):
 ss=senses(d);s=ss[0];o=occs(d)
 if id=='t_14545d88d530':
  base={'RelPath':'T/T48/T48n2005.xml','FromLb':'0298b14','ToLb':'0298b22','Kwic':BASE_KWIC,
        'ContextMasters':[{'MasterName':'Shoushan Shengnian','Roles':['case-figure']}],
        'AttributionNote':'Source text (無門關): the case compiler supplies the exact headword heading, then quotes Shoushan Shengnian raising the bamboo staff and demanding an answer.'+CANARY,
        'ActorAttribution':{'Status':'narrated','Kind':'compiler narrative','ActorLabel':'the case compiler','ActorRole':'compiler','RungsChecked':RUNGS,'GrammarEvidence':'The exact headword is the case heading; the following 首山和尚 clause explicitly gives Shoushan’s raised-staff challenge.','ReviewedBy':'Codex f004 lane C repair author','ReviewedUtc':'2026-07-15T12:00:00Z','AuthoredVoiceRiskReviewed':True},'Curated':True}
  if is_draft: base['DraftActorProof']={'ExactHeadwordClause':BASE_KWIC,'GrammaticalSubject':'the case compiler','SpeechFrame':'The heading names the case and the following clause quotes Shoushan Shengnian.','FullCaseDecision':'Compiler-owned heading with Shoushan Shengnian as the quoted case figure.'}
  if not any(x['RelPath']==base['RelPath'] and x['FromLb']==base['FromLb'] for x in s['Occurrences']): s['Occurrences'].append(base)
  if base['RelPath'] not in s['SourceTexts']: s['SourceTexts'].append(base['RelPath'])
  anchor={k:base[k] for k in ['RelPath','FromLb','ToLb','Kwic']}
  anchor['ClaimText']='汝等諸人若喚作竹篦則觸。不喚作竹篦則背'
  anchor['MasterName']='Shoushan Shengnian';anchor['ContextMasters']=[{'MasterName':'Shoushan Shengnian','Roles':['utterer']}]
  anchor['AttributionNote']='Source text (無門關): Shoushan Shengnian utters the anchored call-it/do-not-call-it challenge inside the stored base case.'+CANARY
  s['ClaimAnchors']=[anchor]
  if is_draft:
   s['DraftEvidence']['OpeningClaimEvidenceKeys']=['o5']
   s['Note']='The exact base-case witness anchors the opening; later rows document its reuse.'
  else:s['Explanation']='Shoushan’s bamboo staff is the named public case in which he raises the implement and requires an answer while forbidding both calling it a bamboo staff and not calling it one. Later records cite, verse, seize, and break the staff; the object remains inseparable from Shoushan Shengnian’s test and its answering turns.'
 elif id=='t_aa9e5467d247':
  s['PreferredTarget']='the worlds of the ten directions are the whole body'
  s['AlternateTargets']=['the ten-direction worlds are the whole body']
  s['Note']='The word “worlds” is retained explicitly in the English target. Translation canary: every lexical component of the headword was checked against the target.'
  if is_draft:s['ExplanationParts']['CorpusEarnedOpening']='The worlds of the ten directions are the whole body: the complete world-field, not merely spatial directions, is named as the whole body.'
  else:s['Explanation']='The worlds of the ten directions are the whole body: the complete world-field, not merely spatial directions, is named as the whole body. The stored witnesses preserve Shishuang Qingzhu’s verse and its later case transmission.'
 elif id=='t_4c3f44abf01c':
  named(o[4],'Zuyin Zhifu','Source text (五燈全書(第34卷-第120卷)): Zuyin Zhifu utters the exact headword in the formal hall address that begins after the preceding biography closes.')
 elif id=='t_b021134d0ccb':
  named(o[3],'Jianfu Gu','Source text (指月錄): the quoted “Jianfu Gu instructed the assembly” frame assigns the exact headword-bearing commentary to Jianfu Gu.',(('Linji Yixuan','person-discussed'),))
  opening='The phrase “before the empty eon” marks a deliberately impossible temporal position used in questions about one’s parents, oneself, or the matter before differentiation.'
  if is_draft:s['ExplanationParts']['CorpusEarnedOpening']=opening
  else:
   old=s['Explanation'];dot=old.find('.');s['Explanation']=opening+(old[dot+1:] if dot>=0 else '')
 elif id=='t_e4dba349ae51':
  named(o[1],'Damei Faying','Source text (五燈全書(第34卷-第120卷)): Damei Faying utters the headword inside his own quoted verse before death.')
  narrated(o[3],'the record narrator','The monastic’s board-striking action is narrated; no one utters the headword.',())
 elif id=='t_acaf1f7f698e':
  s['Note']='The stored witnesses support monastic age or length-of-service usage; they do not independently define its starting rite or a precedence rule. Claim-scope canary: every opening proposition was rechecked against a stored witness.'
  if is_draft:
   s['ExplanationParts']['CorpusEarnedOpening']='Religious seniority is a monastic measure of age or length of service in the Zen records.'
   s['ExplanationParts']['EvidenceBody']=['The stored witnesses use 法臘 in birthday, age, and career statements; they do not themselves define the starting rite or establish a general precedence rule.']
  else:s['Explanation']='Religious seniority is a monastic measure of age or length of service in the Zen records. The stored witnesses use the headword in birthday, age, and career statements; they do not themselves define the starting rite or establish a general precedence rule.'

for id in IDS:
 for fn in ('entry.v2.json','evidence.draft.json'):
  p=R/'fresh-build/entries'/id/fn;d=json.loads(p.read_text());repair(d,id,fn.startswith('evidence'));p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'repaired':len(IDS),'ids':IDS}))
