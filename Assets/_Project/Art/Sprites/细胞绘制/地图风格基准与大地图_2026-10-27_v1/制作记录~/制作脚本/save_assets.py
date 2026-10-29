from pathlib import Path
from PIL import Image
from collections import deque
import hashlib, json, re, shutil, uuid

PROJECT = Path('E:/unity/CellDefense')
ROOT = PROJECT / 'Assets/_Project/Art/Sprites/细胞绘制/地图风格基准与大地图_2026-10-27_v1'
RECORDS = ROOT / '制作记录~'
TEMPLATE = PROJECT / 'Assets/_Project/Art/Sprites/细胞绘制/第二三章地图_前两关_2026-10-26_v1/第二章/2-1_凝血缺口.png.meta'
BASE_NATIVE = Path('C:/Users/a/.codex/generated_images/01a14fcf-47f7-7d81-9e68-5605e5720d6c/exec-e0514b69-e8db-4e38-96af-ab861566f7d4.png')
BASE_LARGE = Path('C:/Users/a/.codex/visualizations/2026/10/18/01a14fcf-47f7-7d81-9e68-5605e5720d6c/地图预览/2-1_棕色描边_3倍像素放大.png')

def write_json(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')

def new_copy(source, target):
    target.parent.mkdir(parents=True, exist_ok=True)
    if target.exists():
        assert target.read_bytes() == source.read_bytes(), f'Preserve existing file: {target}'
    else:
        shutil.copyfile(source, target)

def meta(path, columns):
    dest=Path(str(path)+'.meta')
    if dest.exists(): return
    width=Image.open(path).width
    text=TEMPLATE.read_text(encoding='utf-8')
    text=re.sub(r'(?m)^guid: \w+', 'guid: '+uuid.uuid4().hex, text)
    text=re.sub(r'spriteID: \w+', 'spriteID: '+uuid.uuid4().hex, text)
    text=re.sub(r'maxTextureSize: \d+', 'maxTextureSize: 8192', text)
    text=re.sub(r'spritePixelsToUnits: [\d.]+', f'spritePixelsToUnits: {width/columns:.9f}', text)
    dest.write_text(text, encoding='utf-8')

def baseline():
    target=ROOT / '风格基准/2-1_凝血缺口_棕边淡周边_4398x3219.png'
    new_copy(BASE_LARGE, target)
    meta(target,15)
    new_copy(BASE_NATIVE, RECORDS/'确认画风_生成原图_1466x1073.png')
    native=Image.open(BASE_NATIVE).convert('RGBA')
    enlarged=Image.open(target).convert('RGBA')
    assert enlarged.size==(native.width*3,native.height*3)
    p=native.load(); q=enlarged.load()
    assert all(p[x,y]==q[x*3,y*3] for y in range(native.height) for x in range(native.width))
    write_json(RECORDS/'风格确认与来源.json',{
        'date':'2026-10-27',
        'user_instruction':'这个画风先保存，之后的按这个修改，然后我觉得格数太少了，能不能做一个大地图',
        'confirmed_style':['清晰的像素色块与阶梯轮廓','暖棕色描边，避免近黑色或深紫描边','周边组织淡色、较低饱和度','米白组织膜、桃粉珊瑚细胞、低对比粉紫地面、青绿核心','障碍物偏矩形，周边较工整，实际占格须精确对齐'],
        'native_source':str(BASE_NATIVE),'native_size':list(native.size),
        'approved_upscaled_source':str(BASE_LARGE),'saved':str(target),
        'saved_size':list(enlarged.size),'upscale':'Aseprite --batch --scale 3 (nearest-neighbor; no new painted detail)',
        'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),
        'scope':'后续新绘制或修改的地图沿用本风格；旧图和旧 meta 保留；此记录不表示所有旧地图已完成替换。',
        'generation_prompt_provenance':'本次棕边图的原始内置 imagegen 提示词保存于 完整绘图提示词.json；此前渐进迭代见聊天记录。'
    })
    return target

def layout():
    width,height=30,22
    entry=(0,11); core=(25,11)
    islands=[{'id':'A','min':[7,10],'max':[9,17]},
             {'id':'B','min':[14,4],'max':[17,6]},
             {'id':'C','min':[20,7],'max':[22,13]},
             {'id':'D','min':[7,3],'max':[10,5]}]
    perimeter={(x,y) for x in range(width) for y in range(height)
               if x in (0,width-1) or y in (0,height-1)}-{entry}
    blocked=set(perimeter)
    for item in islands:
        x0,y0=item['min'];x1,y1=item['max']
        blocked.update((x,y) for x in range(x0,x1+1) for y in range(y0,y1+1))
    distance={core:0}; todo=deque([core])
    while todo:
        x,y=todo.popleft()
        for dx,dy in [(0,1),(1,0),(0,-1),(-1,0)]:
            n=x+dx,y+dy
            if 0<=n[0]<width and 0<=n[1]<height and n not in blocked and n not in distance:
                distance[n]=distance[(x,y)]+1;todo.append(n)
    assert len(distance)==width*height-len(blocked)
    assert entry in distance
    data={'version':'experimental-art-layout-v1','width':width,'height':height,'cellSize':1,
          'coordinate_origin':'lower-left; x right, y up','entry':list(entry),'core':list(core),
          'islands_inclusive':islands,'blockedCells':[list(c) for c in sorted(blocked)],
          'totalCells':width*height,'blockedCount':len(blocked),
          'initialBuildableCells':width*height-len(blocked)-2,
          'bfsDirectionOrder':['up','right','down','left'],
          'entryShortestPathSteps':distance[entry],
          'allUnblockedCellsReachCore':True,
          'validation_limit':'仅验证灰盒规划连通。生成美术可能偏移，尚未匹配碰撞或接入运行场景，也未验证触控、波次、经济与平衡。'}
    write_json(RECORDS/'30x22_逻辑布局与连通检查.json',data)
    return data

if __name__=='__main__':
    print(baseline())
    print(json.dumps({k:v for k,v in layout().items() if k in ('totalCells','initialBuildableCells','entryShortestPathSteps')},ensure_ascii=False))
