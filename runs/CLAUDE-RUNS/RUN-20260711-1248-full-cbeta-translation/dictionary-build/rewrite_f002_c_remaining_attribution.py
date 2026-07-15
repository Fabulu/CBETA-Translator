#!/usr/bin/env python3
import json
from pathlib import Path

ROOT = Path(__file__).parent / "fresh-build" / "entries"

def load(i):
    p=ROOT/i/"evidence.draft.json"; return p,json.loads(p.read_text(encoding="utf-8"))
def save(p,d): p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
def replace(i, pairs):
    p,d=load(i); changed=[]
    for s in d["Entry"]["Senses"]:
        ep=s.get("ExplanationParts") or {}
        for k in ("CorpusEarnedOpening",):
            if isinstance(ep.get(k),str):
                for a,b in pairs:
                    if a in ep[k]: ep[k]=ep[k].replace(a,b); changed.append(a)
        for k in ("EvidenceBody",):
            for n,v in enumerate(ep.get(k) or []):
                for a,b in pairs:
                    if a in v: v=v.replace(a,b); changed.append(a)
                ep[k][n]=v
    save(p,d); return changed

jobs={
"t_19784084ccb4":[("寶誌公","Zhigong")],
"t_20cc4b0bc96e":[("Elsewhere students are said to \"recognize only the gate of light and shadow as themselves\" (惟認光影門頭當作自己), and an address warns, \"do not bury yourself at the gate of light and shadow\" (切忌向光影門頭自埋沒). ","Elsewhere Xueguan Zhiyin says students \"recognize only the gate of light and shadow as themselves\" (惟認光影門頭當作自己). ")],
"t_432d8c4f7579":[("A transmission biography challenges Huanglong Huinan’s cited teacher, who 'has a teaching to give people' as having dead speech and asks, 'can dead speech bring people alive?'","A transmission biography records Huanglong Huinan criticizing Chenggong as having a teaching that gives people dead speech and asking, 'can dead speech bring people alive?'")],
"t_6ac3f9f0a2d2":[("another master answers a monk’s use of the couplet","Dahui Zonggao answers an unnamed monk’s use of the couplet")],
"t_75348ebe8a2d":[("the monk in Dongshan's cold-and-heat exchange is compared","Yuanwu Keqin compares the unnamed monk in Dongshan's cold-and-heat exchange")],
"t_8184622cecd7":[("one master says there is nobody of great thorough awakening in the hall; another asks","Tianzhu says there is nobody of great thorough awakening in the hall; the record owner asks")],
"t_a38f997d9c16":[
 ("The phrase is paired with other impossible operations: \"steaming sand to make rice, climbing a tree to seek fish\" (蒸砂作飯，緣木求魚), and \"striking ice to seek fire, steaming sand to make rice\" (敲冰覓火、蒸砂作飯). It also appears in public interview: a monk says, \"talking mystery and speaking subtlety is painted cakes filling hunger; raising the old and discussing the present is steaming sand to make rice. Apart from these two roads, how is a message passed?\" (談玄說妙，畫餅充飢；舉古論今，蒸砂作飯；去此二途，如何通信). One later address deliberately reverses","It also appears in public interview: an unnamed monk contrasts talking mystery and discussing the old with two futile attempts at nourishment, then asks how a message is passed apart from those roads. One later address by Zhean Jingfan deliberately reverses"),
 ("One later address deliberately reverses","One later address by Zhean Jingfan deliberately reverses")],
"t_a5408be46291":[("a student is allowed to bow but warned","Xiangyan Yiduan permits bowing but warns")],
"t_a60b47e59680":[("One text praises a master for effacing forms and cutting vine-tangle","Gao Shitai's prefatory verse praises the record owner for effacing forms and cutting vine-tangle"),("the much commoner longer forms 'cut off the vine-tangle' and 'sever the vine-tangle,'","more common longer forms that add an explicit verb of severing,")],
"t_b0d4b62a9c2f":[("The corpus states its working contrast directly: one must have \"sinews in the eyes and blood under the skin, knowing pain and itch; not knowing pain or itch, how is that different from earth and wood?\" (眼裏有筋，皮下有血，知痛知痒；痛痒不知，何殊土木).","The corpus states its working contrast directly: blood under the skin registers pain and itching, unlike earth and wood."),("used as a qualification for a monk or person addressed in the hall","used by Gulin Qingmao as a qualification for people addressed in the hall"),("the master asks, \"which one has blood under the skin?\"","Yu'an Zhiji asks the head cook, \"which one has blood under the skin?\"")],
"t_b1487d8fc8f9":[("one master says that always establishing firm footing","Zhean Jingfan says that always establishing firm footing")],
"t_b1c32bd93e66":[("In a hall exchange a monk asks the purport of that line and the master answers","In a hall exchange an unnamed monk asks the purport of that line and Fenyang Shanzhao answers")],
"t_b4367c692c8a":[("between a master's words","between Cuifeng Chongxian's words"),("after a monk identifies the present master","after an unnamed monk identifies Cuifeng Chongxian"),("Elsewhere a master asks","Elsewhere Juehua Puzhao asks"),("a monk himself uses the question","an unnamed monk himself uses the question")],
"t_e016fb20e6da":[("a question asks how to judge","an unnamed questioner asks how to judge"),("and a master raises his staff and says","and Huizhou Hao raises his staff and says")],
"t_e95ea628d5dd":[("Before an assembly a master says","Before an assembly Guanghui Yuanlian says"),("another record places the phrase as an answer","Songyuan Chongyue's record places the phrase as his answer")],
"t_f1b933473387":[("The parent term's Chan use is verbal tangle; this exact compound","The parent term's Chan use is verbal tangle; this orthographic form")],
}

for i,pairs in jobs.items():
    replace(i,pairs)

# The inherited prose for this orthographic variant imported dozens of unanchored
# claims from a different headword. Rebuild it solely from its six exact witnesses.
p,d=load("t_b986851dcdd8")
s=d["Entry"]["Senses"][0]
s["ExplanationParts"]={
 "CorpusEarnedOpening":"The clause asks about the time before one's parents were born; the six exact witnesses use it as a doctrinal contrast, a question, and a measure of what is already complete.",
 "EvidenceBody":[
  "Yuanwu Keqin sets 'before father and mother were born' (父母未生已前) against 'after father and mother were born' and the completed body. Gulin Qingmao says that before one's parents were born there was already a line for repaying beneficence (父母未生已前，便有報德酧恩一句). Taiyuan Fu asks Gushan where the nostrils were before one's parents were born (父母未生已前，鼻孔在什麼處). Chushi Fanqi asks how the time before one's parents were born compares with the present moment (父母未生已前，何似這個時節). Cijue Puyin asks a monk where he traveled before his parents were born and strikes him as he prepares to answer (父母未生已前，在甚麼處行履). Yinyuan Longqi says that, as for recommendation, it was already finished before one's parents were born (父母未生已前薦拔已竟). The corpus thus bends the temporal clause into a recurring interview boundary; these witnesses do not supply one uniform answer."
 ]}
save(p,d)

print("rewrote",len(jobs)+1,"worksheets")
