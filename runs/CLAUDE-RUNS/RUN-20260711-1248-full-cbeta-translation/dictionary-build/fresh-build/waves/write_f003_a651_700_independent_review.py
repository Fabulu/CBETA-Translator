#!/usr/bin/env python3
import datetime, hashlib, json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
packet_path = ROOT / "fresh-build/waves/f003-laneA-651-700-current-semantic-review-packet.json"
gate_path = ROOT / "fresh-build/waves/f003-laneA-651-700-formal-gate-current.json"
packet = json.loads(packet_path.read_text())
gate = json.loads(gate_path.read_text())

findings = {
651:"The opening makes 錯 a public Zen verdict, but the evidence also contains ordinary textual error and simple misnaming; it neither splits/narrows those referents nor owns speech marked 妙喜云 and 師云, which is left as compiler narration.",
652:"The generic figure template never says what Manjusri does in these records. Three selected rows are catalogues or tables of contents, while the question 文殊仗劒 and the narrated 文殊白椎 require concrete case explanation and exact actors.",
653:"The staff's teaching-seat deployment is identified, but catalogue material is retained as defining evidence and several direct staff acts after 師/拈 are assigned to compiler narration. Genuine Chinese master labels also remain non-roster-exact.",
654:"The semantic distinction between inability, denial, and an exchange answer is only announced, not represented as different-things/sense adjudication. Most direct quoted turns, including 云不會 and 翠巖芝云, are incorrectly compiler-owned.",
655:"The generic Maudgalyayana paragraph does not define his Zen deployment; one witness is a ritual catalogue. The useful Nanquan image-making account is not synthesized, and the repeated Chinese master name is not roster-exact.",
656:"The opening is category filler—mounting a seat is an act, not an implement, office, rite, or communal act menu. Catalogue/title rows dilute the evidence and named masters remain in unresolved Chinese labels.",
657:"The attendant is an institutional office, but the paragraph is reusable category filler and never distinguishes personal attendant, role title, and catalogue occurrence. Several biographical actions and named record owners are flattened into narration/non-roster Chinese names.",
658:"The witnesses overwhelmingly denote the sixteenth patriarch Rahulatā rather than Buddha's son Rahula, so the preferred target is the wrong person. Genealogical catalogues dominate and require a person/title sense correction, not generic figure prose.",
659:"The bowl's physical scene and Zen bend are not stated; the paragraph is reusable institutional filler. A contents-list row and a long lineage poem are not defining bowl witnesses, while five exact masters remain Chinese/non-roster labels.",
660:"A Chan person is not an implement, office, rite, or communal act. The entry fails to distinguish named addressees/titles from the general class, and its prose never explains how 禪人 functions in addresses and warnings.",
661:"藥師 is under-split: the corpus includes Medicine Buddha/ritual-title material, temple/name strings, and likely occupational wording, while the single target 'medicine master' and generic figure paragraph identify none of them. Actor labels such as 御選 and a poem title are not masters.",
662:"佛祖 is a collective phrase, not one 'figure.' The generic template omits its paired authority/lineage usage; the 古庭語錄 preface occurrence is specifically written by the named preface author Wu Yingbin (吳應賓), not an anonymous compiler, while other catalogue/preface rows and Chinese master labels remain unresolved.",
663:"The Maitreya article is generic and does not surface the Chan deployments visible in the witnesses, including public questions and named-case uses. A catalogue row contributes no defining action, and Chinese master labels remain unresolved.",
664:"The bamboo-switch interpretation is promising, but the full contexts include Dahui's explicit naming/refusal test and quoted older turns whose utterers are not reliably separated. Five Chinese-script MasterName values violate roster-exact linkage and one quoted turn is compiler-owned.",
665:"The explanation merges a buddha's appearance, a master's inauguration/public service, and ordinary emergence without a different-referent test. Catalogue evidence and non-roster actor names prevent the claimed Zen institutional bend from being securely anchored.",
666:"The generic Ashoka paragraph never identifies the relic/stupa and royal-question deployments actually present. Two catalogue/genealogy rows and four Chinese-script actor labels make the selected evidence both semantically thin and attribution-defective.",
667:"The ordinary bowl-washing scene and Zhaozhou bend are well stated, but later quoted retellings are assigned to compiler narration and the named masters are stored in non-roster Chinese forms. Exact utterer ownership therefore still fails.",
668:"The prose usefully distinguishes denial from ignorance but claims a corpus test it does not spell out witness by witness. Multiple 師云/quoted answers remain narrator-owned, and the two named masters are not roster-exact.",
669:"The generic Upali paragraph omits his precept/Vinaya role—the reason the Chan record invokes him—and does not separate the person from names/titles. Both named masters are Chinese-script labels and an embedded speech passage is compiler-owned.",
670:"An evening address is an institutional event, not the category menu 'implement, office, rite, or communal act.' The entry needs the actual evening hall sequence and contrast with other address forms, and its four master names are not roster-exact.",
671:"The face-to-face inference is plausible, but the article does not distinguish ordinary meeting, formal audience, and challenge formula. A long doctrinal speech is compiler-owned despite an enclosing speaker, and three master labels remain Chinese.",
672:"The generic Guanyin template does not define the figure's Chan use (including hearing/seeing and quoted case roles). Catalogue evidence is retained, two speech-bearing contexts are narrator-owned, and two masters are non-roster Chinese strings.",
673:"The turn-taking definition is useful, but direct questions such as 者曰未審 and other marked turns are assigned to compiler narration. The entry therefore contradicts its own claim that 未審 belongs to the questioner; its named masters are also non-roster forms.",
674:"The head monk is an office, but the paragraph is reusable filler and catalogue/lineage-list rows dominate. It does not distinguish office-holder speech from compiler lists, and the one master name is not roster-exact.",
675:"The generic Samantabhadra paragraph withholds the actual Zen predicates and boundaries. An embedded 師曰 turn is compiler-owned and three MasterName strings are titles/Chinese labels rather than roster-exact utterers.",
676:"Taking up the staff is a visible hall action, but the opening is category filler and never tells what the selected masters do with it. Four direct 師/拈 contexts are classified as narration and four Chinese names remain unresolved.",
677:"The selected rows are mainly titles, colophons, and catalogues naming Chan monasteries, not evidence for a characteristic Zen lexical bend. The generic institutional paragraph overclaims encounters while no exact named utterer is established.",
678:"The hard-rule framing is useful, but the entry does not adjudicate ordinary life-protection against the paradoxical 護生須是殺 formula or anchor who quotes it. Five Chinese-script masters and a compiler-owned 古德偈 turn require exact actor repair.",
679:"Three genuinely different referents are plausibly split, but each explanation ends in the same evidence-process filler instead of showing its actual predicates. The source allocation and exact actors, including three Chinese master names and a narrated speech frame, need case-by-case proof.",
680:"The generic Purna paragraph does not say which Purna the witnesses invoke or what his Chan deployment is. A patriarchal title string and embedded speech are mixed with person evidence, while two Chinese actor labels remain unresolved.",
681:"The great precepts are hard rules, but the institutional category template never identifies ordination, reception, or the corpus's tensions around them. A ceremonial narrative is treated as speech evidence and both named masters are non-roster Chinese forms.",
682:"叢林 bends 'grove' into the organized monastic community, but the entry never pictures that lexical/institutional shift; it merely uses generic monastery filler. Four Chinese-script MasterName values also prevent reliable master links.",
683:"The generic Devadatta paragraph gives neither the ordinary/person identity nor the specific accusations, comparisons, or questions for which Chan speakers invoke him. Three Chinese master labels remain unresolved and the four witnesses are too thinly synthesized.",
684:"The causal-relation opening is informative, but the assertion/negation/fox-family controls are stated rather than demonstrated from the selected contexts. One long quoted discourse is compiler-owned and the sole named master is a non-roster Chinese label.",
685:"Senior elder is a title/person role, not a generic implement-office-rite menu. The entry does not distinguish address, office, collective group, and biographical narration; four named masters remain Chinese labels and one speech-like context is narrator-owned.",
686:"The generic Ajatashatru paragraph never explains the king's role in the recorded Kāśyapa/council material. All witnesses are narration or embedded quotation, yet the exact quoted actors and context figures are not separated.",
687:"The semantic account of substitute answers is strong, but 師代云 and named 代云 turns are repeatedly assigned to compiler narration or to labels such as 五家. The five Chinese/title MasterName values do not resolve the actual substitute speaker.",
688:"羅漢 is under-split between arhat as a rank/person and 羅漢 as monastery/master name in catalogue headings. The generic figure paragraph obscures that distinction; catalogue rows and compiler-owned quoted questions make the evidence unsound.",
689:"The witnesses support communal monastery labor, especially Baizhang working before the assembly, but the explanation is reusable category filler and never states those concrete constraints. Four Chinese master names are not roster-exact.",
690:"The public-recognition opening is useful, but one full encounter with explicit speech remains compiler-owned and five exact masters are stored in Chinese labels. The entry also needs a clearer boundary between ordinary identification and case-tested discernment.",
691:"The article does not explain Dipankara's specific Chan deployment: Śākyamuni's 'no teaching obtained' and prediction formula. Three narration rows and one non-roster Chinese master leave the quoted ownership and figure bend underdescribed.",
692:"The definition imports 'conditioned by accumulated action and habit' without an anchored self-definition and does not translate or unpack the busy/entangled predicates. Several direct speeches are narrator-owned and four Chinese MasterName values fail exact linkage.",
693:"住持 is both the act 'to maintain/occupy' and the institutional abbot title in these contexts; the one-sense 'abbot' entry does not adjudicate that grammar. Catalogue/colophon rows dominate and the generic category prose supplies no Zen bend.",
694:"Although equivalent to Dipankara in some contexts, 定光佛 also appears in ritual titles and a direct 'what is 定光佛?' interview. The generic figure prose ignores that deployment and leaves the questioning monk/answer context largely unresolved.",
695:"The flagpole is concrete, but the paragraph is generic and never explains the gate/monastery landmark or the repeated 'topple the flagpole' case. Two long quoted contexts are compiler-owned and three named masters are Chinese labels.",
696:"The lineage-descendant inference is good, but several later commentators explicitly marked X云 are left as compiler narration and two masters remain non-roster Chinese. The entry also needs to separate literal family offspring if any survive the concordance.",
697:"The witnesses supply a distinctive Chan bend—Never-Disparaging's prediction is answered with hitting—yet the generic figure template never says so. Three Chinese master names remain unresolved and the scriptural quotation's narrator/quoted actor needs separation.",
698:"The public-answer definition is useful, but the selected evidence includes the unrelated segmentation 外道得 ('what principle did the outsider obtain'), which is not an occurrence of lexical 道得. Multiple direct turns are narrator-owned and three master labels remain Chinese.",
699:"The no-reply outcome is well bounded, but every witness is assigned to compiler narration although the headword grammatically describes named case participants such as Kāśyapa, a monk, or a lecturer. ContextMasters and exact impersonal/narrative ownership need full-case correction.",
700:"The corpus gives a concrete Zen deployment—Ever-Weeping sells heart and liver to seek prajna and is raised in challenges—but the generic figure paragraph withholds it. The unnamed questioner and two Tianjie Juelang attributions need exact turn/context verification.",
}

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
gate_entries = {x["id"]: x for x in gate["entries"]}
rows=[]
for item in packet["items"]:
    ordinal=item["ordinal"]; entry_path=ROOT/item["path"]; worksheet=entry_path.parent/"evidence.draft.json"
    entry=json.loads(entry_path.read_text()); count=sum(len(s.get("Occurrences",[])) for s in entry["Senses"])
    current=sha(entry_path)
    assert gate_entries[item["id"]]["sha256"] == current
    rows.append({"ordinal":ordinal,"id":item["id"],"term":item["term"],"entrySha256":current,
                 "worksheetSha256":sha(worksheet),"verdict":"REVISE","occurrencesRead":count,
                 "reviewNotes":findings[ordinal]})

