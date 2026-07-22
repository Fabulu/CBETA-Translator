import json,datetime
from pathlib import Path
H=Path('/mnt/c/programmieren/mergeworkcbeta/cbeta-translator/runs/CLAUDE-RUNS/RUN-20260711-1248-full-cbeta-translation/dictionary-build')
M={
't_58b708f84962':('Nanquan and Caoshan cases ask about the matter within the different class, using the phrase for conduct or identity outside ordinary categorical sameness and answering through the case’s concrete response.','Different class remains a relational case term rather than a general theory about kinds of beings.'),
't_94c0efef9a92':('The quoted Vimalakirti proposition identifies direct mind as the place of awakening; later Chan records repeat the line to connect an unbent mind with the site where awakening is enacted.','The ancient scriptural line is distinguished from each later voice that quotes or applies it.'),
't_39d4dad94330':('The idiom says that a person cannot digest or bear a specified saying, offering, or consequence; each retained clause supplies the object whose weight cannot be assimilated.','Its evaluative object controls the sense, rather than the literal physiology of digestion alone.'),
't_8cb6de9f3821':('The inherited imagining-plums story concerns relieving thirst with the thought of sour plums; Chan authors invoke it when words or imagined attainment substitute for what would actually satisfy the need.','The allusion marks an ineffective imagined substitute, not an instruction to seek fruit.'),
't_60459a4cd35b':('The retained lines call a saying or encounter a barrier that tests people when it explicitly exposes whether the respondent can pass the case’s demand.','Only clauses assigning that testing function support the entry; a nearby master is not substituted for an unnamed questioner.'),
't_de69e48f3c92':('The ordinary-action answer says that when hungry one eats rice, in a sequence also pairing cold with fire and tiredness with rest; its Chan deployment lies in answering according to the immediate condition.','The rice clause is one member of the paired responsive-action sequence, not dietary instruction.'),
't_d35eafea11f4':('The honorific groups earlier eminent Chan teachers as venerable predecessors when a current record cites, evaluates, or contrasts their sayings and conduct.','The label identifies a historical class in the clause; it does not automatically include every earlier person mentioned nearby.'),
't_782d669e40b7':('The image places a question or verse before heaven and earth were divided, contrasting the undifferentiated prior moment with the presently articulated world.','The temporal image is interpreted through that before-and-after contrast, not as a cosmological date.'),
't_cff08e760e0e':('The dead-tree assembly denotes the community associated with Shishuang’s dead-tree discipline; later records name and comment on that group and its severe stillness.','Shishuang’s community is the referent even when a later lamp compiler or commentator raises it.'),
't_6a484c91ec10':('The robe-and-bowl attendant is the monastic office charged with custody or service connected to a senior’s robe and bowl; regulations and biographies describe appointment and duty.','Institutional narration is not converted into personal speech merely because Baizhang or Dahui is the person described.'),
}
now=datetime.datetime.now(datetime.timezone.utc).isoformat()
for eid,(ex,note) in M.items():
 p=H/'fresh-build/entries'/eid/'evidence.draft.json';d=json.load(open(p));s=d['Entry']['Senses'][0]
 s['Explanation']=ex;s['Note']=note;s['ExplanationParts']={'CorpusEarnedOpening':ex,'EvidenceBody':[note]};s['DraftEvidence']['ZenBend']=ex;s['DraftEvidence']['CounterexampleOrLimit']=note;s['DraftEvidence']['DifferentThingTest']['Reason']=note
 if eid=='t_6a484c91ec10':
  for o in s['Occurrences']:
   n=o.pop('MasterName',None)
   if n:
    o['ContextMasters']=[{'MasterName':n,'Roles':['person-described']}]
    o['ActorAttribution']={'Status':'narrated','Kind':'institutional office narration','ActorLabel':n,'ActorRole':'person-described','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':'The exact clause narrates the office and the named figure’s institutional relation; it is not a speech turn.','ReviewedBy':'Codex bound independent-review repair','ReviewedUtc':now}
 d['Entry']['WrittenUtc']=now;p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
