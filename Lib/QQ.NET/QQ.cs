/**
 * 版权声明：QQ.NET是基于LumaQQ分析的QQ协议，将其部分代码进行修改和翻译为.NET版本。本人没有对其核心协议进行改动，
 * 也没有与腾讯公司的QQ软件有直接联系。
 * 使用此开发包开发的基于QQ协议的相关应用程序，本人不承担任何法律责任。 
 * 作者：阿不
 * 博客：http://hjf1223.cnblogs.com
 * LumaQQ：http://lumaqq.linuxsir.org 
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace QQ.NET
{
    /// <summary>
    ///  * 定义一些QQ用到的常量，常量的命名方式经过调整，统一为
    ///  * QQ_[类别]_[名称]
    ///  * 
    ///  * 比如表示长度的常量，为QQ_LENGTH_XXXXX
    ///  * 表示最大值的常量，为QQ_MAX_XXXX
    /// 	<remark>abu 2008-02-16 </remark>
    /// </summary>
    public static class QQ
    {
        /// <summary>
        /// 包最大大小
        /// </summary>
        public const int QQ_MAX_PACKET_SIZE = 65535;

        /// <summary>
        /// 得到用户信息的回复包字段个数
        /// </summary>
        public const int QQ_COUNT_GET_USER_INFO_FIELD = 37;        
    }

    /// <summary>
    /// 联系方法的可见类型
    /// </summary>
    public enum OpenContact
    {
        /// <summary>
        /// 完全公开 
        /// </summary>
        Open = 0,
        /// <summary>
        /// 仅好友可见
        /// </summary>
        Friends =1,
        /// <summary>
        /// 完全保密 
        /// </summary>
        Close = 2
    }

    /// <summary>
    /// 认证类型，加一个人为好友时是否需要验证等等
    /// </summary>
    public enum AuthType : byte
    {
        /// <summary>
        /// 不需认证
        /// </summary>
        No = 0,
        /// <summary>
        /// 需要认证
        /// </summary>
        Need = 1,
        /// <summary>
        /// 对方拒绝加好友
        /// </summary>
        Reject = 2
    }
}
