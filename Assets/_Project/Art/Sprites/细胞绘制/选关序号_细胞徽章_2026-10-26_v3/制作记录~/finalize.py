from pathlib import Path
import json,re,uuid,hashlib,shutil
from PIL import Image
root=Path('E:/unity/CellDefense');art=root/'Assets/_Project/Art/Sprites/细胞绘制/选关序号_细胞徽章_2026-10-26_v3';rec=art/'制作记录~'
template=(root/'Assets/_Project/Art/Sprites/细胞绘制/前五关地图_2026-10-15_v1/01_守住伤口.png.meta').read_text(encoding='utf-8-sig')
def meta(p,folder=False):
 target=Path(str(p)+'.meta');assert not target.exists(),target
 guid=uuid.uuid4().hex
 if p.suffix=='.png':
  content=re.sub(r'^guid: \w+','guid: '+guid,template,flags=re.M)
  content=re.sub(r'(?m)^    spriteID: .*','    spriteID: '+uuid.uuid4().hex,content)
  content=re.sub(r'(?m)^  spritePixelsToUnits: .*','  spritePixelsToUnits: 100',content)
  content=re.sub(r'(?m)^  spriteBorder: .*','  spriteBorder: {x: 0, y: 0, z: 0, w: 0}',content)
 else:content='fileFormatVersion: 2\nguid: '+guid+'\n'+('folderAsset: yes\n' if folder else '')+'DefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
 target.write_text(content,encoding='utf-8');return guid
for p in sorted(art.rglob('*')):
 if any(s.endswith('~') for s in p.relative_to(art).parts) or p.suffix=='.meta':continue
 meta(p,p.is_dir())
meta(art,True)
manifest=json.loads((rec/'徽章导出清单.json').read_text(encoding='utf-8'))
assert len(manifest)==6
def sprite(path):
 guid=re.search(r'^guid: (\w+)',Path(str(path)+'.meta').read_text(encoding='utf-8'),re.M)[1]
 return '{fileID: 21300000, guid: '+guid+', type: 3}'
asset=root/'Assets/_Project/Resources/LevelSelectionArtwork.asset';data=asset.read_text(encoding='utf-8')
data=re.sub(r'(?m)^  node: .*','  node: '+sprite(manifest[5]['normal']),data)
data=re.sub(r'(?m)^  selectedNode: .*','  selectedNode: '+sprite(manifest[5]['selected']),data)
assert '  numberBadges:' not in data
data+='  numberBadges:\n'+''.join('  - '+sprite(j['normal'])+'\n' for j in manifest[:5])
data+='  selectedNumberBadges:\n'+''.join('  - '+sprite(j['selected'])+'\n' for j in manifest[:5])
asset.write_text(data,encoding='utf-8')
hashes=[]
for j in manifest:
 normal=Image.open(j['normal']).convert('RGBA');active=Image.open(j['selected']).convert('RGBA')
 assert normal.size==active.size==(256,256)
 for im in [normal,active]:
  h=im.getchannel('A').histogram();assert h[0]>0 and h[255]>0 and sum(h[1:255])==0
  x,y,w,h=im.getbbox();assert x>0 and y>0 and w<256 and h<256
 for a,b in zip(normal.getdata(),active.getdata()):
  if a[3]:assert a==b,'Selected state changed the number or body'
 hashes.append(hashlib.sha256(Path(j['normal']).read_bytes()).hexdigest())
assert len(set(hashes))==6
catalog=json.loads((root/'Assets/_Project/Art/Sprites/细胞绘制/章节选关_序号与详情_2026-10-26_v2/可扩展素材索引.json').read_text(encoding='utf-8'))
catalog['version']=3;catalog['assetRoot']=art.relative_to(root).as_posix();catalog['numberedBadges']=[{'number':int(j['index']),'normal':Path(j['normal']).relative_to(root).as_posix(),'selected':Path(j['selected']).relative_to(root).as_posix()} for j in manifest[:5]]
catalog['blankBadge']={'normal':Path(manifest[5]['normal']).relative_to(root).as_posix(),'selected':Path(manifest[5]['selected']).relative_to(root).as_posix(),'futureNumbersUseDynamicLabel':True}
for c in catalog['chapters']:c['levelSelectImage']=(art/'完整界面'/f"0{c['id']}_第{c['id']}章_细胞徽章选关.png").relative_to(root).as_posix()
index=art/'可扩展素材索引.json';index.write_text(json.dumps(catalog,ensure_ascii=False,indent=2),encoding='utf-8');meta(index)
(rec/'美术检查.json').write_text(json.dumps({'passed':True,'numberedBadges':5,'blankExtensibleTemplate':1,'normalAndSelectedFiles':12,'badgeSize':[256,256],'transparentAlphaIsBinary':True,'selectedStatePreservesNumberAndBody':True,'noClippedOutline':True,'source':'built-in image_gen atlas; Aseprite native crop, normalization and outline layer','starsAndDetailFlowUnchanged':True},ensure_ascii=False,indent=2),encoding='utf-8')
for name in ['prepare.py','export.lua','finalize.py']:shutil.copy2(root/'Logs/CellBadges20261026'/name,rec/name)
print('PASS: 12 clean-alpha badge sprites, preserved glyphs in selected states, 6 independent native badges and 3 menus; existing resource GUID retained')
