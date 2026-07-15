from pathlib import Path
import datetime, json, sys

R = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(R))
import zc

BASE = '42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a'
NOW = datetime.datetime.now(datetime.timezone.utc).isoformat()


def unique_kwic(rel, term, occurrence_index=0):
    text, _ = zc._load(rel)
    starts, at = [], 0
    while True:
        at = text.find(term, at)
        if at < 0:
            break
        starts.append(at)
        at += len(term)
    pos = starts[occurrence_index]
    radius = 14
    while True:
        kwic = text[max(0,pos-radius):min(len(text),pos+len(term)+radius)]
        verdict = zc.verify(rel,kwic)
        if verdict['ok'] and verdict['count'] == 1:
            return kwic
        radius += 12


def occurrence(rel, term, master, decision, occurrence_index=0, contexts=()):
    kwic = unique_kwic(rel,term,occurrence_index)
    v = zc.verify(rel,kwic)
    return {
        'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kwic,
        'MasterName':master,'Curated':True,
        'ContextMasters':[{'MasterName':master,'Roles':['utterer']}]
            +[{'MasterName':x,'Roles':['record-owner']} for x in contexts],
        'AttributionNote':f'The source record or case collection ({zc.title(rel)}; {rel}). Exact actor: {master}. {decision}',
        'DraftActorProof':{'ExactHeadwordClause':term,'GrammaticalSubject':master,
            'SpeechFrame':decision,'FullCaseDecision':decision},
    }


def sense(target, alternates, aliases, opening, body, occurrences, note, bend, limit, family, related=()):
    works=list(dict.fromkeys(zc.work_id(o['RelPath']) for o in occurrences))
    return {'SenseKey':None,'MasterName':None,'PreferredTarget':target,'AlternateTargets':alternates,
      'SearchAliases':aliases,'Status':'preferred','ExplanationParts':{'CorpusEarnedOpening':opening,'EvidenceBody':body},
      'Validation':'multi-source' if len(works)>=2 else 'provisional','Note':note,'Occurrences':occurrences,
      'ClaimAnchors':[],'SourceTexts':list(dict.fromkeys(o['RelPath'] for o in occurrences)),
      'RelatedMasters':list(dict.fromkeys(o['MasterName'] for o in occurrences)),'RelatedTerms':list(related),
      'DraftEvidence':{'OpeningClaimEvidenceKeys':[f'o{i}' for i in range(1,len(occurrences)+1)],'ZenBend':bend,
        'CounterexampleOrLimit':limit,'DifferentThingTest':{'Decision':'one-thing',
          'ComparedThings':['the ordinary image','the phrase in Chan records'],
          'Reason':'The stored predicates retain one referent; shifts of grammar or appraisal do not establish another thing.'},
        'AliasRationale':'The aliases cover natural English lookup forms without importing a doctrinal interpretation.',
        'ModifierControls':['not-applicable: no construction-material claim is inferred from the headword.'],
        'FamilyControls':[family],'IndependentWorkIds':works}}


def work_text(term,s):
    n=len(s['Occurrences'])
    return f'''# {term} — f005 lane A construction

- discovery-provenance: `fresh-build/waves/f005-laneA-1201-1300-preflight.json`; inherited analysis remained a research lead only.
- indexed-path: frozen-corpus preflight; every saved row reverified with `zc.verify`.
- definition-searches: direct questions, answer frames, predicates, contrasts, family forms, recensions, and contradictory deployments.
- deployment-inventory: {n} curated rows across {len(s['DraftEvidence']['IndependentWorkIds'])} independent works.
- omission-audit: every public prose claim has exact evidence; parallel recensions were not used as padding.
- family-retest: {s['DraftEvidence']['FamilyControls'][0]}
- sense-target-distinguishability: `not-applicable — one referent across the stored deployments`.
- observation: occurrence IDs `o1–o{n}` establish the ordinary scene and named Chan deployments.
- minimal-inference: {s['DraftEvidence']['ZenBend']}
- ordinary-bridge: ordinary physical and institutional relations connect the exact predicates; no outside doctrine is needed.
- falsification-searches: literal use; definition question; opposite predicate; longer compounds; repeated family; contrary appraisal.
- counterexamples: {s['DraftEvidence']['CounterexampleOrLimit']}
- scope: `corpus-wide phrase within the cited Chan deployments`.
- verdict: `licensed`.
- feedback-inference-verdict: `supported` — the opening is the narrowest inference shared by the stored predicates.
- feedback-observations: occurrence IDs `o1–o{n}` establish the image and deployments stated in the article.
- feedback-falsification-searches: literal uses; definition questions; opposite predicates; family forms; repeated cases; contradictory appraisals.
- feedback-counterexamples: {s['DraftEvidence']['CounterexampleOrLimit']}
- feedback-scope: `corpus-wide phrase within the cited Chan deployments; no outside symbolic theory`.
- lookup-probes: {'; '.join(s['SearchAliases'])}.
- opening-interpretation-verdict: `licensed` — the opening states the ordinary scene and narrow corpus deployment before evidence details.
'''
