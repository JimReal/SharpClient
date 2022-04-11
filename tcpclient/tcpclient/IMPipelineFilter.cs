using SuperSocket.ProtoBase;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tcpclient
{
    /// <summary>
    /// 消息处理管道
    /// </summary>
    public class IMPipelineFilter : FixedHeaderPipelineFilter<IMPackage>
    {
        public IMPipelineFilter() : base(29)//传输头部长度给基类
        {
        }

        /// <summary>
        /// 实现获取包体长度的方法,即截取第2到5个字节
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        protected override int GetBodyLengthFromHeader(ref ReadOnlySequence<byte> buffer)
        {
            var reader = new SequenceReader<byte>(buffer);
            reader.Advance(1);//跳过命令字
            reader.Advance(8);//跳过Id
            reader.Advance(8);//跳过Sid
            reader.Advance(8);//跳过Rid
            reader.TryReadLittleEndian(out int bodyLength);
            return bodyLength;
        }

        protected override IMPackage DecodePackage(ref ReadOnlySequence<byte> buffer)
        {
            IMPackage data = new();
            var reader = new SequenceReader<byte>(buffer);

            reader.TryRead(out byte key);//命令字
            data.Key = key;

            reader.TryReadLittleEndian(out long id);//发送者id
            data.Id = id;

            reader.TryReadLittleEndian(out long sid);//发送者id
            data.SId = sid;

            reader.TryReadLittleEndian(out long rid);//接受者id
            data.RId = rid;

            reader.Advance(4);//跳过包长(字段)所占用的字节位置,剩下的都是包体
            data.Body = reader.ReadString();
            return data;
        }
    }
}
