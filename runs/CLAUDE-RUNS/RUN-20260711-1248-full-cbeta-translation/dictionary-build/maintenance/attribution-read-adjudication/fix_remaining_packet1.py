import hashlib, json, re, sys
from pathlib import Path

BASE = Path(__file__).resolve().parents[2]
PACKET = Path(__file__).with_name("remaining-quarantine-read-fix-packet-1.json")
CLOSED = {"utterer","respondent","questioner","interlocutor","addressee","section-subject","record-owner","person-described","person-discussed","commentator","later-raiser","later-quoter","teacher","student","compiler","verse-author","case-figure"}
ROLE_MAP = {
    "quoted-commentator":"commentator", "author":"utterer", "formula-source":"person-discussed",
    "text-author-discussed":"person-discussed", "transmission-recipient-discussed":"person-discussed",
    "person-appraised":"person-discussed", "quoted-source":"person-discussed", "case-master":"case-figure",
    "quoted-case reporter":"later-quoter", "person-evaluated":"person-discussed", "raiser":"later-raiser",
    "attributed-quoted-source":"person-discussed", "action-performer":"person-described",
    "record-subject":"section-subject", "section-master":"section-subject", "verse-subject":"person-discussed",
    "monastic-rule compiler":"compiler", "imperial-rule compiler and corrector":"compiler",
}

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

def add_context(o, name, role="person-discussed"):
    if not name: return
    cs=o.setdefault("ContextMasters",[])
    for c in cs:
        if c.get("MasterName")==name:
            rs=c.setdefault("Roles",[])
            if role not in rs: rs.append(role)
            return
    cs.append({"MasterName":name,"Roles":[role]})

def normalize(o):
    aa=o.get("ActorAttribution")
    if aa:
        r=aa.get("ActorRole")
        if r in ROLE_MAP: aa["ActorRole"]=ROLE_MAP[r]
        if aa.get("Status")=="editorial":
            aa["Status"]="impersonal"; aa["Kind"]="editorial or documentary wording"; aa["ActorRole"]="compiler"
        if aa.get("Status")=="identified-non-master":
            aa["ActorLabel"]=re.sub(r"^(?:the named |the )", "", aa.get("ActorLabel", ""))
        if aa.get("Status")=="reviewed-unnamed" and "unnamed" not in aa.get("ActorLabel","").lower():
            aa["ActorLabel"]="unnamed "+aa.get("ActorLabel","actor")
    for c in o.get("ContextMasters",[]):
        roles=[]
        for r in c.get("Roles",[]):
            r=ROLE_MAP.get(r,r)
            if r in CLOSED and r not in roles: roles.append(r)
        c["Roles"]=roles or ["person-discussed"]

def fix(x):
    term=x["SourceTerm"]
    for s in x.get("Senses",[]):
      for o in s.get("Occurrences",[]): normalize(o)
      for a in s.get("ClaimAnchors",[]):
        if isinstance(a,dict): normalize(a)

    # Full-turn adjudications for the packet's known residuals. These are context figures,
    # not headword utterers, unless the stored exact turn itself assigns the headword to them.
    adds={
      "向上": {(0,1):[("Caoshan Benji","case-figure")]},
      "拈古": {(0,2):[("Zhang Shangying","case-figure")],(0,4):[("Zhongfeng Mingben","section-subject")]},
      "大機大用": {(0,1):[("Baizhang Huaihai","section-subject"),("Mazu Daoyi","person-discussed")]},
      "當下": {(0,8):[("Yongzheng Emperor","case-figure")]},
      "如何是祖師西來意": {(0,8):[("Zhitong","addressee")]},
      "惺惺": {(0,2):[("Yongjia Xuanjue","person-discussed")],(0,3):[("Yongjue Yuanxian","person-discussed")]},
      "老婆禪": {(0,2):[("Miyun Yuanwu","person-discussed")]},
      "定慧": {(0,6):[("Yongzheng Emperor","case-figure")]},
      "雲水": {(1,0):[("Chuanzi Decheng","person-discussed")]},
      "現成公案": {(0,5):[("Xuedou Chongxian","person-discussed")]},
      "行腳": {(0,1):[("Nanquan Puyuan","person-discussed")]},
      "本性": {(0,4):[("Lao'an","person-discussed")]},
      "明暗": {(0,0):[("Caoshan Benji","person-discussed")],(0,1):[("Huangbo Xiyun","person-discussed"),("Huanglong Huinan","later-raiser")]},
      "沒蹤跡": {(0,0):[("Hongzhi Zhengjue","person-discussed")],(0,1):[("Fayan Wenyi","person-discussed")]},
      "明心見性": {(0,2):[("Yongzheng Emperor","person-discussed")],(0,3):[("Yongzheng Emperor","case-figure")]},
      "坐禪": {(0,3):[("Mazu Daoyi","person-discussed")],(0,4):[("Songyuan Chongyue","person-discussed")]},
      "堯舜": {(0,4):[("Furong Daokai","person-discussed")]},
      "聲前一句": {(0,0):[("Yantou Quanhuo","person-discussed")],(0,1):[("Caoshan Benji","person-discussed")]},
      "孔子": {(0,0):[("Juelang Daosheng","person-discussed")],(0,2):[("Bodhidharma","person-discussed")]},
      "端的": {(0,2):[("Touzi Yiqing","person-discussed")]},
      "爐鞴": {(0,6):[("Miyun Yuanwu","person-discussed")]},
      "腦後見腮": {(0,4):[("Bodhidharma","person-discussed")]},
    }
    for (si,oi), people in adds.get(term,{}).items():
      s=x["Senses"][si]
      if isinstance(oi,str) and oi.startswith("a"):
        o=s.get("ClaimAnchors",[])[int(oi[1:])-1]
      else: o=s["Occurrences"][oi]
      for name,role in people: add_context(o,name,role)

    # Notes must identify both source and voice.
    for s in x.get("Senses",[]):
      for o in s.get("Occurrences",[]):
        title={
          "T/T47/T47n1997.xml":"Recorded Sayings of Yuanwu (圓悟佛果禪師語錄)",
          "T/T47/T47n1998A.xml":"Recorded Sayings of Dahui (大慧普覺禪師語錄)",
          "T/T48/T48n2008.xml":"Platform Scripture (六祖大師法寶壇經)",
          "X/X82/X82n1571.xml":"Complete Collection of the Five Lamps (五燈全書(第34卷-第120卷))",
          "X/X81/X81n1571.xml":"Complete Collection of the Five Lamps (五燈全書(第1卷-第33卷))",
        }.get(o.get("RelPath"))
        note=o.get("AttributionNote","")
        if title and title.split(" (")[0] not in note and title.split("(")[-1].rstrip(")") not in note:
          o["AttributionNote"]=title+": "+note
    return x

def main():
    packet=json.loads(PACKET.read_text(encoding="utf-8")); limit=int(sys.argv[1]) if len(sys.argv)>1 else 30
    for e in packet["entries"][:limit]:
        path=BASE/e["entry"]
        current=sha(path)
        if current!=e["assignedSha256"]: print("COLLISION",e["id"],current); continue
        x=json.loads(path.read_text(encoding="utf-8")); x=fix(x)
        path.write_text(json.dumps(x,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
        print(e["id"],current,"->",sha(path))

if __name__=="__main__": main()
