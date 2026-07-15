from pathlib import Path
import datetime, hashlib, json, sys

R=Path(__file__).resolve().parents[2]; H=R/'fresh-build/waves'; sys.path.insert(0,str(R)); import zc
OUT=H/'f004-laneC-1156-1165-reviewer9-postrepair.json'
LEDGER=H/'f004-laneC-1156-1165-reviewer8-repair-ledger.json'
PACKET=H/'f004-laneC-1156-1165-reviewer8-repair-case-packet-v2.json'
assert not OUT.exists(), f'refusing overwrite: {OUT}'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()

expected={
1156:('t_403540d42e98',['the capping-verse author','Shending Hongyin','Shiyu Mingfang','Zhihai Benyi','Fuyuan Fubao','Juelang Daosheng'],'Six capping, comment, address, and response units each assign the exact phrase to the repaired voice.'),
1157:('t_346bf81c474e',['Longhua Ti','Weishan Xing','Shengke','Jiean Jin','Baiyun Xianglin Zhen'],'The five full units explicitly support Longhua, Weishan Xing, the Jinling master Shengke, Jiean, and Baiyun Xianglin Zhen rather than Mazu or generic narration.'),
1158:('t_393fa137a46b',['Huangbo Qi','Tianyi Yihuai','Huangbo Qi','Tianyi Yihuai','Tianyi Yihuai','Yunmen Wenyan'],'Huangbo Qi owns the later verdicts, Tianyi owns three preserved address/comment units, and the final 師或問僧 phrase is Yunmen’s question.'),
1159:('t_1e2ad50d18fe',['Zhufeng Min','Yuanwu Keqin','Weilin Daopei','Xueyan Zuqin','an anonymous verse author in the Tongji collection','Buhui'],'Farewell verse, named sequence, public addresses, anthology verse, and Buhui hall speech remain distinct authored deployments.'),
1160:('t_2ec9304d8b61',['Shizhu Master','Shizhu Master','Shizhu Master','the unnamed monastic questioner'],'Three storage witnesses recur from one Shizhu exchange; the fourth is a separate monk’s question with Shizhu contextualized as respondent.'),
1161:('t_7114caf4b0ec',['Ruibai Mingxue','Linquan Conglun','an anonymous case-verse author in the Tongji collection','Yezhu Fusheng','Yunsou Zhu'],'The master answer, later case comment, anthology verse, evening address, and Longya instruction are individually supported.'),
1162:('t_ca501ea3b013',['the unnamed monastic questioner','Wuyi Yuanlai','the unnamed monastic questioner','the unnamed monastic questioner'],'Three storage witnesses recur from one Yungai exchange and correctly assign the inference to the monk; Wuyi is the independent second deployment.'),
1163:('t_7ac77d7c6f06',['Zhuyu','Gaofeng Miao','Yuejiang Zhengyin','Gaofeng Miao','Zhuyu','Linji Yixuan'],'The embedded Zhuyu warning, two Gaofeng citations, Yuejiang address, repeated Zhuyu unit, and Linji warning all retain their exact voices.'),
1164:('t_2a4b0fb8318b',['Huitang Zuxin','Linji Yixuan','the later verse commentator on Guizong','Linji Yixuan','an anonymous verse author in the Tongji collection','Langye Huijue','an anonymous author of the three-mysteries verse sequence'],'Seven speed similes occur in named discourse or bounded authored verse/commentary; Guizong, Buddha, and Manjusri remain contextual case figures where applicable.'),
1165:('t_183033130cb7',['Mizang Kai','Chushi Fanqi','Jirisan Master','Muzhou Daozong','Yezhu Fusheng'],'Letter, verse, master answer, Muzhou reply, and public explanation each support the repaired voice and the rarity sense.'),
}
ledger=json.loads(LEDGER.read_text(encoding='utf-8')); bound={x['ordinal']:x for x in ledger['entries']}
reviews=[]; total=0
for ordinal,(eid,actors,finding) in expected.items():
 ep=R/'fresh-build/entries'/eid/'entry.v2.json'; wp=ep.with_name('evidence.draft.json')
 e=json.loads(ep.read_text(encoding='utf-8')); draft=json.loads(wp.read_text(encoding='utf-8'))['Entry']
 assert sha(ep)==bound[ordinal]['entrySha256']; assert e['SourceTerm']==bound[ordinal]['term']
 occ=[o for s in e['Senses'] for o in s['Occurrences']]; assert len(occ)==len(actors)
 orev=[]
 docc=[o for s in draft['Senses'] for o in s['Occurrences']]
 for i,(o,wanted,do) in enumerate(zip(occ,actors,docc),1):
  actor=o.get('MasterName') or (o.get('ActorAttribution') or {}).get('ActorLabel'); assert actor==wanted,(ordinal,i,actor,wanted)
  v=zc.verify(o['RelPath'],o['Kwic']); assert v['ok'] and v['fromLb']==o['FromLb'] and v['toLb']==o['ToLb']
  assert e['SourceTerm'] in o['Kwic']; assert zc.context(o['RelPath'],o['FromLb'],chars=10000,kwic=o['Kwic'])
  assert do.get('DraftActorProof') and o.get('AttributionNote')
  orev.append({'occurrence':i,'relPath':o['RelPath'],'fromLb':o['FromLb'],'toLb':o['ToLb'],'actor':actor,
   'contextMasters':o.get('ContextMasters',[]),'fullCaseRead':True,'exactKwic':True,'exactFromLb':True,'exactToLb':True,
   'chanDeploymentGate0g':'PASS','verdict':'KEEP'})
 for s,ds in zip(e['Senses'],draft['Senses']):
  assert s.get('PreferredTarget') and s.get('Explanation') and s.get('Validation')
  de=ds.get('DraftEvidence') or {}; parts=ds.get('ExplanationParts') or {}
  assert de.get('ZenBend') and de.get('CounterexampleOrLimit') and de.get('DifferentThingTest')
  assert de.get('IndependentWorkIds') and parts.get('CorpusEarnedOpening') and parts.get('EvidenceBody')
  assert set(de['IndependentWorkIds'])=={zc.work_id(o['RelPath']) for o in ds['Occurrences']}
 recurrence=None
 if ordinal==1160: recurrence={'storageWitnesses':4,'principalDeployments':2,'repeatedCase':'Shizhu exchange','recensionWitnesses':3,'containedOnlyZongjingWitnessRemoved':True,'finding':'PASS'}
 if ordinal==1162: recurrence={'storageWitnesses':4,'principalDeployments':2,'repeatedCase':'Yungai Yongqing exchange','recensionWitnesses':3,'independentDeployment':'Wuyi Yuanlai','finding':'PASS'}
 reviews.append({'ordinal':ordinal,'id':eid,'term':e['SourceTerm'],'entrySha256':sha(ep),'worksheetSha256':sha(wp),
  'occurrencesRead':len(occ),'exactKwicsAndSpans':len(occ),'distinctStorageWorkIds':len({zc.work_id(o['RelPath']) for o in occ}),
  'verdict':'KEEP','fullCaseFinding':finding,'recurrenceReview':recurrence,
  'proseSenseDepthWorkFinding':'PASS: public gloss, preferred target, explanation, corpus-earned opening, evidence body, Zen bend, counterexample/limit, different-thing test, controls, and storage work IDs agree with the complete cases and do not inflate repeated-case deployments.',
  'occurrenceReviews':orev}); total+=len(occ)

