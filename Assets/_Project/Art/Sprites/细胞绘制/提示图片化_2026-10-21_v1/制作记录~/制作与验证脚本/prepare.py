from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from fontTools.ttLib import TTFont
import hashlib,json,re,shutil,uuid,math

root=Path(__file__).resolve().parents[2]; work=Path(__file__).parent
art=root/'Assets/_Project/Art/Sprites/细胞绘制/提示图片化_2026-10-21_v1'
assert not (work/'plan.json').exists(), 'Production plan already exists.'
for name in ['字图','固定文案','面板','制作记录~/原始生成','制作记录~/字形来源','制作记录~/接入前备份']:
    (art/name).mkdir(parents=True,exist_ok=True)
targets=['FirstLevelTutorial','EnemyStatusHud','OpenFieldEntranceWaveButtons','OpenFieldBuildMenu','OpenFieldTowerMenu','MainMenuController','OpenFieldBattleHud']
sources=[root/f'Assets/_Project/Scripts/UI/{s}.cs' for s in targets]
sources.append(root/'Assets/_Project/Prefabs/Menu/OpenFieldBattleHud.prefab')
before=[]
for p in sources:
    rel=p.relative_to(root); dest=art/'制作记录~/接入前备份'/rel
    dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
    before.append(dict(path=rel.as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
font_path=next((work/'font-source').rglob('*zh_hans.ttf'))
font=ImageFont.truetype(str(font_path),12)
for p in (work/'font-source').rglob('*'):
    if p.is_file() and (p.suffix.lower() in ['.txt'] or p==font_path):
        target=art/'制作记录~/字形来源'/p.relative_to(work/'font-source')
        target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,target)
original=Path('C:/Users/a/.codex/generated_images/01a14fcf-47f7-7d81-9e68-5605e5720d6c/exec-78583892-d34b-4975-845e-89eb909b33ca.png')
shutil.copy2(original,art/'制作记录~/原始生成/提示面板_imagegen.png')
all_text=''.join(p.read_text(encoding='utf-8-sig') for p in (root/'Assets/_Project/Scripts').rglob('*.cs'))
for folder,pattern in [('Assets/_Project/Data','*.asset'),('Assets/_Project/Prefabs/Enemies','*.prefab')]:
    for p in (root/folder).rglob(pattern):
        value=p.read_text(encoding='utf-8-sig')
        all_text+=re.sub(r'\\u([0-9a-fA-F]{4})',lambda m:chr(int(m[1],16)),value)
chars=sorted(set(chr(i) for i in range(32,127)) | {c for c in all_text if '\u4e00'<=c<='\u9fff'} | set('·×？（）：，。；、％→＋—…'))
cmap=TTFont(font_path).getBestCmap()
missing=[c for c in chars if ord(c) not in cmap]
assert not missing,missing
glyphs=[]
for c in chars:
    mask=Image.new('1',(18,18));ImageDraw.Draw(mask).text((3,1),c,font=font,fill=1)
    bits=[y*18+x for y in range(18) for x in range(18) if mask.getpixel((x,y))]
    glyphs.append(dict(char=c,code=ord(c),advance=round(font.getlength(c),3),pixels=bits))
phrases={}
for source in sources[:-1]:
    text=source.read_text(encoding='utf-8-sig')
    for raw in re.findall(r'"((?:\\.|[^"\\])*)"',text):
        if not re.search(r'[\u4e00-\u9fff]',raw) or '{' in raw or '}' in raw:continue
        value=raw.replace('\\n','\n').replace('\\r','').replace('\\"','"').replace('\\t','  ')
        phrases.setdefault(value,264)
# Full, exact introduction cards are exported in addition to their reusable glyphs.
for p in (root/'Assets/_Project/Data').rglob('*Definition*.asset'):
    text=p.read_text(encoding='utf-8-sig')
    if '  enemyPrefab:' not in text:continue
    def field(key,default='0'):
        m=re.search(r'^  '+key+r': (.*)$',text,re.M)
        value=m[1].strip() if m else default
        return json.loads(value) if value.startswith('"') else value
    name=field('displayName','病原体'); boss=field('isBoss')=='1'; antigen=int(field('antigenId'))
    # EnemyDefinition.AntigenId values are read below from the source enum.
    enum=(root/'Assets/_Project/Scripts/Data/AntigenId.cs').read_text(encoding='utf-8-sig')
    enum_values={};current=0
    for label,number in re.findall(r'^\s*(\w+)\s*(?:=\s*(\d+))?\s*,?\s*$',enum,re.M):
        if label in ['public','enum']:continue
        current=int(number) if number else current;enum_values[label]=current;current+=1
    antigen_name=next((k for k,v in enum_values.items() if v==antigen),'')
    if boss:detail='受伤时播散孢子，留出清扫火力。' if antigen_name=='Fungus' else '为附近同伴补盾和治疗，兼顾两路压力。' if antigen_name=='Biofilm' else '复制小单位，变异后需要重新采样。'
    elif float(field('armorReduction'))>0:detail='外壳减伤，标记配合吞噬；酸化能破壳。'
    elif antigen_name=='Flagellate':detail='移动很快，接力火力与减速更有效。'
    elif field('killReward')=='0':detail='召唤单位也会伤害核心，需要及时清扫。'
    else:detail='成群进入，注意覆盖不同入口。'
    phrases[name+(' 即将进入' if boss else '')+'\n'+detail]=184
    phrases.setdefault(name,264)
for round in range(1,4):
    for action in ['开始','提前开始']:phrases[f'第{round}波\n{action}']=96
    for action in ['准备开波','守住核心']:phrases[f'第{round}轮 · {action}']=264
for count in range(3):phrases[f'第1轮 · 布置近卫 {count}/2']=264
for count in range(5):phrases[f'第3轮 · 增援近卫 {count}/4']=264

def layout(text,width):
    lines=[];line='';used=0
    for c in text:
        if c=='\n':lines.append(line);line='';used=0;continue
        advance=font.getlength(c)
        if used+advance>width and line:lines.append(line);line='';used=0
        line+=c;used+=advance
    lines.append(line)
    return lines,max(1,math.ceil(max(sum(font.getlength(c) for c in line) for line in lines)))+6,len(lines)*16
entries=[];x=y=row_height=0;atlas_width=1024
for i,(text,width) in enumerate(phrases.items()):
    lines,w,h=layout(text,width)
    if x+w+2>atlas_width:x=0;y+=row_height+2;row_height=0
    entries.append(dict(text=text,lines=lines,name=f'phrase_{i:03d}',x=x,y=y,width=w,height=h,fileId=21300000+i,spriteId=uuid.uuid4().hex))
    x+=w+2;row_height=max(row_height,h)
height=2**math.ceil(math.log2(y+row_height+2))
assert height<=2048,height
def guid(p):
    m=Path(str(p)+'.meta')
    if m.exists():return re.search(r'(?m)^guid: (\w+)',m.read_text(encoding='utf-8-sig'))[1]
    value=uuid.uuid4().hex
    m.write_text('fileFormatVersion: 2\nguid: '+value+'\n',encoding='utf-8')
    return value
guids={key:guid(root/f'Assets/_Project/Scripts/UI/{name}.cs') for key,name in [('catalogScript','PromptImageResources'),('labelScript','PixelImageText')]}
for key in ['glyphAtlas','phraseAtlas','panel','enemyPanel','button','selectedButton','catalog']:guids[key]=uuid.uuid4().hex
plan=dict(root=root.as_posix(),art=art.as_posix(),glyphs=glyphs,columns=56,cellSize=18,
          glyphWidth=1008,glyphHeight=math.ceil(len(glyphs)/56)*18,
          phrases=entries,phraseWidth=atlas_width,phraseHeight=height,guids=guids,before=before)
(work/'plan.json').write_text(json.dumps(plan,ensure_ascii=False),encoding='utf-8')
(art/'制作记录~/处理计划.json').write_text(json.dumps(plan,ensure_ascii=False),encoding='utf-8')
(art/'制作记录~/字形来源/来源说明.txt').write_text('Fusion Pixel 12px Monospaced zh_hans, official release 2026.09.01\nhttps://github.com/TakWolf/fusion-pixel-font/releases/tag/2026.09.01\nSIL Open Font License 1.1; licenses shipped alongside the unchanged font source.\nThe TTF is a production source only; runtime uses PNG artwork.\n',encoding='utf-8')
print(json.dumps(dict(glyphs=len(glyphs),phrases=len(entries),phraseAtlas=[atlas_width,height],art=art.as_posix()),ensure_ascii=False))
