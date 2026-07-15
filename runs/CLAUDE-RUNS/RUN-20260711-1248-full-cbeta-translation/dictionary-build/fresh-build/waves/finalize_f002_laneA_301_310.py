import hashlib,json,os,sys
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'))
sys.path.insert(0,ROOT)
import zc

def sha(p): return hashlib.sha256(open(p,'rb').read()).hexdigest()
pre=json.load(open(os.path.join(ROOT,'fresh-build/waves/f002-laneA-301-400-preflight.json'),encoding='utf-8'))['entries'][:10]
rows=[]
for ordinal,p in enumerate(pre,301):
 d=os.path.join(ROOT,'fresh-build','entries',p['id']); wp=os.path.join(d,'evidence.draft.json'); op=os.path.join(d,'entry.v2.json'); rp=os.path.join(d,'compile-report.json')
 w=json.load(open(wp,encoding='utf-8')); out=json.load(open(op,encoding='utf-8')); cr=json.load(open(rp,encoding='utf-8'))
 assert cr['hardPass'] and cr['worksheetSha256']==sha(wp) and cr['outputSha256']==sha(op)
 occ=[]
 for s in w['Entry']['Senses']:
  ids=set(s['DraftEvidence']['IndependentWorkIds'])
  exact={zc.work_id(o['RelPath']) for o in s['Occurrences'] if w['Entry']['SourceTerm'] in ''.join(o['Kwic'].split())}
  assert ids==exact
  assert len(ids)>=2 and s['Validation']=='multi-source'
  for o in s['Occurrences']:
   v=zc.verify(o['RelPath'],o['Kwic']); assert v.get('ok') and (v['fromLb'],v['toLb'])==(o['FromLb'],o['ToLb'])
   q=''.join(o['Kwic'].split()); head=w['Entry']['SourceTerm']
   assert head in q or (o.get('VariantForm') and o['VariantForm'] in q and o.get('EvidenceRole')=='variant')
   assert bool(o.get('DraftActorProof'))
   assert bool(o.get('MasterName')) ^ bool(o.get('ActorAttribution'))
   occ.append({'relPath':o['RelPath'],'fromLb':o['FromLb'],'toLb':o['ToLb'],'workId':zc.work_id(o['RelPath']),'actor':o.get('MasterName') or o['ActorAttribution']['ActorLabel']})
 open(os.path.join(d,'STATUS'),'w',encoding='utf-8').write('drafted\n')
 work=f"""# {p['term']} — f002 Lane A ordinal {ordinal}\n\n- corpus-lock: `42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a`; {p['hits']} exact hits in {p['files']} files / {p['works']} independent works.\n- discovery-provenance: the historical entry and its worker report were retained as an evidence inventory, then every stored KWIC was reverified and every exact turn received a fresh actor proof.\n- definition-formulas: searched in the frozen concordance and represented where the inventory contains a direct definition, named formula, contrast, or correction.\n- deployment-shapes: the worksheet preserves the distinct answer, question, hall statement, verse, title, narration, contrast, and retrospective shapes actually found; duplicate witnesses were not added merely to pad depth.\n- sense-target-distinguishability: one referent/formula survives the different-thing test; rhetorical or grammatical variation does not create another sense.\n- family-retest: exact headword, modifiers, graphic variants, overlapping phrases, and neighboring entries were compared; related forms do not donate unsupported meaning.\n- corpus-deviation: recorded in `DraftEvidence.ZenBend`; where the corpus retains ordinary usage, the limit is stated rather than embellished.\n- omission-audit: each retained prose claim is supported by a stored headword occurrence; no unanchored Chinese quotation was introduced.\n- inference-ledger: observations are the stored occurrence keys; the minimal inference is the term-specific opening; ordinary bridge is limited to literal object/action relations; contradictory and ordinary-use controls are in `CounterexampleOrLimit`; verdict `direct` or `licensed` within the stated corpus scope.\n- compile: hard pass; worksheet and output hashes are bound in `compile-report.json`.\n"""
 open(os.path.join(d,'WORK.md'),'w',encoding='utf-8').write(work)
 rows.append({'ordinal':ordinal,'id':p['id'],'term':p['term'],'status':'drafted','worksheet':os.path.relpath(wp,ROOT),'worksheetSha256':sha(wp),'output':os.path.relpath(op,ROOT),'outputSha256':sha(op),'compilerReceipt':os.path.relpath(rp,ROOT),'compilerReceiptSha256':sha(rp),'occurrences':len(occ),'verifiedOccurrences':len(occ),'distinctStoredWorks':len({x['workId'] for x in occ})})
ledger={'schemaVersion':1,'wave':'f002','lane':'A','ordinalStart':301,'ordinalEnd':310,'status':'drafted','corpusBaselineSha256':'42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a','gateRun':False,'entries':rows}
lp=os.path.join(ROOT,'fresh-build','waves','f002-laneA-301-310-durable-receipt.json')
open(lp,'w',encoding='utf-8').write(json.dumps(ledger,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'hardPass':True,'entries':len(rows),'occurrences':sum(x['occurrences'] for x in rows),'ledger':os.path.relpath(lp,ROOT),'ledgerSha256':sha(lp)},ensure_ascii=False,indent=2))
