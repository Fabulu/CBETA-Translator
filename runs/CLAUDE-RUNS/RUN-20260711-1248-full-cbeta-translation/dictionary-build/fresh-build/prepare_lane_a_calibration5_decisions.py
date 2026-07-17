#!/usr/bin/env python3
"""Build the explicit five-entry Lane-A calibration decision packet.

All semantic and actor decisions below were made from the full-case and batched
research packets.  Lookup by relPath only retrieves the already-read exact
window; it does not decide actors, senses, prose, or source independence.
"""

from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path

import sys

HERE = Path(__file__).resolve().parent
DB = HERE.parent
sys.path.insert(0, str(DB))
import zc  # noqa: E402

BASE = "42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a"
NOW = datetime.now(timezone.utc).isoformat()
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]
RESEARCH = json.loads((DB / "maintenance/investigation-next300-construction-research-a.json").read_text(encoding="utf-8"))
LABEL_MANIFEST = json.loads((DB / "maintenance/quality-debt-source-label-manifest.json").read_text(encoding="utf-8"))
REGISTRY = Path(LABEL_MANIFEST["authoritativeRegistry"])
LABELS = {}
for line in REGISTRY.read_text(encoding="utf-8").splitlines():
    if line.strip():
        row = json.loads(line)
        LABELS[row["path"]] = f'{row["en"]} ({row["zh"]})'

RESEARCH_BY_TERM = {row["term"]: row for row in RESEARCH["entries"]}


def named(rel, kwic, master, sentence, grammar, roles=None, contexts=None):
    return {
        "rel": rel, "kwic": kwic, "master": master,
        "roles": roles or ["utterer"], "contexts": contexts or [],
        "actorSentence": sentence, "grammar": grammar,
    }


def other(rel, kwic, status, kind, label, role, sentence, grammar, contexts=None):
    return {
        "rel": rel, "kwic": kwic, "master": None,
        "status": status, "kind": kind, "label": label, "role": role,
        "contexts": contexts or [], "actorSentence": sentence, "grammar": grammar,
    }


