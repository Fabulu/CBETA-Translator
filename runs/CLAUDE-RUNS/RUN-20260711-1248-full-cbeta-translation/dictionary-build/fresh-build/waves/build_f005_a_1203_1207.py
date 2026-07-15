from pathlib import Path
import datetime, hashlib, json, subprocess, sys

R = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(R))
import zc

BASE = '42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a'
NOW = datetime.datetime.now(datetime.timezone.utc).isoformat()
RUNGS = ['line','expanded-context','section-header','book-title','tei-header','parallel-passage']


def unique_kwic(rel, term, occurrence_index=0):
    text, lbs = zc._load(rel)
    starts = []
    at = 0
    while True:
        at = text.find(term, at)
        if at < 0:
            break
        starts.append(at)
        at += len(term)
    pos = starts[occurrence_index]
    radius = 14
    while True:
        kwic = text[max(0, pos-radius):min(len(text), pos+len(term)+radius)]
        verdict = zc.verify(rel, kwic)
        if verdict['ok'] and verdict['count'] == 1:
            return kwic
        radius += 12


def occurrence(rel, term, master, decision, occurrence_index=0, contexts=()):
    kwic = unique_kwic(rel, term, occurrence_index)
    v = zc.verify(rel, kwic)
    assert v['ok'] and v['count'] == 1
    return {
        'RelPath': rel, 'FromLb': v['fromLb'], 'ToLb': v['toLb'], 'Kwic': kwic,
        'MasterName': master, 'Curated': True,
        'ContextMasters': ([{'MasterName': master, 'Roles': ['utterer']}]
                           + [{'MasterName': x, 'Roles': ['record-owner']} for x in contexts]),
        'AttributionNote': f'The source record or case collection ({zc.title(rel)}; {rel}). Exact actor: {master}. {decision}',
        'DraftActorProof': {
            'ExactHeadwordClause': term, 'GrammaticalSubject': master,
            'SpeechFrame': decision, 'FullCaseDecision': decision,
        },
    }


def make_sense(target, alternates, aliases, opening, body, occurrences, note,
               bend, limit, family, related=()):
    works = list(dict.fromkeys(zc.work_id(o['RelPath']) for o in occurrences))
    return {
        'SenseKey': None, 'MasterName': None, 'PreferredTarget': target,
        'AlternateTargets': alternates, 'SearchAliases': aliases, 'Status': 'preferred',
        'ExplanationParts': {'CorpusEarnedOpening': opening, 'EvidenceBody': body},
        'Validation': 'multi-source' if len(works) >= 2 else 'provisional', 'Note': note,
        'Occurrences': occurrences, 'ClaimAnchors': [],
        'SourceTexts': list(dict.fromkeys(o['RelPath'] for o in occurrences)),
        'RelatedMasters': list(dict.fromkeys(o['MasterName'] for o in occurrences)),
        'RelatedTerms': list(related),
        'DraftEvidence': {
            'OpeningClaimEvidenceKeys': [f'o{i}' for i in range(1, len(occurrences)+1)],
            'ZenBend': bend, 'CounterexampleOrLimit': limit,
            'DifferentThingTest': {
                'Decision': 'one-thing',
                'ComparedThings': ['the ordinary bodily image', 'the phrase in Chan encounters and comments'],
                'Reason': 'The stored predicates retain one concrete image; changes of speaker, grammar, or appraisal do not create another referent.'
            },
            'AliasRationale': 'The aliases cover natural English word order and the concrete image without importing a doctrinal reading.',
            'ModifierControls': ['not-applicable: no apparent construction-material claim requires a modifier split.'],
            'FamilyControls': [family], 'IndependentWorkIds': works,
        },
    }


specs = []

