from pathlib import Path
import re,json
from PIL import Image
root=Path('E:/unity/CellDefense');art=root/'Assets/_Project/Art/Sprites/细胞绘制/评分美术_2026-10-25_v1'
mapping={}
for p in (root/'Assets/_Project/Art/Sprites').rglob('*.png.meta'):
 if any(part.endswith('~') for part in p.parts):continue
 m=re.search(r'^guid: (\w+)',p.read_text(encoding='utf-8-sig'),re.M)
 if m:mapping[m[1]]=str(p)[:-5].replace('\\','/')
def path(guid):return mapping[guid]
def ref(text,field):return path(re.search(r'^  '+field+r': .*guid: (\w+)',text,re.M)[1])
prompts=(root/'Assets/_Project/Resources/PromptImageResources.asset').read_text(encoding='utf-8')
hud=(root/'Assets/_Project/Resources/OpenFieldUiResources.asset').read_text(encoding='utf-8-sig')
chars=json.loads(re.search(r'^  characters: (.*)',prompts,re.M)[1])
advances=[float(n) for n in re.search(r'^  advances:\n((?:  - [^\n]*\n)+)',prompts,re.M)[1].replace('  - ','').split()]
assert len(chars)==len(advances)==451
data={'art':str(art).replace('\\','/'),'characters':chars,'advances':advances,'glyphAtlas':ref(prompts,'glyphAtlas'),
 'background':str(root/'Assets/_Project/Art/Sprites/细胞绘制/关卡评星_2026-10-25/完整回归_v4~/score-False-Victory-1280x720.png').replace('\\','/'),
 'victory':path('6a6d3789f8d24b85ad53504c1676194b'),'defeat':path('276cd86c2c4948c9810f4d0d9dd6b888'),
 'next':path('20936ae6f0fc469693148daf9ec42a53'),'retry':path('602f567cda9a4af8a6529412aa1611e0'),'menu':path('1e5b3d93ba1b4229aa977267d56ddd12'),
 'life':ref(hud,'lifeIcon'),'kill':ref(hud,'waveIcon')}
data['digits']=[path(g) for g in re.findall(r'guid: (\w+)',re.search(r'^  digits:\n((?:  - [^\n]*\n){10})',hud,re.M)[1])]
assert len(data['digits'])==10
(art/'制作记录~/组合预览计划.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
for name in ['victory','defeat','next','retry','menu']:
 print(name,Image.open(data[name]).size)
