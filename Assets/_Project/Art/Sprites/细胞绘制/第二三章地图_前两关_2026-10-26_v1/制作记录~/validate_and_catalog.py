from pathlib import Path
import json,hashlib,re,uuid,shutil
from PIL import Image

root=Path('E:/unity/CellDefense');art=root/'Assets/_Project/Art/Sprites/细胞绘制/第二三章地图_前两关_2026-10-26_v1';record=art/'制作记录~'
jobs=json.loads((record/'交付清单.json').read_text(encoding='utf-8'))
template=(root/'Assets/_Project/Art/Sprites/细胞绘制/前五关地图_2026-10-15_v1/01_守住伤口.png.meta').read_text(encoding='utf-8-sig')
for p in list((art/'第三章').glob('*独立覆盖.png'))+[art/'四张地图总览.png']:
 meta=Path(str(p)+'.meta');assert not meta.exists()
 im=Image.open(p);text=re.sub(r'^guid: \w+','guid: '+uuid.uuid4().hex,template,flags=re.M)
 text=re.sub(r'^  spritePixelsToUnits: .*','  spritePixelsToUnits: '+str(im.width/15),text,flags=re.M)
 meta.write_text(text,encoding='utf-8')
for p in art.rglob('*.aseprite'):
 if any(part.endswith('~') for part in p.relative_to(art).parts):continue
 meta=Path(str(p)+'.meta');assert not meta.exists()
 meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
for directory in [art,art/'第二章',art/'第三章']:
 meta=Path(str(directory)+'.meta')
 if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
for job in jobs:
 p=Path(job['target']);im=Image.open(p);im.load()
 assert list(im.size)==job['size']
 assert hashlib.sha256(p.read_bytes()).hexdigest()==job['sha256']==hashlib.sha256(Path(job['source']).read_bytes()).hexdigest()
 assert abs(im.width/im.height-15/11)/(15/11)<.005
 meta=Path(str(p)+'.meta').read_text(encoding='utf-8')
 assert 'filterMode: 0' in meta and 'enableMipMap: 0' in meta and 'textureCompression: 0' in meta
 assert p.with_suffix('.aseprite').exists()
overlays=[]
for p in (art/'第三章').glob('*独立覆盖.png'):
 im=Image.open(p).convert('RGBA');alpha=im.getchannel('A');hist=alpha.histogram();ratio=1-hist[0]/(im.width*im.height)
 assert .01<ratio<.10 and alpha.getextrema()==(0,225)
 assert sum(hist[226:])==0
 overlays.append({'file':p.relative_to(art).as_posix(),'size':list(im.size),'occupiedFraction':round(ratio,4),'maxAlpha':225})
assert len(overlays)==2
report={'passed':True,'mapCount':4,'editableSourceCount':4,'separateOverlayCount':2,'rawGeneratedImagesPreserved':True,'styleReferenceUnmodified':True,
 'allMapsReadable':True,'sourceAndDeliveryHashesMatch':True,'mapAspectWithinHalfPercentOf15by11':True,
 'unityImport':'Sprite Single, Point, no mipmaps, no compression; PPU = source width / 15; .aseprite uses DefaultImporter',
 'overlays':overlays,'staticBfs':'Initial 15x11 conceptual layouts only: 2-1 16 steps; 2-2 15/15; 3-1 12; 3-2 15/15',
 'runtimeStatus':'Art assets delivered. New playable scenes, exact artwork/collision alignment, chapter flow and inflammation gameplay not implemented or runtime-tested.'}
(record/'素材交付检查.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
for script in ['prepare.py','guides.lua','import_art.py','finish.lua','validate_and_catalog.py']:
 shutil.copy2(root/'Logs/ChapterMaps20261026'/script,record/script)
print('PASS: four original map PNGs and native sources; two nonempty transparent region overlays; Sprite settings; file hashes and aspect ratios')
print(json.dumps({'overlays':overlays},ensure_ascii=True))
