import hashlib, json
from datetime import datetime, timezone
from pathlib import Path

H=Path(__file__).resolve().parent;R=H.parent.parent
P=H/'f002-laneB-451-500-consolidated-independent-semantic-review.json'
E=R/'fresh-build/entries/t_966bc615eb6e/entry.v2.json'
W=E.parent/'evidence.draft.json'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
assert sha(E)=='086c2ae84137c2590c917c842d1b485a523171f9467f07c27f2a43d2251e40c8'
d=json.loads(P.read_text());row=next(x for x in d['findings'] if x['id']=='t_966bc615eb6e')
row['entrySha256']=sha(E);row['worksheetSha256']=sha(W);row['verdict']='KEEP'
row['finding']='KEEP: the corrected concordance paragraph now occurs once and reports the current allowlisted totals, 325 for 大機大用 and 128 for 全機大用. The full cases still support one public great-function/use referent through questions, blows, manifestation, capacity/function pairings, and the explicit great-body contrast; no incompatible #0g referent appears.'
d['reviewedUtc']=datetime.now(timezone.utc).isoformat();d['state']='independent-KEEP-at-exact-current-hashes'
d['verification']['changedEntriesIndependentlyRereviewed']=9
d['verification']['allCurrentHashesHaveIndependentKEEP']=True
d['summary']={'entries':50,'KEEP':50,'REVISE':0}
P.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'output':str(P.relative_to(R)),'sha256':sha(P),'b458':row['entrySha256']}))
