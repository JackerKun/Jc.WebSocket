/*
*   Author  :   JackerKun
*   Date    :   Sunday, 06 March 2022 20:11:03
*   About   :

启动WebSocket

app.JcWebSocketServer(callBack);

void callBack(WebSocketState state,string token, string msg)
{
    Console.WriteLine($"state:{state.ToString()}>token:{token}>msg:{msg}");
}

链接地址：
ws://ip:port?token=your token uuid
token:必选，每个连接着的唯一身份信息
*/

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using Microsoft.AspNetCore.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jc.WS;

/// <summary>
/// 基于Asp.netCore的websocket
/// </summary>
public static class JcWebSocketServer
{
    private const int BufferSize = 4096;

    /// <summary>
    /// 在线集合
    /// </summary>
    public static List<SocketObject> ArySocket = new List<SocketObject>();

    /// <summary>
    /// 消息回掉
    /// </summary>
    public delegate void MessageCallback(WebSocketState state, string token, string? msg);

    /// <summary>
    /// 定义回掉
    /// </summary>
    private static MessageCallback? _callback;

    /// <summary>
    /// 是否不可重复
    /// </summary>
    private static bool _repeat = false;

    #region 公共方法

    /// <summary>
    /// 创建中间件
    /// </summary>
    /// <param name="app"></param>
    /// <param name="callback"></param>
    /// <param name="repeat">是否允许重复ID，默认否</param>
    /// <returns></returns>
    public static IApplicationBuilder UseJcWebSocketServer(
        this IApplicationBuilder app, 
        MessageCallback? callback,
        bool repeat = false)
    {
        try
        {
            _callback = callback;
            _repeat = repeat;
            app.UseWebSockets();
            app.Use(async (context, next) =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    await JcWebSocketServer.Acceptor(context, next);
                }
                else
                {
                    await next();
                }
            });
            return app;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw e;
        }
    }

    /// <summary>
    /// 异步为指定用户发送数据
    /// </summary>
    /// <param name="token"></param>
    /// <param name="txt"></param>
    /// <returns></returns>
    public static async Task<(bool status, string? message)> SendTxtAsync(string token, string txt)
    {
        bool status = false;
        string? message = null;
        try
        {
            if (ArySocket.Count > 0)
            {
                //查找WS是否存在
                List<SocketObject>? WS = GetWs(token);
                if (WS == null || WS.Count <= 0)
                {
                    status = false;
                    message += $"this client [{token}] not found\t";
                    return (status, message);
                }
                //成功发送的个数
                int successCount = 0;
                for (int i = 0; i < WS.Count; i++)
                {
                    Console.WriteLine("sendText:" + token + ">" + txt);
                    if (WS[i].WebSocket.State != WebSocketState.Open)
                    {
                        string? error = $"[{WS[i].Token}] not open\t";
                        message += error;
                        Console.WriteLine(error);
                        continue;
                    }
                    await _SendMsgAsync(txt, WS[i].WebSocket).ContinueWith(t =>
                    {
                        if (t.Status == TaskStatus.RanToCompletion)
                        {
                            successCount++;
                        }
                        else
                        {
                            Console.WriteLine("send error");
                            status = false;
                            if (t.Exception != null) message = message + t.Exception.Message;
                        }
                    });
                }
                if (successCount == WS.Count)
                {
                    status = true;
                }
            }
            else
            {
                message += $"online client is none";
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            status = false;
            message = e.Message;
        }
        return (status, message);
    }

    /// <summary>
    /// 异步为所有用户发送数据
    /// </summary>
    /// <param name="txt"></param>
    /// <returns></returns>
    public static async Task<(bool status, string? message)> SendTxtAsync(string txt)
    {
        bool status = false;
        string? message = null;
        try
        {
            if (ArySocket.Count > 0)
            {
                int successCount = 0;
                for (int i = 0; i < ArySocket.Count; i++)
                {
                    await _SendMsgAsync(txt, ArySocket[i].WebSocket).ContinueWith(t =>
                    {
                        if (t.Status == TaskStatus.RanToCompletion)
                        {
                            successCount++;
                        }
                        else
                        {
                            Console.WriteLine("send error");
                            status = false;
                            if (t.Exception != null) message = message + t.Exception.Message;
                        }
                    });
                    if (successCount == ArySocket.Count)
                    {
                        status = true;
                    }
                }
            }
            else
            {
                message += $"online client is none";
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            status = false;
            message = e.Message;
        }
        return (status, message);
    }

    /// <summary>
    /// 为所有在线的用户发送消息
    /// </summary>
    /// <param name="txt"></param>
    public static void SendTxt(string txt)
    {
        try
        {
            if (ArySocket.Count > 0)
            {
                for (int i = 0; i < ArySocket.Count; i++)
                {
                    _SendMsgAsync(txt, ArySocket[i].WebSocket);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw e;
        }
    }
    
    /// <summary>
    /// 关闭指定链接
    /// </summary>
    /// <param name="token"></param>
    public static void DisConnect(string token)
    {
        try
        {
            if (ArySocket.Count > 0)
            {
                DeleteOffline(token,true);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw e;
        }
    }

    /// <summary>
    /// 获取在线列表
    /// </summary>
    /// <returns></returns>
    public static List<SocketObject> GetUserList()
    {
        try
        {
            ClearOffLine();
            return ArySocket;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    /// <summary>
    /// 清空列表
    /// </summary>
    /// <returns></returns>
    public static bool ClearUserList()
    {
        try
        {
            ArySocket.Clear();
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw e;
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 启动中间件
    /// </summary>
    /// <param name="hc"></param>
    /// <param name="n"></param>
    /// <returns></returns>
    private static async Task Acceptor(HttpContext hc, Func<Task> n)
    {
        try
        {
            WebSocket webSocket = await hc.WebSockets.AcceptWebSocketAsync();
            //判断入参用户ID 是否为空
            string queryStr = hc.Request.Query["token"].ToString();
            Console.WriteLine("queryStr:" + queryStr);

            if (string.IsNullOrWhiteSpace(queryStr))
            {
                throw new Exception("queryStr is null");
            }
            //将新连接 加入到集合
            ArySocket.Add(new SocketObject
            {
                WebSocket = webSocket,
                Token = queryStr
            });

            //清理下未在线的
            ClearOffLine();

            //开启连接 回传
            if (_callback != null)
            {
                _callback(WebSocketState.Connecting, queryStr, null);
            }
            await RunWs(webSocket, queryStr);
            await webSocket
                .CloseAsync(webSocket.CloseStatus.Value, webSocket.CloseStatusDescription, CancellationToken.None)
                .ContinueWith(
                    t =>
                    {
                        switch (webSocket.State)
                        {
                            case WebSocketState.Open:
                                Console.WriteLine("open");
                                break;
                            default:
                                DeleteOffline(queryStr);
                                break;
                        }
                    });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw e; 
        }
    }

    /// <summary>
    /// 运行ws
    /// </summary>
    /// <param name="webSocket"></param>
    /// <param name="queryStr"></param>
    public static async Task RunWs(WebSocket webSocket, string queryStr)
    {
        try
        {
            var buff = new byte[BufferSize];
            var seg = new ArraySegment<byte>(buff);
            while (!webSocket.CloseStatus.HasValue)
            {
                string recvMsg = "";
                var result = await webSocket.ReceiveAsync(seg, CancellationToken.None);
                if (result.CloseStatus == null)
                {
                    recvMsg = Encoding.UTF8.GetString(buff, 0, result.Count);
                }
                if (_callback != null)
                {
                    _callback(webSocket.State, queryStr, recvMsg);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw e;
        }
    }


    /// <summary>
    /// 根据用户ID 获取Socket
    /// </summary>
    /// <param name="Token"></param>
    /// <returns></returns>
    private static List<SocketObject>? GetWs(string Token)
    {
        try
        {
            List<SocketObject> ws = null;
            ws = ArySocket.FindAll(s => s.Token == Token);
            return ws;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    /// <summary>
    /// 删除离线了的用户 删除的依据就是 websocet.status!=2
    /// </summary>
    private static void ClearOffLine()
    {
        try
        {
            if (ArySocket.Count <= 0)
            {
                return;
            }

            List<SocketObject> tmp = new List<SocketObject>();
            tmp.AddRange(ArySocket);
            foreach (var v in tmp)
            {
                if (v!=null&&(v.WebSocket == null || 
                              v.WebSocket.State == WebSocketState.CloseReceived||
                              v.WebSocket.State == WebSocketState.Closed||
                              v.WebSocket.State == WebSocketState.CloseSent||
                              v.WebSocket.State ==WebSocketState.Aborted||
                              v.WebSocket.State==WebSocketState.None))
                {
                    Console.WriteLine($"deleted offline {v.Token}");
                    ArySocket.Remove(v);
                }
            }
            Console.WriteLine("online count :"+ArySocket.Count());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw e;
        }
    }

    /// <summary>
    /// 删除指定连接
    /// </summary>
    /// <param name="token"></param>
    private static void DeleteOffline(string token, bool byUser = false)
    {
        try
        {
            //是否用户行为删除
            if (byUser)
            {
                SetOffline(token);
            }
            if (!_repeat)
            {
                SetOffline(token);
            }
            //从已经断开的列表中移除
            ClearOffLine();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw e;
        }
    }

    /// <summary>
    /// 使某个ID下线
    /// </summary>
    /// <param name="token"></param>
    private static void SetOffline(string token)
    {
        try
        {
            List<SocketObject> sc = ArySocket.FindAll(s => s.Token == token);
            if (sc == null)
            {
                return;
            }
            foreach (var v in sc)
            {
                //如果已经在线了 就 强制离线
                if (v != null && v.WebSocket != null &&
                    v.WebSocket.State != WebSocketState.Closed &&
                    v.WebSocket.State != WebSocketState.CloseReceived &&
                    v.WebSocket.State != WebSocketState.CloseReceived &&
                    v.WebSocket.State != WebSocketState.Aborted)
                {
                    CancellationToken _cancellation = new CancellationToken();
                    v.WebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "closed", _cancellation);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw e;
        }
    }

    /// <summary>
    /// 执行发送
    /// </summary>
    /// <param name="str"></param>
    /// <param name="ws"></param>
    private static void _SendMsg(string str, WebSocket ws)
    {
        try
        {
            var outgoing = new ArraySegment<byte>(Encoding.UTF8.GetBytes(str));
            ws.SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw e;
        }
    }

    /// <summary>
    /// 执行异步发送
    /// </summary>
    /// <param name="str"></param>
    /// <param name="ws"></param>
    /// <returns></returns>
    private static async Task _SendMsgAsync(string str, WebSocket ws)
    {
        try
        {
            if (ws.State != WebSocketState.Open)
            {
                throw new Exception("ws is closed");
            }
            var outgoing = new ArraySegment<byte>(Encoding.UTF8.GetBytes(str));
            await ws.SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw e;
        }
    }
    #endregion
}

/// <summary>
/// socket连接对象
/// </summary>
public class SocketObject
{
    private WebSocket _webSocket;
    private string _token;

    /// <summary>
    /// 连接对象
    /// </summary>
    public WebSocket WebSocket
    {
        get => _webSocket;
        set => _webSocket = value;
    }

    /// <summary>
    /// 身份
    /// </summary>
    public string Token
    {
        get => _token;
        set => _token = value;
    }

    public int Id { get; set; }
}