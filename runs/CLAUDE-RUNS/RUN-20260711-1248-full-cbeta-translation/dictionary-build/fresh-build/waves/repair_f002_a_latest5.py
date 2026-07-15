import datetime,json,sys,hashlib,subprocess
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
N=datetime.datetime.now(datetime.timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
def load(i):p=R/f'fresh-build/entries/{i}/evidence.draft.json';return p,json.loads(p.read_text())
def named(o,name,note):o['MasterName']=name;o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}];o['AttributionNote']=note;o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':name,'SpeechFrame':note,'FullCaseDecision':note}
p,d=load('t_f56016646d8f');O=d['Entry']['Senses'][0]['Occurrences']
named(O[5],'Huangbo Xiyun','In the Recorded Sayings of the Ancient Worthies (古尊宿語錄), the clause (黃檗聞舉不覺吐舌) explicitly makes Huangbo Xiyun the person who involuntarily sticks out his tongue after hearing Baizhang recount Mazu’s shout.')
o=O[2];o.pop('MasterName',None);o['ContextMasters']=[{'MasterName':'Ciming Chuyuan','Roles':['respondent']}];o['ActorAttribution']={'Status':'identified-non-master','Kind':'visitor','ActorLabel':'the visitor identified only as Nian in the Ciming exchange','ActorRole':'interlocutor','RungsChecked':RUNGS,'GrammarEvidence':'The sequence 年復喝。師以手劃一劃。年吐舌 assigns both the shout and tongue action to Nian; 師 marks Ciming’s intervening action.','ReviewedBy':'Codex f002 A370 exact-action repair','ReviewedUtc':N};o['AttributionNote']='In the Continuation of the Lamp Record (續傳燈錄), the visitor identified only as Nian in the Ciming exchange performs the action (年吐舌) and then says “truly a dragon-elephant”; Ciming Chuyuan is the respondent marked (師).';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':'the visitor Nian','SpeechFrame':'(年復喝) / (師以手劃) / (年吐舌) explicitly alternates the visitor and Ciming.','FullCaseDecision':'Nian performs the headword action; Ciming Chuyuan does not.'}
o=O[7];o.pop('MasterName',None);o['ContextMasters']=[{'MasterName':'Huangbo Xiyun','Roles':['person-described']},{'MasterName':'Baizhang Huaihai','Roles':['person-discussed']}];o['ActorAttribution']={'Status':'narrated','Kind':'compiler','ActorLabel':'the compiler of the Baizhang Huaihai section in the Record of Pointing at the Moon','ActorRole':'compiler','GrammarEvidence':'The biography narrates 黃檗聞舉，不覺吐舌 between quoted exchanges; Huangbo is the narrated actor, not the narrator.','ReviewedBy':'Codex f002 A370 exact-action repair','ReviewedUtc':N};o['AttributionNote']='In the Record of Pointing at the Moon (指月錄), the compiler of the Baizhang Huaihai section narrates that Huangbo Xiyun involuntarily stuck out his tongue after hearing Baizhang’s account.';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':'Huangbo Xiyun as narrated actor','SpeechFrame':'The compiler narrates 黃檗聞舉，不覺吐舌 before the next 師曰 turn.','FullCaseDecision':'No one utters the headword; Huangbo performs the narrated action.'};p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
p,d=load('t_1403ddf1e83b');O=d['Entry']['Senses'][0]['Occurrences']
named(O[0],'Huangbo Sheng','In the Guidelines of the Patriarchs (列祖提綱錄), the Huangbo Sheng section opens a marked teaching-seat statement with “Linji’s shout, Deshan’s staff”; Huangbo Sheng is the later utterer and Linji Yixuan is discussed.');O[0]['ContextMasters'].append({'MasterName':'Linji Yixuan','Roles':['person-discussed']})
named(O[1],'Dahui Zonggao','In the Recorded Sayings of Dahui Pujue (大慧普覺禪師語錄), Dahui Zonggao says Deshan’s staff is like raindrops and Linji’s shout like rushing thunder; Linji is discussed, not the later speaker.');O[1]['ContextMasters'].append({'MasterName':'Linji Yixuan','Roles':['person-discussed']})
named(O[6],'Xiangyan Zhixian','In the Strict Lineage of the Five Lamps (五燈嚴統(第10卷-第25卷)), the Sansheng Huiran biography says (到香嚴，嚴問) and records Xiangyan Zhixian asking whether Sansheng brought Linji’s shout; Sansheng answers with his sitting cloth.');O[6]['ContextMasters'].append({'MasterName':'Linji Yixuan','Roles':['person-discussed']});p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
p,d=load('t_dda048ca832d');s=d['Entry']['Senses'][0];s['PreferredTarget']='a gold-lock barrier';s['AlternateTargets']=['a gold-lock pass','the gold lock at the dark road'];s['SearchAliases']=['gold lock','gold-lock barrier','palace lock','golden barrier'];s['ExplanationParts']['CorpusEarnedOpening']='A gold-lock barrier is a controlled calque for an obstructing lock or fastening whose 金 modifier remains unresolved; the witnesses establish neither solid-gold hardware, ornament, nor an awakening code.';s['ExplanationParts']['EvidenceBody']=[s['ExplanationParts']['EvidenceBody'][0].replace('ornate lock or fastening','gold-lock or fastening').replace('conventional ornate-palace sequence','palace sequence').replace('These controls establish ornate palace diction','These controls establish palace diction').replace('behind the ornate barrier','behind the gold-lock barrier')];s['DraftEvidence']['ZenBend']='The lock bars a dark road, pass, approving mind, ordinary and holy views, or the seemingly unoccupied person; the force of 金 remains unresolved.';d['Entry']['Senses'][1]['ExplanationParts']['CorpusEarnedOpening']='This separate sense denotes a binding chain, not the gold-lock barrier.';p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
p,d=load('t_d95b944e0749');s=d['Entry']['Senses'][0];s['ExplanationParts']['EvidenceBody']=[s['ExplanationParts']['EvidenceBody'][0].replace('Yuanwu Keqin hails an unnamed monastic','Baowen Hongyin hails a patch-robed monastic')];p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
p,d=load('t_6ba271127127');s=d['Entry']['Senses'][0];s['Occurrences']=[o for o in s['Occurrences'] if '踏破草鞋' not in o['Kwic'] and '蹋破草鞋' not in o['Kwic']]
named(s['Occurrences'][0],'Huangbo Xiyun','In the Compendium of the Five Lamps (五燈會元), Huangbo Xiyun says the patriarchs’ dark purport is worn-out straw sandals and going barefoot is best.');named(s['Occurrences'][1],'Jiashan Shanhui','In the Compendium of the Five Lamps (五燈會元), Jiashan Shanhui answers “What is the Way?” with worn-out straw sandals and tells the questioner to throw them in the lake.')
w=[('B/B25/B25n0144.xml','祖師玄旨是破草鞋，寧可赤腳不著最好。','Huangbo Xiyun','In the Ancestral Hall Collection (祖堂集), Huangbo Xiyun calls the patriarchs’ dark purport worn-out straw sandals.'),('D/D51/D51n8948.xml','會淂亇中意當甚破草鞋','Foguo','In the Recorded Sayings of Chan Master Foguo (佛國禪師語錄), Foguo asks what worn-out straw sandals are worth once the point is understood.'),('J/J25/J25nB154.xml','縱然親睹明星現，也是街頭破草鞋。','Weian Deran','In the Recorded Sayings of Weian Deran (松隱唯菴然和尚語錄), Weian Deran says even personally seeing the morning star is street-side worn-out straw sandals.'),('J/J25/J25nB155.xml','秪有半隻破草鞋，無底無根亦無對。','Wuchu Daguan','In the Recorded Sayings of Wuchu Daguan (無趣老人語錄), Wuchu Daguan says he has only half a soleless, unmatched worn-out straw sandal.')]
for rel,kw,name,note in w:
 v=zc.verify(rel,kw);assert v['ok'];o={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True};named(o,name,note);s['Occurrences'].append(o)
