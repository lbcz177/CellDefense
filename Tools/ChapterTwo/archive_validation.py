"""Keep reproducible chapter-two verification evidence in Docs/Testing."""
from pathlib import Path
import hashlib,json,re,shutil
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parents[2]
logs=root/'Logs/ChapterTwo20261028'
target=root/'Logs/ChapterTwoValidation'
evidence=root/'Docs/Testing'
prefix='2026-10-28_ChapterTwo'
results={}
for name,file in [('playmode','play-v5.xml'),('editmode','edit-v2.xml'),('targeted','play-v4.xml')]:
    source=logs/file;report=ET.parse(source).getroot()
    assert report.attrib['result']=='Passed',report.attrib
    assert report.attrib['failed']=='0' and report.attrib['skipped']=='0'
    shutil.copy2(source,evidence/f'{prefix}_{name}.xml')
    results[name]={k:report.attrib[k] for k in ['result','total','passed','failed','skipped','start-time','end-time']}
    results[name]['source']=str(source.relative_to(root))

files=[
 'Assets/_Project/Scripts/OpenField/OpenFieldPrototypeController.cs',
 'Assets/_Project/Scripts/OpenField/LevelProgress.cs',
 'Assets/_Project/Scripts/Data/WaveDefinition.cs',
 'Assets/_Project/Scripts/Data/EnemyDefinition.cs',
 'Assets/_Project/Scripts/Data/AntigenId.cs',
 'Assets/_Project/Scripts/Towers/TowerCombatState.cs',
 'Assets/_Project/Scripts/Towers/PlateletRepairBehaviour.cs',
 'Assets/_Project/Scripts/Towers/ComplementAttackBehaviour.cs',
 'Assets/_Project/Scripts/Towers/ChapterTwoLinkVisual.cs',
 'Assets/_Project/Scripts/UI/MainMenuController.cs',
 'Assets/_Project/Scripts/UI/OpenFieldBuildMenu.cs',
 'Assets/_Project/Scripts/UI/OpenFieldTowerMenu.cs',
 'Assets/_Project/Scripts/UI/OpenFieldEntranceWaveButtons.cs',
 'Assets/_Project/Scripts/UI/ChapterTwoBriefing.cs',
 'Assets/_Project/Editor/PlanningConfigWindow.cs',
 'Assets/_Project/Editor/PlanningConfigValidation.cs',
 'Assets/_Project/Editor/CellDefenseAndroidBuild.cs',
 'Assets/_Project/Editor/ChapterTwoContentBuilder.cs',
 'Assets/_Project/Resources/LevelSelectionArtwork.asset',
 'Assets/_Project/Resources/PromptImageResources.asset',
 'Assets/_Project/Tests/PlayMode/ChapterTwoPlayModeTests.cs',
 'ProjectSettings/EditorBuildSettings.asset',
 'Assets/_Project/Scenes/OpenFieldLevel05.unity',
]
files += [f'Assets/_Project/Scenes/Chapter02Level{i:02}.unity' for i in range(1,5)]
for folder in ['Assets/_Project/Data/Chapter02','Assets/_Project/Prefabs/Chapter02']:
    files += [str(p.relative_to(root)) for p in (root/folder).rglob('*') if p.is_file()]
hashes={}
for name in files:
    assert (root/name).read_bytes()==(target/name).read_bytes(),f'Untested source difference: {name}'
    hashes[name]=hashlib.sha256((root/name).read_bytes()).hexdigest()

log=(logs/'play-v5.log').read_text(encoding='utf-8-sig',errors='ignore')
defenses=[dict(scene=s,result=result,core_life=int(life),remaining_atp=int(atp),side_towers_built=int(count))
          for s,result,life,atp,count in re.findall(r'CHAPTER_TWO_DEFENSE (Chapter02Level\d+): (\w+), life=(\d+), atp=(\d+), built=(\d+)',log)]
assert len(defenses)==4 and all(d['result']=='Victory' for d in defenses)
summary={'unity':'2022.3.53f1c1','date':'2026-10-28','results':results,'sha256':hashes,
         'normal_resource_defenses':defenses,'art':'Assets/_Project/Art/Sprites/细胞绘制/第二章接入_2026-10-28_v1/验证',
         'limits':'Reused unit sprites; prototype balance; no APK rebuild, publishing, or Android device verification.'}
(evidence/f'{prefix}_summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'results':results,'normal_resource_defenses':defenses,'source_matches':True},ensure_ascii=False))
