#!/usr/bin/env python3
"""Write the read-only exact-hash final rereview for 三門 and 開爐."""
import datetime, hashlib, json
from pathlib import Path

HERE=Path(__file__).resolve().parent
ROOT=HERE.parent.parent
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def load(p): return json.loads(p.read_text(encoding='utf-8'))

def main():
    ledger=HERE/'f003-laneB-701-750-final2-repair-author-ledger.json'
    readiness=HERE/'f003-laneB-701-750-final2-repair-readiness.json'
    gate=HERE/'f003-laneB-701-750-final2-full50-formal-gate.json'
    packets=HERE/'f003-laneB-701-750-final2-full50-formal-gate-attribution-packets.json'
    source=HERE/'f003-laneB-701-750-exact8-round2-fresh-independent-rereview.json'
    ld,rd,gd,pd=map(load,(ledger,readiness,gate,packets))
    assert rd['hardPass'] and gd['hardPass']
    assert gd['exactKwic']['verified']==369 and gd['exactKwic']['failureCount']==0
    assert pd['generatorVersion']==3 and gd['attributionPackets']['turnProofMissing']==0
    wave=load(HERE/'f003.json'); byid={x['id']:x for x in wave['entries']}
    assert ld['priorKeepHashProof']['count']==48
    prior=True
    for r in ld['priorKeepHashProof']['rows']:
        cur=sha(ROOT/byid[r['id']]['entryPath'])
        prior &= cur==r['expectedSha256']==r['currentSha256']
    rows=[
      {'ordinal':713,'id':'t_2da0e2fc0478','term':'三門','verdict':'KEEP','occurrencesRead':7,
       'finding':"KEEP — All seven current occurrences were reread in their complete v3 units. The two biographical gate witnesses now retain Sengcan canonically in ContextMasters with the exact role person-described while correctly leaving MasterName null because the recorder, not Sengcan, writes the headword-bearing narration. The Foyan questioner/respondent split remains exact, the other three spoken gate witnesses retain their utterers, and the full 前之三門 enumeration supports the separate different-thing sense 'three approaches.' The architectural canary remains concrete and the prose does not turn passage through the gate into an imported doctrine."},
      {'ordinal':722,'id':'t_298f7fdd14bd','term':'開爐','verdict':'KEEP','occurrencesRead':8,
       'finding':"KEEP — All eight current occurrences were reread. The three formerly ambiguous witnesses are now unambiguous single-token occurrences with synchronized line bounds: Fachang Yiyu utters 法昌今日開爐; an unnamed monastic questioner utters 明旦開爐 with Tian'an Sheng retained only as respondent; Hansong Zhicao utters 青龍今日開爐 after the earlier quoted masters are excluded from the KWIC. The five remaining occasion-label or address witnesses preserve their section subjects/utterers correctly. The prose and canary accurately distinguish the calendrical public furnace-opening occasion from merely lighting a household fire, without splitting the heading and address uses into different things."}
    ]
    for r in rows:
        cur=sha(ROOT/byid[r['id']]['entryPath']); assert cur==ld['repairedEntryHashes'][r['id']];r['entrySha256']=cur
    report={'schemaVersion':1,'reviewType':'fresh independent exact-hash final2 full-case rereview','wave':'f003','lane':'B','ordinals':'701-750 final2 repair set','generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'reviewer':'Codex independent reviewer; not the final2 repair author','readOnly':True,'entriesEdited':0,'promotionOrMergePerformed':False,'siteTouched':False,
      'inputs':{'sourceRejectingReview':source.name,'sourceRejectingReviewSha256':sha(source),'repairAuthorLedger':ledger.name,'repairAuthorLedgerSha256':sha(ledger),'repairReadiness':readiness.name,'repairReadinessSha256':sha(readiness),'full50FormalGate':gate.name,'full50FormalGateSha256':sha(gate),'v3AttributionPackets':packets.name,'v3AttributionPacketsSha256':sha(packets)},
      'formalGateHardPass':True,'repairReadinessHardPass':True,'exactKwic':{'verified':369,'failures':0},'attributionPacketGeneratorVersion':3,'turnProofMissing':0,'repairedEntriesRead':2,'repairedOccurrencesReadInCompleteCaseContext':15,'priorKeepCount':48,'priorKeepHashesByteIdentical':prior,'summary':{'KEEP':2,'REVISE':0},
      'systemicFindings':['Narrator ownership and named contextual-master linkage are separate decisions; both Sengcan witnesses now encode both correctly.','A repeated headword inside one broad KWIC can conceal multiple actors; the repaired spoken 開爐 witnesses now each contain exactly one headword token and one attributable turn.'],'rows':rows}
    out=HERE/'f003-laneB-701-750-final2-fresh-independent-exact-rereview.json'
    out.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({'path':out.name,'sha256':sha(out),'summary':report['summary'],'priorKeepHashesByteIdentical':prior},ensure_ascii=False))
if __name__=='__main__':main()
