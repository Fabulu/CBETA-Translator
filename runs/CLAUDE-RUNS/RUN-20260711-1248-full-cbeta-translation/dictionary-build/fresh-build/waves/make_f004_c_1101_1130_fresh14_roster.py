from pathlib import Path
import json
H=Path(__file__).resolve().parent; R=H.parent.parent
ids=['t_9d60d7613392','t_746f990fba78','t_f9d7324ef449','t_c9940cc4ef80','t_78d931324d99','t_09909bd0c29e','t_1bde390a5df1','t_bf71c3ba483c','t_f0fac372131b','t_5b4dd0205486','t_19abeb747d6d','t_14545d88d530','t_98d9b1ed8cac','t_b021134d0ccb']
aliases={'Longxing':['潭州龍興禪師'],'Xilin Yichen':['邛州西林義琛禪師'],'Xingjiao Weiyi':['杭州南山興教院惟一禪師'],
 'Nanyue Jiqi':['南岳繼起和尚'],'Furi Xi':['佛日晳'],'Yongji Rong':['永吉茸和尚'],'Xiangya Ting':['象崖珽和尚'],
 'Poshan Haiming':['破山海明'],'Hui Ming':['悔明','偃谿真懶子悔明']}
base=[]
for fn in ['f004-laneC-1101-1120-revise8-gate-roster-view.json','f004-laneC-1121-1130-gate-roster-view.json']:
 for c in json.loads((H/fn).read_text())['candidates']:
  if not any(x['canonicalName']==c['canonicalName'] for x in base):base.append(c)
seen={x['canonicalName'] for x in base}
for eid in ids:
 e=json.loads((R/'fresh-build/entries'/eid/'entry.v2.json').read_text())
 for s in e['Senses']:
  for o in s['Occurrences']:
   names=[]
   if o.get('MasterName'):names.append(o['MasterName'])
   names += [x['MasterName'] for x in o.get('ContextMasters',[])]
   for name in names:
    if name in seen:continue
    base.append({'canonicalName':name,'aliases':aliases.get(name,[name]),'evidence':[{'RelPath':o['RelPath'],'FromLb':o['FromLb'],'ToLb':o['ToLb'],'Kwic':o['Kwic']}],
      'reviewedBy':'Codex f004 lane C fresh independent-rereview repair','reviewReport':'fresh-build/waves/f004-laneC-1101-1130-repair-independent-rereview.json','status':'awaiting-roster-integration'})
    seen.add(name)
out={'schemaVersion':1,'rule':'Cohort-local pending roster view only; no shared roster mutation.','candidates':base}
p=H/'f004-laneC-1101-1130-fresh14-gate-roster-view.json';p.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n');print(p,len(base))
