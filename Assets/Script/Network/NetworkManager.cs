using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System;
using System.Linq;
public static class NetManager {
	//定义套接字
	static Socket socket;
	//接收缓冲区
	static ByteArray readBuff;
	//写入队列
	static Queue<ByteArray> writeQueue;
	//是否正在连接
	static bool isConnecting = false;
	//是否正在关闭
	static bool isClosing = false;
	//消息列表
	static List<ProtocolBase> msgList = new List<ProtocolBase>();
	//消息列表长度
	static int msgCount = 0;
	//每一次Update处理的消息量
	readonly static int MAX_MESSAGE_FIRE = 10;
	//是否启用心跳
	public static bool isUsePing = true;
	//心跳间隔时间
	public static int pingInterval = 30;
	//上一次发送PING的时间
	static float lastPingTime = 0;
	//上一次收到PONG的时间
	static float lastPongTime = 0;
	static Int32 msgLength = 0;
	static byte[] lenBytes = new byte[sizeof(Int32)];
	public static ProtocolBase proto;
	//事件
	public enum NetEvent {
		ConnectSucc = 1,
		ConnectFail = 2,
		Close = 3,
	}
	//事件委托类型
	public delegate void EventListener(String err);
	//事件监听列表
	private static Dictionary<NetEvent, EventListener> eventListeners = new Dictionary<NetEvent, EventListener>();
	//添加事件监听
	public static void AddEventListener(NetEvent netEvent, EventListener listener) {
		//添加事件
		if (eventListeners.ContainsKey(netEvent)) {
			eventListeners[netEvent] += listener;
		}
		//新增事件
		else {
			eventListeners[netEvent] = listener;
		}
	}
	//删除事件监听
	public static void RemoveEventListener(NetEvent netEvent, EventListener listener) {
		if (eventListeners.ContainsKey(netEvent)) {
			eventListeners[netEvent] -= listener;
		}
	}
	//分发事件
	private static void FireEvent(NetEvent netEvent, String err) {
		if (eventListeners.ContainsKey(netEvent)) {
			eventListeners[netEvent](err);
		}
	}


	//消息委托类型
	public delegate void MsgListener(ProtocolBase msgBase);
	//消息监听列表
	private static Dictionary<string, MsgListener> msgListeners = new Dictionary<string, MsgListener>();
	//添加消息监听
	public static void AddMsgListener(string msgName, MsgListener listener) {
		//添加
		if (msgListeners.ContainsKey(msgName)) {
			msgListeners[msgName] += listener;
		}
		//新增
		else {
			msgListeners[msgName] = listener;
		}
	}
	//删除消息监听
	public static void RemoveMsgListener(string msgName, MsgListener listener) {
		if (msgListeners.ContainsKey(msgName)) {
			msgListeners[msgName] -= listener;
		}
	}
	//分发消息
	private static void FireMsg(string msgName, ProtocolBase msgBase) {
		if (msgListeners.ContainsKey(msgName)) {
			msgListeners[msgName](msgBase);
		}
	}


	//连接
	public static void Connect(string ip, int port) {
		//状态判断
		if (socket != null && socket.Connected) {
			Debug.Log("Connect fail, already connected!");
			return;
		}
		if (isConnecting) {
			Debug.Log("Connect fail, isConnecting");
			return;
		}
		//初始化成员
		InitState();
		//参数设置
		socket.NoDelay = true;
		//Connect
		isConnecting = true;
		socket.BeginConnect(ip, port, ConnectCallback, socket);
	}

	//初始化状态
	private static void InitState() {
		//Socket
		socket = new Socket(AddressFamily.InterNetwork,
			SocketType.Stream, ProtocolType.Tcp);
		//接收缓冲区
		readBuff = new ByteArray();
		//写入队列
		writeQueue = new Queue<ByteArray>();
		//是否正在连接
		isConnecting = false;
		//是否正在关闭
		isClosing = false;
		//消息列表
		msgList = new List<ProtocolBase>();
		//消息列表长度
		msgCount = 0;
		//上一次发送PING的时间
		lastPingTime = Time.time;
		//上一次收到PONG的时间
		lastPongTime = Time.time;

		proto = new ProtocolBytes();
		//监听PONG协议
		//if (!msgListeners.ContainsKey("MsgPong")) {
		//	AddMsgListener("MsgPong", OnMsgPong);
		//}
	}

	//Connect回调
	private static void ConnectCallback(IAsyncResult ar) {
		try {
			Socket socket = (Socket)ar.AsyncState;
			socket.EndConnect(ar);
			Debug.Log("Socket Connect Succ ");
			FireEvent(NetEvent.ConnectSucc, "");
			isConnecting = false;
			//开始接收
			socket.BeginReceive(readBuff.bytes, readBuff.writeIdx,
											readBuff.remain, 0, ReceiveCallback, socket);

		}
		catch (SocketException ex) {
			Debug.Log("Socket Connect fail " + ex.ToString());
			FireEvent(NetEvent.ConnectFail, ex.ToString());
			isConnecting = false;
		}
	}


