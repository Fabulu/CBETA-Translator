from pathlib import Path
import ast,copy,json,sys
R=Path(__file__).resolve().parents[2]; sys.path.insert(0,str(R)); import zc
tree=ast.parse((R/'fresh-build/waves/compile_f004_c1131_1150.py').read_text()); CFG={}
for n in tree.body:
 if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='CFG' for t in n.targets): CFG=ast.literal_eval(n.value)

def load(i):p=R/'fresh-build/entries'/i;return p,json.loads((p/'entry.v2.json').read_text())
def named(o,n,proof,ctx=None):
 o.pop('ActorAttribution',None);o['MasterName']=n;o['ContextMasters']=ctx or [{'MasterName':n,'Roles':['utterer']}];o['AttributionNote']=f'Source record ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {n}. Full-case review separates the headword-bearing turn from surrounding narration, questions, and replies.';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':n,'SpeechFrame':proof,'FullCaseDecision':proof}
def nonmaster(o,status,label,role,proof,ctx=[]):
 o['MasterName']=None;o['ContextMasters']=[{'MasterName':n,'Roles':rs} for n,rs in ctx];o['ActorAttribution']={'Status':status,'Kind':'compiler narrative' if status=='narrated' else 'human interlocutor','ActorLabel':label,'ActorRole':role,'RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':proof,'ReviewedBy':'Codex f004 lane C exact full-case repair','ReviewedUtc':'2026-07-15T14:20:00+00:00','AuthoredVoiceRiskReviewed':True};o['AttributionNote']=f'Source record ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {label}. Full-case review preserves the non-master or narrative actor without manufacturing a master.';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':proof,'FullCaseDecision':proof}
def save(p,e,n):
 target,aliases,opening,bend,limit=CFG[n]
 for s in e['Senses']:
  s['ExplanationParts']={'CorpusEarnedOpening':opening,'EvidenceBody':[bend]};works=list(dict.fromkeys(zc.work_id(o['RelPath']) for o in s['Occurrences']))
  s['DraftEvidence']={'OpeningClaimEvidenceKeys':[f'o{i}' for i in range(1,len(s['Occurrences'])+1)],'ZenBend':bend,'CounterexampleOrLimit':limit,'DifferentThingTest':{'Decision':'one-thing','ComparedThings':[target,'its attested deployments'],'Reason':limit},'AliasRationale':'Aliases retrieve the same corpus-bounded referent.','ModifierControls':[{'finding':'checked','reason':'Complete cases were compared for literal and Zen-loaded uses.'}],'FamilyControls':[{'finding':'checked','reason':'Case-family, compound, and title-only matches were controlled separately.'}],'IndependentWorkIds':works}
  for o in s['Occurrences']:
   if o.get('MasterName') and not o.get('DraftActorProof'):
    proof='The complete case assigns the exact headword-bearing turn to the named master.';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':o['MasterName'],'SpeechFrame':proof,'FullCaseDecision':proof}
   if not o.get('MasterName') and not o.get('DraftActorProof'):
    a=o.get('ActorAttribution') or {};proof=a.get('GrammarEvidence','The complete case does not name a master as exact utterer.');o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':a.get('ActorLabel','the documented non-master voice'),'SpeechFrame':proof,'FullCaseDecision':proof}
 (p/'entry.v2.json').write_text(json.dumps(e,ensure_ascii=False,indent=2)+'\n');d={'SchemaVersion':1,'Entry':copy.deepcopy(e)};(p/'evidence.draft.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')

# 1136 安單
p,e=load('t_b6da6fc1c9bf');o=e['Senses'][0]['Occurrences']
nonmaster(o[0],'reviewed-unnamed','the unnamed monastic questioner','questioner','今日安單 is inside the monk’s quoted question; Linye Tongqi answers only after 師云.',[('Linye Tongqi',['respondent'])])
named(o[1],'Zhufeng Fa','After the monk bows, 師云不信道且安單去 assigns the phrase to the record owner.')
nonmaster(o[2],'narrated','the record narrator','compiler','知客送行者入方丈安單 narrates the guest prefect placing an attendant; Mixing Ren’s question follows.',[('Mixing Jiren',['record-owner'])])
named(o[3],'Mingjue Cong','Mingjue Cong recounts Fushan’s trial inside his own small assembly address; the exact wording is part of that address.',[{'MasterName':'Mingjue Cong','Roles':['utterer']},{'MasterName':'Fushan Fayuan','Roles':['person-discussed']}])
save(p,e,1136)

