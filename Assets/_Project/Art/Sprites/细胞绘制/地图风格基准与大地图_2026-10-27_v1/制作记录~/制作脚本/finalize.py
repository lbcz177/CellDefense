from pathlib import Path
from PIL import Image
import hashlib, json, shutil, uuid
from save_assets import PROJECT, ROOT, RECORDS, meta, new_copy, write_json

generated=Path('C:/Users/a/.codex/generated_images/01a14fcf-47f7-7d81-9e68-5605e5720d6c/exec-84dc70f0-f4cc-4f37-bbc1-0fa3e457a71b.png')
saved=ROOT/'大地图/30x22_宽域组织_大地图.png'
new_copy(generated, RECORDS/'30x22_大地图_生成原图.png')
meta(saved,30)
a=Image.open(generated).convert('RGBA'); b=Image.open(saved).convert('RGBA')
assert b.size==(a.width*3,a.height*3)
pa=a.load();pb=b.load()
for dy,dx in [(0,0),(1,1),(2,2),(0,2),(2,0)]:
    assert all(pa[x,y]==pb[x*3+dx,y*3+dy] for y in range(a.height) for x in range(a.width))

for path in sorted(ROOT.rglob('*')):
    relative=path.relative_to(ROOT)
    if any(part.endswith('~') for part in relative.parts): continue
    target=Path(str(path)+'.meta')
    if target.exists() or path.suffix=='.meta': continue
    if path.is_dir():
        target.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
    elif path.suffix=='.aseprite':
        target.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
folder_meta=Path(str(ROOT)+'.meta')
if not folder_meta.exists():
    folder_meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')

for script in Path(__file__).parent.glob('*'):
    if script.is_file():new_copy(script,RECORDS/'制作脚本'/script.name)

layout=json.loads((RECORDS/'30x22_逻辑布局与连通检查.json').read_text(encoding='utf-8'))
items=[]
for path in sorted(ROOT.rglob('*')):
    if not path.is_file() or path.name=='交付清单与验证.json':continue
    items.append({'path':path.relative_to(PROJECT).as_posix(),'bytes':path.stat().st_size,
                  'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
report={'date':'2026-10-27','approvedStyleSaved':True,
        'largeMap':{'saved':saved.relative_to(PROJECT).as_posix(),
                    'nativeSize':list(a.size),'exportSize':list(b.size),
                    'nearestNeighbor3xVerified':True,
                    'grid':[layout['width'],layout['height']],
                    'totalCells':layout['totalCells'],
                    'plannedBuildableCells':layout['initialBuildableCells'],
                    'plannedPathSteps':layout['entryShortestPathSteps'],
                    'artReview':'one left wound, four separate islands, one three-cell jade core; warm brown outlines; no UI or grid baked into artwork',
                    'limitations':['生成图与精确网格存在偏差，接入时须对齐障碍物与核心碰撞',
                                   '可建格数仅属于布局草案，未作为现有关卡生效',
                                   '尚未验证缩放、手机点击、波次经济或战斗平衡']},
        'textureImport':'Sprite Single; Point; no mipmaps; no compression; maximum texture 8192 (preserve 4K-width PNG)',
        'nativeSources':'Aseprite, one base image layer per map; no claim of per-object layers',
        'runtimeChanges':False,'oldAssetsPreserved':True,'files':items}
write_json(RECORDS/'交付清单与验证.json',report)
print(json.dumps({k:v for k,v in report.items() if k not in ('files',)},ensure_ascii=False))
