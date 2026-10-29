"""Prepare an isolated Unity project and launch chapter-two build/tests."""
from pathlib import Path
import json, re, shutil, subprocess, sys, os, uuid

root=Path(__file__).resolve().parents[2]
target=Path(os.environ.get('CELLDEFENSE_CHAPTER_TWO_PROJECT',str(root/'Logs/ChapterTwoValidation')))
out=Path(os.environ.get('CELLDEFENSE_VALIDATION_LOG_DIR',str(root/'Logs/ChapterTwo20261028')));out.mkdir(parents=True,exist_ok=True)
mode=sys.argv[1]
def copy(source):
    dest=target/source.relative_to(root);dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,dest)
    meta=Path(str(source)+'.meta')
    if meta.exists():shutil.copy2(meta,Path(str(dest)+'.meta'))

for directory in ['Packages','ProjectSettings','Assets/TextMesh Pro','Assets/_Project/Scripts','Assets/_Project/Tests','Assets/_Project/Editor','Assets/_Project/Data','Assets/_Project/Prefabs','Assets/_Project/Scenes','Assets/_Project/Resources']:
    for source in (root/directory).rglob('*'):
        if source.is_file() and not any(p.endswith('~') for p in source.relative_to(root).parts):
            if source.suffix=='.cs' and not Path(str(source)+'.meta').exists():
                Path(str(source)+'.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf-8')
            copy(source)

# Resolve referenced art, animation, materials, scripts and package metadata transitively.
guid_map={}
for meta in (root/'Assets').rglob('*.meta'):
    if any(p.endswith('~') for p in meta.relative_to(root).parts):continue
    match=re.search(r'^guid: (\w+)',meta.read_text(encoding='utf-8-sig',errors='ignore'),re.M)
    if match:guid_map[match[1]]=Path(str(meta)[:-5])
queue=[p for p in (target/'Assets').rglob('*') if p.suffix in ['.asset','.prefab','.unity','.mat','.controller','.anim']]
seen=set()
while queue:
    current=queue.pop()
    for guid in re.findall(r'guid: (\w+)',current.read_text(encoding='utf-8-sig',errors='ignore')):
        if guid in seen:continue
        seen.add(guid);source=guid_map.get(guid)
        if not source or not source.is_file():continue
        copy(source)
        if source.suffix in ['.asset','.prefab','.unity','.mat','.controller','.anim']:queue.append(source)

settings=target/'ProjectSettings/ProjectSettings.asset'
text=settings.read_text(encoding='utf-8-sig')
text=re.sub(r'^  companyName:.*','  companyName: CellDefenseValidation',text,flags=re.M)
text=re.sub(r'^  productName:.*','  productName: ChapterTwoValidation',text,flags=re.M)
settings.write_text(text,encoding='utf-8')
version=1
while (out/f'{mode}-v{version}.log').exists():version+=1
args=['E:/UnityEditors/2022.3.53f1c1/Editor/Unity.exe','-batchmode','-buildTarget','Android','-projectPath',str(target),'-logFile',str(out/f'{mode}-v{version}.log')]
if mode=='build':args+=['-nographics','-executeMethod',os.environ.get('CELLDEFENSE_BUILD_METHOD','ChapterTwoContentBuilder.Build'),'-quit']
else:
    args+=['-runTests','-testPlatform','PlayMode' if mode=='play' else 'EditMode','-testResults',str(out/f'{mode}-v{version}.xml')]
    args+=['-force-d3d11','-disableAudio'] if mode=='play' else ['-nographics']
    if os.environ.get('CELLDEFENSE_CHAPTER_TWO_FILTER'):args+=['-testFilter',os.environ['CELLDEFENSE_CHAPTER_TWO_FILTER']]
env=os.environ.copy();env.setdefault('CELLDEFENSE_CHAPTER_TWO_CAPTURE',str(root/'Assets/_Project/Art/Sprites/细胞绘制/第二章接入_2026-10-28_v1/验证'))
env.pop('CELLDEFENSE_CHAPTER_CAPTURE_DIR',None)
if mode=='play':env['CELLDEFENSE_VALIDATION_LANDSCAPE']='1'
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
process=subprocess.Popen(args,env=env,startupinfo=startup)
record=dict(pid=process.pid,mode=mode,version=version,log=str(out/f'{mode}-v{version}.log'))
(out/'active-run.json').write_text(json.dumps(record,indent=2),encoding='utf-8');print(json.dumps(record))
