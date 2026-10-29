from pathlib import Path
import json,re,shutil
root=Path(__file__).resolve().parents[2];target=root/'Logs/CodexTradingTests'
record=json.loads((root/'Logs/ChapterThree20261029/active-run.json').read_text())
assert record['mode']=='build' and 'CHAPTER_THREE_CONTENT_READY' in Path(record['log']).read_text(encoding='utf-8-sig',errors='ignore')
history=root/'Docs/History/2026-10-29_before_chapter_three'
def backup(path):
    dest=history/path.relative_to(root)
    if not dest.exists():dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(path,dest)
for folder in ['Assets/_Project/Data/Chapter03','Assets/_Project/Prefabs/Chapter03']:
    for p in (target/folder).rglob('*'):
        if p.is_file():
            dest=root/p.relative_to(target);dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
    if (target/(folder+'.meta')).exists():shutil.copy2(target/(folder+'.meta'),root/(folder+'.meta'))
settings=root/'ProjectSettings/EditorBuildSettings.asset';backup(settings);text=settings.read_text(encoding='utf-8-sig')
for i in range(1,5):
    name=f'Chapter03Level{i:02}';path=f'Assets/_Project/Scenes/{name}.unity'
    for p in [target/path,target/(path+'.meta')]:shutil.copy2(p,root/p.relative_to(target))
    guid=re.search(r'^guid: (\w+)',(root/(path+'.meta')).read_text(),re.M)[1]
    if 'path: '+path not in text:text=text.replace('  m_configObjects:',f'  - enabled: 1\n    path: {path}\n    guid: {guid}\n  m_configObjects:')
settings.write_text(text,encoding='utf-8')
p=root/'Assets/_Project/Scenes/Chapter02Level04.unity';backup(p)
text,n=re.subn(r'^  nextSceneName:.*','  nextSceneName: Chapter03Level01',p.read_text(encoding='utf-8-sig'),flags=re.M);assert n==1;p.write_text(text,encoding='utf-8')
p=root/'Assets/_Project/Resources/LevelSelectionArtwork.asset';backup(p);text=p.read_text(encoding='utf-8-sig')
for i in range(1,5):
    text,n=re.subn(r'(    - id: "3-'+str(i)+r'"\n      name: [^\n]*\n      sceneName:)[^\n]*',lambda m:m[1]+f' Chapter03Level{i:02}',text);assert n==1
p.write_text(text,encoding='utf-8');print('Imported chapter three: four levels, 21 waves, two support towers, local inflammation.')
