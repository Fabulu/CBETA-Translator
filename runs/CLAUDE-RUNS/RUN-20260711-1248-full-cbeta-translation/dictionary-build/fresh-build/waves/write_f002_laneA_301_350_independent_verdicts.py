#!/usr/bin/env python3
"""Emit the read-only independent semantic verdicts for A301-350."""
import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
R=Path(__file__).resolve().parents[2]
S=R/'fresh-build/waves/f002-laneA-301-350-independent-semantic-review.json'
A=R/'fresh-build/waves/f002-laneA-301-350-gate-attribution-packets.json'
O=R/'fresh-build/waves/f002-laneA-301-350-independent-semantic-verdicts.json'
src=json.loads(S.read_text());pack=json.loads(A.read_text())
common=[
 "Delete the repeated process boilerplate, ‘The expression … occurs in the cited questions, answers, actions, narration, or verse.’ It neither defines the headword nor truthfully identifies which deployment types this entry actually stores.",
 "Delete the repeated scope boilerplate, ‘This sense remains limited to those deployments and the explicit contrasts stated in the opening,’ and replace it with term-specific limits or counterexamples earned by the stored cases.",
 "Re-read the entry after that removal so its opening and body explain where Chan bends this particular word rather than describing annotation procedure."
]
specific={
 305:["Retest ‘complete command of the single conduct’: it is not a transparent English gloss of 一行三昧. Lead with the corpus's own Huineng definition and use a searchable English rendering that does not silently replace 三昧 with an unattested competence claim."],
 324:["Keep the Buddha–Kashyapa scene and its hostile as well as affirmative receptions visible; do not let the generic filler obscure that this is the corpus's deployed Zen Buddha scene."],
 336:["Repair item-20 attribution: 便下座 is narrated bodily action in several rows. MasterName currently names the person descending as though he uttered the headword. Narrated action requires null MasterName, narrator ActorAttribution, and the descending master only as person-described/context."],
 339:["Repair item-20 attribution throughout: 歸方丈 is narrated movement, yet all eight rows put the moving master in MasterName. Use narrator ActorAttribution and retain Nanquan, Baizhang, Yaoshan, Linyang, Fengxue, Danyuan, etc. as contextual persons described, not headword utterers."],
 340:["Split attribution outcomes row by row. Compiler narration of Bodhidharma or another master facing the wall must not use that actor as MasterName; actual later speakers who utter the phrase in a hall discourse may retain MasterName. The present entry conflates narrated actor with exact utterer."],
 344:["Retest sense 2 wording. Physical lifting and verbally taking up a case are different acts, but ‘for sustained attention’ adds a purpose not established by every stored 提起 case. Gloss the attested act before any explicitly evidenced continuation."],
 347:["Repair item-20 attribution in the Occurrences: 呵呵大笑 is usually a narrated audible action. The laughing person belongs in context/person-described while the narrative voice owns the headword clause. Preserve direct quoted laughter only where the exact clause is actually spoken."],
 349:["The PreferredTarget ‘barren woman’ erases the flyswatter bend. These cases animate a stone woman beside wooden men: she dances, plays a reed organ, calls back dreams, and impossibly bears a child. Lead with ‘stone woman’; retain infertility/impossible birth only where a case explicitly invokes it."],
 350:["Recheck occurrence 7's nested case: Dahui raises the Zhaoqing–Luoshan exchange, but 慶應諾 is narrative inside the raised case. Do not make Dahui the exact headword utterer merely because he voices the enclosing address."]
}
verdicts=[]
for item in src['items']:
 repairs=[*common,*specific.get(item['ordinal'],[])]
 verdicts.append({'ordinal':item['ordinal'],'id':item['id'],'term':item['term'],'entrySha256':item['sha256'],'independentVerdict':'REVISE','independentReviewer':'Codex /root/feedback_lexicography (read-only cross-lane review)','reviewNotes':' '.join(repairs),'requiredRepairs':repairs})
out={'generatedUtc':datetime.now(timezone.utc).isoformat(),'wave':'f002','lane':'A','ordinals':[301,350],'reviewMode':'independent-read-only','entryEditsMade':False,'mechanicalGateHardPass':True,'occurrenceFullCasesReviewed':pack['occurrences'],'claimAnchorsReviewed':sum(i['claimAnchorCount'] for i in src['items']),'totalEvidenceRowsReviewed':pack['occurrences']+sum(i['claimAnchorCount'] for i in src['items']),'verdictCounts':{'KEEP':0,'REVISE':len(verdicts)},'systemicFinding':'All 50 entries contain the same two reader-facing process sentences. Because those sentences are neither term-specific definitions nor reliable per-entry deployment inventories, every candidate requires revision before promotion. Additional semantic and actor defects are recorded on their exact IDs.','sourceSemanticPacketSha256':hashlib.sha256(S.read_bytes()).hexdigest(),'sourceAttributionPacketSha256':hashlib.sha256(A.read_bytes()).hexdigest(),'verdicts':verdicts}
O.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'output':str(O.relative_to(R)),'KEEP':0,'REVISE':len(verdicts),'evidenceRowsReviewed':out['totalEvidenceRowsReviewed']},ensure_ascii=False))
