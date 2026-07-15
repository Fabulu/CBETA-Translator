#!/usr/bin/env python3
"""Repair exactly the eight REVISE rows from the independent 1101–1120 review."""
import json,re
from pathlib import Path
R=Path(__file__).resolve().parents[2]
IDS=['t_9d60d7613392','t_41476f956295','t_746f990fba78','t_64109b94980d','t_78d931324d99','t_b49a2783af81','t_f0fac372131b','t_5b4dd0205486']
RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
TITLE_BY_REL={
 'C/C077/C077n1710.xml':'古尊宿語錄','X/X70/X70n1386.xml':'石田法薰禪師語錄',
 'X/X82/X82n1571.xml':'五燈全書(第34卷-第120卷)','X/X81/X81n1568.xml':'五燈嚴統(第10卷-第25卷)',
 'J/J28/J28nB219.xml':'紫竹林顓愚衡和尚語錄','X/X78/X78n1556.xml':'建中靖國續燈錄',
 'J/J39/J39nB454.xml':'頻吉祥禪師語錄','J/J26/J26nB187.xml':'天岸昇禪師語錄',
 'J/J27/J27nB189.xml':'三宜盂禪師語錄','J/J29/J29nB239.xml':'吹萬禪師語錄',
 'X/X68/X68n1318.xml':'續古尊宿語要','X/X66/X66n1296.xml':'宗門拈古彙集',
 'J/J33/J33nB287.xml':'自閒覺禪師語錄','J/J40/J40nB483.xml':'竺峰敏禪師語錄'}
def root(d):return d.get('Entry',d)
def flat(d):return [o for s in root(d)['Senses'] for o in s['Occurrences']]
def source(o):
 m=re.search(r'Source text \(([^)]+)\)',o.get('AttributionNote',''))
 if m:return m.group(1)
 return TITLE_BY_REL[o['RelPath']]
def named(o,name,note,ctx=()):
 title=source(o)
 o.pop('ActorAttribution',None);o['MasterName']=name;o['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}]+[{'MasterName':n,'Roles':[r]} for n,r in ctx]
 o['AttributionNote']='Source text ('+title+'): '+note+' Exact-turn canary: the named cue or continuous authored unit was checked on both sides of the headword.'
def unnamed(o,label,note,contexts=()):
 title=source(o)
 o.pop('MasterName',None);o['ContextMasters']=[{'MasterName':n,'Roles':[r]} for n,r in contexts]
 o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'monastic questioner','ActorLabel':label,'ActorRole':'questioner','RungsChecked':RUNGS,'GrammarEvidence':note,'ReviewedBy':'Codex f004 lane C repair author','ReviewedUtc':'2026-07-15T11:15:00Z','AuthoredVoiceRiskReviewed':True}
 o['AttributionNote']='Source text ('+title+'): '+note+' Exact-turn canary: the explicit monk-question clause contains the headword and the master’s reply begins only afterward.'
def repair(d,id):
 o=flat(d)
 if id=='t_9d60d7613392': named(o[0],'Zhaozhou Congshen','Zhaozhou Congshen utters the headword in his uninterrupted formal address in the Zhaozhou record.')
 elif id=='t_41476f956295':
  named(o[3],'Shitian Faxun','Shitian Faxun utters the headword in his own formal general address.')
  named(o[4],'Zhaozhou Congshen','The explicit master/questioner exchange assigns the headword-bearing speech to Zhaozhou Congshen.')
 elif id=='t_746f990fba78':
  unnamed(o[0],'the unnamed monastic questioning Lingyin Huiguang','The explicit monk-question frame assigns the headword to the unnamed monastic; Lingyin Huiguang answers.',(('Lingyin Huiguang','respondent'),))
  unnamed(o[1],'the unnamed monastic questioner','The explicit question/master-answer frame assigns the headword to the unnamed monastic.')
  named(o[5],'Zhuanyu Guanheng','Zhuanyu Guanheng utters the headword in his uninterrupted retreat-opening address.')
  unnamed(o[6],'the unnamed monastic questioner','The explicit monk-question frame assigns the headword to the unnamed monastic; the master answers only afterward.')
 elif id=='t_64109b94980d':
  named(o[0],'Huitang Zuxin','Huitang Zuxin gives the direct reply containing the headword.')
  named(o[1],'Pin Jixiang','Pin Jixiang utters the headword in his own retreat-opening address.')
  named(o[2],"Tian'an Sheng","Tian'an Sheng utters the repeated headword in his own evening address.")
  named(o[3],'Sanyi Mingyu','Sanyi Mingyu utters the headword in his own hall address.')
 elif id=='t_78d931324d99':
  named(o[2],'Chuiwan Guangzhen','Chuiwan Guangzhen raises Zhang Shangying in his own case comment.')
  named(o[3],'Biefeng Baoyin','The explicit master-raised-it frame assigns the headword-bearing case comment to Biefeng Baoyin.')
 elif id=='t_b49a2783af81': named(o[2],'Wuzu Shijie','The explicit substitute-answer frame names Wuzu Shijie as the quoted utterer.')
 elif id=='t_f0fac372131b': named(o[4],'Zijian Jue','Zijian Jue utters the fish image in his incoming formal hall address.')
 elif id=='t_5b4dd0205486': named(o[1],'Zhufeng Min','Zhufeng Min utters the heart-incense line in the continuous address introduced by the continuation marker.')
for id in IDS:
 for fn in ('entry.v2.json','evidence.draft.json'):
  p=R/'fresh-build/entries'/id/fn;d=json.loads(p.read_text());repair(d,id);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'repaired':8,'ids':IDS}))
