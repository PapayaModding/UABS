import importlib.util
import json
from pathlib import Path
import tempfile
import subprocess
import sys
import unittest
import xml.etree.ElementTree as ET

SPEC = importlib.util.spec_from_file_location('summary', Path(__file__).parents[1] / 'summarize.py')
summary = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(summary)
SHA = 'a' * 40
BASELINE = json.loads((Path(__file__).parents[1] / 'linux-baseline.json').read_text())
NS = 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'


class SummaryTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.environment = dict(verified=True, repository='PapayaModding/UABS', ref='refs/heads/v2-recovery', branch='v2-recovery', commit=SHA, expectedCommit=SHA)
        self.execution = dict(commit=SHA, startedUtc='test-start', exitCode=0)
        self.write_context()
        (self.root / 'console.log').write_text('Complete test console テスト', encoding='utf-8')

    def write_context(self):
        (self.root / 'environment.json').write_text(json.dumps(self.environment), encoding='utf-8')
        (self.root / 'execution.json').write_text(json.dumps(self.execution), encoding='utf-8')

    def trx(self, results=(('Known.Pass', 'Passed'),), outcome='Completed'):
        def tag(name): return f'{{{NS}}}{name}'
        root = ET.Element(tag('TestRun'))
        nodes = ET.SubElement(root, tag('Results'))
        for index, (name, verdict) in enumerate(results):
            row = ET.SubElement(nodes, tag('UnitTestResult'), testName=name, outcome=verdict, executionId=str(index), duration='00:00:01')
            error = ET.SubElement(ET.SubElement(row, tag('Output')), tag('ErrorInfo'))
            ET.SubElement(error, tag('Message')).text = 'Exact message テスト'
            ET.SubElement(error, tag('StackTrace')).text = 'Full diagnostic stack'
        status = ET.SubElement(root, tag('ResultSummary'), outcome=outcome)
        ET.SubElement(status, tag('Counters'), total=str(len(results)),
                      executed=str(sum(verdict != 'NotExecuted' for _, verdict in results)),
                      passed=str(sum(verdict == 'Passed' for _, verdict in results)),
                      failed=str(sum(verdict == 'Failed' for _, verdict in results)))
        directory = self.root / 'test-results'
        directory.mkdir(exist_ok=True)
        ET.ElementTree(root).write(directory / 'windows-full.trx', encoding='utf-8')

    def analyze(self):
        return summary.analyze(self.root, SHA, BASELINE, 'success', 'success')

    def test_complete_pass_and_explicit_skip_reporting(self):
        self.trx((('Known.Pass', 'Passed'), ('Known.Skip', 'NotExecuted')))
        result = self.analyze()
        self.assertEqual('PASS', result['status'])
        self.assertEqual(1, result['recordedPassed'])
        self.assertEqual(1, result['recordedSkipped'])
        self.assertEqual('Exact message テスト', result['skippedTests'][0]['message'])
        self.assertIn('not accepted as passes', summary.markdown(result))

    def test_failures_never_waived_by_linux_baseline(self):
        existing = BASELINE['failedTests'][0]
        self.trx(((existing, 'Failed'), ('Windows.OtherFailure', 'Failed')), 'Failed')
        self.execution['exitCode'] = 1
        self.write_context()
        result = self.analyze()
        self.assertEqual('FAIL', result['status'])
        self.assertEqual([existing], result['linuxComparison']['failedAlsoRecordedOnLinux'])
        self.assertEqual(['Windows.OtherFailure'], result['linuxComparison']['failedNotInRecordedLinuxSet'])
        self.assertEqual(['not-reported'], result['linuxComparison']['eachLinuxFailureWindowsOutcomes'][BASELINE['failedTests'][1]])
        self.assertEqual('Full diagnostic stack', result['failedTests'][0]['stackTrace'])

    def test_linux_failure_can_only_be_called_passed_when_recorded(self):
        existing = BASELINE['failedTests'][0]
        self.trx(((existing, 'Passed'),))
        self.assertEqual(['Passed'], self.analyze()['linuxComparison']['eachLinuxFailureWindowsOutcomes'][existing])

    def test_native_abort_with_partial_trx_and_blame(self):
        self.trx(outcome='Aborted')
        (self.root / 'console.log').write_text("terminate PAL_SEHException\nTest host process crashed\nTest Run Aborted", encoding='utf-8')
        (self.root / 'test-results' / 'test_Sequence.xml').write_text('<TestSequence><Test Name="Candidate" Completed="False" /></TestSequence>')
        self.execution['exitCode'] = 1
        self.write_context()
        result = self.analyze()
        self.assertEqual('FAIL', result['status'])
        self.assertEqual('aborted', result['executionState'])
        self.assertTrue(result['runAborted'])
        self.assertEqual([], result['abortedTests'])  # Never invent an individually aborted test.
        self.assertEqual('Candidate', result['inProgressFromBlame'][0]['attributes']['Name'])
        self.assertIn('PAL_SEHException', result['linuxComparison']['nativeSignalsObserved'])

    def test_individual_aborted_test_is_reported(self):
        self.trx((('Aborted.Case', 'Aborted'),), 'Aborted')
        result = self.analyze()
        self.assertEqual('FAIL', result['status'])
        self.assertEqual('Aborted.Case', result['abortedTests'][0]['name'])

    def test_missing_or_corrupt_trx_never_passes(self):
        self.assertEqual('BLOCKED', self.analyze()['status'])
        (self.root / 'test-results').mkdir()
        (self.root / 'test-results' / 'windows-full.trx').write_text('<broken')
        self.assertEqual('BLOCKED', self.analyze()['status'])

    def test_no_exit_receipt_or_different_commit_blocks(self):
        self.trx()
        for changes in ({'exitCode': None}, {'commit': 'b' * 40}, {'exitCode': True}):
            with self.subTest(changes=changes):
                self.execution = dict(commit=SHA, exitCode=0, startedUtc='start') | changes
                self.write_context()
                self.assertEqual('BLOCKED', self.analyze()['status'])

    def test_invalid_context_json_is_blocked_without_losing_report(self):
        self.trx()
        for value in ('null', '[]', '{broken'):
            with self.subTest(value=value):
                self.write_context()
                (self.root / 'execution.json').write_text(value)
                self.assertEqual('BLOCKED', self.analyze()['status'])
                self.write_context()
                (self.root / 'environment.json').write_text(value)
                self.assertEqual('BLOCKED', self.analyze()['status'])

    def test_wrong_branch_detached_unverified_or_sha_blocks(self):
        self.trx()
        for field, value in [('branch', 'main'), ('branch', ''), ('commit', 'b' * 40), ('verified', False), ('repository', 'other/UABS'), ('ref', 'refs/heads/main')]:
            with self.subTest(field=field, value=value):
                original = self.environment[field]
                self.environment[field] = value
                self.write_context()
                self.assertEqual('BLOCKED', self.analyze()['status'])
                self.environment[field] = original

    def test_counter_mismatch_empty_or_unknown_outcome_blocks(self):
        self.trx(())
        self.assertEqual('BLOCKED', self.analyze()['status'])
        self.trx((('Unknown.Case', 'SomethingElse'),))
        self.assertEqual('BLOCKED', self.analyze()['status'])
        self.trx()
        file = self.root / 'test-results' / 'windows-full.trx'
        file.write_text(file.read_text().replace('total="1"', 'total="2"'))
        self.assertEqual('BLOCKED', self.analyze()['status'])

    def test_nonzero_exit_failed_job_or_missing_console_cannot_pass(self):
        self.trx()
        self.execution['exitCode'] = 1
        self.write_context()
        self.assertEqual('FAIL', self.analyze()['status'])
        self.execution['exitCode'] = 0
        self.write_context()
        self.assertEqual('BLOCKED', summary.analyze(self.root, SHA, BASELINE, 'failure', 'success')['status'])
        self.assertEqual('BLOCKED', summary.analyze(self.root, SHA, BASELINE, 'success', 'cancelled')['status'])
        (self.root / 'console.log').write_text('')
        self.assertEqual('BLOCKED', self.analyze()['status'])
        (self.root / 'console.log').unlink()
        self.assertEqual('BLOCKED', self.analyze()['status'])

    def test_baseline_inventory_is_exact_and_not_a_filter(self):
        self.assertEqual(38, len(BASELINE['failedTests']))
        self.assertEqual(38, len(set(BASELINE['failedTests'])))
        workflow = (Path(__file__).parents[3] / '.github/workflows/windows-application-regression.yml').read_text()
        self.assertNotIn('--filter', workflow)
        self.assertNotIn('continue-on-error', workflow)
        self.assertNotIn('MaxParallelThreads', workflow)
        self.assertIn('--blame', workflow)
        self.assertIn('if: always()', workflow)

    def test_cli_writes_artifacts_and_preserves_failure_exit(self):
        self.trx()
        command = [sys.executable, str(Path(__file__).parents[1] / 'summarize.py'), '--evidence', str(self.root),
                   '--expected-commit', SHA, '--job-status', 'success', '--test-step-outcome', 'success',
                   '--step-summary', str(self.root / 'github-summary.md')]
        result = subprocess.run(command, capture_output=True, text=True, encoding='utf-8')
        self.assertEqual(0, result.returncode, result.stderr)
        report = json.loads((self.root / 'summary.json').read_text(encoding='utf-8'))
        self.assertEqual('PASS', report['status'])
        self.assertIn('test-results/windows-full.trx', {p.replace('\\', '/') for p in report['evidenceHashes']})
        self.assertTrue((self.root / 'summary.md').exists())
        self.assertIn('Full Windows application regression', (self.root / 'github-summary.md').read_text(encoding='utf-8'))
        self.execution['exitCode'] = 1
        self.write_context()
        result = subprocess.run(command, capture_output=True, text=True, encoding='utf-8')
        self.assertEqual(1, result.returncode)
        self.assertEqual('FAIL', json.loads((self.root / 'summary.json').read_text(encoding='utf-8'))['status'])


if __name__ == '__main__':
    unittest.main()
