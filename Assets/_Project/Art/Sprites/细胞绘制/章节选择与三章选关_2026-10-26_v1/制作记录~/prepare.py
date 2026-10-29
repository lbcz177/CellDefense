from pathlib import Path
import json,hashlib,shutil
from PIL import ImageFont
from fontTools.ttLib import TTFont

root=Path('E:/unity/CellDefense');base=root/'Assets/_Project/Art/Sprites/细胞绘制'
art=base/'章节选择与三章选关_2026-10-26_v1';record=art/'制作记录~'
for name in ['背景','组件','文字','关卡缩略图','章节封面','完整界面','源文件','预览~','制作记录~/原始生成','制作记录~/字形来源']:(art/name).mkdir(parents=True,exist_ok=True)
jobs=json.loads((record/'生成输出记录.json').read_text(encoding='utf-8'))
for j in jobs:
 raw=record/'原始生成'/f"{j['id']}_imagegen.png";assert not raw.exists(),raw
 shutil.copy2(j['source'],raw);j['raw']=raw.as_posix();j['sourceSha256']=hashlib.sha256(raw.read_bytes()).hexdigest()
(record/'生成输出记录.json').write_text(json.dumps(jobs,ensure_ascii=False,indent=2),encoding='utf-8')
fontsource=base/'提示图片化_2026-10-21_v1/制作记录~/字形来源'
for name in ['fusion-pixel-12px-monospaced-zh_hans.ttf','OFL.txt','来源说明.txt']:
 shutil.copy2(fontsource/name,record/'字形来源'/name)
font=ImageFont.truetype(str(fontsource/'fusion-pixel-12px-monospaced-zh_hans.ttf'),12)
cmap=TTFont(str(fontsource/'fusion-pixel-12px-monospaced-zh_hans.ttf')).getBestCmap()
chapters=[]
def levels(chapter,items):
 return [{'id':f'{chapter}-{i+1}','name':name,'map':(base/folder/file).as_posix()} for i,(name,folder,file) in enumerate(items)]
chapters.append({'id':1,'name':'伤口防线','label':'第一章','tagline':'守住伤口，建立防线','background':'chapter1','levels':levels(1,[(n,'前五关地图_2026-10-15_v1',f'{i+1:02d}_{n}.png') for i,n in enumerate(['守住伤口','集中清除','认出敌人','两侧告急','感染加剧'])])})
chapters.append({'id':2,'name':'持续感染','label':'第二章','tagline':'维持阵地，集中裂解','background':'chapter2','levels':levels(2,[('凝血缺口','第二三章地图_前两关_2026-10-26_v1/第二章','2-1_凝血缺口.png'),('厚膜岔口','第二三章地图_前两关_2026-10-26_v1/第二章','2-2_厚膜岔口.png'),('长廊窗口','第二三章地图_后两关_2026-10-26_v1/第二章','2-3_长廊窗口.png'),('生物膜边缘','第二三章地图_后两关_2026-10-26_v1/第二章','2-4_生物膜边缘.png')])})
chapters.append({'id':3,'name':'免疫平衡','label':'第三章','tagline':'局部反应，稳住平衡','background':'chapter3','levels':levels(3,[('充血边缘','第二三章地图_前两关_2026-10-26_v1/第三章','3-1_充血边缘.png'),('反应交汇','第二三章地图_前两关_2026-10-26_v1/第三章','3-2_反应交汇.png'),('过热前沿','第二三章地图_后两关_2026-10-26_v1/第三章','3-3_过热前沿.png'),('双灶组织','第二三章地图_后两关_2026-10-26_v1/第三章','3-4_双灶组织.png')])})
texts=[]
def add(key,text,scale,style='body'):
 assert not any(t['key']==key for t in texts)
 missing=[c for c in text if not c.isspace() and ord(c) not in cmap];assert not missing,(text,missing)
 mask=font.getmask(text,mode='1');w,h=mask.size
 pixels=[i for i,v in enumerate(mask) if v]
 assert pixels,(key,text)
 texts.append({'key':key,'text':text,'nativeSize':[w,h],'pixels':pixels,'scale':scale,'style':style,'output':(art/'文字'/f'{key}.png').as_posix()})
