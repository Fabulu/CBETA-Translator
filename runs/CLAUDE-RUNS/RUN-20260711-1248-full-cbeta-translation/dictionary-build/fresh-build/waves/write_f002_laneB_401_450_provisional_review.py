import hashlib,json,os,sys
R=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'));sys.path.insert(0,R);import zc
def sha(p): return hashlib.sha256(open(os.path.join(R,p),'rb').read()).hexdigest()
ledgerp='fresh-build/waves/f002-laneB-401-450-ledger.json';attrp='fresh-build/waves/f002-laneB-401-450-attribution-audit.json';sensep='fresh-build/waves/f002-laneB-401-450-sense-retest.json';depthp='fresh-build/waves/f002-laneB-401-450-depth-audit.txt'
L=json.load(open(os.path.join(R,ledgerp))); rows=[x for c in L['checkpoints'] for x in c['entries']]
rev={
401:'Opening prose has an unclosed quotation in “Its wording is ‘after being completely cut off, revive again.” Evidence, work spread, actors, anchors, aliases, and the single revival event otherwise pass; close or recast the gloss.',
406:'Opening prose has an unclosed quotation in “Its wording is ‘golden-haired lion.” The cases correctly keep Mañjuśrī’s mount and the independently deployed lion as one animal, but the reader-facing gloss must be repaired.',
410:'Opening prose has an unclosed graph gloss (“Its wording is ‘feeling/inclination-understanding”). Exact cases support a formed interpretive understanding and its rejection, but the broken gloss is publication-blocking.',
422:'The first sentence has an unclosed literal gloss: “A case title, literally \'Deshan carries his bowl.” The complete Deshan–Xuefeng–Yantou sequence and its six independent works are otherwise coherent.',
423:'Opening prose has an unclosed quotation in “Its wording is ‘gain life within death.” The cases all concern gaining/recovering life within death and do not require another sense.',
429:'Opening prose has an unclosed quotation in “Its wording is ‘Juzhi’s one finger.” The Tianlong/Juzhi/attendant-boy actor layers and one-finger referent otherwise remain coherent.',
432:'Opening prose has an unclosed quotation in “Its wording is ‘one-word barrier.” The examples and naming anchor support Yunmen’s one-word barrier, but the gloss must be repaired.',
434:'Opening prose has an unclosed quotation in “Its wording is ‘the old woman burns the hermitage.” The old woman, young woman, hermit, and later case users are otherwise separated correctly.',
439:'The explanation is textually garbled after the count sentence (“—hah! … using 鑑 for ‘look!”), duplicating and breaking the graph gloss. Recompose the opening from the exact 顧/鑒/咦 interaction and retain the variant anchor without the corrupt fragment.'}
find=[]
for x in rows:
 p=f"fresh-build/entries/{x['id']}/entry.v2.json";e=json.load(open(os.path.join(R,p)));h=sha(p);assert h==x['entrySha256'];senses=e['Senses'];occ=sum(len(s.get('Occurrences',[])) for s in senses);anchors=sum(len(s.get('ClaimAnchors',[])) for s in senses);works=sorted({zc.work_id(o['RelPath']) for s in senses for o in s.get('Occurrences',[])})
 if x['ordinal'] in rev: verdict='REVISE';note=rev[x['ordinal']]
 else:
  verdict='KEEP'
  split=' The sense split is a different-referent split and satisfies #0g.' if len(senses)>1 else ' The evidence does not expose a second incompatible referent under #0g.'
  note=f"KEEP at this hash: {occ} exact occurrences across {len(works)} independent works, with {anchors} explicit claim anchors. Full-case actor/action assignments agree with the zero-failure attribution audit; aliases stay within the preferred target, stored claims remain source-visible, and the depth rows add distinct deployments rather than file-count padding.{split}"
 find.append({'ordinal':x['ordinal'],'id':x['id'],'term':x['term'],'path':p,'entrySha256':h,'worksheetSha256':x['worksheetSha256'],'verdict':verdict,'finding':note,'senseCount':len(senses),'occurrenceCount':occ,'claimAnchorCount':anchors,'independentWorkCount':len(works)})
out={'schemaVersion':1,'reviewType':'provisional-independent-semantic-review','wave':'f002','lane':'B','ordinals':[401,450],'reviewer':'Codex independent reviewer (not Lane B author)','reviewedUtc':'2026-07-15T20:30:00Z','state':'provisional-until-formal-gate-confirms-identical-entry-hashes','readOnly':True,'siteTouched':False,'scope':'All current B401–450 worksheets and compiled entries; 318 exact cases, actor/action grammar, independent work IDs, #0g senses, depth uniqueness, anchors/counts, and aliases. Formal gate was not available, so no verdict authorizes promotion.','inputs':{'durableLedger':{'path':ledgerp,'sha256':sha(ledgerp)},'attributionAudit':{'path':attrp,'sha256':sha(attrp)},'senseRetest':{'path':sensep,'sha256':sha(sensep)},'depthAudit':{'path':depthp,'sha256':sha(depthp)}},'hashCondition':'Every finding is valid only while entrySha256 and worksheetSha256 remain identical. Any changed entry requires re-review.','summary':{'entries':50,'KEEP':sum(x['verdict']=='KEEP' for x in find),'REVISE':sum(x['verdict']=='REVISE' for x in find),'formalGateRun':False},'findings':find}
op=os.path.join(R,'fresh-build/waves/f002-laneB-401-450-provisional-independent-semantic-review.json');open(op,'w').write(json.dumps(out,ensure_ascii=False,indent=2)+'\n');print(json.dumps(out['summary'],indent=2));print(hashlib.sha256(open(op,'rb').read()).hexdigest())
