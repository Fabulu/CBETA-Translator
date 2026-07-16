#!/usr/bin/env python3
"""Apply explicit reviewed decisions for hard-w2-remaining300 source sheets."""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / "maintenance" / "hard-bundle-inputs" / "w2-remaining300"
UTC = "2026-07-14T07:45:00Z"
REVIEWER = "Codex hard-w2-remaining300"
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]
X82 = "The Complete Book of the Five Lamps, volumes 34–120 (五燈全書(第34卷-第120卷))"
X80 = "The Compendium of the Five Lamps (五燈會元)"
LX = "Record of the Ancestral Guidelines (列祖提綱錄)"
T51 = "Continued Record of the Transmission of the Lamp (續傳燈錄)"
JD = "Jingde Record of the Transmission of the Lamp (景德傳燈錄)"
YS = "Imperially Selected Recorded Sayings (御選語錄)"
JZ = "Jianzhong Jingguo Continuation of the Lamp Record (建中靖國續燈錄)"
XG = "Continued Essentials of the Recorded Sayings of Ancient Worthies (續古尊宿語要)"
LH = "Linked Lamps Compendium (聯燈會要)"
ZD = "Correct Lineage of the Continuation of the Lamp (續燈正統)"
YW = "Recorded Sayings of Yuanwu Foguo (圓悟佛果禪師語錄)"
YL = "Recorded Sayings of Chan Master Yunxi Langting Ting (雲溪俍亭挺禪師語錄)"
ZJ = "Record of the Source-Mirror (宗鏡錄)"
JT = "Jiatai Universal Lamp Record (嘉泰普燈錄)"
ZG = "Collected Old Cases Raised in the Lineage (宗門拈古彙集)"
JL = "Complete Record of Chan Master Tianjie Juelang Dasheng (天界覺浪盛禪師全錄)"
WY = "Strict Lineage of the Five Lamps, volumes 10–25 (五燈嚴統(第10卷-第25卷))"
BY = "Recorded Sayings of Chan Master Baiyu (百愚禪師語錄)"
KG = "Recorded Sayings of Chan Master Konggu Daocheng (空谷道澄禪師語錄)"
ZR = "Recorded Sayings of Chan Master Zhanran Yuancheng (湛然圓澄禪師語錄)"
RT = "Eyes of Humans and Devas (人天眼目)"
BQ = "Imperially Revised Baizhang Monastic Code (勅修百丈清規)"
SF = "Recorded Sayings of Monk Sanfeng Cang (三峰藏和尚語錄)"
BC = "Blue Cliff Record (佛果圜悟禪師碧巖錄)"
ZF = "Recorded Sayings of Chan Master Zhean Jingfan (蔗菴範禪師語錄)"
YA = "Recorded Sayings of Chan Master Yuan'an Feng (遠菴僼禪師語錄)"
BK = "Recorded Sayings of Chan Master Baishan Kai of Shanxi (山西柏山楷禪師語錄)"


def ctx(*pairs):
    return [{"MasterName": name, "Roles": list(roles)} for name, roles in pairs]


def named(name, note, contexts=(), title=X82):
    out = {"MasterName": name, "AttributionNote": f"{title}: {note}"}
    if contexts:
        out["ContextMasters"] = list(contexts)
    return out


def unnamed(label, role, note, contexts=(), title=X82):
    out = {
        "ActorAttribution": {
            "Status": "reviewed-unnamed",
            "Kind": "monk",
            "ActorLabel": label,
            "ActorRole": role,
            "RungsChecked": RUNGS,
            "ReviewedBy": REVIEWER,
            "ReviewedUtc": UTC,
        },
        "AttributionNote": f"{title}: {note} The line, expanded context, section header, book title, TEI header, and parallel-passage search do not give a personal name for the {label}.",
    }
    if contexts:
        out["ContextMasters"] = list(contexts)
    return out


def impersonal(label, note, grammar, contexts=(), title=X82):
    out = {
        "ActorAttribution": {
            "Status": "impersonal",
            "Kind": "compiler narrative",
            "ActorLabel": label,
            "ActorRole": "document voice",
            "GrammarEvidence": grammar,
            "ReviewedBy": REVIEWER,
            "ReviewedUtc": UTC,
        },
        "AttributionNote": f"{title}: {note}",
    }
    if contexts:
        out["ContextMasters"] = list(contexts)
    return out


DECISIONS = {
    "t_961b548d6462:0003b19:1:1": named("Huiyin Yining", "in Huiyin Yining's explicitly headed section, Huiyin Yining opens his hall address by contrasting recommendation before words with responsive accord after the phrase."),
    "t_3972185a2e25:0004a05:1:4": named("Fachang Yiyu", "in the Fachang Yiyu section, Fachang Yiyu draws a line with his staff and asks Ying what the lineage matter is.", ctx(("Ying Shouzuo", ("respondent",)))),
    "t_ec1241360056:0004a20:1:1": named("Fachang Yiyu", "in his own hall address, Fachang Yiyu describes a state that water cannot wet and wind cannot enter as resembling an iron hammer without a hole."),
    "t_d2892b1eaae0:0006c17:1:5": unnamed("unnamed questioning monk", "questioner", "inside Jiangshan Faquan's section, an unnamed monk asks the meaning of Huike standing in snow to his waist and then asks why robe and Dharma were transmitted; Jiangshan Faquan answers both questions.", ctx(("Jiangshan Faquan", ("respondent", "section-subject")), ("Huike", ("case-figure",)))),
    "t_84043ffcdf90:0008b18:1:4": named("Rui'an Sengyin", "in Rui'an Sengyin's explicitly headed section, Rui'an Sengyin pauses during his hall address and tells those who possess the eye to look."),
    "t_cb3571346f22:0010a21:1:8": named("Fayun Faxiu", "in Fayun Faxiu's own hall address, Fayun Faxiu says it is not extraordinary for a patch-robed monk to salute Shakyamuni from a distance and decline to bow to Maitreya.", ctx(("Shakyamuni Buddha", ("named-figure",)))),
    "t_4416ef85b3a5:0010b20:1:6": named("Changlu Yingfu", "in Changlu Yingfu's own hall address, Changlu Yingfu surveys the assembly and says the myriad phenomena interpenetrate like the ocean-reflection concentration."),
    "t_fb23e0284d73:0011a06:1:1": named("Tianyi Yihuai", "in the biography of Tianbo Zhongyuan, Tianyi Yihuai is the exact speaker marked 衣 who certifies Zhongyuan and calls him the household's thousand-li colt.", ctx(("Tianbo Zhongyuan", ("student", "biographical-subject")))),
    "t_63ca7d059ee8:0013a22:1:5": unnamed("unnamed questioning monk", "questioner and nonverbal actor", "inside Wangxian Zong's section, an unnamed monk asks a follow-up, receives the answer 'Guizong drags a stone,' and then remains silent; Wangxian Zong calls him a real patch-robed monk.", ctx(("Wangxian Zong", ("respondent", "section-subject")),)),
    "t_936118ea496c:0018a07:1:4": named("Miaohui Wenyi", "in Miaohui Wenyi's explicitly headed section, Miaohui Wenyi's hall address lists spreading the bowls and eating gruel among the ordinary repeated events of the day."),
    "t_7887dc8d449f:0018c02:1:2": named("Xiaoyao Cong", "expanded context places the line in Xiaoyao Cong's biography: Xiaoyao Cong studies doctrine in Chengdu and then travels south to call on venerable masters before meeting Huilin Zongben.", ctx(("Huilin Zongben", ("teacher-met",)))),
    "t_c968268a64d1:0023a18:1:1": named("Chuming Baoyin", "in Chuming Baoyin's explicitly headed section, Chuming Baoyin opens his hall address by describing the ancestral mind-seal as neither long nor short, square nor round, internal nor external."),
    "t_560356022866:0026b12:1:6": named("Miaozhan Wenzhao", "in Miaozhan Wenzhao's explicitly headed section, Miaozhan Wenzhao asks whose radiance is distinctly solitary and warns that answering with suchness or ultimate reality cuts a wound in sound flesh."),
    "t_b88b6a8a5659:0028a08:1:4": named("Jinshan Weizhong", "in Jinshan Weizhong's biography, Jinshan Weizhong enters Foguang's room, hears the cypress-tree case raised, and breaks through in awakening.", ctx(("Foguang", ("teacher", "case-raiser")),)),
    "t_4416ef85b3a5:0028c15:1:2": named("Xuedou Faning", "in Xuedou Faning's explicitly headed section, Xuedou Faning says that the hundred rivers reach their limit in the sea and the myriad phenomena reach their limit in emptiness."),
    "t_ff560195f161:0028c24:1:5": named("Xuedou Faning", "in the same hall address, Xuedou Faning asks the assembly how it understands the staff's own limit and says that discerning it permits unhindered passage."),
    "t_ff560195f161:0044a04:1:6": named("Huanglong Sixin Wuxin", "in Caotang Shanqing's biography, Huanglong Sixin Wuxin is the exact teacher denoted by 龍 who asks Shanqing how he understands the wind-and-banner saying and then instructs him through the cat-catching-a-mouse image.", ctx(("Caotang Shanqing", ("student", "biographical-subject")),)),
    "t_cf07831c1f12:0055a20:1:5": named("Jianfu Daoying", "in Jianfu Daoying's explicitly headed section, Jianfu Daoying distinguishes the relatively easy condition of being wholly stripped clean from the harder condition of being clearly manifest."),
    "t_4416ef85b3a5:0060a21:1:3": named("Shangfeng Bencai", "in Shangfeng Bencai's explicitly headed section, Shangfeng Bencai extends Vimalakirti's illness through his staff, the myriad phenomena, and ordinary and holy beings before asking where the illness begins.", ctx(("Vimalakirti", ("figure-deployed",)))),
    "t_b33fddd5d4f1:0068a22:1:2": named("Zhaojue Chunbai", "in Zhengjue Zongxian's biography, Zhaojue Chunbai is the exact teacher marked 覺 who asks Zongxian how he understands standing on the highest peak and walking on the deepest sea floor; Zongxian awakens at the words.", ctx(("Zhengjue Zongxian", ("student", "biographical-subject")),)),
    "t_84e490b1773f:0075c19:1:3": named("Yangqi Fanghui", "in Yangqi Fanghui's section, Yangqi Fanghui is the first exact actor at the stored occurrence: after the visiting Daowu offering-master points and says 'the spring rain pours down,' Yangqi claps his hands, laughs, and appraises the answer.", ctx(("unnamed Daowu offering-master", ("visiting-interlocutor",)))),
    "t_937f63a4fb51:0082b10:1:6": named("Yuezhang Zhiyuan", "in Yuezhang Zhiyuan's explicitly headed section, Yuezhang Zhiyuan answers the question 'what is the single color?' with 'before the eyes there is no acarya; here there is no old monk.'"),
    "t_cf07831c1f12:0082c01:1:6": named("Wuzhen Chuwen", "in Wuzhen Chuwen's explicitly headed section, Wuzhen Chuwen says that even being clean naked, wholly stripped, and with nothing to grasp still leaves a fine trace."),
    "t_fac9b9afebf6:0101b19:1:2": unnamed("unnamed questioning monk", "questioner", "inside Zhongyan Huayan Zujue's section, an unnamed monk asks how one shout is like a probing pole and shadowing grass; Zhongyan Huayan Zujue answers that he has tested the monk to the bone.", ctx(("Zhongyan Huayan Zujue", ("respondent", "section-subject")),)),
    "t_2baf0ec63b2c:0104c02:1:2": named("Longya Zhicai", "in Longya Zhicai's explicitly headed section, Longya Zhicai tells the assembly that if it sees and can carry it out, it should walk meditation when vigorous and rest when tired."),
    "t_cba9cbb44845:0105b04:1:1": named("Heshan Shouxun", "in Heshan Shouxun's explicitly headed section, Heshan Shouxun raises the old-woman-burns-the-hermitage case and says that supporting the lineage and establishing its teaching requires the right person."),
    "t_eba970114dd2:0107a14:1:2": named("Xichan Wenlian", "in Xichan Wenlian's explicitly headed section, Xichan Wenlian warns that if one's footing is off, one resembles the Handan walker imitating another gait."),
    "t_9a7a00ea0cd1:0115b06:1:5": named("Lanan Dingxu", "in Lanan Dingxu's explicitly headed section, Lanan Dingxu asks a monk in the private room where the one returns when the ten thousand things return to one; the monk answers 'inside Korea.'", ctx(("unnamed monk", ("respondent",)))),
    "t_8bced2c0bc2f:0117c13:1:1": named("Dawei Fabao", "in Dawei Fabao's explicitly headed section, Dawei Fabao says one must be the lion that bites the person and must not imitate the Korean hound that chases the clod."),
    "t_91d84c849fc7:0135a16:1:6": named("Dahui Zonggao", "in Xin'an Weiyin's biography, Dahui Zonggao is the exact speaker marked 慧 who asks Weiyin how he preserves it; Weiyin answers that he eats when hungry and sleeps when tired.", ctx(("Xin'an Weiyin", ("respondent", "biographical-subject")),)),
    "t_a38d5c680c67:0135b24:1:5": named("Wanan Daoyan", "in Kentang Yanchong's biography, Wanan Daoyan is the exact quoted teacher denoted by Donglin who tells his assembly that he has no special mystery, only wooden-slip soup and iron-nail rice for them to chew.", ctx(("Kentang Yanchong", ("listener", "biographical-subject")),)),
    "t_2baf0ec63b2c:0177b06:1:4": named("Songyin Mao", "in Songyin Mao's explicitly headed biography, Songyin Mao walks meditation beneath the pines one evening, hears the rock-spring, and is slightly touched by it."),
    "t_4d4ce329367f:0206b08:1:2": named("Guaishi Qi", "in Guaishi Qi's explicitly headed section, Guaishi Qi says that merely speaking of drink and food cannot cure hunger and thirst; one must drink water and eat rice oneself."),
    "t_df4e71aa0bc5:0210a19:1:4": named("Nanshi Wenxiu", "in Nanshi Wenxiu's explicitly headed section, Nanshi Wenxiu says that those who thoroughly awaken and those who understand wrongly have both been as numerous as rice plants, hemp, bamboo, and reeds."),
    "t_8bced2c0bc2f:0261c01:1:4": named("Kongxiang Gui", "in Kongxiang Gui's explicitly headed section, Kongxiang Gui calls the people just described mad dogs chasing clods and asks when they have ever dreamed of a lion's surging claws and fangs."),
    "t_8bced2c0bc2f:0261c03:1:5": named("Kongxiang Gui", "in the same address, Kongxiang Gui says that chasing clods and following scent does not yet make a good dog."),
    "t_a9f422b3b249:0347a10:1:6": named("Jinsu Rong", "in Lingyue Gu's biography, Jinsu Rong is the exact teacher marked 容 who asks where Lingyue's birth-condition is and then presses what happens apart from the three barriers.", ctx(("Lingyue Gu", ("respondent", "biographical-subject")),)),
    "t_d4df8bc75ad7:0353c05:1:3": named("Fajing Hao", "in the nun Fajing Hao's explicitly headed section, Fajing Hao says that even a fellow with a copper head and iron brow must receive one welt from each staff-blow."),
    "t_f2f4079b20e5:0382b16:1:6": named("Wufeng Ruxue", "in Wufeng Ruxue's explicitly headed section, Wufeng Ruxue says that Dharma has no fixed form, meets conditions as its lineage-principle, and makes the host wherever it goes."),
    "t_78bd967fdcd6:0390c16:1:4": named("Xiuyun Wei", "in Xiuyun Wei's explicitly headed biography, Xiuyun Wei is the grammatical actor who comes into great doubt and investigates with fierce effort."),
    "t_35c3fb655630:0443b22:1:2": named("Shending Yunwai Ze", "in Shending Yunwai Ze's explicitly headed section, Shending Yunwai Ze often raises Zhaozhou's dog-has-no-buddha-nature case in his room to test people, with few matching his function."),
    "t_bf67613e4573:0520c07:1:4": named("Jiesan Hong", "in Jiesan Hong's explicitly headed section, Jiesan Hong suddenly raises the fly-whisk and declares it to be Great Compassion's thousand hands and eyes."),
    "t_898279a78ecf:0649a13:1:5": named("Tianzhang Yu", "in Tianzhang Yu's explicitly headed section, Tianzhang Yu asks where the leak is and, after pausing, answers with Yunmen's cake, Zhaozhou's tea, Xuefeng's wooden ball, and Zihu's dog."),
}

