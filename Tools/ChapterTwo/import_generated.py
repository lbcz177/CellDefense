"""Import verified generated chapter assets, changing existing files only at chapter links."""
from pathlib import Path
import json,re,shutil

root=Path(__file__).resolve().parents[2]
target=root/'Logs/ChapterTwoValidation'
record=json.loads((root/'Logs/ChapterTwo20261028/active-run.json').read_text())
assert record['mode']=='build'
assert 'CHAPTER_TWO_CONTENT_READY' in Path(record['log']).read_text(encoding='utf-8-sig',errors='ignore')
history=root/'Docs/History/2026-10-28_before_chapter_two'
def backup(path):
    destination=history/path.relative_to(root)
    if not destination.exists():
        destination.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(path,destination)

for folder in ['Assets/_Project/Data/Chapter02','Assets/_Project/Prefabs/Chapter02']:
    for source in (target/folder).rglob('*'):
        if source.is_file():
            destination=root/source.relative_to(target);destination.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,destination)
    source=target/(folder+'.meta')
    if source.exists() and not (root/(folder+'.meta')).exists():shutil.copy2(source,root/(folder+'.meta'))

settings=root/'ProjectSettings/EditorBuildSettings.asset';backup(settings)
text=settings.read_text(encoding='utf-8-sig')
for level in range(1,5):
    name=f'Chapter02Level{level:02}'
    path=f'Assets/_Project/Scenes/{name}.unity'
    for source in [target/path,target/(path+'.meta')]:shutil.copy2(source,root/source.relative_to(target))
    guid=re.search(r'^guid: (\w+)',(root/(path+'.meta')).read_text(),re.M)[1]
    if 'path: '+path not in text:text=text.replace('  m_configObjects:',f'  - enabled: 1\n    path: {path}\n    guid: {guid}\n  m_configObjects:')
settings.write_text(text,encoding='utf-8')

scene=root/'Assets/_Project/Scenes/OpenFieldLevel05.unity';backup(scene)
text=scene.read_text(encoding='utf-8-sig')
text,count=re.subn(r'^  nextSceneName:.*','  nextSceneName: Chapter02Level01',text,flags=re.M)
assert count==1;scene.write_text(text,encoding='utf-8')

catalog=root/'Assets/_Project/Resources/LevelSelectionArtwork.asset';backup(catalog)
text=catalog.read_text(encoding='utf-8-sig')
for level in range(1,5):
    pattern=r'(    - id: "2-'+str(level)+r'"\n      name: [^\n]*\n      sceneName:)[^\n]*'
    text,count=re.subn(pattern,lambda m:m[1]+f' Chapter02Level{level:02}',text)
    assert count==1,(level,count)
catalog.write_text(text,encoding='utf-8')
print('Imported 4 chapter-two scenes, two towers, elite and 23 waves; linked sequential progression.')
