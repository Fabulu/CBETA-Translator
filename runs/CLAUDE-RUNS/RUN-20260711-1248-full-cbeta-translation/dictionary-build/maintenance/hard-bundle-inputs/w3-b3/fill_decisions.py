#!/usr/bin/env python3
import json
from datetime import datetime, timezone
from pathlib import Path

HERE=Path(__file__).resolve().parent
TRIAGE=HERE.parent/"worker-3-bundle-3-triage.json"
RUNGS=["line","expanded-context","section-header","book-title","tei-header","parallel-passage"]
NOW=datetime.now(timezone.utc).isoformat().replace("+00:00","Z")

named={
 1:"Dongshan Huiyuan",2:"Fayun Faxiu",3:"Shanquan Huitai",4:"Shanquan Huitai",5:"Zhao Bian",
 6:"Benjue Shouyi",7:"Yuwang Tanzhen",8:"Daochang Yougui",9:"Ganlu Dazhu",10:"Lingyan Yuanri",
 11:"Donglin Changzong",12:"Kaiyuan Ziqi",13:"Renshan Qingxian",14:"Baoning Yuanji",15:"Baohua Pujian",
 16:"Jiufeng Xiguang",17:"Qining Wenyi",18:"Xingkong Miaopu",19:"Liangshan Huan",20:"Doushuai Huizhao",
 21:"Sizu Zhongxuan",22:"Zhongyan Huimu Yunneng",23:"Dawei Yi'an Jian",24:"Baoning Renyong",25:"Yungai Zhiben",
 26:"Yuanwu Keqin",27:"Li Mison",28:"Dawei Fabao",29:"Dayuan Zunpu",30:"Yuanji Yancen",
 31:"Xi'an Daguan",32:"Jue'an Mengzhen",33:"Tieniu Chiding",34:"Duanya Liaoyi",35:"Dafang Yin",
 36:"Yanxi Guangwen",37:"Wanshan Zhengning",38:"Yu'an Zhiji",39:"Mengtang Tan'e",40:"Gufeng Mingde",
 41:"Baowen Hongyin",43:"Shiyu Mingfang",44:"Nanming Guang",45:"Chaozong Tongren",46:"Lingyin Yinwen",
 47:"Guangming Benyuan",48:"Cizong Ming",49:"Tui'an Daoxun",50:"Muyun Tongmen",51:"Tieyuan",
 52:"Tieyuan",53:"Xinzai Chelu",54:"Shiguan Ling",55:"Cuiting Yao",56:"Jinli Tihui Lu",
 57:"Shakyamuni Buddha",58:"Shakyamuni Buddha",59:"Shakyamuni Buddha",60:"Upagupta",61:"Tianzhu Chonghui",
 62:"Jingshan Daoqin",63:"Jufang",64:"Wuzhu",66:"Angulimala",67:"Baozhi",68:"Budai",69:"Ziman",
 70:"Wenzhou Fotuo",71:"Mazu Daoyi",72:"Pingtian Puan",73:"Xiangyan Yiduan",74:"Guannan Daowu",
 75:"Deng Yinfeng",76:"Dayang Jingxuan",77:"Layman Pangyun",78:"Luopu Yuan'an",
 79:"Third-Generation Master of Fulong Mountain",80:"Changsha Jingcen",81:"Yong'an Jingwu",82:"Huizong Emperor",
 84:"Chen Cao",85:"Yaoshan Weiyan",86:"Shengguang",87:"Jiufeng Daoqian",88:"Yongquan Jingxin",
 89:"Luyuan",90:"Zhiyi Kefu",91:"Vinaya Master Bensong",92:"Xuanquan Yan",93:"Langye Huijue",
 94:"Xuansha Shibei",95:"Changqing Huileng",96:"Linyang Zhiduan",97:"Changping",98:"Guangfu Weishang",
 99:"Xuechao Fayi",100:"Shangfang Riyi",101:"Huayan Zujue",102:"Huizong Emperor",103:"Hongren",
 104:"Luohan",105:"Luohan",106:"Yungai Zhiyong",107:"Langye Huijue",108:"Jinshan Daguan",
 109:"Chengtian Chuanzong",110:"Fuyan Baozong",111:"Tianzhang Yuanshan",112:"Hengyue Daobian",
 113:"Fayan Wenyi",114:"Dawei Lingyou",115:"Guangxiao Huijue",116:"Shimen Yuncong",117:"Jingyin Jicheng",
 118:"Wenshu Zhengdao",119:"Puxian Yuansu",120:"Jiaozhong Huian Miguang",121:"Dahui Zonggao",
 122:"Wansong Yunyan Denggu",123:"Guizong Zhenmu Zhengxian",125:"Fojian Huiqin",126:"Guang'e Butcher",
 127:"Dongshan Jue",128:"Zhengdang Mingbian",129:"Fayan Wenyi",130:"Tiantai Deshao",131:"Guizong Yirou",
 132:"Bao'en Faan",133:"Zhanran Yuancheng",134:"Zhanran Yuancheng",135:"Zhanran Yuancheng",
 136:"Zhanran Yuancheng",138:"Juehua Puzhao",139:"Cuifeng Chongxian",140:"Caotang Shanqing",
 141:"Zhongfeng Mingben",142:"Lia'an Qingyu",143:"Wenxiu",144:"Xiaoweng Miaokan",145:"Dahui Zonggao",
 146:"Jianru Yuanmi",147:"Weilin Daopei",148:"Feiyin Tongrong",149:"Minshu Xiang"
}

