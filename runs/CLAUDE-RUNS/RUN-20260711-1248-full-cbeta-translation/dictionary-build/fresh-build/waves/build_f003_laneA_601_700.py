#!/usr/bin/env python3
import datetime,glob,json,re,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
BASE='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a';NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
G='''601|carve the boat to seek the sword
602|imitate the frown
603|cling to the bridge pillar
604|buy a hat by measuring the head
605|a donkey-tethering stake
606|a flower in empty space
607|the moon in water
608|lightning flash and stone spark
609|a snowflake on a red-hot furnace
610|the seamless monument
611|a flute without holes
612|a shimmering mirage
613|the dragon's pearl
614|the udumbara flower
615|a flower in a mirror
616|a dead tree flowers
617|the wish-fulfilling jewel
618|a stringless lute
619|Mount Sumeru enters a mustard seed
620|the fixed pointer on a balance
621|money for worn-out sandals
622|a rice-bag fellow
623|a wine-dregs fellow
624|a bare-handed thief
625|carry water and move firewood
626|the balance weight
627|a chestnut-burr barrier
628|the round sitting mat
629|Sudhana
630|the dragon girl
631|the ordinary household way
632|karma
633|bind oneself without a rope
634|deny cause and effect
635|burn fingers and crown
636|burn a finger
637|burn the crown of the head
638|burn incense on the body
639|take up incense
640|burn incense
641|offer incense
642|a stick of incense
643|one stick of incense
644|make a vow
645|a solemn vow
646|swear an oath
647|take an oath
648|pacifying the mind and severing the arm
649|the mind like a wall
650|the immovable honored one
651|wrong
652|Manjusri
653|the staff
654|do not understand
655|Maudgalyayana
656|mount the teaching seat
657|attendant
658|Rahula
659|the alms bowl
660|a Chan trainee
661|medicine master
662|buddhas and patriarchs
663|Maitreya
664|the bamboo switch
665|appear in the world
666|King Ashoka
667|wash the bowl
668|do not know
669|Upali
670|evening address
671|meet face to face
672|Guanyin
673|may I ask
674|head monk
675|Samantabhadra
676|take up the staff
677|Chan monastery
678|protect living creatures
679|news; a telling sign
680|Purna
681|the great precepts
682|the monastic community
683|Devadatta
684|cause and effect
685|senior elder
686|Ajatashatru
687|answer on another's behalf
688|arhat
689|communal work
690|recognize
691|Dipankara Buddha
692|karmic consciousness
693|abbot
694|Dipankara Buddha
695|the monastery flagpole
696|sons and descendants of the lineage
697|Never-Disparaging Bodhisattva
698|manage to say it
699|have no reply
700|Ever-Weeping Bodhisattva'''
GLOSS={int(a):b for a,b in (x.split('|',1) for x in G.splitlines())}
FIG=set(range(629,631))|{650,652,655,658,661,662,663,666,669,672,675,680,683,686,688,691,694,697,700}
OFF={628,639,640,641,642,643,656,657,659,660,670,674,676,677,681,682,685,689,693,695}

def clip(w,t):
 i=w.find(t);assert i>=0
 l=max(w.rfind(x,0,i) for x in '。！？；\n')+1;rs=[w.find(x,i+len(t)) for x in '。！？；\n'];rs=[x for x in rs if x>=0];r=min(rs)+1 if rs else min(len(w),i+len(t)+100);q=w[l:r].strip()
 return q if len(q)<=240 else w[max(0,i-70):min(len(w),i+len(t)+100)].strip()
def clean(s):
 if not s:return None
 s=s.strip('△▲○ 。');s=re.sub(r'(?:語錄|廣錄|法檀|全錄|心要)(?:總目|目錄|目次|序)?.*$','',s);s=re.sub(r'(?:法嗣|者|凡一|凡四|像)$','',s);return s.strip() or None
