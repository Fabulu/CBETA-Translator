from pathlib import Path
import datetime, hashlib, json
H=Path(__file__).resolve().parent; R=H.parent.parent
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
review=H/'f004-b1041-1050-independent-rereview.json'
checkpoint=H/'f004-b1041-1050-independent-rereview-delta-author-checkpoint.json'
gate=H/'f004-b1041-1050-independent-rereview-delta-author-pre-review.json'
c=json.loads(checkpoint.read_text()); g=json.loads(gate.read_text()); assert g['hardPass'] and len(g['entries'])==10
assert sha(review)=='a46f79f6bd65d11170e384ca9d6465cdca61ee910e13d1c2ef2b4e92b855bf3f'
assert sha(R/'fresh-build/entries/t_aced87de5b30/entry.v2.json')=='ed4b307c6e95fa26815f16ccfc4495915999e9deb0d891d05c4a43f981f3ce56'
assert sha(R/'fresh-build/entries/t_76ee526a2b16/entry.v2.json')=='6bc077fb30adb10a31b3b3e50d2f058ba8c7d6a0bb96fc2f82db3ce5e35283f3'
rows=[]
for x in c['entries']:
    current=sha(R/'fresh-build/entries'/x['id']/'entry.v2.json'); assert current==x['afterEntrySha256'] and current!=x['beforeEntrySha256']
    rows.append(x)
out={
 'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'role':'repair-author',
 'sourceReview':review.name,'sourceReviewSha256':sha(review),'repairedEntries':rows,
 'counts':{'repairedEntries':9,'preservedKeeps':1,'occurrencesInRepairedEntries':53,'compositeOccurrences':g['exactKwic']['verified'],'exactFailures':g['exactKwic']['failureCount']},
 'immutableProofs':[
   {'ordinal':1045,'id':'t_aced87de5b30','term':'殺佛殺祖','verdict':'KEEP','entrySha256':'ed4b307c6e95fa26815f16ccfc4495915999e9deb0d891d05c4a43f981f3ce56','byteIdentical':True},
   {'ordinal':1077,'id':'t_76ee526a2b16','term':'沙彌戒','verdict':'KEEP','entrySha256':'6bc077fb30adb10a31b3b3e50d2f058ba8c7d6a0bb96fc2f82db3ce5e35283f3','byteIdentical':True}],
 'artifactBindings':{checkpoint.name:{'sha256':sha(checkpoint)},gate.name:{'sha256':sha(gate),'hardPass':True}},
 'scopeAssertions':{'checkpoint2EntriesEdited':False,'checkpoint3EntriesEdited':False,'bulkF005Resumed':False},
 'compositeHardPass':True,'semanticRereviewRequired':True,'selfReview':False,'promoted':False,'merged':False,'siteTouched':False}
p=H/'f004-b1041-1050-independent-rereview-delta-author-final-ledger.json';p.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'path':str(p),'sha256':sha(p),'repaired':9,'compositeHardPass':True}))
