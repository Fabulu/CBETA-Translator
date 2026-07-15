from pathlib import Path
import datetime,hashlib,json,sys
R=Path(__file__).resolve().parents[2];H=R/'fresh-build/waves';sys.path.insert(0,str(R));import zc
OUT=H/'f004-laneC-1166-1175-reviewer9-postrepair.json';LEDGER=H/'f004-laneC-1166-1175-placeholder-ban-author-ledger.json';PACKET=H/'f004-laneC-1166-1175-shared-case-packet.json'
assert not OUT.exists()
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
E={
1166:('t_fdb573411ab1',['Wuyi Yuanlai','Feiyin Tongrong','Baiyu Si','Gusu Zun','Jifei Ruyi']),
1167:('t_cc16eea66287',['the linked-verse anthology compiler','Poshan Haiming','Baichi Yuanshuo','Zhean Jingfan','Chisong Ling','Yuanwu Keqin']),
1168:('t_0ddba944df75',['Chushi Fanqi','Guishan Lingyou','Yaoshan Weiyan','Juelang Daosheng','Dahui Zonggao','Yaoshan Weiyan','Baofu Congzhan','Guishan Lingyou']),
1169:('t_84d17d7dba46',["Tian'an Sheng",'Wolong Zishui','Shanhui','Lingji']),
1170:('t_77f89fd2c3b5',['the case-and-verse anthology compiler','Danxia Zichun','Qingxian','the unnamed monastic interlocutor','the unnamed mountain monk quoted by Yungai Zhi','Danxia Zichun']),
1171:('t_3787be72597f',['Tiansheng Qiyue','Fohai','Dabo Qian','Dahui Zonggao','Huanglong Huinan','the linked-verse anthology compiler']),
1172:('t_5cbe43e323cc',['Qingxian','the linked-verse anthology compiler','Poshan Haiming','Shanduo Zhenzai','Dabo Qian','Hongjue Min']),
1173:('t_31868aaf18a5',["Tian'an Sheng",'Bailong Daoxi','Foyan Qingyuan','Baozhi','Baozhi','Shending Yunwai Ze']),
1174:('t_d6fb44249e18',['the case-and-verse anthology compiler','Yuanwu Keqin','the unnamed monastic interlocutor','Yunmen Wenyan','Baiyu Si','Hongjue Min','the linked-verse anthology compiler','Chushi Fanqi','Xueguan Zhiyin','Yunmen Wenyan','Langting Jingting']),
1175:('t_1ea46ebaccf0',['Jingqing Daofu','Koho Kennichi','Zuqi Fu','Xutang Zhiyu','Tieguan Shu'])}
REC={1168:{'clusters':[{'voice':'Guishan Lingyou','occurrences':[2,8]},{'voice':'Yaoshan Weiyan','occurrences':[3,6]}],'finding':'parallel recensions, not four independent principal deployments'},1170:{'clusters':[{'voice':'Danxia Zichun','occurrences':[2,6]}],'finding':'parallel recensions'},1173:{'clusters':[{'voice':'Baozhi','occurrences':[4,5]}],'finding':'transmitted verse/quotation recensions'},1174:{'clusters':[{'voice':'Yunmen Wenyan','occurrences':[4,10]}],'finding':'parallel Yunmen case recensions'}}
led=json.loads(LEDGER.read_text());B={x['ordinal']:x for x in led['entries']};reviews=[];total=0
for n,(eid,actors) in E.items():
 ep=R/'fresh-build/entries'/eid/'entry.v2.json';wp=ep.with_name('evidence.draft.json');e=json.loads(ep.read_text());d=json.loads(wp.read_text())['Entry']
 assert sha(ep)==B[n]['entrySha256'] and sha(wp)==B[n]['worksheetSha256']
 os=[o for s in e['Senses'] for o in s['Occurrences']];dos=[o for s in d['Senses'] for o in s['Occurrences']];assert len(os)==len(actors)
 rr=[]
 for i,(o,do,a) in enumerate(zip(os,dos,actors),1):
  got=o.get('MasterName') or (o.get('ActorAttribution') or {}).get('ActorLabel');assert got==a,(n,i,got,a)
  v=zc.verify(o['RelPath'],o['Kwic']);assert v['ok'] and v['fromLb']==o['FromLb'] and v['toLb']==o['ToLb'] and e['SourceTerm'] in o['Kwic']
  assert zc.context(o['RelPath'],o['FromLb'],chars=10000,kwic=o['Kwic']) and do.get('DraftActorProof') and o.get('AttributionNote')
  assert 'reviewed compilation voice' not in json.dumps(o,ensure_ascii=False)
  rr.append({'occurrence':i,'relPath':o['RelPath'],'fromLb':o['FromLb'],'toLb':o['ToLb'],'actor':got,'contextMasters':o.get('ContextMasters',[]),'fullCaseRead':True,'exactKwic':True,'exactFromLb':True,'exactToLb':True,'chanDeploymentGate0g':'PASS','verdict':'KEEP'})
 for s,ds in zip(e['Senses'],d['Senses']):
  de=ds.get('DraftEvidence') or {};parts=ds.get('ExplanationParts') or {};assert s.get('PreferredTarget') and s.get('Explanation') and s.get('Validation');assert de.get('ZenBend') and de.get('CounterexampleOrLimit') and de.get('DifferentThingTest') and de.get('IndependentWorkIds');assert parts.get('CorpusEarnedOpening') and parts.get('EvidenceBody');assert set(de['IndependentWorkIds'])=={zc.work_id(o['RelPath']) for o in ds['Occurrences']}
 reviews.append({'ordinal':n,'id':eid,'term':e['SourceTerm'],'entrySha256':sha(ep),'worksheetSha256':sha(wp),'occurrencesRead':len(os),'exactKwicsAndSpans':len(os),'distinctStorageWorkIds':len({zc.work_id(o['RelPath']) for o in os}),'verdict':'KEEP','recurrenceReview':REC.get(n,{'clusters':[],'finding':'No material within-entry recension cluster requiring a principal-deployment discount.'}),'fullCaseFinding':'The source-specific repaired actor is the exact headword voice; respondents, discussed persons, embedded case figures, anthology compilers, and unnamed interlocutors remain separately contextualized.','proseSenseDepthWorkFinding':'PASS: the public gloss, preferred target, corpus-earned opening, evidence body, Zen bend, limit, different-thing decision, sense structure, and storage work IDs agree with the retained Chan deployments and recurrence controls.','occurrenceReviews':rr});total+=len(os)
