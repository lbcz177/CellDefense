from pathlib import Path
from collections import deque
import json,hashlib,shutil

root=Path('E:/unity/CellDefense')
art=root/'Assets/_Project/Art/Sprites/细胞绘制/第二三章地图_前两关_2026-10-26_v1'
record=art/'制作记录~';record.mkdir(parents=True,exist_ok=True)
for folder in ['第二章','第三章','预览~']:(art/folder).mkdir(exist_ok=True)
def block(x0,x1,y0,y1):return {(x,y) for x in range(x0,x1+1) for y in range(y0,y1+1)}
maps=[
 {'id':'2-1','name':'凝血缺口','chapter':'第二章','entries':[(0,5)],'core':(12,5),'islands':[block(4,5,5,8),block(9,10,2,5)],'zone':set(),'checks':[(5,4),(9,6)],'intent':'先后经过前后两处交战点，两处侧翼及中间缓冲均留可建空间'},
 {'id':'2-2','name':'厚膜岔口','chapter':'第二章','entries':[(0,8),(0,2)],'core':(12,5),'islands':[block(3,9,4,6)],'zone':set(),'checks':[],'intent':'宽组织岛将上下两路分开，核心前才接近；前置分路与后置共享之间取舍'},
 {'id':'3-1','name':'充血边缘','chapter':'第三章','entries':[(0,5)],'core':(12,5),'islands':[block(5,6,7,8),block(8,9,2,3)],'zone':block(5,7,4,6),'checks':[(6,5)],'intent':'单路经过一片局部炎症区，区外侧翼可攻击区内来源，核心前留后备防线'},
 {'id':'3-2','name':'反应交汇','chapter':'第三章','entries':[(0,8),(0,2)],'core':(12,5),'islands':[block(4,8,4,6),block(11,12,7,8),block(11,12,2,3)],'zone':block(8,10,6,8),'checks':[],'intent':'双路在核心前靠近，炎症区偏上方主路，共享支援空地有限；下侧留稳定方案'}
]
directions=[(0,1),(1,0),(0,-1),(-1,0)]
def bfs(start,goal,blocked):
 q=deque([start]);parents={start:None}
 while q:
  c=q.popleft()
  if c==goal:
   path=[]
   while c is not None:path.append(c);c=parents[c]
   return path[::-1]
  for dx,dy in directions:
   n=(c[0]+dx,c[1]+dy)
   if 0<=n[0]<15 and 0<=n[1]<11 and n not in blocked and n not in parents:parents[n]=c;q.append(n)
 return None
for m in maps:
 boundary={(x,y) for x in range(15) for y in range(11) if x in (0,14) or y in (0,10)}
 boundary-=set(m['entries'])
 blocked=boundary|set().union(*m['islands'])
 paths=[bfs(e,m['core'],blocked) for e in m['entries']]
 assert all(paths)
 assert all(p in paths[0] for p in m['checks']), (m['id'],paths)
 all_open={(x,y) for x in range(15) for y in range(11)}-blocked
 assert all(bfs(cell,m['core'],blocked) for cell in all_open)
 if m['id']=='2-2':assert len(paths[0])==len(paths[1])
 if m['zone']:
  assert any(set(p)&m['zone'] for p in paths)
  if m['id']=='3-2':assert not (set(paths[1])&m['zone'])
 if m['id']=='3-1':
  assert (6,3) in all_open-m['zone'] and (6,4) in all_open&m['zone']
  assert (6-6)**2+(3-5)**2<=2.5**2
 m['blocked']=sorted(blocked);m['paths']=paths;m['pathLengths']=[len(p)-1 for p in paths]
 m['islands']=[sorted(cells) for cells in m['islands']];m['zone']=sorted(m['zone'])
 m.pop('checks');m['guide']=str(record/f"{m['id']}_空间参考.png").replace('\\','/')
plan={'art':str(art).replace('\\','/'),'size':[15,11],'origin':'bottom-left','maps':maps,'method':'four-neighbour BFS, Up Right Down Left; static conceptual layout only, not a playable-scene validation'}
(record/'空间规划与通路检查.json').write_text(json.dumps(plan,ensure_ascii=False,indent=2),encoding='utf-8')
sources=[]
for name in ['Docs/GAME_DESIGN_FRAMEWORK.md','Docs/LevelPlanning/CHAPTER_FLOW_DISCUSSION_2026-10-26.md']:
 p=root/name;target=record/'策划快照'/p.name;target.parent.mkdir(exist_ok=True);shutil.copy2(p,target)
 sources.append({'path':name,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
(record/'来源与范围.json').write_text(json.dumps({'date':'2026-10-26','localVersion':'v0.28 / chapter discussion v0.3','scope':['2-1','2-2','3-1','3-2'],'sources':sources,'notionArt':'https://app.notion.com/p/3b8c4f951620817cb407f1726dc3df97','notionMaps':'https://app.notion.com/p/3b8c4f951620815aa491d632803bd02b','note':'Notion map/page versions predate the local chapter spec; legacy fixed tower pads and route-changing rules do not apply.'},ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({m['id']:m['pathLengths'] for m in maps}))
