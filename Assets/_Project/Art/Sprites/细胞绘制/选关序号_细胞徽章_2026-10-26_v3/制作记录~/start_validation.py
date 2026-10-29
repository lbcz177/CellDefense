from pathlib import Path
import json,re,shutil,subprocess,os,ctypes
from ctypes import wintypes as w
root=Path('E:/unity/CellDefense');target=root/'Logs/CodexTradingTests';out=root/'Logs/CellBadges20261026';art=root/'Assets/_Project/Art/Sprites/细胞绘制/选关序号_细胞徽章_2026-10-26_v3'
settings=target/'ProjectSettings/ProjectSettings.asset'
backup=out/'isolated-settings-before.asset'
if not backup.exists():shutil.copy2(settings,backup)
data=settings.read_text(encoding='utf-8');data=re.sub(r'^  companyName: .*','  companyName: CellDefenseValidation',data,flags=re.M);data=re.sub(r'^  productName: .*','  productName: NumberedSelectionTests',data,flags=re.M);settings.write_text(data,encoding='utf-8')
assert 'companyName: CellDefenseValidation' in data and 'productName: NumberedSelectionTests' in data
for relative in ['Assets/_Project/Scripts/UI/MainMenuController.cs','Assets/_Project/Scripts/UI/LevelSelectionArtwork.cs','Assets/_Project/Resources/LevelSelectionArtwork.asset','Assets/_Project/Tests/PlayMode/NumberedLevelSelectionPlayModeTests.cs','Assets/_Project/Tests/PlayMode/LevelScoringPlayModeTests.cs','Assets/_Project/Tests/PlayMode/MainMenuSettingsPlayModeTests.cs']:
 for suffix in ['', '.meta']:
  source=Path(str(root/relative)+suffix);dest=Path(str(target/relative)+suffix)
  if source.exists():dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,dest)
for folder in ['章节选择与三章选关_2026-10-26_v1','章节选关_序号与详情_2026-10-26_v2','选关序号_细胞徽章_2026-10-26_v3']:
 base=root/'Assets/_Project/Art/Sprites/细胞绘制'/folder
 for source in base.rglob('*'):
  if any(part.endswith('~') for part in source.relative_to(base).parts) or not source.is_file() or source.suffix not in ['.png','.meta']:continue
  dest=target/source.relative_to(root);dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,dest)
 meta=Path(str(base)+'.meta');shutil.copy2(meta,Path(str(target/base.relative_to(root))+'.meta'))
class Data(ctypes.Structure):
 _fields_=[('attrs',w.DWORD),('ct',w.FILETIME),('at',w.FILETIME),('wt',w.FILETIME),('hi',w.DWORD),('lo',w.DWORD),('r0',w.DWORD),('r1',w.DWORD),('name',w.WCHAR*260),('alt',w.WCHAR*14)]
k=ctypes.WinDLL('kernel32',use_last_error=True);k.FindFirstFileW.argtypes=[w.LPCWSTR,ctypes.POINTER(Data)];k.FindFirstFileW.restype=w.HANDLE;k.FindNextFileW.argtypes=[w.HANDLE,ctypes.POINTER(Data)];k.FindClose.argtypes=[w.HANDLE]
d=Data();handle=k.FindFirstFileW('\\\\.\\pipe\\*',ctypes.byref(d));pipes=[]
if handle!=ctypes.c_void_p(-1).value:
 while True:
  if d.name.startswith('Unity-LicenseClient-') and not d.name.endswith('-notifications'):pipes.append(d.name[len('Unity-'):])
  if not k.FindNextFileW(handle,ctypes.byref(d)):break
 k.FindClose(handle)
version=1
while (out/f'playmode-v{version}.log').exists():version+=1
log=out/f'playmode-v{version}.log';xml=out/f'playmode-v{version}.xml';capture=art/f'运行验证_v{version}~'
args=['E:/UnityEditors/2022.3.53f1c1/Editor/Unity.exe','-batchmode','-disableAudio','-buildTarget','Android','-projectPath',str(target),'-runTests','-testPlatform','PlayMode','-testFilter','NumberedLevelSelectionPlayModeTests','-testResults',str(xml),'-logFile',str(log),'-force-d3d11']
if pipes:args+=['-licensingIpc',pipes[0]]
env=os.environ.copy();env['CELLDEFENSE_NUMBER_SELECTION_CAPTURE_DIR']=str(capture)
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
proc=subprocess.Popen(args,env=env,startupinfo=startup)
result={'pid':proc.pid,'log':str(log),'xml':str(xml),'capture':str(capture),'version':version,'isolatedCompany':'CellDefenseValidation','isolatedProduct':'NumberedSelectionTests'}
(out/'active-run.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps(result))
