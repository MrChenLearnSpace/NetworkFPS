using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GameManager :MonoBehaviour  {

    static GameManager instance;
    public static GameManager Instance {
        get => instance;
    }
    public string id;



    public  void Awake() {
        if (instance != null)
            Destroy(gameObject);
        else
            instance =  this;
    }
    public static bool IsInitialized {
        get => instance != null;
    }
    public void OnDestroy() {
        if (instance == this)
            instance = null;
    }


    private void Start() {
        //网络监听
        NetManager.AddEventListener(NetManager.NetEvent.Close, OnConnectClose);
        NetManager.AddMsgListener("MsgKick", OnMsgKick);
        //初始化
        ResManager.Init();//资源总管理在面板管理之前
        PanelManager.Init();

        //打开登陆面板
        PanelManager.Open<LoginPanel>();
        DontDestroyOnLoad(this);
        DontDestroyOnLoad(PanelManager.root);

        //WeaponData weapond = weapon.dataArray[0];
        //playerData.weaponDatas.Add(weapond);

    }
    
    //关闭连接
    void OnConnectClose(string err) {
        Debug.Log("断开连接");
    }
    void OnMsgKick(ProtocolBase msgBase) {
        PanelManager.Open<TipPanel>("被踢下线");
    }
    // Start is called before the fir

}

