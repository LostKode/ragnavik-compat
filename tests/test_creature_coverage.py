import copy
import importlib.util
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('validator', ROOT / 'scripts/validate_creatures.py')
validator = importlib.util.module_from_spec(spec)
spec.loader.exec_module(validator)


class CreatureCoverageTests(unittest.TestCase):
    def setUp(self):
        self.rows = json.loads(validator.CATALOG.read_text())
        self.snapshot = json.loads(validator.SNAPSHOT.read_text())

    def test_complete_catalog(self):
        validator.validate(self.rows, self.snapshot)

    def test_reported_variants_are_required(self):
        for name in ('TentaRoot_wild', 'Skeleton_Meadows', 'Skeleton_Meadows_noarcher'):
            with self.subTest(name=name), self.assertRaisesRegex(ValueError, 'uncovered'):
                validator.validate([e for e in self.rows if e['name'] != name], self.snapshot)

    def test_case_sensitive_lookup(self):
        next(e for e in self.rows if e['name'] == 'TentaRoot_wild')['name'] = 'Tentaroot_wild'
        with self.assertRaisesRegex(ValueError, 'uncovered'):
            validator.validate(self.rows, self.snapshot)

    def test_upstream_duplicates_rejected(self):
        self.rows.append(copy.deepcopy(self.snapshot['upstream'][0]))
        with self.assertRaisesRegex(ValueError, 'upstream'):
            validator.validate(self.rows, self.snapshot)

    def test_duplicate_and_malformed_entries_rejected(self):
        variants = [self.rows + [self.rows[0]], self.rows + [{'name': 'invalid'}]]
        for key, value in [('level', True), ('level', 0), ('minExp', -1), ('maxExp', -1), ('name', 'TentaRoot_wild(Clone)')]:
            rows = copy.deepcopy(self.rows)
            rows[0][key] = value
            variants.append(rows)
        for rows in variants:
            with self.subTest(row=rows[0]), self.assertRaises(ValueError):
                validator.validate(rows, self.snapshot)

    def test_summons_and_offspring_cannot_grant_xp(self):
        for name in self.snapshot['zero_xp']:
            rows = copy.deepcopy(self.rows)
            next(e for e in rows if e['name'] == name)['maxExp'] = 1
            with self.subTest(name=name), self.assertRaisesRegex(ValueError, 'grants XP'):
                validator.validate(rows, self.snapshot)

    def test_sleeping_variants_match_parent(self):
        entries = {e['name']: e for e in self.rows + self.snapshot['upstream']}
        for name in ('Bjorn', 'Troll', 'Draugr', 'Draugr_Ranged', 'Draugr_Elite', 'Ghost'):
            for key in ('level', 'minExp', 'maxExp'):
                self.assertEqual(entries[name][key], entries[name + '_sleeping'][key])


if __name__ == '__main__':
    unittest.main()
