#!/usr/bin/env python3
import json,re,subprocess,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2]
pre={r['term']:r for r in json.loads((R/'fresh-build/waves/f002-laneB-401-500-preflight.json').read_text())['entries'][:50]}
openings={
'絕後再甦':'To revive after being cut off: the records use a death-and-revival image in a recurrent teaching-seat formula and in explicit criticism of that formula.',
'金毛師子':'The golden-haired lion appears both as Mañjuśrī’s mount and as an independently deployed animal in answers, appraisals, contrasts, and verse.',
'情解':'A formed, subjective understanding: case commentaries describe it as something generated around sayings, clung to, warned against, or broken through.',
'德山托鉢':'The case title “Deshan Carries His Bowl” names the complete Deshan–Xuefeng–Yantou encounter, not merely the initial movement with a meal bowl.',
'死中得活':'To gain life within death: the phrase functions as a public-interview question, an appraisal, and one side of an explicit death-and-life contrast.',
'俱胝一指':'“Juzhi’s One Finger” names Juzhi’s recurrent public gesture and the case that includes Tianlong, Juzhi, and the attendant boy.',
'一字關':'The one-word barrier is the monastic name for Yunmen’s recurrent single-graph answers, established by an explicit corpus naming formula.',
'婆子燒庵':'“The Old Woman Burns the Hermitage” names the whole encounter among the old woman, the young woman, and the hermitage-dweller, not only its final fire.',
'顧鑒咦':'The expression names Yunmen’s sequence: he looks toward the interlocutor, says “Look!”, and cuts off the attempted answer with “Hah!”',
}
frozen=re.compile(r'\s*Frozen-corpus concordance:.*?independent works\.',re.I)
for term,opening in openings.items():
 row=pre[term];b=R/'fresh-build/entries'/row['id'];wp=b/'evidence.draft.json';w=json.loads(wp.read_text());s=w['Entry']['Senses'][0]
 s['ExplanationParts']['CorpusEarnedOpening']=opening
 if term=='顧鑒咦':
  s['ExplanationParts']['EvidenceBody']=["Yunmen’s own record describes the interaction: he sometimes looked toward the recorded questioner and said “Look!”; when the questioner prepared to answer, Yunmen said “Hah!” The monasteries consequently named Yunmen by this look-and-two-utterance sequence. A collected transmission states the same sequence and adds that Deshan Yuanming later removed the initial look and called the shortened form “drawn look.” Later texts use the compact phrase as a named case, ask how it operates at face-to-face meeting, and place it alongside Yunmen’s other characteristic responses. The punctuation and speaker sequence matter: this is a recorded look and two utterances, not an abstract three-word maxim."]
 for sense in w['Entry']['Senses']:
  sense['Note']=frozen.sub('',sense.get('Note','')).strip()+f" Frozen-corpus concordance: {row['hits']} exact hits in {row['files']} storage files representing {row['works']} independent works."
 wp.write_text(json.dumps(w,ensure_ascii=False,indent=2)+'\n')
 cmd=[sys.executable,str(R/'compile_evidence_draft.py'),str(wp),'--output',str(b/'entry.v2.json'),'--report',str(b/'evidence-compile-report.json')]
 r=subprocess.run(cmd,capture_output=True,text=True)
 if r.returncode:raise SystemExit(term+'\n'+r.stdout+r.stderr)
print('repaired and compiled',len(openings))
