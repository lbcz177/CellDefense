"""Archive the completed automatic-wave regression and its inspected screenshot."""
from pathlib import Path
import hashlib
import json
import shutil
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[2]
logs = root / 'Logs/ChapterSelection20261028'
target = root / 'Logs/CodexTradingTests'
evidence = root / 'Docs/Testing'
prefix = '2026-10-28_AutoWaves'
results = {}
for name, source in [('playmode', 'playmode-v16.xml'),
                     ('final-targeted', 'playmode-v17.xml'),
                     ('editmode', 'editmode-auto-waves.xml')]:
    path = logs / source
    result = ET.parse(path).getroot()
    assert result.attrib['result'] == 'Passed', (source, result.attrib)
    assert result.attrib['failed'] == '0' and result.attrib['skipped'] == '0'
    dest = evidence / f'{prefix}_{name}.xml'
    shutil.copy2(path, dest)
    results[name] = {key: result.attrib[key] for key in
                     ['result', 'total', 'passed', 'failed', 'skipped', 'start-time', 'end-time']}
    results[name]['source'] = str(path.relative_to(root))

files = [
    'Assets/_Project/Scripts/OpenField/OpenFieldPrototypeController.cs',
    'Assets/_Project/Scripts/UI/OpenFieldEntranceWaveButtons.cs',
    'Assets/_Project/Scripts/UI/FirstLevelTutorial.cs',
    'Assets/_Project/Tests/PlayMode/AutoWavePlayModeTests.cs',
    'Assets/_Project/Tests/PlayMode/FirstLevelTutorialPlayModeTests.cs',
]
hashes = {}
for name in files:
    source_hash = hashlib.sha256((root / name).read_bytes()).hexdigest()
    copied_hash = hashlib.sha256((target / name).read_bytes()).hexdigest()
    assert source_hash == copied_hash, f'Untested source change: {name}'
    hashes[name] = source_hash

art = root / 'Assets/_Project/Art/Sprites/细胞绘制/自动开波验证_2026-10-28_v1'
art.mkdir(parents=True, exist_ok=True)
image = '出兵后倒计时.png'
screenshot = root / 'Assets/_Project/Art/Sprites/细胞绘制/章节接入_2026-10-28/验证_v17~/auto-wave-countdown.png'
if (art / image).exists():
    assert (art / image).read_bytes() == screenshot.read_bytes(), 'Preserve existing screenshot'
else:
    shutil.copy2(screenshot, art / image)

summary = {'date': '2026-10-28', 'unity': '2022.3.53f1c1', 'results': results,
           'sha256': hashes, 'screenshot': str((art / image).relative_to(root)),
           'notes': 'Full PlayMode precedes two final tutorial copy edits; final targeted rerun covers those edits. No APK built or uploaded.'}
(evidence / f'{prefix}_summary.json').write_text(json.dumps(summary, indent=2, ensure_ascii=False), encoding='utf-8')
print(json.dumps({'results': results, 'sources_match': True}, ensure_ascii=False))
