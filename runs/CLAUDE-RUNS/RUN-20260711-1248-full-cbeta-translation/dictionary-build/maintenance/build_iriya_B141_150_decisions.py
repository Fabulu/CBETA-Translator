#!/usr/bin/env python3
import json
import sys
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
M = ROOT / "maintenance"
sys.path.insert(0, str(ROOT))
import zc
packet = json.loads((M / "iriya-author-packet-B071-167-v1.json").read_text(encoding="utf-8"))
struct = json.loads((M / "iriya-construction-001-pos021-end-structural-cases-v2.json").read_text(encoding="utf-8"))
rows = {r["position"]: r for r in packet["rows"]}
cases = {(r["id"], r["caseIndex"]): r["structuralCase"] for r in struct["rows"]}
now = datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")

META = {
  141: ("stultify people to death",
        "A lethal-stultification verdict, not a report of homicide: the records aim it at words or handling that leave people deadened rather than clarified.",
        "Yunmen drops the verdict after a long silence; Zhantang Wenzhun applies it to Yaoshan and Yunyan's lion-play; Sixin Wuxin pairs it with concealing people's capacities, while Fengxian Shen uses it immediately after the ceremonial hammer's formula."),
  142: ("pass through the skull",
        "The image drives clean through the skull: in these interviews it names a phrase, answer, or action whose reach is tested beyond the hearer's head-bound response.",
        "An unnamed monk asks Chongfan Yu for 'a phrase that passes through the skull'; Xuedou Chongxian and Tiantai Deng answer later questions with the image, and Shilin Xinggong says a staff can pass through it even when one refuses to make a buddha."),
  143: ("lose oneself while pursuing things",
        "The paired verbs describe the reversal in which one's own position is lost while one runs after objects; the corpus uses it as an accusation and as a description of people carried along by sights, sounds, and daily concerns.",
        "Dahui places the phrase beside being driven by the six sense fields, Jingqing Daofu says it after an unnamed respondent identifies the sound outside as raindrops, and Baiyu Jingsi places it between turning from awareness and wandering through the modes of birth."),
  144: ("rise in the east and vanish in the west",
        "This is movement without a fixed emergence point: rising here and disappearing there, then reversing or shifting the same pattern across the other directions.",
        "Linji expands the east-west pair into south-north, middle-edge, and edge-middle movement; Beita Siguang gives the pair as the answer to an unnamed questioner asking about a patch-robed monk's transformations; Kaiyuan Ying couples it with suddenness and penetrating through skin and bone."),
  145: ("forget what came before and lose what follows",
        "The formula marks a broken sequence: what preceded is forgotten and what should follow is lost. Chan commentators use it when an answer or appraisal fails to keep both sides of an encounter in view.",
        "Shuiguan Shougui imagines a binding that makes patch-robed people lose their bearings, Danxia Tianran contrasts his interlocutor's lost-before/lost-after with his own reversed formula, and Tianxian Cong turns the words against himself after a visitor's reply."),
  146: ("guard the stump waiting for a rabbit",
        "The old scene is passive waiting at the stump where a rabbit once struck; Chan records preserve it as a criticism of depending on a previous success instead of meeting the present encounter.",
        "Bajiao Huiqing pairs stump-watching with hiding among reeds, Doushuai Zhien says a true-colour monk will not do it, and the Blue Cliff Record places it beside a ram caught in a fence when words circulate without live use."),
  147: ("iron eyes and bronze pupils",
        "Iron eyes with bronze pupils are the corpus's image of exceptionally hard, penetrating sight, yet each retained use stresses what even such eyes cannot see, measure, or withstand.",
        "Mi'an Xianjie says such eyes cannot see through the patriarchal saying, Guxue Zhenzhe says they cannot measure Mazu's displayed activity, and an Anji master says they still shatter before the matter raised at his opening assembly."),
  148: ("protect life by killing until peaceful dwelling is possible",
        "The couplet deliberately binds protection to killing and peaceful residence to killing's exhaustion; the records then ask what is killed, refuse ordinary killing as its settled referent, and test the line in public exchanges.",
        "Yue'an Shanguo calls the couplet only halfway and asks for the homeward phrase; Wuming Huijing places it in an address about holding one word continuously; the layman Haian asks whether the line means killing or releasing life, and Yingshan Shengke corrects an unnamed interlocutor's altered final clause."),
  149: ("feel along a fence and grope along a wall",
        "The bodily scene is blind progress by borrowed supports: hands follow a fence or wall because the way ahead is not directly seen.",
        "Pu'an Yinsu fears descendants returning in the dark by fence and wall, Mi'an Xianjie contrasts this groping with speech and action arriving together, and Tian'an Sheng turns it against an unnamed questioner who claims that everyone stands sheer and sees across heaven and earth."),
  150: ("a world of acquaintances with few who know one's heart",
        "The balanced saying contrasts the breadth of social recognition with the rarity of intimate understanding: the world may be full of acquaintances while heart-knowers remain few.",
        "Dongchan Zhiguan caps his appraisal of the Buddha-and-demon exchange with the question; Daowu Yuanzhi's complete saying is raised and answered by later masters; Feiyin Tongrong uses it to close a hall challenge about where the decisive point lies."),
}

