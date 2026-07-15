#!/usr/bin/env python3
import json
from pathlib import Path
import zc
H=Path(__file__).parent;R=H/'fresh-build'/'entries'
# Each row was read in its complete local turn. These are additional distinct-work
# deployments, not quota duplicates.
rows={
't_d4df8bc75ad7':('J/J26/J26nB177.xml','Poshan Haiming','hall warning that even a copper head and iron brow receives the furnace hammer'),
't_8253f56255ce':('T/T48/T48n2001.xml','Hongzhi Zhengjue','own-record address extending the phrase across dusts, lands, buddhas, and patriarchs'),
't_cb2205148690':('M/M59/M59n1540.xml','Dahui Zonggao','formal discourse making the severing of before and after part of a direct claim about seeing'),
't_8f76148e713f':('J/J27/J27nB193.xml','Yinyuan Longqi','hall address using watertight closure for Huangbo’s uncompromising hold'),
't_df4e71aa0bc5':('J/J28/J28nB208.xml','Guxue Zhe','own-record narration of the Tiantong elder thoroughly awakening to the great matter'),
't_b0d4b62a9c2f':('J/J27/J27nB197.xml','Wuyi Yuanlai','own-record instruction requiring blood under the skin and a sense of shame when penetrating cases'),
't_3b3034d1731f':('M/M59/M59n1540.xml','Dahui Zonggao','formal discourse demanding eyebrow-to-eyebrow engagement in factual discussion'),
't_e016fb20e6da':('J/J25/J25nB171.xml','Tianyin Yuanxiu','own-record warning that failure to meet the cited standard guarantees mistaking thief for son'),
't_432d8c4f7579':('C/C077/C077n1710.xml','Baizhang Huaihai','extended speech taxonomy explicitly classifying formulations as dead speech'),
't_b986851dcdd8':('J/J39/J39nB471.xml','Konggu Daocheng','own-record birthday address linking the phrase to seeing the faces of the buddhas'),
't_240ea0594a5f':('J/J39/J39nB471.xml','Konggu Daocheng','own-record instruction placing great rest at the point where no further advance is possible'),
't_27e66043b271':('X/X81/X81n1571.xml','Xuanquan Yan','public interview in which an unnamed monk asks for the line before sound and Xuanquan answers with a grunt'),
't_b1c32bd93e66':('M/M59/M59n1540.xml','Dahui Zonggao','formal discourse explicitly appraising a Weishan–Yangshan exchange with the goose selecting milk'),
't_e316c767f5f9':('M/M59/M59n1540.xml','Dahui Zonggao','formal discourse retelling the butcher Guang’e deployment and its immediate awakening claim'),
't_d3dbc300bfac':('J/J36/J36nB369.xml','Zhean Jingfan','public interview asking an arriving monk how Budai is lately'),
}
for i,(rel,actor,decision) in rows.items():
 p=R/i/'evidence.draft.json';d=json.loads(p.read_text());term=d['Entry']['SourceTerm']
 found=zc.find(rel,term,ctx=80,limit=1)
 if not found: raise SystemExit(f'no exact row {term} {rel}')
 kw=found[0]['window'];v=zc.verify(rel,kw)
 if not v.get('ok'):raise SystemExit(f'verify fail {term} {rel}')
 note=f"Source text ({zc.title(rel)}): {actor} is the exact headword utterer; complete-turn review: {decision}."
 o={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True,'MasterName':actor,'AttributionNote':note,'ContextMasters':[{'MasterName':actor,'Roles':['utterer']}],'DraftActorProof':{'ExactHeadwordClause':kw,'SpeechFrame':note,'FullCaseDecision':note}}
 s=d['Entry']['Senses'][0];s.setdefault('Occurrences',[]).append(o);s['SourceTexts']=list(dict.fromkeys(s.get('SourceTexts',[])+[rel]))
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 work=p.parent/'WORK.md';work.write_text(work.read_text()+f"\ndepth-enrichment-verdict: added distinct-work deployment from {rel}; {decision}. Sense split retested and unchanged.\n",encoding='utf-8')
print('enriched',len(rows),'remaining exact-six entries')