X80_DECISIONS = {
    "t_bf467ac18ec0:0029b22:2:1": named("Five Direction Deva Kings", "in the Shakyamuni Buddha section, the Five Direction Deva Kings collectively answer that there is no pearl in the Buddha's hand and therefore nowhere for a color to be; Shakyamuni then diagnoses their inversion.", ctx(("Shakyamuni Buddha", ("questioner", "section-subject")),), title=X80),
    "t_72e01bbb3474:0031a13:1:1": named("Shakyamuni Buddha", "Shakyamuni Buddha rebukes Manjusri and says that during forty-nine years in the world he never spoke a single word.", ctx(("Manjusri", ("addressee",)),), title=X80),
    "t_23204fbd253c:0049c12:1:6": named("Zhongshan Tancui", "in Zhongshan Tancui's explicitly headed biography, Zhongshan Tancui silently examines Niutou Zhiyan's words, greatly awakens to the dark purport, and then hides his traces on Zhongshan.", ctx(("Niutou Zhiyan", ("teacher",)),), title=X80),
    "t_72e01bbb3474:0053a23:1:2": named("Pozao Duo Heshang", "Pozao Duo Heshang strikes the stove three times with his staff, rebukes it, and asks how holiness or numinous power could arise from assembled mud and tiles.", title=X80),
    "t_3f7a6ab74b68:0077c21:1:4": impersonal("compiler's biographical narrative", "the compiler's biographical narrative says that Dongsi Ruhui's students became so numerous that the sleeping platforms in the monks' hall collapsed, producing the name 'Collapsed-Bed Assembly.'", "The clause is third-person biographical narration with 學徒 and 僧堂牀榻 as grammatical subjects; it is not a quoted speech turn.", ctx(("Dongsi Ruhui", ("biographical-subject",)),), title=X80),
    "t_93360aaedb7c:0084c19:1:1": named("Wuzhu Heshang", "Wuzhu Heshang tells Shao to watch how the monk behind him answers and, when Shao tries to approach, strikes him and orders him to go to the monks' hall.", ctx(("Shao", ("interlocutor",)),), title=X80),
    "t_757827b8d4cb:0096c14:1:2": named("Zihu Lizong", "Zihu Lizong warns his assembly that Zihu has a dog which takes a person's head, heart, and feet and that deliberation means losing body and life.", title=X80),
    "t_ff560195f161:0098a03:1:1": named("Ganzi Xingzhe", "Ganzi Xingzhe tells a visiting monk that a monk once asked Guishan about the meaning of coming from the west, recounts Guishan raising the fly-whisk, and asks how the visitor understands Guishan's intent.", ctx(("Guishan Lingyou", ("earlier-case-respondent",)),), title=X80),
    "t_4d4cbd834b80:0100c04:1:1": named("Mimi Yan Heshang", "Mimi Yan Heshang holds his wooden fork and challenges students that speaking or failing to speak both die beneath it, repeatedly ordering them to answer quickly.", title=X80),
    "t_a9f422b3b249:0104c21:1:4": named("Dongshan Liangjie", "in Shoushan Shijie's biography, Dongshan Liangjie is the exact questioner denoted by 山 who asks Shijie where his birth-condition is; Shijie answers that if the question is genuine, he is a person of Min.", ctx(("Shoushan Shijie", ("respondent", "biographical-subject")),), title=X80),
    "t_b33fddd5d4f1:0116b10:1:1": named("Yaoshan Weiyan", "in the encounter with governor Li Ao, Yaoshan Weiyan tells the governor that to preserve this matter he must stand on the highest mountain peak and walk on the deepest sea floor.", ctx(("Li Ao", ("addressee", "questioning-governor")),), title=X80),
    "t_2852c7a978c5:0122c19:1:3": unnamed("unnamed questioning monk", "questioner", "inside Touzi Datong's section, an unnamed monk asks Touzi to carve the jade rough he brings and, after Touzi rejects it as timber, says that Bian He then has nowhere to emerge.", ctx(("Touzi Datong", ("respondent", "section-subject")),), title=X80),
    "t_5d84cccab8df:0129a01:1:8": named("Zhaozhou Congshen", "Zhaozhou Congshen answers that Mount Sumeru is tied with lotus-root fiber and then identifies the scene as a lotus heart looking upward from the top of the banner pole.", title=X80),
    "t_d2892b1eaae0:0135b23:1:3": named("Shuwei Tongxuan", "Shuwei Tongxuan answers an unnamed monk's question on the precise intent from the west by saying that standing in the snow was not yet the labor and severing the arm made it exact.", ctx(("Huike", ("case-figure",)),), title=X80),
    "t_eedf4100b3d7:0170b24:1:2": named("Baozi Guangyun", "Baozi Guangyun tells a new arrival to try making the lion's roar; when the monk replies that doing so would leave no master, Baozi spares him thirty blows because he is newly arrived.", ctx(("unnamed monk", ("respondent",))), title=X80),
    "t_644a3152952c:0175c14:1:4": named("Gongchen Daoxian", "Gongchen Daoxian opens his hall address by saying that if he deployed the whole function, the listeners would have nowhere to grope for it.", title=X80),
    "t_1f6124388d25:0180b20:1:1": named("Dizang Guichen", "in Qingliang Xiufu's biography, Dizang Guichen visits the ill Xiufu, points at the lantern, asks whether he sees it, and says that this alone does not turn away from the conditions he has been given.", ctx(("Qingliang Xiufu", ("respondent", "biographical-subject")),), title=X80),
    "t_1f6124388d25:0181c01:1:2": unnamed("unnamed questioning monk", "questioner", "inside Longji Shaoxiu's section, an unnamed monk asks about seeing mind on seeing form, explicitly supplies the lantern as the form, and asks which is mind; Longji answers that the lantern is mind.", ctx(("Longji Shaoxiu", ("respondent", "section-subject")),), title=X80),
    "t_1459058101b7:0219a13:1:6": named("Song Taizong", "Emperor Song Taizong says that knowing the place where a movement begins lets one know the place where body and life come down.", title=X80),
    "t_ab6276be6e08:0249b14:1:4": unnamed("unnamed questioning monk", "questioner", "inside Xuansha Shibei's section, an unnamed monk asks what the final phrase is; Xuansha answers 'beneath the paired sala trees.'", ctx(("Xuansha Shibei", ("respondent", "section-subject")),), title=X80),
    "t_26c9a5cb0fe3:0254b15:1:5": named("Baofu Congzhan", "Baofu Congzhan says that pointing before arrival fails to reach it, while arriving without pointing mixes mud and water before black and white are distinguished.", title=X80),
    "t_bf467ac18ec0:0256c22:1:4": unnamed("unnamed questioning monk", "questioner", "inside Letan Jingxiang's section, an unnamed monk asks how his hand resembles the Buddha's hand; Letan answers that gold and brass are hard to distinguish.", ctx(("Letan Jingxiang", ("respondent", "section-subject")),), title=X80),
    "t_3a0a4e68cf13:0257b24:1:4": named("Qingshan Puning", "in Qingshan Puning's explicitly headed section, Qingshan Puning opens his hall address by saying that circumstances leave him no choice but to entangle vines with the listeners.", title=X80),
    "t_78bd967fdcd6:0291a11:1:3": unnamed("unnamed questioning monk", "questioner", "inside Fenghuang Congchen Hongren's section, an unnamed monk asks what a person of great doubt is; Fenghuang Congchen Hongren supplies the answer.", ctx(("Fenghuang Congchen Hongren", ("respondent", "section-subject")),), title=X80),
    "t_e931d476fd02:0311b01:1:5": unnamed("unnamed questioning monk", "questioner", "inside Jiuxian Yuxian's section, an unnamed monk cites luxuriant yellow flowers as none other than prajna and asks what prajna is; Jiuxian answers that the Yellow Springs have no old or young.", ctx(("Jiuxian Yuxian", ("respondent", "section-subject")),), title=X80),
    "t_135a001a5b0e:0342b16:1:4": named("Fayan Wenyi", "Fayan Wenyi answers the question about the wish-fulfilling jewel following colors with 'green bamboo; luxuriant yellow flowers.'", title=X80),
    "t_49829f59faac:0373a24:1:4": named("Jiuding Jixing Huiquan", "in Jiuding Jixing Huiquan's explicitly headed section, Jiuding Jixing Huiquan introduces Yunmen's three phrases and names the phrase that covers heaven and earth before the phrase that cuts off all streams.", ctx(("Yunmen Wenyan", ("source-of-three-phrases",)),), title=X80),
    "t_19784084ccb4:0406c22:1:6": named("Baozhi", "inside the case raised in Foxing Fatai's section, Baozhi is the exact speaker who shouts at Fu Dashi, tells him not to speak that way, and asks him to say something else.", ctx(("Fu Dashi", ("addressee",)), ("Foxing Fatai", ("later-raiser", "section-subject"))), title=X80),
    "t_5b39f18f89ff:0409b15:1:2": named("Zhongtianzhu Zhongren", "Zhongtianzhu Zhongren raises Zhaozhou's dog-has-no-buddha-nature saying in his hall address and then comments with the embroidered-maiden verse.", ctx(("Zhaozhou Congshen", ("earlier-case-source",)),), title=X80),
    "t_19784084ccb4:0422a18:1:1": named("Wanan Daoyan", "Wanan Daoyan answers an unnamed monk's question 'what is Buddha?' with 'Preceptor Baozhi' and then insists that Baozhi is not an idle monk.", ctx(("Baozhi", ("answer-figure",)),), title=X80),
    "t_e6b94cf3dbc3:0424c20:1:5": named("Wuyong Jingquan", "in Wuyong Jingquan's explicitly headed section, Wuyong Jingquan says that after changing the bones, washing the bowels, and setting oneself in order anew, one must still investigate with the whole body as the eye.", title=X80),
    "t_5b39f18f89ff:0425b21:1:3": named("Jianfu Wuben", "in Jianfu Wuben's explicitly headed biography, Jianfu Wuben takes up the word 'no' in Zhaozhou's dog-has-no-buddha-nature case and keeps it before himself in investigation.", ctx(("Zhaozhou Congshen", ("case-source",)),), title=X80),
}

