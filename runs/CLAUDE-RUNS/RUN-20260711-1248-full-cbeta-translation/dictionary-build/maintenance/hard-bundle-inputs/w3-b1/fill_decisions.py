#!/usr/bin/env python3
import json
from datetime import datetime, timezone
from pathlib import Path

HERE = Path(__file__).resolve().parent
TRIAGE = HERE.parent / "worker-3-bundle-1-triage.json"
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]
NOW = datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")

source_actor = {
 "B25n0145":"Zhongfeng Mingben", "J26nB178":"Feiyin Tongrong", "J33nB294":"Langting Jingting",
 "J28nB202":"Baichi Yuan", "J25nB171":"Tianyin Yuanxiu", "J34nB311":"Juelang Daosheng",
 "J26nB187":"Tian'an Sheng", "J28nB208":"Guxue Zhe", "J36nB359":"Baiyu Si",
 "B27n0152":"Yulin Tongxiu", "J10nA158":"Miyun Yuanwu", "D48n8939":"Foyan Qingyuan",
 "J27nB198":"Xueguan Zhiyin", "J34nB299":"Hanyue Fazang", "J26nB177":"Poshan Haiming",
 "J27nB190":"Shiyu Mingfang", "J28nB219":"Zhuanyu Heng", "J25nB174":"Juelang Daosheng",
 "J36nB369":"Zhean Jingfan", "J37nB386":"Yuan'an Feng", "J38nB406":"Tianran Hanshi",
 "J26nB188":"Ruibai Mingxue", "J29nB244":"Sanshan Denglai", "J34nB300":"Chaozong Tongren",
 "J27nB192":"Daxiu Zhu", "J28nB212":"Eryin Mi", "J20nB098":"Huangbo Wunian Shenyou",
 "J27nB197":"Wuyi Yuanlai", "J32nB273":"Qianyan Yuanzhang", "J33nB286":"Yingning Jing",
 "J37nB392":"Hansong Zhicao", "J26nB180":"Tiantong Hongjue Min", "J28nB220":"Faxi Yin",
 "J29nB249":"Fangrong Tongxi", "J25nB156":"Wuhuan", "J28nB211":"Jizong Che",
 "J29nB223":"Shanhui", "J29nB239":"Chuiwan Guangzhen", "J29nB242":"Tiemei Sanbazhang",
 "J33nB280":"Shending Yunwai Ze", "J37nB396":"Panlong Zisu", "J27nB191":"Xiangtian Jinian",
 "J28nB203":"Yun'e Xi", "J29nB235":"Lianyue Guangsi", "J34nB301":"Nanyue Jiqi Hongchu",
 "J34nB304":"Lingshu Sengyuan", "J35nB343":"Miyin", "J37nB384":"Hanxiu Rugan"
}

# Every number is the global occurrence ordinal in the immutable bundle triage.
named = {
 1:"Mazu Daoyi",2:"Baizhang Huaihai",4:"Tangming Zhichong",5:"Zihu Shenli",6:"Guangzhi Quanwu",7:"Fahua Quanju",
 8:"Foyan Qingyuan",9:"Foyan Qingyuan",10:"Old Master of the 'Not Yet' Case",11:"Foyan Qingyuan",
 12:"Touzi Datong",13:"Gushan Shenyan",14:"Yun'an Kewen",21:"Zhongfeng Mingben",24:"Longtan Chongxin",
 26:"Yaoshan Weiyan",27:"Li Ao",28:"Dongshan Liangjie",40:"Yaoshan Weiyan",43:"Yuantong Xiu",
 47:"Puhui",48:"Yexuan Zun",49:"Jianweng Jing",50:"Bodhidharma",51:"Later Baoshou",
 57:"Wuming Huijing",74:"Shakyamuni Buddha",75:"Shishuang Qingzhu",76:"Zhaozhou Congshen",
 87:"Gaofeng Yuanmiao",109:"Yunmen Wenyan",116:"Huanglong Huinan",123:"Nanquan Puyuan",
 132:"Ruiyan Shiyan",133:"Hanshan",134:"Hanshan"
}

unnamed = {
 3:("monk", "Unnamed monk requesting a teaching"),
 38:("lay practitioner", "Unnamed lay practitioner posing the eighteen-question test"),
 39:("monk", "Unnamed monk saying 'I know for certain'"),
 56:("assembly", "Unnamed assembly requesting instruction"),
 58:("monk", "Unnamed monk asking how the master receives people"),
 60:("interlocutor", "Unnamed interlocutor asking 'Who is reciting the Buddha?'"),
 78:("host monk", "Unnamed host monk left at a loss"),
 79:("questioner", "Unnamed questioner asking about mistaking thief and son"),
 91:("lay practitioner", "Lay practitioner surnamed Wan"),
 110:("monk", "Unnamed monk requesting instruction"),
 111:("monk", "Unnamed monk asserting that the forehead-eye opens"),
 117:("monk", "Unnamed monk asking about the guest within the host"),
 118:("monk", "Unnamed monk asking about the guest within the host"),
 119:("monk", "Unnamed monk left at a loss in Wenxi's exchange"),
 120:("monk", "Unnamed monk asking about Tianzhu's house style"),
 122:("questioner", "Unnamed questioner asking about self-nature"),
 130:("monk", "Unnamed monk answering about the far side and this side"),
 140:("monk", "Unnamed monk asking what the critical phrase is"),
 142:("monk", "Unnamed monk asking for news from the far side"),
 144:("monk", "Unnamed monk asking about the ancient Buddha's house style"),
 145:("monk", "Unnamed monk asking whether it can be discerned without moving sound or form"),
 147:("interlocutor", "Unnamed interlocutor requesting a discrimination")
}

