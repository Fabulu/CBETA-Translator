#!/usr/bin/env python3
import json, subprocess, sys
from pathlib import Path
R=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(R)); import zc
rows=json.loads((R/'fresh-build/waves/f003-laneB-751-800-fresh-independent-exact-review.json').read_text())['rows']
ids={r['ordinal']:r['id'] for r in rows}; targets=[765,766,767,768,769,772,773,774,775,776]

repls={
765:{'藏主':'the canon librarian'},
766:{'乃云':'the speech marker “he then said”','臘八午夜':'the midnight of the eighth day of the twelfth month'},
768:{'還覺頂門重麼':'do you still feel the crown of your head is heavy?','頂門':'the crown of the head','還覺':'do you still feel','重麼':'is heavy?'},
769:{'大溈泰禪師祈雨':'the heading for Chan Master Dawei Tai’s rain invocation','上堂':'entered the hall','大龍王':'great dragon king','師問僧':'the master asked the monk'},
773:{'祖曰':'the patriarch said','祖云':'the patriarch said'},
774:{'大溈喆云':'Dawei Zhe said','提綱宗要':'raise the lineage essentials'},
775:{'浴佛':'bathing the Buddha'},
776:{'雲峰悅云':'Yunfeng Wenyue said','無出身之路':'no road of emergence'},
}
for n in targets:
 d=R/'fresh-build/entries'/ids[n]; p=d/'evidence.draft.json'; x=json.loads(p.read_text()); E=x['Entry']
 for s in E['Senses']:
  for o in s['Occurrences']:
   for a,b in repls.get(n,{}).items(): o['AttributionNote']=o.get('AttributionNote','').replace(a,b)
 if n==772:
  s=E['Senses'][0]; kw='孔夫子千古聖人，禁不得子路馮河暴虎。'; v=zc.verify('J/J27/J27nB189.xml',kw); assert v['ok']
  s['Occurrences'].append({'RelPath':'J/J27/J27nB189.xml','FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'MasterName':'Sanyi Mingyu','ContextMasters':[{'MasterName':'Sanyi Mingyu','Roles':['utterer']},{'MasterName':'Zilu','Roles':['person-discussed']}],'Curated':True,'AttributionNote':f'Source text ({zc.title("J/J27/J27nB189.xml")}): Sanyi Mingyu invokes Zilu as the impetuous disciple who fords rivers and fights tigers bare-handed. The headword is inside Sanyi Mingyu’s own address.','DraftActorProof':{'ExactHeadwordClause':kw,'GrammaticalSubject':'Sanyi Mingyu','SpeechFrame':'The passage is continuous speech in Sanyi Mingyu’s recorded sayings.','FullCaseDecision':'Sanyi Mingyu is the exact-headword utterer; Zilu is the person discussed.'}})
 for s in E['Senses']:
  s['SourceTexts']=sorted({o['RelPath'] for o in s['Occurrences']});s['RelatedMasters']=sorted({o['MasterName'] for o in s['Occurrences'] if o.get('MasterName')}|{c['MasterName'] for o in s['Occurrences'] for c in o.get('ContextMasters',[])})
  s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)];s['DraftEvidence']['IndependentWorkIds']=sorted({zc.work_id(o['RelPath']) for o in s['Occurrences']})
 p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n')
 subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True,stdout=subprocess.DEVNULL)
print('finished',len(targets))
