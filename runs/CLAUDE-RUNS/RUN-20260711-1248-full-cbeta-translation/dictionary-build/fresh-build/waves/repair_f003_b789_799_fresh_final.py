#!/usr/bin/env python3
import datetime,json,subprocess,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
rows=json.loads((R/'fresh-build/waves/f003-laneB-751-800-fresh-independent-exact-review.json').read_text())['rows'];ids={r['ordinal']:r['id'] for r in rows};T=[789,790,791,792,793,794,795,796,799];RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
def now():return datetime.datetime.now(datetime.timezone.utc).isoformat()
def title(o):return zc.title(o['RelPath'])
def clean(o):
 for k in ('MasterName','ActorAttribution','DraftActorProof'):o.pop(k,None)
def named(o,n,ctx=(),why='The complete case places the exact headword inside this named master’s speech turn.'):
 clean(o);o['MasterName']=n;o['ContextMasters']=[{'MasterName':n,'Roles':['utterer']}]+[{'MasterName':x,'Roles':[r]} for x,r in ctx if x!=n];o['AttributionNote']=f'Source text ({title(o)}): {n} utters the exact headword. {why}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':n,'SpeechFrame':why,'FullCaseDecision':f'{n} is the exact-headword utterer.'}
def actor(o,status,label,kind,role,why,ctx=()):
 clean(o);o['ActorAttribution']={'Status':status,'Kind':kind,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'ReviewedBy':'Codex f003 B751-800 fresh repair author','ReviewedUtc':now(),'GrammarEvidence':why};o['ContextMasters']=[{'MasterName':n,'Roles':[r]} for n,r in ctx];o['AttributionNote']=f'Source text ({title(o)}): {label} owns the exact headword. {why}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':why,'FullCaseDecision':why}
def question(o,ctx=(),label='the unnamed monastic questioner'):actor(o,'reviewed-unnamed',label,'monastic questioner','questioner','The explicit question frame assigns the exact headword to this questioner before the separately marked response.',ctx)
def narr(o,label,why,ctx=()):actor(o,'narrated',label,'compiler narrative','compiler',why,ctx)
def ident(o,label,role,why,ctx=()):actor(o,'identified-non-master',label,'identified documentary actor',role,why,ctx)
def add(s,rel,kw,name,why,ctx=()):
 v=zc.verify(rel,kw);assert v['ok'],(rel,kw,v);o={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True,'ContextMasters':[]};named(o,name,ctx,why);s['Occurrences'].append(o)
