/*
 * Author:  Tim Graupmann
 * TAGENIGMA LLC, @copyright 2015  All rights reserved.
 *
*/

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Runtime.Remoting.Messaging;
using System.Text;
using TreeEditor;
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

    private bool _mUpdateSceneCamera = false;

    private bool _mShowFaceDirection = false;

    private bool _mShowFingerTips = false;

    private bool _mDisplayMarchCount = false;

    private bool _mExtendFingerTips = false;

    private List<KeyValuePair<Vector3, string>> _mSceneText = new List<KeyValuePair<Vector3, string>>();

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

    private enum SearchLocations
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

    private static List<KeyValuePair<Vector3, Color32>> _sLines = new List<KeyValuePair<Vector3, Color32>>();

    private void OnSceneGUI(SceneView sceneView)
    {
        GUI.skin.label.fontSize = 48;
        GUI.skin.label.normal.textColor = Color.magenta;
        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        Transform t = sceneView.camera.transform;
        foreach (KeyValuePair<Vector3, string> kvp in _mSceneText)
        {
            float dot = Vector3.Dot(t.forward, (kvp.Key - t.position).normalized);
            if (dot > 0)
            {
                Handles.Label(kvp.Key, kvp.Value);
            }
        }
    }

    private void OnGUI()
    {
        if (_mCompileDetected)
        {
            GUILayout.Label("Compiling...");
        }
        else
        {
            if (SceneView.onSceneGUIDelegate != this.OnSceneGUI)
            {
                SceneView.onSceneGUIDelegate += this.OnSceneGUI;
            }

            for (int index = 0; index < _sLines.Count; index += 2)
            {
                Debug.DrawLine(_sLines[index].Key, _sLines[index + 1].Key, _sLines[index].Value, Time.deltaTime, true);
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
                _mMeshObjects[index] =
                    (GameObject)
                        EditorGUILayout.ObjectField(string.Format("Element {0}", index), _mMeshObjects[index],
                            typeof (GameObject));
                if (flag)
                {
                    _sSelectedMesh = _mMeshObjects[index];
                    if (_sSelectedMesh)
                    {
                        EditorPrefs.SetInt("SelectedMesh", _sSelectedMesh.GetInstanceID());
                    }
                }
                GUILayout.EndHorizontal();
                if (null != _mMeshObjects[index])
                {
                    EditorPrefs.SetInt(string.Format("Mesh{0}", index), _mMeshObjects[index].GetInstanceID());
                }
            }

            _mUpdateSceneCamera = EditorGUILayout.Toggle("MoveSceneCamera", _mUpdateSceneCamera);

            _mShowFaceDirection = EditorGUILayout.Toggle("ShowFaceDirection", _mShowFaceDirection);

            _mShowFingerTips = EditorGUILayout.Toggle("ShowFingerTips", _mShowFingerTips);

            _mDisplayMarchCount = EditorGUILayout.Toggle("DisplayMarchCount", _mDisplayMarchCount);

            _mExtendFingerTips = EditorGUILayout.Toggle("ExtendFingerTips", _mExtendFingerTips);

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

            Texture2D tex = (Texture2D) EditorGUILayout.ObjectField(string.Empty, _sReferenceUVMap, typeof (Texture2D));
            if (tex != _sReferenceUVMap)
            {
                _sReferenceUVMap = tex;
                if (tex)
                {
                    EditorPrefs.SetInt("UVTex", tex.GetInstanceID());
                }
            }

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

    private void ReloadMeshes(int count)
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
            _mMeshObjects[index] = (GameObject) EditorUtility.InstanceIDToObject(instanceId);
        }

        if (EditorPrefs.HasKey("SelectedMesh"))
        {
            int instanceId = EditorPrefs.GetInt("SelectedMesh");
            _sSelectedMesh = (GameObject) EditorUtility.InstanceIDToObject(instanceId);
        }
        else
        {
            _sSelectedMesh = null;
        }

        if (EditorPrefs.HasKey("UVTex"))
        {
            int instanceId = EditorPrefs.GetInt("UVTex");
            _sReferenceUVMap = (Texture2D) EditorUtility.InstanceIDToObject(instanceId);
        }
    }

    private void Update()
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

    private void ClearColor(Mesh mesh)
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

    private void CalculateBounds(Vector3[] verts)
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
        _mSceneText.Clear();

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

        #region Show Bounding Box

        int[] triangles = mesh.triangles;

        Vector3 pos = t.position;
        Quaternion rot = t.rotation;

        Vector3 min;
        Vector3 max;
        GetBoundingBox(triangles, verts, colors, out min, out max);
        DrawBoundingBoxInWorldSpace(t, ref pos, ref rot, min, max, Color.yellow);

        #endregion

        #region Calculate world verts

        Vector3[] worldVerts = new Vector3[verts.Length];

        for (int i = 0; i < verts.Length; ++i)
        {
            Vector3 v = verts[i];
            Transform temp = t;
            while (temp)
            {
                v.x *= temp.localScale.x;
                v.y *= temp.localScale.y;
                v.z *= temp.localScale.z;
                temp = temp.parent;
            }
            worldVerts[i] = v;
        }

        #endregion

		Vector3[] perps = new Vector3[triangles.Length];

		#region Calculate perpendicular angles

		for (int i = 0; i < triangles.Length; i += 3)
		{
			int face1 = triangles[i];
			int face2 = triangles[i + 1];
			int face3 = triangles[i + 2];

			Vector3 side1 = verts[face2] - verts[face1];
			Vector3 side2 = verts[face3] - verts[face1];
			Vector3 perp = Vector3.Cross(side1, side2);
				
			perps[i] = perp;
			perps[i+1] = perp;
			perps[i+2] = perp;
		}

		#endregion

        Vector3[] worldPerps = new Vector3[triangles.Length];

        #region Calculate world perpendicular angles

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int face1 = triangles[i];
            int face2 = triangles[i + 1];
            int face3 = triangles[i + 2];

            Vector3 side1 = worldVerts[face2] - worldVerts[face1];
            Vector3 side2 = worldVerts[face3] - worldVerts[face1];
            Vector3 perp = Vector3.Cross(side1, side2).normalized;

            worldPerps[i] = perp;
            worldPerps[i + 1] = perp;
            worldPerps[i + 2] = perp;
        }

        #endregion

        if (_mShowFaceDirection)
        {
            HighlightFaces(t, triangles, verts, perps, colors);
        }

        FindRightFingers(t, triangles, verts, worldVerts, perps, worldPerps, colors);

        if (_sSelectedMesh == t.gameObject &&
            _sInstanceUVMap)
        {
            HighlightUVs(mesh, colors);
        }

        mesh.colors32 = colors;
        AssetDatabase.Refresh();
        Repaint();
    }

    private void HighlightVerteces(Transform t, Vector3[] verts, Color32[] colors)
    {
        Vector3 pos = t.position;
        Quaternion rot = t.rotation;
        for (int index = 0; index < verts.Length; ++index)
        {
            if (colors[index] != Color.black)
            {
                Vector3 v = verts[index];
                Transform temp = t;
                while (temp)
                {
                    v.x *= temp.localScale.x;
                    v.y *= temp.localScale.y;
                    v.z *= temp.localScale.z;
                    temp = temp.parent;
                }
                _sLines.Add(new KeyValuePair<Vector3, Color32>(pos + rot*v, Color.cyan));
                _sLines.Add(new KeyValuePair<Vector3, Color32>(pos + rot*v + rot*v.normalized, Color.cyan));
            }
        }
    }

    private void HighlightVertex(Transform t, int[] triangles, Vector3[] verts, Color32[] colors, int index,
        Color32 color, float length)
    {
        Vector3 pos = t.position;
        Quaternion rot = t.rotation;

        Vector3 v = verts[index];
        Transform temp = t;
        while (temp)
        {
            v.x *= temp.localScale.x;
            v.y *= temp.localScale.y;
            v.z *= temp.localScale.z;
            temp = temp.parent;
        }

        int face1 = index - (index%3);
        int face2 = face1 + 1;
        int face3 = face1 + 2;

        Vector3 side1 = verts[face2] - verts[face1];
        Vector3 side2 = verts[face3] - verts[face1];
        Vector3 perp = Vector3.Cross(side1, side2);

        _sLines.Add(new KeyValuePair<Vector3, Color32>(pos + rot*v, color));
        _sLines.Add(new KeyValuePair<Vector3, Color32>(pos + rot*v + rot*perp.normalized*length, color));
    }

    void HighlightFace(Transform t, int[] triangles, Vector3[] verts, Vector3[] perps, Color32[] colors, int index, Color32 color)
    {
        Vector3 pos = t.position;
        Quaternion rot = t.rotation;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int face1 = triangles[i];
            int face2 = triangles[i + 1];
            int face3 = triangles[i + 2];
            if (face1 == index ||
                face2 == index ||
                face3 == index)
            {
                Vector3 v = (verts[face1] + verts[face2] + verts[face3]) / 3f;
                Transform temp = t;
                while (temp)
                {
                    v.x *= temp.localScale.x;
                    v.y *= temp.localScale.y;
                    v.z *= temp.localScale.z;
                    temp = temp.parent;
                }

                _sLines.Add(new KeyValuePair<Vector3, Color32>(pos + rot * v, color));
                _sLines.Add(new KeyValuePair<Vector3, Color32>(pos + rot * v + rot * perps[i].normalized * 0.2f, color));

                /*
                float ratio1 = Vector3.Dot(Vector3.right, perp.normalized);
                //float ratio1 = Vector3.Dot(new Vector3(-1, 0, -1).normalized, perp.normalized);
                if (Mathf.Abs(ratio1) < 0.5f)
                {
                    colors[face1] = Color.black;
                }
                else if (ratio1 > 0f)
                {
                    colors[face1] = Color.Lerp(Color.black, Color.red, Mathf.Abs(ratio1));
                }
                else
                {
                    colors[face1] = Color.Lerp(Color.black, Color.green, Mathf.Abs(ratio1));
                }
                colors[face2] = colors[face1];
                colors[face3] = colors[face1];
                */
            }
        }
    }

    void HighlightFace(Transform t, int[] triangles, Vector3[] verts, Vector3[] perps, Color32[] colors, int index)
    {
        HighlightFace(t, triangles, verts, perps, colors, index, Color.cyan);
    }

    void HighlightFaces(Transform t, int[] triangles, Vector3[] verts, Vector3[] perps, Color32[] colors)
    {
        for (int index = 0; index < verts.Length; ++index)
        {
            if (colors[index] != Color.black)
            {
                HighlightFace(t, triangles, verts, perps, colors, index);
            }
        }
    }

    void AddFace(Dictionary<int, List<int>> dictFaces, int face, int face1, int face2, int face3)
    {
        if (!dictFaces.ContainsKey(face))
        {
            dictFaces[face] = new List<int>();
        }
        dictFaces[face].Add(face1);
        dictFaces[face].Add(face2);
        dictFaces[face].Add(face3);
    }

    void AddVertex(Dictionary<Vector3, List<int>> dictVerteces, Vector3[] verts, int face)
    {
        Vector3 v = verts[face];
        if (!dictVerteces.ContainsKey(v))
        {
            dictVerteces[v] = new List<int>();
        }
        if (!dictVerteces[v].Contains(face))
        {
            dictVerteces[v].Add(face);
        }
    }

    void AddVertex(Dictionary<Vector3, List<int>> dictVerteces, Vector3[] verts, int face1, int face2, int face3)
    {
        Vector3 v = verts[face1];
        if (!dictVerteces.ContainsKey(v))
        {
            dictVerteces[v] = new List<int>();
        }
        if (!dictVerteces[v].Contains(face2))
        {
            dictVerteces[v].Add(face2);
        }
        if (!dictVerteces[v].Contains(face3))
        {
            dictVerteces[v].Add(face3);
        }
    }

    void FindRightFingers(Transform t, int[] triangles, Vector3[] verts, Vector3[] worldVerts, Vector3[] perps, Vector3[] worldPerps, Color32[] colors)
    {
        Vector3 pos = t.position;
        Quaternion rot = t.rotation;

        List<int> visited = new List<int>();

        #region Build a dictionary for quick face look-up

        // find all the shared faces and verts
        Dictionary<int, List<int>> dictFaces = new Dictionary<int, List<int>>();
        Dictionary<Vector3, List<int>> dictVerteces = new Dictionary<Vector3, List<int>>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int face1 = triangles[i];
            int face2 = triangles[i + 1];
            int face3 = triangles[i + 2];

            if (colors[face1] != Color.black ||
                colors[face2] != Color.black ||
                colors[face3] != Color.black)
            {
                AddFace(dictFaces, face1, face1, face2, face3);
                AddFace(dictFaces, face2, face1, face2, face3);
                AddFace(dictFaces, face3, face1, face2, face3);

                AddVertex(dictVerteces, verts, face1, face2, face3);
                AddVertex(dictVerteces, verts, face2, face1, face3);
                AddVertex(dictVerteces, verts, face3, face1, face2);
            }
        }

        #endregion

        #region calculate selected bounding box

        //find the bounding box
        Vector3 boundsMin = Vector3.zero;
        Vector3 boundsMax = Vector3.zero;
        int index = 0;
        foreach (KeyValuePair<int, List<int>> kvp in dictFaces)
        {
            int face = kvp.Key;
            if (index == 0)
            {
                boundsMin = verts[face];
                boundsMax = verts[face];
            }
            else
            {
                boundsMin.x = Mathf.Min(boundsMin.x, verts[face].x);
                boundsMin.y = Mathf.Min(boundsMin.y, verts[face].y);
                boundsMin.z = Mathf.Min(boundsMin.z, verts[face].z);

                boundsMax.x = Mathf.Max(boundsMax.x, verts[face].x);
                boundsMax.y = Mathf.Max(boundsMax.y, verts[face].y);
                boundsMax.z = Mathf.Max(boundsMax.z, verts[face].z);
            }
            ++index;
        }

        #endregion

        #region Show right-most edge

        if (_sSelectedMesh == t.gameObject)
        {
            // highlight the edge polys
            foreach (KeyValuePair<int, List<int>> kvp in dictFaces)
            {
                int face = kvp.Key;
                if (verts[face].x == boundsMin.x)
                {
                    if (!visited.Contains(face))
                    {
                        visited.Add(face);
                        int face1 = dictFaces[face][0];
                        int face2 = dictFaces[face][1];
                        int face3 = dictFaces[face][2];
                        /*
                        Vector3 v = (verts[face1] + verts[face2] + verts[face3]) / 3f;
                        if (Vector3.Dot(Vector3.right, perps[face]) > 0f)
                        {
                            DrawVectorInWorldSpace(t, ref pos, ref rot, v, -perps[face], Color.red);
                        }
                        else
                        {
                            DrawVectorInWorldSpace(t, ref pos, ref rot, v, perps[face], Color.red);
                        }
                        */

                        /*
                        DrawPointInWorldSpace(t, ref pos, ref rot, verts[face1], Color.green);
                        DrawPointInWorldSpace(t, ref pos, ref rot, verts[face2], Color.green);
                        DrawPointInWorldSpace(t, ref pos, ref rot, verts[face2], Color.green);
                        DrawPointInWorldSpace(t, ref pos, ref rot, verts[face3], Color.green);
                        DrawPointInWorldSpace(t, ref pos, ref rot, verts[face3], Color.green);
                        DrawPointInWorldSpace(t, ref pos, ref rot, verts[face1], Color.green);
                         */

                        if (_mUpdateSceneCamera &&
                            _sSelectedMesh == t.gameObject)
                        {
                            Vector3 v = GetVectorInWorldSpace(t, ref pos, ref rot,
                                verts[face], perps[face], Color.red);
							foreach (SceneView sceneView in SceneView.sceneViews)
							{
                                sceneView.LookAt(v);
							}
                        }
                    }
                }
            }
        }

        #endregion

        #region Sort faces from the right

        //sort faces
        List<int> sortedFaces = new List<int>();
        foreach (KeyValuePair<int, List<int>> kvp in dictFaces)
        {
            int face = kvp.Key;
            sortedFaces.Add(face);
        }
        //sort faces by X
        sortedFaces.Sort(
            delegate(int index1, int index2)
            {
                return verts[index1].x.CompareTo(verts[index2].x);
            });

        /*
        if (sortedFaces.Count > 0)
        {
            int face1 = dictFaces[sortedFaces[0]][0];
            int face2 = dictFaces[sortedFaces[0]][1];
            int face3 = dictFaces[sortedFaces[0]][2];
            //DrawVectorInWorldSpace(t, ref pos, ref rot, verts[face1], GetPerpendicular(verts, face1, face2, face3), Color.red, 1);

            DrawPointInWorldSpace(t, ref pos, ref rot, verts[face1], Color.green);
            DrawPointInWorldSpace(t, ref pos, ref rot, verts[face2], Color.green);
            DrawPointInWorldSpace(t, ref pos, ref rot, verts[face2], Color.green);
            DrawPointInWorldSpace(t, ref pos, ref rot, verts[face3], Color.green);
            DrawPointInWorldSpace(t, ref pos, ref rot, verts[face3], Color.green);
            DrawPointInWorldSpace(t, ref pos, ref rot, verts[face1], Color.green);
        }
        */

        #endregion

        #region Show sort order with gradient

        // highlight the edge polys
        foreach (int face in sortedFaces)
        {
            int face1 = dictFaces[face][0];
            int face2 = dictFaces[face][1];
            int face3 = dictFaces[face][2];
            if (!visited.Contains(face))
            {
                visited.Add(face);

                float ratio1;
                float ratio2;
                float ratio3;

                Vector3 perp = GetPerpendicular(verts, face1, face2, face3);

                ratio1 = Vector3.Dot(Vector3.right, perp.normalized);
                if (Mathf.Abs(ratio1) < 0.5f)
                {
                    ratio1 = sortedFaces.IndexOf(face1) / (float)sortedFaces.Count;
                    ratio2 = sortedFaces.IndexOf(face2) / (float)sortedFaces.Count;
                    ratio3 = sortedFaces.IndexOf(face3) / (float)sortedFaces.Count;

                    colors[face1] = Color.Lerp(Color.magenta, Color.yellow, ratio1);
                    colors[face2] = Color.Lerp(Color.magenta, Color.yellow, ratio2);
                    colors[face3] = Color.Lerp(Color.magenta, Color.yellow, ratio3);
                }
                else if (ratio1 > 0f)
                {
                    colors[face1] = Color.Lerp(Color.black, Color.magenta, Mathf.Abs(ratio1));
                }
                else
                {
                    colors[face1] = Color.Lerp(Color.black, Color.green, Mathf.Abs(ratio1));
                }
                colors[face2] = colors[face1];
                colors[face3] = colors[face1];

                //HighlightFace(t, triangles, verts, colors, face, Color.green);
                //HighlightVertex(t, triangles, verts, colors, face, Color.black, 0.25f);
            }
        }

        #endregion

        #region Find Adjacent Faces don't just rely on triangles[] to tell you they are adjacent

        /*
        Dictionary<int, List<int>> adjacentList = new Dictionary<int, List<int>>();

        for (int i = 0; i < triangles.Length; ++i)
        {
            int a1 = triangles[i];
            int face1 = triangles[i - i%3];
            int face2 = triangles[i - i%3 + 1];
            int face3 = triangles[i - i % 3 + 2];
            adjacentList[a1] = new List<int>();
            adjacentList[a1].Add(face1);
            adjacentList[a1].Add(face2);
            adjacentList[a1].Add(face3);
            for (int j = i + 1; j < triangles.Length; ++j)
            {
                int b1 = triangles[j];
                if (Vector3.Distance(verts[a1], verts[b1]) < 0.1f)
                {
                    face1 = triangles[j - j % 3];
                    face2 = triangles[j - j % 3 + 1];
                    face3 = triangles[j - j % 3 + 2];
                    adjacentList[a1].Add(face1);
                    adjacentList[a1].Add(face2);
                    adjacentList[a1].Add(face3);
                }
            }
        }
        */

        #endregion

        #region Marching Algorithm

        ClearColors(colors);

		#region Show verts in sort order

        List<int> marchList = new List<int>();
        foreach (int face in sortedFaces)
        {
            marchList.Add(face);
        }

        // march counts in sort order
        Dictionary<int, int> marchCounts = new Dictionary<int, int>();
		int order = 0;
		foreach (int face in sortedFaces)
		{
			marchCounts[face] = order;
			++order;
		}

		#endregion

		#region Show number of adjacent polys
		/*
		order = 0;
		marchCounts.Clear();
		foreach (int face in sortedFaces)
		{
			Vector3 v = verts[face];
			foreach (int adjacent in dictVerteces[v])
			{
				if (marchCounts.ContainsKey(face))
				{
					++marchCounts[face];
					order = Mathf.Max (order, marchCounts[face]);
				}
				else
				{
					marchCounts[face] = 1;
				}
			}
		}
		*/
		#endregion

		#region March adjacent polys
        List<int> searchableList = new List<int>();
		searchableList.Add(sortedFaces[sortedFaces.Count - 1]);

		marchCounts.Clear();
		order = 0;
		int lastOrder = 0;
        while (marchList.Count > 0)
        {
            //Debug.Log("SearchCount: " + searchableList.Count);
            if (searchableList.Count > 0)
            {
				MarchFaces(searchableList, dictFaces, sortedFaces, dictVerteces,
                    verts, perps, marchList, searchableList[0], marchCounts,
                    ref order, 0);
            }
			if (lastOrder != order)
			{
				lastOrder = order;
			}
			else
			{
				break;
			}
            if (searchableList.Count == 0)
            {
                break;
            }
        }

        DisplayMarchCount(marchCounts, dictFaces, colors);

		#endregion

        #region Isolate each finger tip

        if (_mShowFingerTips)
        {
            #region Highlight the ends of the fingers

            Dictionary<int, int> fingerGroups = new Dictionary<int, int>();
            foreach (KeyValuePair<int, int> kvp in marchCounts)
            {
                float ratio1 = kvp.Value / (float)order;
                if (ratio1 > 0.5f)
                {
                    fingerGroups[kvp.Key] = 0;
                }
            }
            marchCounts = fingerGroups;

            #endregion

            DisplayMarchCount(marchCounts, dictFaces, colors);

            List<List<int>> fingers = new List<List<int>>();
            for (int fingerCount = 0; fingerCount < 5; ++fingerCount)
            {
                List<int> searchGroup = new List<int>();
                foreach (KeyValuePair<int, int> kvp in fingerGroups)
                {
                    searchGroup.Add(kvp.Key);
                    break;
                }
                List<int> finger = GetAdjacentFaces(fingerGroups, searchGroup, sortedFaces, dictFaces, dictVerteces, verts);
                fingers.Add(finger);
            }

            //sort fingers by Z
            fingers.Sort(
                delegate(List<int> finger1, List<int> finger2)
                {
                    if (finger1.Count == 0)
                    {
                        return 0;
                    }
                    int face1 = finger1[0];
                    if (finger2.Count == 0)
                    {
                        return 0;
                    }
                    int face2 = finger2[0];
                    return verts[face1].z.CompareTo(verts[face2].z);
                });

            DisplayFingers(fingers, dictFaces, colors);

            if (_mDisplayMarchCount)
            {
                ClearColors(colors);

                // march counts in sort order
                marchCounts = new Dictionary<int, int>();
                order = 0;
                foreach (int face in sortedFaces)
                {
                    marchCounts[face] = order;
                    ++order;
                }

                DisplayMarchCount(marchCounts, dictFaces, colors);
            }

            if (_mExtendFingerTips)
            {
                ClearColors(colors);

                // select a larger part of each finger
                for (int fingerId = 0; fingerId < fingers.Count; ++fingerId)
                {
                    List<int> finger = fingers[fingerId];
                    if (finger.Count > 0)
                    {
                        //find finger midpoint
                        Vector3 startPos = verts[finger[0]];
                        Vector3 midpoint = Vector3.zero;
                        Vector3 min = startPos;
                        Vector3 max = startPos;
                        for (int i = 1; i < finger.Count; ++i)
                        {
                            min.x = Mathf.Min(min.x, verts[finger[i]].x);
                            min.y = Mathf.Min(min.y, verts[finger[i]].y);
                            min.z = Mathf.Min(min.z, verts[finger[i]].z);
                            max.x = Mathf.Min(max.x, verts[finger[i]].x);
                            max.y = Mathf.Max(max.y, verts[finger[i]].y);
                            max.z = Mathf.Max(max.z, verts[finger[i]].z);
                        }
                        midpoint = (min + max)*0.5f;

                        //find finger direction
                        Vector3 direction = (midpoint - startPos).normalized;

                        fingerGroups = new Dictionary<int, int>();
                        for (int i = 0; i < sortedFaces.Count/3; ++i)
                        {
                            fingerGroups[sortedFaces[i]] = 0;
                        }
                        fingers[fingerId] = GetAdjacentFaces(fingerId, t, finger, startPos, direction, 
                            sortedFaces, dictFaces, dictVerteces, triangles, verts, worldVerts, perps, worldPerps, colors);
                        //break;
                    }

                    //DisplayFingers(fingers, dictFaces, colors);
                }
            }
        }

        #endregion

        #endregion
    }

    void ClearColors(Color32[] colors)
    {
        #region Clear colors

        for (int i = 0; i < colors.Length; ++i)
        {
            colors[i] = Color.black;
        }

        #endregion
    }

    void DisplayMarchCount(Dictionary<int, int> marchCounts, Dictionary<int, List<int>> dictFaces, Color32[] colors)
    {
        foreach (KeyValuePair<int, int> kvp in marchCounts)
        {
            int face = kvp.Key;
            int face1 = dictFaces[face][0];
            int face2 = dictFaces[face][1];
            int face3 = dictFaces[face][2];

            float ratio1 = kvp.Value / (float)marchCounts.Count;
            float ratio2 = ratio1;
            float ratio3 = ratio1;

            if (colors[face1] == Color.black)
            {
                colors[face1] = GetColorRatio(ratio1);
            }
            if (colors[face2] == Color.black)
            {
                colors[face2] = GetColorRatio(ratio2);
            }
            if (colors[face3] == Color.black)
            {
                colors[face3] = GetColorRatio(ratio3);
            }
        }
    }

    void DisplayFingers(List<List<int>> fingers, Dictionary<int, List<int>> dictFaces, Color32[] colors)
    {
        // color fingers by id
        Color color = Color.black;
        for (int fingerId = 0; fingerId < fingers.Count; ++fingerId)
        {
            List<int> finger = fingers[fingerId];
            for (int i = 0; i < finger.Count; ++i)
            {
                int face = finger[i];
                int face1 = dictFaces[face][0];
                int face2 = dictFaces[face][1];
                int face3 = dictFaces[face][2];
                switch (fingerId)
                {
                    case 0:
                        color = Color.red;
                        break;
                    case 1:
                        color = Color.green;
                        break;
                    case 2:
                        color = Color.blue;
                        break;
                    case 3:
                        color = Color.magenta;
                        break;
                    case 4:
                        color = Color.yellow;
                        break;
                }
                colors[face1] = color;
                colors[face2] = color;
                colors[face3] = color;
            }
        }

        //make tip white
        for (int fingerId = 0; fingerId < fingers.Count; ++fingerId)
        {
            List<int> finger = fingers[fingerId];
            if (finger.Count > 0)
            {
                int face = finger[0];
                int face1 = dictFaces[face][0];
                int face2 = dictFaces[face][1];
                int face3 = dictFaces[face][2];
                colors[face1] = Color.white;
                colors[face2] = Color.white;
                colors[face3] = Color.white;
            }
        }
    }

    List<int> GetAdjacentFaces(Dictionary<int, int> group, 
        List<int> searchGroup,
        List<int> sortedFaces,
        Dictionary<int, List<int>> dictFaces,
        Dictionary<Vector3, List<int>> dictVerteces,
        Vector3[] verts)
    {
        List<int> result = new List<int>();

        for (int searchId = 0; searchId < searchGroup.Count; ++searchId)
        {
            for (int i = 0; i < 3; ++i)
            {
                int face = dictFaces[searchGroup[searchId]][i];
                Vector3 v = verts[face];
                foreach (int adjacent in dictVerteces[v])
                {
                    if (group.ContainsKey(adjacent) &&
                        !searchGroup.Contains(adjacent))
                    {
                        searchGroup.Add(adjacent);
                    }
                }
            }
        }

        foreach (int face in searchGroup)
        {
            result.Add(face);
        }

        #region remove search group from group
        foreach (int face in searchGroup)
        {
            if (group.ContainsKey(face))
            {
                group.Remove(face);
            }
        }
        #endregion

        //sort faces by X
        result.Sort(
            delegate(int index1, int index2)
            {
                return sortedFaces.IndexOf(index1).CompareTo(sortedFaces.IndexOf(index2));
            });

        return result;
    }

    List<int> GetAdjacentFaces(int fingerIndex,
        Transform t,
        List<int> oldFinger,
        Vector3 startPos,
        Vector3 direction,
        List<int> sortedFaces,
        Dictionary<int, List<int>> dictFaces,
        Dictionary<Vector3, List<int>> dictVerteces,
        int[] triangles,
        Vector3[] verts,
        Vector3[] worldVerts,
        Vector3[] perps,
        Vector3[] worldPerps,
        Color32[] colors)
    {
        Vector3 pos = t.position;
        Quaternion rot = t.rotation;

        List<int> result = new List<int>();

        // copy old finger selection to search group
        List<int> searchGroup = new List<int>();
        foreach (int face in oldFinger)
        {
            foreach (int adjacent in dictFaces[face])
            {
                //colors[adjacent] = Color.red;
                if (!searchGroup.Contains(adjacent))
                {
                    searchGroup.Add(adjacent);
                }
            }
            result.Add(face);
        }

        Vector3 min;
        Vector3 max;
        GetBoundingBox(triangles, verts, oldFinger, out min, out max);
        DrawBoundingBoxInWorldSpace(t, ref pos, ref rot, min, max, Color.green);

        bool first = true;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int face1 = triangles[i];
            int face2 = triangles[i+1];
            int face3 = triangles[i+2];
            Vector3 perp = GetPerpendicular(verts, triangles[i], triangles[i + 1], triangles[i + 2]).normalized;
            colors[triangles[i]] = Color.Lerp(Color.black, Color.white, Vector3.Dot(Vector3.left, perp));
            colors[triangles[i + 1]] = Color.Lerp(Color.black, Color.white, Vector3.Dot(Vector3.left, perp));
            colors[triangles[i + 2]] = Color.Lerp(Color.black, Color.white, Vector3.Dot(Vector3.left, perp));
            if (oldFinger.Contains(face1) ||
                oldFinger.Contains(face2) ||
                oldFinger.Contains(face3))
            {
                for (int j = 0; j < 3; ++j)
                {
                    int k = i + j;
                    //colors[triangles[k]] = GetColorRatio(Mathf.Abs(Vector3.Dot(Vector3.left, (verts[triangles[k]] - startPos).normalized)));
                    //colors[triangles[k]] = GetColorRatio(Vector3.Dot(Vector3.left, perps[k]));
                    //colors[triangles[k]] = GetColorRatio(Vector3.Dot(Vector3.left, GetPerpendicular(verts, triangles[i], triangles[i + 1], triangles[i + 2])));
                    //colors[triangles[k]] = GetColorRatio(Vector3.Dot(Vector3.left, GetPerpendicular(verts, triangles[i], triangles[i + 1], triangles[i + 2])));
                }

                // show face direction
                Vector3 a = (verts[face1] + verts[face2] + verts[face3]) / 3f; // center of the face
                DrawVectorInWorldSpace(t, ref pos, ref rot, a, perps[i], Color.cyan, 0.1f); //show face direction on selected finger verts
                //DrawPointInWorldSpace(t, ref pos, ref rot, min, Color.red);
                //DrawPointInWorldSpace(t, ref pos, ref rot, max, Color.red);
            }
        }

        Vector3 midpoint = (min + max)*0.5f;
        DrawVectorInWorldSpace(t, ref pos, ref rot, midpoint, (min - midpoint).normalized, Color.magenta, GetDistanceInWorldSpace(t, ref pos, ref rot, midpoint, min)); //draw from the midpoint to the min point
        DrawVectorInWorldSpace(t, ref pos, ref rot, midpoint, (max - midpoint).normalized, Color.yellow, GetDistanceInWorldSpace(t, ref pos, ref rot, midpoint, max)); //draw from the midpoint to the max point

        Vector3 texPos = new Vector3(min.x, midpoint.y, midpoint.z);
        _mSceneText.Add(new KeyValuePair<Vector3, string>(GetPointInWorldSpace(t, ref pos, ref rot, texPos), fingerIndex.ToString()));

        // show finger direction
        //DrawPointInWorldSpace(t, ref pos, ref rot, verts[oldFinger[0]], Color.white);
        //DrawPointInWorldSpace(t, ref pos, ref rot, midpoint, Color.red);
        //DrawPointInWorldSpace(t, ref pos, ref rot, min, Color.red);
        //DrawPointInWorldSpace(t, ref pos, ref rot, max, Color.red);


        float scale = verts[sortedFaces[0]].x - verts[sortedFaces[sortedFaces.Count - 1]].x;

        //direction = Vector3.left;

        for (int searchId = 0; searchId < searchGroup.Count; ++searchId)
        {
            for (int i = 0; i < 3; ++i)
            {
                int face = dictFaces[searchGroup[searchId]][i];
                Vector3 v = verts[face];
                ///*
                foreach (int adjacent in dictVerteces[v])
                {
                    //draw based on normalized distance to the start position
                    //colors[adjacent] = GetColorRatio(Vector3.Magnitude((startPos - v)/scale));
                    
                    //ignore Y component and draw based on normalized distance to the start position
                    //colors[adjacent] = GetColorRatio((startPos.x-v.x)/scale);

                    // show the dot product compared to the start position
                    Vector3 av = verts[adjacent];
                    Vector3 ap = perps[adjacent];
                    //colors[adjacent] = GetColorRatio(Mathf.Abs(Vector3.Dot(direction, (av - startPos).normalized)));
                    //colors[adjacent] = GetColorRatio(Mathf.Abs(Vector3.Dot(perps[face], perps[adjacent])));
                    //colors[adjacent] = Color.Lerp(Color.black, Color.red, Vector3.Dot(Vector3.right, worldPerps[adjacent]));
                    //colors[adjacent] = Color.Lerp(Color.black, Color.red, Vector3.Dot(direction, (av - startPos).normalized));
                    //colors[adjacent] = Color.Lerp(Color.black, Color.red, Vector3.Dot(Vector3.left, (worldVerts[adjacent] - worldVerts[oldFinger[0]]).normalized));
                    //colors[adjacent] = Color.Lerp(Color.black, Color.red, Vector3.Dot(Vector3.forward, worldPerps[adjacent]));
                    //colors[adjacent] = GetColorRatio(Mathf.Abs(Vector2.Dot(new Vector2(direction.x, direction.z), new Vector2(av.x - startPos.x, av.z - startPos.z).normalized)));
                    //colors[adjacent] = Color.Lerp(Color.black, Color.red, Vector2.Dot(new Vector2(direction.x, direction.z), new Vector2(av.x - startPos.x, av.z - startPos.z).normalized));
                    if (!searchGroup.Contains(adjacent))
                    {
                        searchGroup.Add(adjacent);
                    }
                }
                //*/
                continue;
                if (Vector3.Magnitude((startPos - v) / scale) < 0.2f)
                {
                    foreach (int adjacent in dictVerteces[v])
                    {
                        colors[adjacent] = Color.white;

                        if (!searchGroup.Contains(adjacent))
                        {
                            searchGroup.Add(adjacent);
                        }

                        if (!result.Contains(adjacent))
                        {
                            result.Add(adjacent);
                        }


                        /*
                        const float k = 1f;
                        const float threshold = .45f;
                        //colors[adjacent] = GetColorRatio(Vector3.Dot(direction, k * (startPos - verts[face]).normalized));
                        if (threshold < Vector3.Dot(direction, k*(startPos - verts[adjacent]).normalized))
                        {
                            //colors[adjacent] = Color.white;
                            if (!result.Contains(adjacent))
                            {
                                result.Add(adjacent);
                            }
                        }
                        */
                    }
                }
            }
        }

        //sort faces by X
        result.Sort(
            delegate(int index1, int index2)
            {
                return sortedFaces.IndexOf(index1).CompareTo(sortedFaces.IndexOf(index2));
            });

        return result;
    }

    Color GetColorRatio(float ratio)
    {
        if (ratio < 0.3f)
        {
            return Color.Lerp(Color.red, Color.green, ratio/.3f);
        }
        else if (ratio < 0.6f)
        {
            return Color.Lerp(Color.green, Color.blue, (ratio - .3f) / .3f);
        }
        else
        {
            return Color.Lerp(Color.blue, Color.magenta, (ratio - 0.6f) / .6f);
        }
    }

    void MarchFaces(List<int> searchableList, Dictionary<int, List<int>> dictFaces,
        List<int> sortedFaces, Dictionary<Vector3, List<int>> dictVerteces,
        Vector3[] verts,
	    Vector3[] perps,
        List<int> marchList,
        int march, Dictionary<int, int> marchCounts, ref int order, int depth)
    {
		//Debug.Log("March: "+march);

		// the selected face
		int face1 = dictFaces[march][0];
		int face2 = dictFaces[march][1];
		int face3 = dictFaces[march][2];

		for (int i = 0; i < 3; ++i)
		{
			int face = dictFaces[march][i];
			if (searchableList.Contains(face))
	        {
				searchableList.Remove(face);
				marchCounts[face] = order;
	        }

	        if (marchList.Contains(face))
	        {
	            marchList.Remove(face);
	        }

			Vector3 v = verts[face];
			
			foreach (int adjacent in dictVerteces[v])
			{
				//Debug.Log("Adjacent: " + adjacent);
				if (marchList.Contains(adjacent) &&
				    !searchableList.Contains(adjacent))
				{
					searchableList.Add(adjacent);
				}
			}
		}

		++order;

		/*
        //sort faces by X
        searchableList.Sort(
            delegate(int index1, int index2)
            {
                return sortedFaces.IndexOf(index1).CompareTo(sortedFaces.IndexOf(index2));
		});
		*/

		/*
        if (searchableList.Count > 2)
        {
            searchableList.RemoveRange(2, searchableList.Count - 2);
        }
		*/

		/*
		//sort faces by dot product
		searchableList.Sort(
			delegate(int index1, int index2)
			{
				return Vector3.Dot(perps[march], perps[index1]).CompareTo(Vector3.Dot(perps[march], perps[index2]));
			}
		);
		*/
    }

    Vector3 GetPerpendicular(Vector3[] verts, int face1, int face2, int face3)
    {
        Vector3 side1 = verts[face2] - verts[face1];
        Vector3 side2 = verts[face3] - verts[face1];
        return Vector3.Cross(side1.normalized, side2.normalized);
    }

    Vector3 GetVectorInWorldSpace(Transform t, ref Vector3 pos, ref Quaternion rot, Vector3 v, Vector3 direction, Color32 color)
    {
        Transform temp = t;
        while (temp)
        {
            v.x *= temp.localScale.x;
            v.y *= temp.localScale.y;
            v.z *= temp.localScale.z;
            temp = temp.parent;
        }
        return pos + rot * v;
    }

    Vector3 DrawVectorInWorldSpace(Transform t, ref Vector3 pos, ref Quaternion rot, Vector3 v, Vector3 direction, Color32 color)
    {
        Transform temp = t;
        while (temp)
        {
            v.x *= temp.localScale.x;
            v.y *= temp.localScale.y;
            v.z *= temp.localScale.z;
            temp = temp.parent;
        }
        _sLines.Add(new KeyValuePair<Vector3, Color32>(pos + rot * v, color));
        _sLines.Add(new KeyValuePair<Vector3, Color32>(pos + rot * v + rot * direction.normalized * 0.2f, color));

        return pos + rot*v;
    }

    Vector3 GetPointInWorldSpace(Transform t, ref Vector3 pos, ref Quaternion rot, Vector3 v)
    {
        Transform temp = t;
        while (temp)
        {
            v.x *= temp.localScale.x;
            v.y *= temp.localScale.y;
            v.z *= temp.localScale.z;
            temp = temp.parent;
        }
        return pos + rot * v;
    }

    float GetDistanceInWorldSpace(Transform t, ref Vector3 pos, ref Quaternion rot, Vector3 point1, Vector3 point2)
    {
        Vector3 worldPoint1 = GetPointInWorldSpace(t, ref pos, ref rot, point1);
        Vector3 worldPoint2 = GetPointInWorldSpace(t, ref pos, ref rot, point2);
        return Vector3.Distance(worldPoint1, worldPoint2);
    }

    Vector3 DrawVectorInWorldSpace(Transform t, ref Vector3 pos, ref Quaternion rot, Vector3 v, Vector3 direction, Color32 color, float length)
    {
        Vector3 p = GetPointInWorldSpace(t, ref pos, ref rot, v);
        _sLines.Add(new KeyValuePair<Vector3, Color32>(p, color));
        _sLines.Add(new KeyValuePair<Vector3, Color32>(p + rot * direction.normalized * length, color));

        return pos + rot * v;
    }

    void DrawPointInWorldSpace(Transform t, ref Vector3 pos, ref Quaternion rot, Vector3 v, Color32 color)
    {
        _sLines.Add(new KeyValuePair<Vector3, Color32>(GetPointInWorldSpace(t, ref pos, ref rot, v), color));
    }

    private void GetBoundingBox(int[] triangles, Vector3[] verts, List<int> subset, out Vector3 min,
        out Vector3 max)
    {
        min = Vector3.zero;
        max = Vector3.zero;
        bool first = true;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int face1 = triangles[i];
            int face2 = triangles[i + 1];
            int face3 = triangles[i + 2];
            if (!subset.Contains(face1) &&
                !subset.Contains(face2) &&
                !subset.Contains(face3))
            {
                continue;
            }

            Vector3 facePoint = (verts[face1] + verts[face2] + verts[face3])/3f; // center of the face
            if (first)
            {
                first = false;
                min = max = facePoint;
            }
            else
            {
                min.x = Mathf.Min(min.x, facePoint.x);
                min.y = Mathf.Min(min.y, facePoint.y);
                min.z = Mathf.Min(min.z, facePoint.z);
                max.x = Mathf.Max(max.x, facePoint.x);
                max.y = Mathf.Max(max.y, facePoint.y);
                max.z = Mathf.Max(max.z, facePoint.z);
            }
        }
    }

    void GetBoundingBox(int[] triangles, Vector3[] verts, Color32[] colors, out Vector3 min, out Vector3 max)
    {
        min = Vector3.zero;
        max = Vector3.zero;
        bool first = true;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int face1 = triangles[i];
            int face2 = triangles[i + 1];
            int face3 = triangles[i + 2];
            if (colors[face1] != Color.black ||
                colors[face2] != Color.black ||
                colors[face3] != Color.black)
            {
                Vector3 facePoint = (verts[face1] + verts[face2] + verts[face3]) / 3f; // center of the face
                if (first)
                {
                    first = false;
                    min = max = facePoint;
                }
                else
                {
                    min.x = Mathf.Min(min.x, facePoint.x);
                    min.y = Mathf.Min(min.y, facePoint.y);
                    min.z = Mathf.Min(min.z, facePoint.z);
                    max.x = Mathf.Max(max.x, facePoint.x);
                    max.y = Mathf.Max(max.y, facePoint.y);
                    max.z = Mathf.Max(max.z, facePoint.z);
                }
            }
        }
    }

    void DrawBoundingBoxInWorldSpace(Transform t, ref Vector3 pos, ref Quaternion rot, Vector3 min, Vector3 max, Color32 color)
    {
        Vector3 p1 = new Vector3(min.x, min.y, min.z);
        Vector3 p2 = new Vector3(min.x, max.y, min.z);
        Vector3 p3 = new Vector3(min.x, min.y, max.z);
        Vector3 p4 = new Vector3(min.x, max.y, max.z);
        Vector3 p5 = new Vector3(max.x, min.y, min.z);
        Vector3 p6 = new Vector3(max.x, max.y, min.z);
        Vector3 p7 = new Vector3(max.x, min.y, max.z);
        Vector3 p8 = new Vector3(max.x, max.y, max.z);

        //X
        DrawPointInWorldSpace(t, ref pos, ref rot, p1, color); //1
        DrawPointInWorldSpace(t, ref pos, ref rot, p5, color);
        DrawPointInWorldSpace(t, ref pos, ref rot, p2, color); //2
        DrawPointInWorldSpace(t, ref pos, ref rot, p6, color);
        DrawPointInWorldSpace(t, ref pos, ref rot, p3, color); //3
        DrawPointInWorldSpace(t, ref pos, ref rot, p7, color);
        DrawPointInWorldSpace(t, ref pos, ref rot, p4, color); //4
        DrawPointInWorldSpace(t, ref pos, ref rot, p8, color);

        //Y
        DrawPointInWorldSpace(t, ref pos, ref rot, p1, color); //1
        DrawPointInWorldSpace(t, ref pos, ref rot, p2, color);
        DrawPointInWorldSpace(t, ref pos, ref rot, p3, color); //3
        DrawPointInWorldSpace(t, ref pos, ref rot, p4, color);
        DrawPointInWorldSpace(t, ref pos, ref rot, p5, color); //5
        DrawPointInWorldSpace(t, ref pos, ref rot, p6, color);
        DrawPointInWorldSpace(t, ref pos, ref rot, p7, color); //7
        DrawPointInWorldSpace(t, ref pos, ref rot, p8, color);

        //Z
        DrawPointInWorldSpace(t, ref pos, ref rot, p1, color); //1
        DrawPointInWorldSpace(t, ref pos, ref rot, p3, color);
        DrawPointInWorldSpace(t, ref pos, ref rot, p2, color); //2
        DrawPointInWorldSpace(t, ref pos, ref rot, p4, color);
        DrawPointInWorldSpace(t, ref pos, ref rot, p5, color); //5
        DrawPointInWorldSpace(t, ref pos, ref rot, p7, color);
        DrawPointInWorldSpace(t, ref pos, ref rot, p6, color); //6
        DrawPointInWorldSpace(t, ref pos, ref rot, p8, color);
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
                if (colors[index] == Color.black)
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