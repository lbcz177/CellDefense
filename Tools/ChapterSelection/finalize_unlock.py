from pathlib import Path
import json,hashlib,shutil,xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[2];logs=root/'Logs/ChapterSelection20261028'
run=json.loads((logs/'active-run.json').read_text(encoding='utf-8'))
results={}
for mode,path in [('PlayMode',Path(run['xml'])),('EditMode',logs/'editmode-sequential.xml')]:
 result=ET.parse(path).getroot();assert result.get('result')=='Passed',(mode,result.attrib)
 results[mode]=result.attrib
 shutil.copy2(path,root/f'Docs/Testing/2026-10-28_SequentialUnlock_{mode.lower()}.xml')
files=['Assets/_Project/Scripts/OpenField/LevelProgress.cs','Assets/_Project/Scripts/OpenField/OpenFieldPrototypeController.cs','Assets/_Project/Scripts/UI/MainMenuController.cs','Assets/_Project/Tests/EditMode/LevelScoringTests.cs','Assets/_Project/Tests/PlayMode/ChapterSelectionPlayModeTests.cs','Assets/_Project/Tests/PlayMode/NumberedLevelSelectionPlayModeTests.cs']
hashes={}
for name in files:
 data=(root/name).read_bytes();assert data==(root/'Logs/CodexTradingTests'/name).read_bytes(),name
 hashes[name]=hashlib.sha256(data).hexdigest()
summary=dict(date='2026-10-28',tests=results,source_sha256=hashes,initially_available_levels=1,all_previous_clears_required=True,star_threshold_required=False,existing_records_preserved=True,preview_chapters_remain_nonplayable=True,captures=run['capture'],apk_built=False,taptap_modified=False)
(root/'Docs/Testing/2026-10-28_SequentialUnlock_summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
shutil.copy2(logs/'settings-before.asset',root/'Logs/CodexTradingTests/ProjectSettings/ProjectSettings.asset')
print(json.dumps({mode:data['passed'] for mode,data in results.items()}))
