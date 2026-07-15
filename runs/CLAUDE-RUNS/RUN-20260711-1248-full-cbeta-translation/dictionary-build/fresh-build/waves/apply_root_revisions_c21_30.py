import hashlib,json,sys
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
lp=ROOT/'fresh-build/waves/f001-laneC.json';led=json.loads(lp.read_text())
def load(id):p=ROOT/'fresh-build/entries'/id/'entry.v2.json';return p,json.loads(p.read_text())
def save(id,p,z):
 p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');e=next(x for x in led['entries'] if x['id']==id);e['entrySha256']=hashlib.sha256(p.read_bytes()).hexdigest();e['state']=(p.parent/'STATUS').read_text().strip();e['gateReport']={'rootRevision':'applied-pending-review'};led['updatedUtc']=datetime.now(timezone.utc).isoformat();lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
def metadata(o,label,note,context=None):
 o.pop('MasterName',None);o['ContextMasters']=context or [];o['ActorAttribution']={'Status':'impersonal','Kind':'title or contents metadata','ActorLabel':label,'ActorRole':'compiler','GrammarEvidence':'The headword appears in a title, contents line, or biographical section heading and is not a spoken turn.','ReviewedBy':'Codex root-revision repair','ReviewedUtc':'2026-07-14T23:40:00Z'};o['AttributionNote']=note

# Immediate attribution-gate repairs from the preceding root revisions.
p,z=load('t_3972185a2e25');o=z['Senses'][0]['Occurrences'][-1];o['AttributionNote']+=' The document voice is the exact grammatical source of the clause.';save('t_3972185a2e25',p,z)
p,z=load('t_b191c4fa2e9f');o=z['Senses'][0]['Occurrences'][2];o['AttributionNote']='Recorded Sayings of Chan Master Yunxi Langting (雲溪俍亭挺禪師語錄): Layman Jinghui is the exact questioner requesting instruction; Yunxi Langting is the contextual respondent.';save('t_b191c4fa2e9f',p,z)
p,z=load('t_ff50c6974a36');o=z['Senses'][1]['Occurrences'][0];o['AttributionNote']='Patriarchs’ Hall Collection (祖堂集): the immortal Gaitong is the exact identified non-master utterer of the five-position line.';save('t_ff50c6974a36',p,z)

# 無住: distinct person sense for Baotang Wuzhu, separate from non-abiding.
p,z=load('t_395ae8fd7f32');occs=[]
for rel,kw in [('X/X80/X80n1568.xml','益州保唐寺無住禪師益州保唐寺無住禪師初得法於無相大師'),('X/X80/X80n1565.xml','益州保唐寺無住禪師益州保唐寺無住禪師初得法於無相大師')]:
 v=zc.verify(rel,kw);assert v['ok'];occs.append({'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'ContextMasters':[{'MasterName':'Baotang Wuzhu','Roles':['section-subject','person-described']}],'Curated':True,'AttributionNote':f'Source text ({zc.title(rel)}). Compiler heading and biography identify Baotang Wuzhu as the person named Wuzhu; the headword is not a spoken lexical predicate.','ActorAttribution':{'Status':'impersonal','Kind':'biographical section heading','ActorLabel':'the lamp-record compiler','ActorRole':'compiler','GrammarEvidence':'The headed biography names the person and immediately begins his life record; no dialogue turn supplies the name.','ReviewedBy':'Codex root-revision repair','ReviewedUtc':'2026-07-14T23:40:00Z'}})
z['Senses'].append({'SenseKey':'person-baotang-wuzhu','MasterName':None,'PreferredTarget':'Baotang Wuzhu','AlternateTargets':['Chan Master Wuzhu of Baotang'],'Status':'drafted','Explanation':'The exact graph sequence also names Baotang Wuzhu, a historical Chan teacher. These headed biographies are a person-name deployment and are kept separate from the lexical predicate meaning non-abiding.','Validation':'Two independent lamp compilations preserve the headed biography.','Note':'This person sense prevents personal-name hits from being folded into the abstract lexical sense.','Occurrences':occs,'SourceTexts':[o['RelPath'] for o in occs],'RelatedMasters':['Baotang Wuzhu'],'RelatedTerms':[]});save('t_395ae8fd7f32',p,z)

# 任運: oxherding title metadata uses the compiler role.
p,z=load('t_7cddddb76d37')
for o in z['Senses'][1]['Occurrences']:o['ActorAttribution']['ActorRole']='compiler';o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}). Impersonal document heading metadata labels the seventh oxherding section; no person utters the headword.'
save('t_7cddddb76d37',p,z)