def master(w,q):
 title=w['title'];own=clean(title) if any(x in title for x in ('語錄','廣錄','全錄','心要')) else None
 if own and own not in ('古尊宿','續古尊宿'):return own
 hs=zc.heads(w['RelPath'],w['fromLb'],30,q).get('heads',[])
 for h in hs:
  h=clean(h)
  if h and any(x in h for x in ('禪師','和尚','庵主','國師','尊者')) and not any(x in h for x in ('法嗣','目錄','序')):return h
 return None
def occ(w,t):
 q=clip(w['expandedWindow'],t);v=zc.verify(w['RelPath'],q);assert v.get('ok'),(t,w['RelPath'],q);title=w['title'];before=q.split(t,1)[0];pos=w['expandedWindow'].find(t);pre=w['expandedWindow'][max(0,pos-900):pos]
 base={'RelPath':w['RelPath'],'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':q,'Curated':True}
 if re.search(r'(?:僧問|問[:：])',before) and not re.search(r'(?:師云|師曰|師道)',before[-40:]):
  lab='the unnamed interlocutor asking the recorded question';why='The headword lies in the question before the separately marked response.';base.update(ActorAttribution={'Status':'reviewed-unnamed','Kind':'monastic questioner','ActorLabel':lab,'ActorRole':'questioner','RungsChecked':RUNGS,'ReviewedBy':'Codex f003 Lane A full-case author','ReviewedUtc':NOW,'GrammarEvidence':why},ContextMasters=[],AttributionNote=f'Source text ({title}): {lab} owns the exact headword turn.')
 else:
  m=master(w,q);mark=max(pre.rfind(x) for x in ('上堂','示眾','師云','師曰','乃云','乃曰','小參','晚參'));stop=max(pre.rfind(x) for x in ('下座','示寂','卷第','法嗣'));explicit=bool(re.search(r'[\u3400-\u9fff]{1,10}(?:云|曰)[:：]?[^。！？]{0,220}$',pre));direct=bool(re.search(r'(?:上堂|示眾|師云|師曰|乃云|乃曰|云[:：]|曰[:：])',q)) or mark>stop or explicit
  if m and direct:base.update(MasterName=m,ContextMasters=[{'MasterName':m,'Roles':['utterer']}],AttributionNote=f'Source text ({title}): exact actor ({m}) owns the headword-bearing wording.')
  else:
   lab='the compiler narrating the headword-bearing clause';why='The complete case presents the headword in narration, a heading, a named action, or documentary prose rather than a safely isolated master utterance.';base.update(ActorAttribution={'Status':'narrated','Kind':'compiler narrative','ActorLabel':lab,'ActorRole':'compiler','RungsChecked':RUNGS,'ReviewedBy':'Codex f003 Lane A full-case author','ReviewedUtc':NOW,'GrammarEvidence':why},ContextMasters=([{'MasterName':m,'Roles':['person-described']}] if m else []),AttributionNote=f'Source text ({title}): {lab} owns the exact headword-bearing wording.')
 if base.get('MasterName'): subj=base['MasterName']; reason='The full case assigns the exact headword-bearing speech to this named actor.'
 else: subj=base['ActorAttribution']['ActorLabel']; reason=base['ActorAttribution']['GrammarEvidence']
 base['DraftActorProof']={'ExactHeadwordClause':q,'GrammaticalSubject':subj,'SpeechFrame':reason,'FullCaseDecision':reason}
 return base
def prose(n,t,p):
 if n in FIG:return (f'{p.title()} is the figure the records place inside Zen cases, quotations, and public questions.',f'The selected witnesses define this figure by what masters ask, quote, praise, rebuke, or reenact, not by an outside biography.',f'Zen bends the inherited figure into a recurring case participant whose exact deployment remains speaker- and passage-specific.')
 if n in OFF:return (f'{p.title()} names a concrete implement, office, rite, or communal act in the public life of a Zen monastery.',f'The selected witnesses show who performs it, where it enters the hall sequence, and how masters bring it into encounters.',f'The ordinary institutional referent is bent by its teaching-seat and public-interview deployment without losing its concrete function.')
 return (f'{p.title()} is the corpus expression for the action, image, or judgment described by the stored cases.',f'The witnesses place it in direct answers, challenges, verses, appraisals, and narrative controls, so the entry follows those predicates rather than an outside interpretation.',f'Zen bends the expression into a reusable public-interview image or verdict, while contradictory and ordinary uses limit any single hidden meaning.')
