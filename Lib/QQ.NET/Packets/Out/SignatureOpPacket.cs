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

using QQ.NET.Entities;
namespace QQ.NET.Packets.Out
{
    /// <summary>
    ///  * 个性签名操作请求包
    /// * 1. 头部
    /// * 2. 子命令，1字节
    /// * 
    /// * 根据2部分的不同
    /// * 为0x01时：
    /// * 3. 未知1字节
    /// * 4. 个性签名的字节长度，1字节
    /// * 5. 个性签名
    /// * 6. 尾部
    /// * 
    /// * 为0x00时，无后续内容
    /// * 3. 尾部
    /// * 
    /// * 为0x02时
    /// * 3. 未知的1字节
    /// * 4. 需要得到个性签名的QQ号数量，1字节
    /// * 5. QQ号，4字节
    /// * 6. 本地的个性签名修改时间，4字节
    /// * 7. 如果有更多QQ号，重复5-6部分
    /// * 8. 尾部 
    /// * 
    /// * 在得到好友的个性签名时，QQ的做法是对所有的QQ号排个序，每次最多请求33个。
    /// 	<remark>abu 2008-02-29 </remark>
    /// </summary>
    public class SignatureOpPacket : BasicOutPacket
    {
        public SignatureSubCmd SubCommand { get; set; }
        public string Signature { get; set; }
        public List<Signature> Signatures { get; set; }

        public SignatureOpPacket(QQUser user)
            : base(QQCommand.Signature_OP, true, user)
        {
            SubCommand = SignatureSubCmd.MODIFY;
            Signature = string.Empty;
        }
        public SignatureOpPacket(ByteBuffer buf, int length, QQUser user) : base(buf, length, user) { }
        public override string GetPacketName()
        {
            return "Signature Op Packet";
        }
        protected override void PutBody(ByteBuffer buf)
        {
            buf.Put((byte)SubCommand);
            switch (SubCommand)
            {
                case SignatureSubCmd.MODIFY:
                    buf.Put((byte)0x01);
                    byte[] b = Utils.Util.GetBytes(Signature);
                    buf.Put((byte)b.Length);
                    buf.Put(b);
                    break;
                case SignatureSubCmd.GET:
                    buf.Put((byte)0);
                    buf.Put((byte)Signatures.Count);
                    foreach (Signature sig in Signatures)
                    {
                        buf.PutInt(sig.QQ);
                        buf.PutInt(sig.ModifiedTime);
                    }
                    break;
            }
        }
    }
}
