const fs=require('fs'),path=require('path'),crypto=require('crypto');
const root=__dirname, waves=path.join(root,'fresh-build','waves'), entries=path.join(root,'fresh-build','entries');
const reviewName='f003-laneA-601-650-evidence-repair-independent-exact-review.json';
const gateName='f003-laneA-601-650-corrective-author-formal-gate-v13.json';
const review=JSON.parse(fs.readFileSync(path.join(waves,reviewName),'utf8'));
const gate=JSON.parse(fs.readFileSync(path.join(waves,gateName),'utf8'));
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const entryHashes={},worksheetHashes={},rows=[];
for(const r of review.rows){
 const ep=path.join(entries,r.id,'entry.v2.json'),wp=path.join(entries,r.id,'evidence.draft.json');
 entryHashes[r.id]=sha(ep);worksheetHashes[r.id]=sha(wp);
 rows.push({ordinal:r.ordinal,id:r.id,term:r.term,previousRejectedEntrySha256:r.entrySha256,currentEntrySha256:entryHashes[r.id],currentWorksheetSha256:worksheetHashes[r.id],changed:r.entrySha256!==entryHashes[r.id]});
}
const payload={schemaVersion:1,wave:'f003',lane:'A',range:'601-650',role:'corrective evidence-repair author',generatedUtc:new Date().toISOString(),reviewInput:reviewName,reviewInputSha256:sha(path.join(waves,reviewName)),formalGate:gateName,formalGateSha256:sha(path.join(waves,gateName)),formalGateHardPass:gate.hardPass,exactKwicVerified:gate.exactKwic.verified,exactKwicFailures:gate.exactKwic.failureCount,attributionPacketGeneratorVersion:gate.attributionPackets.generatorVersion,turnProofMissing:gate.attributionPackets.turnProofMissing,repairedEntryHashes:entryHashes,worksheetHashes,rows,allRejectedHashesChanged:rows.every(r=>r.changed),selfReview:false,promotion:false,merge:false,siteTouched:false};
fs.writeFileSync(path.join(waves,'f003-laneA-601-650-corrective-author-ledger.json'),JSON.stringify(payload,null,2)+'\n');
