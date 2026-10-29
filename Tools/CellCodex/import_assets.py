from pathlib import Path
from PIL import Image
import re,uuid,json,hashlib,shutil
ROOT=Path('E:/unity/CellDefense')
ART=ROOT/'Assets/_Project/Art/Sprites/细胞绘制/细胞图鉴_2026-10-28_v2'
R=ART/'制作记录~'
plan=json.loads((R/'组装计划.json').read_text(encoding='utf-8'))
TEMPLATE=(ROOT/'Assets/_Project/Art/Sprites/细胞绘制/像素精修_2026-10-21_v1/主界面/胞卫战_主界面_v1.png.meta').read_text(encoding='utf-8')

def guid_for(path,kind='DefaultImporter'):
 m=Path(str(path)+'.meta')
 if m.exists():
  return re.search(r'^guid: (\w+)',m.read_text(encoding='utf-8-sig'),re.M)[1]
 guid=uuid.uuid4().hex
 body='fileFormatVersion: 2\nguid: '+guid+'\n'
 if path.is_dir():body+='folderAsset: yes\n'
 if kind=='MonoImporter':body+='MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
 else:body+=kind+':\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
 m.write_text(body,encoding='utf-8')
 return guid

for name in ['CellCodexArtwork','CellCodexView']:
 guid_for(ROOT/f'Assets/_Project/Scripts/UI/{name}.cs','MonoImporter')
guids={}
for p in ART.rglob('*.png'):
 if any(s.endswith('~') for s in p.relative_to(ART).parts):continue
 guid=guid_for(p);guids[p.as_posix()]=guid
 text=re.sub(r'(?m)^guid: \w+','guid: '+guid,TEMPLATE)
 text=re.sub(r'spriteID: \w+','spriteID: '+uuid.uuid4().hex,text)
 text=text.replace('spriteMeshType: 1','spriteMeshType: 0').replace('spriteGenerateFallbackPhysicsShape: 1','spriteGenerateFallbackPhysicsShape: 0')
 text=re.sub(r'maxTextureSize: \d+','maxTextureSize: 4096',text)
 Path(str(p)+'.meta').write_text(text,encoding='utf-8')
for p in ART.rglob('*'):
 if any(s.endswith('~') for s in p.relative_to(ART).parts):continue
 if p.is_dir() or p.suffix=='.aseprite':guid_for(p)
guid_for(ART)

def sprite(path):return '{fileID: 21300000, guid: '+guids[Path(path).as_posix()]+', type: 3}'
script=guid_for(ROOT/'Assets/_Project/Scripts/UI/CellCodexArtwork.cs','MonoImporter')
resource=ROOT/'Assets/_Project/Resources/CellCodexArtwork.asset'
assert not resource.exists(),'Preserve existing codex resource'
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
  m_Script: {fileID: 11500000, guid: '''+script+''', type: 3}
  m_Name: CellCodexArtwork
  m_EditorClassIdentifier: 
'''
text+='  frame: {fileID: 21300000, guid: f5fed57a2bd64a8aaf0864c4e6507282, type: 3}\n'
for key in ['title','defenders','invaders','home','index','previous','next']:
 text+='  '+key+': '+sprite(ART/'文字'/f'{key}.png')+'\n'
text+='  entries:\n'
for e in plan['entries']:
 text+='  - id: '+e['id']+'\n    displayName: '+json.dumps(e['name'],ensure_ascii=False)+'\n    category: '+str(e['category'])+'\n'
 for key,path in [('portrait',e['portrait']),('page',e['page']),('caption',e['captionPath'])]:text+='    '+key+': '+sprite(path)+'\n'
resource.write_text(text,encoding='utf-8')
resourceMeta=Path(str(resource)+'.meta')
resourceMeta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')

index=[]
for e in plan['entries']:
 im=Image.open(e['portrait']).convert('RGBA');a=im.getchannel('A');box=a.getbbox()
 assert im.size==(384,384) and box[0]>=20 and box[1]>=20 and box[2]<=364 and box[3]<=364
 assert set(a.getdata())=={0,255}
 assert Image.open(e['page']).size==(1280,720)
 source=ROOT/e['source'];assert hashlib.sha256(source.read_bytes()).hexdigest()==e['sourceSha256'],'Source values changed while drawing'
 index.append({k:e[k] for k in ['id','name','category','role','source','sourceSha256','portrait','page','captionPath']})
for p in Path(__file__).parent.glob('*'):
 if p.is_file():shutil.copy2(p,R/p.name)
(ART/'图鉴素材索引.json').write_text(json.dumps({'version':1,'entries':index,'layout':'1280x720 full pages, 384x384 transparent portraits','dataDate':'2026-10-28','expansion':'Append stable-id entries and portraits/pages; no fixed roster size in UI, current list fits up to eight entries per category'},ensure_ascii=False,indent=2),encoding='utf-8')
print('Imported 14 portraits and 14 pages; complete alpha padding and current config hashes verified.')