X64_DECISIONS = {
    "t_8253f56255ce:0006c24:1:1": named("Ying'an Tanhua", "the unlabeled continuation of Ying'an Tanhua's imperial-birthday hall addresses says that every dust and every land has displayed majestic radiance and every thing fully manifests the true eye.", title=LX),
    "t_ba8066477571:0009a18:1:3": named("Zhenjing Kewen", "the anthology explicitly introduces Zhenjing Kewen, who lifts a corner of his robe, tells the assembly that everyone has a share but must open the crown eye, gives one shout, and leaves the seat.", title=LX),
    "t_5854f7c24ddf:0011a10:1:3": named("Yuanwu Keqin", "in the continuation of Yuanwu Keqin's imperial-commission address at Zhaojue, Yuanwu raises the fly-whisk, asks whether the assembly sees, strikes the Chan seat, and asks again.", title=LX),
    "t_6edb551acb53:0011a24:1:4": named("Mi'an Xianjie", "the anthology explicitly introduces Mi'an Xianjie as opening an imperial-commission hall address by warning entrants not to retain conceptual understanding.", title=LX),
    "t_5854f7c24ddf:0013c16:1:4": named("Foyin Qing", "the anthology explicitly introduces Foyin Qing, who raises the fly-whisk after the exchange, asks whether the assembly sees, strikes the Chan seat, and asks whether it hears.", title=LX),
    "t_afebc7b2a221:0030c01:1:4": named("Yanxi Guangwen", "in the continuation of Yanxi Guangwen's awakening-day hall address, Yanxi slaps his knee and says the sword has long gone and one must never carve the boat.", title=LX),
    "t_b8d2633b12ef:0033a24:1:6": named("Zhantang Shen", "the anthology explicitly introduces Zhantang Shen, who says that forty-nine years and more than three hundred assemblies took in every exposed failure.", ctx(("Shakyamuni Buddha", ("figure-appraised",))), title=LX),
    "t_48bc24c64738:0049a10:1:4": named("Linji Yixuan", "in Linji Yixuan's instruction on discerning guest and host, Linji says that when guest and host meet there is verbal exchange, responsive manifestation toward things, whole-body function, and displays of authority.", title=LX),
    "t_26c9a5cb0fe3:0051b23:1:1": named("Shishuang Chuyuan", "in the address introduced under Shishuang Chuyuan, Chuyuan contrasts Bao Ying's three phrases with his own Daowu formulation: realizing the first phrase mixes mud and water.", ctx(("Bao Ying", ("earlier-formulation-source",))), title=LX),
    "t_8f76148e713f:0053b10:1:3": named("Fushan Fayuan", "the anthology explicitly introduces Fushan Fayuan, who maps three eyes onto one that is watertight, one that opens the whole earth, and one that surveys high and low.", title=LX),
    "t_167e8b0c7ba3:0059b03:1:3": named("Kaixian Ying", "the anthology explicitly introduces Kaixian Ying, who opens his hall address by saying that a painted cake does not satisfy hunger and a drawn person is intolerant of seeing ugliness.", title=LX),
    "t_63ca7d059ee8:0064c17:1:8": named("Deshan Xuanjian", "inside a case raised by a later speaker, Deshan Xuanjian is the exact quoted speaker who answers that his lineage has no words or phrases and truly has no Dharma to give people.", title=LX),
    "t_b33fddd5d4f1:0067a17:1:4": named("Huayan Jue", "the anthology explicitly introduces Huayan Jue, who opens his hall address with the paired lines about standing on the solitary summit and walking on the deepest sea floor.", title=LX),
    "t_b8d2633b12ef:0067b11:1:7": named("Baohua Xian", "the anthology explicitly introduces Baohua Xian, who says that raising one atom embraces the great earth and that a single exposed failure conceals the whole body.", title=LX),
    "t_73fb9441f4fb:0069b04:2:1": named("Guifeng Guang", "the expanded context explicitly heads the address as Guifeng Guang's; Guifeng Guang tells the assembly that lifting their feet and setting them down is already wrong and standing on the spot is still a mistake.", title=LX),
    "t_811316de4c5f:0075b07:1:2": named("Yunmen Wenyan", "the anthology explicitly introduces Yunmen Wenyan, who says that the old worthies could not help opening their cloth bags and that such words still require the listeners to understand them for themselves.", title=LX),
    "t_68d495f2868b:0084c21:1:3": named("Yunmen Wenyan", "a later speaker explicitly quotes Yunmen Wenyan as saying that present-day old monks all set up mechanisms and devices and strike from a printing block.", title=LX),
    "t_b0f2ccf6d140:0086c09:1:2": named("Gulin Mao", "the target address is an unlabeled continuation within the explicitly headed sequence of Gulin Mao's summer-retreat small addresses; Gulin Mao says that the retreat will be established the next day and gives the present night's small address.", title=LX),
    "t_1bbc921aed44:0102a02:1:3": named("Gulin Mao", "the target New Year's Eve small address remains in the unlabeled continuation of Gulin Mao's explicitly headed sequence; Gulin Mao says to kill Buddha on meeting Buddha and kill an ancestor on meeting an ancestor before appraising Zhenjing and Zhaozhou.", title=LX),
    "t_f59209907f3d:0161c10:1:2": named("Huqiu Shaolong", "the anthology explicitly introduces Huqiu Shaolong, who says that lifting the mallet and raising the fly-whisk amount to taking up the matter alone and cutting it off directly.", title=LX),
    "t_b6a1c449bd53:0181c11:1:4": named("Yuanwu Keqin", "the Jiangshan installation address is an unlabeled continuation in the explicitly headed run of Yuanwu Keqin's installation addresses; Yuanwu says that a single phrase cuts through the billion-world universe and sits atop the tongues of everyone in the realm.", title=LX),
    "t_b6a1c449bd53:0195a18:1:5": named("Yuanwu Keqin", "the anthology explicitly introduces Yuanwu Keqin's imperially commanded departure for Yunju; Yuanwu says that sitting astride the Buddha hall and cutting off the realm's tongues cannot yet be called the matter of a patch-robed monk.", title=LX),
}

T51N2077_DECISIONS = {
    "t_9e7c2fb12ad9:0472b16:1:3": named("Guyin Yuncong", "expanded context places the hall verse in Guyin Yuncong's explicitly headed section; Guyin Yuncong describes a five-white cat with fierce claws whose presence ends the vermin's movement in the hall.", title=T51),
    "t_5d84cccab8df:0473a01:1:3": named("Sanjiao Zhisong", "expanded context places the hall address in Sanjiao Zhisong's explicitly headed section; Sanjiao Zhisong says that if the lineage's purport is raised, Mount Sumeru must be pulverized.", title=T51),
    "t_1f6124388d25:0477a16:1:4": named("Dongshan Xiaocong", "expanded context places the consecutive hall addresses in Dongshan Xiaocong's explicitly headed section; Dongshan Xiaocong says that after gruel the sky is bright while the lantern still dozes and the exposed pillar is alert.", title=T51),
    "t_ba8066477571:0478a18:1:5": named("Yungai Zhiyong", "the explicit Yungai Zhiyong heading governs the exchange and address; Yungai Zhiyong surveys the assembly, shouts once, and asks whether they take this as host and guest distinctly evident.", title=T51),
    "t_5854f7c24ddf:0478c25:1:1": named("Yuwang Changtan", "the explicit Yuwang Changtan heading governs the hall address; Yuwang Changtan contrasts upward, downward, and nondual phrases, strikes the Chan seat, and leaves the seat.", title=T51),
    "t_5854f7c24ddf:0483c09:1:2": named("Shishuang Chuyuan", "expanded context places the instruction in Shishuang Chuyuan's explicitly headed biography; Shishuang Chuyuan strikes the Chan seat once with his staff and asks the assembly whether it understands.", title=T51),
    "t_c968268a64d1:0484a01:1:4": named("Shishuang Chuyuan", "in the same explicitly headed Shishuang Chuyuan section, Shishuang Chuyuan opens a hall address with the ancestral mind-seal and says that one seal imprints emptiness, water, and mud.", title=T51),
    "t_408abe2e38ca:0485b29:1:2": named("Dayu Shouzhi", "the explicit Dayu Shouzhi heading governs the consecutive hall addresses; Dayu Shouzhi calls it a ready-made public case and immediately leaves the seat.", title=T51),
    "t_dab856504b69:0485c24:1:8": named("Dayu Shouzhi", "inside Fahua Quanju's biography, Dayu Shouzhi is the exact interlocutor denoted by 愚 who appraises Quanju as an accomplished poet before Quanju answers with the red-thread line.", ctx(("Fahua Quanju", ("respondent", "biographical-subject"))), title=T51),
    "t_4416ef85b3a5:0501a21:1:4": named("Lingyun Baoyin", "the explicit Yuezhou Yunmenshan Lingyun Baoyin heading governs the hall address; Lingyun Baoyin says that the moon is amid the myriad phenomena and their numinous radiance has neither inside nor outside.", title=T51),
    "t_8f76148e713f:0542b08:1:2": named("Kaiyuan Zongyou", "the explicit Kaiyuan Zongyou heading governs the address; Kaiyuan Zongyou says that the ancestral gate is watertight while Buddha-work bends the grass like wind.", title=T51),
    "t_b88b6a8a5659:0548c12:1:3": named("Chaling Yushan Zhu", "the explicit Chaling Yushan Zhu biography narrates that Chaling Yushan Zhu falls when his donkey breaks through the bridge, involuntarily voices Fadeng's sound, and suddenly accords in awakening.", title=T51),
    "t_ba8066477571:0585b24:1:6": named("Nanyue Falun Qitian", "the explicit Nanyue Falun Qitian heading governs the address; Nanyue Falun Qitian gives four successive shouts and labels their displayed comparisons.", title=T51),
    "t_936118ea496c:0613c02:1:5": named("Huanglong Sixin Wuxin", "inside Huanglong Sixin Wuxin's biography, Huanglong Sixin Wuxin is the respondent denoted by 新 who tells Changyu that he saw Huitang Zuxin while eating gruel and rice.", ctx(("Changyu", ("questioner",)), ("Huitang Zuxin", ("teacher-seen",))), title=T51),
    "t_93360aaedb7c:0634a14:1:2": named("Yuanwu Keqin", "in Yuanwu Keqin's explicitly headed biography, Yuanwu Keqin is the returning student whom Wuzu Fayan orders to enter the hall and who then enters the attendant's quarters.", ctx(("Wuzu Fayan", ("teacher", "one-ordering-entry"))), title=T51),
    "t_e6b94cf3dbc3:0669a19:1:2": named("Xuetang Daoxing", "the explicit Xuetang Daoxing heading governs the hall address; Xuetang Daoxing says that the whole body is mouth yet speaks only half, and the whole body is eye yet uses only one piece.", title=T51),
    "t_5b39f18f89ff:0694a21:1:4": named("Dahui Zonggao", "inside Wu Weiming's biography, Dahui Zonggao is the exact actor marked 慧 who raises the dog-has-no-buddha-nature saying, asks Wu about it, and strikes him with the bamboo slip when he starts to answer.", ctx(("Wu Weiming", ("respondent", "biographical-subject"))), title=T51),
}

