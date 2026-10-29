from pathlib import Path
p=Path(__file__).resolve().parents[2]/'Assets/_Project/Tests/PlayMode/ChapterSelectionPlayModeTests.cs'
s=p.read_text(encoding='utf-8-sig')
s=s.replace('foreach(string scene in Scenes){string key=Key(scene);',
    'foreach(string scene in Scenes.Concat(Enumerable.Range(1,4).Select(i=>"Chapter02Level"+i.ToString("00"))).Concat(Enumerable.Range(1,4).Select(i=>"Chapter03Level"+i.ToString("00")))){string key=Key(scene);')
s=s.replace('foreach(string scene in Scenes)PlayerPrefs.SetInt(Key(scene),100);',
    'foreach(string scene in Scenes.Concat(Enumerable.Range(1,4).Select(i=>"Chapter02Level"+i.ToString("00"))))PlayerPrefs.SetInt(Key(scene),100);')
start=s.index('    [UnityTest] public IEnumerator ThirdChapterPreviewsAllMapsWithoutStartingOrInventingProgress()')
end=s.index('    [UnityTest]',start+20)
s=s[:start]+'''    [UnityTest] public IEnumerator ThirdChapterRemainsLockedUntilPreviousChaptersAreCleared()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu");
        Component menu=Menu();Call(menu,"OpenLevelSelection");Call(menu,"OpenChapter",2);
        Button[] nodes=(Button[])Field(menu,"levelButtons");
        Assert.That(nodes.Count(b=>b.gameObject.activeSelf),Is.EqualTo(4));
        Assert.That(nodes.Take(4).All(b=>!b.interactable),Is.True);
        for(int i=0;i<4;i++){nodes[i].onClick.Invoke();Assert.That(Get(menu,"IsLevelDetailsOpen"),Is.False);}
        Call(menu,"LoadLevel",0);Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("MainMenu"));
        foreach(string scene in Scenes.Concat(Enumerable.Range(1,4).Select(i=>"Chapter02Level"+i.ToString("00"))))PlayerPrefs.SetInt(Key(scene),100);
        Call(menu,"RefreshLevelProgress");Assert.That(nodes[0].interactable,Is.True);
        Call(menu,"OpenLevelDetails",0);Assert.That(Get(menu,"IsLevelDetailsOpen"),Is.True);
        Assert.That(((Component)Field(menu,"detailsPreviewNotice")).gameObject.activeSelf,Is.False);
        Assert.That(((Button)Field(menu,"detailsStart")).interactable,Is.True);
        Assert.That(PlayerPrefs.HasKey(Key("Chapter03Level01")),Is.False);
    }
'''+s[end:]
p.write_text(s,encoding='utf-8')
