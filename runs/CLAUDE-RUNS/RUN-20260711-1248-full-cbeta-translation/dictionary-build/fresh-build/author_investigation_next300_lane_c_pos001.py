#!/usr/bin/env python3
"""Explicitly reviewed Lane C position 1 authoring serialization (no compile/install)."""
import datetime,json,sys
from pathlib import Path
DB=Path(__file__).resolve().parent.parent;sys.path.insert(0,str(DB));import zc
BASE='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a'; NOW=datetime.datetime.now(datetime.timezone.utc).isoformat()
ID='t_4d77cb50e67f'; TERM='破顏微笑'
ROWS=[
('J/J34/J34nB311.xml','Juelang Daosheng','Complete Record of Chan Master Juelang Daosheng of Tianjie (天界覺浪盛禪師全錄)','忽於糞掃堆頭拾得一個泥彈子，於靈山會上百萬眾中驀爾拈出，獨迦葉不知好惡，破顏微笑，特地被渠將眼換去，卻道：『吾有正法眼藏付囑於汝，勿令斷絕。』','In a marked Vesak hall address, Juelang Daosheng retells the raised-flower scene, names Kashyapa as the one who breaks into a smile, and immediately interrogates the reported handover.'),
('X/X71/X71n1419.xml','Yuansou Xingduan','Recorded Sayings of Chan Master Yuansou Xingduan (元叟行端禪師語錄)','上堂，舉世尊一日以兜羅綿手舉金色波羅華普示大眾，時迦葉破顏微笑，世尊云：吾有正法眼藏、涅槃妙心付囑於汝。師云：一盲引眾盲，相牽入火坑。','Yuansou Xingduan raises the case in a hall address and follows it with his own hostile verdict, calling it one blind person leading a crowd of blind people.'),
('X/X72/X72n1435.xml','Wuyi Yuanlai','Extended Record of Chan Master Wuyi Yuanlai (無異元來禪師廣錄)','迦葉得之，破顏微笑，便云：覓我者是汝我。阿難得之，結集聞持，副二傳化。','Wuyi Yuanlai places Kashyapa breaking into a smile in a sequence of what successive figures “obtained,” then gives the words assigned to Kashyapa.'),
('J/J26/J26nB182.xml','Wanru Tongwei','Recorded Sayings of Chan Master Wanru Tongwei (萬如禪師語錄)','所以靈鷲拈花，當時百萬人天悉皆罔措，惟迦葉頭陀破顏微笑，故有正法眼藏涅槃妙心付囑之說。','Wanru Tongwei contrasts the silent assembly with Kashyapa breaking into a smile and identifies the handover account as what follows from that response.'),
('J/J36/J36nB359.xml','Baiyu Jingsi','Recorded Sayings of Chan Master Baiyu Jingsi (百愚禪師語錄)','昔世尊在靈山會上拈花示眾，金色頭陀破顏微笑，世尊將正法眼藏、涅槃玅心付囑與彼。','Baiyu Jingsi retells the raised-flower action and makes Kashyapa’s smile the response preceding the reported charge.'),
('T/T47/T47n1998A.xml','Dahui Zonggao','Recorded Sayings of Chan Master Dahui Pujue (大慧普覺禪師語錄)','末後臨般涅槃。於人天百萬眾前。拈華普示。唯金色頭陀破顏微笑。遂云。吾有正法眼藏涅槃妙心。分付於汝。','Dahui Zonggao contrasts Kashyapa alone breaking into a smile with the vast audience, then quotes the reported charge to him.'),
]
occs=[]
for rel,name,label,kwic,decision in ROWS:
 v=zc.verify(rel,kwic);assert v.get('ok'),(rel,v)
 occs.append({'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kwic,'MasterName':name,'Curated':True,
 'ContextMasters':[{'MasterName':name,'Roles':['utterer']},{'MasterName':'Mahakasyapa','Roles':['case-figure']},{'MasterName':'Shakyamuni Buddha','Roles':['case-figure']}],
 'AttributionNote':f'Source record ({rel}). {label}: {decision} Exact utterer of the headword-bearing retelling: {name}.',
 'DraftActorProof':{'ExactHeadwordClause':kwic,'GrammaticalSubject':name,'SpeechFrame':decision,'FullCaseDecision':f'{name} owns the complete headword-bearing hall-address turn; Kashyapa is the quoted case actor, not MasterName.'}})
