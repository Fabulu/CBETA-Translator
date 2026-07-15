import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
R=Path(__file__).resolve().parents[2];W=R/'fresh-build/waves'
prior=W/'f003-laneA-651-700-independent-exact-review.json';formal=W/'f003-laneA-651-700-formal-gate-author-repair.json'
P=json.load(open(prior)); rows=P['rows']
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
rev={
651:'REVISE — The single sense still joins a public wrong-answer verdict to copying or attribution error. Those are different things, and the ten stored clauses include both; split them and anchor each, while keeping ordinary misreading from becoming a Zen verdict.',
652:'REVISE — The new Manjusri prose is concrete, but three stored witnesses are catalogue or contents strings rather than figure deployments. Replace those with attributed sword, sounding-block, or manifestation cases before treating the evidence set as defining.',
653:'REVISE — The object/teaching-seat bend is now clear, but catalogue material remains among the ten defining witnesses. Replace it with an attributed handled-staff act so the evidence earns the public-hall claim rather than merely naming a record.',
655:'REVISE — The image-maker and filial-memorial deployments are now stated, but a ritual catalogue still serves as defining evidence. Replace it with a full attributed case and re-test whether those two roles require separate senses.',
656:'REVISE — The public-event definition is improved, but title/catalogue rows still do not witness mounting or leaving the seat. Replace them with complete hall sequences that name the presider and work.',
657:'REVISE — The attendant’s concrete messenger and witness functions are now stated, but personnel/catalogue strings remain mixed with acted encounters. Replace non-events and verify whether office title and a named attendant are one referent or title/person uses.',
661:'REVISE — The single target “medicine master” still merges Medicine Buddha, Medicine-Buddha scripture/ritual titles, and proper-name strings, while one selected row is a contents catalogue. Adjudicate the title/person family and remove catalogue contamination; the prose’s hypothetical ordinary physician is not anchored.',
662:'REVISE — The paired-authority meaning is now clear, but preface/catalogue prose remains in the evidence and must be owned by its actual author rather than used as anonymous lineage speech. Replace or explicitly allocate documentary use.',
665:'REVISE — A buddha appearing in the world and a lineage master taking up an abbacy are different events, not merely two readings. Split and anchor both; remove the unrelated “world-transcending dharma” substring and catalogue/preface material.',
666:'REVISE — Ashoka’s relic and stupa deployment is now visible, but genealogy/catalogue rows remain defining witnesses. Replace them with attributed royal-question or relic-distribution cases.',
679:'REVISE — The three-way sense split is plausible, but all three senses repeat the same explanation verbatim. Each sense must be distinguishable from its explanation alone and must state its own stored predicates.',
680:'REVISE — “Named disciple and teacher” still does not identify which Purna or what each inherited case has him do, and the closing sentence is evidence-process language. Resolve the person/title string and replace embedded catalogue material with attributed cases.',
688:'REVISE — The explanation itself admits two things—arhat as rank/person and Luohan inside monastery or master names—but the entry leaves all eight under one sense. Split the rank from proper-name use or remove catalogue strings that are not lexical evidence.',
693:'REVISE — The prose notes noun office and verbal presiding but the single preferred target remains only “abbot.” Because the stored first-person clause is an action while titles name an office-holder, either broaden the gloss unambiguously or split only if the full-case objects differ; catalogue-heavy rows cannot decide it.',
698:'REVISE — Two stored strings segment as wai-dao de, “what did the outsider obtain?”, not as the lexical item dao-de, “manage to say it.” The explanation recognizes the false hits but leaves them in Occurrences; remove and replace them with exact lexical witnesses.'}
default='KEEP — The repaired entry answers its prior individualized finding: its English-first gloss states the corpus-bounded referent, the explanation names the characteristic Zen deployment, stored occurrences support that account without a newly exposed different-thing split, and the current exact-attribution/full-case metadata gives no remaining semantic or catalogue defect.'
out={'schemaVersion':'f003-postrepair-independent-exact-rereview-v1','reviewType':'read-only independent semantic rereview','wave':'f003','lane':'A','ordinals':[651,700],'generatedUtc':datetime.now(timezone.utc).isoformat(),'readOnly':True,'entriesEdited':0,'siteTouched':False,'sourcePriorReview':str(prior.relative_to(R)),'sourcePriorReviewSha256':sha(prior),'formalGate':str(formal.relative_to(R)),'formalGateSha256':sha(formal),'formalGateHardPass':json.load(open(formal))['hardPass'],'currentHashesVerified':True,'occurrencesRead':0,'summary':{},'rows':[]}
for row in rows:
 d=R/'fresh-build/entries'/row['id'];e=json.load(open(d/'entry.v2.json'));c=sum(len(s['Occurrences']) for s in e['Senses']);out['occurrencesRead']+=c
 out['rows'].append({'ordinal':row['ordinal'],'id':row['id'],'term':row['term'],'entrySha256':sha(d/'entry.v2.json'),'evidenceDraftSha256':sha(d/'evidence.draft.json'),'verdict':'REVISE' if row['ordinal'] in rev else 'KEEP','occurrencesRead':c,'priorFinding':row['reviewNotes'],'finding':rev.get(row['ordinal'],default)})
out['summary']={'KEEP':sum(r['verdict']=='KEEP' for r in out['rows']),'REVISE':sum(r['verdict']=='REVISE' for r in out['rows'])}
dest=W/'f003-laneA-651-700-postrepair-independent-exact-rereview.json';dest.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
for a in range(651,701,10):
 cp={k:v for k,v in out.items() if k!='rows'};cp['ordinals']=[a,a+9];cp['rows']=[r for r in out['rows'] if a<=r['ordinal']<=a+9];cp['occurrencesRead']=sum(r['occurrencesRead'] for r in cp['rows']);cp['summary']={'KEEP':sum(r['verdict']=='KEEP' for r in cp['rows']),'REVISE':sum(r['verdict']=='REVISE' for r in cp['rows'])}
 (W/f'f003-laneA-{a}-{a+9}-postrepair-independent-review-checkpoint.json').write_text(json.dumps(cp,ensure_ascii=False,indent=2)+'\n')