SELECT = {
  141:[0,1,2,3,4,5], 142:[0,1,2,3,4,7], 143:[0,1,3,4,5,7],
  144:[0,2,3,4,6,7], 145:[0,2,3,5,6,7], 146:[0,1,2,4,5,7],
  147:[0,1,2,4,5,6], 148:[1,2,3,4,5,7], 149:[0,2,3,4,5,6],
  150:[0,1,3,4,6],
}

ALIAS = {
  141:"Readers may search the complete verdict as either 'stultify people' or 'deaden people'; the killing clause remains visible in the displayed target.",
  142:"The lookup keeps both the motion through and the skull image so it cannot collapse into a generic penetration phrase.",
  143:"Lookup preserves the reflexive loss and outward pursuit together because the corpus repeatedly coordinates both actions.",
  144:"The English lookup keeps the opposed directional verbs that distinguish this mobile formula from ordinary rising or vanishing.",
  145:"Search wording retains the earlier/later sequence and both kinds of loss rather than reducing the verdict to forgetfulness.",
  146:"The stump and rabbit remain together in lookup because either image alone retrieves unrelated literal corpus uses.",
  147:"Both metal eye-parts remain searchable, including the natural English phrase 'iron eyes and bronze pupils'.",
  148:"The lookup compresses the balanced couplet without removing its protection, killing, exhaustion, and dwelling predicates.",
  149:"Fence and wall are both retained so readers can retrieve the fixed two-part groping scene under either common English verb.",
  150:"Lookup joins worldwide acquaintance to rare heart-knowing, preserving the saying's quantitative contrast.",
}
FAMILY = {
  141:"Separate uses of killing, people, or dullness do not reproduce the compound verdict; paired uses with concealing capacities clarify its range.",
  142:"Skull compounds and ordinary piercing were excluded; the selected cases predicate passing through a skull as one fixed expression.",
  143:"Pursuing objects without the reflexive loss clause and generic self-loss do not independently carry the coordinated formula.",
  144:"South-north and middle-edge variants extend the same directional mobility, while isolated east or west motion is outside the article.",
  145:"The reversed lost-after formula is recorded as a deliberate turn on this phrase, not evidence for a second referent.",
  146:"Rabbit stories and stump references were separated from Chan deployments of the complete wait-at-the-stump proverb.",
  147:"Iron-eye and bronze-pupil components remain parts of one imagined organ; nearby golden-eye language was not merged into it.",
  148:"Shortened protection or killing clauses were checked separately; the article retains only the balanced received couplet.",
  149:"Fence, wall, and touching verbs occur independently, but only their coordinated groping scene supports this fixed entry.",
  150:"Acquaintance and knowing-heart vocabulary is common separately; only the balanced worldwide/few-person contrast belongs here.",
}

