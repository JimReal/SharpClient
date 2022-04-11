using SuperSocket.ProtoBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMClient
{
    /// <summary>
    /// 消息处理管道
    /// </summary>
    public class IMPipelineFilter : FixedHeaderReceiveFilter<IMPackage>
    {
        public IMPipelineFilter() : base(29)//传输头部长度给基类
        {
        }

        public override IMPackage ResolvePackage(IBufferStream bufferStream)
        {
            IMPackage data = new IMPackage();

            data.Key = (byte)bufferStream.ReadByte();//命令字

            data.Id = bufferStream.ReadInt64(true); //id

            data.SId = bufferStream.ReadInt64(true);//发送者id

            data.RId = bufferStream.ReadInt64(true);//接受者id

            var lenth = bufferStream.ReadInt32(true);//包长

            data.Body = bufferStream.ReadString(lenth, Encoding.UTF8);
            return data;
        }

        /// <summary>
        /// 实现获取包体长度的方法
        /// </summary>
        /// <param name="bufferStream"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        protected override int GetBodyLengthFromHeader(IBufferStream bufferStream, int length)
        {
            bufferStream.Skip(1);//跳过命令字
            bufferStream.Skip(8);//跳过Id
            bufferStream.Skip(8);//跳过Sid
            bufferStream.Skip(8);//跳过Rid
            var lenth = bufferStream.ReadInt32(true);
            return lenth;
        }
    }
}