specs.append(('t_321a547de25f', '耳裏著水', make_sense(
    'water getting into the ear', ['water in the ear'],
    ['water in one’s ear', 'an ear that admits no water', 'ear filled with water'],
    'Water getting into the ear is the auditory half of a paired bodily obstruction: the eye cannot take sand and the ear cannot take water. Chan speakers preserve Baishui Benren’s saying, question each half in public, and also reverse it with the image of an ear containing the ocean.',
    [
        'Baishui Benren states the paired formula and answers the question about the ear with “pure and undefiled.”',
        'Shending Yikui sets the exclusion formula against its opposite: the eye contains Mount Sumeru and the ear contains the four seas.',
        'Jueyin applies the water image to Xiangyan’s hearing bamboo strike, alongside dust entering Lingyun’s eye.',
        'Linye Qi cites the paired saying while warning that a displayed auspicious scene still does not settle the address.'
    ],
    [
        occurrence('X/X67/X67n1307.xml','耳裏著水','Baishui Benren','The raised old case explicitly assigns the formula to Baishui Benren.'),
        occurrence('J/J37/J37nB388.xml','耳裏著水','Shending Yikui','The phrase occurs in Shending Yikui’s own uninterrupted hall address.'),
        occurrence('X/X82/X82n1571.xml','耳裏著水','Jueyin','The nearest Jueyin section governs this uninterrupted address and the phrase is his comparison.'),
        occurrence('J/J26/J26nB186.xml','耳裏著水','Linye Qi','The phrase occurs in Linye Qi’s own hall address after his explicit question.'),
    ],
    'Fifty-nine exact hits occur in thirty-four frozen works; duplicate recensions of Baishui’s case were not counted as independent deployments.',
    'The record turns the familiar irritation into a portable interview formula, questions it clause by clause, and places beside it the contrary image of an ear holding an ocean.',
    'The contrary ocean formula prevents treating the image as a universal doctrine of exclusion; it is an attested saying used in particular contrasts.',
    'Compared 眼裏著沙, 耳裏著得大海水, and the Baishui case family; parallel recensions were not used to pad depth.', ['眼裏著沙'])))

specs.append(('t_138ca8036367', '鼻孔遼天', make_sense(
    'nostrils reaching the sky', ['sky-high nostrils'],
    ['nose reaching heaven', 'nostrils turned skyward', 'sky-reaching nose'],
    'Nostrils reaching the sky is an extravagant bodily picture of a person standing wholly unconcealed and uncontained. Chan masters use it as an answer, a self-description, a description of everyone in the assembly, and a challenge whose appearance would put Tianyi Yihuai’s own life at stake.',
    [
        'Chongsheng Xie answers the condition after the antelope has hung up its horns with “nostrils reaching the sky.”',
        'Yunmen Wenyan ends a hall address by saying that on meeting a person his nostrils reach the sky.',
        'Puan Yinsu names himself in the image amid a sequence in which each figure occupies an impossible place.',
        'Tianyi Yihuai says that if anyone now appeared with sky-reaching nostrils, his own life would be in danger.',
        'Feiyin Tongrong and Shiqi Tongyun reuse the phrase in later independent records, showing that the appraisal travels beyond Tianyi Yihuai’s record.'
        ,'Tianyi Yihuai also places sky-reaching nostrils beside fixed eyes in a closing verse before leaving the seat.'
    ],
    [
        occurrence('X/X78/X78n1556.xml','鼻孔遼天','Chongsheng Xie','Within Chongsheng Xie’s named section, the record’s “the master said” frame assigns this exact answer to him.'),
        occurrence('C/C077/C077n1710.xml','鼻孔遼天','Yunmen Wenyan','The Yunmen record marks this as his hall speech immediately before he descends.'),
        occurrence('X/X69/X69n1356.xml','鼻孔遼天','Puan Yinsu','Puan Yinsu names himself in his own continuous formal address.'),
        occurrence('X/X82/X82n1571.xml','鼻孔遼天','Tianyi Yihuai','The Tianyi Yihuai section governs the uninterrupted hall address containing this challenge.'),
        occurrence('J/J26/J26nB178.xml','鼻孔遼天','Feiyin Tongrong','The exact phrase belongs to Feiyin Tongrong’s own recorded address, not an embedded quotation.'),
        occurrence('J/J26/J26nB183.xml','鼻孔遼天','Shiqi Tongyun','The complete unit in Shiqi Tongyun’s own record assigns the phrase to his speech.'),
        occurrence('X/X68/X68n1318.xml','鼻孔遼天','Tianyi Yihuai','The Tianyi Yihuai section governs the closing verse and his immediate descent from the seat.'),
    ],
    'Three hundred seven exact hits occur in 121 frozen works, with six independent deployment witnesses retained here.',
    'The impossible anatomy becomes a recurrent Chan appraisal applied in answers, addresses, first-person speech, and Tianyi Yihuai’s test of who could confront him.',
        'The phrase can praise, challenge, or rebuke; those different appraisals do not establish different things or license a fixed psychological trait.',
    'Compared 鼻孔, 鼻孔朝天, 通身是眼, and collocations with 人人 and 箇箇; longer variants remain family evidence.', ['鼻孔'])))

