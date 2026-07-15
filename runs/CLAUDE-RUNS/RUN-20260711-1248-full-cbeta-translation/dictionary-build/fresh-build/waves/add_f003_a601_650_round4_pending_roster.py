#!/usr/bin/env python3
import json,pathlib
B=pathlib.Path(__file__).resolve().parents[2];p=B/'fresh-build/pending-roster.json';d=json.loads(p.read_text(encoding='utf8'))
rows=[
('Bajiao Huiche',['芭蕉徹'],{'RelPath':'X/X66/X66n1297.xml','FromLb':'0280c22','ToLb':'0280c22','Kwic':'芭蕉徹云無孔笛，遇氈拍板。'}),
('Shanci Ji',['山茨際禪師'],{'RelPath':'X/X64/X64n1260.xml','FromLb':'0030a11','ToLb':'0030a12','Kwic':'山茨際禪師佛誕示眾，舉世尊初生話畢，師云：古今尊宿盡道雲門此語奇特'}),
('Manora',['摩拏羅尊者'],{'RelPath':'X/X80/X80n1568.xml','FromLb':'0591a11','ToLb':'0591a12','Kwic':'於是焚香，遙語月氏國鶴勒那比丘曰'})]
have={x['canonicalName'] for x in d['candidates']}
for name,aliases,evidence in rows:
 if name not in have:d['candidates'].append({'canonicalName':name,'aliases':aliases,'evidence':[evidence],'reviewedBy':'Codex f003 A601-650 round4 repair author','reviewReport':'fresh-build/waves/f003-laneA-601-650-round4-fresh-independent-exact-review.json','status':'awaiting-roster-integration'})
d['candidates'].sort(key=lambda x:x['canonicalName']);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(json.dumps({'added':[x[0] for x in rows]},ensure_ascii=False))
