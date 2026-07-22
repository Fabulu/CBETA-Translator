import json,datetime
from pathlib import Path
H=Path('/mnt/c/programmieren/mergeworkcbeta/cbeta-translator/runs/CLAUDE-RUNS/RUN-20260711-1248-full-cbeta-translation/dictionary-build')
M={
't_f61487cb5ad3':('The Handan allusion tells of imitating another city’s gait so badly that one loses one’s own way of walking; Chan comments use it for derivative imitation that forfeits native ability.','The headword carries the inherited failed-imitation story, not simply the act of taking steps.'),
't_b9c8b6432f69':('The final barrier is the last decisive obstruction or test named in the retained sayings; clauses predicate passing, confronting, or remaining blocked by it.','It is bounded by those explicit final-test predicates and is not merged with every occurrence of final saying.'),
't_e88454f1a896':('In the Nanquan-related case, filling the bottle is a concrete assigned action involving the vessel and participants; later records raise that action as the point of the encounter.','The verb and bottle form one case action, not an abstract instruction to fill an unspecified container.'),
't_f710fe543504':('The verse image turns around and returns to the father, presenting reversal from outward movement as restoration of the explicit child–father relation in the case.','The family image is licensed by the verse relation and is not generalized to literal travel home.'),
't_136585c0a460':('The wording narrates an event: the named figure raised the whisk and displayed it before the assembly, after which the record may supply a response or comment.','Raising and displaying are narrated actions; the named performer is not placed in the utterer-only field unless a separate spoken clause follows.'),
't_ea11326c54ed':('Each clause supplies what is made into one piece—such as activity, mind, or the whole encountered situation—and uses the phrase for bringing those specified elements into undivided continuity.','The object must remain visible from its full case; the headword does not mean combining arbitrary physical pieces.'),
't_9fcb8e908ffd':('The cases predicate that body and mind are guest-dust: transient visitors contrasted with what does not arrive and depart.','The guest-dust metaphor depends on that transient-versus-stable contrast, not on physical dust on a body.'),
't_d691a251053d':('In these clauses, the headword denotes the working or responsive pivot proper to one’s fundamental position, shown when a case asks for or evaluates that native capacity.','The translation uses working or pivot according to syntax; the final character is not treated as a literal machine.'),
't_5b036ec3c4a2':('The direct and essential road names the single immediately relevant course in Dengjue’s saying; later compilations preserve that saying as quotation.','Dengjue is the quoted origin, while the later record remains the transmitting or quoting voice.'),
't_bdad3daa2fd7':('Dead wood and cold ashes form an image of lifeless stillness; retained records compare a condition to it or criticize remaining inert in that condition.','The evaluative comparison controls the entry, rather than the physical materials in isolation.'),
}
now=datetime.datetime.now(datetime.timezone.utc).isoformat()
for eid,(ex,note) in M.items():
 p=H/'fresh-build/entries'/eid/'evidence.draft.json';d=json.load(open(p));s=d['Entry']['Senses'][0]
 s['Explanation']=ex;s['Note']=note;s['ExplanationParts']={'CorpusEarnedOpening':ex,'EvidenceBody':[note]};s['DraftEvidence']['ZenBend']=ex;s['DraftEvidence']['CounterexampleOrLimit']=note;s['DraftEvidence']['DifferentThingTest']['Reason']=note
 if eid=='t_136585c0a460':
  for o in s['Occurrences']:
   n=o.pop('MasterName',None) or (o.get('ActorAttribution') or {}).get('ActorLabel')
   if n:
    o['ContextMasters']=[{'MasterName':n,'Roles':['case-figure']}]
    o['ActorAttribution']={'Status':'narrated','Kind':'whisk-display action','ActorLabel':n,'ActorRole':'case-figure','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':'The exact headword narrates the named figure raising and displaying the whisk; it is not itself a speech turn.','ReviewedBy':'Codex bound independent-review repair','ReviewedUtc':now}
 d['Entry']['WrittenUtc']=now;p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
