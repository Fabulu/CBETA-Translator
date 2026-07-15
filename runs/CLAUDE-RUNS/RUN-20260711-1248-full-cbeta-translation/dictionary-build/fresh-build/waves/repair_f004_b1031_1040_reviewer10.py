from pathlib import Path
import copy,datetime,hashlib,json,subprocess,sys
R=Path(__file__).resolve().parents[2];H=R/'fresh-build/waves';sys.path.insert(0,str(R));import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
ROWS={1031:'t_bfef2fc85826',1032:'t_b336769aabdf',1034:'t_e21288d0fefb',1035:'t_641de814fd8a',1036:'t_10b63ac74f61',1037:'t_40cfbcc5f859',1038:'t_b016f513be3d',1039:'t_f24a55791323',1040:'t_74c3c0e1b896'}
N=lambda x,ctx=[]:('named',x,ctx);O=lambda x,r='compiler',ctx=[]:('other',x,r,ctx)
FIX={
1031:{2:O('the lamp-record biographer describing Hui’an’s visitors','compiler',[('Tanan','interlocutor'),('Nanyue Huairang','interlocutor'),('Hui’an','respondent')]),6:O('the lamp-record biographer describing Langya Huijue','compiler',[('Langya Huijue','person-described')])},
1032:{2:O('the Chan verse-anthology author on the Buddha-birth case','verse-author'),3:O('the Zongjian Falin verse commentator on Mazu','commentator',[('Mazu Daoyi','case-figure')]),4:O('the named local-gentry invitation authors','compiler'),6:O('the Beishan ancestral-rite address author','utterer'),7:N('Jingshan Rong',[('Nanyang Huizhong','case-figure')])},
1034:{3:N('Yunmen Cheng'),5:O('the Fayuyi Niangu record owner commenting on Zhaozhou','commentator',[('Zhaozhou Congshen','case-figure')])},
1035:{2:N('Huanji'),5:O('the Zongjing Lu expository compiler','compiler'),6:N('Wuye Guoshi')},
1036:{1:O('the Taiping hall-address author invoking Li Guang','utterer'),2:O('the Chan verse-anthology author invoking Li Guang','verse-author'),3:N('Daqian Heqiao Dai'),4:O('the Zongjian Falin commentator invoking Li Guang','commentator',[('Baizhang Huaihai','case-figure')]),5:N('Juelang Daosheng')},
1037:{1:N('Guxiu Yao'),2:N('Baoshou Fang',[('Fenggan','case-figure'),('Hanshan','case-figure')]),3:N('Nanfeng Yongcheng')},
1038:{1:N('Mingjue Cong'),2:N('Feiyin Tongrong'),3:O('the imperial memorial author presenting Wudeng Yantong','compiler',[('Tongrong','person-discussed')]),4:O('the Tongrong biographer describing compilation of Wudeng Yantong','compiler',[('Tongrong','person-described')])},
1040:{1:O('the lamp-record narrator describing Xiuxi’s descent','compiler',[('Xiuxi','person-described'),('Gushan','interlocutor')]),2:O('the Zhaozhou record narrator describing his descent','compiler',[('Zhaozhou Congshen','person-described')]),3:O('the verse-anthology narrator describing Nanquan’s descent','compiler',[('Nanquan Puyuan','person-described'),('Zhaozhou Congshen','questioner')]),4:O('the lamp-record narrator describing Xiuxi’s descent','compiler',[('Xiuxi','person-described'),('Gushan','interlocutor')]),5:O('the imperial-address narrator describing the Tianning record owner’s dance','compiler',[('Tianning Address Record Owner','person-described')]),6:O('the lamp-record narrator describing Yungai Zhi’s descent','compiler',[('Yungai Zhi','person-described'),('Jiufeng Xiguang','questioner')]),7:O('the lamp-record narrator describing Baizhang Weizheng’s descent','compiler',[('Baizhang Weizheng','person-described')])}}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def setx(o,s):
 if s[0]=='named':
  n=s[1];o['MasterName']=n;o.pop('ActorAttribution',None);ctx=[(n,'utterer')]+s[2]
 else:
  label,role,ctx=s[1:];o['MasterName']=None;o['ActorAttribution']={'Status':'identified-non-master','Kind':label,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':f'The complete source unit assigns the exact headword material to {label}.','ReviewedBy':'Codex reviewer10 repair author','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True};n=label
 o['ContextMasters']=[{'MasterName':x,'Roles':r if isinstance(r,list) else [r]} for x,r in ctx];proof=f'Complete-case grammar assigns this exact occurrence to {n} and preserves surrounding named figures separately.';o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {n}. {proof}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':n,'SpeechFrame':proof,'FullCaseDecision':proof}
