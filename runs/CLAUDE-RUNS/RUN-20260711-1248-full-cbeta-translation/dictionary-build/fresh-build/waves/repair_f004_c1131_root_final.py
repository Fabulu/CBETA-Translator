#!/usr/bin/env python3
import datetime,hashlib,json,subprocess,sys
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
sys.path.insert(0,str(R));import zc
i='t_edfd0b2afa11';d=R/'fresh-build/entries'/i;w=d/'evidence.draft.json';x=json.loads(w.read_text());e=x['Entry'];o=e['Senses'][0]['Occurrences']

o[0]['AttributionNote']='Source text (古尊宿語錄). Xuansha Shibei addresses the assembly that masters everywhere speak of receiving people and benefiting living beings, then poses the three-disabled-person test.'
o[4]['AttributionNote']='Source text (費隱禪師語錄). Feiyin Tongrong says that his receiving people and benefiting living beings would be surplus talk if the visiting lay people had already seen through it.'
o[5]['AttributionNote']='Source text (空谷道澄禪師語錄). Konggu Daocheng says this is the time to raise the ancestral way, establish the lineage style, spread the teaching, receive people, and benefit living beings.'
o[6]['AttributionNote']='Source text (天隱和尚語錄). Tianyin Yuanxiu describes Wuzhu Daoren becoming a nun, spreading the teaching, receiving people, and benefiting living beings.'
for q in (o[0],o[4],o[5],o[6]):
 q['DraftActorProof']['FullCaseDecision']=q['AttributionNote']

o[3]['ContextMasters']=[{'MasterName':'Guishan Lingyou','Roles':['person-described']}]
o[3]['ActorAttribution']['GrammarEvidence']='In the Guishan Lingyou biography, 一日念道在接物利生獨居非是 is third-person narrative: the biographer reports Guishan reflecting that the Way lies in receiving people and benefiting living beings and that solitary residence is not right.'
o[3]['ActorAttribution']['ReviewedBy']='Codex f004 lane C root-review findings repair author'
o[3]['ActorAttribution']['ReviewedUtc']='2026-07-15T18:20:00Z'
o[3]['AttributionNote']='Source text (指月錄). The record narrator reports Guishan Lingyou reflecting that the Way lies in receiving people and benefiting living beings and that solitary residence is not right; Guishan is the person described, not the utterer of a quoted turn.'
o[3]['DraftActorProof']={'ExactHeadwordClause':'一日念道在接物利生獨居非是','GrammaticalSubject':'the record narrator describing Guishan Lingyou','SpeechFrame':'Third-person biography continues from 師遂往焉 and uses no speech marker before 一日念道.','FullCaseDecision':'The narrator owns the clause; Guishan Lingyou is the person whose reflection is reported.'}

e['CreatedBy']='Codex f004 lane C root-review findings repair author';e['WrittenUtc']='2026-07-15T18:20:00Z';w.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n')
subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(w),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True)
for q in o:
 v=zc.verify(q['RelPath'],q['Kwic']);assert v['ok'] and v['fromLb']==q['FromLb'] and v['toLb']==q['ToLb']
ledger={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'sourceReview':'root focused review after reviewer6 repair','ordinal':1131,'id':i,'term':e['SourceTerm'],'repairs':['Added Guishan Lingyou as person-described in occurrence 4 and documented the third-person biographical grammar.','Removed doubled speaker-name constructions from occurrences 1, 5, 6, and 7 without weakening exact source or speaker attribution.'],'exactKwicAndFullSpan':'7/7','entrySha256':hashlib.sha256((d/'entry.v2.json').read_bytes()).hexdigest(),'worksheetSha256':hashlib.sha256(w.read_bytes()).hexdigest(),'compileHardPass':True,'selfReviewed':False,'promoted':False,'merged':False,'published':False}
(H/'f004-laneC-1131-root-final-author-ledger.json').write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+'\n')
