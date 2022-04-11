using SuperSocket.ProtoBase;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace tcpclient
{
    /// <summary>
    /// 消息协议解析
    /// </summary>
    public class IMPackageEncoder : IPackageEncoder<IMPackage>
    {
        /// <summary>
        /// 编码消息,返回编码数据长度
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="pack"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public int Encode(IBufferWriter<byte> writer, IMPackage pack)
        {
            var bodyBuffer = Encoding.UTF8.GetBytes(pack.Body);
            writer.Write(new byte[] { pack.Key});//1
            writer.Write(BitConverter.GetBytes(pack.Id));//8
            writer.Write(BitConverter.GetBytes(pack.SId));//8
            writer.Write(BitConverter.GetBytes(pack.RId));//8
            writer.Write(BitConverter.GetBytes(bodyBuffer.Length));//4
            writer.Write(bodyBuffer);
            return 30 + bodyBuffer.Length;
        }
    }
}
