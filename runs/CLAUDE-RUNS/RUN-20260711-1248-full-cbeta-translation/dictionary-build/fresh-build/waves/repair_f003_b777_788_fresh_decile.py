#!/usr/bin/env python3
import datetime,json,subprocess,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
rows=json.loads((R/'fresh-build/waves/f003-laneB-751-800-fresh-independent-exact-review.json').read_text())['rows'];ids={r['ordinal']:r['id'] for r in rows}
T=[777,778,779,780,781,783,785,786,787,788];RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
def now():return datetime.datetime.now(datetime.timezone.utc).isoformat()
def title(o):return zc.title(o['RelPath'])
def clean(o):
 for k in ('MasterName','ActorAttribution','DraftActorProof'):o.pop(k,None)
def named(o,n,ctx=(),why='The complete case places the exact headword inside this named master’s speech turn.'):
 clean(o);o['MasterName']=n;o['ContextMasters']=[{'MasterName':n,'Roles':['utterer']}]+[{'MasterName':x,'Roles':[r]} for x,r in ctx if x!=n];o['AttributionNote']=f'Source text ({title(o)}): {n} utters the exact headword. {why}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':n,'SpeechFrame':why,'FullCaseDecision':f'{n} is the exact-headword utterer.'}
def actor(o,status,label,kind,role,why,ctx=()):
 clean(o);o['ActorAttribution']={'Status':status,'Kind':kind,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'ReviewedBy':'Codex f003 B751-800 fresh repair author','ReviewedUtc':now(),'GrammarEvidence':why};o['ContextMasters']=[{'MasterName':n,'Roles':[r]} for n,r in ctx];o['AttributionNote']=f'Source text ({title(o)}): {label} owns the exact headword. {why}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':why,'FullCaseDecision':why}
def question(o,ctx=(),label='the unnamed monastic questioner'):actor(o,'reviewed-unnamed',label,'monastic questioner','questioner','The explicit question frame assigns the exact headword to this questioner before the separately marked response.',ctx)
def narrated(o,label,why,ctx=()):actor(o,'narrated',label,'compiler narrative','compiler',why,ctx)
def imp(o,label,why,ctx=()):actor(o,'impersonal',label,'editorial heading','compiler',why,ctx)
def ident(o,label,role,why,ctx=()):actor(o,'identified-non-master',label,'identified non-roster actor',role,why,ctx)
def add(s,rel,kw,name=None,ctx=(),note='',actor_args=None):
 v=zc.verify(rel,kw);assert v['ok'],(rel,kw,v);o={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True,'ContextMasters':[]}
 if name:named(o,name,ctx,note)
 else:actor(o,*actor_args,ctx)
 s['Occurrences'].append(o)
