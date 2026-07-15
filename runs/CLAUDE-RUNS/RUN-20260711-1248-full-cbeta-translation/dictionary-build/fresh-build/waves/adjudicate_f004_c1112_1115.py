#!/usr/bin/env python3
"""Durable human adjudication for f004 C1112–1115 after complete-context reading."""
import datetime,json,hashlib
from pathlib import Path
H=Path(__file__).resolve().parent
p=json.loads((H/'f004-laneC-1106-1150-research-checkpoint.json').read_text(encoding='utf-8'))
by={e['ordinal']:e for e in p['entries']}
dec={
1112:{'term':'草賊','preferredTarget':'petty bandit','aliases':['petty bandit','grass bandit','common outlaw','bandit verdict'],
 'opening':'A petty bandit is a cutting person-directed verdict in an exchange: Linji, Zhaozhou, and later speakers apply the outlaw label when a participant’s move is exposed or defeated.',
 'zenBend':'The ordinary outlaw becomes an encounter appraisal. “The bandit is badly defeated” can be spoken by a monk, a teacher, or Zhaozhou after inspecting a hermit; its force belongs to the completed exchange rather than to social biography.',
 'limit':'The label does not always identify the same participant or carry the same speaker. Three selected files transmit the same Nanquan–Zhaozhou hermit case and constitute one case family, not three independent events.',
 'differentThing':'one-thing','caseFamilies':{'linji-two-hall':'rows 1 and 3 are separate Linji exchanges','tiantong-danjiao':'row 2','nanquan-zhaozhou-hermit':'rows 4–6 are parallel transmissions'},
 'actors':[
  {'row':1,'actor':'unnamed monk','status':'reviewed-unnamed','role':'respondent','context':['Linji Yixuan']},
  {'row':2,'actor':'Tiantong Danjiao','status':'pending-roster','role':'utterer','context':[]},
  {'row':3,'actor':'Linji Yixuan','status':'roster','role':'utterer','context':[]},
  {'row':4,'actor':'Zhaozhou Congshen','status':'roster','role':'utterer','context':['Nanquan Puyuan']},
  {'row':5,'actor':'Zhaozhou Congshen','status':'roster','role':'utterer','context':['Nanquan Puyuan']},
  {'row':6,'actor':'Zhaozhou Congshen','status':'roster','role':'utterer','context':['Nanquan Puyuan']}]},
1113:{'term':'壇場','senses':[
 {'preferredTarget':'ordination platform','aliases':['ordination platform','precept platform','ordination ground'],
  'opening':'The ordination platform is the formal site whose relation to receiving the hard precept is itself questioned in Zen records.',
  'zenBend':'A prospective ordinand asks whether the platform, the formal acts, or the officiant is the precept; Daopei repeats the distinction in a public precept address. The platform remains a concrete ordination institution even when the master denies that it exhausts the precept.','rows':[2,4]},
 {'preferredTarget':'ritual altar ground','aliases':['ritual altar','altar precinct','ceremonial ground'],
  'opening':'The ritual altar ground is a prepared ceremonial precinct furnished for offerings, invocation, or commemoration.',
  'zenBend':'The monastic rule describes an imperial memorial altar; the liturgical text describes constructing and guarding a bounded altar precinct. These are concrete ceremonial sites, not ordination platforms.','rows':[1,3]}],
 'differentThing':'different-thing','reason':'The corpus distinguishes an ordination platform where precepts are conferred from an altar precinct constructed for memorial or invocatory rites.',
 'actors':[{'row':1,'actor':'monastic rule compiler','status':'impersonal','role':'compiler'},{'row':2,'actor':'unnamed ordinand','status':'identified-non-master','role':'questioner','context':['Baofeng Wen']},{'row':3,'actor':'liturgical authorial voice','status':'narrated','role':'compiler'},{'row':4,'actor':'Daopei Weilin','status':'pending-roster','role':'utterer'}]},
1114:{'term':'道中人','preferredTarget':'person of the Way','aliases':['person of the Way','one on the Way','wayfarer','person in the Way'],
 'opening':'A person of the Way is the person demanded in a recurring public question, with the answer supplied locally rather than by a fixed character portrait.',
 'zenBend':'The question receives sharply different answers—one who frowns all day, “what would you make of one?”, a dry dung-stick, or someone lying crosswise and upright. The person is defined by this interview deployment, not by harmonizing the replies.',
 'limit':'Two rows transmit the same Longxing exchange; the differing answers belong to different masters and must not be collapsed into a universal definition.',
 'differentThing':'one-thing','caseFamilies':{'longxing':'rows 1 and 3 parallel','xilin':'row 2','xingjiao-weiyi':'row 4','muzhou':'row 5','shoushan-line':'row 6'},
 'actors':[{'row':i,'actor':'unnamed monk','status':'reviewed-unnamed','role':'questioner','context':[]} for i in range(1,7)]},
1115:{'term':'無舌人','preferredTarget':'tongueless person','aliases':['tongueless person','one without a tongue','the tongueless speaker'],
 'opening':'The tongueless person is an impossible encounter figure who can speak without the organ of speech and is therefore posed as a problem to anyone relying on verbal facility.',
 'zenBend':'Tianyi asks what the tongueless person says after being struck by a handless person; Wansong opens a case by saying the tongueless person can speak; later masters use the figure to test shouting, “meaningless” speech, and the claim that all things speak.',
 'limit':'Rows 1 and 6 are parallel transmissions of Tianyi’s same chamber question. The image does not license a doctrine of silence or an anatomical claim.',
 'differentThing':'one-thing','caseFamilies':{'tianyi-handless':'rows 1 and 6 parallel','wansong-case-opening':'row 2','shiyu-address':'row 3','linji-commentary':'row 4','baiyu-address':'row 5'},
 'actors':[{'row':1,'actor':'Tianyi Yihuai','status':'roster','role':'utterer'},{'row':2,'actor':'Wansong Xingxiu','status':'roster','role':'utterer'},{'row':3,'actor':'Shiyu Mingfang','status':'roster','role':'utterer'},{'row':4,'actor':'Fengxin','status':'pending-roster','role':'utterer','context':['Linji Yixuan']},{'row':5,'actor':'Baiyu Si','status':'roster','role':'utterer'},{'row':6,'actor':'Tianyi Yihuai','status':'roster','role':'utterer'}]}}
