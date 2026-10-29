from pathlib import Path
import json,hashlib,shutil
root=Path('E:/unity/CellDefense');art=root/'Assets/_Project/Art/Sprites/细胞绘制/选关序号_细胞徽章_2026-10-26_v3';record=art/'制作记录~'
for f in ['徽章','源文件','完整界面','预览~','制作记录~/原始生成','制作记录~/接入前备份']:(art/f).mkdir(parents=True,exist_ok=True)
checks=[]
for name in ['Assets/_Project/Scripts/UI/MainMenuController.cs','Assets/_Project/Scripts/UI/LevelSelectionArtwork.cs','Assets/_Project/Resources/LevelSelectionArtwork.asset']:
 p=root/name;target=record/'接入前备份'/name;target.parent.mkdir(parents=True,exist_ok=True);assert not target.exists()
 shutil.copy2(p,target)
 if Path(str(p)+'.meta').exists():shutil.copy2(Path(str(p)+'.meta'),Path(str(target)+'.meta'))
 checks.append({'path':name,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
(record/'接入前清单.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf-8')
old=root/'Assets/_Project/Art/Sprites/细胞绘制/章节选关_序号与详情_2026-10-26_v2'
plan=json.loads((old/'制作记录~/Aseprite组装计划.json').read_text(encoding='utf-8'))
plan['previousV2']=old.as_posix();plan['art']=art.as_posix();plan['source']='';plan['sourceMode']='built-in image_gen cohesive 3x2 badge atlas; Aseprite alpha cleanup and hover rings'
(record/'Aseprite计划.json').write_text(json.dumps(plan,ensure_ascii=False,indent=2),encoding='utf-8')
print('Prepared versioned art folders, native-edit plan and 3-file pre-change backup')
