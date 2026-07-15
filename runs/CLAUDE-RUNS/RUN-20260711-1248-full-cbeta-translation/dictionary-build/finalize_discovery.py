# -*- coding: utf-8 -*-
import json, sys
sys.stdout.reconfigure(encoding="utf-8")
# candidate union from the 5 agent reports, by category (noise/李白/桃花 already excluded; variants noted)
cats = {
 "Non-master allusion figures": "傅大士 維摩詰 寒山 拾得 布袋和尚 誌公 莊周 張三李四 張公喫酒李公醉 邯鄲 卞和 許由 南柯 漁父 孔子 堯舜 黃粱 鍾馗 呂洞賓 牧童 爛柯 秦鏡 驪龍".split(),
 "Animal & creature metaphors": "鐵牛 木馬 龜毛兔角 蟭螟 大蟲 犀牛 香象 師子兒 靈龜 死蛇 猢猻 蝦蟆 龍蛇 金翅鳥 木雞 羚羊掛角 老鼠入牛角 蝸角 醯雞".split(),
 "Folk proverbs / stock sayings": "將錯就錯 壓良為賤 逢場作戲 賊過後張弓 鑽龜打瓦 掩耳偷鈴 一盲引眾盲 畫蛇添足 守株待兔 水中捉月 認影迷頭 緣木求魚 拋磚引玉 貧兒思舊債 入海算沙 眾盲摸象 刻舟求劍 效顰 抱橋柱 買帽相頭 繫驢橛".split(),
 "Nature / object allusions": "空華 水月 電光石火 紅爐點雪 無縫塔 無孔笛 陽焰 驪珠 優曇華 鏡花 枯木生花 摩尼珠 無絃琴 芥子納須彌".split(),
 "Trades / market / household": "定盤星 草鞋錢 飯袋子 酒糟漢 白拈賊 運水搬柴 秤錘 栗棘蓬 蒲團".split(),
}
wave = open("WAVE_PLAN.md", encoding="utf-8").read()
req  = open("REQUESTED_TERMS.md", encoding="utf-8").read()
tb = json.load(open(r"C:/temp/NewTranslationrepos/CbetaZenTranslations/termbase.v2.json", encoding="utf-8"))
tbset = set(e["SourceTerm"] for e in tb["Entries"])
rtext = json.dumps(json.load(open(r"C:/programmieren/MergeWorkCbeta/CBETA-Translator/Assets/Data/master-dates.json", encoding="utf-8")), ensure_ascii=False)
def covered(h):
    if h in tbset: return "termbase"
    if h in wave: return "WAVE_PLAN"
    if h in req: return "REQUESTED"
    if h in rtext: return "ROSTER"
    return None
newcats={}; dropped=[]
seen=set()
for cat, terms in cats.items():
    keep=[]
    for h in terms:
        if h in seen: continue
        seen.add(h)
        c=covered(h)
        if c: dropped.append(f"{h}[{c}]")
        else: keep.append(h)
    newcats[cat]=keep
total=sum(len(v) for v in newcats.values())
print(f"FINAL NEW GAPS: {total}   (dropped {len(dropped)} already-covered)")
for cat,ks in newcats.items():
    print(f"\n{cat} ({len(ks)}):\n  "+ "  ".join(ks))
print("\nDROPPED:", ", ".join(dropped))
# write summary object for the append step
open("_newgaps.json","w",encoding="utf-8").write(json.dumps(newcats,ensure_ascii=False,indent=1))
