import json,sys
from pathlib import Path
root=Path(__file__).resolve().parents[2];sys.path.insert(0,str(root));import audit_attribution,zc
NOW="2026-07-16T17:20:00Z"; roster=audit_attribution.roster_names()
def load(i):
 p=root/f"fresh-build/entries/{i}/entry.v2.json";return p,json.loads(p.read_text())
def save(p,e):p.write_text(json.dumps(e,ensure_ascii=False,indent=2)+"\n")
def cm(n,r):return {"MasterName":n,"Roles":r}
def note(o,s):return f"Source text ({zc.title(o['RelPath'])}): {s}"
def master(o,n,s,links=[]):
 o["MasterName"]=n;o.pop("ActorAttribution",None);o["ContextMasters"]=[cm(n,["utterer"])]+links;o["AttributionNote"]=note(o,f"The exact headword speaker is {n}. {s}")
def actor(o,label,role,status,kind,s,links=[],rungs=False):
 o.pop("MasterName",None);ev=note(o,f"The exact headword actor is {label}. {s}")
 a={"Status":status,"Kind":kind,"ActorLabel":label,"ActorRole":role,"GrammarEvidence":ev,"ReviewedBy":"Codex v6 full-case manual audit 046-055","ReviewedUtc":NOW}
 if rungs:a["RungsChecked"]=["line","expanded-context","section-header","book-title","tei-header","parallel-passage"]
 o["ActorAttribution"]=a;o["ContextMasters"]=links;o["AttributionNote"]=ev

# 自己: the full signed preface names Wang Xigun, not Ding Libiao from an
# earlier, separate preface in the same volume.
p,e=load("t_fb43354d2aae");O=e["Senses"][0]["Occurrences"]
actor(O[3],"Wang Xigun","compiler","identified-non-master","signed preface",
 "Wang Xigun's first-person preface asks how he could permit himself while confronting uncertainty about life and death; Juelang Daosheng enters only in the ensuing visit.",
 [cm("Juelang Daosheng",["interlocutor","person-discussed"])])
save(p,e)

# 主賓: three nulls were recoverable only by reading the governing full units.
p,e=load("t_e6b5cae9bd56");O=e["Senses"][0]["Occurrences"]
actor(O[0],"Jingfu","compiler","identified-non-master","signed preface",
 "The signed authorial preface contrasts claims of no words with the host-and-guest exchanges circulating through the monasteries.")
actor(O[1],"Leitan Zhun","commentator","identified-non-master","named case commentary",
 "The bound clause belongs to Leitan Zhun's explicitly introduced appraisal; later appraisals in the same case are separate voices.")
actor(O[3],"Zhe'an Fan","utterer","identified-non-master","continuous inaugural address",
 "After the opening incense rite, the marker 'the master said' begins Zhe'an Fan's uninterrupted address, which names host-and-guest as one lineage device.")
actor(O[4],"Muyun Men","utterer","identified-non-master","named memorial address",
 "The heading identifies Muyun Men as the master invited to address the assembly while sweeping the Tiantong ancestral towers; his speech asks for the fitting line when host and guest interchange.")
save(p,e)

# 黃龍三關: Zhang Jiucheng is the named lay verse author, while the repeated
# lamp-record formulas are collective compiler narration about Huanglong.
p,e=load("t_747b1f22d089");O=e["Senses"][0]["Occurrences"]
actor(O[2],"Zhang Jiucheng","verse-author","identified-non-master","named lay verse",
 "The dossier heading names Attendant Zhang Jiucheng, whose three-part verse comments on Huanglong's barriers.",
 [cm("Huanglong Huinan",["case-figure","person-discussed"])])
save(p,e)

# 豐干: the second occurrence is inside Fenyang Shanzhao's continuous direct
# explanation of his three mysteries, not anonymous anthology narration.
p,e=load("t_1095b3f1544e");O=e["Senses"][0]["Occurrences"]
master(O[1],"Fenyang Shanzhao","Fenyang calls the phrase about Lü asking Fenggan his verse on the three mysteries, then demands that the assembly decide its purport.",[cm("Fenggan",["person-discussed"])])
save(p,e)

# English-first prose repairs exposed by the depth gate.
p,e=load("t_1f264c7d97ac");S=e["Senses"][0]
S["Explanation"]="To spend the summer residence (坐夏) is to remain in one monastery for the summer. Biographical records count a monastic life by the number of summer residences completed, locate encounters during a shared summer, and record officials spending the summer at a monastery. A public address can also ask how the completed summer residence should be rewarded. The term therefore functions both as an institutional stay and as a chronological marker in masters' biographies."
S["Occurrences"][1]["AttributionNote"]=note(S["Occurrences"][1],"The biographical compiler reports a lifespan of eighty years and fifty-two completed summer residences; the described master does not utter the headword.")
save(p,e)