SPECS = [
    {
        "id": "t_cec3f7124f0b", "term": "無生曲", "target": "the unborn song",
        "alternates": ["song of the unborn", "unborn tune"],
        "aliases": ["unborn song", "song of no birth", "what is the unborn song", "sing the unborn song", "unborn tune"],
        "alias_reason": "English readers may search the literal song or tune, the recurrent what-is-it question, or the corpus's repeated singing construction.",
        "opening": "The unborn song is a tune-image that Chan records make audible in death verses, hall addresses, lineage appraisals, and public questions.",
        "body": [
            "Tianyin Yuanxiu makes mountains, plants, beings, and nonbeings sing it together during a patron's longevity address; Xiyan Zonghui calls his final verse a tune now completed; and an anonymous question preserved in Jifei Ruyi's record receives the answer that the myriad sounds are silent.",
            "The image is not confined to death or silence. Meixi Fudu tells a departing layman not to sing that tune, Baichi Xingyuan assigns responsive singing of it to the Guiyang house, Linye Tongqi's questioner asks whether he may join the abbot's song, Wuyi Yuanlai urges a layman to listen, and Shanhui Huan asks how many listeners know the tune."
        ],
        "zenbend": "An ordinary song becomes a public Chan formula that can be sung by the whole landscape, completed in a death verse, assigned to a lineage house, refused to a departing patron, or posed as a question whose answer is silence.",
        "limit": "The witnesses do not establish one concealed doctrinal definition: the same tune is sung, withheld, completed, questioned, and answered with silence.",
        "different": ["a recurring Chan tune-image", "particular verses and questions that deploy that image"],
        "different_reason": "The verses and questions all retain one figurative song; death, celebration, refusal, and lineage appraisal are deployments, not different referents.",
        "modifier": [{"Control": "無生 modifies 曲", "Finding": "The compound names a song or tune characterized as unborn; it is not evidence that bare 曲 means no-birth."}],
        "family": [
            {"Term": "無生話", "Finding": "The unborn talk is a separate speech formula centered on Layman Pang's family verse."},
            {"Term": "唱和", "Finding": "Responsive singing describes one deployment of the song, not a second sense."},
            {"Term": "知音", "Finding": "The knowing-listener image accompanies the tune but remains its own lexical unit."}
        ],
        "occ": [
            named("J/J25/J25nB171.xml", None, "Tianyin Yuanxiu", "Tianyin Yuanxiu says that the whole landscape and every class of being sing the unborn song together.", "The marked 師拈拄杖云 opens Tianyin Yuanxiu's uninterrupted hall statement.", ["utterer", "record-owner"]),
            named("X/X82/X82n1571.xml", None, "Xiyan Zonghui", "Xiyan Zonghui recites the headword-bearing death verse immediately before dying.", "The biography names Xiyan Zonghui; 說偈曰 opens his verse and 言訖而逝 closes his utterance.", ["utterer", "verse-author", "section-subject"]),
            other("J/J38/J38nB425.xml", None, "reviewed-unnamed", "compiled question voice", "the unnamed question voice in the Ancient Worthies' Ten Noes", "questioner", "The unnamed question voice asks what the unborn song is; the adjacent anonymous answer says that the myriad sounds are silent.", "The headword occurs inside 如何是無生曲, while 答 opens a different anonymous answering turn.", [{"MasterName": "Jifei Ruyi", "Roles": ["record-owner", "compiler"]}]),
            named("J/J39/J39nB447.xml", None, "Meixi Fudu", "Meixi Fudu tells the departing layman not to sing the tune of the unborn.", "The headword is inside the marked invited hall address preserved in Meixi Fudu's own record.", ["utterer", "record-owner"]),
            named("J/J28/J28nB202.xml", None, "Baichi Xingyuan", "Baichi Xingyuan answers that the Guiyang house responsively sings the unborn song.", "師云 assigns the answer to Baichi Xingyuan; the following quoted question starts only afterward.", ["utterer", "respondent", "record-owner"]),
            other("J/J26/J26nB186.xml", None, "reviewed-unnamed", "monastic questioner", "the unnamed monastic questioner", "questioner", "An unnamed monastic asks whether he may join the abbot when the prefect requests the unborn song.", "進云 continues the unnamed monastic's question; 師云 opens Linye Tongqi's reply.", [{"MasterName": "Linye Tongqi", "Roles": ["respondent", "record-owner"]}]),
            named("J/J27/J27nB197.xml", None, "Wuyi Yuanlai", "Wuyi Yuanlai's presentation verse asks the layman to listen to the unborn song.", "The headword is inside Wuyi Yuanlai's titled 贈 verse, not a recipient's speech.", ["utterer", "verse-author", "record-owner"]),
            named("J/J29/J29nB223.xml", None, "Shanhui Huan", "Shanhui Huan says that he sings the unborn song himself and asks how many are knowing listeners.", "師云 opens Shanhui Huan's answer; 乃云 begins his continuation after it.", ["utterer", "respondent", "record-owner"]),
        ],
    },
    {
        "id": "t_0193acac38f3", "term": "回光返照", "target": "turn the light back and reflect",
        "alternates": ["turn the illumination back", "reflect the light back"],
        "aliases": ["turn the light back", "reflect the light back", "look back at your own understanding", "returning illumination", "quietistic counterfeit"],
        "alias_reason": "The probes cover both literal orders of the light-reflection image, Guishan's self-inspection setting, and Zhongfeng's explicit quietistic counterfeit.",
        "opening": "Turn the light back and reflect is a recurrent demand to examine one's own understanding, and the records also use the phrase to expose a counterfeit built from shutting out sights and sounds.",
        "body": [
            "Guishan Lingyou tells Yangshan Huiji to turn the light back because nobody else knows Yangshan's understanding, and asks him to present that understanding. Yuanwu Keqin rebukes an audience that will not turn back despite being stopped at every sense gate; Tianyin Yuanxiu tells an assembly not to seek outside; Dahui Zonggao addresses officials absorbed in public business.",
            "Zhongfeng Mingben supplies the sharpest limit: he says people who withdraw sight and hearing until they resemble wood and stone falsely call that turning the light back. Jifei Ruyi instead pairs it with tracing the source of the delusive body, while Yuanwu's Essentials tells a student who has been turned by a phrase to turn back and inspect."
        ],
        "zenbend": "The light image becomes an encounter instruction tied to presenting one's own understanding, and a diagnostic that rejects sensory withdrawal as a false version of the same phrase.",
        "limit": "The corpus does not license a quiet-sitting technique: Zhongfeng Mingben explicitly rejects still, wood-and-stone sensory withdrawal under this name.",
        "different": ["the instruction to turn illumination back", "criticisms of a false performance claimed under that instruction"],
        "different_reason": "Correction of a counterfeit does not create a second referent; both sides dispute what counts as carrying out the same instruction.",
        "modifier": [{"Control": "回 and 返 govern the light/illumination image", "Finding": "The compound's doubled reversal is retained in English rather than reduced to generic introspection."}],
        "family": [
            {"Term": "返觀", "Finding": "Looking back is related but does not carry this compound's explicit light image."},
            {"Term": "自看", "Finding": "Guishan's exchange pairs self-looking with this phrase without making the forms identical."},
            {"Term": "閉目藏睛", "Finding": "Closing the eyes supplies a corpus-attested hostile control, not a synonym."}
        ],
        "occ": [
            named("T/T47/T47n1997.xml", None, "Yuanwu Keqin", "Yuanwu Keqin says the audience is unwilling to turn the light back after describing obstruction at each sense gate.", "The headword lies in Yuanwu Keqin's marked uninterrupted small-address statement.", ["utterer", "record-owner"]),
            named("X/X70/X70n1402.xml", None, "Zhongfeng Mingben", "Zhongfeng Mingben names the four-character phrase and immediately tests what it would mean for the light to turn and the reflection to return.", "The titled instruction 示無地立禪人 is Zhongfeng Mingben's prose in his own miscellaneous record.", ["utterer", "record-owner"]),
            named("X/X79/X79n1557.xml", None, "Guishan Lingyou", "Guishan Lingyou tells Yangshan Huiji to turn the light back and present his actual understanding.", "溈山謂師云 explicitly names Guishan as utterer; 師云 then opens Yangshan's reply.", ["utterer", "teacher"], [{"MasterName": "Yangshan Huiji", "Roles": ["student", "addressee"]}]),
            named("J/J25/J25nB171.xml", None, "Tianyin Yuanxiu", "Tianyin Yuanxiu tells the assembly not to seek outside but to turn the light back to the original body.", "啟華嚴禪期示眾，師云 opens Tianyin Yuanxiu's verse and address.", ["utterer", "record-owner"]),
            named("X/X69/X69n1357.xml", None, "Yuanwu Keqin", "Yuanwu Keqin says one who is turned by the preceding phrase should turn the light back and reflect.", "The titled instruction 示泉上人 is Yuanwu Keqin's direct prose to the named recipient.", ["utterer", "record-owner"]),
            named("J/J38/J38nB425.xml", None, "Jifei Ruyi", "Jifei Ruyi tells the memorial spirits to use the body's image as a road home and directly turn the light back.", "The full small address is introduced by 師云 and remains Jifei Ruyi's speech through the headword-bearing clause.", ["utterer", "record-owner"]),
            named("M/M59/M59n1540.xml", None, "Dahui Zonggao", "Dahui Zonggao tells officials who do not know the footing of life and death to turn the light back amid their daily dealings.", "The marked general discourse is Dahui Zonggao's uninterrupted address and he self-identifies as Miaoxi within it.", ["utterer", "record-owner"]),
        ],
    },
    {
        "id": "t_602a5f095818", "term": "無生話", "target": "talk of the unborn",
        "alternates": ["unborn talk", "talk of no birth"],
        "aliases": ["unborn talk", "talk of no birth", "Layman Pang family verse", "what is the unborn talk", "family talks of the unborn"],
        "alias_reason": "The probes join the literal talk phrase to Layman Pang's family verse and to the repeated public question asking what that talk is.",
        "opening": "Talk of the unborn is the named speech-object in Layman Pang's family verse, which later masters repeatedly quote, question, counter, and demand from their own assemblies.",
        "body": [
            "Pang Yun's verse places an unmarried son and daughter together with the household, all talking of the unborn. Xueguan Zhiyin juxtaposes it with Yang Jie's counterverse about marrying; Lianfeng Su says his own patrons still have not spoken it; and Zhongfeng Mingben asks what the phrase names before setting it beside Zhaozhou's exchange with a nun.",
            "The formula remains live interview material rather than an inherited slogan. Miyun Yuanwu asks his assembly what the talk is, Konggu Daocheng offers a birthday saying under its name, an unnamed monk asks Guxue Zhenzhe for a line through it, and another asks Chaozong Tongren directly what it is."
        ],
        "zenbend": "A household verse becomes a repeatedly reopened public question: later masters preserve its family scene but counter it with marriage, ask what the talk is, or demand that contemporary patrons actually speak it.",
        "limit": "The records preserve contrary verses and fresh questions; they do not settle the headword with one abstract formula imported from outside those exchanges.",
        "different": ["Layman Pang's named talk in the family verse", "later questions and counterverses about that same talk"],
        "different_reason": "Later questions and counterverses explicitly point back to Pang's lexical object, so they contest one referent rather than introduce another.",
        "modifier": [{"Control": "無生 modifies 話", "Finding": "The whole compound names the talk under discussion; bare 話 remains a broader word for talk or saying."}],
        "family": [
            {"Term": "無生曲", "Finding": "The unborn song is a separate tune-image with its own death-verse and hall deployments."},
            {"Term": "龐居士", "Finding": "Layman Pang is the verse's attributed speaker and case figure, not a second sense of the phrase."},
            {"Term": "團圞頭", "Finding": "The gathered family scene is a fixed collocation in the verse, not the definition by itself."}
        ],
        "occ": [
            named("J/J10/J10nA158.xml", "且無生話又作麼生？", "Miyun Yuanwu", "Miyun Yuanwu asks his assembly what the talk of the unborn is after quoting Layman Pang's verse.", "且 marks Miyun Yuanwu's own follow-up after the closed quoted verse; the question remains inside his hall address.", ["utterer", "later-raiser", "record-owner"], [{"MasterName": "Pang Yun", "Roles": ["case-figure"]}]),
            named("X/X83/X83n1578.xml", "有偈曰：有男不婚，有女不嫁。大家團欒頭，共說無生話。", "Pang Yun", "The Record Pointing at the Moon attributes the complete headword-bearing verse to Pang Yun.", "The biography names Pang Yun as subject and 有偈曰 introduces his verse.", ["utterer", "verse-author", "case-figure"]),
            named("B/B25/B25n0145.xml", "且喚甚麼作無生話。", "Zhongfeng Mingben", "Zhongfeng Mingben asks what is called the talk of the unborn before raising Zhaozhou's exchange with a nun.", "且喚甚麼作 follows the closed quotation and belongs to Zhongfeng Mingben's uninterrupted instruction.", ["utterer", "later-raiser", "record-owner"]),
            named("J/J27/J27nB198.xml", "龐居士云：『有男不婚，有女不嫁，大家團圞頭，共說無生話。』", "Pang Yun", "Xueguan Zhiyin explicitly quotes Pang Yun's family verse before setting it against Yang Jie's counterverse.", "龐居士云 identifies Pang Yun as the quoted utterer; Xueguan's comparison begins after the quotation.", ["utterer", "verse-author", "case-figure"], [{"MasterName": "Yingshan Zhiyin", "Roles": ["later-quoter", "record-owner"]}]),
            named("J/J38/J38nB410.xml", "今諸居士盡有男可婚、有女可嫁，只是無生話未曾說著，", "Lianfeng Su", "Lianfeng Su tells the assembled laypeople that, although they have sons and daughters to marry, the unborn talk has not yet been spoken.", "The sentence is Lianfeng Su's continuation after quoting the two older verses in his own hall address.", ["utterer", "commentator", "record-owner"]),
            named("J/J39/J39nB471.xml", None, "Konggu Daocheng", "Konggu Daocheng says a birthday celebration can offer only a saying of the unborn and then asks what no-birth is.", "The headword-bearing sentence stands inside Konggu Daocheng's marked hall address before his 良久 pause.", ["utterer", "record-owner"]),
            other("J/J28/J28nB208.xml", None, "reviewed-unnamed", "monastic questioner", "the unnamed monastic questioner", "questioner", "An unnamed monk invokes Pang Yun's talk and asks Guxue Zhenzhe to provide a connecting line.", "僧問 opens the headword-bearing verse-question; 師云 opens Guxue Zhenzhe's answer.", [{"MasterName": "Guxue Zhenzhe", "Roles": ["respondent", "record-owner"]}, {"MasterName": "Pang Yun", "Roles": ["case-figure"]}]),
            other("J/J34/J34nB300.xml", "如何是無生話？", "reviewed-unnamed", "monastic questioner", "the unnamed monastic questioner", "questioner", "An unnamed monk asks Chaozong Tongren directly what the talk of the unborn is.", "The question follows 僧問; 師云 opens Chaozong Tongren's answer.", [{"MasterName": "Chaozong Tongren", "Roles": ["respondent", "record-owner"]}]),
        ],
    },
    {
        "id": "t_9c6aff6f14ae", "term": "拈花微笑", "target": "hold up a flower and smile",
        "alternates": ["the flower-and-smile case", "holding up the flower, smiling"],
        "aliases": ["flower sermon", "hold up a flower and smile", "flower and smile case", "Buddha holds up flower", "Kasyapa smiles"],
        "alias_reason": "Road, path, and way are irrelevant here; these aliases instead expose the familiar flower-sermon lookup, the literal two actions, and both named case figures.",
        "opening": "Hold up a flower and smile is the compact case-name for the flower assembly, and later records keep it public by versifying, questioning, crediting, and openly criticizing it.",
        "body": [
            "Wuyi Yuanlai makes the phrase a verse heading. An unnamed monk asks Konggu Daocheng what the matter amounts to, and another asks Feiyin Tongrong who receives the bag and bowl after this single precedent. Baiyun Shouduan's verse questions whether the flower and smile can simply be accepted as the house style.",
            "The tradition also refuses a uniformly reverent reading. Juelang Daosheng connects the case with entrusting the treasury of the true teaching-eye to Kasyapa, while Puxian Yuansu says it still misses the measureless mechanism and Liang Dianhua's preface calls it the Buddha's first leak."
        ],
        "zenbend": "The inherited Buddha-and-Kasyapa scene functions as a compact public case-name that can title a verse, support a succession question, or become the object of direct criticism by later masters.",
        "limit": "The sources disagree in appraisal: some connect it with entrusting the teaching-eye, while others call it a leak, say it misses the mechanism, or challenge its acceptance as the house style.",
        "different": ["the flower-and-smile action formula", "the named public case indexed by that formula"],
        "different_reason": "The formula and case-name refer to the same recorded scene; title use does not introduce a different thing.",
        "modifier": [{"Control": "拈花 and 微笑 form a coordinated case formula", "Finding": "Neither graph group alone carries the whole Buddha-and-Kasyapa case reference."}],
        "family": [
            {"Term": "拈花示眾", "Finding": "Holding up the flower to show the assembly names the Buddha's action more explicitly."},
            {"Term": "迦葉微笑", "Finding": "Kasyapa's smile is the responding action within the same scene."},
            {"Term": "正法眼藏", "Finding": "The teaching-eye treasury is linked to succession in some versions but is a separate lexical object."}
        ],
        "occ": [
            named("X/X72/X72n1435.xml", "拈花微笑瑞瓣靈枝劫外春，拈來攪動海山雲，婆心況是如天遠，誰是拖泥帶水人？", "Wuyi Yuanlai", "Wuyi Yuanlai titles and verses the flower-and-smile case.", "The section is Wuyi Yuanlai's 頌古; the headword is the verse heading immediately followed by his verse.", ["utterer", "verse-author", "record-owner"]),
            other("J/J39/J39nB471.xml", None, "reviewed-unnamed", "monastic questioner", "the unnamed monastic questioner", "questioner", "An unnamed monastic asks Konggu Daocheng what the flower-and-smile matter is; Konggu answers that it is not believable.", "The headword occurs before 師云 in the monastic's question; 師云 begins Konggu Daocheng's answer.", [{"MasterName": "Konggu Daocheng", "Roles": ["respondent", "record-owner"]}]),
            named("X/X68/X68n1318.xml", None, "Baiyun Shouduan", "Baiyun Shouduan's verse says everyone calls the flower-and-smile right, then asks what they take as the house style.", "The headword stands in a verse under the Baiyun Shouduan sayings section and the 頌古 heading.", ["utterer", "verse-author", "record-owner"]),
            named("J/J34/J34nB311.xml", None, "Juelang Daosheng", "Juelang Daosheng links the flower-and-smile scene with entrusting the treasury of the true teaching-eye to Kasyapa.", "師曰 opens Juelang Daosheng's extended answer to Jiao Hong and continues through the headword.", ["utterer", "record-owner"], [{"MasterName": "Mahakasyapa", "Roles": ["case-figure"]}]),
            other("J/J38/J38nB406.xml", None, "identified-non-master", "named preface writer", "Liang Dianhua", "compiler", "Liang Dianhua's signed preface calls the flower-and-smile scene the Buddha's first leak.", "The headword is inside the preface signed 今轉梁殿華; it is not speech by Tianran Hanshi.", [{"MasterName": "Tianran Hanshi", "Roles": ["person-discussed", "record-owner"]}]),
            named("X/X79/X79n1557.xml", None, "Puxian Yuansu", "Puxian Yuansu says the flower and smile still miss the mechanism beyond measure.", "The entry heading names Puxian Yuansu and 示眾云 opens his uninterrupted statement.", ["utterer", "record-owner"]),
            other("J/J26/J26nB178.xml", None, "reviewed-unnamed", "monastic questioner", "the unnamed monastic questioner", "questioner", "An unnamed monk cites the flower-and-smile as a single precedent and asks Feiyin Tongrong who receives the bag and bowl.", "僧問 opens the headword-bearing question; 師云 begins Feiyin Tongrong's reply.", [{"MasterName": "Feiyin Tongrong", "Roles": ["respondent", "record-owner"]}]),
        ],
    },
    {
        "id": "t_bd7bf7138925", "term": "瓊樓玉殿", "target": "jeweled towers and jade halls",
        "alternates": ["jade halls and jeweled towers", "magnificent palace halls"],
        "aliases": ["jeweled towers", "jade halls", "palaces on a blade of grass", "grass covers the palace", "magnificent palace halls"],
        "alias_reason": "The aliases provide both palace nouns and the two grass-blade relations—appearing on it and being covered by it—that define the corpus deployment.",
        "opening": "Jeweled towers and jade halls are the palace-image in Baiyun Shouduan's blade-of-grass saying: the palace appears on one grass blade, yet the same blade can cover it.",
        "body": [
            "Baiyun Shouduan frames both directions around whether one has truly sweated once. Qianyan Yuanzhang asks how a doubtful listener could make a grass blade into a great body, establish a monastery, or show the palace on it; Feiyin Tongrong recasts the same reversal as making a palace from grass.",
            "Later masters keep testing the proportion. Dahui Zonggao accepts the palace appearing on grass but warns against being deceived by its being covered; Yuejian makes the palace cheap as grass in one direction and grass costly as the palace in the other; Chushi Fanqi removes the grass and asks where the palace is; Lianfeng Su asks how grass and palace become one piece."
        ],
        "zenbend": "The luxurious palace is not an architectural report but the movable term in a public Chan comparison: it appears on, is covered by, is exchanged with, or is made indistinguishable from one grass blade.",
        "limit": "The corpus supports the observable grass-and-palace reversal but does not identify the palace with one imported symbolic meaning.",
        "different": ["literal jeweled palace imagery", "the palace term inside Baiyun's grass-blade formula"],
        "different_reason": "The selected corpus uses all refer to the same imagined palace within comparison and reversal; no witness points to an actual building bearing the headword as its proper name.",
        "modifier": [{"Control": "瓊 and 玉 are value/appearance modifiers", "Finding": "They make the towers and halls splendid or jewel-like; the records do not assert buildings materially constructed from gemstones."}],
        "family": [
            {"Term": "一莖草", "Finding": "The single grass blade is the formula's contrasting term and remains separately searchable."},
            {"Term": "玉殿", "Finding": "Jade hall is a shorter palace image with additional independent deployments."},
            {"Term": "金殿", "Finding": "Golden hall is a related material/value image, not evidence that this whole compound denotes gold."}
        ],
        "occ": [
            named("X/X66/X66n1296.xml", "若端的得一回汗出，便向一莖草上現瓊樓玉殿；", "Baiyun Shouduan", "Baiyun Shouduan says that after truly sweating once, jeweled towers and jade halls appear on one grass blade.", "The section explicitly names Baiyun Shouduan and 上堂 opens his quoted address.", ["utterer", "case-figure"]),
            named("J/J32/J32nB273.xml", "如何能拈一莖草作丈六金身，插一莖草而建梵剎，向一莖草上現瓊樓玉殿耶？", "Qianyan Yuanzhang", "Qianyan Yuanzhang asks how someone who does not fully trust could show jeweled towers and jade halls on one grass blade.", "The rhetorical question belongs to Qianyan Yuanzhang's uninterrupted instruction before he quotes Baiyun Shouduan.", ["utterer", "later-raiser", "record-owner"]),
            named("J/J26/J26nB178.xml", None, "Feiyin Tongrong", "Feiyin Tongrong says to take one grass blade as jeweled towers and jade halls during a New Year's hall address.", "元旦上堂 and 云 open Feiyin Tongrong's address; the headword remains inside that turn.", ["utterer", "record-owner"]),
            named("J/J38/J38nB410.xml", "今城外草木漫空，城中樓臺插漢，喚作一莖草又是瓊樓玉殿，", "Lianfeng Su", "Lianfeng Su places the city grass and towers together and says that calling it a blade of grass also makes it jeweled towers and jade halls.", "乃云 opens Lianfeng Su's continuation after the monk's question and his own answer.", ["utterer", "commentator", "record-owner"]),
            named("T/T47/T47n1998A.xml", "一莖草上現瓊樓玉殿。決定可信。", "Dahui Zonggao", "Dahui Zonggao says the jeweled towers and jade halls appearing on one grass blade are certainly credible.", "師云 explicitly opens Dahui Zonggao's comment after the quotation of Baiyun Shouduan.", ["utterer", "commentator", "record-owner"]),
            named("X/X70/X70n1392.xml", "瓊樓玉殿賤似一莖草；", "Yuejian", "Yuejian says that after truly sweating once, jeweled towers and jade halls are cheap as a blade of grass.", "薦福道 introduces Yuejian's own monastery-name voice within his marked hall address.", ["utterer", "commentator", "record-owner"]),
            named("X/X71/X71n1420.xml", "師云：拈却一莖草，瓊樓玉殿在什麼處？", "Chushi Fanqi", "Chushi Fanqi removes the single grass blade and asks where the jeweled towers and jade halls are.", "師云 explicitly assigns the headword-bearing comment to Chushi Fanqi after his quotation of Baiyun Shouduan.", ["utterer", "commentator", "record-owner"]),
        ],
    },
]