# Exact-turn decisions, made after reading the complete structural case.  Values are
# (actor type, label/canonical name, voice layer, proof, optional contextual masters).
A = {
 (141,0):("named-master","Yunmen Wenyan","direct-turn","The hall-address marker governs Yunmen's long silence and his exact four-graph verdict before he leaves the seat.",[]),
 (141,1):("named-master","Zhantang Wenzhun","direct-turn","After quoting Yaoshan and Yunyan, Zhantang addresses the assembly in his own voice and predicates the headword of both men.",[]),
 (141,2):("named-master","Sixin Wuxin","direct-turn","The surrounding address remains Sixin Wuxin's speech when he coordinates concealing people with the exact lethal-stultification verdict.",[]),
 (141,3):("identified-unlinked-master","Wenju Huiri","direct-turn","The Wenju Huiri section identifies the resident master, and the master-said marker immediately after the questioner's turn governs the headword answer.",[]),
 (141,4):("identified-unlinked-master","Fengxian Shen","direct-turn","The inline Fengxian Shen section begins before the opening ceremony, and its master-said marker assigns the verdict to Fengxian rather than the preceding Dongshan section.",[]),
 (141,5):("named-master","Yikui Yuankui","direct-turn","In Yikui Yuankui's own hall record, the master-said marker directly governs the headword answer to the dragon-jewel question.",[]),

 (142,0):("reviewed-unnamed","an unnamed questioning monk","question-turn","The headword is inside the monk's third marked question; the following master-said marker begins Chongfan Yu's separate answer.",[]),
 (142,1):("named-master","Xuedou Chongxian","direct-turn","The biography and section establish Xuedou Chongxian as the resident master, and the master-said marker directly governs his answer to the before-and-after question.",[]),
 (142,2):("identified-unlinked-master","Tiantai Deng","direct-turn","The Tiantai Deng section governs the exchange, and the final master-said marker assigns the headword and painlessness clause to Deng.",[]),
 (142,3):("identified-unlinked-master","Shilin Xinggong","direct-turn","The Chengtiang hall-address boundary remains inside Shilin Xinggong's section, so the headword-bearing staff clause is Xinggong's utterance.",[]),
 (142,4):("identified-unlinked-master","Jingju Liaowei","direct-turn","The passage names Jingju Liaowei before the exchange, and the second master-said marker governs the exact headword answer.",[]),
 (142,7):("named-master","Yingshan Zhiyin","direct-turn","The Xueguan record's Yingshan hall address assigns the three-phrase answers to Yingshan Zhiyin; the headword occurs in his answer on following the waves.",[]),

 (143,0):("named-master","Dahui Zonggao","direct-turn","The complete passage is Dahui Zonggao's general discourse, and the headword occurs in his continuous contrast between ordinary pursuit and the buddhas' description of suffering.",[]),
 (143,1):("named-master","Jingqing Daofu","quoted-original","Yuanwu raises Jingqing's raindrop exchange, and the explicit Qing-said marker assigns the headword clause to Jingqing Daofu.",[{"masterName":"Yuanwu Keqin","roles":["later-raiser"]}]),
 (143,3):("named-master","Baiyu Jingsi","direct-turn","The Baiyu record's hall-address boundary governs the description in which the headword precedes wandering through the three realms.",[]),
 (143,4):("narrated","the compiler of the Complete Five Lamps","compiler-narration","The headword stands in compiler exposition about people who follow things, not inside a locally marked master's turn.",[]),
 (143,5):("named-master","Jingqing Daofu","direct-turn","The Pointing at the Moon section names Jingqing Daofu, whose master-said marker follows the raindrop answer and governs the exact accusation.",[]),
 (143,7):("named-master","Baichi Xingyuan","direct-turn","The headword lies in Baichi Xingyuan's marked informal address and remains within his uninterrupted speech to the assembly.",[]),

 (144,0):("named-master","Linji Yixuan","direct-turn","Linji's address explicitly makes the directional pair part of his sequence of using circumstances without being turned by them.",[]),
 (144,2):("named-master","Yuan'an Liao","direct-turn","The Yuan'an record's hall address keeps the directional pair inside Yuan'an Liao's uninterrupted description of manifestations.",[]),
 (144,3):("identified-unlinked-master","Beita Siguang","direct-turn","The inline Beita Siguang section names the master, and the master-said marker directly governs his answer about a patch-robed monk's transformation.",[]),
 (144,4):("named-master","Kaiyuan Ying","direct-turn","Kaiyuan Ying's hall address begins before the paired suddenness clauses and continues through the headword into the skin-and-bone line.",[]),
 (144,6):("reviewed-unnamed","an unnamed questioning monk","question-turn","The exact phrase occurs in the monk's marked question; Shishuang Chuyuan's following master-said marker begins the one-word answer.",[{"masterName":"Shishuang Chuyuan","roles":["respondent"]}]),
 (144,7):("named-master","Hanyue Fazang","direct-turn","Hanyue Fazang repeats the east-west clause inside his own appraisal of the older staff saying in his extensive record.",[]),

 (145,0):("identified-unlinked-master","Shuiguan Shougui","direct-turn","The Shuiguan Shougui hall address uses the headword in the master's hypothetical challenge to the assembly.",[]),
 (145,2):("named-master","Danxia Tianran","direct-turn","In the Danxia exchange the master-said marker governs 'you forget before and lose after,' before Danxia reverses the pair for himself.",[]),
 (145,3):("identified-unlinked-master","Zifu Xian","direct-turn","The explicit Zifu Xian-said marker assigns the headword appraisal of Yaoshan to Xian within the later compilation.",[]),
 (145,5):("named-master","Benxi","direct-turn","The Benxi section's master-said marker governs the exact accusation before Benxi supplies the reversed self-description.",[]),
 (145,6):("narrated","the compiler of the Five Lamps Compendium","compiler-narration","The headword belongs to the numbered doctrinal question reproduced by the compiler, not to the surrounding section master as an uttered turn.",[]),
 (145,7):("identified-unlinked-master","Tianxian Cong","direct-turn","The Tianxian exchange explicitly has Tianxian say that he has forgotten before and lost after; later commentators remain outside that turn.",[]),

 (146,0):("named-master","Bajiao Huiqing","direct-turn","Bajiao Huiqing's instruction pairs stump-watching with hiding among reeds before quoting Jiashan's demand to wield the sword.",[]),
 (146,1):("identified-unlinked-master","Doushuai Zhien","direct-turn","The inline Doushuai Zhien hall-address section governs the statement that a true-colour monk does not wait at the stump.",[]),
 (146,2):("named-master","Touzi Yiqing","direct-turn","The Touzi Yiqing record keeps the phrase inside his continuous address contrasting expedient prescriptions with stump-watching.",[]),
 (146,4):("named-master","Letan Hongying","direct-turn","The Letan Hongying section's master-said marker directly governs the answer that waiting at a stump wastes the mind.",[]),
 (146,5):("named-master","Yuanwu Keqin","direct-turn","The headword occurs in Yuanwu Keqin's formal pointer, where it is paired with the ram caught against a fence.",[]),
 (146,7):("named-master","Zhuanyu Guanheng","direct-turn","The portrait-request text remains Zhuanyu Guanheng's authored description when it says the portrayed teacher can only wait at a stump.",[]),

 (147,0):("named-master","Mi'an Xianjie","direct-turn","The explicit Mi'an Xianjie-said marker assigns the entire four-line appraisal, including the headword, to Mi'an.",[]),
 (147,1):("named-master","Guxue Zhenzhe","direct-turn","The text names Guxue Zhenzhe at the start of the tomb address, whose uninterrupted speech says iron eyes cannot measure the displayed matter.",[]),
 (147,2):("identified-unlinked-master","Shangfang Riyi","direct-turn","The inline Anji Shangfang Riyi section precedes the opening assembly, and the master-said marker governs the headword warning.",[]),
 (147,4):("named-master","Eryin Mi","direct-turn","Eryin Mi introduces and verses the Xinghua case in his own record; the headword lies in Mi's closing verse.",[]),
 (147,5):("identified-unlinked-master","Tiantong Hua","direct-turn","The explicit Tiantong Hua-said marker assigns the numbered-seat appraisal and its iron-eye clause to Hua.",[]),
 (147,6):("named-master","Yinshan Can","direct-turn","The Yinshan Can record remains in Can's hall speech when he raises the staff and says iron eyes dare not look.",[]),

 (148,1):("named-master","Yue'an Shanguo","direct-turn","Yue'an Shanguo summons the assembly, speaks the complete couplet, and immediately judges it only halfway.",[]),
 (148,2):("named-master","Wuming Huijing","direct-turn","The complete couplet occurs in Wuming Huijing's uninterrupted informal address before his list of actions that are not required.",[]),
 (148,3):("named-master","Wanru Tongwei","question-turn","Wanru Tongwei repeats the questioner's couplet in his own subsequent hall address, where he asks what people mean by saying it.",[]),
 (148,4):("identified-non-master","Haian","question-turn","The explicit Haian-layman question marker governs the couplet and asks whether it means killing life or releasing life.",[]),
 (148,5):("named-master","Jie'an Jin","direct-turn","Jie'an Jin cites the old saying in his hall address while recounting Yunmen's blow and the claim of peace under heaven.",[]),
 (148,7):("named-master","Shengke Deyu","direct-turn","After rejecting an unnamed interlocutor's altered final clause, Shengke Deyu states the complete received couplet himself and later repeats it in his verse.",[]),

 (149,0):("named-master","Pu'an Yinsu","direct-turn","Pu'an Yinsu uses the fence-and-wall phrase in his own appraisal of Baizhang's test and Guishan's response.",[]),
 (149,2):("named-master","Mi'an Xianjie","direct-turn","Mi'an Xianjie's fourfold hall contrast assigns fence-and-wall groping to action that arrives without corresponding speech.",[]),
 (149,3):("named-master","Tian'an Sheng","direct-turn","Tian'an Sheng directly answers the questioner's boast about universal sheer standing by saying only that questioner gropes along fence and wall.",[]),
 (149,4):("identified-unlinked-master","Tianzhang","direct-turn","The explicit Tianzhang contrast rewrites Xuefeng's all-seeing claim as a question about groping in broad daylight.",[]),
 (149,5):("named-master","Xueyan Zuqin","direct-turn","Xueyan Zuqin contrasts opening one's own eyes and sitting in one's own house with following fences and walls at other people's doors.",[]),
 (149,6):("named-master","Shoushan Xingnian","direct-turn","The explicit Shoushan Xingnian-said marker assigns the appraisal of Guishan to Shoushan within the later case compilation.",[]),

 (150,0):("identified-unlinked-master","Dongchan Zhiguan","direct-turn","The explicit Dongchan Zhiguan-said marker assigns the closing acquaintance/heart-knower question to Zhiguan's appraisal.",[]),
 (150,1):("named-master","Daowu Yuanzhi","quoted-original","The raised saying is explicitly introduced as Daowu's words; the later master and Dahui appraise it in distinct following turns.",[]),
 (150,2):("identified-unlinked-master","Dongchan Zhiguan","direct-turn","The explicit Dongchan Zhiguan-said marker again governs the complete couplet in this independent compilation.",[]),
 (150,3):("named-master","Feiyin Tongrong","direct-turn","Feiyin Tongrong places the full couplet after his own question about the decisive location and closes the hall address with a shout.",[]),
 (150,4):("named-master","Daxiu Zhu","transmitted-verse","The headword occurs in Daxiu Zhu's four-line verse attached to the question about the twelve-faced Guanyin.",[]),
 (150,6):("named-master","Shiguan Xingling","direct-turn","During his entry into the monastery, Shiguan Xingling speaks the complete saying at the patriarch hall before asking whether the ancestors recognize their descendant.",[]),
 (150,5):("named-master","Daowu Yuanzhi","quoted-original","Yezhu Fuhui explicitly raises Daowu's old saying; the headword remains inside Daowu's quoted words before Yezhu's response.",[{"masterName":"Yezhu Fuhui","roles":["later-raiser"]}]),
}