p,e=load("t_747b1f22d089");S=e["Senses"][0]
S["Explanation"]="Huanglong Huinan's three barriers (黃龍三關) are: 'Where is your birth-origin?', 'How does my hand resemble the Buddha's hand?', and 'How does my foot resemble a donkey's foot?' Lamp records preserve the questions together, say that Huanglong raised them for more than thirty years, and report that he neither approved nor rejected answers. Huanglong explains that one already through a barrier walks straight on rather than asking the gatekeeper for approval. Later masters raise, versify, contrast, and criticize the named set; those are deployments of the inherited case, not additional barriers."
S["Occurrences"][3]["AttributionNote"]=note(S["Occurrences"][3],"An unnamed monk asks about Huanglong's three barriers; Yuanwu Keqin answers the separate question that follows.")
save(p,e)

p,e=load("t_e6b5cae9bd56");S=e["Senses"][0]
S["Explanation"]="Host and guest (主賓) are the two encounter positions in question and answer. Chan records explicitly describe the positions as mutually usable and interchangeable. Public addresses coordinate this exchange with paired challenge and response, while prefaces and comments use the pair for recorded dialogue itself. The terms name relational positions rather than permanent identities: the same encounter can exchange which participant occupies host and which guest."
save(p,e)

p,e=load("t_1095b3f1544e");S=e["Senses"][0]
S["Occurrences"][0]["AttributionNote"]=note(S["Occurrences"][0],"Guizong Huitong raises the scene of Hanshan and Shide bowing to Fenggan in a public hall address.")
save(p,e)

# Pei Xiu enrichment: two independently transmitted, distinct Huangbo scenes.
# Both full cases were read; each continues to denote the same named lay
# official/interlocutor, so the single person sense remains valid.
p,e=load("t_de2ade080f36");S=e["Senses"][0]
S["Explanation"]="Pei Xiu (裴休) is the Tang official whom Chan records deploy as a lay questioner, patron, student of Huangbo Xiyun, and preface writer. He visits Hualin Shanjue and questions him after two tigers answer the master's call. Other records show Huangbo loudly calling 'Pei Xiu!' and questioning where he is, and later using the same name as the answer when Pei asks him to name a painted Buddha. Three lamp compilations also transmit Pei's preface on the damage caused when inherited teachings become competing doorways and weapons. Those parallel prefaces are one event, while the two added Huangbo exchanges supply distinct public uses of his name."
S["Occurrences"][0]["AttributionNote"]=note(S["Occurrences"][0],"The compiler introduces the visit by naming Surveillance Commissioner Pei Xiu; Pei is the narrated subject, and his direct question begins afterward.")
for o in S["Occurrences"][1:4]:
 o["AttributionNote"]=note(o,"The compiler introduces Pei Xiu as the writer of the preface; Pei owns the quoted preface that follows, but the selected name token belongs to the compiler's introduction.")
added_relpaths={"X/X84/X84n1580.xml","X/X87/X87n1620.xml"}
S["Occurrences"]=[o for o in S["Occurrences"] if o.get("RelPath") not in added_relpaths]
for rel,kw,fr,to,why in [
 ("X/X84/X84n1580.xml","黃檗朗聲曰：裴休！公應諾。黃檗曰：在甚麼處？","0223b04","0223b05","Huangbo loudly calls Pei Xiu by name; Pei answers, and Huangbo asks where he is."),
 ("X/X87/X87n1620.xml","公一日拓一尊佛於黃檗前，跪曰：請師安名。黃檗召曰：裴休。公應諾，黃檗曰：與汝安名竟。公禮拜。","0187b13","0187b15","When Pei asks him to name a painted Buddha, Huangbo calls Pei's own name and declares the naming complete."),
]:
 S["Occurrences"].append({"RelPath":rel,"FromLb":fr,"ToLb":to,"Kwic":kw,"MasterName":"Huangbo Xiyun","Curated":True,"AttributionNote":note({"RelPath":rel},f"The exact headword speaker is Huangbo Xiyun. {why}"),"ContextMasters":[cm("Huangbo Xiyun",["utterer","respondent"]),cm("Pei Xiu",["interlocutor","person-discussed"]) ]})
S["RelatedMasters"]=[n for n in list(dict.fromkeys(S.get("RelatedMasters",[])+["Huangbo Xiyun"])) if n in roster]
save(p,e)

# Keep link-only lists roster-clean; named non-roster actors remain fully
# visible in ActorAttribution and prose pending roster integration.
for eid in "t_efa1e241a7f0 t_fb43354d2aae t_ff59d753a7b1 t_e6b5cae9bd56 t_1f264c7d97ac t_4e2840f46db3 t_747b1f22d089 t_9a4a6df85ba0 t_de2ade080f36 t_1095b3f1544e".split():
 p,e=load(eid)
 for s in e.get("Senses",[]):s["RelatedMasters"]=[n for n in s.get("RelatedMasters",[]) if n in roster]
 save(p,e)
print("repaired checkpoint 046-055")
