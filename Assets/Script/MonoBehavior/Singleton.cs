using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Singleton<T> {
     static T instance;
    public static T Instance {
        get => instance;
    }
    protected virtual void Awake() {
        if (instance != null)
            Destroy(gameObject);
        else
            instance = (T)this;
        //print("111");
    }
    public static bool IsInitialized {
        get => instance != null;
    }
    protected virtual void OnDestroy() {
        if (instance == this)
            instance = null;
    }
    // Start is called before the first frame update

}
