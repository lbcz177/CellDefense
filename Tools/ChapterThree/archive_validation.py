from pathlib import Path
import xml.etree.ElementTree as ET
import hashlib,json,re,shutil
r=Path(__file__).resolve().parents[2];logs=r/'Logs/ChapterThree20261029';target=r/'Logs/CodexTradingTests';dest=r/'Docs/Testing'
prefix='2026-10-29_ChapterThree';results={}
for kind,name in [('playmode','play-v3.xml'),('editmode','edit-v1.xml'),('targeted','play-v2.xml')]:
    report=ET.parse(logs/name).getroot();assert report.attrib['result']=='Passed',report.attrib
    assert report.attrib['failed']=='0' and report.attrib['skipped']=='0'
    shutil.copy2(logs/name,dest/f'{prefix}_{kind}.xml')
    results[kind]={k:report.attrib[k] for k in ['result','total','passed','failed','skipped','start-time','end-time']}
files=[
 'Assets/_Project/Scripts/OpenField/LevelProgress.cs','Assets/_Project/Scripts/OpenField/OpenFieldPrototypeController.cs',
 'Assets/_Project/Scripts/OpenField/ChapterThreeInflammation.cs','Assets/_Project/Scripts/OpenField/InflammationRegionState.cs',
 'Assets/_Project/Scripts/Towers/ImmuneSupportBehaviour.cs','Assets/_Project/Scripts/Towers/TowerController.cs',
 'Assets/_Project/Scripts/Towers/AntibodyAttackBehaviour.cs','Assets/_Project/Scripts/Data/EnemyDefinition.cs',
 'Assets/_Project/Scripts/Data/AntigenId.cs','Assets/_Project/Scripts/UI/OpenFieldBuildMenu.cs',
 'Assets/_Project/Editor/ChapterThreeContentBuilder.cs','Assets/_Project/Editor/CellDefenseAndroidBuild.cs',
 'Assets/_Project/Editor/PlanningConfigWindow.cs','Assets/_Project/Tests/EditMode/InflammationStateTests.cs',
 'Assets/_Project/Tests/PlayMode/ChapterThreePlayModeTests.cs','Assets/_Project/Tests/PlayMode/ChapterTwoPlayModeTests.cs',
 'Assets/_Project/Tests/PlayMode/ChapterSelectionPlayModeTests.cs',
 'Assets/_Project/Resources/LevelSelectionArtwork.asset','Assets/_Project/Resources/PromptImageResources.asset',
 'Assets/_Project/Scenes/Chapter02Level04.unity','ProjectSettings/EditorBuildSettings.asset']
files += [f'Assets/_Project/Scenes/Chapter03Level{i:02}.unity' for i in range(1,5)]
for folder in ['Assets/_Project/Data/Chapter03','Assets/_Project/Prefabs/Chapter03']:
    files += [str(p.relative_to(r)) for p in (r/folder).rglob('*') if p.is_file()]
files += [str(p.relative_to(r)) for p in (r/'Assets/_Project/Art/Sprites/细胞绘制/第三章接入_2026-10-29_v1/字图').glob('*.png')]
hashes={}
for f in files:
    assert (r/f).read_bytes()==(target/f).read_bytes(),f
    hashes[f]=hashlib.sha256((r/f).read_bytes()).hexdigest()
text=(logs/'play-v3.log').read_text(encoding='utf-8-sig',errors='ignore')
defenses=[dict(scene=s,result=v,life=int(life),atp=int(atp),side_towers=int(built))
    for s,v,life,atp,built in re.findall(r'CHAPTER_THREE_DEFENSE (Chapter03Level\d+): (\w+), life=(\d+), atp=(\d+), built=(\d+)',text)]
assert len(defenses)==4 and all(d['result']=='Victory' for d in defenses)
(dest/f'{prefix}_summary.json').write_text(json.dumps(dict(date='2026-10-29',unity='2022.3.53f1c1',results=results,sha256=hashes,
    normal_resource_defenses=defenses,limits='Prototype art and balance; no Android device check, APK rebuild or publishing.'),ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(dict(results=results,defenses=defenses,source_matches=True),ensure_ascii=False))