context = {
 3:["Shimen Cizhao"],10:["Foyan Qingyuan"],24:["Tianhuang Daowu"],38:["Langting Jingting"],39:["Langting Jingting"],
 40:["Baichi Yuan"],43:["Baichi Yuan"],51:["Tianyin Yuanxiu"],56:["Juelang Daosheng"],57:["Juelang Daosheng"],
 58:["Juelang Daosheng"],60:["Juelang Daosheng"],74:["Mahakasyapa","Yulin Tongxiu"],75:["Yulin Tongxiu"],
 76:["Yulin Tongxiu"],78:["Miyun Yuanwu"],79:["Miyun Yuanwu"],87:["Hanyue Fazang"],91:["Poshan Haiming"],
 109:["Ruibai Mingxue"],110:["Sanshan Denglai"],111:["Chaozong Tongren"],117:["Eryin Mi"],118:["Eryin Mi"],
 119:["Wenxi"],120:["Tianzhu Chonghui"],122:["Huangbo Wunian Shenyou"],123:["Wuyi Yuanlai"],
 130:["Faxi Yin"],132:["Fangrong Tongxi"],140:["Tiemei Sanbazhang"],142:["Shending Yunwai Ze"],
 144:["Xiangtian Jinian"],145:["Yun'e Xi"],147:["Nanyue Jiqi Hongchu"]
}

triage = json.loads(TRIAGE.read_text(encoding="utf-8"))
ordered=[]
for source in triage["sources"]:
    for cluster in source["clusters"]:
        ordered.extend(cluster["occurrences"])
key_to_ord={(r["entryId"],r["RelPath"],r["FromLb"],r["Kwic"]):i for i,r in enumerate(ordered,1)}

def reviewed_unnamed(kind,label):
    return {"Status":"reviewed-unnamed","Kind":kind,"ActorLabel":label,"ActorRole":"exact headword-bearing speaker or grammatical actor",
            "RungsChecked":RUNGS,"ReviewedBy":"Codex hard-bundle six-rung exact-turn review","ReviewedUtc":NOW}

for sheet_path in sorted(HERE.glob("decisions-*.json")):
    sheet=json.loads(sheet_path.read_text(encoding="utf-8"))
    for row in sheet["rows"]:
        ordinal=key_to_ord[(row["entryId"],row["RelPath"],row["FromLb"],row["Kwic"])]
        title=row["sourceTitle"]
        stem=Path(row["RelPath"]).stem
        if ordinal == 68:
            label="Impersonal volume and lineage heading"
            actor={"Status":"impersonal","Kind":"document heading","ActorLabel":label,"ActorRole":"grammatical heading, not a speaking person",
                   "GrammarEvidence":"The string is a volume/attribution heading naming the record and its dharma-heir compiler; it contains no personal speech turn.",
                   "ReviewedBy":"Codex hard-bundle six-rung exact-turn review","ReviewedUtc":NOW}
            decision={"MasterName":None,"ActorAttribution":actor,
                      "AttributionNote":f"{title}. Full six-rung exact-turn review identifies {label}; the wording is document metadata rather than a personal utterance."}
        elif ordinal in unnamed:
            kind,label=unnamed[ordinal]; actor=reviewed_unnamed(kind,label)
            decision={"MasterName":None,"ActorAttribution":actor,
                      "AttributionNote":f"{title}. Full six-rung exact-turn review identifies {label} as the exact actor; all six rungs leave this non-master unnamed."}
        else:
            actor=named.get(ordinal) or source_actor.get(stem)
            if not actor: raise RuntimeError(f"no explicit actor for row {ordinal} {row['key']}")
            decision={"MasterName":actor,"ActorAttribution":None,
                      "AttributionNote":f"{title}. Full six-rung exact-turn review identifies {actor} as the exact headword-bearing speaker, author, or grammatical actor for this occurrence."}
        if ordinal in context: decision["ContextMasters"]=context[ordinal]
        row["Override"]=decision
    sheet["reviewedAllCases"]=True
    sheet["reviewer"]="Codex hard-bundle six-rung exact-turn review"
    sheet["reviewedUtc"]=NOW
    sheet_path.write_text(json.dumps(sheet,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")

print(json.dumps({"signedSheets":len(list(HERE.glob('decisions-*.json'))),"explicitOverrides":len(ordered),"reviewedUtc":NOW},indent=2))
