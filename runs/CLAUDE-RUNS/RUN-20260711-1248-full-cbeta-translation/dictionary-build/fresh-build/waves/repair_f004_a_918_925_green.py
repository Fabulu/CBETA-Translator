#!/usr/bin/env python3
import json, subprocess, sys
from pathlib import Path
HERE=Path(__file__).resolve().parent;R=HERE.parent.parent
sys.path.insert(0,str(R));import zc
IDS=['t_dd5f8d8801d2','t_09cbe12e4c36','t_bdc0cdca39d0','t_72ed81907d68','t_c9f69715e823','t_1c236703f164','t_c02887fbd979','t_94f424853f5b']

def load(i):
 p=R/'fresh-build/entries'/i/'evidence.draft.json';return p,json.loads(p.read_text())
def actor_text(o):
 if o.get('MasterName'): return o['MasterName']
 return (o.get('ActorAttribution') or {}).get('ActorLabel') or 'the documentary voice'
def normalize_notes(e):
 for s in e['Senses']:
  # ContextMasters links only roster masters; lay people and book compilers stay in prose.
  for o in s['Occurrences']:
   o['ContextMasters']=[c for c in o.get('ContextMasters',[]) if c['MasterName'] not in {'Lu Gen','Dachuan Puji'}]
   title=zc.title(o['RelPath']);old=o.get('AttributionNote','')
   body=old.split('. ',1)[1] if '. ' in old else old
   o['AttributionNote']=f"Source text ({title}). {actor_text(o)}: {body}"
   if o.get('DraftActorProof'): o['DraftActorProof']['FullCaseDecision']=o['AttributionNote']
def save(i,p,d):
 e=d['Entry'];normalize_notes(e);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
 out=p.parent/'entry.v2.json';rep=p.parent/'compile-report.json';subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(out),'--report',str(rep)],check=True)

# Exact canonical/pending-roster names.
p,d=load('t_dd5f8d8801d2');e=d['Entry'];s=e['Senses'][0];
for o in s['Occurrences']:
 if o.get('MasterName')=='Jichuang Youzhao': o['MasterName']='Tiantong Zongjue';o['ContextMasters'][0]['MasterName']='Tiantong Zongjue'
 for c in o.get('ContextMasters',[]):
  if c['MasterName']=='Jichuang Youzhao':c['MasterName']='Tiantong Zongjue'
  if c['MasterName']=='Yanguan Qi’an':c['MasterName']="Yanguan Qi'an"
save('t_dd5f8d8801d2',p,d)

p,d=load('t_bdc0cdca39d0');e=d['Entry'];s=e['Senses'][0];o=s['Occurrences'];o[0]['MasterName']="Lia'an Qingyu";o[0]['ContextMasters'][0]['MasterName']="Lia'an Qingyu";o[2]['ContextMasters'][0]['MasterName']='Guxue Zhenzhe';o[3]['MasterName']="Zhe'an Jingfan";o[3]['ContextMasters'][0]['MasterName']="Zhe'an Jingfan";o[2]['AttributionNote']=o[2]['AttributionNote'].replace('repeated 進云 frames','repeated continuation-question frames');save('t_bdc0cdca39d0',p,d)

p,d=load('t_72ed81907d68');e=d['Entry'];s=e['Senses'][0];o=s['Occurrences'];s['Explanation']=s['Explanation'].replace('施為 means','Conduct and activity mean');s['ExplanationParts']['CorpusEarnedOpening']=s['ExplanationParts']['CorpusEarnedOpening'].replace('施為 means','Conduct and activity mean');o[2].pop('ActorAttribution',None);o[2]['MasterName']='Mazu Daoyi';o[2]['ContextMasters']=[{'MasterName':'Mazu Daoyi','Roles':['utterer']}];o[2]['AttributionNote']='Source text (古尊宿語錄). Mazu Daoyi: Mazu says that dressing, eating, speaking, responding, and every activity are the nature of things.';o[3]['MasterName']="Zhe'an Jingfan";o[3]['ContextMasters'][0]['MasterName']="Zhe'an Jingfan";o[6].pop('ActorAttribution',None);o[6]['MasterName']='Yuanwu Keqin';o[6]['ContextMasters']=[{'MasterName':'Yuanwu Keqin','Roles':['utterer']}];o[6]['AttributionNote']='Source text (列祖提綱錄). Yuanwu Keqin: Yuanwu says that all the prince’s conduct is illuminated at the mind-source.';save('t_72ed81907d68',p,d)

