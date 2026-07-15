import json,subprocess,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2]
M={
651:('Wrong marks an answer, move, or judgment as missing the demanded point rather than merely containing a factual error.','The witnesses use the verdict after concrete replies and actions, so its Zen force is public correction: the fault is shown in what was just done.'),
653:('The staff is the walking and teaching-seat implement a presiding figure carries, raises, plants, throws down, or uses to strike.','Stored actions keep the object concrete while making its handling an immediately visible answer before the assembly.'),
654:("The phrase ‘do not understand’ or ‘does not understand’ reports failed comprehension and can also be the answer deliberately returned in an exchange.",'The witnesses distinguish accusation, self-report, and direct reply; the same grammar does not guarantee the same appraisal.'),
664:('The bamboo switch is a flat bamboo implement held up, named, challenged, or used in the public hall.','The corpus bends the ordinary implement into a visible test: calling it a switch and refusing that name are both answerable moves in the encounter.'),
665:('To appear in the world is to take up public activity or be presented as entering the human scene, especially in records of buddhas and lineage figures.','Zen records also apply the phrase to a presiding figure’s public service, shifting emphasis from supernatural arrival to entering the teaching seat and meeting people.'),
667:('Washing the bowl is the ordinary cleaning act attached to the Zhaozhou case in which a meal question receives the command to wash the bowl.','Later speakers cite the compact case as an answer already enacted in monastery life; bowl washing remains literal while its placement in the interview does the bending.'),
668:("The phrase ‘do not know’ or ‘does not know’ denies or reports knowledge, from Bodhidharma’s answer to ordinary failures of identification.",'The corpus tests whether not-knowing is evasive, exact, or merely ignorant by preserving who says it and what question it answers.'),
671:('Meeting face to face is an actual encounter or audience in which two people are present to one another.','Zen speakers make that ordinary meeting evidentiary: claims of recognition are tested against what happened when the people actually met.'),
673:("The phrase ‘may I ask’ introduces a question while marking uncertainty or a request for clarification.",'In public interviews it signals the questioner’s turn and therefore belongs to that actor, not automatically to the presiding figure named by the record.'),
678:('Protecting living creatures means preserving or sparing sentient life under a stated rule or undertaking.','The selected cases test this hard obligation against concrete killing, rescue, food, and monastery decisions instead of dissolving it into general kindness.'),
684:('Cause and effect names the relation by which an act or condition bears a consequence.','Zen records assert that relation, debate falling into or being blind to it, and condemn casting it aside; rhetorical negation is therefore checked against the full causal family.'),
687:('To answer on another’s behalf is to supply the reply that an earlier participant did not give or that a compiler asks readers to test.','The substitute answer is a marked editorial or teaching move, not evidence that the original participant uttered it.'),
690:('To recognize is to identify or directly discern the person, thing, or point presented in a case.','Questions repeatedly demand whether someone recognizes it, making recognition publicly testable rather than a private claim of understanding.'),
692:('Karmic consciousness is discriminating activity conditioned by accumulated action and habit, not a synonym for every appearance of mind.','Speakers describe it as busy, entangling, or mistaken for insight; the karma-family controls prevent the compound from swallowing non-karmic uses of consciousness.'),
696:('Sons and descendants are the later members claimed as heirs of a lineage, whether praised as continuers or rebuked as unworthy offspring.','The kinship language institutionalizes transmission: speakers judge what descendants preserve, lose, or falsely inherit.'),
698:('To manage to say it is to produce an answer that meets the demand of the present exchange.','The phrase makes speech a performance under examination; fluency alone does not establish that the required point was said.'),
699:('To have no reply is the recorded outcome when a participant cannot or does not answer the question or move just presented.','Zen records preserve the silence as an event with an exact actor; they do not automatically interpret every absence of words as attainment.')}
ledger=json.loads((R/'fresh-build/waves/f003-laneA-651-700-author-ledger.json').read_text());by={x['ordinal']:x['id'] for x in ledger['entries']}
for n,(opening,body) in M.items():
 d=R/'fresh-build/entries'/by[n];p=d/'evidence.draft.json';x=json.loads(p.read_text())
 for s in x['Entry']['Senses']:
  s['ExplanationParts']={'CorpusEarnedOpening':opening,'EvidenceBody':[body]};s['DraftEvidence']['ZenBend']=body
 p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True,stdout=subprocess.DEVNULL)
print('repaired',len(M),'template entries')
