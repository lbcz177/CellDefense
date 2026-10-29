from pathlib import Path
import re,json,hashlib
ROOT=Path('E:/unity/CellDefense')
ART=ROOT/'Assets/_Project/Art/Sprites/细胞绘制/细胞图鉴_2026-10-28_v1'
for name in ['立绘','图鉴页','文字','组件','源文件','制作记录~','验证~']:(ART/name).mkdir(parents=True,exist_ok=True)
ICON=ROOT/'Assets/_Project/Art/Sprites/细胞绘制/像素精修_2026-10-21_v1/图标'
ANIM=ROOT/'Assets/_Project/Art/Sprites/细胞绘制/像素精修_2026-10-21_v1/动画'
rows=[
 ('neutrophil','TowerDefinition','01_近卫.png','前线采样 · 单体输出',
  ['命中敌人时采集其当前抗原。','向三格内一跳邻居共享采样。'],['与抗体塔相邻布置，建立识别链。','射程交叉覆盖路线，分担前线压力。']),
 ('antibody','InterceptorTowerDefinition','06_抗体细胞.png','抗原记忆 · 标记增伤',
  ['记住共享的抗原八秒。','标记已知目标四秒，使其多受伤害15%。'],['需要近卫先采样，再进行标记。','与巨噬细胞配合，提高吞噬门槛。']),
 ('phagocyte','TowerDefinition 1','05_巨噬细胞.png','前排承伤 · 吞噬清除',
  ['可吞噬生命不高于20的普通目标。','标记后门槛提高到50，消化需三秒。'],['可同时拦截两名敌人。','首领、有护盾或免疫目标不能吞噬。']),
 ('fibrin','FibrinTowerDefinition','02_纤维蛋白壁垒.png','拦截全部 · 范围减速',
  ['拦截经过自身位置的全部敌人。','射程内减速40%，不改变行进路线。'],['放在主路交战点，为后排争取时间。','生命耗尽后及时补建；自身不输出。']),
 ('lysosome','AcidTowerDefinition','03_酸化炮.png','持续酸区 · 装甲破壳',
  ['命中后留下持续三秒的酸区。','酸区每秒造成10点伤害，可叠加。'],['配合壁垒集中敌人，发挥范围伤害。','酸伤可以破除重甲敌人的装甲。']),
 ('neural','NeuralPulseTowerDefinition','04_神经脉冲.png','连锁打击 · 短暂停顿',
  ['攻击最多连锁三个目标。','伤害逐跳递减，并使目标停顿0.2秒。'],['适合处理靠近的一群敌人。','连锁半径2.5格，配合聚怪位置。']),
 ('staph','StaphylococcusDefinition','07_staph_move','基础入侵者 · 群体推进',
  ['常规敌人，生命和速度较均衡。','葡萄状簇集外形便于辨认。'],['用交叉火力持续削减数量。','配合壁垒和范围酸区处理成群目标。']),
 ('flagellate','FlagellateDefinition','08_flagellate_move','高速入侵者 · 快速穿线',
  ['基础速度较快，但生命较低。','双球体与鞭毛是主要识别特征。'],['提前布置火力，不要等接近核心。','用减速或停顿增加输出时间。']),
 ('bacillus','ArmoredBacillusDefinition','09_bacillus_move','重甲入侵者 · 抗伤推进',
  ['完整装甲减少25%的所受伤害。','酸性伤害可以使装甲暂时失效。'],['先用酸化炮破壳，再集中输出。','减速和拦截能延长破壳窗口。']),
 ('spore','FungalSporeDefinition','10_fungal_spore_move','真菌召唤物 · 快速增援',
  ['由变异超级真菌在阶段变化时召唤。','生命较低、移动较快，击杀不奖励ATP。'],['保留范围输出，及时清理增援。','召唤物不计入计划敌人的评分数量。']),
 ('clone','VirusCloneDefinition','11_virus_clone_move','病毒召唤物 · 分散压力',
  ['由病毒母体生成的复制体。','移动较快，击杀不奖励ATP。'],['兼顾母体与复制体，防止漏怪。','召唤物不计入计划敌人的评分数量。']),
 ('fungal','FungalBossDefinition','12_fungal_boss_move','首领 · 孢子增援',
  ['两次阶段变化各召唤三枚孢子。','后期移动加快，不能被巨噬吞噬。'],['以壁垒承伤，并预留清理孢子的火力。','集中主目标时兼顾周围小型增援。']),
 ('membrane','MembraneBossDefinition','13_membrane_boss_move','首领 · 护盾支援',
  ['周期性补充护盾并恢复生命。','还会支援附近最多三名普通敌人。'],['集中火力，避免长时间分散输出。','先打掉护盾，再持续压低其生命。']),
 ('virus','VirusBossDefinition','14_virus_boss_move','首领 · 复制与变异',
  ['生成复制体，并在阶段变化时换抗原。','后期移动加快，不能被巨噬吞噬。'],['变异后重新采样，恢复抗体标记。','准备范围伤害，防止复制体穿过防线。'])]

def fields(path):
 out={}
 for key,value in re.findall(r'^  (\w+): (.+)$',path.read_text(encoding='utf-8-sig'),re.M):
  try:value=json.loads(value)
  except (ValueError,TypeError):pass
  out[key]=value
 return out

entries=[]
for index,(slug,asset,reference,role,traits,tactics) in enumerate(rows):
 friendly=index<6
 source=ROOT/f'Assets/_Project/Data/{"Towers" if friendly else "Enemies"}/{asset}.asset'
 f=fields(source)
 if friendly:
  reference=ICON/reference
  stats=[f"建造 {f['buildCost']} ATP    生命 {f.get('maxHealth',100)}",f"伤害 {f['damage']}    射程 {f['attackRange']} 格"]
 else:
  reference=ANIM/reference/'animation_sheet.png'
  stats=[f"生命 {f['maxHealth']}    基础速度 {f['baseMoveSpeed']}",f"击杀 {f['killReward']} ATP    漏怪损伤 {f['leakDamage']}"]
 assert reference.exists(),reference
 entries.append({'id':slug,'number':index+1,'category':0 if friendly else 1,'name':f['displayName'],
  'role':role,'stats':stats,'traits':traits,'tactics':tactics,'reference':reference.as_posix(),
  'source':source.relative_to(ROOT).as_posix(),'sourceSha256':hashlib.sha256(source.read_bytes()).hexdigest(),
  'data':{k:f[k] for k in ['buildCost','maxHealth','damage','attackRange','attackInterval','baseMoveSpeed','killReward','leakDamage','isBoss'] if k in f}})
plan={'date':'2026-10-28','art':ART.as_posix(),'entries':entries,'canvas':[1280,720],
 'scope':'6 implemented defenses + 8 current enemies including bosses and summons; exclude duplicate legacy placeholder definitions and planned units',
 'footer':'基础属性，不含升级与关卡倍率'}
(ART/'制作记录~/图鉴内容与来源.json').write_text(json.dumps(plan,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(plan,ensure_ascii=True))