def actor_obj(kind, label):
    if kind == "named-master":
        return {"type": kind, "masterName": label}
    role = "compiler" if kind == "narrated" else ("questioner" if kind in {"reviewed-unnamed", "identified-non-master"} else "utterer")
    obj = {"type": kind, "kind": "full-case exact-turn adjudication", "label": label,
           "role": role, "subject": label, "reviewedBy": "Codex Iriya lane B141-150 author",
           "reviewedUtc": now}
    return obj

def recut(text, needle, occurrence=0):
    starts=[]; at=0
    while True:
        i=text.find(needle,at)
        if i<0: break
        starts.append(i); at=i+len(needle)
    if not starts:
        # governed graphic variant used by queue position 146
        needle = needle.replace("兔","兎")
        i=text.find(needle)
        if i<0: raise ValueError((needle,text[:120]))
        starts=[i]
    i=starts[min(occurrence,len(starts)-1)]
    # Retain enough exact-turn grammar while keeping one governed span.
    lo=max(0,i-150); hi=min(len(text),i+len(needle)+180)
    for sep in "。！？":
        x=text.rfind(sep,lo,i)
        if x>=0: lo=max(lo,x+1)
    ends=[x for sep in "。！？" if (x:=text.find(sep,i+len(needle),hi))>=0]
    if ends: hi=min(ends)+1
    out=text[lo:hi]
    if out.count(needle)!=1:
        lo=max(0,i-80); hi=min(len(text),i+len(needle)+100); out=text[lo:hi]
    return out, needle

