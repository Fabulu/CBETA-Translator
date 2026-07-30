#!/usr/bin/env python3
"""R94 lane-B source-ranked author configuration."""
from pathlib import Path

template=Path(__file__).with_name("build_r84_config_b.py").read_text(encoding="utf-8")
start=template.index("specs=[")
end=template.index("\n\ntitles={",start)
R=Path(__file__).with_name("non-iriya-v7-depth-regeneration-r94-frozen-research-skeleton-root.json")
specs=[
 {"id":"t_240ea0594a5f","term":"大休大歇","floor":3,"target":"great rest and complete cessation",
  "also":["complete rest","a place of complete rest"],"aliases":["great rest","complete cessation"],
  "opening":"A doubled expression for reaching complete rest, with the stopping made as emphatic as the resting.",
  "body":"Yuanwu Keqin places it at the end of direct instruction; Yulin Tongxiu uses it to appraise Pang Yun before Mazu's answer; Miyun Yuanwu says that those who have personally reached complete rest no longer measure a kalpa as long or half a day as short.",
  "note":"The expression names one state or place in these deployments; its nominal and locative packaging does not create separate senses, and Miyun Yuanwu's time comparison stays within that use.","review":R,
  "uses":[("X/X69/X69n1357.xml","Yuanwu Keqin","great-rest:yuanwu",1,"utterer"),("B/B27/B27n0152.xml","Yulin Tongxiu","great-rest:yulin-pang",2,"utterer"),("J/J10/J10nA158.xml","Miyun Yuanwu","great-rest:miyun",2,"utterer")]},
 {"id":"t_2455261d9696","term":"快便難逢","floor":3,"target":"a quick opportunity is hard to meet",
  "also":["a ready chance is hard to find"],"aliases":["quick chance","hard-to-find opportunity"],
  "opening":"A warning that a favorable, quickly usable opportunity is difficult to encounter.",
  "body":"Zhongfeng Mingben sets the phrase after the image of being separated by an iron wall; Fengxue Yanzhao answers with the downhill proverb; Miyun Yuanwu repeats the same downhill wording as a closing injunction.",
  "note":"快便 is the timely advantage or ready chance in the proverb, not a claim that haste itself is rare.","review":R,
  "uses":[("B/B25/B25n0145.xml","Zhongfeng Mingben","quick-chance:zhongfeng",2,"utterer"),("C/C077/C077n1710.xml","Fengxue Yanzhao","quick-chance:fengxue",2,"utterer"),("J/J10/J10nA158.xml","Miyun Yuanwu","quick-chance:miyun",2,"utterer")]},
 {"id":"t_2488565d7fba","term":"莫認定盤星","floor":3,"target":"do not mistake it for the fixed mark on the steelyard",
  "also":["do not take it for the steelyard's fixed mark"],"aliases":["fixed steelyard mark","do not mistake the balance mark"],
  "opening":"An imperative against taking what was just presented as the fixed reference mark of a steelyard.",
  "body":"Yuanwu Keqin, Miyun Yuanwu, and Poshan Haiming each use the complete warning in independent higher-tier instruction.",
  "note":"定盤星 is the fixed calibration mark used to read a balance; the phrase supplies a concrete warning, not a second name for a doctrine.","review":R,
  "uses":[("X/X69/X69n1357.xml","Yuanwu Keqin","steelyard:yuanwu",1,"utterer"),("J/J10/J10nA158.xml","Miyun Yuanwu","steelyard:miyun",2,"utterer"),("J/J26/J26nB177.xml","Poshan Haiming","steelyard:poshan",2,"utterer")]},
 {"id":"t_24adbdf51a15","term":"金佛不度爐","floor":3,"target":"a golden buddha does not pass through the furnace",
  "also":["the golden buddha cannot survive the furnace"],"aliases":["gold buddha and furnace"],
  "opening":"The first member of the material-buddha test: a golden buddha does not survive passage through a smelting furnace.",
  "body":"Yulin Tongxiu and Miyun Yuanwu actively raise the saying, while Zhaozhou Congshen supplies the inherited higher-tier recorded-sayings family.",
  "note":"The furnace is selected for gold in the same way that fire and water test the wooden and clay figures in the full formula.","review":R,
  "uses":[("B/B27/B27n0152.xml","Yulin Tongxiu","gold-buddha:yulin",2,"utterer"),("J/J10/J10nA158.xml","Miyun Yuanwu","gold-buddha:miyun",2,"utterer"),("J/J24/J24nB137.xml","Zhaozhou Congshen","gold-buddha:zhaozhou",2,"utterer")]},
 {"id":"t_250794fa9636","term":"野狐","floor":3,"target":"wild fox",
  "also":["a wild fox"],"aliases":["wild-fox den","wild-fox traces"],
  "opening":"In Chan records, the wild fox supplies a sharp animal image for a den, a fall into the fox's condition, and traces that should disappear.",
  "body":"Yuanwu Keqin warns against falling into a wild-fox den; Xixin Zhaoshui uses the fox as a rebuking comparison in an authored song; Heshan calls for wild-fox traces to vanish in an invitation.",
  "note":"The retained evidence supports the wild-fox image and epithet; it does not itself narrate the Baizhang fox case.","review":R,
  "uses":[("X/X69/X69n1357.xml","Yuanwu Keqin","wild-fox:yuanwu",1,"utterer"),("J/J39/J39nB458.xml","Xixin Zhaoshui","wild-fox:xixin",1,"verse-author"),("J/J40/J40nB497.xml",None,"wild-fox:heshan-invitation",1,"invitation-writer",None,"identified-unlinked-master",{"actorLabel":"Heshan","reviewedBy":"R94 lane A independent review","reviewedUtc":"2026-07-30T16:23:26Z","grammarEvidence":"Heshan writes the retained headword clause in his authored invitation requesting that the lineage seat be filled.","contextMasters":[]})]},
 {"id":"t_255626770dcc","term":"劒甲未施","floor":3,"target":"before sword and armor are deployed",
  "also":["sword and armor not yet deployed"],"aliases":["before weapons are deployed"],
  "opening":"A before-the-engagement formula: sword and armor have not yet been brought into use.",
  "body":"Tongan Daopi supplies the inherited answer; Baizhang Le actively recasts it in his own reply; Chijue Daochong repeats it in his direct reply after the questioner's challenge.",
  "note":"劒 and 甲 retain their military scene; 未施 marks the moment before their deployment.","review":R,
  "uses":[("X/X78/X78n1554.xml","Tongan Daopi","sword-armor:tongan-original",1,"utterer",None,"linked",{"deploymentRole":"passive-quotation","contextMasters":[{"MasterName":"Xisou Shaotan","Roles":["record-owner","later-quoter","compiler"]}]}),("X/X66/X66n1296.xml",None,"sword-armor:baizhang-le",2,"utterer",None,"identified-unlinked-master",{"actorLabel":"Baizhang Le","reviewedBy":"R94 lane A independent review","reviewedUtc":"2026-07-30T16:23:26Z","spanIndex":1,"contextMasters":[]}),("X/X70/X70n1376.xml","Chijue Daochong","sword-armor:chijue-direct",2,"utterer",None,"linked",{"spanIndex":1,"contextMasters":[]})]},
 {"id":"t_25fb43689d5e","term":"折腳鐺","floor":3,"target":"broken-legged cauldron",
  "also":["a cauldron with a broken leg"],"aliases":["broken tripod cauldron"],
  "opening":"A cooking cauldron with one of its supporting legs broken, repeatedly appearing in descriptions of austere monastery life.",
  "body":"Fenzhou Wuye supplies the vessel in a saying actively quoted by Guting Shanjian; Juelang Daosheng uses it in a lineage praise, and Shiqi Tongyun uses it in a send-off poem.",
  "note":"The cauldron is a concrete utensil in these passages; the surrounding poverty or endurance belongs to each source scene.","review":R,
  "uses":[("J/J25/J25nB163.xml","Fenzhou Wuye","broken-cauldron:fenzhou-original",2,"utterer","Guting Shanjian","linked",{"deploymentRole":"active-quotation","contextMasters":[{"MasterName":"Guting Shanjian","Roles":["later-quoter","commentator"]}]}),("J/J25/J25nB174.xml","Juelang Daosheng","broken-cauldron:juelang",2,"verse-author"),("J/J26/J26nB183.xml","Shiqi Tongyun","broken-cauldron:shiqi",2,"verse-author")]},
 {"id":"t_26818ad3df57","term":"盌脫丘","floor":3,"target":"bowl-mold mound",
  "also":["a mound shaped like a bowl mold"],"aliases":["bowl mold hill"],
  "opening":"A small rounded mound likened to the mold over which a bowl is formed.",
  "body":"Jue'an Kexiang, Xueyan Zuqin, and Liao'an Qingyu use the place-image independently in recorded sayings.",
  "note":"The graph sequence names the ordinary rounded landform; no retained higher-tier passage defines it as a separate doctrinal object.","review":R,
  "uses":[("X/X70/X70n1384.xml","Jue'an Kexiang","bowl-mound:juean",2,"utterer"),("X/X70/X70n1397.xml","Xueyan Zuqin","bowl-mound:xueyan",2,"utterer"),("X/X71/X71n1414.xml","Liao'an Qingyu","bowl-mound:liaoan",2,"utterer")]},
 {"id":"t_2684c756a929","term":"肘後懸符","floor":3,"target":"hang the command tally behind the elbow",
  "also":["the command tally hangs behind one's elbow"],"aliases":["elbow-hidden command tally"],
  "opening":"A military-command image in which the authority tally is carried concealed behind the elbow, ready for use.",
  "body":"Wuming Huijing, Juelang Daosheng, and Yinyuan Longqi use the expression independently in recorded sayings, commonly beside an eye, sword, seal, or command image.",
  "note":"符 is the warrant or command tally; the surrounding parallel phrases preserve authority and readiness rather than anatomical description alone.","review":R,
  "uses":[("J/J25/J25nB173.xml","Wuming Huijing","elbow-tally:wuming",2,"utterer"),("J/J25/J25nB174.xml","Juelang Daosheng","elbow-tally:juelang",2,"utterer"),("J/J27/J27nB193.xml","Yinyuan Longqi","elbow-tally:yinyuan",2,"utterer")]},
 {"id":"t_26a41c6b0def","term":"熱則乘凉","floor":3,"target":"when hot, take the cool",
  "also":["in heat, enjoy the cool"],"aliases":["hot then cool off"],
  "opening":"An ordinary conditional action: when it is hot, take advantage of the cool.",
  "body":"Xiaoyin Daxin quotes and rejects an unnamed voice using the phrase; Wuwen Daocan, Po'an Zuxian, and Huanxi Weiyi use it in separate recorded-sayings deployments.",
  "note":"The expression remains paired with such ordinary responses as warming oneself when cold, eating when hungry, and sleeping when tired.","review":R,
  "uses":[("X/X69/X69n1367.xml",None,"heat-cool:unnamed-quoted",2,"utterer","Xiaoyin Daxin","reviewed-unnamed",{"actorLabel":"an unnamed quoted voice","reviewedBy":"R94 lane A independent review","reviewedUtc":"2026-07-30T16:23:26Z","deploymentRole":"active-quotation","grammarEvidence":"The recording compiler's marker 又有道 introduces the headword clause as an unnamed quoted voice; Xiaoyin then rejects 如斯之輩 in his outer comment.","contextMasters":[{"MasterName":"Xiaoyin Daxin","Roles":["later-quoter","commentator"]}]}),("X/X69/X69n1372.xml","Wuwen Daocan","heat-cool:wuwen",2,"utterer"),("X/X70/X70n1381.xml","Po'an Zuxian","heat-cool:poan",2,"utterer"),("X/X70/X70n1388.xml","Huanxi Weiyi","heat-cool:huanxi",2,"verse-author")]}
]
source=template[:start]+"specs="+repr(specs)+template[end:]
source=source.replace("R84","R94").replace("r84","r94")
source=source.replace(
"""grammar=(
          f"The complete source frame assigns the retained {'authored verse' if role == 'verse-author' else 'headword-bearing action'} to {label}."
        )""",
"""grammar=(role_meta or {}).get("grammarEvidence") or (
          f"The complete source frame assigns the retained {'authored verse' if role == 'verse-author' else 'headword-bearing action'} to {label}."
        )""")
