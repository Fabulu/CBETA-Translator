#!/usr/bin/env python3
import json
import sys
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
WB = ROOT / "maintenance" / "source-workbooks"
NOW = datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")
REVIEWER = "Codex /root/repair_bird_path"

def ctx(*pairs):
    return [{"MasterName": name, "Roles": list(roles)} for name, roles in pairs]

def named(name, note, contexts=()):
    out = {"MasterName": name, "AttributionNote": note}
    if contexts:
        out["ContextMasters"] = list(contexts)
    return out

def unnamed(note, contexts=(), kind="monk", role="questioner"):
    return {
        "MasterName": None,
        "ActorAttribution": {
            "Status": "reviewed-unnamed", "Kind": kind,
            "ActorLabel": f"unnamed {kind}", "ActorRole": role,
            "RungsChecked": ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"],
            "ReviewedBy": REVIEWER, "ReviewedUtc": NOW,
        },
        "ContextMasters": list(contexts),
        "AttributionNote": note,
    }

X80 = "The Compendium of the Five Lamps (五燈會元)"
X82 = "The Complete Book of the Five Lamps, volumes 34–120 (五燈全書(第34卷-第120卷))"
T51 = "The Jingde Record of the Transmission of the Lamp (景德傳燈錄)"

O = {
    # X80n1565
    "t_ba6668ef3e6e:0029a18:1:3": named("Mahakasyapa", f"{X80}, Shakyamuni Buddha section. Mahakasyapa is the exact speaker who strikes the sounding block and announces that the World-Honored One has finished speaking; Shakyamuni Buddha then leaves the seat.", ctx(("Shakyamuni Buddha", ("assembly-teacher", "actor-who-leaves-seat")))),
    "t_d2892b1eaae0:0042c21:1:1": named("Huike", f"{X80}, Bodhidharma biography. Huike, still called Shenguang in this part of the account, is the exact actor who stands unmoving in the snow until it reaches above his knees; Bodhidharma then questions him.", ctx(("Bodhidharma", ("respondent", "section-subject")))),
    "t_38695d7fdbe2:0064a16:1:3": unnamed(f"{X80}, Sikong Benjing's written question-and-answer sequence. An unnamed questioner asks what the Way is and how it is cultivated; Sikong Benjing supplies the answer. The six-rung ladder does not identify the questioner.", ctx(("Sikong Benjing", ("respondent", "section-subject"))), kind="questioner"),
    "t_33d49f4710be:0070b03:1:4": named("Mazu Daoyi", f"{X80}, Mazu Daoyi's address to his assembly. Mazu is the exact speaker who says that Bodhidharma transmitted the highest-vehicle one-mind teaching to open the listeners into awakening.", ctx(("Bodhidharma", ("named-subject", "transmitter")))),
    "t_38695d7fdbe2:0077b05:1:4": unnamed(f"{X80}, Panshan Baoji section. An unnamed monk asks, 'What is the Way?'; Panshan responds first with a rebuff and then with 'go.' The six-rung ladder and parallel forms do not name the monk.", ctx(("Panshan Baoji", ("respondent", "section-subject")))),
    "t_37771a869b4f:0080b16:1:6": named("Letan Fahui", f"{X80}. Letan Fahui is explicitly named as the exact questioner who asks Mazu Daoyi, 'What is the ancestral teacher's meaning in coming from the west?' Mazu tells him to lower his voice and come closer, then strikes him.", ctx(("Mazu Daoyi", ("respondent",)))),
    "t_8dc9df82b364:0087c03:1:1": named("Mazu Daoyi", f"{X80}, Layman Pang biography. Mazu Daoyi is the exact speaker of the headword-bearing answer, 'When you drink up all the West River's water in one mouthful, I will tell you.'", ctx(("Pang Yun", ("questioner", "biographical-subject")))),
    "t_19784084ccb4:0089b14:1:5": named("Baozhi", f"{X80}, Changqing Da'an section. Baozhi (Zhigong) is explicitly introduced as the quoted speaker of the contrast between finding nothing through inner and outer search and abundant activity on objects.", ctx(("Changqing Da'an", ("later-quoter", "section-subject")))),
    "t_93e6bc6b2103:0089b22:1:5": unnamed(f"{X80}, Changqing Da'an section. An unnamed monk is the exact actor who enters the hall, looks east and west for the absent master, and comments that the hall has no one in it. The six-rung ladder does not name him.", ctx(("Changqing Da'an", ("absent-master", "section-subject"))), role="actor-and-speaker"),
    "t_38695d7fdbe2:0093a04:1:5": unnamed(f"{X80}, Zhaozhou Congshen section. An unnamed monk asks 'What is the Way?' and then distinguishes the great Way from the road outside the wall; Zhaozhou answers. The six-rung ladder does not name the monk.", ctx(("Zhaozhou Congshen", ("respondent", "section-subject")))),
    "t_694f447dbd89:0113a23:1:2": named("Datong Ji", f"{X80}, Datong Ji section. Datong Ji is the exact actor who turns the Chan seat and faces the wall when Mihu arrives; Mihu stands behind him and later returns to the guest place.", ctx(("Mihu", ("visitor",)))),
    "t_bf67613e4573:0114c21:1:2": named("Daowu Yuanzhi", f"{X80}, Yunyan Tansheng section. Daowu Yuanzhi is the exact questioner who asks which of Great Compassion's thousand hands and eyes is the true eye; Yunyan answers with the nighttime pillow image.", ctx(("Yunyan Tansheng", ("respondent", "section-subject")))),
    "t_e5259ce8bbf5:0116b21:2:2": named("Touzi Datong", f"{X80}, Cuiwei Wuxue section. Touzi Datong is the exact speaker who asks the teacher to offer an indication; Cuiwei answers that he would then be asking for a second ladleful of foul water.", ctx(("Cuiwei Wuxue", ("respondent", "section-subject")))),
    "t_e84753568cda:0132a13:1:2": unnamed(f"{X80}, Zhaozhou Congshen section. An unnamed questioner asks whether this person knows there is an upward matter; Zhaozhou answers, 'does not know.' The six-rung ladder does not name the questioner.", ctx(("Zhaozhou Congshen", ("respondent", "section-subject"))), kind="questioner"),
    "t_77821881a767:0143b02:1:1": named("Puhua", f"{X80}, Puhua biography. Puhua is the exact speaker of the recorded address that calls Bodhidharma an old foreigner, old man Shakyamuni a dry shit-stick, and Manjusri and Samantabhadra shit-carrying fellows.", ctx(("Bodhidharma", ("named-target",)), ("Shakyamuni Buddha", ("named-target",)))),
    "t_757827b8d4cb:0146b08:1:3": named("Changqing Huileng", f"{X80}, Xuefeng Yicun section. After Xuefeng raises the turtle-nosed snake warning, Changqing Huileng is the exact speaker who says that many people in the hall will lose body and life.", ctx(("Xuefeng Yicun", ("case-raiser", "section-subject")))),
    "t_e9adcb470950:0204b23:1:1": named("Yongming Daoqian", f"{X80}, Yongming Daoqian biography. Daoqian is the exact grammatical subject whom King Zhongyi summons to the palace to receive the bodhisattva precepts and then appoints to the new Yongming monastery.", ctx(("King Zhongyi", ("patron", "summoning-actor")))),
    "t_bf67613e4573:0222c24:1:3": named("Magu Baoche", f"{X80}, Linji Yixuan section. Magu Baoche is the exact questioner who asks which of Great Compassion's thousand hands and eyes is the true eye; Linji grabs him and returns the question.", ctx(("Linji Yixuan", ("respondent", "section-subject")))),
    "t_acccac1051a4:0230c19:1:8": unnamed(f"{X80}, Fengxue Yanzhao section. An unnamed monk asks, 'What is the patch-robed monk's place of conduct?'; Fengxue answers with taking a staff-blow on the head and muttering. The six-rung ladder does not name the monk.", ctx(("Fengxue Yanzhao", ("respondent", "section-subject")))),
    "t_cc6b4c4e9ba8:0340a14:1:3": named("Liu Jingchen", f"{X80}, Liu Jingchen's Warning to Confucians on Clarifying the Way (明道諭儒篇). Liu Jingchen is the exact prose author who states that Shakyamuni held up the flower and transmitted the wondrous mind to Mahakasyapa, pairing it with Bodhidharma's wall-gazing and transmission to Huike.", ctx(("Shakyamuni Buddha", ("named-case-actor",)), ("Mahakasyapa", ("transmission-recipient",)), ("Bodhidharma", ("parallel-case-actor",)), ("Huike", ("parallel-transmission-recipient",)))),

    # X82n1571
    "t_21a3463bc0db:0019c21:1:3": named("Xiangji Yongmin", f"{X82}, Xiangji Yongmin section. Xiangji Yongmin is the exact speaker who says that a lineage teacher raises the guiding principle wherever he is and answers according to the occasion."),
    "t_93e6bc6b2103:0019c21:1:7": named("Xiangji Yongmin", f"{X82}, Xiangji Yongmin section. Xiangji Yongmin is the exact actor who looks over the assembly and then defines the responsive public work of a lineage teacher."),
    "t_f3488daf27fd:0023b24:1:6": named("Xuefeng Sihui", f"{X82}, Xuefeng Sihui section. Xuefeng Sihui is the exact hall speaker who compares Yaoshan's sparse appearances with contemporary incessant questioning and says the former was somewhat closer.", ctx(("Yaoshan Weiyan", ("earlier-case-subject",)))),
    "t_ff560195f161:0024c22:1:2": named("Wufeng Ziqi", f"{X82}, Wufeng Ziqi section. Wufeng Ziqi is the exact speaker who asks, 'How do you understand it?', judges the monk's shout 'not yet,' and presses what comes after one or two shouts."),
    "t_a14a883193a5:0036c22:1:2": named("Huangbo Weisheng", f"{X82}, Huangbo Weisheng section. Huangbo Weisheng is the exact speaker who pairs Linji's shout and Deshan's staff as models left for Chan people.", ctx(("Linji Yixuan", ("named-model",)), ("Deshan Xuanjian", ("named-model",)))),
    "t_bcc96a299271:0041b17:1:1": unnamed(f"{X82}, Guizong Zhizhi section. An unnamed monk asks for an indication of the ancestral meaning; Guizong Zhizhi answers, 'a mud ox swallows the great waves.' The six-rung ladder does not name the monk.", ctx(("Guizong Zhizhi", ("respondent", "section-subject")))),
    "t_a7c8b47ff1a3:0050c03:1:2": named("Dongshan Fanyan", f"{X82}, Dongshan Fanyan section. Dongshan Fanyan is the exact speaker who quotes Hanshan's autumn-moon verse and then judges that Hanshan labored without result.", ctx(("Hanshan", ("quoted-poet",)))),
    "t_643fab6ecc1b:0057b14:1:3": named("Baizhang Weigu", f"{X82}, Baizhang Weigu section. Baizhang Weigu is the exact speaker who pairs Bodhidharma's single transmission of the mind-seal with the Sixth Patriarch Huineng's not knowing one written character.", ctx(("Bodhidharma", ("named-lineage-figure",)), ("Huineng", ("named-lineage-figure",)))),
    "t_24adbdf51a15:0094c10:1:3": named("Zhaozhou Congshen", f"{X82}, Dahui Zonggao's hall address. Dahui explicitly attributes the headword-bearing four-buddha formula to Zhaozhou Congshen; Zhaozhou is retained as source speaker and Dahui as later raiser.", ctx(("Dahui Zonggao", ("later-raiser", "record-speaker")))),
    "t_2baf0ec63b2c:0110a09:1:3": named("Feng Ji", f"{X82}, Feng Ji biography. Feng Ji is the biographical subject and exact grammatical actor who walks through the teaching hall together with Foyan Qingyuan when a child recites the line that prompts his breakthrough.", ctx(("Foyan Qingyuan", ("walking-companion", "teacher")))),
    "t_fc585583b815:0187a09:1:5": named("Wumen Huikai", f"{X82}, Wumen Huikai biography. The narration says that Wumen Huikai and Yuelin Shiguan's responsive operation matched closely after their exchange of shouts; Wumen is the biographical subject and Yuelin the teacher in the two-person event.", ctx(("Yuelin Shiguan", ("teacher", "co-actor")))),
    "t_82829641b07c:0194b17:1:2": named("Gufeng Xiu", f"{X82}, Wanshan Zhengning biography. Gufeng Xiu is the exact speaker who tells Wanshan, 'You have got it, but you are still not there yet,' after Wanshan's verse on Zhaozhou.", ctx(("Wanshan Zhengning", ("student", "biographical-subject")), ("Zhaozhou Congshen", ("verse-subject",)))),
    "t_dc02eefd07f5:0279b20:1:6": named("Baizhang Ruibai Mingxue", f"{X82}, Baizhang Ruibai Mingxue section. Baizhang Ruibai Mingxue is the exact hall speaker who maps the Caodong ranks onto Linji-school terms and says that 'biased within straight' removes the object."),

    # T51n2076
    "t_96f468d6b843:0220a23:1:4": named("Bodhidharma", f"{T51}, Bodhidharma biography. The narration records Vinaya Master Guangtong and Bodhiruci as the two named clerics who repeatedly disputed with Bodhidharma after seeing him reject appearances and point to mind; Bodhidharma is the section subject and teaching actor.", ctx(("Vinaya Master Guangtong", ("named-disputant",)), ("Bodhiruci", ("named-disputant",)))),
    "t_33d49f4710be:0268a10:1:1": named("Guling Shenzan", f"{T51}, Guling Shenzan biography. Guling Shenzan is the exact grammatical subject who, while traveling, meets Baizhang Huaihai and opens into awakening before returning to his home temple.", ctx(("Baizhang Huaihai", ("teacher-met",)))),
    "t_707c9af5cb8e:0275a07:1:4": unnamed(f"{T51}, Nanquan-lineage exchange. An unnamed student is the exact speaker who infers, 'then sound does not enter dharma-realm nature'; the master corrects him. The six-rung ladder does not name the student.", ctx(("Nanquan Puyuan", ("lineage-context",))), kind="student", role="speaker"),
    "t_707c9af5cb8e:0276a01:1:1": unnamed(f"{T51}, Nanquan-lineage exchange. An unnamed student is the exact speaker who infers, 'then I have nowhere to lodge my body'; the master immediately reverses the claim. The six-rung ladder does not name the student.", ctx(("Nanquan Puyuan", ("lineage-context",))), kind="student", role="speaker"),
    "t_757827b8d4cb:0284b23:1:1": named("Xiangyan Zhixian", f"{T51}, Xiangyan Zhixian section. Xiangyan is the exact hall speaker who states the tree-case double bind: opening the mouth loses body and life, while not answering violates the question."),
    "t_fab146271b10:0291a28:1:7": named("Muzhou Daoming", f"{T51}, Muzhou Daoming section. Muzhou is the exact speaker who says that during his abbacy he has never seen a person with nothing going on arrive, calls listeners forward, and orders the advancing monk outside for twenty blows."),
    "t_91d84c849fc7:0299a17:1:3": unnamed(f"{T51}, Zizhou Shuilu Heshang section. An unnamed monk asks how to preserve this matter; Shuilu answers, 'strictly avoid it.' The six-rung ladder does not supply the monk's personal name.", ctx(("Zizhou Shuilu Heshang", ("respondent", "section-subject")))),
    "t_3a400cdd72bb:0337a26:1:2": unnamed(f"{T51}, Dongshan Daoquan section. An unnamed monk asks how the pure practitioner does not enter nirvana and the precept-breaking monk does not enter hell; Daoquan answers. The six-rung ladder does not name the monk.", ctx(("Dongshan Daoquan", ("respondent", "section-subject")))),
    "t_cf1445e57ef2:0365a17:1:5": unnamed(f"{T51}, Ca'an Fayi section. An unnamed monk asks how to advance in the Way when framing the mind misses and moving thought goes astray; Fayi answers and presses the follow-up. The six-rung ladder does not name the monk.", ctx(("Ca'an Fayi", ("respondent", "section-subject")))),
    "t_da6965508721:0400b21:1:5": unnamed(f"{T51}, Fayan Wenyi section. An unnamed monk asks what the ancients obtained that let them go to rest; Fayan answers by returning the question. The six-rung ladder does not name the monk.", ctx(("Fayan Wenyi", ("respondent", "section-subject")))),
}

selected = set(sys.argv[1:])
for path in sorted(WB.glob("overrides-owner2-*.json")):
    if selected and path.name not in selected:
        continue
    data = json.loads(path.read_text(encoding="utf-8"))
    for row in data["rows"]:
        if row["key"] in O:
            row["Override"] = O[row["key"]]
    data["reviewedAllCases"] = True
    data["reviewer"] = REVIEWER
    data["reviewedUtc"] = NOW
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(path.name, len(data["rows"]), sum(r["Override"] is not None for r in data["rows"]))
