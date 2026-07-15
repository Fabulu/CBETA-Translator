from pathlib import Path
import copy,json,subprocess,sys
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
# Restore the excluded literal plant witness as its own provisional sense.
b=R/'fresh-build/entries/t_27a6c937c485';p=b/'evidence.draft.json';w=json.loads(p.read_text());e=w['Entry'];main=e['Senses'][0]
old=json.loads(subprocess.check_output(['git','show','HEAD:runs/CLAUDE-RUNS/RUN-20260711-1248-full-cbeta-translation/dictionary-build/fresh-build/entries/t_27a6c937c485/evidence.draft.json'],cwd=R,text=True))['Entry']['Senses'][0]
literal=next(o for o in old['Occurrences'] if '綠雖千種草' in o['Kwic'])
if len(e['Senses'])==1:
 ls=copy.deepcopy(main);ls['PreferredTarget']='kinds of grass';ls['AlternateTargets']=[];ls['SearchAliases']=['kinds of grass'];ls['Status']='provisional';ls['Validation']='single-source';ls['Occurrences']=[literal];ls['SourceTexts']=[literal['RelPath']];ls['Note']='One literal plant occurrence retained as a separate provisional sense.';ls['ExplanationParts']={'CorpusEarnedOpening':'Kinds of grass, in the literal botanical sense.','EvidenceBody':['One verse contrasts many kinds of green grass with the fragrance of a single orchid. This plant use is distinct from the human and lineage stock sense.']};ls['DraftEvidence']['OpeningClaimEvidenceKeys']=['o1'];ls['DraftEvidence']['ZenBend']=ls['ExplanationParts']['EvidenceBody'][0];ls['DraftEvidence']['IndependentWorkIds']=[f'work:{Path(literal["RelPath"]).stem}'];e['Senses']=[main,ls];p.write_text(json.dumps(w,ensure_ascii=False,indent=2)+'\n')
# Remove a vague generic noun from public prose without changing the sense.
b2=R/'fresh-build/entries/t_020169e22fdb';p2=b2/'evidence.draft.json';t=p2.read_text().replace('the monk from whom one received the precepts','the ordination teacher from whom one received the precepts');p2.write_text(t)
for d in (b,b2):
 q=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(d/'evidence.draft.json'),'--output',str(d/'entry.v2.json'),'--report',str(d/'b1073-1099-delta8-compile.json')]);assert q.returncode==0
# Recut the original Zhaozhou-bridge witness to the unnamed monk's exact question,
# excluding the master's later answer turns from the actor-bearing evidence span.
b3=R/'fresh-build/entries/t_4f3cd3b1c155';p3=b3/'evidence.draft.json';w3=json.loads(p3.read_text());o=w3['Entry']['Senses'][0]['Occurrences'][0];qtext='僧云如何是趙州橋';v=zc.verify(o['RelPath'],qtext);assert v['ok'];o.update(Kwic=qtext,FromLb=v['fromLb'],ToLb=v['toLb']);o['DraftActorProof']['ExactHeadwordClause']=qtext;p3.write_text(json.dumps(w3,ensure_ascii=False,indent=2)+'\n');q=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p3),'--output',str(b3/'entry.v2.json'),'--report',str(b3/'b1073-1099-delta8-compile.json')]);assert q.returncode==0
