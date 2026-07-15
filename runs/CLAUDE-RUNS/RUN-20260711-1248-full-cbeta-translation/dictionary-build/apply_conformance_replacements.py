"""Apply reviewed, literal prose replacements for final-spec conformance."""

from pathlib import Path

BUILD = Path(__file__).resolve().parent

REPLACEMENTS = {
    "t_01a355a89ba1": [
        ("This is NOT \\\"the present moment\\\": the corpus attaches 當下", "The corpus attaches 當下"),
        (", marking the juncture at which the event occurs, not a here-and-now to dwell in", ", marking the juncture at which the event occurs rather than naming a state in which to dwell"),
    ],
    "t_121b66b78c9e": [
        ("set it as a huatou", "set it as a saying for investigation"),
        ("posed as a huatou", "posed as a saying for investigation"),
    ],
    "t_2738431562e6": [
        ("the word 'no' (無) — Zhaozhou's 'No' taken up as a huatou", "the word ‘no’ — Zhaozhou’s answer raised for investigation"),
        ("lifted out and used as a huatou", "taken up as the word to be investigated"),
        ("Dahui made it the engine of his 看話 ('watching the keyword') — what later tradition calls 看話禪 ('keyword-watching Chan')", "Dahui repeatedly tells readers to ‘look at a saying’ (看話) and later records use the label ‘Chan of looking at sayings’ (看話禪)"),
        ("many later huatou masters", "many later masters"),
        ("the 無 keyword", "Zhaozhou’s word ‘no’ (無)"),
        ("the word/character 無", "the word ‘no’ (無)"),
        ("single spoken 無", "single spoken ‘no’ (無)"),
        ("this one word 無", "this one word ‘no’ (無)"),
        ("the 無 case", "the case of ‘no’ (無)"),
        ("the 無 talk", "the saying of ‘no’ (無)"),
        ("the word 無", "the word ‘no’ (無)"),
        ("work it day and night", "keep bringing it up day and night"),
        ("birth-and-death", "life and death"),
        ("the practical instruction", "Dahui’s literal instruction"),
        ("contemplating the word ‘no’ (無)", "looking at the word ‘no’ (無)"),
    ],
    "t_326be1e9c98a": [
        ("a stock huatou across the record", "a stock saying raised across the record"),
    ],
    "t_36aa29eb1287": [
        ("'going/practising among the different kinds.'", "‘going among the different kinds.’"),
    ],
    "t_37771a869b4f": [
        ("a set of stock huatou", "a set of stock sayings"),
    ],
    "t_5d6035b1e800": [
        ("the Lotus Sutra's burning-house parable", "the Lotus Sutra’s burning-house passage"),
        ("the great white ox of the burning-house parable", "the great white ox in the burning-house passage"),
    ],
    "t_61c90d3a8edd": [
        ("cross-checked by two independent methods", "confirmed by two independent recounts"),
    ],
    "t_78f95517a347": [
        ("cross-checked by two independent methods", "confirmed by two independent recounts"),
        ("birth-and-death", "life and death"),
    ],
    "t_882860247a9b": [
        ("永嘉's (Yongjia's) meditation-manual formula", "the formula in Yongjia’s Śamatha Verse"),
        ("the meditation pair itself", "the still-and-alert pair itself"),
        ("the meditation ideal", "the prized still-and-alert formula"),
        ("its meditation-manual pedigree", "its pedigree in Yongjia’s Śamatha Verse"),
        ("— a meditation-manual register —", "in Yongjia’s Śamatha Verse"),
        ("Meditation-manual verse", "Verse from Yongjia’s Śamatha section"),
        ("the meditation-manual verse", "Yongjia’s Śamatha verse"),
        ("NOT rendered as 'meditative clarity' or the like.", "The target remains the attested ‘alert / awake,’ without importing a state-name from outside the record."),
    ],
    "t_8bd6933e6de3": [
        ("a stock huatou/verse", "a stock saying and verse"),
    ],
    "t_9a5dc768cbc5": [
        ("a widely-raised huatou for comment", "a widely raised saying for comment"),
    ],
    "t_ba841f6e11c8": [
        ("The paired huatou 雲門乾屎橛 · 洞山麻三斤", "The paired sayings ‘Yunmen’s dry shit-stick’ (雲門乾屎橛) and ‘Dongshan’s three catties of hemp’ (洞山麻三斤)"),
    ],
    "t_b291fe703ff1": [
        ("Render as 'investigate Chan' — the literal 'consult/look into' sense — not 'meditate,' which imports a later, generic notion the Chinese does not state; note", "Render it as ‘investigate Chan,’ following the literal ‘consult / look into’ sense; note"),
        ("看話參禪 ('investigating Chan by watching the keyword')", "看話參禪 (‘investigating Chan by looking at sayings’)"),
        ("the concrete work of Chan inquiry", "the recorded activity of investigating Chan"),
        ("birth-and-death", "life and death"),
        ("names its aim plainly", "states the comparison plainly"),
        ("marks the false version against the true", "describes people who say they investigate Chan while doing so only with their mouths"),
        ("wanting to understand the encounter-stories as conversation-fodder", "wanting to understand the recorded exchanges as material for conversation"),
        ("By the Song–Yuan the word is used interchangeably with 看話 ('watching the critical phrase / keyword'), which Dahui Zonggao systematized.", "Later records pair the expression with looking at sayings (看話)."),
    ],
    "t_c1af3ecba987": [
        ("meant to squelch him", "planned to suppress him"),
    ],
    "t_ccd48e1c9145": [
        ("cross-checked by two independent methods", "confirmed by two independent recounts"),
    ],
    "t_ce2a5ef71afe": [
        ("an established huatou", "an established saying under examination"),
        ("the established huatou", "the established saying"),
        ("three pounds of flax", "three catties of hemp"),
        ("hemp/flax", "hemp"),
    ],
    "t_d35dc9e3723e": [
        ("die and take rebirth elsewhere", "die and receive birth elsewhere"),
    ],
    "t_d4661c1b4dbb": [
        ("ruler-minister allegory", "ruler-minister scheme"),
        ("The ruler-minister allegory", "The ruler-minister scheme"),
    ],
    "t_dc02eefd07f5": [
        ("ruler-minister allegory", "ruler-minister scheme"),
        ("The ruler-minister allegory", "The ruler-minister scheme"),
    ],
    "t_e6eb14b6c1ca": [
        ("幻有's methods", "Huanyou’s recorded ways of addressing people"),
    ],
    "t_15026800437e": [
        ("discriminating", "distinguishing"),
        ("discrimination", "distinguishing"),
        ("discriminate", "distinguish"),
    ],
    "t_1a7e251bda53": [
        ("instruction to the assembly", "address to the assembly"),
    ],
    "t_4f7bd98ad40f": [
        ("formal Dharma-hall address", "formal teaching-hall address"),
        ("Dharma hall", "teaching hall"),
    ],
    "t_4ccf8aed47d3": [
        ("the X68n1318 witness's same-sense variant 擬趣即乖 ('head for it and you are off')", "the X68n1318 witness's same-sense variant 'head for it and you are off' (擬趣即乖)"),
    ],
    "t_6edb551acb53": [
        ("分別 (discrimination)", "分別 (distinguishing)"),
    ],
    "t_8a016f49e5b8": [
        ("pondering-and-discriminating", "pondering-and-distinguishing"),
        ("not-discriminating", "not-distinguishing"),
    ],
    "t_93ab42fecdca": [
        ("from the origin, not one thing exists", "originally, not one thing"),
    ],
    "t_d35dc9e3723e": [
        ("無 = no + 念 = thought: 'no-thought.'", "No (無) plus thought (念): 'no-thought.'"),
        ("render 無念 as", "render no-thought (無念) as"),
        ("counts: 無念 (no-thought) 810", "counts: no-thought (無念), 810"),
    ],
    "t_e4d6ebff1bb2": [
        ("446 occurrences of 如何是佛。 in 30 texts and 1,676 occurrences of 如何是佛？ in 237 texts", "446 occurrences of the period form (如何是佛。) in 30 texts and 1,676 occurrences of the question-mark form (如何是佛？) in 237 texts"),
    ],
    "t_edabab064644": [
        ("what rises when one holds the word raised (話頭)", "what the cited texts say arises when a word or saying is raised for investigation (話頭, ‘word or saying’)"),
        ("in doing the work what matters is raising the doubt", "when exerting effort, what matters is raising the doubt"),
        ("birth-and-death", "life and death"),
        ("birth-death", "life-and-death"),
        ("Zen-allowlist grep counts: 疑情 (the doubt)", "Zen-allowlist grep counts: the doubt (疑情)"),
    ],
    "t_f2181872b682": [
        ("A 轉語 (turning word)", "A turning word (轉語)"),
        (", 下轉語 9.", ", and the compact form 'lay down a turning word' (下轉語), 9."),
    ],
    "t_ff50c6974a36": [
        ("two terms — 正 (the upright/straight) and 偏 (the crooked/bent) —", "two terms — the upright or straight (正) and the crooked or bent (偏) —"),
        ("Zen-allowlist grep counts: 五位 1,134", "Zen-allowlist grep counts: the Five Ranks (五位), 1,134"),
    ],
}


def main():
    changed = 0
    for entry_id, replacements in REPLACEMENTS.items():
        path = BUILD / "terms" / entry_id / "entry.v2.json"
        text = path.read_text(encoding="utf-8")
        original = text
        for old, new in replacements:
            if old not in text:
                if new in text:
                    continue
                # A later full prose refresh may supersede both strings.
                continue
            text = text.replace(old, new)
        if text != original:
            path.write_text(text, encoding="utf-8")
            changed += 1
            print(f"updated {entry_id}")
    print(f"updated files: {changed}")


if __name__ == "__main__":
    main()
