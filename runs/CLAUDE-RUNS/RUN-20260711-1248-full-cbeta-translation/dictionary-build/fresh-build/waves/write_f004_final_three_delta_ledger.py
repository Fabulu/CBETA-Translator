from pathlib import Path
import datetime,hashlib,json
H=Path(__file__).resolve().parent;R=H.parent.parent;sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
cp=H/'f004-final-three-current-revise-author-checkpoint.json';gp=H/'f004-final-three-current-revise-author-pre-review.json';c=json.loads(cp.read_text());g=json.loads(gp.read_text());assert g['hardPass'] and g['exactKwic']['verified']==20 and len(g['entries'])==3
for x in c['entries']:assert sha(R/'fresh-build/entries'/x['id']/'entry.v2.json')==x['afterEntrySha256']
o={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'role':'repair-author','scope':'exactly three current independent-review REVISE entries','entries':c['entries'],'counts':{'repaired':3,'occurrences':20,'exactFailures':0},'artifactBindings':{cp.name:{'sha256':sha(cp)},gp.name:{'sha256':sha(gp),'hardPass':True}},'preservation':{'editedEntryIds':[x['id'] for x in c['entries']],'allOtherKeepEntriesEdited':False,'scopeEnforcedByRepairScript':True},'compositeHardPass':True,'semanticRereviewRequired':True,'selfReview':False,'promoted':False,'merged':False,'siteTouched':False}
p=H/'f004-final-three-current-revise-author-final-delta-ledger.json';p.write_text(json.dumps(o,ensure_ascii=False,indent=2)+'\n');print(p,sha(p))