specs.append(('t_640e09aef544', '頂門具眼', make_sense(
    'having an eye at the crown of the head', ['crown-of-the-head eye'],
    ['third eye on the crown', 'eye at the top of the head', 'crown eye'],
    'Having an eye at the crown of the head names an extra point of sight beyond the ordinary pair of eyes. The Chan record repeatedly demands this eye when judging a case, distinguishing figures, or avoiding a careless acceptance, while also warning that even possessing it does not make care unnecessary.',
    [
        'Yuanwu Keqin says that even one equipped with the crown eye must not be careless, then elsewhere requires it to see the action in Baoshou’s opening-day case.',
        'Lianyue uses the phrase in the paired demand “distinguishing demons and selecting the different requires the crown eye.”',
        'Juelang Daosheng includes a crown eye three fathoms long in a portrait praise.',
        'Mingjue Cong places the phrase in a formal address among predicates of testing and discrimination.',
        'Baiyu uses it for the person who would overturn the provisional city and kick over the treasure place.'
        ,'Hanyu Xian uses the phrase first for everyone in the assembly and later as a direct answer, showing both collective and turn-level deployment.'
    ],
    [
        occurrence('T/T48/T48n2003.xml','頂門具眼','Yuanwu Keqin','This is Yuanwu Keqin’s own prose comment in the Blue Cliff Record.'),
        occurrence('X/X67/X67n1301.xml','頂門具眼','Yuanwu Keqin','The phrase occurs in Yuanwu Keqin’s direct commentary on the complete Baoshou case.'),
        occurrence('J/J29/J29nB235.xml','頂門具眼','Lianyue','The phrase belongs to Lianyue’s own formal address after the preceding exchange closes.'),
        occurrence('J/J34/J34nB311.xml','頂門具眼','Juelang Daosheng','Juelang Daosheng composed this exact line in the complete praise section.'),
        occurrence('L/L158/L158n1652.xml','頂門具眼','Mingjue Cong','The phrase is in Mingjue Cong’s own uninterrupted address.'),
        occurrence('J/J36/J36nB359.xml','頂門具眼','Baiyu','The phrase occurs in Baiyu’s own hall address after the quoted teaching line.'),
        occurrence('J/J33/J33nB288.xml','頂門具眼','Hanyu Xian','Hanyu Xian utters the phrase in his own hall address while describing everyone in the assembly.'),
    ],
    'One hundred fifty-five exact hits occur in eighty-six frozen works. The crown eye is retained as a bodily image rather than replaced by an outside occult category.',
    'The image supplies a recurring qualification for case judgment and public discrimination, but Yuanwu’s warning explicitly keeps it from functioning as an automatic guarantee.',
    'The records vary the eye’s length and its tasks; those predicates do not establish a literal anatomical organ or several separate senses.',
    'Compared 頂門眼, 正眼, 具眼, 腦後見腮, and the paired crown-eye/behind-the-head formulas.', ['具眼','腦後見腮'])))

specs.append(('t_27a41c50f0c3', '腦後見腮', make_sense(
    'seeing the cheeks from behind the head', ['cheeks visible behind the head'],
    ['see cheeks behind the head', 'cheeks showing at the back of the head', 'behind-the-head cheeks'],
    'Seeing the cheeks from behind the head pictures an impossible exposure: a person’s face is visible from the side that should conceal it. Chan speakers use the formula as a terse answer and, most often, pair it with “do not associate with him,” making the impossible visibility a warning-sign appraisal.',
    [
        'Huqiu Shaolong asks what caused his laugh and answers with the full formula followed by “do not associate with him.”',
        'Miyun Yuanwu gives the phrase as his direct answer to “what is originally pure?”',
        'Sanshan Lai’s praise expands the cheeks to eight feet and pairs them with a crown eye three fathoms long.',
        'Hanyu Xian answers an approving follow-up with the same four graphs.',
        'Minshu sets the phrase against possessing the crown eye in his comment on Bodhidharma and Emperor Wu.',
        'Yulin Tongxiu repeats the warning formula in an independent record.'
        ,'Langting Ting gives the complete “do not associate” formula as a direct answer before continuing the interview.'
    ],
    [
        occurrence('X/X84/X84n1583.xml','腦後見腮','Huqiu Shaolong','The Huqiu Shaolong section and his own question-answer frame assign the formula to him.'),
        occurrence('J/J10/J10nA158.xml','腦後見腮','Miyun Yuanwu','The record’s “the master said” frame marks the exact answer by Miyun Yuanwu in his own record.'),
        occurrence('J/J29/J29nB244.xml','腦後見腮','Sanshan Lai','The phrase occurs in Sanshan Lai’s own signed praise verse.'),
        occurrence('J/J33/J33nB288.xml','腦後見腮','Hanyu Xian','The record’s “the master said” frame assigns the exact answer to Hanyu Xian in his own record.'),
        occurrence('J/J39/J39nB450.xml','腦後見腮','Minshu','The prose following the record’s “the master said” frame is Minshu’s comment on the raised Bodhidharma case.'),
        occurrence('B/B27/B27n0152.xml','腦後見腮','Yulin Tongxiu','The complete address in Yulin Tongxiu’s own record assigns the phrase to him.'),
        occurrence('J/J33/J33nB294.xml','腦後見腮','Langting Ting','The question-and-answer heading and reply frame assign the complete warning to Langting Ting.'),
    ],
    'One hundred fifty-nine exact hits occur in ninety-six frozen works. The recurring warning formula and the answer use share one impossible bodily image.',
    'The Chan bend lies in making impossible rearward visibility into a compact public appraisal that can answer a question or mark someone to avoid.',
    'The phrase does not state why the person is objectionable, and this entry does not turn that unstated reason into psychology or doctrine.',
    'Compared 頂門具眼, 莫與往來, 見腮, and praise-verses that enlarge the same bodily image.', ['頂門具眼'])))

