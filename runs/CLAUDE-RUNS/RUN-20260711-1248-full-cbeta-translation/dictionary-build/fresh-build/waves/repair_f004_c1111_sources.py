#!/usr/bin/env python3
"""Replace the four rejected Zhang Shangying paratext hits with case evidence."""
import datetime,json,sys
from pathlib import Path
H=Path(__file__).resolve().parent; R=H.parent.parent; sys.path.insert(0,str(R)); import zc
term='張商英'
choices=[
 ('X/X68/X68n1319.xml',0,'Zhang Shangying’s own recorded statement about the teaching at a mote-tip'),
 ('X/X85/X85n1593.xml',0,'biographical account of his meeting with Doushuai Congyue'),
 ('J/J29/J29nB239.xml',0,'Chuiwan’s raising and appraisal of the Doushuai–Zhang exchange'),
 ('X/X68/X68n1318.xml',0,'a later named master raises Zhang’s Jiangling encounter as a public case'),
]
rows=[]
for rel,index,reason in choices:
 found=zc.find(rel,term,ctx=180,limit=12)
 # X68n1318 has only one relevant hit; the others use their first substantive hit.
 x=found[index]; v=zc.verify(rel,x['window']); assert v.get('ok')
 rows.append({'workId':zc.work_id(rel),'RelPath':rel,'title':zc.title(rel),'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':x['window'],
  'completeContext':zc.context(rel,v['fromLb'],chars=2000,kwic=x['window']),'sectionHead':zc.head(rel,v['fromLb']).get('head'),
  'zcVerified':True,'admitted':True,'admissionReason':reason})
p={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f004','lane':'C','ordinal':1111,
 'id':'t_78d931324d99','term':term,'rejectedOriginalRows':4,'rejectionReason':'all four initial rows were contents, index, or title paratext and did not define the figure by Zen deployment',
 'replacementRows':rows,'distinctWorks':len({x['workId'] for x in rows}),'allKwicsVerified':all(x['zcVerified'] for x in rows),
 'senseDecision':'one person: the Song chancellor and lay Chan student; biography, own words, and later case-raising are deployments of the same figure',
 'corpusEarnedOpening':'Zhang Shangying is the Song chancellor whom Zen records preserve as Doushuai Congyue’s tested lay student, a speaker about Chan claims, and a named participant in cases later masters raise and criticize.',
 'scopeLimit':'The entry defines him by those recorded meetings, words, patronage, and later case use, not by a general political biography.'}
(H/'f004-laneC-1111-source-replacement.json').write_text(json.dumps(p,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'hardPass':p['allKwicsVerified'] and p['distinctWorks']==4,'replacementRows':4},ensure_ascii=False))
