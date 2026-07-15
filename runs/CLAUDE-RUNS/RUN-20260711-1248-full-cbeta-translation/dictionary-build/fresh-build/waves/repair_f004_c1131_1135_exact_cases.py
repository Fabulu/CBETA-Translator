from pathlib import Path
import copy, json

R = Path(__file__).resolve().parents[2]
DEPTH={
'接物利生':('Receiving people and benefiting living beings is the public work of meeting those who come and responding in a way intended to help them.','The phrase appears in questions, biographies, and addresses where solitary understanding is contrasted with going out to meet people.','It names attested public engagement, not a promise that every response produces benefit.'),
'橫按拄杖':('Holding the staff crosswise is a visible teaching-seat action that interrupts speech and presents the staff as the immediate response.','Masters hold it across the body before challenging the assembly, answering a three-phrase question, or issuing a verse-like line.','The action is not collapsed into a universal symbolic meaning for every staff.'),
'雲巖掃地':('Yunyan sweeping the ground is the named case in which Daowu calls the work overly busy and Yunyan raises the broom to test whether there is a second moon.','Later masters raise, verse, and comment on the complete broom exchange, including Xuansha’s and Yunmen’s appended judgments.','The headword names the whole case, not a doctrine extracted from one reply.'),
'體露金風':('The body exposed in the golden wind is Yunmen’s compact answer to what remains when the trees are bare and the leaves have fallen.','Later speakers quote, verse, criticize, and re-answer the line as a public case phrase while retaining its autumn scene.','Golden wind is the autumn wind in these stored cases; no free-standing symbolism is added.'),
'不放過':('Not letting it pass is to press an encounter instead of granting an opening, dismissal, or unchallenged answer.','The records contrast letting someone pass with squeezing, striking, or continuing the questioning turn.','The phrase describes the attested interview action and does not decide whether pressure is always warranted.')}

def load(i):
    p = R/'fresh-build/entries'/i
    return p, json.loads((p/'entry.v2.json').read_text())

def save(p,e):
    import sys; sys.path.insert(0,str(R)); import zc
    for s in e['Senses']:
        opening,bend,limit=DEPTH[e['SourceTerm']]
        s['ExplanationParts']={'CorpusEarnedOpening':opening,'EvidenceBody':[bend]}
        works=list(dict.fromkeys(zc.work_id(o['RelPath']) for o in s['Occurrences']))
        s['DraftEvidence']={'OpeningClaimEvidenceKeys':[f'o{i}' for i in range(1,len(s['Occurrences'])+1)],'ZenBend':bend,'CounterexampleOrLimit':limit,'DifferentThingTest':{'Decision':'one-thing','ComparedThings':[s['PreferredTarget'],'its attested deployments'],'Reason':limit},'AliasRationale':'The aliases retrieve the same corpus-bounded referent.','ModifierControls':[{'finding':'checked','reason':'Literal, material, and Zen-loaded readings were compared against the stored full cases.'}],'FamilyControls':[{'finding':'checked','reason':'Case-family, compound, and title-only matches were controlled separately.'}],'IndependentWorkIds':works}
        for o in s['Occurrences']:
            if o.get('MasterName') and not o.get('DraftActorProof'):
                proof='The complete case assigns the exact headword-bearing turn to the named master.'
                o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':o['MasterName'],'SpeechFrame':proof,'FullCaseDecision':proof}
            if not o.get('MasterName') and not o.get('DraftActorProof'):
                a=o.get('ActorAttribution') or {}; proof=a.get('GrammarEvidence','The complete case was read and does not assign the exact headword to a named master.')
                o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':a.get('ActorLabel','the documented non-master voice'),'SpeechFrame':proof,'FullCaseDecision':proof}
    (p/'entry.v2.json').write_text(json.dumps(e,ensure_ascii=False,indent=2)+'\n')
    d=json.loads((p/'evidence.draft.json').read_text()); d['Entry']=copy.deepcopy(e)
    (p/'evidence.draft.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')

def named(o,n,proof,ctx=None):
    o.pop('ActorAttribution',None); o['MasterName']=n
    o['ContextMasters']=ctx or [{'MasterName':n,'Roles':['utterer']}]
    import sys;sys.path.insert(0,str(R));import zc
    o['AttributionNote']=f'Source record ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {n}. Full-case review separates the headword-bearing turn from surrounding narration, questions, and replies.'
    o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':n,'SpeechFrame':proof,'FullCaseDecision':proof}

def narrated(o,label,proof,ctx):
    o['MasterName']=None
    o['ContextMasters']=[{'MasterName':n,'Roles':roles} for n,roles in ctx]
    o['ActorAttribution']={'Status':'narrated','Kind':'compiler narrative','ActorLabel':label,'ActorRole':'compiler','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':proof,'ReviewedBy':'Codex f004 lane C exact full-case repair','ReviewedUtc':'2026-07-15T14:00:00+00:00','AuthoredVoiceRiskReviewed':True}
    import sys;sys.path.insert(0,str(R));import zc
    o['AttributionNote']=f'Source record ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {label}. Full-case review preserves narration and does not manufacture a quoted speaker.'
    o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':proof,'FullCaseDecision':proof}

