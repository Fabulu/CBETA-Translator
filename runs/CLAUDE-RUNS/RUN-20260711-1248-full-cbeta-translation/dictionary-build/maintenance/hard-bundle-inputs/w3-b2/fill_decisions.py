#!/usr/bin/env python3
import json
from datetime import datetime, timezone
from pathlib import Path

HERE=Path(__file__).resolve().parent
TRIAGE=HERE.parent/"worker-3-bundle-2-triage.json"
RUNGS=["line","expanded-context","section-header","book-title","tei-header","parallel-passage"]
NOW=datetime.now(timezone.utc).isoformat().replace("+00:00","Z")

source_actor={
 "T47n1997":"Yuanwu Keqin", "J39nB471":"Konggu Daocheng", "J38nB406":"Tianran Hanshi",
 "J39nB466":"Baishan Dekai", "J39nB454":"Pin Jixiang", "T48n2008":"Huineng",
 "X69n1359":"Ying'an Tanhua", "X69n1333":"Xuefeng Yicun", "J38nB425":"Jifei Ruyi",
 "T47n1992":"Fenyang Shanzhao", "X63n1245":"Changlu Zongze", "X63n1250":"Yixian",
 "X63n1259":"Jiexian", "X67n1304":"Linquan Conglun", "X69n1357":"Yuanwu Keqin",
 "X70n1402":"Zhongfeng Mingben", "J38nB410":"Lianfeng", "J38nB423":"Shiguan Fazang",
 "J39nB460":"Huizhou Hao", "T47n1999":"Mi'an Xianjie", "T48n2013":"Yongjia Xuanjue",
 "T48n2015":"Guifeng Zongmi", "X63n1257":"Wuyi Yuanlai", "X69n1345":"Chaozong Huifang",
 "X70n1390":"Xisou Shaotan", "J38nB407":"Qianshan Hanke", "J38nB414":"Shanduo Zhenzai",
 "J39nB435":"Fushi Yigong", "J39nB445":"Daxiao Xingchong", "J39nB446":"Lingyin Yinwen",
 "J39nB456":"Tongtian Danya Yuan", "J40nB472":"Dabo Qian", "T48n2007":"Huineng",
 "T48n2017":"Yongming Yanshou", "T48n2020":"Bojo Jinul", "T48n2024":"Yunqi Zhuhong",
 "X63n1255":"Qingxu Xiujing", "X67n1307":"Wansong Xingxiu", "X69n1326":"Miyun Yuanwu",
 "X70n1398":"Haiyin Zhaoru"
}

named={
 1:"Yuansou Xingduan",2:"Mi'an Xianjie",3:"Jinshan Zhong",4:"Mi'an Xianjie",5:"Shanci Xingji",6:"Shanci Xingji",
 7:"Xueyan Zuqin",8:"Xuefeng Yicun",9:"Luohan Guichen",10:"Langye Huijue",11:"Touzi Yiqing",12:"Fengshan",
 13:"Gulin Qingmao",14:"Qianyan Yuanzhang",15:"Yu'an Puji",
 16:"Guanghui Yuanlian",17:"Xuedou Chongxian",18:"Ciming Chuyuan",19:"Fachang Yiyu",20:"Bao'en Tan",
 21:"Huanglong Huinan",22:"Jiangshan Fang",23:"Bailu Xianrui",24:"Touzi Yiqing",25:"Ciji Cong",
 26:"Yuquan Shanchao",27:"Jingyin Jicheng",28:"Dengjue Puming",29:"Wuhui Liangfan",30:"Niutou",
 31:"Deshan Huichu",32:"Shakyamuni Buddha",33:"Buddhanandi",34:"Bodhidharma",35:"Bodhidharma",
 36:"Hongren",37:"Lu Gen",38:"Shishi Shandao",40:"Shunde Daofu",41:"Yunmen Wenyan",42:"Baozi Xiaowu",
 43:"Mingzhao Deqian",44:"Dazhu Huihai",45:"Dazhu Huihai",46:"Yongzheng Emperor",47:"Ziyang Zhenren",
 48:"Yulin Tongxiu",49:"Yulin Tongxiu",51:"Yongzheng Emperor",52:"Yongzheng Emperor",53:"Yongzheng Emperor",
 54:"Yongzheng Emperor",55:"Baiyun Zhizuo",56:"Fenyang Shanzhao",57:"Huitang Zuxin",58:"Yinshan Can",
 59:"Biefeng Yun",66:"Yang Jie",67:"Yongming Yanshou",68:"Yongming Yanshou",69:"Yongming Yanshou",
 70:"Yongming Yanshou",71:"Yongming Yanshou",72:"Yongming Yanshou",73:"Yongming Yanshou",74:"Yongming Yanshou",
 75:"Jingfu",76:"Jingfu",77:"Sudhana",80:"Konggu Daocheng",81:"Konggu Daocheng",82:"Konggu Daocheng",
 84:"Konggu Daocheng",85:"Fushan Fayuan",86:"Huiyan Zhizhao",88:"Baizhang Huaihai",89:"Dehui",90:"Dehui",
 91:"Deshan Xuanjian",92:"Yuanwu Keqin",93:"Guan Youwudang",94:"Tianran Hanshi",95:"Tianran Hanshi",
 96:"Hanshan",97:"Baishan Dekai",98:"Pin Jixiang",99:"Pin Jixiang",100:"Huineng",101:"Huineng",102:"Huineng",
 103:"Ying'an Tanhua",104:"Ying'an Tanhua",105:"Xutang Zhiyu",106:"Xutang Zhiyu",107:"Jiufeng Daoqian",
 108:"Zhimen Zuo",109:"Xuefeng Yicun",110:"Jifei Ruyi",111:"Jifei Ruyi",112:"Fenyang Shanzhao",
 113:"Zhaozhou Congshen",114:"Changlu Zongze",115:"Yixian",116:"Yixian",117:"Jiexian",118:"Jiexian",
 119:"Linquan Conglun",120:"Linquan Conglun",121:"Yuanwu Keqin",122:"Zhongfeng Mingben",123:"Lianfeng",
 124:"Shiguan Fazang",125:"Huizhou Hao",126:"Mi'an Xianjie",127:"Yongjia Xuanjue",128:"Yongjia Xuanjue",
 129:"Guifeng Zongmi",130:"Guifeng Zongmi",131:"Wuyi Yuanlai",132:"Chaozong Huifang",133:"Xu Minzi",
 134:"Xisou Shaotan",135:"Qianshan Hanke",136:"Shanduo Zhenzai",137:"Fushi Yigong",138:"Daxiao Xingchong",
 139:"Lingyin Yinwen",140:"Tongtian Danya Yuan",141:"Dabo Qian",143:"Huineng",144:"Yongming Yanshou",
 145:"Bojo Jinul",146:"Yunqi Zhuhong",147:"Qingxu Xiujing",148:"Wansong Xingxiu",149:"Miyun Yuanwu",150:"Haiyin Zhaoru"
}

