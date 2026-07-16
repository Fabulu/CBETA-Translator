#!/usr/bin/env python3
"""Write reviewed hard-w2-b1 overrides for sources listed in DECISIONS."""
from __future__ import annotations
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / "maintenance/hard-bundle-inputs/w2-b1"
UTC = "2026-07-14T07:30:00Z"
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]

def m(name, title, note, context=None):
    d={"MasterName":name,"AttributionNote":f"{title}: {note}"}
    if context:d["ContextMasters"]=[{"MasterName":n,"Roles":r} for n,r in context]
    return d
def u(kind,label,role,title,note,context=None):
    d={"ActorAttribution":{"Status":"reviewed-unnamed","Kind":kind,"ActorLabel":label,"ActorRole":role,"RungsChecked":RUNGS,"ReviewedBy":"Codex hard-w2-b1","ReviewedUtc":UTC},"AttributionNote":f"{title}: {note} The line, expanded context, section header, book title, TEI header, and parallel-passage search do not give a personal name for the {label}."}
    if context:d["ContextMasters"]=[{"MasterName":n,"Roles":r} for n,r in context]
    return d
def imp(kind,label,title,note,grammar,context=None):
    d={"ActorAttribution":{"Status":"impersonal","Kind":kind,"ActorLabel":label,"ActorRole":"document voice","GrammarEvidence":grammar,"ReviewedBy":"Codex hard-w2-b1","ReviewedUtc":UTC},"AttributionNote":f"{title}: {note}"}
    if context:d["ContextMasters"]=[{"MasterName":n,"Roles":r} for n,r in context]
    return d

