# Hard bundle W1-B1 ledger

Status: **in progress**.

Scope is 150 occurrences across 58 sources from `worker-1-bundle-1-triage.json`, with exclusive entry ownership defined by `attribution-hard-bundles.json` worker 1.

## Current checkpoint

- Reviewed exact turns: 43/150.
- Signed sources: 8/58.
- Applied sources: 8/58.
- Exact verification successes: 43/150.
- The complete guide, `ATTRIBUTION_FIX.md`, hard-bundle manifest, triage, compiler, strict application tool, and the 58 paired workbooks/decision-sheet structure have been inspected.
- The first ten rows have explicit full decisions in `review-map.json` and their paired source sheets: all eight `B25n0144` rows and the first two `B25n0145` rows.
- Named decisions: Simha, Tengteng, Shenhui, Nanyue Huairang, Shi Zhicheng (two rows), Luopu Yuanan, Lu Gen, and Zhongfeng Mingben. The table-of-contents heading `卷第三拈古頌古` takes the impersonal XOR branch with concrete grammar evidence.
- The first row is not a structural blocker: the complete case explicitly says Simha addressed Vasiasita.
- No row is being counted as reviewed merely because its packet or title suggests a candidate.

Next action is the full signed compile/dry-run/apply/focused-gate/verification chain for completed source `B25n0144`; the ledger will be checkpointed again after that source application. No merge, commit, or push has been performed.

## Applied-source checkpoint: B25n0144

- Signed compile: 8/8 explicit overrides.
- Strict dry-run and apply: 8/8 prepared, zero failures.
- Focused stored-state comparison: 8/8.
- Exact XML replay with `zc.verify`, including stored `FromLb` and `ToLb`: 8/8.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-B25n0144.json` under `maintenance/source-workbooks/`.

## Twenty-row review checkpoint

- `B25n0145` is fully reviewed 5/5: Zhongfeng Mingben owns three exact turns, the TOC heading is impersonal, and `或者依文解義道` introduces an unnamed hypothetical interpreter rather than Zhongfeng's own definition.
- `B27n0152` is fully reviewed 3/3: Yulin Tongxiu owns the flower-smile recap, an unnamed monk owns the deliberating action, and named disciple Huiji Zhou owns the presentation of more than ten old-case comments.
- `J26nB178` is reviewed 4/7 and remains unsigned: Feiyin Tongrong owns three turns; the diamond-sword question belongs to an unnamed monk, with Feiyin recorded separately as respondent.

## Applied-source checkpoint: B25n0145

- Signed compile, strict dry-run, strict apply, focused stored-state gate, and exact XML replay all passed 5/5 with zero failures.
- The impersonal TOC and reviewed-unnamed hypothetical-interpreter XOR states survived the stored-state comparison exactly.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-B25n0145.json`.

## Applied-source checkpoint: B27n0152

- Signed compile, strict dry-run, strict apply, focused stored-state gate, and exact XML replay all passed 3/3 with zero failures.
- The exact-actor distinctions are preserved: Yulin Tongxiu's recap, the unnamed monk's deliberation, and Huiji Zhou's presentation of old-case comments.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-B27n0152.json`.

## Thirty-row review checkpoint

- `J26nB178` is complete 7/7. Feiyin Tongrong owns the record's own turns; the diamond-sword question remains with its unnamed monk; the incoming quoted appeal belongs to named correspondent Cao Xun rather than Feiyin.
- `J33nB294` is complete 2/2. Langting Jingting owns both his old-case comment and his direct formless-precept question; Miyun Yuanwu is contextual respondent/precept-conferrer in the latter.
- `J28nB202` is reviewed 5/9 and remains unsigned; all five are Baichi Yuan's own governing speech after raised-case boundaries were checked.

## Applied-source checkpoint: J26nB178