# Add pending names and context figures with source evidence.
pp=R/'fresh-build/pending-roster.json';pd=json.loads(pp.read_text());have={x['canonicalName'] for x in pd['candidates']}
for n,eid in ROWS.items():
 d=json.loads((R/'fresh-build/entries'/eid/'evidence.draft.json').read_text())['Entry'];os=d['Senses'][0]['Occurrences']
 for i,s in FIX.get(n,{}).items():
  names=([s[1]] if s[0]=='named' else [])+[x for x,_ in (s[2] if s[0]=='named' else s[3]) if not x.startswith('the ')]
  for name in names:
   if name not in have:
    o=os[i-1];pd['candidates'].append({'canonicalName':name,'aliases':[name],'evidence':[{'RelPath':o['RelPath'],'FromLb':o['FromLb'],'ToLb':o['ToLb'],'Kwic':o['Kwic']}],'reviewedBy':'Codex reviewer10 repair author','reviewReport':'fresh-build/waves/f004-laneB-1031-1040-reviewer10-independent.json','status':'awaiting-roster-integration'});have.add(name)
for n,eid in ROWS.items():
 d=json.loads((R/'fresh-build/entries'/eid/'evidence.draft.json').read_text())['Entry']
 for o in d['Senses'][0]['Occurrences']:
  for name in ([o.get('MasterName')] if o.get('MasterName') else [])+[x.get('MasterName') for x in o.get('ContextMasters',[])]:
   if name and name not in have:
    pd['candidates'].append({'canonicalName':name,'aliases':[name],'evidence':[{'RelPath':o['RelPath'],'FromLb':o['FromLb'],'ToLb':o['ToLb'],'Kwic':o['Kwic']}],'reviewedBy':'Codex reviewer10 repair author','reviewReport':'fresh-build/waves/f004-laneB-1031-1040-reviewer10-independent.json','status':'awaiting-roster-integration'});have.add(name)
pp.write_text(json.dumps(pd,ensure_ascii=False,indent=2)+'\n')
out=[]
for n,eid in ROWS.items():
 b=R/'fresh-build/entries'/eid;wp=b/'evidence.draft.json';w=json.loads(wp.read_text());e=w['Entry'];os=e['Senses'][0]['Occurrences']
 for i,s in FIX.get(n,{}).items():setx(os[i-1],s)
 for sense in e['Senses']:sense['DraftEvidence']['IndependentWorkIds']=list(dict.fromkeys(zc.work_id(o['RelPath']) for o in sense['Occurrences']))
 w['Entry']=e;wp.write_text(json.dumps(w,ensure_ascii=False,indent=2)+'\n');ep=b/'entry.v2.json';rp=b/'reviewer10-repair-compile-report.json';q=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(wp),'--output',str(ep),'--report',str(rp)],text=True,capture_output=True)
 if q.returncode:raise SystemExit(q.stdout+q.stderr)
 ce=json.loads(ep.read_text());tot=ok=0
 for ss in ce['Senses']:
  for o in ss['Occurrences']:
   tot+=1;v=zc.verify(o['RelPath'],o['Kwic']);ok+=int(v['ok'] and v['fromLb']==o['FromLb'] and v['toLb']==o['ToLb'] and ce['SourceTerm'] in o['Kwic'])
 assert ok==tot;row={'ordinal':n,'id':eid,'term':ce['SourceTerm'],'occurrences':tot,'exactKwicsAndSpans':ok,'entrySha256':sha(ep),'worksheetSha256':sha(wp),'compileHardPass':True,'selfReview':False,'promoted':False};(H/f'f004-laneB-{n}-reviewer10-repair-author-checkpoint.json').write_text(json.dumps(row,ensure_ascii=False,indent=2)+'\n');out.append(row)
(H/'f004-laneB-1031-1040-reviewer10-repair-author-ledger.json').write_text(json.dumps({'schemaVersion':1,'generatedUtc':NOW,'sourceReview':'f004-laneB-1031-1040-reviewer10-independent.json','entries':out,'occurrences':sum(x['occurrences'] for x in out),'exactKwicsAndSpans':sum(x['exactKwicsAndSpans'] for x in out),'selfReview':False,'promoted':False},ensure_ascii=False,indent=2)+'\n');print(json.dumps({'entries':len(out),'occurrences':sum(x['occurrences'] for x in out)}))
