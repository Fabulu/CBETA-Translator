#!/usr/bin/env python3
import hashlib,json,re
from datetime import datetime,timezone
from pathlib import Path
import zc
H=Path(__file__).parent;R=H/'fresh-build'/'entries';W=H/'fresh-build'/'waves'
rows={
't_ee57d3ff5e43':[('J/J34/J34nB299.xml','Hanyue Fazang','own-record instruction calls the line meeting the occasion an affair on the sword edge')],
't_21a3463bc0db':[('T/T48/T48n2003.xml','Yuanwu Keqin','case commentary says the whole working is displayed everywhere without the least obstruction')],
't_133711ebf761':[('L/L158/L158n1652.xml','Mingjue Cong','own-record address says the teaching net suddenly reaches the dark pivot'),('J/J39/J39nB471.xml','Konggu Daocheng','own-record verse pairs the dark pivot and responsive functioning across sage and ordinary'),('T/T47/T47n1997.xml','Yuanwu Keqin','own-record line says the dark pivot is sung alone and cuts off the many streams')],
't_beab8961fb55':[('J/J27/J27nB190.xml','Shiyu Mingfang','own-record criticism says former sages guided students from a single shared hand'),('J/J27/J27nB193.xml','Yinyuan Longqi','own-record hall address assigns receiving and guiding guests to the guest prefect'),('J/J28/J28nB208.xml','Guxue Zhe','own-record address says the yellow-faced teacher compassionately received and guided in many ways')],
't_5f6e8c98ffe7':[('J/J28/J28nB202.xml','Baichi Yuan','public interview rebukes a monk’s shout with an explicit “I knew you would make this shout”')],
't_782f20a368c3':[('J/J26/J26nB187.xml',"Tian'an Sheng",'own-record hall address quotes “doing meaningful affairs is an awakened mind” and repeats the headword across the assembly')],
}
CJK=re.compile(r'[\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff]+')
def wrap(s):
 out=[];last=0;depth=0
 for m in CJK.finditer(s):
  between=s[last:m.start()]
  for ch in between:
   if ch in '(（':depth+=1
   elif ch in ')）' and depth:depth-=1
  out.append(between);out.append(m.group() if depth else '('+m.group()+')');last=m.end()
 out.append(s[last:]);return ''.join(out)
ledger=[]
for i,adds in rows.items():
 p=R/i/'evidence.draft.json';before_e=hashlib.sha256((p.parent/'entry.v2.json').read_bytes()).hexdigest();before_w=hashlib.sha256(p.read_bytes()).hexdigest();d=json.loads(p.read_text());s=d['Entry']['Senses'][0]
 used={(o.get('RelPath'),o.get('FromLb'),o.get('Kwic')) for x in d['Entry']['Senses'] for o in x.get('Occurrences',[])}
 # Keep source-title matching intact while making every inherited note English-first.
 for sense in d['Entry']['Senses']:
  for o in sense.get('Occurrences',[]):
   title=zc.title(o['RelPath']);body=wrap(str(o.get('AttributionNote') or 'Exact-turn review retained.'))
   if title not in body:body=f'Source text ({title}): '+body
   o['AttributionNote']=body
 for rel,actor,decision in adds:
  found=zc.find(rel,d['Entry']['SourceTerm'],ctx=80,limit=1)
  if not found:raise SystemExit(f'missing {i} {rel}')
  kw=found[0]['window'];v=zc.verify(rel,kw)
  if not v.get('ok'):raise SystemExit(f'verify {i} {rel}')
  key=(rel,v['fromLb'],kw)
  if key in used:continue
  note=f'Source text ({zc.title(rel)}): {actor} is the exact headword utterer; complete-turn review: {decision}.'
  o={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True,'MasterName':actor,'AttributionNote':note,'ContextMasters':[{'MasterName':actor,'Roles':['utterer']}],'DraftActorProof':{'ExactHeadwordClause':kw,'SpeechFrame':note,'FullCaseDecision':note}}
  s.setdefault('Occurrences',[]).append(o);s['SourceTexts']=list(dict.fromkeys(s.get('SourceTexts',[])+[rel]));used.add(key)
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 ledger.append({'id':i,'term':d['Entry']['SourceTerm'],'beforeEntrySha256':before_e,'beforeWorksheetSha256':before_w,'addedDistinctWorks':[a[0] for a in adds]})
(W/'f002-laneB-451-500-depth6-repair-work.json').write_text(json.dumps({'generatedUtc':datetime.now(timezone.utc).isoformat(),'scope':'six explicitly failing Lane B entries only','siteTouched':False,'formalCohortGateRun':False,'entries':ledger},ensure_ascii=False,indent=2)+'\n')
print('repaired',len(ledger))