T25="The Extensive Record of Tianmu Zhongfeng (天目中峰廣錄)"
TZ="Ancestor Hall Collection (祖堂集)"
TF="Recorded Sayings of Chan Master Feiyin (費隱禪師語錄)"
TL="Recorded Sayings of Chan Master Yunxi Langting Ting (雲溪俍亭挺禪師語錄)"
TB="Recorded Sayings of Chan Master Baichi (百癡禪師語錄)"
TC="Connected Anthology of Verses on Old Chan Cases (禪宗頌古聯珠通集)"
TT="Recorded Sayings of the Monk Tianyin (天隱和尚語錄)"
TA="Recorded Sayings of Chan Master Tian'an Sheng (天岸昇禪師語錄)"
TG="Recorded Sayings of Chan Master Guxue Zhe (古雪哲禪師語錄)"
TY="Recorded Sayings of National Master Puji Yulin (普濟玉琳國師語錄)"
TM="Recorded Sayings of Chan Master Miyun (密雲禪師語錄)"
TD="Recorded Sayings of Ancient Worthies (古尊宿語錄)"
TX="Recorded Sayings of Chan Master Xueguan (雪關禪師語錄)"
TP="Recorded Sayings of Chan Master Poshan (破山禪師語錄)"
TS="Teaching Altar of Chan Master Shiyu (石雨禪師法檀)"
TZH="Recorded Sayings of Preceptor Zhuanyu Heng at Purple Bamboo Grove (紫竹林顓愚衡和尚語錄)"
TJD="Recorded Sayings of Chan Master Juelang Dasheng (天界覺浪盛禪師語錄)"
TGT="Collected Abridgment of the Recorded Sayings of Chan Master Guting (古庭禪師語錄輯略)"
TRB="Recorded Sayings of Chan Master Ruibai (入就瑞白禪師語錄)"
TSS="Recorded Sayings of Chan Master Sanshan Lai (三山來禪師語錄)"
TDR="Recorded Sayings of Chan Master Daxiu Zhu (大休珠禪師語錄)"
TEI="Recorded Sayings of Chan Master Eryin Mi (二隱謐禪師語錄)"
TYJ="Transmission of the Lamp, Jade Flowers Collection, fragment (傳燈玉英集（殘卷）)"
TWN="Further Questions of Chan Master Huangbo Wunian (黃蘗無念禪師復問)"
TFS="Recorded Sayings of Chan Master Fushi (浮石禪師語錄)"
TBW="Essential Collection of Boshan Wuyi's Recorded Sayings (博山無異大師語錄集要)"
TQY="Recorded Sayings of Preceptor Qianyan (千巖和尚語錄)"
TYG="Recorded Sayings of Chan Master Yingning Jing (攖寧靜禪師語錄)"
THJ="Northern Travels Collection of Chan Master Hongjue Min of Tiantong (天童弘覺忞禪師北遊集)"
TFX="Recorded Sayings of Chan Master Faxi Yin (法璽印禪師語錄)"
TFH="Selected Records of the Five Houses (五家語錄（選錄）)"
TWH="Recorded Sayings of Chan Master Wuhuan (無幻禪師語錄)"
TSY="Recorded Sayings of Chan Master Sanyi Mingyu (三宜盂禪師語錄)"
TJW="Recorded Sayings of Chan Master Jie Weizhou (介為舟禪師語錄)"
TJA="Recorded Sayings of Chan Master Jie'an Wujin (介菴進禪師語錄)"
TSD="Recorded Sayings of Chan Master Shending Yunwai Ze (神鼎雲外澤禪師語錄)"
TSW="Recorded Sayings of Wuming of Shouchang (壽昌無明和尚語錄)"
TSE="Recorded Sayings of Chan Master Shishuang Erzhan (石霜爾瞻尊禪師語錄)"
TWZ="Recorded Sayings of Chan Master Wolong Zishui (夔州臥龍字水禪師語錄)"
TYC="Recorded Sayings of Chan Master Yichu Yuan (一初元禪師語錄)"
TTB="Recorded Sayings of Chan Master Tiebi Ji of Qingzhong (慶忠鐵壁機禪師語錄)"
TZX="Recorded Sayings of Chan Master Zixian Jue (自閒覺禪師語錄)"
DECISIONS={
"B25n0145":{
"t_68d495f2868b:0688b13:2:2":imp("imperial decree","imperial decree",T25,"the quoted imperial decree orders that Zhongfeng Mingben's collected writings be carved for every Tripitaka printing-block collection.","The occurrence is inside a formally introduced 聖旨 whose command voice governs 教刊板入藏經; it is documentary speech rather than a Chan master's turn.",[("Zhongfeng Mingben",["subject-of-decree"])]),
"t_ab6276be6e08:0696a13:1:5":m("Zhongfeng Mingben",T25,"Zhongfeng Mingben criticizes people who call seated or standing death the 'final phrase' in his own extended address."),
"t_8bd6933e6de3:0734b05:1:3":m("Zhongfeng Mingben",T25,"Zhongfeng Mingben says that boundless lands, the ten ages, teaching gates, and samadhis are all gathered within this one blow and one shout."),
"t_3a0a4e68cf13:0741a10:1:3":m("Zhongfeng Mingben",T25,"Zhongfeng Mingben lists proliferated labels for Chan, including 'entangling-vine Chan,' before criticizing the multiplication of names."),
"t_2dd4fec35455:0747b06:1:1":m("Zhongfeng Mingben",T25,"Zhongfeng Mingben tells a sick addressee to let the mind be like wood or stone and the intention like dead ashes."),
"t_4d4ce329367f:0796a17:1:4":m("Zhongfeng Mingben",T25,"Zhongfeng Mingben says that talking about food does not cure hunger and talking about clothing does not cure cold."),
"t_f2482d04a86a:0879a06:1:3":m("Zhongfeng Mingben",T25,"Zhongfeng Mingben's signed long Chan verse warns against violating precepts and says action and restraint can indeed be divided by permission and prohibition."),
"t_9a5dc768cbc5:0907b11:1:5":m("Nanquan Puyuan",T25,"inside a case raised and then discussed by Zhongfeng Mingben, Nanquan Puyuan answers Zhaozhou Congshen's question by saying 'ordinary mind is the Way.'",[("Zhaozhou Congshen",["quoted-questioner"]),("Zhongfeng Mingben",["later-raiser","commentator","record-owner"])])
},
"B25n0144":{
"t_cb3571346f22:0306a14:1:1":imp("compiler's narrative","compiler's narrative",TZ,"the compiler's narrative explains that the royal line took the surname Shakya and directly glosses Shakya as 'Able and Humane.'","The definition is third-person historical and translation prose following the king's quoted exclamation; no speech marker assigns it to the king or a master."),
"t_ad2c9d24126f:0364a03:1:1":m("Zhirong",TZ,"the section body names Zhirong (智榮; its table heading has the variant 智策) as the teacher marked 師 who questions Zhihuang's entering concentration, then says constant concentration should have no entry or exit.",[("Zhihuang",["respondent"]),("Huineng",["teacher-of-Zhirong","later-respondent"]) ]),
"t_1793c3514a69:0388a01:1:1":m("Zhenjue",TZ,"Zhenjue is named immediately before 舉問 and raises the eye-light statement as a question to Xuanwu.",[("Xuanwu",["respondent"])]),
"t_4cf045deab37:0548a01:1:1":m("Baoci Guangyun",TZ,"Baoci Guangyun answers an unnamed student's question by calling himself a gruel-and-rice monk."),
"t_936118ea496c:0550a09:1:1":m("Baoci Guangyun",TZ,"Baoci Guangyun asks whether five or six hundred people gathered to eat gruel and rice see alike or differently."),
"t_84e490b1773f:0592a08:1:1":m("Jinniu Heshang",TZ,"Jinniu Heshang brings food before the hall, claps his hands, dances, laughs, and calls the bodhisattva-children to eat.",[("Changqing Huileng",["later-respondent"]),("Dongshan Liangjie",["later-respondent"])]),
"t_283dce854520:0627b12:1:2":m("Cen Heshang",TZ,"Cen Heshang answers the question 'what is awakening wherever the eye meets?' first with 'all things constantly abide' and then by repeating the headword."),
"t_ff50c6974a36:0676a05:2:1":m("Gaitong",TZ,"inside the named teaching allegory composed in Shunzhi's section, the immortal Gaitong answers the wanderer that Samantabhadra is provisionally lodged in the five causal positions through the fruit-position but does not abide in them.",[("Shunzhi",["section-owner","authorial-frame"])])
},
"J26nB178":{
"t_dd2b39789323:0104b22:1:4":m("Feiyin Tongrong",TF,"Feiyin Tongrong comments after raising the Buddha's birth scene and Yunmen Wenyan's response that Yunmen clarifies a phrase beyond the standard."),
"t_6293dead3bb2:0104c22:1:6":m("Feiyin Tongrong",TF,"Feiyin Tongrong says in his teaching-seat address that the clear-eyed patchrobed monk has no road on which to turn around."),
"t_549e7766dfa1:0105b07:1:4":m("Feiyin Tongrong",TF,"Feiyin Tongrong raises his staff and says that this one move is presented face-to-face and must be personally taken in."),
"t_6293dead3bb2:0105b08:1:3":m("Feiyin Tongrong",TF,"Feiyin Tongrong continues the same staff verse: if one can turn around and shift one's step, one stands vividly alive in the thorn forest."),
"t_1f6124388d25:0106c28:1:5":m("Feiyin Tongrong",TF,"Feiyin Tongrong's autumn address reaches the stock image of pillars knitting their brows and lanterns rising to dance before he stops his own entangling talk with a shout."),
"t_fac9b9afebf6:0119a24:1:3":u("monk","unnamed monk","questioner",TF,"an unnamed monk asks how one shout is like a probing pole and shadowing grass; Feiyin Tongrong answers that he knows whether the monk is dragon or snake.",[("Feiyin Tongrong",["respondent","record-owner"]) ]),
"t_dd2b39789323:0132c18:1:5":u("monk","unnamed monk","questioner",TF,"an unnamed monk sets aside the upward function and phrase beyond the standard and asks Feiyin Tongrong to present the exact intent of the Five Houses.",[("Feiyin Tongrong",["respondent","record-owner"]) ])
},
"J33nB294":{
"t_300236cb6368:0726a02:1:5":m("Langting Jingting",TL,"Langting Jingting, in his ascent-to-the-hall address, calls the flying kites, leaping fish, standing mountains, and flowing water an excellent encounter and warns the assembly not to miss it face-to-face."),
"t_592227b212c1:0727b12:1:4":m("Langting Jingting",TL,"Langting Jingting, in his own informal address, compares the matter to raising silkworms and says that after extinction and renewed revival one comes alive within death and the thread can issue."),
"t_a38d5c680c67:0734b12:1:2":m("Langting Jingting",TL,"Langting Jingting says that, without understanding, the Qin-era diamond drill is something one cannot chew through."),
"t_b90a5f36ec86:0735a12:1:5":m("Langting Jingting",TL,"Langting Jingting rebukes people who manipulate the conscious spirit and stand all day guarding a dead corpse."),
"t_2069b9c33315:0745b19:1:5":m("Langting Jingting",TL,"in a tea conversation with the monks called Nanshan and Tianmu, Langting Jingting argues that Caoshan Benji answered the immediate dialogue and that later readers wrongly classified it as a fivefold guest-and-minister scheme.",[("Caoshan Benji",["master-discussed"])]),
"t_5ddde30711a4:0747b03:1:3":u("monk","unnamed monk","questioner",TL,"an unnamed monk asks what the golden lock on the dark road is; Langting Jingting answers 'this side, that side.'",[("Langting Jingting",["respondent","record-owner"]) ]),
"t_cba9cbb44845:0748c19:1:2":m("Longmen Huai",TL,"the preceding line identifies the visitor as Longmen Huai (龍門懷姪); he asks what discernment supports the monk in the old-woman-burns-the-hermitage case.",[("Langting Jingting",["respondent","record-owner"]),("Dongshan Qing",["co-respondent"])]),
"t_a38d5c680c67:0751b22:1:3":m("Langting Jingting",TL,"Langting Jingting answers an unnamed questioner that Wujin could not chew through the bowl-carrying public case for his entire life."),
"t_e156057131dc:0755c14:1:4":m("Langting Jingting",TL,"in his instruction to the attendant Qianyun, Langting Jingting says that Qianyun's doubt has lacked urgency, that he has cast aside the line of his primary investigation, and that he must now place the saying before him."),
"t_cba9cbb44845:0762b29:1:3":m("Langting Jingting",TL,"in his own old-case comment, Langting Jingting says that the old woman had not shed ordinary airs and that the hermitage-dweller should have recognized the approach and laughed.")
},
"J28nB202":{
"t_936118ea496c:0078c05:1:6":m("Baichi Yuan",TB,"Baichi Yuan, in his own informal address, lists studying Chan, eating gruel and rice, bodily functions, and ordinary movement as the listeners' own fundamental matter."),
"t_eba970114dd2:0136a28:2:2":m("Baichi Yuan",TB,"Baichi Yuan, in his inscription on the Record of Lu Sheng's Yellow-Millet Dream (題盧生黃粱夢記), recounts the Handan pillow dream and says that Lu Sheng awoke before the millet was cooked.")
},
"C078n1720":{
"t_2baf0ec63b2c:0634c11:1:5":m("Deyun Bhikshu",TC,"the anthology's named Sudhana episode makes Deyun Bhikshu the exact actor: after a seven-day search, Sudhana sees Deyun walking slowly in meditation on another peak."),
"t_8bced2c0bc2f:0668c02:1:2":m("Dahong En",TC,"the inline contributor note 大洪恩 assigns this verse to Chan master Dahong En, who contrasts the lion biting a person with the Korean hound chasing a clod."),
"t_6c20139c8cc0:0733a12:1:6":m("Shui'an Yi",TC,"the inline contributor note 水菴一 assigns this verse to Chan master Shui'an Yi, who says that one press collapses the silver mountain and iron wall."),
"t_592227b212c1:0773a11:1:5":m("Yuanwu Keqin",TC,"the inline contributor note 圓悟勤 assigns this verse to Yuanwu Keqin, who contrasts the many who come alive within death with the rarity of dying within life."),
"t_c0a6177c9c44:0780a11:2:2":m("Zhenjing Kewen",TC,"the inline contributor note 真淨文 assigns this verse to Zhenjing Kewen; he likens the exchange to drum and lute in concert, then says the old tune is not musical measure."),
"t_a326343ab7c3:0803a08:1:4":u("monk","unnamed monk","questioner",TC,"an unnamed monk tells Mingjiao Kuan that every day is a good day and every year a good year, then asks why there is no New Year Buddha-dharma.",[("Mingjiao Kuan",["respondent"])]),
"t_bf467ac18ec0:0824a16:2:2":m("Zhengjue Yi",TC,"the inline contributor note 正覺逸 assigns this verse to Chan master Zhengjue Yi; his own verse says that the Buddha's hand cannot cover the burning mountain answer."),
"t_e96268628f2c:0831a16:1:3":u("monk","unnamed monk","questioner",TC,"after the preceding Shimen Shaoyuan case, the anthology explicitly introduces Wenshu Yingzhen; an unnamed monk asks him where the ten thousand things return when they return to one, and Wenshu answers 'the Yellow River's nine bends.'",[("Wenshu Yingzhen",["respondent"])]),
"t_d1e06fd225fa:0837a18:1:6":u("monastic visitor","unnamed room entrant","person entering for a private interview",TC,"the anthology narrates that Ciming Chuyuan placed a sword, sandals, and water in his room and challenged each unnamed visitor he saw entering for a private interview.",[("Ciming Chuyuan",["room-master","challenger"])] )
},
"J25nB171":{
"t_afebc7b2a221:0513b12:1:3":m("Tianyin Yuanxiu",TT,"Tianyin Yuanxiu closes his own public instruction by asking whether the assembly feels hair-cold and bone-chilled and warning them especially against conjecture."),
"t_fac9b9afebf6:0514c12:1:5":m("Tianyin Yuanxiu",TT,"Tianyin Yuanxiu supplies the stored comment on Baijuyi's reply in the raised Guizong case, calling it feigned drunkenness displaying sobriety and also a probing pole and shadowing grass."),
"t_6b8e3b4f44bb:0515b11:1:4":u("monk","unnamed monk","questioner",TT,"an unnamed monk asks Tianyin Yuanxiu what illumination and function operating simultaneously is; Tianyin answers with a shout and a blow.",[("Tianyin Yuanxiu",["respondent","record-owner"])]),
"t_d7167b5f3236:0520b16:1:4":m("Jiashan Shanhui",TT,"inside a case raised by Tianyin Yuanxiu, Jiashan Shanhui tells his assembly that Shishuang Qingzhu has a killing blade but no life-giving sword, whereas Yantou Quanhuo has both.",[("Tianyin Yuanxiu",["later-raiser","commentator"]),("Shishuang Qingzhu",["master-appraised"]),("Yantou Quanhuo",["master-appraised"])]),
"t_e5259ce8bbf5:0528c18:1:4":m("Tianyin Yuanxiu",TT,"Tianyin Yuanxiu gives the private-room indication and opens it with the contrast between not hearing the bell in deep sleep and hearing the drum upon waking."),
"t_8bd6933e6de3:0539c14:1:4":m("Guyin Zhiyan",TT,"Tianyin Yuanxiu explicitly quotes Guyin Zhiyan as saying that one shout distinguishes guest and host, illumination and function operate simultaneously, and noon strikes the third watch."),
"t_78f95517a347:0549c10:1:6":m("Tianru Weize",TT,"in a case raised by Tianyin Yuanxiu, Tianru Weize tells the assembly that birth-and-death is the great matter and that Chan worthies must paste those two words on their foreheads.",[("Tianyin Yuanxiu",["later-raiser","record-owner"])]),
"t_db4a932ce500:0564a27:1:3":u("monk","unnamed monk","questioner",TT,"in the case raised by Tianyin Yuanxiu, an unnamed monk asks Huayan Xiujing why a greatly awakened person nevertheless becomes confused; Huayan answers with the broken mirror and fallen flower.",[("Huayan Xiujing",["respondent"]),("Tianyin Yuanxiu",["later-raiser","record-owner"])])
},
"J26nB187":{
"t_73fb9441f4fb:0666c20:1:3":m("Tian'an Sheng",TA,"Tian'an Sheng says in his own evening address that a butcher becoming a Buddha on the spot is not called a hasty nature."),
"t_d9c587fad710:0668c02:1:3":u("monk","unnamed monk","questioner",TA,"an unnamed monk asks what it means that a clay Buddha does not cross water; Tian'an Sheng answers that it is submerged over its head.",[("Tian'an Sheng",["respondent","record-owner"])]),
"t_68d495f2868b:0700a08:1:2":u("personified object","personified staff","quoted speaker",TA,"Tian'an Sheng stages his staff as reporting three kinds of Jiangnan Chan; the personified staff defines block-print Chan as memorizing the records of Yuanwu Keqin, Dahui Zonggao, and Zhongfeng Mingben and producing something vaguely resembling them.",[("Tian'an Sheng",["framing-speaker","record-owner"]),("Yuanwu Keqin",["master-in-definition"]),("Dahui Zonggao",["master-in-definition"]),("Zhongfeng Mingben",["master-in-definition"]) ]),
"t_8bd6933e6de3:0711b10:1:8":u("monk","unnamed monk","person shouting",TA,"an unnamed monk gives one shout and then two more; Tian'an Sheng calls the first a good shout and the monk a true lion cub.",[("Tian'an Sheng",["respondent","record-owner"])]),
"t_167e8b0c7ba3:0739c08:1:5":m("Tian'an Sheng",TA,"in his own verse answering the layman Eryuan (次韻酬二願居士), Tian'an Sheng pairs talking about food without becoming full with the appropriateness of a painted cake."),
"t_4d4ce329367f:0739c08:1:5":m("Tian'an Sheng",TA,"in his own verse answering the layman Eryuan (次韻酬二願居士), Tian'an Sheng says that talking about food never makes one full and pairs it with a painted cake.")
},
"J28nB208":{
"t_b8d2633b12ef:0322c13:1:5":m("Guxue Zhe",TG,"after raising the Shoukuo and Fengxue case and interspersing comments, Guxue Zhe tells his assembly that both parties show a failure and both are adepts."),
"t_bb19ed0e0fab:0345b22:1:3":m("Guxue Zhe",TG,"Guxue Zhe gives the explicitly headed incident-related general address (因事普說), opening with a verse on the dispute between those asking him to leave and those asking him to stay."),
"t_9dfa307c0458:0362c13:1:4":u("questioner","unnamed questioner","questioner",TG,"an unnamed questioner asks first about hiding the body where there is no trace and then about not hiding the body where no trace remains; Guxue Zhe answers both questions.",[("Guxue Zhe",["respondent","record-owner"])])
},
"B27n0152":{
"t_df4e71aa0bc5:0521b16:1:3":m("Shakyamuni Buddha",TY,"within Yulin Tongxiu's extended argument, Shakyamuni Buddha is the grammatical actor who sees the morning star and completely awakens; Yulin then compares layman Zhang Wujin's immediate recognition.",[("Yulin Tongxiu",["framing-speaker","record-owner"])]),
"t_8bd6933e6de3:0540b15:1:5":m("Yulin Tongxiu",TY,"Yulin Tongxiu enters the east hall during an evening instruction, looks left and right, and issues a commanding shout before leaving."),
"t_d9c587fad710:0555a20:1:6":u("quoted source speaker","unnamed quoted source speaker","speaker of an anonymously transmitted counter-saying",TY,"after attributing the preceding non-crossing formula only to an old worthy, Yulin Tongxiu introduces this opposite crossing formula with 又有云, without naming its source speaker; the headword-bearing clay-Buddha clause belongs to that anonymously transmitted counter-saying.",[("Yulin Tongxiu",["raiser","commentator","record-owner"])]),
"t_eba970114dd2:0565a02:1:3":m("Yulin Tongxiu",TY,"Yulin Tongxiu warns that examining public cases by turning inside their verbal thread is like the ugly woman imitating a frown or learning the Handan walk, inviting ridicule from knowledgeable people.")
},
"J10nA158":{
"t_faf30cf1fb87:0036a04:1:4":m("Miyun Yuanwu",TM,"in his first-person account of his own course, Miyun Yuanwu says that despite confronting his teacher he remained hazy, bright and numinous, and had not yet attained stability."),
"t_ff50c6974a36:0040b17:1:1":m("Miyun Yuanwu",TM,"replying to the monk Cunyi, Miyun Yuanwu names the ruler-minister, upright-crooked, princes, and merit arrangements as five-rank systems, then contrasts them with Linji Yixuan's true person of no rank.",[("Linji Yixuan",["master-quoted","contrasting-source"])]),
"t_db4a932ce500:0074a07:1:4":m("Tang Shiji",TM,"the byline to the Inscription for the Pagoda of Miyun Yuanwu's Bequeathed Robe at Jinsu names Tang Shiji as author; Tang says the claim of eighteen great awakenings and uncounted small awakenings came from a Song Confucian and was not spoken by Dahui Zonggao.",[("Miyun Yuanwu",["subject-of-inscription"]),("Dahui Zonggao",["denied-speaker"])])
},
"D48n8939":{
"t_c3a7862b9971:0031b04:1:1":m("Foyan Qingyuan",TD,"inside the Upper-Hall Record of Foyan Qingyuan at Longmen, Foyan ends the exchange by telling everyone to drink tea freely and at ease in the hall."),
"t_84e490b1773f:0038a08:1:6":m("Foyan Qingyuan",TD,"inside his Longmen upper-hall record, Foyan Qingyuan claps his hands, laughs loudly, pauses, and asks the assembly what they think he is laughing at."),
"t_a2612eb1f803:0416a06:1:4":m("Zhenjing Kewen",TD,"in the first recorded address from his residence at Jinling Baoning, Zhenjing Kewen says that sentient beings are originally complete in spiritual powers and transformations and need not seek them outside.")
},
"J27nB198":{
"t_20cc4b0bc96e:0462a08:1:2":m("Xueguan Zhiyin",TX,"responding to Hengru's request for instruction, Xueguan Zhiyin says that students with coarse activity-consciousness and clouded eyes recognize only the gate of light and shadow as themselves, then quotes Fenyang Shanzhao on the five positions.",[("Fenyang Shanzhao",["master-quoted"])]),
"t_68d495f2868b:0490c10:1:1":m("Xueguan Zhiyin",TX,"in his extended classification of Chan defects, Xueguan Zhiyin asks what block-print Chan is and defines it as lacking genuine insight while leaning entirely on old masters' sayings as a staff.")
},
"J26nB177":{
"t_6293dead3bb2:0028b05:1:5":u("monk","unnamed monk","questioner",TP,"an unnamed monk sets aside easy emergence inside the gate and difficult turning outside it, then asks Poshan Haiming what a turning phrase is.",[("Poshan Haiming",["respondent","record-owner"])]),
"t_96255c741b17:0028c03:1:6":u("monk","unnamed monk","questioner",TP,"after Poshan Haiming answers the monk's question about starting a retreat with one melon seed and two shell halves, the unnamed monk says it has no flavor; Poshan hisses once.",[("Poshan Haiming",["respondent","record-owner"])]),
"t_ba8066477571:0028c16:1:7":u("monk","unnamed monk","person shouting",TP,"after Poshan Haiming strikes him twice, the unnamed monk gives one shout and leaves; Poshan then rests.",[("Poshan Haiming",["respondent","record-owner"])])
},
"J27nB190":{
"t_6edb551acb53:0096a15:1:3":m("Shiyu Mingfang",TS,"in the hall address for sending Yunmen Zhan's wooden image to the Jingshan patriarch hall, Shiyu Mingfang lists several attempted interpretations of an old case and says that such views all belong to followers of conceptual understanding.")
},
"J28nB219":{
"t_aef7434b8470:0663c23:1:6":m("Zhuanyu Guanheng",TZH,"in his instruction to Huixin Zhao, Zhuanyu Guanheng says that merely memorizing abstruse sayings, even eloquently enough to make a stubborn stone nod, has no connection at all with one's own share."),
"t_2baf0ec63b2c:0667b05:1:6":m("Zhuanyu Guanheng",TZH,"referring to himself as the sick monk in his instruction to Wanbai Hao, Zhuanyu Guanheng recounts walking in meditation after meals and observing Hao's consistently careful service and eating routine."),
"t_cb3571346f22:0668b25:1:2":m("Shakyamuni Buddha",TZH,"inside Zhuanyu Guanheng's instruction to Kuanju Yong, Shakyamuni Buddha is the exact actor who holds up the flower, paired with Mahakasyapa's smile as a direct and rapid exchange.",[("Mahakasyapa",["responding-master"]),("Zhuanyu Guanheng",["raiser","commentator","record-owner"])]),
"t_2baf0ec63b2c:0722b06:1:1":u("hermit","unnamed rock-hermit","person walking in meditation",TZH,"in the biography of Hanshan Deqing, an unnamed hermit living in a rock shelter goes outside at night to walk in meditation; Hanshan follows and the two walk separately east and west.",[("Hanshan Deqing",["companion","biographical-subject"])]),
"t_d1e06fd225fa:0723c04:2:2":m("Hanshan Deqing",TZH,"the biography of Hanshan Deqing recounts that after a night of walking meditation at Laoshan, Hanshan enters his room and takes up the Surangama Sutra to test the luminous, mirror-like experience against its words.")
},
"J25nB174":{
"t_7c1991e9eabb:0687c30:1:1":m("Juelang Dasheng",TJD,"in his Jingju Temple hall address, Juelang Dasheng describes the Dongshan house style as wondrously coordinating upright and inclined positions."),
"t_7c1991e9eabb:0699b09:1:2":m("Juelang Dasheng",TJD,"in the hall address requested by Yunmen Weicen, Juelang Dasheng says that the Dongshan house style has been revived across western Jiangxi and eastern Yue.")
},
"J25nB163":{
"t_66630c6a8a37:0224c05:2:2":m("Zhang Hui",TGT,"the byline names Zhang Hui as author of this preface; Zhang says Guting Shanjian is Wuji's true transmission and that their encounter and affinity accorded.",[("Guting Shanjian",["subject-of-preface"])]),
"t_66630c6a8a37:0229c09:1:3":m("Guting Shanjian",TGT,"in his own extended Dharma instruction, Guting Shanjian opens by naming the establishment of the house, question-and-answer encounters, scholastic followers, and Chan monastics before rejecting their false talk."),
"t_f04c29743e77:0245c11:1:3":m("Guting Shanjian",TGT,"in his Niushou daily record, Guting Shanjian says that one thought for ten thousand years and ten thousand years in one thought, unobstructed like empty space, is called the person of the Way beyond learning and contrivance."),
},
"J26nB188":{
"t_ff50c6974a36:0756a04:1:2":u("monk","unnamed monk","questioner",TRB,"an unnamed monk asks Ruibai Mingxue about the five ruler-minister positions and then asks successively about each named position; Ruibai supplies each answer.",[("Ruibai Mingxue",["respondent","record-owner"])]),
"t_b0f2ccf6d140:0762c06:1:6":u("monk","unnamed monk","questioner",TRB,"during the Yunmen retreat-opening tea, an unnamed monk asks Ruibai Mingxue what opening the retreat is; Ruibai tells him to return to his assigned place.",[("Ruibai Mingxue",["respondent","record-owner"])])
},
"J29nB244":{
"t_49829f59faac:0718b16:1:3":m("Sanshan Denglai",TSS,"in his Five Houses verses, Sanshan Denglai begins his verse on the first of Yunmen's three phrases by saying that the business of box-lid covering heaven and earth is inexhaustible."),
"t_cf6aac2f936b:0719a09:1:3":m("Sanshan Denglai",TSS,"after commenting on Yunmen Wenyan's one-word answer 'ancestor,' Sanshan Denglai introduces his own verse, repeats 'one-word barrier,' and asks why it is single rather than paired.",[("Yunmen Wenyan",["master-in-raised-case"])]),
"t_b655ff97e2c3:0725c08:1:4":m("Sanshan Denglai",TSS,"in his old-case comment, Sanshan Denglai asks what connection one-finger Chan has, says it buried the monk Juzhi alive for his whole life, and asks how many later finger-raisers actually received its use.")
},
"J27nB192":{
"t_78f95517a347:0197a17:1:1":m("Yongjia Xuanjue",TDR,"inside the older audience with Huineng raised in Daxiu Zhu's record, Yongjia Xuanjue says that birth-and-death is the great matter and impermanence is swift.",[("Huineng",["respondent"]),("Daxiu Zhu",["later-raiser","record-owner"])])
},
"J28nB212":{
"t_2069b9c33315:0474c22:1:2":m("Caoshan Benji",TEI,"Eryin Mi explicitly introduces Caoshan Benji's formulation; Caoshan defines ruler as the upright position, minister as the inclined position, their mutual orientations, and their accord.",[("Eryin Mi",["raiser","commentator","record-owner"])]),
"t_cb3571346f22:0475c11:1:7":m("Shakyamuni Buddha",TEI,"in Eryin Mi's address appointing officers, Shakyamuni Buddha is the exact actor who mounts the seat, paired with Manjusri's sounding the gavel; Eryin sets this beside Mazu mounting the hall and Baizhang rolling the mat.",[("Manjusri",["gavel-officiant"]),("Mazu Daoyi",["parallel-master"]),("Baizhang Huaihai",["parallel-master"]),("Eryin Mi",["raiser","record-owner"])])
},
"B14n0082":{
"t_dab856504b69:0275b12:1:7":m("Qingping Weikuang",TYJ,"the section heading names Qingping Weikuang; after a monk steps out and bows during Qingping's hall address, Qingping says he is not an adept and sends him out."),
"t_f2482d04a86a:0314a03:1:2":m("Dazhu Huihai",TYJ,"in the long question-and-answer record of Dazhu Huihai, Dazhu describes the Vinaya master as thoroughly understanding observance and transgression and mastering permission and prohibition.")
},
"J20nB098":{
"t_6edb551acb53:0514b20:1:5":m("Huangbo Wunian Shenyou",TWN,"replying to the student Mao Xuanshu, Huangbo Wunian Shenyou says that although Mao's letter is earnest, he has not entered the correct path and is repeatedly deceived by conceptual understanding and conjecture."),
"t_73fb9441f4fb:0517c30:2:3":m("Huangbo Wunian Shenyou",TWN,"in his Dharma instruction, Huangbo Wunian Shenyou says that standing right here he sees the addressees would remain only equal to him even after hundreds of thousands of years of ascetic cultivation.")
},
"J26nB185":{
"t_5e59b126e608:0592c03:3:2":u("monk","unnamed monk","questioner",TFS,"an unnamed monk asks Fushi Tongxian for permission to inquire about Doushuai Congyue's three barriers, then proceeds through the three questions.",[("Fushi Tongxian",["respondent","record-owner"]),("Doushuai Congyue",["source-of-three-barriers"])]),
"t_e156057131dc:0595c24:1:7":m("Fushi Tongxian",TFS,"in his end-of-retreat informal address, Fushi Tongxian tells the assembly urgently to turn around at its primary public case and break the ball of doubt in the breast."),
"t_20cc4b0bc96e:0621c24:1:3":m("Fushi Tongxian",TFS,"in his verse replying to the layman Shen Shuding, Fushi Tongxian says not to halt at the gate of light and shadow and to open the eyes amid the thicket of forms and sounds.")
},
"J27nB197":{
"t_300236cb6368:0398b13:1:4":m("Wuyi Yuanlai",TBW,"in his hall address, Wuyi Yuanlai says that rice makes rice, water makes soup, and a nun is a woman, and warns the assembly not to miss this face-to-face."),
"t_300236cb6368:0408a01:1:3":m("Wuyi Yuanlai",TBW,"after asking whether anyone in the assembly can appraise the costly cheap sale, Wuyi Yuanlai pauses and says that it must not be missed face-to-face.")
},
"J32nB273":{
"t_78f95517a347:0204b18:1:5":m("Qianyan Yuanzhang",TQY,"opening a ninth-month hall address, Qianyan Yuanzhang tells the assembly that birth-and-death is the great matter and impermanence is swift."),
"t_7887dc8d449f:0228a28:1:5":m("Qianyan Yuanzhang",TQY,"in his own verse preceding the praise requested by guest-prefect Dezhi, Qianyan Yuanzhang caricatures one called a venerable master as blind, deaf, ignorant of both Buddhist and worldly matters, and destructive of Linji's true lineage.")
},
"J33nB286":{
"t_6ac3f9f0a2d2:0489c10:1:3":u("monk","unnamed monk","questioner",TYG,"an unnamed monk asks Yingning Jing what 'he is now precisely me' is; Yingning answers that water in a land of poison must not be tasted.",[("Yingning Jing",["respondent","record-owner"])]),
"t_19705602b956:0495a04:1:3":m("Yingning Jing",TYG,"in his own hall address, Yingning Jing says that original presence appears once all constructions of ordinary feelings and holy views have ceased, then draws a figure with the ceremonial scepter.")
},
"J26nB180":{
"t_ff50c6974a36:0295b04:2:2":u("anecdotal interlocutor","unnamed anecdotal questioner","questioner in a joke recounted by the Shunzhi Emperor",THJ,"in the Shunzhi Emperor's joke about a village pedant who counts only five sages, an unnamed interlocutor asks who the fifth sage is.",[("Shunzhi Emperor",["anecdote-teller"]),("Hongjue Min",["audience","compiler-subject"])]),
"t_78f95517a347:0301c02:1:4":imp("inscription","hall-door inscription",THJ,"the hall-door inscription at the Shunzhi Emperor's residence reads 'birth-and-death is the great matter'; the narrative does not identify its writer.","堂門書 introduces displayed writing on the hall door and supplies no personal speaker or author.",[("Shunzhi Emperor",["resident","person-reflecting-on-inscription"]),("Hongjue Min",["addressee-of-report"])])
},
"J28nB220":{
"t_dab856504b69:0781b11:1:6":u("monk","unnamed monk","questioner",TFX,"during the retreat-opening hall exchange, an unnamed monk says that forging buddhas and patriarchs requires an adept and asks Faxi Yin what an adept is; Faxi answers 'one who kills without blinking.'",[("Faxi Yin",["respondent","record-owner"])])
},
"J23nB134":{
"t_bc7bbb4299f1:0520c26:1:5":m("Ding Shangzuo",TFH,"in the Linji Yixuan section, the named Ding Shangzuo (Elder Ding) arrives for an interview and asks what the great meaning of the Buddha's teaching is; Linji descends from the rope-seat, seizes and strikes him, and a nearby monk prompts Ding's awakening.",[("Linji Yixuan",["respondent"])]),
},
"J25nB156":{
"t_ff50c6974a36:0062c07:1:5":m("Wuhuan Xingchong",TWH,"in the first of his six occasional verses beyond things, Wuhuan Xingchong sets Dongshan's five positions beside Linji's three mysteries and questions both as awakening and as so-yet-not-so.")
},
"J27nB189":{
"t_6ac3f9f0a2d2:0041a19:1:5":u("monk","unnamed monk","interjecting commentator",TSY,"an unnamed monk adds the comment 'he is now precisely me; I now am not him'; Sanyi Mingyu warns of bearing bit and saddle and asks the assembly to locate the fault.",[("Sanyi Mingyu",["respondent","record-owner"])])
},
"J28nB205":{
"t_d4df8bc75ad7:0234c09:1:5":m("Jie Weizhou",TJW,"entering his room for a private interview session, Jie Weizhou says that one enacted staff-blow encompasses a hundred commands and leaves copper heads and iron foreheads streaming blood.")
},
"J29nB233":{
"t_e931d476fd02:0333a13:1:1":u("monk","unnamed monk","questioner in a raised case",TJA,"inside the case raised by Jie'an Wujin, an unnamed monk asks Nanyang Huizhong about the competing claims that luxuriant yellow flowers are none other than prajna and that this is false teaching.",[("Nanyang Huizhong",["respondent"]),("Jie'an Wujin",["later-raiser","commentator","record-owner"])])
},
"J33nB280":{
"t_8f76148e713f:0286c28:1:4":u("monk","unnamed monk","questioner",TSD,"an unnamed monk asks whether there is still a fault where not even water can leak through; Shending Yunwai Ze answers 'a dead toad.'",[("Shending Yunwai Ze",["respondent","record-owner"])])
},
"J25nB173":{
"t_c3a7862b9971:0671b07:1:7":m("Wuming Huijing",TSW,"Wuming Huijing says that penetrating one of four phrases gives freedom in birth and death, two gives freedom in coming and going, and all four allows one to be teacher of buddhas and patriarchs.")
},
"J27nB200":{
"t_408abe2e38ca:0568b30:1:3":m("Shishuang Dazun",TSE,"in his eighth-day-of-the-twelfth-month hall address, Shishuang Dazun asks the assembly what the ready-made public case is and answers with water flowing east and west and clouds rising north and south.")
},
"J29nB222":{
"t_6edb551acb53:0007b20:1:2":m("Wolong Zishui",TWZ,"in his hall address, Wolong Zishui says that even Shenhui and Guifeng Zongmi were rejected as followers of conceptual understanding and asks what hope remains for loose expounders of words.",[("Shenhui",["master-discussed"]),("Guifeng Zongmi",["master-discussed"])])
},
"J29nB234":{
"t_d9c587fad710:0382c02:1:4":u("monk","unnamed monk","questioner",TYC,"an unnamed monk asks what it means that a clay Buddha does not cross water; Yichu Yuan answers that soaking does not rot it.",[("Yichu Yuan",["respondent","record-owner"])])
},
"J29nB240":{
"t_408abe2e38ca:0558c22:1:4":m("Muzhou Daoming",TTB,"in a case raised by Tiebi Huiji, Muzhou Daoming is quoted as saying 'ready-made public case' whenever a monk arrives; Tiebi then asks what that ready-made public case is and supplies his own verse-like answer.",[("Tiebi Huiji",["raiser","commentator","record-owner"])])
},
"J33nB287":{
"t_ab6276be6e08:0545b15:1:7":u("personified pavilion","personified Three-Arrivals Pavilion","staged speaker",TZX,"in Zixian Jue's invitation for Chuanzi Mi to mount the hall, the Three-Arrivals Pavilion is personified as repeatedly saying that it wishes to hear the late teacher's final phrase.",[("Zixian Jue",["framing-speaker","record-owner"]),("Chuanzi Mi",["invited-hall-speaker"])])
}}

for stem, overrides in DECISIONS.items():
    p=BASE/f"decisions-{stem}.json"; d=json.loads(p.read_text(encoding="utf-8"))
    keys={r["key"] for r in d["rows"]}; assert keys==set(overrides),(stem,keys-set(overrides),set(overrides)-keys)
    for r in d["rows"]: r["Override"]=overrides[r["key"]]
    d["reviewedAllCases"]=True;d["reviewer"]="Codex hard-w2-b1";d["reviewedUtc"]=UTC
    p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(stem,len(d["rows"]))
