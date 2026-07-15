from pathlib import Path
import copy,json
R=Path(__file__).resolve().parents[2]
def load(i):p=R/'fresh-build/entries'/i;return p,json.loads((p/'entry.v2.json').read_text())
def save(p,e):
 (p/'entry.v2.json').write_text(json.dumps(e,ensure_ascii=False,indent=2)+'\n');d=json.loads((p/'evidence.draft.json').read_text());d['Entry']=copy.deepcopy(e);(p/'evidence.draft.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
def named(o,n,why,ctx=None):
 o.pop('ActorAttribution',None);o['MasterName']=n;o['ContextMasters']=ctx or [{'MasterName':n,'Roles':['utterer']}];o['AttributionNote']=f'Source text exact-turn review: {n} utters the headword-bearing clause. {why}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':n,'FullCaseDecision':why}
# Xuansha is explicitly introduced before 接物利生.
p,e=load('t_edfd0b2afa11');named(e['Senses'][0]['Occurrences'][0],'Xuansha Shibei','The enclosing record says “Xuansha instructed the assembly,” so the historical quoted speaker is not the later record owner.');save(p,e)
# Every exact 體露金風 quotation with 雲門云 retains Yunmen as historical utterer.
p,e=load('t_47b3313788e2')
for o in e['Senses'][0]['Occurrences']:
 if ('雲門' in o['Kwic'] or '門云' in o['Kwic'] or '門曰' in o['Kwic']) and '體露金風' in o['Kwic']:
  named(o,'Yunmen Wenyan','The full quoted question-answer explicitly assigns “the body exposed in the golden wind” to Yunmen Wenyan.')
save(p,e)
# The explicit Yun'an Yue attribution owns 陷虎之機 where it immediately governs the phrase.
p,e=load('t_aa56c106ef82')
for o in e['Senses'][0]['Occurrences']:
 if '雲庵' in o['Kwic'] and '陷虎之機' in o['Kwic']: named(o,"Yun'an Keyue",'The inline Yun’an Yue said cue immediately governs the headword-bearing judgment.')
save(p,e)
print('concrete round1 applied')
