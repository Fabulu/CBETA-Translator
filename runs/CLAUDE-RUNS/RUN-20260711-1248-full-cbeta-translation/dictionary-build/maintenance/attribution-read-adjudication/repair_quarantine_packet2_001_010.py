import json
from pathlib import Path

R = Path(__file__).resolve().parents[2]

IDS = {
    "逢佛殺佛": "t_121b66b78c9e", "一句": "t_4cc95950b59a", "本來無一物": "t_93ab42fecdca",
    "以心傳心": "t_d11d5f0c78a5", "衲子": "t_0427b79d8ba4", "轉身": "t_6293dead3bb2",
    "一物": "t_94ee610a30f7", "攝心": "t_057cc9ea8755", "活鱍鱍": "t_1d1a833551a9",
    "放下著": "t_34143e43daf4",
}

def load(term):
    p = R / "fresh-build/entries" / IDS[term] / "entry.v2.json"
    return p, json.loads(p.read_text(encoding="utf-8"))

def occ(d, n):
    return d["Senses"][0]["Occurrences"][n - 1]

def add(o, name, *roles):
    cms = o.setdefault("ContextMasters", [])
    row = next((x for x in cms if x["MasterName"] == name), None)
    if row is None:
        row = {"MasterName": name, "Roles": []}; cms.append(row)
    for role in roles:
        if role not in row["Roles"]: row["Roles"].append(role)

def roles(o, name, *new):
    row = next(x for x in o["ContextMasters"] if x["MasterName"] == name)
    row["Roles"] = list(new)

def save(p, d):
    p.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

p,d=load("逢佛殺佛"); add(occ(d,3),"Linji Yixuan","case-figure"); save(p,d)
p,d=load("一句"); add(occ(d,4),"Yuanwu Keqin","later-quoter","commentator"); add(occ(d,9),"Daowu Yuanzhi","respondent"); save(p,d)
p,d=load("本來無一物"); roles(occ(d,2),"Shenxiu","verse-author"); roles(occ(d,2),"Hongren","teacher","case-figure"); roles(occ(d,4),"Huineng","case-figure"); add(occ(d,4),"Shenxiu","person-discussed"); save(p,d)
p,d=load("以心傳心"); roles(occ(d,1),"Huineng","addressee","student"); roles(occ(d,1),"Bodhidharma","case-figure"); add(occ(d,3),"Zhitong","addressee"); roles(occ(d,6),"Bodhidharma","utterer"); roles(occ(d,6),"Huike","questioner","student"); roles(occ(d,6),"Guifeng Zongmi","compiler","later-quoter"); save(p,d)
p,d=load("衲子"); add(occ(d,5),"Fengxue Yanzhao","person-discussed"); save(p,d)
p,d=load("轉身"); o=occ(d,7); row=next(x for x in o["ContextMasters"] if x["MasterName"] in {"Sanfeng Hanyue Fazang","Hanyue Fazang"}); row["MasterName"]="Hanyue Fazang"; save(p,d)
p,d=load("一物"); add(occ(d,1),"Huineng","respondent","teacher"); add(occ(d,3),"Xuedou Chongxian","teacher"); d["Senses"][0].pop("Definition",None); d["Senses"][0]["Explanation"] = d["Senses"][0]["Explanation"].replace("a open-ended", "an open-ended"); save(p,d)
p,d=load("攝心"); aa=occ(d,6)["ActorAttribution"]; aa["GrammarEvidence"] = aa["GrammarEvidence"].replace(" The prior assignment to Guifeng Zongmi had no source support.", ""); save(p,d)
p,d=load("活鱍鱍"); roles(occ(d,2),"Yaoshan Weiyan","case-figure"); add(occ(d,2),"Danxia Zichun","verse-author"); add(occ(d,2),"Dongshan Liangjie","case-figure"); save(p,d)
p,d=load("放下著"); add(occ(d,1),"Wuming Huijing","later-raiser"); save(p,d)
