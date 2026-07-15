import hashlib,json,re,sys
from pathlib import Path
root=Path(__file__).resolve().parents[2]; sys.path.insert(0,str(root)); import audit_attribution,zc
NOW="2026-07-16T00:10:00Z"; roster=audit_attribution.roster_names(); changed=set()
def load(i):
 p=root/f"fresh-build/entries/{i}/entry.v2.json"; return p,json.loads(p.read_text())
def save(p,e): p.write_text(json.dumps(e,ensure_ascii=False,indent=2)+"\n"); changed.add(p)
def cm(n,r): return {"MasterName":n,"Roles":r}
def master(o,n,roles,note):
 source=zc.title(o["RelPath"])
 if note.startswith(source+": "): note=note[len(source)+2:]
 o["MasterName"]=n;o.pop("ActorAttribution",None);o["ContextMasters"]=[cm(n,roles)];o["AttributionNote"]=f"Source text ({source}): {note}"
def actor(o,label,role,status,kind,note,ctx=[]):
 if status in {"reviewed-unnamed","identified-non-master"} and label.lower() not in note.lower(): note=f"{label} is the exact headword actor. {note}"
 if kind=="editorial heading" and "editorial heading" not in note.lower(): note += " This is an editorial heading."
 source=zc.title(o["RelPath"])
 if note.startswith(source+": "): note=note[len(source)+2:]
 note=f"Source text ({source}): {note}"
 o.pop("MasterName",None);o["ActorAttribution"]={"Status":status,"Kind":kind,"ActorLabel":label,"ActorRole":role,"ReviewedBy":"Codex bound-occurrence manual audit 001-015","ReviewedUtc":NOW,"GrammarEvidence":note};o["ContextMasters"]=ctx;o["AttributionNote"]=note
 if status=="reviewed-unnamed": o["ActorAttribution"]["RungsChecked"]=["line","expanded-context","section-header","book-title","tei-header","parallel-passage"]

# 序: the stored KWICs in 1,2,3,5,6,7 are title/headings, not their later authors' prose.
p,e=load("t_6f47a97d45b0"); S=e["Senses"][0]; title="序"
for n,src,proof in [(1,"古尊宿語錄","舒州龍門佛眼和尚語錄序…豫章徐俯撰 is a title/author block"),(2,"永覺元賢禪師廣錄","No.1437-C永覺和尚廣錄序 is an editorial title"),(3,"三峰藏和尚語錄","序熊開元序三峰藏和尚語錄序 is the title block"),(5,"景德傳燈錄","No.2076景德傳燈錄序 is an editorial title"),(6,"林泉老人評唱投子青和尚頌古空谷集","林泉老人評唱投子丹霞頌古總序 is the preface heading"),(7,"永覺元賢禪師廣錄","No.1437-G鼓山晚錄序 is an editorial title")]:
 actor(S["Occurrences"][n-1],"the source title compiler","compiler","impersonal","editorial heading",f"{src}: {proof}; no human speaker utters the headword in this stored heading.")
master(S["Occurrences"][3],"Yunqi Zhuhong",["utterer"],"無幻禪師語錄: Yunqi Zhuhong's written voice says 門人…徵序於予; he utters the headword in the request he received.")
master(S["Occurrences"][7],"Hanyue Fazang",["utterer","record-owner"],"三峰藏和尚語錄: Hanyue Fazang says 屬山僧序其端 in his own written voice.")
S["Explanation"]="The headword names a preface: prose placed before a record, collection, or scripture to introduce its compilation, transmission, author, or publication. In the stored evidence, six occurrences are the source's own preface headings, while Yunqi Zhuhong and Hanyue Fazang utter the word when describing requests to preface works. The title blocks also name writers such as Xu Fu, Xiong Kaiyuan, and Yang Yi, but a writer named beside a title is not thereby the utterer of the title's headword."
S["RelatedMasters"]=[x for x in S.get("RelatedMasters",[]) if x in roster]
save(p,e)

