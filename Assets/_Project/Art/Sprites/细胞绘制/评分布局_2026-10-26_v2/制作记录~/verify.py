from pathlib import Path
import ctypes,json,re,subprocess,os,shutil,time,xml.etree.ElementTree as ET
from ctypes import wintypes as w
root=Path('E:/unity/CellDefense');isolated=root/'Logs/CodexTradingTests';out=root/'Logs/ScoreLayout20261026'
art=root/'Assets/_Project/Art/Sprites/细胞绘制/评分布局_2026-10-26_v2';record=art/'制作记录~'
settings=(isolated/'ProjectSettings/ProjectSettings.asset').read_text(encoding='utf-8')
assert 'companyName: CellDefenseValidation' in settings and 'productName: ScoreFlowTests' in settings
for relative in ['Assets/_Project/Scripts/UI/LevelResultScoreView.cs','Assets/_Project/Scripts/OpenField/OpenFieldPrototypeController.cs']:
 shutil.copy2(root/relative,isolated/relative)
# Read only pipe names, never licensing credentials or editor command lines.
class Data(ctypes.Structure):
 _fields_=[('attrs',w.DWORD),('ct',w.FILETIME),('at',w.FILETIME),('wt',w.FILETIME),('hi',w.DWORD),('lo',w.DWORD),('r0',w.DWORD),('r1',w.DWORD),('name',w.WCHAR*260),('alt',w.WCHAR*14)]
k=ctypes.WinDLL('kernel32',use_last_error=True)
k.FindFirstFileW.argtypes=[w.LPCWSTR,ctypes.POINTER(Data)];k.FindFirstFileW.restype=w.HANDLE
k.FindNextFileW.argtypes=[w.HANDLE,ctypes.POINTER(Data)];k.FindClose.argtypes=[w.HANDLE]
d=Data();handle=k.FindFirstFileW('\\\\.\\pipe\\*',ctypes.byref(d));pipes=[]
if handle!=ctypes.c_void_p(-1).value:
 while True:
  if d.name.startswith('Unity-LicenseClient-') and not d.name.endswith('-notifications'):pipes.append(d.name[len('Unity-'):])
  if not k.FindNextFileW(handle,ctypes.byref(d)):break
 k.FindClose(handle)
log=out/'playmode-v1.log';xml=out/'playmode-v1.xml';assert not log.exists() and not xml.exists()
args=['E:/UnityEditors/2022.3.53f1c1/Editor/Unity.exe','-batchmode','-disableAudio','-buildTarget','Android','-projectPath',str(isolated),'-runTests','-testPlatform','PlayMode','-testFilter','ZzzLevelScoringPlayModeTests','-testResults',str(xml),'-logFile',str(log),'-force-d3d11']
if pipes:args+=['-licensingIpc',pipes[0]]
env=os.environ.copy();env['CELLDEFENSE_SCORE_CAPTURE_DIR']=str(art/'运行验证_v1~')
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
started=time.monotonic();proc=subprocess.Popen(args,env=env,startupinfo=startup)
(out/'pid.txt').write_text(str(proc.pid),encoding='utf-8')
try:
 proc.wait(timeout=90);timed_out=False
except subprocess.TimeoutExpired:
 proc.kill();proc.wait(timeout=15);timed_out=True
text=log.read_text(encoding='utf-8',errors='replace') if log.exists() else ''
last=text[text.rfind('Tundra build success'):] if 'Tundra build success' in text else ''
report={'timedOut':timed_out,'elapsedSeconds':round(time.monotonic()-started),'csharpCompiled':bool(last and not re.search(r'error CS\d+',last)),'runtimeTestsCompleted':xml.exists()}
if xml.exists():
 result=ET.parse(xml).getroot();report.update({key:result.attrib.get(key) for key in ['result','total','passed','failed']})
(record/'Unity校验结果.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report))
