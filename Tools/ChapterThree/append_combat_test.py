from pathlib import Path
r=Path(__file__).resolve().parents[2]
p=r/'Assets/_Project/Tests/PlayMode/ChapterThreePlayModeTests.cs'
s=p.read_text(encoding='utf-8');source=(r/'Assets/_Project/Tests/PlayMode/ChapterTwoPlayModeTests.cs').read_text(encoding='utf-8')
start=source.index('    [UnityTest] public IEnumerator ResourceFundedDefenses')
end=source.index('    [UnityTearDown]',start)
method=source[start:end].replace('Second','Third').replace('CHAPTER_TWO_DEFENSE','CHAPTER_THREE_DEFENSE')
method=method.replace('new Vector2Int(12,5)','(Vector2Int)Field(owner,"coreCell")')
marker='    // Normal resource-funded combat regression is appended by the preparation tool.\n'
assert marker in s;p.write_text(s.replace(marker,method),encoding='utf-8')
