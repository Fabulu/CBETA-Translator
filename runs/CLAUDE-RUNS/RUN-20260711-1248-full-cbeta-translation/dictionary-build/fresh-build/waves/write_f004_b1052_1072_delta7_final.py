from pathlib import Path
import datetime,hashlib,json
H=Path(__file__).resolve().parent;R=H.parent.parent;sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
rp=H/'f004-b1052-1072-independent-rereview.json';cp=H/'f004-b1052-1072-independent-rereview-delta7-author-checkpoint.json';gp=H/'f004-b1052-1072-independent-rereview-delta7-author-pre-review.json';c=json.loads(cp.read_text());g=json.loads(gp.read_text());assert g['hardPass'] and g['exactKwic']['verified']==58
for x in c['entries']:assert sha(R/'fresh-build/entries'/x['id']/'entry.v2.json')==x['afterEntrySha256']
for x in c['immutableKeeps']:assert sha(R/'fresh-build/entries'/x['id']/'entry.v2.json')==x['entrySha256']
o={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'role':'repair-author','sourceReview':rp.name,'sourceReviewSha256':sha(rp),'entries':c['entries'],'counts':{'repaired':7,'preservedKeeps':3,'compositeOccurrences':58,'exactFailures':0},'immutableKeeps':c['immutableKeeps'],'artifactBindings':{cp.name:{'sha256':sha(cp)},gp.name:{'sha256':sha(gp),'hardPass':True}},'compositeHardPass':True,'semanticRereviewRequired':True,'selfReview':False,'promoted':False,'merged':False,'siteTouched':False}
p=H/'f004-b1052-1072-independent-rereview-delta7-author-final-ledger.json';p.write_text(json.dumps(o,ensure_ascii=False,indent=2)+'\n');print(p,sha(p))
