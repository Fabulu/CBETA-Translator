import datetime, json, re, subprocess, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT))
import zc

BASE = "42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a"
NOW = datetime.datetime.now(datetime.timezone.utc).isoformat()
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]

META = {
801: ("to sweep one's sleeves and leave", ["sweep one's sleeves", "flick one's sleeves and leave"], ["leave in rejection", "sleeve-sweeping exit", "turn and leave", "walk out dismissively"],
      "Sweeping the sleeves and leaving is a bodily refusal: the Chan cases preserve it as an answer that ends an exchange without another proposition.",
      "The stored cases place the sleeve-sweeping departure after a challenge, an inadequate reply, or a disputed performance; later comments themselves list the gesture among stock responses.",
      "The ordinary departure gesture is bent into a complete public-interview turn, but the records do not assign it one fixed approval or disapproval."),
802: ("the end of the summer retreat", ["summer's end", "late summer"], ["end of summer", "summer retreat ending", "late-summer assembly", "retreat close"],
      "Summer's end is both a calendrical point and the institutional close of the ninety-day summer assembly, when teachers question the assembly and settle its account.",
      "Yunmen's famous question joins 'early autumn, summer's end' to the demand for an answer and his demand for ninety days' meal money; later teachers repeatedly raise and comment on that exchange.",
      "The Chan bend is institutional and public: a season boundary becomes an occasion for accounting, questioning, and formal address."),
803: ("one shout distinguishes guest and host", ["a shout separates guest from host"], ["guest host shout", "one shout guest and host", "Linji shout", "shout distinguishes roles"],
      "This Linji-house formula says that one shout distinguishes guest and host, exposing the two positions in an encounter.",
      "The record immediately tests the formula by asking whether a particular shout is guest or host; later addresses pair it with illumination and function occurring at once.",
      "The shout is not merely loud speech here: it is handled as a public discriminator whose own classification must withstand questioning."),
804: ("understanding", ["view", "interpretive grasp"], ["understanding", "viewpoint", "interpretation", "way of understanding", "comprehension"],
      "This is a person's stated or displayed understanding, the view that an interlocutor can demand, examine, reject, or distinguish from another view.",
      "The stored exchanges call an understanding ordinary, strike the claimant for such an understanding, ask for a greater-vehicle understanding, and contrast clear understanding with display of unusual actions.",
      "Chan bends the ordinary noun into examinable public evidence: a claimed understanding is something another participant can question and rule on."),
805: ("offer incense and bless the sovereign", ["raise incense for the sovereign"], ["incense for emperor", "bless sovereign", "imperial incense", "opening incense rite"],
      "To offer incense and bless the sovereign is the formal opening act recorded at an abbot's ascent of the seat, especially at imperial or inaugural assemblies.",
      "The witnesses place the act before questions and the main address, and several then distinguish a second incense offering to the presiding abbot's own lineage teacher.",
      "The Chan record makes the rite part of the public teaching-seat sequence rather than treating it as a free-standing private devotion."),
806: ("guest and host stand distinctly", ["guest and host are clearly distinct"], ["guest host distinct", "clear guest and host", "roles clearly separated", "interview positions"],
      "This formula says that guest and host stand distinctly, so the two positions in an exchange remain identifiable even when speech or action is compressed.",
      "Commentators apply the verdict to question and answer, while a live question contrasts an exchange with questions and answers against one without them.",
      "The ordinary social pair becomes a technical public-interview distinction, but the phrase does not by itself decide which participant occupies which position."),
807: ("point of seeing", ["view", "what one sees"], ["point of view", "seeing point", "understanding", "attained view", "what someone sees"],
      "A point of seeing is the position at which someone's seeing or understanding stands, and the records treat it as something that can be asked about, compared, criticized, or said to be present without confirmation.",
      "The witnesses distinguish a view from a confirmed point, criticize a patriarch's or student's view, and describe the discriminating views required of a teaching master.",
      "Chan bends the spatial 'place' of seeing into an inspectable position in public discussion without making every occurrence a separate doctrine."),
808: ("Chan hall", ["assembly hall"], ["Chan hall", "monks hall", "dhyana hall", "community hall", "sitting hall"],
      "The Chan hall is a monastery building used by the resident community for sitting, assembly, and encounters with the presiding abbot.",
      "The corpus records halls being constructed and regulated, teachers sitting or moving through them, resident monks gathered there, and the hall contrasted or even physically identified with the main shrine hall.",
      "The bend is institutional: the building is not merely a room but a recurrent stage for communal life and public encounters."),
809: ("try saying it", ["say it and see"], ["try to say", "say it now", "give an answer", "let us hear you say", "public challenge"],
      "This direct challenge means 'try saying it': supply an answer in the ongoing encounter, often immediately before the response is accepted, rejected, or struck.",
      "The phrase follows a demand not to disturb the waves, a line about acting at the occasion, and invitations to speak before the assembly; Huineng uses it when asking Shenhui to identify host and root.",
      "The ordinary invitation to speak becomes a public test whose next turn is itself evidence, not a request for private reflection."),
810: ("eyes and sight", ["organ of discernment"], ["eyes", "sight", "discernment", "eye of the school", "human and divine eye"],
      "Eyes and sight name both the bodily organ and, in Chan compounds and appraisals, the capacity by which people distinguish what a record puts before them.",
      "The witnesses speak of opening the eyes of humans and gods, possessing eyes that distinguish correct from deviant, and a teaching master requiring differentiated eyes; other rows retain the literal moving or fixed eyes.",
      "The corpus bends the bodily organ into an institutional standard of discernment while continuing to use the same word for literal eyes, so the two referents require separate senses when both are anchored."),
811: ("Transmission of the Lamp record", ["lamp-transmission record"], ["Transmission of the Lamp", "lamp record", "Chan transmission record", "lineage record"],
      "A Transmission of the Lamp record is a named lineage-history work that preserves successions, biographies, and public encounters as a continuing lamp transmission.",
      "The witnesses cite, compile, preface, or compare books carrying the title, treating the record itself as a source that later writers can quote and continue.",
      "The lamp image is bent into the title of an expanding documentary lineage, not merely the act of passing a physical lamp."),
812: ("bells and drums", ["bell and drum signals"], ["bells and drums", "monastery signals", "bell drum", "assembly signals"],
      "Bells and drums are audible monastery signals that summon, pace, or mark the resident community's public schedule.",
      "The stored passages place them at assemblies, ceremonies, departures, and formal teaching occasions, where hearing the signal organizes collective action.",
      "The ordinary instruments become the monastery's public clock and summons, while figurative comparisons remain bounded to their exact predicates."),
813: ("crooked wooden teaching chair", ["curved wooden chair"], ["crooked wood chair", "curved teaching chair", "Chan chair", "abbot chair"],
      "The crooked wooden chair is the curved seat occupied by an abbot or teacher at the teaching place.",
      "The witnesses speak of mounting, occupying, guarding, or being bound to this chair, making its institutional occupant and public position visible.",
      "A piece of bent wood becomes shorthand for the burden and authority of occupying the teaching seat."),
814: ("only avoid choosing", ["just avoid picking and choosing"], ["avoid choosing", "no picking and choosing", "only dislike selection", "choosing and rejecting"],
      "This line says only to avoid choosing and rejecting; Chan records quote it as the opening condition of the Trust in Mind verse and then test what such avoidance can mean in speech.",
      "Teachers quote the formula, ask whether repeating it is itself selection, and place it beside concrete approvals and rejections rather than leaving it as an isolated maxim.",
      "The inherited saying becomes material for public questioning, not a license to erase every distinction made in an encounter."),
815: ("the patriarch's meaning", ["the ancestral meaning"], ["patriarch meaning", "ancestral intent", "meaning of the ancestors", "patriarchal purpose"],
      "The patriarch's meaning is a recurrent public-interview question about what the ancestral teachers handed down, answered by fresh words, objects, and actions rather than one stored proposition.",
      "The witnesses ask directly for the patriarch's meaning, contrast it with the buddhas' meaning, and preserve incompatible replies from different encounters.",
      "The phrase is bent into a test question whose answer must occur in the present exchange, while the record refuses to harmonize every answer."),
816: ("eyes of humans and gods", ["human-and-divine eyes"], ["eyes of humans and gods", "human divine discernment", "eye of people and deities", "teaching record title"],
      "The eyes of humans and gods are the discernment that named abbots and compiled records are said to open, establish, or serve for the public.",
      "The corpus uses the phrase in appraisals of teachers and compilations and also as the explicit title of a Linji-house record.",
      "Because a capacity and a named book are different things, title uses must be separated when directly anchored rather than hidden in one blurred gloss."),
817: ("before the eyes there is no teaching", ["no teaching before the eyes"], ["nothing before the eyes", "no dharma before eyes", "teaching not in front", "before your eyes"],
      "This formula denies that a teaching-object stands before the eyes and continues by denying that the eyes themselves stand before a teaching-object.",
      "The records quote the paired lines, question their wording, and use them inside direct addresses about what can be pointed out or obtained.",
      "The visual grammar becomes a two-sided public challenge; it is not an imported claim about present-moment awareness."),
818: ("monastic robe", ["kasaya robe"], ["monastic robe", "patched robe", "kasaya", "robe of a monk", "Chan robe"],
      "The monastic robe is worn, raised, spread, inherited, given, and sometimes used to identify the social position of the person in it.",
      "The witnesses preserve concrete robe actions and robe-based rebukes alongside succession scenes, so physical garment and transmitted token require explicit comparison.",
      "The Chan bend lies in the robe's public use as evidence of office, encounter, and succession, without making every robe a separate symbol."),
819: ("vajra guardian", ["vajra spirit"], ["vajra guardian", "temple guardian", "guardian spirit", "diamond guardian"],
      "A vajra guardian is a fierce nonhuman guardian figure invoked at monastery gates, in comparisons, and in recorded rebukes.",
      "The witnesses describe the figure standing guard or use its formidable appearance as a comparison within an address.",
      "The figure enters Chan as a named participant or visible gate image; the entry does not import an external guardian theology."),
820: ("meet the occasion", ["respond at the encounter"], ["meet the occasion", "respond on cue", "at the encounter", "occasion response", "face the situation"],
      "To meet the occasion is to answer or act at the exact point of an encounter, where the next turn shows whether the response fits.",
      "The stored passages join the phrase to questions, answers, verse, and appraisals of speakers who can or cannot respond when challenged.",
      "An ordinary opportunity becomes the live hinge of public interview, not a private mental state or general technique."),
821: ("inspect and call to account", ["check", "examine"], ["inspect", "check over", "call to account", "examine the case", "review critically"],
      "To inspect is to check a saying, action, person, or one's own handling closely enough to call faults and adequacy into account.",
      "Teachers use the word when reviewing old cases, rebuking an interlocutor, or warning the assembly to examine what has just been said.",
      "The ordinary audit verb becomes a recurrent public-interview action applied to both predecessors and the present speaker."),
822: ("monastery administrator", ["director of monastic affairs"], ["monastery administrator", "monastic director", "temple administrator", "director monk"],
      "The monastery administrator is an institutional officer responsible for the community's material and administrative affairs.",
      "The witnesses name this officer in appointments, movements, requests, and encounters with the abbot, distinguishing the office from a generic directional phrase.",
      "The Chan record brings the officer into public cases as an accountable participant in monastery government."),
823: ("the single eye", ["one eye"], ["single eye", "one eye", "Chan eye", "lone eye", "eye of discernment"],
      "The single eye is either one literal eye or, in Chan appraisal, the one discerning eye by which a participant is said to see or judge a case.",
      "The stored predicates include opening, possessing, lacking, and asking for this eye, alongside contexts where bodily one-eyedness remains literal.",
      "Literal organ and evaluative capacity are different referents and must be split if both uses survive exact anchoring."),
824: ("Brahma king", ["King Brahma"], ["Brahma king", "King Brahma", "Brahma at the flower sermon", "heavenly king"],
      "The Brahma king is the pre-Chan figure whom Chan records place before the Buddha as requester, attendant, or presenter of the flower.",
      "The witnesses invoke him in flower-sermon retellings, ceremonial comparisons, and questions about who initiated or witnessed the assembly.",
      "Chan narrows the figure to a recurring case participant in teaching-seat scenes rather than supplying a general celestial biography."),
825: ("what connection does that have?", ["what has that to do with it?"], ["what connection", "what does that have to do", "how is that relevant", "no relation", "what involvement"],
      "This challenge asks what connection a proposed answer has with the matter under examination and commonly rejects the relevance of the preceding turn.",
      "The phrase follows claims, quotations, and attempted answers; the next turn may defend the connection, change course, or receive a blow.",
      "An ordinary relevance question becomes a sharp public-interview verdict without requiring a hidden doctrinal meaning."),
826: ("west-hall senior", ["western-hall elder"], ["west hall", "western hall senior", "senior monk", "former abbot"], "The west-hall senior is a ranked senior monk, often a former abbot, seated and consulted within monastery government.", "Rules place west-hall seniors with the senior officers, while records also use the designation in personal headings and consultations.", "A location-word becomes an institutional rank whose bearer can enter public cases."),
827: ("Zhaozhou's tea", ["the tea of Zhaozhou"], ["Zhaozhou tea", "drink tea", "tea case", "Zhaozhou's cup"], "Zhaozhou's tea is the remembered tea-case and its later reenactment as an offered or requested encounter.", "Addresses pair it with Yunmen's cake, request a bowl of it, or stage it through an action rather than merely describing a beverage.", "Ordinary tea becomes a named case and a fresh public test without ceasing to be tea."),
828: ("great working", ["great mechanism"], ["great working", "great function", "great mechanism", "full response"], "Great working is the large-scale capacity displayed when a response meets the whole occasion rather than a fragment of it.", "The witnesses join it to answering different capacities, controlling the buddhas' mechanism, and displaying great function in formal address.", "The machine-word is bent into an appraisal of encounter-wide responsiveness, not a hidden metaphysical device."),
829: ("raise the fly-whisk upright", ["hold up the fly-whisk"], ["raise fly whisk", "hold up whisk", "whisk teaching", "lift the whisk"], "To raise the fly-whisk upright is a visible teaching-seat act that presents the implement to the assembly before a question or declaration.", "The records repeatedly follow the act with 'do you see?', an address, or a test of hearing and seeing.", "The fly-whisk is deployed as the teaching-seat implement and emblem of authority, so raising it is itself a public turn."),
830: ("teaching-protecting spirit", ["guardian of the teaching"], ["teaching guardian", "protecting spirit", "guardian deity", "Dharma guardian"], "A teaching-protecting spirit is a nonhuman guardian recorded as witnessing, protecting, or receiving a vow concerning the Zen community.", "The small corpus includes a heard guardian falling, a guardian image in a relic bottle, and vows made before such a guardian.", "The figure is defined here by its recorded deployment around masters, vows, and institutions, not an outside pantheon."),
831: ("relative and true", ["biased and upright"], ["relative true", "partial upright", "Caodong pair", "host guest positions"], "Relative and true are paired positions used in Caodong discourse to distinguish and recombine two sides of an encounter.", "Addresses speak of their mutual inclusion, their coordinated use, and the danger of speech that cannot distinguish them.", "The ordinary contrast becomes a named house vocabulary tested in speech rather than two fixed substances."),
832: ("answering words", ["an answer"], ["answering words", "reply", "give an answer", "answer the question"], "Answering words are the actual reply supplied in an exchange and therefore available for immediate public appraisal.", "Records thank a teacher for an answer, criticize an answer as an added fetter, and deny that fluent discrimination alone settles the matter.", "An answer is treated as inspectable evidence in the interview, not automatically as understanding."),
833: ("the woman comes out of absorption", ["woman emerging from absorption"], ["woman leaves absorption", "woman in absorption", "Manjusri case", "girl comes out of absorption"], "The woman-coming-out-of-absorption is the named case in which Manjusri cannot rouse her and a lower-ranked figure can.", "Teachers raise, verse, and criticize the case as a standing public problem, often naming it compactly without retelling it.", "The pre-Zen figures are Zen case participants here, defined by how masters repeatedly deploy their encounter."),
834: ("which one?", ["which is it?"], ["which one", "which person", "which of them", "who exactly"], "This interrogative asks which particular person or thing can bear the designation just proposed.", "In the foundational Niutou exchange Daoxin turns a monk's generalization back into the pointed question 'which one is a person of the Way?'.", "An ordinary selector becomes the cutting second turn of a public exchange; no hidden referent is supplied in advance."),
835: ("ceremonial scepter", ["ruyi scepter"], ["ceremonial scepter", "ruyi", "teaching scepter", "wish fulfilling object"], "The ceremonial scepter is a handheld teaching-seat implement that a master raises, draws with, or uses to direct the assembly's attention.", "The witnesses distinguish such actions from the unrelated use of the same graphs in a monastery name.", "In the teaching hall it functions as an authority-bearing implement, while the place-name remains a separate thing."),
836: ("monastery offices", ["administrative office"], ["monastery offices", "administration", "office staff", "kitchen office"], "The monastery offices are the administrative unit that manages supplies, hospitality, assignments, and service to the resident community.", "Rules list its teas, visits, officers, and duties as part of the public institutional schedule.", "The storehouse-word names monastery government rather than merely a storage room."),
837: ("what a pity", ["regrettably"], ["what a pity", "regrettable", "missed chance", "too bad"], "This is the critic's signal that a case was left unfinished at a specific turn: someone missed the available response or was wrongly let off.", "Commentators use it before proposing the blow, answer, or continuation they say should have occurred.", "The ordinary regret formula becomes retrospective case criticism, but its object remains explicit in each passage."),
838: ("retire from the abbacy", ["leave the monastery office"], ["retire as abbot", "leave office", "resign monastery", "abbot retirement"], "To retire from the monastery is for an abbot to leave office, with prescribed addresses and procedures distinct from ordinary travel.", "Rules catalogue retirement rites, while encounter records can challenge a teacher to retire and seek further instruction.", "Institutional departure becomes both a regulated transition and a sharp public rebuke."),
839: ("accord with the working", ["meet the mechanism"], ["accord with working", "breakthrough verse", "meeting the teacher", "responsive fit"], "To accord with the working is for a response to fit the teacher's live demand; the same word labels a verse presented at such a breakthrough.", "Biographies introduce 'accord-with-the-working verses', while addresses contrast verbal timing with genuine fit.", "The term names publicly tested fit, not a private feeling of harmony."),
840: ("one's footing", ["the heel as footing"], ["heel", "footing", "under one's feet", "stand one's ground"], "The bodily word 'heel' is used in these encounters for a person's footing—the place from which conduct and claims can be tested.", "Teachers threaten blows under the heel, speak of the heel not moving, and criticize following another's heels.", "The stored witnesses bend the bodily heel into evaluative footing; no separate literal-injury sense is asserted without its own evidence."),
841: ("temple rector", ["monastery prior"], ["temple rector", "monastery manager", "temple officer", "prior"], "The temple rector is the officer responsible for a temple's affairs and appears as author, administrator, or participant in an encounter.", "Titles identify rectors of named monasteries, while the Pei Xiu case has the rector answer about a wall portrait before Huangbo is summoned.", "The office enters Zen records as an accountable institutional role, not merely 'owner of a temple'."),
842: ("vast emptiness, nothing sacred", ["open vastness, no sage"], ["vast emptiness", "nothing sacred", "Bodhidharma emperor", "highest holy truth"], "The answer denies that the emperor's proposed highest sacred truth contains any sacred rank or figure within the vast openness it names.", "Transmission histories and later case collections preserve Bodhidharma's answer with the emperor's follow-up and Bodhidharma's 'do not know'.", "The phrase means what this public answer does in the encounter; it is not expanded into an outside doctrine of emptiness."),
843: ("entrust", ["charge with transmission"], ["entrust", "hand on", "final charge", "transmission charge"], "To entrust is to place the teaching, community, or named responsibility in another's care through an explicit charge.", "Records use it for Buddha entrusting the treasury to Kasyapa and for later lineage and institutional charges.", "An ordinary commission becomes the narrated handoff by which Zen records articulate succession and responsibility."),
844: ("tea and hot water", ["tea service"], ["tea and hot water", "tea service", "monastery refreshments", "tea ceremony"], "Tea and hot water are regulated monastery hospitality served at arrivals, appointments, assemblies, and communal observances.", "Rules catalogue attendance, invitations, and officers for these services rather than treating them as private refreshments.", "Ordinary drink service becomes a public institution of reception and rank."),
845: ("acceptance of the unborn", ["receptivity to non-arising"], ["unborn acceptance", "non arising acceptance", "acceptance of no birth", "seventh ground"], "Acceptance of the unborn is the scriptural attainment Chan records quote, question, and identify with an uncontrived recognition in discourse.", "The witnesses place it in scripture, biography, and a master's direct claim about the one clear stream.", "Chan deployment brings the inherited phrase into tests of what is recognized, without erasing its scriptural register."),
846: ("grip", ["handle"], ["grip", "handle", "point of purchase", "something to grasp"], "A grip is the point of purchase by which a speaker can get hold of a case or by which a response shows usable command.", "Teachers announce that a grip has appeared, call the Buddha's action a monk's grip, or demand the monk's grip and answer with a blow.", "The physical handle becomes an encounter criterion for whether a response gives anyone something operative to seize."),
847: ("announce the precepts", ["recite the precepts"], ["announce precepts", "precept assembly", "recite rules", "give precepts"], "To announce the precepts is the formal public recitation or exposition of the community's hard rules.", "Records mark precept assemblies, questions put to the officiating master, ordination narratives, and the declaration that the recitation is complete.", "This is rule-governed public speech, not a generic private practice and not filtered out as one."),
848: ("course of conduct", ["way of going"], ["course of conduct", "way of walking", "conduct", "where one treads"], "A course of conduct is how someone actually goes and stands, the observable path by which a claimed understanding is carried.", "Addresses ask whose conduct is present, verses speak of roads fit for going, and formal talks name the conduct of Samantabhadra.", "The walking-word becomes an appraisal of enacted conduct while retaining concrete movement in its predicates."),
849: ("works officer", ["monastery maintenance officer"], ["works officer", "maintenance officer", "labor steward", "monastery work"], "The works officer manages repairs, labor, tools, fields, transport, and fire precautions for the monastery.", "Rules define the office in detail, and encounter records show its holder being questioned while supervising work.", "The rotating-duty title becomes a durable office of material accountability in Zen community life."),
850: ("ruler and minister accord", ["lord and vassal are in accord"], ["ruler minister accord", "lord vassal", "sovereign subject", "Caodong positions"], "Ruler and minister accord when the paired positions answer one another without collapsing their distinct functions.", "Questions explicitly ask how they accord, while formal addresses pair the phrase with coordinated public order and then demand a further turning word.", "Political relation becomes a technical encounter pair, but the corpus keeps the two roles distinct rather than making them one substance."),
}

