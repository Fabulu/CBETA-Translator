#!/usr/bin/env python3
import copy,glob,json,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
NOW='2026-07-15T00:00:00Z';RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
ids={x['term']:x['id'] for x in json.loads((R/'fresh-build/waves/f002-laneB-401-500-preflight.json').read_text())['entries'][:50]}
idx=[]
for fn in glob.glob(str(R/'terms/*/entry.v2.json')):
 try:d=json.load(open(fn))
 except Exception:continue
 for s in d.get('Senses',[]):
  for o in [*(s.get('Occurrences') or []),*(s.get('ClaimAnchors') or [])]:idx.append(o)
def actor(status,kind,label,role,proof):
 a={'Status':status,'Kind':kind,'ActorLabel':label,'ActorRole':role,'GrammarEvidence':proof,'ReviewedBy':'Codex f002 B401-450 quote anchoring','ReviewedUtc':NOW}
 if status=='reviewed-unnamed':a['RungsChecked']=RUNGS
 return a
def row(q,rel,kw,master=None,a=None):
 v=zc.verify(rel,kw);assert v['ok'],(q,rel,kw,v)
 o={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'ClaimText':q,'Curated':True,'ContextMasters':[]}
 if master:
  o['MasterName']=master;o['ContextMasters']=[{'MasterName':master,'Roles':['utterer']}];o['DraftActorProof']={'ExactHeadwordClause':kw,'SpeechFrame':'The complete case was read around this exact quotation.','FullCaseDecision':f'{master} owns the exact quoted clause.'}
 else:o['ActorAttribution']=a;o['DraftActorProof']={'GrammaticalSubject':a['ActorLabel'],'FullCaseDecision':a['GrammarEvidence']}
 title=zc.title(rel) or rel;who=master or a['ActorLabel'];o['AttributionNote']=f'Source text ({title}). {who} owns the exact anchored quotation in the complete case.';return o
def reused(q):
 for o in idx:
  if q in str(o.get('Kwic','')) and (o.get('MasterName') or o.get('ActorAttribution')):
   x=copy.deepcopy(o);x['ClaimText']=q;x['Curated']=True
   if x.get('MasterName'):x['DraftActorProof']={'ExactHeadwordClause':x['Kwic'],'SpeechFrame':'Previously curated complete-case attribution.','FullCaseDecision':x.get('AttributionNote') or f"{x['MasterName']} owns the clause."}
   else:
    a=x['ActorAttribution'];x['DraftActorProof']={'GrammaticalSubject':a.get('ActorLabel') or 'the textual actor','FullCaseDecision':x.get('AttributionNote') or a.get('GrammarEvidence')}
   return x
 raise KeyError(q)
special={
 '識情':row('識情','J/J33/J33nB294.xml','為你將識情擬議言語差排','Juelang Daosheng'),
 '泥牛入海公案':row('泥牛入海公案','X/X81/X81n1568.xml','後參竺田，值田上堂，舉隱山泥牛入海公案，音聲如雷。',a=actor('narrated','compiler narration','the lamp-record compiler','compiler','The biography narrator reports the later master attending the raised case.')),
 '驗人眼':row('驗人眼','T/T47/T47n2000.xml','看。他老漢。驗人眼目。','Xutang Zhiyu'),
 '尋牛':row('尋牛','J/J23/J23nB129.xml','外更有尋牛以至入廛，亦為圖者十，與今大同小異，俱附末簡，以便參考。','Zhuhong'),
 '見跡':row('見跡','J/J23/J23nB129.xml','納允菴居士見跡第二頌',a=actor('impersonal','section heading','the work compiler','compiler','The work heading names the second picture and verse; it is not a dialogue turn.')),
 '眉毛在麼':row('眉毛在麼','T/T48/T48n2004.xml','翠巖夏末示眾云一夏以來為兄弟說話看。翠巖眉毛在麼','Cuiyan Lingcan'),
 '眉毛落':row('眉毛落','C/C078/C078n1720.xml','眼睛動處眉毛落',a=actor('reviewed-unnamed','verse voice','the unattributed verse voice','verse-author','The phrase occurs in the case verse; all attribution rungs leave its verse author unnamed.')),
 '青州布衫話':row('青州布衫話','X/X82/X82n1571.xml','上堂，舉青州布衫話，頌曰：昨夜三更裏，雨打虗空溼。狸奴知不知，倒上樹梢立。','Dagui Zhengzhang'),
 '三冬無暖氣':row('三冬無暖氣','X/X66/X66n1297.xml','主曰：枯木倚寒巖，三冬無暖氣。',a=actor('reviewed-unnamed','monk','the unnamed hermitage-dweller','respondent','The dialogue marker assigns the reply to the unnamed hermitage-dweller; all six attribution rungs were checked.')),
 '枯木倚寒巖':row('枯木倚寒巖','X/X66/X66n1297.xml','主曰：枯木倚寒巖，三冬無暖氣。',a=actor('reviewed-unnamed','monk','the unnamed hermitage-dweller','respondent','The dialogue marker assigns the reply to the unnamed hermitage-dweller; all six attribution rungs were checked.')),
 '菴':row('菴','X/X66/X66n1297.xml','燒菴婆一婆子供養一菴主，經二十年，常令一女子給侍。',a=actor('narrated','compiler narration','the collection compiler','compiler','The collection narrator introduces the hermitage case and its participants.')),
 '去皮換骨':row('去皮換骨','T/T48/T48n2024.xml','若要超凡入聖。永脫塵勞。直須去皮換骨。絕後再甦。','Duanya Yi'),
}
targets={'情解':['知見','知解','識情'],'泥牛入海':['泥牛入海公案'],'驗人':['驗人眼'],'顧鑒咦':['鑑','顧鑑咦'],'十牛':['尋牛','見跡'],'眉毛墮落':['眉毛在麼','眉毛廝結','眉毛落'],'一指頭禪':['俱胝一指'],'俱胝一指':['一指頭禪'],'青州布衫':['青州布衫話'],'婆子燒庵':['三冬無暖氣','枯木倚寒巖','菴'],'一字關':['顧鑒咦'],'絕後再甦':['去皮換骨','死中得活']}
for term,quotes in targets.items():
 p=R/'fresh-build/entries'/ids[term]/'evidence.draft.json';d=json.loads(p.read_text());s=d['Entry']['Senses'][0];anchors=s.setdefault('ClaimAnchors',[])
 for q in quotes:
  if not any(q==a.get('ClaimText') for a in anchors):anchors.append(copy.deepcopy(special[q] if q in special else reused(q)))
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');print(term,len(quotes))
p=R/'fresh-build/entries'/ids['情解']/'evidence.draft.json';d=json.loads(p.read_text());s=d['Entry']['Senses'][0]
for k,v in enumerate(s['ExplanationParts']['EvidenceBody']):s['ExplanationParts']['EvidenceBody'][k]=v.replace('因言句轉生情解','如今却因言句。轉生情解')
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
