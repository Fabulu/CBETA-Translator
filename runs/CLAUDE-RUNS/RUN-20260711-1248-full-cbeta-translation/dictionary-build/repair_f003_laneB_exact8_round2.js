const fs = require('fs');
const path = require('path');
const root = __dirname;
const entries = path.join(root, 'fresh-build', 'entries');
const now = '2026-07-15T17:30:00Z';
const reviewer = 'Codex f003 laneB exact8 priority repair author';

function load(id) {
  const ep = path.join(entries, id, 'entry.v2.json');
  const wp = path.join(entries, id, 'evidence.draft.json');
  return { id, ep, wp, e: JSON.parse(fs.readFileSync(ep)), w: JSON.parse(fs.readFileSync(wp)) };
}
function save(x) {
  x.e.CreatedBy = reviewer; x.e.WrittenUtc = now;
  const old=x.w.Entry;
  x.w.Entry = JSON.parse(JSON.stringify(x.e));
  for(let i=0;i<x.w.Entry.Senses.length;i++) {
    const ns=x.w.Entry.Senses[i], os=old.Senses[i]||{};
    if(os.DraftEvidence) ns.DraftEvidence=os.DraftEvidence;
    const m=(ns.Explanation||'').match(/^(.+?[.!?])\s+([\s\S]+)$/);
    ns.ExplanationParts=m?{CorpusEarnedOpening:m[1],EvidenceBody:[m[2]]}:{CorpusEarnedOpening:ns.Explanation,EvidenceBody:[]};
    for(const no of ns.Occurrences||[]) {
      const oo=(os.Occurrences||[]).find(v=>v.RelPath===no.RelPath&&v.FromLb===no.FromLb);
      const label=no.MasterName||(no.ActorAttribution||{}).ActorLabel||'the documented source actor';
      no.DraftActorProof=no.DraftActorProof||(oo&&oo.DraftActorProof)||{ExactHeadwordClause:no.Kwic,GrammaticalSubject:label,SpeechFrame:no.AttributionNote,FullCaseDecision:no.AttributionNote};
    }
  }
  fs.writeFileSync(x.wp, JSON.stringify(x.w, null, 2) + '\n');
}
function ctx(name, role) { return { MasterName: name, Roles: [role] }; }
function actor(status, kind, label, role, grammar) {
  return { Status: status, Kind: kind, ActorLabel: label, ActorRole: role,
    RungsChecked: ['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],
    ReviewedBy: reviewer, ReviewedUtc: now, GrammarEvidence: grammar };
}
function narrated(o, label, contexts, grammar, note, status='narrated', kind='compiler or recorder narration') {
  delete o.MasterName; o.ContextMasters = contexts; o.ActorAttribution = actor(status, kind, label, 'compiler', grammar); o.AttributionNote = note;
}
function unnamedQuestion(o, respondent, note) {
  delete o.MasterName; o.ContextMasters = [ctx(respondent, 'respondent')];
  o.ActorAttribution = actor('reviewed-unnamed','anonymous monastic speech','the unnamed monastic questioner','questioner','The explicit 僧問/問 frame assigns the headword-bearing question to the monk; the following 師 turn belongs to the named respondent.');
  o.AttributionNote = note;
  o.DraftActorProof={ExactHeadwordClause:o.Kwic,GrammaticalSubject:'the unnamed monastic questioner',SpeechFrame:'The explicit 僧問/問 frame introduces the headword-bearing question; the following 師 turn is the response.',FullCaseDecision:'The unnamed monastic questioner owns the exact headword-bearing question; '+respondent+' is the respondent.'};
}

// 禪床: narration owns action clauses; named actors and case owners remain linked.
{
  const x=load('t_c212062774f9'), s=x.e.Senses[0], o=s.Occurrences;
  o[1].ContextMasters=[ctx('Zhaozhou Congshen','person-described')];
  o[1].AttributionNote='Source text (古尊宿語錄): the recorder narrates Zhaozhou Congshen remaining on the Chan couch before King Zhao and quotes his reply; Zhaozhou is the acting case figure, not the utterer of the narrated headword.';
  o[2].ContextMasters=[ctx("Ying'an Tanhua",'section-subject')];
  o[2].AttributionNote="Source text (列祖提綱錄): the recorder narrates Ying'an Tanhua descending from the Chan couch and dancing before his marked speech; the action clause, not the following marked speech turn, contains the headword.";
  o[3].ContextMasters=[ctx('Shexian Guisheng','section-subject')];
  o[3].AttributionNote='Source text (續傳燈錄), Shexian Guisheng section: the recorder narrates the presiding master striking the Chan couch between a monk’s question and reply.';
  o[4].ContextMasters=[ctx('Shishuang Chuyuan','section-subject')];
  o[4].AttributionNote='Source text (嘉泰普燈錄), Shishuang Chuyuan section: the recorder narrates the presiding master striking the Chan couch before he addresses the assembly.';
  o[5].ContextMasters=[ctx('Niutou Huizhong','person-described')];
  o[5].AttributionNote='Source text (景德傳燈錄): the biographer narrates Niutou Huizhong striking the Chan couch to summon his tigers; the action is not quoted speech.';
  o[6].ContextMasters=[ctx('Nanyang Huizhong','person-discussed')];
  o[6].AttributionNote='Source text (禪宗頌古聯珠通集): the verse narrator describes circling the Chan couch in a verse attached to the Nanyang Huizhong case; the following prose case names Huizhong separately.';
  o[7].ContextMasters=[ctx('Dahui Zonggao','section-subject')];
  o[7].AttributionNote='Source text (大慧普覺禪師語錄): the recorder narrates Dahui Zonggao striking the Chan couch and leaving the seat after his address.';
  o[8].ContextMasters=[ctx('Shimen Yuncong','section-subject')];
  o[8].AttributionNote='Source text (古尊宿語錄), Shimen Yuncong record: the recorder narrates the presiding master striking the Chan couch and leaving the seat.';
  o[9].ContextMasters=[ctx('Mayu Baoche','person-described'),ctx('Nanyang Huizhong','respondent')];
  o[9].AttributionNote='Source text (五燈全書(第1卷-第33卷)), Nanyang Huizhong section: the recorder narrates Mayu Baoche circling the Chan couch; Nanyang Huizhong then responds.';
  s.Explanation='A couch or raised seat used at a Chan teaching assembly is a physical furnishing that records make part of public action: Zhaozhou remains on it before a king; Niutou Huizhong, Shishuang Chuyuan, Dahui Zonggao, and Shimen Yuncong strike it or leave it; Magu Baoche circles it. The compound names a real couch or seat. Its Chan force comes from its placement at the teaching seat and from recorded gestures performed on or around it, not from turning the furniture into an abstract symbol.';
  save(x);
}

// 三門 O1: 進/monk, not Foyan.
{
  const x=load('t_2da0e2fc0478'), o=x.e.Senses[0].Occurrences[0];
  unnamedQuestion(o,'Foyan Qingyuan','Source text (古尊宿語錄), Foyan Qingyuan record: the unnamed monastic questioner lists kitchen, storehouse, monastery gate, bell tower, and Buddha hall; Foyan gives the following marked response.');
  save(x);
}

// 開爐 O2-O7: editorial occasion labels, with the presiding section masters retained.
{
  const x=load('t_298f7fdd14bd'), o=x.e.Senses[0].Occurrences;
  const rows=[
    [1,'Zhenjing Kewen','Source text (列祖提綱錄): the occasion-heading recorder introduces Zhenjing Kewen’s furnace-opening hall address; Kewen presides but does not utter the label.'],
    [2,'Baichi Yuanshuo','Source text (百癡禪師語錄): the occasion-heading recorder introduces Baichi Yuanshuo’s following public address.'],
    [3,"Yuan'an Liao","Source text (遠菴僼禪師語錄): the occasion-heading recorder introduces Yuan'an Liao’s following instruction."],
    [4,'Feiyin Tongrong','Source text (費隱禪師語錄): the occasion-heading recorder introduces Feiyin Tongrong’s following hall address.'],
    [5,"Tian'an Sheng","Source text (天岸昇禪師語錄): the occasion-heading recorder introduces Tian'an Sheng’s following evening address."],
    [6,'Hansong Zhicao','Source text (寒松操禪師語錄): the occasion-heading recorder introduces Hansong Zhicao’s following hall address.']
  ];
  for(const [i,name,note] of rows) narrated(o[i],'the occasion-heading recorder',[ctx(name,'section-subject')],'開爐 occurs in an editorial occasion label immediately before the named master’s 上堂, 示眾, or 晚參; it is not inside the master’s spoken turn.',note,'impersonal','editorial occasion heading');
  save(x);
}

// 坐斷: Yuanwu owns T47n1997; recut O6 to the actual incense-address witness.
{
  const x=load('t_74390b40f658'), o=x.e.Senses[0].Occurrences;
  o[0].MasterName='Yuanwu Keqin'; o[0].ContextMasters=[ctx('Yuanwu Keqin','utterer')]; delete o[0].ActorAttribution;
  o[0].AttributionNote='Source text (圓悟佛果禪師語錄): Yuanwu Keqin utters the standalone line; the Song source cannot belong to Ming master Feiyin Tongrong.';
  o[5].Kwic='千巖長禪師初祖忌，拈香：九年面壁，坐斷天下人舌頭；';
  o[5].FromLb='0035a07'; o[5].ToLb='0035a08';
  o[5].AttributionNote='Source text (列祖提綱錄): Qianyan Yuanzhang utters the phrase in his incense address for Bodhidharma’s memorial; duplicated volume/editorial headings have been removed from the stored KWIC.';
  o[5].DraftActorProof={ExactHeadwordClause:'坐斷天下人舌頭',GrammaticalSubject:'Qianyan Yuanzhang',SpeechFrame:'The named 千巖長禪師 incense address contains the exact phrase after 拈香.',FullCaseDecision:'Qianyan Yuanzhang utters the headword in the memorial incense address.'};
  save(x);
}

// 端的: identify Zeng Hui, avoid unsupported verse ownership, and classify 僧問 exactly.
{
  const x=load('t_51a4f3a03bd8'), o=x.e.Senses[0].Occurrences;
  delete o[0].MasterName; o[0].ContextMasters=[ctx('Xuedou Chongxian','respondent')];
  o[0].ActorAttribution=actor('identified-non-master','lay official speech','Zeng Hui','questioner','The section names 修撰曾會居士; 公曰 introduces his headword-bearing question to Xuedou Chongxian.');
  o[0].AttributionNote='Source text (五燈全書(第34卷-第120卷)): Zeng Hui, the named compiling secretary and lay official, asks Xuedou Chongxian whether the case was really penetrated.';
  narrated(o[2],'the anthology verse recorder',[], 'The exact headword occurs in a verse whose personal author is not established by the complete section; Touzi Yiqing must not be inferred from anthology proximity.','Source text (禪宗頌古聯珠通集): the anthology verse recorder owns the headword-bearing verse; the complete section does not prove a named verse author.','reviewed-unnamed','unresolved verse authorship');
  unnamedQuestion(o[7],'Dahui Zonggao','Source text (續燈正統), Dahui Zonggao section: the unnamed monastic questioner asks whether the raised-fist answer is really exact; Dahui is the respondent who then leaves the seat.');
  save(x);
}

// 宗乘 O5: explicit monk questioner; the T51/X80 pair is one transmitted case family.
{
  const x=load('t_32a92c635f49'), s=x.e.Senses[0], o=s.Occurrences[4];
  unnamedQuestion(o,'Letan Changxing','Source text (景德傳燈錄): the unnamed monastic questioner asks what the ultimate matter of the lineage vehicle is; Letan Changxing gives the following marked response.');
  s.Note='Eight reviewed witnesses comprise seven independent case/work families; the T51 and X80 Letan witnesses are parallel transmissions of one case and count once for independence.';
  save(x);
}

// 化主: restore respondent context; headings are impersonal editorial text, not a poet’s utterance.
{
  const x=load('t_5306489d35c6'), o=x.e.Senses[0].Occurrences;
  o[0].ContextMasters=[ctx('Shoushan Xingnian','respondent')];
  o[0].AttributionNote='Source text (古尊宿語錄), Shoushan Xingnian record: the case narrator introduces an unnamed alms officer; Shoushan is the named respondent to the officer’s following question.';
  narrated(o[5],'the poem-heading editor',[ctx('Huilin Zongben','verse-author')],'The exact headword occurs in the editorial title before Huilin Zongben’s verse. Authorship of the verse does not turn its heading into Huilin’s spoken headword occurrence.','Source text (慧林宗本禪師別錄): the poem-heading editor owns the departure-poem title for the alms officer Guang; Huilin Zongben is retained as author of the following verse, not as utterer of the heading.','impersonal','editorial poem heading');
  narrated(o[6],'the poem-heading editor',[],'The exact headword occurs in a poem or occasion heading. The complete section does not establish a documentary appointment or a spoken headword turn.','Source text (雪峰空和尚外集): the poem-heading editor owns the departure-poem title for the Huzhou alms officer Jue; the witness is an editorial heading, not a documented appointment or master utterance.','impersonal','editorial poem heading');
  save(x);
}

// 十二時: remove contaminated 十二時歌 title witness; 幻寄 is the explicit commentator.
{
  const x=load('t_0229ebe0b9e7'), s=x.e.Senses[0];
  s.Occurrences=s.Occurrences.filter(v=>!(v.RelPath==='X/X81/X81n1571.xml'&&v.FromLb==='0424a16'));
  const o=s.Occurrences.find(v=>v.RelPath==='X/X83/X83n1578.xml'&&v.FromLb==='0415a04');
  delete o.MasterName; o.ContextMasters=[ctx('Baozhi','person-discussed')];
  o.ActorAttribution=actor('identified-non-master','named compiler commentary','Huanji','commentator','The paragraph explicitly names Huanji before the continuing comment; Baozhi is discussed rather than speaking.');
  o.AttributionNote='Source text (指月錄): Huanji, the explicitly named commentator, discusses Baozhi’s “twelve” and the all-day phrase; Baozhi is the person discussed.';
  s.Note='Seven reviewed witnesses delimit the all-day expression; a biographical title meaning “Twelve-Hour Song” was excluded as a false standalone witness.';
  s.SourceTexts=[...new Set(s.Occurrences.map(v=>v.RelPath))];
  s.Explanation='The twelve periods are the traditional double-hours that together span a complete day and night. Huangbo Xiyun speaks of relying on nothing throughout them; Xitang Zhizang asks the same all-day question in two parallel transmissions; an unnamed monk puts it to Fayan Wenyi; Maqiaoshan Benkong and Fenyang Shanzhao use the span in their own addresses. Huanji comments on the phrase while discussing Baozhi. A biographical title meaning “Twelve-Hour Song” is not evidence for this standalone sense and has been excluded. The phrase means the whole day, not twelve modern sixty-minute hours.';
  const de=x.w.Entry.Senses[0].DraftEvidence;
  de.OpeningClaimEvidenceKeys=s.Occurrences.map((_,i)=>'o'+(i+1));
  de.ZenBend='Huangbo Xiyun, Xitang Zhizang, an unnamed monk questioning Fayan Wenyi, Maqiaoshan Benkong, and Fenyang Shanzhao deploy the full daily cycle in encounter questions and public address; Huanji separately comments on the wording.';
  de.IndependentWorkIds=(de.IndependentWorkIds||[]).filter(v=>v!=='work:wudeng-quanshu');
  save(x);
}

// Mechanically expressible exact-turn and false-witness canaries.
{
  const p=path.join(root,'fresh-build','semantic-regressions.json'), r=JSON.parse(fs.readFileSync(p));
  r.t_2da0e2fc0478={term:'三門',occurrenceAssertions:[{RelPath:'D/D48/D48n8939.xml',FromLb:'0011a10',mustActorStatus:'reviewed-unnamed',forbiddenMasterNames:['Foyan Qingyuan']}]};
  r.t_298f7fdd14bd={term:'開爐',occurrenceAssertions:[{RelPath:'X/X64/X64n1260.xml',FromLb:'0304b22',mustActorStatus:'impersonal',forbiddenMasterNames:['Zhenjing Kewen']}]};
  r.t_74390b40f658={term:'坐斷天下人舌頭',forbiddenOccurrenceSubstrings:['列祖提綱錄卷第五列祖提綱錄卷第六'],occurrenceAssertions:[{RelPath:'T/T47/T47n1997.xml',FromLb:'0721b14',mustMasterName:'Yuanwu Keqin',forbiddenMasterNames:['Feiyin Tongrong']}]};
  r.t_51a4f3a03bd8={term:'端的',occurrenceAssertions:[{RelPath:'X/X84/X84n1583.xml',FromLb:'0409a06',mustActorStatus:'reviewed-unnamed',forbiddenMasterNames:['Dahui Zonggao']},{RelPath:'X/X82/X82n1571.xml',FromLb:'0003a15',mustActorStatus:'identified-non-master'}]};
  r.t_32a92c635f49={term:'宗乘',occurrenceAssertions:[{RelPath:'T/T51/T51n2076.xml',FromLb:'0252a06',mustActorStatus:'reviewed-unnamed',forbiddenMasterNames:['Letan Changxing']}]};
  r.t_5306489d35c6={term:'化主',occurrenceAssertions:[{RelPath:'X/X73/X73n1450.xml',FromLb:'0089b19',mustActorStatus:'impersonal',forbiddenMasterNames:['Huilin Zongben']}]};
  r.t_0229ebe0b9e7={term:'十二時',forbiddenOccurrenceSubstrings:['十二時歌行世'],occurrenceAssertions:[{RelPath:'X/X83/X83n1578.xml',FromLb:'0415a04',mustActorStatus:'identified-non-master'}]};
  fs.writeFileSync(p,JSON.stringify(r,null,2)+'\n');
}
console.log('repaired eight evidence worksheets; compile required');
