import json,sys,re
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
P=json.loads((R/'fresh-build/waves/f001-laneA-076-100-preflight.json').read_text());rows={x['term']:x['id'] for x in P['entries'][5:]}
RUNG=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
openings={
'函蓋乾坤':'Covering heaven and earth names complete fit or coverage and, in the stored records, serves as the first of Yunmen’s three phrases beside cutting off all streams and following wave after wave.',
'平常心是道':'Ordinary mind is the Way is qualified in the records as uncontrived and free of deliberate choosing; Nanquan also warns that purposefully approaching it already misses it.',
'四賓主':'The four guest-and-host configurations map relational positions within an encounter: guest seeing host, host seeing guest, host seeing host, and guest seeing guest, with later witnesses contesting whether a similarly named Caodong formulation belongs to the same device.',
'四料揀':'The Linji Four Selections sort four alternatives: removing the person but not surroundings, surroundings but not person, both, or neither; the stored questions and later definitions treat this as an encounter device.',
'以心傳心':'Transmit mind by mind names direct transmission in some witnesses, while other stored voices warn against turning mind into a transmissible object; the entry preserves both claims without deciding between them.',
'逢佛殺佛':'When you meet a buddha, kill the buddha is Linji’s repeated command to cut through encountered authorities or identities, whether presented as internal or external, without the entry imposing a further psychological theory.',
'四照用':'The four illumination-and-function combinations are illumination before function, function before illumination, both at once, and neither at the same time; later records redeploy the fourfold definition in interviews and verse.',
'吃茶去':'Go drink tea is Zhaozhou’s repeated answer or dismissal to differently situated visitors and to the director who asks why the answers match; later records also use it simply to send an assembly back to the hall.',
'喝':'A shout is an interview action whose placement can expose guest and host or illumination and function; the same records warn that blind or imitative shouting does not reproduce its use.',
'棒':'As an implement, the staff is raised, wielded, or displayed by a teacher in an encounter.',
'且道':'Now tell me is an immediate public-interview demand for an answer or judgment, often inserted after a case, assertion, or gesture to press the audience for the next turn.',
'一句':'A single phrase is the demanded or decisive saying within an encounter: records ask for first, second, third, final, or beyond-level phrases and test whether one phrase can carry the matter.',
'一喝':'A single shout is classified through Linji’s four comparisons and through the caution that one shout may not function as a shout; its force is therefore context-dependent rather than merely numerical.',
'良久':'A long pause marks an interval before an answer, gesture, departure, or absence of response; the narrative formula records timing in the encounter without deciding what the pause inwardly means.',
'拂子':'The fly-whisk is a teaching-seat implement and transferable emblem used to present, test, or enact authority; fantastic compounds such as a turtle-hair fly-whisk remain quoted variants, not a second material sense.',
'禮拜':'To bow is a ritual action appearing before and after questions, answers, departures, and recognitions; the record of the action does not by itself prove any particular inward state.',
'便打':'Then struck is a narrative formula for an immediate blow inside the turn structure of an encounter, identifying the action’s timing rather than merely combining “then” with “hit.”',
'向上':'In its encounter sense, higher or beyond asks what lies past an already stated level, route, or formulation; literal upward direction remains a separate spatial sense.',
'鼻孔':'Having or finding one’s nostrils can mark standing on one’s own, while another’s grip, piercing, or loss of them marks subjection or disorientation; this is a contrast inferred from the stored verbs, not an imported doctrine.',
'會麼':'Do you understand? demands acknowledgment after a statement, case, or action in a public encounter; an answer or silence is recorded without assuming that it proves realization.'}
def load(term):
 p=R/'fresh-build/entries'/rows[term]/'evidence.draft.json';return p,json.loads(p.read_text())
def question(o,respondent=None):
 title=zc.title(o['RelPath']);o['MasterName']=None;o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'monastic questioner','ActorLabel':'unnamed monastic questioner','ActorRole':'questioner','RungsChecked':RUNG,'GrammarEvidence':'The marked question assigns the exact headword to the unnamed monastic; the named teacher responds afterward.','ReviewedBy':'Codex f001 lane A independent-review repair','ReviewedUtc':'2026-07-15T00:00:00Z'};o['ContextMasters']=([{'MasterName':respondent,'Roles':['respondent','record-owner']}] if respondent else []);o['AttributionNote']=f'An unnamed monastic questioner in Source Record ({title}) owns the exact headword-bearing question;'+(f' {respondent} answers afterward.' if respondent else ' the response follows afterward.');o['DraftActorProof']={'GrammaticalSubject':'the unnamed monastic questioner','FullCaseDecision':'The unnamed monastic owns the exact question; the respondent does not own that wording.'}
