from pathlib import Path
import re,uuid,shutil,json,hashlib
root=Path('E:/unity/CellDefense');art=root/'Assets/_Project/Art/Sprites/细胞绘制/评分美术_2026-10-25_v1'
backup=art/'制作记录~/接入前备份'
original=root/'Logs/CodexTradingTests/Assets/_Project/Scripts/UI/PixelImageText.cs'
target=backup/'Assets/_Project/Scripts/UI/PixelImageText.cs'
if not target.exists():
 target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(original,target);shutil.copy2(Path(str(original)+'.meta'),Path(str(target)+'.meta'))
template=Path(str(art/'评分补全字图_12px.png')+'.meta').read_text(encoding='utf-8')
refs={}
for size,field in [(12,'scoreGlyphAtlas'),(8,'smallScoreGlyphAtlas')]:
 p=art/f'结算净字图_{size}px.png';meta=Path(str(p)+'.meta')
 guid=re.search(r'^guid: (\w+)',meta.read_text(encoding='utf-8'),re.M)[1] if meta.exists() else uuid.uuid4().hex
 meta.write_text(re.sub(r'^guid: \w+','guid: '+guid,template,flags=re.M),encoding='utf-8')
 refs[field]=guid
asset=root/'Assets/_Project/Resources/ScoreArtworkResources.asset'
t=asset.read_text(encoding='utf-8')
for field,guid in refs.items():
 line=f'  {field}: {{fileID: 2800000, guid: {guid}, type: 3}}'
 t=re.sub(r'^  '+field+r':.*',line,t,flags=re.M) if re.search(r'^  '+field+r':',t,re.M) else t+line+'\n'
asset.write_text(t,encoding='utf-8')
for p in art.glob('*.aseprite'):
 m=Path(str(p)+'.meta')
 if not m.exists():m.write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
folder_meta=Path(str(art)+'.meta')
if not folder_meta.exists():folder_meta.write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
records=[]
for p in backup.rglob('*'):
 if p.is_file() and p.suffix!='.meta': records.append({'path':p.relative_to(backup).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
(art/'制作记录~/接入前清单.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
print('Finalized six runtime PNGs, native source metadata, resource references and six-file backup manifest')
