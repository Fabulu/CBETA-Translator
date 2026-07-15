#!/usr/bin/env python3
import json
from pathlib import Path
root=Path(__file__).resolve().parents[1]; p=root/'entries/t_d0e5e6e5d1ee/evidence.draft.json';d=json.loads(p.read_text());e=d['Entry'];s=e['Senses'][0]
e['CreatedBy']='Codex f004 lane A manual full-case repair author'
s['PreferredTarget']='a turning word'
s['AlternateTargets']=['a word that turns the encounter','one turning phrase']
s['SearchAliases']=['turning word','turning phrase','decisive response']
s['Note']='A response requested or supplied at the point where an encounter is stuck; it turns the inherited case, exposes the speaker, or closes the exchange without becoming a reusable formula.'
actors=[('Tianyi Huai',None),('Tianyi Huai',None),(None,'an unnamed old man in the wild-fox case'),('Xiangcheng Shun',None),('Tianyi Huai',None),('Tianyi Huai',None),('Fayan Wenyi',None)]
titles=['宗門拈古彙集','宗鑑法林','古尊宿語錄','五燈全書','指月錄','宗門統要正續集','五燈嚴統']
for o,(name,unnamed),title in zip(s['Occurrences'],actors,titles):
 o['MasterName']=name
 if name:
  o['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}]
  o.pop('ActorAttribution',None)
  who=name
  o['AttributionNote']=f'Source text ({title}): {name} utters the exact headword in the reviewed complete case.'
 else:
  o['ContextMasters']=[{'MasterName':'Baizhang Huaihai','Roles':['respondent','record-owner']}]
  o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'unnamed case figure','ActorLabel':unnamed,'ActorRole':'questioner','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':'The old man explicitly asks Baizhang to supply 一轉語; Baizhang answers 不昧因果.','ReviewedBy':'Codex f004 lane A manual full-case repair author','ReviewedUtc':'2026-07-15T12:45:00Z','AuthoredVoiceRiskReviewed':True}
  who=unnamed;o['AttributionNote']=f'Source text ({title}): {unnamed} asks Baizhang Huaihai to supply 一轉語; Baizhang is the respondent.'
 pr=o.setdefault('DraftActorProof',{});pr['GrammaticalSubject']=who;pr['FullCaseDecision']=o['AttributionNote'];pr['SpeechFrame']=f'The complete case assigns the headword-bearing clause to {who}.'
s['RelatedMasters']=['Tianyi Huai','Baizhang Huaihai','Xiangcheng Shun','Fayan Wenyi']
s['ExplanationParts']={'CorpusEarnedOpening':'一轉語 is a word that turns an encounter at the point where its inherited answer, question, or impasse can no longer simply be repeated.','EvidenceBody':['In the wild-fox case, an old man asks Baizhang Huaihai to supply one; Baizhang’s 不昧因果 changes the answer on which the whole case turns. The parallel records repeat that same case family and are not seven independent inventions of the phrase.','Tianyi Huai asks for a turning word on behalf of the Buddha when the demon king’s vow leaves the usual contrast of Buddha and demon unusable; later commentators test proposed replies rather than treating the label as praise by itself.','Xiangcheng Shun says Sheng’s single turning word won him Huangbo but had not yet reached the teaching, while Fayan Wenyi asks what a filial child should say and supplies a reply. A turning word can close or redirect an exchange, but possessing one successful line does not certify understanding.']}
s['DraftEvidence']={'OpeningClaimEvidenceKeys':['o1','o2','o3','o4','o5','o6','o7'],'ZenBend':'The records ask for a word precisely where a case has jammed; its force lies in changing the live relation among question, answer, and speaker, not in furnishing a portable slogan.','CounterexampleOrLimit':'Xiangcheng Shun explicitly separates giving one successful turning word from understanding the teaching; repeated demon-king and wild-fox witnesses are parallel transmissions, not independent senses.','DifferentThingTest':{'Decision':'one-thing','ComparedThings':['a requested turning word','a supplied turning word'],'Reason':'Requesting and supplying describe opposite sides of the same encounter-function, not different referents.'},'AliasRationale':'English aliases preserve the verbal force of 轉 and the encounter function of 語.','ModifierControls':[{'finding':'controlled','reason':'Parallel wild-fox and demon-king transmissions are grouped as case families.'}],'FamilyControls':[{'finding':'controlled','reason':'Successful replies, requests for replies, and critical judgments all concern the same turning-word function.'}],'IndependentWorkIds':s['DraftEvidence']['IndependentWorkIds']}
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