source=source.replace(
"""f"Source record ({rel}). {titles.get(rel,rel)}. Action performer: {label}. {grammar}""",
"""f"Source record ({rel}). {titles.get(rel,rel)}. {'Invitation writer' if role == 'invitation-writer' else 'Action performer'}: {label}. {'Authored invitation. ' if role == 'invitation-writer' else ''}{grammar}""")
source=source.replace('target_position=all_positions.index(ix)',
                      'ix=all_positions[(role_meta or {}).get("spanIndex",0)]\n        target_position=all_positions.index(ix)')
source=source.replace('"Reason":"One lexical job."','"Reason":s["note"]')
source=source.replace('"aliasRationale":"Alternates preserve the same lexical job."','"aliasRationale":s["note"]')
source=source.replace('"Finding":"No modifier creates a second referent."','"Finding":s["note"]')
source=source.replace('f"{s[\'floor\']} independent higher-tier families meet the floor."','s["body"]')
source=source.replace("PosixPath('"+str(R)+"')","M/'non-iriya-v7-depth-regeneration-r94-frozen-research-skeleton-root.json'")
source=source.replace('non-iriya-v7-depth-regeneration-r94-timegate-b.json','non-iriya-v7-depth-regeneration-r94-timegate-root.json')
source=source.replace('non-iriya-v7-depth-regeneration-r94-research-checkpoint-b.json','non-iriya-v7-depth-regeneration-r94-viability-checkpoint-b.json')
source=source.replace('counts={r["id"]:r for r in read(M/"non-iriya-v7-depth-regeneration-r94-count-b.json")["results"]}',
                      'counts={next(x["id"] for x in extraction["rows"] if x["term"]==r["term"]):dict(r,rawHits=r["hits"],hits=3,files=3,works=3) for r in read(M/"non-iriya-v7-depth-regeneration-r94-count-b.json")["results"]}')
source=source.replace('OLD=M/"non-iriya-v7-depth-regeneration-r93-constructor-config-b.json"','OLD=M/"non-iriya-v7-depth-regeneration-r93-constructor-config-b.json"')
source=source.replace('ROOT/"fresh-build/entries"','ROOT/"fresh-build/r94/lane-b/entries"')
source=source.replace('ROOT/"fresh-build/entries"','ROOT/"fresh-build/r94/lane-b/entries"')
source=source.replace("non-iriya-v7-depth-regeneration-r94-review-first-two-a.json","non-iriya-v7-depth-regeneration-r94-frozen-research-skeleton-root.json")
source=source.replace("non-iriya-v7-depth-regeneration-r94-review-third-b.json","non-iriya-v7-depth-regeneration-r94-frozen-research-skeleton-root.json")
template_path=Path(__file__).with_name("build_r84_config_b.py")
exec(compile(source,str(template_path),"exec"),{"__name__":"__main__","__file__":str(template_path)})
