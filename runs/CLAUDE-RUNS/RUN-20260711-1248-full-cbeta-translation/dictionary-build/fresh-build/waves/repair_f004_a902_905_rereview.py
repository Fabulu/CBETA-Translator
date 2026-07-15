#!/usr/bin/env python3
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]

def load(i):
    p=ROOT/'entries'/i/'evidence.draft.json'; return p,json.loads(p.read_text(encoding='utf-8'))
def save(p,d): p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def impersonal(o,label,note):
    o['MasterName']=None
    o['ActorAttribution']={'Status':'impersonal','Kind':'editorial heading','ActorLabel':label,'ActorRole':'compiler','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':note,'ReviewedBy':'Codex f004 lane A independent-rereview repair','ReviewedUtc':'2026-07-15T12:20:00Z','AuthoredVoiceRiskReviewed':True}

# 902 香雲
p,d=load('t_77811c966dba'); s=d['Entry']['Senses'][1]; o=s['Occurrences'][0]
o['MasterName']=None
o['ContextMasters']=[{'MasterName':'Meixi Fudu','Roles':['respondent','record-owner']}]
o['AttributionNote']='Source text (東山梅溪度禪師語錄): an unnamed monk asks with 香雲結蓋; Meixi Fudu responds 萬里青山展壽圖.'
o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'unnamed monk questioner','ActorLabel':'an unnamed monk asking the formal question','ActorRole':'questioner','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':'The complete exchange assigns 香雲結蓋 to 問 and Meixi Fudu’s reply to 師云.','ReviewedBy':'Codex f004 lane A independent-rereview repair','ReviewedUtc':'2026-07-15T12:20:00Z','AuthoredVoiceRiskReviewed':True}
o['DraftActorProof']['GrammaticalSubject']='unnamed monk questioner';o['DraftActorProof']['FullCaseDecision']='The unnamed monk utters 香雲; Meixi Fudu is the respondent.'
s['ExplanationParts']['EvidenceBody']=[x.replace("Meixi Fudu's address makes the cloud a canopy", "An unnamed monk’s question makes the fragrant cloud a canopy; Meixi Fudu answers with the image of ten thousand miles of green mountains") for x in s['ExplanationParts']['EvidenceBody']]
o=s['Occurrences'][3]
o['Kwic']='所生一切寶蓮華雲。一切堅固香雲。一切無邊色華雲。一切種種色妙衣雲。一切無邊清淨栴檀香雲。一切妙莊嚴寶蓋雲。一切燒香雲。一切妙鬘雲。一切清淨莊嚴具雲。皆遍法界。出過諸天。供養之具。供養於佛。'
o['FromLb']='0535a11';o['ToLb']='0535a16'
o['AttributionNote']='Source text (宗鏡錄): the compiler narrates a list of 香雲 among clouds of offerings that fill the dharma-realm; the expanded KWIC preserves the complete list context.'
save(p,d)

# 903 頂門正眼
p,d=load('t_602a0b760189');o=d['Entry']['Senses'][0]['Occurrences'][3]
o['ContextMasters']=[{'MasterName':'Baichi Yuanshuo','Roles':['respondent','record-owner']}]
o['AttributionNote']='Source text (百癡禪師語錄): an unnamed monk utters the headword in a question; Baichi Yuanshuo is the recorded respondent.'
save(p,d)

# 904 知事: two occasion headings are impersonal.
p,d=load('t_f5e1fe96407c'); os=d['Entry']['Senses'][0]['Occurrences']
impersonal(os[3],'editorial request-to-administrators occasion heading','請東序知事提綱 / 請知事，上堂 is an occasion heading; no human utters the headword.')
os[3]['ContextMasters']=[];os[3]['AttributionNote']='Source text (列祖提綱錄): 知事 occurs in the impersonal editorial occasion heading 請東序知事提綱 / 請知事，上堂.'
impersonal(os[6],'editorial thanks-to-administrators occasion heading','謝新舊知事，小參 is an occasion heading; no human utters the headword.')
os[6]['ContextMasters']=[{'MasterName':'Furong Daokai','Roles':['section-subject']}];os[6]['AttributionNote']='Source text (續古尊宿語要): 知事 occurs in the impersonal occasion heading 謝新舊知事，小參; Furong Daokai remains the section master, not the utterer.'
save(p,d)

# 905 續傳燈錄: title witnesses impersonal; signed Wenxiu preface canonicalized.
p,d=load('t_1d8554f83698');os=d['Entry']['Senses'][0]['Occurrences']
impersonal(os[0],'bibliographic title heading','The headword is the work title in a catalogue/title heading, with no human utterer.')
os[0]['AttributionNote']='Source text (續傳燈錄): the headword occurs as an impersonal bibliographic title heading.'
impersonal(os[1],'bibliographic title heading','The headword is an expanded work-title heading, with no human utterer.')
os[1]['AttributionNote']='Source text (增集續傳燈錄): the headword occurs in the impersonal expanded-work title heading.'
o=os[5];o.pop('ActorAttribution',None);o['MasterName']='Nanshi Wenxiu';o['ContextMasters']=[{'MasterName':'Nanshi Wenxiu','Roles':['utterer']}]
o['AttributionNote']='Source text (增集續傳燈錄): Nanshi Wenxiu, identified by the signature 永樂十五年三月徑山禪寺前住持比丘文琇書, utters the headword in the signed preface voice.'
o['DraftActorProof']['GrammaticalSubject']='Nanshi Wenxiu';o['DraftActorProof']['FullCaseDecision']='The signed former-abbot monk Wenxiu is canonical Nanshi Wenxiu and is the headword utterer.'
o['DraftActorProof']['SpeechFrame']='The dated signature 徑山禪寺前住持比丘文琇書 assigns the preface voice to Nanshi Wenxiu.'
save(p,d)
