import json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
def setkw(o,k):
 v=zc.verify(o['RelPath'],k);assert v['ok'],(o['RelPath'],k,v);o.update(Kwic=k,FromLb=v['fromLb'],ToLb=v['toLb'])
def save(id,z):(ROOT/'fresh-build/entries'/id/'entry.v2.json').write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n')
z=json.loads((ROOT/'fresh-build/entries/t_c728f3a8e02b/entry.v2.json').read_text())
o=next(o for o in z['Senses'][0]['Occurrences'] if o['RelPath']=='J/J24/J24nB137.xml');setkw(o,'你問我家風，我卻識你家風。');save('t_c728f3a8e02b',z)
z=json.loads((ROOT/'fresh-build/entries/t_6da91f8ce284/entry.v2.json').read_text())
for o in z['Senses'][0]['Occurrences']:
 if o['RelPath']=='T/T47/T47n1985.xml' and o['Kwic'].startswith('僧問師'):
  setkw(o,'師云：「賓主歷然。」');o['AttributionNote']='鎮州臨濟慧照禪師語錄: Linji Yixuan alone utters this recut statement that guest and host are distinct.'
 if o['RelPath']=='J/J34/J34nB311.xml':
  setkw(o,'進云：「賓主相去幾何？」');o.pop('MasterName',None);o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'monk','ActorLabel':'the unnamed questioning monk','ActorRole':'questioner','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'ReviewedBy':'Codex fresh lane-C exact-turn correction','ReviewedUtc':'2026-07-14T18:00:00Z'};o['ContextMasters']=[{'MasterName':'Juelang Daosheng','Roles':['respondent','record-owner']}];o['AttributionNote']='天界覺浪盛禪師全錄: the unnamed monk utters the recut headword question; Juelang Daosheng is respondent and record owner.'
 if o['RelPath']=='X/X82/X82n1571.xml':
  setkw(o,'如何是一喝分賓主？');o.pop('MasterName',None);o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'questioner','ActorLabel':'the unnamed questioner','ActorRole':'questioner','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'ReviewedBy':'Codex fresh lane-C exact-turn correction','ReviewedUtc':'2026-07-14T18:00:00Z'};o['ContextMasters']=[{'MasterName':'Xuean Congjin','Roles':['respondent','section-subject']}];o['AttributionNote']='五燈全書(第34卷-第120卷): the unnamed questioner utters the recut headword question; Xuean Congjin responds.'
save('t_6da91f8ce284',z)
z=json.loads((ROOT/'fresh-build/entries/t_0f97bfab265c/entry.v2.json').read_text())
for o in z['Senses'][0]['Occurrences']:
 if o['RelPath']=='J/J28/J28nB220.xml':setkw(o,'認作棒喝，入地獄如箭射。');o['AttributionNote']='三宜盂禪師語錄: Faxi Yin utters the recut reply criticizing recognition of the action merely as stick-blows and shouts.'
 if o['RelPath']=='J/J36/J36nB359.xml':
  o.pop('MasterName',None);o['ActorAttribution']={'Status':'impersonal','Kind':'editorial heading','ActorLabel':'an editorial section heading','ActorRole':'compiler','GrammarEvidence':'The phrase is a recorder-supplied heading introducing Baiyu Jingsi’s rebuke; it is not part of his following utterance.','ReviewedBy':'Codex fresh lane-C exact-turn correction','ReviewedUtc':'2026-07-14T18:00:00Z'};o['ContextMasters']=[{'MasterName':'Baiyu Jingsi','Roles':['record-owner']}];o['AttributionNote']='百愚禪師語錄: the recorder uses the headword in a section heading introducing Baiyu Jingsi’s rebuke; the heading itself is impersonal metadata.'
save('t_0f97bfab265c',z)
z=json.loads((ROOT/'fresh-build/entries/t_ff50c6974a36/entry.v2.json').read_text())
z['Senses'][1]['Occurrences']=[o for o in z['Senses'][1]['Occurrences'] if '五十五位' not in o['Kwic'] and '第五位' not in o['Kwic']]
save('t_ff50c6974a36',z)
z=json.loads((ROOT/'fresh-build/entries/t_b0f2ccf6d140/entry.v2.json').read_text())
o=next(o for o in z['Senses'][0]['Occurrences'] if o['RelPath']=='L/L158/L158n1652.xml');o['ActorAttribution'].update(Status='narrated',Kind='compiler narrative',ActorLabel='the record compiler narrating the imperial order',ActorRole='compiler',GrammarEvidence='The compiler narrates that the emperor ordered the restriction period to open on the fifteenth; the headword belongs to that narrative, not to an occasion heading or Mingjue Cong’s speech.');o['ContextMasters']=[{'MasterName':'Mingjue Cong','Roles':['person-described','record-owner']}];o['AttributionNote']='明覺聰禪師語錄: the compiler narrates the imperial order setting the opening date; Mingjue Cong is the person described and record owner, not the utterer.';save('t_b0f2ccf6d140',z)
