const fs=require('fs'),path=require('path');
const base=__dirname,entries=path.join(base,'fresh-build','entries'),now='2026-07-15T18:30:00Z',author='Codex f003 laneB final2 repair author';
function load(id){const ep=path.join(entries,id,'entry.v2.json'),wp=path.join(entries,id,'evidence.draft.json');return {id,ep,wp,e:JSON.parse(fs.readFileSync(ep)),w:JSON.parse(fs.readFileSync(wp))};}
function proof(o,subject,frame,decision){o.DraftActorProof={ExactHeadwordClause:o.Kwic,GrammaticalSubject:subject,SpeechFrame:frame,FullCaseDecision:decision};}
function save(x){x.e.CreatedBy=author;x.e.WrittenUtc=now;const old=x.w.Entry;x.w.Entry=JSON.parse(JSON.stringify(x.e));for(let i=0;i<x.w.Entry.Senses.length;i++){const ns=x.w.Entry.Senses[i],os=old.Senses[i]||{};if(os.DraftEvidence)ns.DraftEvidence=os.DraftEvidence;const m=(ns.Explanation||'').match(/^(.+?[.!?])\s+([\s\S]+)$/);ns.ExplanationParts=m?{CorpusEarnedOpening:m[1],EvidenceBody:[m[2]]}:{CorpusEarnedOpening:ns.Explanation,EvidenceBody:[]};for(const no of ns.Occurrences||[]){const oo=(os.Occurrences||[]).find(v=>v.RelPath===no.RelPath&&v.FromLb===no.FromLb);if(!no.DraftActorProof)no.DraftActorProof=(oo&&oo.DraftActorProof)||{ExactHeadwordClause:no.Kwic,GrammaticalSubject:no.MasterName||(no.ActorAttribution||{}).ActorLabel,SpeechFrame:no.AttributionNote,FullCaseDecision:no.AttributionNote};}}fs.writeFileSync(x.wp,JSON.stringify(x.w,null,2)+'\n');}
function context(name,role){return {MasterName:name,Roles:[role]};}
function unnamedQuestion(o,respondent){delete o.MasterName;o.ContextMasters=[context(respondent,'respondent')];o.ActorAttribution={Status:'reviewed-unnamed',Kind:'anonymous monastic speech',ActorLabel:'the unnamed monastic questioner',ActorRole:'questioner',RungsChecked:['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],ReviewedBy:author,ReviewedUtc:now,GrammarEvidence:'The explicit question frame owns the exact headword-bearing question; the following marked master turn belongs to the named respondent.'};}

// 三門: both parallel biographies explicitly describe Sengcan at the monastery gate.
{
 const x=load('t_2da0e2fc0478'),o=x.e.Senses[0].Occurrences;
 for(const i of [1,3]){o[i].ContextMasters=[context('Sengcan','person-described')];o[i].AttributionNote=`Source text (${i===1?'五燈全書(第1卷-第33卷)':'五燈會元'}): the biographical recorder narrates Sengcan speaking below the monastery gate; Sengcan is the named person described, not the utterer of the narrator-owned headword.`;}
 save(x);
}

// 開爐: replace three multi-token case cuts with one exact, single-owner token each.
{
 const x=load('t_298f7fdd14bd'),o=x.e.Senses[0].Occurrences;
 o[0].FromLb='0004b18';o[0].ToLb='0004b19';o[0].Kwic='法昌今日開爐，行脚僧無一箇。';o[0].MasterName='Fachang Yiyu';o[0].ContextMasters=[context('Fachang Yiyu','utterer')];delete o[0].ActorAttribution;o[0].AttributionNote='Source text (五燈全書(第34卷-第120卷)): Fachang Yiyu utters the clean single-token line that Fachang opens the furnace today; the preceding recorder-owned occasion label is outside this occurrence.';proof(o[0],'Fachang Yiyu','The marked speech after the furnace-opening ascent contains this single headword token.','Fachang Yiyu is the exact utterer.');
 o[5].FromLb='0675c13';o[5].ToLb='0675c13';o[5].Kwic='問：「明旦開爐即不問，趙州四佛請宣揚。」';unnamedQuestion(o[5],"Tian'an Sheng");o[5].AttributionNote="Source text (天岸昇禪師語錄): the unnamed monastic questioner asks about tomorrow’s furnace opening; Tian'an Sheng gives the following response. The editorial occasion label is outside this occurrence.";proof(o[5],'the unnamed monastic questioner','The explicit question marker introduces the single headword token; the master response follows.','The unnamed monk utters the headword and Tian\'an Sheng responds.');
 o[6].FromLb='0565a03';o[6].ToLb='0565a03';o[6].Kwic='青龍今日開爐，親切無如者箇';o[6].MasterName='Hansong Zhicao';o[6].ContextMasters=[context('Hansong Zhicao','utterer')];delete o[6].ActorAttribution;o[6].AttributionNote='Source text (寒松操禪師語錄): Hansong Zhicao utters the clean single-token line that Qinglong opens the furnace today; the heading and quoted earlier masters are outside this occurrence.';proof(o[6],'Hansong Zhicao','The presiding master contrasts his own wording with earlier quoted examples, then utters this single headword token.','Hansong Zhicao is the exact utterer.');
 x.e.Senses[0].Explanation='Opening the furnace is the recorded start-of-cold-season occasion on which a presiding master gives a public address. Fachang Yiyu says that Fachang opens it today, an unnamed monk asks Tian\'an Sheng about tomorrow’s opening, Hansong Zhicao contrasts Qinglong’s opening with earlier masters, and Hongjue Min uses the furnace as the frame of his own address. The term names a calendrical monastic occasion and its public teaching event, not merely lighting a household fire.';
 save(x);
}

// Durable omitted-context and bundled-token canaries.
{
 const p=path.join(base,'fresh-build','semantic-regressions.json'),r=JSON.parse(fs.readFileSync(p));
 r.t_2da0e2fc0478={term:'三門',occurrenceAssertions:[{RelPath:'D/D48/D48n8939.xml',FromLb:'0011a10',mustActorStatus:'reviewed-unnamed',forbiddenMasterNames:['Foyan Qingyuan']},{RelPath:'X/X81/X81n1571.xml',FromLb:'0420a20',mustContextMasterName:'Sengcan'},{RelPath:'X/X80/X80n1565.xml',FromLb:'0044b06',mustContextMasterName:'Sengcan'}]};
 r.t_298f7fdd14bd={term:'開爐',forbiddenOccurrenceSubstrings:['開爐日，以一力撾鼓陞座','開爐，晚參。問：「明旦開爐','開爐，上堂。「安居樹下'],occurrenceAssertions:[{RelPath:'X/X82/X82n1571.xml',FromLb:'0004b18',KwicContains:'法昌今日開爐',mustMasterName:'Fachang Yiyu'},{RelPath:'J/J26/J26nB187.xml',FromLb:'0675c13',KwicContains:'明旦開爐',mustActorStatus:'reviewed-unnamed',forbiddenMasterNames:["Tian'an Sheng"]},{RelPath:'J/J37/J37nB392.xml',FromLb:'0565a03',KwicContains:'青龍今日開爐',mustMasterName:'Hansong Zhicao'}]};
 fs.writeFileSync(p,JSON.stringify(r,null,2)+'\n');
}
console.log('final2 worksheets repaired');
