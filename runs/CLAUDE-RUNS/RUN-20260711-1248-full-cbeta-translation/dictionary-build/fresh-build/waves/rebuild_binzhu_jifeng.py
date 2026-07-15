import json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
B='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a'
def named(o):
    if o.get('MasterName'):
        o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':o['MasterName'],'Roles':['utterer']}]
def save(z,id):
    z.update(CreatedBy='Codex fresh-build lane C',WrittenUtc=None,CorpusBaselineSha256=B)
    d=ROOT/'fresh-build/entries'/id;d.mkdir(parents=True,exist_ok=True);(d/'entry.v2.json').write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');(d/'STATUS').write_text('drafted\n')

z=json.loads((ROOT/'terms/t_6da91f8ce284/entry.v2.json').read_text());s=z['Senses'][0]
s['Occurrences']=[o for o in s['Occurrences'] if '賓主' in o.get('Kwic','')]
for o in s['Occurrences']:named(o)
s.update(PreferredTarget='guest and host',AlternateTargets=['guest-host roles','visitor and host'],SearchAliases=['guest and host','guest host roles','visitor and host','exchange guest and host'],Explanation="Guest and host are the paired roles through which an encounter's initiative and response are described. Records ask whether the roles are distinguished, show one role questioning the other, and speak of their positions turning or being exchanged. Interchange does not name a second thing: it is an action performed with the same guest-host pair. The phrase can refer to literal social positions, but in encounter analysis it marks who sets and who receives the situation.",Note="The frozen corpus has 2,692 exact hits in 355 files representing 351 works. Ten standalone anchors span role distinction, questioning, reversal, exchange, criticism, and later commentary across independent works. A legacy witness lacking the exact headword was rejected.")
save(z,'t_6da91f8ce284')

z=json.loads((ROOT/'terms/t_c1af3ecba987/entry.v2.json').read_text());s=z['Senses'][0]
for o in s['Occurrences']:named(o)
for o in s['Occurrences']:
    if o['RelPath']=='T/T48/T48n2006.xml' and o['Kwic'].startswith('師資辨難'):
        o.pop('MasterName',None);o['ActorAttribution']={'Status':'narrated','Kind':'compiler narrative','ActorLabel':'the compiler Huiyan Zhizhao','ActorRole':'compiler','GrammarEvidence':'The sentence is expository compiler narration describing teacher and student exchanging encounter edges, not a quoted utterance by either participant.','ReviewedBy':'Codex fresh lane-C complete-case review','ReviewedUtc':'2026-07-14T17:20:00Z'};o['ContextMasters']=[{'MasterName':'Huiyan Zhizhao','Roles':['compiler']}];o['AttributionNote']='人天眼目: compiler Huiyan Zhizhao narrates that teacher and student debate and exchange encounter edges; the line is expository narration rather than participant speech.'
s.update(PreferredTarget='the sharp edge of an encounter',AlternateTargets=['encounter edge','sharp responsive edge','pointed response'],SearchAliases=['encounter edge','sharp response','pointed exchange','verbal edge'],Explanation="The sharp edge of an encounter is the quick, pointed capacity displayed when speakers meet in questioning and response. Records call it swift, steep, lightning-like, exchanged, or brought into contact with another's. The term can praise acuity, but criticism of merely rapid performance shows that speed alone does not establish success. It names the responsive edge being displayed or assessed, not a guaranteed result.",Note="The frozen corpus has 780 exact hits in 221 files representing 217 works. Seven standalone anchors cover description, exchange, vivid comparison, direct instruction, and explicit criticism across independent works. Compiler narration is recorded as narration rather than assigned to a participant.")
save(z,'t_c1af3ecba987')