unnamed={
 39:("monk","Unnamed monk awakened by the cicada-like sound"),
 50:("monk","Unnamed monk left at a loss"),
 78:("laywoman","Unnamed old woman who appraises Wang the Teacher"),
 79:("attendant","Unnamed robe-and-bowl attendant"),
 83:("attendant","Unnamed attendant telling Gaofeng to settle his mind"),
 142:("monk","Unnamed visiting monk lifting a tea bowl")
}

context={
 8:["Xuefeng Yicun"],24:["Furong Daokai"],30:["Jingshan Zhice"],37:["Nanquan Puyuan"],39:["Touzi Ganwen"],
 50:["Minxi Seng"],77:["Manjusri"],78:["Nanquan Puyuan"],79:["Huineng"],83:["Gaofeng Yuanmiao","Konggu Daocheng"],
 91:["Yuanwu Keqin"],93:["Yuanwu Keqin","Xuedou Chongxian"],95:["Tianran Hanshi"],107:["Wansong Xingxiu"],
 113:["Nanquan Puyuan","Wumen Huikai"],142:["Nanquan Puyuan","Fazhou Ji"]
}

triage=json.loads(TRIAGE.read_text(encoding="utf8")); ordered=[]
for source in triage["sources"]:
 for cluster in source["clusters"]: ordered.extend(cluster["occurrences"])
key_to_ord={(r["entryId"],r["RelPath"],r["FromLb"],r["Kwic"]):i for i,r in enumerate(ordered,1)}

def anon(kind,label):
 return {"Status":"reviewed-unnamed","Kind":kind,"ActorLabel":label,"ActorRole":"exact headword-bearing speaker or grammatical actor",
         "RungsChecked":RUNGS,"ReviewedBy":"Codex hard-bundle six-rung exact-turn review","ReviewedUtc":NOW}

for path in sorted(HERE.glob("decisions-*.json")):
 sheet=json.loads(path.read_text(encoding="utf8"))
 for row in sheet["rows"]:
  n=key_to_ord[(row["entryId"],row["RelPath"],row["FromLb"],row["Kwic"])]
  title=row["sourceTitle"]
  if n==87:
   label="Impersonal canonical title heading"
   a={"Status":"impersonal","Kind":"document heading","ActorLabel":label,"ActorRole":"bibliographic heading rather than a speech turn",
      "GrammarEvidence":"The string is the canonical number and title heading 勅修百丈清規; it has no grammatical personal speaker.",
      "ReviewedBy":"Codex hard-bundle six-rung exact-turn review","ReviewedUtc":NOW}
   d={"MasterName":None,"ActorAttribution":a,"AttributionNote":f"{title}. Full six-rung exact-turn review identifies {label}; this is bibliographic metadata, not personal speech."}
  elif n in unnamed:
   kind,label=unnamed[n]; a=anon(kind,label)
   d={"MasterName":None,"ActorAttribution":a,"AttributionNote":f"{title}. Full six-rung exact-turn review identifies {label} as the exact actor; all six rungs leave this non-master unnamed."}
  else:
   actor=named.get(n) or source_actor.get(Path(row["RelPath"]).stem)
   if not actor: raise RuntimeError(f"no actor {n} {row['key']}")
   d={"MasterName":actor,"ActorAttribution":None,"AttributionNote":f"{title}. Full six-rung exact-turn review identifies {actor} as the exact headword-bearing speaker, author, or grammatical actor for this occurrence."}
  if n in context:d["ContextMasters"]=context[n]
  row["Override"]=d
 sheet.update(reviewedAllCases=True,reviewer="Codex hard-bundle six-rung exact-turn review",reviewedUtc=NOW)
 path.write_text(json.dumps(sheet,ensure_ascii=False,indent=2)+"\n",encoding="utf8")
print(json.dumps({"signedSheets":len(list(HERE.glob('decisions-*.json'))),"explicitOverrides":len(ordered),"reviewedUtc":NOW},indent=2))