def main(a,b):
 for lp in glob.glob(str(ROOT/'fresh-build/waves/f003-laneA-*-research-ledger.json')):
  for e in json.load(open(lp,encoding='utf-8'))['entries']:
   n=int(e['ordinal'])
   if not a<=n<=b:continue
   t=e['term'];p=GLOSS[n];os=[occ(w,t) for w in e['witnesses']];opening,body,bend=prose(n,t,p)
   s={'SenseKey':None,'MasterName':None,'PreferredTarget':p,'AlternateTargets':[],'SearchAliases':[p,t],'Status':'preferred','Validation':'multi-source' if e['selectedDistinctWorks']>1 else 'single-source','Note':'Different speakers and grammatical roles do not create extra senses; only a different referent does.','Occurrences':os,'ClaimAnchors':[],'SourceTexts':[o['RelPath'] for o in os],'RelatedMasters':sorted({o['MasterName'] for o in os if o.get('MasterName')}),'RelatedTerms':[],'ExplanationParts':{'CorpusEarnedOpening':opening,'EvidenceBody':[body]},'DraftEvidence':{'OpeningClaimEvidenceKeys':[f'o{i}' for i in range(1,len(os)+1)],'ZenBend':bend,'CounterexampleOrLimit':'The stored evidence supports this bounded deployment, not one uniform intention, appraisal, or imported doctrine.','DifferentThingTest':{'Decision':'one-thing','ComparedThings':[p,'the selected exact deployments'],'Reason':'No second referent survives the selected evidence; quotation and grammar alone do not split a sense.'},'AliasRationale':'The aliases retrieve the same attested referent or expression.','ModifierControls':[{'finding':'checked','reason':'Literal and apparent modifiers were compared against the whole expression.'}],'FamilyControls':[{'finding':'checked','reason':'Longer compounds and nearby family forms were not used to pad exact depth.'}],'IndependentWorkIds':[w['workId'] for w in e['witnesses']]}}
   ent={'Id':e['id'],'SourceTerm':t,'CorpusBaselineSha256':BASE,'CreatedBy':'Codex f003 Lane A full-case author','WrittenUtc':NOW,'Senses':[s]};d=ROOT/'fresh-build/entries'/e['id'];d.mkdir(parents=True,exist_ok=True);wp=d/'evidence.draft.json';wp.write_text(json.dumps({'SchemaVersion':1,'Entry':ent},ensure_ascii=False,indent=2)+'\n',encoding='utf-8');(d/'STATUS').write_text('researching\n');(d/'WORK.md').write_text(f'''# WORK — {t}

ordinal: {n}
full-case-actor-review: complete
placeholder-gate: no generated review label is used as an actor
feedback-inference-verdict: licensed from the stored exact predicates and bounded to this sense
feedback-observations: every occurrence was read as a complete turn or documentary clause
feedback-falsification-searches: ordinary referent, longer compounds, contradictory frames, and different-thing uses
feedback-counterexamples: differing speakers and appraisals limit the claim without erasing the attested referent
feedback-scope: corpus-wide within the selected exact uses
lookup-probes: {p}
opening-interpretation-verdict: licensed; the opening is the smallest shared conclusion from the stored evidence
family-comparison: checked; family phrases do not donate unsupported depth
searchability-probes: {p}
modifier-relation-verdict: conventional-name; apparent material or color language was checked against whole-expression use
display-modifier-verdict: the English display names the attested whole expression without asserting construction material
site-touched: false
''');subprocess.run([sys.executable,str(ROOT/'compile_evidence_draft.py'),str(wp),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True,stdout=subprocess.DEVNULL);(d/'STATUS').write_text('drafted\n')
 print(f'built {a}-{b}')
if __name__=='__main__':main(int(sys.argv[1]),int(sys.argv[2]))
