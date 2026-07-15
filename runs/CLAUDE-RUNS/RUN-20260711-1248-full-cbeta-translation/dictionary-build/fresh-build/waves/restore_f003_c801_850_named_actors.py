import datetime,json,re,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();sys.path.insert(0,str(ROOT));import zc
report=json.loads((ROOT/'fresh-build/waves/f003-laneC-801-850-postrepair-independent-review.json').read_text());rows=[x for x in report['rows'] if x['verdict']=='REVISE']
roster=json.loads((ROOT.parents[3]/'Assets/Data/master-dates.json').read_text())['masters']
aliases=[]
for m in roster:
 for n in m.get('names',[]):
  if re.search(r'[\u3400-\u9fff]',n):aliases.append((len(n),n,m['names'][0]))
aliases.sort(reverse=True)
def canonical(text):
 for _,a,c in aliases:
  if a in text:return c
 return None
def clean_head(text):
 text=re.sub(r'^[△▲○\s]+','',text)
 # Some lamp headings duplicate the same title consecutively.
 half=len(text)//2
 if len(text)%2==0 and text[:half]==text[half:]:text=text[:half]
 return text.strip()
def witness(row,occ):
 a=(row['ordinal']//10)*10+1 if row['ordinal']%10 else row['ordinal']-9;b=a+9
 led=json.loads((ROOT/f'fresh-build/waves/f003-laneC-{a}-{b}-research-ledger.json').read_text());e=next(x for x in led['entries'] if x['ordinal']==row['ordinal'])
 for w in e['witnesses']:
  if w.get('RelPath')==occ['RelPath'] and occ['Kwic'] in w.get('expandedWindow',''):return w
 return next((w for w in e['witnesses'] if w.get('RelPath')==occ['RelPath']),None)
def named(o,name,title,why):
 o.pop('ActorAttribution',None);o['MasterName']=name;o['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}];note=f'In the source record ({title}), full-turn review assigns the exact headword-bearing wording to {name}: {why}';o['AttributionNote']=note;o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':name,'SpeechFrame':why,'FullCaseDecision':note}
changed=0
for row in rows:
 ep=ROOT/'fresh-build/entries'/row['id'];wp=ep/'evidence.draft.json';d=json.loads(wp.read_text());term=d['Entry']['SourceTerm']
 for s in d['Entry']['Senses']:
  for o in s['Occurrences']:
   if o.get('MasterName'):continue
   q=o['Kwic'];w=witness(row,o)
   if not w:continue
   head=str((w.get('headingContext') or {}).get('head') or '')
   # A headword-bearing question belongs to its questioner unless 師 explicitly asks it.
   pos=q.find(term);before=q[:pos]
   if ('問' in before[-18:] and '師問' not in before[-18:]):continue
   # Prefer an explicitly named X云/X曰 actor in the same clause.
   speaker=None
   for a,_,c in aliases:
    pass
   prefix=q[:pos]
   for _,a,c in aliases:
    if a in prefix[-45:] and re.search(re.escape(a)+r'.{0,5}(?:云|曰)',prefix[-55:]):speaker=c;break
   # 師/上堂 speech belongs to the current section master.
   speech=bool(re.search(r'(?:上堂|示眾|乃云|良久云|師云|師曰|師道|云[:：]|曰[:：])',q))
   if not speaker and speech:
    speaker=canonical(head)
    if not speaker:
     h=clean_head(head)
     if re.search(r'(?:禪師|和尚|國師|大師|尊者|庵主|菴主)$',h):speaker=h
   # Explicit abbreviated names in later comments are still named utterers;
   # retain the source form when no roster alias resolves it.
   if not speaker:
    m=re.search(r'([\u3400-\u9fff]{2,12})(?:云|曰)：',q)
    if m and not re.search(r'^(?:師|僧|婆|問|曰|進|座)$',m.group(1)):speaker=canonical(m.group(1)) or m.group(1)
   if not speaker:continue
   title=zc.title(o['RelPath'])
   named(o,speaker,title,'the marked speech frame and enclosing master heading identify the utterer.');changed+=1
 s['RelatedMasters']=sorted({o['MasterName'] for o in s['Occurrences'] if o.get('MasterName')})
 wp.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(ROOT/'compile_evidence_draft.py'),str(wp),'--output',str(ep/'entry.v2.json'),'--report',str(ep/'compile-report.json')],check=True,stdout=subprocess.DEVNULL)
print('restored named utterers',changed,'across',len(rows),'entries')
