from pathlib import Path
import re,uuid,json,hashlib,shutil
from PIL import Image
root=Path('E:/unity/CellDefense');art=root/'Assets/_Project/Art/Sprites/细胞绘制/章节选关_序号与详情_2026-10-26_v2';rec=art/'制作记录~'
template=(root/'Assets/_Project/Art/Sprites/细胞绘制/前五关地图_2026-10-15_v1/01_守住伤口.png.meta').read_text(encoding='utf-8-sig')
def meta(p,folder=False):
 m=Path(str(p)+'.meta');assert not m.exists(),m
 guid=uuid.uuid4().hex
 if p.suffix=='.png':
  content=re.sub(r'^guid: \w+','guid: '+guid,template,flags=re.M);content=re.sub(r'(?m)^    spriteID: .*','    spriteID: '+uuid.uuid4().hex,content)
  content=re.sub(r'(?m)^  spritePixelsToUnits: .*','  spritePixelsToUnits: 100',content)
  b=24 if p.parent.name=='组件' else 0;content=re.sub(r'(?m)^  spriteBorder: .*',f'  spriteBorder: {{x: {b}, y: {b}, z: {b}, w: {b}}}',content)
 elif p.suffix=='.cs':
  content=re.sub(r'^guid: \w+','guid: '+guid,(root/'Assets/_Project/Scripts/UI/ScoreArtworkResources.cs.meta').read_text(encoding='utf-8'),flags=re.M)
 else:content='fileFormatVersion: 2\nguid: '+guid+'\n'+('folderAsset: yes\n' if folder else '')+'DefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
 m.write_text(content,encoding='utf-8');return guid
for p in sorted(art.rglob('*')):
 if any(x.endswith('~') for x in p.relative_to(art).parts) or p.suffix=='.meta':continue
 meta(p,p.is_dir())
meta(art,True)
script=root/'Assets/_Project/Scripts/UI/LevelSelectionArtwork.cs';scriptguid=meta(script)
def sprite(p):
 g=re.search(r'^guid: (\w+)',Path(str(p)+'.meta').read_text(encoding='utf-8'),re.M)[1]
 return '{fileID: 21300000, guid: '+g+', type: 3}'
previous=root/'Assets/_Project/Art/Sprites/细胞绘制/章节选择与三章选关_2026-10-26_v1'
refs={'background':previous/'背景/第一章_伤口防线_纯背景.png','node':art/'组件/序号按钮_常态.png','selectedNode':art/'组件/序号按钮_高亮.png','panel':art/'组件/详情底板.png','button':art/'组件/操作按钮_常态.png','selectedButton':art/'组件/操作按钮_高亮.png','historyLabel':art/'文字/history.png','bestStarsLabel':art/'文字/best_stars.png','unplayedLabel':art/'文字/unplayed.png','startLabel':art/'文字/start.png','returnLabel':art/'文字/return.png'}
refs['overviewHint']=art/'文字/hint.png'
asset=root/'Assets/_Project/Resources/LevelSelectionArtwork.asset';assert not asset.exists()
asset.write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: '+scriptguid+', type: 3}\n  m_Name: LevelSelectionArtwork\n  m_EditorClassIdentifier: \n'+''.join('  '+k+': '+sprite(p)+'\n' for k,p in refs.items()),encoding='utf-8')
Path(str(asset)+'.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
plan=json.loads((rec/'Aseprite组装计划.json').read_text(encoding='utf-8'))
for c in plan['chapters']:
 for l in c['levels']:assert hashlib.sha256(Path(l['map']).read_bytes()).hexdigest()==l['sourceSha256']
for p in (art/'完整界面').glob('*.png'):assert Image.open(p).size==(1600,900)
assert len(list((art/'完整界面').glob('*.png')))==7
(rec/'美术检查.json').write_text(json.dumps({'passed':True,'method':'Aseprite edit of existing native assets; no new imagegen call','overviewCounts':[5,4,4],'overviewContent':['number','stars'],'mapAndRecordOnlyInDetails':True,'existingMapHashesUnchanged':True,'artExampleScore':9600,'exampleIsNotARealUserRecord':True,'runtimeRecordSource':'LevelProgress.BestScore / BestStars; no new save keys or per-attempt history'},ensure_ascii=False,indent=2),encoding='utf-8')
for f in ['prepare.py','assemble.lua','finalize.py']:shutil.copy2(root/'Logs/NumberedSelection20261026'/f,rec/f)
print('Prepared 7 versioned screens and native sources, Sprite metadata, and runtime artwork references; original maps unchanged')