for key,text,scale,style in [('chapter_select','选择章节',4,'heading'),('level_select','选择关卡',2,'body'),('back_chapters','选章',2,'body'),('back_home','主页',2,'body'),('level_hint','选择关卡，开始防守',2,'footer'),('chapter_hint','选择章节，深入组织',2,'footer'),('page','1 / 2',2,'body'),('more_title','新的篇章',3,'heading'),('more_body','敬请期待',2,'body')]:add(key,text,scale,style)
for c in chapters:
 add(f"chapter{c['id']}_title",c['name'],4,'heading');add(f"chapter{c['id']}_label",c['label'],2)
 add(f"chapter{c['id']}_cardtitle",c['name'],3,'heading');add(f"chapter{c['id']}_tagline",c['tagline'],2)
 add(f"chapter{c['id']}_count",f"{len(c['levels'])} 关",2)
 for l in c['levels']:
  assert Path(l['map']).exists(),l
  l['sourceSha256']=hashlib.sha256(Path(l['map']).read_bytes()).hexdigest()
  add(l['id']+'_name',l['name'],2);add(l['id']+'_id',l['id'],2,'ivory')
  l['thumbnail']=(art/'关卡缩略图'/f"{l['id']}_{l['name']}.png").as_posix()
  l['card']=(art/'预览~'/f"{l['id']}_{l['name']}_组合卡.png").as_posix()
 c['cover']=(art/'章节封面'/f"第{c['id']}章_{c['name']}_地图封面.png").as_posix()
plan={'art':art.as_posix(),'canvas':[1600,900],'jobs':jobs,'chapters':chapters,'texts':texts,'stars':(base/'评分美术_2026-10-25_v1/评分星星图集.png').as_posix(),
 'kitRegions':[{'name':'卡片_常态','region':[0,0,512,512],'size':[192,128],'border':24},{'name':'卡片_高亮','region':[512,0,488,512],'size':[192,128],'border':24},{'name':'标题底板','region':[1000,0,536,512],'size':[256,80],'border':24},{'name':'返回箭头','region':[0,512,512,512],'fit':[88,76]},{'name':'下一页箭头','region':[512,512,488,512],'fit':[88,76]},{'name':'编号底板','region':[1000,512,536,512],'size':[128,40],'border':16}],
 'layout':{'card':[380,270],'levelRows':[216,516],'firstChapterColumns':3,'otherChapterColumns':2,'maxLevelsPerPage':6,'chapterCard':[400,520],'chapterPositions':[140,600,1060],'safeInset':64,'showPagerOnlyWhenNeeded':True},
 'scope':'Raster art and reusable assets only. No existing UI, gameplay, scene, progress or chapter-unlock code is modified.'}
(record/'Aseprite组装计划.json').write_text(json.dumps(plan,ensure_ascii=False,indent=2),encoding='utf-8')
catalog={'version':1,'artOnly':True,'chapters':[{k:v for k,v in c.items() if k!='levels'}|{'levels':[{'id':l['id'],'name':l['name'],'map':l['map'],'thumbnail':l['thumbnail']} for l in c['levels']]} for c in chapters],
 'extension':{'stableStringIds':True,'pageCapacity':6,'appendChaptersAndLevelsWithoutRedrawingBackground':True,'cardNormal':'组件/卡片_常态.png','cardHighlight':'组件/卡片_高亮.png','chapterCardUsesSameNineSlice':True,'nineSliceBorderPixels':24,'noBakedStarOrScoreInTemplates':True,'paginationIsSeparate':True,'futurePagePreviewIsIllustrative':True}}
(art/'可扩展素材索引.json').write_text(json.dumps(catalog,ensure_ascii=False,indent=2),encoding='utf-8')
print('Prepared 3 chapter catalogs, 13 verified map references,',len(texts),'exact pixel-text masks, reusable component and pagination plan')
