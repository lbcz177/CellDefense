from pathlib import Path
import re,json,hashlib,shutil,uuid
root=Path(__file__).resolve().parents[2];work=Path(__file__).parent
plan=json.loads((work/'plan.json').read_text(encoding='utf-8'));art=Path(plan['art']);guids=plan['guids']
assert not (work/'integrated.json').exists(),'Already applied.'
template=(root/'Assets/_Project/Art/Sprites/细胞绘制/设置界面_声音与拉条_v1/01_声音图标.png.meta').read_text(encoding='utf-8-sig')
def texture_meta(path,guid,border=0,multiple=None,raw=False):
    text=re.sub(r'(?m)^guid: \w+','guid: '+guid,template)
    text=re.sub(r'(?m)^  spriteBorder:.*',f'  spriteBorder: {{x: {border}, y: {border}, z: {border}, w: {border}}}',text)
    if raw:
        text=text.replace('  textureType: 8','  textureType: 0').replace('  spriteMode: 1','  spriteMode: 0')
    if multiple:
        text=text.replace('  spriteMode: 1','  spriteMode: 2')
        blocks=[]
        for e in multiple:
            blocks.append(f'''    - serializedVersion: 2
      name: {e['name']}
      rect:
        serializedVersion: 2
        x: {e['x']}
        y: {plan['phraseHeight']-e['y']-e['height']}
        width: {e['width']}
        height: {e['height']}
      alignment: 0
      pivot: {{x: 0.5, y: 0.5}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: {e['spriteId']}
      internalID: {e['fileId']}
      vertices: []
      indices:
      edges: []
      weights: []''')
        text=text.replace('    sprites: []','    sprites:\n'+'\n'.join(blocks))
        text=text.replace('    nameFileIdTable: {}','    nameFileIdTable:\n'+'\n'.join(f"      {e['name']}: {e['fileId']}" for e in multiple))
    Path(str(path)+'.meta').write_text(text,encoding='utf-8')
texture_meta(art/'字图/提示像素字图.png',guids['glyphAtlas'],raw=True)
texture_meta(art/'固定文案/固定提示文案图集.png',guids['phraseAtlas'],multiple=plan['phrases'])
for key,name,border in [('panel','01_引导与提示面板.png',12),('enemyPanel','02_怪物出现面板.png',12),('button','03_提示按钮_常态.png',4),('selectedButton','04_提示按钮_高亮.png',4)]:
    texture_meta(art/'面板'/name,guids[key],border)

catalog=f'''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guids['catalogScript']}, type: 3}}
  m_Name: PromptImageResources
  m_EditorClassIdentifier:
  glyphAtlas: {{fileID: 2800000, guid: {guids['glyphAtlas']}, type: 3}}
  characters: {json.dumps(''.join(g['char'] for g in plan['glyphs']),ensure_ascii=False)}
  advances:
'''
catalog+=''.join(f"  - {g['advance']}\n" for g in plan['glyphs'])
catalog+=f"  columns: {plan['columns']}\n  cellSize: {plan['cellSize']}\n"
for key in ['panel','enemyPanel','button','selectedButton']:catalog+=f'  {key}: {{fileID: 21300000, guid: {guids[key]}, type: 3}}\n'
catalog+='  phrases:\n'
for e in plan['phrases']:
    catalog+=f"  - text: {json.dumps(e['text'],ensure_ascii=False)}\n    sprite: {{fileID: {e['fileId']}, guid: {guids['phraseAtlas']}, type: 3}}\n"
p=root/'Assets/_Project/Resources/PromptImageResources.asset';assert not p.exists();p.write_text(catalog,encoding='utf-8')
Path(str(p)+'.meta').write_text(f'fileFormatVersion: 2\nguid: {guids["catalog"]}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 11400000\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n',encoding='utf-8')

changed=[]
for entry in plan['before']:
    p=root/entry['path'];original=p.read_bytes()
    assert hashlib.sha256(original).hexdigest()==entry['sha256'],p
    text=original.decode('utf-8-sig')
    if p.suffix=='.cs':
        text=text.replace('TextMeshProUGUI','PixelImageText')
        text=re.sub(r'(?m)^\s*(?:label|text|hint)\.font = (?:font|menuFont);\r?\n','\n',text)
    else:
        # Keep the existing component local ID, RectTransform and HUD field reference.
        parts=re.split(r'(?m)(?=^--- !u!)',text)
        matches=0
        for i,part in enumerate(parts):
            if '  m_text: 壁垒可承伤' not in part:continue
            matches+=1
            head=part.split('  m_text:')[0]
            head=re.sub(r'(m_Script: \{fileID: 11500000, guid: )\w+',r'\g<1>'+guids['labelScript'],head)
            parts[i]=head+'''  displayText: 壁垒可承伤 · 拦截全部经过的敌人
  pointSize: 16
  textAlignment: 513
  enableAutoSizing: 1
  fontSizeMin: 12
  fontSizeMax: 16
'''
        assert matches==1,matches
        text=''.join(parts)
    p.write_text(text,encoding='utf-8',newline='')
    changed.append(dict(path=entry['path'],beforeSha256=entry['sha256'],afterSha256=hashlib.sha256(p.read_bytes()).hexdigest()))
(work/'integrated.json').write_text(json.dumps(changed,ensure_ascii=False,indent=2),encoding='utf-8')
(art/'制作记录~/初始接入记录.json').write_text(json.dumps(changed,ensure_ascii=False,indent=2),encoding='utf-8')
shutil.copy2(work/'draw.lua',art/'制作记录~/Aseprite提示绘制.lua')
print('Integrated image resources and migrated',len(changed),'UI source/prefab files.')
