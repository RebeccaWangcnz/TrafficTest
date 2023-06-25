using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
//给材质批量贴贴图
public class Editor_AddMap : EditorWindow
{
    //特性“MenuItem”要写在静态函数前，标记此静态函数，便可以在编辑器内出现该窗口页面
    [MenuItem("拓展编辑器/材质批量添加贴图")]
    public static void ShowWindows()
    {
        Rect wr = new Rect(0, 200, 310, 500);//声明一个矩形区域
        Editor_AddMap Window = (Editor_AddMap)EditorWindow.GetWindowWithRect(typeof(Editor_AddMap), wr, true, "给材质批量贴图");//声明一个编辑窗口
        Window.Show();//绘制窗口
    }
    public List<Material> material = new List<Material>();
    public List<Texture> texture = new List<Texture>();
    public enum MapType
    {
        Main,
        Normal,
        Height,
        Light,
        Emission
    }
    public MapType Maptype;
    public enum LoadMathed
    {
        Select,
        Materials
    }
    public LoadMathed load;
    private string mLog = "所选材质为：";
    private string tLog = "所选贴图为：";

    private void OnGUI()
    {
        GUILayout.Space(10);//10单位的空格
        EditorGUILayout.HelpBox("贴图与材质顺序对应，请检查是否对应正确", MessageType.None);//文字提醒栏
        GUILayout.Space(10);
        GUILayout.BeginVertical();//开启垂直排布组，后面记得关
        GUILayout.BeginHorizontal();//水平排布组
        GUILayout.Label("请选择待贴图的材质：", GUILayout.Width(150f));//不可写文本框（标签框）
        EditorGUILayout.HelpBox("选择方法：点选确定或直接拖拽", MessageType.None);
        GUILayout.EndHorizontal();

        Rect mLogRect = EditorGUILayout.GetControlRect(GUILayout.Height(100));//声明矩形区域
        if (Event.current.type == EventType.DragExited
        && mLogRect.Contains(Event.current.mousePosition))//区域内是否发生了拖拽事件
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;//改变光标形状
            // 判断是否拖拽了文件
            if (DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0)
            {
                foreach(object p in DragAndDrop.objectReferences)//DragAndDrop.objectReferences表示拖拽对象
                {
                    material.Add((Material)p);
                }
                foreach(string p in DragAndDrop.paths)//DragAndDrop.path表示拖拽对象的路径
                {
                    mLog = mLog + "\n" + p;
                }
            }
        }
        mLog = EditorGUI.TextField(mLogRect, mLog);//区域生成文本框

        if (GUILayout.Button("读取材质信息", GUILayout.Width(300f)))
        {
            ChooseMaterial();
        }
        if (GUILayout.Button("清除材质信息", GUILayout.Width(300f)))
        {
            if (material != null)
            { material.Clear(); mLog = "所选材质为："; }
        }

        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        GUILayout.Label("请选择贴图：", GUILayout.Width(100f));
        GUILayout.EndHorizontal();

        Rect tLogRect = EditorGUILayout.GetControlRect(GUILayout.Height(100));
        if (Event.current.type == EventType.DragExited
        && tLogRect.Contains(Event.current.mousePosition))
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;//改变光标形状
            // 判断是否拖拽了文件
            if (DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0)
            {
                foreach (object p in DragAndDrop.objectReferences)
                {
                    texture.Add((Texture2D)p);
                }
                foreach (string p in DragAndDrop.paths)
                {
                    tLog = tLog + "\n" + p;
                }
            }
        }
        tLog = EditorGUI.TextField(tLogRect, tLog);
        if (GUILayout.Button("读取贴图信息", GUILayout.Width(300f)))
        {
            ChooseTexture();
        }
        if (GUILayout.Button("清除贴图信息", GUILayout.Width(300f)))
        {
            if (texture != null)
            { texture.Clear(); tLog = "所选贴图为："; }
        }

        GUILayout.BeginHorizontal();
        //声明一个枚举，需要先声明一个枚举布局，布局赋值为该枚举，再转换该布局为枚举类型。格式：（枚举转换符）布局（枚举+布局属性）
        load = (LoadMathed)EditorGUILayout.EnumPopup("选择读取贴图的方式:", load, GUILayout.Width(300f));
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        switch(load)
        {
            case LoadMathed.Select:
                {
                    EditorGUILayout.HelpBox("鼠标点选贴图", MessageType.None);
                    break;
                }
            case LoadMathed.Materials:
                {
                    EditorGUILayout.HelpBox("直接使用材质的主贴图", MessageType.None);
                    break;
                }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(30);
        GUILayout.BeginHorizontal();
        Maptype = (MapType)EditorGUILayout.EnumPopup("选择贴图种类:", Maptype, GUILayout.Width(300f));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("改变贴图", GUILayout.Width(300f)))
        {
            ChangeMap();
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }
    public void ChooseMaterial()
    {
        //Selection，编辑器类，鼠标点选对象；SelectionMode，返回选择；Editable（仅可修改），TopLevel（不包括子物体），DeepAssets（资源内点选所有层级）等
        Object[] selection = Selection.GetFiltered(typeof(Material), SelectionMode.Editable | SelectionMode.TopLevel);
        if (selection.Length == 0) return;
        foreach (Material test in selection)
        {
            material.Add(test);
            mLog = mLog + "\n" + test.name;
        }
    }
    public void ChooseTexture()
    {
        switch (load)
        {
            case LoadMathed.Select:
                {
                    Object[] selection = Selection.GetFiltered<Texture2D>(SelectionMode.DeepAssets);
                    if (selection.Length == 0) return;
                    foreach (Texture2D test in selection)
                    {
                        texture.Add(test);
                        tLog = tLog + "\n" + test.name;
                    }
                    break;
                }
            case LoadMathed.Materials:
                {
                    foreach (Material test in material)
                    {
                        texture.Add(test.mainTexture);
                        tLog = tLog + "\n" + test.mainTexture.name;
                    }
                    break;
                }
        }
    }
    public void ChangeMap()
    {
        string KeyWord = null;//Shader使用的是另一套代码底层，C#可以使用关键字来控制Shader，关键字可在Shader的Inspector内查看
        switch(Maptype)
        {
            case MapType.Main:
                KeyWord = "_MainTextrue";
                break;
            case MapType.Normal:
                KeyWord = "_BumpMap";
                break;
            case MapType.Height:
                KeyWord = "_ParallaxMap";
                break;
            case MapType.Light:
                KeyWord = "_LightMap";
                break;
            case MapType.Emission:
                KeyWord = "_EmissiveColorMap";
                break;
        }
        for (int i = 0; i < material.Count; i++) material[i].SetTexture(KeyWord, texture[i]);
    }
}
