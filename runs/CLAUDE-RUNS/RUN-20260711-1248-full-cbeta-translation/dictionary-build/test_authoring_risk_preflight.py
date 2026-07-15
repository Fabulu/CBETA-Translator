import json,tempfile,unittest
from pathlib import Path
from authoring_risk_preflight import lint
class RiskTests(unittest.TestCase):
 def run_case(self,sense):
  with tempfile.TemporaryDirectory() as td:
   p=Path(td)/'evidence.draft.json';p.write_text(json.dumps({'Entry':{'Id':'t_x','SourceTerm':'卓一下','Senses':[sense]}}));return lint(p)
 def test_action_performer_is_flagged(self):
  s={'PreferredTarget':'to strike once','Occurrences':[{'MasterName':'Mazu Daoyi','DraftActorProof':{'GrammaticalSubject':'Mazu Daoyi'}}]};self.assertIn('action-performer-mastername-risk',[x['kind'] for x in self.run_case(s)['flags']])
 def test_backward_assignment_mismatch_is_flagged(self):
  s={'PreferredTarget':'a phrase','Occurrences':[{'MasterName':'Longya Judun','DraftActorProof':{'GrammaticalSubject':'the unnamed monk questioner'}}]};self.assertIn('master-proof-subject-mismatch',[x['kind'] for x in self.run_case(s)['flags']])
 def test_risky_claim_is_flagged(self):
  s={'PreferredTarget':'a phrase','ExplanationParts':{'CorpusEarnedOpening':'It discloses the source.'},'Occurrences':[]};self.assertIn('unsupported-prose-claim-risk',[x['kind'] for x in self.run_case(s)['flags']])
if __name__=='__main__':unittest.main()
