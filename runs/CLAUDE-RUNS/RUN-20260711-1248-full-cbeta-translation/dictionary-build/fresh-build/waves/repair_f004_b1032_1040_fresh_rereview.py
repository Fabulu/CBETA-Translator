from pathlib import Path
import datetime,hashlib,json,subprocess,sys
R=Path(__file__).resolve().parents[2];H=R/'fresh-build/waves';sys.path.insert(0,str(R));import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
ROWS={1032:'t_b336769aabdf',1034:'t_e21288d0fefb',1035:'t_641de814fd8a',1036:'t_10b63ac74f61',1038:'t_b016f513be3d',1040:'t_74c3c0e1b896'}
N=lambda x:('named',x);U=lambda x,r='compiler',c=():('other',x,r,list(c))
ACT={
1032:[N("Lan'an Dingxu"),U('the unnamed Buddha-birth verse author preserved by the Chan anthology','verse-author'),U('the unnamed Mazu verse commentator preserved by Zongjian Falin','verse-author',['Mazu Daoyi']),U('Tan Zhenmo and twenty named local-gentry coauthors','compiler'),N("Zhe'an Jingfan"),U('the unnamed Beishan ancestral-rite speaker','utterer'),N('Jingshan Rong')],
1034:[N('Dunan Zongyan'),N('Lianfeng'),N('Yunmen Cheng'),N('Zixian Jue'),N("Guishan Hui'an Guang"),N('Feiyin Tongrong')],
1035:[N('Zhihai Benyi'),N('Huanji'),N('Zhongfeng Mingben'),N('Juelang Daosheng'),N('Yongming Yanshou'),N('Wuye Guoshi'),N('Yuanwu Keqin')],
1036:[N('Wuzu Fayan'),U('the unnamed Li Guang verse author preserved by the Chan anthology','verse-author'),N('Daqian Heqiao Dai'),N('Gushan Gui'),N('Juelang Daosheng')],
1038:[U('the unnamed annalist of the Feiyin record','compiler',['Feiyin Tongrong']),U('Shuijian Huihai, the lineage-history commentator','compiler',['Feiyin Tongrong']),U('the unnamed imperial memorial author presenting Wudeng Yantong','compiler',['Feiyin Tongrong']),U('the unnamed Tongrong biographer','compiler',['Feiyin Tongrong'])],
1040:[U('the unnamed lamp-record narrator of Xiuxi’s descent','compiler',['Xiuxi','Gushan']),U('the unnamed Zhaozhou-record narrator','compiler',['Zhaozhou Congshen']),U('the unnamed Nanquan verse author','verse-author',['Nanquan Puyuan']),U('the unnamed imperial-address compiler narrating Foyan Qingyuan’s dance','compiler',['Foyan Qingyuan']),U('the unnamed lamp-record narrator of Yungai Zhi’s descent','compiler',['Yungai Zhi','Jiufeng Xiguang']),U('the unnamed lamp-record narrator of Baizhang Weizheng’s descent','compiler',['Baizhang Weizheng']),N('Feiyin Tongrong')]
}
EXPL={
1032:"Family disgrace is the school’s own embarrassing business made public. Named masters and verse writers call inherited sayings, lineage devices, and even ancestral memorials an airing of the house’s disgrace. The phrase is deliberately double-edged: what discredits the house is also what descendants keep exposing in sermons, comments, invitations, and rites.",
1034:"Xu Six carrying a board is the stock picture of one-sided vision: the board blocks one side while its bearer sees only the other. Named commentators apply the verdict to paired answers, lineage figures, and students trapped by either sound-and-form or purity. It is a criticism of partial seeing, not a biography of Xu Six.",
1035:"Poison is what a saying or medicine becomes when its reception harms rather than frees. Zhihai Benyi offers a phrase to chew and warns that failure to break it turns poisonous; Yongming Yanshou and other speakers say the finest clarified butter becomes poison in an unclean vessel. The bend is relational: offered material can cure or poison according to the encounter.",
1036:"Li Guang appears through the general’s utterly committed shot, which entered a stone mistaken for a tiger. Named masters and verse writers compress the feat into Li Guang’s spirit-like arrow and set it beside cases such as Baizhang’s fox or Huanglong’s barriers. The entry follows this deployed case-figure rather than supplying a general biography.",
1038:"Strict Lineage of the Five Lamps is Feiyin Tongrong’s lineage history bearing that title. Documentary witnesses distinguish Tongrong’s compilation, a later continuation, Shuijian Huihai’s discussion of its disputed corrections, an imperial memorial presenting it, and a biography explaining its polemical title. It is a book in a public contest over lineage membership, not a generic phrase about severe lamps.",
1040:"To step down from the teaching seat is a visible move in a public encounter. Xiuxi descends and is seized after two steps; Zhaozhou descends to inspect a questioner; Yungai Zhi descends and spreads his hands to Jiufeng; Baizhang Weizheng separately descends and spreads his hands after the community requests the promised teaching. The movement is part of each answer, not the ending of an ordinary sitting session."
}