for n in T:
 d=R/'fresh-build/entries'/ids[n];p=d/'evidence.draft.json';x=json.loads(p.read_text());E=x['Entry'];s=E['Senses'][0]
 def O(i):return s['Occurrences'][i-1]
 if n==777:
  del s['Occurrences'][1]
  add(s,'X/X81/X81n1568.xml','祇如山僧適來教上座參取聖僧，聖僧還道箇甚麼？','Yunju Yineng',note='Yunju Yineng asks a monk what the sacred monk said after directing the assembly to consult that figure; this is spoken deployment of the image, not a contents-list office title.')
 elif n==778:
  kws=['因僧問：寒暑到來，如何𢌞避？山曰：何不向無寒暑處去？曰：如何是無寒暑處？山曰：寒時寒殺闍黎，熱時熱殺闍黎。','師因僧問寒暑到來如何回避師云何不向無寒暑處去僧云如何是無寒暑處師云寒時寒殺闍黎熱時熱殺闍黎。','僧問洞山。寒暑到來如何迴避山云。何不向無寒暑處去僧云。如何是無寒暑處山云。寒時寒殺闍黎。熱時熱殺闍黎。','僧問：寒暑到來，如何回避？師云：何不向無寒暑處去？云：如何是無寒暑處？師云：寒時寒殺闍黎，熱時熱殺闍黎。']
  for i,kw in enumerate(kws,1):v=zc.verify(O(i)['RelPath'],kw);assert v['ok'];O(i).update(Kwic=kw,FromLb=v['fromLb'],ToLb=v['toLb']);question(O(i),[('Dongshan Liangjie','respondent')])
 elif n==779:
  named(O(4),'Hongzhi Zhengjue',why='This is continuous discourse in Hongzhi Zhengjue’s Extensive Record; the former Dahui attribution came from the wrong record owner.')
  named(O(7),'Chushi Fanqi',why='The explicit heading “Chan Master Chushi Qi—returning from thanking for rain, entered the hall” assigns the address and 髑髏 wording to Chushi Fanqi.')
  ident(O(2),'the verse author in the Shakyamuni section','verse-author','The headword is in a verse commenting on Shakyamuni’s awakening story; Shakyamuni is the case figure, not its utterer.',[('Shakyamuni Buddha','case-figure')])
 elif n==780:
  named(O(2),'Pu’an Yinsu',why='The exact wording is in Pu’an Yinsu’s signed pagoda inscription preserved in his own recorded sayings.')
  named(O(3),'Sanyi Mingyu',why='The complete small-gathering address in Sanyi Mingyu’s own record contains the exact headword; it is not anonymous assembly speech.')
 elif n==781:
  named(O(1),'Dunan Zongyan',why='The immediately preceding header names Dunan Zongyan; he rejects the inherited claim that recognizing the staff finishes a lifetime of inquiry.')
  named(O(2),'Lingyan Chu',why='靈巖儲云 explicitly introduces Lingyan Chu’s comment containing the headword.')
  named(O(4),'Chengshan Qia',[('Zhaozhou Congshen','case-figure')],'城山洽云 explicitly introduces Chengshan Qia’s comment containing the headword; Zhaozhou is the case figure.')
  named(O(5),'Changqing Huileng',why='The explicit 長慶稜禪師 heading governs the following hall addresses, including this sentence.')
 elif n==783:
  question(O(1),[('Shoushan Shengnian','respondent')],'the unnamed alms officer')
  imp(O(3),'the editorial return-of-the-alms-officer occasion label','The phrase 因化主歸 labels the occasion for Huanglong Huinan’s following address; Huanglong does not utter the office title.',[('Huanglong Huinan','section-subject')])
  narrated(O(4),'the inherited-case narrator','The narrator says that Yaoshan’s alms officer arrived at Gan Zhi’s house; Yaoshan is a case figure, not the utterer.',[('Yaoshan Weiyan','case-figure')])
  imp(O(5),'the editorial thanks-to-two-alms-officers label','謝二化主 is the occasion heading before Sixin Wuxin’s verse; the presiding master does not utter the label.',[('Sixin Wuxin','section-subject')])
 elif n==785:
  named(O(8),'Fenyang Shanzhao',why='The full passage is continuous instruction in Fenyang Shanzhao’s recorded sayings; the former documentary label discarded the recoverable speaker.')
  ident(O(9),'the unnamed layman in the encounter','interlocutor','The layman directly threatens to report the master’s conduct to clear-eyed people; the narrator only frames his quoted turn.')
 elif n==786:
  question(O(3),[('Langting Jingting','respondent')])
  named(O(4),'Sanyi Mingyu',why='The occurrence is continuous speech in Sanyi Mingyu’s small-gathering address, not anonymous assembly prose.')
 elif n==787:
  s['PreferredTarget']='use; function; effective point';s['AlternateTargets']=['utility','what it accomplishes'];s['ExplanationParts']['CorpusEarnedOpening']='The word names a thing’s use, function, utility, or the effective point shown by its use.'
  s['ExplanationParts']['EvidenceBody']=['Fayun Faxiu, Huangbo Xiyun, and Fayan Wenyi ask what use a presented claim or verbal maneuver has. Magu Baoche dismisses a monk as useless, while Hongzhi Zhengjue asks what use remains when speech and action are both exhausted. The object varies, but the grammatical demand is consistently for what something does or accomplishes.']
  s['DraftEvidence']['ZenBend']=s['ExplanationParts']['EvidenceBody'][0];s['DraftEvidence']['DifferentThingTest']={'Decision':'one-thing','ComparedThings':['use','function','utility','effective point'],'Reason':'These are context-sensitive English renderings of the same attested question about what something does; the nested concentration-name 不用處定 is excluded as a different longer term.'}
  for i in (7,3):del s['Occurrences'][i-1]
  add(s,'T/T48/T48n2001.xml','說不得行不得。直是無氣息。有甚麼用處。','Hongzhi Zhengjue',note='Hongzhi asks what use remains when it cannot be spoken or enacted and has no breath; the word demands the effective point of the presented condition.')
  add(s,'X/X82/X82n1571.xml','驀拈拄杖曰：還知這箇堪作甚麼？打香臺一下，曰：莫道無用處。', 'Yandang Ji',note='Yandang Ji raises the staff, asks what it can be used for, strikes the incense stand, and says not to call it useless; action supplies the answer.')
 elif n==788:
  for i in (7,3,2):del s['Occurrences'][i-1]
  s['ExplanationParts']['CorpusEarnedOpening']='Shariputra is a named disciple whom Chan masters repeatedly place inside questions, reversals, comparisons, and inherited public cases.'
  s['ExplanationParts']['EvidenceBody']=['Yongming Yanshou cites Shariputra asking for a concise statement of dependent arising. Tianyin Yuanxiu, Yuanwu Keqin, and Dahui Zonggao raise exchanges in which Shariputra questions Moon-above Woman, Subhuti, or a heavenly woman and is answered or reversed. The entry defines the figure through these Chan deployments and does not import the unattested title “foremost in wisdom.”']
  s['DraftEvidence']['ZenBend']=s['ExplanationParts']['EvidenceBody'][0]
  add(s,'T/T47/T47n1998A.xml','記得舍利弗問月上女曰。汝於今者。行何乘也。','Dahui Zonggao',note='Dahui raises Shariputra’s question to Moon-above Woman as a public case in his own discourse.')
  add(s,'L/L154/L154n1639.xml','示眾舉舍利弗問天女汝何不轉卻女身','Tianyin Yuanxiu',note='Tianyin Yuanxiu raises the exchange in which Shariputra asks the heavenly woman why she does not change her female body.')
  add(s,'P/P154/P154n1519.xml','舍利弗因入城遙見月上女出城舍利弗心口思惟此姊見佛不知得忍不得忍我當問之',actor_args=('narrated','the inherited-case compiler','case narration','compiler','The compiler narrates Shariputra meeting Moon-above Woman as the setup to a public exchange.'))
 for ss in E['Senses']:
  ss['SourceTexts']=sorted({o['RelPath'] for o in ss['Occurrences']});ss['RelatedMasters']=sorted({o['MasterName'] for o in ss['Occurrences'] if o.get('MasterName')}|{c['MasterName'] for o in ss['Occurrences'] for c in o.get('ContextMasters',[])})
  ss['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(ss['Occurrences'])+1)];ss['DraftEvidence']['IndependentWorkIds']=sorted({zc.work_id(o['RelPath']) for o in ss['Occurrences']})
 p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True,stdout=subprocess.DEVNULL);print('repaired',n)
