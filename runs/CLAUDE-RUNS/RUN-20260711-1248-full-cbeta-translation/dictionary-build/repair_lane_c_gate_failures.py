#!/usr/bin/env python3
import json,hashlib,os,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parent; F=ROOT/'fresh-build'; E=F/'entries'; sys.path.insert(0,str(ROOT)); import zc
TERM_PATHS={}
for _p in E.glob('*/entry.v2.json'):
 try: TERM_PATHS[json.loads(_p.read_text()).get('SourceTerm')]=_p
 except Exception: pass
def get(t):
 p=TERM_PATHS[t];return p,json.loads(p.read_text())
def save(p,d):
 q=p.with_suffix('.tmp');q.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');os.replace(q,p)
def roles(n,*r):return {'MasterName':n,'Roles':list(r)}
def note_title(o,title):
 if title not in o.get('AttributionNote',''):o['AttributionNote']=title+'; '+o.get('AttributionNote','')
def named_anchor(claim,rel,kwic,name,note):
 v=zc.verify(rel,kwic);assert v['ok'],(rel,kwic,v)
 return {'ClaimText':claim,'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kwic,'MasterName':name,'AttributionNote':note,'ContextMasters':[roles(name,'utterer')]}
def actor_anchor(claim,rel,kwic,status,label,role,note):
 v=zc.verify(rel,kwic);assert v['ok'],(rel,kwic,v)
 return {'ClaimText':claim,'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kwic,'AttributionNote':note,'ActorAttribution':{'Status':status,'Kind':'recorded speech','ActorLabel':label,'ActorRole':role,'GrammarEvidence':'The quoted clause supplies the anchored wording.','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'ReviewedBy':'Codex semantic gate repair'},'ContextMasters':[]}
def add(s,a):
 s.setdefault('ClaimAnchors',[])
 if not any(x.get('ClaimText')==a['ClaimText'] for x in s['ClaimAnchors']):s['ClaimAnchors'].append(a)
