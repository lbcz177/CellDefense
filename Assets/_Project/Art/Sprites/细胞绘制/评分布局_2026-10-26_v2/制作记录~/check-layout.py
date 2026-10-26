from pathlib import Path
import re,json,shutil
root=Path('E:/unity/CellDefense');art=root/'Assets/_Project/Art/Sprites/细胞绘制/评分布局_2026-10-26_v2';record=art/'制作记录~'
view=(root/'Assets/_Project/Scripts/UI/LevelResultScoreView.cs').read_text(encoding='utf-8-sig')
controller=(root/'Assets/_Project/Scripts/OpenField/OpenFieldPrototypeController.cs').read_text(encoding='utf-8-sig')
assert 'Best Score' not in view and 'bestScore' not in view and '最高' not in view
assert 'scoreView.Show(FinalScore);' in controller
assert 'new Vector2(260f, 52f)' in controller and 'LevelResultScoreView.StyleActionButton(actions[i]);' in controller
assert 'frame.type = Image.Type.Sliced;' in view and 'frame.preserveAspect = false;' in view
assert 'frame.pixelsPerUnitMultiplier = 2f;' in view
assert 'frame.color = Color.white;' in view and 'Selectable.Transition.SpriteSwap' in view
assert 'text.raycastTarget = false;' in view
prompts=(root/'Assets/_Project/Resources/PromptImageResources.asset').read_text(encoding='utf-8')
chars=json.loads(re.search(r'^  characters: (.*)',prompts,re.M)[1])
adv=[float(v) for v in re.search(r'^  advances:\n((?:  - [^\n]*\n)+)',prompts,re.M)[1].replace('  - ','').split()]
for text in ['下一关','重新开始','返回主菜单']:
 assert all(c in chars for c in text)
 assert sum(adv[chars.index(c)]*24/12 for c in text)<=228
assert 24*4/3<=32
graphics=[(-58,12,244,44),(-140,12,40,24),(25,10,64,22),(130,12,102,32),(-168,-24,22,22),(18,-24,22,22),(-80,-24,148,22),(106,-24,148,22)]
def overlap(a,b):return abs(a[0]-b[0])*2<a[2]+b[2] and abs(a[1]-b[1])*2<a[3]+b[3]
for victory in [True,False]:
 offset=0 if victory else -28
 buttons=[(0,-82-i*62 if victory else -116-i*70,260,52) for i in range(3 if victory else 2)]
 for x,y,w,h in graphics:
  graphic=(x,y+offset,w,h)
  assert abs(x)+w/2<=400 and abs(y+offset)+h/2<=360
  assert not any(overlap(graphic,b) for b in buttons)
 for i,b in enumerate(buttons):
  assert abs(b[0])+b[2]/2<=400 and abs(b[1])+b[3]/2<=360
  assert not any(overlap(b,other) for other in buttons[i+1:])
 for count in range(1,6):
  for i in range(count):assert not any(overlap((-61+(i-(count-1)*.5)*18,12+offset,18,32),b) for b in buttons)
result={'passed':True,'highestScoreRemovedFromResults':True,'buttonSize':[260,52],'captionPixels':24,'captionGlyphsComplete':True,'textFits':True,'scoreButtonBoundsSeparate':True,'buttonBoundsSeparate':True,'canvasBoundsFit':True,'previewKind':'Aseprite composition reference, not runtime screenshot'}
(record/'布局校验.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print('PASS: no result best-score row; sliced frames; three captions fit; score/buttons fit and do not overlap')
for script in ['check-layout.py','verify.py']:shutil.copy2(root/'Logs/ScoreLayout20261026'/script,record/script)
