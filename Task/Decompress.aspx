<%@ Page Title="解压缩中" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Decompress.aspx.cs" Inherits="Mygod.Skylark.Task.Decompress" %>
<asp:Content ID="Content1" ContentPlaceHolderID="Head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <hr />
    <h2 class="center">解压缩状态</h2>
    <asp:ScriptManager runat="server" />
    <asp:UpdatePanel runat="server">
        <ContentTemplate>
            <asp:Timer runat="server" Interval="1000" />
            <div>压缩包地址：　<%=Archive %></div>
            <div>当前状态：　　<%=Status %></div>
            <div>目标目录：　　<%=TargetDirectory %></div>
            <div>当前文件：　　<%=CurrentFile %></div>
            <div>开始时间：　　<%=StartTime %></div>
            <div>花费时间：　　<%=SpentTime %></div>
            <div>预计剩余时间：<%=RemainingTime %></div>
            <div>预计结束时间：<%=EndingTime %></div>
            <div class="progress-bar">
                <%-- ReSharper disable UnexpectedValue --%>
                <div class="bar" style="width: <%=Percentage %>%;"></div>
                <%-- ReSharper restore UnexpectedValue --%>
            </div>
        </ContentTemplate>
    </asp:UpdatePanel>
    <div class="center"><asp:Button runat="server" Text="多功能按钮" OnClick="CleanUp" /></div>
    <div>上面的多功能按钮有清理所有任务产生的临时文件（垃圾）以及终止所有当前正在执行的任务的功能。</div>
</asp:Content>
