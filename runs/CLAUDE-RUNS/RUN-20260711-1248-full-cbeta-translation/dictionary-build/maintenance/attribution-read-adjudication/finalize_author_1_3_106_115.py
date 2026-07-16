import hashlib,json,re,sys
from pathlib import Path
B=Path(__file__).resolve().parents[2]; sys.path.insert(0,str(B)); import zc
IDS=['t_ff3b9302050a','t_0051fc72360c','t_041f65670cd4','t_04efe13911ae','t_073dcbf657a3','t_09ed57d56bcf','t_0a686fa27769','t_0c53a5a2243b','t_11a4ff234f5a','t_135a001a5b0e']
def ep(i): return B/'fresh-build'/'entries'/i/'entry.v2.json'
def recut(o,q):
 v=zc.verify(o['RelPath'],q)
 if not v['ok']: raise ValueError((o['RelPath'],q,v))
 o['Kwic']=q; o['FromLb']=v['fromLb']; o['ToLb']=v['toLb']

# Remove false-positive mixed-turn/action frames without changing the evidence.
d=json.loads(ep(IDS[0]).read_text()); recut(d['Senses'][0]['Occurrences'][2],'道既人弘，逢場作戲。'); ep(IDS[0]).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
d=json.loads(ep(IDS[2]).read_text()); recut(d['Senses'][0]['Occurrences'][5],'洛浦因僧問供養百千諸佛不如供養一箇無心道人未審百千諸佛有何過無心道人有何德'); ep(IDS[2]).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
d=json.loads(ep(IDS[4]).read_text()); recut(d['Senses'][0]['Occurrences'][1],'僧云如何是無寒暑處'); ep(IDS[4]).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
d=json.loads(ep(IDS[6]).read_text()); recut(d['Senses'][0]['Occurrences'][4],'僧舉莊上喫油餈話求著語'); ep(IDS[6]).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
d=json.loads(ep(IDS[6]).read_text()); d['Senses'][0]['Occurrences'][1]['AttributionNote']=d['Senses'][0]['Occurrences'][1]['AttributionNote'].replace('Dahui Zonggao Record','Yuanwu Keqin Record'); ep(IDS[6]).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
d=json.loads(ep(IDS[7]).read_text()); recut(d['Senses'][0]['Occurrences'][4],'本淨知客'); d['Senses'][0]['Explanation']=d['Senses'][0]['Explanation'].replace('未舉先知客','the separate verb–object phrase in Miyun Yuanwu’s saying'); ep(IDS[7]).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
d=json.loads(ep(IDS[9]).read_text()); recut(d['Senses'][0]['Occurrences'][5],'青青翠竹，盡是真如'); ep(IDS[9]).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')

# The parallel text identifies the anonymous anthology speakers.
d=json.loads(ep(IDS[5]).read_text()); o=d['Senses'][0]['Occurrences'][5]; o['MasterName']='Yingan Tanhua'; o.pop('ActorAttribution',None); o['ContextMasters']=[{'MasterName':'Yingan Tanhua','Roles':['utterer']}]; o['AttributionNote']='Source text (列祖提綱錄). Exact speaker: Yingan Tanhua; the same address is preserved in Yingan Tanhua’s own record (應菴曇華禪師語錄).'; ep(IDS[5]).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
d=json.loads(ep(IDS[9]).read_text()); o=d['Senses'][0]['Occurrences'][4]; o['MasterName']='Yuejiang Yin'; o.pop('ActorAttribution',None); o['ContextMasters']=[{'MasterName':'Yuejiang Yin','Roles':['utterer']}]; o['AttributionNote']='Source text (列祖提綱錄). Exact speaker: Yuejiang Yin; the passage belongs to the explicitly headed summer-before-incense formal discourse of Yuejiang Yin.'; ep(IDS[9]).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')

# Normalize attribution notes to the exact corpus title and exact actor label. This is presentation-only.
for i in IDS:
 d=json.loads(ep(i).read_text())
 for s in d['Senses']:
  for o in s.get('Occurrences',[]):
   title=zc.title(o['RelPath']) or o['RelPath']; a=o.get('ActorAttribution'); master=o.get('MasterName')
   if a:
    if a.get('ActorRole')=='author': a['ActorRole']='compiler'
    if a.get('Status')=='identified-non-master' and a.get('ActorLabel')=='the Kangxi Emperor': a['ActorLabel']='Kangxi Emperor'
    if a.get('Status')=='reviewed-unnamed' and 'unnamed' not in a.get('ActorLabel','').lower(): a['ActorLabel']='the unnamed '+a.get('ActorLabel','').removeprefix('the ')
    actor=a.get('ActorLabel')
   else: actor=master
   oldnote=re.sub(r'^(?:Source text \([^)]*\)\. Exact actor: [^.]+\. )+', '', o.get('AttributionNote',''))
   o['AttributionNote']=f"Source text ({title}). Exact actor: {actor}. "+oldnote
 ep(i).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
print('finalized',len(IDS))
# Keep the crash ledger's final hashes synchronized after presentation/recut normalization.
lp=Path(__file__).with_name('cohorts-1-3-106-115-full-read-repair-ledger.json')
if lp.exists():
 ledger=json.loads(lp.read_text()); by={x['Id']:x for x in ledger['entries']}
 for i in IDS: by[i]['newSha256']=hashlib.sha256(ep(i).read_bytes()).hexdigest()
 ledger['finalized']=True; lp.write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+'\n')