for n,d in dec.items():
 rows=by[n]['rows']; d['sourceRows']=[]
 for i,r in enumerate(rows,1):
  d['sourceRows'].append({'row':i,'workId':r['workId'],'RelPath':r['RelPath'],'FromLb':r['FromLb'],'ToLb':r['ToLb'],'Kwic':r['Kwic'],'zcVerified':r['zcVerified'],'contextSha256':hashlib.sha256(r['completeContext']['window'].encode()).hexdigest()})
out={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f004','lane':'C','ordinals':[1112,1115],
 'productionOrder':'complete context -> exact actor -> case family -> different-things test -> opening/limit','entries':[dec[n] for n in sorted(dec)],
 'allRowsRead':True,'allKwicsVerified':all(r['zcVerified'] for n in dec for r in by[n]['rows']),'compiled':False,'promotion':False,'merge':False,'siteTouched':False}
(H/'f004-laneC-1112-1115-adjudication.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
pending={'schemaVersion':1,'wave':'f004','lane':'C','candidates':[{'MasterName':'Tiantong Danjiao','ChineseName':'天童澹交','evidence':'X/X82/X82n1571.xml section 明州天童澹交禪師'},{'MasterName':'Baofeng Wen','ChineseName':'寶峯文','evidence':'T/T51/T51n2077.xml section 寶峯文禪師法嗣'},{'MasterName':'Daopei Weilin','ChineseName':'為霖道霈','evidence':'X/X72/X72n1439.xml title 為霖道霈禪師餐香錄'},{'MasterName':'Fengxin','ChineseName':'風信','evidence':'X/X66/X66n1297.xml exact commentary label 語風信云'}]}
(H/'f004-laneC-1112-1115-pending-roster-candidates.json').write_text(json.dumps(pending,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'entries':4,'rows':sum(len(by[n]['rows']) for n in dec),'hardPass':out['allRowsRead'] and out['allKwicsVerified']},ensure_ascii=False))
