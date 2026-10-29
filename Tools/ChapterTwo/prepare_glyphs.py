"""Append glyphs using the project's pixel font and editable Aseprite source."""
from pathlib import Path
import json, re, uuid, shutil, subprocess, math
from PIL import ImageFont

root = Path(__file__).resolve().parents[2]
art = root/'Assets/_Project/Art/Sprites/细胞绘制/第二章接入_2026-10-28_v1/字图'
archive = art/'制作记录~'
archive.mkdir(parents=True, exist_ok=True)
resource = root/'Assets/_Project/Resources/PromptImageResources.asset'
text = resource.read_text(encoding='utf-8-sig')
characters = json.loads(re.search(r'^  characters: (.*)', text, re.M)[1])
files = [root/'Assets/_Project/Editor/ChapterTwoContentBuilder.cs']
files += [root/'Assets/_Project/Scripts'/p for p in ['UI/ChapterTwoBriefing.cs','UI/OpenFieldBuildMenu.cs','UI/OpenFieldTowerMenu.cs','OpenField/OpenFieldPrototypeController.cs']]
strings = ''.join(s for p in files for s in re.findall(r'"([^"\n]*)"',p.read_text(encoding='utf-8-sig')))
added = ''.join(sorted(set(c for c in strings if '\u4e00' <= c <= '\u9fff' and c not in characters)))
if not added:
    print('Chapter two glyphs already present');raise SystemExit
shutil.copy2(resource, archive/'PromptImageResources-before.asset')
font_dir = root/'Assets/_Project/Art/Sprites/细胞绘制/提示图片化_2026-10-21_v1/制作记录~/字形来源'
jobs = []
for native, cell, field, font_name in [(12,18,'glyphAtlas','fusion-pixel-12px-monospaced-zh_hans.ttf'),(8,14,'smallGlyphAtlas','8px/fusion-pixel-8px-monospaced-zh_hans.ttf')]:
    guid = re.search(r'^  '+field+r': .*guid: (\w+)',text,re.M)[1]
    meta = next(p for p in (root/'Assets/_Project/Art').rglob('*.png.meta') if not any(x.endswith('~') for x in p.parts) and 'guid: '+guid in p.read_text(encoding='utf-8-sig'))
    font = ImageFont.truetype(str(font_dir/font_name),native)
    glyphs = []
    for i, char in enumerate(added):
        mask=font.getmask(char,mode='1');width,height=mask.size
        bbox=font.getbbox(char)
        pixels=[(n%width+3,n//width+1+bbox[1]) for n,p in enumerate(mask) if p]
        glyphs.append({'index':len(characters)+i,'char':char,'pixels':pixels})
    destination=art/f'第二章补全字图_{native}px.png'
    assert not destination.exists(), destination
    new_guid=uuid.uuid4().hex
    Path(str(destination)+'.meta').write_text(re.sub(r'^guid: \w+','guid: '+new_guid,meta.read_text(encoding='utf-8-sig'),flags=re.M),encoding='utf-8')
    text=text.replace('guid: '+guid,'guid: '+new_guid)
    jobs.append({'source':str(meta)[:-5].replace('\\','/'),'target':destination.as_posix(),'cell':cell,'glyphs':glyphs})

lua=['local jobs = {']
for job in jobs:
    lua.append('{source='+json.dumps(job['source'],ensure_ascii=False)+',target='+json.dumps(job['target'],ensure_ascii=False)+',cell='+str(job['cell'])+',glyphs={')
    for g in job['glyphs']:
        lua.append('{index='+str(g['index'])+',pixels={'+','.join('{'+str(x)+','+str(y)+'}' for x,y in g['pixels'])+'}},')
    lua.append('}},')
lua += ['}', '''for _,job in ipairs(jobs) do
  local sprite=app.open(job.source)
  local required=math.ceil(('''+str(len(characters)+len(added))+''')/56)*job.cell
  if sprite.height<required then sprite:resize(sprite.width,required) end
  local image=sprite.cels[1].image
  for _,glyph in ipairs(job.glyphs) do
    local x=(glyph.index%56)*job.cell
    local y=math.floor(glyph.index/56)*job.cell
    for _,p in ipairs(glyph.pixels) do image:drawPixel(x+p[1],y+p[2],app.pixelColor.rgba(255,255,255,255)) end
  end
  sprite:saveAs(job.target)
  sprite:saveAs(string.gsub(job.target,".png$",".aseprite"))
  sprite:close()
end''']
script=archive/'append_glyphs.lua';script.write_text('\n'.join(lua),encoding='utf-8')
aseprite=next(Path('D:/aseprite').rglob('Aseprite.exe'))
subprocess.run([str(aseprite),'-b','--script',str(script)],check=True)
text=re.sub(r'^  characters: .*',lambda _: '  characters: '+json.dumps(characters+added,ensure_ascii=False),text,flags=re.M)
for key,value in [('advances',12),('smallAdvances',8)]:
    text=re.sub(r'(^  '+key+r':\n(?:  - [^\n]*\n)+)',lambda m:m[1]+('  - '+str(value)+'.0\n')*len(added),text,flags=re.M)
resource.write_text(text,encoding='utf-8')
(archive/'glyph-jobs.json').write_text(json.dumps(jobs,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'added_glyphs':added,'count':len(added)},ensure_ascii=True))