for n in T:
 d=R/'fresh-build/entries'/ids[n];p=d/'evidence.draft.json';x=json.loads(p.read_text());E=x['Entry'];s=E['Senses'][0]
 def O(i):return s['Occurrences'][i-1]
 if n==789:
  for i,name in ((2,'Yunfeng Zhideng'),(3,'Baofu Juxu'),(4,'Zhimen Zuozu'),(6,'Zeng Kai')):question(O(i),[(name,'respondent')])
 elif n==790:
  named(O(3),'Baofeng Bian',why='The explicit introduction “Baofeng Bian said” governs the clause about Kasyapa holding the strategic pass; the generic later-commentator label discarded a recoverable master.')
 elif n==791:
  for i in (6,3):del s['Occurrences'][i-1]
  named(O(6),'Shakyamuni Buddha',why='Inside the inherited case, the Buddha’s quoted rebuke says that Subhuti seated in the rocks saw his teaching body; this is not documentary narrator wording.')
  s['ExplanationParts']['CorpusEarnedOpening']='Subhuti is a named disciple whom Chan masters repeatedly place inside questions about speaking, hearing, sitting, and what counts as seeing the Buddha.'
  s['ExplanationParts']['EvidenceBody']=['Yongming Yanshou quotes Subhuti asking whether equality is conditioned or unconditioned. Dahui Zonggao and Zeng Kai raise the rock-sitting case in which flowers fall and Subhuti denies having spoken a word. Yuanwu Keqin tests the exchange with the flower-scattering god, while Shakyamuni Buddha invokes Subhuti’s rock seat in rebuking the nun Utpalavarna. These are the corpus-earned deployments; the entry does not reduce him to a generic imported emblem of emptiness.']
  s['DraftEvidence']['ZenBend']=s['ExplanationParts']['EvidenceBody'][0]
  add(s,'T/T47/T47n1998A.xml','上堂舉。須菩提巖中宴坐。諸天雨華讚歎尊者曰。空中雨華讚歎者是何人。','Dahui Zonggao','Dahui raises Subhuti’s rock-sitting and flower-rain exchange in a hall address and then tests both speakers with his own shouts.')
  add(s,'B/B25/B25n0145.xml','第六分佛對須菩提云。今之信般若者甚非偶然。','Zhongfeng Mingben','Zhongfeng Mingben’s commentary directly stages the Buddha addressing Subhuti while explaining the sixth section of the Diamond discourse.')
 elif n==792:named(O(4),'Jiashan Shanhui',why='The case heading and the immediate phrase “Jiashan instructed the assembly, saying” assign the whole headword-bearing statement to Jiashan Shanhui.')
 elif n==793:
  recuts=[(3,'州曰：既是羅漢，為甚麼却作牛去？山曰：蒼天！蒼天！','Hanshan',()),(4,'臨濟乃曰蒼天蒼天','Linji Yixuan',( ('Foyan Qingyuan','later-quoter'),)),(5,'州曰。既是羅漢。為甚麼却作牛去。山曰。蒼天。蒼天。','Hanshan',()),(7,'頭云：蒼天！蒼天！峯無語','Shitou Xiqian',( ('Mazu Daoyi','later-quoter'),))]
  for i,kw,name,ctx in recuts:
   v=zc.verify(O(i)['RelPath'],kw);assert v['ok'],(i,v);O(i).update(Kwic=kw,FromLb=v['fromLb'],ToLb=v['toLb']);named(O(i),name,ctx,'The recut witness isolates this speaker’s exact exclamation and excludes the other repeated turns in the surrounding case.')
 elif n==794:narr(O(6),'the signed preface author','The exact headword occurs in documentary date prose saying that the letter was written on Buddha’s-birthday day; it is neither a sermon heading nor speech by the record owner.')
 elif n==795:
  for i,name in ((1,'Sansheng Huoran'),(2,'Shoushan Shengnian'),(5,'Zhiyi Xinyin'),(6,'Zhiyi Kefu')):question(O(i),[(name,'respondent')])
  ident(O(3),'the signed preface author','compiler','The preface author describes the record’s subject through the host-within-host phrase; Juelang Daosheng is the person praised, not the utterer.',[('Juelang Daosheng','person-described')])
  named(O(7),'Dongshan Liangjie',[('Langting Jingting','later-quoter')],'Langting explicitly quotes Dongshan Liangjie’s definition “continuity is called host within host”; Dongshan owns the exact quoted phrase.')
  named(O(8),'Yongjue Yuanxian',why='The complete passage is continuous sermon speech in Yongjue Yuanxian’s Extensive Record; the former documentary label discarded the recoverable record owner.')
 elif n==796:
  named(O(1),'Nanquan Puyuan',[('Huangbo Xiyun','respondent')],'Nanquan asks Huangbo whether he relies on anything throughout the twelve periods; Huangbo’s answer does not repeat the headword.')
  narr(O(2),'the lamp-record biographer','The biography lists Baozhi’s Twelve-Hour Song among his writings; the phrase is a work title in narration, not his speech.',[('Baozhi','person-described')])
  question(O(3),[('Fayan Wenyi','respondent')])
  for i,name in ((4,'Niaoke Daolin'),(6,'Xuansu')):named(O(i),'Xitang Zhizang',[(name,'respondent')],'Mazu’s named disciple Xitang Zhizang asks what serves as the field throughout the twelve periods; the respondent answers without repeating the headword.')
  named(O(7),'Maqiaoshan Benkong',why='The explicit Maqiaoshan Benkong heading governs the following hall address containing the headword.')
  del s['Occurrences'][8]
 elif n==799:
  del s['Occurrences'][1]
  s['DraftEvidence']['CounterexampleOrLimit']='The unrelated phrase 三般物, “three requested things,” and other accidental or nested matches are excluded; this sense is limited to Linji’s Three Essentials and formulas that explicitly invoke that system.'
  add(s,'B/B27/B27n0152.xml','所以道一句中具三玄一玄中具三要有權有實有照有用','Yulin Tongxiu','Yulin Tongxiu explicitly repeats Linji’s architecture of three mysteries and three essentials, with authority, actuality, illumination, and function.')
  add(s,'X/X68/X68n1318.xml','先聖道：一句語須具三玄門，一玄門須具三要。','Fenyang Shanzhao','Fenyang Shanzhao quotes the predecessor’s formula and immediately asks which sentence embodies the three mysteries and three essentials.')
 for ss in E['Senses']:
  ss['SourceTexts']=sorted({o['RelPath'] for o in ss['Occurrences']});ss['RelatedMasters']=sorted({o['MasterName'] for o in ss['Occurrences'] if o.get('MasterName')}|{c['MasterName'] for o in ss['Occurrences'] for c in o.get('ContextMasters',[])})
  ss['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(ss['Occurrences'])+1)];ss['DraftEvidence']['IndependentWorkIds']=sorted({zc.work_id(o['RelPath']) for o in ss['Occurrences']})
 p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True,stdout=subprocess.DEVNULL);print('repaired',n)