- Signed compile, strict dry-run, strict apply, focused stored-state gate, and exact XML replay all passed 7/7.
- Named correspondent Cao Xun, unnamed questioning monk, contextual respondents, and Feiyin Tongrong's own turns all match their explicit decisions.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-J26nB178.json`.

## Applied-source checkpoint: J33nB294

- Signed compile, strict dry-run, strict apply, focused stored-state gate, and exact XML replay all passed 2/2.
- Langting Jingting is exact actor in both rows; cited Yunmen and responding Tiantong remain contextual rather than displacing him.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-J33nB294.json`.

## Forty-row review checkpoint

- `J28nB202` is complete 9/9; all are Baichi Yuan's own addresses, essays, or inscriptions after section boundaries were checked.
- `C078n1720` is complete 4/4. Three verse authors were recovered from source-inline TEI attributions omitted by normalized KWIC extraction: Hongzhi Zhengjue (`天童覺`), Yuanwu Keqin (`圓悟勤`), and Fojian Huiqin (`佛鑑懃`). Nanyuan Huiyong owns the first headword-bearing turn of the simultaneous-pecking case.
- `J25nB171` is reviewed 2/5 and remains unsigned; both completed turns are Tianyin Yuanxiu's own hall speech.

## Applied-source checkpoint: J28nB202

- Signed compile and every downstream gate passed 9/9 with zero failures.
- Three Ten-Ox rows remain separate exact occurrences but correctly share Baichi Yuan as author of the self-herding essay and Ten-Ox inscription.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-J28nB202.json`.

## Applied-source checkpoint: C078n1720

- Signed compile and every downstream gate passed 4/4 with zero failures.
- Focused comparison confirms the TEI-note verse authors and Nanyuan Huiyong's case speech exactly; all four XML anchors replay with stored line ranges.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-C078n1720.json`.

## Applied-source checkpoint: J25nB171

- Signed compile and every downstream gate passed 5/5 with zero failures.
- Exact actors are Tianyin Yuanxiu (two rows), named questioner Lin Xuan, embedded-case speaker Baling Haojian, and quoted speaker Dahui Zonggao.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-J25nB171.json`.

## Fifty-row review checkpoint

- `J34nB311` is complete 6/6. Exact actors are preface author Xu Fang, Juelang Daosheng in four own-record turns/essays, and an unnamed questioning monk in the advance beginning `進云`.
- The heading `尊正鑒序` was checked against the surrounding juan structure: it is Juelang's preface to a named work, not an author named 尊正鑒.
- `J26nB187` is reviewed 1/5 and remains unsigned; Tian'an Sheng owns the winter informal-address comparison of meditation to carrying snow to fill a well.

## Applied-source checkpoint: J34nB311

- Signed compile, strict dry-run, strict apply, focused stored-state gate, and exact XML replay all passed 6/6.
- The gate confirms Xu Fang, Juelang Daosheng, and the reviewed-unnamed questioning monk remain distinct exact actors; every stored source span and line range verifies.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-J34nB311.json`.

## Applied-source checkpoint: J26nB187

- Signed compile and every downstream gate passed 5/5 with zero failures.
- All five stored lines are Tian'an Sheng's own winter or precept-seat speech; the paired `殺活` and `活人劍` rows share one fully checked four-turn formulation without collapsing their separate occurrences.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-J26nB187.json`.

## Sixty-row review checkpoint

- `J26nB188` is complete 2/2: both are Ruibai Mingxue's own exact turns.
- `J27nB189` is complete 1/1: the TEI author `明盂` resolves the title shorthand to Sanyi Mingyu.
- `J29nB223` and `J33nB285` are complete 1/1 each: in both, the headword occurs in an unnamed monk's question, not the record owner's reply.
- `J32nB276` is complete 1/1: Beichan Xian owns the explicitly introduced and quoted year-end white-ox address; Buhui Tongfa is only its later raiser.

## Applied-source checkpoint: J26nB188

- Ruibai Mingxue's two exact turns passed signed compile, dry-run, apply, focused stored-state comparison, and exact XML replay 2/2.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-J26nB188.json`.

