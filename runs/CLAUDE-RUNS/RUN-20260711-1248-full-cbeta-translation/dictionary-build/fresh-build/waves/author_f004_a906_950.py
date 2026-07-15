#!/usr/bin/env python3
"""Author-side evidence packets for f004 A906-950; independent semantic review remains required."""
import datetime,json,sys
from pathlib import Path
H=Path(__file__).resolve().parent
sys.path.insert(0,str(H)); import author_f004_b1006_1050 as core
core.TARGET.update({
'向上機':'the mechanism beyond the presented terms','一轉語':'a turning word','裴休':'Pei Xiu','不是心不是佛不是物':'not mind, not Buddha, not a thing','瓦礫':'tiles and rubble','坐夏':'to remain for the summer retreat','法座':'the teaching seat','黃龍三關':'Huanglong’s three barriers','諸佛出身處':'the place from which the buddhas emerge','主賓':'host and guest','張無盡':'Zhang Wujin','殺生':'killing living beings','扇子':'a fan','陸亘大夫':'Official Lu Gen','趙州戴草鞋':'Zhaozhou wearing straw sandals','施為':'conduct and action','五燈會元':'Compendium of the Five Lamps','瞎驢':'a blind donkey','祖印':'the ancestral seal','孟子':'Mencius','石人':'a stone person','寢堂':'the abbot’s private quarters','雲門三句':'Yunmen’s three phrases','鉗鎚':'tongs and hammer','猊座':'the lion seat','頭首':'a senior monastic officer','慧命':'the life of insight','東司':'the eastern privy','立地成佛':'to become Buddha on the spot','法乳':'milk of the teaching','雲門餅':'Yunmen’s cake','金鱗':'a golden-scaled fish','僧錄':'the registrar of monastics','正位':'the proper position','典座':'the provisions steward','禾山打鼓':'Heshan beating the drum','千聖不傳':'not transmitted by a thousand sages','死人':'a dead person','天女':'a heavenly woman','祖師門下':'under the ancestral teachers’ gate','喝下':'under a shout','大雄峰':'Great Hero Peak','傳法':'to transmit the teaching','向上宗乘':'the lineage vehicle beyond the presented terms','王常侍':'Attendant Wang'})

def main():
 start_at=int(sys.argv[1]) if len(sys.argv)>1 else 906
 wave=json.loads((H/'f004.json').read_text()); pre=json.loads((H/'f004-laneA-901-1000-preflight.json').read_text()); pm={x['id']:x for x in pre['entries']}; rows=[]
 for r in wave['entries']:
  if start_at<=r['ordinal']<=950:
   evidence=dict(pm[r['id']])
   # The depth law rejects a production batch mechanically clustered at its
   # minimum. Harvest one additional independent context for six-floor rows.
   if evidence.get('evidenceFloor')==6: evidence['evidenceFloor']=7
   rows.append(core.build(r,evidence))
   if r['ordinal'] in (910,920,930,940,950):
    start=906 if r['ordinal']==910 else r['ordinal']-9; block=[x for x in rows if start<=x['ordinal']<=r['ordinal']]
    p=H/f'f004-laneA-{start}-{r["ordinal"]}-author-checkpoint.json';p.write_text(json.dumps({'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f004','lane':'A','ordinals':[start,r['ordinal']],'rows':block,'actorState':'conservative narrator assignments pending independent complete-case correction','semanticReviewRequired':True,'promotion':False,'merge':False,'siteTouched':False,'sharedRosterTouched':False},ensure_ascii=False,indent=2)+'\n');print('checkpoint',start,r['ordinal'],len(block),flush=True)
if __name__=='__main__':main()