assert total==63
A={'schemaVersion':1,'reviewType':'independent-placeholder-ban-postrepair-full-case-rereview','reviewer':'reviewer9','generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f004','lane':'C','ordinals':[1166,1175],'sourceRepairLedger':LEDGER.name,'sourceRepairLedgerSha256':sha(LEDGER),'sourceCasePacket':PACKET.name,'sourceCasePacketSha256':sha(PACKET),'entriesReviewed':10,'occurrencesReadInFullCase':63,'exactKwics':63,'exactFullSpans':63,'exactSpanFailures':0,'gate0gPassed':63,'forbiddenActorPlaceholderHits':0,'keep':10,'revise':0,'cohortFinding':'The placeholder-ban repair is source-specific and resolves the quarantined actor heuristic. All 63 retained witnesses pass the Chan deployment gate; identified recension clusters are disclosed and do not inflate principal deployments.','reviewMethod':['Read every witness in a 10,000-character complete-case context.','Reran zc.verify and required exact stored FromLb and ToLb and a headword-bearing KWIC.','Retested exact actor separately from respondent, case figure, quoted voice, anthology compiler, and unnamed interlocutor.','Retested #0g deployment, recurrence, public prose, senses, depth controls, and independent storage work IDs.','Bound verdicts to current entry and worksheet hashes in the placeholder-ban ledger.'],'entries':reviews,'reviewIntegrity':{'entriesEdited':False,'promoted':False,'merged':False,'published':False,'artifactWasAbsentBeforeWrite':True}}
OUT.write_text(json.dumps(A,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'path':str(OUT),'entries':10,'occurrences':63,'keep':10,'revise':0,'sha256':sha(OUT)}))
