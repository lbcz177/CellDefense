from pathlib import Path
import json,hashlib,shutil,xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[2];logs=root/'Logs/ChapterSelection20261028'
run=json.loads((logs/'active-run.json').read_text(encoding='utf-8'))
result=ET.parse(run['xml']).getroot();assert result.get('result')=='Passed',result.attrib
files=['Assets/_Project/Scripts/UI/MainMenuController.cs','Assets/_Project/Tests/PlayMode/ChapterSelectionPlayModeTests.cs']
hashes={}
for name in files:
 data=(root/name).read_bytes();assert data==(root/'Logs/CodexTradingTests'/name).read_bytes(),name
 hashes[name]=hashlib.sha256(data).hexdigest()
images=list(Path(run['capture']).glob('*.png'));assert len(images)==27,len(images)
summary=dict(date='2026-10-28',tests=result.attrib,source_sha256=hashes,capture_directory=run['capture'],screenshots=len(images),
    resolutions=['640x480','1280x720','2048x928'],modes=['desktop','mobile safe area'],
    three_cards_fully_inside_viewport=True,non_overlapping=True,all_three_buttons_raycast_and_navigation_verified=True,
    chapter_scrolling_disabled=True,apk_built=False,taptap_modified=False)
out=root/'Docs/Testing'
(out/'2026-10-28_ChapterCardsFit_summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
shutil.copy2(run['xml'],out/'2026-10-28_ChapterCardsFit_playmode.xml')
shutil.copy2(logs/'settings-before.asset',root/'Logs/CodexTradingTests/ProjectSettings/ProjectSettings.asset')
print(json.dumps(dict(passed=result.get('passed'),captures=len(images),source_matches=True)))
