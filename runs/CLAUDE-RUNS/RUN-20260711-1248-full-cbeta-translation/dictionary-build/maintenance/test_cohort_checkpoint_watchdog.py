#!/usr/bin/env python3
import hashlib, json, os, shutil
from datetime import datetime, timezone
from pathlib import Path
import subprocess, sys, tempfile, time, unittest
from maintenance import cohort_checkpoint_watchdog as watchdog

ROOT = Path(__file__).resolve().parent.parent
W = ROOT / "maintenance/cohort_checkpoint_watchdog.py"
WRAPPER = ROOT / "maintenance/dictionary_python_env.py"
AUDIT_WRITER = ROOT / "maintenance/write_governed_research_audit.py"

def h(text): return hashlib.sha256(text.encode()).hexdigest()
def sh(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()

class CheckpointTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(dir=ROOT/"maintenance"); self.r=Path(self.temp.name)
        self.terms=["甲","乙"]
        self.ids=["t_"+hashlib.sha256(x.encode()).hexdigest()[:12] for x in self.terms]
        self.floors=[6,4]
        self.deadlines={"viability":120,"researchExtraction":120,"adjudicatedConfig":300,
          "constructor":310,"firstProduct":330,"construction":390,"review":530,
          "correction":630,"publication":720}
        self.tg=self.r/"timegate.json"
        self.tg.write_text(json.dumps({"startedEpoch":1000,"artifactZero":True,
          "createdUtc":datetime.fromtimestamp(1000,timezone.utc).isoformat(),
          "requiredFloors":self.floors,"admittedRequiredOccurrences":10,
          "adjudicatedCaseLoad":10,
          "deadlinesSeconds":self.deadlines}))
        os.utime(self.tg,(1000,1000))
    def tearDown(self): self.temp.cleanup()
    def call(self,*a): return subprocess.run([sys.executable,str(W),*map(str,a)],capture_output=True,text=True)
    def candidate(self,term):
        span=f"xx{term}yy"; context=f"bounded {span} complete case"
        return {"relPath":"J/J01.xml","fromLb":"0001a01","toLb":"0001a04",
                "workId":"work:J01","tier":2,"context":context,"spanText":span,
                "matchedTerm":term,"contextSha256":h(context),"spanSha256":h(span)}
    def extraction(self, shallow=False, pre_epoch=False):
        out=self.r/"extract.json"; research=self.r/"research.json"; script=self.r/"extract.py"
        rows=[]; skeleton=[]
        for ident,term in zip(self.ids,self.terms):
            cs=[{"relPath":"J/J01.xml"}] if shallow else [self.candidate(term)]
            rows.append({"id":ident,"term":term,"sourceCandidates":cs})
            hashes=[hashlib.sha256(json.dumps(x,ensure_ascii=False,sort_keys=True).encode()).hexdigest() for x in cs]
            skeleton.append({"id":ident,"term":term,"candidateHashes":hashes})
        script.write_text(
          "import argparse,json,zc\n"
          "p=argparse.ArgumentParser(); p.add_argument('--extraction-output',required=True); "
          "p.add_argument('--research-skeleton',required=True); a=p.parse_args()\n"+
          f"json.dump({{'rows':{rows!r}}},open(a.extraction_output,'w'))\n"+
          f"json.dump({{'rows':{skeleton!r}}},open(a.research_skeleton,'w'))\n")
        argv=watchdog.governed_research_command(
          WRAPPER.resolve(),script.resolve(),out.resolve(),research.resolve())
        audit=self.r/"audit.json"
        written=subprocess.run([
          sys.executable,str(AUDIT_WRITER),"--output",str(audit),
          "--wrapper",str(WRAPPER),"--extractor",str(script),
          "--extraction-output",str(out),"--research-skeleton",str(research)],
          cwd=self.r,capture_output=True,text=True)
        if written.returncode:
            raise AssertionError(written.stderr)
        if pre_epoch:
            payload=json.loads(audit.read_text())
            payload["commands"][0]["epoch"]=999
            audit.write_text(json.dumps(payload))
        return out,research,argv,audit
    def research_call(self,shallow=False,pre_epoch=False,now="1100"):
        out,res,argv,audit=self.extraction(shallow,pre_epoch)
        result=self.call("research","--timegate",self.tg,"--receipt",self.r/"rr.json",
                         "--now-epoch",now,"--command-audit",audit,
                         "--extraction-output",out,"--research-skeleton",res,
                         "--ids",*self.ids,"--terms",*self.terms,
                         "--extractor",self.r/"extract.py","--wrapper",WRAPPER,
                         "--authorized-extractor-sha",sh(self.r/"extract.py"),
                         "--authorized-wrapper-sha",sh(WRAPPER))
        return result
    def full_config(self, engine, id_only=False, empty_payload=False):
        config=self.r/"config.json"; paths={k:str(self.r/k) for k in
          ["selection","research","outputRoot","firstProductReceipt","preclosure","manifest","closure"]}
        Path(paths["selection"]).write_text(json.dumps({"rows":[
          {"id":i,"term":t} for i,t in zip(self.ids,self.terms)]}))
        Path(paths["research"]).write_text(json.dumps({"rows":[
          {"id":i,"term":t} for i,t in zip(self.ids,self.terms)]}))
        entries=[{"id":i} if id_only else {"id":i,"term":t,
          "sourceDossier":{} if empty_payload else {"id":i,"term":t},
          "evidenceDraft":{} if empty_payload else {"Entry":{"Id":i,"SourceTerm":t,
            "Senses":[{"Occurrences":[{"Kwic":t}]}]}}}
                 for i,t in zip(self.ids,self.terms)]
        data={"schemaVersion":"generic-bounded-constructor-config.v2","cohort":"T","startedEpoch":1000,
              "timegatePath":str(self.tg),"watchdogReceiptPath":str(self.r/"start"),
              "commandAuditPath":str(self.r/"ca"),"engineSha256":sh(engine),"paths":paths,"entries":entries}
        config.write_text(json.dumps(data)); os.utime(config,(1200,1200)); return config
    def constructor_call(self,id_only=False,empty_payload=False,unauthorized=False,decorative=False,now="1309",overwrite=False):
        rr=self.r/"research-receipt.json"
        rr.write_text(json.dumps({"hardPass":True,"ids":self.ids,"terms":self.terms,
          "requiredFloors":self.floors,"admittedRequiredOccurrences":10,
          "adjudicatedCaseLoad":10,
          "deadlinesSeconds":self.deadlines}))
        marker=self.r/"invoked"; engine=self.r/"authorized-engine.py"
        engine.write_text(
          "import json,sys\n"+
          f"open({str(marker)!r},'w').write(json.dumps(sys.argv[1:]))\n")
        wrapper=self.r/"stable-wrapper.py"; shutil.copy2(WRAPPER,wrapper)
        os.utime(engine,(900,900)); os.utime(wrapper,(900,900))
        config=self.full_config(engine,id_only,empty_payload)
        supplied_engine=self.r/"unauthorized.py" if unauthorized else engine
        if unauthorized: supplied_engine.write_text("pass\n")
        argv=[str(Path(sys.executable).resolve()),str(wrapper),"--script",str(supplied_engine),"--",
              "--config",str(config.resolve()),"--allowed-build-root",str(self.r.resolve())]
        supplied=[sys.executable,str(wrapper),"--script",str(supplied_engine)] if decorative else []
        audit=self.r/"constructor-audit.json"; audit.write_text(json.dumps(
          {"complete":True,"commands":[{"epoch":1001,"argv":argv}]}))
        receipt=self.r/"constructor-receipt.json"
        if overwrite: receipt.write_text("{}")
        result=self.call("constructor","--timegate",self.tg,"--receipt",receipt,"--now-epoch",now,
          "--config",config,"--research-receipt",rr,"--ids",*self.ids,"--terms",*self.terms,
          "--command-audit",audit,"--engine",supplied_engine,"--wrapper",wrapper,"--allowed-root",self.r,
          "--authorized-engine-sha",sh(engine),"--authorized-wrapper-sha",sh(wrapper),*supplied)
        self.last_marker=marker
        return result

    def test_real_extraction_passes(self):
        result=self.research_call()
        self.assertEqual(0,result.returncode,result.stderr)
        receipt=json.loads((self.r/"rr.json").read_text())
        self.assertEqual(str((self.r/"extract.json").resolve()),receipt["extractionOutputPath"])
        self.assertEqual(sh(self.r/"extract.json"),receipt["extractionOutputSha256"])
        self.assertEqual(str((self.r/"audit.json").resolve()),receipt["commandAuditPath"])
        self.assertEqual(sh(self.r/"audit.json"),receipt["commandAuditSha256"])
        self.assertEqual(str((self.r/"extract.py").resolve()),receipt["extractorPath"])
        self.assertEqual(sh(self.r/"extract.py"),receipt["extractorSha256"])

    def test_audit_writer_launches_from_its_script_directory(self):
        out,res,argv,audit=self.extraction()
        second=self.r/"audit-from-script-cwd.json"
        result=subprocess.run([
          sys.executable,str(AUDIT_WRITER),"--output",str(second),
          "--wrapper",str(WRAPPER),"--extractor",str(self.r/"extract.py"),
          "--extraction-output",str(out),"--research-skeleton",str(res)],
          cwd=AUDIT_WRITER.parent,capture_output=True,text=True)
        self.assertEqual(0,result.returncode,result.stderr)
        self.assertEqual(argv,json.loads(second.read_text())["commands"][0]["argv"])

    def test_bare_extractor_audit_is_rejected_before_launch(self):
        out,res,argv,audit=self.extraction()
        audit.write_text(json.dumps({"complete":True,"commands":[{
          "epoch":1001,"argv":[sys.executable,str(self.r/"extract.py")]}]}))
        result=self.call("research","--timegate",self.tg,"--receipt",self.r/"rr.json",
          "--now-epoch","1100","--command-audit",audit,
          "--extraction-output",out,"--research-skeleton",res,
          "--ids",*self.ids,"--terms",*self.terms,
          "--extractor",self.r/"extract.py","--wrapper",WRAPPER,
          "--authorized-extractor-sha",sh(self.r/"extract.py"),
          "--authorized-wrapper-sha",sh(WRAPPER))
        self.assertEqual(124,result.returncode)
        self.assertIn("does not bind exact invoked argv",result.stderr)
        self.assertFalse(out.exists())
        self.assertFalse(res.exists())

    def test_caller_assembled_research_argv_is_not_a_cli_contract(self):
        out,res,argv,audit=self.extraction()
        result=self.call("research","--timegate",self.tg,"--receipt",self.r/"rr.json",
          "--now-epoch","1100","--command-audit",audit,
          "--extraction-output",out,"--research-skeleton",res,
          "--ids",*self.ids,"--terms",*self.terms,
          "--extractor",self.r/"extract.py","--wrapper",WRAPPER,
          "--authorized-extractor-sha",sh(self.r/"extract.py"),
          "--authorized-wrapper-sha",sh(WRAPPER),"--",*argv)
        self.assertEqual(2,result.returncode)
        self.assertFalse((self.r/"rr.json").exists())
    def test_n20_evidence_scaled_schedule(self):
        total,deadlines=watchdog.evidence_schedule([6,6,8],20)
        self.assertEqual(20,total)
        self.assertEqual({"viability":120,"researchExtraction":120,"adjudicatedConfig":420,
          "constructor":430,"firstProduct":450,"construction":510,"review":730,
          "correction":870,"publication":960},deadlines)
    def test_n23_and_n24_evidence_scaled_schedule(self):
        _, n23=watchdog.evidence_schedule([8,4,7],23)
        self.assertEqual((456,546,790,942,1032),
          tuple(n23[k] for k in ("adjudicatedConfig","construction","review","correction","publication")))
        _, n24=watchdog.evidence_schedule([8,8,8],24)
        self.assertEqual((468,558,810,966,1056),
          tuple(n24[k] for k in ("adjudicatedConfig","construction","review","correction","publication")))
    def test_schedule_positive_bounds_and_low_case_load(self):
        with self.assertRaises(TypeError): watchdog.evidence_schedule([8])
        with self.assertRaises(ValueError): watchdog.evidence_schedule([],0)
        with self.assertRaises(ValueError): watchdog.evidence_schedule([0],0)
        with self.assertRaises(ValueError): watchdog.evidence_schedule([True],1)
        with self.assertRaises(ValueError): watchdog.evidence_schedule([8,4,7],18)
        with self.assertRaises(ValueError): watchdog.evidence_schedule([8],True)
    def test_timegate_schedule_mismatch_rejected(self):
        gate=json.loads(self.tg.read_text()); gate["deadlinesSeconds"]["constructor"]=999
        self.tg.write_text(json.dumps(gate)); os.utime(self.tg,(1000,1000))
        self.assertEqual(124,self.research_call().returncode)
    def test_shallow_candidate_rejected(self): self.assertEqual(124,self.research_call(shallow=True).returncode)
    def test_pre_receipt_command_epoch_rejected(self): self.assertEqual(124,self.research_call(pre_epoch=True).returncode)
    def test_late_research_not_invoked(self): self.assertEqual(124,self.research_call(now="1120.1").returncode)
    def test_id_only_config_rejected(self): self.assertEqual(124,self.constructor_call(id_only=True).returncode)
    def test_empty_dossier_and_worksheet_rejected(self):
        self.assertEqual(124,self.constructor_call(empty_payload=True).returncode)
    def test_unauthorized_engine_rejected(self): self.assertEqual(124,self.constructor_call(unauthorized=True).returncode)
    def test_decorative_wrapper_arguments_rejected(self):
        self.assertEqual(124,self.constructor_call(decorative=True).returncode)
    def test_real_wrapper_launches_authorized_engine(self):
        result=self.constructor_call()
        self.assertEqual(0,result.returncode,result.stderr)
        self.assertEqual(["--config",str((self.r/"config.json").resolve()),
          "--allowed-build-root",str(self.r.resolve())],json.loads(self.last_marker.read_text()))
        receipt=json.loads((self.r/"constructor-receipt.json").read_text())
        self.assertEqual("construction-start-receipt.v1",receipt["schemaVersion"])
        self.assertEqual({"config","selection","research","command-audit"},
                         {row["kind"] for row in receipt["cohortArtifacts"]})

    def test_exact_governed_command_passes_generic_engine_start_contract(self):
        rr=self.r/"research-receipt.json"
        rr.write_text(json.dumps({"hardPass":True,"ids":self.ids,"terms":self.terms,
          "requiredFloors":self.floors,"admittedRequiredOccurrences":10,
          "adjudicatedCaseLoad":10,
          "deadlinesSeconds":self.deadlines}))
        engine=ROOT/"maintenance/generic_bounded_constructor.py"
        wrapper=WRAPPER
        config=self.full_config(engine)
        # Deliberately fail after the engine has accepted the governed argv,
        # start-receipt schema, and artifact hashes.
        selection=self.r/"selection"
        selection.write_text(json.dumps({"rows":[{"id":"wrong","term":"甲"}]}))
        receipt=self.r/"generic-start-receipt.json"
        argv=[str(Path(sys.executable).resolve()),str(wrapper),"--script",str(engine),"--",
              "--config",str(config.resolve()),"--allowed-build-root",str(self.r.resolve())]
        audit=self.r/"constructor-audit.json"
        audit.write_text(json.dumps({"complete":True,"commands":[{"epoch":1001,"argv":argv}]}))
        config_data=json.loads(config.read_text())
        config_data["watchdogReceiptPath"]=str(receipt)
        config_data["commandAuditPath"]=str(audit)
        config.write_text(json.dumps(config_data)); os.utime(config,(1200,1200))
        result=self.call("constructor","--timegate",self.tg,"--receipt",receipt,
          "--now-epoch","1249","--config",config,"--research-receipt",rr,
          "--ids",*self.ids,"--terms",*self.terms,"--command-audit",audit,
          "--engine",engine,"--wrapper",wrapper,"--allowed-root",self.r,
          "--authorized-engine-sha",sh(engine),"--authorized-wrapper-sha",sh(wrapper))
        self.assertEqual(124,result.returncode)
        self.assertIn("selection/research/config/watchdog IDs are not exactly equal and ordered",
                      result.stderr)
        self.assertNotIn("invalid watchdog receipt schema",result.stderr)
        start=json.loads(receipt.read_text())
        self.assertEqual("construction-start-receipt.v1",start["schemaVersion"])

    def test_real_watchdog_wrapper_engine_rc0_with_preexisting_output_root(self):
        started=time.time()
        term="弟子身纏風恙"
        ident="t_"+hashlib.sha256(term.encode()).hexdigest()[:12]
        floors=[4]; total,deadlines=watchdog.evidence_schedule(floors,4)
        tg=self.r/"real-timegate.json"
        tg.write_text(json.dumps({"startedEpoch":started,"artifactZero":True,
          "createdUtc":datetime.fromtimestamp(started,timezone.utc).isoformat(),
          "requiredFloors":floors,"admittedRequiredOccurrences":total,
          "adjudicatedCaseLoad":total,
          "deadlinesSeconds":deadlines}))
        os.utime(tg,(started,started))
        selection=self.r/"real-selection.json"
        selection.write_text(json.dumps({"rows":[{"id":ident,"term":term,"requiredFloor":4}]}))
        research=self.r/"real-research.json"
        research.write_text(json.dumps({"rows":[{"id":ident,"term":term}]}))
        output=self.r/"pre-existing-output-root"; output.mkdir()
        os.utime(output,(started-100,started-100))
        first=self.r/"real-first.json"; pre=self.r/"real-pre.json"
        manifest=self.r/"real-manifest.json"; closure=self.r/"real-closure.json"
        receipt=self.r/"real-constructor-receipt.json"
        rr=self.r/"real-research-receipt.json"
        rr.write_text(json.dumps({"hardPass":True,"ids":[ident],"terms":[term],
          "requiredFloors":floors,"admittedRequiredOccurrences":total,
          "adjudicatedCaseLoad":total,
          "deadlinesSeconds":deadlines}))
        engine=ROOT/"maintenance/generic_bounded_constructor.py"; wrapper=WRAPPER
        source=json.loads((ROOT/"maintenance/non-iriya-v7-depth-regeneration-r50-constructor-config-b.json").read_text())
        entry=json.loads(json.dumps(next(row for row in source["entries"] if row["id"]==ident)))
        entry["sourceDossier"].update({"requiredFloor":4,"semanticReadComplete":True,
          "tier3Lamp":0,"predecessorEvidenceAudit":[]})
        entry["evidenceDraft"]["Entry"]["CreatedBy"]="R50 real lifecycle test"
        entry["evidenceDraft"]["FamilyHarvest"]["Scope"]="R50 exact source-first family harvest"
        config=self.r/"real-config.json"; audit=self.r/"real-audit.json"
        paths={"selection":str(selection),"research":str(research),"outputRoot":str(output),
          "firstProductReceipt":str(first),"preclosure":str(pre),
          "manifest":str(manifest),"closure":str(closure)}
        config.write_text(json.dumps({"schemaVersion":"generic-bounded-constructor-config.v2",
          "cohort":"REAL","startedEpoch":started,"timegatePath":str(tg),
          "watchdogReceiptPath":str(receipt),"commandAuditPath":str(audit),
          "engineSha256":sh(engine),"paths":paths,"entries":[entry]}))
        argv=[str(Path(sys.executable).resolve()),str(wrapper),"--script",str(engine),"--",
          "--config",str(config.resolve()),"--allowed-build-root",str(self.r.resolve())]
        audit.write_text(json.dumps({"complete":True,"commands":[{"epoch":started+0.1,"argv":argv}]}))
        result=self.call("constructor","--timegate",tg,"--receipt",receipt,
          "--now-epoch",str(started+1),"--config",config,"--research-receipt",rr,
          "--ids",ident,"--terms",term,"--command-audit",audit,
          "--engine",engine,"--wrapper",wrapper,"--allowed-root",self.r,
          "--authorized-engine-sha",sh(engine),"--authorized-wrapper-sha",sh(wrapper))
        self.assertEqual(0,result.returncode,result.stderr)
        self.assertTrue(json.loads(receipt.read_text())["hardPass"])
        product=output/ident/"entry.v2.json"
        self.assertTrue(product.is_file())
        for path in (first,pre,manifest,closure):
            self.assertTrue(path.is_file(),path)
        self.assertEqual([ident],[row["id"] for row in json.loads(manifest.read_text())["rows"]])
    def test_late_config_and_constructor_rejected(self):
        self.assertEqual(124,self.constructor_call(now="1310.1").returncode)
    def test_receipt_overwrite_rejected(self): self.assertEqual(124,self.constructor_call(overwrite=True).returncode)

    def test_viability_count_ids_terms_must_match(self):
        selection=self.r/"selection.json"; union=self.r/"union.json"; count=self.r/"count.json"
        selection.write_text(json.dumps({"rows":[{"id":i,"term":t,"requiredFloor":f}
          for i,t,f in zip(self.ids,self.terms,self.floors)]}))
        union.write_text(json.dumps({"ids":[]}))
        count.write_text(json.dumps({"results":[{"id":"wrong","term":t,"hits":1} for t in self.terms]}))
        result=self.call("viability","--timegate",self.tg,"--receipt",self.r/"v.json",
          "--now-epoch","1080","--selection",selection,"--union",union,"--count",count,
          "--ids",*self.ids,"--terms",*self.terms)
        self.assertEqual(124,result.returncode)

    def test_timegate_without_artifact_zero_rejected(self):
        self.tg.write_text(json.dumps({"startedEpoch":1000}))
        self.assertEqual(124,self.research_call().returncode)

    def test_timegate_mtime_mismatch_rejected(self):
        os.utime(self.tg,(1002,1002))
        self.assertEqual(124,self.research_call().returncode)

    def test_artifact_before_actual_timegate_mtime_rejected(self):
        # The mounted workspace truncates mtimes to whole seconds: these are the
        # representable equivalents of artifact +0.1 versus timegate +0.9.
        os.utime(self.tg,(1001,1001))
        out,res,argv,audit=self.extraction()
        os.utime(audit,(1000,1000))
        result=self.call("research","--timegate",self.tg,"--receipt",self.r/"rr.json",
          "--now-epoch","1100","--command-audit",audit,"--extraction-output",out,
          "--research-skeleton",res,"--ids",*self.ids,"--terms",*self.terms,
          "--extractor",self.r/"extract.py","--wrapper",WRAPPER,
          "--authorized-extractor-sha",sh(self.r/"extract.py"),
          "--authorized-wrapper-sha",sh(WRAPPER))
        self.assertEqual(124,result.returncode)

    def test_empty_and_wrong_product_rejected(self):
        product=self.r/"entry.json"; product.write_text("{}")
        report=self.r/"report.json"; report.write_text(json.dumps({"hardPass":True,"outputSha256":sh(product)}))
        engine=self.r/"product-engine.py"; engine.write_text("pass\n")
        config=self.full_config(engine)
        result=self.call("first-product","--timegate",self.tg,"--receipt",self.r/"first.json",
          "--now-epoch","1260","--product",product,"--compiler-report",report,
          "--config",config,
          "--id",self.ids[0],"--term",self.terms[0],"--ids",*self.ids)
        self.assertEqual(124,result.returncode)

    def test_first_product_binds_config_and_configured_path(self):
        engine=self.r/"product-engine.py"; engine.write_text("pass\n")
        config=self.full_config(engine)
        product=self.r/"outputRoot"/self.ids[0]/"entry.v2.json"
        product.parent.mkdir(parents=True)
        product.write_text(json.dumps({"Id":self.ids[0],"SourceTerm":self.terms[0]}))
        os.utime(product,(1250,1250))
        report=self.r/"report.json"
        report.write_text(json.dumps({"hardPass":True,"outputSha256":sh(product)}))
        os.utime(report,(1251,1251))
        receipt=self.r/"first.json"
        result=self.call("first-product","--timegate",self.tg,"--receipt",receipt,
          "--now-epoch","1260","--product",product,"--compiler-report",report,
          "--config",config,"--id",self.ids[0],"--term",self.terms[0],"--ids",*self.ids)
        self.assertEqual(0,result.returncode,result.stderr)
        self.assertEqual(sh(config),json.loads(receipt.read_text())["configSha256"])

    def construction_call(self, empty=False, mismatch=False, now="1320"):
        output=self.r/"out"; rows=[]
        if not empty:
            for ident in self.ids:
                p=output/ident/"entry.v2.json"; p.parent.mkdir(parents=True,exist_ok=True); p.write_text("{}")
                rows.append({"id":ident,"productSha256":sh(p)})
        manifest=self.r/"manifest.json"; manifest.write_text(json.dumps({"rows":rows}))
        pre=self.r/"pre.json"; pre.write_text(json.dumps({"hardPass":True,"ids":self.ids}))
        close=self.r/"close.json"; close.write_text(json.dumps(
          {"hardPass":True,"manifestSha256":"bad" if mismatch else sh(manifest),"preclosureSha256":sh(pre)}))
        return self.call("construction","--timegate",self.tg,"--receipt",self.r/"done.json",
          "--now-epoch",now,"--manifest",manifest,"--preclosure",pre,"--closure",close,
          "--output-root",output,"--ids",*self.ids)
    def test_empty_manifest_rejected(self): self.assertEqual(124,self.construction_call(empty=True).returncode)
    def test_mismatched_closure_rejected(self): self.assertEqual(124,self.construction_call(mismatch=True).returncode)
    def test_late_construction_rejected(self): self.assertEqual(124,self.construction_call(now="1390.1").returncode)
    def test_valid_construction_passes(self): self.assertEqual(0,self.construction_call().returncode)

if __name__=="__main__": unittest.main()
