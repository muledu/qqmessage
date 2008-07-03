﻿#region 版权声明
/**
 * 版权声明：QQ.NET是基于LumaQQ分析的QQ协议，将其部分代码进行修改和翻译为.NET版本，并且继续使用LumaQQ的开源协议。
 * 本人没有对其核心协议进行改动， 也没有与腾讯公司的QQ软件有直接联系，请尊重LumaQQ作者Luma的著作权和版权声明。
 * 
 * 作者：阿不
 * 博客：http://hjf1223.cnblogs.com
 * Email：hjf1223@gmail.com
 * LumaQQ：http://lumaqq.linuxsir.org 
 * LumaQQ - Java QQ Client
 * 
 * Copyright (C) 2004 luma <stubma@163.com>
 * 
 * LumaQQ - For .NET QQClient
 * Copyright (C) 2008 阿不<hjf1223@gmail.com>
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
 */
#endregion
using System;
using System.Collections.Generic;
using System.Text;

namespace QQ.NET.Packets.Out
{
    /// <summary>
    ///  * 讨论组操作请求：
    /// * 1. 头部
    /// * 2. 命令类型，1字节，0x36
    /// * 3. 子命令，1字节
    /// * 4. 根据3的不同，有：
    /// * 		i. 3为0x02(得到讨论组)时，4为群内部ID，4字节
    /// * 		ii. 3为0x01(得到多人对话)时，这里为0，4字节
    /// * 5. 尾部
    /// 	<remark>abu 2008-02-29 </remark>
    /// </summary>
    public class ClusterSubClusterOpPacket : ClusterCommandPacket
    {
        public ClusterSubCmd OpByte { get; set; }
        public ClusterSubClusterOpPacket(QQUser user)
            : base(user)
        {
            SubCommand = ClusterCommand.SUB_CLUSTER_OP;
        }
        public ClusterSubClusterOpPacket(ByteBuffer buf, int length, QQUser user) : base(buf, length, user) { }
        public override string GetPacketName()
        {
            return "Cluster Subject Op Packet";
        }
        protected override void PutBody(ByteBuffer buf)
        {
            // 命令类型
            buf.Put((byte)SubCommand);
            // 子命令
            buf.Put((byte)OpByte);
            switch (OpByte)
            {
                case ClusterSubCmd.GET_SUBJECT_LIST:
                    buf.PutInt(ClusterId);
                    break;
                case ClusterSubCmd.GET_DIALOG_LIST:
                    buf.PutInt(0);
                    break;
            }
        }
    }
}
