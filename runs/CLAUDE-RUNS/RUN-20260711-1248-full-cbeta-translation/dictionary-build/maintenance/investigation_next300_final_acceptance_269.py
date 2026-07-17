#!/usr/bin/env python3
"""Accept the 269-entry investigation wave without redundant semantic rereads.

The evidence chain is deliberately asymmetric:
  * semantic admission is ID-bound to independently read full-case packets;
  * later semantic revisions require targeted current-byte rechecks;
  * one final strict cohort gate seals every current entry byte mechanically.
"""
from __future__ import annotations
import argparse,datetime,hashlib,json,pathlib,re,subprocess,sys
from functools import lru_cache

B=pathlib.Path(__file__).resolve().parents[1]
REPO=B.parents[3]
MANIFESTS={x:B/f'maintenance/investigation-next300-construction-lane-{x.lower()}.json' for x in 'ABC'}
EXCLUDE={('B',11,'天聖廣燈錄'),('B',43,'嘉泰普燈錄')}
SEMANTIC_INDEX=B/'maintenance/investigation-next300-final-semantic-review-index.json'
CORPUS=REPO/'Assets/Data/zen-corpus.json'
ROSTER=REPO/'Assets/Data/lineage-masters.json'
PROTECTED_ROSTER_SHA='33c008e98468ecab8b89bbb6dcd6008fba631bee01f4909091c7d2887dce1ded'
FORBIDDEN=re.compile(r'\b(?:Buddhism|meditation|Bodhiteaching)\b',re.I)
EDITORIAL_PUNCTUATION="、。！，：；？（）《》〈〉「」『』【】—…·・"

@lru_cache(maxsize=None)
def sha(p:pathlib.Path)->str:return hashlib.sha256(p.read_bytes()).hexdigest()
@lru_cache(maxsize=None)
def load(p):return json.loads(pathlib.Path(p).read_text(encoding='utf-8-sig'))
def canonical_search_form(value):return ''.join(c for c in str(value or '') if not c.isspace() and c not in EDITORIAL_PUNCTUATION)
def pointer(doc,p):
    cur=doc
    for part in p.strip('/').split('/') if p.strip('/') else []:
        part=part.replace('~1','/').replace('~0','~')
        cur=cur[int(part)] if isinstance(cur,list) else cur[part]
    return cur
def relpath(value):
    p=pathlib.Path(value)
    if p.is_absolute():return p
    return B/p if str(p).startswith('maintenance/') else B/'maintenance'/p
def expected():
    out=[]
    for lane,path in MANIFESTS.items():
        d=load(path)
        for pos,row in enumerate(d['rows'],1):
            term=row.get('headword') or row.get('term') or row.get('sourceTerm')
            if (lane,pos,term) in EXCLUDE:continue
            out.append({'lane':lane,'position':pos,'id':row['id'],'term':term,'manifest':str(path.relative_to(B))})
    ids=[x['id'] for x in out]
    if len(out)!=269 or len(set(ids))!=269:raise SystemExit(f'expected exactly 269 unique IDs, got {len(out)}/{len(set(ids))}')
    return out
def receipt_ref(ledger,key):
    value=ledger[key]
    if isinstance(value,dict):return relpath(value['path']),value['sha256']
    return relpath(value),ledger[key+'Sha256']
def validate_semantic_chain(rows,current_hashes,index_path):
    index=load(index_path)
    if index.get('schemaVersion')!='investigation-next300-final-semantic-review-index.v1' or index.get('entryCount')!=269 or index.get('allAuthoritativeIdsMapped') is not True:
        raise SystemExit('semantic review index is incomplete or has the wrong contract')
    indexed={r['id']:r for r in index['rows']}
    if len(indexed)!=269 or set(indexed)!={r['id'] for r in rows}:raise SystemExit('semantic review index does not cover exactly the authoritative 269 IDs')
    bindings={}
    for expected_row in rows:
        ident=expected_row['id'];row=indexed[ident]
        if (row.get('lane'),row.get('position'),row.get('term'))!=(expected_row['lane'],expected_row['position'],expected_row['term']):raise SystemExit(f'semantic index identity mismatch: {ident}')
        manifest=row['manifest'];manifest_path=B/manifest['path']
        if sha(manifest_path)!=manifest['sha256']:raise SystemExit(f'manifest drift in semantic index: {ident}')
        manifest_row=pointer(load(manifest_path),manifest['rowPointer'])
        if manifest_row.get('id')!=ident:raise SystemExit(f'manifest pointer mismatch: {ident}')
        candidate=row['candidateReview'];review_path=B/candidate['path']
        if sha(review_path)!=candidate['sha256']:raise SystemExit(f'independent review drift: {ident}')
        review_doc=load(review_path)
        if len(review_doc.get('rows',[]))!=100:raise SystemExit(f'independent review is not a complete 100-row lane: {ident}')
        review=pointer(review_doc,candidate['rowPointer'])
        # Corrected/merged headwords may intentionally have a different discovery ID.
        # The immutable 269-row index binds that candidate pointer to the constructed ID.
        if candidate.get('acceptedSemanticDisposition') not in {'KEEP','PROVISIONAL'}:raise SystemExit(f'non-admitting semantic disposition: {ident}')
        reason=review.get(candidate['reasonField'])
        evidence=review.get('evidence') or review.get('reviewedEvidence') or []
        if not reason or not evidence or candidate.get('fullCasesRead',0)<len(evidence) or any(e.get('exactVerifyOk') is not True for e in evidence):raise SystemExit(f'incomplete independent full-case review: {ident}')
        for key in ('packet','bundle'):
            spec=candidate[key];path=B/spec['path']
            if sha(path)!=spec['sha256']:raise SystemExit(f'{key} drift in semantic review chain: {ident}')
        post=[]
        for spec in row.get('postBuildReviews',[]):
            path=B/spec['path']
            if sha(path)!=spec['sha256']:raise SystemExit(f'post-build semantic receipt drift: {ident}')
            recheck=pointer(load(path),spec['rowPointer'])
            if recheck.get('id')!=ident or recheck.get('reviewedCurrentEntrySha256')!=current_hashes[ident]:raise SystemExit(f'stale post-build semantic recheck: {ident}')
            if str(recheck.get('disposition')).upper() not in {'PASS','ACCEPT','APPROVED'} or recheck.get('independentQualified') is not True:raise SystemExit(f'non-passing post-build semantic recheck: {ident}')
            post.append({'path':spec['path'],'sha256':spec['sha256'],'rowPointer':spec['rowPointer']})
        bindings[ident]={'candidateReview':{'path':candidate['path'],'sha256':candidate['sha256'],'rowPointer':candidate['rowPointer']},'postBuildReviews':post}
    return bindings,sha(index_path)