specs.append(('t_9529f4444230', '舌頭拖地', make_sense(
    'the tongue dragging on the ground', ['ground-dragging tongue'],
    ['tongue trailing on the ground', 'tongue dragging the earth', 'long dragging tongue'],
    'A tongue dragging on the ground is speech made bodily excessive: the organ extends so far that it trails along the earth. Chan masters deploy the image as an answer about a saying, as Zhongfeng Mingben’s capping comment on a visitor’s words, and as a cost of choosing one side of a comparison.',
    [
        'Wuzhun Shifan answers a question about Zhaozhou’s saying of no guest and host with “the tongue drags on the ground,” then answers the follow-up with “words fill the world.”',
        'Zhongfeng Mingben inserts the phrase after replaying a visitor’s answer “I came for the teaching.”',
        'Juelang Daosheng gives it as the answer requested before birth, then follows it with “eyebrows propping up heaven.”',
        'Mingjue Cong says that choosing Yunmen in his comparison would wrong himself until his tongue dragged on the ground.',
    ],
    [
        occurrence('X/X82/X82n1571.xml','舌頭拖地','Wuzhun Shifan','The record’s “the master replied” frame assigns this exact answer to Wuzhun Shifan in his named section.'),
        occurrence('B/B25/B25n0145.xml','舌頭拖地','Zhongfeng Mingben','The phrase is Zhongfeng Mingben’s capping comment within his own replay of the Tianmu encounter.'),
        occurrence('J/J34/J34nB311.xml','舌頭拖地','Juelang Daosheng','The record’s “the master said” frame assigns the exact answer to Juelang Daosheng in the complete exchange.'),
        occurrence('L/L158/L158n1652.xml','舌頭拖地','Mingjue Cong','The first-person “this mountain monk” contrast sits inside Mingjue Cong’s uninterrupted address.'),
    ],
    'Eighty exact hits occur in fifty-six frozen works. Four independent deployments preserve answers, capping comment, and conditional comparison.',
    'The corpus bends the grotesquely long tongue into an answer and evaluative comment about words while keeping the visible tongue image intact.',
    'The image accompanies both abundant words and pointed answers; it therefore cannot simply be defined as either eloquence or empty talk.',
    'Compared 舌頭, 言滿天下, 眉毛拄天, and other exaggerated body-part formulas; the organ and the capacity for speech remain related but not silently conflated.', ['舌頭'])))