T51N2076_DECISIONS = {
    "t_ad2c9d24126f:0211c16:1:4": named("Sanghanandi", "in Sanghanandi's explicitly headed lineage biography, Sanghanandi is found seated in meditation and emerges after twenty-one days before Rahulatabhadra questions him about body and mind.", ctx(("Rahulatabhadra", ("questioner", "lineage-predecessor"))), title=JD),
    "t_c968268a64d1:0221a28:1:6": named("Sengna", "in Sengna's explicitly headed biography, Sengna tells his disciple Huiman that the ancestral mind-seal does not consist exclusively in harsh conduct and contrasts two uses of such conduct.", ctx(("Huiman", ("disciple", "addressee"))), title=JD),
    "t_37261001c332:0227a07:1:1": named("Niutou Farong", "inside Niutou Farong's encounter with Daoxin, Niutou Farong answers that he is inspecting mind; Daoxin asks who inspects and what mind is, and Farong gives no answer.", ctx(("Daoxin", ("questioner", "teacher"))), title=JD),
    "t_1793c3514a69:0254c29:1:3": named("Jiashan Shanhui", "inside Damei Fachang's section, Jiashan Shanhui raises his and Dingshan's paired claims and asks Damei which understanding is more intimate; Damei answers that one is intimate and one distant.", ctx(("Damei Fachang", ("respondent", "section-subject")), ("Dingshan", ("paired-case-figure",))), title=JD),
    "t_5d84cccab8df:0256b09:1:5": named("Li Bo", "inside Guizong Zhichang's section, Governor Li Bo is the exact questioner who accepts Mount Sumeru containing a mustard seed but challenges a mustard seed containing Sumeru; Guizong answers with Li's ten thousand books.", ctx(("Guizong Zhichang", ("respondent", "section-subject"))), title=JD),
    "t_2852c7a978c5:0303b06:1:2": named("Fengxue Yanzhao", "expanded context places the response in Fengxue Yanzhao's explicitly headed section; Fengxue Yanzhao answers the request for a slight indication of the mysterious mechanism with flawless white jade and Bian He's severed feet.", title=JD),
    "t_1e41b014d80e:0354b10:1:4": named("Dongyan Kexiu", "the explicit Dongyan Kexiu heading governs the exchange; asked about the road upward, Dongyan Kexiu raises his robe collar to show it.", title=JD),
    "t_9a7a00ea0cd1:0367c16:1:3": unnamed("unnamed questioning monk", "questioner", "inside Baizhang An's explicitly headed section, an unnamed monk asks where the one returns when the ten thousand things return to one; Baizhang An answers that not one person has failed to ask.", ctx(("Baizhang An", ("respondent", "section-subject"))), title=JD),
    "t_48bc24c64738:0382a14:1:2": unnamed("unnamed questioning monk", "questioner", "inside Anguo Xiang's explicitly headed section, an unnamed monk quotes responsive manifestation as like the moon in water and asks what the moon is; Anguo Xiang raises the fly-whisk.", ctx(("Anguo Xiang", ("respondent", "section-subject"))), title=JD),
    "t_c0a6177c9c44:0412b24:1:5": named("Fayan Wenyi", "inside Daoqian's biography, Fayan Wenyi is the exact questioner denoted by Jinghui who cites the Vinaya rule about hearing ornaments through a wall and asks whether seeing the gathered colors and precious metals is breaking the precepts.", ctx(("Yongming Daoqian", ("respondent", "biographical-subject"))), title=JD),
    "t_19784084ccb4:0449a12:1:4": impersonal("compiler's table of verse and praise collections", "the compiler's table of verse and praise collections lists Baozhi's ten Mahayana Praises, twelve Twelve-Hours Songs, and Fourteen-Section Verses; Baozhi is the named author, while the headword occurs in documentary catalog prose rather than a speech turn.", "The passage is a bibliographic list of authors and work counts, without a reporting verb or quoted speaker.", ctx(("Baozhi", ("named-author",))), title=JD),
}

X68N1319_DECISIONS = {
    "t_c3a7862b9971:0523c19:1:3": named("Yongzheng Emperor", "in his imperially authored general preface, the Yongzheng Emperor describes the second barrier as the interpenetration of object and knowledge and of form and emptiness, obtaining great freedom.", title=YS),
    "t_a784d81e277b:0550b13:1:5": unnamed("unnamed lecture master", "respondent", "inside Xingsen's explicitly headed encounter collection, Xingsen calls 'Dharma teacher' and an unnamed lecture master answers; Xingsen then calls him a fine lecture master.", ctx(("Xingsen", ("questioner", "record-owner"))), title=YS),
    "t_8253f56255ce:0554a04:1:5": named("Yongzheng Emperor", "in the Yuanming Layman's recorded discourse, the Yongzheng Emperor says that dust after dust and land after land are himself, that he is every dust and land, and asks who sees them.", title=YS),
    "t_a2612eb1f803:0558c19:1:3": named("Yongzheng Emperor", "in the Yuanming Layman's recorded discourse, the Yongzheng Emperor says that this matter is originally complete and asks who could take it up or put it down.", title=YS),
    "t_37261001c332:0559c09:1:5": named("Yongzheng Emperor", "in the Yuanming Layman's recorded discourse, the Yongzheng Emperor warns that making silence final and then devoting oneself to quietly inspecting mind is escaping one pit only to fall into a ditch.", title=YS),
    "t_5b39f18f89ff:0562b08:1:5": named("Yongzheng Emperor", "in the Yuanming Layman's one-word response sequence, the Yongzheng Emperor answers the question about the dog having no buddha-nature with the single word 'fishhook.'", title=YS),
    "t_a2612eb1f803:0566b20:1:2": unnamed("unnamed questioning monk", "questioner", "inside the Yuanming Layman's recorded encounters, an unnamed monk asks what 'originally complete in itself' means; the Yongzheng Emperor answers that because not one thing can be obtained, completeness is spoken of.", ctx(("Yongzheng Emperor", ("respondent", "record-owner"))), title=YS),
    "t_e156057131dc:0587c09:1:1": named("Yongzheng Emperor", "in the Yuanming Layman's instructional writing, the Yongzheng Emperor asks what one's fundamental investigation is and answers that, for one who trusts the buddha-recollection gate, it is investigating who recollects Buddha.", title=YS),
    "t_37261001c332:0727c13:1:6": named("Xuri Jushi", "in the explicitly headed Initial-Learning Poems by Xuri Jushi, Xuri Jushi says that closing the eyes vainly seeks seeing and inspecting mind mistakenly seeks penetration.", title=YS),
    "t_37261001c332:0735a09:1:7": named("Xuri Jushi", "in Xuri Jushi's Song of Awakening within his explicitly headed collection, Xuri Jushi says that holding mind and inspecting mind divide guest and host.", title=YS),
}

X78N1556_DECISIONS = {
    "t_961b548d6462:0641a15:1:5": named("Song Huizong", "in his imperially bestowed preface dated 1101, Emperor Song Huizong says that the record's direct pointing and singly transmitted mind-seal may be recommended before words.", title=JZ),
    "t_68d495f2868b:0647a17:2:1": named("Puming", "in the colophon signed by Puming, Puming records collecting money for the printing-block heads and carving one set of printing blocks for the Continuation of the Lamp Record in three cases.", title=JZ),
    "t_c968268a64d1:0651a06:1:2": named("Jingjie Shoumi", "the nearest explicit Hezhou Jingjie Shoumi heading governs the hall address; Jingjie Shoumi says that the ancestral mind-seal has neither trace nor shape and has remained distinct and uninterrupted through ages.", title=JZ),
    "t_e5259ce8bbf5:0654c02:2:1": unnamed("unnamed questioning monk", "questioner", "inside Jiufeng Qin's explicitly headed section, an unnamed monk asks Jiufeng Qin to offer an indication within the gate of expedients; Jiufeng answers that Buddha does not take away beings' wishes.", ctx(("Jiufeng Qin", ("respondent", "section-subject"))), title=JZ),
    "t_5d84cccab8df:0658b14:1:6": named("Heshan Chanzhi", "inside Heshan Chanzhi's explicitly headed section, Heshan Chanzhi answers that the seamless monument is atop Mount Sumeru and that Brahma and Indra are the people inside it.", title=JZ),
    "t_8f76148e713f:0660a19:1:1": named("Shishuang Chuyuan", "inside Shishuang Chuyuan's explicitly headed section, Shishuang Chuyuan opens a hall address by saying that Magadha is watertight while the command is personally carried out before Shaoshi Peak.", title=JZ),
    "t_757827b8d4cb:0660b24:1:6": named("Shishuang Chuyuan", "in the same Shishuang Chuyuan biography, Shishuang Chuyuan arranges a sword, sandals, and water in his room and warns a hesitant entrant that he is in danger and has lost body and life.", title=JZ),
    "t_d4df8bc75ad7:0661a01:1:1": named("Dayu Shouzhi", "the explicit Dayu Shouzhi heading governs the hall address; Dayu Shouzhi says that the ten stages are startled and the two vehicles cannot measure, while beneath the patch-robed gate there are copper heads and iron foreheads.", title=JZ),
    "t_d4df8bc75ad7:0661a12:1:2": named("Langya Huijue", "inside Langya Huijue's explicitly headed section, Langya Huijue answers 'what is Buddha?' with 'copper head and iron forehead' and explains with 'bird beak and fish gills.'", title=JZ),
    "t_ad2c9d24126f:0669c22:1:5": unnamed("unnamed questioning monk", "questioner", "inside Liuhe Xiangji Zi's explicitly headed section, an unnamed monk asks what a monk in meditation is; Liuhe Xiangji Zi answers that the four seas are originally limpid.", ctx(("Liuhe Xiangji Zi", ("respondent", "section-subject"))), title=JZ),
    "t_48bc24c64738:0671a23:1:3": named("Zhongshan Tan", "inside Zhongshan Tan's explicitly headed section, Zhongshan Tan says that the teaching body has no image yet manifests form in response to things, then uses his staff to question the assembly about that response.", title=JZ),
}

X68N1318_DECISIONS = {
    "t_73fb9441f4fb:0348a21:1:2": named("Fenyang Shanzhao", "inside Fenyang Shanzhao's explicitly headed recorded sayings, Fenyang says that those who do not see the person of Fenyang are all dead fellows standing on the spot.", title=XG),
    "t_26c9a5cb0fe3:0352b12:1:2": named("Shishuang Chuyuan", "inside Shishuang Chuyuan's explicitly headed recorded sayings, Shishuang calls the preceding account dream-talk, mixing mud with water, fouling the scene, and not knowing good from bad.", title=XG),
    "t_1f6124388d25:0371a04:1:6": named("Huitang Zuxin", "inside Huitang Zuxin's explicitly headed chamber essentials, Huitang says there is one treasure hidden in the body-mountain, then sets the lantern in the Buddha hall and the three gates atop the lantern as a response-demand.", title=XG),
    "t_898279a78ecf:0411a16:1:6": named("Wuzu Fayan", "inside Wuzu Fayan's explicitly headed hall addresses, Wuzu says that Shakyamuni leaving the city at midnight for the Snow Mountains already exposed no small leak and asks what more he contemplated.", ctx(("Shakyamuni Buddha", ("figure-appraised",))), title=XG),
    "t_4416ef85b3a5:0488b07:1:5": named("Kongsou Yin", "inside Kongsou Yin's explicitly headed recorded sayings, Kongsou says in a post-illness address that empty space is the illness and the myriad phenomena are the medicine.", title=XG),
}

