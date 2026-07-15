#!/usr/bin/env python3
import json,pathlib
B=pathlib.Path(__file__).resolve().parents[2];p=B/'fresh-build/semantic-regressions.json';d=json.loads(p.read_text(encoding='utf8'))
spec={
't_d1aa91b2b347':('刻舟求劍',[('J/J33/J33nB294.xml','0739a28','Langting Jingting',None)]),'t_a9babbddf1a8':('效顰',[('J/J27/J27nB198.xml','0464a30','Xueguan Zhiyin',None),('J/J38/J38nB425.xml','0664a08','Jifei Ruyi',None)]),'t_8f91a9f06c79':('繫驢橛',[('X/X79/X79n1557.xml','0083c10','Linji Yixuan',None)]),'t_d27432779ce8':('水月',[('X/X69/X69n1356.xml','0372a16','Puan Yinsu',None)]),'t_e1b4c379b919':('紅爐點雪',[('J/J27/J27nB198.xml','0511c19','Xueguan Zhiyin',None)]),'t_612a2e5cbf5d':('無孔笛',[('X/X66/X66n1297.xml','0280c22','Bajiao Huiche',None)]),'t_c86b1e91c7b5':('陽焰',[('J/J26/J26nB188.xml','0777a22','Ruibai Mingxue',None)]),'t_e1eda88159c6':('優曇華',[('B/B25/B25n0145.xml','0754a16','Zhongfeng Mingben',None)]),'t_40bcab45a004':('枯木生花',[('J/J33/J33nB294.xml','0722b15','Langting Jingting',None)]),'t_35cd0cccddc7':('摩尼珠',[('C/C077/C077n1710.xml','0706c05',None,'reviewed-unnamed')]),'t_b15eaab0dc3c':('無絃琴',[('X/X71/X71n1414.xml','0352a19',"Lia'an Qingyu",None)]),'t_a6754d726742':('定盤星',[('C/C077/C077n1710.xml','0670a12','Baoen Xuanze',None)]),'t_9cd83a160990':('飯袋子',[('M/M59/M59n1540.xml','0840b14','Yunmen Wenyan',None)]),'t_bd6a1e9054a5':('白拈賊',[('X/X66/X66n1296.xml','0041b15','Gaofeng Yuanmiao',None)]),'t_3600c4babcdf':('秤錘',[('L/L158/L158n1652.xml','0029a06','Mingjue Cong',None)]),'t_2f6dd23d26e9':('栗棘蓬',[('X/X82/X82n1571.xml','0109b15',None,'reviewed-unnamed')]),'t_cff94bb09481':('蒲團',[('B/B25/B25n0145.xml','0696a07','Zhongfeng Mingben',None),('J/J27/J27nB198.xml','0444b16','Xueguan Zhiyin',None)]),'t_a66ef543d2ea':('善財',[('M/M59/M59n1540.xml','0802b17','Dahui Zonggao',None),('X/X64/X64n1260.xml','0052a09',None,'reviewed-unnamed')]),'t_3efd163c8697':('龍女',[('J/J33/J33nB294.xml','0742b18','Langting Jingting',None)]),'t_37cd9bfc3e67':('焚香',[('X/X83/X83n1578.xml','0423a18',None,'identified-non-master'),('X/X80/X80n1568.xml','0591a11',None,'narrated')]),'t_0f4c2ed08d86':('一炷香',[('X/X64/X64n1260.xml','0030a11','Shanji Jichan',None),('J/J26/J26nB183.xml','0493c16','Shiqi Tongyun',None)]),'t_d801848213ab':('發願',[('X/X72/X72n1435.xml','0318a02','Wuyi Yuanlai',None)]),'t_e89833bb5e63':('誓願',[('X/X68/X68n1318.xml','0401c11','Baiyun Shouduan',None)]),'t_ee77766b424b':('安心斷臂',[('J/J29/J29nB223.xml','0068c24','Shanhui',None),('J/J29/J29nB224.xml','0143b12','Yezhu Fusheng',None),('J/J35/J35nB343.xml','0856a29','Miyin',None)]),'t_376913189794':('心如牆壁',[('J/J27/J27nB197.xml','0424c12','Bodhidharma',None),('T/T47/T47n1998A.xml','0925b18','Bodhidharma',None),('B/B25/B25n0145.xml','0792a14','Bodhidharma',None),('J/J29/J29nB239.xml','0543b02','Bodhidharma',None)])}
spec['t_0f4c2ed08d86']=('一炷香',[('X/X64/X64n1260.xml','0030a11','Shanci Ji',None),('J/J26/J26nB183.xml','0493c16','Shiqi Tongyun',None)])
for id,(term,rows) in spec.items():
 q=d.setdefault(id,{});q['term']=term;a=q.setdefault('occurrenceAssertions',[]);keys={(x.get('RelPath'),x.get('FromLb')) for x in a}
 for rel,lb,mn,status in rows:
  x={'RelPath':rel,'FromLb':lb}
  if mn:x['mustMasterName']=mn
  if status:x['mustActorStatus']=status
  if (rel,lb) not in keys:a.append(x)
  else:
   for old in a:
    if (old.get('RelPath'),old.get('FromLb'))==(rel,lb):old.update(x)
d.setdefault('t_18a76480bf9',{'term':'空華'})['forbiddenEntrySubstrings']=['flowers in empty spacethe moon in water']
q=d.setdefault('t_a66ef543d2ea',{'term':'善財'});q.setdefault('forbiddenOccurrenceSubstrings',[])
for x in ['宗鑑法林目錄','指月錄總目']:
 if x not in q['forbiddenOccurrenceSubstrings']:q['forbiddenOccurrenceSubstrings'].append(x)
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(json.dumps({'updated':len(spec)+1},ensure_ascii=False))
