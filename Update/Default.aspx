<%@ Page Title="升级" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs"
     Inherits="Mygod.Skylark.Update.Default" %>
<%@ Import Namespace="System.Reflection" %>
<asp:Content ID="Content1" ContentPlaceHolderID="Head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="Body" runat="server">
    <h2 class="center">升级</h2>
    <% if (string.IsNullOrWhiteSpace(new WebClient().DownloadString("https://mygod.be/product/update/" +
            Assembly.GetExecutingAssembly().GetName().Version.Revision + '/')))
        { %>
    <div>恭喜您，您的 云雀™ 是最新版！不过你还是可以部署一次最新版。</div>
    <% }
       else
       { %>
    <div>新版 云雀™ 现在已可用，请尽快升级。</div>
    <% } %>
	<small>P.S. 不是所有的更新都会在这里显示，只有重大、稳定的更新才会显示，即不重大的（小 BUG 修复或小改进）或不稳定的（测试版）不会在这里显示。你可以<a href="https://github.com/Mygod/Skylark/commits/master">点击这里看看最近的更新日志</a>，那里通常会有这里不提醒的更新。如果你是个更新狂魔，在 GitHub 上 watch 这个 repo 可在第一时间获得 云雀™ 更新的最新动态。</small>
</asp:Content>