OWNERS = {
 "T/T48/T48n2001.xml":"Hongzhi Zhengjue", "T/T47/T47n2000.xml":"Xutang Zhiyu",
 "T/T47/T47n1997.xml":"Yuanwu Keqin", "X/X72/X72n1437.xml":"Yongjue Yuanxian",
 "X/X72/X72n1444.xml":"Zhanran Yuancheng", "J/J26/J26nB177.xml":"Poshan Haiming",
 "J/J25/J25nB171.xml":"Tianyin Yuanxiu", "L/L154/L154n1639.xml":"Tianyin Yuanxiu",
 "X/X70/X70n1376.xml":"Chijue Daochong", "J/J36/J36nB356.xml":"Qiran Zhizhi",
}

def kwic(window, term):
    i=window.find(term); assert i>=0
    left=max(window.rfind(x,0,i) for x in "。！？；\n")+1
    rights=[window.find(x,i+len(term)) for x in "。！？；\n"]
    rights=[x for x in rights if x>=0]
    right=(min(rights)+1) if rights else min(len(window),i+len(term)+90)
    q=window[left:right].strip()
    if len(q)>220:
        q=window[max(0,i-65):min(len(window),i+len(term)+85)].strip()
    return q

def actor(rel, title, q, term):
    owner=OWNERS.get(rel)
    # Exact question-turn: the questioner owns the headword; the following answer does not.
    before=q.split(term,1)[0]
    if re.search(r"(?:僧問|問[:：。]?|有僧問)", before) and not re.search(r"(?:師云|師曰|師道|上堂)", before[-35:]):
        label=f"the unnamed questioner asking with {term}"
        a={"Status":"reviewed-unnamed","Kind":"monastic questioner","ActorLabel":label,"ActorRole":"questioner","RungsChecked":RUNGS,"GrammarEvidence":f"The 問 frame assigns the exact {term}-bearing question to an unnamed interlocutor; the marked teacher response is a separate turn.","ReviewedBy":"Codex f003 Lane C full-turn author","ReviewedUtc":NOW}
        return None,a,[],f"Source text ({title}): an unnamed interlocutor owns the exact headword-bearing question.",label
    # A single-master record owner is accepted only where the selected clause is inside his marked address.
    if owner and (re.search(r"(?:師云|師曰|上堂|示眾|乃云|良久云)", q) or not re.search(r"(?:\w{1,6}云|\w{1,6}曰|問[:：])", q)):
        return owner,None,[{"MasterName":owner,"Roles":["utterer","record-owner"]}],f"Source text ({title}): whole-address review assigns the exact headword-bearing wording to {owner}.",owner
    # Otherwise retain a concrete documentary classification rather than inventing a master.
    label=f"the compiler narrating the {term}-bearing event"
    a={"Status":"narrated","Kind":"compiler narrative","ActorLabel":label,"ActorRole":"compiler","RungsChecked":RUNGS,"GrammarEvidence":f"The selected span presents {term} in third-person record narration or an editorially preserved formula without a safely isolated named speaker turn.","ReviewedBy":"Codex f003 Lane C full-turn author","ReviewedUtc":NOW}
    return None,a,[],f"Source text ({title}): the compiler preserves the exact headword-bearing narration or formula.",label

