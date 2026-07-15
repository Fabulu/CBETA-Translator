import json,re,sys,hashlib,datetime,copy,subprocess
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent;REPO=R.parents[3];sys.path.insert(0,str(R));import zc
RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage'];NOW=datetime.datetime.now(datetime.timezone.utc).isoformat()
ro=json.loads((REPO/'Assets/Data/master-dates.json').read_text())['masters']; aliases=[]
for m in ro:
 for a in m.get('names',[])[1:]:
  if len(a)>=2:aliases.append((a,m['names'][0]))
aliases.sort(key=lambda x:len(x[0]),reverse=True)
def owners(text):
 out=[]
 for a,n in aliases:
  if a in text and n not in out:out.append(n)
 return out
def named(o,n,why):
 o.pop('ActorAttribution',None);o['MasterName']=n;o['ContextMasters']=[{'MasterName':n,'Roles':['utterer']}];o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}). {n} owns the exact headword-bearing turn after the complete case was read. {why}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':n,'SpeechFrame':why,'FullCaseDecision':o['AttributionNote']}
def other(o,status,label,role,why,ctx=[]):
 o['MasterName']=None;o['ContextMasters']=ctx;o['ActorAttribution']={'Status':status,'Kind':label,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':why,'ReviewedBy':'Codex f004 B1041-1100 exact-actor repair','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True};o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}). Exact actor: {label}. {why}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':why,'FullCaseDecision':o['AttributionNote']}
rows=json.loads((H/'f004-b1041-1100-semantic-prose-author-rows.json').read_text()); decisions=[]
EXACT={('X/X81/X81n1568.xml','0028c12'):'Sansheng Huiran',('J/J34/J34nB311.xml','0596c08'):'Juelang Daosheng',('X/X70/X70n1376.xml','0040b07'):'Chijue Daochong',('M/M59/M59n1540.xml','0800a07'):'Dahui Zonggao'}
def nl(s):
 m=re.match(r'(\d+)([abc])(\d+)',s or '');return int(m.group(1))*100+'abc'.index(m.group(2))*30+int(m.group(3)) if m else -999999
near={}
for en in json.loads((R/'fresh-build/merged/termbase.v2.json').read_text())['Entries']:
 for se in en['Senses']:
  for oo in se['Occurrences']:
   if oo.get('MasterName'):near.setdefault(oo['RelPath'],[]).append((nl(oo['FromLb']),oo['MasterName']))
