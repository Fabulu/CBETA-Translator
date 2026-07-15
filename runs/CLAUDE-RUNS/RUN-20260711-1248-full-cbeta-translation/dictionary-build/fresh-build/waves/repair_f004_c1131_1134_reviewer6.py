#!/usr/bin/env python3
import datetime,hashlib,json,subprocess,sys
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
sys.path.insert(0,str(R));import zc
NOW='2026-07-15T18:00:00Z';REVIEW='Codex f004 lane C reviewer6 findings repair author'
def named(o,name,decision,contexts=()):
 o.pop('ActorAttribution',None);o['MasterName']=name;o['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}]+[{'MasterName':n,'Roles':[r]} for n,r in contexts]
 title=zc.title(o['RelPath']);o['AttributionNote']=f'Source text ({title}). {name}: {decision}'
 o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':name,'SpeechFrame':decision,'FullCaseDecision':decision}
def compile_entry(ident,repair):
 d=R/'fresh-build/entries'/ident;w=d/'evidence.draft.json';x=json.loads(w.read_text());repair(x['Entry']);x['Entry']['CreatedBy']=REVIEW;x['Entry']['WrittenUtc']=NOW;w.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n')
 subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(w),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True)
 return d,x['Entry']
def repair1131(e):
 s=e['Senses'][0];o=s['Occurrences']
 s['ExplanationParts']['EvidenceBody']=['The phrase appears in questions, biographies, and addresses where solitary understanding is contrasted with going out to meet people.']
 named(o[0],'Xuansha Shibei','Xuansha Shibei addresses the assembly that masters everywhere speak of receiving people and benefiting living beings, then poses the three-disabled-person test.')
 named(o[4],'Feiyin Tongrong','Feiyin Tongrong says that his receiving people and benefiting living beings would be surplus talk if the visiting lay people had already seen through it.')
 named(o[5],'Konggu Daocheng','Konggu Daocheng says this is the time to raise the ancestral way, establish the lineage style, spread the teaching, receive people, and benefit living beings.')
 named(o[6],'Tianyin Yuanxiu','Tianyin Yuanxiu describes Wuzhu Daoren becoming a nun, spreading the teaching, receiving people, and benefiting living beings.')
 s['RelatedMasters']=list(dict.fromkeys(x.get('MasterName') for x in o if x.get('MasterName')))
def repair1134(e):
 s=e['Senses'][0];o=s['Occurrences']
 s['ExplanationParts']['EvidenceBody']=['Later speakers quote, verse, criticize, and re-answer the line as a public case phrase while retaining its autumn scene.']
 for i,decision in {
  0:'The later record raises the case in which a monk asks about trees withering and leaves falling, and Yunmen Wenyan answers “the body exposed in the golden wind.”',
  1:'The record owner quotes the case; Yunmen Wenyan owns the answer before the later comment begins.',
  3:'The case-and-verse compilation explicitly introduces Yunmen Wenyan and assigns the exact answer to him before the collected verses.',
  4:'The Blue Cliff Record quotes the monk’s question and Yunmen Wenyan’s exact answer before Yuanwu’s case commentary.',
  6:'The compiled case-and-verse unit explicitly quotes Yunmen Wenyan’s answer before its own verse.'
 }.items(): named(o[i],'Yunmen Wenyan',decision)
 # O3 was already correctly named; O6 remains the source-anonymous compiled verse.
 s['RelatedMasters']=['Yunmen Wenyan']

rows=[]
for n,i,fn in [(1131,'t_edfd0b2afa11',repair1131),(1134,'t_47b3313788e2',repair1134)]:
 d,e=compile_entry(i,fn);exact=[]
 for s in e['Senses']:
  for o in s['Occurrences']:
   v=zc.verify(o['RelPath'],o['Kwic']);assert v['ok'] and v['fromLb']==o['FromLb'] and v['toLb']==o['ToLb'];exact.append(v)
 rows.append({'ordinal':n,'id':i,'term':e['SourceTerm'],'occurrences':len(exact),'exactSpans':len(exact),'entrySha256':hashlib.sha256((d/'entry.v2.json').read_bytes()).hexdigest(),'worksheetSha256':hashlib.sha256((d/'evidence.draft.json').read_bytes()).hexdigest(),'compileHardPass':True})
(H/'f004-laneC-1131-1134-reviewer6-author-repair-ledger.json').write_text(json.dumps({'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'sourceReview':'f004-laneC-1131-1132-1134-reviewer6-final.json','entries':rows,'exactKwicAndFullSpan':'14/14','selfReviewed':False,'promoted':False,'merged':False,'published':False},ensure_ascii=False,indent=2)+'\n')
