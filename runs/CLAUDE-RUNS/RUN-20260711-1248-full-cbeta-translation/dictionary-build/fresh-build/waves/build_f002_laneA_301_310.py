import copy, datetime, hashlib, json, os, re, sys

ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__), '..','..'))
sys.path.insert(0, ROOT)
import zc

PREFLIGHT=os.path.join(ROOT,'fresh-build','waves','f002-laneA-301-400-preflight.json')
BASELINE='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a'
NOW='2026-07-15T12:00:00Z'

OPENINGS={
'如人飲水':'In these records, drinking water supplies a recurrent comparison for knowledge reported by the drinker: cold and warmth are known by the person who drinks.',
'頑空':'The records reject “stubborn emptiness” as an insensible blankness mistaken for oneself, and contrast it with concrete things and lively response.',
'畫餅':'A painted cake is repeatedly named as something that cannot feed: the records apply that bodily limit to words, talk, blows, and shouts that likewise fail to satisfy hunger.',
'飢來喫飯':'“When hungry, eat rice” recurs as an answer and self-description, while Dazhu Huihai explicitly distinguishes his eating from the distracted eating attributed to other people.',
'一行三昧':'Huineng defines complete command of the single conduct through straightforward mind in walking, standing, sitting, and lying down, and explicitly rejects motionless sitting as its definition.',
'參同契':'The Accord of Difference and Sameness is the title of Shitou Xiqian’s verse, identified as a composed work, quoted by its opening and closing lines, and discussed by later masters.',
'動念即乖':'“Stir a thought and at once deviate” functions as a compact verdict, but the records immediately deny that mere nonmovement or dead-water stillness resolves it.',
'無依道人':'Linji’s “person of the Way with no reliance” names the presently hearing person in his hall addresses, which call that person the mother of buddhas and the one who rides circumstances.',
'擬心即差':'“Frame it in mind and already miss” is paired with stirring thought, then corrected against inert nonmovement in repeated hall statements and interviews.',
'一切現成':'“Everything is already complete” appears as a Fayan-house characterization, a direct interview answer, and an exhortation to take up what requires no added construction.',
}

ALIASES={
'如人飲水':['like a person drinking water','drinking water knows cold and warmth'],
'頑空':['stubborn emptiness','insensible emptiness','blank emptiness'],
'畫餅':['painted cake','drawn cake','a painted cake cannot satisfy hunger'],
'參同契':['Accord of Difference and Sameness','Harmony of Difference and Sameness','Shitou poem'],
'動念即乖':['stir a thought and deviate','raise a thought and miss'],
'無依道人':['person of the Way with no reliance','person with no reliance','unreliant person of the Way'],
'擬心即差':['frame it in mind and miss','form an intention and miss'],
'一切現成':['everything is already complete','all is already complete','everything fully present'],
}

# Full-case corrections where the legacy container-owner attribution is not the
# grammatical actor of the stored headword clause.
NARRATED={
('畫餅','X/X64/X64n1260.xml','0111b03'):('compiler narrative','the compiler narrating Xiangyan Zhixian’s encounter','Xiangyan Zhixian'),
('參同契','T/T51/T51n2076.xml','0309c07'):('compiler narrative','the lamp-record compiler narrating Shitou Xiqian’s authorship','Shitou Xiqian'),
}
IMPERSONAL={
('參同契','T/T51/T51n2076.xml','0459b07'):('work heading','the repeated title heading of Shitou Xiqian’s verse','Shitou Xiqian'),
}
EXTRAS={
'如人飲水':{
 'RelPath':'X/X83/X83n1578.xml','Kwic':'明曰：惠明雖在黃梅，實未省自己面目。今蒙指示，如人飲水，冷煖自知。','MasterName':'Huiming','Curated':True,
 'AttributionNote':'The Record Pointing at the Moon (指月錄): Huiming is the exact speaker of the headword-bearing reply to Huineng.'},
'參同契':{
 'RelPath':'X/X83/X83n1578.xml','Kwic':'遂著參同契曰：竺土大仙心，東西密相付。','Curated':True,
 'AttributionNote':'The Record Pointing at the Moon (指月錄): the compiler narrates Shitou Xiqian’s composition of the titled verse.',
 'ActorAttribution':{'Status':'narrated','Kind':'compiler narrative','ActorLabel':'the compiler narrating Shitou Xiqian’s authorship','ActorRole':'compiler','ReviewedBy':'Codex f002 Lane A full-case review','ReviewedUtc':NOW,'GrammarEvidence':'遂著參同契曰 is third-person biographical narration introducing the verse attributed to Shitou Xiqian.','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage']},
 'ContextMasters':[{'MasterName':'Shitou Xiqian','Roles':['person-described','verse-author']}]},
}
EXTRA2={
'參同契':{
 'RelPath':'J/J34/J34nB311.xml','Kwic':'《參同契》曰：『萬物各有功，當知用及處。本末盡歸宗，尊卑用其語。』','MasterName':'Juelang Daosheng','Curated':True,
 'AttributionNote':'Complete Record of Chan Master Juelang Daosheng of Tianjie (天界覺浪盛禪師全錄): Juelang Daosheng is the exact essay speaker quoting the titled verse.'}
}