unnamed={
 42:("monk","Unnamed monk asking about the naturally true Buddha of self-nature"),
 65:("monk","Unnamed monk left at a loss after Kuduo Tripitaka's question"),
 83:("monk","Unnamed monk who meditated daily in the sutra hall")
}

impersonal={
 124:("canonical title heading","Canonical number-and-title heading for the Jiatai Record of the Lamp"),
 137:("catalogue heading","Canonical title and table-of-contents heading for the Tiansheng Expanded Record of the Lamp")
}

context={
 5:["Jiangshan Quan"],42:["Wuhuan Guchan Xingchong"],65:["Kuduo Tripitaka"],66:["Nanyang Huizhong"],
 83:["Xiangyan Yiduan"],86:["Dinghui"],98:["Jueyin"],103:["Huineng"],113:["Fayan Wenyi"],
 122:["Wansong Xingxiu"],126:["Dongshan Jue"],145:["Shuzhong Wuyun"]
}

triage=json.loads(TRIAGE.read_text(encoding="utf8")); ordered=[]
for source in triage["sources"]:
 for cluster in source["clusters"]: ordered.extend(cluster["occurrences"])
if len(ordered)!=149: raise RuntimeError(f"expected 149 occurrences, got {len(ordered)}")
key_to_ord={(r["entryId"],r["RelPath"],r["FromLb"],r["Kwic"]):i for i,r in enumerate(ordered,1)}

def anon(kind,label):
 return {"Status":"reviewed-unnamed","Kind":kind,"ActorLabel":label,
         "ActorRole":"exact headword-bearing speaker or grammatical actor","RungsChecked":RUNGS,
         "ReviewedBy":"Codex hard-bundle six-rung exact-turn review","ReviewedUtc":NOW}

signed=rows=0
for path in sorted(HERE.glob("decisions-*.json")):
 sheet=json.loads(path.read_text(encoding="utf8"))
 for row in sheet["rows"]:
  n=key_to_ord[(row["entryId"],row["RelPath"],row["FromLb"],row["Kwic"])]
  title=row["sourceTitle"]
  if n in impersonal:
   kind,label=impersonal[n]
   a={"Status":"impersonal","Kind":kind,"ActorLabel":label,"ActorRole":"bibliographic heading rather than a speech turn",
      "GrammarEvidence":"The occurrence is document metadata with no grammatical personal speaker.",
      "ReviewedBy":"Codex hard-bundle six-rung exact-turn review","ReviewedUtc":NOW}
   d={"MasterName":None,"ActorAttribution":a,
      "AttributionNote":f"{title}. Full six-rung exact-turn review identifies: {label}. This is not personal speech."}
  elif n in unnamed:
   kind,label=unnamed[n]
   d={"MasterName":None,"ActorAttribution":anon(kind,label),
      "AttributionNote":f"{title}. Full six-rung exact-turn review identifies {label} as the exact actor; all six rungs leave this non-master unnamed."}
  else:
   actor=named.get(n)
   if not actor: raise RuntimeError(f"no explicit actor for row {n}: {row['key']}")
   d={"MasterName":actor,"ActorAttribution":None,
      "AttributionNote":f"{title}. Full six-rung exact-turn review identifies {actor} as the exact headword-bearing speaker, author, or grammatical actor for this occurrence."}
  if n in context: d["ContextMasters"]=context[n]
  row["Override"]=d; rows+=1
 sheet.update(reviewedAllCases=True,reviewer="Codex hard-bundle six-rung exact-turn review",reviewedUtc=NOW)
 path.write_text(json.dumps(sheet,ensure_ascii=False,indent=2)+"\n",encoding="utf8"); signed+=1
print(json.dumps({"signedSheets":signed,"explicitOverrides":rows,"reviewedUtc":NOW},indent=2))