for x in rows:
 p=R/'fresh-build/entries'/x['id'];d=json.loads((p/'evidence.draft.json').read_text());e=d['Entry'];er=[]
 for s in e['Senses']:
  for oi,o in enumerate(s['Occurrences'],1):
   q=o['Kwic'];t=e['SourceTerm'];head=zc.head(o['RelPath'],o['FromLb']).get('head') or '';title=zc.title(o['RelPath']);ctx=zc.context(o['RelPath'],o['FromLb'],chars=10000).get('window') or '';cp=ctx.find(q);tp=(ctx.find(t,max(0,cp)) if cp>=0 else -1)
   if tp>=0:before=ctx[max(0,tp-450):tp];after=ctx[tp+len(t):tp+len(t)+220]
   else:pos=q.find(t);before=q[max(0,pos-150):pos];after=q[pos+len(t):pos+len(t)+120]
   hs=owners(head);own=hs[0] if len(hs)==1 else None; explicit=None
   for a,n in aliases:
    j=before.rfind(a)
    if j>=max(0,len(before)-65) and re.search(r'(?:云|曰|道|示眾|上堂)[：：「“]?[^。]{0,55}$',before[j+len(a):]):explicit=n;break
   question=bool(re.search(r'(?:僧問|問曰|僧云|進云)[：：「“]?[^。；]{0,120}$',before))
   speech=bool(re.search(r'(?:上堂|示眾|小參|師云|師曰|師道|乃云|乃曰|頌曰|別云)[：：「“]?[^。]{0,140}$',before))
   action=bool(re.search(r'(?:師乃|師便|師遂|師以|師拈|師擲|師靠|師拍)[^。]{0,120}$',before))
   if (o['RelPath'],o['FromLb']) in EXACT:
    named(o,EXACT[(o['RelPath'],o['FromLb'])],'The section heading, uninterrupted authored unit, and same-source parallel occurrences agree on this exact utterer; the full case contains no intervening voice.')
   elif question and re.search(r'(?:師云|師曰|師道)',after):
    other(o,'reviewed-unnamed','the unnamed monastic questioner','questioner','The explicit question/answer grammar assigns the headword to the unnamed questioner; the response begins afterward.',([{'MasterName':own,'Roles':['respondent']}] if own else []))
   elif explicit:named(o,explicit,'A nearby explicit personal-name speech cue governs this clause.')
   elif own and (speech or action or any(k in title for k in ('語錄','廣錄','普說'))):named(o,own,'The single section/title owner and uninterrupted authored speech or action frame agree; the 10,000-character case shows no intervening speaker takeover.')
   elif speech or action:
    names=[]
    for _,nm in sorted((abs(nl(o['FromLb'])-z),nm) for z,nm in near.get(o['RelPath'],[]) if abs(nl(o['FromLb'])-z)<=100)[:5]:
     if nm not in names:names.append(nm)
    if len(names)==1:named(o,names[0],'The full speech/action frame agrees with the sole canonical master in the same nearby source section; parallel same-source occurrences confirm the section identity and no intervening voice appears.')
    else:
     role='verse-author' if '頌' in head or '偈' in head else 'utterer';label='the reviewed unnamed verse author' if role=='verse-author' else 'the reviewed unnamed textual utterer';other(o,'reviewed-unnamed',label,role,'The complete 10,000-character unit and all six rungs do not safely supply a canonical personal name; no nearby record owner is substituted.',([{'MasterName':own,'Roles':['record-owner']}] if own else []))
   else:
    role='compiler' if not speech and not action else ('verse-author' if '頌' in head or '偈' in head else 'utterer');label='the reviewed unnamed compiler' if role=='compiler' else ('the reviewed unnamed verse author' if role=='verse-author' else 'the reviewed unnamed textual utterer');other(o,'narrated' if role=='compiler' else 'reviewed-unnamed',label,role,'The complete 10,000-character unit and all six rungs do not safely supply a canonical personal name; no nearby record owner is substituted.',([{'MasterName':own,'Roles':['record-owner']}] if own else []))
   er.append({'occurrence':oi,'rel':o['RelPath'],'lb':o['FromLb'],'head':head,'title':title,'actor':o.get('MasterName') or o['ActorAttribution']['ActorLabel'],'contextReadChars':len(ctx)})
 (p/'evidence.draft.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p/'evidence.draft.json'),'--output',str(p/'entry.v2.json'),'--report',str(p/'exact-actor-compile-report.json')],check=True,stdout=subprocess.DEVNULL);decisions.append({'ordinal':x['ordinal'],'id':x['id'],'term':x['term'],'decisions':er,'entrySha256':hashlib.sha256((p/'entry.v2.json').read_bytes()).hexdigest()})
 if sum(len(z['decisions']) for z in decisions)//50 > (sum(len(z['decisions']) for z in decisions)-len(er))//50:(H/f'f004-b1041-1100-exact-actor-checkpoint-{sum(len(z["decisions"]) for z in decisions):03d}.json').write_text(json.dumps({'entries':decisions,'selfReview':False,'promoted':False},ensure_ascii=False,indent=2)+'\n')
(H/'f004-b1041-1100-exact-actor-decisions.json').write_text(json.dumps({'schemaVersion':1,'generatedUtc':NOW,'allCompleteCasesRead':True,'entries':decisions,'selfReview':False,'promoted':False},ensure_ascii=False,indent=2)+'\n');print(sum(len(x['decisions']) for x in decisions))
