import datetime, hashlib, json, subprocess, sys
from pathlib import Path

R=Path(__file__).resolve().parents[2]; E=R/'fresh-build/entries'; W=R/'fresh-build/waves'
sys.path.insert(0,str(R)); import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat()
REVIEW=W/'f004-cohort1-round3-independent-rereview.json'
review=json.loads(REVIEW.read_text()); revise=[x for x in review['entries'] if x['verdict']=='REVISE']; keep=[x for x in review['entries'] if x['verdict']=='KEEP']
before={x['id']:hashlib.sha256((E/x['id']/'entry.v2.json').read_bytes()).hexdigest() for x in keep}
RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']

def unnamed(o,label,role,reason):
 o['MasterName']=None;o['ContextMasters']=[]
 o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':label,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':reason,'ReviewedBy':'Codex f004 cohort1 round4 delta repair','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True}
 o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':reason,'FullCaseDecision':reason}
 o['AttributionNote']=f"Source text ({zc.title(o['RelPath'])}; {o['RelPath']}). Exact actor: {label}. {reason}"

def nonmaster(o,label,role,reason):
 o['MasterName']=None;o['ContextMasters']=[]
 o['ActorAttribution']={'Status':'identified-non-master','Kind':label,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':reason,'ReviewedBy':'Codex f004 cohort1 round4 delta repair','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True}
 o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':reason,'FullCaseDecision':reason}
 o['AttributionNote']=f"Source text ({zc.title(o['RelPath'])}; {o['RelPath']}). Exact actor: {label}. {reason}"

def recut(o,query,ctx=20):
 hit=zc.find(o['RelPath'],query,ctx=ctx,limit=10)[0]; v=zc.verify(o['RelPath'],hit['window'])
 o['Kwic']=hit['window'];o['FromLb']=v['fromLb'];o['ToLb']=v['toLb']

def load(eid):return json.loads((E/eid/'evidence.draft.json').read_text())
def occs(d):return [o for s in d['Entry']['Senses'] for o in s['Occurrences']]
def save(eid,d):
 p=E/eid; draft=p/'evidence.draft.json';draft.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
 subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(draft),'--output',str(p/'entry.v2.json'),'--report',str(p/'round4-delta-report.json')],check=True,stdout=subprocess.DEVNULL)

d=load('t_ef00d55c2d8b');x=occs(d)[1];recut(x,'野老猶󵞪祭鼓聲',20);unnamed(x,'the unnamed individual poem author','verse-author','The headword occurs in an uncued poem after the stove-spirit narrative; the enclosing Pizao exchange does not prove that Pizao authored this poem.');save('t_ef00d55c2d8b',d)
d=load('t_c5ff2fdc37ca');s=d['Entry']['Senses'][0];s['ExplanationParts']={'CorpusEarnedOpening':'Meeting conditions is itself the source.','EvidenceBody':['Formal addresses pair the phrase with acting as host wherever one is; the stored passages present the formula directly without defining a hidden metaphysical source.']};s['DraftEvidence']['ZenBend']=s['ExplanationParts']['EvidenceBody'][0];save('t_c5ff2fdc37ca',d)
d=load('t_085b87d75535');x=occs(d)[0];recut(x,'箇空皮袋開口便納敗',25);unnamed(x,'the unnamed individual poem author','verse-author','The headword is in a verse/commentarial unit in Zhongfeng’s record; the preceding monk’s turn ends before it, and the surviving frame does not safely identify the individual author.');save('t_085b87d75535',d)
d=load('t_71f85cf94e5d');o=occs(d);unnamed(o[2],'the unnamed individual poem author','verse-author','The headword occurs in one poem among an uncued sequence of verses; the compilation frame does not identify this individual author.');nonmaster(o[4],'Jingfu, the named preface writer','compiler','The dated signature names the writer as the monk Jingfu; this first-person preface is his authored prose, not anonymous compiler narration.');save('t_71f85cf94e5d',d)
d=load('t_7c5f24652dfa');o=occs(d);recut(o[0],'僧問如何是法身向上事',15);unnamed(o[0],'the unnamed monk asking Longya','questioner','The headword is inside the monk’s question; Longya’s answer follows it, so the respondent is not assigned backward.');recut(o[1],'曰：如何是法身向上事',15);unnamed(o[1],'the unnamed monk asking Jingqing','questioner','The headword is inside the monk’s follow-up question; Jingqing’s answer follows it, so the respondent is not assigned backward.');save('t_7c5f24652dfa',d)
d=load('t_1fe4eac13d6e');x=occs(d)[2];recut(x,'臨濟入門便喝，德山入門便棒',18);unnamed(x,'the unnamed monk asking Lingyin Xuanben','questioner','The headword is inside the monk’s question comparing Linji and Deshan; Lingyin’s answer follows it, so the respondent is not assigned backward.');save('t_1fe4eac13d6e',d)

after={x['id']:hashlib.sha256((E/x['id']/'entry.v2.json').read_bytes()).hexdigest() for x in keep}
assert before==after
check={'schemaVersion':'f004-cohort1-round4-delta-checklist-v1','sourceReview':REVIEW.name,'sourceReviewSha256':hashlib.sha256(REVIEW.read_bytes()).hexdigest(),'repaired':[{'ordinal':x['ordinal'],'id':x['id'],'term':x['term'],'findings':x['findings'],'resolved':True,'entrySha256':hashlib.sha256((E/x['id']/'entry.v2.json').read_bytes()).hexdigest()} for x in revise],'preservedKeepHashes':before,'all15KeepsByteIdentical':True}
(W/'f004-cohort1-round4-delta-checklist.json').write_text(json.dumps(check,ensure_ascii=False,indent=2)+'\n')
