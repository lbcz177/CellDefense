# E01：策划配置工具源码课程

状态：已展开。日期：2026-10-15。依据当前磁盘源码；仅讲解，不修改游戏逻辑。

## 课程范围

本课只跟随“把某个塔的价格从 25 改成 30，再撤销或保存”这条主链，并解释敌人、波次和关卡如何扩展。课程大纲为本课一节，不生成额外面经或练习文件。窗口实际运行验证仍由用户完成；源码静态检查不能证明 UI 与 Undo 已经试玩通过。

## 1. 先知道要改的是什么

工具没有另存一份策划数值表。它编辑项目里现有的对象：

| 对象 | 存放位置 | 本工具的编辑目标 |
| --- | --- | --- |
| TowerDefinition / EnemyDefinition / WaveDefinition | ScriptableObject 资源 | 原 .asset 对象 |
| WaveSequenceDefinition | ScriptableObject 资源 | waves 数组 |
| OpenFieldPrototypeController | 已打开关卡场景内的组件 | 原场景组件的序列化字段 |
| PlanningConfigWindow | EditorWindow | 编辑器窗口状态，不是游戏中的细胞或敌人 |

这些对象原本就被战斗代码读取。因此保存后，游戏继续读取同一份资源；特殊效果仍由对应策略组件实现，不是窗口执行伤害或召唤。

## 2. 主链和阅读顺序

```text
点击“胞卫战 → 策划配置”
  → Open() / GetWindow
  → OnEnable() / RefreshCatalog() / AddAssets<T>()
  → OnGUI() / DrawCatalog() / Choose()
  → DrawDetail() / Edit(selected)
  → DrawTower() / Field()
  → SerializedProperty 改值
  → ApplyModifiedProperties()，应用到对象并记录 Undo
  → DrawValidation() / PlanningConfigValidation.Collect()
  → 点击保存 / DrawSave()
  → AssetDatabase.SaveAssetIfDirty(selected)
```

Undo 是另一条事件链：撤销操作 → Unity 恢复值 → undoRedoPerformed → OnUndoRedo → 清理旧编辑包装 / 列表 → Repaint → OnGUI 重新读取恢复后的数据。

建议顺序：

| 顺序 | 源码位置 | 要理解的内容 |
| --- | --- | --- |
| 1 | Assets/_Project/Scripts/Data/TowerDefinition.cs | 真正存放价格的 buildCost 字段和只读 BuildCost 属性 |
| 2 | Assets/_Project/Editor/PlanningConfigWindow.cs：Open、OnEnable、OnDisable | 菜单、窗口生命周期和事件解绑 |
| 3 | 同文件：RefreshCatalog、AddAssets、Choose | 查找资源及保存当前选项 |
| 4 | 同文件：Edit、DrawDetail、DrawTower、Field | 读取、绘制和应用修改，主链核心 |
| 5 | Assets/_Project/Editor/PlanningConfigValidation.cs：Collect、CheckTower | 校验规则及错误列表 |
| 6 | 窗口文件：DrawSave | 脏标记和磁盘保存 |
| 选读 | 窗口文件：CreateGroups、SyncWaveSummary | 拖动列表、回调和数组编辑 |
| 选读 | 窗口文件：DrawLevel、DrawSequence、FindController | SceneAsset、加载的 Scene 与场景组件的区别 |

## 3. 菜单与窗口

```csharp
[MenuItem("胞卫战/策划配置", false, 100)]
public static void Open()
{
    PlanningConfigWindow window = GetWindow<PlanningConfigWindow>();
    window.titleContent = new GUIContent("策划配置");
    window.minSize = new Vector2(840f, 540f);
    window.Show();
}
```

- MenuItem 是特性，告诉 Unity 把静态方法注册为菜单。斜杠表示子菜单。
- EditorWindow 是编辑器窗口基类。GetWindow 获取 / 打开这个类型的窗口，不需要在场景中创建 GameObject。
- OnEnable 准备目录并注册 Undo、场景打开关闭、Play 模式变化事件。
- OnDisable 用 -= 解除事件，并 Dispose 当前 SerializedObject。
- 这些代码在 Editor 文件夹内，和游戏运行脚本分开编译。

