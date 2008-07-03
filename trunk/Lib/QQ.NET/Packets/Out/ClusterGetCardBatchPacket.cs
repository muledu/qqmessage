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
    ///  * 批量得到群名片中的真实姓名
    /// * 1. 头部
    /// * 2. 命令，1字节，0x0F
    /// * 3. 群内部ID，4字节
    /// * 4. 未知的4字节，全0
    /// * 5. 起始记录位置，4字节，从0开始，为1表示从第二条记录开始得到
    /// * 6. 尾部
    /// 	<remark>abu 2008-02-28 </remark>
    /// </summary>
    public class ClusterGetCardBatchPacket : ClusterCommandPacket
    {
        public int Start { get; set; }
        public ClusterGetCardBatchPacket(ByteBuffer buf, int length, QQUser user) : base(buf, length, user) { }
        public ClusterGetCardBatchPacket(QQUser user)
            : base(user)
        {
            SubCommand = ClusterCommand.GET_CARD_BATCH;
        }
        public override string GetPacketName()
        {
            return "Cluster Get Card Batch Packet";
        }
        protected override void PutBody(ByteBuffer buf)
        {
            buf.Put((byte)SubCommand);
            buf.PutInt(ClusterId);
            buf.PutInt(0);
            buf.PutInt(Start);
        }
    }
}
