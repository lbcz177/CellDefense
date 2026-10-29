from pathlib import Path
import json,re,uuid,shutil
root=Path(__file__).resolve().parents[2];art=root/'Assets/_Project/Art/Sprites/细胞绘制/炎症与引导_2026-10-29_v1'
template=(root/'Assets/_Project/Art/Sprites/细胞绘制/第二章新塔_2026-10-29_v1/战斗与图标/01_血小板修补塔.png.meta').read_text(encoding='utf-8-sig')
def meta(p):
 m=Path(str(p)+'.meta');old=m.read_text(encoding='utf-8-sig') if m.exists() else None;g=re.search(r'^guid: (\w+)',old,re.M)[1] if old else uuid.uuid4().hex
 if p.suffix=='.png':
  t=re.sub(r'^guid: \w+','guid: '+g,template,flags=re.M);t=re.sub(r'spriteID: \w+','spriteID: '+uuid.uuid4().hex,t)
  border=80 if p.stem in ['tutorial','inflammation'] else 0
  t=re.sub(r'spriteBorder: .*',f'spriteBorder: {{x: {border}, y: {border}, z: {border}, w: {border}}}',t)
  if p.stem in ['particle','warning']:t=t.replace('spritePixelsToUnits: 128','spritePixelsToUnits: 16')
  m.write_text(t,encoding='utf-8')
 elif not old:
  t='fileFormatVersion: 2\nguid: '+g+'\n';t+='folderAsset: yes\n' if p.is_dir() else ''
  importer='MonoImporter' if p.suffix=='.cs' else 'NativeFormatImporter' if p.suffix=='.asset' else 'DefaultImporter'
  t+=importer+':\n  externalObjects: {}\n'
  if importer=='NativeFormatImporter':t+='  mainObjectFileID: 11400000\n'
  m.write_text(t,encoding='utf-8')
 return g
for p in [art,*art.glob('*')]:
 if p.suffix!='.meta' and not p.name.endswith('~'):meta(p)
script=meta(root/'Assets/_Project/Scripts/UI/GuidanceArtwork.cs');meta(root/'Assets/_Project/Scripts/OpenField/InflammationRegionVisual.cs')
p=root/'Assets/_Project/Resources/GuidanceArtwork.asset';assert not p.exists()
t='''%YAML 1.1
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
  m_Script: {fileID: 11500000, guid: '''+script+''', type: 3}
  m_Name: GuidanceArtwork
  m_EditorClassIdentifier: 
'''
for field,name in [('tutorialFrame','tutorial'),('regionFrame','inflammation'),('tissue','tissue'),('particle','particle'),('warning','warning')]:t+='  '+field+': {fileID: 21300000, guid: '+meta(art/(name+'.png'))+', type: 3}\n'
p.write_text(t,encoding='utf-8');meta(p)
for p in Path(__file__).parent.glob('*'):
 if p.is_file():shutil.copy2(p,art/'制作记录~'/p.name)
print('Imported five sprites and guidance resource; old artwork preserved.')
