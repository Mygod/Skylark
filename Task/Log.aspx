<%@ Page Title="人事部记过处分记录" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Log.aspx.cs" Inherits="Mygod.Skylark.Task.Log" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="Mygod.Skylark" %>
<asp:Content ID="Content1" ContentPlaceHolderID="Head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <h2 class="center">人事部记过处分记录</h2>
    <pre><% var path = FileHelper.GetDataPath("error.log");
            if (File.Exists(path)) Response.WriteFile(path); %></pre>
</asp:Content>