X79N1557_DECISIONS = {
    "t_a784d81e277b:0014a20:1:2": named("Ananda", "in the Shakyamuni Buddha section, Ananda is the exact respondent who answers Shakyamuni's call before Shakyamuni tells him to take the bowl and go.", ctx(("Shakyamuni Buddha", ("caller", "teacher"))), title=LH),
    "t_937f63a4fb51:0017b24:1:5": named("Manjusri", "in the exchange with Shanzhu Tianzi, Manjusri is the exact speaker who says that the Tathagata is right before him and then identifies true seeing with seeing nothing at all.", ctx(("Shanzhu Tianzi", ("questioner",))), title=LH),
    "t_a784d81e277b:0018b02:1:3": named("Ananda", "inside Ananda's explicitly headed lineage section, Ananda is the exact respondent who answers Mahakasyapa's call before Mahakasyapa orders the banner pole before the gate overturned.", ctx(("Mahakasyapa", ("caller", "teacher"))), title=LH),
    "t_84e490b1773f:0051b11:1:2": named("Xiyuan Tancang", "inside Xiyuan Tancang's explicitly headed section, Xiyuan Tancang responds to the monk's challenge about heating his own bath by clapping his hands three times.", title=LH),
    "t_b8d2633b12ef:0053a22:1:4": named("Pang Yun", "inside Qifeng Heshang's section, Pang Yun is the exact lay speaker who seizes Qifeng's staff and says that the thief has suffered an exposed failure today.", ctx(("Qifeng Heshang", ("interlocutor", "section-subject"))), title=LH),
    "t_84e490b1773f:0054c18:1:4": named("Shuilao Heshang", "inside Shuilao Heshang's explicitly headed case, Shuilao is knocked down by Mazu Daoyi, awakens, rises clapping his hands, and laughs while declaring the many teaching gates known at one point.", ctx(("Mazu Daoyi", ("teacher", "striker"))), title=LH),
    "t_ec1241360056:0106c13:2:1": named("Guanghui Yuanlian", "inside Guanghui Yuanlian's explicitly headed section, Guanghui raises his staff, calls the assembly a company of holeless iron hammers, and orders them to withdraw quickly.", title=LH),
    "t_e95ea628d5dd:0106c15:1:2": named("Guanghui Yuanlian", "in the same section, Guanghui Yuanlian says that he does not avoid the scrutiny of masters everywhere and enters mud and water for the listeners before demanding news from anyone who understands.", title=LH),
    "t_ec1241360056:0107b16:2:2": named("Guanghui Yuanlian", "in Guanghui Yuanlian's exchange with an unnamed lecture master, Guanghui appraises the answer as a holeless iron hammer and orders the lecturer to enter the hall.", title=LH),
    "t_84043ffcdf90:0112a14:1:5": named("Langya Huijue", "inside Langya Huijue's explicitly headed section, Langya asks whether beneath his gate there is a patch-robed monk who possesses the eye and a genuine person of the Way.", title=LH),
    "t_31575552ede2:0120a18:1:3": named("Cuiyan Kezhen", "inside Cuiyan Kezhen's explicitly headed section, Cuiyan says that a hearer sees the staff, recognizes stubborn emptiness, and denies the staff.", title=LH),
    "t_372fb5a2b7ce:0133b21:1:3": named("Baofeng Shanqing", "inside Baofeng Shanqing's explicitly headed section, Baofeng answers that a living phrase is obtained within death and a dead phrase within life.", title=LH),
    "t_f2f4079b20e5:0159a13:1:5": named("Tiantong Tanhua", "inside Tiantong Tanhua's explicitly headed section, Tiantong quotes the paired formulation about making the host wherever one goes and the lineage according with conditions, then says the Dharma banner is established everywhere.", title=LH),
}

X84N1583_DECISIONS = {
    "t_ec1241360056:0415a19:1:2": named("Huguo Cian Jingyuan", "inside Huguo Cian Jingyuan's explicitly headed section, Huguo answers that what heaven cannot cover and earth cannot support is a holeless iron hammer, then rejects the monk's extrapolation as delusive thought.", title=ZD),
    "t_9e7c2fb12ad9:0426a11:1:4": named("Zhengdang Mingbian", "inside Zhengdang Mingbian's explicitly headed section, Zhengdang poses the chamber question asking why a cat loves catching mice, followed by another question about a dog barking when the board sounds.", title=ZD),
    "t_93360aaedb7c:0437c11:1:5": named("Wenshu Siye", "inside Wenshu Siye's explicitly headed section, Wenshu asks the former butcher what he saw while killing a pig that made him shave his head and travel; after the man makes a knife-beating gesture, Wenshu shouts and orders him to enter the hall.", title=ZD),
    "t_ec1241360056:0448c07:1:4": named("Shimen Zhenjue Yuanweng Xin", "inside Shimen Zhenjue Yuanweng Xin's explicitly headed section, Yuanweng says that the whole ten-direction world is a holeless iron hammer unknown to Kasyapa, leaving no place for conjecture or assumption.", ctx(("Mahakasyapa", ("figure-appraised",))), title=ZD),
    "t_5e59b126e608:0487c16:2:2": named("Xiaoyin Daxin", "inside Xiaoyin Daxin's explicitly headed hall address, Daxin raises Huanglong Huinan's three chamber questions, calls them Huanglong's Three Checkpoints, and compares them to Lord Shang instituting laws.", ctx(("Huanglong Huinan", ("earlier-formulation-source",))), title=ZD),
    "t_a9f422b3b249:0487c17:1:3": named("Huanglong Huinan", "inside a case raised by Xiaoyin Daxin, Huanglong Huinan is the explicitly named source of the question asking each person to identify the senior monk's birth-condition; Daxin then appraises the three questions as Huanglong's Three Checkpoints.", ctx(("Xiaoyin Daxin", ("later-raiser", "section-subject"))), title=ZD),
    "t_85eef19d3d3a:0489b09:1:5": named("Baozhou Juean", "inside Baozhou Juean's explicitly headed biography, Juean is explaining the Surangama when water from a pure bottle suddenly surges into his robe; he laughs and calls it accidental.", title=ZD),
    "t_eba970114dd2:0631b07:1:4": named("Sanyi Mingyu", "inside Sanyi Mingyu's explicitly headed small address, Sanyi says that patch-robed monks lose their noses and forget the old Handan gait, then surveys the assembly and says 'exposed.'", title=ZD),
}

T47N1997_DECISIONS = {
    "t_b6a1c449bd53:0714c12:1:6": unnamed("unnamed questioning monk", "questioner", "in Yuanwu Keqin's hall address, an unnamed monk concludes that this means sitting across and cutting off the ten directions; Yuanwu answers 'seven vertical, eight horizontal.'", ctx(("Yuanwu Keqin", ("respondent", "record-owner"))), title=YW),
    "t_aef7434b8470:0719c01:1:2": unnamed("unnamed questioning monk", "questioner", "in Yuanwu Keqin's hall address, an unnamed monk advances the comment 'well, no connection'; Yuanwu presses him by asking where there is no connection.", ctx(("Yuanwu Keqin", ("respondent", "record-owner"))), title=YW),
    "t_aef7434b8470:0725a27:1:5": named("Yuanwu Keqin", "in his own hall address, Yuanwu Keqin says that before the King of Awe-Voice there is no connection, the patriarch did not come from the west, and Shaolin has a subtle key.", title=YW),
    "t_408abe2e38ca:0732b16:1:5": named("Yuanwu Keqin", "at the opening of his Tianning Monastery installation address, Yuanwu Keqin says that before the ready-made public case is spoken its pattern is already manifest, and that raising its penetrated root leaks through layer after layer.", title=YW),
    "t_9a7a00ea0cd1:0793b24:1:1": unnamed("unnamed questioning monk", "questioner in an earlier case", "inside a case raised and commented on by Yuanwu Keqin, an unnamed monk asks Zhaozhou Congshen where the one returns when the ten thousand things return to one; Zhaozhou answers with his seven-catty cloth shirt.", ctx(("Zhaozhou Congshen", ("respondent", "earlier-case-master")), ("Yuanwu Keqin", ("later-raiser", "record-owner"))), title=YW),
}

J33NB294_DECISIONS = {
    "t_a38d5c680c67:0736c15:1:1": named("Yunxi Langting Ting", "in his volume-four general address, Yunxi Langting Ting instructs beginning students to chew over an unbreakable ready-made sentence repeatedly until old masters' tongues can no longer deceive them.", title=YL),
}

T48N2016_DECISIONS = {
    "t_37261001c332:0550a25:1:4": impersonal("quoted Puxian Contemplation text", "the quoted Puxian Contemplation text is explicitly introduced by Yongming Yanshou as saying to inspect mind as no-mind, that mind itself is empty, and that this is called correct contemplation.", "The reporting formula 普賢觀云 names a source text rather than a human speech turn.", ctx(("Yongming Yanshou", ("compiler", "quoting-author"))), title=ZJ),
    "t_ad2c9d24126f:0613a15:1:6": named("Yongming Yanshou", "in his Source-Mirror argument, Yongming Yanshou criticizes merely leaving thought to enter concentration and rejecting objects to seek truth, saying this misses each speck as Manjusri and each thought as Samantabhadra.", title=ZJ),
}

X79N1559_DECISIONS = {
    "t_dfd1dbffe9f2:0310a22:1:5": named("Dahong Baoen", "inside Dahong Baoen's explicitly headed biography, Dahong Baoen says in a hall address that saying 'this mind is this buddha' here resembles placing a head atop one's head, while saying 'not mind, not buddha' resembles mistaking a reflected head.", title=JT),
    "t_d1e06fd225fa:0439a08:1:4": named("Zhengwu Yuanzhi", "inside Zhengwu Yuanzhi's explicitly headed biography, Zhengwu Yuanzhi relies on Dharma Master Bailian Xian, enters his chamber, and asks about the Way of complete change; Bailian Xian answers by pointing to a moving lamp.", ctx(("Bailian Xian", ("teacher", "respondent"))), title=JT),
}

X66N1296_DECISIONS = {
    "t_549e7766dfa1:0011a08:1:1": named("Baofeng Bian", "in his explicitly introduced comment, Baofeng Bian says that old worthies take Kasyapa as holding the strategic pass and Manjusri as cutting off the ten directions, but in Baofeng's view this is precisely letting one move pass.", title=ZG),
    "t_b90a5f36ec86:0024c09:1:4": named("Tiantong Hongzhi Zhengjue", "in his explicitly introduced comment after raising Baozhi's words, Tiantong Hongzhi Zhengjue asks how many people there are who toy with their spirit-soul.", ctx(("Baozhi", ("earlier-source-raised",))), title=ZG),
    "t_592227b212c1:0247a03:1:3": named("Letan Hongying", "inside Letan Hongying's explicitly headed exchange, after the unnamed monk claps once Letan says that this too is obtaining life within death.", title=ZG),
}

J34NB311_DECISIONS = {
    "t_d9c587fad710:0645c23:1:5": named("Juelang Dasheng", "in his tea conversation, Juelang Dasheng answers Zhang Gongzhi's question about a clay buddha turning around with 'lose money in the water and dredge for it in the river.'", title=JL),
    "t_2dd4fec35455:0651b07:1:3": named("Juelang Dasheng", "in his instruction to Chan student Huisheng, Juelang Dasheng says that he is accustomed to lighting a cold stove and that a bean might burst from dead ashes.", title=JL),
}

X81N1568_DECISIONS = {
    "t_57ef1bbc3a81:0001a16:1:5": named("Dizang Guichen", "inside Fayan Wenyi's biography, Dizang Guichen answers Fayan's statement that he does not know the business of pilgrimage by saying 'not knowing is most intimate.'", ctx(("Fayan Wenyi", ("student", "biographical-subject"))), title=WY),
    "t_4416ef85b3a5:0002c03:1:1": named("Fayan Wenyi", "inside Fayan Wenyi's explicitly headed section, Fayan answers the question about the second moon with 'the myriad phenomena' and the question about the first moon with the reversed wording 'phenomena myriad.'", title=WY),
    "t_23204fbd253c:0011a17:1:3": unnamed("unnamed questioning monk", "questioner", "inside Zhangyi Daoqin's explicitly headed section, an unnamed monk asks what the mysterious purport is; Zhangyi Daoqin answers by asking what purport the mysterious has.", ctx(("Zhangyi Daoqin", ("respondent", "section-subject"))), title=WY),
    "t_9e7c2fb12ad9:0020b23:1:5": named("Luohan Xinglin Zuyin", "inside Luohan Xinglin Zuyin's explicitly headed section, Zuyin lifts a cat that has jumped onto him, contrasts Nanquan's earlier killing with his own present display, and offers the cat to the students before setting it down.", ctx(("Nanquan Puyuan", ("earlier-case-master",))), title=WY),
    "t_549e7766dfa1:0056b20:1:2": named("Yunfeng Wenyue", "inside Yunfeng Wenyue's explicitly headed hall address, Yunfeng sets Linji's vanguard as letting one move pass and Deshan's later command aside before asking for the uniquely exposed impartial phrase.", title=WY),
}

J36NB359_DECISIONS = {
    "t_84043ffcdf90:0628c24:1:3": named("Baiyu Si", "in his Longhua Monastery hall address, Baiyu Si says that an expert who possesses the eye must have a mechanism of escape within every phrase.", title=BY),
    "t_1bbc921aed44:0643a22:1:4": named("Baiyu Si", "in his Qinglong Monastery hall address, Baiyu Si contrasts having no grandmotherly kindness twenty-five years earlier with having it afterward, glossing the latter as supporting infants and encouraging children.", title=BY),
    "t_2facdfa49dd9:0669c01:1:5": named("Baiyu Si", "in his Shanquan Monastery end-of-retreat address, Baiyu Si says that the final solid checkpoint cannot be measured by humans or devas and cannot be reached by discriminating consciousness.", title=BY),
}

