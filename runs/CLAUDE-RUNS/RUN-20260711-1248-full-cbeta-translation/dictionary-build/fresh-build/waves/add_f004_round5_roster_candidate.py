import json
from pathlib import Path
R=Path(__file__).resolve().parents[2];E=R/'fresh-build/entries';W=R/'fresh-build/waves'
d=json.loads((E/'t_1fe4eac13d6e'/'evidence.draft.json').read_text())['Entry'];o=next(x for s in d['Senses'] for x in s['Occurrences'] if x.get('MasterName')=='Tiantong Pu')
p=W/'f004-cohort1-round5-roster-candidates.json';payload={'schemaVersion':'pending-roster-candidates-v1','candidates':[{'canonicalName':'Tiantong Pu','aliases':['Tiantong Pu'],'evidence':[{k:o[k] for k in ('RelPath','FromLb','ToLb','Kwic')}],'reviewedBy':'Codex f004 cohort1 round5 delta repair','reviewReport':'fresh-build/waves/f004-cohort1-round4-delta-independent-rereview.json','status':'awaiting-roster-integration'}]};p.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n')
