<%@ Page Title="新建离线下载任务" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Mygod.Skylark.Offline.Default" %>
<asp:Content ID="Content1" ContentPlaceHolderID="Head" runat="server">
    <style type="text/css">
        .link-box {
            width: 100%;
            height: 200px;
        }
    </style>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <h2 class="center">新建离线下载任务</h2>
    <div>离线下载，即将目标服务器上的文件弄到云雀™上再让你下载的简单伎俩。（当你和服务器之间有厚障壁时这个玩意儿很有用，因此推荐离线下载国外的地址，也可以提升速度）</div>
    <div>要新建任务只需把地址（一个或多个）粘贴到下面那个矩形中，然后按下那个神奇的按钮即可。</div>
    <div>支持协议：http https thunder flashget qqdl fs2you</div>
    <p><asp:TextBox ID="LinkBox" runat="server" TextMode="MultiLine" CssClass="link-box"></asp:TextBox></p>
    <div class="center"><asp:Button runat="server" Text="神奇的按钮！" OnClick="Submit" /></div>
    <div>点了神奇的按钮后就会回到刚才的目录。如果没有出现你创建的任务，尝试刷新一下。</div>
    <div>反复刷新也没有用？研究表明有时候会有这种奇怪的情况，此时只需再次启动任务即可。</div>
    <div>&nbsp;</div>
    <div>请注意离线下载以下格式的文件方法比较特殊，请按照下面说的进行：（否则可能会失败）</div>
    <ul>
        <li><a href="/NiGuan/">你管（YouTube）视频</a>。</li>
        <li>
            http://www.mediafire.com/?<asp:TextBox runat="server" ID="MediaFireBox" Width="400px" />
            <asp:Button runat="server" OnClick="MediaFire" Text="下载MediaFire网盘文件" />
        </li>
    </ul>
</asp:Content>
