"""Archive passing menu regressions and verify they tested the current paging assets."""
from pathlib import Path
import hashlib
import json
import shutil
import sys
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[2]
run = sys.argv[1] if len(sys.argv) > 1 else 'play-v2'
result = root / f'Logs/ChapterPaging20261029/{run}.xml'
xml = ET.parse(result).getroot()
assert xml.get('failed') == '0' and int(xml.get('passed', 0)) >= 13, xml.attrib
files = [
    'Assets/_Project/Scripts/UI/MainMenuController.cs',
    'Assets/_Project/Tests/PlayMode/ChapterPagingPlayModeTests.cs',
    'Assets/_Project/Resources/ScoreArtworkResources.asset',
]
font = root / 'Assets/_Project/Art/Sprites/细胞绘制/章节翻页_2026-10-29_v1/字图'
files += [str(path.relative_to(root)) for path in sorted(font.glob('*.png'))]
hashes = []
for filename in files:
    original, tested = root / filename, root / 'Logs/CodexTradingTests' / filename
    digest = lambda path: hashlib.sha256(path.read_bytes()).hexdigest()
    source_hash, tested_hash = digest(original), digest(tested)
    assert source_hash == tested_hash, filename
    hashes.append({'path': filename, 'sha256': source_hash, 'matches_tested_copy': True})
tests = [{'name': t.get('fullname'), 'result': t.get('result')} for t in xml.iter('test-case')]
report = {'date': '2026-10-29', 'result': xml.get('result'), 'total': int(xml.get('total')),
          'passed': int(xml.get('passed')), 'failed': int(xml.get('failed')), 'run': str(result.relative_to(root)),
          'files': hashes, 'tests': tests, 'screenshots': 12,
          'scope': 'Chapter paging, chapter hierarchy/progression/focus, numbered selection, original start button/menu focus. No APK build or publishing.'}
destination = root / 'Docs/Testing/2026-10-29_ChapterPaging'
shutil.copy2(result, Path(str(destination) + '_playmode.xml'))
Path(str(destination) + '_summary.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({key: report[key] for key in ['total', 'passed', 'failed', 'screenshots']}))
