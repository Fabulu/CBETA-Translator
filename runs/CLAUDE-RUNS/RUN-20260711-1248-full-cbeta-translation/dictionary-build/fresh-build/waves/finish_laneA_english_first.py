import json,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];lp=ROOT/'fresh-build/waves/f001-laneA.json';led=json.loads(lp.read_text());by={e['term']:e for e in led['entries']}
repls={
'示眾':{'With a concrete object and a gesture verb, 示眾 keeps':'With a concrete object and a gesture verb, the headword keeps'},
'無心':{'In ordinary adverbial syntax, 無心 means':'In ordinary adverbial syntax, the headword means'},
'小參':{'in 緇門警訓:':'in Admonitions for the Monastic Gate (緇門警訓):'},
'和尚':{'Here 戒 identifies Master Wuzu Jie;':'Here Jie is Master Wuzu Jie’s personal name;'},
'如何是佛':{'如何是 supplies the interrogative “what is,”, and 佛 is its requested predicate;':'The opening words supply the interrogative “what is,” and the final graph is its requested predicate;', '如何是 supplies the interrogative “what is,” and 佛 is its requested predicate;':'The opening words supply the interrogative “what is,” and the final graph is its requested predicate;'}
}
for term,rr in repls.items():
 e=by[term];p=ROOT/'fresh-build/entries'/e['id']/'entry.v2.json';t=p.read_text()
 for a,b in rr.items():t=t.replace(a,b)
 p.write_text(t);e['entrySha256']=hashlib.sha256(p.read_bytes()).hexdigest()
led['updatedUtc']='2026-07-15T02:55:00Z';lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
