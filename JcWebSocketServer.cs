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
public  static class JcWebSocketServer
{
    private const int BufferSize = 4096;

    /// <summary>
    /// 在线集合
    /// </summary>
    public static List<SocketObject> ArySocket = new List<SocketObject>();

    /// <summary>
    /// 消息回掉
    /// </summary>
    public delegate void MessageCallback(WebSocketState state,string token, string msg);

    /// <summary>
    /// 定义回掉
    /// </summary>
    private static MessageCallback _callback;

    /// <summary>
    /// 启动中间件
    /// </summary>
    /// <param name="hc"></param>
    /// <param name="n"></param>
    /// <returns></returns>
    private static async Task Acceptor(HttpContext hc, Func<Task> n)
    {
        if (!hc.WebSockets.IsWebSocketRequest)
        {
            return;
        }
        //判断入参用户ID 是否为空
        string query_str = hc.Request.Query["token"].ToString();
        try
        {
            if (string.IsNullOrEmpty(query_str)) return;
            //终端已经存在的token链接
            DeleteOffline(query_str);
            var socket = await hc.WebSockets.AcceptWebSocketAsync();

            //将新连接 加入到集合
            SocketObject ws = new SocketObject();
            ws.WebSocket = socket;
            ws.Token = query_str;
            ArySocket.Add(ws);
            //开启连接 回传
            if (_callback != null)
            {
                _callback(WebSocketState.Connecting, query_str, null);
            }
            var buffer = new byte[BufferSize];
            var seg = new ArraySegment<byte>(buffer);

            while (true && ArySocket.Count > 0)
            {
                List<SocketObject> thisSockets = GetWS_Parm(query_str);
                if (thisSockets == null || thisSockets.Count <= 0)
                {
                    continue;
                }
                Console.WriteLine("connected:" + ArySocket.Count());
                for (int i = 0; i < thisSockets.Count; i++)
                {
                    SocketObject thisSocket = thisSockets[i];
                    string receivemsg = null;
                    switch (thisSocket.WebSocket.State)
                    {
                        //连接成功
                        case WebSocketState.Open:
                            try
                            {
                                if (thisSocket!=null&&thisSocket.WebSocket != null&&thisSocket.WebSocket.State==WebSocketState.Open)
                                {
                                    var incoming = await thisSocket.WebSocket.ReceiveAsync(seg, CancellationToken.None);
                                    if (incoming.CloseStatus == null)
                                    {
                                        receivemsg = Encoding.UTF8.GetString(buffer, 0, incoming.Count);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                            break;
                        default:
                            if (thisSocket.WebSocket.State != WebSocketState.Aborted)
                            {
                                thisSocket.WebSocket.Abort();
                            }
                            DeleteOffline(query_str);
                            break;
                    }
                    if (_callback != null)
                    {
                        _callback(thisSocket.WebSocket.State, query_str, receivemsg);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    #region 公共方法

    /// <summary>
    /// 创建中间件
    /// </summary>
    /// <param name="app"></param>
    public static IApplicationBuilder UseJcWebSocketServer(this IApplicationBuilder app,MessageCallback callback)
    {
        try
        {
            _callback = callback;
            app.UseWebSockets();
            app.Use(async (context, next) =>
            {
                 await JcWebSocketServer.Acceptor(context, next);
                 next();
            });
            return app;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    /// <summary>
    /// 公开SendTxt指定用户发送消息
    /// </summary>
    /// <param name="token"></param>
    /// <param name="txt"></param>
    public static void SendTxt(string token, string txt)
    {
        try
        {
            if (ArySocket.Count > 0)
            {
                List<SocketObject> WS = GetWS_Parm(token);
                for (int i = 0; i < WS.Count; i++)
                {
                    _SendMsgAsync(txt, WS[i].WebSocket);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
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
            throw;
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
                DeleteOffline(token);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
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
            DeleteOffline();
            return ArySocket;
        }
        catch (Exception ex)
        {
            throw;
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
            throw;
        }
    }

    #endregion

    /// <summary>
    /// 根据用户ID 获取Socket
    /// </summary>
    /// <param name="Token"></param>
    /// <returns></returns>
    private static List<SocketObject> GetWS_Parm(string Token)
    {
        List<SocketObject> ws = new List<SocketObject>();
        ws = ArySocket.FindAll(s => s.Token == Token);
        return ws;
    }

    /// <summary>
    /// 删除离线了的用户 删除的依据就是 websocet.status!=2
    /// </summary>
    private static void DeleteOffline()
    {
        try
        {
            foreach (var v in ArySocket)
            {
                if (v!=null&&(v.WebSocket == null || v.WebSocket.State != WebSocketState.Open))
                {
                    ArySocket.Remove(v);
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        Console.WriteLine("online count :"+ArySocket.Count());
    }

    /// <summary>
    /// 删除指定连接
    /// </summary>
    /// <param name="token"></param>
    private static void DeleteOffline(string token)
    {
        List<SocketObject> sc = ArySocket.FindAll(s => s.Token == token);
        if (sc == null)
        {
            return;
        }
        foreach (var v in sc)
        {
            if (v!=null&&v.WebSocket != null&&v.WebSocket.State!=WebSocketState.Closed)
            {
                CancellationToken _cancellation = new CancellationToken();
                v.WebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "closed", _cancellation);
            }
        }
        DeleteOffline();
    }

    /// <summary>
    /// 执行发送
    /// </summary>
    /// <param name="str"></param>
    /// <param name="ws"></param>
    private static void _SendMsg(string str, WebSocket ws)
    {
        var outgoing = new ArraySegment<byte>(Encoding.UTF8.GetBytes(str));
        ws.SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    /// <summary>
    /// 执行异步发送
    /// </summary>
    /// <param name="str"></param>
    /// <param name="ws"></param>
    /// <returns></returns>
    private static async Task _SendMsgAsync(string str, WebSocket ws)
    {
        var outgoing = new ArraySegment<byte>(Encoding.UTF8.GetBytes(str));
        await ws.SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);
    }

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
}