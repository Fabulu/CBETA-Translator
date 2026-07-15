import json,subprocess,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];E=R/'fresh-build'/'entries';M={'來機':('The incoming person’s move or capacity as it presents itself.','A master answers or tests the incoming move—the approach arriving from the interlocutor—and commentators judge whether the response meets what comes.'),'宗匠':('An accomplished leading master or craftsman of the lineage.','Prefaces, patrons, and records use this title to mark recognized mastery and capacity to shape the lineage; it names standing in the house, not an ordinary artisan.'),'法身向上事':('The matter beyond the teaching-body, repeatedly posed as an interview question.','Monks ask “what is the matter beyond the teaching-body?” and masters answer differently; the phrase is the named question-field, not a freestanding abstract answer.')}
for p in E.glob('*/evidence.draft.json'):
 try:d=json.loads(p.read_text());t=d['Entry']['SourceTerm']
 except:continue
 if t not in M:continue
 for s in d['Entry']['Senses']:s['ExplanationParts']={'CorpusEarnedOpening':M[t][0],'EvidenceBody':[M[t][1]]};s['DraftEvidence']['ZenBend']=M[t][1]
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(p.parent/'entry.v2.json'),'--report',str(p.parent/'round2-prose-residual-compile.json')],check=True,stdout=subprocess.DEVNULL)
