import hashlib,json,sys
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
IDS=['t_72fd192e30c4','t_745dde2c8711','t_757827b8d4cb','t_8482770fe735','t_8beda961c75a','t_97e211e4846e','t_9d60d7613392','t_a66ef543d2ea','t_a7f67b4983d9','t_a9f422b3b249']
P={i:ROOT/'fresh-build/entries'/i/'entry.v2.json' for i in IDS}; old={i:hashlib.sha256(p.read_bytes()).hexdigest() for i,p in P.items()};D={i:json.loads(p.read_text(encoding='utf8')) for i,p in P.items()};R=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']; changes={i:[] for i in IDS}
def cm(n,*r):return {'MasterName':n,'Roles':list(r)}
def named(o,n,note,ctx=None):o['MasterName']=n;o.pop('ActorAttribution',None);o['ContextMasters']=ctx or [cm(n,'utterer')];o['AttributionNote']=note
def un(o,kind,label,role,ev,note,ctx=None):
 o.pop('MasterName',None);o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':kind,'ActorLabel':label,'ActorRole':role,'GrammarEvidence':ev,'RungsChecked':R,'ReviewedBy':'Codex personal full-read 086-095','ReviewedUtc':datetime.now(timezone.utc).isoformat()};o['ContextMasters']=ctx or [];o['AttributionNote']=note
def non(o,kind,label,role,ev,note,ctx=None):
 o.pop('MasterName',None);o['ActorAttribution']={'Status':'identified-non-master','Kind':kind,'ActorLabel':label,'ActorRole':role,'GrammarEvidence':ev,'RungsChecked':R,'ReviewedBy':'Codex personal full-read 086-095','ReviewedUtc':datetime.now(timezone.utc).isoformat()};o['ContextMasters']=ctx or [];o['AttributionNote']=note

# Explicit defects exposed by reading.
o=D['t_72fd192e30c4']['Senses'][0]['Occurrences'][2];o['ActorAttribution']['ActorLabel']='the unnamed preface author of the Recorded Sayings of Feiyin';o['ActorAttribution']['Kind']='anonymous preface author';changes['t_72fd192e30c4'].append('Made the unresolved preface author explicitly unnamed.')
o=D['t_745dde2c8711']['Senses'][0]['Occurrences'][9];un(o,'anonymous case-verse author','the unnamed case-verse author in Zongjian Falin','verse-author','The headword occurs in an unattributed capping verse; all six rungs leave its author unnamed.','宗鑑法林: an unnamed case-verse author pictures a three-legged donkey being knocked over by Chang’e.');changes['t_745dde2c8711'].append('Reclassified anonymous case verse as reviewed-unnamed.')
o=D['t_757827b8d4cb']['Senses'][0]['Occurrences'][7];named(o,'Gaofeng Yuanmiao','宗鑑法林 explicitly quotes Gaofeng Yuanmiao saying that without Zhuge Liang’s capacity the monk would certainly lose body and life.',[cm('Gaofeng Yuanmiao','utterer','later-commentator')]);changes['t_757827b8d4cb'].append('Corrected compiled-comment occurrence to quoted speaker Gaofeng Yuanmiao.')
o=D['t_8482770fe735']['Senses'][0]['Occurrences'][7];un(o,'unnamed monastic speaker','the unnamed monk comparing the former lion’s roar with today’s elephant-king turn','utterer','The headword occurs in the monk’s 曰 turn before the master’s 師曰 response.','五燈嚴統: an unnamed monk says “formerly the lion roared; today the elephant king turns”; Yiyuan answers that this has no connection.',[]);changes['t_8482770fe735'].append('Corrected adjacent dialogue from compiler to unnamed monk utterance.')
o=D['t_97e211e4846e']['Senses'][0]['Occurrences'][5];non(o,'named biographical commentator','Bao Tan','commentator','寶曇曰 introduces Bao Tan’s named appraisal of Yanyang.','禪林僧寶傳: Bao Tan compares Yanyang with an iron stake while appraising his recorded encounters.');changes['t_97e211e4846e'].append('Named biographical commentator Bao Tan.')
o=D['t_9d60d7613392']['Senses'][0]['Occurrences'][5];o['ActorAttribution']['ActorLabel']='Hui Ming (悔明)';o['AttributionNote']='徑石滴乳集序: Hui Ming (悔明), the signed preface author, says that the patriarchs’ half a word or single phrase was like an electric flash.';changes['t_9d60d7613392'].append('Normalized signed preface author Hui Ming as a named non-master.')
o=D['t_a66ef543d2ea']['Senses'][0]['Occurrences'][1];named(o,'Yongming Yanshou','宗鏡錄: Yongming Yanshou explains that among the five hundred youths transformed by Manjushri, Shancai alone reached the source of mind and travelled to ask about awakening-conduct.');changes['t_a66ef543d2ea'].append('Recovered Yongming Yanshou as authorial speaker.')
o=D['t_a66ef543d2ea']['Senses'][0]['Occurrences'][3];o['ActorAttribution']['ActorLabel']='the unnamed record-owner delivering the hall address';o['ActorAttribution']['Kind']='unnamed record-owner';o['ActorAttribution']['ActorRole']='utterer';changes['t_a66ef543d2ea'].append('Made unresolved hall-address owner explicitly unnamed.')
o=D['t_a7f67b4983d9']['Senses'][0]['Occurrences'][4];named(o,'Dahui Zonggao','大慧普覺禪師語錄: Dahui Zonggao says that beings are rolled along by the dust and toil of daily use and later says the teaching is luminous within beings’ daily use.');changes['t_a7f67b4983d9'].append('Corrected full discourse to Dahui Zonggao.')
o=D['t_a7f67b4983d9']['Senses'][0]['Occurrences'][7];non(o,'named petition author','Shandamidi','author','The memorial closes with 善達密的理上表, identifying its author.','天目中峰廣錄: Shandamidi petitions that Zhongfeng Mingben’s collected writings be entered into the canon and says his teacher used himself first to remedy the defects of contemporary learning.',[cm('Zhongfeng Mingben','person-discussed')]);changes['t_a7f67b4983d9'].append('Named memorial author Shandamidi.')
o=D['t_a9f422b3b249']['Senses'][0]['Occurrences'][6];o['ActorAttribution']['ActorLabel']='the unnamed verse author responsible for the headword-bearing clause';o['ActorAttribution']['Kind']='anonymous verse author';changes['t_a9f422b3b249'].append('Made anonymous verse author explicit.')