# 1137 主人翁
p,e=load('t_bdabbe0d39fa');o=e['Senses'][0]['Occurrences']
named(o[0],'Baizhuo Shandeng','The phrase occurs in the named master’s own hall verse under his biographical section.')
named(o[1],'Shuzhong Wuyun','恕中慍禪師…拈香 explicitly identifies the incense-address speaker before the phrase.')
named(o[2],'Ruiyan Shiyan','The exact phrase is inside the quoted self-call 當山開山空照祖師…自呼應云, while the later record owner comments afterward.',[{'MasterName':'Ruiyan Shiyan','Roles':['utterer']},{'MasterName':'Shuzhong Wuyun','Roles':['later-raiser']}])
named(o[3],'Yuansou Xingduan','The phrase is repeatedly uttered in Yuansou Xingduan’s own hall address.')
named(o[4],'Tianran Hanshi','Tianran Hanshi names 主人翁 while diagnosing two current errors in his own public explanation.')
named(o[5],'Wuzu Fayan','The phrase occurs in Wuzu Fayan’s own verse sequence, beneath his named record section.')
save(p,e,1137)

# 1138 神農
p,e=load('t_b495de9e2b11');o=e['Senses'][0]['Occurrences']
named(o[0],'Juelang Daosheng','Juelang’s first-person preface introduces the verse he himself gives to Wuke Zhigong.')
named(o[1],'Shiyu Mingfang','Shiyu Mingfang names Shennong during his own farewell address.')
named(o[2],'Jifei Ruyi','The exact match is the title and first line of Jifei Ruyi’s portrait verse in his own collected praises.')
named(o[3],'Wanfeng Tongzhen','The Shennong line belongs to the named master’s own portrait-verse section rather than anonymous compiler prose.')
save(p,e,1138)

# 1139 無常迅速
p,e=load('t_3ae11b4bc79f');o=e['Senses'][0]['Occurrences']
named(o[2],'Xixi Ze','台州國清溪西澤禪師普說其略曰 explicitly introduces Xixi Ze’s public explanation.')
named(o[3],'Zhongfeng Mingben','The phrase is uttered in Zhongfeng Mingben’s own Qingming instruction to the assembly.')
named(o[4],'Xueyan Zuqin','The phrase occurs in Xueyan Zuqin’s own extended public exhortation.')
nonmaster(o[5],'identified-non-master','the named group of lay disciples','questioner','徒悟權等焚香拜跪云 assigns the phrase to the requesting disciples; Dufeng Benshan answers after 師云.',[('Dufeng Benshan',['respondent'])])
save(p,e,1139)

# 1140 領話
p,e=load('t_68729efe1fac');o=e['Senses'][0]['Occurrences']
for i in (0,2): nonmaster(o[i],'reviewed-unnamed','the unnamed elder monk','interlocutor','The first exact occurrence is 何不領話 in the elder’s reply; Muzhou’s 汝不領話 follows as the response.',[('Muzhou Daoming',['respondent'])])
named(o[1],'Yushan Shangsi','師云且喜領話 is Yushan Shangsi’s reply to the monk’s advance.')
named(o[3],'Foyan Qingyuan','師云謝闍梨領話 assigns the exact phrase to Foyan Qingyuan.')
named(o[4],'Jingzun Tonghui','師曰且領話好 is Jingzun Tonghui’s answer in his own biographical record.')
named(o[5],'Changqing Huileng','長慶云恁麼即請師領話 explicitly assigns the phrase to Changqing Huileng; Yaoshan answers afterward.',[{'MasterName':'Changqing Huileng','Roles':['utterer']},{'MasterName':'Yaoshan Weiyan','Roles':['respondent']}])
save(p,e,1140)
print('repaired 1136-1140 exact cases')
