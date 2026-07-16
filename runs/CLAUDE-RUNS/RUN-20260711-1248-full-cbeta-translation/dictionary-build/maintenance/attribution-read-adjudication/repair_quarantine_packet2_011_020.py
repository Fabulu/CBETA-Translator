import json
from pathlib import Path
R=Path(__file__).resolve().parents[2]
IDS={"出身處":"t_447ad9648add","見聞覺知":"t_4c9320095ba1","孤明":"t_560356022866","寶鏡三昧":"t_5db4dbd2bc17","嗣法":"t_7ccccfa5fe9a","老婆心切":"t_b23e58454acd","芥子":"t_df9aad1ce22d","掛搭":"t_f0cb4dcfc70c","歸方丈":"t_15eac1a3b037","劫外":"t_17c1d8b4f105"}
def ld(t):
 p=R/'fresh-build/entries'/IDS[t]/'entry.v2.json';return p,json.loads(p.read_text())
def o(d,n):return d['Senses'][0]['Occurrences'][n-1]
def add(x,n,*rs):
 a=next((z for z in x.setdefault('ContextMasters',[]) if z['MasterName']==n),None)
 if not a:a={'MasterName':n,'Roles':[]};x['ContextMasters'].append(a)
 for r in rs:
  if r not in a['Roles']:a['Roles'].append(r)
def roles(x,n,*rs):next(z for z in x['ContextMasters'] if z['MasterName']==n)['Roles']=list(rs)
def sv(p,d):p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
p,d=ld('出身處');add(o(d,4),'Yunmen Wenyan','case-figure');sv(p,d)
p,d=ld('見聞覺知');add(o(d,5),'Mazu Daoyi','teacher');sv(p,d)
p,d=ld('孤明');add(o(d,3),'Yangshan Huiji','case-figure');sv(p,d)
p,d=ld('寶鏡三昧');add(o(d,2),'Caoshan Benji','person-discussed');add(o(d,2),'Dongshan Liangjie','person-discussed');sv(p,d)
p,d=ld('嗣法')
x=o(d,2);x['ActorAttribution']['ActorRole']='compiler';roles(x,'Weilin Daopei','compiler','student');roles(x,'Yongjue Yuanxian','teacher','section-subject');x['AttributionNote']='Extensive Record of Chan Master Yongjue Yuanxian (永覺元賢禪師廣錄): the signed preface author Weilin Daopei identifies himself as Yuanxian’s lineage-inheriting disciple.'
x=o(d,4);x['ActorAttribution']['ActorRole']='compiler';roles(x,'Wufeng Ruxue','person-described','student');roles(x,'Miyun Yuanwu','teacher');x['AttributionNote']='Recorded Sayings of Chan Master Dawei Wufeng Xue (大溈五峰學禪師語錄): pagoda-inscription author Tao Runai records that Wufeng Ruxue inherited transmission from Miyun Yuanwu.'
add(o(d,5),'Xuedou Chongxian','teacher')
x=o(d,7);roles(x,'Touzi Yiqing','person-discussed','student');roles(x,'Dayang Jingxuan','teacher');roles(x,'Fushan Fayuan','case-figure');roles(x,'Muzhou Daoming','teacher');roles(x,'Xuefeng Yicun','case-figure');x['AttributionNote']='Book of Serenity (萬松老人評唱天童覺和尚頌古從容庵錄): Wansong Xingxiu’s 示眾 voice compares Yunmen Wenyan’s and Touzi Yiqing’s lineage reception.'
sv(p,d)
p,d=ld('老婆心切');roles(o(d,1),next(z['MasterName'] for z in o(d,1)['ContextMasters'] if 'person-appraised' in z['Roles']),'person-discussed');roles(o(d,3),next(z['MasterName'] for z in o(d,3)['ContextMasters'] if 'later-teacher' in z['Roles']),'teacher');sv(p,d)
p,d=ld('芥子');o(d,1)['ActorAttribution']['ActorLabel']='Li Bo';add(o(d,5),'Yongzheng Emperor','respondent');sv(p,d)
p,d=ld('掛搭');add(o(d,5),'Fushan Fayuan','addressee','student');add(o(d,5),'Tianyi Yihuai','addressee','student');sv(p,d)
p,d=ld('劫外');x=o(d,6);x['ActorAttribution']['Kind']='person';x['ActorAttribution']['ActorLabel']='an unnamed verse author';roles(x,'Hongren','person-discussed','case-figure');roles(x,'Daoxin','teacher','case-figure');x['AttributionNote']='Mirror of the Lineage (宗鑑法林): an unnamed verse author calls the returned Hongren a spiritual sprout outside the kalpa; Daoxin is the teacher in the transmission story.';sv(p,d)
