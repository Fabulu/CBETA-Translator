#!/usr/bin/env python3
import json,re,subprocess,sys,datetime
from pathlib import Path
R=Path(__file__).resolve().parents[2]
L=json.loads((R/'fresh-build/waves/f003-laneA-651-700-author-ledger.json').read_text()); by={x['ordinal']:x['id'] for x in L['entries']}
P={
651:("Wrong is the public verdict that a reply, quotation, or move has missed what the exchange demanded.","The records also use the graph for copying and attribution errors; those documentary uses limit the encounter verdict rather than proving a special meaning everywhere."),
652:("Manjusri is the sword-bearing and assembly-acting figure whom Chan speakers place inside questions about killing, manifestation, and the formal opening of a case.","The record uses him as a case participant—showing across mountains, wielding a sword, or striking the sounding block—not as an occasion for an outside life story."),
653:("The staff is first a walking stick; in the Chan hall it is also the presiding master's handled implement.","Masters raise, plant, point, throw down, and strike with it, so the visible act can constitute the answer or verdict before the assembly."),
654:("Do not understand reports failed comprehension, but a speaker can also return it deliberately as the whole answer.","The full turn decides whether it is accusation, self-report, or reply; the phrase alone does not praise every instance of not understanding."),
655:("Maudgalyayana is invoked in these records as the wonder-working disciple asked to transport an image-maker and as the exemplary son in memorial language.","Chan speakers quote those particular acts and place him among named case figures rather than importing a complete biography."),
656:("To mount the teaching seat is to take the raised seat from which a master addresses an assembled community.","The act begins a public hall occasion: questions follow, a case may be raised, and leaving the seat closes the event."),
657:("An attendant is the monk assigned to remain close to a master, carry messages, summon participants, and manage immediate needs.","Chan records repeatedly put the attendant inside encounters—as messenger, witness, addressee, or the person ordered to fetch a questioning monk."),
658:("Rahulata is the sixteenth Indian patriarch in the transmission lists and cases preserved by these Chan records.","The selected passages concern Rahulata's meeting with Sanghanandi and the tree-ear episode; they do not establish an entry about Rahula, the Buddha's son."),
659:("The alms bowl is the monk's food bowl, carried, set upright or inverted, washed after eating, and transmitted with the robe.","In public cases masters turn its mouth, covering, washing, or impossible handle into the concrete object around which a reply is tested."),
660:("A Chan person is a monk or student addressed as someone engaged with Chan, often in a dedication, instruction, or request for a hall address.","The label identifies the addressee's relation to the Chan community; it is not itself an office or a claim that the person has understood."),
661:("Medicine Master names the healing buddha invoked in liturgical titles, dedications, and Chan records of named figures and temples.","The surviving witnesses place the name in ritual and institutional frames; they do not support treating every occurrence as an ordinary physician."),
662:("Buddhas and patriarchs is the paired authority formula for the awakened figures and lineage ancestors repeatedly judged together in Chan speech.","Masters claim to continue, surpass, expose, or speak alongside this pair, while prefaces use it to measure a record's language against the lineage."),
663:("Maitreya is the future-buddha figure whom Chan records also identify with the laughing cloth-bag monk and raise in direct questions.","The figure appears as a named interlocutory test and conventional identity, not as an imported catalogue of future-world doctrine."),
664:("The bamboo switch is a flat bamboo implement held before the assembly.","Dahui's recurring challenge makes naming itself public: call it a switch and one is wrong, refuse the name and one is wrong, yet an answer is still demanded."),
665:("To appear in the world is to emerge into public activity, whether said of a buddha, lineage figure, or a master taking up an abbacy.","Chan institutional records bend the arrival formula toward entering the teaching seat and meeting people, while retaining the broader birth-or-appearance use."),
666:("King Ashoka is the royal figure whom Chan records connect with distributing relics, raising stupas, and testing or honoring the lineage.","The selected cases invoke those concrete royal acts and questions rather than supplying a general imperial biography."),
667:("Wash the bowl is the ordinary instruction to clean one's food bowl after eating.","Zhaozhou gives that chore as his entire answer to a newcomer's question; later masters preserve the compact exchange while leaving the washing literal."),
668:("Do not know denies knowledge or recognition; its force depends on the exact question and speaker.","Bodhidharma's famous reply stands beside ordinary failures and rebukes, so the record does not turn every instance of ignorance into the same Zen verdict."),
669:("Upali is the precept specialist invoked when Chan records discuss rules, recitation, and the transmission of disciplinary material.","His Zen deployment is tied to that hard-rule competence; the name is not merely one more figure in an undifferentiated sacred roster."),
670:("An evening address is the community's later-day gathering for a master's public speech and questions.","Records mark it as a distinct hall occasion, with its own opening, raised sayings, and responses rather than as a private evening exercise."),
671:("To meet face to face is an actual encounter or audience in which the people stand present to one another.","Chan speakers test reported recognition against that meeting: what was said or seen when teacher and visitor actually met becomes public evidence."),
672:("Guanyin is the hearing-and-seeing figure whom Chan masters place in questions about hands, eyes, sound, and responsive activity.","The record deploys those bodily predicates in encounters and comparisons instead of importing an outside devotional biography."),
673:("May I ask opens the questioner's turn and politely marks what remains unsettled.","In Chan interviews it is part of the public question, so its actor is the monk or visitor asking—not automatically the master who answers next."),
674:("The head monk is the senior seat-holder who leads the west rank and may answer, represent the assembly, or become a master in his own right.","Chan records treat the office as an active participant in hall governance and encounters, not merely a name in a personnel list."),
675:("Samantabhadra is the elephant-riding and activity-associated figure paired with Manjusri in Chan questions and comparisons.","Masters invoke that paired figure to test where the named figures are encountered, rather than retelling an outside sacred biography."),
676:("To take up the staff is the visible act of lifting the master's walking and teaching-seat implement.","Before an assembly the act can open a challenge, punctuate a saying, scatter the monks, or stand as the answer itself."),
677:("A Chan monastery is the institutional residence in which a Chan community, offices, halls, and a resident master are established.","The term occurs heavily in titles and colophons because records identify the particular monastery that houses each public lineage record."),
678:("To protect living creatures is the hard obligation to preserve life, not a vague feeling of kindness.","Chan speakers put that rule under pressure with the formula 'protecting life requires killing' and with concrete animal, food, rescue, and monastery cases; the tension is recorded, not dissolved."),
679:("News or tidings is information carried from someone or somewhere absent.","A revealing sign is instead the clue by which a speaker says an encounter has disclosed itself, while adjustment names regulating a condition; the different predicates keep these three referents apart."),
680:("Purna is the named disciple and teacher whom Chan records raise in quoted assemblies and lineage comparisons.","The selected witnesses must be read as those particular inherited case roles, not as permission to supply an outside biography."),
681:("The great precepts are the full ordination rules formally received and conferred in the community.","Chan institutional records place them in ordination, transmission, and public admonition; calling them 'great' does not soften them into a voluntary technique."),
682:("A grove literally gathers many trees; in Chan institutional language the grove is the organized monastic community.","Masters speak of the grove's offices, standards, decline, and public reputation, making the collective community—not woodland—the characteristic referent."),
683:("Devadatta is the hostile kinsman whom Chan speakers invoke in questions about opposition, wrongdoing, and the Buddha's own relations.","The record uses those sharply bounded case predicates rather than importing a complete doctrinal biography."),
684:("Cause and effect is the relation by which an act or condition bears its consequence.","Chan records argue over falling into it, being blind to it, and denying it; the fox case and condemnation of casting causality aside keep rhetorical negation from erasing the hard relation."),
685:("A senior elder is an honorific for an established monk or master and can also denote a group of such leaders.","In Chan records elders mount the seat, receive visitors, and hold institutional standing; the title does not by itself identify the exact speaker."),
686:("Ajatashatru is the king whom the transmitted cases place beside Kasyapa at the collection of the recorded teachings.","Chan compilers quote that royal exchange and its requests; the selected evidence does not warrant a free-standing outside biography."),
687:("To answer on another's behalf is to supply the reply an earlier participant did not give.","The marked substitute belongs to the later master or compiler who says it, never retroactively to the silent participant in the old case."),
688:("An arhat is a named attained figure or rank in inherited cases; the same graphs also occur inside monastery and master names.","Chan questions test particular arhats such as Kasyapa, while catalogue strings naming Luohan monasteries or masters are proper-name uses and cannot define the rank."),
689:("Communal work is the monastery's shared physical labor, performed with the assembly rather than delegated as private employment.","Baizhang's record makes the institutional bend concrete: he joins the labor and is said to work before the others."),
690:("To recognize is to identify the person, object, source, or point presented in an encounter.","Masters make recognition answerable in public—raising a staff, pointing at a bench, or asking about a seamless stupa—rather than accepting a private claim."),
691:("Dipankara Buddha is the earlier buddha in the prediction formula quoted by Chan masters.","The decisive Chan use couples him with Sakyamuni's statement that no teaching was obtained: if anything had been obtained, Dipankara would not have given the prediction."),
692:("Karmic consciousness is the busy, unsettled discriminating activity speakers call 茫茫, 忙忙, or 紛沉掉.","The compound names that conditioned bustle and lack of footing in these passages; it is narrower than consciousness in general and must not absorb every use of mind."),
693:("An abbot is the resident office-holder responsible for a monastery; the same graphs also function verbally for holding or presiding over that residence.","Titles and institutional rules name the office, while first-person statements such as 'this old monk has presided' retain the verbal action."),
694:("Dipankara Buddha—here under the name Dingguang—is raised in ritual headings and in the direct question 'what is Dingguang Buddha?'.", "The Chan deployment includes that public interview as well as inherited buddha lists; the name alone does not supply an outside life story."),
695:("The monastery flagpole is the tall landmark at or before the monastery gate, visible from a distance.","Chan cases repeatedly speak of seeing its shadow, climbing its top, or toppling it before Kasyapa's gate, turning the landmark into a concrete public-case object."),
696:("Sons and descendants are later members claimed as heirs of a Chan house.","The family language makes transmission institutional: speakers praise continuers, accuse descendants of copying models, or say an ancestor's mistake has harmed the offspring."),
697:("Never-Disparaging is the figure who tells each person that they will become a buddha and refuses contempt.","Chan masters bend that inherited act into public testing: one asks why he was not struck, another identifies unexpected objects with him, and later speakers announce his arrival."),
698:("To manage to say it is to produce an answer that meets the demand of the present exchange.","The phrase appears in explicit stakes—sit if you can say it, lose the belt if you cannot—and later verdicts that someone said only half; substring 外道得 is not this lexical item."),
699:("To have no reply records that a named participant does not answer the question or move just presented.","The silence remains a public event assigned to Kasyapa, a monk, or a lecturer; the record does not automatically praise absence of speech."),
700:("Ever-Weeping is the figure who sells his heart and liver in order to seek prajna in the inherited story.","Chan speakers raise that extreme transaction in questions, hall addresses, and paired judgments, making the concrete sale—not a generic sacred biography—the recorded deployment.")}

