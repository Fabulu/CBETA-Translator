#!/usr/bin/env python3
"""Explicit evidence-first article for Lane B position 014: 不自在."""
import datetime,json,subprocess,sys
from pathlib import Path
DB=Path(__file__).resolve().parent.parent;ROOT=DB/'fresh-build';sys.path.insert(0,str(DB));import zc
TERM='不自在';BASE='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a';NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();M=json.loads((DB/'maintenance/investigation-next300-construction-lane-b.json').read_text());ID=next(x['id'] for x in M['rows'] if x['headword']==TERM)
ROWS=[
('L/L154/L154n1640.xml','chan:miyun-yuanwu-yulu','故凡作意則不自在','Recorded Sayings of Chan Master Miyun Yuanwu','Miyun Yuanwu','Miyun Yuanwu says deliberate contrivance brings lack of freedom; the immediately following clauses say lack of freedom prevents quick responsiveness and becomes obstruction.'),
('L/L154/L154n1640.xml','chan:miyun-yuanwu-yulu','復清都史居士手教云不知不覺有一種不自在隱隱胸次中者其病有四','Recorded Sayings of Chan Master Miyun Yuanwu','Miyun Yuanwu','Replying to Layman Shi of Qingdu, Miyun Yuanwu quotes his report of a subtle unease in the chest and begins a four-part diagnosis of it.'),
('B/B25/B25n0145.xml','work:B25n0145','自在不成人成人不自在莫隨眼底貪瞋癡換却如今好皮袋','Extended Record of Zhongfeng','Zhongfeng Mingben','Zhongfeng Mingben quotes the warning that ease alone does not make a person, while becoming a person is not easy or free, then warns against trading away the present body through greed, anger, and folly.'),
('M/M59/M59n1540.xml','chan:dahui-pushuo','曰恁麼則某甲亦得自在去也南曰脚下鞋甚處得來曰廬山七百錢唱得南曰何曾得自在曰何曾不自在南異之','General Discourses of Chan Master Dahui Pujue','Zhenjing Kewen','In the case raised by Dahui, Zhenjing Kewen answers the challenge from Huanglong Huinan, “when were you ever free?” with “when were you ever not free?” and Huinan finds this remarkable.'),
('T/T47/T47n1998A.xml','work:T47n1998A','苟能於經教及古德入道因緣中。不起第二念。直下知歸。則於自境界他境界。無不如意。無不自在者。','Recorded Sayings of Chan Master Dahui Pujue','Dahui Zonggao','Dahui Zonggao says that if no second thought arises over records and old encounters and one directly knows the return, neither the person’s own nor another person’s circumstances are contrary to wish or lacking freedom.'),
('T/T47/T47n2000.xml','work:T47n2000','法眼云。在心內。地藏云。行脚人置者一塊石。在心頭多少不自在。','Extended Record of Chan Master Hongzhi Zhengjue','Luohan Guichen','In the raised case, after Fayan puts a stone inside mind, Luohan Guichen asks how constrained a traveling monk becomes by placing that stone on his mind.'),
]
def main():
 occ=[]
 for rel,work,kwic,label,master,decision in ROWS:
  v=zc.verify(rel,kwic);assert v.get('ok'),(rel,v)
  ctx=[{'MasterName':master,'Roles':['utterer']}]
  if master == 'Zhenjing Kewen':
   ctx.append({'MasterName':'Huanglong Huinan','Roles':['questioner','interlocutor']})
  occ.append({'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kwic,'MasterName':master,'Curated':True,'AttributionNote':f'Source record ({rel}). {label}: {decision}','ContextMasters':ctx,'DraftActorProof':{'ExactHeadwordClause':kwic,'GrammaticalSubject':master,'SpeechFrame':decision,'FullCaseDecision':decision}})
 s={'SenseKey':None,'MasterName':None,'PreferredTarget':'not free','AlternateTargets':['constrained','not at ease','uncomfortable'],'SearchAliases':['not free','constrained','not at ease','uneasy','uncomfortable'],'Status':'preferred','Validation':'multi-source','Note':'Fresh concordance: 158 exact hits in 85 files representing 81 works. Six full turns across five independent works preserve causal definition, reported unease, warning, reversal, positive contrast, and public-case burden.','Occurrences':occ,'ClaimAnchors':[],'SourceTexts':list(dict.fromkeys(x[0] for x in ROWS)),'RelatedMasters':['Miyun Yuanwu','Zhongfeng Mingben','Zhenjing Kewen','Dahui Zonggao','Luohan Guichen'],'RelatedTerms':['自在','作意','拘滯'],'ExplanationParts':{'CorpusEarnedOpening':'Not free names constraint: inability to move or respond readily, or an inward unease that does not let a person rest. Chan records trace it to deliberate contrivance and to carrying an asserted object “on the mind,” while a public case reverses the complaint by asking when one was ever not free.','EvidenceBody':['Miyun Yuanwu gives the clearest causal chain: deliberate contrivance leads to not being free, which prevents quick response and becomes obstruction. In a letter he also takes up a layman’s report of a subtle unease in the chest and analyzes four causes.','Luohan Guichen turns the word into a public test. When Fayan locates a stone inside mind, Guichen asks how constrained a traveler becomes by carrying that stone on his mind.','The record also preserves reversals and contrasts. Zhenjing Kewen answers “when were you ever not free?” after Huanglong Huinan challenges his claimed freedom, while Dahui describes circumstances in which neither the person’s own nor another person’s situation lacks freedom.','The inward discomfort and restriction on response remain one state of constraint rather than different things. The corpus supplies no basis for turning the adjective into a named technical category or permanent human condition.']},'DraftEvidence':{'OpeningClaimEvidenceKeys':['o1','o2','o3','o4','o5','o6'],'ZenBend':'An ordinary lack of ease becomes a public diagnostic: masters link it to contrivance, delayed response, and carrying a proposition like a stone, then reverse claims about when freedom is present.','CounterexampleOrLimit':'The word does not name one permanent state; the reversal by Kewen and the positive contrast from Dahui prevent a one-sided claim.','DifferentThingTest':{'Decision':'one-thing','ComparedThings':['inward unease','constraint on movement or response'],'Reason':'Both are observable forms of not being free; the contexts do not establish separate referents.'},'AliasRationale':'Free, constrained, ease, uneasy, and uncomfortable cover the English state without importing a technical category.','ModifierControls':[{'finding':'not-applicable','reason':'The negative prefix modifies 自在; no material or color relation occurs.'}],'FamilyControls':[{'finding':'checked','reason':'自在 provides the direct positive contrast; 作意 and 拘滯 are causes/effects, not synonyms.'}],'IndependentWorkIds':['chan:miyun-yuanwu-yulu','work:B25n0145','chan:dahui-pushuo','work:T47n1998A','work:T47n2000']}}
 data={'SchemaVersion':1,'Entry':{'Id':ID,'SourceTerm':TERM,'CorpusBaselineSha256':BASE,'CreatedBy':'Codex investigation-next300 Lane B explicit author','WrittenUtc':NOW,'Senses':[s]}};out=ROOT/'entries'/ID;out.mkdir(parents=True,exist_ok=True);d=out/'evidence.draft.json';d.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')
 (out/'WORK.md').write_text('''# 不自在 — construction Lane B position 014

- 158 hits / 81 works; six full turns across five works.
- Deployment: causal chain, reported inward unease, warning, reversal, positive contrast, stone-on-mind case.
- One state of constraint under bodily, responsive, and conversational predicates.

feedback-inference-verdict: Not being free is constraint or unease, diagnosed through contrivance, delayed response, and a carried proposition.
feedback-observations: contrivance -> not free -> not quick -> obstructed; chest unease; stone on mind; when ever not free; positive contrast.
feedback-falsification-searches: bodily discomfort; freedom contrast; 作意; 拘滯; permanent-state claims; contradictory reversals.
feedback-counterexamples: Guizong and Dahui prevent a permanent or uniformly negative category.
feedback-scope: corpus-wide adjective.
lookup-probes: not free; constrained; not at ease; uneasy; uncomfortable.
opening-interpretation-verdict: direct causal predicates and case comparison license the state of constraint.
sense-target-distinguishability: one state; inward and responsive manifestations do not make different things.
''')
 p=subprocess.run([sys.executable,str(DB/'compile_evidence_draft.py'),str(d),'--output',str(out/'entry.v2.json'),'--report',str(out/'evidence-compile-report.json')],text=True,capture_output=True);assert p.returncode==0,p.stdout+p.stderr
if __name__=='__main__':main()