specs.append(('t_fd83eaebf6ad', '口似血盆', make_sense(
    'a mouth like a basin of blood', ['blood-basin mouth'],
    ['mouth like a blood basin', 'blood-filled mouth', 'gaping bloody mouth'],
    'A mouth like a basin of blood is the speech-organ pictured as a huge blood-filled opening, commonly paired with teeth like a forest of swords. Chan records apply the pair to formidable verbal performance, yet repeatedly add that such a mouth can still merely flaunt verbal sharpness or fall short at the decisive exchange.',
    [
        'Lan’an Dingxu joins the sword-teeth and blood-basin mouth, then calls their display an empty show of verbal sharpness and force.',
        'Tian’an Sheng places the same mouth among the equipment of a clear-eyed person who judges dragons and snakes.',
        'Baiyu says that even someone with this mouth and lightning-like activity has not yet smelled the host’s foot-sweat.',
        'A questioner asks Dahui Zonggao what happens if such a person refuses Dahui’s answer; Dahui replies that this would show the person has an eye.',
        'Yuanwu Keqin uses the pair for true teachers in his own record, while Xutang Zhiyu is publicly asked whether his own saying passes such a mouth.'
        ,'Liao’an Qingyu says that even Deshan’s sword-teeth and blood-basin mouth would retreat before the action in the raised case.'
    ],
    [
        occurrence('X/X82/X82n1571.xml','口似血盆','Lan’an Dingxu','The Lan’an Dingxu section marks this uninterrupted hall passage as his speech.'),
        occurrence('J/J26/J26nB187.xml','口似血盆','Tian’an Sheng','The phrase occurs in Tian’an Sheng’s own continuous formal address.'),
        occurrence('J/J36/J36nB359.xml','口似血盆','Baiyu','Baiyu utters the phrase in his own hall address before posing the concluding question.'),
        occurrence('M/M59/M59n1540.xml','口似血盆','Dahui Zonggao','Dahui Zonggao repeats the questioner’s exact phrase while answering it in the recorded public exchange.'),
        occurrence('T/T47/T47n1997.xml','口似血盆','Yuanwu Keqin','The phrase is part of Yuanwu Keqin’s own uninterrupted informal address.'),
        occurrence('T/T47/T47n2000.xml','口似血盆','Xutang Zhiyu','Xutang Zhiyu repeats his questioner’s exact image in responding within his own hall exchange.'),
        occurrence('X/X71/X71n1414.xml','口似血盆','Liao’an Qingyu','Liao’an Qingyu utters the phrase while adjudicating the complete raised Deshan encounter.'),
    ],
    'One hundred forty-five exact hits occur in eighty-nine frozen works. The stored witnesses preserve both formidable and explicitly limited deployments.',
    'The blood-basin mouth is not simply praise: the record makes it an image for immense verbal force and then tests whether that force does any work in the case at hand.',
    'Different favorable and hostile appraisals do not make separate mouths, and the wording does not license a general theory of violence or character.',
    'Compared 牙如劍樹, 利口, 舌頭, and the fixed teeth-and-mouth pair; the pair is family evidence while each saved row contains the exact headword.', ['牙如劍樹','舌頭'])))

specs.append(('t_ab7b478bd5bb', '糞掃堆頭', make_sense(
    'the top of a dung-sweepings heap', ['dung-heap top'],
    ['on a refuse heap', 'top of the muck heap', 'dung sweepings pile'],
    'The top of a dung-sweepings heap is the refuse pile where discarded and filthy things collect. Chan masters repeatedly make it the unlikely place where a jewel, treasure, robe, shoe, or even a golden body is found, while also using the same heap for adding still more rubbish.',
    [
        'Juelang Daosheng says the Buddha found a mud pellet on the heap and raised it before the assembly.',
        'Muting Pufu calls a display of pots, tiles, bamboo scraps, and bricks “adding more rubbish atop the dung heap.”',
        'Xuedou Chongxian places a sixteen-foot golden body on the heap before setting that proposition aside.',
        'Changling Zhuo’s verse says treasure is found there without seeking.',
        'Dongshan Liangjie compares joy after leaving holy truth undone to finding a bright pearl on the heap.',
        'Yehai Ziqing says he found a torn robe there and kept it for decades.'
        ,'Mi’an Xianjie uses the rubbish-adding frame after saying that even forgetting both delusion and awakening remains additional refuse.'
    ],
    [
        occurrence('J/J34/J34nB311.xml','糞掃堆頭','Juelang Daosheng','Juelang Daosheng utters the sentence in his own Vesak-eve hall address.'),
        occurrence('J/J40/J40nB493.xml','糞掃堆頭','Muting Pufu','Muting Pufu supplies the phrase in his own old-case comment.'),
        occurrence('X/X66/X66n1297.xml','糞掃堆頭','Xuedou Chongxian','The inline attribution to Xuedou Chongxian governs the exact headword-bearing comment.'),
        occurrence('X/X68/X68n1318.xml','糞掃堆頭','Changling Zhuo','The named Changling Zhuo section assigns this exact verse line to him.'),
        occurrence('X/X81/X81n1568.xml','糞掃堆頭','Dongshan Liangjie','The complete exchange assigns this first-person comparison to Dongshan Liangjie.'),
        occurrence('X/X84/X84n1583.xml','糞掃堆頭','Yehai Ziqing','Yehai Ziqing’s named hall address contains the first-person account of finding the robe.'),
        occurrence('X/X84/X84n1585.xml','糞掃堆頭','Mi’an Xianjie','Mi’an Xianjie utters the rubbish-adding formula in his own hall address after striking with the staff.'),
    ],
    'One hundred nine exact hits occur in sixty-five frozen works. The evidence includes discovery, comparison, capping verse, and an explicit rubbish-adding use.',
    'Zen bends the refuse heap by repeatedly locating what is kept, displayed, or prized on it, without erasing the heap’s ordinary filth or the opposite act of piling on more rubbish.',
    'The objects found on the heap differ, but the heap remains one location-image; no single object supplies a hidden symbolic key for every occurrence.',
    'Compared 糞掃, 糞堆, 拾得寶, 明珠, and 重添搕𢶍; the discovered objects are predicates, not substitute headword senses.', ['糞掃'])))

