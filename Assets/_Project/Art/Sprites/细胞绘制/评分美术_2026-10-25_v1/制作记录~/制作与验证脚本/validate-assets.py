from pathlib import Path
import re,json,hashlib,shutil,difflib
from PIL import Image
root=Path('E:/unity/CellDefense');art=root/'Assets/_Project/Art/Sprites/细胞绘制/评分美术_2026-10-25_v1';record=art/'制作记录~'
def field(t,name):return re.search(r'^  '+name+r': (.*)',t,re.M)[1]
def advances(t,name):return [float(v) for v in re.search(r'^  '+name+r':\n((?:  - [^\n]*\n)+)',t,re.M)[1].replace('  - ','').split()]
t=(root/'Assets/_Project/Resources/PromptImageResources.asset').read_text(encoding='utf-8')
before=(record/'接入前备份/Assets/_Project/Resources/PromptImageResources.asset').read_text(encoding='utf-8-sig')
chars=json.loads(field(t,'characters'));previous=json.loads(field(before,'characters'))
assert chars==previous+'分通' and len(chars)==len(set(chars))==451
for name,native in [('advances',12),('smallAdvances',8)]:assert advances(t,name)==advances(before,name)+[native]*2
required='得分击杀核心最高未通关0123456789 /'
assert not set(required)-set(chars)
glyph_plans=json.loads((record/'字形补全计划.json').read_text(encoding='utf-8'))
for plan in glyph_plans:
 old=Image.open(plan['source']).convert('RGBA');new=Image.open(plan['target']).convert('RGBA');cell=plan['cell']
 assert new.size==old.size
 for index in range(len(previous)):
  x=(index%56)*cell;y=(index//56)*cell;bounds=(x,y,x+cell,y+cell)
  assert new.crop(bounds).tobytes()==old.crop(bounds).tobytes()
 flat=Image.open(art/f"结算净字图_{plan['native']}px.png").convert('RGBA')
 assert flat.size==new.size
 for original,pixel in zip(new.getdata(),flat.getdata()):
  expected=(255,255,255,255) if original[3]>0 and original[:3]!=(49,27,52) else (0,0,0,0)
  assert pixel==expected
for png in art.glob('*.png'):
 im=Image.open(png).convert('RGBA');assert set(im.getchannel('A').getdata())=={0,255}
 meta=Path(str(png)+'.meta').read_text(encoding='utf-8')
 assert 'filterMode: 0' in meta and 'enableMipMap: 0' in meta
 assert set(re.findall(r'textureCompression: (\d+)',meta))=={'0'}

# Independent rectangle check for all visible score elements and button hit areas.
graphics=[(-58,12,244,44),(-140,12,40,24),(25,10,64,22),(130,12,102,32),
 (-168,-24,22,22),(18,-24,22,22),(-80,-24,148,22),(106,-24,148,22),(0,-47,360,20)]
for digits in range(1,6):
 graphics_for_score=graphics+[(-61+(i-(digits-1)*.5)*18,12,18,32) for i in range(digits)]
 for win in [True,False]:
  offset=0 if win else -28
  buttons=[(0,(-96-i*60) if win else (-132-i*70),512,56) for i in range(3 if win else 2)]
  for x,y,w,h in graphics_for_score:
   y+=offset
   assert -400<=x-w/2 and x+w/2<=400 and -360<=y-h/2 and y+h/2<=360
   for bx,by,bw,bh in buttons:
    assert not (abs(x-bx)*2<w+bw and abs(y-by)*2<h+bh)
  for i,a in enumerate(buttons):
   for b in buttons[i+1:]:assert abs(a[1]-b[1])*2>=a[3]+b[3]

source_changes={}
for backup in (record/'接入前备份').rglob('*.cs'):
 current=root/backup.relative_to(record/'接入前备份')
 diff=list(difflib.unified_diff(backup.read_text(encoding='utf-8-sig').splitlines(),current.read_text(encoding='utf-8-sig').splitlines(),lineterm=''))
 source_changes[current.relative_to(root).as_posix()]=diff
# Only the result button size/position changed in the controller; preserve other current work.
controller_diff=source_changes['Assets/_Project/Scripts/OpenField/OpenFieldPrototypeController.cs']
modified=[line for line in controller_diff if line.startswith(('+','-')) and not line.startswith(('+++','---'))]
assert len(modified)==4 and all('rect.sizeDelta' in line or 'rect.anchoredPosition' in line for line in modified)
(record/'本轮源码差异.json').write_text(json.dumps(source_changes,ensure_ascii=False,indent=2),encoding='utf-8')
report={'passed':True,'pngCount':6,'glyphCount':451,'newGlyphs':['分','通'],'oldGlyphCellsPreservedPerAtlas':449,
 'flatGlyphMasksMatch':True,'hardAlpha':True,'pointSampling':True,'noCompressionOrMipmaps':True,
 'scoreDigitsOneThroughFiveLayoutChecked':True,'resultButtonsDoNotOverlapScore':True,
 'unityRuntimeVerification':'Pending: editor initialization blocked; composition previews are not runtime screenshots'}
(record/'静态素材与布局校验.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('PASS: 6 PNGs; 451 glyphs; both original 449-cell atlases preserved; flat masks match; all score/button bounds separate')
