import json
from pathlib import Path
BUILD=Path(__file__).resolve().parents[2]
def load(t):
 p=BUILD/"fresh-build/entries"/t/"entry.v2.json";return p,json.loads(p.read_text(encoding="utf-8"))
def save(p,d):p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")

p,d=load("t_97b566635d6c");s=d["Senses"][0]
s["Explanation"]=s["Explanation"].replace("Guishan Lingyou says a lineage master must meet people with the fundamental matter.","Zhaozhou Congshen says a lineage master must meet people with the fundamental matter.").replace("Deshan Yuanmi","Deshan Yuanming")
save(p,d)

p,d=load("t_37261001c332");s=d["Senses"][0];os=s["Occurrences"]
os[6]["MasterName"]=None
os[6]["ContextMasters"]=[{"MasterName":"Zhang Tingyu","Roles":["named-unrostered","verse-author"]}]
os[6]["ActorAttribution"]={"Status":"identified-non-master","Kind":"lay author","ActorLabel":"Zhang Tingyu / Layman Chenghuai (張廷玉／澄懷居士)","ActorRole":"utterer","GrammarEvidence":"The section heading 大學士張廷玉澄懷居士 identifies Zhang Tingyu, Layman Chenghuai, as author of the headword-bearing verse.","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"ReviewedBy":"Codex grouped 4-6 independent-review full-unit REVISE","ReviewedUtc":"2026-07-16T13:00:00Z"}
os[6]["AttributionNote"]="The section heading names Grand Secretary Zhang Tingyu, Layman Chenghuai, as author of the verse saying that holding mind and inspecting mind divide guest and host."
os[8]["MasterName"]="Chaoding Yuxuan"
os[8]["ContextMasters"]=[{"MasterName":"Chaoding Yuxuan","Roles":["utterer","verse-author"]}]
os[8].pop("ActorAttribution",None)
os[8]["AttributionNote"]="The section heading names Wanshou Monastery abbot Chaoding Yuxuan as author of the verse saying that when inspecting mind is clear, the single undivided flavor is alert."
s["Explanation"]="Inspect mind or look at mind. The compound occurs 474 times in 84 allowed texts, but 257 hits are concentrated in the Record of the Source-Mirror, and the corpus does not give the action one uniform set of predicates. In the Daoxin–Niutou Farong encounter, Farong answers ‘inspecting mind’ when asked what he is doing; Daoxin asks, ‘Who is doing the inspecting, and what thing is mind?’ (觀是何人，心是何物), and Farong gives no answer. The Treatise on Breaking Appearances says, ‘Only inspecting mind, this one thing, gathers all things’ (唯觀心一法，總攝諸法), then asks, ‘How does inspecting mind count as understanding?’ (云何觀心稱之為了). Its reply distinguishes a clean mind and a soiled mind and later says, ‘Inspecting mind in this way may be called understanding’ (如是觀心可名為了). The Record of the Source-Mirror quotes a different direct formula: ‘Inspect mind as no mind; things do not dwell on things; my mind is itself empty; fault and fortune have no owner—this is called correct inspection’ (觀心無心，法不住法，我心自空，罪福無主，即是無心無數，名為正觀). Other Chan witnesses criticize particular deployments. The Yongzheng Emperor says that making silence final and ‘then devoting oneself to quietly inspecting mind’ is ‘escaping a pit and falling into a ditch’ (便務靜觀心，又所謂逃坑落塹). Layman Xuri says, ‘closing the eyes, vainly seek seeing; inspecting mind, mistakenly seek penetration’ (閉目妄求見，觀心誤覓通). Zhang Tingyu, Layman Chenghuai, writes that ‘holding mind and inspecting mind divide guest and host’ (持心觀心紛主客). Chaoding Yuxuan writes, ‘if inspecting mind here is clear, the undivided single flavor is itself alert’ (於此觀心若了了，混然一味自惺惺). These are text-drawn assertions and criticisms by different named voices; they do not supply a single imported category for the headword."
save(p,d)
print("repaired independent-review REVISE entries 066-075")