# 傳燈: distinguish metadata, Yang Yi's official prose, and actual hall speech.
p,z=load('t_0e49b88aecba');s0=z['Senses'][0];o=s0['Occurrences'][2];o.pop('MasterName',None);o['ContextMasters']=[];o['ActorAttribution']={'Status':'identified-non-master','Kind':'named official preface author','ActorLabel':'the official Yang Yi','ActorRole':'compiler','GrammarEvidence':'The preface identifies Yang Yi by official title; this sentence belongs to his authored prose, not a Chan-master speech turn.','ReviewedBy':'Codex root-revision repair','ReviewedUtc':'2026-07-14T23:40:00Z'};o['AttributionNote']='Jingde Record of the Transmission of the Lamp (景德傳燈錄): the official Yang Yi is the identified non-master author who invokes the metaphor of transmitting the lamp.'
s1=z['Senses'][1];metadata(s1['Occurrences'][0],'the book-title compiler','Jingde Record of the Transmission of the Lamp (景德傳燈錄): impersonal title metadata supplies the book name.',[{'MasterName':'Yang Yi','Roles':['person-described']}]);metadata(s1['Occurrences'][1],'the contents compiler','Continuation of the Lamp Record (續傳燈錄): impersonal title and contents metadata supply the book name.');o=s1['Occurrences'][2];o.pop('MasterName',None);o['ContextMasters']=[];o['ActorAttribution']={'Status':'identified-non-master','Kind':'named official preface author','ActorLabel':'the official Yang Yi','ActorRole':'compiler','GrammarEvidence':'The signed official preface assigns this naming sentence to Yang Yi.','ReviewedBy':'Codex root-revision repair','ReviewedUtc':'2026-07-14T23:40:00Z'};o['AttributionNote']='Jingde Record of the Transmission of the Lamp (景德傳燈錄): the official Yang Yi states that the thirty-volume compilation was titled the Jingde Record of the Transmission of the Lamp.';save('t_0e49b88aecba',p,z)

# 心印 person-title heading: Zhixun is section subject, not utterer.
p,z=load('t_c968268a64d1');o=z['Senses'][1]['Occurrences'][0];metadata(o,'the biographical heading compiler','Complete Collection of the Five Lamps (五燈全書(第34卷-第120卷)): impersonal biographical heading metadata names Chan Master Xinyin of Kaixian, whose personal name is Zhixun.',[{'MasterName':'Kaixian Zhixun','Roles':['section-subject','person-described']}]);save('t_c968268a64d1',p,z)

# 行腳 first occurrence: unnamed monk asks; Poshan responds.
p,z=load('t_52fdda90e9ab');o=z['Senses'][0]['Occurrences'][0];o.pop('MasterName',None);o['ContextMasters']=[{'MasterName':'Poshan Haiming','Roles':['respondent','record-owner']}];o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'monk','ActorLabel':'the unnamed monk asking about traveling on foot','ActorRole':'questioner','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'ReviewedBy':'Codex root-revision repair','ReviewedUtc':'2026-07-14T23:40:00Z'};o['AttributionNote']='Recorded Sayings of Chan Master Poshan (破山禪師語錄): an unnamed monk is the exact questioner asking about the business of traveling on foot; Poshan Haiming is the respondent and record owner.';save('t_52fdda90e9ab',p,z)