def source_for(term, rel):
    matches = [x for x in RESEARCH_BY_TERM[term]["sources"] if x["RelPath"] == rel]
    if len(matches) != 1:
        raise ValueError(f"{term}: expected one research source for {rel}, got {len(matches)}")
    return matches[0]


def materialize_occ(term, spec):
    source = source_for(term, spec["rel"])
    kwic = spec["kwic"] or source["windows"][0]["window"]
    verified = zc.verify(spec["rel"], kwic)
    if not verified.get("ok") or kwic.count(term) != 1:
        raise ValueError((term, spec["rel"], verified, kwic.count(term)))
    if spec["rel"] not in LABELS:
        raise ValueError(f"missing authoritative label for {spec['rel']}")
    exact_clause = spec["kwic"]
    if exact_clause is None:
        # Mechanical recut only: the semantic and utterer decisions above were
        # made from the full case.  Keep the smallest punctuation-bounded span
        # in the verified witness that contains the one exact headword.
        at = kwic.index(term)
        left = max([kwic.rfind(mark, 0, at) for mark in "。！？；"] + [-1]) + 1
        right_candidates = [kwic.find(mark, at + len(term)) for mark in "。！？；"]
        right_candidates = [value for value in right_candidates if value >= 0]
        right = min(right_candidates) + 1 if right_candidates else len(kwic)
        exact_clause = kwic[left:right].strip()
    if exact_clause.count(term) != 1:
        raise ValueError(f"{term}: exact clause must contain one exact headword: {exact_clause}")
    occ = {
        "RelPath": spec["rel"], "FromLb": verified["fromLb"], "ToLb": verified["toLb"],
        "Kwic": kwic, "MasterName": spec["master"], "Curated": True,
        "ContextMasters": ([{"MasterName": spec["master"], "Roles": spec["roles"]}] if spec["master"] else []) + spec["contexts"],
        "AttributionNote": f"Source record ({spec['rel']}). {LABELS[spec['rel']]}: {spec['actorSentence']}",
        "DraftActorProof": {"GrammaticalSubject": spec["master"] or spec["label"], "FullCaseDecision": spec["actorSentence"], "SpeechFrame": spec["grammar"], "ExactHeadwordClause": exact_clause},
    }
    if not spec["master"]:
        occ["ActorAttribution"] = {
            "Status": spec["status"], "Kind": spec["kind"], "ActorLabel": spec["label"], "ActorRole": spec["role"],
            "GrammarEvidence": spec["grammar"], "ReviewedBy": "Codex Lane A calibration author", "ReviewedUtc": NOW,
        }
        if spec["status"] == "reviewed-unnamed":
            occ["ActorAttribution"]["RungsChecked"] = RUNGS
    return occ


