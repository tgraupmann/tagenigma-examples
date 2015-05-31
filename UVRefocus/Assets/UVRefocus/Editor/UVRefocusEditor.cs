/*
 * Author:  Tim Graupmann
 * TAGENIGMA LLC, @copyright 2015  All rights reserved.
 *
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class UVRefocusEditor : EditorWindow
{
    public static string VERSION = "1.0";

    private bool _mCompileDetected = false;

    private static GameObject[] _mMeshObjects = null;

    private static GameObject _sSelectedMesh = null;

    private static Texture2D _sReferenceUVMap = null;

    private static Texture2D _sInstanceUVMap = null;

    /// <summary>
    /// Open an instance of the panel
    /// </summary>
    /// <returns></returns>
    public static UVRefocusEditor GetPanel()
    {
        UVRefocusEditor window = GetWindow<UVRefocusEditor>();
        window.position = new Rect(300, 300, 500, 500);
        window.minSize = new Vector2(200, 20);
        return window;
    }

    /// <summary>
    /// Get Toolbox Window
    /// </summary>
    [MenuItem("Window/Open UV Refocus")]
    private static void MenuOpenPanel()
    {
        GetPanel();
    }

    enum SearchLocations
    {
        None,
        TopHalf,
        BottomHalf,
        Head,
        LeftEye,
        RightEye,
        Nose,
        Mouth,
        LeftArm,
        RightArm,
        LeftHand,
        RightHand,
        LeftThumb,
        LeftIndex,
        LeftMiddle,
        LeftRing,
        LeftPinky,
        RightThumb,
        RightIndex,
        RightMiddle,
        RightRing,
        RightPinky,
        LeftLeg,
        RightLeg,
        LeftFoot,
        RightFoot,
    }

    static List<Vector3> _sLines = new List<Vector3>();

    void OnGUI()
    {
        if (_mCompileDetected)
        {
            GUILayout.Label("Compiling...");
        }
        else
        {
            for (int index = 0; index < _sLines.Count; index += 2)
            {
                Debug.DrawLine(_sLines[index], _sLines[index+1], Color.white, Time.deltaTime, false);
            }

            GUILayout.Label(string.Format("VERSION={0}", VERSION));

            if (null == _mMeshObjects)
            {
                _mMeshObjects = new GameObject[0];
            }

            int count = EditorGUILayout.IntField("Size", _mMeshObjects.Length);
            if (count != _mMeshObjects.Length &&
                count >= 0 &&
                count < 100)
            {
                _mMeshObjects = new GameObject[count];
                EditorPrefs.SetInt("MeshCount", count);
                ReloadMeshes(count);
            }
            for (int index = 0; index < count && count < 100; ++index)
            {
                GUILayout.BeginHorizontal();
                bool flag = GUILayout.Toggle(_sSelectedMesh == _mMeshObjects[index], string.Empty, GUILayout.Width(10));
                _mMeshObjects[index] = (GameObject)EditorGUILayout.ObjectField(string.Format("Element {0}", index), _mMeshObjects[index], typeof(GameObject));
                if (flag)
                {
                    _sSelectedMesh = _mMeshObjects[index];
                }
                GUILayout.EndHorizontal();
                if (null != _mMeshObjects[index])
                {
                    EditorPrefs.SetInt(string.Format("Mesh{0}", index), _mMeshObjects[index].GetInstanceID());
                }
            }

            /*
            string meshAsset = AssetDatabase.GetAssetPath(_mMesh);
            GUILayout.Label(string.Format("Asset={0}", meshAsset));
            EditorPrefs.SetString("MeshAsset", meshAsset);

            if (null == _mMesh.colors ||
                _mMesh.colors32.Length == 0)
            {
                if (_mMesh.vertexCount > 0)
                {
                    _mMesh.colors32 = new Color32[_mMesh.vertexCount];
                }
            }

            GUILayout.Label(string.Format("Vertex Colors 32: {0}", _mMesh.colors32.Length));

            EditorGUILayout.Vector3Field("Bounds Min:", _mBoundsMin);
            EditorGUILayout.Vector3Field("Bounds Max:", _mBoundsMax);
            */

            if (GUILayout.Button("None"))
            {
                Find(SearchLocations.None);
            }

            GUILayout.Label(string.Empty);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("L-Eye"))
            {
                Find(SearchLocations.LeftEye);
            }
            if (GUILayout.Button("Head"))
            {
                Find(SearchLocations.Head);
            }
            if (GUILayout.Button("R-Eye"))
            {
                Find(SearchLocations.RightEye);
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Nose"))
            {
                Find(SearchLocations.Nose);
            }

            if (GUILayout.Button("Mouth"))
            {
                Find(SearchLocations.Mouth);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("L-Hand"))
            {
                Find(SearchLocations.LeftHand);
            }
            if (GUILayout.Button("L-Arm"))
            {
                Find(SearchLocations.LeftArm);
            }
            if (GUILayout.Button("R-Arm"))
            {
                Find(SearchLocations.RightArm);
            }
            if (GUILayout.Button("R-Hand"))
            {
                Find(SearchLocations.RightHand);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("L-Foot"))
            {
                Find(SearchLocations.LeftFoot);
            }
            if (GUILayout.Button("L-Leg"))
            {
                Find(SearchLocations.LeftLeg);
            }
            if (GUILayout.Button("R-Leg"))
            {
                Find(SearchLocations.RightLeg);
            }
            if (GUILayout.Button("R-Foot"))
            {
                Find(SearchLocations.RightFoot);
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(string.Empty);

            _sReferenceUVMap = (Texture2D)EditorGUILayout.ObjectField(string.Empty, _sReferenceUVMap, typeof(Texture2D));

            if (_sInstanceUVMap)
            {
                //GUILayout.Label(_sInstanceUVMap, GUILayout.Width(256), GUILayout.Height(256));
                GUILayout.Label(_sInstanceUVMap);
            }

            foreach (SearchLocations search in Enum.GetValues(typeof (SearchLocations)))
            {
                if (GUILayout.Button(string.Format("{0}", search)))
                {
                    Find(search);
                }
            }
        }
    }

    void ReloadMeshes(int count)
    {
        _mMeshObjects = new GameObject[count];
        for (int index = 0; index < count; ++index)
        {
            string key = string.Format("Mesh{0}", index);
            if (!EditorPrefs.HasKey(key))
            {
                continue;
            }
            int instanceId = EditorPrefs.GetInt(key);
            _mMeshObjects[index] = (GameObject)EditorUtility.InstanceIDToObject(instanceId);
        }
    }

    void Update()
    {
        if (EditorApplication.isCompiling)
        {
            _mCompileDetected = true;
            if (_sInstanceUVMap)
            {
                DestroyImmediate(_sInstanceUVMap);
                _sInstanceUVMap = null;
            }
        }
        else if (_mCompileDetected)
        {
            _mCompileDetected = false;
            if (EditorPrefs.HasKey("MeshCount"))
            {
                int count = EditorPrefs.GetInt("MeshCount");
                ReloadMeshes(count);
            }
            Repaint();
        }
    }

    void ClearColor(Mesh mesh)
    {
        Color32[] colors = mesh.colors32;
        for (int index = 0; index < colors.Length; ++index)
        {
            colors[index] = Color.black;
        }
        mesh.colors32 = colors;
    }

    private Vector3 _mBoundsMin = Vector3.zero;
    private Vector3 _mBoundsMax = Vector3.zero;
    private Vector3 _mBoundsMid = Vector3.zero;

    void CalculateBounds(Vector3[] verts)
    {
        if (verts.Length > 0)
        {
            _mBoundsMin = verts[0];
            _mBoundsMax = verts[0];
        }
        for (int index = 1; index < verts.Length; ++index)
        {
            _mBoundsMin.x = Mathf.Min(_mBoundsMin.x, verts[index].x);
            _mBoundsMin.y = Mathf.Min(_mBoundsMin.y, verts[index].y);
            _mBoundsMin.z = Mathf.Min(_mBoundsMin.z, verts[index].z);

            _mBoundsMax.x = Mathf.Max(_mBoundsMax.x, verts[index].x);
            _mBoundsMax.y = Mathf.Max(_mBoundsMax.y, verts[index].y);
            _mBoundsMax.z = Mathf.Max(_mBoundsMax.z, verts[index].z);
        }

        _mBoundsMid = (_mBoundsMin + _mBoundsMax)*0.5f;
    }

    private void Find(SearchLocations search)
    {
        if (_sInstanceUVMap)
        {
            DestroyImmediate(_sInstanceUVMap);
            _sInstanceUVMap = null;
        }

        _sLines.Clear();

        if (_sReferenceUVMap)
        {
            _sInstanceUVMap = Instantiate(_sReferenceUVMap);
        }

        foreach (GameObject go in _mMeshObjects)
        {
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf &&
                mf.sharedMesh)
            {
                Process(search, go.transform, mf.sharedMesh);
            }
            SkinnedMeshRenderer mr = go.GetComponent<SkinnedMeshRenderer>();
            if (mr &&
                mr.sharedMesh)
            {
                Process(search, go.transform, mr.sharedMesh);
            }
        }
    }

    private void Process(SearchLocations search, Transform t, Mesh mesh)
    {
        if (null == mesh)
        {
            return;
        }

        if (search == SearchLocations.None)
        {
            ClearColor(mesh);
            return;
        }

        Vector3[] verts = mesh.vertices;

        CalculateBounds(verts);

        Color32[] colors = mesh.colors32;

        if (colors.Length != verts.Length)
        {
            colors = new Color32[verts.Length];
        }

        float thresholdLeft;
        float thresholdRight;
        float thresholdBack;
        float thresholdFront;
        float thresholdUpper;
        float thresholdLower;

        switch (search)
        {
            case SearchLocations.TopHalf:
                for (int index = 0; index < verts.Length; ++index)
                {
                    if (verts[index].y >= _mBoundsMid.y)
                    {
                        colors[index] = Color.white;
                    }
                    else
                    {
                        colors[index] = Color.black;
                    }
                }
                break;
            case SearchLocations.BottomHalf:
                for (int index = 0; index < verts.Length; ++index)
                {
                    if (verts[index].y <= _mBoundsMid.y)
                    {
                        colors[index] = Color.white;
                    }
                    else
                    {
                        colors[index] = Color.black;
                    }
                }
                break;
            case SearchLocations.Head:
                thresholdUpper = Mathf.Lerp(_mBoundsMin.y, _mBoundsMax.y, 0.85f);
                for (int index = 0; index < verts.Length; ++index)
                {
                    if (verts[index].y > thresholdUpper)
                    {
                        colors[index] = Color.white;
                    }
                    else
                    {
                        colors[index] = Color.black;
                    }
                }
                break;
            case SearchLocations.LeftEye:
                thresholdUpper = Mathf.Lerp(_mBoundsMin.y, _mBoundsMax.y, 0.95f);
                thresholdLower = Mathf.Lerp(_mBoundsMin.y, _mBoundsMax.y, 0.92f);
                thresholdLeft = Mathf.Lerp(_mBoundsMin.x, _mBoundsMax.x, 0.465f);
                thresholdRight = Mathf.Lerp(_mBoundsMin.x, _mBoundsMax.x, 0.4925f);
                thresholdFront = Mathf.Lerp(_mBoundsMin.z, _mBoundsMax.z, 0.7f);
                for (int index = 0; index < verts.Length; ++index)
                {
                    if (verts[index].y <= thresholdUpper &&
                        verts[index].y >= thresholdLower &&
                        verts[index].x >= thresholdLeft &&
                        verts[index].x <= thresholdRight &&
                        verts[index].z >= thresholdFront)
                    {
                        colors[index] = Color.white;
                    }
                    else
                    {
                        colors[index] = Color.black;
                    }
                }
                break;
            case SearchLocations.RightEye:
                thresholdUpper = Mathf.Lerp(_mBoundsMin.y, _mBoundsMax.y, 0.95f);
                thresholdLower = Mathf.Lerp(_mBoundsMin.y, _mBoundsMax.y, 0.92f);
                thresholdLeft = Mathf.Lerp(_mBoundsMin.x, _mBoundsMax.x, 0.535f);
                thresholdRight = Mathf.Lerp(_mBoundsMin.x, _mBoundsMax.x, 0.5075f);
                thresholdFront = Mathf.Lerp(_mBoundsMin.z, _mBoundsMax.z, 0.7f);
                for (int index = 0; index < verts.Length; ++index)
                {
                    if (verts[index].y <= thresholdUpper &&
                        verts[index].y >= thresholdLower &&
                        verts[index].x <= thresholdLeft &&
                        verts[index].x >= thresholdRight &&
                        verts[index].z >= thresholdFront)
                    {
                        colors[index] = Color.white;
                    }
                    else
                    {
                        colors[index] = Color.black;
                    }
                }
                break;
            case SearchLocations.Nose:
                thresholdUpper = Mathf.Lerp(_mBoundsMin.y, _mBoundsMax.y, 0.93f);
                thresholdLower = Mathf.Lerp(_mBoundsMin.y, _mBoundsMax.y, 0.9f);
                thresholdLeft = Mathf.Lerp(_mBoundsMin.x, _mBoundsMax.x, 0.4925f);
                thresholdRight = Mathf.Lerp(_mBoundsMin.x, _mBoundsMax.x, 0.5075f);
                thresholdFront = Mathf.Lerp(_mBoundsMin.z, _mBoundsMax.z, 0.7f);
                for (int index = 0; index < verts.Length; ++index)
                {
                    if (verts[index].y <= thresholdUpper &&
                        verts[index].y >= thresholdLower &&
                        verts[index].x >= thresholdLeft &&
                        verts[index].x <= thresholdRight &&
                        verts[index].z >= thresholdFront)
                    {
                        colors[index] = Color.white;
                    }
                    else
                    {
                        colors[index] = Color.black;
                    }
                }
                break;
            case SearchLocations.Mouth:
                thresholdUpper = Mathf.Lerp(_mBoundsMin.y, _mBoundsMax.y, 0.905f);
                thresholdLower = Mathf.Lerp(_mBoundsMin.y, _mBoundsMax.y, 0.89f);
                thresholdLeft = Mathf.Lerp(_mBoundsMin.x, _mBoundsMax.x, 0.48f);
                thresholdRight = Mathf.Lerp(_mBoundsMin.x, _mBoundsMax.x, 0.52f);
                thresholdFront = Mathf.Lerp(_mBoundsMin.z, _mBoundsMax.z, 0.7f);
                for (int index = 0; index < verts.Length; ++index)
                {
                    if (verts[index].y <= thresholdUpper &&
                        verts[index].y >= thresholdLower &&
                        verts[index].x >= thresholdLeft &&
                        verts[index].x <= thresholdRight &&
                        verts[index].z >= thresholdFront)
                    {
                        colors[index] = Color.white;
                    }
                    else
                    {
                        colors[index] = Color.black;
                    }
                }
                break;
            case SearchLocations.LeftLeg:
                for (int index = 0; index < verts.Length; ++index)
                {
                    if (verts[index].x <= _mBoundsMid.x &&
                        verts[index].y <= _mBoundsMid.y)
                    {
                        colors[index] = Color.white;
                    }
                    else
                    {
                        colors[index] = Color.black;
                    }
                }
                break;
            case SearchLocations.RightLeg:
                for (int index = 0; index < verts.Length; ++index)
                {
                    if (verts[index].x >= _mBoundsMid.x &&
                        verts[index].y <= _mBoundsMid.y)
                    {
                        colors[index] = Color.white;
                    }
                    else
                    {
                        colors[index] = Color.black;
                    }
                }
                break;
            case SearchLocations.LeftArm:
                thresholdLeft = Mathf.Lerp(_mBoundsMin.x, _mBoundsMax.x, 0.4f);
                thresholdFront = Mathf.Lerp(_mBoundsMin.z, _mBoundsMax.z, 0.6f);
                thresholdUpper = Mathf.Lerp(_mBoundsMin.y, _mBoundsMax.y, 0.85f);
                thresholdLower = Mathf.Lerp(_mBoundsMin.y, _mBoundsMax.y, 0.75f);
                for (int index = 0; index < verts.Length; ++index)
                {
                    if (verts[index].x <= thresholdLeft &&
                        verts[index].y <= thresholdUpper &&
                        verts[index].y >= thresholdLower &&
                        verts[index].z <= thresholdFront)
                    {
                        colors[index] = Color.white;
                    }
                    else
                    {
                        colors[index] = Color.black;
                    }
                }
                break;
            case SearchLocations.RightArm:
                thresholdRight = Mathf.Lerp(_mBoundsMin.x, _mBoundsMax.x, 0.6f);
                thresholdFront = Mathf.Lerp(_mBoundsMin.z, _mBoundsMax.z, 0.6f);
                thresholdUpper = Mathf.Lerp(_mBoundsMin.y, _mBoundsMax.y, 0.85f);
                thresholdLower = Mathf.Lerp(_mBoundsMin.y, _mBoundsMax.y, 0.75f);
                for (int index = 0; index < verts.Length; ++index)
                {
                    if (verts[index].x >= thresholdRight &&
                        verts[index].y <= thresholdUpper &&
                        verts[index].y >= thresholdLower &&
                        verts[index].z <= thresholdFront)
                    {
                        colors[index] = Color.white;
                    }
                    else
                    {
                        colors[index] = Color.black;
                    }
                }
                break;
            case SearchLocations.LeftHand:
                thresholdLeft = Mathf.Lerp(_mBoundsMin.x, _mBoundsMax.x, 0.1f);
                thresholdUpper = Mathf.Lerp(_mBoundsMin.y, _mBoundsMax.y, 0.85f);
                thresholdLower = Mathf.Lerp(_mBoundsMin.y, _mBoundsMax.y, 0.75f);
                for (int index = 0; index < verts.Length; ++index)
                {
                    if (verts[index].x <= thresholdLeft &&
                        verts[index].y <= thresholdUpper &&
                        verts[index].y >= thresholdLower)
                    {
                        colors[index] = Color.white;
                    }
                    else
                    {
                        colors[index] = Color.black;
                    }
                }
                break;
            case SearchLocations.RightHand:
                thresholdRight = Mathf.Lerp(_mBoundsMin.x, _mBoundsMax.x, 0.9f);
                thresholdUpper = Mathf.Lerp(_mBoundsMin.y, _mBoundsMax.y, 0.85f);
                thresholdLower = Mathf.Lerp(_mBoundsMin.y, _mBoundsMax.y, 0.75f);
                for (int index = 0; index < verts.Length; ++index)
                {
                    if (verts[index].x >= thresholdRight &&
                        verts[index].y <= thresholdUpper &&
                        verts[index].y >= thresholdLower)
                    {
                        colors[index] = Color.white;
                    }
                    else
                    {
                        colors[index] = Color.black;
                    }
                }
                break;
            case SearchLocations.LeftFoot:
                thresholdLeft = Mathf.Lerp(_mBoundsMin.x, _mBoundsMax.x, 0.5f);
                thresholdUpper = Mathf.Lerp(_mBoundsMin.y, _mBoundsMax.y, 0.1f);
                for (int index = 0; index < verts.Length; ++index)
                {
                    if (verts[index].x <= thresholdLeft &&
                        verts[index].y <= thresholdUpper)
                    {
                        colors[index] = Color.white;
                    }
                    else
                    {
                        colors[index] = Color.black;
                    }
                }
                break;
            case SearchLocations.RightFoot:
                thresholdLeft = Mathf.Lerp(_mBoundsMin.x, _mBoundsMax.x, 0.5f);
                thresholdUpper = Mathf.Lerp(_mBoundsMin.y, _mBoundsMax.y, 0.1f);
                for (int index = 0; index < verts.Length; ++index)
                {
                    if (verts[index].x >= thresholdLeft &&
                        verts[index].y <= thresholdUpper)
                    {
                        colors[index] = Color.white;
                    }
                    else
                    {
                        colors[index] = Color.black;
                    }
                }
                break;
        }

        Vector3 pos = t.position;
        Quaternion rot = t.rotation;
        for (int index = 0; index < verts.Length; ++index)
        {
            if (colors[index] == Color.white)
            {
                _sLines.Add(pos + rot*verts[index]);
                _sLines.Add(pos + rot * verts[index] + rot * verts[index].normalized);
            }
        }

        if (_sSelectedMesh == mesh &&
            _sInstanceUVMap)
        {
            HighlightUVs(mesh, colors);
        }

        mesh.colors32 = colors;
        AssetDatabase.Refresh();
        Repaint();
    }

    void HighlightUVs(Mesh mesh, Color32[] colors)
    {
        if (mesh &&
            _sInstanceUVMap)
        {
            Vector2[] uvs = mesh.uv;
            Color[] pixels = _sInstanceUVMap.GetPixels();
            for (int index = 0; index < colors.Length; ++index)
            {
                if (colors[index] == Color.white)
                {
                    Vector2 uv = uvs[index];
                    //SetPixelForUV(pixels, ref uv, Color.white);
                    for (int y = 0; y < 10; ++y)
                    {
                        for (int x = 0; x < 10; ++x)
                        {
                            Vector2 temp = uv + new Vector2(x / (float)_sInstanceUVMap.width, y / (float)_sInstanceUVMap.height);
                            SetPixelForUV(pixels, ref temp, Color.white);
                        }
                    }
                }
            }
            _sInstanceUVMap.SetPixels(pixels);
            _sInstanceUVMap.Apply();
        }
    }

    void SetPixelForUV(Color[] pixels, ref Vector2 uv, Color color)
    {
        int x = (int)(uv.x * _sInstanceUVMap.width);
        x = Mathf.Max(0, x);
        x = Mathf.Min(_sInstanceUVMap.width - 1, x);

        int y = (int)(uv.y * _sInstanceUVMap.height);
        y = Mathf.Max(0, y);
        y = Mathf.Min(_sInstanceUVMap.width - 1, y);

        pixels[x + y * _sInstanceUVMap.width] = color;
    }
}