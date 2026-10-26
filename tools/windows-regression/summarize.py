"""Evidence reporting only. Never filters tests or waives known Linux failures."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

NS = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}


def analyze(root, expected_commit, baseline, job_status='', test_step_outcome=''):
    root = Path(root)
    problems = []

    def read_json(name):
        try:
            value = json.loads((root / name).read_text(encoding='utf-8-sig'))
            if not isinstance(value, dict):
                raise ValueError('Expected a JSON object')
            return value
        except (OSError, ValueError) as error:
            problems.append(f'{name}: {error}')
            return {}

    environment = read_json('environment.json')
    execution = read_json('execution.json')
    verified = (re.fullmatch('[0-9a-f]{40}', expected_commit or '') is not None
                and environment.get('verified') is True
                and environment.get('repository') == 'PapayaModding/UABS'
                and environment.get('ref') == 'refs/heads/v2-recovery'
                and environment.get('branch') == 'v2-recovery'
                and environment.get('commit') == expected_commit
                and environment.get('expectedCommit') == expected_commit)
    if not verified:
        problems.append('Exact recovery checkout evidence is absent or inconsistent.')
    receipt_valid = execution.get('commit') == expected_commit and type(execution.get('exitCode')) is int
    if not receipt_valid:
        problems.append('No valid final test-process exit receipt for this commit.')
    exit_code = execution.get('exitCode') if receipt_valid else None
    results, counters, outcome = [], {}, None
    trx = root / 'test-results' / 'windows-full.trx'
    try:
        tree = ET.parse(trx)
        summary = tree.find('t:ResultSummary', NS)
        if summary is None:
            raise ValueError('Missing ResultSummary')
        outcome = summary.get('outcome')
        counts = summary.find('t:Counters', NS)
        if counts is None:
            raise ValueError('Missing Counters')
        counters = {key: int(value) for key, value in counts.attrib.items()}
        for result in tree.findall('t:Results/t:UnitTestResult', NS):
            results.append(dict(name=result.get('testName'), outcome=result.get('outcome'),
                                executionId=result.get('executionId'), duration=result.get('duration'),
                                message=result.findtext('t:Output/t:ErrorInfo/t:Message', default='', namespaces=NS),
                                stackTrace=result.findtext('t:Output/t:ErrorInfo/t:StackTrace', default='', namespaces=NS)))
    except (OSError, ET.ParseError, ValueError) as error:
        problems.append(f'No valid complete TRX: {error}')
    console = ''
    try:
        console = (root / 'console.log').read_text(encoding='utf-8-sig', errors='replace')
        if not console.strip():
            problems.append('Console evidence is empty; capture completeness is not established.')
    except OSError as error:
        problems.append(f'Console evidence unavailable: {error}')
    native_signals = [signal for signal in ('PAL_SEHException', 'Test host process crashed', 'testhost process crashed') if signal in console]
    run_aborted = outcome == 'Aborted' or bool(native_signals) or 'Test Run Aborted' in console
    failed = [r for r in results if r['outcome'] == 'Failed']
    skipped = [r for r in results if r['outcome'] == 'NotExecuted']
    aborted = [r for r in results if r['outcome'] in ('Aborted', 'Error', 'Timeout')]
    run_aborted = run_aborted or bool(aborted) or counters.get('aborted', 0) > 0
    passed = [r for r in results if r['outcome'] == 'Passed']
    allowed = {'Passed', 'Failed', 'NotExecuted', 'Aborted', 'Error', 'Timeout'}
    coherent = (len(results) > 0 and counters.get('total') == len(results)
                and counters.get('passed') == len(passed) and counters.get('failed') == len(failed)
                and counters.get('executed') == len(results) - len(skipped)
                and all(r['name'] and r['outcome'] in allowed for r in results)
                and all(counters.get(key, 0) == 0 for key in ('error', 'timeout', 'aborted', 'notRunnable', 'disconnected', 'inProgress', 'pending', 'passedButRunAborted'))
                and len({r['executionId'] for r in results}) == len(results)
                and all(r['executionId'] for r in results))
    if not coherent:
        problems.append('Missing/incoherent test outcomes or counters; no complete-suite PASS.')
    in_progress = []
    for sequence in sorted((root / 'test-results').rglob('*Sequence.xml')):
        try:
            for test in ET.parse(sequence).iter():
                if test.get('Completed', '').lower() == 'false':
                    in_progress.append(dict(sequence=str(sequence.relative_to(root)), attributes=test.attrib))
        except (OSError, ET.ParseError) as error:
            problems.append(f'Blame sequence unavailable: {error}')
    if in_progress:
        problems.append('Blame sequence reports unfinished tests; inspect raw sequence, not a complete-suite PASS.')
    if run_aborted:
        execution_state = 'aborted'
    elif receipt_valid and outcome in ('Completed', 'Passed', 'Failed') and coherent:
        execution_state = 'completed'
    else:
        execution_state = 'incomplete' if execution.get('startedUtc') else 'not-run'
    if not verified:
        status = 'BLOCKED'
    elif failed or aborted or run_aborted or (exit_code is not None and exit_code != 0):
        status = 'FAIL'
    elif (exit_code == 0 and execution_state == 'completed' and outcome in ('Completed', 'Passed')
          and not problems and job_status == 'success' and test_step_outcome == 'success'):
        status = 'PASS'
    else:
        status = 'BLOCKED'
    linux_names = set(baseline['failedTests'])
    failures = {r['name'] for r in failed}
    observed = {}
    for result in results:
        observed.setdefault(result['name'], []).append(result['outcome'])
    comparison = dict(
        baseline=baseline,
        failedAlsoRecordedOnLinux=sorted(failures & linux_names),
        failedNotInRecordedLinuxSet=sorted(failures - linux_names),
        eachLinuxFailureWindowsOutcomes={name: observed.get(name, ['not-reported']) for name in sorted(linux_names)},
        nativeSignalsObserved=native_signals,
        caveat='No failures are waived. Different SHA/runtime/OS; missing or skipped tests are not passes. Blame in-progress entries do not establish crash causality.')
    return dict(status=status, commit=expected_commit, checkoutVerified=verified, executionState=execution_state,
                testExitCode=exit_code, jobStatus=job_status, testStepOutcome=test_step_outcome,
                trxOutcome=outcome, counters=counters, recordedPassed=len(passed), recordedFailed=len(failed),
                recordedSkipped=len(skipped), recordedAborted=len(aborted), runAborted=run_aborted,
                failedTests=failed, skippedTests=skipped, abortedTests=aborted, inProgressFromBlame=in_progress,
                allTestOutcomes=results, problems=problems, linuxComparison=comparison,
                checkpoint='Checkpoint 2.1 remains open; this is not UI or closure acceptance.')


def markdown(report):
    lines = [f"## Full Windows application regression: {report['status']}",
             f"Commit: `{report['commit']}`; execution: {report['executionState']}; exit: {report['testExitCode']}",
             f"Recorded: {report['recordedPassed']} passed, {report['recordedFailed']} failed, "
             f"{report['recordedSkipped']} skipped, {report['recordedAborted']} individually aborted/error/timeout.",
             f"Run aborted: {report['runAborted']}. An incomplete run has no known complete suite total."]
    for title, key in [('Failed tests', 'failedTests'), ('Skipped tests (not accepted as passes)', 'skippedTests'),
                       ('Individually aborted/error/timeout tests', 'abortedTests')]:
        lines += [f'### {title}']
        lines += [f"- {r['name']}: {r['message'].splitlines()[0] if r['message'] else r['outcome']}" for r in report[key]] or ['- None recorded.']
    comparison = report['linuxComparison']
    lines += ['### Recorded Linux comparison',
              f"Linux {comparison['baseline']['commit']}: 731 pass / 38 fail / 1 skip, PAL_SEHException abort.",
              f"Windows failures also recorded on Linux: {len(comparison['failedAlsoRecordedOnLinux'])}; "
              f"not in that recorded set: {len(comparison['failedNotInRecordedLinuxSet'])}.",
              f"Native crash signals in this console: {comparison['nativeSignalsObserved']}.", comparison['caveat'],
              'See summary.json for every baseline test outcome, all messages/stacks, and blame entries.',
              '### Evidence limitations', *[f'- {p}' for p in report['problems']], report['checkpoint']]
    return '\n\n'.join(lines) + '\n'


def main():
    # Preserve Unicode test names/diagnostics when piped through PowerShell on Windows.
    if hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8')
    parser = argparse.ArgumentParser()
    parser.add_argument('--evidence', type=Path, required=True)
    parser.add_argument('--expected-commit', required=True)
    parser.add_argument('--job-status', default='')
    parser.add_argument('--test-step-outcome', default='')
    parser.add_argument('--step-summary', type=Path)
    args = parser.parse_args()
    baseline = json.loads(Path(__file__).with_name('linux-baseline.json').read_text())
    report = analyze(args.evidence, args.expected_commit, baseline, args.job_status, args.test_step_outcome)
    report['evidenceHashes'] = {str(p.relative_to(args.evidence)): hashlib.sha256(p.read_bytes()).hexdigest()
                                for p in sorted(args.evidence.rglob('*')) if p.is_file() and p.suffix in ('.trx', '.log', '.xml', '.json')
                                and p.name != 'summary.json'}
    args.evidence.mkdir(parents=True, exist_ok=True)
    (args.evidence / 'summary.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    text = markdown(report)
    (args.evidence / 'summary.md').write_text(text, encoding='utf-8')
    if args.step_summary:
        with args.step_summary.open('a', encoding='utf-8') as stream:
            stream.write(text)
    print(text)
    return 0 if report['status'] == 'PASS' else 1


if __name__ == '__main__':
    raise SystemExit(main())