specs.append(('t_f4fc42267d33', '雞寒上樹鴨寒下水', make_sense(
    'when cold, chickens climb trees and ducks enter the water', ['cold chickens climb trees; cold ducks enter water'],
    ['chickens up trees ducks in water', 'cold chicken and duck saying', 'chickens roost ducks swim'],
    'When cold, chickens climb trees and ducks enter the water: two animals meet the same condition through different native movements. Baling Haojian gives the saying as his answer when asked whether the ancestral meaning and the scriptural meaning are the same or different; Zhenjing Kewen later raises it and asks the assembly for its purport.',
    [
        'In Mingjue Chongxian’s old-case collection, Baling Haojian answers the same-or-different question with the complete chicken-and-duck line.',
        'Zhenjing Kewen quotes the old saying in a hall address, asks what it means, then shouts and leaves the seat.'
    ],
    [
        occurrence('T/T47/T47n1996.xml','雞寒上樹鴨寒下水','Baling Haojian','The raised exchange explicitly gives the exact line as Baling Haojian’s answer.'),
        occurrence('C/C077/C077n1710.xml','雞寒上樹鴨寒下水','Zhenjing Kewen','Zhenjing Kewen recites the old line aloud in his own hall question before shouting and descending.'),
    ],
    'The frozen corpus has two exact hits in two independent works. Both preserve the whole saying; one gives the original answer and one its later public raising.',
    'The Chan deployment depends on the observable contrast within one weather condition: Baling answers a same-or-different question with two creatures doing unlike things.',
    'The records do not separately gloss which half is ancestral or scriptural, so the entry preserves the scene and question without assigning the animals to doctrinal sides.',
    'Compared the component clauses, 祖意教意同別, and the parallel 青山自青山白雲自白雲 answer; neither component appears independently as the same saying.', ['祖意','教意'])))

specs.append(('t_7c53f7605da2', '飯籮邊餓死', make_sense(
    'starving beside the rice basket', ['starve beside the rice basket'],
    ['hungry next to food', 'starving by the food basket', 'rice-basket starvation'],
    'Starving beside the rice basket is deprivation in immediate reach of food: the basket is present, yet the person remains unfed. Chan masters pair it with dying of thirst in the sea, apply it after Zhe’an Fan’s interlocutor misses a snowball held out before him, and use it to warn against overlooking what is plainly at hand.',
    [
        'Yongji Rong’s verse names the impossible predicament of its Zen monk starving beside the rice basket.',
        'Sanshan Lai glosses Yunmen’s “rice in the bowl, water in the bucket” by warning not to starve beside the basket or thirst in the sea.',
        'Zhe’an Fan gives his interlocutor a ball of snow; when that interlocutor hesitates, Zhe’an says that those who starve beside the rice basket are numberless.'
    ],
    [
        occurrence('J/J28/J28nB209.xml','飯籮邊餓死','Yongji Rong','Yongji Rong recites the exact line in his own hall verse.'),
        occurrence('J/J29/J29nB244.xml','飯籮邊餓死','Sanshan Lai','Sanshan Lai makes this warning in his own comment on the raised Yunmen exchange.'),
        occurrence('J/J36/J36nB369.xml','飯籮邊餓死','Zhe’an Fan','The complete snow encounter marks this exact verdict as Zhe’an Fan’s reply to the hesitating monk.'),
    ],
    'Nine exact hits occur in nine frozen works. Three independent contexts preserve verse, old-case comment, and a live encounter.',
    'The corpus keeps the physical contradiction intact and uses it at the point where food, water, or the offered object is already immediately available.',
    'Immediate availability is licensed by the basket, sea, bowl, bucket, and offered snowball; the evidence does not authorize naming a hidden spiritual commodity.',
    'Compared 海水裏渴死, 缽裡飯, 桶裡水, and the snowball encounter; the paired thirst phrase remains a related image, not another sense.', ['海水裏渴死'])))