J39NB471_DECISIONS = {
    "t_e156057131dc:0959a10:1:5": named("Konggu Daocheng", "in his end-of-retreat hall address, Konggu Daocheng says to raise one's fundamental investigation and work at it day and night until the karmic obstruction of a thousand lives melts away in a moment.", title=KG),
    "t_96255c741b17:0962b22:1:5": named("Konggu Daocheng", "in his hall address to laypeople, Konggu Daocheng compares the lineage's tea and rice to iron-nail rice and wood-splinter soup, and says that persisting at the tasteless point makes a great vessel.", title=KG),
}

X72N1444_DECISIONS = {
    "t_e156057131dc:0772b20:1:3": named("Zhanran Yuancheng", "in his opening winter-retreat hall address, Zhanran Yuancheng tells beginners to place their fundamental-investigation phrase before them like leaning against Mount Sumeru, without interruption in walking, standing, sitting, or lying down.", title=ZR),
    "t_b0f2ccf6d140:0795c16:1:4": named("Zhanran Yuancheng", "in his Yunmen retreat tea conversation, Zhanran Yuancheng defines forming the retreat as joining like-minded friends and controlling the wild mind.", title=ZR),
    "t_cba9cbb44845:0798b24:1:4": named("Zhanran Yuancheng", "in his verse on the old woman burning the hut, Zhanran Yuancheng says that when the hut-burning old woman comes straight at one there is not a single thing and warns clear-eyed patch-robed monks not to take it lightly.", title=ZR),
    "t_b8d2633b12ef:0801c13:1:3": named("Zhanran Yuancheng", "in his recorded questions and answers, Zhanran Yuancheng answers how to avoid an exposed failure with 'eat three meals,' then continues the public exchange when challenged.", title=ZR),
}

T48N2006_DECISIONS = {
    "t_6b8e3b4f44bb:0304a13:1:1": named("Linji Yixuan", "in the section on Linji's Four Illuminations and Functions, Linji Yixuan says simultaneous illumination and function drive off the ploughman's ox and seize the hungry person's food, whereas nonsimultaneous illumination and function establish questioner and respondent, host and guest.", title=RT),
    "t_bf467ac18ec0:0310b12:1:3": named("Huanglong Huinan", "in the Three Checkpoints case, Huanglong Huinan asks Longqing Xian how his hand resembles Buddha's hand and how his foot resembles a donkey's foot; Longqing supplies the answers.", ctx(("Longqing Xian", ("respondent",))), title=RT),
    "t_a9f422b3b249:0310c03:1:2": named("Zhenjing Kewen", "in his explicitly labelled verse, Zhenjing Kewen says that everyone has a place of birth-condition but recognizing it still means losing the road.", title=RT),
    "t_cf6aac2f936b:0312b28:1:2": impersonal("anthology's One-Word Checkpoint heading", "the anthology's One-Word Checkpoint heading introduces a sequence of questions answered by Yunmen Wenyan with single written characters, beginning with the Yunmen sword.", "The headword is the anthology's rubric for the collected exchange, not a spoken word inside the exchange.", ctx(("Yunmen Wenyan", ("one-word-respondent",))), title=RT),
    "t_cf6aac2f936b:0312c12:1:1": impersonal("anthology's lineage-labeling statement", "the anthology's lineage-labeling statement says that because the master commonly responded to circumstances in this fashion, the Chan community called it the One-Word Checkpoint.", "The sentence is third-person documentary classification, with 叢林 as the naming community and no speech turn.", title=RT),
}

T48N2025_DECISIONS = {
    "t_3f7a6ab74b68:1113c13:1:1": impersonal("Baizhang monastic-code procedural voice", "the Baizhang monastic-code procedural voice directs that after three strikes on the bell before the monks' hall, the assembly exchanges bows and disperses.", "The clause is prescriptive institutional procedure without a quoted speaker.", ctx(("Dehui", ("compiler", "redactor"))), title=BQ),
    "t_3f7a6ab74b68:1123a14:1:2": impersonal("Baizhang monastic-code procedural voice", "the Baizhang monastic-code procedural voice directs that a donor's place be set inside the monks' hall, apart from the abbot, for the communal meal.", "The clause is prescriptive institutional procedure without a quoted speaker.", ctx(("Dehui", ("compiler", "redactor"))), title=BQ),
    "t_f0cb4dcfc70c:1140a14:1:3": impersonal("Baizhang monastic-code procedural voice", "the Baizhang monastic-code procedural voice describes the old rule for a traveling monk seeking lodging: first present oneself at the guest office, then register with the hall office and receive an assigned place.", "The clause is prescriptive institutional procedure without a quoted speaker.", ctx(("Dehui", ("compiler", "redactor"))), title=BQ),
    "t_f0cb4dcfc70c:1141a29:1:4": impersonal("Baizhang monastic-code procedural voice", "the Baizhang monastic-code procedural voice says that after the abbot permits registration, the attendant's office posts the notice to the hall office and the newcomer is sent into the communal quarters.", "The clause is prescriptive institutional procedure without a quoted speaker.", ctx(("Dehui", ("compiler", "redactor"))), title=BQ),
}

J34NB299_DECISIONS = {
    "t_fac9b9afebf6:0132a05:1:4": named("Sanfeng Hanyue Fazang", "in his Shengen Monastery address, Sanfeng Hanyue Fazang comments on Linji's classification of a shout as probing pole and shadowing grass by saying that before beacon smoke moves, the cannon has already fired.", ctx(("Linji Yixuan", ("earlier-classification-source",))), title=SF),
    "t_19705602b956:0135b07:1:4": named("Sanfeng Hanyue Fazang", "in his Shengen Monastery address, Sanfeng Hanyue Fazang says there is no need to remove ordinary feelings and holy views, followed by cold everywhere as cold and heat everywhere as heat.", title=SF),
    "t_6293dead3bb2:0139b05:1:7": named("Sanfeng Hanyue Fazang", "inside his Shengen Monastery exchange, Sanfeng Hanyue Fazang turns around and immediately walks away when pressed for another statement; the text then begins a new hall address.", title=SF),
    "t_57ef1bbc3a81:0146c04:1:3": named("Sanfeng Hanyue Fazang", "in the extended Shengen Monastery record, Sanfeng Hanyue Fazang says that one part of ignorance called lack of knowledge is named mountains, rivers, and great earth.", title=SF),
}

T48N2003_DECISIONS = {
    "t_35c3fb655630:0150b15:1:1": named("Yuanwu Keqin", "in his commentary on Muzhou's exchange, Yuanwu Keqin says that at the exact point of testing a person one knows the tone as soon as the person opens his mouth.", ctx(("Muzhou Daoming", ("earlier-case-master",))), title=BC),
}

J36NB369_DECISIONS = {
    "t_c3a7862b9971:0903a30:1:4": named("Zhean Jingfan", "in his Guzisheng Monastery hall address, Zhean Jingfan describes being free and unbound in every place and without fault at every time.", title=ZF),
}

J37NB386_DECISIONS = {
    "t_2dd4fec35455:0379b03:1:4": named("Yuan'an Feng", "in his formal taking-up-the-whisk address, Yuan'an Feng says that stirring dead ashes sends fierce flames soaring and that spring wind melts frost and snow without moving.", title=YA),
    "t_2facdfa49dd9:0382c05:1:1": named("Yuan'an Feng", "in his general address, Yuan'an Feng says that the whole tangle of named old cases is called the final solid checkpoint.", title=YA),
}

J39NB466_DECISIONS = {
    "t_96255c741b17:0845a18:1:4": named("Baishan Kai", "in his small address, Baishan Kai instructs students not to let go until they reach the point with nowhere to set a hand, no flavor, and the mind-road cut off.", title=BK),
    "t_b90a5f36ec86:0848a25:1:1": named("Baishan Kai", "in his instruction to the assembly, Baishan Kai repeatedly classifies speaking of school or doctrine, Chan or the Way, mind or nature as toying with the spirit-soul.", title=BK),
}