# 1131 接物利生: distinguish Xuansha's quotation, a monk's question,
# record-owner addresses, and the Guishan biography's narrated thought.
p,e=load('t_edfd0b2afa11'); os=e['Senses'][0]['Occurrences']
named(os[0],'Xuansha Shibei','舉玄沙示眾云 explicitly introduces Xuansha before the clause.')
narrated(os[3],'the lamp-record biographer','一日念道在接物利生 reports Guishan’s deliberation in biography rather than quoting a spoken turn.',[('Guishan Lingyou',['person-described'])])
named(os[4],'Feiyin Tongrong','The headword occurs in Feiyin Tongrong’s own 上堂, inside the speech introduced by 乃云.')
named(os[5],'Konggu Daocheng','The headword occurs in Konggu Daocheng’s own 上堂 after 乃云 and before his concluding staff-and-shout actions.')
named(os[6],'Tianyin Yuanxiu','The headword occurs in Tianyin Yuanxiu’s direct instruction to the laywomen introduced by 師云.')
save(p,e)

# 1132 橫按拄杖: the words are narration of visible acts, so performers are
# contextual masters and never falsely promoted to MasterName.
p,e=load('t_e251ef5cbc12'); os=e['Senses'][0]['Occurrences']
narrated(os[0],'the lamp-record narrator','乃橫按拄杖 narrates Foyin Liaoyuan’s teaching-seat action; it is not dialogue.',[('Foyin Liaoyuan',['person-described'])])
for i in (1,2): narrated(os[i],'the lamp-record narrator','上堂橫按拄杖 narrates Qinshan Wensui’s action before his spoken challenge.',[('Qinshan Wensui',['person-described'])])
narrated(os[3],'the lamp-record narrator','師橫按拄杖 answers the second question by narrating Rifang Shangzuo’s bodily action.',[('Rifang Shangzuo',['person-described'])])
save(p,e)

# 1133 雲巖掃地: later masters utter the case-label when raising it; embedded
# historical figures retain separate roles. The collected verse heading remains paratext.
p,e=load('t_68fbf8a2329c'); os=e['Senses'][0]['Occurrences']; embedded=[('Yunyan Tansheng',['case-figure']),('Daowu Yuanzhi',['case-figure'])]
named(os[0],'Hongzhi Zhengjue','In Hongzhi’s record, 舉 marks his public raising of the case label.',[{'MasterName':'Hongzhi Zhengjue','Roles':['utterer']}]+[{'MasterName':n,'Roles':r} for n,r in embedded])
narrated(os[1],'the collected-record editor','The first exact match is the case heading 雲巖掃地 before the transcribed exchange and verse.',embedded)
named(os[2],'Yongjue Yuanxian','In Yongjue’s 拈古, 舉 marks his public raising of the case label.',[{'MasterName':'Yongjue Yuanxian','Roles':['utterer']}]+[{'MasterName':n,'Roles':r} for n,r in embedded])
named(os[3],'Yunmen Wenyan','In Yunmen’s 室中語要, 舉 marks Yunmen’s public raising of the case label before his judgment.',[{'MasterName':'Yunmen Wenyan','Roles':['utterer']}]+[{'MasterName':n,'Roles':r} for n,r in embedded])
save(p,e)

# 1134 體露金風: Yunmen is the historical utterer only where the exact phrase
# lies in his answer; the later anthology verse remains an authored but unresolved verse.
p,e=load('t_47b3313788e2'); os=e['Senses'][0]['Occurrences']
for i in (0,1,3,4,6): named(os[i],'Yunmen Wenyan','The complete question-answer explicitly gives the phrase as Yunmen’s answer to the falling-leaves question.')
save(p,e)

# 1135 不放過: resolve record-owned speeches and preserve historical quoted speakers.
p,e=load('t_4c1e5a42155d'); os=e['Senses'][0]['Occurrences']
named(os[0],'Cian Jingyuan','The phrase is spoken in Cian Jingyuan’s own bathing-the-awakened-one hall address; the quoted Yunmen episode is context, not this exact turn.',[{'MasterName':'Cian Jingyuan','Roles':['utterer']},{'MasterName':'Yunmen Wenyan','Roles':['case-figure']}])
named(os[1],'Huangbo Xiyun','師一日捏拳謂眾 introduces Huangbo’s own fist demonstration and conditional wording.')
named(os[5],'Lumen Chuzhen','The monk’s reply ends before 師曰若不放過; Lumen Chuzhen utters the exact phrase in his answer.')
named(os[6],'Yunmen Wenyan','The phrase occurs in Yunmen’s own 室中語要 after 師示眾云.')
save(p,e)
print('repaired 1131-1135 by complete-case actor reading')
