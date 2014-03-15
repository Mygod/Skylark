<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Forbidden.aspx.cs" Inherits="Mygod.Skylark.Forbidden" %>
<asp:Content ID="Content2" ContentPlaceHolderID="Body" runat="server">
    <h2 class="center"><%=Head %></h2>
    <h3>天空一声巨响，你的访问被无情地拒绝。</h3>
    <div>经专家组分析，原因可能如下：</div>
    <ul>
        <li>
            <div>你正在试图访问 云雀™ 的最高机密！</div>
            <div>访问 云雀™ 的最高机密是不被允许的，如果你只是想执行一些奇怪的操作，请看看<a href="http://mygod.tk/skylark/api/">开发者 API 指南</a>。</div>
        </li>
        <li>
            <div>您访问的并非最高机密，但是由于您职位太小，权限不够。</div>
            <div>忘记登录了？点击页面上方的 [修改] 登录您的账号。</div>
        </li>
    </ul>
</asp:Content>