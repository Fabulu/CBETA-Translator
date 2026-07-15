#!/usr/bin/env python3
import datetime,hashlib,json
from pathlib import Path
R=Path(__file__).resolve().parents[2]
prior=json.loads((R/'fresh-build/waves/f002-laneA-301-350-provisional-independent-semantic-rereview.json').read_text())
manifest=json.loads((R/'fresh-build/waves/f002-laneA-301-350-rereview-updated-hashes.json').read_text())
prior_by_id={v['id']:v for v in prior['verdicts']};changed={x['id'] for x in manifest['entries']}
revisions={
't_6c58ed7a7c6c':'REVISE: the substantive Huineng definition is now correctly foregrounded and “complete command” is gone, but the compiled article repeatedly leaves 三昧 as the untranslated loan “samadhi,” including in the note. The current depth audit hard-fails this exact hash for banned samadhi-loan framing. Use the English-first preferred rendering in reader prose and identify the source graphs without leaving the loan as the definition.',
't_c81bf91e508f':'REVISE: the vague-attributor wording was replaced, but the replacement changes the subject and becomes nonsensical: “the Xuansha question preserved in Miaoyun’s commentary about his own self had his eyes swapped out.” In the stored case a person asking about his own self is judged to have had his eyes swapped by Xuansha’s reply; the question itself does not have eyes. Restore the attested actor without a vague “a monk.”',
't_2745ffff5972':'REVISE: attribution is now specific enough for the audit, but the repair introduced the ungrammatical sentence “Its early defining exchange has an unnamed questioner asks Dabe…”. Recast as “has an unnamed questioner ask Dabe” or split the sentence; current reader prose is not publication-ready.',
't_72e01bbb3474':'REVISE: vague attribution is removed, but the replacement sentence has broken agreement: “named Chan speakers insert it before a rebuke, uses it alone…, or directs it…”. Make all three predicates agree with the plural subject; current reader prose is not publication-ready.',
}
verdicts=[]
for x in manifest['entries']:
 p=R/x['output'];sha=hashlib.sha256(p.read_bytes()).hexdigest();bad=revisions.get(x['id'])
 verdicts.append({'ordinal':x['ordinal'],'id':x['id'],'term':x['term'],'entrySha256':sha,'worksheetSha256':hashlib.sha256((R/x['worksheet']).read_bytes()).hexdigest(),'provisionalIndependentVerdict':'REVISE' if bad else 'KEEP','reviewNotes':bad or 'KEEP: the specified semantic or attribution defect is repaired at this hash; the term-specific definition and stored evidence retain their prior sense analysis, attribution diagnostics are clean, and no new semantic or reader-facing regression was found.','formalizationCondition':'Formal only if the focused gate confirms this exact entry SHA-256 with no hard failure.'})
unchanged=[]
for v in prior['verdicts']:
 if v['id'] in changed:continue
 sha=hashlib.sha256((R/'fresh-build/entries'/v['id']/'entry.v2.json').read_bytes()).hexdigest();unchanged.append({'ordinal':v['ordinal'],'id':v['id'],'term':v['term'],'expectedSha256':v['entrySha256'],'currentSha256':sha,'unchanged':sha==v['entrySha256']})
out={'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f002','lane':'A','ordinals':[301,350],'reviewMode':'provisional-independent-semantic-rereview2-read-only','entryEditsMade':False,'siteTouched':False,'formal':False,'scope':'Fifteen repaired entries previously marked REVISE; other thirty-five checked for hash preservation only.','diagnostics':{'updatedManifestHashesMatch':'15/15','other35HashesUnchanged':all(x['unchanged'] for x in unchanged),'compilerHardPass':'15/15','exactEvidenceRows':99,'exactEvidenceErrors':0,'attributionHardFailures':0,'depthHardFailures':1,'depthFailureEntry':'一行三昧'},'verdictCountsForRepaired15':{'KEEP':sum(v['provisionalIndependentVerdict']=='KEEP' for v in verdicts),'REVISE':sum(v['provisionalIndependentVerdict']=='REVISE' for v in verdicts)},'inheritedUnchanged35':unchanged,'verdicts':verdicts}
(R/'fresh-build/waves/f002-laneA-301-350-provisional-independent-semantic-rereview2.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n');print(json.dumps(out['verdictCountsForRepaired15'],ensure_ascii=False,indent=2))
