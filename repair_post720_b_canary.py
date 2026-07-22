#!/usr/bin/env python3
import json
from pathlib import Path
H=Path('/mnt/c/programmieren/mergeworkcbeta/cbeta-translator/runs/CLAUDE-RUNS/RUN-20260711-1248-full-cbeta-translation/dictionary-build')
MAP={
't_5f532706138e':{0:{0:'Yuansou Xingduan',3:'Dahui Zonggao',4:'Shixi Xinyue',5:'Yuejiang Zhengyin'}},
't_2f97445940bc':{0:{0:'Xuedou Chongxian',1:"Ying'an Tanhua",2:'Mingzhao Deqian',4:'Yuanwu Keqin',5:'Hongzhi Zhengjue'}},
't_b10c9e660874':{0:{2:'Xiangtian Jinian Jingxian',3:'Zhuanyu Guanheng'}},
}
LABELS={'Yuansou Xingduan':'Outline Record of the Successive Patriarchs','Dahui Zonggao':'Recorded Sayings of Chan Master Dahui Pujue','Shixi Xinyue':'Recorded Sayings of Chan Master Shixi Xinyue','Yuejiang Zhengyin':'Recorded Sayings of Chan Master Yuejiang Zhengyin','Xuedou Chongxian':'Blue Cliff Record','Ying\'an Tanhua':'Forest of Models of the Chan School','Mingzhao Deqian':'Outline Record of the Successive Patriarchs','Yuanwu Keqin':'Recorded Sayings of Chan Master Yuanwu Foguo','Hongzhi Zhengjue':'Book of Serenity','Xiangtian Jinian Jingxian':'Recorded Sayings of Chan Master Xiangtian Jinian','Zhuanyu Guanheng':'Recorded Sayings of Zhuanyu Guanheng','Juelang Daosheng':'Recorded Sayings of Chan Master Juelang Daosheng','Jieweizhou Xingzhou':'Recorded Sayings of Chan Master Jiewei Xingzhou'}
def bind(o,name,term):
 o['MasterName']=name;o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}]+[x for x in o.get('ContextMasters',[]) if x.get('MasterName')!=name]
 o['AttributionNote']=f"Source record ({o['RelPath']}). {LABELS[name]}: {name} is the exact utterer of the headword-bearing clause."
 o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':name,'SpeechFrame':f'The complete case or authored section assigns the exact {term} clause to {name}.','FullCaseDecision':f'{name} owns the exact headword-bearing turn; title-only fallback was not used.'}
for eid,sm in MAP.items():
 out=H/'fresh-build/entries'/eid
 for fn in ('entry.v2.json','evidence.draft.json'):
  p=out/fn;d=json.load(open(p));e=d.get('Entry',d)
  for si,om in sm.items():
   for oi,name in om.items():bind(e['Senses'][si]['Occurrences'][oi],name,e['SourceTerm'])
  if eid=='t_2f97445940bc':
   o=e['Senses'][0]['Occurrences'][3]
   o['MasterName']=None
   o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'verse author unresolved','ActorLabel':'an unnamed verse author','ActorRole':'verse-author','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':'The headword occurs in an authored verse following the named case, but the full section does not label the verse author.','ReviewedBy':'Codex post720 B canary revision full-case review','ReviewedUtc':'2026-07-17T16:00:00+00:00'}
   o['AttributionNote']='Source record (C/C078/C078n1720.xml). Linked Collection of the Chan School’s Verses on Old Cases (禪宗頌古聯珠通集): The verse author is unnamed after the six-rung review and owns the exact headword-bearing verse line.'
   o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':'an unnamed verse author','SpeechFrame':'The exact clause is a verse line, not anonymous narrative or a generic passage voice.','FullCaseDecision':'All six attribution rungs were checked; no personal name is attached to this verse.'}
  if eid=='t_b10c9e660874':
   e['Senses'][0]['RelatedMasters']=['Xiangtian Jinian Jingxian' if x=='Xiangtian Jinian' else x for x in e['Senses'][0].get('RelatedMasters',[])]
  p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')

