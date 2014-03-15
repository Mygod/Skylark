<%@ Page Title="升级" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Mygod.Skylark.Update.Default" %>
<asp:Content ID="Content1" ContentPlaceHolderID="Head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="Body" runat="server">
    <h2 class="center">升级</h2>
    <% if (Mygod.Net.WebsiteManager.UpdateAvailable)
       { %>
    <div>新版 云雀™ 现在已可用。</div>
    <% }
       else
       { %>
    <div>恭喜您，您的 云雀™ 是最新版！</div>
    <% } %>
    <div class="center"><asp:Button ID="UpdateButton" runat="server" Text="下载并部署最新版" OnClick="Update" /></div>
</asp:Content>