# Final small-source tail: every row below was reviewed against its complete case,
# governing section, source title, TEI metadata, and available parallel evidence.
TAIL_DECISIONS = {
"X78n1553": {
 "t_961b548d6462:0488a03:1:4": impersonal("lamp compiler's narrative", "the lamp compiler's narrative says that the matter before words handed down from Fengxue Yanzhao was presented to Shoukuo Shangzuo, who then replied.", "The headword occurs in third-person transmission narrative rather than a quoted turn.", ctx(("Fengxue Yanzhao",("transmission-source",)),("Shoukuo Shangzuo",("respondent",))), title="Tiansheng Expanded Lamp Record (天聖廣燈錄)"),
},
"J34nB300": {"t_4d4ce329367f:0243a06:1:3": named("Chaozong Tongren", "in his small address, Chaozong Tongren says that talking food with the lips never fills the belly.", title="Recorded Sayings of Chaozong (朝宗禪師語錄)")},
"J39nB454": {
 "t_dd2b39789323:0602b26:1:7": named("Pin Jixiang", "in his Yunfeng Foguo Monastery discourse, Pin Jixiang calls a phrase outside the standard a worn-out straw sandal.", title="Recorded Sayings of Pin Jixiang (頻吉祥禪師語錄)"),
 "t_19705602b956:0649b20:1:2": unnamed("unnamed questioning monk", "questioner", "in Pin Jixiang's record, an unnamed monk quotes Caoshan's ordinary-feelings-and-holy-views formula and asks how to interchange without falling.", ctx(("Pin Jixiang",("respondent","record-owner")),("Caoshan Benji",("quoted-source",))), title="Recorded Sayings of Pin Jixiang (頻吉祥禪師語錄)"),
},
"X69n1359": {"t_7887dc8d449f:0522c05:1:4": named("Ying'an Tanhua", "in his Guizong Monastery discourse, Ying'an Tanhua praises the old venerable elders who bore their communities without weariness.", title="Recorded Sayings of Ying'an Tanhua (應菴曇華禪師語錄)")},
"X72n1443": {
 "t_6ac3f9f0a2d2:0737c22:1:2": named("Zongbao Daodu", "in his instruction to the assembly, Zongbao Daodu quotes 'he now is precisely me; I now am not him' and says the five lord-minister positions emerge from it.", title="Recorded Sayings of Zongbao Daodu (宗寶道獨禪師語錄)"),
 "t_c9ba42aa7e47:0739c02:1:5": named("Zongbao Daodu", "in his instruction to the assembly, Zongbao Daodu says Chan-hall sitting commonly presents dull sinking or agitation.", title="Recorded Sayings of Zongbao Daodu (宗寶道獨禪師語錄)"),
 "t_faf30cf1fb87:0740b23:1:3": named("Zongbao Daodu", "in his instruction to the assembly, Zongbao Daodu calls taking the presently bright and numinous as correct a fault.", title="Recorded Sayings of Zongbao Daodu (宗寶道獨禪師語錄)"),
},
"J37nB392": {
 "t_2069b9c33315:0580c26:2:1": named("Hansong Zhicao", "in his Xianglu Monastery discourse, Hansong Zhicao lists duty between lord and minister among the five relations.", title="Recorded Sayings of Hansong Zhicao (寒松操禪師語錄)"),
 "t_167e8b0c7ba3:0582c27:1:4": named("Hansong Zhicao", "in his Yunji Monastery discourse, Hansong Zhicao compares wielding staff and shout to filling hunger with a painted cake.", title="Recorded Sayings of Hansong Zhicao (寒松操禪師語錄)"),
},
"T47n2000": {"t_7ba176a992ec:0994c10:1:6": named("Yunmen Wenyan", "inside a case raised by Xutang Zhiyu, Yunmen Wenyan is the explicitly named earlier speaker who says to meet the eye without obstruction.", ctx(("Xutang Zhiyu",("later-raiser","record-owner")),), title="Recorded Sayings of Xutang (虛堂和尚語錄)")},
"T48n2004": {
 "t_85eef19d3d3a:0254b23:1:2": named("Nanyang Huizhong", "in case forty-two, Nanyang Huizhong asks for the pure bottle and then orders it returned to its old place.", title="Book of Serenity (萬松老人評唱天童覺和尚頌古從容庵錄)"),
 "t_85eef19d3d3a:0254b27:1:3": named("Wansong Xingxiu", "in his commentary on case forty-two, Wansong Xingxiu says that he calls an attendant and orders water added to the pure bottle.", title="Book of Serenity (萬松老人評唱天童覺和尚頌古從容庵錄)"),
 "t_2852c7a978c5:0257c27:1:1": impersonal("Book of Serenity's historical narrative", "the Book of Serenity's historical narrative recounts Bian He's discovery and presentation of the uncut jade and the king's punishment.", "The sentence is third-person explanatory narrative, not a speech turn.", ctx(("Wansong Xingxiu",("commentator",)),), title="Book of Serenity (萬松老人評唱天童覺和尚頌古從容庵錄)"),
},
"X67n1299": {"t_2852c7a978c5:0047a01:1:5": impersonal("Chan Grove anthology verse", "the Chan Grove anthology verse contrasts Bian He's wisdom with the Chu king's dislike while invoking the fine jade of Jing Mountain.", "The stored witness is an unattributed anthology verse rather than a marked speech turn.", title="Classified Collection of the Chan Grove (禪林類聚)")},
"X69n1333": {
 "t_7ba176a992ec:0074a15:1:1": unnamed("unnamed questioning monk", "questioner", "an unnamed monk asks Xuefeng Yicun about the affair meeting the eye; Xuefeng answers 'what is it?'.", ctx(("Xuefeng Yicun",("respondent","record-owner")),), title="Recorded Sayings of Xuefeng Yicun (雪峰義存禪師語錄（真覺禪師語錄）)") ,
 "t_7ba176a992ec:0074c23:1:2": unnamed("unnamed questioning monk", "questioner", "an unnamed monk asks Xuefeng Yicun about the affair meeting the eye; Xuefeng raises the whisk and tests the monk.", ctx(("Xuefeng Yicun",("respondent","record-owner")),), title="Recorded Sayings of Xuefeng Yicun (雪峰義存禪師語錄（真覺禪師語錄）)"),
 "t_7ba176a992ec:0075b14:1:3": unnamed("unnamed questioning monk", "questioner", "an unnamed monk asks Xuefeng Yicun about awakening meeting the eye; Xuefeng answers with the exposed pillar.", ctx(("Xuefeng Yicun",("respondent","record-owner")),), title="Recorded Sayings of Xuefeng Yicun (雪峰義存禪師語錄（真覺禪師語錄）)"),
},
"J37nB388": {
 "t_e96268628f2c:0450a19:1:5": unnamed("unnamed questioning monk", "questioner", "an unnamed monk asks Shending Yikui how the ten thousand things return to one and then where the one returns.", ctx(("Shending Yikui",("respondent","record-owner")),), title="Recorded Sayings of Shending Yikui (神鼎一揆禪師語錄)"),
 "t_e931d476fd02:0472c20:1:3": named("Shending Yikui", "in his formal small address, Shending Yikui criticizes rigid adherence to the luxuriant-yellow-flowers formula.", title="Recorded Sayings of Shending Yikui (神鼎一揆禪師語錄)"),
 "t_e931d476fd02:0489a03:1:4": named("Shending Yikui", "in his recorded encounter, Shending Yikui raises the luxuriant-yellow-flowers formula as a question and rejects the officer's reply.", title="Recorded Sayings of Shending Yikui (神鼎一揆禪師語錄)"),
},
"J37nB394": {"t_eba970114dd2:0699b18:2:1": named("Hanshan", "in the collected Hanshan poem, Hanshan's poetic voice says that his home is in Handan and his singing rises and falls.", title="Recorded Sayings of Yi'an (翼菴禪師語錄)")},
"J38nB424": {"t_4cf045deab37:0623a09:2:4": named("Puming", "in Puming's biographical record, Puming's verse says to remain a gruel-and-rice monk, stop pursuing learned knowledge, and insist on clarifying mind.", title="Recorded Sayings of Xiangyan (香嚴禪師語錄)")},
"J38nB425": {"t_bf67613e4573:0669a29:1:5": named("Jifei Ruyi", "in his verse on Guanyin, Jifei Ruyi places a hundred-thousand hands and eyes on a single hair-tip.", title="Complete Record of Jifei (即非禪師全錄)")},
"J39nB447": {
 "t_300236cb6368:0379b25:1:1": named("Meixi Fudu", "in his hall address, Meixi Fudu contrasts directly shouldering it with missing it face-to-face.", title="Recorded Sayings of Dongshan Meixi Du (東山梅溪度禪師語錄)"),
 "t_300236cb6368:0412c27:1:2": named("Meixi Fudu", "in his reply to layman Yuye Huang, Meixi Fudu calls his earlier failure to recognize the layman's freedom missing it face-to-face.", title="Recorded Sayings of Dongshan Meixi Du (東山梅溪度禪師語錄)"),
},
"J40nB494": {"t_8bced2c0bc2f:0555c17:1:3": named("Yushan Shangsi", "in public exchange, Yushan Shangsi appraises the monk with 'the lion bites the person; the Han hound chases the clod.'", title="Recorded Sayings of Yushan (雨山和尚語錄)")},
"T47n1992": {"t_b887bf089b98:0600a06:1:3": named("Fenyang Shanzhao", "in his discourse, Fenyang Shanzhao contrasts outward seeking with arising right there and appearing wherever one goes.", title="Recorded Sayings of Fenyang Wude (汾陽無德禪師語錄)")},
"T48n2005": {"t_9a5dc768cbc5:0295b14:1:2": named("Nanquan Puyuan", "in case nineteen, Nanquan Puyuan answers Zhaozhou Congshen's question about the Way with 'ordinary mind is the Way.'", ctx(("Zhaozhou Congshen",("questioner",)),), title="The Gateless Barrier (無門關)")},
"X63n1245": {
 "t_f0cb4dcfc70c:0523c22:1:2": impersonal("Chan Monastic Regulations procedural voice", "the Chan Monastic Regulations procedural voice directs that after registration luggage be locked in a hall cabinet.", "The clause is institutional procedure without a speech turn.", ctx(("Changlu Zongze",("compiler",)),), title="Chan Monastic Regulations ((重雕補註)禪苑清規)"),
 "t_f0cb4dcfc70c:0524a07:1:1": impersonal("Chan Monastic Regulations procedural voice", "the Chan Monastic Regulations procedural voice directs one seeking registration to present formally at the hall office.", "The clause is institutional procedure without a speech turn.", ctx(("Changlu Zongze",("compiler",)),), title="Chan Monastic Regulations ((重雕補註)禪苑清規)"),
},
"X63n1250": {"t_bb19ed0e0fab:0628d02:1:2": impersonal("Chan Grove Auxiliary Code procedural voice", "the Chan Grove Auxiliary Code procedural voice directs placard posting and hall preparation for a general address.", "The clause is institutional procedure without a speech turn.", ctx(("Yixian",("compiler",)),), title="Chan Grove Auxiliary Code (禪林備用清規)")},
"X63n1259": {"t_e156057131dc:0776c11:1:2": named("Jiexian", "in his hall-management instruction, Jiexian says one must know each member's fundamental-investigation phrase before applying the forge.", title="Discourse on Forging Chan Practitioners (禪門鍛鍊說)")},
"X67n1303": {
 "t_84043ffcdf90:0270a24:1:1": named("Linquan Conglun", "at the close of his comment on case four, Linquan Conglun asks one who possesses the eye to discern the case.", title="Empty Valley Collection (林泉老人評唱投子青和尚頌古空谷集)"),
 "t_a38d5c680c67:0287b02:1:4": named("Linquan Conglun", "after the old monk's thousand-year peach pit, Linquan Conglun comments 'strictly avoid chewing.'", title="Empty Valley Collection (林泉老人評唱投子青和尚頌古空谷集)"),
},
"X67n1304": {"t_283dce854520:0332a19:1:6": named("Linquan Conglun", "in his comment on the Daowu-Shishuang case, Linquan Conglun says awakening wherever the eye meets has no empty gap.", title="Empty Hall Collection (林泉老人評唱丹霞淳禪師頌古虗堂集)")},
"X69n1362": {
 "t_f0cb4dcfc70c:0625b05:1:5": named("Yexian Guixing", "Yexian Guixing laughs at Fushan Fayuan and Tianyi Yihuai's persistence and tells the two to go register.", ctx(("Fushan Fayuan",("student",)),("Tianyi Yihuai",("student",))), title="Recorded Sayings of Pujue Zonggao (普覺宗杲禪師語錄)"),
 "t_f0cb4dcfc70c:0625c11:1:6": named("Fayun Faxiu", "Fayun Faxiu directs that Ziqing register elsewhere until a place opens at Fayun.", title="Recorded Sayings of Pujue Zonggao (普覺宗杲禪師語錄)"),
 "t_eba970114dd2:0627c03:3:1": impersonal("biographical compiler's narrative", "the biographical compiler's narrative identifies Handan Gong as Li Shu, then aged seven.", "The clause is third-person biography rather than a speech turn.", title="Recorded Sayings of Pujue Zonggao (普覺宗杲禪師語錄)"),
},
"X70n1402": {"t_78bd967fdcd6:0736c10:1:1": named("Zhongfeng Mingben", "in his instruction to Chan student Yi, Zhongfeng Mingben defines heavy doubt as great doubt and light doubt as small doubt.", title="Miscellaneous Records of Tianmu Mingben (天目明本禪師雜錄)")},
"X81n1571": {"t_f04c29743e77:0641b22:1:4": named("Pan'an Jicheng", "in his hall address, Pan'an Jicheng contrasts a world of buddha-seekers with the rarity of an at-ease person of the Way.", title="Complete Book of the Five Lamps, volumes 1–33 (五燈全書(第1卷-第33卷))")},
"J35nB336": {"t_ab6276be6e08:0691c06:1:6": named("Tianning Xipu", "at death, Tianning Xipu tells the assembly 'a mighty great man does not understand the final phrase' and lies down on his right side.", title="Recorded Sayings of Huigong Xiong (南海寶象林慧弓詗禪師語錄)")},
"J37nB396": {"t_300236cb6368:0725c24:1:6": named("Panlong Zisu", "in his small address, Panlong Zisu raises the staff and warns his brothers not to miss its news face-to-face.", title="Recorded Sayings of Panlong Zisu (終南山蟠龍子肅禪師語錄)")},
"J38nB410": {"t_d4df8bc75ad7:0336b24:1:4": named("Lianfeng", "in his Xinghua Ximing Monastery verse, Lianfeng says bronze-headed, iron-browed fellows are all within this mountain.", title="Recorded Sayings of Lianfeng (蓮峰禪師語錄)")},
"J38nB418": {"t_240ea0594a5f:0500a10:1:4": named("Huiyue Xu", "in his Jinming Monastery discourse, Huiyue Xu criticizes later descendants for taking a bitten excrement-stake as the ground of great rest and cessation.", title="Recorded Sayings of Huiyue Xu (晦嶽旭禪師語錄)")},
"J38nB423": {"t_dd2b39789323:0596a28:1:6": named("Shiguan Fazang", "in his case appraisal, Shiguan Fazang says Jiashan uses the basic-function mechanism while the monk performs a phrase outside the standard.", title="Recorded Sayings of Shiguan (石關禪師語錄)")},
"J38nB431": {
 "t_c9ba42aa7e47:0976a19:1:3": unnamed("unnamed questioning student", "questioner", "an unnamed student claims not to fall into dull sinking or follow agitation; Yunfeng Tizong Ning prescribes thirty blows.", ctx(("Yunfeng Tizong Ning",("respondent","record-owner")),), title="Recorded Sayings of Yunfeng Tizong Ning (雲峰體宗寧禪師語錄)"),
 "t_c9ba42aa7e47:0978b06:1:2": named("Yunfeng Tizong Ning", "in his Kaiyuan Monastery discourse, Yunfeng Tizong Ning defines agitation as fierce and difficult to suppress.", title="Recorded Sayings of Yunfeng Tizong Ning (雲峰體宗寧禪師語錄)"),
},
"T47n1996": {
 "t_757827b8d4cb:0672c12:1:4": named("Baofu Congzhan", "inside an old case raised by Xuedou Chongxian, Baofu Congzhan says that whether one catches it or not, one loses body and life.", ctx(("Xuedou Chongxian",("later-raiser","record-owner")),), title="Recorded Sayings of Mingjue (明覺禪師語錄)"),
 "t_b887bf089b98:0677c19:1:4": unnamed("unnamed lay questioner", "questioner", "an unnamed layman quotes the line about not leaving the very place and asks Xuedou Chongxian where it is.", ctx(("Xuedou Chongxian",("respondent","record-owner")),), title="Recorded Sayings of Mingjue (明覺禪師語錄)"),
},
"T47n1999": {"t_ffb0ee18f1a2:0962b04:1:3": named("Mi'an Xianjie", "in his Jiangshan monastery discourse, Mi'an Xianjie says 'break the solid checkpoint; heaven is broad and earth is broad.'", title="Recorded Sayings of Mi'an (密菴和尚語錄)")},
"T48n2012A": {"t_6edb551acb53:0382c20:1:7": named("Huangbo Xiyun", "in his Essentials of Mind Transmission, Huangbo Xiyun says undigested intellectual understanding is all poison.", title="Essentials of Mind Transmission of Huangbo Duanji (黃檗山斷際禪師傳心法要)")},
"X63n1257": {"t_412d9358cc70:0758a13:1:1": named("Wuyi Yuanlai", "in his admonitions to beginners, Wuyi Yuanlai asks what close-woven means and defines it as permitting no hairbreadth gap.", title="Boshan Chan Admonitions (博山禪警語)")},
"X69n1345": {"t_7ba176a992ec:0236a02:1:4": named("Chaozong Huifang", "in his Heshan record, Chaozong Huifang raises the whisk and says both naming and not naming it obstruct what meets the eye.", title="Recorded Sayings of Chaozong Huifang (超宗慧方禪師語錄（黃龍四家錄第四）)")},
"X70n1390": {"t_31575552ede2:0442b19:1:4": named("Xisou Shaotan", "in his small address, Xisou Shaotan describes dull students guarding stubborn emptiness as a wooden puppet and wall without openings.", title="Extensive Record of Xisou Shaotan (希叟紹曇禪師廣錄)")},
"X71n1412": {
 "t_372fb5a2b7ce:0209a22:1:4": named("Gulin Qingmao", "in his Tianping Mountain discourse, Gulin Qingmao says the dead phrase is the living phrase and the living phrase the dead phrase, then describes the living phrase's freedom.", title="Recorded Sayings of Gulin Qingmao (古林清茂禪師語錄)"),
 "t_5d84cccab8df:0242c01:1:2": named("Gulin Qingmao", "in his instruction to attendant Jian, Gulin Qingmao defines Mount Sumeru as the move before beings and buddhas were established.", title="Recorded Sayings of Gulin Qingmao (古林清茂禪師語錄)"),
},
"X73n1457": {"t_372fb5a2b7ce:0864c05:1:6": named("Mailang Minghuai", "in his Questions Set Up for the Chan House, Mailang Minghuai directly defines a living phrase through eight observable constraints.", title="Mailang Minghuai's Questions Set Up for the Chan House (雲門麥浪懷禪師宗門設難)")},
"X80n1568": {"t_dfd1dbffe9f2:0635b07:1:4": named("Funiu Zizai", "inside Funiu Zizai's headed section, Funiu calls 'this mind is this buddha' a phrase seeking medicine without illness.", title="Strict Lineage of the Five Lamps, volumes 1–9 (五燈嚴統(第1卷-第9卷))")},
"X83n1574": {"t_4cf045deab37:0351c20:1:3": named("Huaiyu Xuan Shouzuo", "when invited to lead Nanyuan, Huaiyu Xuan Shouzuo says he has long been a gruel-and-rice monk and vows not to emerge into the world.", title="Expanded Continuation of the Lamp Record (增集續傳燈錄)")},
"X84n1585": {"t_9a7a00ea0cd1:0706c09:1:6": named("Zhuquan Liaohuan Falin", "in his headed section, Zhuquan Liaohuan Falin says that if asked where the one returns he would answer that today is as hot as yesterday.", title="Draft Continuation of the Lamp (續燈存稿)")},
"J34nB309": {"t_ba8066477571:0553b09:1:8": named("Yunfu Zhi", "at his Wudeng Monastery opening, Yunfu Zhi shouts once and immediately ascends the seat for the incense offering.", title="Recorded Sayings of Yunfu Zhi (雲腹智禪師語錄)")},
"J34nB312": {"t_dd2b39789323:0809a18:1:3": named("Juelang Dasheng", "in his Jiahe record, Juelang Dasheng answers the question about a phrase outside the standard with a paired verse line.", title="Jiahe Recorded Sayings of Juelang Dasheng (天界覺浪盛禪師嘉禾語錄)")},
"J36nB363": {"t_1f6124388d25:0782a07:1:7": named("Fachang Yu", "inside a case raised by Wenmu Nian, Fachang Yu is the explicitly named earlier speaker who says even sealed-mouth sitters are exposed by the lantern.", ctx(("Wenmu Nian",("later-raiser","record-owner")),), title="Recorded Sayings of Wenmu Nian (文穆念禪師語錄)")},
"J37nB383": {"t_b0f2ccf6d140:0205c14:1:5": named("Hanxiu Ruqian", "in his release-day hall address, Hanxiu Ruqian describes the retreat restriction from the fifteenth of the tenth month through the Lantern Festival.", title="Recorded Sayings of Hanxiu (憨休禪師語錄)")},
"J38nB421": {"t_cb3571346f22:0558b07:1:10": named("Hefeng", "in his ascent-of-seat address, Hefeng says Shakyamuni being Shakyamuni is no increase and wood and stone being themselves no decrease.", title="Recorded Sayings of Hefeng (鶴峰禪師語錄)")},
"J38nB427": {"t_84043ffcdf90:0866c01:1:2": named("Zhusheng", "in his hall address, Zhusheng calls for a patch-robed monk who possesses the eye to come forward and speak.", title="Recorded Sayings of Zhusheng of Qingcheng (青城竹浪生禪師語錄)")},
"J38nB433": {"t_31575552ede2:1001b28:1:5": named("Chunbei De", "in his instruction to Chan student Mianjiu, Chunbei De warns not to dwell in stubborn emptiness or hide in dead water.", title="Recorded Sayings of Chunbei De (純備德禪師語錄)")},
"J39nB453": {"t_2dd4fec35455:0580c12:1:2": named("Yuanjie Ying", "in his general address, Yuanjie Ying rejects treating an emptied mind like drenched dead ashes as ultimate.", title="Recorded Sayings of Yuanjie Ying (元潔瑩禪師語錄)")},
"T48n2019B": {"t_f2482d04a86a:1004b06:1:1": impersonal("Admonitions for Beginning Students textual voice", "the Admonitions for Beginning Students textual voice tells beginners to know keeping, breaking, permission, and prohibition after receiving precepts.", "The treatise presents authorial instruction without a named speech turn.", title="Admonitions for Beginning Students (誡初心學人文)")},
"X63n1220": {"t_37261001c332:0008c17:1:3": impersonal("Bodhidharma Treatise's question-and-answer voice", "the Bodhidharma Treatise's question-and-answer voice asks what understanding through inspecting mind means and supplies the pure-and-defiled distinction.", "The witness is an unattributed treatise dialogue rather than a biographical speech turn.", ctx(("Bodhidharma",("attributed-treatise-figure",)),), title="Bodhidharma's Treatise on Breaking Through Appearances (達磨大師破相論)")},
"X69n1321": {"t_ad2c9d24126f:0002c10:1:2": named("Mazu Daoyi", "in his extensive record, Mazu Daoyi says choosing good and rejecting evil, observing emptiness and entering concentration, are contrivance.", title="Extensive Record of Mazu Daoyi (馬祖道一禪師廣錄（四家語錄卷一）)")},
"X69n1323": {"t_b887bf089b98:0008a02:1:2": named("Baizhang Huaihai", "in his extensive record, Baizhang Huaihai enumerates liberation, quiescence, and the teaching place right there.", title="Extensive Record of Baizhang Huaihai (百丈懷海禪師廣錄（四家語錄卷三）)")},
"X69n1335": {"t_19784084ccb4:0130a17:1:2": named("Baozhi", "after Fu Dashi leaves the lecture seat, Baozhi asks Emperor Wu whether he understands and declares the great man's scripture lecture finished.", ctx(("Fu Dashi",("silent-lecture-actor",)),("Emperor Wu of Liang",("interlocutor",))), title="Recorded Sayings of Shanhui Dashi (善慧大士語錄)")},
"X69n1347": {"t_b887bf089b98:0262a22:1:5": named("Changling Shouzhuo", "in his Zifu Monastery discourse, Changling Shouzhuo says that one who recognizes buddhas and patriarchs immediately transcends right there.", title="Recorded Sayings of Changling Shouzhuo (長靈守卓禪師語錄)")},
"X69n1368": {"t_372fb5a2b7ce:0744a02:1:2": named("Yanxi Guangwen", "in his general address, Yanxi Guangwen defines speech containing speech as a dead phrase and speech without speech as a living phrase.", title="Recorded Sayings of Yanxi Guangwen (偃溪廣聞禪師語錄)")},
"X70n1388": {"t_93360aaedb7c:0395c11:1:3": impersonal("biographical compiler's narrative", "the biographical compiler's narrative says applicants waited months to enter the hall while Huanxi Weiyi was admitted by Fojian Huiqin that day.", "The passage is third-person biography rather than a speech turn.", ctx(("Huanxi Weiyi",("biographical-subject",)),("Fojian Huiqin",("admitting-master",))), title="Recorded Sayings of Huanxi Weiyi (環溪惟一禪師語錄)")},
"X70n1392": {"t_bb19ed0e0fab:0524a09:1:7": named("Yuejian Shanquan", "in his announced general address, Yuejian Shanquan says he will give the assembly one general address and they should regard it as rare.", title="Recorded Sayings of Yuejian (月磵禪師語錄)")},
"X71n1405": {"t_2069b9c33315:0047b15:1:6": named("Shixi Xinyue", "in his Bao'en Monastery small address, Shixi Xinyue contrasts the three mysteries and essentials with the five lord-minister positions.", title="Recorded Sayings of Shixi Xinyue (石溪心月禪師語錄)")},
"X85n1591": {"t_8253f56255ce:0230c08:1:4": named("Xiangya Ting", "in his Buddha-birthday hall address, Xiangya Ting says understanding means dust after dust and land after land, while not understanding earns a frontal kick.", title="Lamp Compendium of Qiannan (黔南會燈錄)")},
}


