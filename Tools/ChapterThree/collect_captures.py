from pathlib import Path
import shutil
r=Path(__file__).resolve().parents[2]
dest=r/'Assets/_Project/Art/Sprites/细胞绘制/第三章接入_2026-10-29_v1/验证';dest.mkdir(parents=True,exist_ok=True)
for first in (r/'Assets/_Project/Art/Sprites').rglob('Chapter03Level01-planning.png'):
    if first.parent==dest:continue
    for p in first.parent.glob('*.png'):
        if not (dest/p.name).exists():shutil.copy2(p,dest/p.name)
raw=r/'Logs/CodexTradingTests/Logs/PauseArtwork'
for pattern in ['Chapter03Level*.png','ten-towers-*.png','inflammation-warning-and-damage.png']:
    for p in raw.glob(pattern):shutil.copy2(p,dest/p.name)
print(str(dest))
