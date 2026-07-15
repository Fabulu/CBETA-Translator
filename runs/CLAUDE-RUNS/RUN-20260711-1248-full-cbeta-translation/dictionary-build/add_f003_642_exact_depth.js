const fs = require('fs');
const path = require('path');
const p = path.join(__dirname, 'fresh-build', 'entries', 't_08c0c321eb2a', 'evidence.draft.json');
const d = JSON.parse(fs.readFileSync(p, 'utf8'));
const s = d.Entry.Senses[0];
const reviewed = '2026-07-15T10:05:00+02:00';
function narrated(RelPath, FromLb, ToLb, Kwic, title, context) {
  return { RelPath, FromLb, ToLb, Kwic, Curated:true,
    ActorAttribution:{Status:'narrated',Kind:'compiler narrative',ActorLabel:'the compiler narrating the incense-lighting action',ActorRole:'compiler',RungsChecked:['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],ReviewedBy:'Codex f003 Lane A corrective author',ReviewedUtc:reviewed,GrammarEvidence:'The headword is the narrated action surrounding or introducing a separately marked utterance.'},
    ContextMasters: context ? [{MasterName:context,Roles:['person-described']}] : [],
    AttributionNote:`Source text (${title}): the compiler narrates the exact incense-lighting action; ${context || 'no master'} is context rather than utterer of the headword.`,
    DraftActorProof:{ExactHeadwordClause:Kwic,GrammaticalSubject:'the compiler',SpeechFrame:'Narrated action, not the following quoted words.',FullCaseDecision:'The compiler owns the headword; any named master remains contextual.'}
  };
}
s.Occurrences.push(
  narrated('X/X70/X70n1382.xml','0233a04','0233a04','下座，同詣妙勝殿炷香作禮。','無準師範禪師語錄','Wuzhun Shifan'),
  narrated('J/J40/J40nB479.xml','0132a13','0132a13','遂炷香云：「相看投機處，青山覆白雲。」','昭覺竹峰續禪師語錄','昭覺竹峰續禪師'),
  narrated('X/X83/X83n1578.xml','0672a17','0672a18','師炷香曰：此香為殺人不眨眼上將軍，立地成佛大居士。','指月錄','雲居佛印元禪師'),
  {RelPath:'X/X70/X70n1376.xml',FromLb:'0055a02',ToLb:'0055a02',Kwic:'昨日徒弟德言炷香請就五參，時為眾東語西話。',Curated:true,
   MasterName:'Chijue Daochong',ContextMasters:[{MasterName:'Chijue Daochong',Roles:['utterer']}],AttributionNote:'Source text (痴絕道冲禪師語錄): Chijue Daochong owns the exact clause in his formal discourse; disciple Deyan performs the narrated action inside that utterance.',DraftActorProof:{ExactHeadwordClause:'昨日徒弟德言炷香請就五參，時為眾東語西話。',GrammaticalSubject:'Chijue Daochong',SpeechFrame:'The complete formal discourse remains Chijue Daochong’s speech; disciple Deyan is the person described.',FullCaseDecision:'MasterName records the utterer of the headword-bearing clause, Chijue Daochong.'}}
);
s.Validation='multi-source';
s.SourceTexts=[...new Set(s.Occurrences.map(o=>o.RelPath))].sort();
s.DraftEvidence.OpeningClaimEvidenceKeys=s.Occurrences.map((_,i)=>`o${i+1}`);
s.DraftEvidence.IndependentWorkIds=s.SourceTexts.map(r=>'work:'+r.split('/').pop().replace('.xml',''));
s.ExplanationParts.EvidenceBody=["Monastic regulations use the headword as an instruction to light incense and report. Wuzhun Shifan’s record narrates going to the hall to light incense and bow; other records narrate lighting incense before a verse or dedication. Chijue Daochong says that his disciple Deyan lit incense to request the formal discourse. Counted one-stick constructions remain in the separate phrase entry, and a table-of-contents string is excluded."];
fs.writeFileSync(p, JSON.stringify(d,null,2)+'\n');
