#!/usr/bin/env python3
import datetime,hashlib,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parent;sys.path.insert(0,str(ROOT.parent));import zc
src=ROOT/'waves'/'f004-laneB-1031-1040-reviewer10-repair-author-ledger.json';ledger=json.loads(src.read_text(encoding='utf-8'))
R={
1031:('KEEP',["All six uses are narrated visit/consultation language rather than utterances of the headword; visitor, respondent, and person-described links now preserve the named figures.","The opening distinguishes public consultation from generic information-seeking, and the retained work IDs match the six witnesses."]),
1032:('REVISE',["o2-o3 still use generic anthology/commentator labels where the full verse unit must be checked for a named verse author.","o4 says 'named local-gentry invitation authors' but supplies no names; this is a placeholder description, not exact attribution.","o6 is first-person ancestral-rite address language (北山忝為末裔…家醜…外揚); resolve the master delivering the address instead of calling him a non-master author."]),
1034:('REVISE',["o5 contains 師拈云：這老宿大似徐六擔板, a direct master's comment; 'the Fayuyi Niangu record owner' cannot be retained as an identified non-master actor.","The other five named utterers and the one-sided-vision explanation are sound, but one wrong direct actor blocks the entry."]),
1035:('REVISE',["o5 is an expository quotation in Zongjing Lu (菩提心者…如除毒藥), not securely the compiler's own utterance; exact quoted/document voice remains unresolved.","That witness is also ordinary poison/antidote comparison rather than a saying or answer becoming poison. Re-test the different-thing/deployment decision: remove it as contained-only or split a genuinely attested literal substance sense rather than blur it into the relational Zen sense."]),
1036:('REVISE',["o1 is a direct Taiping hall address (上堂云…李廣神箭); the master must be named, not labeled a non-master hall-address author.","o2 and o4 still collapse distinct verse/commentary units into generic anthology/commentator labels; inspect and name their authors where the units provide them.","The Li Guang stone-piercing-arrow explanation itself passes #0g." ]),
1037:('KEEP',["All seven headwords sit in the named masters' own comments or addresses; quoted case figures are separated in ContextMasters.","Release/seize is correctly described as observable control of encounter, with work IDs, depth, and one-sense decision supported."]),
1038:('REVISE',["o1 is biographical narration that Feiyin completed the continuation; Mingjue Cong did not utter the stored headword in this case.","o2 is the letter author's statement '吾徑山容和尚…遂著《五燈嚴統》'; Feiyin Tongrong is the book's discussed author, not the utterer of that sentence.","The title definition is useful, but two of four exact actors are reversed, so the entry cannot pass attribution." ]),
1039:('KEEP',["All six complete units place the formula inside the named master's own address; duplicate Miyun editions are controlled by distinct canonical work IDs rather than file count.","The opening states both the single-transmission and direct-pointing claims without splitting grammar or importing outside interpretation."]),
1040:('REVISE',["o5 retains the fabricated placeholder ContextMasters value 'Tianning Address Record Owner'. Resolve the named master who descends and dances, or mark the performer genuinely unresolved after the ladder.","The other movement cases correctly keep the narrator as actor and the descending master as person-described; the teaching-seat #0g explanation is strong." ]),
}
out={'schemaVersion':1,'reviewType':'independent-postrepair-full-case-review','reviewer':'reviewer11','generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'sourceLedger':src.name,'sourceLedgerSha256':hashlib.sha256(src.read_bytes()).hexdigest(),'entries':[],'summary':{},'reviewIntegrity':{'entriesEdited':False,'promoted':False,'merged':False,'published':False}}
exact=hashes=0
for row in ledger['entries']:
 ep=ROOT/'entries'/row['id']/'entry.v2.json';raw=ep.read_bytes();e=json.loads(raw);hm=hashlib.sha256(raw).hexdigest()==row['entrySha256'];hashes+=hm;checks=[]
 for s in e['Senses']:
  for o in s['Occurrences']:
   v=zc.verify(o['RelPath'],o['Kwic']);checks.append(bool(v.get('ok') and v.get('fromLb')==o['FromLb'] and v.get('toLb')==o['ToLb']))
 exact+=sum(checks);ver,find=R[row['ordinal']]
 out['entries'].append({'ordinal':row['ordinal'],'id':row['id'],'term':row['term'],'reviewedSha256':row['entrySha256'],'hashMatches':hm,'occurrencesRead':len(checks),'exactKwicsAndSpans':sum(checks),'verdict':ver,'findings':find})
keep=sum(x['verdict']=='KEEP' for x in out['entries']);out['summary']={'entriesReviewed':len(out['entries']),'fullCasesRead':sum(x['occurrencesRead'] for x in out['entries']),'currentHashMatches':hashes,'exactKwicsAndSpans':exact,'keep':keep,'revise':len(out['entries'])-keep,'genericActorLabelsAbsent':False,'systemicFinding':"The repair removed most generic labels and corrected work IDs, but six entries retain at least one source-type label, reversed utterer/subject assignment, or fabricated ContextMasters value. Mechanical green does not cure those source-level defects."}
(ROOT/'waves'/'f004-laneB-1031-1040-reviewer11-postrepair.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(out['summary'],ensure_ascii=False,indent=2))
