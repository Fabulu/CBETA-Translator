#!/usr/bin/env python3
import hashlib,json,sys
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];E=ROOT/'fresh-build'/'entries';HERE=Path(__file__).parent
sys.path.insert(0,str(ROOT));import zc
IDS=['t_5d8f2f79dc60','t_5da65e2989a0','t_5ed1c64c6559','t_68729efe1fac','t_6abcff898d95']
ORIGINAL={'t_5d8f2f79dc60':'d436e0b9f3089181eab5fa975e1740f9155999abb43cb889b9347d9de83325d2','t_5da65e2989a0':'b9c14c0c08344e7159d9686ded5ca7eb9a74b35c468ae073cfc84961f7342bf4','t_5ed1c64c6559':'cc1f49d086033e5b429a4910d8d171c857d335e50a63ca55e7ce53f179d040d3','t_68729efe1fac':'39a757769c40ff153b1fcf4b6f7e732719a8909d360fbb43f0fdfa1192fc22ee','t_6abcff898d95':'121600415b5cd401a849a7d1753474296f81937e665609ac59668d66f7638cff'}
RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
def now():return datetime.now(timezone.utc).isoformat().replace('+00:00','Z')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def cm(n,*r):return {'MasterName':n,'Roles':list(r)}
def unnamed(kind,label,role,evidence):return {'Status':'reviewed-unnamed','Kind':kind,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':evidence,'ReviewedBy':'Codex cohorts 1-3 146-150 literal full-case read','ReviewedUtc':now()}
def narrated(kind,label,evidence):return {'Status':'narrated','Kind':kind,'ActorLabel':label,'ActorRole':'compiler','RungsChecked':RUNGS,'GrammarEvidence':evidence,'ReviewedBy':'Codex cohorts 1-3 146-150 literal full-case read','ReviewedUtc':now()}
def add_pending():
 p=ROOT/'fresh-build'/'pending-roster.json';d=json.loads(p.read_text());known={x['canonicalName'] for x in d['candidates']}
 rows=[
  ('Taiping An',['永州太平安禪師','太平安'],'X/X82/X82n1571.xml','0051a23','鎮州蘿蔔極貴，廬陵米價甚賤。'),
  ('Yuelin Zhen',['明州岳林真禪師','岳林真'],'X/X82/X82n1571.xml','0029b13','古人道，初秋夏末，合有責情三十棒。'),
  ('Tianping Qiyu',['相州天平山契愚禪師','天平契愚'],'X/X78/X78n1556.xml','0492a04','師曰。鎮州蘿蔔石。'),
  ('Songshan Junji',['嵩山峻極禪師','峻極禪師'],'X/X80/X80n1565.xml','0054c09','僧良久。師曰。會麼僧。曰。不會。'),
  ('Raoshan',['饒州嶢山和尚','嶢山和尚'],'T/T51/T51n2076.xml','0286c22','長慶云。恁麼即請師領話。')]
 for name,aliases,rel,lb,kw in rows:
  if name not in known:d['candidates'].append({'canonicalName':name,'aliases':aliases,'evidence':[{'RelPath':rel,'FromLb':lb,'ToLb':lb,'Kwic':kw}],'reviewedBy':'Codex cohorts 1-3 146-150 literal full-case read','reviewReport':'cohorts-1-3-146-150-full-read-repair-ledger.json','status':'awaiting-roster-integration'})
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
def note(o):
 aa=o.get('ActorAttribution') or {};who=o.get('MasterName') or aa.get('ActorLabel') or 'no human actor';proof=aa.get('GrammarEvidence') or f'The complete source unit assigns the headword-bearing wording to {who}.'
 o['AttributionNote']=f"{zc.title(o['RelPath'])}. Exact headword actor: {who}. {proof}"
def main():
 add_pending();rows=[]
 for tid in IDS:
  p=E/tid/'entry.v2.json';d=json.loads(p.read_text());before=sha(p);term=d['SourceTerm'];o=d['Senses'][0]['Occurrences']
  if term=='竿頭進步':
   o[:]=[x for x in o if not x.get('Kwic','').startswith('百尺竿頭坐底人。雖然得入。')]
   o[2]['Kwic']='問如何是百尺竿頭進步底句';o[2]['ActorAttribution']=unnamed('monastic questioner','the unnamed monk','questioner','The marked question contains the headword; Yexian Guixing answers after 師云.');o[2]['ContextMasters']=[cm('Yexian Guixing','respondent','section-subject')]
   o[3]['Kwic']='僧參，問：「百尺竿頭進步時如何？」';o[3]['ActorAttribution']=unnamed('monastic questioner','the unnamed monk','questioner','The direct question contains the headword; Linye Qi’s answer begins after 師云.');o[3]['ContextMasters']=[cm('Linye Qi','respondent','section-subject')]
   o[4]['Kwic']='竿頭進步事如何';o[4]['ContextMasters']=[cm('Gulun','utterer','questioner'),cm('Shanduo Zhenzai','respondent','section-subject')]
   d['Senses'][0]['ClaimAnchors']=[
    {'ClaimText':'南贍部洲北欝單越','RelPath':'C/C077/C077n1710.xml','FromLb':'0790b17','ToLb':'0790b18','Kwic':'師云南贍部洲北欝單越問如何是西來意','MasterName':'Yexian Guixing','ContextMasters':[cm('Yexian Guixing','utterer','respondent','section-subject')],'AttributionNote':'古尊宿語錄. Yexian Guixing directly answers the pole-top-step question: 南贍部洲，北欝單越.'},
    {'ClaimText':'退後即得','RelPath':'J/J26/J26nB186.xml','FromLb':'0651a10','ToLb':'0651a10','Kwic':'師云：「退後即得。」','MasterName':'Linye Qi','ContextMasters':[cm('Linye Qi','utterer','respondent','section-subject')],'AttributionNote':'林野奇禪師語錄. Linye Qi directly answers the pole-top-step question: 退後即得.'},
    {'ClaimText':'斷頭船子下揚州','RelPath':'J/J38/J38nB414.xml','FromLb':'0420c06','ToLb':'0420c06','Kwic':'師曰：「斷頭船子下揚州。」','MasterName':'Shanduo Zhenzai','ContextMasters':[cm('Shanduo Zhenzai','utterer','respondent','section-subject')],'AttributionNote':'山鐸真在禪師語錄. Shanduo Zhenzai directly answers Gulun: 斷頭船子下揚州.'},
    {'ClaimText':'百尺竿頭坐底人。雖然得入。未為真。百尺竿頭須進步十方世界現全身。','RelPath':'T/T48/T48n2005.xml','FromLb':'0298c13','ToLb':'0298c14','Kwic':'百尺竿頭坐底人。雖然得入。未為真。百尺竿頭須進步十方世界現全身。','MasterName':'Changsha Jingcen','ContextMasters':[cm('Changsha Jingcen','utterer','verse-author','case-figure'),cm('Wumen Huikai','later-raiser','compiler')],'AttributionNote':'無門關. Wumen Huikai raises the verse attributed in Chan transmission to Changsha Jingcen.'}]
  elif term=='夏末':
   o[0]['Kwic']='問初秋夏末前程忽有人問如何秪對';o[0]['FromLb']=o[0]['ToLb']='0724c01';o[0]['ContextMasters']=[cm('Yunmen Wenyan','respondent','section-subject')]
   o[1]['ActorAttribution']=unnamed('quoted monastic questioner','the unnamed monk in the raised Yunmen case','questioner','復舉 introduces the old exchange; the headword occurs in the quoted monk’s question to Yunmen.')
   o[1]['ContextMasters']=[cm('Yunmen Wenyan','respondent','case-figure')]
   o[2]['MasterName']=None;o[2]['ActorAttribution']=unnamed('quoted predecessor','the unnamed old saying’s speaker','utterer','Yuelin Zhen says 古人道 and quotes the headword-bearing formula; the quoted predecessor is not named by any rung.');o[2]['ContextMasters']=[cm('Yuelin Zhen','later-raiser','section-subject')]
   o[4]['MasterName']='Dongshan Liangjie';o[4].pop('ActorAttribution',None);o[4]['ContextMasters']=[cm('Dongshan Liangjie','utterer','case-figure'),cm('Hongzhi Zhengjue','later-raiser','section-subject')]
   o[5]['MasterName']=None;o[5]['ActorAttribution']=unnamed('quoted monastic questioner','the unnamed monk in Yunmen’s case','questioner','Xutang raises Yunmen’s case; the stored headword belongs to the quoted monk’s question.');o[5]['ContextMasters']=[cm('Xutang Zhiyu','later-raiser','section-subject'),cm('Yunmen Wenyan','respondent','case-figure')]
   o[6]['ContextMasters']=[cm('Yangshan Huiji','person-described','questioner'),cm('Guishan Lingyou','respondent','case-figure')]
  elif term=='鎮州蘿蔔':
   for i,n in [(0,'Taiping An'),(1,'Shoushan Xingnian'),(2,'Shoushan Xingnian'),(4,'Dahui Zonggao'),(5,'Tianping Qiyu')]:o[i]['MasterName']=n;o[i].pop('ActorAttribution',None);o[i]['ContextMasters']=[cm(n,'utterer','section-subject')]
   o[3]['MasterName']=None;o[3]['ActorAttribution']=unnamed('capping-verse author','the unnamed author of the first 頌曰 verse','verse-author','頌曰 introduces the headword-bearing verse, but the complete anthology unit does not name its author.');o[3]['ContextMasters']=[cm('Zhaozhou Congshen','case-figure')]
  elif term=='領話':
   o[0]['Kwic']='曰：問阿誰？師曰：問長老。曰：何不領話？';o[0]['FromLb']=o[0]['ToLb']='0478a22';o[0]['ActorAttribution']=unnamed('elder monk','the unnamed elder monk','utterer','The elder’s 曰 turn contains 何不領話; Muzhou replies only afterward.');o[0]['ContextMasters']=[cm('Muzhou Daoming','respondent','section-subject')]
   o[1]['Kwic']='師云：「且喜領話。」';o[1]['FromLb']=o[1]['ToLb']='0527a23'
   o[2]['Kwic']='曰。問阿誰。師曰。問長老。曰。何不領話。';o[2]['FromLb']='0101b03';o[2]['ToLb']='0101b04';o[2]['ActorAttribution']=unnamed('elder monk','the unnamed elder monk','utterer','The elder’s 曰 turn contains 何不領話; Muzhou’s response follows.');o[2]['ContextMasters']=[cm('Muzhou Daoming','respondent','section-subject')]
   o[3]['Kwic']='師云謝闍梨領話';o[3]['FromLb']='0187b07';o[3]['ToLb']='0187b08';o[4]['Kwic']='師曰：且領話好。';o[4]['FromLb']=o[4]['ToLb']='0013b06'
   o[5]['ContextMasters']=[cm('Changqing Huileng','utterer','questioner'),cm('Raoshan','respondent','section-subject')]
  elif term=='良久':
   o[2]['Kwic']='問如何是佛師良久';o[2]['ContextMasters']=[cm('Shoushan Xingnian','person-described','respondent','section-subject')]
   o[5]['ContextMasters']=[cm('Songshan Junji','respondent','section-subject')]
   o[10]['ActorAttribution']={'Status':'impersonal','Kind':'elapsed silence','ActorLabel':'an interval in which nobody asks a question','ActorRole':'compiler','RungsChecked':RUNGS,'GrammarEvidence':'良久無人問 records elapsed silence and the absence of a question, not an utterance.','ReviewedBy':'Codex cohorts 1-3 146-150 literal full-case read','ReviewedUtc':now()};o[10]['ContextMasters']=[cm('Huanglong Huinan','section-subject')]
  for s in d['Senses']:
   for x in s['Occurrences']:
    for c in x.get('ContextMasters') or []:c['Roles']=['section-subject' if r=='record-owner' else r for r in c['Roles']]
    note(x)
   for a in s.get('ClaimAnchors') or []:note(a)
  p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');rows.append({'id':tid,'term':term,'beforeSha256':ORIGINAL[tid],'afterSha256':sha(p),'completeCasesRead':len(o)})
 (HERE/'cohorts-1-3-146-150-full-read-repair-ledger.json').write_text(json.dumps({'packetUnitsRead':37,'tierARead':1,'rows':rows},ensure_ascii=False,indent=2)+'\n')
if __name__=='__main__':main()
