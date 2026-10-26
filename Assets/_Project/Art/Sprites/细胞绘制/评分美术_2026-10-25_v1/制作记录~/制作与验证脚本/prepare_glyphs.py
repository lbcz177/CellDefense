from pathlib import Path
import json, re, shutil, uuid
from PIL import Image, ImageDraw, ImageFont

root = Path('E:/unity/CellDefense')
art = root / 'Assets/_Project/Art/Sprites/细胞绘制/评分美术_2026-10-25_v1'
archive = art / '制作记录~'
asset = root / 'Assets/_Project/Resources/PromptImageResources.asset'
text = asset.read_text(encoding='utf-8-sig')
characters = json.loads(re.search(r'^  characters: (.*)', text, re.M)[1])
added = ''.join(c for c in '分通' if c not in characters)
assert added == '分通', repr(added)
backup = archive / '接入前备份/Assets/_Project/Resources/PromptImageResources.asset'
backup.parent.mkdir(parents=True, exist_ok=True)
if backup.exists():
    assert backup.read_bytes() == asset.read_bytes()
else:
    shutil.copy2(asset, backup)
    shutil.copy2(Path(str(asset)+'.meta'), Path(str(backup)+'.meta'))
original = root / 'Assets/_Project/Art/Sprites/细胞绘制/提示图片化_2026-10-21_v1/制作记录~'
plan = json.loads((original/'处理计划.json').read_text(encoding='utf-8-sig'))
small = json.loads((original/'小字号处理计划.json').read_text(encoding='utf-8-sig'))
for data, field, cell in [(plan, 'glyphs', 18), (small, 'smallGlyphs', 14)]:
    g = next(g for g in data[field] if g['char'] == '关')
    xs=[v%cell for v in g['pixels']]; ys=[v//cell for v in g['pixels']]
    print('Existing glyph placement',cell,min(xs),min(ys),max(xs),max(ys))

entries = []
for size, cell, field, font_path, target in [
    (12,18,'glyphAtlas', original/'字形来源/fusion-pixel-12px-monospaced-zh_hans.ttf','评分补全字图_12px'),
    (8,14,'smallGlyphAtlas', original/'字形来源/8px/fusion-pixel-8px-monospaced-zh_hans.ttf','评分补全字图_8px')]:
    guid = re.search(r'^  '+field+r': .*guid: (\w+)',text,re.M)[1]
    meta = next(p for p in (root/'Assets/_Project/Art/Sprites/细胞绘制/提示图片化_2026-10-21_v1').rglob('*.png.meta') if 'guid: '+guid in p.read_text(encoding='utf-8'))
    source = Path(str(meta)[:-5])
    source_image = Image.open(source)
    font = ImageFont.truetype(str(font_path),size)
    glyphs=[]
    for i,c in enumerate(added):
        mask=Image.new('1',(cell,cell)); draw=ImageDraw.Draw(mask)
        draw.text((3,1),c,font=font,fill=1)
        pixels=[y*cell+x for y in range(cell) for x in range(cell) if mask.getpixel((x,y))]
        glyphs.append({'code':ord(c),'index':len(characters)+i,'pixels':pixels})
    new_guid=uuid.uuid4().hex
    out=art/(target+'.png')
    Path(str(out)+'.meta').write_text(re.sub(r'^guid: \w+','guid: '+new_guid,meta.read_text(encoding='utf-8'),flags=re.M),encoding='utf-8')
    text=text.replace('guid: '+guid,'guid: '+new_guid)
    entries.append({'source':str(source).replace('\\','/'),'target':str(out).replace('\\','/'),'cell':cell,'native':size,'glyphs':glyphs})
    print('Atlas size',size,source_image.size,'font bounds',font.getbbox('分'))
text=re.sub(r'^  characters: .*',lambda m:'  characters: '+json.dumps(characters+added,ensure_ascii=False),text,flags=re.M)
for key,value in [('advances',12),('smallAdvances',8)]:
    text=re.sub(r'(^  '+key+r':\n(?:  - [^\n]*\n)+)',lambda m:m[1]+('  - '+str(value)+'.0\n')*len(added),text,flags=re.M)
asset.write_text(text,encoding='utf-8')
(archive/'字形补全计划.json').write_text(json.dumps(entries,ensure_ascii=False,indent=2),encoding='utf-8')
print('Appended',len(added),'glyphs without shifting existing cells')
