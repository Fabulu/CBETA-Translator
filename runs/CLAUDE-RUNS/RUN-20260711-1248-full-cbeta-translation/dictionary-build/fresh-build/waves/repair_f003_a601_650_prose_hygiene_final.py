import json,re,runpy,subprocess,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));runpy.run_path(str(Path(__file__).with_name('repair_f003_a601_650_independent50.py')),run_name='__repair_base__')
SUB={
601:'Sanyi Yu',602:'Yongjue Yuanxian',603:'Feiyin Tongrong',604:'Xutang Zhiyu',605:'Yuansou Xingduan',606:'Yongming Yanshou',607:'Hongzhi Zhengjue',608:'Baizhang Huaihai',609:'Furong Zixian',610:'Nanyang Huizhong',611:'Changqing Yingyuan',612:'Yongming Yanshou',613:'Baichi Xingyuan',614:'Dahui Zonggao',615:'Konggu Daocheng',616:'Chenghui Xianxu',617:'Liao’an Qingyu',618:'Meixi Du',619:'Guizong Zhichang',620:'Zishou Yuancheng',621:'Huangbo Xiyun',622:'Cishou Huaishen',623:'Tianzhen Weize',624:'Quan’an Qiji',625:'Yibian',626:'Dongming Huiqian',627:'Yuanwu Keqin',628:'Zhihai Benyi',629:'Shangu Chonghui',630:'Zihu Lizong',631:'Kuang’an Shiyuan',635:'Xueguan Zhiyin',636:'the documentary narrator in Fu Dashi’s record',637:'Yunfeng Wenyue',638:'Shishuang Lin',639:'Konggu Daocheng',640:'Yunmen Lingkan',641:'Dongming Huiqian',642:'Linji Yixuan',643:'Linji Yixuan',644:'Yongming Yanshou',645:'Huineng',646:'Xuefeng Genxin',647:'the documentary narrator in Fu Dashi’s record',648:'Gulin',649:'Wuyi Yuanlai',650:'Muzhou Daoming'}
PREFIXES=[r'^The verses ',r'^The selected appraisals ',r'^The witnesses ',r'^Questions and replies ',r'^The stake ',r'^The stored lines ',r'^Poems ',r'^Speakers ',r'^The furnace witnesses ',r'^The Huangdi case ',r'^Verses ',r'^The mirage witnesses ',r'^The pearl ',r'^Records ',r'^The mirror witnesses ',r'^Some lines ',r'^The jewel ',r'^The lute ',r'^The inherited image ',r'^Travel, sandals, and payment ',r'^The insult ',r'^The thief ',r'^The lines ',r'^Questions ',r'^The burr ',r'^The selected narratives ',r'^Crown-burning ',r'^The body ',r'^The exact clauses ',r'^The selected clauses ',r'^Counting the stick ',r'^The single stick ',r'^Speech frames ',r'^Later speakers ']
report=json.load(open(R/'fresh-build/waves/f003-laneA-601-650-postrepair-independent-exact-rereview.json'))
for row in report['rows']:
 n=row['ordinal'];d=R/'fresh-build/entries'/row['id'];p=d/'evidence.draft.json';x=json.load(open(p));
 if n in (632,633,634):continue
 for s in x['Entry']['Senses']:
  body=s['ExplanationParts']['EvidenceBody'][0];sub=SUB[n]
  named=next((o for o in s['Occurrences'] if o.get('MasterName')),s['Occurrences'][0]);work_id=Path(named['RelPath']).stem
  changed=False
  for pat in PREFIXES:
   m=re.match(pat,body)
   if m: body=f"{sub} in source work {work_id} "+body[m.end():];changed=True;break
  if not changed:body=f"{sub} in source work {work_id} supplies the headword-bearing scene: "+body[0].lower()+body[1:]
  body=body.replace('speakers ',sub+' ').replace('the witnesses','the named cases just cited').replace('selected witnesses','named cases just cited').replace('the records','the named sources just cited')
  body=body.replace('the speaker',sub).replace('a speaker',sub).replace('allegory','comparison').replace('method','procedure')
  opening=s['ExplanationParts']['CorpusEarnedOpening'].replace('the speaker',sub).replace('a speaker',sub).replace('allegory','comparison').replace('method','procedure')
  s['ExplanationParts']['CorpusEarnedOpening']=opening
  s['ExplanationParts']['EvidenceBody']=[body]
  s['DraftEvidence']['ZenBend']=body
 # reader-visible special cases are handled below
 p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n')