## Applied-source checkpoint: J27nB189

- Sanyi Mingyu's exact invitational-address turn passed all gates 1/1; the stored name follows TEI author `明盂`, not the truncated title character alone.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-J27nB189.json`.

## Applied-source checkpoint: J29nB223

- The reviewed-unnamed questioning monk and contextual respondent Shanhui passed all gates 1/1; the headword remains attributed to the monk's question rather than the master's answering blow.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-J29nB223.json`.

## Applied-source checkpoint: J32nB276

- Beichan Xian's explicitly quoted year-end address passed all gates 1/1; Buhui Tongfa remains the contextual later raiser, not the quote's speaker.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-J32nB276.json`.

## Applied-source checkpoint: J33nB285

- The reviewed-unnamed monk's question and Doushuai Bulin Jian's contextual response passed all gates 1/1.
- All 60 reviewed rows are now applied and exactly XML-verified.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-J33nB285.json`.

## Seventy-row review checkpoint

- `J27nB190` is complete 4/4: the first exact quote belongs to Shiyu Mingfang's teacher Zhanran Yuancheng; the remaining three are Shiyu's own turns.
- `J26nB177` is complete 3/3: Poshan Haiming owns the public answer, reply-letter, and signed self-reference as `病僧` in the critique of a Juyun address.
- `J25nB163` is complete 3/3: Guting Shanjian owns two precept-address lines and his comment on Luopu's final instruction.

## Applied-source checkpoint: J27nB190

- All four rows passed the complete chain and exact XML replay 4/4; Zhanran Yuancheng's quoted command remains distinct from Shiyu Mingfang's framing address.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-J27nB190.json`.

## Applied-source checkpoint: J26nB177

- Poshan Haiming's public answer, reply-letter, and authorial critique passed all gates and exact XML replay 3/3.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-J26nB177.json`.

## Applied-source checkpoint: J25nB163

- Guting Shanjian's three exact turns passed all gates and exact XML replay 3/3.
- All 70 reviewed rows are now applied and exactly XML-verified.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-J25nB163.json`.

## Eighty-row review checkpoint

- `J27nB198` is complete 7/7: Baling Haojian owns the embedded silver-bowl answer; two headwords belong to unnamed monk questions; Xueguan Zhiyin owns the remaining speech, prose, and titled verse.
- `J23nB128` and `J23nB129` are complete 1/1 each: `任運` is an impersonal Ten Oxherding document heading, with Puming preserved only as the following signed verse author.
- `J23nB134` is complete 1/1: Linji Yixuan owns the answer that repeats Mayu Baoche's true-eye question and commands an immediate response.

## Applied-source checkpoint: J27nB198

- All seven exact decisions passed the complete chain and exact XML replay 7/7, including Baling's quoted answer, two reviewed-unnamed questioners, and Xueguan's own speech/prose/verse.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-J27nB198.json`.

## Applied-source checkpoint: J23nB128

- The impersonal Ten Oxherding heading and contextual Puming verse author passed all gates and exact XML replay 1/1.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-J23nB128.json`.

## Applied-source checkpoint: J23nB129

- The expanded line span's Nayu'an signature, impersonal seventh-stage heading, and Puming signature were kept distinct; all gates and exact XML replay passed 1/1.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-J23nB129.json`.

## Applied-source checkpoint: J23nB134