# 付法: occurrence 2's exact stored use is inside 今諸子云, not Jie'an's reply.
p,e=load("t_77774b8724f1");o=e["Senses"][0]["Occurrences"][1]
actor(o,"Jie'an Wujin's unnamed disciples","questioner","reviewed-unnamed","quoted collective question","介菴進禪師語錄: 今諸子云 governs 從上佛祖皆見有付法偈; the disciples utter this stored headword, and Jie'an's reply begins at 咄.",[cm("Jie'an Wujin",["respondent","record-owner"])])
save(p,e)

# 梁武帝: title occurrence and two corrected continuous hall speakers.
p,e=load("t_7f696f177766");O=e["Senses"][0]["Occurrences"]
O[1]["Kwic"]="達磨見梁武帝黃檗噇糟漢一箭尋常落一鵰"
O[1]["ToLb"]="0405c06"
actor(O[1],"the source heading compiler","compiler","impersonal","editorial heading","宗統編年: 達磨見梁武帝黃檗噇糟漢 is a bare anthology title; no human speaker utters this stored name.")
master(O[3],"Huanyou Chuan",["utterer"],"幻有傳禪師語錄: Huanyou Chuan's uninterrupted hall address says 試看伊初遇梁武帝 and continues the case.")
master(O[5],"Minshu Dazhe",["utterer","later-raiser"],"明州天童景德禪寺宏智覺禪師語錄: Minshu Dazhe raises 舉梁武帝見達磨大師 and then comments 師云 in the same hall address.")
save(p,e)

# 便打: bind each compact stored KWIC to its own grammatical action performer.
p,e=load("t_8879b278cd83");O=e["Senses"][0]["Occurrences"]
actors=[("Yangqi Fanghui",False,"師便打 after the monk attempts to lift the sitting cloth"),("an unnamed monk",True,"僧便打; the monk strikes after answering 也是"),("Linji Yixuan",False,"臨濟見僧來…僧禮拜師便打"),("Linji Yixuan",False,"無位真人 question followed by 師便打"),("Linji Yixuan",False,"落浦遂喝，師便打之"),("Mazu Daoyi",False,"僧問西來意，師便打，乃云我若不打汝"),("Huayan Shengke",False,"老僧便打 in Huayan's first-person report"),("Dadian Baotong",False,"座曰是，師便打趁出院"),("an unnamed monk",True,"僧便打一掌; the student delivers the slap"),("Mazu Daoyi",False,"僧纔入，師便打 in Mazu's raised case"),("Nanyuan Huiyong",False,"僧擬議，師便打 in Nanyuan's exchange")]
for o,(n,unnamed,proof) in zip(O,actors):
 ctx=[] if unnamed else [cm(n,["action-performer"])]
 actor(o,n,"action-performer","reviewed-unnamed" if unnamed else "narrated","unnamed participant" if unnamed else "narrated physical action",f"{o['RelPath']}: {proof}; the stored headword is narrated action, not quoted speech.",ctx)
save(p,e)

# 本來人 occurrence 2 mixed narration+verse is recut to the authored verse only.
p,e=load("t_88de22b8a40e");o=e["Senses"][0]["Occurrences"][1]
o["Kwic"]="述偈曰：本來人，本來人，無腦無頭作麼尋？驀然揪著個鼻孔，試看元來是白丁。"
master(o,"Yuelin Jing",["utterer","verse-author"],"五燈全書(第34卷-第120卷): 述偈曰 begins Yuelin Jing's verse; the recut excludes preceding compiler narration 因參本來人有省.")
save(p,e)

# 參請: exact headword-bearing actions belong to the narrated subjects/collective.
p,e=load("t_90e46d995978");O=e["Senses"][0]["Occurrences"]
for n,label,ctx,proof in [(1,"Zhicheng",[cm("Zhicheng",["action-performer"])],"Zhicheng is the biography's subject in 隨眾參請"),(2,"Shuangfeng Gu",[cm("Shuangfeng Gu",["action-performer"])],"the 古侍者 dossier says he 更不參請"),(5,"Huanyuan Fuyu",[cm("Huanyuan Fuyu",["action-performer"])],"the Huanyuan Fuyu biography says he 乃屈膝參請焉"),(6,"clerical and lay followers",[],"緇素景從，晨夕參請 makes the clerical and lay following the collective performers")]:
 actor(O[n-1],label,"action-performer","narrated" if ctx else "identified-non-master","narrated action",f"{O[n-1]['RelPath']}: {proof}; the compiler narrates but does not perform the action.",ctx)
