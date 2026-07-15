import json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; sys.path.insert(0,str(ROOT)); import zc
z=json.loads((ROOT/'terms/t_0f97bfab265c/entry.v2.json').read_text()); s=z['Senses'][0]
s['Occurrences']=[o for o in s['Occurrences'] if '棒喝' in o.get('Kwic','')]
for o in s['Occurrences']:
    if o.get('MasterName'):
        o.pop('ActorAttribution',None); o['ContextMasters']=[{'MasterName':o['MasterName'],'Roles':['utterer']}]
s.update(PreferredTarget='stick-blows and shouts',AlternateTargets=['blows and shouts','the stick-and-shout method'],SearchAliases=['stick blows and shouts','blows and shouts','stick and shout','encounter actions'],Explanation="Stick-blows and shouts names the paired encounter actions of striking with a staff and shouting. Records describe teachers using them, students receiving them, and later speakers appraising reliance on them as a recognizable method. The compound can summarize a style even where a stored sentence does not narrate a fresh blow or shout. These are deployments of one paired action-set rather than separate senses.",Note="The frozen corpus has 1,087 exact hits in 252 files representing 249 works. Ten standalone anchors span direct use, instruction, retrospective description, stylistic appraisal, and criticism across independent works; a legacy witness lacking the exact headword was rejected.")
z.update(CreatedBy='Codex fresh-build lane C',WrittenUtc=None,CorpusBaselineSha256='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a')
out=ROOT/'fresh-build/entries/t_0f97bfab265c'; out.mkdir(parents=True,exist_ok=True); (out/'entry.v2.json').write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n'); (out/'STATUS').write_text('drafted\n')