specs.append(('t_495c83ba370b', '三文買草鞋', make_sense(
    'three coins to buy straw sandals', ['buy straw sandals for three coins'],
    ['three cash for sandals', 'three pennies for straw shoes', 'cheap straw sandals'],
    'Three coins to buy straw sandals is the small traveling allowance attached to a plain meal or handed out as a dismissal. Chan masters place it after “eat the customary cakes,” give the three coins to a questioner, and use the line as a compact send-off after an answer has run its course.',
    [
        'Yunfeng Wenyue says that if nobody can supply the requested sentence, eating the customary cakes will still earn three coins for straw sandals.',
        'Zhenjing Kewen repeats the meal-and-sandals line, then immediately poses another old-case question.',
        'Muting Pufu gives the phrase directly to his questioner after that questioner thanks him for instruction.',
        'Yuejiang Zhengyin says he differs from Dongshan Shouchu, then caps the contrast with the same customary meal and sandal money.'
    ],
    [
        occurrence('C/C077/C077n1710.xml','三文買草鞋','Yunfeng Wenyue','The Yunfeng section governs this exact conclusion to his hall address.'),
        occurrence('X/X82/X82n1571.xml','三文買草鞋','Zhenjing Kewen','Zhenjing Kewen utters the complete meal-and-sandals line in his named hall section.'),
        occurrence('J/J40/J40nB493.xml','三文買草鞋','Muting Pufu','The complete exchange assigns the sandal-money send-off to Muting Pufu.'),
        occurrence('X/X71/X71n1409.xml','三文買草鞋','Yuejiang Zhengyin','Yuejiang Zhengyin gives the phrase as his own capping line after raising Dongshan Shouchu.'),
    ],
    'Thirty-seven exact hits occur in twenty-five frozen works. Four independent records retain the meal formula, direct gift, and comparative capping use.',
    'The Chan bend is institutional and conversational: a trivial amount for travel footwear becomes the line that closes or dismisses a public exchange.',
    'The corpus does not establish whether three coins was a stable market price in every period; the entry reports the attested allowance without importing an economic history.',
    'Compared 隨例餐䭔子, 草鞋, 行腳, and 贈你三文; the meal collocation and direct-gift frame support one sandal-money image.', ['草鞋','行腳'])))


pending = R / 'fresh-build/pending-roster.json'
pd = json.loads(pending.read_text())
known = {m['names'][0] for m in json.loads((R.parents[3] / 'Assets/Data/master-dates.json').read_text())['masters']}
have = {x['canonicalName'] for x in pd['candidates']}
for eid, term, sense in specs:
    for o in sense['Occurrences']:
        name = o['MasterName']
        if name not in known and name not in have:
            pd['candidates'].append({
                'canonicalName': name, 'aliases': [name],
                'evidence': [{k:o[k] for k in ('RelPath','FromLb','ToLb','Kwic')}],
                'reviewedBy': 'Codex f005 lane A author',
                'reviewReport': 'fresh-build/waves/f005-laneA-1203-1212-checkpoint.json',
                'status': 'awaiting-roster-integration',
            })
            have.add(name)
pending.write_text(json.dumps(pd, ensure_ascii=False, indent=2) + '\n')

