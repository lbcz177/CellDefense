from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from fontTools.ttLib import TTFont
import json,uuid,shutil
work=Path(__file__).parent;plan=json.loads((work/'plan.json').read_text(encoding='utf-8'));art=Path(plan['art'])
source=next((work/'font-source-small').rglob('*zh_hans.ttf'));font=ImageFont.truetype(str(source),8)
cmap=TTFont(source).getBestCmap();missing=[g['char'] for g in plan['glyphs'] if g['code'] not in cmap]
assert not missing,missing
glyphs=[]
for g in plan['glyphs']:
    mask=Image.new('1',(14,14));ImageDraw.Draw(mask).text((3,1),g['char'],font=font,fill=1)
    glyphs.append(dict(char=g['char'],advance=round(font.getlength(g['char']),3),pixels=[y*14+x for y in range(14) for x in range(14) if mask.getpixel((x,y))]))
plan['smallGlyphs']=glyphs
plan['guids']['smallGlyphAtlas']=uuid.uuid4().hex
(work/'plan-small.json').write_text(json.dumps(plan,ensure_ascii=False),encoding='utf-8')
(art/'制作记录~/小字号处理计划.json').write_text(json.dumps(plan,ensure_ascii=False),encoding='utf-8')
for p in (work/'font-source-small').rglob('*'):
    if p.is_file() and (p.suffix=='.txt' or p==source):
        target=art/'制作记录~/字形来源/8px'/p.relative_to(work/'font-source-small');target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,target)
print('Prepared native 8px glyphs:',len(glyphs))
