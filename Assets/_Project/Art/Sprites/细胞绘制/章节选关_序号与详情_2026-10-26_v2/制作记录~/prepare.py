from pathlib import Path
import json,shutil,hashlib
from PIL import ImageFont
from fontTools.ttLib import TTFont

root=Path('E:/unity/CellDefense');base=root/'Assets/_Project/Art/Sprites/细胞绘制';old=base/'章节选择与三章选关_2026-10-26_v1'
art=base/'章节选关_序号与详情_2026-10-26_v2';rec=art/'制作记录~'
for folder in ['组件','文字','完整界面','源文件','预览~','制作记录~/接入前备份','制作记录~/字形来源']:(art/folder).mkdir(parents=True,exist_ok=True)
for path in ['Assets/_Project/Scripts/UI/MainMenuController.cs','Assets/_Project/Tests/PlayMode/LevelScoringPlayModeTests.cs','Docs/Art/README.md']:
 source=root/path;target=rec/'接入前备份'/path;target.parent.mkdir(parents=True,exist_ok=True)
 if not target.exists():shutil.copy2(source,target)
 meta=Path(str(source)+'.meta')
 if meta.exists() and not Path(str(target)+'.meta').exists():shutil.copy2(meta,Path(str(target)+'.meta'))
oldplan=json.loads((old/'制作记录~/Aseprite组装计划.json').read_text(encoding='utf-8'))
fontdir=old/'制作记录~/字形来源'
for name in ['fusion-pixel-12px-monospaced-zh_hans.ttf','OFL.txt','来源说明.txt']:shutil.copy2(fontdir/name,rec/'字形来源'/name)
font=ImageFont.truetype(str(fontdir/'fusion-pixel-12px-monospaced-zh_hans.ttf'),12);cmap=TTFont(str(fontdir/'fusion-pixel-12px-monospaced-zh_hans.ttf')).getBestCmap()
texts=[]
def add(key,value,scale=2,style='body'):
 assert all(c.isspace() or ord(c) in cmap for c in value),value
 mask=font.getmask(value,mode='1');w,h=mask.size
 texts.append({'key':key,'text':value,'nativeSize':[w,h],'pixels':[i for i,v in enumerate(mask) if v],'scale':scale,'style':style,'output':(art/'文字'/f'{key}.png').as_posix()})
for i in range(1,6):add(f'number{i}',str(i),8)
for key,value,scale,style in [('history','历史最高分',3,'body'),('best_stars','最佳星级',2,'body'),('unplayed','未通关',3,'body'),('sample_score','9600',6,'body'),('start','开始游戏',3,'body'),('return','返回选关',3,'body'),('hint','选择序号，查看地图与成绩',2,'footer')]:add(key,value,scale,style)
for c in oldplan['chapters']:
 for l in c['levels']:
  add(l['id']+'_detailtitle',f"第{l['id'].split('-')[1]}关 · {l['name']}",3,'body')
plan={'art':art.as_posix(),'previous':old.as_posix(),'canvas':[1600,900],'chapters':oldplan['chapters'],'oldTexts':oldplan['texts'],'texts':texts,
 'backgrounds':{j['id']:j['target'] for j in oldplan['jobs'] if j['id']!='kit'},'exampleScore':9600,'sampleScoreIsIllustrative':True,'chapterCounts':[5,4,4],
 'runtimeScope':'Existing five playable scenes: number/star overview, detail modal reads existing best-score record, explicit start button. Chapter 2/3 remain art-only.'}
(rec/'Aseprite组装计划.json').write_text(json.dumps(plan,ensure_ascii=False,indent=2),encoding='utf-8')
catalog=json.loads((old/'可扩展素材索引.json').read_text(encoding='utf-8'));catalog['version']=2;catalog['artOnly']=False;catalog['runtimeScope']=plan['runtimeScope'];catalog['assetRoot']=art.relative_to(root).as_posix()
catalog['selectionFlow']={'overview':['localStageNumber','bestStars'],'onStageClick':'openDetails','details':['mapPreview','existingHistoricalBestScore','bestStars','explicitStartAction'],'localStageNumbers':True,'newAttemptHistoryAdded':False,'chapterSelectUnchanged':True}
catalog['chapterBrowserImage']=(old/'完整界面/04_章节选择.png').relative_to(root).as_posix()
for c in catalog['chapters']:
 c['levelSelectImage']=(art/'完整界面'/f"0{c['id']}_第{c['id']}章_序号选关.png").relative_to(root).as_posix()
 for i,l in enumerate(c['levels']):l['localNumber']=i+1
(art/'可扩展素材索引.json').write_text(json.dumps(catalog,ensure_ascii=False,indent=2),encoding='utf-8')
print('Prepared numbered-stage artwork, 13 detail titles and safe pre-integration backups')