- Linji Yixuan's exact answer and contextual Mayu Baoche question passed all gates and exact XML replay 1/1.
- All 80 reviewed rows are now applied and exactly XML-verified.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-J23nB134.json`.

## Ninety-row review checkpoint

- `D48n8939` is complete 5/5: Foyan Qingyuan owns three teaching turns, Nanyuan Huiyong owns the quoted thousand-fathom wall, and Dasui Fazhen owns the repeated answer 'go along with it.'
- `J10nA158` is complete 3/3: Miyun Yuanwu owns two hall-address lines; his root teacher Huanyou Zhengchuan owns the paired donkey-leg/horse-leg questions.
- `J25nB174` is complete 2/2: Juelang Daosheng owns his own third-phrase joke; Baling Haojian owns the embedded silver-bowl answer in Juelang's old-case verse.

## Applied-source checkpoint: D48n8939

- All five anthology decisions passed the complete chain and exact XML replay 5/5, preserving Foyan, Nanyuan, Langya as raiser, and Dasui's exact roles.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-D48n8939.json`.

## Applied-source checkpoint: J10nA158

- Miyun Yuanwu's two turns and Huanyou Zhengchuan's paired questions passed all gates and exact XML replay 3/3.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-J10nA158.json`.

## Applied-source checkpoint: J25nB174

- Juelang Daosheng's own turn and Baling Haojian's embedded answer passed all gates and exact XML replay 2/2.
- All 90 reviewed rows are now applied and exactly XML-verified.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-J25nB174.json`.

## One-hundred-row review checkpoint

- `J28nB208` is complete 3/3 under Guxue Zhenzhe's own hall speech.
- `J27nB193` is complete 3/3: Yinyuan Longqi owns two turns; Huanglong Huinan owns the quoted donkey-leg question.
- `J26nB186` is complete 2/2 under Linye Tongqi's exact replies.
- `J29nB244` is complete 2/2 under Sanshan Denglai's exact hall speech and public answer.

## Applied-source checkpoint: J28nB208

- Guxue Zhenzhe's three exact hall-speech turns passed all gates and exact XML replay 3/3.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-J28nB208.json`.

## Applied-source checkpoint: J27nB193

- Yinyuan Longqi's two turns and Huanglong Huinan's embedded third-barrier question passed all gates and exact XML replay 3/3.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-J27nB193.json`.

## Applied-source checkpoint: J26nB186

- Linye Tongqi's two exact replies passed all gates and exact XML replay 2/2.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-J26nB186.json`.

## Applied-source checkpoint: J29nB244

- Sanshan Denglai's two exact turns passed all gates and exact XML replay 2/2.
- All 100 reviewed rows are now applied and exactly XML-verified.
- Artifacts: `hard-w1-b1-{compiled,dryrun,applied,focused}-J29nB244.json`.

## One-hundred-ten-row review checkpoint

- Nine sources are complete: `J25nB159`, `J26nB182`, `J26nB185`, `J27nB192`, `J27nB196`, `J27nB197`, `J28nB205`, `J28nB206`, and `J28nB211`.
- Exact attribution recovers three signed non-record-owner authors from colophons: Gao Shitai, Chen Zhishen, and Zhang Zhu.
- The `J27nB192` barrier question remains with its reviewed-unnamed monk; Daxiu Zhu is the contextual respondent and owns the separate Dharma-transmission instruction.

## Applied-source checkpoint: J25nB159

- Dufeng Benshan's exact instruction passed all gates and XML replay 1/1.

## Applied-source checkpoint: J26nB182

- Gao Shitai's signed tower inscription passed all gates and XML replay 1/1.

## Applied-source checkpoint: J26nB185

- Fushi Tongxian's exact hall speech passed all gates and XML replay 1/1.

## Applied-source checkpoint: J27nB192

- The unnamed questioner and Daxiu Zhu's exact instruction passed all gates and XML replay 2/2.

## Applied-source checkpoint: J27nB196

- Yuanhu Miaoyong's exact reply-letter prose passed all gates and XML replay 1/1.

## Applied-source checkpoint: J27nB197

- Wuyi Yuanlai's exact public answer passed all gates and XML replay 1/1.

## Applied-source checkpoint: J28nB205

- Jie Weizhou's exact letter prose passed all gates and XML replay 1/1.

## Applied-source checkpoint: J28nB206