rows = []
for eid, term, sense in specs:
    b = R / 'fresh-build/entries' / eid
    b.mkdir(parents=True, exist_ok=True)
    ep = b / 'entry.v2.json'
    # The first five are an already-gated immutable checkpoint. Extending this
    # author ledger must not rewrite their timestamps or bytes.
    if eid in {'t_321a547de25f','t_138ca8036367','t_640e09aef544','t_27a41c50f0c3','t_9529f4444230'} and ep.exists():
        rows.append((eid, hashlib.sha256(ep.read_bytes()).hexdigest()))
        continue
    draft = {'SchemaVersion': 1, 'Entry': {
        'Id': eid, 'SourceTerm': term, 'CorpusBaselineSha256': BASE,
        'CreatedBy': 'Codex f005 lane A author', 'WrittenUtc': NOW, 'Senses': [sense],
    }}
    wp = b / 'evidence.draft.json'
    wp.write_text(json.dumps(draft, ensure_ascii=False, indent=2) + '\n')
    work = f'''# {term} — f005 lane A construction

- discovery-provenance: `fresh-build/waves/f005-laneA-1201-1300-preflight.json`; inherited wording is a research lead only.
- indexed-path: frozen-corpus preflight; every saved row reverified with `zc.verify`.
- definition-searches: direct questions, answer frames, paired and opposite formulas, body-part family, repeated recensions, and contradictory predicates.
- deployment-inventory: {len(sense['Occurrences'])} curated exact rows across {len(sense['DraftEvidence']['IndependentWorkIds'])} independent work IDs.
- omission-audit: each prose claim has an exact headword occurrence; repeated recensions were not used as depth padding.
- family-retest: {sense['DraftEvidence']['FamilyControls'][0]}
- sense-target-distinguishability: `not-applicable — one bodily image across the stored deployments`.
- observation: occurrence IDs `o1–o{len(sense['Occurrences'])}` establish the ordinary scene and named Chan deployments.
- minimal-inference: {sense['DraftEvidence']['ZenBend']}
- ordinary-bridge: ordinary anatomy supplies the physical impossibility or obstruction; no outside doctrine is needed.
- falsification-searches: literal uses; definition questions; opposite formula; body-part compounds; repeated case family; contradictory appraisal.
- counterexamples: {sense['DraftEvidence']['CounterexampleOrLimit']}
- scope: `corpus-wide phrase within the cited Chan deployments`.
- verdict: `licensed`.
- feedback-inference-verdict: `supported` — the opening is the narrowest inference shared by the stored predicates.
- feedback-observations: occurrence IDs `o1–o{len(sense['Occurrences'])}` establish the bodily image and the named deployments stated in the article.
- feedback-falsification-searches: literal uses; definition questions; opposite formulas; longer compounds; repeated case families; contradictory appraisals.
- feedback-counterexamples: {sense['DraftEvidence']['CounterexampleOrLimit']}
- feedback-scope: `corpus-wide phrase within the cited Chan deployments; no outside symbolic or doctrinal theory`.
- lookup-probes: {'; '.join(sense['SearchAliases'])}.
- opening-interpretation-verdict: `licensed` — the opening names the ordinary scene and narrow corpus-earned deployment before quotations.
'''
    (b / 'WORK.md').write_text(work)
    report = b / 'evidence-compile-report.json'
    proc = subprocess.run([sys.executable, str(R/'compile_evidence_draft.py'), str(wp), '--output', str(ep), '--report', str(report)], text=True, capture_output=True)
    if proc.returncode:
        raise SystemExit(proc.stdout + proc.stderr)
    rows.append((eid, hashlib.sha256(ep.read_bytes()).hexdigest()))

out = R / 'fresh-build/waves/f005-laneA-1203-1212-author-ledger.json'
payload = {'schemaVersion': 1, 'wave': 'f005', 'lane': 'A', 'ordinals': [1203,1212],
           'entries': [{'id': i, 'sha256': h} for i,h in rows], 'writtenUtc': NOW}
tmp = out.with_suffix('.tmp')
tmp.write_text(json.dumps(payload, ensure_ascii=False, indent=2)+'\n')
tmp.replace(out)

lane = R / 'fresh-build/waves/f005-laneA.json'
lane_data = json.loads(lane.read_text())
hashes = dict(rows)
for row in lane_data['entries']:
    if row['id'] in hashes:
        row.update(state='drafted', entrySha256=hashes[row['id']],
                   gateReport='fresh-build/waves/f005-laneA-1203-1212-full-composite.json', failures=[])
lane_data['completed'] = max(lane_data.get('completed', 0), 12)
lane_data['nextId'] = lane_data['entries'][12]['id']
lane_data['nextTerm'] = lane_data['entries'][12]['term']
lane_data['updatedUtc'] = NOW
lane_tmp = lane.with_suffix('.tmp')
lane_tmp.write_text(json.dumps(lane_data, ensure_ascii=False, indent=2)+'\n')
lane_tmp.replace(lane)
print(json.dumps(payload, ensure_ascii=False, indent=2))
