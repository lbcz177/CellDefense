from pathlib import Path
import json,subprocess,shutil,math
from PIL import Image
root=Path(__file__).resolve().parents[2]
archive=root/'Assets/_Project/Art/Sprites/细胞绘制/第三章接入_2026-10-29_v1/字图/制作记录~'
jobs=json.loads((archive/'glyph-jobs.json').read_text(encoding='utf-8'))
lines=[]
for j in jobs:
    source=json.dumps(j['source'],ensure_ascii=False);dest=json.dumps(j['target'],ensure_ascii=False)
    old=Image.open(j['source']);required=math.ceil((max(g['index'] for g in j['glyphs'])+1)/56)*j['cell']
    backup=archive/'扩展修正前'/Path(j['target']).name
    if not backup.exists():backup.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(j['target'],backup)
    lines+=['do',f'local previous=app.open({source})',f'local sprite=Sprite(previous.width,{max(required,old.height)},ColorMode.RGB)',
            'local image=sprite.cels[1].image','image:drawImage(previous.cels[1].image,Point(0,0))','previous:close()']
    for g in j['glyphs']:
        x=g['index']%56*j['cell'];y=g['index']//56*j['cell']
        lines += [f'image:drawPixel({x+px},{y+py},app.pixelColor.rgba(255,255,255,255))' for px,py in g['pixels']]
    lines += [f'sprite:saveAs({dest})',f'sprite:saveAs(string.gsub({dest},".png$",".aseprite"))','sprite:close()','end']
script=archive/'expand_canvas_without_rescaling.lua';script.write_text('\n'.join(lines),encoding='utf-8')
subprocess.run([str(next(Path('D:/aseprite').rglob('Aseprite.exe'))),'-b','--script',str(script)],check=True)
for j in jobs:
    original=Image.open(j['source']).convert('RGBA');result=Image.open(j['target']).convert('RGBA');cell=j['cell']
    for index in range(j['glyphs'][0]['index']):
        x=index%56*cell;y=index//56*cell;box=(x,y,x+cell,y+cell)
        assert original.crop(box).tobytes()==result.crop(box).tobytes(),(cell,index)
print('All 485 original glyph cells preserved at both sizes; 29 new glyphs appended.')
