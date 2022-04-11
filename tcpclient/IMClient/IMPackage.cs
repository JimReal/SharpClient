using SuperSocket.ProtoBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMClient
{
    /// <summary>
    /// IM消息协议体
    /// 由头部和包体组成。
    /// </summary>
    public class IMPackage : IPackageInfo<byte>
    { 
        /// <summary>
        /// 实体类只需要定义命令字即可
        /// 0：鉴权
        /// 1：单聊发
        /// 2：单聊收
        /// 3：群聊发
        /// 4：群聊收
        /// 5：群聊无实体发
        /// 6：群聊无实体收
        /// 9：心跳
        /// </summary>
        public byte Key { get; set; } = TestClient.KEY;

        /// <summary>
        /// 长度为15的消息id，客户端发送时为0
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 用户Id长度固定为17位
        /// </summary>
        public long SId { get; set; } = TestClient.SID;

        /// <summary>
        /// 用户Id长度固定为17位
        /// </summary>
        public long RId { get; set; } = TestClient.RID;

        /// <summary>
        /// 包体(实际为json字符串)
        /// </summary>
        public string Body { get; set; }
    }

    public class IMSendPackage
    {
        /// <summary>
        /// 一个字节命令字
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 包体
        /// </summary>
        public string Body { get; set; }
    }

    public class LoopPackage
    {
        public IMPackage package { get; set; }
        /// <summary>
        /// 周期数
        /// </summary>
        public int CycleNum { get; set; }
    }
}
