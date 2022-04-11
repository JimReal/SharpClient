using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Configuration;

namespace IMClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            //Console.WriteLine("请输入长整形用户Id");
            //if (!Int64.TryParse(Console.ReadLine(), out long sId))
            //{
            //    Console.WriteLine("输入异常,即将退出");
            //    return;
            //}
            TestClient.SID = Convert.ToInt64(ConfigurationManager.AppSettings["id"]);
            //TestClient.SID = sId;
            TestClient.KEY = 1;
            //
            ////登陆服务器，必须同步处理
            //HttpClient HttpClient = new HttpClient();
            //var res = await HttpClient.GetAsync($"http://localhost:5000/api/auth/login?id={sId}");
            //res.EnsureSuccessStatusCode();
            //Console.WriteLine("IM服务器登陆成功");

            //启动侦听
            TestClient client = new TestClient(ConfigurationManager.AppSettings["ip"], Convert.ToInt32(ConfigurationManager.AppSettings["imPort"]));
            _ = Task.Factory.StartNew(() => { client.Recevice(); }, TaskCreationOptions.LongRunning);

            Console.WriteLine("\r\n");
            while (true)
            {
                await Task.Delay(500);
                Console.WriteLine("\r\n请输入 命令类型：接收方id：消息");
                var data = Console.ReadLine();
                var arr = data.Split('\uff1a');
                if (arr.Length == 2)
                {
                    TestClient.RID = Convert.ToInt64(arr[0]);
                }
                else if (arr.Length == 3)
                {
                    TestClient.KEY = Convert.ToByte(arr[0]);
                    TestClient.RID = Convert.ToInt64(arr[1]);
                }

                client.Send(new IMPackage() { Body = arr.Length > 1 ? arr[arr.Length - 1] : data });
                //单聊发送时，key=1。
                //群聊发送时key=3,群聊消息内容太长时key=5
            }
        }
    }
}
