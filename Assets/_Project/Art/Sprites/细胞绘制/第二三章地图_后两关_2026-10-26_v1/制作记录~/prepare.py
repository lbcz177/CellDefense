from pathlib import Path
from collections import deque
import json, hashlib, shutil

root=Path('E:/unity/CellDefense')
previous=root/'Assets/_Project/Art/Sprites/细胞绘制/第二三章地图_前两关_2026-10-26_v1'
art=root/'Assets/_Project/Art/Sprites/细胞绘制/第二三章地图_后两关_2026-10-26_v1'
record=art/'制作记录~'
record.mkdir(parents=True,exist_ok=True)
for folder in ['第二章','第三章','预览~']:(art/folder).mkdir(exist_ok=True)
def block(x0,x1,y0,y1):return {(x,y) for x in range(x0,x1+1) for y in range(y0,y1+1)}
maps=[
 {'id':'2-3','name':'长廊窗口','chapter':'第二章','entries':[(0,6),(0,2)],'core':(12,5),
  'islands':[block(4,5,5,8),block(8,9,2,5)],'zones':[],
  'intent':'左侧主入口与下方次入口；两块错位组织岛构成弯曲接触区，连续侧翼建造面，核心前留恢复空间'},
 {'id':'2-4','name':'生物膜边缘','chapter':'第二章','entries':[(0,8),(5,0)],'core':(12,5),
  'islands':[block(3,4,5,6),block(7,8,2,3)],'zones':[],
  'intent':'左上主路与下侧次路通向宽中后战区，两块偏前小岛，侧前与核心前可形成交叉火力，无单一天然窄口'},
 {'id':'3-3','name':'过热前沿','chapter':'第三章','entries':[(0,6),(14,2)],'core':(11,5),
  'islands':[block(4,5,7,8),block(4,5,2,4),block(8,9,2,3)],
  'zones':[{'key':'主前线','cells':block(5,7,5,6),'center':[6.5,6],'radius':[1.48,.98]}],
  'intent':'左主入口穿过局部炎症前线，右下短次路需预告；区后分散安全建造面与侧翼保留清源和后备方案'},
 {'id':'3-4','name':'双灶组织','chapter':'第三章','entries':[(0,8),(14,8)],'core':(7,2),
  'islands':[block(6,8,7,8),block(1,3,2,5),block(11,13,2,5)],
  'zones':[{'key':'左前线','cells':block(3,5,6,7),'center':[4.5,7],'radius':[1.48,.98]},
           {'key':'右前线','cells':block(9,11,6,7),'center':[10.5,7],'radius':[1.48,.98]}],
  'intent':'左右对向入口，各自穿过一片不重叠区域，后中部核心；两侧独立前线，中央后场留有限共享支援带'}
]
directions=[(0,1),(1,0),(0,-1),(-1,0)]
def bfs(start,goal,blocked):
 q=deque([start]);parent={start:None}
 while q:
  c=q.popleft()
  if c==goal:
   path=[]
   while c is not None:path.append(c);c=parent[c]
   return path[::-1]
  for dx,dy in directions:
   n=(c[0]+dx,c[1]+dy)
   if 0<=n[0]<15 and 0<=n[1]<11 and n not in blocked and n not in parent:parent[n]=c;q.append(n)
 return None
for m in maps:
 boundary={(x,y) for x in range(15) for y in range(11) if x in (0,14) or y in (0,10)}-set(m['entries'])
 blocked=boundary|set().union(*m['islands'])
 paths=[bfs(e,m['core'],blocked) for e in m['entries']]
 assert all(paths),m['id']
 open_cells={(x,y) for x in range(15) for y in range(11)}-blocked
 assert all(bfs(c,m['core'],blocked) for c in open_cells)
 for zone in m['zones']:
  assert not zone['cells']&blocked
  assert any(zone['cells']&set(p) for p in paths),(m['id'],paths,zone)
  zone['cells']=sorted(zone['cells'])
 if m['id']=='3-3':
  assert len(paths[1])<len(paths[0])
  assert not set(paths[1])&set(m['zones'][0]['cells'])
 if m['id']=='3-4':
  a,b=[set(z['cells']) for z in m['zones']]
  assert not a&b and a&set(paths[0]) and b&set(paths[1])
  assert not a&set(paths[1]) and not b&set(paths[0])
  assert len(paths[0])==len(paths[1])
 m['blocked']=sorted(blocked);m['paths']=paths;m['pathLengths']=[len(p)-1 for p in paths]
 m['islands']=[sorted(i) for i in m['islands']]
 m['zone']=sorted(set().union(*(set(z['cells']) for z in m['zones']))) if m['zones'] else []
 m['guide']=(record/f"{m['id']}_空间参考.png").as_posix()
plan={'art':art.as_posix(),'size':[15,11],'origin':'bottom-left','maps':maps,'method':'four-neighbour BFS, Up Right Down Left; conceptual topology only, not a playable-scene or range validation'}
(record/'空间规划与通路检查.json').write_text(json.dumps(plan,ensure_ascii=False,indent=2),encoding='utf-8')
sources=[]
for name in ['Docs/GAME_DESIGN_FRAMEWORK.md','Docs/LevelPlanning/CHAPTER_FLOW_DISCUSSION_2026-10-26.md']:
 p=root/name;target=record/'策划快照'/p.name;target.parent.mkdir(exist_ok=True);shutil.copy2(p,target)
 sources.append({'path':name,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
(record/'来源与范围.json').write_text(json.dumps({'date':'2026-10-26','localVersion':'v0.28 / chapter discussion v0.3','scope':[m['id'] for m in maps],'sources':sources,'approvedPreviousArt':previous.as_posix(),'note':'Continue the user-approved first two maps of each chapter. No gameplay, scene, wave, remote Notion or publishing changes.'},ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({m['id']:m['pathLengths'] for m in maps}))