## 4. 资源目录与窗口状态

AssetDatabase.FindAssets 找到的是 GUID；GUIDToAssetPath 转成路径，LoadAssetAtPath<T> 再加载对象。不是直接返回已加载对象。

主要变量：

| 字段 | 类型 | 谁读写 / 何时准备或重置 |
| --- | --- | --- |
| tab | int | Toolbar 切页时修改；OnEnable 限制为 0–3 |
| selected | UnityEngine.Object | Choose 记录当前配置；刷新时为空 / 类型不匹配才选默认对象 |
| catalog | List<UnityEngine.Object>[] | 四份目录；OnEnable 和项目资源变化时刷新 |
| data | SerializedObject | Edit 包装当前配置；选项 / 撤销 / 场景变化时 Dispose 并重建 |
| groups | ReorderableList | 波次页首次绘制时绑定 groups 数组；更换 data 时一起重置 |
| errors | List<string> | 每次校验前清空，再收集错误；非空时禁用保存 |
| listScroll / detailScroll | Vector2 | 保存两个滚动区域位置，避免每次绘制都跳回顶部 |
| search / advanced | string / bool | 搜索条件与高级折叠状态，不是战斗数值 |

源码的 Object 是 UnityEngine.Object 的别名，表示可以引用资源 / 组件的 Unity 对象，避免与 System.Object 混淆。

## 5. IMGUI 怎样绘制

OnGUI 根据当前状态重新声明界面。它会因 Layout、Repaint、鼠标或键盘事件被多次调用，不能当成只执行一次的初始化函数。

EditorGUILayout 自动布局；EditorGUI 常用于给定 Rect 的元素。水平 / 垂直 / 滚动的 using Scope 保证布局正确收尾。

因此不能在 OnGUI 无条件创建资源、保存文件或反复注册事件。当前代码把扫描放在初始化 / 资源变化时，把保存放在按钮条件内。绘制函数不负责扣 ATP、执行攻击或启动关卡。

## 6. 序列化编辑是核心

TowerDefinition 用 [SerializeField] private int buildCost 保存价格。公开 BuildCost 是读取接口。窗口使用的字符串是 buildCost，不是 BuildCost。

```csharp
SerializedObject current = Edit(selected);
EditorGUI.BeginChangeCheck();
DrawTower(current);

if (EditorGUI.EndChangeCheck() && current.hasModifiedProperties)
{
    Undo.SetCurrentGroupName("修改胞卫战策划配置");
    current.ApplyModifiedProperties();
}
```

Edit 先调用 Update，让编辑包装读取目标对象的当前值。Field 找到 SerializedProperty，再用 PropertyField 绘制。用户修改后，ApplyModifiedProperties 把修改应用回对象，并接入 Unity 的 Undo / 脏标记流程。

三件事应分清：

1. 修改 SerializedProperty：编辑包装里有待应用的值。
2. ApplyModifiedProperties：值进入当前编辑会话的真实对象，可以撤销。
3. SaveAssetIfDirty / SaveScene：把对象或场景写到磁盘。

Update 不是保存操作；在待应用修改提交前反复 Update 可能丢弃这些待应用值。当前代码按“Update → 绘制 / 修改 → Apply”的顺序处理。

不建议为了做编辑器，把所有配置字段改成 public。若通过普通 C# 赋值改对象，则需要自己处理 Undo.RecordObject（修改前）和正确的脏标记 / 场景保存；不能只改内存就以为已经持久化。

## 7. 校验和保存

Collect 按目标类型调用 CheckTower / CheckEnemy / CheckWave / CheckLevel，只读检查并收集错误。

例子：价格至少 1；生命和间隔为正有限数；波次敌人引用不能为空；每个开放关卡恰好有一种改路塔；需要三轮。关卡配置中的一种改路塔是“类型唯一”，不等于场上只能建一座壁垒。

窗口禁用保存，并在点击保存时再校验一次。保存使用：

- 普通配置：AssetDatabase.SaveAssetIfDirty(target)，只处理当前资源。
- 关卡：EditorSceneManager.SaveScene(scene)，保存整张对应场景。
- 关卡所引用的顺序资源：SaveAssetIfDirty(sequence)。
- 每轮的 WaveDefinition 内容在波次页单独保存，不跟着关卡页一起自动保存。

