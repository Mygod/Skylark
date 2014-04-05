<%@ Page Title="" Language="C#" MasterPageFile="~/FileSystem.master" AutoEventWireup="true" CodeBehind="Edit.aspx.cs" Inherits="Mygod.Skylark.Edit" %>
<asp:Content ID="Content1" ContentPlaceHolderID="Head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="Body" runat="server">
    <section>
        <textarea runat="server" ID="TextArea" class="stretch" style="height: 500px;"></textarea>
        <div class="center">
            <asp:Button runat="server" Text="保存并返回" OnClick="Save" />
        </div>
    </section>
</asp:Content>