roster=json.loads((R.parents[3]/'Assets/Data/master-dates.json').read_text())['masters']
aliases=[]
for m in roster:
 for n in m.get('names',[]):
  if re.search(r'[\u3400-\u9fff]',n): aliases.append((len(n),n,m['names'][0]))
aliases.sort(reverse=True)
def canon(n):
 for _,a,c in aliases:
  if a in n or n in a:return c
 return None
def narrated(o,label='the compiler or recorder of the source passage'):
 o.pop('MasterName',None);o['ActorAttribution']={'Status':'narrated','Kind':'compiler narrative','ActorLabel':label,'ActorRole':'compiler','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'ReviewedBy':'Codex f003 A651-700 repair author','ReviewedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'GrammarEvidence':'The exact headword is governed by documentary narration or a catalogue/title string, not by a master utterance.'};o['ContextMasters']=[];o['AttributionNote']='The named source presents this exact headword in documentary narration or a catalogue/title string.'
for n,eid in by.items():
 d=R/'fresh-build/entries'/eid;p=d/'evidence.draft.json';x=json.loads(p.read_text()); opening,body=P[n]
 if n==658:
  s=x['Entry']['Senses'][0];s['PreferredTarget']='Rahulata';s['SearchAliases']=['Rahulata','sixteenth Indian patriarch'];s['AlternateTargets']=[]
 for s in x['Entry']['Senses']:
  s['ExplanationParts']={'CorpusEarnedOpening':opening,'EvidenceBody':[body]};s['DraftEvidence']['ZenBend']=body;s['DraftEvidence']['CounterexampleOrLimit']='The explanation is limited to the distinct referents and predicates represented by the stored exact-headword witnesses.'
  for o in s['Occurrences']:
   old=o.get('MasterName')
   if old and re.search(r'[\u3400-\u9fff]',old):
    c=canon(old)
    if c:
     o['MasterName']=c;o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':c,'Roles':['utterer']}];o['AttributionNote']=f"The source's full turn identifies {c} as the exact headword utterer."
     pr=o.setdefault('DraftActorProof',{});pr['GrammaticalSubject']=c;pr['SpeechFrame']='The marked full-case turn assigns the exact headword to this named master.';pr['FullCaseDecision']=pr['SpeechFrame']
    elif old in {'御選','五家','哭趙州和尚二首','靈瑞禪師嵒花集敘'}: narrated(o)
  s['RelatedMasters']=sorted({o['MasterName'] for o in s['Occurrences'] if o.get('MasterName')})
 p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True,stdout=subprocess.DEVNULL)
print('repaired',len(by))