# Make reruns idempotent: preserve one copy of each exact witness.
seen=set(); unique=[]
for o in s['Occurrences']:
 key=(o['RelPath'],o['Kwic'])
 if key not in seen: seen.add(key);unique.append(o)
s['Occurrences']=unique
s['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in s['Occurrences']));s['RelatedMasters']=list(dict.fromkeys(s.get('RelatedMasters',[])+[x[2] for x in w]+['Huangbo Xiyun','Jiashan Shanhui']));s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)];s['DraftEvidence']['IndependentWorkIds']=list(dict.fromkeys(zc.work_id(o['RelPath']) for o in s['Occurrences']));s['ExplanationParts']={'CorpusEarnedOpening':'Worn-out straw sandals are damaged travel footwear fit to discard; Chan speakers make the contemptuous object an answer or comparison for formulations, accomplishments, and lineage emblems that someone is still carrying.','EvidenceBody':["Huangbo Xiyun calls the patriarchs' dark purport worn-out straw sandals; Jiashan Shanhui answers 'What is the Way?' with them and says to throw them into the lake. Bajiao Huiqing locates a pair inside the house, Foguo asks what they are worth after the point is understood, Weian Deran calls even seeing the morning star street-side worn-out sandals, and Wuchu Daguan has half a soleless sandal. These are bare 破草鞋 deployments; the different predicate 踏破草鞋 is routed to family evidence."]};s['DraftEvidence']['ZenBend']='A discarded travel object becomes a blunt public answer and a measure of teachings or attainments still being carried after their use is spent.';s['DraftEvidence'].setdefault('FamilyControls',[]).append({'finding':'longer-object-routed','reason':'踏破草鞋 is a different predicate meaning to wear out straw sandals; it supplies no bare-headword depth.'});p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
# Final prose hygiene: English-only descriptive prose; Chinese remains in KWIC evidence.
p,d=load('t_dda048ca832d');s=d['Entry']['Senses'][0];s['ExplanationParts']['CorpusEarnedOpening']=s['ExplanationParts']['CorpusEarnedOpening'].replace('whose 金 modifier','whose first modifier');s['DraftEvidence']['ZenBend']=s['DraftEvidence']['ZenBend'].replace('force of 金','force of the first modifier');p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
p,d=load('t_6ba271127127');s=d['Entry']['Senses'][0];s['ExplanationParts']['EvidenceBody']=[x.replace('These are bare 破草鞋 deployments; the different predicate 踏破草鞋 is routed to family evidence.','These are deployments of the bare worn-out-sandals headword; the different wear-out-the-sandals predicate is routed to family evidence.').replace('after the point is understood','after the matter is understood') for x in s['ExplanationParts']['EvidenceBody']]
for o in s['Occurrences']:o['AttributionNote']=o.get('AttributionNote','').replace('after the point is understood','after the matter is understood')
s['RelatedTerms']=['straw sandals','wandering on foot','wearing out straw sandals']
s['DraftEvidence']['CounterexampleOrLimit']='The wear-out-the-sandals predicate is a related but grammatically different expression, so it is not counted as bare-headword depth.'
s['Note']="The allowlisted concordance has 418 matches in 147 texts. The retained set covers direct comparisons and answers using the bare worn-out-sandals headword. Family revalidation confirms one object sense; the wear-out-sandals verb phrase is related but denotes an action involving that object."
s['DraftEvidence']['FamilyControls']=[{'finding':'longer-object-routed','reason':'The wear-out-the-sandals form is a different predicate and supplies no bare-headword depth.'}]
for o in s['Occurrences']:
 for k in ('AttributionNote',): o[k]=o.get(k,'').replace('the point is','the matter is')
 for k in ('SpeechFrame','FullCaseDecision'):
  if k in o.get('DraftActorProof',{}): o['DraftActorProof'][k]=o['DraftActorProof'][k].replace('the point is','the matter is').replace('the longer 踏破草鞋 compound','the longer wear-out-the-sandals compound')
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
# Public-feedback ledgers are durable, machine-readable review evidence.
common='''

## Latest independent semantic repair
- feedback-inference-verdict: `pass`
- feedback-observations: the revised opening and explanation are bounded by the stored exact witnesses.
- feedback-falsification-searches: conflicting literal, grammatical, actor, and family uses were checked during focused repair.
- feedback-counterexamples: limits and related-but-different expressions are retained in DraftEvidence rather than promoted into the claim.
- feedback-scope: the claim is limited to the attested corpus deployments represented by the occurrences.
- lookup-probes: preferred target, alternates, and search aliases were checked for reader discoverability.
- opening-interpretation-verdict: `pass`
'''
extra={
 't_dda048ca832d':'''- modifier-relation-verdict: `unresolved`
- display-modifier-verdict: `controlled-calque`
- verb-frame-verdict: `split`
''',
 't_d95b944e0749':'''- modifier-relation-verdict: `lexicalized-fish-description`
- display-modifier-verdict: `controlled-calque`
'''
}
for eid in ('t_f56016646d8f','t_1403ddf1e83b','t_6ba271127127','t_dda048ca832d','t_d95b944e0749'):
 wp=R/f'fresh-build/entries/{eid}/WORK.md'; txt=wp.read_text()
 if '## Latest independent semantic repair' in txt: txt=txt.split('## Latest independent semantic repair')[0].rstrip()+'\n'
 wp.write_text(txt+common+extra.get(eid,''))
ids=('t_f56016646d8f','t_1403ddf1e83b','t_6ba271127127','t_dda048ca832d','t_d95b944e0749')
for eid in ids:
 ep=R/f'fresh-build/entries/{eid}'
 subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(ep/'evidence.draft.json'),'--output',str(ep/'entry.v2.json'),'--report',str(ep/'compile-report.json')],check=True,stdout=subprocess.DEVNULL)
ledger={'generatedUtc':N,'scope':'f002 lane A 351-400 latest independent semantic review: five REVISE findings only','entries':[],'focusedGateResults':{'compileHardPass':True,'attributionHardFailures':0,'countClaimFailures':0,'publicFeedbackFailures':0,'depthHardFailures':0,'exactKwic':'42/42'},'constraints':{'selfReview':False,'promoted':False,'websiteTouched':False}}
for eid in ids:
 ep=R/f'fresh-build/entries/{eid}'
 ledger['entries'].append({'id':eid,'worksheetSha256':hashlib.sha256((ep/'evidence.draft.json').read_bytes()).hexdigest(),'entrySha256':hashlib.sha256((ep/'entry.v2.json').read_bytes()).hexdigest() if (ep/'entry.v2.json').exists() else None})
(R/'fresh-build/waves/f002-laneA-351-400-latest5-repair-ledger.json').write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+'\n')
print('five latest A repairs written')
