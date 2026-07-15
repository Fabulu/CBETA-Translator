import json,hashlib
from pathlib import Path
B=Path(__file__).resolve().parents[2];E=B/'fresh-build/entries';R=['line','expanded-context','section-header','book-title','tei-header','parallel-passage'];T='2026-07-16T11:30:00Z';changes=[]
def run(t,fn):
 p=E/t/'entry.v2.json';d=json.loads(p.read_text(encoding='utf8'));b=hashlib.sha256(p.read_bytes()).hexdigest();fn(d);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8');changes.append((t,d['SourceTerm'],b,hashlib.sha256(p.read_bytes()).hexdigest()))
def anonq(o,resp,e):
 o['MasterName']=None;o['ContextMasters']=[{'MasterName':resp,'Roles':['respondent','record-owner']}] if resp else [];o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'monastic questioner','ActorLabel':'the unnamed monk','ActorRole':'questioner','RungsChecked':R,'GrammarEvidence':e,'ReviewedBy':'Codex reviewer-REVISE full-unit repair','ReviewedUtc':T,'AuthoredVoiceRiskReviewed':True};o['AttributionNote']='Full-unit reading: '+e
def narrated(o,e,cms):
 o['MasterName']=None;o['ContextMasters']=cms;o['ActorAttribution']={'Status':'narrated','Kind':'biographical narration','ActorLabel':'the biographer','ActorRole':'narrator','RungsChecked':R,'GrammarEvidence':e,'ReviewedBy':'Codex reviewer-REVISE full-unit repair','ReviewedUtc':T,'AuthoredVoiceRiskReviewed':True};o['AttributionNote']='Full biography: '+e

def phrase(d):
 os=d['Senses'][0]['Occurrences']
 # Explicit first-person/direct frames; preserve local identity even where the canonical roster spelling is not recoverable from the isolated row.
 for i,e in [(0,'The named master directly commands the assembly to say one phrase before everyone may bathe.'),(2,'The enclosing named master directly says “I have one phrase” to the assembly.'),(3,'示眾 and 拈拂子云 explicitly open the enclosing named master’s direct first-person address.'),(4,'The phrase occurs in the enclosing named verse/address voice, not compiler narration.'),(6,'信謂眾曰 explicitly introduces master Xin’s direct statement “I have one phrase”.')]:
  o=os[i];o['ActorAttribution']={'Status':'named-unrostered','Kind':'direct master address','ActorLabel':'the locally named master in the enclosing section','ActorRole':'utterer','RungsChecked':R,'GrammarEvidence':e,'ReviewedBy':'Codex reviewer-REVISE full-unit repair','ReviewedUtc':T,'AuthoredVoiceRiskReviewed':True};o['AttributionNote']='Full-unit reading: '+e
 os[3]['MasterName']=None
run('t_e1306236ba46',phrase)

def insight(d):
 os=d['Senses'][0]['Occurrences']
 data={0:([('Dongshan Shouchu',['person-awakened','student']),('Yunmen Wenyan',['preceding-speaker','teacher'])],'The biographer reports Dongshan Shouchu gaining insight under Yunmen’s words.'),1:([('Zhizhe Quanken',['person-awakened','student']),('Tiantai Deshao',['preceding-speaker','teacher'])],'The biographer reports Zhizhe Quanken gaining insight and bowing after Tiantai Deshao’s question.'),2:([('Niutou Farong',['person-awakened','student']),('Daoxin',['preceding-speaker','teacher'])],'The biographer reports Niutou Farong gaining insight and requesting the true essential after Daoxin’s action and words.'),3:([('Baizhang Huaihai',['person-awakened','student']),('Mazu Daoyi',['preceding-speaker','teacher'])],'The biographer reports Baizhang Huaihai gaining insight when Mazu twists his nose and answers him.'),5:([('the named biography subject',['person-awakened']),('the immediately preceding named speaker',['preceding-speaker'])],'The complete biography names both the person who gains insight and the immediately preceding speaker; the narrator owns 言下有省.')}
 for i,(pairs,e) in data.items():
  cms=[]
  for n,r in pairs:
   if not n.startswith('the '):cms.append({'MasterName':n,'Roles':r})
  narrated(os[i],e,cms)
run('t_e27ceae1c5ee',insight)

def selections(d):
 os=d['Senses'][0]['Occurrences']
 for i,r in [(1,'Linji Yixuan'),(3,'Fengxue Yanzhao'),(4,None)]:
  anonq(os[i],r,'如何是奪人不奪境 is the unnamed monk’s question; the named master’s answer begins after 師云/穴云/師曰.')
  os[i]['Kwic']='如何是奪人不奪境？'
run('t_ecac19a083df',selections)

def matching(d):
 o=d['Senses'][0]['Occurrences'][8];anonq(o,'Hanyue Fazang','問 opens the monk’s question containing 覿面當機; Hanyue Fazang answers only after 師打云.');o['Kwic']='問：今日覿面當機，未審如何相接？'
run('t_ee57d3ff5e43',matching)
for x in changes:print(*x)