def apply_sheet(stem, decisions):
    path = BASE / f"decisions-{stem}.json"
    data = json.loads(path.read_text(encoding="utf-8"))
    used = set()
    for row in data["rows"]:
        if row["key"] in decisions:
            row["Override"] = decisions[row["key"]]
            used.add(row["key"])
    data["reviewedAllCases"] = all(row.get("Override") is not None for row in data["rows"])
    data["reviewer"] = REVIEWER
    data["reviewedUtc"] = UTC
    data["candidateMissingKeys"] = [row["key"] for row in data["rows"] if row.get("Override") is None]
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    missing = set(decisions) - used
    if missing:
        raise SystemExit(f"keys missing from {stem}: {sorted(missing)}")
    print(json.dumps({"source": stem, "mapped": len(used), "sourceRows": len(data["rows"]), "sourceComplete": data["reviewedAllCases"]}, indent=2))


apply_sheet("X82n1571", DECISIONS)
apply_sheet("X80n1565", X80_DECISIONS)
apply_sheet("X64n1260", X64_DECISIONS)
apply_sheet("T51n2077", T51N2077_DECISIONS)
apply_sheet("T51n2076", T51N2076_DECISIONS)
apply_sheet("X68n1319", X68N1319_DECISIONS)
apply_sheet("X78n1556", X78N1556_DECISIONS)
apply_sheet("X68n1318", X68N1318_DECISIONS)
apply_sheet("X79n1557", X79N1557_DECISIONS)
apply_sheet("X84n1583", X84N1583_DECISIONS)
apply_sheet("T47n1997", T47N1997_DECISIONS)
apply_sheet("J33nB294", J33NB294_DECISIONS)
apply_sheet("T48n2016", T48N2016_DECISIONS)
apply_sheet("X79n1559", X79N1559_DECISIONS)
apply_sheet("X66n1296", X66N1296_DECISIONS)
apply_sheet("J34nB311", J34NB311_DECISIONS)
apply_sheet("X81n1568", X81N1568_DECISIONS)
apply_sheet("J36nB359", J36NB359_DECISIONS)
apply_sheet("J39nB471", J39NB471_DECISIONS)
apply_sheet("X72n1444", X72N1444_DECISIONS)
apply_sheet("T48n2006", T48N2006_DECISIONS)
apply_sheet("T48n2025", T48N2025_DECISIONS)
apply_sheet("J34nB299", J34NB299_DECISIONS)
apply_sheet("T48n2003", T48N2003_DECISIONS)
apply_sheet("J36nB369", J36NB369_DECISIONS)
apply_sheet("J37nB386", J37NB386_DECISIONS)
apply_sheet("J39nB466", J39NB466_DECISIONS)
for _stem, _decisions in TAIL_DECISIONS.items():
    apply_sheet(_stem, _decisions)
