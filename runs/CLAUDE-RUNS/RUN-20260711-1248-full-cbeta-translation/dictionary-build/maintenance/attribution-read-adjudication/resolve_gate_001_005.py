import json
from pathlib import Path

root = Path(__file__).resolve().parents[2]
def load(i):
 p=root/f"fresh-build/entries/{i}/entry.v2.json"; return p,json.loads(p.read_text())
def save(p,e): p.write_text(json.dumps(e,ensure_ascii=False,indent=2)+"\n")

p,e=load("t_6bc71cc88c2f")
e["Senses"][1]["Occurrences"][0]["ContextMasters"]=[{"MasterName":"Qining Wenyi","Roles":["utterer"]},{"MasterName":"Huineng","Roles":["case-figure"]}]
e["Senses"][1]["Occurrences"][2]["AttributionNote"]="Collected Old Cases Raised in the Lineage (宗門拈古彙集): in compiler narration, no human speaker utters the headword; an unnamed Caoqi robe-and-bowl attendant performs 提起, and the record does not name him in any of the six attribution rungs."
save(p,e)

p,e=load("t_6f47a97d45b0")
for n,title in [(1,"永覺元賢禪師廣錄"),(5,"林泉老人評唱投子青和尚頌古空谷集"),(6,"永覺元賢禪師廣錄")]: e["Senses"][0]["Occurrences"][n]["AttributionNote"]+=f" Source work: {title}."
e["Senses"][0]["RelatedMasters"]=[x for x in e["Senses"][0].get("RelatedMasters",[]) if x not in {"Xu Fu","Lin Zhifan","Xiong Kaiyuan","Yang Yi"}]
for n in (0,2,4): e["Senses"][0]["Occurrences"][n]["ContextMasters"]=[]
e["Senses"][0]["RelatedMasters"]=["Hanyue Fazang" if x=="Sanfeng Hanyue Fazang" else x for x in e["Senses"][0].get("RelatedMasters",[])]
o=e["Senses"][0]["Occurrences"][7]; o["MasterName"]="Hanyue Fazang"; o["ContextMasters"]=[{"MasterName":"Hanyue Fazang","Roles":["utterer","record-owner"]}]
save(p,e)

p,e=load("t_77774b8724f1"); e["Senses"][0]["Explanation"]=e["Senses"][0]["Explanation"].replace("from a teacher to a successor","from an entrusting predecessor to a successor"); save(p,e)
p,e=load("t_7887dc8d449f"); e["Senses"][0]["Occurrences"][0]["ContextMasters"]=[{"MasterName":"Huangbo Xiyun","Roles":["respondent","section-subject"]}]; save(p,e)
p,e=load("t_78d931324d99"); e["Senses"][0]["Occurrences"][2]["ContextMasters"]=[{"MasterName":"Chuiwan Guangzhen","Roles":["utterer"]},{"MasterName":"Zhang Shangying","Roles":["case-figure","person-described"]}]; save(p,e)