sources=[x['RelPath'] for x in occs]; works=[zc.work_id(x) for x in sources]
opening="Breaking into a smile is Kashyapa's recorded response when Shakyamuni Buddha raises a flower before the assembly. In the Chan record the action is not a settled badge of approval: masters repeatedly raise the same scene and then praise, question, or attack the reported exchange."
body="Wanru Tongwei and Dahui Zonggao contrast Kashyapa's smile with the assembly's failure to respond; both place the reported charge immediately afterward. Yuansou Xingduan retells exactly that sequence and caps it with ‘one blind person leads a crowd of blind people’ (一盲引眾盲), while Juelang Daosheng asks whether the eye reportedly handed over was originally present or absent. The stable fact is the case action and its place in the exchange; the later verdicts do not supply a second meaning of the phrase."
claim_kwic='上堂，舉世尊一日以兜羅綿手舉金色波羅華普示大眾，時迦葉破顏微笑，世尊云：吾有正法眼藏、涅槃妙心付囑於汝。師云：一盲引眾盲，相牽入火坑。'
claim_verify=zc.verify('X/X71/X71n1419.xml',claim_kwic);assert claim_verify.get('ok'),claim_verify
claim={'ClaimText':'一盲引眾盲','RelPath':'X/X71/X71n1419.xml','FromLb':claim_verify['fromLb'],'ToLb':claim_verify['toLb'],'Kwic':claim_kwic,'MasterName':'Yuansou Xingduan','AttributionNote':'Source record (X/X71/X71n1419.xml). Recorded Sayings of Chan Master Yuansou Xingduan (元叟行端禪師語錄): after raising the flower-and-smile case, Yuansou Xingduan says “one blind person leads a crowd of blind people.”','ContextMasters':[{'MasterName':'Yuansou Xingduan','Roles':['utterer']},{'MasterName':'Mahakasyapa','Roles':['case-figure']},{'MasterName':'Shakyamuni Buddha','Roles':['case-figure']}],'Curated':True,'DraftActorProof':{'ExactHeadwordClause':'師云：一盲引眾盲，相牽入火坑。','GrammaticalSubject':'Yuansou Xingduan','SpeechFrame':'The marked 師云 after the raised case begins Yuansou Xingduan’s own verdict.','FullCaseDecision':'Yuansou Xingduan is the exact utterer of the anchored verdict; Kashyapa and Shakyamuni remain quoted case figures.'}}
sense={'SenseKey':None,'MasterName':None,'PreferredTarget':'break into a smile','AlternateTargets':['smile broadly','a smile breaks across the face'],'SearchAliases':['Kashyapa smiles','Kasyapa smiles','flower sermon smile','raised flower smile','break into a broad smile'],'Status':'preferred','Validation':'multi-source','Note':'Six exact headword-bearing retellings from six independent works are stored; later masters disagree in appraisal without changing the action denoted.','Occurrences':occs,'ClaimAnchors':[claim],'SourceTexts':sources,'RelatedMasters':['Mahakasyapa','Shakyamuni Buddha','Juelang Daosheng','Yuansou Xingduan','Wuyi Yuanlai','Wanru Tongwei','Baiyu Jingsi','Dahui Zonggao'],'RelatedTerms':['拈花','正法眼藏','靈山會上'],'ExplanationParts':{'CorpusEarnedOpening':opening,'EvidenceBody':[body]},'DraftEvidence':{'OpeningClaimEvidenceKeys':[f'o{i}' for i in range(1,7)],'ZenBend':'The ordinary facial action becomes the fixed response in the raised-flower public case; later masters keep the action stable while openly disputing its standing.','CounterexampleOrLimit':'Yuansou calls the exchange blind-leading-blind, and Juelang interrogates the handover. Their criticism prevents the entry from equating the smile with approval or realization.','DifferentThingTest':{'Decision':'one-thing','ComparedThings':['Kashyapa breaking into a smile in the raised-flower case','later masters retelling and judging that smile'],'Reason':'Every witness denotes the same facial action in the same case; favorable and hostile later readings are appraisals, not different referents.'},'AliasRationale':'Search probes cover natural English descriptions, the named actor, and the raised-flower case without converting an interpretation into a translation.','ModifierControls':[{'Control':'破顏','Finding':'Across the selected witnesses it predicates a smile breaking across Kashyapa’s face; no separate material or title referent occurs.'}],'FamilyControls':[{'Term':'拈花','Finding':'The raised flower is the preceding case action and remains a related entry, not part of this headword.'},{'Term':'正法眼藏','Finding':'The reported charge follows the smile but does not define the facial action.'},{'Term':'微笑','Finding':'The shorter smile word is a family component; this entry preserves the recurrent full phrase.'}],'IndependentWorkIds':works}}
entry={'Id':ID,'SourceTerm':TERM,'CorpusBaselineSha256':BASE,'CreatedBy':'Codex construction Lane C evidence-first','WrittenUtc':NOW,'Senses':[sense]}
out=DB/'fresh-build/entries'/ID;out.mkdir(parents=True,exist_ok=True)
(out/'evidence.draft.json').write_text(json.dumps({'SchemaVersion':1,'Entry':entry},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(out/'WORK.md').write_text(f'''# {TERM} — construction Lane C position 1

- corpus baseline: `{BASE}`
- fresh counts: exact 188 / 130 files / 129 works; bridged 188 / 130 files
- depth: six stored exact anchors from six independent works, spanning own records and a compiled record
- full-turn actor review: all six headword clauses occur in marked public addresses owned by the named speaker; Kashyapa and Shakyamuni are quoted case actors only

feedback-observations: all six witnesses place Kashyapa’s breaking smile immediately after the raised flower; Wanru and Dahui contrast him with the assembly; Yuansou attacks the exchange; Juelang questions the reported handover.
feedback-inference-verdict: licensed — the opening states only the repeated action, its place in the public case, and the observable disagreement in later handling.
feedback-falsification-searches: ordinary smiles outside the raised-flower case; other actors described by the same phrase; title/catalogue containment; favorable-only readings; hostile-only readings.
feedback-counterexamples: hostile retellings prevent ‘approval’ from entering the target or definition; no selected witness uses the phrase for a different actor or event.
feedback-scope: the raised-flower case and its later Chan retellings.
lookup-probes: break into a smile; Kashyapa smiles; Kasyapa smiles; flower sermon smile; raised flower smile.
opening-interpretation-verdict: the opening names the action and the corpus-specific bend before quotation, while remaining falsifiable by the stored sequence and counter-verdicts.

sense-target-distinguishability: one sense; changing appraisal does not change the facial action.
family-control: 拈花, 正法眼藏, 靈山會上, and 微笑 were rechecked and remain related but distinct lexical units.
''',encoding='utf-8')
print(out/'evidence.draft.json')
