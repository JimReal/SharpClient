using SuperSocket.Client;
using SuperSocket.Channel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace tcpclient
{
    public class TestClient
    {
        public static long SID;
        public static long RID;
        public static byte KEY;
        private bool _conn;
        private IPEndPoint _url;
        private readonly IEasyClient<IMPackage, IMPackage> _client;
        private IMPackageEncoder _encoder;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip">服务器IP</param>
        /// <param name="port">服务器端口</param>
        public TestClient(string ip, int port)
        {
            _url = new IPEndPoint(IPAddress.Parse(ip), port);
            _encoder = new IMPackageEncoder();
            var client = new EasyClient<IMPackage, IMPackage>(new IMPipelineFilter(), _encoder, new ChannelOptions()
            {
                SendTimeout = 30//...
            });
            _client = client;
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        public async ValueTask Closeing()
        {
            await _client.CloseAsync();
            _conn = false;
        }

        /// <summary>
        /// 开始连接
        /// </summary>
        public async ValueTask Recevice()
        {
            _conn = await _client.ConnectAsync(_url);
            while (_conn)
            {
                //客户端本地需要维护好友信息（用户信息）、群聊信息，
                //接收消息时，需要根据用户id、群聊id匹配基本信息并展示
                var msg = await _client.ReceiveAsync();
                if (msg != null)
                {
                    switch (msg.Key)
                    {
                        case (byte)0:
                            if (msg.Id == 0)
                            {
                                Console.WriteLine($"服务端要求鉴权......");
                                await _client.SendAsync(new IMPackage() { Key = 0, Body = String.Empty });
                            }
                            else
                            {
                                Console.WriteLine($"鉴权成功，开始拉取离线消息");//拉取离线消息，必须同步处理
                                var msgRes = await new HttpClient().GetAsync($"http://localhost:5000/api/msg/offline?id={SID}");
                                msgRes.EnsureSuccessStatusCode();
                                var msgStr = await msgRes.Content.ReadAsStringAsync();
                                if (!string.IsNullOrWhiteSpace(msgStr))
                                {
                                    var megs = JsonSerializer.Deserialize<IMPackage[]>(msgStr, new JsonSerializerOptions()
                                    {
                                        PropertyNameCaseInsensitive = true
                                    });
                                    if (megs != null && megs.Length > 0)
                                    {
                                        for (int i = megs.Length - 1; i >= 0; i--)
                                        {
                                            var mm = megs[i];
                                            Console.WriteLine($"收到一条离线{(mm.Key == 1 ? "单" : "群")}聊消息");
                                            Console.WriteLine($"消息id：{mm.Id}，发送者：{mm.SId}，接受者：{mm.RId},内容：{ mm.Body}");
                                        }
                                    }
                                };
                            }
                            break;

                        case (byte)1:
                            Console.WriteLine($"收到一条单聊消息");
                            Console.WriteLine($"消息id：{msg.Id}，发送者：{msg.SId}，接受者：{msg.RId},内容：{ msg.Body}");

                            //发送回执给服务端：服务端会在5S内发送4次消息，接收消息后需要立即发送msg.Id的回执，并根据msg.Id对重复消息去重
                            //如5S内未发送回执，服务端会移除连接。
                            await _client.SendAsync(new IMPackage() { Key = 2, Id = msg.Id, SId = SID, Body = String.Empty, RId = 0 });
                            break;

                        case (byte)2:
                            Console.WriteLine($"单聊消息收到回执");
                            break;

                        case (byte)3:
                            //此处Rid是群聊id，客户端收到消息后需要分发并展示到Rid对应的群聊中。
                            Console.WriteLine($"收到一条群聊消息");
                            Console.WriteLine($"消息id：{msg.Id}，发送者：{msg.SId}，接受者：{msg.RId},内容：{ msg.Body}");
                            await _client.SendAsync(new IMPackage() { Key = 4, Id = msg.Id, SId = SID, Body = String.Empty, RId = 0 });
                            break;

                        case (byte)4:
                            Console.WriteLine($"群聊消息收到回执");
                            break;

                        case (byte)5:
                            //此处Rid是群聊id，客户端收到消息后需要分发并展示到Rid对应的群聊中。
                            Console.WriteLine($"收到一条无包体群聊消息，将从端口拉取消息内容");
                            Console.WriteLine($"消息id：{msg.Id}，发送者：{msg.SId}，接受者：{msg.RId},内容：{ msg.Body}");
                            await _client.SendAsync(new IMPackage() { Key = 6, Id = msg.Id, SId = SID, Body = String.Empty, RId = 0 });
                            break;

                        case (byte)6:
                            Console.WriteLine($"无包体群聊消息收到回执");
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public async ValueTask Send(IMPackage model)
        {
            if (_conn)
            {
                await _client.SendAsync(model);
            }
        }
    }
}
