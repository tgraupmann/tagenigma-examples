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

    private static Mesh[] _mMeshes = null;

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
        LeftLeg,
        RightLeg,
        LeftFoot,
        RightFoot,
    }

    void OnGUI()
    {
        if (_mCompileDetected)
        {
            GUILayout.Label("Compiling...");
        }
        else
        {
            GUILayout.Label(string.Format("VERSION={0}", VERSION));

            if (null == _mMeshes)
            {
                _mMeshes = new Mesh[0];
            }

            int count = EditorGUILayout.IntField("Size", _mMeshes.Length);
            if (count != _mMeshes.Length &&
                count >= 0 &&
                count < 100)
            {
                _mMeshes = new Mesh[count];
                EditorPrefs.SetInt("MeshCount", count);
                ReloadMeshes(count);
            }
            for (int index = 0; index < count && count < 100; ++index)
            {
                _mMeshes[index] = (Mesh)EditorGUILayout.ObjectField(string.Format("Element {0}", index), _mMeshes[index], typeof(Mesh));
                if (null != _mMeshes[index])
                {
                    EditorPrefs.SetString(string.Format("Mesh{0}", index), AssetDatabase.GetAssetPath(_mMeshes[index]));
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
        _mMeshes = new Mesh[count];
        for (int index = 0; index < count; ++index)
        {
            string key = string.Format("Mesh{0}", index);
            if (!EditorPrefs.HasKey(key))
            {
                continue;
            }
            _mMeshes[index] = (Mesh)Resources.LoadAssetAtPath(EditorPrefs.GetString(key), typeof(Mesh));
        }
    }

    void Update()
    {
        if (EditorApplication.isCompiling)
        {
            _mCompileDetected = true;
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
        foreach (Mesh mesh in _mMeshes)
        {
            Process(search, mesh);
        }
    }

    private void Process(SearchLocations search, Mesh mesh)
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

        mesh.colors32 = colors;
        AssetDatabase.Refresh();
        Repaint();
    }
}