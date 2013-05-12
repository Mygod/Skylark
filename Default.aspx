<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Mygod.Skylark.Default" %>
<%@ Import Namespace="Mygod.Skylark" %>
<asp:Content ID="Content1" ContentPlaceHolderID="Head" runat="server">
    <link href="/plugins/fineuploader/fineuploader-3.5.0.css" rel="stylesheet" />
    <style type="text/css">
        .hidden {
            display: none;
        }
    </style>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div><a href="?">根目录</a><% WritePath(); %></div>
    <input type="hidden" id="Hidden" runat="server" ClientIDMode="Static" />
    <% if (InfoDirectory.Exists)
       { %>
    <div id="manual-fine-uploader"></div>
    <script src="plugins/fineuploader/jquery.fineuploader-3.5.0.min.js"></script>
    <script type="text/javascript">
        function NewFolder() {
            var result = prompt("请输入文件夹名：", "");
            if (result) $("#Hidden").val(result);
            return !!result;
        }
        function OfflineDownload() {
            var result = prompt("请输入要下载的链接：", "");
            if (result) $("#Hidden").val(result);
            return !!result;
        }
        function DeleteConfirm() {
            return $("input:checked").length > 0 && confirm("确定要删除吗？此操作没有后悔药吃。");
        }
        var selectAll = false;
        function InvertSelection() {
            var checked = $("input:checkbox:checked");
            $("input:checkbox:not(:checked)").prop("checked", true);
            checked.prop("checked", false);
        }
        function Rename(oldName) {
            var result = prompt("请输入新的名字：", oldName);
            if (result && result != oldName) {
                $("#Hidden").val(result);
                return true;
            }
            return false;
        }
        var manualuploader = new qq.FineUploader({
            element: $('#manual-fine-uploader')[0],
            request: {
                endpoint: '/Upload?<%=RelativePath.Replace("'", @"\'") %>'
            },
            autoUpload: true,
            text: {
                uploadButton: '上传文件'
            }
        });
    </script>
    <div>
        <asp:LinkButton runat="server" Text="[新建文件夹]" OnClick="NewFolder" OnClientClick="return NewFolder();" />
        <asp:LinkButton runat="server" Text="[新建离线下载任务]" OnClick="OfflineDownload" OnClientClick="return OfflineDownload();" />
        <a href="javascript:$('input:checkbox').prop('checked', selectAll = !selectAll);">[全选]</a>
        <a href="javascript:InvertSelection();">[反选]</a>
        <asp:LinkButton runat="server" Text="[删除选中项]" OnClick="Delete" OnClientClick="return DeleteConfirm();" />
    </div>
    <table>
        <asp:Repeater runat="server" ID="DirectoryList" OnItemCommand="DirectoryCommand">
            <ItemTemplate>
                <tr>
                    <td style="width: 65px;">
                        <asp:CheckBox ID="Check" runat="server" Text=' <img src="/Image/Directory.png" alt="目录" />' />
                    </td>
                    <td><a href="?/<%#FileHelper.Combine(RelativePath, Eval("Name").ToString()) %>"><%#Eval("Name") %></a></td>
                    <td>
                        <input type="hidden" id="Hidden" runat="server" value='<%#Eval("Name") %>' />
                        <asp:LinkButton runat="server" Text="[重命名]" CommandName="Rename"
                                        OnClientClick='<%#string.Format("return Rename(\"{0}\");", Eval("Name"))%>' />
                    </td>
                </tr>
            </ItemTemplate>
        </asp:Repeater>
        <asp:Repeater runat="server" ID="FileList" OnItemCommand="FileCommand">
            <ItemTemplate>
                <tr>
                    <td style="width: 65px;">
                        <asp:CheckBox ID="Check" runat="server" Text='<%#FileHelper.IsReady(
                            Server.GetDataPath(FileHelper.Combine(RelativePath, Eval("Name").ToString())))
                                ? " <img src=\"/Image/File.png\" alt=\"文件\" />"
                                : " <img src=\"/Image/Busy.png\" alt=\"文件 (处理中)\" />" %>' />
                    </td>
                    <td><a href="?/<%#FileHelper.Combine(RelativePath, Eval("Name").ToString()) %>"><%#Eval("Name") %></a></td>
                    <td>
                        <input type="hidden" id="Hidden" runat="server" value='<%#Eval("Name") %>' />
                        <asp:LinkButton runat="server" Text="[重命名]" CommandName="Rename"
                                        OnClientClick='<%#string.Format("return Rename(\"{0}\");", Eval("Name"))%>' />

                    </td>
                </tr>
            </ItemTemplate>
        </asp:Repeater>
    </table>
    <% }
       else if (InfoFile.Exists)
       {
           var mime = FileHelper.GetDefaultMime(Server.GetDataPath(RelativePath)); %>
    <script type="text/javascript">
        function ModifyMime() {
            var oldValue = "<%=mime %>";
            var result = prompt("请输入新的MIME类型：", oldValue);
            if (result && result != oldValue) {
                $("#Hidden").val(result);
                return true;
            }
            return false;
        }
        function StartCustomMime() {
            window.open("/View/" + $("#custom-mime")[0].value + "/?<%=RelativePath %>");
        }
    </script>
    <div>大小：　　<%=Helper.GetSize(InfoFile.Length) %></div>
    <div>修改日期：<%=InfoFile.LastWriteTime %></div>
    <div>默认类型：<%=GetMimeType(mime) %>
        <asp:LinkButton runat="server" Text="[修改]" OnClick="ModifyMime" OnClientClick="return ModifyMime();" />
    </div>
    <div><a href="/View/?<%=RelativePath %>" target="_blank">使用默认类型查看</a>　<a href="/Download/?<%=RelativePath %>" target="_blank">下载链接</a>　<a href="javascript:StartCustomMime();">使用自定义MIME类型查看</a></div>
    <div>自定义MIME：<input type="text" id="custom-mime" value="<%=mime %>" /></div>
    <% }
       else
       {
           Response.StatusCode = 404;
           Response.Write("<div>您要查找的东西已消失！</div>");
       } %>
</asp:Content>
