from pathlib import Path
import json,hashlib,shutil,xml.etree.ElementTree as ET,struct
root=Path(__file__).resolve().parents[2];out=root/'Logs/ChapterSelection20261028'
run=json.loads((out/'active-run.json').read_text(encoding='utf-8'))
xml=Path(run['xml']);result=ET.parse(xml).getroot()
assert result.get('result')=='Passed',result.attrib
files=['Assets/_Project/Scripts/UI/MainMenuController.cs','Assets/_Project/Scripts/UI/LevelSelectionArtwork.cs','Assets/_Project/Resources/LevelSelectionArtwork.asset','Assets/_Project/Tests/PlayMode/ChapterSelectionPlayModeTests.cs','Assets/_Project/Tests/PlayMode/NumberedLevelSelectionPlayModeTests.cs','Assets/_Project/Tests/PlayMode/MainMenuSettingsPlayModeTests.cs','Assets/_Project/Tests/PlayMode/LevelScoringPlayModeTests.cs']
hashes={}
for relative in files:
 source=root/relative;other=root/'Logs/CodexTradingTests'/relative
 assert source.read_bytes()==other.read_bytes(),relative
 hashes[relative]=hashlib.sha256(source.read_bytes()).hexdigest()
 meta=Path(str(source)+'.meta');other_meta=Path(str(other)+'.meta')
 if not meta.exists() and other_meta.exists():shutil.copy2(other_meta,meta)
captures=Path(run['capture']);images={p.name:list(struct.unpack('>II',p.read_bytes()[16:24])) for p in captures.glob('*.png')}
assert len(images)==21,len(images)
for w,h in [(640,480),(1280,720),(2048,928)]:
 assert images[f'chapters-{w}x{h}.png']==[w,h]
summary=dict(date='2026-10-28',tests=result.attrib,source_sha256=hashes,capture_directory=str(captures),image_dimensions=images,chapters=3,level_counts=[5,4,4],playable_levels=5,preview_levels=8,existing_progress_keys_preserved=True,android_device_tested=False,apk_built=False,taptap_modified=False,isolated_profile='CellDefenseValidation / ChapterSelectionTests')
(root/'Docs/Testing/2026-10-28_ChapterSelection_summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
shutil.copy2(xml,root/'Docs/Testing/2026-10-28_ChapterSelection_playmode.xml')
shutil.copy2(out/'settings-before.asset',root/'Logs/CodexTradingTests/ProjectSettings/ProjectSettings.asset')
print(json.dumps(dict(passed=result.get('passed'),failed=result.get('failed'),screenshots=len(images),source_files_matched=len(hashes))))
