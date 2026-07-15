import hashlib,json,sys
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
IDS=['t_ec1241360056','t_f3488daf27fd','t_f41961e0e5be','t_f56016646d8f','t_fb331b159983','t_f4c65b25832f','t_f7c3da035832','t_fac9b9afebf6','t_fe8d4efe588f']
P={i:ROOT/'fresh-build/entries'/i/'entry.v2.json' for i in IDS};old={i:hashlib.sha256(p.read_bytes()).hexdigest() for i,p in P.items()};D={i:json.loads(p.read_text(encoding='utf8')) for i,p in P.items()};R=['line','expanded-context','section-header','book-title','tei-header','parallel-passage'];chg={i:[] for i in IDS}
def cm(n,*r):return {'MasterName':n,'Roles':list(r)}
def named(o,n,note,ctx=None):o['MasterName']=n;o.pop('ActorAttribution',None);o['ContextMasters']=ctx or [cm(n,'utterer')];o['AttributionNote']=note
def un(o,kind,label,role,ev,note,ctx=None,status='reviewed-unnamed'):
 o.pop('MasterName',None);o['ActorAttribution']={'Status':status,'Kind':kind,'ActorLabel':label,'ActorRole':role,'GrammarEvidence':ev,'RungsChecked':R,'ReviewedBy':'Codex personal full-read 106-115','ReviewedUtc':datetime.now(timezone.utc).isoformat()};o['ContextMasters']=ctx or [];o['AttributionNote']=note

# Explicit speaker markers recovered only by reading the whole compiled case.
named(D['t_ec1241360056']['Senses'][0]['Occurrences'][4],'Xuedou Chongxian','續古尊宿語要: Xuedou Chongxian says that if an imperforate iron hammer can bear the burden for the assembly, it can cage past and present.')
named(D['t_f3488daf27fd']['Senses'][0]['Occurrences'][7],'Songan Zheng','宗鑑法林: Songan Zheng explicitly comments that Sudhana was a little closer while searching without seeing, but the meeting on the other peak still missed it.',[cm('Songan Zheng','utterer','commentator'),cm('Sudhana','person-discussed')])
chg['t_ec1241360056'].append('Recovered Xuedou from 雪竇云 in the complete compiled case.');chg['t_f3488daf27fd'].append('Recovered Songan Zheng from 嵩菴正云.')

# The ox-herding preface identifies itself only as a lay author.
un(D['t_f41961e0e5be']['Senses'][0]['Occurrences'][5],'anonymous lay preface author','the unnamed lay author who calls himself 本俗子','utterer','本俗子 marks the first-person lay author of the preface; no personal name survives the six-rung review.','牧牛圖頌: the unnamed lay preface author says ox-herding began in scripture and was displayed by generations of patriarchs.')
chg['t_f41961e0e5be'].append('Classified the 本俗子 preface voice as reviewed-unnamed lay author.')

# Compiled comments explicitly name their utterers.
named(D['t_fb331b159983']['Senses'][0]['Occurrences'][6],'Falin Yin','宗鑑法林: Falin Yin says the old man wrongly supplied a phrase, then verses that he has no leisure to play with a dead snake.',[cm('Falin Yin','utterer','commentator')])
named(D['t_fb331b159983']['Senses'][0]['Occurrences'][7],'Yuwang Guang','列祖提綱錄: Yuwang Guang says on the teaching seat, “Do not kill a dead snake met on the road; carry it home in a bottomless basket.”')
chg['t_fb331b159983'].append('Recovered Falin Yin and Yuwang Guang from explicit compiled-section markers.')

# The repeated Linji four-shout formula in the提綱 anthology belongs to the same named Zhihai address.
named(D['t_fac9b9afebf6']['Senses'][0]['Occurrences'][6],'Dagui Zhe','列祖提綱錄: Dagui Zhe raises the staff and says Zhihai’s staff may act as probing pole and shadowing grass.',[cm('Dagui Zhe','utterer')])
chg['t_fac9b9afebf6'].append('Recovered Dagui Zhe as owner of the Zhihai staff address.')

# 見處: two headword clauses are questions by unnamed monks; the last is an unsigned verse.
E=D['t_fe8d4efe588f']['Senses'][0]['Occurrences']
un(E[2],'unnamed monastic questioner','the unnamed monk telling the World-Honored One he has a point of seeing but not realization','utterer','比丘問 introduces the headword-bearing question; 世尊曰 begins the response.','指月錄: an unnamed monk says he has a point of seeing in the teaching but not yet a point of realization.',[cm('Buddha','respondent')])
un(E[5],'unnamed monastic questioner','the unnamed monk telling the World-Honored One he has a point of seeing but not realization','utterer','比丘問 introduces the headword-bearing question in this parallel recension.','五燈會元: an unnamed monk says he has a point of seeing in the teaching but not yet a point of realization.',[cm('Buddha','respondent')])
un(E[6],'anonymous verse author','the unnamed case-verse author','verse-author','The complete source unit is an unsigned verse with no dialogue cue or recoverable author.','宗鑑法林: an unnamed verse author writes of turning through the place where patch-robed monks meet.')
chg['t_fe8d4efe588f'].append('Distinguished two unnamed monk questions and an anonymous verse from nearby case figures.')

# 斬貓: the precept objection is the questioner's utterance, not Miyun's.
E=D['t_f4c65b25832f']['Senses'][0]['Occurrences'];un(E[1],'unnamed monastic questioner','the unnamed monk asking why Nanquan cut the cat despite the precept against killing','utterer','問 introduces the headword-bearing objection; 師云 begins Miyun Yuanwu’s answer.','密雲禪師語錄: an unnamed monk asks why Nanquan cut the cat and Guizong cut the snake if killing is a major precept.',[cm('Miyun Yuanwu','respondent'),cm('Nanquan Puyuan','person-discussed')]);chg['t_f4c65b25832f'].append('Corrected the precept objection to its unnamed monk utterer.')

for d in D.values():
 for s in d['Senses']:
  for o in s.get('Occurrences',[]):
   title=zc.title(o['RelPath']);note=o.get('AttributionNote','')
   if title and title not in note:o['AttributionNote']=f'{title}: {note}'
rows=[]
for i,p in P.items():p.write_text(json.dumps(D[i],ensure_ascii=False,indent=2)+'\n',encoding='utf8');rows.append({'id':i,'term':D[i]['SourceTerm'],'oldSha256':old[i],'newSha256':hashlib.sha256(p.read_bytes()).hexdigest(),'changes':chg[i] or ['Full reading confirmed all stored actor decisions.']})
out=ROOT/'maintenance/attribution-read-adjudication/cohorts-7-9-106-115-full-read-repair-ledger.json';out.write_text(json.dumps({'generatedUtc':datetime.now(timezone.utc).isoformat(),'readCount':68,'rows':rows},ensure_ascii=False,indent=2)+'\n',encoding='utf8');print(out)
