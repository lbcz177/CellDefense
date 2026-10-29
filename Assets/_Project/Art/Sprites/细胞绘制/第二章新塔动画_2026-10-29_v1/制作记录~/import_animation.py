"""Import individually drawn action/contact frames and attach the clock-driven renderer."""
from pathlib import Path
from PIL import Image
import hashlib,json,re,shutil,uuid,sys

ROOT=Path(__file__).resolve().parents[2]
ART=ROOT/'Assets/_Project/Art/Sprites/细胞绘制/第二章新塔动画_2026-10-29_v1'
RECORD=ART/'制作记录~'
plan=json.loads((RECORD/'生成与组装计划.json').read_text(encoding='utf-8-sig'))
reports={j['id']:j for j in json.loads((RECORD/'逐帧检查.json').read_text(encoding='utf-8'))}
template=(ROOT/'Assets/_Project/Art/Sprites/细胞绘制/第二章新塔_2026-10-29_v1/战斗与图标/01_血小板修补塔.png.meta').read_text(encoding='utf-8')

def meta(path,ppu=None):
    target=Path(str(path)+'.meta')
    if target.exists():
        text=target.read_text(encoding='utf-8-sig');guid=re.search(r'^guid: (\w+)',text,re.M)[1]
        if path.suffix=='.png':
            text=re.sub(r'filterMode: \d+','filterMode: 0',text)
            text=re.sub(r'enableMipMap: \d+','enableMipMap: 0',text)
            text=re.sub(r'textureCompression: \d+','textureCompression: 0',text)
            text=re.sub(r'spriteMeshType: \d+','spriteMeshType: 0',text)
            text=re.sub(r'spriteGenerateFallbackPhysicsShape: \d+','spriteGenerateFallbackPhysicsShape: 0',text)
            if ppu is not None:text=re.sub(r'spritePixelsToUnits: [\d.]+','spritePixelsToUnits: '+str(ppu),text)
            target.write_text(text,encoding='utf-8')
        return guid
    guid=uuid.uuid4().hex;text='fileFormatVersion: 2\nguid: '+guid+'\n'
    if path.is_dir():text+='folderAsset: yes\n'
    importer='MonoImporter' if path.suffix=='.cs' else 'DefaultImporter'
    text+=importer+':\n  externalObjects: {}\n'
    if importer=='MonoImporter':text+='  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n'
    text+='  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    if path.suffix=='.png':
        text=re.sub(r'^guid: \w+','guid: '+guid,template,flags=re.M)
        text=re.sub(r'spriteID: \w+','spriteID: '+uuid.uuid4().hex,text)
        text=re.sub(r'spritePixelsToUnits: [\d.]+','spritePixelsToUnits: '+str(ppu if ppu is not None else 128),text)
    target.write_text(text,encoding='utf-8');return guid

script=ROOT/'Assets/_Project/Scripts/Towers/ChapterTwoActionAnimation.cs';script_guid=meta(script)
frames={};validation=[]
for job in plan['jobs']:
    directory=Path(job['directory']);report=reports[job['id']];guids=[];hashes=[]
    for frame in sorted((directory/'frames').glob('*.png')):
        image=Image.open(frame).convert('RGBA');alpha=image.getchannel('A');box=alpha.getbbox()
        assert image.size==(128,128) and set(alpha.getdata())=={0,255}
        assert box and min(box[:2])>=7 and max(box[2:])<=121
        guids.append(meta(frame,report['ppu']));hashes.append(hashlib.sha256(image.tobytes()).hexdigest())
    assert len(guids)==len(set(hashes))==job['count']
    frames[job['id']]=guids;validation.append(dict(id=job['id'],count=len(guids),distinct=len(set(hashes)),ppu=report['ppu'],pixelHashes=hashes))
for path in [ART,*ART.rglob('*')]:
    if not any(part.endswith('~') for part in path.relative_to(ART).parts) and path.suffix!='.meta':meta(path)

for tower,body,effect in ([] if '--settings-only' in sys.argv else [('PlateletTower','platelet_action','repair_impact'),('ComplementTower','complement_action','lysis_impact')]):
    path=ROOT/'Assets/_Project/Prefabs/Chapter02'/(tower+'.prefab')
    text=path.read_text(encoding='utf-8-sig');component_id='905070321000000001'
    assert script_guid not in text,'Do not reattach or overwrite the existing animation setup'
    backup=RECORD/'接入前备份'/path.relative_to(ROOT);backup.parent.mkdir(parents=True,exist_ok=True)
    assert not backup.exists();shutil.copy2(path,backup);shutil.copy2(Path(str(path)+'.meta'),Path(str(backup)+'.meta'))
    text,count=re.subn(r'(?m)^(  m_Component:\n)',r'\g<1>  - component: {fileID: '+component_id+'}\n',text);assert count==1
    object_id=re.search(r'--- !u!1 &(\d+)',text)[1]
    text+='--- !u!114 &'+component_id+'\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: '+object_id+'}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: '+script_guid+', type: 3}\n  m_Name: \n  m_EditorClassIdentifier: \n'
    for field,key in [('actionFrames',body),('contactFrames',effect)]:
        text+='  '+field+':\n'+''.join('  - {fileID: 21300000, guid: '+g+', type: 3}\n' for g in frames[key])
    text+='  lysis: '+('1' if tower=='ComplementTower' else '0')+'\n';path.write_text(text,encoding='utf-8')

(RECORD/'导入检查.json').write_text(json.dumps(validation,ensure_ascii=False,indent=2),encoding='utf-8')
for source in Path(__file__).parent.glob('*'):
    if source.is_file():shutil.copy2(source,RECORD/source.name)
print('Attached 10 unique body poses and 6 unique contact poses to each tower; old sprites/meta preserved.')
