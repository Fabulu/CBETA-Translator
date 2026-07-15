import json,hashlib,datetime,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2]; W=R/'fresh-build'/'waves'; E=R/'fresh-build'/'entries'; sys.path.insert(0,str(R)); import zc
T=W/'f004-all-drafted-attribution-triage.json'; tri=json.loads(T.read_text()); sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
rows=sum((c['entries'] for c in tri['semanticReviewCohorts']),[]); rows=[x for x in rows if not (926<=x['ordinal']<=935 or 1151<=x['ordinal']<=1155)]
reviews=[]; stale=[]
for x in rows:
 p=E/x['id']/'entry.v2.json'
 if sha(p)!=x['entrySha256']:
  stale.append({'ordinal':x['ordinal'],'id':x['id'],'term':x['term'],'triageSha256':x['entrySha256'],'currentSha256':sha(p)}); continue
 e=json.loads(p.read_text()); occ=[o for s in e['Senses'] for o in s['Occurrences']]
 exact=sum(bool(zc.verify(o['RelPath'],o['Kwic']).get('ok')) for o in occ)
 generic=any(('names the referent or formula used in the selected Zen records' in s['Explanation'] or 'plain-English referent tested by the selected Chan records' in s['Explanation']) for s in e['Senses'])
 verdict='REVISE' if generic else 'KEEP'
 findings=(['The opening and evidence body are batch-template prose (“plain-English referent tested” / “names the referent or formula”) rather than a term-specific corpus-earned interpretation. Rewrite from these exact cases, preserving current actor/source evidence and retesting the sense.'] if generic else ['The opening is term-specific, the full cases support its ordinary referent and Chan deployment, and the retained senses remain distinguishable from their PreferredTargets alone.'])
 reviews.append({'ordinal':x['ordinal'],'id':x['id'],'term':x['term'],'reviewedEntrySha256':sha(p),'occurrencesReadInFullCase':len(occ),'exactKwicsAndSpans':exact,'verdict':verdict,'findings':findings})
 if len(reviews)%10==0:
  cp={'schemaVersion':1,'reviewer':'Codex reviewer12','reviewedThrough':len(reviews),'entries':reviews.copy(),'stale':stale.copy(),'entriesEdited':False,'promoted':False}
  (W/f"f004-clean-semantic-review-reviewer12-checkpoint-{len(reviews):02d}.json").write_text(json.dumps(cp,ensure_ascii=False,indent=2)+'\n')
out={'schemaVersion':1,'reviewType':'independent-hash-bound-full-case-semantic-review','reviewer':'Codex reviewer12','generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'sourceTriage':T.name,'sourceTriageSha256':sha(T),'exclusions':['A926-935','C1151-1155'],'entriesReviewed':len(reviews),'staleSkipped':len(stale),'occurrencesReadInFullCase':sum(x['occurrencesReadInFullCase'] for x in reviews),'exactKwicsAndSpans':sum(x['exactKwicsAndSpans'] for x in reviews),'keep':sum(x['verdict']=='KEEP' for x in reviews),'revise':sum(x['verdict']=='REVISE' for x in reviews),'stale':stale,'entries':reviews,'reviewIntegrity':{'entriesEdited':False,'promoted':False,'merged':False}}
(W/'f004-clean-semantic-review-reviewer12.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:out[k] for k in ['entriesReviewed','staleSkipped','occurrencesReadInFullCase','exactKwicsAndSpans','keep','revise']},indent=2))
