const fs=require('fs'),crypto=require('crypto'),path=require('path');
const base=__dirname,waves=path.join(base,'fresh-build','waves'),entries=path.join(base,'fresh-build','entries');
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const specs=[
 {review:'f003-laneB-701-750-final4-fresh-independent-exact-review.json',priorGate:'f003-laneB-701-750-final4-formal-gate-v4.json',gate:'f003-laneB-701-750-exact8-round2-full50-formal-gate.json',ledger:'f003-laneB-701-750-exact8-round2-repair-author-ledger.json',ids:['t_c212062774f9','t_2da0e2fc0478','t_298f7fdd14bd','t_74390b40f658']},
 {review:'f003-laneB-751-800-final4-fresh-independent-exact-review.json',priorGate:'f003-laneB-final4-exact-turn-full50-formal-gate-v3.json',gate:'f003-laneB-751-800-exact8-round2-full50-formal-gate.json',ledger:'f003-laneB-751-800-exact8-round2-repair-author-ledger.json',ids:['t_51a4f3a03bd8','t_32a92c635f49','t_5306489d35c6','t_0229ebe0b9e7']}
];
for(const s of specs){
 const reviewPath=path.join(waves,s.review),priorPath=path.join(waves,s.priorGate),gatePath=path.join(waves,s.gate);
 const prior=JSON.parse(fs.readFileSync(priorPath)),gate=JSON.parse(fs.readFileSync(gatePath));
 const repairedEntryHashes={},worksheetHashes={};for(const id of s.ids){repairedEntryHashes[id]=sha(path.join(entries,id,'entry.v2.json'));worksheetHashes[id]=sha(path.join(entries,id,'evidence.draft.json'));}
 const keeps=prior.entries.filter(e=>!s.ids.includes(e.id)).map(e=>({id:e.id,expectedSha256:e.sha256,currentSha256:sha(path.join(entries,e.id,'entry.v2.json'))}));
 const priorKeepHashProof={count:keeps.length,allPriorKeepHashesUnchanged:keeps.every(r=>r.expectedSha256===r.currentSha256),rows:keeps};
 const out={schemaVersion:1,role:'repair-author',sourceRejectingReview:s.review,sourceRejectingReviewSha256:sha(reviewPath),finalFormalGate:s.gate,finalFormalGateSha256:sha(gatePath),formalHardPass:gate.hardPass,clusterScopeIds:gate.clusterScopeIds,strictRosterScopeIds:gate.strictRosterScopeIds,exactKwic:{verified:gate.exactKwic.verified,failures:gate.exactKwic.failureCount},attributionPackets:{path:gate.attributionPackets.report,generatorVersion:gate.attributionPackets.generatorVersion,turnProofMissing:gate.attributionPackets.turnProofMissing,hardPass:gate.attributionPackets.hardPass},semanticRegressions:{path:'fresh-build/semantic-regressions.json',sha256:sha(path.join(base,'fresh-build','semantic-regressions.json'))},cohortPendingRosterPacket:'f003-laneB-exact8-cohort-pending-roster.json',priorKeepHashProof,repairedEntryHashes,worksheetHashes,selfReview:false,promotion:false,merge:false,siteTouched:false};
 fs.writeFileSync(path.join(waves,s.ledger),JSON.stringify(out,null,2)+'\n');
 console.log(s.ledger,gate.hardPass,priorKeepHashProof.count,priorKeepHashProof.allPriorKeepHashesUnchanged);
}
