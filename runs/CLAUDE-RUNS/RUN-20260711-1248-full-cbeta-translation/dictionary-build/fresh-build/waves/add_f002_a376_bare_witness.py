import datetime,json,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
p=R/'fresh-build/entries/t_6ba271127127/evidence.draft.json';root=json.loads(p.read_text(encoding='utf8'));e=root['Entry'];s=e['Senses'][0]
kw='師曰屋裏有一緉破草鞋';rel='B/B14/B14n0082.xml';v=zc.verify(rel,kw);assert v['ok']
name='Bajiao Huiqing';now=datetime.datetime.now(datetime.timezone.utc).isoformat()
o={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'MasterName':name,'Curated':True,
 'AttributionNote':'In the Fragments of the Jade Flowers Collection of Lamp Records (傳燈玉英集（殘卷）), Bajiao Huiqing answers that inside the house is a pair of worn-out straw sandals.',
 'ContextMasters':[{'MasterName':name,'Roles':['utterer','respondent']}],
 'DraftActorProof':{'ExactHeadwordClause':kw,'GrammaticalSubject':name,'SpeechFrame':'The Bajiao Huiqing section marks the answer with 師曰.','FullCaseDecision':'Bajiao Huiqing utters the exact bare-headword answer; this is not the longer 踏破草鞋 compound.'}}
old=next((x for x in s['Occurrences'] if x.get('RelPath')==rel and x.get('Kwic')==kw),None)
if old:old.update(o)
else:s['Occurrences'].append(o)
s['SourceTexts']=list(dict.fromkeys(s['SourceTexts']+[rel]));s['RelatedMasters']=list(dict.fromkeys(s.get('RelatedMasters',[])+[name]));s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)];s['DraftEvidence']['IndependentWorkIds']=list(dict.fromkeys(s['DraftEvidence']['IndependentWorkIds']+[zc.work_id(rel)]));e['WrittenUtc']=now
p.write_text(json.dumps(root,ensure_ascii=False,indent=2)+'\n');print(v)
