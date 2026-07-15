const fs=require('fs'),crypto=require('crypto'),path=require('path');
const base=__dirname,waves=path.join(base,'fresh-build','waves'),entries=path.join(base,'fresh-build','entries');
const reviewName='f003-laneB-701-750-round2-fresh-independent-exact-review.json';
const originalName='f003-laneB-701-750-independent-exact-review.json';
const gateName='f003-laneB-701-750-final4-formal-gate-v4.json';
const review=JSON.parse(fs.readFileSync(path.join(waves,reviewName))),original=JSON.parse(fs.readFileSync(path.join(waves,originalName))),gate=JSON.parse(fs.readFileSync(path.join(waves,gateName)));
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const ids=['t_c212062774f9','t_2da0e2fc0478','t_298f7fdd14bd','t_74390b40f658'];
const currentReviewKeeps=review.rows.filter(r=>r.verdict==='KEEP');
const originalKeeps=original.rows.filter(r=>r.verdict==='KEEP');
const keepRows=[...currentReviewKeeps,...originalKeeps];
const priorKeepHashProof={count:keepRows.length,allPriorKeepHashesUnchanged:keepRows.every(r=>sha(path.join(entries,r.id,'entry.v2.json'))===r.entrySha256)};
const repairedEntryHashes={},worksheetHashes={};for(const id of ids){repairedEntryHashes[id]=sha(path.join(entries,id,'entry.v2.json'));worksheetHashes[id]=sha(path.join(entries,id,'evidence.draft.json'));}
const out={schemaVersion:'f003-laneB-701-750-final4-repair-author-v1',role:'repair-author',sourceRejectingReview:reviewName,sourceRejectingReviewSha256:sha(path.join(waves,reviewName)),finalFormalGate:gateName,finalFormalGateSha256:sha(path.join(waves,gateName)),formalHardPass:gate.hardPass,clusterScopeIds:gate.clusterScopeIds,strictRosterScopeIds:gate.strictRosterScopeIds,exactKwic:{verified:gate.exactKwic.verified,failures:gate.exactKwic.failureCount},attributionPackets:{path:gate.attributionPackets.report,generatorVersion:gate.attributionPackets.generatorVersion,turnProofMissing:gate.attributionPackets.turnProofMissing},pendingRosterPacket:'f003-laneB-701-750-final4-roster-candidates.json',priorKeepHashProof,repairedEntryHashes,worksheetHashes,selfReview:false,promotion:false,merge:false,siteTouched:false};
fs.writeFileSync(path.join(waves,'f003-laneB-701-750-final4-repair-author-ledger.json'),JSON.stringify(out,null,2)+'\n');console.log(JSON.stringify({hardPass:out.formalHardPass,priorKeepHashProof,clusterScopeIds:out.clusterScopeIds},null,2));
