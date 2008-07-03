using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using QQ.NET;
using QQ.NET.Utils;
using QQ.NET.Events;
using QQ.NET.Packets;
using QQ.NET.Packets.In;
using QQ.NET.Packets.Out;


namespace QQMessage
{
    public partial class Form1 : Form
    {
        QQUser user;
        QQClient client;
        string msg = string.Empty;
        bool had;

        #region Normal
        public Form1()
        {
            InitializeComponent();
        }

        

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (client != null)
            {
                client.LoginManager.Logout();
            }                                                       
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.2081.org/labs.html");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (System.IO.File.Exists(System.IO.Directory.GetCurrentDirectory() + "\\log.txt"))
            {
                System.Diagnostics.Process.Start(System.IO.Directory.GetCurrentDirectory() + "\\log.txt");
            }
        }


        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            if (!had)
            {
                had = true;
                this.label5.Visible = true;
                msg = this.richTextBox1.Text.Trim()+"\n----------------------------------\n本消息由QQMessage发送\nhttp://www.2081.org/labs.html";
                user = new QQUser(Int32.Parse(this.textBox1.Text.Trim()), this.textBox2.Text);
                client = new QQ.NET.QQClient(user);
                user.IsUdp = true;
                client.LoginServerHost = "219.133.62.8";
                client.ConnectionManager.NetworkError += new EventHandler<ErrorEventArgs>(ConnectionManager_NetworkError);
                client.LoginManager.LoginFailed += new EventHandler<QQEventArgs<LoginReplyPacket, LoginPacket>>(LoginManager_LoginFailed);
                client.LoginManager.LoginSuccessed += new EventHandler<QQEventArgs<LoginReplyPacket, LoginPacket>>(LoginManager_LoginSuccessed);
                client.FriendManager.GetFriendListSuccessed += new EventHandler<QQEventArgs<GetFriendListReplyPacket, GetFriendListPacket>>(FriendManager_GetFriendListSuccessed);
                client.Error += new EventHandler<QQEventArgs<ErrorPacket, OutPacket>>(client_Error);
                client.MessageManager.ReceiveNormalIM += new EventHandler<QQEventArgs<ReceiveIMPacket, OutPacket>>(MessageManager_ReceiveNormalIM);

                client.Login();
            }
            else
            {
                MessageBox.Show("请关闭程序后，重新登录发送。");
            }


        }

        void MessageManager_ReceiveNormalIM(object sender, QQEventArgs<ReceiveIMPacket, OutPacket> e)
        {
            System.IO.File.AppendAllText(System.IO.Directory.GetCurrentDirectory() + "\\log.txt", DateTime.Now.ToString() + " " + e.InPacket.NormalHeader.Sender.ToString() + ":" + e.InPacket.NormalIM.Message + "\r\n");
            e.QQClient.MessageManager.SendIM(e.InPacket.NormalHeader.Sender, e.InPacket.NormalHeader.Sender.ToString() + "，你好！\n我正在使用群发器发送信息，请稍候联系。");
        }

        void client_Error(object sender, QQEventArgs<ErrorPacket, OutPacket> e)
        {
            MessageBox.Show("Error");
        }

        void FriendManager_GetFriendListSuccessed(object sender, QQEventArgs<GetFriendListReplyPacket, GetFriendListPacket> e)
        {
            if (e.InPacket.Finished)
            {
                int l = e.QQClient.QQUser.Friends.Count;
                foreach (KeyValuePair<int, FriendInfo> f in e.QQClient.QQUser.Friends)
                {
                    client.MessageManager.SendIM(f.Key, msg);
                    System.Threading.Thread.Sleep(1000);

                }
                MessageBox.Show("发送完成。");
            }
        }

        void LoginManager_LoginSuccessed(object sender, QQEventArgs<LoginReplyPacket, LoginPacket> e)
        {
            e.QQClient.FriendManager.GetFriendList();
        }

        void LoginManager_LoginFailed(object sender, QQEventArgs<LoginReplyPacket, LoginPacket> e)
        {
            MessageBox.Show(e.InPacket.ReplyMessage);
        }

        void ConnectionManager_NetworkError(object sender, ErrorEventArgs e)
        {
            MessageBox.Show(e.Exception.Message);
        }

        
    }
}