def make_row(spec):
    research = RESEARCH_BY_TERM[spec["term"]]
    occurrences = [materialize_occ(spec["term"], x) for x in spec["occ"]]
    sources = list(dict.fromkeys(x["RelPath"] for x in occurrences))
    works = list(dict.fromkeys(zc.work_id(x) for x in sources))
    related_masters = []
    for occ in occurrences:
        if occ.get("MasterName"):
            related_masters.append(occ["MasterName"])
        related_masters.extend(x["MasterName"] for x in occ.get("ContextMasters") or [])
    related_masters = list(dict.fromkeys(related_masters))
    keys = [f"o{i}" for i in range(1, len(occurrences) + 1)]
    sense = {
        "SenseKey": None, "MasterName": None, "PreferredTarget": spec["target"],
        "AlternateTargets": spec["alternates"], "SearchAliases": spec["aliases"], "Status": "preferred",
        "Validation": "multi-source",
        "Note": f'{research["count"]["hits"]} exact hits in {research["count"]["works"]} independent works; {len(occurrences)} selected exact witnesses are stored.',
        "Occurrences": occurrences, "ClaimAnchors": [], "SourceTexts": sources,
        "RelatedMasters": related_masters, "RelatedTerms": [x["Term"] for x in spec["family"]],
        "ExplanationParts": {"CorpusEarnedOpening": spec["opening"], "EvidenceBody": spec["body"]},
        "DraftEvidence": {
            "OpeningClaimEvidenceKeys": keys, "ZenBend": spec["zenbend"], "CounterexampleOrLimit": spec["limit"],
            "DifferentThingTest": {"Decision": "one-thing", "ComparedThings": spec["different"], "Reason": spec["different_reason"]},
            "AliasRationale": spec["alias_reason"],
            "ModifierControls": spec["modifier"], "FamilyControls": spec["family"], "IndependentWorkIds": works,
        },
        "DraftAcceptedDerivedFields": {"SourceTexts": sources, "RelatedMasters": related_masters},
    }
    entry = {
        "Id": spec["id"], "SourceTerm": spec["term"], "CorpusBaselineSha256": BASE,
        "CreatedBy": "Codex Lane A evidence-first calibration", "WrittenUtc": NOW, "Senses": [sense],
    }
    work = f'''# {spec["term"]} — Lane A calibration worksheet

- queue: investigation-next300 construction Lane A
- corpus baseline: `{BASE}`
- exact concordance: {research["count"]["hits"]} hits / {research["count"]["files"]} files / {research["count"]["works"]} independent works
- stored evidence: {len(occurrences)} exact headword-bearing rows / {len(works)} independent works

feedback-inference-verdict: licensed — {spec["opening"]}
feedback-observations: {" ".join(spec["body"])}
feedback-falsification-searches: definition formulas; direct questions; answers; verses; case headings; hostile comments; family and modifier controls; duplicated parallel records.
feedback-counterexamples: {spec["limit"]}
feedback-scope: corpus-wide Chan deployment under the frozen 494-file / 487-work baseline.
lookup-probes: {"; ".join(spec["aliases"])}.
opening-interpretation-verdict: the opening states the smallest reproducible inference licensed by the stored full cases.

## Sense and family controls

- different-thing test: {spec["different_reason"]}
- sense-target-distinguishability: not applicable; one sense.
- modifier-relation-verdict: {spec["modifier"][0]["Finding"]}
- display-modifier-verdict: the preferred target keeps every meaning-bearing modifier visible.
- family-definition-retest: {" ".join(x["Term"] + ": " + x["Finding"] for x in spec["family"])}

## Actor review

Every stored row was read in its full transported case. MasterName is the exact headword utterer only; quoted figures, respondents, record owners, and compilers are separated in ContextMasters. Reviewed-unnamed rows record all six attribution rungs. No actor was inferred from source title alone.
'''
    return {"Entry": entry, "WorkMarkdown": work}


def main():
    out = DB / "maintenance/investigation-next300-lane-a-calibration5-explicit-decisions.json"
    payload = {"schemaVersion": "explicit-authoring-decisions.v1", "generatedUtc": NOW, "rows": [make_row(x) for x in SPECS]}
    out.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"output": str(out), "rows": len(payload["rows"])}))


if __name__ == "__main__":
    main()