脏标记表示存在尚未保存的修改，不代表值一定正确。保存场景会包括那张场景已有的其他修改。Ctrl+Z 是全局编辑历史，不是本窗口私有历史；保存后再撤销，需要重新保存撤销后的状态。

## 8. 波次和关卡的不同处理

### 波次

CreateGroups 把 ReorderableList 绑定到序列化的 groups 数组。drawElementCallback 画每一组；添加 / 删除 / 重排回调修改同一数组。SyncWaveSummary 同步总数与首组敌人的备用摘要。

插入数组元素可能复制原内容，所以添加回调会明确写入敌人、数量、批次和等待时间。删除全部混合分组后使用旧的单类型配置，不等于本轮没有敌人。

### 关卡

SceneAsset 只是场景文件资源，不是里面已经实例化的组件。DrawLevel 取得路径并查找已加载 Scene；没加载时，只显示用户可点击的打开按钮，先走 Unity 原生的未保存场景处理。

场景加载后 FindController 在该 Scene 的根对象及其子对象中查找唯一 OpenFieldPrototypeController，再对它使用 SerializedObject。不会随便找全工程第一个控制器，也不会只改一个同名临时变量。

## 9. 自己写时的顺序和注意点

先完成一个窗口、一个 TowerDefinition 选择框、一个 buildCost 输入框。之后依次补 Apply / Undo、保存、校验、资源目录、多页面、混合分组，最后再处理场景。不要一开始照抄整个窗口。

最小编辑主干（放在你自己的 EditorWindow 的 OnGUI 内，target 为已选 TowerDefinition）：

```csharp
using (var editor = new SerializedObject(target))
{
    editor.Update();
    EditorGUI.BeginChangeCheck();
    EditorGUILayout.PropertyField(
        editor.FindProperty("buildCost"), new GUIContent("价格"));

    if (EditorGUI.EndChangeCheck() && editor.hasModifiedProperties)
        editor.ApplyModifiedProperties();

    using (new EditorGUI.DisabledScope(target.BuildCost < 1))
        if (GUILayout.Button("保存"))
            AssetDatabase.SaveAssetIfDirty(target);
}
```

这只演示已选择目标之后的核心编辑流程，不是本项目完整实现，也没有包含空目标、Play 只读、错误提示和全套业务校验。

注意：

- 字段名必须与源码私有字段一致；重命名字段后检查 FindProperty 字符串。
- UI 单位要明确：百分数显示 25，底层存 0.25；速度基数与关卡倍率分开。
- 共享资源会影响多个关卡，先看资源路径，不要无意改到旧版配置。
- 不在绘制时无条件保存，不默认保存所有其他脏资源。
- 使用事件要解绑；绑定的 SerializedObject / SerializedProperty 换目标后重建。
- 使用项目实际 Unity 版本的编辑器 API。ReorderableList 位于 UnityEditorInternal，升级引擎时尤其应复查。
- .meta 保留现有有效 GUID。新文件最好让 Unity 生成 .meta；本次故障是我手写了 33 位 GUID，导致整个校验脚本被忽略。合法 GUID 为 32 位十六进制字符且不能重复。
- C# 类型诊断不检查 .meta 导入、布局、焦点、场景切换和真实 Undo 行为。需要用户在 Unity 做这些验证，不能用静态检查代替。

若某个函数或原理看不懂，可继续按本课的阅读顺序追问；先沿完整修改链理解，再挑函数展开。

## 10. 完整流程回顾

选到价格为 25 的塔资源 → Edit 包装并 Update → Field 画 buildCost → 用户输入 30 → Apply 把当前对象变为 30 → 校验通过 → 保存只写该资源。

另一分支：Apply 后点击撤销 → Unity 恢复为 25 → 事件清理旧包装并 Repaint → 重新读取 25。撤销前若已保存过 30，磁盘不会因撤销自动改回；需再次保存 25。

复述任务：不用运行项目，解释“Update、ApplyModifiedProperties、Undo、SaveAssetIfDirty 分别在哪一步，改变的是哪一层状态”。能讲清这条链，再阅读数组与场景分支。

