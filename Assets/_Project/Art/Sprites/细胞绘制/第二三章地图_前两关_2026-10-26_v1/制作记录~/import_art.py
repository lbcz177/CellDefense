from pathlib import Path
import json,hashlib,shutil,re,uuid
from PIL import Image
root=Path('E:/unity/CellDefense');art=root/'Assets/_Project/Art/Sprites/细胞绘制/第二三章地图_前两关_2026-10-26_v1';record=art/'制作记录~'
deliveries=json.loads((record/'生成输出记录.json').read_text(encoding='utf-8'))
template=(root/'Assets/_Project/Art/Sprites/细胞绘制/前五关地图_2026-10-15_v1/01_守住伤口.png.meta').read_text(encoding='utf-8-sig')
for job in deliveries:
 source=Path(job['source']);target=Path(job['target']);assert not target.exists()
 shutil.copy2(source,target)
 assert hashlib.sha256(source.read_bytes()).digest()==hashlib.sha256(target.read_bytes()).digest()
 im=Image.open(target);job['size']=list(im.size);job['sha256']=hashlib.sha256(target.read_bytes()).hexdigest()
 job['guid']=uuid.uuid4().hex;job['ppu']=im.width/15
 meta=re.sub(r'^guid: \w+','guid: '+job['guid'],template,flags=re.M)
 meta=re.sub(r'(?m)^  spritePixelsToUnits: .*','  spritePixelsToUnits: '+str(job['ppu']),meta)
 meta=meta.replace('maxTextureSize: 2048','maxTextureSize: 4096')
 Path(str(target)+'.meta').write_text(meta,encoding='utf-8')
 print(job['id'],im.size,'copied and Sprite metadata ready')
(record/'交付清单.json').write_text(json.dumps(deliveries,ensure_ascii=False,indent=2),encoding='utf-8')
plan=json.loads((record/'空间规划与通路检查.json').read_text(encoding='utf-8'))
for entry in plan['maps']:
 job=next(j for j in deliveries if j['id']==entry['id'])
 entry['image']=job['target'];entry['size']=job['size']
 entry['overlayCells']=[cell for cell in entry['zone'] if cell not in entry['blocked']]
 entry['overlay']=str(art/'第三章'/f"{entry['id']}_炎症区域_独立覆盖.png").replace('\\','/')
(record/'叠层与预览计划.json').write_text(json.dumps(plan,ensure_ascii=False,indent=2),encoding='utf-8')