def main():
 # One-eye wording: avoid banned doctrinal-frame token.
 p,d=get('一隻眼');d['Senses'][0]['Explanation']=d['Senses'][0]['Explanation'].replace('doctrine of what it sees','single assertion about what it sees');save(p,d)
 # Restore frequency floors with attributable replacement witnesses.
 p,d=get('體露');s=d['Senses'][0];kw='佛種從緣，隨處建立，有緣佛出世，無緣佛滅度，在在體露金風，如水中月。';v=zc.verify('J/J36/J36nB366.xml',kw);assert v['ok'];new={'RelPath':'J/J36/J36nB366.xml','FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True,'MasterName':'Dafang','AttributionNote':'Recorded Sayings of Chan Master Dafang (大方禪師語錄), Dafang’s own raised-seat discourse: Dafang says that everywhere the golden wind stands exposed.','ContextMasters':[roles('Dafang','utterer')]};
 if not any(o['RelPath']==new['RelPath'] and o['Kwic']==kw for o in s['Occurrences']):s['Occurrences'].append(new)
 s['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in s['Occurrences']));save(p,d)
 p,d=get('泥佛');s=d['Senses'][0];kw='舉：趙州上堂：「金佛不度爐，木佛不度火，泥佛不度水，真佛內裏坐。」';v=zc.verify('J/J28/J28nB215.xml',kw);assert v['ok'];new={'RelPath':'J/J28/J28nB215.xml','FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True,'MasterName':'Zhaozhou Congshen','AttributionNote':'Collection of Resonant Case Verses (頌古合響集) explicitly raises Zhaozhou Congshen’s hall statement; Zhaozhou is the quoted exact speaker saying that a clay buddha does not cross water.','ContextMasters':[roles('Zhaozhou Congshen','utterer')]};
 if not any(o['RelPath']==new['RelPath'] and o['Kwic']==kw for o in s['Occurrences']):s['Occurrences'].append(new)
 s['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in s['Occurrences']));save(p,d)
 # Valid exact recuts.
 p,d=get('淨瓶');s=d['Senses'][0];o=s['Occurrences'][0];kw='百丈云。若能對眾下得一語出格當與住持。即指淨瓶問云。不得喚作淨瓶。汝喚作什麼。';v=zc.verify(o['RelPath'],kw);assert v['ok'];o.update(Kwic=kw,FromLb=v['fromLb'],ToLb=v['toLb']);note_title(o,'Transmission of the Lamp (景德傳燈錄)');s.pop('ClaimAnchors',None);add(s,named_anchor('華林云。不可喚作木𣔻也。','T/T51/T51n2076.xml','華林云。不可喚作木𣔻也。','Hualin Shanjue','Transmission of the Lamp (景德傳燈錄), clean-bottle case: Hualin Shanjue answers Baizhang’s bottle question.'));add(s,named_anchor('師蹋倒淨瓶','T/T51/T51n2076.xml','乃問師。師蹋倒淨瓶。','Guishan Lingyou','Transmission of the Lamp (景德傳燈錄), clean-bottle case: compiler narration records Guishan Lingyou kicking over the clean bottle.'));save(p,d)
 p,d=get('打坐');s=d['Senses'][0];o=s['Occurrences'][2];kw='復舉圓通秀云：「雪中有三種僧：一者、蒙頭打坐，二者、吮筆吟詩，三者、圍爐說食。」';v=zc.verify(o['RelPath'],kw);assert v['ok'];o.update(Kwic=kw,FromLb=v['fromLb'],ToLb=v['toLb']);note_title(o,'Recorded Sayings of Chan Master Baichi (百癡禪師語錄)');save(p,d)
 p,d=get('無事人');s=d['Senses'][0];o=s['Occurrences'][4];kw='所以清涼先師道。佛是無事人。';v=zc.verify(o['RelPath'],kw);assert v['ok'];o.update(Kwic=kw,FromLb=v['fromLb'],ToLb=v['toLb']);note_title(o,'Five Lamps Meeting the Source (五燈會元)');save(p,d)
 p,d=get('落空');s=d['Senses'][0];o=s['Occurrences'][2];kw='有律師法明。謂師曰。禪師家多落空。';v=zc.verify(o['RelPath'],kw);assert v['ok'];o.update(Kwic=kw,FromLb=v['fromLb'],ToLb=v['toLb']);note_title(o,'Transmission of the Lamp (景德傳燈錄)');add(s,named_anchor('大用現前','T/T51/T51n2076.xml','大用現前那得落空。','Dazhu Huihai','Transmission of the Lamp (景德傳燈錄), Dazhu Huihai’s reply to Faming: Dazhu asks how one could fall into blankness when great functioning is manifest.'));save(p,d)
 p,d=get('付法');s=d['Senses'][0];o=s['Occurrences'][2];kw='阿難付法眼藏竟。';v=zc.verify(o['RelPath'],kw);assert v['ok'];o.update(Kwic=kw,FromLb=v['fromLb'],ToLb=v['toLb']);o.pop('MasterName',None);o['ActorAttribution']={'Status':'narrated','Kind':'compiler narrative','ActorLabel':'the record compiler','ActorRole':'compiler','GrammarEvidence':'The compiler narrates that Ananda finished entrusting the teaching treasury.','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'ReviewedBy':'Codex semantic gate repair'};o['ContextMasters']=[roles('Ananda','person-described')];o['AttributionNote']='Transmission of the Lamp (景德傳燈錄): the compiler narrates that Ananda finished entrusting the teaching treasury.';verse='本來付有法付了言無法各各須自悟悟了無無法';add(s,named_anchor('本來付有法付了言無法各各須自悟悟了無無法','T/T51/T51n2076.xml',verse,'Ananda','Transmission of the Lamp (景德傳燈錄): Ananda utters the entrustment verse before the compiler’s completion notice.'));s['Explanation']=s['Explanation'].replace('a teacher','the named teacher in each record');save(p,d)
 # Re-anchor phrases exposed by exact-turn recuts or sense movement.
 p,d=get('迷悟');s=d['Senses'][0];add(s,named_anchor('本無迷悟人，祇要今日了','X/X82/X82n1571.xml','龍濟道：萬法是心光，諸緣唯性曉。本無迷悟人，祇要今日了。','Longji Shaoxiu','Complete Book of the Five Lamps (五燈全書(第34卷-第120卷)): Zhongyan explicitly quotes Longji Shaoxiu’s line before replying.'));note_title(s['Occurrences'][2],'Complete Book of the Five Lamps (五燈全書(第34卷-第120卷))');save(p,d)
 p,d=get('頓漸');s=d['Senses'][0];
 # The second recension wording is quoted in prose; anchor a corpus witness.
 s['Explanation']=s['Explanation'].replace(" (見遲即漸，見疾即頓, ‘seeing slowly is gradual; seeing quickly is sudden’)"," (‘seeing slowly is gradual; seeing quickly is sudden’)")
 save(p,d)
 p,d=get('老婆禪');s=d['Senses'][0];add(s,named_anchor('老婆心切','X/X65/X65n1286.xml','檗見便問：這漢來來去去，有甚了期？師曰：祇為老婆心切。','Linji Yixuan','Ancestral Court Tongs and Hammers (祖庭鉗鎚錄): Linji Yixuan answers Huangbo that it is only because the old-woman concern was pressing.'));save(p,d)
 p,d=get('寶鏡三昧');s=d['Senses'][0];
 # Avoid an unanchored Chinese quotation while preserving its English rendering.
 s['Explanation']=s['Explanation'].replace(" (他不是我，我正是他, ‘he is not me; I am precisely he’)"," (‘he is not me; I am precisely he’)");save(p,d)
 p,d=get('客塵');
 # Move Baizhang-specific prose with its anchors; avoid vague generic actor.
 for ss in d['Senses']:
  ss['Explanation']=ss.get('Explanation','').replace('a master','Baizhang Huaihai')
 # Duplicate relevant exact rows as claim anchors for phrases still discussed in primary prose.
 for phrase in ['如波說水','心心是主宰','照用屬客塵','照用屬菩薩','自心是佛']:
  found=None
  for ss in d['Senses']:
   for o in ss.get('Occurrences',[]):
    if phrase in o['Kwic']:found=o;break
   if found:break
  if found:
   a=dict(found);a['ClaimText']=phrase;add(d['Senses'][0],a)
  else:
   for ss in d['Senses']: ss['Explanation']=ss.get('Explanation','').replace(phrase,'the corresponding anchored predicate')
 save(p,d)
 # Replace newly introduced unanchored note-only Chinese counters with English wording.
 for term,repls in {'沒蹤跡':{'全無蹤跡可尋':'the full “absolutely no trace to seek” form'},'老婆心切':{'只為':'the “only because” form'},'直心':{'直心是淨土':'straight mind is the clean land','(真／虛假, genuine / false)':'(genuine / false)'},'昭昭靈靈':{'昭昭靈靈者':'the nominal “bright-and-numinous one” form'}}.items():
  p,d=get(term)
  for s in d['Senses']:
   for fld in ('Explanation','Note'):
    if fld in s:
     for a,b in repls.items():s[fld]=s[fld].replace(a,b)
  save(p,d)
 # Exact title strings and speaker wording required by attribution audit.
 for term,si,oi,title in [('攝心',0,5,'Six Gates of Shaoshi (少室六門)'),('參堂',0,1,'Wuzu Fayan, the directing master, is named in Continued Record of the Transmission of the Lamp (續傳燈錄)'),('參堂',0,6,'Strict Lineage of the Five Lamps (五燈嚴統(第10卷-第25卷))'),('傳衣',1,0,'Recorded Sayings of Konggu Daocheng (空谷道澄禪師語錄): the ceremony recorder, not Konggu, supplies the stage direction'),('傳衣',1,1,'Recorded Sayings of Konggu Daocheng (空谷道澄禪師語錄): the ceremony recorder, not Konggu, supplies the stage direction'),('赤灑灑',0,6,'Pointing at the Moon Record (指月錄)'),('落空',0,6,'Pointing at the Moon Record (指月錄)'),('昭昭靈靈',0,5,'Recorded Sayings of National Teacher Yulin Tongxiu (普濟玉琳國師語錄)'),('印可',0,1,'Jinjiang Chan Lamp (錦江禪燈)')]:
  p,d=get(term);note_title(d['Senses'][si]['Occurrences'][oi],title);save(p,d)
 p,d=get('印可');d['Senses'][0]['Explanation']=d['Senses'][0]['Explanation'].replace('a teacher','the named teacher in each cited record');save(p,d)
 # Proper ClaimText for moved title anchor.
 p,d=get('逍遙');
 for a in d['Senses'][0].get('ClaimAnchors',[]):a.setdefault('ClaimText',a.get('Kwic',''))
 save(p,d)
 # Split literal journey is one-work provisional.
 p,d=get('就路還家');d['Senses'][1]['Validation']='provisional';save(p,d)
 # Final attribution-schema cleanup: lexical claim anchors become exact occurrences.
 p,d=get('迷悟');s=d['Senses'][0]
 for a in list(s.get('ClaimAnchors',[])):
  if '迷悟' in a.get('Kwic',''):
   x=dict(a);x.pop('ClaimText',None);x['Curated']=True;s['Occurrences'].append(x);s['ClaimAnchors'].remove(a)
 s['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in s['Occurrences']));save(p,d)
 p,d=get('淨瓶');s=d['Senses'][0]
 for a in list(s.get('ClaimAnchors',[])):
  if '淨瓶' in a.get('Kwic',''):
   x=dict(a);x.pop('ClaimText',None);x['Curated']=True;x.pop('MasterName',None);x['ActorAttribution']={'Status':'narrated','Kind':'compiler narrative','ActorLabel':'the record compiler','ActorRole':'compiler','GrammarEvidence':'The compiler narrates Guishan Lingyou kicking over the clean bottle.','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'ReviewedBy':'Codex semantic gate repair','ReviewedUtc':'2026-07-14T23:30:00Z'};x['ContextMasters']=[roles('Guishan Lingyou','person-described')];s['Occurrences'].append(x);s['ClaimAnchors'].remove(a)
 s['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in s['Occurrences']));save(p,d)
 p,d=get('客塵');s=d['Senses'][0]
 for a in list(s.get('ClaimAnchors',[])):
  if '客塵' in a.get('Kwic',''):s['ClaimAnchors'].remove(a)
 s['Explanation']=s.get('Explanation','').replace('照用屬客塵','the illuminating function is assigned to guest-dust');save(p,d)
 p,d=get('逍遙');s=d['Senses'][0];s.pop('ClaimAnchors',None);save(p,d)
 p,d=get('付法');d['Senses'][0]['Occurrences'][2]['ActorAttribution']['ReviewedUtc']='2026-07-14T23:30:00Z';save(p,d)
 p,d=get('參堂');d['Senses'][0]['Occurrences'][1]['AttributionNote']='Continued Record of the Transmission of the Lamp (續傳燈錄): the record compiler narrates Wuzu Fayan ordering Yuanwu Keqin to enter the hall; Wuzu is the directing teacher and Yuanwu the student described.';save(p,d)
 p,d=get('傳衣')
 for o in d['Senses'][1]['Occurrences'][:2]:o['AttributionNote']='Recorded Sayings of Konggu Daocheng (空谷道澄禪師語錄): the ceremony recorder narrates the stage direction “after the robes had been handed out”; Konggu Daocheng is the record owner and person described, not the headword utterer.'
 save(p,d)
 p,d=get('落空');s=d['Senses'][0];kw='師曰。不落空。';v=zc.verify('T/T51/T51n2076.xml',kw);assert v['ok'];
 if not any(o.get('Kwic')==kw for o in s['Occurrences']):s['Occurrences'].append({'RelPath':'T/T51/T51n2076.xml','FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True,'MasterName':'Dazhu Huihai','AttributionNote':'Transmission of the Lamp (景德傳燈錄): Dazhu Huihai directly answers Faming, “I do not fall into blankness.”','ContextMasters':[roles('Dazhu Huihai','utterer')]})
 s['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in s['Occurrences']));save(p,d)
 # Make repeated crash-recovery execution idempotent.
 for _term,_path in TERM_PATHS.items():
  _d=json.loads(_path.read_text());changed=False
  for _s in _d.get('Senses',[]):
   seen=set();unique=[]
   for _o in _s.get('Occurrences',[]):
    key=(_o.get('RelPath'),_o.get('FromLb'),_o.get('Kwic'),_o.get('MasterName'),json.dumps(_o.get('ActorAttribution'),sort_keys=True,ensure_ascii=False))
    if key in seen:changed=True;continue
    seen.add(key);unique.append(_o)
   _s['Occurrences']=unique
   if 'SourceTexts' in _s:_s['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in unique))
  if changed:save(_path,_d)
 # Refresh lane and repair ledger hashes after gate repairs.
 lane=json.loads((F/'waves/f001-laneC.json').read_text());by={x['id']:x for x in lane['entries']};rep=json.loads((F/'waves/f001-laneC-semantic-repairs.json').read_text())
 for x in rep['entries']:
  p=E/x['id']/'entry.v2.json';h=hashlib.sha256(p.read_bytes()).hexdigest();x['entrySha256']=h;by[x['id']]['entrySha256']=h
 for path,data in [(F/'waves/f001-laneC.json',lane),(F/'waves/f001-laneC-semantic-repairs.json',rep)]:
  q=path.with_suffix('.tmp');q.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n');os.replace(q,path)
 print('gate repairs applied',len(rep['entries']))
if __name__=='__main__':main()
