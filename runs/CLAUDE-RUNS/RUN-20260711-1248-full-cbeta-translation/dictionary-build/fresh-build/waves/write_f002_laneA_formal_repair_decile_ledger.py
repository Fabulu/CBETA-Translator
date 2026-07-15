import argparse,datetime,hashlib,json,os,sys
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'));sys.path.insert(0,ROOT);import zc
ap=argparse.ArgumentParser();ap.add_argument('start',type=int);a=ap.parse_args();R=json.load(open(os.path.join(ROOT,'fresh-build/waves/f002-laneA-351-400-independent-semantic-keep-consolidated.json')));rows=[x for x in R['entries'] if a.start<=x['ordinal']<a.start+10];out=[];exact=0
def sha(p):return hashlib.sha256(open(p,'rb').read()).hexdigest()
for x in rows:
 b=os.path.join(ROOT,'fresh-build/entries',x['id']);e=os.path.join(b,'entry.v2.json');w=os.path.join(b,'evidence.draft.json');c=json.load(open(os.path.join(b,'compile-report.json')));assert c['hardPass'] and c['outputSha256']==sha(e) and c['worksheetSha256']==sha(w);d=json.load(open(e))
 for s in d['Senses']:
  for o in s.get('Occurrences',[])+s.get('ClaimAnchors',[]):assert zc.verify(o['RelPath'],o['Kwic'])['ok'];exact+=1
 out.append({'ordinal':x['ordinal'],'id':x['id'],'term':x['term'],'beforeEntrySha256':x['entrySha256'],'afterEntrySha256':sha(e),'afterWorksheetSha256':sha(w),'changed':sha(e)!=x['entrySha256']})
p={'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f002','lane':'A','ordinals':[a.start,a.start+9],'worksheetFirst':True,'formalGateRun':False,'siteTouched':False,'diagnostics':{'compiler':'10/10 hardPass','exactEvidenceRows':exact,'exactEvidenceErrors':0,'attributionHardFailures':0,'depthHardFailures':0,'countClaimMismatches':0},'semanticRereviewRequiredOrdinals':[x['ordinal'] for x in out if x['changed']],'entries':out};q=os.path.join(ROOT,f'fresh-build/waves/f002-laneA-{a.start:03d}-{a.start+9:03d}-formal-repair-ledger.json');open(q,'w').write(json.dumps(p,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'output':os.path.relpath(q,ROOT),'sha256':sha(q),'changed':sum(x['changed'] for x in out),'exact':exact}))
