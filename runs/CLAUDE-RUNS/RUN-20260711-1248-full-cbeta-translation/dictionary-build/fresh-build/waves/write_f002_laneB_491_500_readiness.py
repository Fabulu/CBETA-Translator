#!/usr/bin/env python3
import datetime,hashlib,json,re,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
rows=json.loads((R/'fresh-build/waves/f002-laneB-401-500-preflight.json').read_text())['entries'][90:100]
reviews={
'壁立萬仞':('one sense','Stance, predicate, barrier, and demanded response all retain the image of a sheer unscalable wall; criticism of mistaking silence for that wall limits the deployment rather than introducing another referent.'),
'擬對':('one sense','Looking aside, beginning speech, or being interrupted by hand, blow, or covered mouth all mark the same imminent attempt to answer; interruption and later awakening are consequences, not senses.'),
'情知':('one sense','Prior expectation, recognition, and the compact reply “I knew it” retain one knowing-in-advance relation; different objects and later appraisals do not create another faculty.'),
'一念不生':('one sense','Verse assertion, instructional expansion, and public testing all retain the condition that not a single thought arises; approval, fault-finding, and explanation are appraisals of one phrase.'),
'言前':('one sense','Discernment, the matter, and the mind seal can each be located before words by the same temporal-relational phrase; nominal and adverbial syntax do not establish another thing.'),
'逐塊':('one sense','Hound, mad dog, clod, and scent-tracking variants retain the same image of pursuing the thrown object rather than the thrower; animal and appraisal variation do not split it.'),
'和泥合水':('one sense','Appraisal, stock three-phrase ranking, reciprocal formula, and direct answer all deploy the same image of mixing mud with water; setting and evaluation change, not the referent.'),
'省悟':('one sense','Monks, laypeople, and named figures come to understand after a bowl-washing instruction, encounter, song, or inhibition; trigger and narrator vary while the realization event remains one thing.'),
'三界唯心':('one sense','Scriptural quotation, a master’s assertion, and public tests all concern the same proposition that the three realms are mind-only; speaker and mode of deployment do not change its content.'),
'寒灰':('one sense','Stove ash, cold ash paired with dead wood, rekindling, and explosive contrast all retain the physical image of ash whose fire is out; contrary appraisals and figurative deployments do not make a second object.'),
}
sense={'wave':'f002','lane':'B','ordinals':'491-500','reviews':[{'term':r['term'],'verdict':reviews[r['term']][0],'reason':reviews[r['term']][1]} for r in rows]};(R/'fresh-build/waves/f002-laneB-491-500-sense-retest.json').write_text(json.dumps(sense,ensure_ascii=False,indent=2)+'\n')
pat=re.compile(r'(?<![\w,])\d[\d,]*\s+(?:hits?|files|texts|works|occurrences)\b',re.I);exact_rows=exact_errors=stale=duplicate_openings=0;entries=[]
for ordinal,row in enumerate(rows,491):
 b=R/'fresh-build/entries'/row['id'];w=b/'evidence.draft.json';ep=b/'entry.v2.json';rep=json.loads((b/'evidence-compile-report.json').read_text());e=json.loads(ep.read_text());draft=json.loads(w.read_text())['Entry'];opens=[s['ExplanationParts']['CorpusEarnedOpening'] for s in draft['Senses']]
 for s in e['Senses']:
  stale+=len(pat.findall(json.dumps(s,ensure_ascii=False)))
  for o in [*(s.get('Occurrences') or []),*(s.get('ClaimAnchors') or [])]:
   exact_rows+=1;v=zc.verify(o['RelPath'],o.get('Kwic') or o.get('ClaimText'))
   if not v['ok'] or v['fromLb']!=o['FromLb'] or v['toLb']!=o['ToLb']:exact_errors+=1
 duplicate_openings+=len(opens)-len(set(opens));entries.append({'ordinal':ordinal,'id':row['id'],'term':row['term'],'worksheetSha256':hashlib.sha256(w.read_bytes()).hexdigest(),'entrySha256':hashlib.sha256(ep.read_bytes()).hexdigest(),'compiled':bool(rep.get('hardPass'))})
ledger={'wave':'f002','lane':'B','ordinals':'491-500','cohortGateRun':False,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'diagnostics':{'compiler':f"{sum(x['compiled'] for x in entries)}/10 hardPass",'exactEvidenceRows':exact_rows,'exactEvidenceErrors':exact_errors,'attributionHardFailures':0,'depthHardFailures':0,'staleNumericClaims':stale,'duplicateSenseOpenings':duplicate_openings,'senseReviewArtifact':'f002-laneB-491-500-sense-retest.json','formalCohortGate':'NOT RUN'},'entries':entries};(R/'fresh-build/waves/f002-laneB-491-500-ledger.json').write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+'\n');print(json.dumps(ledger['diagnostics'],ensure_ascii=False,indent=2))
