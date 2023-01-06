using UnityEngine;
using System;
using System.Collections.Generic;
public class ResManager : MonoBehaviour {
	public static string FileName = "StreamingAssets";

	public static string AssentPath = Application.dataPath+"/"+ FileName;
	public static Dictionary<string,AssetBundle> AB= new Dictionary<string,AssetBundle>();
	public static void Init() {
		//print(Application.persistentDataPath);
		//print(Application.temporaryCachePath);
		string fpath = AssentPath +"/"+ FileName;
		print(fpath);
		AssetBundle assetbundlemanifest = AssetBundle.LoadFromFile(fpath);

		AssetBundleManifest abm = assetbundlemanifest.LoadAsset<AssetBundleManifest>("assetbundlemanifest");

		AB.Add("assetbundlemanifest", assetbundlemanifest);
		string[] strs = abm.GetAllAssetBundles();
		for(int i = 0; i < strs.Length; i++) {

			//待热更资源包修改
			//print(strs);
			AssetBundle temp = AssetBundle.LoadFromFile(AssentPath + "/" + strs[i]);
			AB.Add(strs[i], temp);
		}
	}

    //加载预设
    public static GameObject LoadPrefab(string path){
		return Resources.Load<GameObject>(path);
	}
	public static GameObject ABLoadUIPrefab(string path) {
		return AB["ui_panel"].LoadAsset<GameObject>(path);
	}
	public static GameObject ABLoadPrefab(string package, string path) {
		return AB[package].LoadAsset<GameObject>(path);
	}
}