base={"schemaVersion":1,"reviewType":"independent-semantic-full-exact-hash-review","wave":"f003","lane":"A",
      "ordinals":[651,700],"generatedUtc":datetime.datetime.now(datetime.timezone.utc).isoformat(),
      "reviewer":"Codex independent reviewer /root/fresh_semantic_reviewer/f003_c851_900",
      "readOnly":True,"entriesEdited":False,"siteTouched":False,"sourcePacket":str(packet_path.relative_to(ROOT)),
      "sourcePacketSha256":sha(packet_path),"sourcePacketStaleEntryHashes":sum(item['sha256']!=sha(ROOT/item['path']) for item in packet['items']),
      "mechanicalGate":{"path":str(gate_path.relative_to(ROOT)),"sha256":sha(gate_path),"hardPass":gate['hardPass'],
                         "exactKwicVerified":gate['exactKwic']['verified'],"exactKwicFailures":gate['exactKwic']['failureCount']},
      "currentHashesVerifiedAgainstFormalGate":True,"occurrencesRead":sum(r['occurrencesRead'] for r in rows),
      "summary":{"entries":50,"KEEP":0,"REVISE":50},"rows":rows}
out=ROOT/'fresh-build/waves/f003-laneA-651-700-independent-exact-review.json'
out.write_text(json.dumps(base,ensure_ascii=False,indent=2)+'\n')
for start in [651,661,671,681,691]:
    sub=[r for r in rows if start<=r['ordinal']<=start+9]
    checkpoint={k:v for k,v in base.items() if k!='rows'}
    checkpoint.update({'checkpointThrough':start+9,'checkpointRows':sub,'checkpointOccurrenceCount':sum(r['occurrencesRead'] for r in sub)})
    p=ROOT/f'fresh-build/waves/f003-laneA-{start}-{start+9}-independent-review-checkpoint.json'
    p.write_text(json.dumps(checkpoint,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'report':str(out.relative_to(ROOT)),'sha256':sha(out),'entries':50,'occurrencesRead':base['occurrencesRead'],'sourcePacketStaleHashes':base['sourcePacketStaleEntryHashes']},indent=2))
