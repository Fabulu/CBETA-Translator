import json
from pathlib import Path
R=Path(__file__).resolve().parents[2]
IDS={"金翅鳥":"t_12fe3700913c","拾得":"t_4dd50050b279","認奴作郎":"t_a5408be46291","邯鄲":"t_eba970114dd2","坐斷天下人舌頭":"t_74390b40f658","主中主":"t_f266d9e034ea","韓愈":"t_1b4bf4fff6bb","趙州橋":"t_4f3cd3b1c155","東坡居士":"t_efc6a42814ee","三文買草鞋":"t_495c83ba370b"}
def ld(t):p=R/'fresh-build/entries'/IDS[t]/'entry.v2.json';return p,json.loads(p.read_text())
def o(d,n):return d['Senses'][0]['Occurrences'][n-1]
def add(x,n,*rs):
 a=next((z for z in x.setdefault('ContextMasters',[]) if z['MasterName']==n),None)
 if not a:a={'MasterName':n,'Roles':[]};x['ContextMasters'].append(a)
 for r in rs:
  if r not in a['Roles']:a['Roles'].append(r)
def sv(p,d):p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
p,d=ld('金翅鳥');add(o(d,2),'Xuefeng Yicun','person-discussed');sv(p,d)
p,d=ld('拾得');o(d,4)['AttributionNote']='Recorded Sayings of Chan Master Baiyu (百愚禪師語錄), image praise headed Hanshan and Shide (寒山拾得): Baiyu Si is the exact authorial speaker who depicts the dishevelled pair with a broom over the shoulder.';sv(p,d)
p,d=ld('認奴作郎');add(o(d,4),'Deshan Xuanjian','case-figure');add(o(d,4),'Longtan Chongxin','case-figure');sv(p,d)
p,d=ld('邯鄲');add(o(d,1),'Xuedou Chongxian','later-raiser','commentator');add(o(d,1),'Zhaozhou Congshen','case-figure');sv(p,d)
p,d=ld('坐斷天下人舌頭');o(d,1)['AttributionNote']='Recorded Sayings of Chan Master Yuanwu Foguo (圓悟佛果禪師語錄): Yuanwu Keqin utters the standalone line; a much later attribution is chronologically impossible.';add(o(d,6),'Bodhidharma','case-figure');sv(p,d)
p,d=ld('韓愈');add(o(d,3),'Muchen Daomin','addressee','interlocutor');sv(p,d)
p,d=ld('趙州橋');add(o(d,5),'Wuzu Fayan','respondent');sv(p,d)
p,d=ld('三文買草鞋');add(o(d,4),'Dongshan Shouchu','case-figure');sv(p,d)
