using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MsgBase {
    ProtocolBytes protocolBytes;
    public string protoName = "null";
    public MsgBase() { }
    public virtual ProtocolBytes Encode() {
        protocolBytes = new ProtocolBytes();
        protocolBytes.AddString(protoName);
        protocolBytes.AddString(JsonUtility.ToJson(this));
        return protocolBytes;
    }
    public virtual void Decode(ProtocolBytes protocol) {
        int start = 0;
        protoName = protocol.GetString(start, ref start);
        string json = protocol.GetString(start, ref start);
       // return (MsgBase)JsonUtility.FromJson(json,Type.GetType(protoName));
        JsonUtility.FromJsonOverwrite(json,this);

    }
    public virtual string GetName() {
        return protoName;
    }

}