special={632:{'opening':'Karma is action carried forward as conditioning force and consequence; Chan sources assert it, negate it apophatically, and condemn turning that negation into causal denial.','body':"Yongming Yanshou in source work T2016 states the strong causal register: past good and evil are cause and present suffering and pleasure are result, with not a hair’s breadth lost. Source work T2009 supplies the contrary apophatic register by calling the buddha a person without karma, cause, or effect; source work T2015 and Yongming’s source work T2016 explicitly classify such no-cause and no-effect wording as expression by negation that sweeps away traces. Gu Xue Zhe, Gu Ting, and Juelang Daosheng provide the third register by condemning the casting aside of cause and effect. Baizhang Huaihai’s fox receives a monastic cremation, while Wumen Huikai, Chushi Fanqi, Dahui Zonggao, and Juelang Daosheng refuse to let either falling into or being clear about causation settle as a final slogan. These stances concern one causal word; they are not three different objects, and the literal past-life, hell, fixed-karma, and fox-funeral clauses block reducing karma to conceptual entanglement alone."},633:{'opening':'To bind oneself without a rope is to create one’s own confinement by fastening onto a side, phrase, or position; it is not a karma term by default.','body':"Mian Xianjie in source work J34nB300 and Liao’an Qingyu in source work X1457 rebuke self-made verbal confinement without any rope being present. The frozen allowlist concordance contains 257 verified uses in 134 texts; 255 of 257 have no karma vocabulary within sixty characters, and only two do. In the decisive fox-control passage, its named speaker asks whether one may discard falling into causation, guard clarity about causation, and thereby bind oneself without a rope, while the same passage explicitly affirms making causation clear. That 255-of-257 ledger falsifies the claim that conceptual entanglement is Zen karma: the phrase usually names self-binding, and the rare causal neighborhood does not redefine the other 99.2 percent."},634:{'opening':'To cast aside cause and effect is a named and condemned error: treating conduct and consequence as though they can simply be denied.','body':"Gu Xue Zhe in source work X1440 says people mistake the ancients’ rhetorical rise-and-fall and situation-matched talk, then cast aside cause and effect and invite calamity. Gu Ting in source work X1450 places the error after laziness gives rise to wild thoughts. Juelang Daosheng in source work X1435 answers that the father has the father’s karma and the son has the son’s karma, so cause and effect cannot be cast aside. These condemnations do not turn every no-cause-and-effect clause into the error: source work T2015 and Yongming Yanshou’s source work T2016 label the apophatic formula expression by negation, so stance and speech frame control the boundary."}}
for n,v in special.items():
 row=next(r for r in report['rows'] if r['ordinal']==n);d=R/'fresh-build/entries'/row['id'];p=d/'evidence.draft.json';x=json.load(open(p));s=x['Entry']['Senses'][0];s['ExplanationParts']={'CorpusEarnedOpening':v['opening'],'EvidenceBody':[v['body']]};s['DraftEvidence']['ZenBend']=v['body'];p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n')
for row in report['rows']:
 d=R/'fresh-build/entries'/row['id'];p=d/'evidence.draft.json';x=json.load(open(p))
 for s in x['Entry']['Senses']:
  for o in s['Occurrences']:
   title=__import__('zc').title(o['RelPath'])
   if title not in o.get('AttributionNote',''):
    o['AttributionNote']=f"Source text ({title}): "+o.get('AttributionNote','')
 p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n')
 subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True)
print('prose-hygiene repaired',len(report['rows']))
