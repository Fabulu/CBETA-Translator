import json, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; sys.path.insert(0,str(ROOT)); import zc
src=json.loads((ROOT/'terms/t_ff50c6974a36/entry.v2.json').read_text())
closed={"utterer","respondent","questioner","interlocutor","addressee","section-subject","record-owner","person-described","person-discussed","commentator","later-raiser","later-quoter","teacher","student","compiler","verse-author","case-figure"}
for s in src['Senses']:
    s['Occurrences']=[o for o in s['Occurrences'] if '五位' in o.get('Kwic','')]
    for o in s['Occurrences']:
        # Exact-turn correction required by ACTOR_AUDIT.md.
        if ('五位對賓' in o['Kwic'] or '五位對賔' in o['Kwic']) and ('僧問' in o['Kwic'] or '因僧問' in o['Kwic']):
            o.pop('MasterName',None)
            o['ActorAttribution']={"Status":"reviewed-unnamed","Kind":"monk","ActorLabel":"the unnamed questioning monk","ActorRole":"questioner","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"ReviewedBy":"Codex fresh lane-C complete-case review","ReviewedUtc":"2026-07-14T17:00:00Z"}
            o['ContextMasters']=[{"MasterName":"Caoshan Benji","Roles":["respondent","section-subject"]}]
            o['AttributionNote']=f"{zc.title(o['RelPath'])}: the unnamed monk utters the headword in his question; Caoshan Benji is the respondent and section subject."
        elif o.get('MasterName'):
            o.pop('ActorAttribution',None); o['ContextMasters']=[{"MasterName":o['MasterName'],"Roles":["utterer"]}]
        else:
            a=o.get('ActorAttribution') or {}
            a['ActorRole']='questioner' if 'question' in (a.get('ActorLabel','')+a.get('Kind','')).lower() else ('compiler' if a.get('Status') in {'narrated','impersonal'} else 'utterer')
            if a: o['ActorAttribution']=a
            cs=[]
            for c in o.get('ContextMasters') or []:
                if isinstance(c,str): cs.append({'MasterName':c,'Roles':['section-subject']})
                elif isinstance(c,dict) and c.get('MasterName'):
                    roles=[r for r in c.get('Roles',[]) if r in closed]
                    cs.append({'MasterName':c['MasterName'],'Roles':roles or ['section-subject']})
            o['ContextMasters']=cs

tech, ordinary=src['Senses']
tech.update(PreferredTarget='the Caodong Five Ranks',AlternateTargets=['the Five Ranks','the five positions of the Caodong house'],SearchAliases=['Caodong Five Ranks','Five Ranks','five positions','upright and crooked ranks'],Explanation="The Caodong Five Ranks are a named five-part scheme associated with Dongshan Liangjie and Caoshan Benji. Corpus passages identify sets framed through upright and crooked, ruler and minister, merit, princes, or facing the guest; later records repeatedly pair the Five Ranks with the Linji house's Three Mysteries when distinguishing house vocabularies. Questions can ask for the ranks as a whole or for each member in order. The headword names the fivefold scheme, while the particular paired terms specify its formulation.",Note="The frozen corpus has 1,279 exact hits in 233 files representing 230 works. The technical anchors include defining enumerations, verses, house comparisons, interview questions, and later summaries. In the Caoshan facing-the-guest cases, the unnamed monk—not Caoshan—utters the headword.")
ordinary.update(PreferredTarget='five positions',AlternateTargets=['five ranks','five stages'],SearchAliases=['five positions','five ranks','five stages'],Explanation="Five positions is also an ordinary count phrase for a five-member sequence not referring to the Caodong scheme. Context identifies the particular series, such as successive positions in an explanatory classification or an ordinal place. This sense is retained to prevent every standalone occurrence from being forced into the house-specific reading.",Note="This sense is separate because it denotes other five-member sequences rather than the named Caodong Five Ranks. Longer numerals and compounds do not count unless the exact standalone graphs function as the phrase.")
src.update(CreatedBy='Codex fresh-build lane C',WrittenUtc=None,CorpusBaselineSha256='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a')
out=ROOT/'fresh-build/entries/t_ff50c6974a36'; out.mkdir(parents=True,exist_ok=True)
(out/'entry.v2.json').write_text(json.dumps(src,ensure_ascii=False,indent=2)+'\n'); (out/'STATUS').write_text('drafted\n')
