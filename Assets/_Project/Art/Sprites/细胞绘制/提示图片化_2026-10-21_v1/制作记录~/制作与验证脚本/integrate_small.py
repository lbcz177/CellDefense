from pathlib import Path
import json,re,shutil
work=Path(__file__).parent;plan=json.loads((work/'plan-small.json').read_text(encoding='utf-8'));art=Path(plan['art']);root=Path(plan['root'])
p=art/'字图/提示小字号字图_8px.png'
template=Path(str(art/'字图/提示像素字图.png')+'.meta').read_text(encoding='utf-8-sig')
meta=re.sub(r'(?m)^guid: \w+','guid: '+plan['guids']['smallGlyphAtlas'],template)
Path(str(p)+'.meta').write_text(meta,encoding='utf-8')
asset=root/'Assets/_Project/Resources/PromptImageResources.asset';text=asset.read_text(encoding='utf-8-sig')
assert '  smallGlyphAtlas:' not in text
marker='  characters:'
addition=f"  smallGlyphAtlas: {{fileID: 2800000, guid: {plan['guids']['smallGlyphAtlas']}, type: 3}}\n  smallAdvances:\n"
addition+=''.join(f"  - {g['advance']}\n" for g in plan['smallGlyphs'])
text=text.replace(marker,addition+marker,1);asset.write_text(text,encoding='utf-8')
shutil.copy2(work/'draw_small.lua',art/'制作记录~/Aseprite小字号绘制.lua')
print('Added 449 native 8px glyph images and metrics.')
