"""Reviewed low-residual #0c fixes for already merged entries."""

from __future__ import annotations

import json
from pathlib import Path

BUILD = Path(__file__).resolve().parent


def save(entry_id: str, edit) -> None:
    path = BUILD / "terms" / entry_id / "entry.v2.json"
    entry = json.loads(path.read_text(encoding="utf-8"))
    edit(entry)
    path.write_text(json.dumps(entry, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"updated {entry_id}")


def huatou(entry: dict) -> None:
    sense = entry["Senses"][0]
    sense["Explanation"] = "The first graph means speech or a saying (話); the second can mark a head or end and also serves as a noun-forming suffix (頭). In the passages grouped here, the compound names a word, question, or saying that someone is explicitly told to look at, raise, take up, remember, recite, or investigate. Dahui Zonggao says to 'look at a saying' (看箇話頭) and immediately supplies Zhaozhou's question, 'Does a dog have Buddha-nature or not?' (狗子還有佛性也無), answered 'no' (無). The corpus likewise supplies Zhaozhou's cypress tree in the courtyard, Yunmen's dry shit-stick, and other named exchanges after directions to look at a saying. Later records define the term directly. Hanyue Fazang says, 'What is called a saying is one matter or one thing before the eyes' (所謂話頭者，即目前一事一法也), and elsewhere says that this one question gathers all spoken and unspoken words of the worldly and world-transcending, therefore it is called the saying (只者一問，便收盡世出世間一切有言無言等語，故謂之話頭). Ruibai Mingxue says that what is spoken of as the saying is one's own native scenery, distinctly and singly bright, not interrupted by a hair's breadth (所言話頭者，即自己本地風光，歷歷孤明，不可絲毫間斷). A later retrospective reports that only in the Song did talk of looking at sayings and exerting effort arise (直至宋朝，始有看話頭、作工夫之說), and says people were told to take up a saying and look at what principle it was. The Discourse on Forging in the Chan Gate criticizes guarding a dead saying without raising doubt (死守話頭，不起疑情). Current allowlist counts show the range: the headword as 'saying' (話頭), 2,575 hits in 297 texts; 'investigate a saying' (參話頭), 187 in 76; 'look at a saying' (看話頭), 118 in 46; 'one saying' (一句話頭), 136 in 30; 'one's originally investigated saying' (本參話頭), 89 in 50; 'raise a saying' (舉話頭), 35 in 24; 'take up a saying' (提話頭), 32 in 24; 'the saying consisting of the word no' (無字話頭), 14 in 12; and 'dead saying' (死話頭), 40 in 33."
    sense["Note"] = "This is a word or saying in the Chinese record, not the name of a separate object, exercise, mantra, or category imported through Japanese. The sense is corpus-wide even though Dahui is a prominent early source: later masters define and deploy it independently, so historical prominence does not justify a master-specific SenseKey. Hanyue also records the comparison, 'In ancestral-teacher Chan it is called the saying; among Confucians it is called investigating things' (在祖師禪謂之話頭，在儒家謂之格物). Tiemei's record asks separately about the head, tail, and waist of a saying (話頭、話尾、話腰). The corpus has only two hits for 'saying-tail' (話尾) and one for 'saying-waist' (話腰), so those forms are recorded as wordplay rather than general definitions."


def distinguish(entry: dict) -> None:
    sense = entry["Senses"][0]
    sense["Explanation"] = sense["Explanation"].replace("分 to divide + 別 to distinguish/separate", "To divide (分) and to distinguish or separate (別)")
    sense["Note"] = sense["Note"].replace("The 宗鏡錄 (Records of the Source-Mirror) witness by 永明延壽 (Yongming Yanshou)", "The Records of the Source-Mirror (宗鏡錄) witness by Yongming Yanshou (永明延壽)")
    notes = sense["Occurrences"]
    notes[0]["AttributionNote"] = notes[0]["AttributionNote"].replace("六祖大師法寶壇經", "Platform Sutra of the Sixth Ancestor's Dharma Treasure (六祖大師法寶壇經)")
    notes[1]["AttributionNote"] = notes[1]["AttributionNote"].replace("大慧普覺禪師語錄 (Dahui Zonggao)", "Recorded Sayings of Chan Master Dahui Pujue (大慧普覺禪師語錄), by Dahui Zonggao")
    notes[2]["AttributionNote"] = "Dharma Altar of Chan Master Shiyu (石雨禪師法檀), the record of Shiyu Mingfang (石雨明方), in an address to the assembly (示眾). The passage says that marks arise with second-thought distinguishing and are absent apart from it (分別). Shiyu Mingfang is absent from the roster, so MasterName is null."
    notes[3]["AttributionNote"] = notes[3]["AttributionNote"].replace("宗鏡錄", "Records of the Source-Mirror (宗鏡錄)")


def master_in_charge(entry: dict) -> None:
    sense = entry["Senses"][0]
    sense["Explanation"] = sense["Explanation"].replace("主人 = host / master of a house / the one in charge; 公 = a personifying honorific ('sir, master'). 主人公 is 'the master-in-charge,' personified.", "Host, master of a house, or the one in charge (主人), plus a personifying honorific such as 'sir' or 'master' (公), forms the personified 'master-in-charge' (主人公).")
    sense["Note"] = sense["Note"].replace("Cross-ref 無位真人 ('true person of no rank')", "Compare 'true person of no rank' (無位真人)").replace("主人公 (master-in-charge)", "master-in-charge (主人公)").replace("the graph-variant 主人翁 (same word, with 翁 'old man' as the honorific)", "the graph variant 'master-in-charge' (主人翁), with 'old man' as the honorific (翁)")
    sense["Occurrences"][2]["AttributionNote"] = sense["Occurrences"][2]["AttributionNote"].replace("the 無位真人 cross-reference", "the cross-reference to 'true person of no rank' (無位真人)")


def native_scenery(entry: dict) -> None:
    sense = entry["Senses"][0]
    sense["Explanation"] = sense["Explanation"].replace("本地 = one's own ground / native place; 風光 = scenery / the view.", "One's own ground or native place (本地), plus scenery or view (風光):")
    sense["Explanation"] = sense["Explanation"].replace("(allowlist, grep-verified): 踏著/\"step onto,\" (蹋著本地風光 23×", "(allowlist, grep-verified): 'step onto the native scenery' (蹋著本地風光, 23×")
    sense["Occurrences"][0]["AttributionNote"] = sense["Occurrences"][0]["AttributionNote"].replace("佛果克勤禪師心要 (the Essentials of Mind of Chan Master Foguo Keqin", "Essentials of Mind of Chan Master Foguo Keqin (佛果克勤禪師心要")
    sense["Occurrences"][1]["AttributionNote"] = sense["Occurrences"][1]["AttributionNote"].replace("圓悟佛果禪師語錄 (the recorded sayings of Chan Master Yuanwu Foguo)", "Recorded Sayings of Chan Master Yuanwu Foguo (圓悟佛果禪師語錄)")
    sense["Occurrences"][2]["AttributionNote"] = sense["Occurrences"][2]["AttributionNote"].replace("大慧普覺禪師語錄 (the recorded sayings of Chan Master Dahui Pujue)", "Recorded Sayings of Chan Master Dahui Pujue (大慧普覺禪師語錄)")
    sense["Occurrences"][3]["AttributionNote"] = "Recorded Sayings of Chan Master Miyun (密雲禪師語錄). A nun asks and the master answers, so MasterName is null under the two-speaker rule. The answerer is Miyun Yuanwu: 'it has always been going in and out at your face-gate' (嘗在汝面門出入)."


def before_eyes(entry: dict) -> None:
    sense = entry["Senses"][0]
    sense["Explanation"] = "Eye (目) plus in front or before (前): before the eyes, or what is right in front of someone. The headword occurs 3,984 times in 360 allowlist texts. Its best-known deployment is Jiashan Shanhui's formula: 'before your eyes there is no dharma; the meaning is right before your eyes; it is not the dharma before your eyes, not what eye and ear reach' (目前無法，意在目前；不是目前法，非耳目之所到). Lamp records place the statement in Jiashan's section, while later compilations raise it with the heading 'Jiashan addressed the assembly, saying' (夾山示眾云). Its clauses recur widely: 'before your eyes there is no dharma' (目前無法), 262 occurrences in 120 texts; 'it is not the dharma before your eyes' (不是目前法), 222 in 112; and 'not what eye and ear reach' (非耳目之所到), 181 in 95. Other forms point to this location: 'only right before you' (只在目前), 166; 'the Great Way is only right before you, yet what is before you is hard to see' (大道只在目前，要且目前難覩); and 'clearly right before you' (分明在目前), 115. The location can also be emptied in the line 'before your eyes there is no monk-you; here there is no old teacher' (目前無闍黎，此間無老僧), whose first clause occurs 78 times. The word remains a plain locative throughout these deployments; it does not name a state in which to dwell."
    sense["Note"] = "One corpus-wide locative sense: 'before the eyes' or 'in front of you.' Jiashan Shanhui (夾山善會) does not give the word a private meaning; he uses its ordinary locative force in the frequently cited formula 'before your eyes there is no dharma' (目前無法). The formula receives Jiashan's MasterName only where the text's own heading places it in his record; raised versions remain null."
    notes = sense["Occurrences"]
    notes[0]["AttributionNote"] = "Jingde-era Record of the Transmission of the Lamp (景德傳燈錄), in Jiashan Shanhui's own entry under the heirs of Chuanzi Decheng (船子德誠法嗣). Jiashan speaks the formula 'before your eyes there is no dharma' (目前無法) to Daowu, who then smiles."
    notes[1]["AttributionNote"] = "Collected Pearls Linking Verses on Old Cases in the Chan Lineage (禪宗頌古聯珠通集). The same formula is raised with the explicit heading 'Jiashan addressed the assembly, saying' (夾山示眾云). As a raised case, MasterName is null."
    notes[2]["AttributionNote"] = "Compendium of the Five Lamps (五燈會元). A master cites the same clauses in a hall address, introducing them with 'this old monk says' (老僧道). This is a raised formula, so MasterName is null."
    notes[3]["AttributionNote"] = "Complete Book of the Five Lamps (五燈全書). The statement says that the Great Way is only right before you, yet what is before you is hard to see (大道只在目前，要且目前難覩)."
    notes[4]["AttributionNote"] = "Combined Essentials of the Five Lamps (聯燈會要). 'Only right before you' (只在目前) appears as an answer in a raised dialogue between an emperor and Manjusri (文殊)."
    notes[5]["AttributionNote"] = "Complete Book of the Five Lamps (五燈全書). The exchange uses the emptied-locus line 'before your eyes there is no monk-you; here there is no old teacher' (目前無闍黎，此間無老僧)."


def whisk(entry: dict) -> None:
    sense = entry["Senses"][0]
    sense["Explanation"] = "To brush or whisk away (拂), plus a noun suffix (子): a whisk, specifically the fly-whisk carried and handled by a Chan master. The headword occurs 11,115 times in 378 allowlist texts. The record repeatedly makes it the object of a visible action: raise the whisk (豎起拂子 / 竪起拂子, about 999 occurrences), strike with it (擊拂子, 1,068), strike the Chan seat with it (拂子擊禪床, 288), lift it (舉拂子, 871), pick it up (拈拂子, 368), throw it down (擲下拂子, 276), or wave it (揮拂子, 343). A master draws a circle with the whisk (拂子打圓相, 208) or speaks of what is on its tip (拂子頭, 374), as in the statement that all the sages from of old are on this mountain monk's whisk-tip (從上許多賢聖，緫在山僧拂子頭上). A fixed naming test says, 'call it a whisk and you offend; do not call it a whisk and you turn your back on it' (喚作拂子則觸，不喚作拂子則背), occurring 31 times in 26 texts. An impossible-object form, 'tortoise-hair whisk' (龜毛拂子, 106), is paired with 'rabbit-horn staff' (兔角拄杖, 28). The implement appears beside the staff (拄杖), bamboo stick (竹篦), and wish-fulfilling scepter (如意). The records describe the physical object and what speakers do with it without assigning it a hidden symbolic meaning."
    sense["Note"] = "One corpus-wide sense: the physical fly-whisk and the gestures performed with it. The curated passages show raising and throwing it down, the naming test, the tortoise-hair variant, the whisk-tip, and striking the Chan seat. They are narrated exchanges, recollections, or generic hall acts, so MasterName remains null. Compare the staff (拄杖), bamboo stick (竹篦), and wish-fulfilling scepter (如意)."
    notes = sense["Occurrences"]
    notes[0]["AttributionNote"] = "Transmission Lamp Jade-Flower Collection (傳燈玉英集), section on Huangbo Xiyun of Hongzhou (洪州黃蘗希運禪師). Huangbo asks Baizhang Huaihai what dharma he uses to show people; Baizhang raises the whisk (豎起拂子) and then throws it down (拋下拂子). This is a narrated two-speaker case."
    notes[2]["AttributionNote"] = "The impossible-object variant 'tortoise-hair whisk' (龜毛拂子) is paired with 'rabbit-horn staff' (兔角拄杖). The occurrence is in dialogue, and the section master for this line is not confirmed, so MasterName is null."
    notes[3]["AttributionNote"] = "Old Recorded Sayings of Venerable Masters (古尊宿語錄). In a hall act, the master lifts the whisk and says that all the sages from of old are on this mountain monk's whisk-tip (拂子頭)."
    notes[4]["AttributionNote"] = "Old Recorded Sayings of Venerable Masters (古尊宿語錄). The stage direction says that the master strikes the Chan seat with the whisk (拂子擊禪床), a recurrent hall gesture."


def sudden_awakening(entry: dict) -> None:
    sense = entry["Senses"][0]
    sense["Explanation"] = "All at once or sudden (頓), plus to awaken (悟): to awaken all at once. The Platform Sutra defines it with the line that self-nature awakens of itself, sudden awakening and sudden cultivation, with no gradual steps (自性自悟，頓悟頓修，亦無漸次). Its standing contrast is gradual (漸). The lamp record explains 'Southern sudden, Northern gradual' (南頓北漸) as a difference in how people are led to awaken, not as original north and south labels within Chan (開導發悟有頓漸之異，故曰南頓北漸，非禪宗本有南北之號也). In encounter narratives it appears in the formula 'awakened all at once at the words' (言下頓悟), where a recorded statement immediately precedes the awakening. Later Chinese Chan records pair sudden and gradual across awakening and cultivation. Records of the Source-Mirror gives four propositions: gradual cultivation and sudden awakening, sudden awakening and gradual cultivation, gradual cultivation and gradual awakening, and sudden awakening and sudden cultivation (漸修頓悟 / 頓悟漸修 / 漸修漸悟 / 頓悟頓修). Another pairing states that principle must be awakened to all at once while affairs are removed gradually (理須頓悟，事在漸除). A Chinese Chan succession chart defines the term by saying that turning from delusion to awakening is sudden and turning an ordinary person into a sage is sudden awakening (從迷而悟即頓，轉凡成聖即頓悟也), then describes beginningless confusion that takes the four elements as the body and deluded thinking as the mind. Dazhu Huihai's book title also preserves the term: Treatise on the Essential Gate of Entering the Way by Sudden Awakening (頓悟入道要門論). The allowlist count is 878 hits in 183 texts; 'awakened all at once at the words' (言下頓悟) occurs 57 times, 'sudden awakening and gradual cultivation' (頓悟漸修) 14, and 'sudden awakening and sudden cultivation' (頓悟頓修) 13."
    sense["Note"] = "A corpus-wide Chinese Chan term defined through the sudden and gradual contrast (頓 / 漸). The entry distinguishes awakening all at once (頓悟) from cultivation all at once (頓修): the recorded phrase 'sudden awakening and gradual cultivation' (頓悟漸修) deliberately separates them. Korean Seon material formerly cited here has been removed under the Chinese-Chan-only scope; the remaining evidence is from Chinese Chan texts and Chinese Chan discussions preserved in the allowlist."
    sense["Occurrences"][1]["AttributionNote"] = "Jingde-era Record of the Transmission of the Lamp (景德傳燈錄), in an imperial dialogue. It explains 'Southern sudden, Northern gradual' (南頓北漸) as a difference between sudden and gradual (頓 / 漸) in leading people to awaken, not an original north-south division in Chan."
    sense["SourceTexts"] = [value for value in sense.get("SourceTexts", []) if value != "T/T48/T48n2020.xml"]


def take_care(entry: dict) -> None:
    sense = entry["Senses"][0]
    sense["Explanation"] = "To treasure or hold precious (珍), joined to value or regard highly (重): 'take care.' In recorded sayings it is a stock closing word. A master ends a hall address, informal convocation, address to the assembly, or whisk talk (上堂 / 小參 / 示眾 / 秉拂) with it and steps down. Recurrent formulas include 'you have stood long, kind assembly; take care; he stepped down' (久立眾慈，伏惟珍重，下座) and the shorter 'Take care; he then stepped down' (珍重便下座). It is also the parting word between people in a recorded dialogue: a departing monk says 'take care' and leaves (珍重而去). In letters and memorials, the respectful closing 'I humbly hope you take care' (伏惟珍重) occurs 287 times. Other forms are 'each of you take care' (各自珍重) and 'assembly, take care' (大眾珍重). The records use it to end an address, exchange, or letter without adding a technical gloss."
    sense["Note"] = "A formulaic close and parting word, not a doctrinal term. MasterName is null in the curated passages because the word appears as a stock dismissal or a departing monk's farewell. It commonly precedes stepping down from the seat (下座) and closes hall addresses, informal convocations, addresses to the assembly, and whisk talks."
    notes = sense["Occurrences"]
    notes[0]["AttributionNote"] = "Record of Guiding Principles from the Patriarchs (列祖提綱錄). The formula closes a whisk talk (秉拂) at the end of a retreat: the assembly is dismissed and the master steps down."
    notes[1]["AttributionNote"] = "Old Recorded Sayings of Venerable Masters (古尊宿語錄). The word closes a hall address (上堂) in the short formula 'take care; he then stepped down' (珍重便下座)."
    notes[2]["AttributionNote"] = "Jingde-era Record of the Transmission of the Lamp (景德傳燈錄), section on the heirs of Guichen (桂琛). After raising an old worthy's expedient, the speaker says 'take care' and steps down."
    notes[3]["AttributionNote"] = "Recorded Sayings of Chan Master Fenyang Wude (汾陽無德禪師語錄), the record of Fenyang Shanzhao. In dialogue, a monk says 'take care' and leaves (珍重而去); Fenyang then supplies an alternate reply. The departing monk speaks the farewell, so MasterName is null."


def staff_blow(entry: dict) -> None:
    sense = entry["Senses"][0]
    sense["Explanation"] = "A stick, cudgel, or staff (棒); in Chan records, both the wooden striking staff a master wields and the blow struck with it. It is counted as 'one blow' (一棒, 2,954 occurrences), 'thirty blows' (三十棒, 2,766), 'twenty blows' (二十棒, 407), or 'a bout of the staff' (一頓棒, 63). To 'eat the staff' means to be struck (喫棒, 1,191), and the question 'does he deserve the staff or not?' (合喫棒不合喫棒) asks whether a blow is due. The implement is paired with the shout (喝) in 'staff and shout' (棒喝, 992) and 'staff and shout racing together' (棒喝交馳, 185). The recurring emblem is 'Deshan's staff, Linji's shout' (德山棒臨濟喝, 27), also listed beside Tianhuang's cakes and Zhaozhou's tea. A formula attributed broadly to Deshan, and specifically in some witnesses to the second-generation Deshan Yuanming, says: speak and receive thirty blows; do not speak and receive thirty blows (道得也三十棒，道不得也三十棒). Another report says that under Deshan's staff the bottom fell out like a bucket (德山棒下，如桶底脫). The line 'under the staff, the forbearance of no-arising; meeting the event, one does not defer to the teacher' (棒下無生忍，臨機不讓師) belongs to Nanyuan Huiyong's exchange in the account of Fengxue Yanzhao. The graph occurs 18,109 times in 415 allowlist texts. It is distinct from the held or planted staff (拄杖), which a master raises or sets down rather than using primarily as a striking implement."
    sense["Note"] = "One corpus-wide word covering the implement and its blow. Do not cross-attribute its formulas: the emblem 'Deshan's staff' (德山棒) refers to Deshan Xuanjian, while some witnesses assign the thirty-blows address to the second-generation Deshan Yuanming. The no-arising line belongs to Nanyuan Huiyong. Compare the shout (喝), staff and shout (棒喝), 'thereupon struck' (便打), and the held staff (拄杖)."
    notes = sense["Occurrences"]
    notes[0]["AttributionNote"] = "Extensive Record of Tianmu Zhongfeng (天目中峰廣錄), by Zhongfeng Mingben. A device list sets Deshan's staff beside Linji's shout, Tianhuang's cakes, and Zhaozhou's tea. As a commentarial list, MasterName is null."
    notes[1]["AttributionNote"] = "Collected Pearls Linking Verses on Old Cases in the Chan Lineage (禪宗頌古聯珠通集). A raised case gives Deshan's thirty-blows address (三十棒) and reports Linji's comment to Luopu. Other witnesses specify the second-generation Deshan Yuanming."
    notes[2]["AttributionNote"] = "Old Recorded Sayings of Venerable Masters (古尊宿語錄). Nanyuan Huiyong picks up the staff and speaks the line 'under the staff, the forbearance of no-arising' (棒下無生忍) in the account of Fengxue Yanzhao's awakening."
    notes[3]["AttributionNote"] = "Extensive Record of Tianmu Zhongfeng (天目中峰廣錄). Zhongfeng raises the staff and shout (棒喝) associated with Dajue and Xinghua Cunjiang, witnessing the phrase 'staff and shout racing together' (棒喝交馳)."
    notes[4]["AttributionNote"] = "Blue Cliff Record of Chan Master Foguo Yuanwu (佛果圜悟禪師碧巖錄). Inside Yuanwu's commentary, Xuefeng's quoted report says that under Deshan's staff it was as if a bucket's bottom fell out (德山棒下，如桶底脫)."
    notes[5]["AttributionNote"] = "Patriarchs' Hall Collection (祖堂集). A master's test asks whether someone deserves the staff or not (合喫棒，不合喫棒), showing the verbal form 'eat the staff' (喫棒), meaning receive a blow."


def mind_to_mind(entry: dict) -> None:
    sense = entry["Senses"][0]
    sense["Explanation"] = "By means of (以) mind (心), transmit (傳) mind (心): transmitting mind by mind. In the Platform Sutra, when the Fifth Patriarch hands Huineng the robe, he says, 'As for the Dharma, it is transmitted mind by mind; all are made to awaken and understand for themselves' (法則以心傳心，皆令自悟自解). The phrase names the mode of patriarchal transmission. The Jingde-era lamp record says that after the Buddha's nirvana the Dharma was entrusted to Kasyapa and transmitted mind by mind (佛滅後付法於迦葉，以心傳心). Dahui says that each transmitted mind by mind in unbroken succession (各各以心傳心，相續不斷). The phrase repeatedly stands beside 'not setting up words and letters' (不立文字), as in the statement that Bodhidharma came from the west, transmitted mind by mind, and did not set up words and letters (達磨西來，以心傳心，不立文字). Dahui contrasts it with transmission by language: the sages from of old had no transmission by words and spoke only of transmitting mind by mind (從上諸聖，無言語傳授，只說以心傳心而已). The record also preserves criticism of the phrase itself. One address says, 'Driving out a wedge with a wedge turns into a beaten track; transmitting mind by mind, the disease turns deeper still' (以榍出榍，翻成途轍；以心傳心，其病轉深). The allowlist contains 110 occurrences in 56 texts."
    sense["Note"] = "A corpus-wide lineage phrase, commonly paired with 'not setting up words and letters' (不立文字) and contrasted with transmission by language (以語言傳授). The record deploys the same phrase affirmatively and under criticism; the critical use is not a separate lexical meaning."
    notes = sense["Occurrences"]
    notes[0]["AttributionNote"] = "Platform Sutra of the Sixth Ancestor's Dharma Treasure (六祖大師法寶壇經). In the robe-transmission scene, Hongren tells Huineng that the Dharma is transmitted mind by mind and each is made to awaken and understand for himself (法則以心傳心，皆令自悟自解)."
    notes[1]["AttributionNote"] = "Combined Essentials of the Five Lamps (聯燈會要). The standing collocation joins 'transmitting mind by mind' (以心傳心) to 'not setting up words and letters' (不立文字) and attributes the arrival from the west to Bodhidharma."
    notes[2]["AttributionNote"] = "Recorded Sayings of Chan Master Dahui Pujue (大慧普覺禪師語錄), instruction to Layman Zhitong. Dahui says the sages from of old had no transmission by words, only transmitting mind by mind, and contrasts it with contemporary learned interpretation passed through language (從上諸聖，無言語傳授，只說以心傳心而已；今時多是師承學解)."
    notes[3]["AttributionNote"] = "Jingde-era Record of the Transmission of the Lamp (景德傳燈錄). In a raised question, it says that after the Buddha's nirvana the Dharma was entrusted to Kasyapa and transmitted mind by mind down through the patriarchs."
    notes[4]["AttributionNote"] = "Combined Essentials of the Five Lamps (聯燈會要), in an address to the assembly. The phrase is criticized: transmitting mind by mind makes the disease turn deeper (以心傳心，其病轉深), paired with driving out a wedge by means of a wedge."


def true_dharma_eye(entry: dict) -> None:
    sense = entry["Senses"][0]
    sense["Explanation"] = "True or correct (正), Dharma or the teaching (法), and eye (眼): the true Dharma eye, the eye that sees the Dharma correctly. As a standalone term, distinct from 'treasury of the true Dharma eye' (正法眼藏), it is something one possesses, lacks, brightens, or blinds. The forms 'possess the true Dharma eye' (具正法眼, 15 occurrences) and 'better to possess the true Dharma eye' (不如具正法眼好) name its possession. Its absence is described in the line 'one who does not possess the true Dharma eye misses at every turn' (非具正法眼者，頭頭蹉過). Its presence verifies: 'if one possesses the true Dharma eye, one will verify the nirvana-mind' (若具正法眼，必證涅槃心). The preface to Linji's record says that Linji illuminated the nirvana-mind by means of the true Dharma eye (臨濟祖師以正法眼明涅槃心). It can also be blinded (瞎却正法眼). Most raw occurrences of the string belong to the longer compound 'treasury of the true Dharma eye' (正法眼藏, 1,381 occurrences), so this entry is deliberately limited to the standalone eye."
    sense["Note"] = "One corpus-wide standalone sense. Every curated occurrence uses 'true Dharma eye' (正法眼) without the following graph 'treasury' (藏). The longer compound 'treasury of the true Dharma eye' (正法眼藏) has its own entry and is related literally as the treasury of this eye."
    notes = sense["Occurrences"]
    notes[0]["AttributionNote"] = "Old Recorded Sayings of Venerable Masters (古尊宿語錄), admonitions on questioning (誡問話). The line says that it is better to possess the true Dharma eye (不如具正法眼好)."
    notes[1]["AttributionNote"] = "Empty Hall Collection, verses by Danxia Chun with comments by old Linquan (林泉老人評唱丹霞淳禪師頌古虗堂集), case 26, Luopu (洛浦). The comment says that one who lacks the true Dharma eye misses at every turn."
    notes[3]["AttributionNote"] = "Recorded Sayings of Chan Master Linji Huizhao of Zhenzhou (鎮州臨濟慧照禪師語錄), in the preface. The author says that Linji illuminated the nirvana-mind by means of the true Dharma eye (故臨濟祖師以正法眼明涅槃心). This is about Linji but is preface prose, so MasterName is null."
    notes[4]["AttributionNote"] = "Continuation of the Lamp Record (續傳燈錄), section on the heirs of Huanglong Zhen (黃龍震法嗣). A hall verse speaks of blinding the true Dharma eye (瞎却正法眼)."


def attached_words(entry: dict) -> None:
    sense = entry["Senses"][0]
    sense["Explanation"] = "To attach, apply, or lay on (著), plus words (語): 'attached words,' a short remark appended by a commentator to a case, one line of dialogue, or a verse. The headword occurs 315 times in 119 allowlist texts; the formula 'attached the words, saying' (著語云) occurs 141 times in 42. It belongs to the commentarial device set beside picking up old cases (拈古), verses on old cases (頌古), words supplied in another's place (代語), and prose exposition (評唱). Blue Cliff Record names and describes the device twice. Of Xuedou's remark 'Seen through!' it says: 'Xuedou attached the words, saying, Seen through! It is like an iron peg. In the assembly this is called attached words. Though it is on both sides, it does not dwell on both sides' (雪竇著語云，勘破了也，一似鐵橛相似；眾中謂之著語，雖然在兩邊，却不住在兩邊). Elsewhere it repeats that this is called attached words and that, although they land on both sides, they do not dwell there (此謂之著語，落在兩邊，雖落在兩邊，却不住兩邊). The regular form names an author, a point in a case, and the appended phrase. Xuedou attaches 'Stuff up his nostrils' at the words 'do not think falsely' (雪竇顯於莫妄想處著語云：塞却鼻孔); Dahui attaches 'No small tangle of vines' at the point where someone steps down (徑山杲於下座處著語云：葛藤不少). The author can be anonymous, as in 'an old worthy attached the words, saying' (古德著語云). Living masters and writers are also asked to attach words to sets of questions or at the end of a scroll."
    sense["Note"] = "A commentarial device, not generic speech. Render it as 'attached words,' retaining the corpus's own definition 'in the assembly this is called attached words' (眾中謂之著語). Neighboring devices are words supplied in another's place (代語), laying down words (下語), and a verse on an old case (頌古). It is attested in Blue Cliff Record (碧巖錄), Hongzhi's Extensive Record (宏智禪師廣錄), Eye of Humans and Gods (人天眼目), and Collected Old Cases Raised in the Lineage (宗門拈古彙集)."
    notes = sense["Occurrences"]
    notes[0]["AttributionNote"] = "Blue Cliff Record (碧巖錄), Yuanwu's prose exposition on case four, Deshan carrying his bundle. Yuanwu describes Xuedou's attached words 'Seen through!' and defines the device with 'in the assembly this is called attached words' (眾中謂之著語)."
    notes[1]["AttributionNote"] = "Hongzhi's Extensive Record (宏智禪師廣錄), in the hall-address record of Master Jue of Mount Tiantong. Xuedou attaches words to a raised case in the main running text, showing the formula 'attached the words, saying' (著語云) followed by a terse phrase."
    notes[2]["AttributionNote"] = "Collected Old Cases Raised in the Lineage (宗門拈古彙集). Xuedou Chongxian attaches words at the 'do not think falsely' point of the case (雪竇顯於莫妄想處著語云)."
    notes[3]["AttributionNote"] = "Collected Old Cases Raised in the Lineage (宗門拈古彙集). Dahui Zonggao, named as Jingshan Gao (徑山杲), attaches words at the point where the speaker steps down."


def main() -> None:
    save("t_d190cf45c531", huatou)
    save("t_15026800437e", distinguish)
    save("t_16140def874d", master_in_charge)
    save("t_831f84399d0b", native_scenery)
    save("t_937f63a4fb51", before_eyes)
    save("t_9f119d7965c2", whisk)
    save("t_ebb0995c99fc", sudden_awakening)
    save("t_ada407625f42", take_care)
    save("t_f25cebd24730", staff_blow)
    save("t_d11d5f0c78a5", mind_to_mind)
    save("t_970c3f191929", true_dharma_eye)
    save("t_0a686fa27769", attached_words)


if __name__ == "__main__":
    main()
