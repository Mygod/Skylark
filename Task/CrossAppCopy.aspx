<%@ Page Title="跨云雀复制中" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="CrossAppCopy.aspx.cs" Inherits="Mygod.Skylark.Task.CrossAppCopy" %>
<asp:Content ID="Content1" ContentPlaceHolderID="Head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <hr />
    <h2 class="center">跨云雀复制状态</h2>
    <asp:ScriptManager runat="server" />
    <asp:UpdatePanel runat="server">
        <ContentTemplate>
            <asp:Timer runat="server" Interval="1000" />
            <div>云雀源：　　　<%=Source %></div>
            <div>目标位置：　　<%=Target %></div>
            <div>当前状态：　　<%=Status %></div>
            <div>当前文件：　　<%=CurrentFile %></div>
            <div>已复制文件：　<%=FileCopied %></div>
            <div>已复制大小：　<%=SizeCopied %></div>
            <div>开始时间：　　<%=StartTime %></div>
            <div>花费时间：　　<%=SpentTime %></div>
            <div>结束时间：　　<%=EndingTime %></div>
            <div>错误信息：</div>
            <pre><%=Message %></pre>
        </ContentTemplate>
    </asp:UpdatePanel>
    <div class="center"><asp:Button runat="server" Text="多功能按钮" OnClick="CleanUp" /></div>
    <div>上面的多功能按钮有清理所有任务产生的临时文件（垃圾）以及终止所有当前正在执行的任务的功能。</div>
</asp:Content>
