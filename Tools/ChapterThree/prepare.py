from pathlib import Path
import shutil,subprocess,sys
root=Path(__file__).resolve().parents[2]
source=(root/'Tools/ChapterTwo/prepare_glyphs.py').read_text(encoding='utf-8')
source=source.replace('第二章接入_2026-10-28_v1','第三章接入_2026-10-29_v1')
source=source.replace('ChapterTwoContentBuilder.cs','ChapterThreeContentBuilder.cs')
source=source.replace("'UI/ChapterTwoBriefing.cs',","'OpenField/ChapterThreeInflammation.cs','Towers/ImmuneSupportBehaviour.cs',")
script=Path(__file__).with_name('prepare_glyphs.py');script.write_text(source,encoding='utf-8')
for png in (root/'Assets/_Project/Art/Sprites/细胞绘制').glob('第二三章地图*/第三章/*独立覆盖.png'):
    for path in [png,Path(str(png)+'.meta')]:
        dest=root/'Logs/CodexTradingTests'/path.relative_to(root)
        dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(path,dest)
subprocess.run([sys.executable,str(script)],check=True)
subprocess.run([sys.executable,str(Path(__file__).with_name('fix_glyph_canvas.py'))],check=True)