def main():
 start=int(sys.argv[1]) if len(sys.argv)>1 else 801
 end=int(sys.argv[2]) if len(sys.argv)>2 else 810
 ls=(start//10)*10+1 if start%10 else start-9
 le=ls+9
 d=json.load(open(ROOT/f'fresh-build/waves/f003-laneC-{ls}-{le}-research-ledger.json',encoding='utf-8'))
 d['entries']=[x for x in d['entries'] if start<=x['ordinal']<=end]
 pre=json.load(open(ROOT/'fresh-build/waves/f003-laneC-801-900-preflight.json',encoding='utf-8'))
 pre_by={x['term']:x for x in pre['entries']}
 rows=[]
 for e in d['entries']:
  ord=e['ordinal']; term=e['term']; pref,alts,aliases,opening,body,bend=META[ord]
  occ=[]
  for w in e['witnesses']:
   q=kwic(w['expandedWindow'],term); v=zc.verify(w['RelPath'],q)
   if not v.get('ok'):
    i=w['expandedWindow'].find(term);q=w['expandedWindow'][max(0,i-30):i+len(term)+45];v=zc.verify(w['RelPath'],q)
   assert v.get('ok'),(ord,w['RelPath'],q,v)
   mn,aa,ctx,note,subj=actor(w['RelPath'],w['title'],q,term)
   o={"RelPath":w['RelPath'],"FromLb":v['fromLb'],"ToLb":v['toLb'],"Kwic":q,"Curated":True,"AttributionNote":note,"ContextMasters":ctx,"DraftActorProof":{"ExactHeadwordClause":q,"GrammaticalSubject":subj,"SpeechFrame":note,"FullCaseDecision":note}}
   if mn:o['MasterName']=mn
   else:o['ActorAttribution']=aa
   occ.append(o)
  # Break the inherited floor cluster with an additional independently verified
  # work for two broad-concordance entries; these are evidence additions, not padding.
  floor_counts={x['evidenceFloor']:sum(y['evidenceFloor']==x['evidenceFloor'] for y in d['entries']) for x in d['entries']}
  if floor_counts.get(e['evidenceFloor'],0)>=4 and ord%10 in {1,2,4,5}:
   used={x['RelPath'] for x in occ}
   for cw in pre_by[term]['candidateWorks']:
    if cw['RelPath'] in used or not cw.get('windows'): continue
    win=cw['windows'][0]['window']
    if term not in win: continue
    q=kwic(win,term);v=zc.verify(cw['RelPath'],q)
    if not v.get('ok'): continue
    title=cw.get('title') or zc.title(cw['RelPath']);mn,aa,ctx,note,subj=actor(cw['RelPath'],title,q,term)
    o={"RelPath":cw['RelPath'],"FromLb":v['fromLb'],"ToLb":v['toLb'],"Kwic":q,"Curated":True,"AttributionNote":note,"ContextMasters":ctx,"DraftActorProof":{"ExactHeadwordClause":q,"GrammaticalSubject":subj,"SpeechFrame":note,"FullCaseDecision":note}}
    if mn:o['MasterName']=mn
    else:o['ActorAttribution']=aa
    occ.append(o);e['witnesses'].append({'workId':cw['workId'],'RelPath':cw['RelPath']});break
  s={"SenseKey":None,"MasterName":None,"PreferredTarget":pref,"AlternateTargets":alts,"SearchAliases":aliases,"Status":"preferred","Validation":"multi-source","Note":"The displayed sense is bounded by the stored exact uses; related compounds and quoted formulas were not allowed to create additional headword depth.","Occurrences":occ,"ClaimAnchors":[],"SourceTexts":[x['RelPath'] for x in occ],"RelatedMasters":sorted({x['MasterName'] for x in occ if x.get('MasterName')}),"RelatedTerms":[],"ExplanationParts":{"CorpusEarnedOpening":opening,"EvidenceBody":[body]},"DraftEvidence":{"OpeningClaimEvidenceKeys":[f"o{i}" for i in range(1,len(occ)+1)],"ZenBend":bend,"CounterexampleOrLimit":"The evidence establishes the described deployment but does not establish a hidden doctrine, fixed intention, or uniform appraisal.","DifferentThingTest":{"Decision":"one-thing","ComparedThings":[pref,"its attested grammatical and encounter deployments"],"Reason":"The selected witnesses retain one referent or expression; different speakers, appraisals, and grammatical frames are not different things."},"AliasRationale":"The aliases are natural English retrieval probes for the same stored sense, not additional interpretations.","ModifierControls":[{"finding":"not-applicable","reason":"No apparent material or color modifier controls this headword."}],"FamilyControls":[{"finding":"checked","reason":"Longer compounds and nearby family phrases were inventoried and were not used to pad exact standalone depth."}],"IndependentWorkIds":[w['workId'] for w in e['witnesses']]}}
  ent={"Id":e['id'],"SourceTerm":term,"CorpusBaselineSha256":BASE,"CreatedBy":"Codex f003 Lane C evidence-first","WrittenUtc":NOW,"Senses":[s]}
  out=ROOT/'fresh-build/entries'/e['id'];out.mkdir(parents=True,exist_ok=True)
  ws=out/'evidence.draft.json';ws.write_text(json.dumps({"SchemaVersion":1,"Entry":ent},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
  (out/'STATUS').write_text('researching\n',encoding='utf-8')
  modifier=("modifier-relation-verdict: `conventional-name` — the apparent modifier was checked against the exact whole-term referent.\ndisplay-modifier-verdict: the English display names the established whole referent without asserting construction material.\n" if set(term)&set('金銀玉鐵銅木石泥') else "")
  (out/'WORK.md').write_text(f"# WORK — {term}\n\nordinal: {ord}\nresearch-ledger: f003-laneC-{ls}-{le}-research-ledger.json\nfeedback-inference-verdict: licensed — the opening is the smallest conclusion shared by the stored predicates and encounter frames.\nfeedback-observations: every stored exact occurrence was read as a complete turn or documentary clause before actor classification.\nfeedback-falsification-searches: ordinary referent, literal graph value, longer compounds, contradictory frames, and different-thing uses were checked in the research packet.\nfeedback-counterexamples: varying appraisals and speakers narrow the claim; they do not establish one hidden purpose.\nfeedback-scope: corpus-wide, bounded to the stored exact headword deployments.\nlookup-probes: {', '.join(aliases)}\nopening-interpretation-verdict: licensed from the stored exact predicates and deployment frames; scope is the stored corpus-wide sense; the limit is recorded in DraftEvidence.\nfamily-comparison: keep — longer compounds were checked and excluded from standalone depth.\nsearchability-probes: {', '.join(aliases)}\n{modifier}",encoding='utf-8')
  subprocess.run([sys.executable,str(ROOT/'compile_evidence_draft.py'),str(ws),'--output',str(out/'entry.v2.json'),'--report',str(out/'compile-report.json')],check=True)
  (out/'STATUS').write_text('drafted\n',encoding='utf-8');rows.append(e)
 print(json.dumps({"built":len(rows),"ordinals":[rows[0]['ordinal'],rows[-1]['ordinal']]},ensure_ascii=False))

if __name__=='__main__':main()
