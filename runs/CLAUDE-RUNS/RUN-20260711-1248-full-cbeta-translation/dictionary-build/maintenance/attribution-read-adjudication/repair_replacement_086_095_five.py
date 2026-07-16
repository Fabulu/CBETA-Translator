#!/usr/bin/env python3
import hashlib, json
from datetime import datetime, timezone
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]; FRESH=ROOT/'fresh-build'/'entries'
OUT=Path(__file__).with_name('independent-review-1-3-086-095-replacement-author-repair-ledger.json')
IDS=['t_c212062774f9','t_c4a694970a12','t_cbf868f557e2','t_d8868082a16c','t_e17068150613']
ROLE_MAP={'action-performer':'person-described','hall-speaker':'section-subject','case-teacher':'teacher','case-source':'case-figure','quoted-utterer':'utterer','substitute-answerer':'case-figure'}

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def actor(kind,label,role='compiler',evidence=''):
 return {'Status':'narrated','Kind':kind,'ActorLabel':label,'ActorRole':role,'RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':evidence,'ReviewedBy':'Codex literal full-case replacement repair','ReviewedUtc':datetime.now(timezone.utc).isoformat().replace('+00:00','Z')}
def cm(name,*roles): return {'MasterName':name,'Roles':list(dict.fromkeys(roles))}
def normalize(d):
 for s in d['Senses']:
  for o in s['Occurrences']:
   aa=o.get('ActorAttribution') or {}
   if aa.get('ActorRole')=='narrator': aa['ActorRole']='compiler'
   for c in o.get('ContextMasters') or []:
    c['Roles']=list(dict.fromkeys(ROLE_MAP.get(r,r) for r in c.get('Roles',[])))

def add_pending():
 p=ROOT/'fresh-build'/'pending-roster.json'; d=json.loads(p.read_text()); known={x['canonicalName'] for x in d['candidates']}
 rows=[
  ('Wuwei Zongtai',['漢州無為宗泰禪師','宗泰'],'X/X82/X82n1571.xml','0089c18','漢州無為宗泰禪師'),
  ('Qingshan Shoulong',['杭州慶善守隆禪師','守隆'],'X/X81/X81n1568.xml','0061c17','杭州慶善守隆禪師'),
  ('Zhenru Muzhe',['潭州大溈真如慕喆禪師','慕喆'],'X/X79/X79n1559.xml','0317a22','潭州大溈真如慕喆禪師')]
 for name,aliases,rel,lb,kw in rows:
  if name not in known:
   d['candidates'].append({'canonicalName':name,'aliases':aliases,'evidence':[{'RelPath':rel,'FromLb':lb,'ToLb':lb,'Kwic':kw}], 'reviewedBy':'Codex 086-095 complete-section repair','reviewReport':'independent-review-1-3-086-095-replacement.json','status':'awaiting-roster-integration'})
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')

def main():
 add_pending(); rows=[]
 for tid in IDS:
  p=FRESH/tid/'entry.v2.json'; d=json.loads(p.read_text()); before=sha(p); term=d['SourceTerm']
  if term=='禪床':
   o=d['Senses'][0]['Occurrences'][6]
   o['MasterName']=None; o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'verse author','ActorLabel':'the unnamed author of the 頌曰 verse','ActorRole':'verse-author','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':'The complete unit introduces the headword-bearing lines with 頌曰, but none of the six attribution rungs names that verse author. Nanyang Huizhong belongs to the case quoted after the verse and is not its author.','ReviewedBy':'Codex literal full-case replacement repair','ReviewedUtc':datetime.now(timezone.utc).isoformat().replace('+00:00','Z')}
  elif term=='洗鉢盂':
   occ=d['Senses'][0]['Occurrences']
   occ[0]['ContextMasters']=[cm('Zhenru Muzhe','section-subject','person-described')]
   occ[1]['ContextMasters']=[cm('Wuzu Fayan','teacher'),cm('Zhaozhou Congshen','case-figure'),cm('Wuwei Zongtai','section-subject','person-described')]
   occ[5]['MasterName']='Qingshan Shoulong'; occ[5].pop('ActorAttribution',None); occ[5]['ContextMasters']=[cm('Qingshan Shoulong','utterer','section-subject')]
  elif term=='陞座':
   names=[('Fachang Yiyu',0),('Fayan Wenyi',2),('Shakyamuni Buddha',3),('Huangbo Xiyun',4),('Huqiu Shaolong',5),('Yushan Shangsi',6),('Yulin Tongxiu',7)]
   for name,i in names: d['Senses'][0]['Occurrences'][i]['ContextMasters']=[cm(name,'person-described','section-subject')]
  elif term=='代云':
   occ=d['Senses'][0]['Occurrences']
   contexts={0:[cm('Foyan Qingyuan','later-raiser','section-subject'),cm('Yunmen Wenyan','case-figure')],1:[cm('Yunmen Wenyan','case-figure')],2:[cm('Yunmen Wenyan','case-figure')],3:[cm('Yunmen Wenyan','case-figure')],4:[cm('Lingrui','case-figure','section-subject')],5:[cm('Fenyang Shanzhao','case-figure','section-subject')],7:[cm('Fayan Wenyi','case-figure')]}
   for i,cms in contexts.items():
    occ[i]['MasterName']=None; occ[i]['ContextMasters']=cms
    occ[i]['ActorAttribution']=actor('reporting narration','the recorder of the substitute answer',evidence='代云/自代云 is the recorder’s reporting verb introducing a substitute answer; the named master supplies the answer but does not utter the reporting label itself.')
   # occurrence 7 is genuinely in the unnamed monk's direct 進云 turn.
   occ[6]['ContextMasters']=[cm('Chushi Fanqi','respondent','section-subject'),cm('Xuedou Chongxian','case-figure')]
  elif term=='法身邊事':
   occ=d['Senses'][0]['Occurrences']
   occ[4]['ContextMasters']=[cm('Langye Huijue','utterer','person-described','section-subject')]
   # X80 parallel belongs to the explicitly headed Langye Huijue section.
   occ[5]['MasterName']='Langye Huijue'; occ[5].pop('ActorAttribution',None); occ[5]['ContextMasters']=[cm('Langye Huijue','utterer','section-subject')]
  normalize(d); p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
  rows.append({'entryId':tid,'term':term,'beforeSha256':before,'afterSha256':sha(p),'reviewSource':'all complete packet units read; independent-review-1-3-086-095-replacement.json'})
 OUT.write_text(json.dumps({'selfReview':False,'rows':rows},ensure_ascii=False,indent=2)+'\n')
if __name__=='__main__': main()
