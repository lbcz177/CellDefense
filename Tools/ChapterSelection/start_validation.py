from pathlib import Path
import re,shutil,os,subprocess,json
root=Path(__file__).resolve().parents[2];target=root/'Logs/CodexTradingTests';out=root/'Logs/ChapterSelection20261028';out.mkdir(exist_ok=True)
for folder in ['Assets/_Project/Scripts','Assets/_Project/Tests','Assets/_Project/Resources']:
 for source in (root/folder).rglob('*'):
  if source.is_file() and not any(p.endswith('~') for p in source.relative_to(root).parts):
   dest=target/source.relative_to(root);dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,dest)
# Copy every PNG referenced by the shared resource catalogs, retaining original GUIDs.
guids={}
for meta in (root/'Assets/_Project/Art').rglob('*.png.meta'):
 if any(p.endswith('~') for p in meta.parts):continue
 match=re.search(r'^guid: (\w+)',meta.read_text(encoding='utf-8-sig'),re.M)
 if match:guids[match.group(1)]=meta
for resource in (root/'Assets/_Project/Resources').glob('*.asset'):
 for guid in re.findall(r'guid: (\w+)',resource.read_text(encoding='utf-8-sig')):
  if guid not in guids:continue
  meta=guids[guid]
  for source in [meta,Path(str(meta)[:-5])]:
   dest=target/source.relative_to(root);dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,dest)
settings=target/'ProjectSettings/ProjectSettings.asset';backup=out/'settings-before.asset'
if not backup.exists():shutil.copy2(settings,backup)
text=settings.read_text(encoding='utf-8-sig');text=re.sub(r'^  companyName:.*','  companyName: CellDefenseValidation',text,flags=re.M);text=re.sub(r'^  productName:.*','  productName: ChapterSelectionTests',text,flags=re.M);settings.write_text(text,encoding='utf-8')
v=1
while (out/f'playmode-v{v}.log').exists():v+=1
capture=root/f'Assets/_Project/Art/Sprites/细胞绘制/章节接入_2026-10-28/验证_v{v}~'
env=os.environ.copy();env['CELLDEFENSE_CHAPTER_CAPTURE_DIR']=str(capture)
args=['E:/UnityEditors/2022.3.53f1c1/Editor/Unity.exe','-batchmode','-disableAudio','-buildTarget','Android','-projectPath',str(target),'-runTests','-testPlatform','PlayMode','-testResults',str(out/f'playmode-v{v}.xml'),'-logFile',str(out/f'playmode-v{v}.log'),'-force-d3d11']
if os.environ.get('CELLDEFENSE_CHAPTER_TEST_FILTER'):args+=['-testFilter',os.environ['CELLDEFENSE_CHAPTER_TEST_FILTER']]
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
process=subprocess.Popen(args,env=env,startupinfo=startup)
record=dict(pid=process.pid,version=v,capture=str(capture),xml=str(out/f'playmode-v{v}.xml'))
(out/'active-run.json').write_text(json.dumps(record,indent=2),encoding='utf-8');print(json.dumps(record))
