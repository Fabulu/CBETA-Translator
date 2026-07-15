const fs=require('fs'),path=require('path');
const root=path.join(__dirname,'fresh-build','entries');
const now='2026-07-15T10:25:00+02:00';
function load(id){const p=path.join(root,id,'evidence.draft.json');return {p,d:JSON.parse(fs.readFileSync(p,'utf8'))};}
function save(x){fs.writeFileSync(x.p,JSON.stringify(x.d,null,2)+'\n');}
function named(RelPath,FromLb,ToLb,Kwic,name,title,proof){return {RelPath,FromLb,ToLb,Kwic,Curated:true,MasterName:name,ContextMasters:[{MasterName:name,Roles:['utterer']}],AttributionNote:`Source text (${title}): ${name} owns the exact headword-bearing clause.`,DraftActorProof:{ExactHeadwordClause:Kwic,GrammaticalSubject:name,SpeechFrame:proof,FullCaseDecision:proof}};}
function narrated(RelPath,FromLb,ToLb,Kwic,title,context){return {RelPath,FromLb,ToLb,Kwic,Curated:true,ActorAttribution:{Status:'narrated',Kind:'compiler narrative',ActorLabel:'the compiler narrating the headword-bearing action',ActorRole:'compiler',RungsChecked:['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],ReviewedBy:'Codex f003 Lane A corrective author',ReviewedUtc:now,GrammarEvidence:'The exact headword is an action reported by the recorder, not quoted speech.'},ContextMasters:context?[{MasterName:context,Roles:['person-described']}]:[],AttributionNote:`Source text (${title}): the compiler narrates the exact action${context?`; ${context} is the person described`:''}.`,DraftActorProof:{ExactHeadwordClause:Kwic,GrammaticalSubject:'the compiler',SpeechFrame:'Documentary narration.',FullCaseDecision:'No master utterer is invented.'}};}
{
 const x=load('t_6e57aada2121'),s=x.d.Entry.Senses[0];
 s.Occurrences.push(
  named('J/J38/J38nB425.xml','0689c11','0689c12','徹骨徹髓之言，炙膚然頂，難報此恩。','Jifei Ruyi','即非禪師全錄','The complete letter is Jifei Ruyi’s first-person written response.'),
  narrated('X/X70/X70n1400.xml','0700a01','0700a02','於塔前慟哭，然頂煉臂者猶憧憧不絕。','高峰原妙禪師語錄','Gaofeng Yuanmiao'),
  named('J/J27/J27nB198.xml','0449b05','0449b06','然指然頂，無非示誠重之心','Xueguan Zhiyin','雪關禪師語錄','The clause remains inside Xueguan Zhiyin’s formal discourse and reports Gaofeng’s demand.')
 );
 s.Validation='multi-source';s.SourceTexts=[...new Set(s.Occurrences.map(o=>o.RelPath))].sort();s.DraftEvidence.OpeningClaimEvidenceKeys=s.Occurrences.map((_,i)=>`o${i+1}`);s.DraftEvidence.IndependentWorkIds=['work:wudeng-quanshu','work:J38nB425','work:X70n1400','work:J27nB198'];
 s.ExplanationParts.CorpusEarnedOpening='Burning the crown of the head is literal bodily burning offered as devotion, recompense, or proof of serious intent.';
 s.ExplanationParts.EvidenceBody=['The Complete Record of the Five Lamps narrates monastic and lay admirers burning their crowns and cauterizing their arms as offerings after Longchi Yiyuan’s death. Gaofeng Yuanmiao’s biography likewise reports mourners burning their crowns and arms before his stupa. Xueguan Zhiyin says Gaofeng required burning a finger or the crown to show a weighty and sincere heart, while Jifei Ruyi pairs scorching the skin and burning the crown with an inability to repay instruction. These distinct deployments keep the act literal without claiming that every passage records the same ceremony.'];
 save(x);
}
{
 const x=load('t_8bbc811be86e'),s=x.d.Entry.Senses[0];
 s.Occurrences.push(
  named('J/J35/J35nB342.xml','0795c23','0795c23','師云：「去然香來。」','Huayan Shengke','華嚴聖可禪師語錄','Huayan Shengke gives the exact instruction before the monk performs it.'),
  narrated('J/J27/J27nB192.xml','0190c30','0191a01','預夜然香，禱告龍天、十方諸佛','大休珠禪師語錄','Daxiu Zhu'),
  narrated('J/J28/J28nB210.xml','0439c28','0439c29','遠近奔赴，然香慟哭，奉全身于伏獅之左','伏獅祇園禪師語錄','伏獅祇園禪師')
 );
 s.Validation='multi-source';s.SourceTexts=[...new Set(s.Occurrences.map(o=>o.RelPath))].sort();s.DraftEvidence.OpeningClaimEvidenceKeys=s.Occurrences.map((_,i)=>`o${i+1}`);s.DraftEvidence.IndependentWorkIds=['work:T48n2021','work:J35nB342','work:J27nB192','work:J28nB210'];
 s.ExplanationParts.CorpusEarnedOpening='To burn incense is a literal action whose purpose changes with the surrounding ceremony.';
 s.ExplanationParts.EvidenceBody=['The Collection for Resolving Chan Doubts narrates incense burned before a sacred image while establishing an oath. Huayan Shengke directly tells a monk to go light incense; Daxiu Zhu’s biography records incense burned before a prayer to celestial protectors and the buddhas; and the record of Fushi Qiyuan describes distant and nearby mourners burning incense and weeping at his death. Oath, instruction, prayer, and mourning are distinct settings for the same incense-burning action; none of these witnesses says that incense is burned into the body.'];
 save(x);
}
