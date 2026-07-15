import datetime,hashlib,json,subprocess,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];E=R/'fresh-build/entries';W=R/'fresh-build/waves';sys.path.insert(0,str(R));import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
review_path=W/'f004-cohort1-round4-delta-independent-rereview.json';review=json.loads(review_path.read_text());rev=[x for x in review['entries'] if x['verdict']=='REVISE'];keep=[x for x in review['entries'] if x['verdict']=='KEEP']
before={x['id']:hashlib.sha256((E/x['id']/'entry.v2.json').read_bytes()).hexdigest() for x in keep};assert all(before[x['id']]==x['reviewedSha256'] for x in keep)
def load(eid):return json.loads((E/eid/'evidence.draft.json').read_text())
def occs(d):return [o for s in d['Entry']['Senses'] for o in s['Occurrences']]
def recut(o,q,ctx=20):
 h=zc.find(o['RelPath'],q,ctx=ctx,limit=10)[0];v=zc.verify(o['RelPath'],h['window']);o.update(Kwic=h['window'],FromLb=v['fromLb'],ToLb=v['toLb'])
def named(o,name,why):
 o['MasterName']=name;o['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}];o.pop('ActorAttribution',None)
 o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':name,'SpeechFrame':why,'FullCaseDecision':why}
 o['AttributionNote']=f"Source text ({zc.title(o['RelPath'])}; {o['RelPath']}). Exact actor: {name}. {why}"
def nullactor(o,label,status,role,why):
 o['MasterName']=None;o['ContextMasters']=[];o['ActorAttribution']={'Status':status,'Kind':label,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':why,'ReviewedBy':'Codex f004 cohort1 round5 delta repair','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True}
 o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':why,'FullCaseDecision':why};o['AttributionNote']=f"Source text ({zc.title(o['RelPath'])}; {o['RelPath']}). Exact actor: {label}. {why}"
def save(eid,d):
 p=E/eid;draft=p/'evidence.draft.json';draft.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(draft),'--output',str(p/'entry.v2.json'),'--report',str(p/'round5-delta-report.json')],check=True,stdout=subprocess.DEVNULL)

d=load('t_ef00d55c2d8b');o=occs(d);named(o[3],'Nanyang Huizhong','The explicit national-teacher-lamented frame introduces Nanyang Huizhong’s headword-bearing poem.');recut(o[5],'因齋時聞鼓聲',0);nullactor(o[5],'the source compiler narrating the meal-time event','narrated','compiler','The headword belongs to narrative setup reporting that the drum was heard at mealtime; Yunmen’s speech begins afterward.');save('t_ef00d55c2d8b',d)
d=load('t_c5ff2fdc37ca');named(occs(d)[0],'Shiwu Qinggong','The enclosing heading names Shiwu Qinggong and the uninterrupted entrance and public-address unit is his speech.');save('t_c5ff2fdc37ca',d)
d=load('t_085b87d75535');named(occs(d)[0],'Zhongfeng Mingben','The immediate self-eulogy heading in Zhongfeng’s own extended record identifies Zhongfeng Mingben as the poem’s voice.');save('t_085b87d75535',d)
d=load('t_1fe4eac13d6e');o=occs(d);recut(o[3],'問臨濟入門便喝',35);nullactor(o[3],'the unnamed monk asking Zhimen Guangzuo','reviewed-unnamed','questioner','The anonymous question continues through the comparison; Zhimen Guangzuo’s answer follows it and is not assigned backward.');named(o[4],'Tiantong Pu','The full unit explicitly opens with Tiantong Pu’s public-address heading, and the headword remains in that uninterrupted address.');save('t_1fe4eac13d6e',d)

after={x['id']:hashlib.sha256((E/x['id']/'entry.v2.json').read_bytes()).hexdigest() for x in keep};assert before==after
check={'schemaVersion':'f004-cohort1-round5-delta-checklist-v1','sourceReview':review_path.name,'sourceReviewSha256':hashlib.sha256(review_path.read_bytes()).hexdigest(),'repaired':[{'ordinal':x['ordinal'],'id':x['id'],'term':x['term'],'findings':x['findings'],'resolved':True,'entrySha256':hashlib.sha256((E/x['id']/'entry.v2.json').read_bytes()).hexdigest()} for x in rev],'preservedKeepHashes':before,'allKeepsByteIdentical':True}
(W/'f004-cohort1-round5-delta-checklist.json').write_text(json.dumps(check,ensure_ascii=False,indent=2)+'\n')
