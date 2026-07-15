const fs=require('fs'), path=require('path');
const p=path.join(__dirname,'fresh-build','entries','t_d27432779ce8','evidence.draft.json');
const d=JSON.parse(fs.readFileSync(p,'utf8')), s=d.Entry.Senses[0];
s.Occurrences.push({RelPath:'X/X80/X80n1565.xml',FromLb:'0294b14',ToLb:'0294b16',Kwic:'寶月流輝。澄潭布影。水無蘸月之意。月無分照之心。水月兩忘。方可稱斷。',Curated:true,MasterName:'皷山智嚴了覺禪師',ContextMasters:[{MasterName:'皷山智嚴了覺禪師',Roles:['utterer']}],AttributionNote:'Source text (五燈會元; speaker: 皷山智嚴了覺禪師): the named master owns the complete headword-bearing sermon clause.',DraftActorProof:{ExactHeadwordClause:'寶月流輝。澄潭布影。水無蘸月之意。月無分照之心。水月兩忘。方可稱斷。',GrammaticalSubject:'皷山智嚴了覺禪師',SpeechFrame:'The nearest section head names the master, and the complete unit is his uninterrupted sermon.',FullCaseDecision:'The named section master utters the headword-bearing clause.'}});
s.SourceTexts=[...new Set(s.Occurrences.map(o=>o.RelPath))].sort();
s.DraftEvidence.OpeningClaimEvidenceKeys=s.Occurrences.map((_,i)=>`o${i+1}`);
s.DraftEvidence.IndependentWorkIds=[...new Set([...(s.DraftEvidence.IndependentWorkIds||[]),'work:X80n1565'])];
s.ExplanationParts.EvidenceBody=["The Record of Xiangyan Xixin pictures the moon disk suspended in the blue sky and its round image falling into the ocean; the Records of the Source-Mirror describes the water-moon beside a mirror image and explicitly calls the constructed image provisional rather than real. Zhiyan Liaojue says that the bright moon casts its image over a clear pool while neither water nor moon intends the relation, and then speaks of forgetting both. Hongzhi’s verse retains the image as a moonlit waterscape. A previous temple-name substring has been removed because it names a place, not the reflection."];
fs.writeFileSync(p,JSON.stringify(d,null,2)+'\n');