p,d=load('t_c02887fbd979');e=d['Entry'];s=e['Senses'][0];o=s['Occurrences'];
new={'RelPath':'J/J26/J26nB188.xml','FromLb':'0757a01','ToLb':'0757a09','Kwic':'步步彌勒下生。」良久云：「鳥啼花發笑，雲興宇宙清。今日是湖城錢開府老夫人及眾比丘尼送法衣上山，設齋修福延生，請山僧陞座，為眾結般若緣。且道般若一句如何舉揚？夏初日漸暖，行人盡解襟。」後堂素朴問：「高提祖印，最上一乘，弘範毘尼，一乘最上。今日和尚二法並談，施主功德還有邊際也無？」師云：「弁石播苔痕。」進云：「青龍峰頂獅子吼，百千群品證無生。」便禮拜。師云：「非為分外。」遂下座。上堂。崇禎甲戌年十月望日，','Curated':True,'ContextMasters':[],'ActorAttribution':{'Status':'identified-non-master','Kind':'named rear-hall officer','ActorLabel':'the rear-hall officer Supu','ActorRole':'questioner','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':'The complete case explicitly introduces 後堂素朴問; Supu owns the headword-bearing question and the master owns only the reply.','ReviewedBy':'Codex f004 A918-925 green repair','ReviewedUtc':'2026-07-15T17:20:00Z','AuthoredVoiceRiskReviewed':True},'AttributionNote':'Source text (入就瑞白禪師語錄). the rear-hall officer Supu: Supu asks about raising the patriarchal seal while the master discusses the highest vehicle and the monastic code.','DraftActorProof':{'ExactHeadwordClause':'後堂素朴問：「高提祖印，最上一乘，弘範毘尼，一乘最上。','GrammaticalSubject':'the rear-hall officer Supu','SpeechFrame':'後堂素朴問 explicitly names the questioner.','FullCaseDecision':'Supu utters the headword; the master replies afterward.'}}
assert zc.verify(new['RelPath'],new['Kwic'])['ok'];o.append(new);s['Note']='7 full-case witnesses distinguish speech, questions, narration, and documentary uses.';save('t_c02887fbd979',p,d)

p,d=load('t_94f424853f5b');e=d['Entry'];person=e['Senses'][0];o=person['Occurrences'];o[4].pop('ActorAttribution',None);o[4]['MasterName']='Yanju Deshen';o[4]['ContextMasters']=[{'MasterName':'Yanju Deshen','Roles':['utterer']}];o[4]['AttributionNote']='Source text (雲山燕居申禪師語錄). Yanju Deshen: Yanju tells Layman Liu Wanbi to begin with Mencius’s “neither assist nor forget.”';save('t_94f424853f5b',p,d)

# Exact titles and actor labels for all remaining notes, plus the required reader-facing ledgers.
ledger='''feedback-inference-verdict: the opening is a minimal inference from the stored complete-case evidence.\nfeedback-observations: exact turns, actors, documentary frames, work identities, and sense boundaries were read in context.\nfeedback-falsification-searches: checked titles, catalogues, compounds, crossing boundaries, parallel recensions, and incompatible referents.\nfeedback-counterexamples: limits are recorded in DraftEvidence.CounterexampleOrLimit.\nfeedback-scope: frozen allowlisted historical corpus only.\nlookup-probes: preferred target, alternate targets, and every SearchAlias were reviewed for English lookup.\nopening-interpretation-verdict: English-first, corpus-earned, and anchored.\nsense-target-distinguishability: retained targets name different things; grammar, capitalization, and paraphrase do not create a split.\n'''
for i in IDS:
 p,d=load(i);normalize_notes(d['Entry']);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(p.parent/'entry.v2.json'),'--report',str(p.parent/'compile-report.json')],check=True);(p.parent/'WORK.md').write_text(f"# {d['Entry']['SourceTerm']} — f004 A918–925 repaired author draft\n\nstatus: authored; awaiting independent review\n"+ledger)

# Register only lane-local, source-attested masters still absent from the integrated roster.
pending=R/'fresh-build/pending-roster.json';pd=json.loads(pending.read_text());have={x.get('canonicalName') for x in pd.get('candidates',[])}
evidence={
 'Guxue Zhenzhe':('t_bdc0cdca39d0',0,2,['古雪真哲','古雪哲']),
 "Zhe'an Jingfan":('t_bdc0cdca39d0',0,3,['蔗菴淨範','蔗菴範']),
 'Konggu Daocheng':('t_72ed81907d68',0,4,['空谷道澄','空谷澄']),
 'Yanju Deshen':('t_94f424853f5b',0,4,['雲山燕居申德申','燕居德申','燕居申']),
}
for name,(i,si,oi,aliases) in evidence.items():
 if name in have:continue
 _,d=load(i);o=d['Entry']['Senses'][si]['Occurrences'][oi];pd.setdefault('candidates',[]).append({'canonicalName':name,'aliases':aliases,'evidence':[{k:o[k] for k in ('RelPath','FromLb','ToLb','Kwic')}],'reviewedBy':'Codex f004 A918-925 green repair author','reviewReport':'fresh-build/waves/f004-a-918-925-author-unique-pre-review-gate.json','status':'awaiting-roster-integration'})
pending.write_text(json.dumps(pd,ensure_ascii=False,indent=2)+'\n')
