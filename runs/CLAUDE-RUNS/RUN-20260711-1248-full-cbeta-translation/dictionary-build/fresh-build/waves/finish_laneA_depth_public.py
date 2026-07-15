import json,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];lp=ROOT/'fresh-build/waves/f001-laneA.json';led=json.loads(lp.read_text());by={e['term']:e for e in led['entries']}
def save(term,z):
 e=by[term];p=ROOT/'fresh-build/entries'/e['id']/'entry.v2.json';p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');e['entrySha256']=hashlib.sha256(p.read_bytes()).hexdigest()
def load(term):return json.loads((ROOT/'fresh-build/entries'/by[term]['id']/'entry.v2.json').read_text())
z=load('作麼生');s=z['Senses'][0]
s['Occurrences'].append({'RelPath':'J/J24/J24nB137.xml','FromLb':'0359c12','ToLb':'0359c12','Kwic':'師云：「作麼生道？」','MasterName':'Zhaozhou Congshen','ContextMasters':[{'MasterName':'Zhaozhou Congshen','Roles':['utterer','record-owner']}],'Curated':True,'AttributionNote':'Recorded Sayings of Master Zhaozhou (趙州和尚語錄): Zhaozhou Congshen asks how the student would say it.'})
if 'J/J24/J24nB137.xml' not in s['SourceTexts']:s['SourceTexts'].append('J/J24/J24nB137.xml')
save('作麼生',z)
z=load('序');z['Senses'][0]['Explanation']=z['Senses'][0]['Explanation'].replace('序 names','the headword names')
z['Senses'][0]['Occurrences'][6]['AttributionNote']=z['Senses'][0]['Occurrences'][6]['AttributionNote'].replace('in 永覺元賢禪師廣錄 (鼓山晚錄序)','in the Extensive Record of Chan Master Yongjue Yuanxian (永覺元賢禪師廣錄), under the Preface to the Late Gushan Record (鼓山晚錄序)')
save('序',z)
z=load('葛藤');z['Senses'][0]['Explanation']=z['Senses'][0]['Explanation'].replace('Literally naming intertwining vines, the Chan metaphor denotes','In Chan records, the image of intertwining vines denotes')
save('葛藤',z)
for term,lines in {
 '金鎖玄路':['modifier-relation-verdict: gold modifies the lock image within the compound; it is not an independent material claim.','display-modifier-verdict: retain “golden lock” as the graph-level image while denying praise or literal construction.'],
 '葛藤':['literal-countersearch-verdict: broad KWIC countersearch found vine imagery, roots, branches, and creepers used within the verbal-entanglement metaphor, but no independently anchored botanical plant sense; no split is licensed.']}.items():
 e=by[term];w=ROOT/'fresh-build/entries'/e['id']/'WORK.md';t=w.read_text();w.write_text(t.rstrip()+'\n'+'\n'.join(lines)+'\n')
led['updatedUtc']='2026-07-15T02:45:00Z';lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
