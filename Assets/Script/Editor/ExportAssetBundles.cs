using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class ExportAssetBundles : MonoBehaviour {
    [MenuItem("Custom Editor/Buile AssetBundles")]
    static void CreateAssetBunldesMain() {
        string path = Application.dataPath;
        //path = path.Substring(0, path.Length - 6) + "abc";
        path = path + "/StreamingAssets";
        if (!Directory.Exists(path)) {
            Directory.CreateDirectory(path);
        }
        BuildPipeline.BuildAssetBundles(path, BuildAssetBundleOptions.UncompressedAssetBundle, BuildTarget.StandaloneWindows);
        print("AB包建立成功 path： " + path);
    }
    // Start is called before the first frame update

}