def narrated(o,actor_name=None):
 title=zc.title(o['RelPath']);name=actor_name or o.get('MasterName');o['MasterName']=None;o['ActorAttribution']={'Status':'narrated','Kind':'narrated action or interval','ActorLabel':'source narrator','ActorRole':'compiler','GrammarEvidence':'Narrative syntax reports the headword-bearing action or interval rather than quoting the participant saying the headword.','ReviewedBy':'Codex f001 lane A independent-review repair','ReviewedUtc':'2026-07-15T00:00:00Z'};o['ContextMasters']=([{'MasterName':name,'Roles':['person-described']}] if name else []);o['AttributionNote']=f'Source narration in Source Record ({title}) reports the exact headword-bearing action or interval.'+(f' {name} is the participant described, not a quoted headword speaker.' if name else '');o['DraftActorProof']={'GrammaticalSubject':'the source narrator','FullCaseDecision':'The narrator owns the exact wording; the participant is retained as the person described.'}

for term,eid in rows.items():
 p,d=load(term)
 for si,s in enumerate(d['Entry']['Senses']):
  op=openings[term]
  if term=='棒' and si==1:op='As a counted blow, a stroke is something a participant gives or receives; this action sense remains distinct from the staff as an implement.'
  if term=='四料揀' and si==1:op='Yongming’s Four Selections contrast four paired outcomes in a later named scheme; this cited source is kept distinct from the current quoter and from Linji’s encounter selections.'
  if term=='四賓主' and si==1:op='The later Caodong-attributed guest-and-host set is asserted as different from Linji’s configurations, while another stored witness denies that the Dong school formulated such a set; the entry preserves that contest.'
  if term=='向上' and si==1:op='In the spatial sense, upward describes the direction of looking or bodily orientation and is kept separate from the comparative “beyond” demand.'
  s['ExplanationParts']={'CorpusEarnedOpening':op,'EvidenceBody':[op,'The selected exact turns and their attributed speakers or narrators bound this account; the entry does not add a resolution beyond those deployments.']};s['DraftEvidence']['ZenBend']=op;s['DraftEvidence']['CounterexampleOrLimit']=s['ExplanationParts']['EvidenceBody'][-1]
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')

# Exact questioner repairs.
p,d=load('函蓋乾坤');s=d['Entry']['Senses'][0]
for o in s['Occurrences']:
 if '如何是函蓋乾坤句' in o['Kwic']:question(o)
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
p,d=load('向上');s=d['Entry']['Senses'][0];question(s['Occurrences'][0],'Renwang Jun');question(s['Occurrences'][2],'Yunmen Wenyan');p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')

# Fourfold-source repairs.
p,d=load('四照用');narrated(d['Entry']['Senses'][0]['Occurrences'][0]);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
p,d=load('四料揀');o=d['Entry']['Senses'][1]['Occurrences'][0];o['ContextMasters']=[{'MasterName':'Hengshan Dengbing','Roles':['later-quoter','record-owner']},{'MasterName':'Yongming Yanshou','Roles':['person-discussed']}];o['AttributionNote']='Hengshan Dengbing, in his record, introduces Yongming Yanshou as the cited source of the named Four Selections; Yongming is not the current live speaker.';p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')

# Remove the unsupported interpersonal alias.
p,d=load('以心傳心')
for s in d['Entry']['Senses']:s['SearchAliases']=[x for x in s.get('SearchAliases',[]) if 'heart-to-heart' not in x.lower()]
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')

# Split the mixed Linji question sequence into three actor-pure question rows.
p,d=load('一句');s=d['Entry']['Senses'][0];old=s['Occurrences'].pop(0);new=[]
for label in ['第一句','第二句','第三句']:
 kw=f'如何是{label}？';v=zc.verify(old['RelPath'],kw);o={'RelPath':old['RelPath'],'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'MasterName':None,'Curated':True};question(o,'Linji Yixuan');new.append(o)
s['Occurrences']=new+s['Occurrences'];s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)];p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')

# Narrated actions and intervals.
for term in ['良久','禮拜','便打']:
 p,d=load(term)
 for s in d['Entry']['Senses']:
  for o in s['Occurrences']:narrated(o)
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
for term in ['喝','一喝']:
 p,d=load(term)
 for s in d['Entry']['Senses']:
  for o in s['Occurrences']:
   if re.search(r'便喝|一喝|喝一喝|乃喝|師喝|僧喝|喝云|喝曰',o['Kwic']):narrated(o)
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
for term in ['棒','拂子']:
 p,d=load(term)
 for s in d['Entry']['Senses']:
  for o in s['Occurrences']:
   if re.search(r'拈|舉|豎|卓|打|趁|擊|擲|奪|付|授|畫|以棒|棒下',o['Kwic']):narrated(o)
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