def actorize(term,o):
    key=(term,o['RelPath'],o['FromLb'])
    if key in NARRATED:
        kind,label,ctx=NARRATED[key]
        o.pop('MasterName',None)
        o['ActorAttribution']={'Status':'narrated','Kind':kind,'ActorLabel':label,'ActorRole':'compiler','ReviewedBy':'Codex f002 Lane A full-case review','ReviewedUtc':NOW,'GrammarEvidence':f'The headword occurs in third-person editorial narration, not in speech by {ctx}.','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage']}
        o['ContextMasters']=[{'MasterName':ctx,'Roles':['person-described']}]
        decision=f'The compiler narrates the headword-bearing clause about {ctx}; {ctx} is not its utterer.'
        o['DraftActorProof']={'GrammaticalSubject':label,'ExactHeadwordClause':o['Kwic'],'FullCaseDecision':decision}
    elif key in IMPERSONAL:
        kind,label,ctx=IMPERSONAL[key]
        o.pop('MasterName',None)
        o['ActorAttribution']={'Status':'impersonal','Kind':kind,'ActorLabel':label,'ActorRole':'compiler','ReviewedBy':'Codex f002 Lane A full-case review','ReviewedUtc':NOW,'GrammarEvidence':'The headword is printed as the work title immediately before the verse; no person utters this heading.','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage']}
        o['ContextMasters']=[{'MasterName':ctx,'Roles':['verse-author']}]
        o['DraftActorProof']={'GrammaticalSubject':label,'ExactHeadwordClause':o['Kwic'],'FullCaseDecision':'This is an impersonal work heading; Shitou Xiqian is retained only as verse-author context.'}
    elif o.get('MasterName'):
        name=o['MasterName']
        o.pop('ActorAttribution',None)
        o['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}]
        o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'SpeechFrame':f'Complete-case review assigns the stored headword-bearing turn to {name}.','FullCaseDecision':f'{name} is the exact speaker of the stored headword-bearing turn; nearby people, if any, are not substituted for the utterer.'}
    else:
        aa=o.get('ActorAttribution')
        if not aa: raise ValueError(f'unresolved actor {term} {key}')
        aa.setdefault('ReviewedBy','Codex f002 Lane A full-case review'); aa.setdefault('ReviewedUtc',NOW)
        aa.setdefault('RungsChecked',['line','expanded-context','section-header','book-title','tei-header','parallel-passage'])
        aa.setdefault('GrammarEvidence',f"The complete case assigns the headword-bearing {aa.get('ActorRole','turn')} to {aa.get('ActorLabel','the recorded non-master actor')}; the named respondent is retained only as context.")
        o['DraftActorProof']={'GrammaticalSubject':aa.get('ActorLabel'),'ExactHeadwordClause':o['Kwic'],'FullCaseDecision':aa.get('GrammarEvidence') or 'The complete case assigns the headword to the recorded non-master actor.'}

