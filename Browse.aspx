<%@ Page Title="" Language="C#" MasterPageFile="~/FileSystem.Master" AutoEventWireup="true" CodeBehind="Browse.aspx.cs" Inherits="Mygod.Skylark.Browse" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="Mygod.Skylark" %>
<asp:Content ID="Content1" ContentPlaceHolderID="Head" runat="server">
    <link href="/plugins/fineuploader/fineuploader-3.5.0.css" rel="stylesheet" />
    <style type="text/css">
        .hidden {
            display: none;
        }
    </style>
    <script type="text/javascript">
        function pickFolderCore() {
            uriParser.exec(location.href);
            var result = prompt("请输入目标文件夹：（重名文件/文件夹将被跳过）", unescape(RegExp.$3));
            if (result) $("#Hidden").val(result);
            return !!result;
        }
        function pickFolder() {
            if ($("input:checked").length == 0) return true;
            return pickFolderCore();
        }
    </script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <asp:ScriptManager runat="server" />
    <input type="hidden" id="Hidden" runat="server" ClientIDMode="Static" />
    <asp:MultiView runat="server" ID="Views">
        <asp:View runat="server" ID="DirectoryView">
            <div id="manual-fine-uploader"></div>
            <div>（支持多选、拖放，自动覆盖已有文件）</div>
            <script src="/plugins/fineuploader/jquery.fineuploader-3.5.0.min.js"></script>
            <script type="text/javascript">
                function newFolder() {
                    var result = prompt("请输入文件夹名：", "");
                    if (result) $("#Hidden").val(result);
                    return !!result;
                }
                function deleteConfirm() {
                    return $("input:checked").length > 0 && confirm("确定要删除吗？此操作没有后悔药吃。");
                }
                var selectAll = false;
                function doSelectAll() {
                    $('input:checkbox').prop('checked', selectAll = !selectAll);
                }
                function invertSelection() {
                    var checked = $("input:checkbox:checked");
                    $("input:checkbox:not(:checked)").prop("checked", true);
                    checked.prop("checked", false);
                }
                function showCompressConfig() {
                    $("#compress-config").show();
                }
                function getDownloadLink() {
                    var array = $("input:checkbox:checked").parent().parent().find("input:hidden");
                    var result = "";
                    uriParser.exec(location.href);
                    var prefix = (RegExp.$1 + "/Download/" + RegExp.$3).replace("\\", "/");
                    while (prefix[prefix.length - 1] == "/") prefix = prefix.substr(0, prefix.length - 1);
                    prefix = prefix + "/";
                    for (var i = 0; i < array.length; i++) result += prefix + array[i].value + "\r\n";
                    var box = $("#running-result");
                    box.val(result);
                    box.select();
                    box.focus();
                    box.show();
                }
                function rename(oldName) {
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
                        endpoint: '/Upload/<%=RelativePath.Replace("'", @"\'") %>'
                    },
                    autoUpload: true,
                    text: {
                        uploadButton: '上传文件'
                    }
                });
            </script>
            <div>
                <asp:LinkButton runat="server" Text="[新建文件夹]" OnClick="NewFolder" OnClientClick="return newFolder();" />
                <a href="/Offline/New/<%=RelativePath %>">[新建离线下载任务]</a>
                <a href="javascript:doSelectAll();">[全选]</a>
                <a href="javascript:invertSelection();">[反选]</a>
                <a href="javascript:showCompressConfig();">[压缩选中项]</a>
                <a href="javascript:getDownloadLink();">[生成选中项的下载链接]</a>
                <asp:LinkButton runat="server" Text="[移动到]" OnClick="Move" OnClientClick="return pickFolder();" />
                <asp:LinkButton runat="server" Text="[复制到]" OnClick="Copy" OnClientClick="return pickFolder();" />
                <asp:LinkButton runat="server" Text="[删除选中项]" OnClick="Delete" OnClientClick="return deleteConfirm();" />
            </div>
            <div id="running-result" style="display: none;">
                <a href="javascript:hideParent();">[隐藏]</a><br />
                <textarea style="width: 100%; height: 100px;"></textarea>
            </div>
            <div id="compress-config" style="display: none;">
                <a href="javascript:hideParent();">[隐藏]</a>
                <asp:DropDownList ID="CompressionLevelList" runat="server">
                    <asp:ListItem Value="Ultra">最小</asp:ListItem>
                    <asp:ListItem Value="High">较小</asp:ListItem>
                    <asp:ListItem Value="Normal">普通</asp:ListItem>
                    <asp:ListItem Value="Low">较快</asp:ListItem>
                    <asp:ListItem Value="Fast">最快</asp:ListItem>
                    <asp:ListItem Value="None">不压缩</asp:ListItem>
                </asp:DropDownList>
                <asp:TextBox ID="ArchiveFilePath" runat="server" Width="300px" />
                <asp:LinkButton runat="server" Text="[压缩到指定路径]" OnClick="Compress" />
                （请以 <a href="http://zh.wikipedia.org/wiki/7-Zip">7z</a>,
                 <a href="http://zh.wikipedia.org/wiki/ZIP格式">zip</a> 或
                 <a href="http://zh.wikipedia.org/wiki/Tar">tar</a> 作为扩展名）
            </div>
            <table>
                <asp:Repeater runat="server" ID="DirectoryList" OnItemCommand="DirectoryCommand">
                    <ItemTemplate>
                        <tr>
                            <td class="nowrap">
                                <asp:CheckBox ID="Check" runat="server" Text=' <img src="/Image/Directory.png" alt="目录" />' />
                            </td>
                            <td><a href="<%#Eval("Name") %>/"><%#Eval("Name") %></a></td>
                            <td class="nowrap">&lt;DIR&gt;</td>
                            <td class="nowrap"><%#File.GetLastWriteTimeUtc(Server.GetFilePath(
                                               FileHelper.Combine(RelativePath, Eval("Name").ToString()))).ToChineseString() %></td>
                            <td class="nowrap">
                                <input type="hidden" id="Hidden" runat="server" value='<%#Eval("Name") %>' />
                                <asp:LinkButton ID="LinkButton4" runat="server" Text="[重命名]" CommandName="Rename"
                                                OnClientClick='<%#string.Format("return rename(\"{0}\");", Eval("Name"))%>' />
                            </td>
                        </tr>
                    </ItemTemplate>
                </asp:Repeater>
                <asp:Repeater runat="server" ID="FileList" OnItemCommand="FileCommand">
                    <ItemTemplate>
                        <tr>
                            <td class="nowrap">
                                <asp:CheckBox ID="Check" runat="server" Text='<%#FileHelper.IsReady(
                                    Server.GetDataPath(FileHelper.Combine(RelativePath, Eval("Name").ToString())))
                                        ? " <img src=\"/Image/File.png\" alt=\"文件\" />"
                                        : " <img src=\"/Image/Busy.png\" alt=\"文件 (处理中)\" />" %>' />
                            </td>
                            <td><a href="<%#Eval("Name") %>"><%#Eval("Name") %></a></td>
                            <td class="nowrap"><%#Helper.GetSize(Server.GetFileSize(
                                               FileHelper.Combine(RelativePath, Eval("Name").ToString()))) %></td>
                            <td class="nowrap"><%#File.GetLastWriteTimeUtc(Server.GetFilePath(
                                               FileHelper.Combine(RelativePath, Eval("Name").ToString()))).ToChineseString() %></td>
                            <td class="nowrap">
                                <input type="hidden" id="Hidden" runat="server" value='<%#Eval("Name") %>' />
                                <asp:LinkButton ID="LinkButton5" runat="server" Text="[重命名]" CommandName="Rename"
                                                OnClientClick='<%#string.Format("return rename(\"{0}\");", Eval("Name"))%>' />

                            </td>
                        </tr>
                    </ItemTemplate>
                </asp:Repeater>
            </table>
        </asp:View>
        <asp:View runat="server" ID="FileView">
            <script type="text/javascript">
                function ModifyMime() {
                    var oldValue = "<%=Mime %>";
                    var result = prompt("请输入新的MIME类型：", oldValue);
                    if (result && result != oldValue) {
                        $("#Hidden").val(result);
                        return true;
                    }
                    return false;
                }
                function StartCustomMime() {
                    window.open("/View/<%=RelativePath %>?Mime=" + $("#custom-mime")[0].value);
                } 
            </script>
            <div>大小：　　<%=Helper.GetSize(InfoFile.Length) %></div>
            <div>修改日期：<%=InfoFile.LastWriteTimeUtc.ToChineseString() %></div>
            <div>默认类型：<%=GetMimeType(Mime) %><asp:LinkButton runat="server" Text="[修改]" OnClick="ModifyMime" OnClientClick="return ModifyMime();" /></div>
            <div>
                <a href="/View/<%=RelativePath %>" target="_blank">[使用默认类型查看]</a>
                <a href="/Download/<%=RelativePath %>" target="_blank">[下载链接]</a>
                <a href="javascript:StartCustomMime();">[使用自定义MIME类型查看]</a>
                <asp:LinkButton runat="server" Text="[解压缩]" OnClick="Decompress" OnClientClick="return pickFolderCore();" />
                <%  %>
            </div>
            <div>自定义MIME：<input type="text" id="custom-mime" value="<%=Mime %>" style="width: 200px;" /></div>
        </asp:View>
        <asp:View runat="server" ID="FileDownloadingView">
            <div>离线下载正在进行中……无聊的话来看看这堆奇怪的文字吧。</div>
            <asp:UpdatePanel runat="server">
                <ContentTemplate>
                    <asp:Timer runat="server" Interval="1000" />
                    <div style="word-wrap: break-word;">原文件地址：　<%=Url %></div>
                    <div>当前状态：　　<%=Status %></div>
                    <div>文件总大小：　<%=FileSize %></div>
                    <div>已下载大小：　<%=DownloadedFileSize %></div>
                    <div>平均下载速度：<%=AverageDownloadSpeed %>&nbsp;每秒</div>
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
        </asp:View>
        <asp:View runat="server" ID="FileDecompressingView">
            <div>
                该文件正在被解压缩……
                <a href="/Task/Decompress/<%=FileHelper.GetFileValue(Server.GetDataPath(RelativePath), "id") %>">现在去看看吧！</a>
            </div>
        </asp:View>
        <asp:View runat="server" ID="FileCompressingView">
            <div>正在压缩中……</div>
            <asp:UpdatePanel runat="server">
                <ContentTemplate>
                    <asp:Timer runat="server" Interval="1000" />
                    <div>当前状态：　　<%=Status %></div>
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
        </asp:View>
        <asp:View runat="server" ID="GoneView">
            <div>您要查找的东西已消失！</div>
        </asp:View>
    </asp:MultiView>
</asp:Content>
