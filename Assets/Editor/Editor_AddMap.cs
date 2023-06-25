using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
//��������������ͼ
public class Editor_AddMap : EditorWindow
{
    //���ԡ�MenuItem��Ҫд�ھ�̬����ǰ����Ǵ˾�̬������������ڱ༭���ڳ��ָô���ҳ��
    [MenuItem("��չ�༭��/�������������ͼ")]
    public static void ShowWindows()
    {
        Rect wr = new Rect(0, 200, 310, 500);//����һ����������
        Editor_AddMap Window = (Editor_AddMap)EditorWindow.GetWindowWithRect(typeof(Editor_AddMap), wr, true, "������������ͼ");//����һ���༭����
        Window.Show();//���ƴ���
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
    private string mLog = "��ѡ����Ϊ��";
    private string tLog = "��ѡ��ͼΪ��";

    private void OnGUI()
    {
        GUILayout.Space(10);//10��λ�Ŀո�
        EditorGUILayout.HelpBox("��ͼ�����˳���Ӧ�������Ƿ��Ӧ��ȷ", MessageType.None);//����������
        GUILayout.Space(10);
        GUILayout.BeginVertical();//������ֱ�Ų��飬����ǵù�
        GUILayout.BeginHorizontal();//ˮƽ�Ų���
        GUILayout.Label("��ѡ�����ͼ�Ĳ��ʣ�", GUILayout.Width(150f));//����д�ı��򣨱�ǩ��
        EditorGUILayout.HelpBox("ѡ�񷽷�����ѡȷ����ֱ����ק", MessageType.None);
        GUILayout.EndHorizontal();

        Rect mLogRect = EditorGUILayout.GetControlRect(GUILayout.Height(100));//������������
        if (Event.current.type == EventType.DragExited
        && mLogRect.Contains(Event.current.mousePosition))//�������Ƿ�������ק�¼�
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;//�ı�����״
            // �ж��Ƿ���ק���ļ�
            if (DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0)
            {
                foreach(object p in DragAndDrop.objectReferences)//DragAndDrop.objectReferences��ʾ��ק����
                {
                    material.Add((Material)p);
                }
                foreach(string p in DragAndDrop.paths)//DragAndDrop.path��ʾ��ק�����·��
                {
                    mLog = mLog + "\n" + p;
                }
            }
        }
        mLog = EditorGUI.TextField(mLogRect, mLog);//���������ı���

        if (GUILayout.Button("��ȡ������Ϣ", GUILayout.Width(300f)))
        {
            ChooseMaterial();
        }
        if (GUILayout.Button("���������Ϣ", GUILayout.Width(300f)))
        {
            if (material != null)
            { material.Clear(); mLog = "��ѡ����Ϊ��"; }
        }

        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        GUILayout.Label("��ѡ����ͼ��", GUILayout.Width(100f));
        GUILayout.EndHorizontal();

        Rect tLogRect = EditorGUILayout.GetControlRect(GUILayout.Height(100));
        if (Event.current.type == EventType.DragExited
        && tLogRect.Contains(Event.current.mousePosition))
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;//�ı�����״
            // �ж��Ƿ���ק���ļ�
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
        if (GUILayout.Button("��ȡ��ͼ��Ϣ", GUILayout.Width(300f)))
        {
            ChooseTexture();
        }
        if (GUILayout.Button("�����ͼ��Ϣ", GUILayout.Width(300f)))
        {
            if (texture != null)
            { texture.Clear(); tLog = "��ѡ��ͼΪ��"; }
        }

        GUILayout.BeginHorizontal();
        //����һ��ö�٣���Ҫ������һ��ö�ٲ��֣����ָ�ֵΪ��ö�٣���ת���ò���Ϊö�����͡���ʽ����ö��ת���������֣�ö��+�������ԣ�
        load = (LoadMathed)EditorGUILayout.EnumPopup("ѡ���ȡ��ͼ�ķ�ʽ:", load, GUILayout.Width(300f));
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        switch(load)
        {
            case LoadMathed.Select:
                {
                    EditorGUILayout.HelpBox("����ѡ��ͼ", MessageType.None);
                    break;
                }
            case LoadMathed.Materials:
                {
                    EditorGUILayout.HelpBox("ֱ��ʹ�ò��ʵ�����ͼ", MessageType.None);
                    break;
                }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(30);
        GUILayout.BeginHorizontal();
        Maptype = (MapType)EditorGUILayout.EnumPopup("ѡ����ͼ����:", Maptype, GUILayout.Width(300f));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("�ı���ͼ", GUILayout.Width(300f)))
        {
            ChangeMap();
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }
    public void ChooseMaterial()
    {
        //Selection���༭���࣬����ѡ����SelectionMode������ѡ��Editable�������޸ģ���TopLevel�������������壩��DeepAssets����Դ�ڵ�ѡ���в㼶����
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
        string KeyWord = null;//Shaderʹ�õ�����һ�״���ײ㣬C#����ʹ�ùؼ���������Shader���ؼ��ֿ���Shader��Inspector�ڲ鿴
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
