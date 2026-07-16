#!/usr/bin/env python3
import hashlib,json,sys
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; E=ROOT/'fresh-build'/'entries'; HERE=Path(__file__).parent
sys.path.insert(0,str(ROOT));import zc
IDS=Path('/tmp/backlog_ids.txt').read_text().split()
RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def now():return datetime.now(timezone.utc).isoformat().replace('+00:00','Z')
def cm(n,*roles):return {'MasterName':n,'Roles':list(roles)}
def unnamed(kind,label,role,evidence):return {'Status':'reviewed-unnamed','Kind':kind,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':evidence,'ReviewedBy':'Codex live backlog literal full-case repair','ReviewedUtc':now()}
def narrated(kind,label,evidence):return {'Status':'narrated','Kind':kind,'ActorLabel':label,'ActorRole':'compiler','RungsChecked':RUNGS,'GrammarEvidence':evidence,'ReviewedBy':'Codex live backlog literal full-case repair','ReviewedUtc':now()}
def normalize_note(o):
 aa=o.get('ActorAttribution') or {}; actor=o.get('MasterName') or aa.get('ActorLabel') or 'no human actor'
 evidence=aa.get('GrammarEvidence') or ('The complete case assigns the headword-bearing wording to '+actor+'.' if o.get('MasterName') else 'The complete case was read to distinguish utterance, narration, and action.')
 o['AttributionNote']=f"{zc.title(o['RelPath'])}. Exact headword actor: {actor}. {evidence}"
def add_pending():
 p=ROOT/'fresh-build'/'pending-roster.json';d=json.loads(p.read_text());known={x['canonicalName'] for x in d['candidates']}
 for name,aliases,rel,lb,kw in [
  ('Benxi',['本谿和尚','本溪和尚'],'X/X79/X79n1557.xml','0053c18','師問居士：達磨西來第一句作麼生道？'),
  ('Yunmen Cheng',['雲門澄'],'X/X66/X66n1296.xml','0023c20','雲門澄云：老翁更若如何若何，便與一喝，拂袖竟去。')]:
  if name not in known:d['candidates'].append({'canonicalName':name,'aliases':aliases,'evidence':[{'RelPath':rel,'FromLb':lb,'ToLb':lb,'Kwic':kw}],'reviewedBy':'Codex live backlog literal full-case repair','reviewReport':'live-backlog-25-repair-ledger.json','status':'awaiting-roster-integration'})
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
def main():
 add_pending()
 rows=[]
 for tid in IDS:
  p=E/tid/'entry.v2.json';d=json.loads(p.read_text());before=sha(p)
  term=d['SourceTerm']; senses=d['Senses'];o=senses[0]['Occurrences']
  if term=='觀心':
   o[4]['ActorAttribution']['ActorLabel']='Aisin Gioro Yinzhen, the Yongzheng emperor'
   o[4]['ActorAttribution']['GrammarEvidence']='The first-person imperial discourse directly warns that making silent inspection of mind the ultimate is escaping one pit only to fall into another.'
  elif term=='下語':
   o[2]['Kwic']='多有人下語並不契泉意';o[2]['FromLb']=o[2]['ToLb']='0702b05'
   o[3]['ContextMasters']=[cm('Yulin Tongxiu','person-described','section-subject')]
  elif term=='下禪床':
   o[1]['Kwic']='師下禪床';o[1]['FromLb']=o[1]['ToLb']='0710c19'
   o[2]['Kwic']='識得伊跳下禪床便歸去';o[2]['FromLb']='0675c11';o[2]['ToLb']='0675c12';o[2]['ActorAttribution']['Kind']='capping-verse authorship'
  elif term=='安心':
   o[3]['Kwic']='安心竟';o[3]['FromLb']=o[3]['ToLb']='0942a28';o[3]['ContextMasters']=[cm('Konggu Daocheng','utterer','respondent','section-subject')]
  elif term=='法身向上事':
   o[3]['Kwic']='老僧咸通年前會得法身邊事，咸通年後會得法身向上事';o[3]['FromLb']='0175c08';o[3]['ToLb']='0175c09';o[3]['ContextMasters']=[cm('Shushan Kuangren','utterer','section-subject')]
  elif term=='目前無法':
   o[6]['Kwic']='目前無法，意在目前';o[6]['FromLb']=o[6]['ToLb']='0410b07'
  elif term=='撫掌':
   o[2]['Kwic']='師撫掌大笑';o[2]['FromLb']=o[2]['ToLb']='0075c19'
   o[4]['Kwic']='僧撫掌一下便打';o[4]['FromLb']='0691b06';o[4]['ToLb']='0691b07'
   senses[0]['Explanation']=senses[0]['Explanation'].replace('another monk','a second unnamed monk')
  elif term=='言前':
   o[1]['Kwic']='言前薦得辜負平生句後投機殊乖道體離此二途請師方便';o[1]['FromLb']='0670b15';o[1]['ToLb']='0670b17';o[1]['MasterName']=None;o[1]['ActorAttribution']=unnamed('monastic questioner','the unnamed monk','questioner','The complete exchange assigns the headword-bearing request to the monk; Shoushan raises the whisk only in response.');o[1]['ContextMasters']=[cm('Shoushan Xingnian','respondent','section-subject')]
   o[3]['Kwic']='風穴下來言前事';o[3]['FromLb']=o[3]['ToLb']='0488a03'
  elif term=='臘八':o[7]['Kwic']='臘八上堂';o[7]['FromLb']=o[7]['ToLb']='0019a04'
  elif term=='茫然':o[5]['Kwic']='僧茫然';o[5]['FromLb']=o[5]['ToLb']='0131b23'
  elif term=='佛手':
   x=senses[1]['Occurrences'][1];x['Kwic']='火燒山佛手遮不得';x['FromLb']='0824a16';x['ToLb']='0824a17'
  elif term=='禪床':
   o[0]['ActorAttribution']['ActorLabel']='Dongpo, the named lay verse author'
   o[0]['ActorAttribution']['GrammarEvidence']='The explicit frame 士乃作偈曰 introduces Dongpo’s verse; the verse itself contains 借君四大作禪床.'
   o[6]['Kwic']='遶禪床掣電之機落二三';o[6]['FromLb']=o[6]['ToLb']='0659a08'
  elif term=='家風':o[0]['Kwic']='僧問如何是天柱家風';o[0]['FromLb']='0169b07';o[0]['ToLb']='0169b08'
  elif term=='陞座':o[4]['Kwic']='來日陞座';o[4]['FromLb']=o[4]['ToLb']='0635b20'
  elif term=='石人':o[5]['Kwic']='若問經中何極則石人夜𦗟木雞鳴';o[5]['FromLb']='0670b16';o[5]['ToLb']='0670b17'
  elif term=='第一句':
   o[1]['Kwic']='問如何是第一句';o[1]['FromLb']=o[1]['ToLb']='0178a10'
   o[4]['MasterName']='Benxi';o[4].pop('ActorAttribution',None);o[4]['ContextMasters']=[cm('Benxi','utterer','questioner','section-subject'),cm('Bodhidharma','case-figure')]
  elif term=='問訊':o[5]['Kwic']='僧問訊次';o[5]['FromLb']=o[5]['ToLb']='0002c11'
  elif term=='法身邊事':
   o[4]['Kwic']='君臣道合，猶是法身邊事';o[4]['FromLb']='0111b19';o[4]['ToLb']='0111b20';o[4]['ContextMasters']=[cm('Langye Huijue','utterer','section-subject')]
  elif term=='拂袖':
   o[3]['Kwic']='泉拂袖便行';o[3]['FromLb']=o[3]['ToLb']='0616c16';o[3]['ContextMasters']=[cm('Nanquan Puyuan','person-described')]
   o[4]['MasterName']='Yunmen Cheng';o[4].pop('ActorAttribution',None);o[4]['ContextMasters']=[cm('Yunmen Cheng','utterer','section-subject')]
  elif term=='藥師':
   o[2]['Kwic']='卒哭藥師';o[2]['FromLb']=o[2]['ToLb']='0104a06';o[2]['ActorAttribution']={'Status':'impersonal','Kind':'editorial ceremony heading','ActorLabel':'the anthology ceremony heading','ActorRole':'compiler','RungsChecked':RUNGS,'GrammarEvidence':'The headword labels the Medicine Master memorial occasion before Zhongfeng Mingben’s address; it is not spoken dialogue.','ReviewedBy':'Codex live backlog literal full-case repair','ReviewedUtc':now()}
  elif term=='雪山童子':o[0]['Kwic']='問雪山童子捨身為求諸行此行如何';o[0]['FromLb']=o[0]['ToLb']='0789c01'
  for s in d['Senses']:
   for o in s['Occurrences']:
    aa=o.get('ActorAttribution') or {}
    if aa.get('Status')=='reviewed-unnamed' and 'unnamed' not in (aa.get('ActorLabel') or '').lower():aa['ActorLabel']='unnamed '+(aa.get('ActorLabel') or aa.get('Kind') or 'actor')
    normalize_note(o)
   for a in s.get('ClaimAnchors') or []:normalize_note(a)
  p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
  rows.append({'id':tid,'term':d['SourceTerm'],'beforeSha256':before,'afterSha256':sha(p)})
 (HERE/'live-backlog-25-repair-ledger.json').write_text(json.dumps({'generatedUtc':now(),'rows':rows},ensure_ascii=False,indent=2)+'\n')
if __name__=='__main__':main()
