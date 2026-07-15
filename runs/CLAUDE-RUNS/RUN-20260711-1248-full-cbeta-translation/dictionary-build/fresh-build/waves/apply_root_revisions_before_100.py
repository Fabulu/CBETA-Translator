import hashlib,json,sys
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
lp=ROOT/'fresh-build/waves/f001-laneC.json';led=json.loads(lp.read_text())
def load(id):
 p=ROOT/'fresh-build/entries'/id/'entry.v2.json';return p,json.loads(p.read_text())
def save(id,p,z):
 p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');e=next(x for x in led['entries'] if x['id']==id);e['entrySha256']=hashlib.sha256(p.read_bytes()).hexdigest();e['state']=(p.parent/'STATUS').read_text().strip();e['gateReport']={'rootRevision':'applied-pending-review'};led['updatedUtc']=datetime.now(timezone.utc).isoformat();lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
def imp(rel,kwic,note):
 v=zc.verify(rel,kwic);assert v['ok'];return {'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kwic,'ContextMasters':[],'Curated':True,'AttributionNote':note,'ActorAttribution':{'Status':'impersonal','Kind':'signed preface prose','ActorLabel':'the preface voice','ActorRole':'compiler','GrammarEvidence':'The full preface context supplies continuous editorial prose rather than a Chan dialogue turn.','ReviewedBy':'Codex root-revision repair','ReviewedUtc':'2026-07-14T23:25:00Z'}}

# 尊宿: retain the Xiaoyao Cong biography once as narration; replace duplicate.
p,z=load('t_7887dc8d449f');s=z['Senses'][0];s['Occurrences'].pop(0)
s['Occurrences'].append(imp('X/X83/X83n1578.xml','觀古之尊宿，幾十年點胸自許，直至末後為明眼人煅煉過，方始開省','Pointing at the Moon (指月錄), signed preface prose: the preface voice observes that senior worthies tested their confidence for decades before finally awakening under an incisive person’s refinement.'))
s['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in s['Occurrences']));save('t_7887dc8d449f',p,z)

# 宗門: remove the repeated Wuyi Yuanlai line and add a distinct preface deployment.
p,z=load('t_3972185a2e25');s=z['Senses'][0];seen=False;kept=[]
for o in s['Occurrences']:
 if o['RelPath']=='X/X72/X72n1435.xml' and '宗門中長處' in o['Kwic']:
  if seen:continue
  seen=True
 kept.append(o)
s['Occurrences']=kept;s['Occurrences'].append(imp('J/J34/J34nB311.xml','始知吾宗門大事，時時激揚。','Complete Record of Tianjie Juelang Sheng (天界覺浪盛禪師全錄), signed preface prose: the writer says he first came to understand the great matter of our lineage and repeatedly raised it for discussion.'))
s['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in s['Occurrences']));save('t_3972185a2e25',p,z)

# 盡大地: retain exact canonical title without mechanically duplicated clauses.
p,z=load('t_9199b9a31645')
for o in z['Senses'][0]['Occurrences']:
 if o['RelPath']=='X/X82/X82n1571.xml':
  actor=o.get('MasterName') or 'the headed record speaker';o['AttributionNote']=f'Complete Collection of the Five Lamps (五燈全書(第34卷-第120卷)): the full headed case identifies {actor} as the exact actor of this occurrence.'
save('t_9199b9a31645',p,z)

# 五位: Gaitong is a named immortal, not a roster Chan master.
p,z=load('t_ff50c6974a36');o=z['Senses'][1]['Occurrences'][0];o.pop('MasterName',None);o['ContextMasters']=[];o['ActorAttribution']={'Status':'identified-non-master','Kind':'immortal and teaching-dialogue figure','ActorLabel':'the immortal Gaitong','ActorRole':'utterer','GrammarEvidence':'The expanded dialogue explicitly names Gaitong, identifies him as an immortal, and assigns the headword-bearing turn to him.','ReviewedBy':'Codex root-revision repair','ReviewedUtc':'2026-07-14T23:25:00Z'};o['AttributionNote']='Patriarchs’ Hall Collection (祖堂集): the explicitly named immortal Gaitong utters the five-position teaching-dialogue line; he is not classified as a roster Chan master.';save('t_ff50c6974a36',p,z)

# 普請: one communal-summons sense, including the distinctive summons to look.
p,z=load('t_1274824e797b');base=z['Senses'][0];extra=z['Senses'][1]['Occurrences'];base['Occurrences'].extend(extra);base['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in base['Occurrences']));base['PreferredTarget']='communal summons';base['AlternateTargets']=['general work summons','communal call'];base['Explanation']='A communal summons calls the whole assembly to a shared assigned activity. Monastic regulations use it for collective labor and explain that all ranks share the work. Chan hall addresses redeploy the same summons formula in “strike the drum and summon everyone to look”: look names the assigned action, while the headword still denotes the communal call. The institutional call remains the common referent across labor, attendance, and rhetorical redeployment.';base['Note']='Ten anchors preserve regulations, work scenes, headings, narrative usage, and three distinctive hall-address redeployments of the communal summons.';z['Senses']=[base];save('t_1274824e797b',p,z)

# 良久曰: the recorder narrates the pause/turn; following masters remain context only.
p,z=load('t_d926adb80feb');s=z['Senses'][0]
for o in s['Occurrences']:
 name=o.pop('MasterName',None);o['ContextMasters']=([{'MasterName':name,'Roles':['person-described']}] if name else o.get('ContextMasters',[]));o['ActorAttribution']={'Status':'narrated','Kind':'dialogue-turn narration','ActorLabel':'the case recorder or compiler','ActorRole':'compiler','GrammarEvidence':'The headword is the recorder’s narrative formula marking a pause and the resumption of speech; the following words, not the marker, belong to the named speaker.','ReviewedBy':'Codex root-revision repair','ReviewedUtc':'2026-07-14T23:25:00Z'};o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}). Compiler narration marks an interval and reports that speech resumed; the following speaker is contextual rather than the utterer of the headword.'
s['RelatedMasters']=list(dict.fromkeys(c['MasterName'] for o in s['Occurrences'] for c in o.get('ContextMasters',[])));s['Explanation']='A narrative turn marker used by case recorders: after an interval, the record says that someone spoke. It packages an elapsed pause together with the resumption-of-speech verb. The named master supplies the words that follow the marker, but the marker itself belongs to the recorder’s narration.';save('t_d926adb80feb',p,z)

# 請益: Layman Jinghui is the non-master questioner; Yunxi Langting responds.
p,z=load('t_b191c4fa2e9f');o=z['Senses'][0]['Occurrences'][2];o.pop('MasterName',None);o['ContextMasters']=[{'MasterName':'Yunxi Langting','Roles':['respondent']}];o['ActorAttribution']={'Status':'identified-non-master','Kind':'named lay questioner','ActorLabel':'Layman Jinghui','ActorRole':'questioner','GrammarEvidence':'The expanded exchange explicitly introduces Layman Jinghui and assigns the headword-bearing request for instruction to him; Yunxi Langting gives the response.','ReviewedBy':'Codex root-revision repair','ReviewedUtc':'2026-07-14T23:25:00Z'};o['AttributionNote']='Recorded Sayings of Chan Master Yunxi Langting (雲溪俪亭挺禪師語錄): Layman Jinghui is the exact questioner requesting instruction; Yunxi Langting is the contextual respondent.';save('t_b191c4fa2e9f',p,z)

# Avoid an untranslated loan in the English attribution prose.
p,z=load('t_5db4dbd2bc17')
for s in z['Senses']:
 for o in s['Occurrences']:o['AttributionNote']=o.get('AttributionNote','').replace('samadhi','concentration')
save('t_5db4dbd2bc17',p,z)
