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
/*
 * 
 * 加密解密QQ消息的工具类. QQ消息的加密算法是一个16次的迭代过程，并且是反馈的，每一个加密单元是8字节，输出也是8字节，密钥是16字节
 * 我们以prePlain表示前一个明文块，plain表示当前明文块，crypt表示当前明文块加密得到的密文块，preCrypt表示前一个密文块
 * f表示加密算法，d表示解密算法 那么从plain得到crypt的过程是: crypt = f(plain &circ; preCrypt) &circ;
 * prePlain 所以，从crypt得到plain的过程自然是 plain = d(crypt &circ; prePlain) &circ;
 * preCrypt 此外，算法有它的填充机制，其会在明文前和明文后分别填充一定的字节数，以保证明文长度是8字节的倍数
 * 填充的字节数与原始明文长度有关，填充的方法是:
 * 
 * <pre>
 * <code>
 * 
 *      ------- 消息填充算法 ----------- 
 *      a = (明文长度 + 10) mod 8
 *      if(a 不等于 0) a = 8 - a;
 *      b = 随机数 &amp; 0xF8 | a;              这个的作用是把a的值保存了下来
 *      plain[0] = b;         	          然后把b做为明文的第0个字节，这样第0个字节就保存了a的信息，这个信息在解密时就要用来找到真正明文的起始位置
 *      plain[1 至 a+2] = 随机数 &amp; 0xFF;    这里用随机数填充明文的第1到第a+2个字节
 *      plain[a+3 至 a+3+明文长度-1] = 明文; 从a+3字节开始才是真正的明文
 *      plain[a+3+明文长度, 最后] = 0;       在最后，填充0，填充到总长度为8的整数为止。到此为止，结束了，这就是最后得到的要加密的明文内容
 *      ------- 消息填充算法 ------------
 *   
 * </code>
 * </pre>
 * 
 * @author luma
 * @author notXX
 * 
 * @C# author abu
 */
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace QQ.NET.Utils
{
    public class Crypter
    {
        static MD5 MD5Instance = System.Security.Cryptography.MD5.Create();       
        /// <summary>
        /// 当前的明文块
        /// </summary>
        private byte[] plain;
        /// <summary>
        /// 前面的明文块
        /// </summary>
        private byte[] prePlain;
        /// <summary>
        /// 输出的密文或明文
        /// </summary>
        private byte[] outData;
        /// <summary>
        /// 当前加密的密文位置和上一次加密的密文块位置，它们相差8
        /// </summary>
        private int crypt, preCrypt;
        /// <summary>
        /// 当前处理的加密解密块的位置
        /// </summary>
        private int pos;
        /// <summary>
        /// 填充数（长度）
        /// </summary>
        private int padding;
        /// <summary>
        /// 密钥
        /// </summary>
        private byte[] key;
        /// <summary>
        /// 用于加密时，表示当前是否是第一个8字节块，因为加密算法是反馈的,
        /// 但是最开始的8个字节没有反馈可用，所以要标明这种情况。
        /// </summary>
        private bool header = true;
        /// <summary>
        /// 这个表示当前解密开始的位置，之所以要这么一个变量是为了避免当解密到最后时
        /// 后面已经没有数据，这时候就会出错，这个变量就是用来判断这种情况免得出错
        /// </summary>
        private int contextStart;
        /// <summary>
        /// 随机数对象
        /// </summary>
        private static Random random = new Random();
        /// <summary>
        /// 字节输出流
        /// </summary>
        private List<byte> baos;

        public Crypter()
        {
            baos = new List<byte>();
        }
        /// <summary>
        /// 解密
        /// 	<remark>abu 2008-02-15 </remark>
        /// </summary>
        /// <param name="inData">密文</param>
        /// <param name="offset">密文开始的位置</param>
        /// <param name="len">密文长度</param>
        /// <param name="k">密钥</param>
        /// <returns>明文</returns>
        public byte[] Decrypt(byte[] inData, int offset, int len, byte[] k)
        {
            crypt = preCrypt = 0;
            this.key = k;
            int count;
            byte[] m = new byte[offset + 8];
            // 因为QQ消息加密之后至少是16字节，并且肯定是8的倍数，这里检查这种情况
            if ((len % 8 != 0) || (len < 16))
            {
                return null;
            }
            // 得到消息的头部，关键是得到真正明文开始的位置，这个信息存在第一个字节里面，所以其用解密得到的第一个字节与7做与
            prePlain = Decipher(inData, offset);
            pos = prePlain[0] & 0x7;
            // 得到真正明文的长度
            count = len - pos - 10;
            //如果明文长度小于0，那肯定是出错了，比如传输错误之类的，返回
            if (count < 0) return null;
            // 这个是临时的preCrypt，和加密时第一个8字节块没有prePlain一样，解密时
            // 第一个8字节块也没有preCrypt，所有这里建一个全0的
            for (int i = offset; i < m.Length; i++)
                m[i] = 0;
            // 通过了上面的代码，密文应该是没有问题了，我们分配输出缓冲区
            outData = new byte[count];
            // 设置preCrypt的位置等于0，注意目前的preCrypt位置是指向m的，因为java没有指针，所以我们在后面要控制当前密文buf的引用
            preCrypt = 0;
            // 当前的密文位置，为什么是8不是0呢？注意前面我们已经解密了头部信息了，现在当然该8了
            crypt = 8;
            // 自然这个也是8
            contextStart = 8;
            // 加1，和加密算法是对应的
            pos++;
            // 开始跳过头部，如果在这个过程中满了8字节，则解密下一块
            // 因为是解密下一块，所以我们有一个语句 m = in，下一块当然有preCrypt了，我们不再用m了
            // 但是如果不满8，这说明了什么？说明了头8个字节的密文是包含了明文信息的，当然还是要用m把明文弄出来
            // 所以，很显然，满了8的话，说明了头8个字节的密文除了一个长度信息有用之外，其他都是无用的填充
            padding = 1;
            while (padding <= 2)
            {
                if (pos < 8)
                {
                    pos++;
                    padding++;
                }
                if (pos == 8)
                {
                    m = inData;
                    if (!Decrypt8Bytes(inData, offset, len)) return null;
                }
            }
            // 这里是解密的重要阶段，这个时候头部的填充都已经跳过了，开始解密
            // 注意如果上面一个while没有满8，这里第一个if里面用的就是原始的m，否则这个m就是in了
            int index = 0;
            while (count != 0)
            {
                if (pos < 8)
                {
                    outData[index] = (byte)(m[offset + preCrypt + pos] ^ prePlain[pos]);
                    index++;
                    count--;
                    pos++;
                }
                if (pos == 8)
                {
                    m = inData;
                    preCrypt = crypt - 8;
                    if (!Decrypt8Bytes(inData, offset, len)) return null;
                }
            }

            // 最后的解密部分，上面一个while已经把明文都解出来了，就剩下尾部的填充了，应该全是0
            // 所以这里有检查是否解密了之后是不是0，如果不是的话那肯定出错了，返回null
            for (padding = 1; padding < 8; padding++)
            {
                if (pos < 8)
                {
                    if ((m[offset + preCrypt + pos] ^ prePlain[pos]) != 0)
                    {
                        return null;
                    }
                    pos++;
                }
                if (pos == 8)
                {
                    m = inData;
                    preCrypt = crypt;
                    if (!Decrypt8Bytes(inData, offset, len)) return null;
                }
            }
            return outData;
        }

        /// <summary>
        /// 需要被解密的密文
        /// 	<remark>abu 2008-02-16 </remark>
        /// </summary>
        /// <param name="inData">密文</param>
        /// <param name="k">密钥</param>
        /// <returns>已解密的消息</returns>
        public byte[] Decrypt(byte[] inData, byte[] k)
        {
            return Decrypt(inData, 0, inData.Length, k);
        }

        /// <summary>
        /// 加密
        /// 	<remark>abu 2008-02-16 </remark>
        /// </summary>
        /// <param name="inData">明文字节数组</param>
        /// <param name="offset">开始加密的偏移</param>
        /// <param name="len">加密长度</param>
        /// <param name="k">密钥</param>
        /// <returns>密文字节数组</returns>
        public byte[] Encrypt(byte[] inData, int offset, int len, byte[] k)
        {
            plain = new byte[8];
            prePlain = new byte[8];
            pos = 1;
            padding = 0;
            crypt = preCrypt = 0;
            this.key = k;
            header = true;

            // 计算头部填充字节数
            pos = (len + 0x0A) % 8;
            if (pos != 0)
                pos = 8 - pos;
            // 计算输出的密文长度
            outData = new byte[len + pos + 10];
            // 这里的操作把pos存到了plain的第一个字节里面
            // 0xF8后面三位是空的，正好留给pos，因为pos是0到7的值，表示文本开始的字节位置
            plain[0] = (byte)((Rand() & 0xF8) | pos);
            // 这里用随机产生的数填充plain[1]到plain[pos]之间的内容
            for (int i = 1; i <= pos; i++)
            {
                plain[i] = (byte)(Rand() & 0xFF);
            }
            pos++;
            // 这个就是prePlain，第一个8字节块当然没有prePlain，所以我们做一个全0的给第一个8字节块
            for (int i = 0; i < 8; i++)
            {
                prePlain[i] = 0x0;
            }
            // 继续填充2个字节的随机数，这个过程中如果满了8字节就加密之
            padding = 1;
            while (padding <= 2)
            {
                if (pos < 8)
                {
                    plain[pos++] = (byte)(Rand() & 0xFF);
                    padding++;
                }
                if (pos == 8)
                {
                    Encrypt8Bytes();
                }
            }
            // 头部填充完了，这里开始填真正的明文了，也是满了8字节就加密，一直到明文读完
            int index = offset;
            while (len > 0)
            {
                if (pos < 8)
                {
                    plain[pos++] = inData[index++];
                    len--;
                }
                if (pos == 8)
                {
                    Encrypt8Bytes();
                }
            }
            // 最后填上0，以保证是8字节的倍数
            padding = 1;
            while (padding <= 7)
            {
                if (pos < 8)
                {
                    plain[pos++] = 0x0;
                    padding++;
                }
                if (pos == 8)
                {
                    Encrypt8Bytes();
                }
            }
            return outData;
        }
        /// <summary>
        /// 加密
        /// 	<remark>abu 2008-02-16 </remark>
        /// </summary>
        /// <param name="inData">需要加密的明文</param>
        /// <param name="k">密钥</param>
        /// <returns>密文字节数组</returns>
        public byte[] Encrypt(byte[] inData, byte[] k)
        {
            return Encrypt(inData, 0, inData.Length, k);
        }
        /// <summary>
        /// 加密一个8字节块
        /// 	<remark>abu 2008-02-15  </remark>
        /// </summary>
        /// <param name="inData">明文字节数组</param>
        /// <returns>密文字节数组</returns>
        private byte[] Encipher(byte[] inData)
        {
            // 迭代次数，16次
            int loop = 0x10;
            // 得到明文和密钥的各个部分
            uint y = Util.GetUInt(inData, 0, 4);
            uint z = Util.GetUInt(inData, 4, 4);
            uint a = Util.GetUInt(key, 0, 4);
            uint b = Util.GetUInt(key, 4, 4);
            uint c = Util.GetUInt(key, 8, 4);
            uint d = Util.GetUInt(key, 12, 4);
            // 这是算法的一些控制变量，为什么delta是0x9E3779B9呢？
            // 这个数是TEA算法的delta，实际是就是(sqr(5) - 1) * 2^31 (根号5，减1，再乘2的31次方)
            uint sum = 0;
            uint delta = 0x9E3779B9;
            //delta &= 0xFFFFFFFFL;
            // 开始迭代了，乱七八糟的，我也看不懂，反正和DES之类的差不多，都是这样倒来倒去
            while (loop-- > 0)
            {
                sum += delta;
                //sum &= 0xFFFFFFFFL;
                y += ((z << 4) + a) ^ (z + sum) ^ ((z >> 5) + b);
                // y &= 0xFFFFFFFFL;
                z += ((y << 4) + c) ^ (y + sum) ^ ((y >> 5) + d);
                //  z &= 0xFFFFFFFFL;
            }
            // 最后，我们输出密文，因为我用的long，所以需要强制转换一下变成int
            baos.Clear();
            WriteInt(y);
            WriteInt(z);
            return baos.ToArray();
        }

        /// <summary>
        /// 解密从offset开始的8字节密文
        /// 	<remark>abu 2008-02-15 </remark>
        /// </summary>
        /// <param name="inData">密文字节数组</param>
        /// <param name="offset">密文开始位置.</param>
        /// <returns>明文</returns>
        private byte[] Decipher(byte[] inData, int offset)
        {
            //迭代次数，16次
            int loop = 0x10;
            //得到密文和密钥的各个部分
            uint y = Util.GetUInt(inData, offset, 4);
            uint z = Util.GetUInt(inData, offset + 4, 4);
            uint a = Util.GetUInt(key, 0, 4);
            uint b = Util.GetUInt(key, 4, 4);
            uint c = Util.GetUInt(key, 8, 4);
            uint d = Util.GetUInt(key, 12, 4);
            // 算法的一些控制变量，sum在这里也有数了，这个sum和迭代次数有关系
            // 因为delta是这么多，所以sum如果是这么多的话，迭代的时候减减减，减16次，最后
            // 得到0。反正这就是为了得到和加密时相反顺序的控制变量，这样才能解密呀～～
            uint sum = 0xE3779B90;
            // sum &= 0xFFFFFFFFL;
            uint delta = 0x9E3779B9;
            // delta &= 0xFFFFFFFFL;
            // 迭代开始了， @_@
            while (loop-- > 0)
            {
                z -= ((y << 4) + c) ^ (y + sum) ^ ((y >> 5) + d);
                //  z &= 0xFFFFFFFFL;
                y -= ((z << 4) + a) ^ (z + sum) ^ ((z >> 5) + b);
                // y &= 0xFFFFFFFFL;
                sum -= delta;
                //  sum &= 0xFFFFFFFFL;
            }

            baos.Clear();
            WriteInt(y);
            WriteInt(z);
            return baos.ToArray();
        }

        /// <summary>解密
        /// 	<remark>abu 2008-02-16 </remark>
        /// </summary>
        /// <param name="inData">密文</param>
        /// <returns>明文</returns>
        private byte[] Decipher(byte[] inData)
        {
            return Decipher(inData, 0);
        }
        private void WriteInt(uint t)
        {
            baos.Add((byte)(t >> 24));
            baos.Add((byte)(t >> 16));
            baos.Add((byte)(t >> 8));
            baos.Add((byte)t);
        }
        /// <summary>
        /// 加密8字节
        /// 	<remark>abu 2008-02-16 </remark>
        /// </summary>
        private void Encrypt8Bytes()
        {
            // 这部分完成我上面所说的 plain ^ preCrypt，注意这里判断了是不是第一个8字节块，如果是的话，那个prePlain就当作preCrypt用
            for (pos = 0; pos < 8; pos++)
            {
                if (header)
                {
                    plain[pos] ^= prePlain[pos];
                }
                else
                    plain[pos] ^= outData[preCrypt + pos];
            }
            // 这个完成我上面说的 f(plain ^ preCrypt)
            byte[] crypted = Encipher(plain);
            // 这个没什么，就是拷贝一下，java不像c，所以我只好这么干，c就不用这一步了 在.NET里面还得测试
            Array.Copy(crypted, 0, outData, crypt, 8);
            // 这个完成了 f(plain ^ preCrypt) ^ prePlain，ok，下面拷贝一下就行了
            for (pos = 0; pos < 8; pos++)
            {
                outData[crypt + pos] ^= prePlain[pos];
            }
            Array.Copy(plain, 0, prePlain, 0, 8);
            // 完成了加密，现在是调整crypt，preCrypt等等东西的时候了
            preCrypt = crypt;
            crypt += 8;
            pos = 0;
            header = false;
        }
        /// <summary>
        /// 解密8个字节
        /// 	<remark>abu 2008-02-15 </remark>
        /// </summary>
        /// <param name="inData">密文字节数组.</param>
        /// <param name="offset">从何处开始解密.</param>
        /// <param name="len">密文的长度.</param>
        /// <returns>true表示解密成功</returns>
        private bool Decrypt8Bytes(byte[] inData, int offset, int len)
        {
            // 这里第一步就是判断后面还有没有数据，没有就返回，如果有，就执行 crypt ^ prePlain
            for (pos = 0; pos < 8; pos++)
            {
                if (contextStart + pos >= len)
                {
                    return true;
                }
                prePlain[pos] ^= inData[offset + crypt + pos];
            }
            // 好，这里执行到了 d(crypt ^ prePlain)
            prePlain = Decipher(prePlain);
            if (prePlain == null)
            {
                return false;
            }
            // 解密完成，最后一步好像没做？ 
            // 这里最后一步放到decrypt里面去做了，因为解密的步骤有点不太一样
            // 调整这些变量的值先
            contextStart += 8;
            crypt += 8;
            pos = 0;
            return true;
        }
        /// <summary>
        /// 这是个随机因子产生器，用来填充头部的，如果为了调试，可以用一个固定值
        /// 随机因子可以使相同的明文每次加密出来的密文都不一样
        /// 	<remark>abu 2008-02-16 </remark>
        /// </summary>
        /// <returns>随机因子</returns>        
        private int Rand()
        {
            return random.Next();
        }

        /// <summary>
        /// MD5加密
        /// 	<remark>abu 2008-02-18 </remark>
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static byte[] MD5(byte[] data)
        {
            return MD5Instance.ComputeHash(data);
        }
    }
}
