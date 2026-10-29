from pathlib import Path
import json,hashlib,shutil,re,uuid
from PIL import Image

root=Path('E:/unity/CellDefense')
art=root/'Assets/_Project/Art/Sprites/细胞绘制/第二三章地图_后两关_2026-10-26_v1'
record=art/'制作记录~'
jobs=json.loads((record/'生成输出记录.json').read_text(encoding='utf-8'))
template=(root/'Assets/_Project/Art/Sprites/细胞绘制/前五关地图_2026-10-15_v1/01_守住伤口.png.meta').read_text(encoding='utf-8-sig')
def write_sprite_meta(p):
 meta=Path(str(p)+'.meta');assert not meta.exists(),meta
 im=Image.open(p)
 content=re.sub(r'^guid: \w+','guid: '+uuid.uuid4().hex,template,flags=re.M)
 content=re.sub(r'(?m)^  spritePixelsToUnits: .*','  spritePixelsToUnits: '+str(im.width/15),content)
 content=re.sub(r'(?m)^    spriteID: .*','    spriteID: '+uuid.uuid4().hex,content)
 content=content.replace('maxTextureSize: 2048','maxTextureSize: 4096')
 meta.write_text(content,encoding='utf-8')
for j in jobs:
 source=Path(j['source']);target=Path(j['target']);assert not target.exists(),target
 shutil.copy2(source,target)
 assert hashlib.sha256(source.read_bytes()).digest()==hashlib.sha256(target.read_bytes()).digest()
 im=Image.open(target);j['size']=list(im.size);j['sha256']=hashlib.sha256(target.read_bytes()).hexdigest();j['ppu']=im.width/15
 assert abs(im.width/im.height-15/11)/(15/11)<.005
 write_sprite_meta(target)
 print(j['id'],im.size,'copied with unique Sprite metadata')
(record/'交付清单.json').write_text(json.dumps(jobs,ensure_ascii=False,indent=2),encoding='utf-8')
plan=json.loads((record/'空间规划与通路检查.json').read_text(encoding='utf-8'))
for m in plan['maps']:
 j=next(j for j in jobs if j['id']==m['id']);m['image']=j['target'];m['size']=j['size']
 # Keep overlays off the final organic island silhouettes, which differ from square guides.
 if m['id']=='3-3':
  m['zones'][0]['cells']=[[x,y] for x in range(6,9) for y in range(5,7)]
  m['zones'][0]['center']=[7.7,6];m['zones'][0]['radius']=[1.1,.95]
  m['overlayAlignment']='Front region shifted right within the same contact area to clear the generated upper island tip. Artwork alignment, not final collision data.'
 if m['id']=='3-4':
  for z,x0,x1,cx in zip(m['zones'],[3,9],[5,11],[4.5,10.5]):
   z['cells']=[[x,y] for x in range(x0,x1+1) for y in range(5,7)]
   z['center']=[cx,6];z['radius']=[1.48,.95]
  m['overlayAlignment']='Both local front regions sit below the generated central island and above rear shoulders; left/right remain independent. Artwork alignment, not final collision data.'
 for z in m['zones']:
  z['overlay']=(art/'第三章'/f"{m['id']}_炎症区域_{z['key']}_独立覆盖.png").as_posix()
(record/'叠层与预览计划.json').write_text(json.dumps(plan,ensure_ascii=False,indent=2),encoding='utf-8')
