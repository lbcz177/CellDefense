from pathlib import Path
from PIL import Image
import hashlib,json,re,shutil,uuid

ROOT=Path(__file__).resolve().parents[2]
ART=ROOT/'Assets/_Project/Art/Sprites/细胞绘制/全塔打击反馈_2026-10-29_v1'
RECORD=ART/'制作记录~'
plan=json.loads((RECORD/'生成与组装计划.json').read_text(encoding='utf-8-sig'))
template=(ROOT/'Assets/_Project/Art/Sprites/细胞绘制/第二章新塔_2026-10-29_v1/战斗与图标/01_血小板修补塔.png.meta').read_text(encoding='utf-8-sig')

def meta(path,ppu=128):
    target=Path(str(path)+'.meta');existing=target.read_text(encoding='utf-8-sig') if target.exists() else None
    guid=re.search(r'^guid: (\w+)',existing,re.M)[1] if existing else uuid.uuid4().hex
    if path.suffix=='.png':
        text=existing or re.sub(r'^guid: \w+','guid: '+guid,template,flags=re.M)
        for key,value in [('filterMode',0),('enableMipMap',0),('textureCompression',0),('spriteMeshType',0),('spriteGenerateFallbackPhysicsShape',0),('spritePixelsToUnits',ppu)]:
            text=re.sub(key+r': [\d.]+',key+': '+str(value),text)
        if not existing:text=re.sub(r'spriteID: \w+','spriteID: '+uuid.uuid4().hex,text)
        target.write_text(text,encoding='utf-8')
    elif not existing:
        text='fileFormatVersion: 2\nguid: '+guid+'\n'
        if path.is_dir():text+='folderAsset: yes\n'
        importer='MonoImporter' if path.suffix=='.cs' else 'NativeFormatImporter' if path.suffix in ['.anim','.mat','.asset'] else 'DefaultImporter'
        text+=importer+':\n  externalObjects: {}\n'
        if importer=='MonoImporter':text+='  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n'
        if importer=='NativeFormatImporter':text+='  mainObjectFileID: '+str({'.mat':2100000,'.anim':7400000,'.asset':11400000}[path.suffix])+'\n'
        text+='  userData: \n  assetBundleName: \n  assetBundleVariant: \n';target.write_text(text,encoding='utf-8')
    return guid

def sprite(path):return '{fileID: 21300000, guid: '+meta(path,64 if path.parent.name=='投射物' else 128)+', type: 3}'
for name in ['CellImpactArtwork','CellCombatFeedback','TowerImpactRecoil']:meta(ROOT/f'Assets/_Project/Scripts/Combat/{name}.cs')
for path in [ART,*ART.rglob('*')]:
    if path.suffix!='.meta' and not any(s.endswith('~') for s in path.relative_to(ART).parts):meta(path,64 if path.parent.name=='投射物' else 128)
checks=[]
for j in plan['jobs']:
    hashes=[]
    for path in sorted((Path(j['directory'])/'frames').glob('*.png')):
        im=Image.open(path).convert('RGBA');alpha=im.getchannel('A');box=alpha.getbbox()
        assert im.size==(128,128) and set(alpha.getdata())=={0,255}
        assert box and min(box[:2])>=7 and max(box[2:])<=121
        hashes.append(hashlib.sha256(im.tobytes()).hexdigest())
    assert len(hashes)==len(set(hashes))==6;checks.append({'id':j['id'],'frames':6,'distinct':6,'hashes':hashes})

unity=ART/'Unity';unity.mkdir(exist_ok=True)
material=unity/'CellImpact.mat'
source=(ROOT/'Assets/_Project/Art/Animations/TopDown10_v3/AnimatedEnemy.mat').read_text(encoding='utf-8-sig')
if not material.exists():material.write_text(source.replace('m_Name: AnimatedEnemy','m_Name: CellImpact'),encoding='utf-8')
material_guid=meta(material)

resource=ROOT/'Assets/_Project/Resources/CellImpactArtwork.asset'
script_guid=meta(ROOT/'Assets/_Project/Scripts/Combat/CellImpactArtwork.cs')
text='''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: '''+script_guid+''', type: 3}
  m_Name: CellImpactArtwork
  m_EditorClassIdentifier: 
  spriteMaterial: {fileID: 2100000, guid: '''+material_guid+''', type: 2}
  styles:
'''
sizes=[.80,.78,1.0,.95,1.10,.80,.90,1.15]
for kind in range(8):
    directory=Path(plan['jobs'][kind]['directory']) if kind<6 else ROOT/'Assets/_Project/Art/Sprites/细胞绘制/第二章新塔动画_2026-10-29_v1'/('修补接触_特效' if kind==6 else '裂解命中_特效')
    text+='  - kind: '+str(kind)+'\n    frames:\n'
    for p in sorted((directory/'frames').glob('*.png')):text+='    - '+sprite(p)+'\n'
    head={0:'近卫颗粒',1:'抗体Y蛋白',4:'酸化囊泡'}.get(kind)
    text+='    projectile: '+(sprite(ART/'投射物'/(head+'.png')) if head else '{fileID: 0}')+'\n    worldSize: '+str(sizes[kind])+'\n'
if not resource.exists():resource.write_text(text,encoding='utf-8')
meta(resource)

# Retiming preserves the actual release event at 160ms and the original clip duration.
times=[0,.06,.105,.14,.16,.185,.245,.28,.325,.38,.48]
timing=[]
for path in sorted((ROOT/'Assets/_Project/Art/Animations/TopDown10_v3').glob('0[1-6]_*_Attack.anim')):
    old_guid=meta(path);new_path=unity/(path.stem+'_ImpactTiming.anim')
    clip=path.read_text(encoding='utf-8-sig');index=[0]
    def replace_time(match):
        value=times[index[0]];index[0]+=1;return '    - time: '+str(value)
    clip=re.sub(r'(?m)^    - time: [\d.]+',replace_time,clip);assert index[0]==11
    if not path.stem.startswith('02_fibrin'):assert '  - time: 0.16\n    functionName: ReleaseAttack' in clip
    if not new_path.exists():new_path.write_text(clip.replace('m_Name: '+path.stem,'m_Name: '+new_path.stem),encoding='utf-8')
    new_guid=meta(new_path)
    controller=path.with_name(path.name.replace('_Attack.anim','.controller'))
    original=controller.read_text(encoding='utf-8-sig');assert old_guid in original or new_guid in original
    if old_guid in original:controller.write_text(original.replace(old_guid,new_guid),encoding='utf-8')
    timing.append({'original':str(path.relative_to(ROOT)),'versionedClip':str(new_path.relative_to(ROOT)),'releaseSeconds':.16,'poseTimelineSeconds':.48,'unityStopSeconds':float(re.search(r'm_StopTime: ([\d.]+)',clip)[1]),'frameTimes':times})
meta(unity)
(RECORD/'导入与节奏检查.json').write_text(json.dumps({'newFrames':checks,'retimedClips':timing,'styles':8,'resource':str(resource.relative_to(ROOT))},ensure_ascii=False,indent=2),encoding='utf-8')
for path in Path(__file__).parent.glob('*'):
    if path.is_file():shutil.copy2(path,RECORD/path.name)
print('Imported 36 unique contact frames, 3 travel heads, 8 styles and 6 versioned attack timings. Release time and combat values preserved.')
