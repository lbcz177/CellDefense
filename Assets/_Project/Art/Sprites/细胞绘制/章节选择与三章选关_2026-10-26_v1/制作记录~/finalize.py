from pathlib import Path
import json,re,uuid,hashlib,shutil
from PIL import Image,ImageChops

root=Path('E:/unity/CellDefense');art=root/'Assets/_Project/Art/Sprites/细胞绘制/章节选择与三章选关_2026-10-26_v1';r=art/'制作记录~'
plan=json.loads((r/'Aseprite组装计划.json').read_text(encoding='utf-8'))
export=json.loads((r/'Aseprite导出清单.json').read_text(encoding='utf-8'))
template=(root/'Assets/_Project/Art/Sprites/细胞绘制/前五关地图_2026-10-15_v1/01_守住伤口.png.meta').read_text(encoding='utf-8-sig')
def path_relative(data):
 if isinstance(data,dict):return {k:path_relative(v) for k,v in data.items()}
 if isinstance(data,list):return [path_relative(v) for v in data]
 if isinstance(data,str) and data.startswith(root.as_posix()+'/'):return data[len(root.as_posix())+1:]
 return data
catalog=json.loads((art/'可扩展素材索引.json').read_text(encoding='utf-8'))
catalog=path_relative(catalog);catalog['assetRoot']=art.relative_to(root).as_posix()
catalog['extension']['chapterPageCapacity']=3
catalog['extension']['paginationVisibleOnlyIfMoreThanOnePage']=True
catalog['extension']['responsiveColumnOptions']=[2,3]
catalog['extension']['frameSliceBorder']=[24,24,24,24]
for c,j in zip(catalog['chapters'],[j for j in plan['jobs'] if j['id'].startswith('chapter') and j['id']!='chapters']):c['backgroundImage']=path_relative(j['target'])
(art/'可扩展素材索引.json').write_text(json.dumps(catalog,ensure_ascii=False,indent=2),encoding='utf-8')
def metadata(file,kind='file'):
 meta=Path(str(file)+'.meta');assert not meta.exists(),meta
 guid=uuid.uuid4().hex
 if file.suffix=='.png':
  content=re.sub(r'^guid: \w+','guid: '+guid,template,flags=re.M)
  content=re.sub(r'(?m)^  spritePixelsToUnits: .*','  spritePixelsToUnits: 100',content)
  content=re.sub(r'(?m)^    spriteID: .*','    spriteID: '+uuid.uuid4().hex,content)
  content=content.replace('maxTextureSize: 2048','maxTextureSize: 4096')
  border=24 if file.stem in ['卡片_常态','卡片_高亮','标题底板'] else 12 if file.stem=='编号底板' else 0
  content=re.sub(r'(?m)^  spriteBorder: .*',f'  spriteBorder: {{x: {border}, y: {border}, z: {border}, w: {border}}}',content)
 else:
  content='fileFormatVersion: 2\nguid: '+guid+'\n'+('folderAsset: yes\n' if kind=='folder' else '')+'DefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
 meta.write_text(content,encoding='utf-8');return guid
def ignored(p):return any(part.endswith('~') for part in p.relative_to(art).parts)
files=[];guids=[]
for p in sorted(art.rglob('*')):
 if ignored(p) or p.suffix=='.meta':continue
 if p.is_dir():guids.append(metadata(p,'folder'));continue
 guid=metadata(p);guids.append(guid)
 item={'file':p.relative_to(art).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'guid':guid}
 if p.suffix=='.png':
  im=Image.open(p).convert('RGBA');im.load();item['size']=list(im.size)
  meta=Path(str(p)+'.meta').read_text(encoding='utf-8')
  assert all(t in meta for t in ['filterMode: 0','enableMipMap: 0','textureCompression: 0','spriteMode: 1','textureType: 8'])
  if p.parent.name in ['组件','文字']:
   hist=im.getchannel('A').histogram();assert hist[0]>0 and hist[255]>0 and sum(hist[1:255])==0,p
  if p.parent.name in ['完整界面','背景']:assert im.size==(1600,900) and im.getchannel('A').getextrema()==(255,255),p
 files.append(item)
guids.append(metadata(art,'folder'));assert len(guids)==len(set(guids))
assert len(export['screens'])==4 and len(export['cards'])==13 and len(export['textSlices'])==50
assert len(list((art/'关卡缩略图').glob('*.png')))==13
assert len(list((art/'组件').glob('*.png')))==8
assert len(list((art/'源文件').glob('*.aseprite')))==6
for c in plan['chapters']:
 for l in c['levels']:
  assert hashlib.sha256(Path(l['map']).read_bytes()).hexdigest()==l['sourceSha256'],'Original map changed: '+l['id']
  assert Image.open(l['thumbnail']).size==(300,174)
for j in plan['jobs']:assert hashlib.sha256(Path(j['source']).read_bytes()).hexdigest()==j['sourceSha256']==hashlib.sha256(Path(j['raw']).read_bytes()).hexdigest()
for s in export['screens']:
 assert Path(s['image']).exists() and Path(s['source']).exists() and not s['paginationVisible']
for c in export['cards']:
 x,y,w,h=c['rect'];assert x>=64 and y>=64 and x+w<=1536 and y+h<=836,c
for a in export['cards']:
 for b in export['cards']:
  if a is b or a['id'].split('-')[0]!=b['id'].split('-')[0]:continue
  x,y,w,h=a['rect'];u,v,d,e=b['rect'];assert min(x+w,u+d)<=max(x,u) or min(y+h,v+e)<=max(y,v),'Card overlap'
(r/'交付清单.json').write_text(json.dumps(files,ensure_ascii=False,indent=2),encoding='utf-8')
report={'passed':True,'canvas':[1600,900],'completeMenuImages':4,'chapterStageCounts':[5,4,4],'independentComponents':8,'mapThumbnails':13,'chapterCovers':3,'exactTextImages':50,'editableAsepriteFiles':6,'layersPerMenu':7,'normalStateUnselected':True,'starState':'empty neutral example; bright and dark sprites exported separately, not baked into blank cards','highestScoreRow':False,'lockedProgressionInvented':False,'opaqueBackgroundsAndScreens':True,'binaryTransparentUiSprites':True,'originalMapHashesUnchanged':True,'cardBoundsAndNoOverlap':True,'allCharacterCoverageChecked':True,'uniqueGuids':True,'frameNineSliceBorder':24,'genericSeparatePagingArt':True,'runtimeStatus':'Art and source delivery only. Existing main menu/level selection, chapter logic and scene availability not modified or runtime-tested.'}
(r/'素材交付检查.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
for name in ['prepare.py','assemble.lua','finalize.py']:
 shutil.copy2(root/'Logs/ChapterMenus20261026'/name,r/name)
print('PASS: 4 complete 1600x900 menus; 13 map thumbnails; 8 alpha-clean shared components; 50 exact bitmap labels; 6 Aseprite sources; unique Sprite metadata, safe card bounds, original-map SHA-256 unchanged')
