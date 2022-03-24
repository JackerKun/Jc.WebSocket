# Jc.WebSocket
websocket中间件

## JcWebSocketServer
.net core webapi Websocket服务中间件

### 启动方法
``` c#
//启动方法
app.UseJcWebSocketServer(callBack);

//回调函数处理
void callBack(WebSocketState state,string token, string msg)
{
    Console.WriteLine($"state:{state.ToString()}>token:{token}>msg:{msg}");
}
```
### 客户端 连接方式：
ws://ip:port?token=your token uuid

 > token:
必选，每个连接者的唯一身份信息
只能是唯一，如果有重复 将关闭上一个链接

### 为指定Token发送消息
JcWebSocketServer.SendText(token,text)

### 为所有连接发送消息
JcWebSocketServer.SendText(text)

### 获取在线列表
JcWebSocketServer.GetUserList()

### 关闭指定Token链接
JcWebSocketServer.DisConnect(token)


## JcWebSocketClient
.net core webapi Websocket客户端中间件

### 连接Server

### 发送消息

### 接收消息

# WebSocket在线测试工具
[http://websocket-test.cn](http://websocket-test.cn)


# Upgrade content:
#### v1.0.4
1.Fixed Net6 Mini api compatibility