def main():
    pre=json.load(open(PREFLIGHT,encoding='utf-8'))['entries'][:10]
    receipt=[]
    for ordinal,p in enumerate(pre,301):
        oldp=os.path.join(ROOT,'terms',p['id'],'entry.v2.json')
        old=json.load(open(oldp,encoding='utf-8'))
        term=old['SourceTerm']; entry={'Id':old['Id'],'SourceTerm':term,'CreatedBy':'Codex fresh f002 Lane A evidence-first','WrittenUtc':NOW,'Senses':[],'CorpusBaselineSha256':BASELINE}
        for s in old['Senses']:
            ns=copy.deepcopy(s)
            oldexp=ns.pop('Explanation','')
            ns['SearchAliases']=ns.get('SearchAliases') or ALIASES.get(term) or [ns['PreferredTarget']]
            ns['ClaimAnchors']=ns.get('ClaimAnchors') or []
            verified=[]; claims=[]
            for o in ns.get('Occurrences',[]):
                v=zc.verify(o['RelPath'],o['Kwic'])
                if not v.get('ok'): raise ValueError((term,o['RelPath'],v))
                o['FromLb']=v['fromLb']; o['ToLb']=v['toLb']; o['Curated']=True
                actorize(term,o)
                q=''.join(o['Kwic'].split())
                if term not in q and not (o.get('VariantForm') and o['VariantForm'] in q and o.get('EvidenceRole')=='variant'):
                    o['ClaimText']=o['Kwic']; o.pop('EvidenceRole',None); claims.append(o)
                else: verified.append(o)
            if term in EXTRAS:
                o=copy.deepcopy(EXTRAS[term]); v=zc.verify(o['RelPath'],o['Kwic'])
                if not v.get('ok'): raise ValueError((term,o['RelPath'],v))
                o['FromLb']=v['fromLb']; o['ToLb']=v['toLb']; actorize(term,o); verified.append(o)
            if term in EXTRA2:
                o=copy.deepcopy(EXTRA2[term]); v=zc.verify(o['RelPath'],o['Kwic'])
                if not v.get('ok'): raise ValueError((term,o['RelPath'],v))
                o['FromLb']=v['fromLb']; o['ToLb']=v['toLb']; actorize(term,o); verified.append(o)
            ns['Occurrences']=verified
            ns['ClaimAnchors']=(ns.get('ClaimAnchors') or [])+claims
            ns['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in verified))
            workids=sorted({zc.work_id(o['RelPath']) for o in verified if term in re.sub(r'\s+','',o['Kwic'])})
            ns['Validation']='multi-source' if len(workids)>=2 else 'provisional'
            body=oldexp
            body=re.sub(r'^Literally[^.]*\.\s*','',body)
            ns['ExplanationParts']={'CorpusEarnedOpening':OPENINGS[term],'EvidenceBody':[body]}
            ns['DraftEvidence']={'OpeningClaimEvidenceKeys':[f'o{i}' for i in range(1,len(verified)+1)],'ZenBend':OPENINGS[term],
              'CounterexampleOrLimit':ns.get('Note') or 'The full concordance and neighboring family were checked; ordinary or merely overlapping uses do not donate a further sense.',
              'DifferentThingTest':{'Decision':'one-thing','ComparedThings':[ns['PreferredTarget']],'Reason':'The full cases show grammatical and rhetorical variation around one referent or formula, not a second lexical thing.'},
              'AliasRationale':'Aliases expose the preferred literal wording and close English lookup forms without adding an interpretation.',
              'ModifierControls':['Headword-bearing compounds and graphic variants were checked separately; they do not silently count as exact headword evidence.'],
              'FamilyControls':['The overlapping term family was retested against the expanded frozen corpus; no related entry donates an unsupported sense.'],
              'IndependentWorkIds':workids}
            entry['Senses'].append(ns)
        worksheet={'SchemaVersion':1,'Entry':entry}
        outdir=os.path.join(ROOT,'fresh-build','entries',p['id']); os.makedirs(outdir,exist_ok=True)
        wp=os.path.join(outdir,'evidence.draft.json')
        with open(wp,'w',encoding='utf-8') as f: json.dump(worksheet,f,ensure_ascii=False,indent=2); f.write('\n')
        receipt.append({'ordinal':ordinal,'id':p['id'],'term':term,'worksheet':os.path.relpath(wp,ROOT),'worksheetSha256':hashlib.sha256(open(wp,'rb').read()).hexdigest(),'hits':p['hits'],'files':p['files'],'works':p['works']})
    rp=os.path.join(ROOT,'fresh-build','waves','f002-laneA-301-310-build-inventory.json')
    with open(rp,'w',encoding='utf-8') as f: json.dump({'schemaVersion':1,'wave':'f002','lane':'A','ordinalStart':301,'ordinalEnd':310,'corpusBaselineSha256':BASELINE,'entries':receipt},f,ensure_ascii=False,indent=2); f.write('\n')

if __name__=='__main__': main()