entries=[]
for pos in range(141,151):
    row=rows[pos]; preferred, opening, body=META[pos]; occ=[]
    for ci in SELECT[pos]:
        text=cases[(row["id"],ci)]
        nth=1 if (pos,ci)==(148,7) else 0
        kwic, clause=recut(text,row["searchTerm"],nth)
        kind,label,voice,proof,context=A[(pos,ci)]
        verify=zc.verify(row["candidateCases"][ci]["relPath"],kwic)
        if not verify.get("ok"):
            raise ValueError((pos,ci,verify))
        o={"caseIndex":ci,"fromLb":verify["fromLb"],"toLb":verify["toLb"],"kwic":kwic,"exactHeadwordClause":clause,
           "grammaticalProof":proof,"voiceLayer":voice,"speechMarkerReviewed":True,
           "speechMarkerAcknowledgement":"The complete case and every local speech boundary were read before this exact voice-layer decision.",
           "actor":actor_obj(kind,label)}
        if context: o["contextMasters"]=context
        if (pos,ci)==(150,1):
            o["contextActors"]=[{"type":"identified-unlinked-master","label":"Xiuyan Shirui","roles":["later-raiser"],"grammaticalProof":"Xiuyan Shirui raises Daowu's complete saying before appraising it in his own hall address."}]
            o["attributionContext"]="Xiuyan Shirui raises Daowu Yuanzhi's saying and then answers it in a separate later turn."
        if voice=="transmitted-verse":
            o["explicitVerseAttribution"]=True
        occ.append(o)
    note = {
      141:"The phrase is a verdict about ruinous handling of people; its killing graph does not create a separate homicide sense.",
      142:"Anatomical skulls and this fixed penetration image were checked; the retained cases all deploy the complete phrase in interviews or addresses.",
      143:"The two coordinated actions form one reversal and do not warrant separate senses for losing oneself and pursuing things.",
      144:"Directional variants extend one mobile image; east-west and south-north are not separate referents.",
      145:"The reversed companion formula, forgetting after and losing before, is a deliberate variation rather than another thing.",
      146:"Ordinary rabbit and agricultural uses are excluded; every witness here applies the complete proverb in Chan speech.",
      147:"The metal modifiers describe the imagined eyes, while the corpus repeatedly predicates their failure rather than a literal organ.",
      148:"The article reports the corpus's questions and exclusions without deciding what must be killed beyond what each cited speaker says.",
      149:"Literal contact with fences and walls supplies the scene; the retained cases use that scene for dependent, unseeing movement.",
      150:"The two clauses are one balanced saying; component-only acquaintance or heart-knowing uses do not establish this article.",
    }[pos]
    sense={"senseKey":None,"masterName":None,"preferredTarget":preferred,
      "alternateTargets":[],"searchAliases":[preferred],"status":"preferred","validation":"multi-source",
      "note":note,"explanationParts":{"corpusEarnedOpening":opening,"evidenceBody":[body]},
      "relatedMasters":[],"relatedTerms":[],"occurrences":occ,
      "draftEvidence":{"OpeningClaimEvidenceKeys":[f"o{i+1}" for i in range(len(occ))],"ZenBend":body,
        "CounterexampleOrLimit":note,
        "DifferentThingTest":{"Decision":"one-thing","ComparedThings":[preferred,"the retained interview, address, and commentary deployments"],"Reason":note},
        "AliasRationale":ALIAS[pos],
        "ModifierControls":[{"Form":"the complete source expression","Finding":note}],
        "FamilyControls":[{"Term":"component and variant forms","Finding":FAMILY[pos]}]}}
    entries.append({"id":row["id"],"createdBy":"Codex Iriya lane B141-150 full-case author","writtenUtc":now,"senses":[sense]})

out={"schemaVersion":"iriya-compact-decisions-v1","entries":entries}
(M/"iriya-decisions-B141-150.json").write_text(json.dumps(out,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print(f"wrote {len(entries)} entries and {sum(len(e['senses'][0]['occurrences']) for e in entries)} occurrences")
