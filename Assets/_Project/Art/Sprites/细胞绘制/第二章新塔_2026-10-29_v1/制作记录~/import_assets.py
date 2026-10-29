"""Import the versioned two-tower art and preserve pre-integration references."""
from pathlib import Path
from PIL import Image
import hashlib,json,re,shutil,uuid

ROOT=Path(__file__).resolve().parents[2]
ART=ROOT/'Assets/_Project/Art/Sprites/细胞绘制/第二章新塔_2026-10-29_v1'
RECORD=ART/'制作记录~'
plan=json.loads((RECORD/'组装计划.json').read_text(encoding='utf-8'))
template=(ROOT/'Assets/_Project/Art/Sprites/细胞绘制/细胞图鉴_2026-10-28_v2/立绘/01_近卫塔.png.meta').read_text(encoding='utf-8')

def metadata(path):
    meta=Path(str(path)+'.meta')
    if meta.exists():return re.search(r'^guid: (\w+)',meta.read_text(encoding='utf-8-sig'),re.M)[1]
    guid=uuid.uuid4().hex
    text='fileFormatVersion: 2\nguid: '+guid+'\n'
    if path.is_dir():text+='folderAsset: yes\n'
    text+='DefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    if path.suffix=='.png':
        text=re.sub(r'^guid: \w+','guid: '+guid,template,flags=re.M)
        text=re.sub(r'spriteID: \w+','spriteID: '+uuid.uuid4().hex,text)
        text=text.replace('spritePixelsToUnits: 100','spritePixelsToUnits: 128')
    meta.write_text(text,encoding='utf-8');return guid

for path in [ART,*ART.rglob('*')]:
    if not any(s.endswith('~') for s in path.relative_to(ART).parts):metadata(path)
checks=[]
for job in plan['jobs']:
    png=Path(job['sprite']);guid=metadata(png)
    image=Image.open(png).convert('RGBA');alpha=image.getchannel('A');box=alpha.getbbox()
    assert image.size==(128,128) and set(alpha.getdata())=={0,255}
    assert box and min(box[:2])>=12 and max(box[2:])<=116,box
    assert Image.open(job['portrait']).size==(384,384)
    for relative in [job['prefab'],job['definition']]:
        path=ROOT/relative;backup=RECORD/'接入前备份'/relative
        assert not backup.exists(),'Refuse to overwrite pre-integration backup'
        backup.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(path,backup);shutil.copy2(Path(str(path)+'.meta'),Path(str(backup)+'.meta'))
        text=path.read_text(encoding='utf-8-sig')
        field='m_Sprite' if path.suffix=='.prefab' else 'buildIcon'
        text,count=re.subn(r'(?m)^(  '+field+r': )\{[^\n]+\}',r'\g<1>{fileID: 21300000, guid: '+guid+', type: 3}',text)
        assert count==1
        if path.suffix=='.prefab':
            text,count=re.subn(r'(?m)^  m_LocalScale:.*','  m_LocalScale: {x: 0.9, y: 0.9, z: 0.9}',text);assert count==1
            text,count=re.subn(r'(?m)^  m_Color:.*','  m_Color: {r: 1, g: 1, b: 1, a: 1}',text);assert count==1
            text=re.sub(r'(?m)^  m_Size:.*','  m_Size: {x: 1, y: 1}',text)
        path.write_text(text,encoding='utf-8')
    checks.append(dict(id=job['id'],sprite=job['sprite'],guid=guid,size=image.size,opaqueBounds=box,worldCanvasSize=.9,sha256=hashlib.sha256(png.read_bytes()).hexdigest()))
for source in Path(__file__).parent.glob('*'):
    if source.is_file():shutil.copy2(source,RECORD/source.name)
(RECORD/'接入检查.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(checks,ensure_ascii=False))