save(p,e)

# Strict structured-link repairs exposed by the bound audit; these are context
# figures, not substitutions for the exact headword actor.
for eid,sense,occ,name,roles in [
 ("t_7bd745af24d7",1,4,"Xuedou Chongxian",["case-figure"]),
 ("t_7f696f177766",1,3,"Bodhidharma",["case-figure"]),
 ("t_81147ad4e8bf",1,3,"Fengxue Yanzhao",["case-figure"]),
 ("t_8184622cecd7",1,3,"Dahui Zonggao",["later-quoter"]),
 ("t_898279a78ecf",1,3,"Deshan Xuanjian",["later-quoter"]),
 ("t_8a016f49e5b8",1,14,"Yongming Yanshou",["record-owner"]),
 ("t_90e46d995978",1,7,"Fayan Wenyi",["case-figure"]),
 ("t_98c97bba590b",1,7,"Fayan Wenyi",["case-figure"]),
]:
 p,e=load(eid); o=e["Senses"][sense-1]["Occurrences"][occ-1]; links=o.get("ContextMasters") or []
 if name not in [x.get("MasterName") for x in links]: links.append(cm(name,roles))
 o["ContextMasters"]=links; save(p,e)
p,e=load("t_88de22b8a40e"); master(e["Senses"][0]["Occurrences"][4],"Pang Yun",["utterer","questioner"],"五燈會元: 龐居士問 marks Pang Yun as the exact questioner who utters 不昧本來人."); save(p,e)

# Frozen-corpus count-claim refreshes exposed by the full cohort gate.
p,e=load("t_6bc71cc88c2f"); s=e["Senses"][1]; s["Explanation"]=s["Explanation"].replace("傳衣鉢, 31 hits in 26 texts","傳衣鉢, 33 hits in 28 texts").replace("付衣鉢, 10 hits in 8 texts","付衣鉢, 12 hits in 10 texts"); save(p,e)
p,e=load("t_90e46d995978"); e["Senses"][0]["Note"]=e["Senses"][0]["Note"].replace("隨眾參請, 23 occurrences","隨眾參請, 27 occurrences"); save(p,e)

# English-first display hygiene: Chinese evidence remains visible, but only in
# parentheses after the English attribution statement.
cjk=re.compile(r"[\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff]+")
def parenthesize_outside(text):
 out=[]; last=0; depth=0
 for m in cjk.finditer(text):
  between=text[last:m.start()]; out.append(between)
  for ch in between:
   if ch in "(（": depth+=1
   elif ch in ")）" and depth: depth-=1
  out.append(m.group(0) if depth else f"({m.group(0)})")
  last=m.end()
 out.append(text[last:]); return "".join(out)
packet=json.loads((root/"maintenance/attribution-read-adjudication/cohorts-7-9-fullcase-packets.json").read_text())["packets"]
ordered=[]
for row in packet:
 if row["entryId"] not in ordered: ordered.append(row["entryId"])
for eid in ordered[:15]:
 p,e=load(eid)
 for s in e["Senses"]:
  for o in s["Occurrences"]:
   if o.get("AttributionNote"): o["AttributionNote"]=parenthesize_outside(o["AttributionNote"])
 save(p,e)

rows=[]
for p in sorted(changed): rows.append({"entryId":p.parent.name,"sha256":hashlib.sha256(p.read_bytes()).hexdigest()})
(root/"maintenance/attribution-read-adjudication/cohorts-7-9-bound-repair-001-015.json").write_text(json.dumps({"schemaVersion":"bound-repair-v1","entries":rows},indent=2)+"\n")
print(json.dumps(rows,indent=2))
