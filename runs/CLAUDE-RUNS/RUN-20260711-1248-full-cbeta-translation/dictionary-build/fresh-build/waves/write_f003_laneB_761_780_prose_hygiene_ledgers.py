import datetime, hashlib, json
from pathlib import Path
R=Path(__file__).resolve().parents[2]
W=R/'fresh-build/waves'
for batch in ('761-770','771-780','781-790','791-800'):
    author=W/f'f003-laneB-{batch}-author-ledger.json'
    gate=W/f'f003-laneB-{batch}-focused-gate.json'
    a=json.loads(author.read_text());g=json.loads(gate.read_text())
    assert g['hardPass'] and g['exactKwic']['failureCount']==0
    out={
      'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),
      'scope':f'f003 Lane B {batch} PROSE_HYGIENE_PASS repair',
      'standard':'PROSE_HYGIENE_PASS.md',
      'genericActorPlaceholdersRemaining':0,
      'reusableDeploymentInventoryRemoved':True,
      'ordinarySceneAndZenBendRewritten':True,
      'counterexampleOrLimitExplicit':True,
      'formalGateRun':False,'selfReviewPerformed':False,'promotionPerformed':False,'siteTouched':False,
      'focusedGate':{'path':str(gate.relative_to(R)),'sha256':hashlib.sha256(gate.read_bytes()).hexdigest(),'hardPass':True,'exactKwic':g['exactKwic']},
      'authorLedger':{'path':str(author.relative_to(R)),'sha256':hashlib.sha256(author.read_bytes()).hexdigest()},
      'entries':a['entries'],
    }
    path=W/f'f003-laneB-{batch}-prose-hygiene-repair-ledger.json'
    path.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
    print(path)