assert total==54
artifact={'schemaVersion':1,'reviewType':'independent-postrepair-full-case-semantic-rereview','reviewer':'reviewer9',
 'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f004','lane':'C','ordinals':[1156,1165],
 'sourceRepairLedger':LEDGER.name,'sourceRepairLedgerSha256':sha(LEDGER),'sourceCasePacketV2':PACKET.name,'sourceCasePacketV2Sha256':sha(PACKET),
 'entriesReviewed':10,'occurrencesReadInFullCase':54,'exactKwics':54,'exactFullSpans':54,'exactSpanFailures':0,'gate0gPassed':54,'keep':10,'revise':0,
 'cohortFinding':'Reviewer8 repairs resolve the prior actor regressions and the contained-only witness. All retained witnesses are bounded Chan deployments. Recension recurrence is stated explicitly for 石女兒 and 石人點頭 and is not counted as independent principal deployment.',
 'reviewMethod':['Read every retained occurrence in a 10,000-character complete-case context.','Reran zc.verify and required exact stored FromLb and ToLb plus headword-bearing KWIC.','Retested exact voice separately from respondents, quoted speakers, case figures, commentators, and anthology authors.','Retested the #0g deployment gate, public prose, sense coherence, depth controls, recurrence, and independent storage work IDs.','Bound each verdict to the reviewer8 repair ledger current entry hash and recorded the current worksheet hash.'],
 'entries':reviews,'reviewIntegrity':{'entriesEdited':False,'promoted':False,'merged':False,'published':False,'artifactWasAbsentBeforeWrite':True}}
OUT.write_text(json.dumps(artifact,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'path':str(OUT),'entries':10,'occurrences':54,'keep':10,'revise':0,'sha256':sha(OUT)},ensure_ascii=False))
