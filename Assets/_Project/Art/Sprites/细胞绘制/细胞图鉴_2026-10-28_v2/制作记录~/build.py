from pathlib import Path
from PIL import ImageFont
from fontTools.ttLib import TTFont
import json,shutil,hashlib,os
ROOT=Path('E:/unity/CellDefense')
ART=Path(os.environ.get('CELLDEFENSE_CODEX_ART',str(ROOT/'Assets/_Project/Art/Sprites/细胞绘制/细胞图鉴_2026-10-28_v1')))
R=ART/'制作记录~'
plan=json.loads((R/'图鉴内容与来源.json').read_text(encoding='utf-8'))
plan['art']=ART.as_posix()
jobs=json.loads((R/'生成记录.json').read_text(encoding='utf-8'))
fontdir=ROOT/'Assets/_Project/Art/Sprites/细胞绘制/提示图片化_2026-10-21_v1/制作记录~/字形来源'
(R/'字形来源').mkdir(exist_ok=True)
for name in ['fusion-pixel-12px-monospaced-zh_hans.ttf','OFL.txt']:
 target=R/'字形来源'/name
 if not target.exists():shutil.copy2(fontdir/name,target)
font=ImageFont.truetype(str(fontdir/'fusion-pixel-12px-monospaced-zh_hans.ttf'),12)
cmap=TTFont(str(fontdir/'fusion-pixel-12px-monospaced-zh_hans.ttf')).getBestCmap()
texts=[]
def text(key,value,scale=2):
 assert all(c.isspace() or ord(c) in cmap for c in value),(key,value)
 mask=font.getmask(value,mode='1');w,h=mask.size
 assert w>0 and h>0,(key,value)
 texts.append({'key':key,'text':value,'width':w,'height':h,'pixels':[i for i,p in enumerate(mask) if p],'scale':scale})
 return key

nav={k:text(k,v,s) for k,v,s in [('title','细胞图鉴',3),('defenders','免疫防线',2),('invaders','入侵者',2),('home','返回主页',2),('index','返回目录',2),('previous','上一条',2),('next','下一条',2)]}
for e in plan['entries']:
 key=e['id'];e['caption']=text(key+'_caption',e['name'],2)
 e['drawText']=[]
 def add(suffix,value,x,y,scale=2,color='body'):
  k=text(key+'_'+suffix,value,scale)
  info=texts[-1]
  assert info['width']*scale<=740 if x>=460 else True,(key,value,info['width']*scale)
  e['drawText'].append({'key':k,'x':x,'y':y,'color':color})
 add('heading','细胞图鉴',104,48,3,'heading')
 add('category','免疫防线' if e['category']==0 else '入侵者',956,54,2,'accent')
 add('number',f"{e['number']:02d} / 14",1080,57,2)
 add('name',e['name'],500,118,4,'heading')
 add('role',e['role'],500,169,2,'accent')
 add('stat1',e['stats'][0],500,235)
 add('stat2',e['stats'][1],500,270)
 add('traits_label','战斗特性',500,335,2,'accent')
 for i,line in enumerate(e['traits']):add('trait'+str(i),line,500,373+i*34)
 add('tips_label','使用建议' if e['category']==0 else '应对建议',500,466,2,'accent')
 for i,line in enumerate(e['tactics']):add('tip'+str(i),line,500,510+i*34)
 add('note','基础属性，不含升级与关卡倍率',500,595,1,'muted')
 kind='防御单位' if e['category']==0 else ('首领' if e['data'].get('isBoss') else ('召唤物' if key in ['spore','clone'] else '普通敌人'))
 add('kind',kind,132,576,2,'accent')
 e['drawText'][-1]['x']=236-texts[-1]['width']
 e['portrait']=(ART/'立绘'/f"{e['number']:02d}_{e['name']}.png").as_posix()
 e['page']=(ART/'图鉴页'/f"{e['number']:02d}_{e['name']}_图鉴.png").as_posix()
 e['captionPath']=(ART/'文字'/f"{key}_名称.png").as_posix()
 for output in ['portrait','page','captionPath']:assert not Path(e[output]).exists(),e[output]

for j in jobs:
 target=R/'原始生成'/f"{j['id']}.png";target.parent.mkdir(exist_ok=True)
 source=Path(j['source'])
 if target.exists():assert target.read_bytes()==source.read_bytes()
 else:shutil.copy2(source,target)
 j['raw']=target.as_posix();j['sha256']=hashlib.sha256(source.read_bytes()).hexdigest()
plan.update({'jobs':jobs,'texts':texts,'navigation':nav})
(R/'组装计划.json').write_text(json.dumps(plan,ensure_ascii=False,indent=2),encoding='utf-8')
print('Prepared 14 accurate image-only pages and navigation captions.')
