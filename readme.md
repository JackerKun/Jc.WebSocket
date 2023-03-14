# Jc.WebSocket

websocket中间件

## JcWebSocketServer
.net core webapi Websocket服务中间件

### 启动方法
``` c#
//启动方法
app.UseJcWebSocketServer(callBack,isrepeat);

//回调函数处理
void callBack(WebSocketState state,string token, string msg)
{
    Console.WriteLine($"state:{state.ToString()}>token:{token}>msg:{msg}");
}
```
### 客户端 连接方式：
ws://ip:port?token=your token uuid

 > token:
当：isrepeat=false
只能是唯一，如果有重复 将关闭上一个链接


### 为指定Token发送消息
JcWebSocketServer.SendText(token,text)

await JcWebSocketServer.SendTxtAsync(token, text);

### 为所有连接发送消息
JcWebSocketServer.SendText(text)

### 获取在线列表
JcWebSocketServer.GetUserList()

### 关闭指定Token链接
JcWebSocketServer.DisConnect(token)


## JcWebSocketClient
.net core webapi Websocket客户端中间件

### 连接Server
 ```c#
 
 //创建链接
void testWebClient()
{
    JcWebSocketClient wSocketClient = new JcWebSocketClient("ws://192.168.1.140:5010?token=2");
    wSocketClient.OnOpen -= WSocketClient_OnOpen;
    wSocketClient.OnMessage -= WSocketClient_OnMessage;
    wSocketClient.OnClose -= WSocketClient_OnClose;
    wSocketClient.OnError -= WSocketClient_OnError;
 
    wSocketClient.OnOpen += WSocketClient_OnOpen;
    wSocketClient.OnMessage += WSocketClient_OnMessage;
    wSocketClient.OnClose += WSocketClient_OnClose;
    wSocketClient.OnError += WSocketClient_OnError;
    wSocketClient.Open();

    wSocketClient.Send("info");
}

 
  void WSocketClient_OnError(object sender, Exception ex)
{
 
}
 
  void WSocketClient_OnClose(object sender, EventArgs e)
{
 
}
 
  void WSocketClient_OnMessage(object sender, string data)
{
    Console.WriteLine(data);
}
 
  void WSocketClient_OnOpen(object sender, EventArgs e)
{
 
}
 ```

# WebSocket在线测试工具
[http://websocket-test.cn](http://websocket-test.cn)


# Upgrade content:
#### v1.0.4
1.Fixed Net6 Mini api compatibility

#### v1.3.16
1.修复SendTxt 返回状态

#### v1.3.18
https://zhuanlan.zhihu.com/p/338772340

修复 问题

> One or more errors occurred. (The remote party closed the WebSocket connection without completing the close handshake.
