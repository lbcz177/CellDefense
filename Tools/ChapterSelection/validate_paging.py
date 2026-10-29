"""Run chapter paging and existing menu regressions in the isolated Unity project."""
from pathlib import Path
import os
import runpy

root = Path(__file__).resolve().parents[2]
os.environ['CELLDEFENSE_CHAPTER_TWO_PROJECT'] = str(root / 'Logs/CodexTradingTests')
os.environ['CELLDEFENSE_VALIDATION_LOG_DIR'] = str(root / 'Logs/ChapterPaging20261029')
os.environ['CELLDEFENSE_CHAPTER_TWO_FILTER'] = ';'.join([
    'ChapterPagingPlayModeTests', 'ChapterSelectionPlayModeTests',
    'NumberedLevelSelectionPlayModeTests', 'MainMenuFocusPlayModeTests',
])
os.environ['CELLDEFENSE_CHAPTER_PAGING_CAPTURE'] = str(
    root / 'Assets/_Project/Art/Sprites/细胞绘制/章节翻页_2026-10-29_v1/验证'
)
runpy.run_path(str(root / 'Tools/ChapterTwo/validate.py'), run_name='__main__')
