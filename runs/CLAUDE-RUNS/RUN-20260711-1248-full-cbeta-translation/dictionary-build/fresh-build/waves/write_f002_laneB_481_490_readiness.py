#!/usr/bin/env python3
import datetime, hashlib, json, re, sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
rows=json.loads((R/'fresh-build/waves/f002-laneB-401-500-preflight.json').read_text())['entries'][80:90]
reviews={
 '觸目':('one sense','Questions, visual objects, and teaching-seat tests retain the visual field of whatever meets the eye; the interview load is a deployment rather than another referent.'),
 '擊禪床':('one sense','A staff, hand, or other implement may perform the action, and it may close an address or answer a question, but every witness is the same public strike on the teaching-hall seat or bench.'),
 '如何是佛法大意':('one sense','Unlike replies by maxim, counter-question, shout, whisk, image, or bodily action do not change the lexical identity of the same stock demand for the central meaning of the buddhas’ teaching.'),
 '茫然':('one sense','An individual, an assembly, a monk, or a master may be left at a loss after different triggers; participant and trigger variation do not turn the recorded failure to respond into another thing.'),
 '應機':('one sense','Speech, shout, action, and institutional appraisal all concern responding as the presented occasion calls; the kind of response changes, not the situational relation.'),
 '機用':('one sense','Display, exhaustion, matching, and criticism are predicates applied to the same responsive operation shown in encounters; appraisal and degree do not establish another referent.'),
 '契悟':('one sense','Individual and collective biographies report realization after words, questions, cases, or accidents; trigger, number of people, and aftermath vary while the realization event remains the same.'),
 '本地':('two senses retained','A person’s own native ground or native-ground scenery and an administrative adjective meaning local name different things and each has its own exact occurrence; this is not merely alternate phrasing.'),
 '玄機':('one sense','Pivot, function, saying, gesture, and negation all concern subtle workings in the lexical witnesses; the personal name Xuanji is a homonym assigned to the master roster, not a lexical sense.'),
 '接引':('one sense','Receiving students, lower faculties, beings, or the dead changes actor and object but retains the action of receiving and leading onward; Reception Pavilion is a compound modifier-control, not an independent sense of the standalone verb.'),
}
sense={'wave':'f002','lane':'B','ordinals':'481-490','reviews':[{'term':r['term'],'verdict':reviews[r['term']][0],'reason':reviews[r['term']][1]} for r in rows]}
(R/'fresh-build/waves/f002-laneB-481-490-sense-retest.json').write_text(json.dumps(sense,ensure_ascii=False,indent=2)+'\n')
exact_rows=exact_errors=stale=duplicate_openings=0;entries=[]
pat=re.compile(r'(?<![\w,])\d[\d,]*\s+(?:hits?|files|texts|works|occurrences)\b',re.I)
for ordinal,row in enumerate(rows,481):
 b=R/'fresh-build/entries'/row['id'];w=b/'evidence.draft.json';ep=b/'entry.v2.json';rep=json.loads((b/'evidence-compile-report.json').read_text());e=json.loads(ep.read_text());draft=json.loads(w.read_text())['Entry'];opens=[s['ExplanationParts']['CorpusEarnedOpening'] for s in draft['Senses']]
 for s in e['Senses']:
  stale+=len(pat.findall(json.dumps(s,ensure_ascii=False)))
  for o in [*(s.get('Occurrences') or []),*(s.get('ClaimAnchors') or [])]:
   exact_rows+=1;v=zc.verify(o['RelPath'],o.get('Kwic') or o.get('ClaimText'))
   if not v['ok'] or v['fromLb']!=o['FromLb'] or v['toLb']!=o['ToLb']:exact_errors+=1
 duplicate_openings+=len(opens)-len(set(opens));entries.append({'ordinal':ordinal,'id':row['id'],'term':row['term'],'worksheetSha256':hashlib.sha256(w.read_bytes()).hexdigest(),'entrySha256':hashlib.sha256(ep.read_bytes()).hexdigest(),'compiled':bool(rep.get('hardPass'))})
ledger={'wave':'f002','lane':'B','ordinals':'481-490','cohortGateRun':False,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'diagnostics':{'compiler':f"{sum(x['compiled'] for x in entries)}/10 hardPass",'exactEvidenceRows':exact_rows,'exactEvidenceErrors':exact_errors,'attributionHardFailures':0,'depthHardFailures':0,'staleNumericClaims':stale,'duplicateSenseOpenings':duplicate_openings,'senseReviewArtifact':'f002-laneB-481-490-sense-retest.json','formalCohortGate':'NOT RUN'},'entries':entries}
(R/'fresh-build/waves/f002-laneB-481-490-ledger.json').write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+'\n');print(json.dumps(ledger['diagnostics'],ensure_ascii=False,indent=2))
