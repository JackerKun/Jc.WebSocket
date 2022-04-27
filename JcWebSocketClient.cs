/*
*   Author  :   JackerKun
*   Date    :   Wednesday, 27 April 2022 10:49:43
*   About   :
*/

using System.Net.WebSockets;
using System.Text;

namespace Jc.WS;

/// <summary>
/// websocket client
/// </summary>
public class JcWebSocketClient
{
    /// <summary>
    /// 当前链接对象
    /// </summary>
    ClientWebSocket ws = null;
    
    /// <summary>
    /// Uri
    /// </summary>
    Uri uri = null;
    
    /// <summary>
    /// 是否最后由用户手动关闭
    /// </summary>
    bool isUserClose = false;

    /// <summary>
    /// WebSocket状态
    /// </summary>
    public WebSocketState? State
    {
        get => ws?.State;
    }

    /// <summary>
    /// 包含一个数据的事件
    /// </summary>
    public delegate void MessageEventHandler(object sender, string data);

    /// <summary>
    /// 错误时间
    /// </summary>
    public delegate void ErrorEventHandler(object sender, Exception ex);

    /// <summary>
    /// 连接建立时触发
    /// </summary>
    public event EventHandler? OnOpen;

    /// <summary>
    /// 客户端接收服务端数据时触发
    /// </summary>
    public event MessageEventHandler? OnMessage;

    /// <summary>
    /// 通信发生错误时触发
    /// </summary>
    public event ErrorEventHandler? OnError;

    /// <summary>
    /// 连接关闭时触发
    /// </summary>
    public event EventHandler? OnClose;

    /// <summary>
    /// cot
    /// </summary>
    /// <param name="wsUrl"></param>
    public JcWebSocketClient(string wsUrl)
    {
        uri = new Uri(wsUrl);
        ws = new ClientWebSocket();
    }

    /// <summary>
    /// 打开链接
    /// </summary>
    public void Open()
    {
        Task.Run(async () =>
        {
            if (ws.State == WebSocketState.Connecting || ws.State == WebSocketState.Open)
                return;

            string netErr = string.Empty;
            try
            {
                //初始化链接
                isUserClose = false;
                ws = new ClientWebSocket();
                await ws.ConnectAsync(uri, CancellationToken.None);

                if (OnOpen != null)
                    OnOpen(ws, new EventArgs());

                //全部消息容器
                List<byte> bs = new List<byte>();
                //缓冲区
                var buffer = new byte[1024 * 4];
                //监听Socket信息
                WebSocketReceiveResult result =
                    await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                //是否关闭
                while (!result.CloseStatus.HasValue)
                {
                    //文本消息
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        bs.AddRange(buffer.Take(result.Count));

                        //消息是否已接收完全
                        if (result.EndOfMessage)
                        {
                            //发送过来的消息
                            string userMsg = Encoding.UTF8.GetString(bs.ToArray(), 0, bs.Count);

                            if (OnMessage != null)
                                OnMessage(ws, userMsg);

                            //清空消息容器
                            bs = new List<byte>();
                        }
                    }

                    //继续监听Socket信息
                    result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                //关闭WebSocket（服务端发起）
                //await ws.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }
            catch (Exception ex)
            {
                if (OnError != null)
                    OnError(ws, ex);
            }
            finally
            {
                if (!isUserClose)
                    Close(ws.CloseStatus.Value, ws.CloseStatusDescription + netErr);
            }
        });

    }

    /// <summary>
    /// 使用连接发送文本消息
    /// </summary>
    /// <param name="mess"></param>
    /// <returns>是否尝试了发送</returns>
    public bool Send(string mess)
    {
        try
        {
            if (ws.State != WebSocketState.Open)
                return false;

            Task.Run(async () =>
            {
                var replyMess = Encoding.UTF8.GetBytes(mess);
                //发送消息
                await ws.SendAsync(new ArraySegment<byte>(replyMess), WebSocketMessageType.Text, true,
                    CancellationToken.None);
            });

            return true;
        }
        catch (Exception ex)
        {
            if (OnError != null)
                OnError(ws, ex);
            return false;
        }
    }

    /// <summary>
    /// 使用连接发送字节消息
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns>是否尝试了发送</returns>
    public bool Send(byte[] bytes)
    {
        try
        {
            if (ws.State != WebSocketState.Open)
                return false;

            Task.Run(async () =>
            {
                //发送消息
                await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true,
                    CancellationToken.None);
            });
        }
        catch (Exception ex)
        {
            if (OnError != null)
                OnError(ws, ex);
            return false;
        }
        return true;
    }

    /// <summary>
    /// 关闭连接
    /// </summary>
    public void Close()
    {
        try
        {
            isUserClose = true;
            Close(WebSocketCloseStatus.NormalClosure, "closed by user");
        }
        catch (Exception e)
        {
            if (OnError != null)
                OnError(ws, e);
        }
    }

    /// <summary>
    /// 断开链接
    /// </summary>
    /// <param name="closeStatus"></param>
    /// <param name="statusDescription"></param>
    private void Close(WebSocketCloseStatus closeStatus, string statusDescription)
    {
        Task.Run(async () =>
        {
            try
            {
                await ws.CloseAsync(closeStatus, statusDescription, CancellationToken.None);
            }
            catch (Exception ex)
            {
                if (OnError != null)
                    OnError(ws, ex);
            }
            ws.Abort();
            ws.Dispose();
            if (OnClose != null)
                OnClose(ws, new EventArgs());
        });
    }
}