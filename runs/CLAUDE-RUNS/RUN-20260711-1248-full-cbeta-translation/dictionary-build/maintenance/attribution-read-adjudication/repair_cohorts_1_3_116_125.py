#!/usr/bin/env python3
import hashlib,json,sys
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; F=ROOT/'fresh-build'/'entries'; OUT=Path(__file__).with_name('cohorts-1-3-116-125-full-read-repair-ledger.json')
sys.path.insert(0,str(ROOT)); import zc
IDS=['t_171059a90935','t_1e41b014d80e','t_21926ca0b92e','t_22865f34533e','t_23d23141b4c6','t_24adbdf51a15','t_27c71a091873','t_2816f418822c','t_2b6fde4fdf58','t_2b9a5ab567cc']
NOW=lambda:datetime.now(timezone.utc).isoformat().replace('+00:00','Z')
ROLES={'record-owner':'section-subject','case-teacher':'teacher','case-source':'case-figure','quoted-utterer':'utterer','action-performer':'person-described','hall-speaker':'section-subject'}
RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def cm(n,*r):return {'MasterName':n,'Roles':list(dict.fromkeys(r))}
def unnamed(kind,label,role,evidence):return {'Status':'reviewed-unnamed','Kind':kind,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':evidence,'ReviewedBy':'Codex cohorts 1-3 116-125 literal full-unit read','ReviewedUtc':NOW()}
def narrated(label,evidence):return {'Status':'narrated','Kind':'record narration','ActorLabel':label,'ActorRole':'compiler','RungsChecked':RUNGS,'GrammarEvidence':evidence,'ReviewedBy':'Codex cohorts 1-3 116-125 literal full-unit read','ReviewedUtc':NOW()}
def identified(kind,label,role,evidence):return {'Status':'identified-non-master','Kind':kind,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':evidence,'ReviewedBy':'Codex cohorts 1-3 116-125 literal full-unit read','ReviewedUtc':NOW()}
def pending():
 p=ROOT/'fresh-build'/'pending-roster.json';d=json.loads(p.read_text());known={x['canonicalName'] for x in d['candidates']}
 rows=[
 ('Helin Xuansu',['鶴林玄素','玄素'],'T/T51/T51n2076.xml','0229c04','師曰。會即不會。疑即不疑。'),
 ('Yunfeng Zhixuan',['潭州雲峰志璿祖燈禪師','雲峰志璿'],'X/X82/X82n1571.xml','0024b07','僧問：如何是西來意？師曰：築著額頭磕著鼻。'),
 ('Chengtian Chuanzong',['泉州承天傳宗禪師','承天傳宗'],'X/X82/X82n1571.xml','0002b03','僧便喝，師曰：臨濟兒孫。'),
 ('Ruixiang Zilai',['瑞州瑞相子來禪師','瑞相子來'],'X/X82/X82n1571.xml','0019c22','若也棒頭取證，喝下承當。'),
 ('Damei Ying',['大梅英禪師','大梅英'],'X/X64/X64n1260.xml','0019a20','達磨擕將一隻歸，兒孫從此赤脚走。'),
 ('Prajnatara',['般若多羅','西天二十七祖般若多羅'],'C/C077/C077n1710.xml','0615a23','震旦雖闊無別路要假兒孫脚下行')]
 for name,aliases,rel,lb,kw in rows:
  if name not in known:d['candidates'].append({'canonicalName':name,'aliases':aliases,'evidence':[{'RelPath':rel,'FromLb':lb,'ToLb':lb,'Kwic':kw}],'reviewedBy':'Codex cohorts 1-3 116-125 literal full-unit read','reviewReport':'cohorts-1-3-116-125-full-read-repair-ledger.json','status':'awaiting-roster-integration'})
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
def normalize(d):
 for s in d['Senses']:
  for o in s['Occurrences']:
   aa=o.get('ActorAttribution') or {}
   if aa.get('ActorRole')=='narrator':aa['ActorRole']='compiler'
   for c in o.get('ContextMasters') or []:c['Roles']=list(dict.fromkeys(ROLES.get(r,r) for r in c.get('Roles',[])))
   who=o.get('MasterName') or (o.get('ActorAttribution') or {}).get('ActorLabel') or 'no human actor'
   proof=(o.get('ActorAttribution') or {}).get('GrammarEvidence') or 'The complete source unit assigns the headword-bearing wording to this named utterer.'
   o['AttributionNote']=f"{zc.title(o['RelPath'])}. Exact headword actor: {who}. {proof}"
def main():
 pending();rows=[]
 for tid in IDS:
  p=F/tid/'entry.v2.json';d=json.loads(p.read_text());before=sha(p);term=d['SourceTerm'];o=d['Senses'][0]['Occurrences']
  if term=='西來意':
   for i,n in {0:'Tianzhu Chonghui',1:'Helin Xuansu',2:'Yunfeng Zhixuan',3:'Mazu Daoyi',6:'Zhaozhou Congshen',7:'Shitou Xiqian'}.items():o[i]['ContextMasters']=[cm(n,'respondent','section-subject')]
   o[3]['Kwic']='問如何是西來意';o[3]['FromLb']='0616c08';o[3]['ToLb']='0616c09'
  elif term=='向上一路':
   o[5]['Kwic']='向上一路還許學人會也無';o[5]['FromLb']=o[5]['ToLb']='0013b09'
  elif term=='頂門眼':
   o[4]['Kwic']='進云：「恁麼則豁開頂門眼。」';o[4]['FromLb']=o[4]['ToLb']='0225a05'
   d['Senses'][0]['ClaimAnchors']=[{
    'ClaimText':'擉瞎了也','RelPath':'J/J34/J34nB300.xml','FromLb':'0225a06','ToLb':'0225a06','Kwic':'師打，云：「擉瞎了也。」',
    'MasterName':'Chaozong Tongren','ContextMasters':[cm('Chaozong Tongren','utterer','respondent','section-subject')],
    'AttributionNote':'朝宗禪師語錄. Chaozong Tongren utters 擉瞎了也 as his answer to the monk’s 頂門眼 remark.'}]
  elif term=='兒孫':
   o[0]['MasterName']='Chengtian Chuanzong';o[0]['ContextMasters']=[cm('Chengtian Chuanzong','utterer','section-subject')]
   o[1]['MasterName']='Tianzhang Yuanchu';o[1].pop('ActorAttribution',None);o[1]['ContextMasters']=[cm('Tianzhang Yuanchu','utterer')]
   o[2]['MasterName']=None;o[2]['ActorAttribution']=unnamed('verse author','an unnamed capping-verse author','verse-author','The anthology passage is a run of capping verses; all six rungs fail to identify the author of the verse containing 兒孫.')
   o[3]['MasterName']='Prajnatara';o[3].pop('ActorAttribution',None);o[3]['ContextMasters']=[cm('Prajnatara','utterer','case-figure'),cm('Huineng','later-quoter'),cm('Nanyue Huairang','section-subject')]
   o[4]['MasterName']=None;o[4]['ActorAttribution']=identified('preface author','Jingfu (淨符), author of the first 宗門拈古彙集序','compiler','The signed preface itself contains 脚下兒孫; this is Jingfu’s prose, not anonymous compilation narration.')
   o[5]['MasterName']="Zhe'an Jingfan";o[5].pop('ActorAttribution',None);o[5]['ContextMasters']=[cm("Zhe'an Jingfan",'utterer','verse-author','section-subject')]
   o[7]['MasterName']='Damei Ying';o[7].pop('ActorAttribution',None);o[7]['ContextMasters']=[cm('Damei Ying','utterer','section-subject')]
  elif term=='棒頭':
   o[0]['MasterName']='Ruixiang Zilai';o[0].pop('ActorAttribution',None);o[0]['ContextMasters']=[cm('Ruixiang Zilai','utterer','section-subject')]
   o[1]['MasterName']='Miyun Yuanwu';o[1].pop('ActorAttribution',None);o[1]['ContextMasters']=[cm('Miyun Yuanwu','utterer','section-subject')]
   o[2]['MasterName']=None;o[2]['ActorAttribution']=unnamed('verse author','the unnamed capping-verse author after the Guizong case','verse-author','The complete unit places the line as an uncredited verse after the Guizong case; six rungs do not identify its author.')
   o[3]['MasterName']=None;o[3]['ActorAttribution']=unnamed('verse author','the unnamed author of the 頌曰 sequence','verse-author','頌曰 governs the headword-bearing verse, but all six rungs leave its author unnamed.')
   o[4]['MasterName']=None;o[4]['ActorAttribution']=identified('lay preface author','Cai Lianbi','verse-author','The full preface closes with 菩薩戒弟子蔡聯璧拜序, identifying Cai Lianbi as author of the sentence containing 棒頭.')
   o[5]['MasterName']=None;o[5]['ActorAttribution']=identified('lay questioner','Zhenli layman (𨍏轢居士)','questioner','The complete hall case explicitly introduces 𨍏轢居士問 before the headword-bearing question.')
   o[6]['MasterName']='Miyun Yuanwu';o[6].pop('ActorAttribution',None);o[6]['ContextMasters']=[cm('Miyun Yuanwu','utterer','section-subject')]
  elif term=='首座':
   o[0]['MasterName']=None;o[0]['ActorAttribution']=narrated('the record biographer','The record narrator says Fachang received the invitation from the two head-seat monks; Fachang’s quoted farewell begins after 相別曰.');o[0]['ContextMasters']=[cm('Fachang Yiyu','section-subject')]
   o[3]['MasterName']=None;o[3]['ActorAttribution']=unnamed('monastic questioner','the unnamed monk asking Mazu','questioner','問 governs the statement that Shenxiu was head seat; Mazu’s 師云 answer begins afterward.');o[3]['ContextMasters']=[cm('Mazu Daoyi','respondent','section-subject'),cm('Shenxiu','person-discussed')]
   o[3]['Kwic']='問六祖不會經書何得傳衣為祖秀上座是五百人首座為教授師講得三十二本經論云何不傳衣';o[3]['FromLb']='0631b08';o[3]['ToLb']='0631b10'
  elif term=='念佛是誰':
   o[0]['MasterName']=None;o[0]['ActorAttribution']=narrated('the biographer','The biographer reports that Fazhou gave Yungu the 念佛是誰 saying; the label is not preserved as a quoted turn.');o[0]['ContextMasters']=[cm('Fazhou Daoji','teacher'),cm('Yungu Fahui','student','section-subject')]
   o[1]['Kwic']='呈偈曰：「念佛是誰誰念佛？一聲高了一聲低，高低聲罷歸何所？石女雲中駕鷺鷥。」';o[1]['FromLb']='0352a13';o[1]['ToLb']='0352a14'
   o[4]['Kwic']='曰：「念佛是誰？」';o[4]['FromLb']=o[4]['ToLb']='0634a22'
   o.append({'RelPath':'J/J39/J39nB445.xml','FromLb':'0352a12','ToLb':'0352a12','Kwic':'命參念佛是誰','Curated':True,
    'MasterName':None,'ActorAttribution':narrated('the biographer','The biography reports Gaofeng Yuanmiao commanding Daxiao Xingchong to investigate 念佛是誰; the wording is narration, not a quoted utterance.'),
    'ContextMasters':[cm('Gaofeng Yuanmiao','teacher'),cm('Daxiao Xingchong','student','section-subject')]})
   o.append({'RelPath':'J/J34/J34nB311.xml','FromLb':'0634a22','ToLb':'0634a22','Kwic':'師云：「不念佛是誰？」','Curated':True,
    'MasterName':'Juelang Daosheng','ContextMasters':[cm('Juelang Daosheng','utterer','respondent','section-subject')]})
   d['Senses'][0]['ClaimAnchors']=[]
  elif term=='金佛不度爐':
   o[4]['Kwic']='一日示眾卻云金佛不度爐木佛不度火泥佛不度水';o[4]['FromLb']='0570b18';o[4]['ToLb']='0570b19'
  elif term=='壁立千仞':
   o[1]['Kwic']='有僧問赤肉團上壁立千仞豈不是和尚語';o[1]['FromLb']='0662a06';o[1]['ToLb']='0662a07'
  elif term=='虛空粉碎':
   o[1]['MasterName']='Guxue Zhe';o[1].pop('ActorAttribution',None);o[1]['ContextMasters']=[cm('Guxue Zhe','utterer','respondent','section-subject')]
  normalize(d);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');rows.append({'entryId':tid,'term':term,'beforeSha256':before,'afterSha256':sha(p),'completeUnitsRead':len(o)})
 OUT.write_text(json.dumps({'selfReview':False,'packetUnitsRead':69,'tierARead':1,'rows':rows},ensure_ascii=False,indent=2)+'\n')
if __name__=='__main__':main()