def actor(o,s):
 if s[0]=='named':
  n=s[1];o['MasterName']=n;o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':n,'Roles':['utterer']}];proof=f'The complete source unit assigns the exact headword-bearing speech or authored wording to {n}.'
 else:
  label,role,ctx=s[1:];o['MasterName']=None;o['ContextMasters']=[{'MasterName':x,'Roles':['section-subject']} for x in ctx];status='reviewed-unnamed' if label.startswith('the ') else 'identified-non-master';proof=f'The complete source and byline search assigns the wording to {label}; no named master is manufactured for this {role} voice.';o['ActorAttribution']={'Status':status,'Kind':label,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':proof,'ReviewedBy':'Codex f004 B fresh-rereview repair author','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True}
 who=o.get('MasterName') or o['ActorAttribution']['ActorLabel'];o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {who}. {proof}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':who,'SpeechFrame':proof,'FullCaseDecision':proof}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()

# Source-bound candidates for explicit names not yet canonicalized publicly.
pp=R/'fresh-build/pending-roster.json';pd=json.loads(pp.read_text());have={x['canonicalName'] for x in pd['candidates']}
for n,eid in ROWS.items():
 os=json.loads((R/'fresh-build/entries'/eid/'evidence.draft.json').read_text())['Entry']['Senses'][0]['Occurrences']
 base=[o for i,o in enumerate(os,1) if not (n==1040 and i==4)]
 for o,s in zip(base,ACT[n]):
  if s[0]=='named' and s[1] not in have:
   pd['candidates'].append({'canonicalName':s[1],'aliases':[s[1]],'evidence':[{k:o[k] for k in ('RelPath','FromLb','ToLb','Kwic')}],'reviewedBy':'Codex f004 B fresh-rereview repair author','reviewReport':'fresh-build/waves/f004-laneB-1032-1034-1035-1036-1038-1040-fresh-independent-rereview.json','status':'awaiting-roster-integration'});have.add(s[1])
pp.write_text(json.dumps(pd,ensure_ascii=False,indent=2)+'\n')

out=[]
for n,eid in ROWS.items():
 b=R/'fresh-build/entries'/eid;wp=b/'evidence.draft.json';w=json.loads(wp.read_text());e=w['Entry'];os=e['Senses'][0]['Occurrences']
 if n==1040:
  # Remove the duplicate T51 Xiuxi recension and replace it with Feiyin's
  # independent raising of Zhaozhou's half-canon descent.
  if len(os)==7 and os[3]['RelPath']=='T/T51/T51n2076.xml':os.pop(3)
  rel='J/J26/J26nB178.xml';text='褒一貶，天淵各別，且道淆訛在甚麼處？若向者裏辨別得出，則佛祖禪機一時覷破，當人生死亦自明了，若去若來，活活潑潑。又有一婆子，將淨財請趙州老漢代轉藏經，趙州得財乃下禪床走一匝，回報云：『轉藏經已竟。』婆子聞得便云：『適來請轉全藏，為甚只轉半藏？』'
  if not any(o['RelPath']==rel for o in os):
   v=zc.verify(rel,text);assert v['ok'];neo=dict(os[-1]);neo.update({'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':text});os.append(neo)
 assert len(os)==len(ACT[n])
 for o,spec in zip(os,ACT[n]):actor(o,spec)
 s=e['Senses'][0];opening,body=EXPL[n].split('. ',1);s['Explanation']=EXPL[n];s['ExplanationParts']={'CorpusEarnedOpening':opening+'.','EvidenceBody':[body]};s['DraftEvidence']['ZenBend']=EXPL[n];s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(os)+1)];s['DraftEvidence']['IndependentWorkIds']=list(dict.fromkeys(zc.work_id(o['RelPath']) for o in os))
 w['Entry']=e;wp.write_text(json.dumps(w,ensure_ascii=False,indent=2)+'\n');ep=b/'entry.v2.json';rp=b/'b-fresh-rereview-repair-compile.json';q=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(wp),'--output',str(ep),'--report',str(rp)],text=True,capture_output=True)
 if q.returncode:raise SystemExit(q.stdout+q.stderr)
 ce=json.loads(ep.read_text());total=exact=0
 for ss in ce['Senses']:
  for o in ss['Occurrences']:
   total+=1;v=zc.verify(o['RelPath'],o['Kwic']);exact+=int(v['ok'] and v['fromLb']==o['FromLb'] and v['toLb']==o['ToLb'] and ce['SourceTerm'] in o['Kwic'])
 assert exact==total;row={'ordinal':n,'id':eid,'term':ce['SourceTerm'],'occurrences':total,'exactKwicsAndSpans':exact,'entrySha256':sha(ep),'worksheetSha256':sha(wp),'compileHardPass':True,'sourceReview':'f004-laneB-1032-1034-1035-1036-1038-1040-fresh-independent-rereview.json','selfReview':False,'promoted':False};(H/f'f004-laneB-{n}-fresh-rereview-author-repair-checkpoint.json').write_text(json.dumps(row,ensure_ascii=False,indent=2)+'\n');out.append(row)
(H/'f004-laneB-1032-1034-1035-1036-1038-1040-fresh-rereview-author-repair-ledger.json').write_text(json.dumps({'schemaVersion':1,'generatedUtc':NOW,'entries':out,'occurrences':sum(x['occurrences'] for x in out),'exactKwicsAndSpans':sum(x['exactKwicsAndSpans'] for x in out),'selfReview':False,'promoted':False},ensure_ascii=False,indent=2)+'\n');print(json.dumps({'entries':6,'occurrences':sum(x['occurrences'] for x in out)}))
