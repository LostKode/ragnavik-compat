#!/usr/bin/env python3
"""Validate exact EpicMMO prefab coverage against the reviewed game snapshot."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / 'package/config/EpicMMOSystem/Ragnavik_AddedCreatures.json'
SNAPSHOT = ROOT / 'tests/fixtures/creature-coverage.json'


def validate(creatures, snapshot):
    if not isinstance(creatures, list) or not creatures:
        raise ValueError('creature database must be a nonempty array')
    names = set()
    for entry in creatures:
        if not isinstance(entry, dict) or set(entry) != {'name', 'minExp', 'maxExp', 'level'}:
            raise ValueError(f'invalid creature fields: {entry}')
        name = entry['name']
        if not isinstance(name, str) or not name or name.strip() != name or '(Clone)' in name:
            raise ValueError(f'invalid prefab name: {name!r}')
        if name in names:
            raise ValueError(f'duplicate prefab: {name}')
        names.add(name)
        if any(type(entry[key]) is not int for key in ('minExp', 'maxExp', 'level')):
            raise ValueError(f'non-integer progression values: {name}')
        if not 1 <= entry['level'] <= 100 or not 0 <= entry['minExp'] <= entry['maxExp']:
            raise ValueError(f'invalid progression values: {name}')
    upstream = {entry['name'] for entry in snapshot['upstream']}
    overlap = names & upstream
    if overlap:
        raise ValueError(f'upstream mappings must not be duplicated: {sorted(overlap)}')
    excluded = set(snapshot['excluded'])
    if names & excluded:
        raise ValueError(f'excluded entities mapped: {sorted(names & excluded)}')
    missing = set(snapshot['characters']) - names - upstream - excluded
    if missing:
        raise ValueError(f'uncovered game prefabs: {sorted(missing)}')
    required = set(snapshot['required_mod_prefabs']) | set(snapshot['zero_xp'])
    if required - names:
        raise ValueError(f'missing retained or protected prefab: {sorted(required - names)}')
    for entry in creatures:
        if entry['name'] in snapshot['zero_xp'] and (entry['minExp'] or entry['maxExp']):
            raise ValueError(f'protected creature grants XP: {entry["name"]}')


if __name__ == '__main__':
    validate(json.loads(CATALOG.read_text()), json.loads(SNAPSHOT.read_text()))
    print('Creature schema, game coverage, retained mod coverage and zero-XP protections valid.')
