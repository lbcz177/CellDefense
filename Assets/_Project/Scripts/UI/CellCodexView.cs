using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class CellCodexView : MonoBehaviour
{
    private CellCodexArtwork artwork;
    private Canvas canvas;
    private RectTransform panel;
    private GameObject list;
    private Image detail;
    private Button closeButton, indexButton, previousButton, nextButton;
    private Button defenderTab, invaderTab;
    private Button[] entryButtons;
    private RectTransform cardContent;
    private ScrollRect cardScroll;
    private readonly List<int> filtered = new List<int>();
    private readonly List<GameObject> cards = new List<GameObject>();
    private Action onClose;
    private int category, selected = -1;
    public bool IsOpen => gameObject.activeSelf;
    public bool IsDetailOpen => detail != null && detail.gameObject.activeSelf;
    public int EntryCount => artwork.entries.Length;
    public int SelectedEntry => selected;

    public static CellCodexView Create(Action close)
    {
        CellCodexArtwork art=Resources.Load<CellCodexArtwork>("CellCodexArtwork");
        if(art==null || art.entries==null || art.entries.Length==0)return null;
        GameObject root=new GameObject("Cell Codex Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        root.layer=LayerMask.NameToLayer("UI");
        CellCodexView view=root.AddComponent<CellCodexView>();
        view.artwork=art;view.onClose=close;
        view.canvas=root.GetComponent<Canvas>();
        view.canvas.renderMode=RenderMode.ScreenSpaceOverlay;view.canvas.sortingOrder=70;
        CanvasScaler scaler=root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
        MobileDisplay.ConfigureCanvas(view.canvas);
        Image shade=view.Image("Codex shade",root.transform,null);
        shade.color=new Color(.12f,.035f,.1f,.96f);shade.raycastTarget=true;
        Stretch(shade.rectTransform);
        Image background=view.Image("Codex panel",root.transform,null);
        background.sprite=art.frame;background.type=UnityEngine.UI.Image.Type.Sliced;
        background.preserveAspect=false;view.panel=background.rectTransform;
        view.panel.anchorMin=view.panel.anchorMax=view.panel.pivot=Vector2.one*.5f;
        view.panel.sizeDelta=new Vector2(1184,664);
        view.list=new GameObject("Codex index",typeof(RectTransform));
        view.list.transform.SetParent(view.panel,false);Stretch(view.list.GetComponent<RectTransform>());
        Image title=view.Image("Codex title",view.list.transform,art.title);Place(title.rectTransform,42,24,280,54);
        view.defenderTab=view.Button("Defense category",view.list.transform,art.defenders,()=>view.ShowCategory(0));
        Place(view.defenderTab.GetComponent<RectTransform>(),360,82,200,50);
        view.invaderTab=view.Button("Enemy category",view.list.transform,art.invaders,()=>view.ShowCategory(1));
        Place(view.invaderTab.GetComponent<RectTransform>(),580,82,200,50);
        GameObject scroll=new GameObject("Codex scrolling entries",typeof(RectTransform),typeof(ScrollRect));
        scroll.layer=root.layer;scroll.transform.SetParent(view.list.transform,false);
        RectTransform scrollRect=scroll.GetComponent<RectTransform>();Place(scrollRect,24,150,1136,452);
        scroll.AddComponent<RectMask2D>();
        GameObject content=new GameObject("Codex cards",typeof(RectTransform));
        content.layer=root.layer;content.transform.SetParent(scroll.transform,false);
        view.cardContent=content.GetComponent<RectTransform>();
        view.cardContent.anchorMin=new Vector2(0,1);view.cardContent.anchorMax=new Vector2(1,1);view.cardContent.pivot=new Vector2(.5f,1);
        view.cardScroll=scroll.GetComponent<ScrollRect>();view.cardScroll.viewport=scrollRect;view.cardScroll.content=view.cardContent;
        view.cardScroll.horizontal=false;view.cardScroll.movementType=ScrollRect.MovementType.Clamped;view.cardScroll.scrollSensitivity=36f;
        view.detail=view.Image("Full codex page",view.panel,null);Stretch(view.detail.rectTransform);
        view.detail.preserveAspect=true;view.detail.gameObject.SetActive(false);
        view.closeButton=view.Button("Close codex",view.panel,art.home,()=>view.onClose?.Invoke());
        view.indexButton=view.Button("Codex index",view.panel,art.index,()=>view.ShowCategory(view.category));
        view.previousButton=view.Button("Previous specimen",view.panel,art.previous,()=>view.Step(-1));
        view.nextButton=view.Button("Next specimen",view.panel,art.next,()=>view.Step(1));
        root.SetActive(false);
        return view;
    }

    public void Show()
    {
        gameObject.SetActive(true);ShowCategory(0);Fit();
    }

    public void Hide() => gameObject.SetActive(false);

    public void HandleEscape()
    {
        if(IsDetailOpen)ShowCategory(category);else onClose?.Invoke();
    }

    public void ShowCategory(int value)
    {
        int oldSelection=selected;bool sameCategory=category==value;
        category=value;selected=-1;filtered.Clear();
        for(int i=0;i<artwork.entries.Length;i++)if(artwork.entries[i].category==category)filtered.Add(i);
        foreach(GameObject card in cards){card.SetActive(false);Destroy(card);}cards.Clear();
        list.SetActive(true);detail.gameObject.SetActive(false);
        indexButton.gameObject.SetActive(false);previousButton.gameObject.SetActive(false);nextButton.gameObject.SetActive(false);
        Place(closeButton.GetComponent<RectTransform>(),978,28,160,46);
        int columns=filtered.Count<=6?3:4;
        float width=columns==3?276f:244f,gap=16f;
        float start=(1136f-(columns*width+(columns-1)*gap))*.5f;
        int rows=Mathf.CeilToInt(filtered.Count/(float)columns);
        cardContent.sizeDelta=new Vector2(0,Mathf.Max(452f,rows*224f));
        cardContent.anchoredPosition=Vector2.zero;cardScroll.vertical=rows>2;cardScroll.verticalNormalizedPosition=1f;
        entryButtons=new Button[filtered.Count];
        for(int i=0;i<filtered.Count;i++)
        {
            int entryIndex=filtered[i];CellCodexEntry entry=artwork.entries[entryIndex];
            Button card=Button("Codex entry "+entry.id,cardContent,null,()=>OpenEntry(entryIndex));
            Place(card.GetComponent<RectTransform>(),start+(i%columns)*(width+gap),8+(i/columns)*224,width,206);
            Image portrait=Image("Portrait",card.transform,entry.portrait);
            Place(portrait.rectTransform,(width-146)*.5f,10,146,146);
            Image caption=Image("Name",card.transform,entry.caption);
            Place(caption.rectTransform,12,163,width-24,34);
            entryButtons[i]=card;cards.Add(card.gameObject);
        }
        defenderTab.image.color=category==0?new Color(.72f,1f,1f):Color.white;
        invaderTab.image.color=category==1?new Color(1f,.8f,.94f):Color.white;
        int oldIndex=filtered.IndexOf(oldSelection);
        Focus(sameCategory && oldIndex>=0?entryButtons[oldIndex]:(category==0?defenderTab:invaderTab));
    }

    public void OpenEntry(int index)
    {
        if(index<0 || index>=artwork.entries.Length || artwork.entries[index].category!=category)return;
        selected=index;list.SetActive(false);detail.gameObject.SetActive(true);detail.sprite=artwork.entries[index].page;
        indexButton.gameObject.SetActive(true);previousButton.gameObject.SetActive(true);nextButton.gameObject.SetActive(true);
        Place(indexButton.GetComponent<RectTransform>(),46,608,178,42);
        Place(closeButton.GetComponent<RectTransform>(),242,608,178,42);
        Place(previousButton.GetComponent<RectTransform>(),760,608,150,42);
        Place(nextButton.GetComponent<RectTransform>(),928,608,150,42);
        Focus(indexButton);
    }

    private void Step(int direction)
    {
        if(!IsDetailOpen || filtered.Count==0)return;
        int index=filtered.IndexOf(selected);
        OpenEntry(filtered[(index+direction+filtered.Count)%filtered.Count]);
    }

    private void LateUpdate() => Fit();
    private void Fit()
    {
        if(panel==null || canvas==null)return;
        RectTransform bounds=(RectTransform)canvas.transform;
        Rect safe=MobileDisplay.LocalSafeArea(bounds);
        panel.anchoredPosition=safe.center-bounds.rect.center;
        panel.localScale=Vector3.one*Mathf.Max(.01f,Mathf.Min((safe.width-16f)/1184f,(safe.height-16f)/664f));
    }

    private Image Image(string name,Transform parent,Sprite sprite)
    {
        GameObject go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        go.layer=gameObject.layer;go.transform.SetParent(parent,false);
        Image image=go.GetComponent<Image>();image.sprite=sprite;image.preserveAspect=sprite!=null;image.raycastTarget=false;
        return image;
    }

    private Button Button(string name,Transform parent,Sprite label,Action click)
    {
        Image image=Image(name,parent,null);image.raycastTarget=true;
        Button button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
        image.sprite=artwork.frame;image.type=UnityEngine.UI.Image.Type.Sliced;image.preserveAspect=false;
        button.transition=Selectable.Transition.ColorTint;
        ColorBlock palette=button.colors;
        palette.normalColor=Color.white;palette.highlightedColor=new Color(.81f,1f,1f);
        palette.selectedColor=new Color(.76f,.98f,1f);palette.pressedColor=new Color(.64f,.9f,.94f);
        palette.fadeDuration=.08f;button.colors=palette;
        button.onClick.AddListener(()=>click());
        if(label!=null){Image caption=Image("Caption",image.transform,label);Stretch(caption.rectTransform);caption.rectTransform.offsetMin=new Vector2(12,8);caption.rectTransform.offsetMax=new Vector2(-12,-8);}
        return button;
    }

    private static void Focus(Button button)
    {
        if(UnityEngine.EventSystems.EventSystem.current!=null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(button.gameObject);
    }
    private static void Place(RectTransform rect,float x,float y,float width,float height)
    {
        rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
    }
}