# 法網: split the legal net from the teaching-net deployment and bind exact actors.
eid='t_2d9781afa9a0';out=H/'fresh-build/entries'/eid
for fn in ('entry.v2.json','evidence.draft.json'):
 p=out/fn;d=json.load(open(p));e=d.get('Entry',d);base=e['Senses'][0]
 if len(e['Senses'])==2:
  old=[e['Senses'][0]['Occurrences'][0],e['Senses'][1]['Occurrences'][0],e['Senses'][0]['Occurrences'][1],e['Senses'][1]['Occurrences'][1]]
 else:
  old=base['Occurrences']
 # Imperial edict speaker is explicitly identified by the edict and first-person 朕.
 emp=old[0];emp['MasterName']=None;emp['ActorAttribution']={'Status':'identified-non-master','Kind':'imperial edict speaker','ActorLabel':'Yongzheng Emperor','ActorRole':'case-figure','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':'The appended edict uses first-person 朕 in the exact clause; the imperial source and edict frame identify the issuing Yongzheng Emperor.','ReviewedBy':'Codex post720 B canary revision full-case review','ReviewedUtc':'2026-07-17T16:00:00+00:00'}
 emp['AttributionNote']='Source record (X/X68/X68n1319.xml). Imperially Selected Recorded Sayings: the Yongzheng Emperor threatens to cast a fame-seeking offender into the net of the law.'
 emp['DraftActorProof']={'ExactHeadwordClause':emp['Kwic'],'GrammaticalSubject':'Yongzheng Emperor','SpeechFrame':'First-person 朕 in the appended imperial edict governs the exact clause.','FullCaseDecision':'The issuing Yongzheng Emperor is the exact non-master speaker.'}
 monk=old[1];monk['AttributionNote']='Source record (C/C077/C077n1710.xml). Recorded Sayings of Ancient Venerable Masters: an unnamed monk asks why a boundless teaching net still divides delusion and awakening.'
 bind(old[2],'Juelang Daosheng','法網');bind(old[3],'Jieweizhou Xingzhou','法網')
 old[3]['AttributionNote']='Source record (J/J28/J28nB205.xml). Recorded Sayings of Chan Master Jiewei Xingzhou: Jieweizhou Xingzhou authors the verse that spreads the teaching net over the young mouth.'
 legal=[emp,old[2]];teaching=[monk,old[3]]
 common={k:base.get(k) for k in ('SenseKey','MasterName','Status')};
 s1={**common,'PreferredTarget':'the net of the law','AlternateTargets':['legal net'],'SearchAliases':['the net of the law','legal net'],'Validation':'multi-source','Note':'The imperial edict and night-patrol comparison use a judicial net that catches or threatens an offender.','Occurrences':legal,'SourceTexts':[o['RelPath'] for o in legal],'RelatedMasters':['Juelang Daosheng'],'RelatedTerms':[],'ClaimAnchors':[]}
 s2={**common,'PreferredTarget':'the teaching net','AlternateTargets':['net of the teaching'],'SearchAliases':['the teaching net','net of the teaching'],'Validation':'multi-source','Note':'An interview calls the teaching net boundless, while a verse depicts it being spread; these clauses do not use the edict’s judicial threat.','Occurrences':teaching,'SourceTexts':[o['RelPath'] for o in teaching],'RelatedMasters':['Jieweizhou Xingzhou'],'RelatedTerms':[],'ClaimAnchors':[]}
 for s,opening,limit,work_ids in [(s1,'The net of the law catches an offender in an imperial threat and a night-patrol comparison.','These judicial predicates do not define the boundless teaching-net question.',['work:X68n1319','work:J25nB174']),(s2,'The teaching net is called boundless in a direct question and spread in a verse.','These teaching deployments are separated from the imperial legal threat.',['work:guzunsu-yulu','work:J28nB205'])]:
  body=[limit];s['ExplanationParts']={'CorpusEarnedOpening':opening,'EvidenceBody':body};s['Explanation']=' '.join([opening,*body]);s['DraftEvidence']={'OpeningClaimEvidenceKeys':['o1','o2'],'ZenBend':opening,'CounterexampleOrLimit':limit,'DifferentThingTest':{'Decision':'different-thing','ComparedThings':['judicial net','teaching net'],'Reason':'The edict/night-patrol predicates and boundless/spreading predicates activate different referents.'},'SenseTargetDistinguishability':'The judicial net catches or threatens an offender; the teaching net is boundless or spread as a doctrinal instrument. The independently anchored predicates identify different referents.','AliasRationale':'The alternate preserves the same attested net relation in natural English.','ModifierControls':[{'finding':'checked','reason':opening}],'FamilyControls':[{'finding':'checked','reason':limit}],'IndependentWorkIds':work_ids}
 e['Senses']=[s1,s2];p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
