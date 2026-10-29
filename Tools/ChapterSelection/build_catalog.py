"""Wire existing sprites into the shared catalog; never rewrite art or its metadata."""
from pathlib import Path
import json,re,shutil
ROOT=Path(__file__).resolve().parents[2]
ART=ROOT/'Assets/_Project/Art/Sprites/细胞绘制'
V1=ART/'章节选择与三章选关_2026-10-26_v1'
V2=ART/'章节选关_序号与详情_2026-10-26_v2'
asset=ROOT/'Assets/_Project/Resources/LevelSelectionArtwork.asset'
log=ROOT/'Logs/ChapterSelection20261028'
log.mkdir(exist_ok=True)
backup=log/'LevelSelectionArtwork-before.asset'
if not backup.exists():shutil.copy2(asset,backup)
def sprite(p):
    assert p.exists(),p
    guid=re.search(r'^guid: (\w+)',Path(str(p)+'.meta').read_text(encoding='utf-8'),re.M).group(1)
    return '{fileID: 21300000, guid: '+guid+', type: 3}'
fields={'chapterBackground':V1/'背景/选章界面_纯背景.png','chapterTitle':V1/'文字/chapter_select.png',
        'chapterHint':V1/'文字/chapter_hint.png','chapterFrame':V1/'组件/卡片_常态.png',
        'chapterHighlight':V1/'组件/卡片_高亮.png','titlePlate':V1/'组件/标题底板.png',
        'backArrow':V1/'组件/返回箭头.png','homeCaption':V1/'文字/back_home.png','chaptersCaption':V1/'文字/back_chapters.png'}
text=asset.read_text(encoding='utf-8').split('  chapterBackground:')[0].rstrip()+'\n'
text+=''.join(f'  {key}: {sprite(path)}\n' for key,path in fields.items())
text+='  chapters:\n'
scenes=['OpenFieldPrototype','OpenFieldLevel02','OpenFieldLevel03','OpenFieldLevel04','OpenFieldLevel05']
playable={1:scenes,2:[f'Chapter02Level{i:02}' for i in range(1,5)],3:[f'Chapter03Level{i:02}' for i in range(1,5)]}
for chapter in json.loads((V1/'可扩展素材索引.json').read_text(encoding='utf-8'))['chapters']:
    n=chapter['id'];text+=f'  - id: "{n}"\n    name: '+json.dumps(chapter['name'],ensure_ascii=True)+'\n'
    mapping={'background':ROOT/chapter['backgroundImage'],'cover':ROOT/chapter['cover']}
    mapping.update({key:V1/f'文字/chapter{n}_{filename}.png' for key,filename in [('label','label'),('title','title'),('cardTitle','cardtitle'),('tagline','tagline'),('count','count')]})
    text+=''.join(f'    {key}: {sprite(path)}\n' for key,path in mapping.items())
    text+='    levels:\n'
    for i,level in enumerate(chapter['levels']):
        text+=f'    - id: "{level["id"]}"\n      name: '+json.dumps(level['name'],ensure_ascii=True)+'\n'
        text+='      sceneName: '+json.dumps(playable[n][i] if n in playable else '')+'\n'
        text+='      map: '+sprite(ROOT/level['map'])+'\n'
        text+='      title: '+sprite(V2/f'文字/{level["id"]}_detailtitle.png')+'\n'
asset.write_text(text,encoding='utf-8')
print('Linked 3 chapters / 13 maps with 13 playable scene references.')