	//关闭连接
	public static void Close() {
		//状态判断
		if (socket == null || !socket.Connected) {
			return;
		}
		if (isConnecting) {
			return;
		}
		//还有数据在发送
		if (writeQueue.Count > 0) {
			isClosing = true;
		}
		//没有数据在发送
		else {
			socket.Close();
			FireEvent(NetEvent.Close, "");
		}
	}

	//发送数据
	public static void Send(ProtocolBase msg) {
		//状态判断
		if (socket == null || !socket.Connected) {
			return;
		}
		if (isConnecting) {
			return;
		}
		if (isClosing) {
			return;
		}
		//数据编码
		byte[] bytes = msg.Encode();
		byte[] length = BitConverter.GetBytes(bytes.Length);
		byte[] sendBytes = length.Concat(bytes).ToArray();

		//写入队列
		ByteArray ba = new ByteArray(sendBytes);
		int count = 0;  //writeQueue的长度
		lock (writeQueue) {
			writeQueue.Enqueue(ba);
			count = writeQueue.Count;
		}
		//send
		if (count == 1) {
			socket.BeginSend(sendBytes, 0, sendBytes.Length,
				0, SendCallback, socket);
		}
	}

	//Send回调
	public static void SendCallback(IAsyncResult ar) {

		//获取state、EndSend的处理
		Socket socket = (Socket)ar.AsyncState;
		//状态判断
		if (socket == null || !socket.Connected) {
			return;
		}
		//EndSend
		int count = socket.EndSend(ar);
		//获取写入队列第一条数据            
		ByteArray ba;
		lock (writeQueue) {
			ba = writeQueue.First();
		}
		//完整发送
		ba.readIdx += count;
		if (ba.length == 0) {
			lock (writeQueue) {
				writeQueue.Dequeue();
				ba = writeQueue.First();
			}
		}
		//继续发送
		if (ba != null) {
			socket.BeginSend(ba.bytes, ba.readIdx, ba.length,
				0, SendCallback, socket);
		}
		//正在关闭
		else if (isClosing) {
			socket.Close();
		}
	}



	//Receive回调
	public static void ReceiveCallback(IAsyncResult ar) {
		try {
			Socket socket = (Socket)ar.AsyncState;
			//获取接收数据长度
			int count = socket.EndReceive(ar);
			readBuff.writeIdx += count;
			//处理二进制消息
			OnReceiveData();
			//继续接收数据
			if (readBuff.remain < 8) {
				readBuff.MoveBytes();
				readBuff.ReSize(readBuff.length * 2);
			}
			socket.BeginReceive(readBuff.bytes, readBuff.writeIdx,	readBuff.remain, 0, ReceiveCallback, socket);
		}
		catch (SocketException ex) {
			Debug.Log("Socket Receive fail" + ex.ToString());
		}
	}

	//数据处理
	public static void OnReceiveData() {
		//消息长度
		if (readBuff.length < sizeof(Int32)) {
			return;
		}
		// Console.WriteLine("接收到了 " + conn.readBuff.length + " 个字节") ;
		Array.Copy(readBuff.bytes, lenBytes, sizeof(Int32));
		msgLength = BitConverter.ToInt32(lenBytes, 0);

		//小于最小要求长度则返回表示未接收完全
		if (readBuff.length < msgLength + sizeof(Int32)) {
			return;
		}
		//
		ProtocolBase protocol = proto.Decode(readBuff.bytes, sizeof(Int32), msgLength);
		Debug.Log("Name: " + protocol.GetName() + "GetDesc: " + protocol.GetDesc());
		//HandleMsg(protocol);
		readBuff.readIdx += (msgLength + sizeof(Int32));
		readBuff.CheckAndMoveBytes();
		//添加到消息队列
		lock (msgList) {
			msgList.Add(protocol);
			msgCount++;
		}
		//继续读取消息
		if (readBuff.length > 4) {
			OnReceiveData();
		}
	}

	//Update
	public static void Update() {
		MsgUpdate();
		PingUpdate();
	}

	//更新消息
	public static void MsgUpdate() {
		//初步判断，提升效率
		if (msgCount == 0) {
			return;
		}
		//重复处理消息
		for (int i = 0; i < MAX_MESSAGE_FIRE; i++) {
			//获取第一条消息
			ProtocolBase msgBase = null;
			lock (msgList) {
				if (msgList.Count > 0) {
					msgBase = msgList[0];
					msgList.RemoveAt(0);
					msgCount--;
				}
			}
			//分发消息
			if (msgBase != null) {
				FireMsg(msgBase.GetName(), msgBase);
			}
			//没有消息了
			else {
				break;
			}
		}
	}

	//发送PING协议
	private static void PingUpdate() {
		//是否启用
		/*if (!isUsePing) {
			return;
		}
		//发送PING
		if (Time.time - lastPingTime > pingInterval) {
			MsgPing msgPing = new MsgPing();
			Send(msgPing);
			lastPingTime = Time.time;
		}
		//检测PONG时间
		if (Time.time - lastPongTime > pingInterval * 4) {
			Close();
		}*/
	}

	//监听PONG协议
	private static void OnMsgPong(ProtocolBase msgBase) {
		lastPongTime = Time.time;
	}
}
