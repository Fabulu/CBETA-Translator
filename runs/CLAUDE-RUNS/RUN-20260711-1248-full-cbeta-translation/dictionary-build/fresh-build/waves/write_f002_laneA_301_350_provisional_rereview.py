#!/usr/bin/env python3
import datetime, hashlib, json
from pathlib import Path
R=Path(__file__).resolve().parents[2]
rows=json.loads((R/'fresh-build/waves/f002-laneA-301-400-preflight.json').read_text())['entries'][:50]
prior={v['id']:v for v in json.loads((R/'fresh-build/waves/f002-laneA-301-350-independent-semantic-verdicts.json').read_text())['verdicts']}
revisions={
't_6c58ed7a7c6c':'The preferred target was improved, but the article still repeatedly replaces 三昧 with “complete command,” and its note explicitly defends that replacement. The required repair asked for Huineng’s corpus definition and a searchable rendering without an unattested competence claim; that repair is incomplete.',
't_7653f61478aa':'The preferred target now says “stone woman,” but the reader-facing opening and body still define her as “the barren woman,” and the note says “stone woman” is only a search alias. This directly reverses the required flyswatter repair: the animated stone woman must lead, with infertility or impossible birth limited to cases that state it.',
't_15eac1a3b037':'The occurrence fields now correctly make the movement narrated, but every current AttributionNote calls the moving master the “exact actor” and fails to name the narrative speaker. The article also retains vague “a master/a monk/the master” prose. Current hash therefore regresses the required attribution repair.',
't_1a86ee3d406f':'The occurrence fields now correctly make descent narrated, but all eight current AttributionNotes call the descending figure the “exact actor” and omit the narrative speaker. The current hash therefore fails the no-regression check for the exact-ID repair.',
't_694f447dbd89':'The row-by-row MasterName/ActorAttribution split is substantively correct, but the four narrated rows’ current AttributionNotes call Bodhidharma or Datong Ji the “exact actor” and omit the narrative speaker; vague “a monk” prose also remains. Current hash is not attribution-clean.',
't_aab4ca02ec21':'All seven laughter rows are now correctly stored as narrated actions, but every current AttributionNote calls the laughing person the “exact actor” and omits the narrative speaker. Current hash regresses the exact-ID attribution repair.',
't_a784d81e277b':'Occurrence 7 now correctly stores the raised Zhaoqing case as narration, but its current AttributionNote identifies Dahui Zonggao as the exact actor. The nested-case repair is therefore not clean at the reader-facing attribution layer.',
't_2745ffff5972':'Substantive term prose replaced the boilerplate, but current reader-facing prose still uses vague “a monk/the monk/the speaker” attribution and fails the attribution audit.',
't_5ac2c5d1fc1e':'Substantive term prose replaced the boilerplate, but current reader-facing prose still uses vague “the speaker/a monk” attribution and fails the attribution audit.',
't_72e01bbb3474':'Substantive term prose replaced the boilerplate, but current reader-facing prose still uses vague “a master/the monk” attribution and fails the attribution audit.',
't_ab715aa474d5':'Substantive term prose replaced the boilerplate, but current reader-facing prose still uses vague “the teacher” attribution and fails the attribution audit.',
't_af92172da506':'Substantive term prose replaced the boilerplate, but current reader-facing prose still uses vague “a speaker” attribution and fails the attribution audit.',
't_c81bf91e508f':'Substantive term prose replaced the boilerplate, but current reader-facing prose still uses vague “a monk/a master” attribution and fails the attribution audit.',
't_c9ba42aa7e47':'Substantive term prose replaced the boilerplate, but current reader-facing prose still uses vague “the teacher” attribution and fails the attribution audit.',
't_f1eb87aa18ef':'Substantive term prose replaced the boilerplate, but current reader-facing prose still uses vague “a monk/one master” attribution and fails the attribution audit.',
}
exact_good={
't_986000f3d4d3':'The Buddha–Kashyapa scene now leads the article, distinguishes the two actors, preserves entrustment, and includes affirmative, hostile, and cautionary later receptions.',
't_18b083a026ba':'The two different acts remain split, and sense 2 now leads with the attested act “take up a saying or question”; the sustained-attention continuation is tied to the stored Zhongfeng instructions rather than built into the PreferredTarget.',
}
verdicts=[]
for ordinal,row in enumerate(rows,301):
 p=R/'fresh-build/entries'/row['id']/'entry.v2.json';sha=hashlib.sha256(p.read_bytes()).hexdigest();bad=revisions.get(row['id'])
 if bad: verdict='REVISE';notes=bad
 else:
  verdict='KEEP';notes=exact_good.get(row['id'],'The repeated process and scope boilerplate is absent. The current article gives a term-specific definition, Chan deployment, and corpus-bounded limit grounded in its stored evidence; no unresolved exact-ID defect or semantic regression was found in this read-only rereview.')
 verdicts.append({'ordinal':ordinal,'id':row['id'],'term':row['term'],'entrySha256':sha,'priorEntrySha256':prior[row['id']]['entrySha256'],'provisionalIndependentVerdict':verdict,'reviewNotes':notes,'formalizationCondition':'Formal only if the focused cohort gate confirms this exact entrySha256 and reports no hard failure.'})
out={'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f002','lane':'A','ordinals':[301,350],'reviewMode':'provisional-independent-semantic-rereview-read-only','entryEditsMade':False,'formal':False,'formalizationRule':'This rereview becomes formal only for entries whose focused-gate output is hard-pass and whose entry SHA-256 is identical to the hash recorded here. Any changed hash requires rereview.','systemicBoilerplateCheck':{'entriesChecked':50,'oldProcessSentenceRemaining':0,'oldScopeSentenceRemaining':0,'finding':'All 50 articles replaced the repeated process prose with substantive term-specific prose.'},'currentMechanicalSnapshot':{'compilerHardPass':'50/50','exactEvidenceRows':322,'exactEvidenceErrors':0,'depthHardFailures':0,'attributionHardFailures':49,'attributionAffectedEntries':13,'note':'The pending focused gate has not yet cleared the current hashes; the 13 affected entries are provisionally REVISE.'},'verdictCounts':{'KEEP':sum(v['provisionalIndependentVerdict']=='KEEP' for v in verdicts),'REVISE':sum(v['provisionalIndependentVerdict']=='REVISE' for v in verdicts)},'exactIdFindings':{'passed':['拈華微笑','提起'],'incompleteSemantic':['一行三昧','石女'],'attributionRegressed':['便下座','歸方丈','面壁','呵呵大笑','應諾']},'verdicts':verdicts}
(R/'fresh-build/waves/f002-laneA-301-350-provisional-independent-semantic-rereview.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'verdictCounts':out['verdictCounts'],'attributionHardFailures':49,'artifact':'f002-laneA-301-350-provisional-independent-semantic-rereview.json'},ensure_ascii=False,indent=2))
