<%@ Page Title="处理中" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Details.aspx.cs" Inherits="Mygod.Skylark.Task.Details" %>
<%@ Register TagPrefix="skylark" tagName="TaskViewer" src="/TaskViewer.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="Head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="Body" runat="server">
    <hr />
    <h2 class="center"><%=Task == null ? "未知任务" : TaskHelper.GetName(Task.Type) %> 状态</h2>
    <asp:ScriptManager runat="server" />
    <skylark:TaskViewer runat="server" ID="Viewer" />
</asp:Content>
