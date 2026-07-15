#!/usr/bin/env python3
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
REPAIRS = {
    "t_0d6794766098": {
        "CorpusEarnedOpening": ('“what is it?” or “what is this?” It questions an object just indicated, a person, a place, or a phrase under discussion.')
    },
    "t_ee57d3ff5e43": {
        "EvidenceBody": ('The phrase can introduce a request—“with one phrase meeting the occasion, please speak the teaching” (一句當機請師說法)—or appraise an immediate response. Feiyin Tongrong strikes the staff and says, “freed through on the occasion; it is no other thing” (當機透脫，更非別物). Yuanwu\'s verse says that buddhas and patriarchs “value what is ready-made when meeting the occasion” (佛祖當機貴見成) and immediately calls for someone to come before the assembly and settle doubt. Thus the corpus bends a general word for apt timing toward the live occasion of encounter, question, answer, and response.')
    },
    "t_21a3463bc0db": {
        "CorpusEarnedOpening": ('“wherever” or “in every place.” Throughout the sampled corpus, its Chan value lies chiefly in the predicates whose scope it sets.')
    },
    "t_bf4ad761840f": {
        "CorpusEarnedOpening": ('A person\'s native ground is that person\'s own ground, most often deployed in the fixed phrase “native-ground scenery.”')
    },
    "t_133711ebf761": {
        "EvidenceBody": ('Chan records use the compound for workings disclosed in sayings, gestures, and exchanges. One verse calls them “subtle workings and marvelous function” and immediately lists raising the whisk, lifting the eyebrows, and answering questions. Another stock line says that “subtle workings beyond the norms” seek one who knows; a verse warns that people who try to grasp what precedes the working violate it beneath the phrase. The corpus can also negate it: “the intent cuts off subtle workings” stands beside “the myriad things do not arise.”')
    },
    "t_beab8961fb55": {
        "EvidenceBody": ('In Chan records it commonly names a teacher\'s reception of students and the answers given when they come before him. Asked, “how do you receive and lead?” (作麼生接引), Juelang Daosheng answers, “every house has a road open to carts and horses” (家家有路通車馬). Konggu Daocheng, asked “by which gate do you receive and lead later students?” (以那一門接引後學), answers with the recorded alternatives of staff and shout, expedient and actual, opposing and following. An unnamed questioner says, “I too hope to be received and led” (某甲亦望接引), and Konggu replies, “one cannot walk by taking another by the hand; only one\'s own assent is intimate” (把手牽人行不得，為人自肯乃方親). Qianshan Hanke asks whether the fundamental matter can be used to receive people, then sets “if there is receiving” against “without relying on receiving” (若有接引；不假接引). Thus the corpus uses an ordinary verb of reception as a category for named teachers\' dealings with arrivals, while also recording challenges to the category itself; it supplies no single fixed procedure under the word.')
    },
    "t_5f6e8c98ffe7": {
        "EvidenceBody": ('Its frequent Chan setting is the public interview, where one turn marks the recorded respondent’s prior expectation or recognition of the other. Ziman says, “as I knew, you are at a loss” (情知汝罔措), and cuts off the unnamed monk as he is about to continue. Pingtian Puan hears that a visitor has come from Huangbo and says, “as I knew, you had been to see an adept” (情知你見作家來). Jingfu’s case commentary anticipates the reader: “as I knew, you misconstrue it here” (情知你向者裏錯會). An unnamed monk also gives the compact reply “I knew it” (情知), after which Langting Ting says, “a fierce tiger does not eat meat lying down” (猛虎不食伏肉). These grammatical forms attest recognition or expectation; they do not name a separate faculty of “feeling-knowledge.”')
    },
    "t_782f20a368c3": {
        "CorpusEarnedOpening": ('To examine and awaken; in the Chan records, to come to understand.'),
        "EvidenceBody": ('An unnamed monk is told by Zhaozhou Congshen to wash his bowl and suddenly understands; Shigong Huizang cannot bring himself to shoot Mazu Daoyi and understands; the layman Li Linzong is asked by Yunfeng Yuanyi whether he understands and suddenly does; and Guannan Daowu hears a spirit-medium\'s song and suddenly understands. Foyan Qingyuan also says there must be an occasion for understanding and warns against stopping after grasping a few fixed phrases. The term records the narrator\'s or teacher\'s verdict that understanding occurred; the word itself adds no account of what was understood.')
    },
}

REPAIRS["t_beab8961fb55"]["EvidenceBody"] = REPAIRS["t_beab8961fb55"]["EvidenceBody"].replace(
    "a teacher's reception of students and the answers given when they come before him",
    "reception of students by named teachers and the answers given when students come before them",
)

for entry_id, fields in REPAIRS.items():
    path = ROOT / "fresh-build" / "entries" / entry_id / "evidence.draft.json"
    data = json.loads(path.read_text(encoding="utf-8"))
    parts = data["Entry"]["Senses"][0]["ExplanationParts"]
    for key, value in fields.items():
        parts[key] = [value] if key == "EvidenceBody" else value
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