- Chen Zhishen's signed preface passed all gates and XML replay 1/1.

## Applied-source checkpoint: J28nB211

- Zhang Zhu's signed preface passed all gates and XML replay 1/1. All artifacts for these nine sources use the standard `hard-w1-b1-{compiled,dryrun,applied,focused}-<source>.json` names.

## One-hundred-twenty-row review checkpoint

- Nine sources are complete: `J28nB212`, `J28nB218`, `J28nB219`, `J28nB220`, `J29nB224`, `J29nB233`, `J29nB236`, `J29nB238`, and `J29nB249`.
- Exact speakers are the record owners except the final `J29nB249` row, where `恁麼則` belongs to an unnamed monk's advance and Fangrong Xi remains the contextual respondent.

## Applied-source checkpoints: 111–120

- `J28nB212` 1/1, `J28nB218` 2/2, `J28nB219` 1/1, `J28nB220` 1/1, `J29nB224` 1/1, `J29nB233` 1/1, `J29nB236` 1/1, `J29nB238` 1/1, and `J29nB249` 1/1 each passed signed compile, strict dry-run, apply, focused stored-state comparison, and exact XML replay.
- Per-source artifacts use `hard-w1-b1-{compiled,dryrun,applied,focused}-<source>.json`; all 120 reviewed rows are now applied and exactly verified.

## One-hundred-thirty-row review checkpoint

- Nine sources are complete: `J33nB286`, `J34nB299`, `J34nB300`, `J34nB305`, `J34nB306`, `J35nB336`, `J35nB337`, `J35nB342`, and `J36nB352`.
- Embedded exact speech remains with Huanglong Huinan, Linji Yixuan, and Zhaozhou Congshen where their words carry the headword; record owners are contextual raisers or commentators there.

## Applied-source checkpoints: 121–130

- `J33nB286` 1/1, `J34nB299` 1/1, `J34nB300` 1/1, `J34nB305` 2/2, `J34nB306` 1/1, `J35nB336` 1/1, `J35nB337` 1/1, `J35nB342` 1/1, and `J36nB352` 1/1 each passed signed compile, strict dry-run, apply, focused stored-state comparison, and exact XML replay.
- Per-source artifacts use the standard hard-bundle names; all 130 reviewed rows are applied and exactly verified.

## One-hundred-fifty-row review checkpoint

- All 150 occurrences across all 58 sources now have explicit full decisions; every decision sheet is signed and every row has a non-null override.
- The final source review resolves Baiyu Jingsi's own hall address in `J36nB359` and all nineteen `C077n1710` rows by exact turn, including embedded quotations, biography grammar, two genuinely unnamed interlocutors, and the section owners named by the XML hierarchy.
- Applied and exact-XML-verified state remains honestly checkpointed at 130/150 until the final two source application chains pass.

## Final applied-source checkpoints: J36nB359 and C077n1710

- `J36nB359` passed 1/1: Baiyu Jingsi's own hall address is now keyed to him from the book title, fascicle title, temple-record section, and complete first-person turn.
- `C077n1710` passed 19/19 across eighteen entries: exact actors include Nanyue Huairang, Mazu Daoyi, Baizhang Huaihai, Yangshan Huiji, Huangbo Xiyun, Fengxue Yanzhao, Shimen Yuncong, Guishan Lingyou, Yunmen Wenyan, Wuchu Daguan's quoted Mazu, Linji Yixuan, Foyan Qingyuan, and Yunfeng Wenyue; two genuinely unnamed speakers retain full six-rung reviewed-unnamed records.
- Both sources passed signed compile, strict dry-run, apply, focused stored-state comparison, and direct `zc.verify` replay with exact stored `FromLb`/`ToLb` matches.

## Bundle complete

- 150/150 occurrences reviewed, applied, focused-gated, and directly XML-verified.
- 58/58 source sheets signed; 58/58 applied and focused reports passed with zero failures.
- No merge, commit, or push was performed.