def main():
    ap=argparse.ArgumentParser();ap.add_argument('--semantic-review-index',type=pathlib.Path,default=SEMANTIC_INDEX);ap.add_argument('--output',type=pathlib.Path)
    a=ap.parse_args(); rows=expected()
    if not a.output:ap.error('--output required')
    a.output=a.output.resolve()
    corpus_sha=sha(CORPUS)
    if corpus_sha!=load(B/'fresh-build/state.json')['corpusBaselineSha256']:raise SystemExit('frozen corpus hash mismatch')
    if sha(ROSTER)!=PROTECTED_ROSTER_SHA:raise SystemExit('protected lineage roster changed')
    current_hashes={};expected_exact=0
    for x in rows:
        p=B/f"fresh-build/entries/{x['id']}/entry.v2.json";entry=load(p);current_hashes[x['id']]=sha(p)
        if entry.get('Id')!=x['id'] or canonical_search_form(entry.get('SourceTerm'))!=canonical_search_form(x['term']):raise SystemExit(f"entry identity mismatch {x['id']}")
        if entry.get('CorpusBaselineSha256')!=corpus_sha:raise SystemExit(f"entry corpus mismatch {x['id']}")
        if FORBIDDEN.search(json.dumps(entry,ensure_ascii=False)):raise SystemExit(f"forbidden vocabulary {x['id']}")
        expected_exact+=sum(len(s.get('Occurrences',[]))+len(s.get('ClaimAnchors',[])) for s in entry.get('Senses',[]))
    semantic_bindings,semantic_index_sha=validate_semantic_chain(rows,current_hashes,a.semantic_review_index)
    gate=a.output.with_name(a.output.stem+'-cohort-gate.json')
    cmd=[sys.executable,str(B/'run_cohort_gate.py'),*[x['id'] for x in rows],'--output',str(gate),'--pending-roster',str((B/'fresh-build/pending-roster.json').resolve())]
    cp=subprocess.run(cmd,cwd=REPO);report=load(gate)
    exact=report.get('exactKwic') or {}
    if cp.returncode or not report.get('hardPass') or exact.get('verified')!=expected_exact:raise SystemExit('269-entry final cohort gate failed')
    gate_rows={r['id']:r for r in report.get('entries',[])}
    if set(gate_rows)!=set(current_hashes):raise SystemExit('final cohort gate did not seal exactly 269 IDs')
    for ident,current in current_hashes.items():
        if gate_rows[ident].get('sha256')!=current:raise SystemExit(f'final cohort gate byte mismatch: {ident}')
        if sha(B/f"fresh-build/entries/{ident}/entry.v2.json")!=current:raise SystemExit(f'entry changed while final cohort gate was running: {ident}')
    if sha(ROSTER)!=PROTECTED_ROSTER_SHA:raise SystemExit('lineage roster changed during acceptance')
    plan=[]
    for x in rows:plan.append({**x,'source':f"fresh-build/entries/{x['id']}/entry.v2.json",'entrySha256':current_hashes[x['id']],
      'semanticEvidence':semantic_bindings[x['id']]})
    payload={'schemaVersion':'investigation-next300-install-plan-269.v2','generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),
      'hardPass':True,'entryCount':269,'excluded':[{'lane':a,'position':b,'term':c} for a,b,c in sorted(EXCLUDE)],
      'corpusSha256':corpus_sha,'protectedLineageRosterSha256':PROTECTED_ROSTER_SHA,
      'manifestSha256':{k:sha(v) for k,v in MANIFESTS.items()},'semanticReviewIndex':{'path':str(a.semantic_review_index.relative_to(B)),'sha256':semantic_index_sha},
      'finalCohortGate':{'path':str(gate.relative_to(B)),'sha256':sha(gate),'entries':269,'exactKwic':exact,'hardPass':True},'rows':plan}
    a.output.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n');print(a.output)
if __name__=='__main__':main()
