//#define ENABLE_VERBOSE_LOG

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class PodFileFormat : EditorWindow
{
    public static string VERSION = "1.0";

    /// <summary>
    /// Open an instance of the panel
    /// </summary>
    /// <returns></returns>
    public static PodFileFormat GetPanel()
    {
        PodFileFormat window = GetWindow<PodFileFormat>();
        window.position = new Rect(300, 300, 500, 500);
        window.minSize = new Vector2(200, 20);
        return window;
    }

    /// <summary>
    /// Get Toolbox Window
    /// </summary>
    [MenuItem("Window/Open POD Viewer")]
    private static void MenuOpenPanel()
    {
        GetPanel();
    }

    private string _mPath = string.Empty;
    private GameObject _mPreviewObject = null;
    private Mesh _mMesh = null;
    private int _mFileSize = 0;
    private string _mPodVersion = string.Empty;
    private int _mVertexCount = 0;
    private PodDataTypes _mPodDataType = 0;
    private int _mFaceCount = 0;
    private static List<KeyValuePair<Vector3, Color32>> _sLines = new List<KeyValuePair<Vector3, Color32>>();
    private int _mParseIndexStride = 0;
    private int _mParseIndexNodeName = 0;
    private List<BlockTypes> _mIdentifiers = new List<BlockTypes>();

    class MeshNode
    {
        public string _mName = string.Empty;
        public int _mStride = 0;
        public int _mVertexPosition = 0;
        public int _mFacePosition = 0;
    }

    private List<MeshNode> _mMeshNodes = new List<MeshNode>();

    private PodDataIdentifiers _mPodDataIdentifier = PodDataIdentifiers.NONE;

    void OnGUI()
    {
        if (EditorApplication.isCompiling)
        {
            GUILayout.Label("Compiling...");
            return;
        }

        GUILayout.Label("Lines: " + _sLines.Count);

        GameObject go = (GameObject)EditorGUILayout.ObjectField("Preview Object", _mPreviewObject, typeof(GameObject), true);
        if (go != _mPreviewObject)
        {
            _mPreviewObject = go;
            EditorPrefs.SetInt("PreviewObject", _mPreviewObject.GetInstanceID());
        }

        if (!string.IsNullOrEmpty(_mPath))
        {
            if (GUILayout.Button("Reload"))
            {
                Preview();
                EditorGUIUtility.ExitGUI();
                return;
            }
        }

        if (Selection.activeObject)
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!string.IsNullOrEmpty(path) &&
                path.ToLower().EndsWith(".pod"))
            {
                if ((_mPath != path))
                {
                    _mPath = path;
                    Preview();
                }
            }
        }

        if (!string.IsNullOrEmpty(_mPath))
        {
            GUILayout.Label(_mPath);
            GUILayout.Label("File Size: " + _mFileSize);
        }

        foreach (MeshNode item in _mMeshNodes)
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label("Name:");
            GUILayout.Label(item._mName);

            GUILayout.Label("Stride:");
            GUILayout.Label(item._mStride.ToString());
            
            GUILayout.EndHorizontal();
        }
    }

    private DateTime _mLastTime = DateTime.Now;

    void Update()
    {
        float elapsed;
        if (EditorApplication.isPlaying)
        {
            elapsed = Time.deltaTime;
        }
        else
        {
            DateTime currentTime = DateTime.Now;
            elapsed = (float) (currentTime - _mLastTime).TotalSeconds;
            _mLastTime = currentTime;
        }
        for (int index = 0; index < _sLines.Count; index += 2)
        {
            Debug.DrawLine(_sLines[index].Key, _sLines[index + 1].Key, _sLines[index].Value, elapsed, false);
        }

        Repaint();
    }

    string BinaryToString(byte data)
    {
        string msg = string.Empty;
        for (int i = 7; i >= 0; --i)
        {
            byte bit = (byte)(1 << i);
            if ((data & bit) == bit)
            {
                msg += "1";
            }
            else
            {
                msg += "0";
            }
        }
        return msg;
    }

    void ReadBlock(byte[] block, out int identifier, out int length, out bool blockEnd, bool log)
    {
        if (log)
        {
            for (int i = 0; i < block.Length; ++i)
            {
                Debug.Log("byte " + i + " =" + block[i] + " Binary=" + BinaryToString(block[i]));
            }
        }

        identifier = block[0] | (block[1] << 8);
        _mIdentifiers.Add((BlockTypes)identifier);
        if (log)
        {
            Debug.Log("Identifier=" + (BlockTypes)identifier);
        }

        if ((block[3] & 1) == 1)
        {
            blockEnd = false;
            //Debug.Log("Beginning of block");
        }
        else
        {
            blockEnd = true;
            //Debug.Log("End of block");
        }

        length = block[4] | (block[5] << 8) | (block[6] << 16) | (block[7] << 24);
        if (log)
        {
            Debug.Log("Length=" + length);
        }
    }

    void ReadVersion(ref int position, byte[] buffer)
    {
        for (int i = position; i < buffer.Length; ++i)
        {
            if (buffer[i] == 0)
            {
                byte[] version = new byte[i - position];
                Array.Copy(buffer, position, version, 0, version.Length);
                _mPodVersion = System.Text.ASCIIEncoding.ASCII.GetString(version);
                Debug.Log("Version=" + _mPodVersion);
                break;
            }
        }
    }

    void Preview()
    {
        _mMeshNodes.Clear();
        _mParseIndexStride = 0;
        _mParseIndexNodeName = 0;
        _mIdentifiers.Clear();

        if (_mPreviewObject)
        {
            MeshFilter mf = _mPreviewObject.GetComponent<MeshFilter>();
            if (mf)
            {
                _mMesh = mf.sharedMesh;
                if (_mMesh)
                {
                    _sLines.Clear();
                    _mMesh.triangles = new int[0];
                    _mMesh.normals = new Vector3[0];
                    _mMesh.vertices = new Vector3[0];
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }
        else
        {
            return;
        }

        if (!File.Exists(_mPath))
        {
            return;
        }

        byte[] buffer;
        using (FileStream fs = File.Open(_mPath, FileMode.Open, FileAccess.ReadWrite))
        {
            using (BinaryReader br = new BinaryReader(fs))
            {
                _mFileSize = (int)fs.Length;
                buffer = br.ReadBytes(_mFileSize);
            }
        }

        int position = 0;
        byte[] block = new byte[8];

        ParseNextChunk(buffer, ref position, block);
    }

    enum PodDataTypes
    {
        None = 0,
        SIGNED_FLOAT_32 = 1,
        UNSIGNED_INTEGER_32 = 2,
        UNSIGNED_SHORT = 3,
        RGBA_32 = 4,
        ARGB_32 = 5,
        D3DCOLOR_32 = 6,
        UBYTE_32 = 7,
        DEC3N_32 = 8,
        FIXED_POINT_32 = 9,
        UNSIGNED_BYTE = 10,
        SHORT = 11,
        NORMALIZED_SHORT = 12,
        BYTE = 13,
        NORMALIZED_BYTE = 14,
        UNSIGNED_NORMALIZED_BYTE = 15,
        UNSIGNED_NORMALIZED_SHORT = 16,
        UNSIGNED_INTEGER = 17,
    }

    enum BlockTypes
    {
        BLOCK_IDENTIFIER_VERSION = 1000,
        BLOCK_IDENTIFIER_SCENE = 1001,
        BLOCK_IDENTIFIER_EXPORT_OPTIONS = 1002,
        BLOCK_IDENTIFIER_HISTORY = 1003,
        BLOCK_IDENTIFIER_SCENE_CLEAR_COLOUR = 2000,
        BLOCK_IDENTIFIER_SCENE_AMBIENT_COLOUR = 2001,
        BLOCK_IDENTIFIER_SCENE_NUM_CAMERAS = 2002,
        BLOCK_IDENTIFIER_SCENE_NUM_LIGHTS = 2003,
        BLOCK_IDENTIFIER_SCENE_NUM_MESHES = 2004,
        BLOCK_IDENTIFIER_SCENE_NUM_NODES = 2005,
        BLOCK_IDENTIFIER_SCENE_NUM_MESH_NODES = 2006,
        BLOCK_IDENTIFIER_SCENE_NUM_TEXTURES = 2007,
        BLOCK_IDENTIFIER_SCENE_NUM_MATERIALS = 2008,
        BLOCK_IDENTIFIER_SCENE_NUM_FRAMES = 2009,
        BLOCK_IDENTIFIER_SCENE_CAMERA = 2010,
        BLOCK_IDENTIFIER_SCENE_LIGHT = 2011,
        BLOCK_IDENTIFIER_SCENE_MESH = 2012,
        BLOCK_IDENTIFIER_SCENE_NODE = 2013,
        BLOCK_IDENTIFIER_SCENE_TEXTURE = 2014,
        BLOCK_IDENTIFIER_SCENE_MATERIAL = 2015,
        BLOCK_IDENTIFIER_SCENE_FLAGS = 2016,
        BLOCK_IDENTIFIER_SCENE_FPS = 2017,
        BLOCK_IDENTIFIER_SCENE_USER_DATA = 2018,
        BLOCK_IDENTIFIER_SCENE_UNITS_IDENTIFIER = 2019,
        BLOCK_IDENTIFIER_MATERIAL_NAME = 3000,
        BLOCK_IDENTIFIER_MATERIAL_DIFFUSE_TEXTURE_INDEX = 3001,
        BLOCK_IDENTIFIER_MATERIAL_OPACITY = 3002,
        BLOCK_IDENTIFIER_MATERIAL_AMBIENT_COLOR = 3003,
        BLOCK_IDENTIFIER_MATERIAL_DIFFUSE_COLOR = 3004,
        BLOCK_IDENTIFIER_MATERIAL_SPECULAR_COLOR = 3005,
        BLOCK_IDENTIFIER_MATERIAL_SHININESS = 3006,
        BLOCK_IDENTIFIER_MATERIAL_EFFECT_FILE_NAME = 3007,
        BLOCK_IDENTIFIER_MATERIAL_EFFECT_NAME = 3008,
        BLOCK_IDENTIFIER_MATERIAL_AMBIENT_TEXTURE_INDEX = 3009,
        BLOCK_IDENTIFIER_MATERIAL_SPECULAR_COLOUR_TEXTURE_INDEX = 3010,
        BLOCK_IDENTIFIER_MATERIAL_SPECULAR_LEVEL_TEXTURE_INDEX = 3011,
        BLOCK_IDENTIFIER_MATERIAL_BUMP_MAP_TEXTURE_INDEX = 3012,
        BLOCK_IDENTIFIER_MATERIAL_EMISSIVE_TEXTURE_INDEX = 3013,
        BLOCK_IDENTIFIER_MATERIAL_GLOSSINESS_TEXTURE_INDEX = 3014,
        BLOCK_IDENTIFIER_MATERIAL_OPACITY_TEXTURE_INDEX = 3015,
        BLOCK_IDENTIFIER_MATERIAL_REFLECTION_TEXTURE_INDEX = 3016,
        BLOCK_IDENTIFIER_MATERIAL_REFRACTION_TEXTURE_INDEX = 3017,
        BLOCK_IDENTIFIER_MATERIAL_BLENDING_RGB_SOURCE_VALUE = 3018,
        BLOCK_IDENTIFIER_MATERIAL_BLENDING_ALPHA_SOURCE_VALUE = 3019,
        BLOCK_IDENTIFIER_MATERIAL_BLENDING_RGB_DESTINATION_VALUE = 3020,
        BLOCK_IDENTIFIER_MATERIAL_BLENDING_ALPHA_DESTINATION_VALUE = 3021,
        BLOCK_IDENTIFIER_MATERIAL_BLENDING_RGB_OPERATION = 3022,
        BLOCK_IDENTIFIER_MATERIAL_BLENDING_ALPHA_OPERATION = 3023,
        BLOCK_IDENTIFIER_MATERIAL_BLENDING_RGBA_COLOUR = 3024,
        BLOCK_IDENTIFIER_MATERIAL_BLENDING_FACTOR_ARRAY = 3025,
        BLOCK_IDENTIFIER_MATERIAL_FLAGS = 3026,
        BLOCK_IDENTIFIER_TEXTURE_NAME = 4000,
        BLOCK_IDENTIFIER_NODE_INDEX = 5000,
        BLOCK_IDENTIFIER_NODE_NAME = 5001,
        BLOCK_IDENTIFIER_NODE_MATERIAL_INDEX = 5002,
        BLOCK_IDENTIFIER_NODE_PARENT_INDEX = 5003,
        BLOCK_IDENTIFIER_NODE_ANIMATION_POSITION = 5007,
        BLOCK_IDENTIFIER_NODE_ANIMATION_ROTATION = 5008,
        BLOCK_IDENTIFIER_NODE_ANIMATION_SCALE = 5009,
        BLOCK_IDENTIFIER_NODE_ANIMATION_MATRIX = 5010,
        BLOCK_IDENTIFIER_NODE_ANIMATION_UNKNOWN = 5011, //?
        BLOCK_IDENTIFIER_NODE_ANIMATION_FLAGS = 5012,
        BLOCK_IDENTIFIER_NODE_ANIMATION_POSITION_INDEX = 5013,
        BLOCK_IDENTIFIER_NODE_ANIMATION_ROTATION_INDEX = 5014,
        BLOCK_IDENTIFIER_NODE_ANIMATION_SCALE_INDEX = 5015,
        BLOCK_IDENTIFIER_NODE_ANIMATION_MATRIX_INDEX = 5016,
        BLOCK_IDENTIFIER_NODE_USER_DATA = 5017,
        BLOCK_IDENTIFIER_MESH_NUM_VERTICES = 6000,
        BLOCK_IDENTIFIER_MESH_NUM_FACES = 6001,
        BLOCK_IDENTIFIER_MESH_NUM_UVW_CHANNELS = 6002,
        BLOCK_IDENTIFIER_MESH_VERTEX_INDEX_LIST = 6003,
        BLOCK_IDENTIFIER_MESH_STRIP_LENGTH = 6004,
        BLOCK_IDENTIFIER_MESH_NUM_STRIPS = 6005,
        BLOCK_IDENTIFIER_MESH_VERTEX_LIST = 6006,
        BLOCK_IDENTIFIER_MESH_NORMAL_LIST = 6007,
        BLOCK_IDENTIFIER_MESH_TANGENT_LIST = 6008,
        BLOCK_IDENTIFIER_MESH_BINORMAL_LIST = 6009,
        BLOCK_IDENTIFIER_MESH_UVW_LIST = 6010,
        BLOCK_IDENTIFIER_MESH_VERTEX_COLOUR_LIST = 6011,
        BLOCK_IDENTIFIER_MESH_BONE_INDEX_LIST = 6012,
        BLOCK_IDENTIFIER_MESH_BONE_WEIGHTS = 6013,
        BLOCK_IDENTIFIER_MESH_INTERLEAVED_DATA_LIST = 6014,
        BLOCK_IDENTIFIER_MESH_BONE_BATCH_INDEX_LIST = 6015,
        BLOCK_IDENTIFIER_MESH_NUM_BONE_INDICES_PER_BATCH = 6016,
        BLOCK_IDENTIFIER_MESH_NUM_BONE_OFFSET_PER_BATCH = 6017,
        BLOCK_IDENTIFIER_MESH_MAX_NUM_BONES_PER_BATCH = 6018,
        BLOCK_IDENTIFIER_MESH_NUM_BONE_BATCHES = 6019,
        BLOCK_IDENTIFIER_MESH_UNPACK_MATRIX = 6020,
        BLOCK_IDENTIFIER_LIGHT_TARGET_OBJECT_INDEX = 7000,
        BLOCK_IDENTIFIER_LIGHT_COLOUR = 7001,
        BLOCK_IDENTIFIER_LIGHT_TYPE = 7002,
        BLOCK_IDENTIFIER_LIGHT_CONSTANT_ATTENUATION = 7003,
        BLOCK_IDENTIFIER_LIGHT_LINEAR_ATTENUATION = 7004,
        BLOCK_IDENTIFIER_LIGHT_QUADRATIC_ATTENUATION = 7005,
        BLOCK_IDENTIFIER_LIGHT_FALLOFF_ANGLE = 7006,
        BLOCK_IDENTIFIER_LIGHT_FALLOFF_EXPONENT = 7007,
        BLOCK_IDENTIFIER_CAMERA_TARGET_OBJECT_INDEX = 8000,
        BLOCK_IDENTIFIER_CAMERA_FIELD_OF_VIEW = 8001,
        BLOCK_IDENTIFIER_CAMERA_FAR_PLANE = 8002,
        BLOCK_IDENTIFIER_CAMERA_NEAR_PLANE = 8003,
        BLOCK_IDENTIFIER_CAMERA_FOV_ANIMATION = 8004,
        BLOCK_IDENTIFIER_POD_DATA_TYPE = 9000,
        BLOCK_IDENTIFIER_POD_NUM_COMPONENTS = 9001,
        BLOCK_IDENTIFIER_POD_STRIDES = 9002,
        BLOCK_IDENTIFIER_POD_DATA = 9003,
    }

    private enum PodDataIdentifiers
    {
        NONE,
        BLOCK_IDENTIFIER_MESH_BINORMAL_LIST = BlockTypes.BLOCK_IDENTIFIER_MESH_BINORMAL_LIST,
        BLOCK_IDENTIFIER_MESH_BONE_INDEX_LIST = BlockTypes.BLOCK_IDENTIFIER_MESH_BONE_INDEX_LIST,
        BLOCK_IDENTIFIER_MESH_BONE_WEIGHTS = BlockTypes.BLOCK_IDENTIFIER_MESH_BONE_WEIGHTS,
        BLOCK_IDENTIFIER_MESH_NORMAL_LIST = BlockTypes.BLOCK_IDENTIFIER_MESH_NORMAL_LIST,
        BLOCK_IDENTIFIER_MESH_TANGENT_LIST = BlockTypes.BLOCK_IDENTIFIER_MESH_TANGENT_LIST,
        BLOCK_IDENTIFIER_MESH_VERTEX_COLOUR_LIST = BlockTypes.BLOCK_IDENTIFIER_MESH_VERTEX_COLOUR_LIST,
        BLOCK_IDENTIFIER_MESH_VERTEX_INDEX_LIST = BlockTypes.BLOCK_IDENTIFIER_MESH_VERTEX_INDEX_LIST,
        BLOCK_IDENTIFIER_MESH_VERTEX_LIST = BlockTypes.BLOCK_IDENTIFIER_MESH_VERTEX_LIST,
    }

    MeshNode GetMeshNode(int item)
    {
        for (int index = _mMeshNodes.Count; index <= item; ++index)
        {
            MeshNode meshNode = new MeshNode();
            _mMeshNodes.Add(meshNode);
        }

        return _mMeshNodes[item];
    }

    void ParseNextChunk(byte[] buffer, ref int position, byte[] block)
    {
        int identifier;
        int length;
        bool blockEnd;

        if ((position + 8) >= buffer.Length)
        {
            return;
        }

        Array.Copy(buffer, position, block, 0, 8);
        position += 8;
        ReadBlock(block, out identifier, out length, out blockEnd, false);

        //Debug.Log("Identifier=" + (BlockTypes) identifier + " position=" + position + " length=" + length);

        foreach (PodDataIdentifiers podIdentifier in Enum.GetValues(typeof (PodDataIdentifiers)))
        {
            if (podIdentifier == (PodDataIdentifiers)(int)identifier)
            {
                _mPodDataIdentifier = (PodDataIdentifiers)(int)identifier;
                break;
            }
        }

        switch ((BlockTypes) identifier)
        {
            case BlockTypes.BLOCK_IDENTIFIER_VERSION:
                if (length != 0)
                {
                    ReadVersion(ref position, buffer);
                }
                break;

            case BlockTypes.BLOCK_IDENTIFIER_POD_STRIDES:
                if (length != 0)
                {
                    int stride =
                            buffer[position] | (buffer[position + 1] << 8) | (buffer[position + 2] << 16) |
                            (buffer[position + 3] << 24);
                    Debug.Log("BLOCK_IDENTIFIER_POD_STRIDES: data=" + stride + " position=" + position +
                              " length=" + length);

                    if (_mPodDataIdentifier == PodDataIdentifiers.BLOCK_IDENTIFIER_MESH_VERTEX_LIST)
                    {
                        MeshNode item = GetMeshNode(_mParseIndexStride);

                        item._mStride = stride;
                        ++_mParseIndexStride;

                        /*
                        Debug.Log("Previous Identifier-0=" + _mIdentifiers[_mIdentifiers.Count - 1]);
                        Debug.Log("Previous Identifier-1=" + _mIdentifiers[_mIdentifiers.Count - 2]);
                        Debug.Log("Previous Identifier-2=" + _mIdentifiers[_mIdentifiers.Count - 3]);
                        Debug.Log("Previous Identifier-3=" + _mIdentifiers[_mIdentifiers.Count - 4]);
                        Debug.Log("Previous Identifier-4=" + _mIdentifiers[_mIdentifiers.Count - 5]);
                        Debug.Log("Previous Identifier-5=" + _mIdentifiers[_mIdentifiers.Count - 6]);
                        Debug.Log("Previous Identifier-6=" + _mIdentifiers[_mIdentifiers.Count - 7]);
                        */
                    }
                }
                break;

            case BlockTypes.BLOCK_IDENTIFIER_SCENE_NUM_MESHES:
                if (length != 0)
                {
                    int data =
                            buffer[position] | (buffer[position + 1] << 8) | (buffer[position + 2] << 16) |
                             (buffer[position + 3] << 24);
#if ENABLE_VERBOSE_LOG
                    Debug.Log("BLOCK_IDENTIFIER_SCENE_NUM_MESHES: data=" + data + " position=" + position + " length=" + length);
#endif
                }
                break;

            case BlockTypes.BLOCK_IDENTIFIER_SCENE_NUM_MESH_NODES:
                if (length != 0)
                {
                    int data =
                            buffer[position] | (buffer[position + 1] << 8) | (buffer[position + 2] << 16) |
                             (buffer[position + 3] << 24);
#if ENABLE_VERBOSE_LOG
                    Debug.Log("BLOCK_IDENTIFIER_SCENE_NUM_MESH_NODES: data=" + data + " position=" + position + " length=" + length);
#endif
                }
                break;

            case BlockTypes.BLOCK_IDENTIFIER_MESH_NUM_VERTICES:
                if (length != 0)
                {
                    _mVertexCount =
                            buffer[position] | (buffer[position + 1] << 8) | (buffer[position + 2] << 16) |
                             (buffer[position + 3] << 24);
#if ENABLE_VERBOSE_LOG
                    Debug.Log("BLOCK_IDENTIFIER_MESH_NUM_VERTICES: " + _mVertexCount + " position=" + position + " length=" + length);
#endif
                }
                break;

            case BlockTypes.BLOCK_IDENTIFIER_MESH_VERTEX_LIST:

                if (length != 0)
                {
                    PodDataTypes podDataType =
                        (PodDataTypes)
                            (buffer[position] | (buffer[position + 1] << 8) | (buffer[position + 2] << 16) |
                             (buffer[position + 3] << 24));
#if ENABLE_VERBOSE_LOG
                    Debug.Log("BLOCK_IDENTIFIER_MESH_VERTEX_LIST: " + podDataType + " position=" + position + " length=" + length);
#endif
                    /*
                    Debug.Log("Vertex Data Type: " + _mVertexDataType);
                    for (int i = position; i < (position+4); ++i)
                    {
                        Debug.Log("byte " + i + ": " + buffer[i] + " Binary=" + BinaryToString(buffer[i]));
                    }
                    */
                }
                break;

            case BlockTypes.BLOCK_IDENTIFIER_POD_DATA_TYPE:
                if (length != 0)
                {
                    _mPodDataType =
                        (PodDataTypes)
                            (buffer[position] | (buffer[position + 1] << 8) | (buffer[position + 2] << 16) |
                             (buffer[position + 3] << 24));
#if ENABLE_VERBOSE_LOG
                    Debug.Log("BLOCK_IDENTIFIER_POD_DATA_TYPE: " + _mPodDataType + " position=" + position + " length=" + length);
#endif
                }
                break;
            case BlockTypes.BLOCK_IDENTIFIER_POD_NUM_COMPONENTS:
                if (length != 0)
                {
                    int data =
                        (buffer[position] | (buffer[position + 1] << 8) | (buffer[position + 2] << 16) |
                         (buffer[position + 3] << 24));
#if ENABLE_VERBOSE_LOG
                    Debug.Log("BLOCK_IDENTIFIER_POD_NUM_COMPONENTS: " + data + " position=" + position + " length=" + length);
#endif
                }
                break;
            case BlockTypes.BLOCK_IDENTIFIER_POD_DATA:
                if (length != 0)
                {
                    switch (_mPodDataType)
                    {
                        case PodDataTypes.UNSIGNED_SHORT:
#if ENABLE_VERBOSE_LOG
                            Debug.Log("BLOCK_IDENTIFIER_POD_DATA: " + _mVertexCount + " dataType=" + _mPodDataType + " position=" + position + " length=" + length);
#endif
                            if ((position + _mFaceCount*6) < buffer.Length)
                            {
                                int temp = position;
                                int[] triangles = new int[_mFaceCount*3];
                                byte[] face = new byte[2];
                                for (int i = 0; i < _mFaceCount; ++i)
                                {
                                    Array.Copy(buffer, temp, face, 0, 2);
                                    ushort f1 = BitConverter.ToUInt16(face, 0);
                                    temp += 2;

                                    Array.Copy(buffer, temp, face, 0, 2);
                                    ushort f2 = BitConverter.ToUInt16(face, 0);
                                    temp += 2;

                                    Array.Copy(buffer, temp, face, 0, 2);
                                    ushort f3 = BitConverter.ToUInt16(face, 0);
                                    temp += 2;

                                    triangles[i*3] = f1;
                                    triangles[i*3+1] = f2;
                                    triangles[i*3+2] = f3;
                                }
                                _mMesh.triangles = triangles;
                                Debug.Log("Assigned Faces: " + _mFaceCount);
                            }
                            else
                            {
                                Debug.LogError("Unexpected faces size!");
                            }
                            break;
                        case PodDataTypes.SIGNED_FLOAT_32:
                            int data = (buffer[position] | (buffer[position + 1] << 8) | (buffer[position + 2] << 16) |
                             (buffer[position + 3] << 24));
#if ENABLE_VERBOSE_LOG
                            Debug.Log("BLOCK_IDENTIFIER_POD_DATA: " + _mVertexCount + " dataType=" + _mPodDataType +
                                      " data" + data + " position=" + position + " length=" + length);
#endif
                            /*
                            if ((position + _mVertexCount * 12) < buffer.Length)
                            {
                                int temp = position;
                                Vector3[] verts = new Vector3[_mVertexCount];
                                byte[] data = new byte[4];
                                for (int i = 0; i < _mVertexCount; ++i)
                                {
                                    Array.Copy(buffer, temp, data, 0, 4);
                                    float x = System.BitConverter.ToSingle(data, 0);

                                    Array.Copy(buffer, temp+4, data, 0, 4);
                                    float y = System.BitConverter.ToSingle(data, 0);

                                    Array.Copy(buffer, temp+8, data, 0, 4);
                                    float z = System.BitConverter.ToSingle(data, 0);

                                    temp += 12;
                                    verts[i] = new Vector3(x, y, z);
                                }
                                Debug.LogError("Assigned verts: " + verts.Length);
                                _mMesh.vertices = verts;
                            }
                            else
                            {
                                Debug.LogError("Unexpected verts size! " + (position + _mVertexCount*12) + " at " +
                                               position);
                            }
                            */
                            break;
                    }
                }
                break;

            case BlockTypes.BLOCK_IDENTIFIER_MESH_INTERLEAVED_DATA_LIST:

                Debug.Log("BLOCK_IDENTIFIER_MESH_INTERLEAVED_DATA_LIST: " + _mVertexCount + " dataType=" + _mPodDataType +
                                      " position=" + position + " length=" + length);
                if ((position + length) < buffer.Length)
                {
                    byte[] data = new byte[4];

                    bool inRange = true;
                    Vector3[] verts = new Vector3[_mVertexCount];
                    Vector3[] normals = new Vector3[_mVertexCount];
                    int temp = position;
                    for (int i = 0; i < _mVertexCount; ++i)
                    {
                        #region Verts

                        float x = 0;
                        if ((temp+4) < buffer.Length)
                        {
                            Array.Copy(buffer, temp, data, 0, 4);
                            x = BitConverter.ToSingle(data, 0);
                            temp += 4;
                        }
                        else
                        {
                            Debug.Log("Temp is out of X range temp="+temp+" length="+buffer.Length);
                            inRange = false;
                            break;
                        }

                        float y = 0;
                        if ((temp + 4) < buffer.Length)
                        {
                            Array.Copy(buffer, temp, data, 0, 4);
                            y = BitConverter.ToSingle(data, 0);
                            temp += 4;
                        }
                        else
                        {
                            Debug.Log("Temp is out of Y range temp=" + temp + " length=" + buffer.Length);
                            inRange = false;
                            break;
                        }

                        float z = 0;
                        if ((temp + 4) < buffer.Length)
                        {
                            Array.Copy(buffer, temp, data, 0, 4);
                            z = BitConverter.ToSingle(data, 0);
                            temp += 4;
                        }
                        else
                        {
                            Debug.Log("Temp is out of Z range temp=" + temp + " length=" + buffer.Length);
                            inRange = false;
                            break;
                        }

                        verts[i] = new Vector3(-x, y, -z);

                        #endregion

                        //_sLines.Add(new KeyValuePair<Vector3, Color32>(verts[i], Color.cyan));
                        //_sLines.Add(new KeyValuePair<Vector3, Color32>(verts[i] + Vector3.up*.1f, Color.cyan));

                        #region Normals

                        if ((temp + 4) < buffer.Length)
                        {
                            Array.Copy(buffer, temp, data, 0, 4);
                            x = BitConverter.ToSingle(data, 0);
                            temp += 4;
                        }
                        else
                        {
                            Debug.Log("Temp is out of X range temp=" + temp + " length=" + buffer.Length);
                            inRange = false;
                            break;
                        }

                        if ((temp + 4) < buffer.Length)
                        {
                            Array.Copy(buffer, temp, data, 0, 4);
                            y = BitConverter.ToSingle(data, 0);
                            temp += 4;
                        }
                        else
                        {
                            Debug.Log("Temp is out of Y range temp=" + temp + " length=" + buffer.Length);
                            inRange = false;
                            break;
                        }

                        if ((temp + 4) < buffer.Length)
                        {
                            Array.Copy(buffer, temp, data, 0, 4);
                            z = BitConverter.ToSingle(data, 0);
                            temp += 4;
                        }
                        else
                        {
                            Debug.Log("Temp is out of Z range temp=" + temp + " length=" + buffer.Length);
                            inRange = false;
                            break;
                        }

                        normals[i] = new Vector3(-x, y, -z);

                        #endregion

                        //temp += 12;
                    }

                    if (inRange)
                    {
                        Debug.Log("Assigned verts: " + verts.Length + " inRange=" + inRange);
                        _mMesh.vertices = verts;
                        _mMesh.normals = normals;
                        _mMesh.RecalculateBounds();
                    }
                }
                else
                {
                    Debug.LogError("Unexpected verts size! " + (position + _mVertexCount*12) + " at " +
                                    position);
                }

                break;

            case BlockTypes.BLOCK_IDENTIFIER_MESH_NUM_FACES:
                if (length != 0)
                {
                    _mFaceCount =
                            buffer[position] | (buffer[position + 1] << 8) | (buffer[position + 2] << 16) |
                             (buffer[position + 3] << 24);
#if ENABLE_VERBOSE_LOG
                    Debug.Log("BLOCK_IDENTIFIER_MESH_NUM_FACES: " + _mFaceCount + " position=" + position + " length=" + length);
#endif
                }
                break;
            case BlockTypes.BLOCK_IDENTIFIER_MESH_VERTEX_INDEX_LIST:
                if (length != 0)
                {
                    PodDataTypes podDataTypes =
                        (PodDataTypes)
                            (buffer[position] | (buffer[position + 1] << 8) | (buffer[position + 2] << 16) |
                             (buffer[position + 3] << 24));
#if ENABLE_VERBOSE_LOG
                    Debug.Log("BLOCK_IDENTIFIER_MESH_VERTEX_INDEX_LIST: " + podDataTypes + " position="+position+" length=" + length);
#endif
                    /*
                    for (int i = position; i < (position+4); ++i)
                    {
                        Debug.Log("byte " + i + ": " + buffer[i] + " Binary=" + BinaryToString(buffer[i]));
                    }
                    */
                }
                break;

            case BlockTypes.BLOCK_IDENTIFIER_MATERIAL_NAME:
                if (length != 0)
                {
//#if ENABLE_VERBOSE_LOG
                    Debug.LogWarning("BLOCK_IDENTIFIER_MATERIAL_NAME: position=" + position + " length=" + length);
//#endif
                }
                break;

            case BlockTypes.BLOCK_IDENTIFIER_MATERIAL_EFFECT_FILE_NAME:
                if (length != 0)
                {
#if ENABLE_VERBOSE_LOG
                    byte[] strData = new byte[length];
                    Array.Copy(buffer, position, strData, 0, length);
                    string strName = System.Text.UTF8Encoding.UTF8.GetString(strData);
                    Debug.Log("BLOCK_IDENTIFIER_MATERIAL_EFFECT_FILE_NAME: name=" + strName + "position=" + position + " length=" + length);
                    
#endif
                }
                break;

            case BlockTypes.BLOCK_IDENTIFIER_MATERIAL_EFFECT_NAME:
                if (length != 0)
                {
                    //#if ENABLE_VERBOSE_LOG
                    Debug.LogWarning("BLOCK_IDENTIFIER_MATERIAL_EFFECT_NAME: position=" + position + " length=" + length);
                    //#endif
                }
                break;

            case BlockTypes.BLOCK_IDENTIFIER_TEXTURE_NAME:
                if (length != 0)
                {
                    //#if ENABLE_VERBOSE_LOG
                    Debug.LogWarning("BLOCK_IDENTIFIER_TEXTURE_NAME: position=" + position + " length=" + length);
                    //#endif
                }
                break;

            case BlockTypes.BLOCK_IDENTIFIER_NODE_NAME:
                if (length != 0)
                {
                    MeshNode item = GetMeshNode(_mParseIndexNodeName);

                    //#if ENABLE_VERBOSE_LOG
                    byte[] strData = new byte[length];
                    Array.Copy(buffer, position, strData, 0, length);
                    item._mName = System.Text.UTF8Encoding.UTF8.GetString(strData);
                    Debug.Log("BLOCK_IDENTIFIER_NODE_NAME: name=" + item._mName + "position=" + position + " length=" + length);
                    //#endif

                    ++_mParseIndexNodeName;
                }
                break;
        }

        bool found = false;
        foreach (BlockTypes blockType in Enum.GetValues(typeof (BlockTypes)))
        {
            if (identifier == (int) blockType)
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            Debug.LogError("Unknown identifier=" + identifier);
            return;
        }

        position += length;

        ParseNextChunk(buffer, ref position, block);
    }
}