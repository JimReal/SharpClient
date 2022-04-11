using Newtonsoft.Json;
using SuperSocket.ClientEngine;
using System;
using System.Collections.Generic;
using SuperSocket.ProtoBase;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace IMClient
{
    public class TestClient
    {
        public static long SID;
        public static long RID;
        public static byte KEY;
        private bool _conn;
        private IPEndPoint _url;
        private readonly EasyClient<IMPackage> _client;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip">服务器IP</param>
        /// <param name="port">服务器端口</param>
        public TestClient(string ip, int port)
        {
            _url = new IPEndPoint(IPAddress.Parse(ip), port);
            var client = new EasyClient<IMPackage>();//初始化客户端
            _client = client;
            _client.Initialize(new IMPipelineFilter());//使用自定义解码器。
            _client.NewPackageReceived += OnReceived;
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        public async ValueTask Closeing()
        {
            await _client.Close();
            _conn = false;
        }

        public byte[] EncodingMsg(IMPackage pack)
        {
            byte[] buffer = null;
            var id = BitConverter.GetBytes(pack.Id);
            var sid = BitConverter.GetBytes(pack.SId);
            var rid = BitConverter.GetBytes(pack.RId);
            var bodyBuffer = Encoding.UTF8.GetBytes(pack.Body);
            var bodylenth = BitConverter.GetBytes(bodyBuffer.Length);
            using (MemoryStream ms = new MemoryStream())
            {
                ms.WriteByte(pack.Key);//命令字
                ms.Write(id, 0, id.Length);//id
                ms.Write(sid, 0, sid.Length);//发送者
                ms.Write(rid, 0, rid.Length);//接受者
                ms.Write(bodylenth, 0, bodylenth.Length);//包体长度
                ms.Write(bodyBuffer, 0, bodyBuffer.Length);//包体
                buffer = ms.ToArray();
            }
            return buffer;
        }


        public async void OnReceived(object sender, PackageEventArgs<IMPackage> e)
        {
            if (e.Package != null)
            {
                var msg = e.Package;
                switch (msg.Key)
                {
                    case (byte)0:
                        if (msg.Id == 0)
                        {
                            Console.WriteLine($"服务端要求鉴权......");
                            _client.Send(EncodingMsg(new IMPackage() { Key = 0, Body = string.Empty }));
                        }
                        else
                        {
                            Console.WriteLine($"鉴权成功，开始拉取离线消息");//拉取离线消息，必须同步处理
                            var msgRes = await new HttpClient().GetAsync($"http://{_url.Address}:{ConfigurationManager.AppSettings["apiPort"]}/api/msg/offline?id={SID}");
                            msgRes.EnsureSuccessStatusCode();
                            var msgStr = await msgRes.Content.ReadAsStringAsync();
                            if (!string.IsNullOrWhiteSpace(msgStr))
                            {
                                var megs = JsonConvert.DeserializeObject<IMPackage[]>(msgStr);
                                if (megs != null && megs.Length > 0)
                                {
                                    for (int i = megs.Length - 1; i >= 0; i--)
                                    {
                                        var mm = megs[i];
                                        Console.WriteLine($"收到一条离线{(mm.Key == 1 ? "单" : "群")}聊消息");
                                        Console.WriteLine($"消息id：{mm.Id}，发送者：{mm.SId}，接受者：{mm.RId},内容：{ mm.Body}");
                                    }
                                }
                            }
                        }
                        break;

                    case (byte)1:
                        Console.WriteLine($"收到一条单聊消息");
                        Console.WriteLine($"消息id：{msg.Id}，发送者：{msg.SId}，接受者：{msg.RId},内容：{ msg.Body}");

                        //发送回执给服务端：服务端会在5S内发送4次消息，接收消息后需要立即发送msg.Id的回执，并根据msg.Id对重复消息去重
                        //如5S内未发送回执，服务端会移除连接。
                        _client.Send(EncodingMsg(new IMPackage() { Key = 2, Id = msg.Id, SId = SID, Body = string.Empty, RId = 0 }));
                        break;

                    case (byte)2:
                        //通常情况下消息回执均不需要解析内容，framework452版本客户端无法正常接收body为空的包
                        //服务端会发送body内容是"0"的包，客户端直接遗弃即可
                        Console.WriteLine($"单聊消息收到回执");
                        break;

                    case (byte)3:
                        //此处Rid是群聊id，客户端收到消息后需要分发并展示到Rid对应的群聊中。
                        Console.WriteLine($"收到一条群聊消息");
                        Console.WriteLine($"消息id：{msg.Id}，发送者：{msg.SId}，接受者：{msg.RId},内容：{ msg.Body}");
                        _client.Send(EncodingMsg(new IMPackage() { Key = 4, Id = msg.Id, SId = SID, Body = string.Empty, RId = 0 }));
                        break;

                    case (byte)4:
                        Console.WriteLine($"群聊消息收到回执");
                        break;

                    case (byte)5:
                        //此处Rid是群聊id，客户端收到消息后需要分发并展示到Rid对应的群聊中。
                        Console.WriteLine($"收到一条无包体群聊消息，将从端口拉取消息内容");
                        Console.WriteLine($"消息id：{msg.Id}，发送者：{msg.SId}，接受者：{msg.RId},内容：{ msg.Body}");
                        _client.Send(EncodingMsg(new IMPackage() { Key = 6, Id = msg.Id, SId = SID, Body = string.Empty, RId = 0 }));
                        break;

                    case (byte)6:
                        Console.WriteLine($"无包体群聊消息收到回执");
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// 开始连接
        /// </summary>
        public async ValueTask Recevice()
        {
            _conn = await _client.ConnectAsync(_url);
            if (_conn)
            {
                Console.WriteLine("开始连接");
                //客户端本地需要维护好友信息（用户信息）、群聊信息，
                //接收消息时，需要根据用户id、群聊id匹配基本信息并展示
            }
        }

        public void Send(IMPackage model)
        {
            if (_conn)
            {
                _client.Send(EncodingMsg((model)));
            }
        }
    }
}