# Closed-role and exact-label normalization after the semantic decisions.
D['t_72fd192e30c4']['Senses'][0]['Occurrences'][2]['AttributionNote']='費隱禪師語錄: the unnamed preface author of the Recorded Sayings of Feiyin praises Feiyin’s hammer-and-tongs treatment of students.'
D['t_757827b8d4cb']['Senses'][0]['Occurrences'][7]['ContextMasters'][0]['Roles']=['utterer','commentator']
D['t_a66ef543d2ea']['Senses'][0]['Occurrences'][3]['AttributionNote']='列祖提綱錄: the unnamed record-owner delivering the hall address says after a long pause, “Everywhere is Maitreya; no gate, no Shancai.”'
D['t_a7f67b4983d9']['Senses'][0]['Occurrences'][7]['ActorAttribution']['ActorRole']='utterer'
D['t_a9f422b3b249']['Senses'][0]['Occurrences'][6]['AttributionNote']='禪宗頌古聯珠通集: the unnamed verse author responsible for the headword-bearing clause says that the white-haired man still clings to the conditions of life.'

# Every note names the exact source title; preserve semantics while satisfying reader-facing attribution.
for d in D.values():
 for s in d['Senses']:
  for o in s.get('Occurrences',[]):
   title=zc.title(o['RelPath']);note=o.get('AttributionNote','')
   if title and title not in note:o['AttributionNote']=f'{title}: {note}'
rows=[]
for i,p in P.items():p.write_text(json.dumps(D[i],ensure_ascii=False,indent=2)+'\n',encoding='utf8');rows.append({'id':i,'term':D[i]['SourceTerm'],'oldSha256':old[i],'newSha256':hashlib.sha256(p.read_bytes()).hexdigest(),'changes':changes[i] or ['Full-case reading confirmed all stored actor decisions.']})
out=ROOT/'maintenance/attribution-read-adjudication/cohorts-7-9-086-095-full-read-repair-ledger.json';out.write_text(json.dumps({'generatedUtc':datetime.now(timezone.utc).isoformat(),'rows':rows},ensure_ascii=False,indent=2)+'\n',encoding='utf8');print(out)
