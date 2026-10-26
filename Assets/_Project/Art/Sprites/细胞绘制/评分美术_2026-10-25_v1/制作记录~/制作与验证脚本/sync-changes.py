from pathlib import Path
import shutil, re
root=Path('E:/unity/CellDefense')
isolated=root/'Logs/CodexTradingTests'
art=root/'Assets/_Project/Art/Sprites/细胞绘制/评分美术_2026-10-25_v1'
paths=[root/p for p in [
 'Assets/_Project/Scripts/UI/LevelResultScoreView.cs',
 'Assets/_Project/Scripts/UI/PixelImageText.cs',
 'Assets/_Project/Scripts/UI/StarRatingGraphic.cs',
 'Assets/_Project/Scripts/UI/ScoreArtworkResources.cs',
 'Assets/_Project/Scripts/OpenField/OpenFieldPrototypeController.cs',
 'Assets/_Project/Tests/PlayMode/LevelScoringPlayModeTests.cs',
 'Assets/_Project/Resources/ScoreArtworkResources.asset',
 'Assets/_Project/Resources/PromptImageResources.asset']]
paths+=list(art.glob('*.*'))
for p in list(paths):
 meta=Path(str(p)+'.meta')
 if meta.exists(): paths.append(meta)
for p in set(paths):
 if not p.is_file():continue
 target=isolated/p.relative_to(root);target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,target)
settings=isolated/'ProjectSettings/ProjectSettings.asset'
text=settings.read_text(encoding='utf-8')
text=re.sub(r'(?m)^  companyName:.*','  companyName: CellDefenseValidation',text)
text=re.sub(r'(?m)^  productName:.*','  productName: ScoreFlowTests',text)
settings.write_text(text,encoding='utf-8')
print('Synced current score art integration; protected main PlayerPrefs namespace')
