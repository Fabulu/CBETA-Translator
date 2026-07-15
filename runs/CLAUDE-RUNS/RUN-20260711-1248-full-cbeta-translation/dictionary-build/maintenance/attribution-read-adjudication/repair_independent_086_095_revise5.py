import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
IDS=['t_8482770fe735','t_8beda961c75a','t_a66ef543d2ea','t_a7f67b4983d9','t_a9f422b3b249']
P={i:ROOT/'fresh-build/entries'/i/'entry.v2.json' for i in IDS}; old={i:hashlib.sha256(p.read_bytes()).hexdigest() for i,p in P.items()};D={i:json.loads(p.read_text(encoding='utf8')) for i,p in P.items()};changes={i:[] for i in IDS}

# 象王: the evidence is distributed across speakers; only one selected utterance is Changlu's own backward-look line.
s=D['t_8482770fe735']['Senses'][0]
s['Explanation']="The elephant king appears through elephant-like gait, return, and royal-animal comparisons, distinct from the lion language paired with it. Changlu Timing looks right and says, 'the bearing of the elephant king—how could it forget the backward look?' Other speakers independently use the image: Luopu Yuanan says fox tracks cease where the elephant king walks; Yuanwu Keqin contrasts the lion's world-shaking vigor with the elephant king's effortless backward look; Linji Yixuan lists riding an elephant king among manifested forms. The backward look also enters encounter history when Fushan Fayuan awakens after hearing a seat-address pair the lion's frown with the elephant king's backward look. The image can be tested rather than merely praised: an unnamed monk quotes the fox-track line and is driven out with blows. Zen bends the royal animal's gait and backward glance into teaching-seat bearing, verse, and encounter language. The corpus does not make all of these deployments Changlu Timing's personal conduct, and neither does this entry."
changes['t_8482770fe735'].append('Narrowed synthesis from Changlu-specific recurrence to the actual multi-speaker distribution; roster-only deferrals retained.')

# 陶淵明: semantic account stands; two real masters remain explicit roster deferrals rather than being falsified.
changes['t_8beda961c75a'].append('Full reading reconfirmed Baofang Jin and Shengfa Fa as exact utterers; names remain explicit pending-roster candidates, not downgraded to non-masters.')

# 善財: link fields must contain canonical roster names, not Chinese section titles.
s=D['t_a66ef543d2ea']['Senses'][0];s['RelatedMasters']=['Baizhang Daoheng','Sanzu Chonghui'];changes['t_a66ef543d2ea'].append('Canonicalized RelatedMasters from Chinese section-title strings to roster names[0].')

# 日用: 今日用處 is a cross-boundary collision, not the lexical headword 日用. Remove both such rows.
s=D['t_a7f67b4983d9']['Senses'][0]; kept=[];removed=[]
for o in s['Occurrences']:
 if '今日用' in o.get('Kwic',''): removed.append({'RelPath':o['RelPath'],'FromLb':o['FromLb'],'Kwic':o['Kwic']})
 else: kept.append(o)
s['Occurrences']=kept
s['Explanation']="Daily functioning is what occurs in the ordinary course of the day and in activities repeatedly carried out there. Speakers say it cannot be hidden, ask what the daily matter is, and locate it amid recurring work, perception, doubt, and dust-and-toil rather than outside them. The entry excludes the character-boundary string 今日用處 ('today's use'), which is not the word 日用."
changes['t_a7f67b4983d9'].append(f'Removed {len(removed)} false 今日用處 boundary collision(s); six genuine 日用 occurrences remain.')

# 生緣: 戀生緣 is neither birthplace nor arising-condition grammar; the verse supplies an attested third thing.
d=D['t_a9f422b3b249'];s0=d['Senses'][0]; verse=s0['Occurrences'].pop(6)
s0['Explanation']="A person's place or circumstance of origin. In public interviews Dongshan asks Lingyun where his 生緣 is, and Huanglong Huinan turns the same origin question into one of his three chamber checkpoints. Later records quote, versify, appraise, and answer that origin question."
s2={'SenseId':'s3','PreferredTarget':'worldly ties; ties binding one to life','Definition':'The ties or circumstances of worldly life to which one remains attached.','Explanation':"In an anonymous case verse, 白頭猶自戀生緣 says that although the homeland has long been peaceful, the white-haired person still clings to 生緣. The verb 戀 ('cling to, be attached to') makes this a worldly or life-binding tie, not a question about birthplace and not the condition by which all things arise.",'Validation':'provisional','Occurrences':[verse],'RelatedMasters':[]}
d['Senses'].append(s2)
d['Senses'][0]['Explanation'] += " The corpus separately uses 生緣 for a condition of arising and, in 戀生緣, for worldly or life-binding ties; those are split below rather than blurred into the origin question."
changes['t_a9f422b3b249'].append('Split 戀生緣 into a provisional third sense, worldly/life-binding ties, anchored by its verse occurrence; roster-only Jinsu Rong deferral retained.')

rows=[]
for i,p in P.items():
 p.write_text(json.dumps(D[i],ensure_ascii=False,indent=2)+'\n',encoding='utf8');rows.append({'id':i,'term':D[i]['SourceTerm'],'oldSha256':old[i],'newSha256':hashlib.sha256(p.read_bytes()).hexdigest(),'changes':changes[i]})
out=ROOT/'maintenance/attribution-read-adjudication/cohorts-7-9-086-095-independent-revise5-ledger.json';out.write_text(json.dumps({'generatedUtc':datetime.now(timezone.utc).isoformat(),'rows':rows},ensure_ascii=False,indent=2)+'\n',encoding='utf8');print(out)
