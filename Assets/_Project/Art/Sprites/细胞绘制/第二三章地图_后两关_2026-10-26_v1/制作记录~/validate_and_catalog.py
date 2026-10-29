from pathlib import Path
import json,hashlib,re,uuid,shutil
from PIL import Image,ImageChops

root=Path('E:/unity/CellDefense');art=root/'Assets/_Project/Art/Sprites/细胞绘制/第二三章地图_后两关_2026-10-26_v1';record=art/'制作记录~'
jobs=json.loads((record/'交付清单.json').read_text(encoding='utf-8'))
plan=json.loads((record/'叠层与预览计划.json').read_text(encoding='utf-8'))
template=(root/'Assets/_Project/Art/Sprites/细胞绘制/前五关地图_2026-10-15_v1/01_守住伤口.png.meta').read_text(encoding='utf-8-sig')
for p in list((art/'第三章').glob('*独立覆盖.png'))+[art/'四张地图总览.png']:
 meta=Path(str(p)+'.meta');assert not meta.exists(),meta
 im=Image.open(p);content=re.sub(r'^guid: \w+','guid: '+uuid.uuid4().hex,template,flags=re.M)
 content=re.sub(r'(?m)^  spritePixelsToUnits: .*','  spritePixelsToUnits: '+str(im.width/15),content)
 content=re.sub(r'(?m)^    spriteID: .*','    spriteID: '+uuid.uuid4().hex,content)
 content=content.replace('maxTextureSize: 2048','maxTextureSize: 4096')
 meta.write_text(content,encoding='utf-8')
for p in art.rglob('*.aseprite'):
 if any(part.endswith('~') for part in p.relative_to(art).parts):continue
 meta=Path(str(p)+'.meta');assert not meta.exists(),meta
 meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
for folder in [art,art/'第二章',art/'第三章']:
 meta=Path(str(folder)+'.meta');assert not meta.exists(),meta
 meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
for j in jobs:
 p=Path(j['target']);im=Image.open(p);im.load()
 assert list(im.size)==j['size']
 assert hashlib.sha256(p.read_bytes()).hexdigest()==j['sha256']==hashlib.sha256(Path(j['source']).read_bytes()).hexdigest()
 assert abs(im.width/im.height-15/11)/(15/11)<.005
 assert p.with_suffix('.aseprite').exists()
 for path in [p]+[Path(z['overlay']) for m in plan['maps'] if m['id']==j['id'] for z in m['zones']]:
  meta=Path(str(path)+'.meta').read_text(encoding='utf-8')
  assert all(v in meta for v in ['filterMode: 0','enableMipMap: 0','textureCompression: 0','spriteMode: 1','textureType: 8'])
  assert f"spritePixelsToUnits: {j['ppu']}" in meta
overlays=[]
for m in plan['maps']:
 masks=[]
 for z in m['zones']:
  p=Path(z['overlay']);im=Image.open(p).convert('RGBA');alpha=im.getchannel('A');hist=alpha.histogram();ratio=1-hist[0]/(im.width*im.height)
  assert list(im.size)==m['size'] and .01<ratio<.06 and alpha.getextrema()==(0,225)
  assert sum(hist[226:])==0
  for previous in masks:assert ImageChops.multiply(previous,alpha).getbbox() is None,'Overlapping independent zones'
  masks.append(alpha)
  overlays.append({'map':m['id'],'region':z['key'],'file':p.relative_to(art).as_posix(),'size':list(im.size),'occupiedFraction':round(ratio,4),'maxAlpha':225})
assert len(overlays)==3
guids=[]
for p in art.rglob('*.meta'):
 if any(part.endswith('~') for part in p.relative_to(art).parts):continue
 match=re.search(r'^guid: (\w+)',p.read_text(encoding='utf-8'),re.M);assert match
 guids.append(match.group(1))
assert len(set(guids))==len(guids)
report={'passed':True,'mapCount':4,'editableSourceCount':4,'separateOverlayCount':3,'basePngsAreUnresampledGeneratedOutputs':True,'sourceAndDeliveryHashesMatch':True,
 'mapAspectWithinHalfPercentOf15by11':True,'nativeBasePixels':'verified by Aseprite fresh open; base layer exactly equals delivered PNG',
 'unityImport':'Sprite Single, Point, no mipmap, no compression, centered pivot; PPU = width / 15. Native sources use DefaultImporter.',
 'overlays':overlays,'thirdChapterDoubleZonesDoNotOverlap':True,
 'staticBfs':{m['id']:m['pathLengths'] for m in plan['maps']},
 'scope':'Art delivery only. Graybox topology is reachable, not a visual collision/range/economy validation. New scenes, chapter flow, directional waves and inflammation gameplay are not implemented or runtime-tested.'}
(record/'素材交付检查.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
for script in ['prepare.py','guides.lua','import_art.py','finish.lua','validate_and_catalog.py']:
 shutil.copy2(root/'Logs/ChapterMapsRemaining20261026'/script,record/script)
print('PASS: four maps and native sources, three nonempty separate region overlays, no overlap, checked Sprite settings, unique GUIDs and matching SHA-256')
print(json.dumps({'sizes':{j['id']:j['size'] for j in jobs},'overlays':overlays},ensure_ascii=True))
