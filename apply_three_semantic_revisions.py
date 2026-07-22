#!/usr/bin/env python3
import json
from pathlib import Path
H=Path('/mnt/c/programmieren/mergeworkcbeta/cbeta-translator/runs/CLAUDE-RUNS/RUN-20260711-1248-full-cbeta-translation/dictionary-build')
REV={
't_cb2186c9f436':{
 'target':'wooden-plaque soup','alts':['wooden-slip soup'],
 'opening':'Wooden-plaque soup is an impossible dish name repeatedly paired with iron-nail rice in rebukes and testing speech.',
 'body':['The compound keeps its internal relation visible: wood modifies the slip or plaque, and soup is made with that wooden object; it does not mean soup containing generic splinters.','The retained speakers emphasize the difficulty of chewing, biting, or taking this offered fare. The article reports that observable impossible-food handling without turning it into a general theory of passive consumption.'],
 'note':'The compound names wooden-slip or wooden-plaque soup. Complete cases pair it with iron-nail rice and make its impossible chewing or swallowing part of a rebuke or test.'},
't_8e0e666cb806':{
 'target':'Jinniu’s rice','alts':[],
 'opening':'Jinniu’s rice is the inherited meal from Jinniu’s monastery case, repeatedly offered or recut beside other named case provisions.',
 'body':['Wei’an pairs it with Yunmen’s cake as food for someone arriving empty-bellied; Xieyun warns of overturning and eating it without knowing the price of Luling rice. Liangting cooks it from a drop of Caoxi water, while Shiyu pairs eating it with beating Heshan’s drum.','These predicates preserve one case-meal under different handling. Jinniu names the case-master rather than the rice’s material, and Yunmen’s cake, Zhaozhou’s tea, Luling rice, and Heshan’s drum remain distinct companion terms.'],
 'note':'Jinniu’s rice names one inherited case-meal. The retained cases offer, overturn, cook, and pair it with other separately named provisions or actions.'},
't_227099445b8c':{
 'target':'understanding-realization','alts':['realization through understanding'],
 'opening':'Understanding-realization names realization grounded in comprehension, but the retained records do not evaluate it uniformly.',
 'body':['The Source-Mirror compiler conditionally commends such understanding as entry into an inconceivable means; Juelang uses the compound less explicitly in verse; and Guting warns that making a view of understanding-realization and adaptability becomes an obstruction.','Guifeng Zongmi supplies the clearest contrast: sudden understanding followed by gradual cultivation is understanding-realization, whereas cultivation culminating in realization is verification-realization. That explicit distinction anchors this technical use without erasing the looser or critical deployments in the other three witnesses.'],
 'note':'Understanding-realization can name realization through comprehension. Zongmi explicitly contrasts it with verification-realization, while the other retained witnesses conditionally commend, verse, or criticize the expression.'},
}
for eid,r in REV.items():
 out=H/'fresh-build/entries'/eid
 for fn in ('entry.v2.json','evidence.draft.json'):
  p=out/fn; d=json.load(open(p,encoding='utf8')); e=d.get('Entry',d); s=e['Senses'][0]
  s['PreferredTarget']=r['target'];s['AlternateTargets']=r['alts'];s['SearchAliases']=[r['target'],*r['alts']];s['Note']=r['note']
  if fn=='entry.v2.json':s['Explanation']=' '.join([r['opening'],*r['body']])
  else:
   s['ExplanationParts']={'CorpusEarnedOpening':r['opening'],'EvidenceBody':r['body']}
   if 'DraftEvidence' in s:
    s['DraftEvidence']['ZenBend']=r['opening'];s['DraftEvidence']['CounterexampleOrLimit']=r['body'][-1]
  p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')

work={
't_cb2186c9f436':'''feedback-inference-verdict: 木 modifies 札: the headword is wooden-slip or wooden-plaque soup, not wooden-splinter soup.\nfeedback-observations: Complete cases pair the dish with iron-nail rice and foreground the impossibility of chewing, biting, or swallowing the offered fare in rebuke or testing speech.\nfeedback-falsification-searches: Checked generic splinters, a separate wooden utensil, recipe language, and abstract anti-consumption readings; the compound grammar and predicates support none of them.\nfeedback-counterexamples: The cases vary in who serves or confronts whom, so the entry does not impose one passive-eating interpretation or universal symbolism.\nfeedback-scope: Exact 木札羹 witnesses and their full cases in the frozen allowlist corpus.\nlookup-probes: wooden-plaque soup; wooden-slip soup; iron-nail rice pairing.\nopening-interpretation-verdict: The opening names the compound’s internal modifier relation and only the impossible-food handling visible in the cases.\nmodifier-relation-verdict: Wood modifies the slip or plaque inside the soup compound.\ndisplay-modifier-verdict: The target displays the wooden plaque and does not substitute splinters.\n''',
't_8e0e666cb806':'''feedback-inference-verdict: Jinniu’s rice is one inherited case-meal under offering, overturning, cooking, and comparison predicates.\nfeedback-observations: Wei’an pairs it with Yunmen’s cake; Xieyun overturns it against Luling rice; Liangting cooks it from Caoxi water; Shiyu pairs eating it with Heshan’s drum.\nfeedback-falsification-searches: Checked whether these predicates create separate foods or make Jinniu a material adjective; full clauses preserve the same named meal.\nfeedback-counterexamples: Companion provisions and actions remain distinct terms and are not absorbed into the definition.\nfeedback-scope: Four exact 金牛飯 cases in four frozen allowlist works.\nlookup-probes: Jinniu’s rice; Jinniu meal; Yunmen cake and Jinniu rice.\nopening-interpretation-verdict: The revised opening states the inherited meal before enumerating its distinct handling.\n''',
't_227099445b8c':'''feedback-inference-verdict: Understanding-realization is comprehension-grounded realization; Zongmi explicitly contrasts it with verification-realization, while other witnesses remain looser or critical.\nfeedback-observations: The four witnesses respectively commend it conditionally, use it in verse, warn against making it a view, and distinguish its cultivation sequence from verification-realization.\nfeedback-falsification-searches: Rechecked the former three-witness limitation after Zongmi’s fourth witness; the contrast is now anchored but is not projected backward onto every occurrence.\nfeedback-counterexamples: Guting’s warning prevents uniform praise, and Juelang’s opaque verse prevents an overly narrow technical definition.\nfeedback-scope: Four exact 解悟 witnesses in four frozen allowlist works.\nlookup-probes: understanding-realization; realization through understanding; verification-realization contrast.\nopening-interpretation-verdict: The opening states the bounded common relation and immediately preserves divergent evaluation.\n'''}
for eid,block in work.items():
 p=H/'fresh-build/entries'/eid/'WORK.md'; old=p.read_text(encoding='utf8') if p.exists() else ''
 prefixes=tuple(x.split(':',1)[0]+':' for x in block.splitlines() if ':' in x)
 old='\n'.join(x for x in old.splitlines() if not x.startswith(prefixes))
 p.write_text(old.rstrip()+'\n\n## Current-byte independent semantic revision\n\n'+block,encoding='utf8')
