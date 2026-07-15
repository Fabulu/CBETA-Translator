import json,hashlib,datetime,subprocess,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];W=R/'fresh-build'/'waves';E=R/'fresh-build'/'entries';NOW=datetime.datetime.now(datetime.timezone.utc).isoformat()
# Human-read complete-case decisions. Keys are immutable source anchors.
D={
('J/J34/J34nB300.xml','0251a27'):'Chaozong Tongren',('J/J33/J33nB294.xml','0730c17'):'Langting Ting',('J/J38/J38nB406.xml','0140b13'):'Tianran Hanshi',
('C/C077/C077n1710.xml','0641c07'):'Linji Yixuan',('T/T48/T48n2001.xml','0014a19'):'Hongzhi Zhengjue',('X/X69/X69n1359.xml','0509b08'):"Ying'an Tanhua",
('X/X82/X82n1571.xml','0075a06'):'Yangqi Fanghui',('J/J27/J27nB190.xml','0106a07'):'Shiyu Mingfang',('J/J34/J34nB311.xml','0590a04'):'Juelang Daosheng',
('X/X80/X80n1565.xml','0050b09'):'Tianzhu Chonghui',('X/X82/X82n1571.xml','0035c23'):'Zhenjing Kewen',('X/X84/X84n1583.xml','0436b21'):'Quanan Qiji',
('T/T47/T47n1998A.xml','0867b13'):'Dahui Zonggao',('X/X69/X69n1356.xml','0376c02'):'Puan Yinsu',('J/J26/J26nB177.xml','0011a28'):'Poshan Haiming',
('X/X71/X71n1414.xml','0295a06'):"Liao'an Qingyu",('X/X79/X79n1559.xml','0303c04'):'Yangqi Fanghui',('J/J40/J40nB494.xml','0525c18'):'Yushan Shangsi',
('J/J26/J26nB188.xml','0751a20'):'Ruibai Mingxue',('J/J34/J34nB311.xml','0605c12'):'Juelang Daosheng',('X/X78/X78n1553.xml','0516a06'):'Lushan Huacheng Jian',
('T/T47/T47n1997.xml','0717b06'):'Yuanwu Keqin',('X/X66/X66n1296.xml','0017c15'):'Hongzhi Zhengjue',('X/X83/X83n1578.xml','0415b02'):'Baozhi',
('B/B25/B25n0145.xml','0724a15'):'Zhongfeng Mingben'}
rows=json.loads((W/'f004-b1041-1100-semantic-prose-author-rows.json').read_text());applied=[]
for x in rows:
 p=E/x['id'];d=json.loads((p/'evidence.draft.json').read_text());changed=False
 for s in d['Entry']['Senses']:
  for o in s['Occurrences']:
   k=(o['RelPath'],o['FromLb'])
   if k not in D:continue
   n=D[k];o.pop('ActorAttribution',None);o['MasterName']=n;o['ContextMasters']=[{'MasterName':n,'Roles':['utterer']}];proof='The complete source unit was read: its named section and uninterrupted 師云, 上堂, 小參, or authored-address frame assign this exact headword-bearing turn to the named master, with no embedded speaker takeover.';o['AttributionNote']=f'Source text ({o["RelPath"]}). {n} utters the exact headword-bearing wording. {proof}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':n,'SpeechFrame':proof,'FullCaseDecision':proof};applied.append({'id':x['id'],'term':x['term'],'rel':k[0],'lb':k[1],'actor':n});changed=True
 if changed:
  (p/'evidence.draft.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p/'evidence.draft.json'),'--output',str(p/'entry.v2.json'),'--report',str(p/'immutable-actor-compile-report.json')],check=True,stdout=subprocess.DEVNULL)
(W/'f004-b1041-1100-immutable-actor-decisions.json').write_text(json.dumps({'schemaVersion':1,'generatedUtc':NOW,'decisions':applied,'decisionCount':len(applied),'selfReview':False,'promoted':False},ensure_ascii=False,indent=2)+'\n');print(len(applied))
