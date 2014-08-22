<%@ Page Title="" Language="C#" MasterPageFile="~/FileSystem.Master" AutoEventWireup="true" CodeBehind="Browse.aspx.cs" Inherits="Mygod.Skylark.Browse" %>
<%@ Register TagPrefix="skylark" tagName="TaskViewer" src="/TaskViewer.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="Head" runat="server">
    <script src="/plugins/resumable.js"></script>
    <script src="/plugins/jquery/jquery.sticky.js"></script>
    <script type="text/javascript">
        function pickCore(text, defaultText) {
            var result = prompt(text, defaultText);
            if (result) $("#Hidden").val(result);
            return !!result;
        }
        function pickFolderCore(stripExtension) {
            var target = unescape(uriParser[3]);
            if (stripExtension) target = target.replace(/\.[^\.]+?$/, "");
            return pickCore("请输入目标文件夹：（重名文件/文件夹将被跳过）", target);
        }
        function pickFolder() {
            if ($("input:checked").length == 0) return true;
            return pickFolderCore();
        }
    </script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="Body" runat="server">
    <asp:ScriptManager runat="server" />
    <input type="hidden" id="Hidden" runat="server" ClientIDMode="Static" />
    <asp:MultiView runat="server" ID="Views">
        <asp:View runat="server" ID="DirectoryView">
            <div class="sticky">
                <section class="button-set">
                    <button onclick="doSelectAll();" type="button">全选</button>
                    <button onclick="invertSelection();" type="button">反选</button>
                    <% if (CurrentUser.Download)
                       { %>
                    <button onclick="getDownloadLink();" type="button">生成下载链接</button>
                    <% }
                       if (CurrentUser.OperateFiles)
                       { %>
                    <button runat="server" onclick="return newFolder() && !" onServerClick="NewFolder">新建文件夹</button>
                    <button runat="server" onclick="return pickFolder() && !" onServerClick="Move">移动到</button>
                    <button runat="server" onclick="return pickFolder() && !" onServerClick="Copy">复制到</button>
                    <button runat="server" onclick="return deleteConfirm() && !" onServerClick="Delete">删除</button>
                    <%     if (CurrentUser.OperateTasks)
                           { %>
                    <a class="button" href="/Offline/New/<%= RelativePath %>">新建离线下载任务</a>
                    <button onclick="showCompressConfig();" type="button">压缩选中项</button>
                    <button runat="server" onServerClick="CrossAppCopy" onclick="return pickApp() && !">跨云雀传输</button>
                    <button runat="server" onServerClick="FtpUpload" onclick="return pickFtp() && !">上传到 FTP</button>
                    <%     }
                       } %>
                </section>
                <section id="upload-panel" ondragenter="$(this).addClass('upload-dragover');"
                         ondragleave="$(this).removeClass('upload-dragover');"
                         ondrop="$(this).removeClass('upload-dragover');">
                    <table id="upload-file-table" class="hovered bordered table" style="display: none;">
                        <thead>
                            <tr>
                                <th class="nowrap">文件名</th>
                                <th class="nowrap">大小</th>
                                <th class="stretch">进度</th>
                                <th class="nowrap">状态</th>
                                <th class="nowrap"></th>
                            </tr>
                        </thead>
                        <tbody></tbody>
                    </table>
                    <div>该目录下有 <%= DirectoryCount %> 个目录，<%= FileCount %>&nbsp;个文件，当前选中 <span id="selected-count">0</span> 个项目。<% if (CurrentUser.OperateFiles) { %>请将要上传的文件或文件夹拖动到这里，或者你也可以<a id="upload-browse" href="#">点击这里浏览你要上传的文件</a>或<a id="upload-browse-dir" href="#">文件夹</a>。如果上传速度过慢，请<a href="javascript:changeUploadThreads();">点击这里</a>调整<span class="help" title="上传线程数默认为 10，增大线程数可在一定程度上加快上传速度，但线程数过多可能会使上传反而变慢。推荐普通用户保留默认值。">上传线程数</span>。<% } %></div>
                </section>
                <section id="running-result" style="display: none;">
                    <button type="button" onclick="hideParent();">隐藏</button><br />
                    <textarea class="stretch" style="height: 100px;"></textarea>
                </section>
                <section id="compress-config" style="display: none;">
                    <button type="button" onclick="hideParent();">隐藏</button>
                    <asp:DropDownList ID="CompressionLevelList" runat="server">
                        <asp:ListItem Value="Ultra">最小</asp:ListItem>
                        <asp:ListItem Value="High">较小</asp:ListItem>
                        <asp:ListItem Value="Normal">普通</asp:ListItem>
                        <asp:ListItem Value="Low">较快</asp:ListItem>
                        <asp:ListItem Value="Fast">最快</asp:ListItem>
                        <asp:ListItem Value="None">不压缩</asp:ListItem>
                    </asp:DropDownList>
                    <asp:TextBox ID="ArchiveFilePath" runat="server" Width="300px" />
                    <button runat="server" onServerClick="Compress">压缩到指定路径</button>
                    （请以 <a href="http://zh.wikipedia.org/wiki/7-Zip">7z</a>,
                        <a href="http://zh.wikipedia.org/wiki/ZIP格式">zip</a> 或
                        <a href="http://zh.wikipedia.org/wiki/Tar">tar</a> 作为扩展名）
                </section>
            </div>
            <script type="text/javascript">
                function updateSelectedCount() {
                    $('#selected-count').text($("#file-list >>>>> input:checkbox:checked").length);
                }
                $(function () {
                    $('#file-list >>>>> input:checkbox').change(updateSelectedCount);
                });
                function newFolder() {
                    return pickCore("请输入文件夹名：", "");
                }
                function deleteConfirm() {
                    return $("#file-list >>>>> input:checkbox:checked").length > 0
                        && confirm("确定要删除吗？此操作没有后悔药吃。");
                }
                var selectAll = false;
                function doSelectAll() {
                    $('#file-list >>>>> input:checkbox').prop('checked', selectAll = !selectAll);
                    updateSelectedCount();
                }
                function invertSelection() {
                    var checked = $("#file-list >>>>> input:checkbox:checked");
                    $("#file-list >>>>> input:checkbox:not(:checked)").prop("checked", true);
                    checked.prop("checked", false);
                    updateSelectedCount();
                }
                function showCompressConfig() {
                    $("#compress-config").show();
                }
                function getDownloadLink() {
                    var array = $("#file-list >>>>> input:checkbox:checked").parent().parent().parent()
                                    .find("input:hidden");
                    var result = "";
                    var prefix = (uriParser[1] + "/Download/" + uriParser[3]).replace("\\", "/");
                    while (prefix[prefix.length - 1] == "/") prefix = prefix.substr(0, prefix.length - 1);
                    prefix = prefix + "/";
                    for (var i = 0; i < array.length; i++)
                        result += prefix + encodeURIComponent(array[i].value) + "\r\n";
                    var box = $("#running-result");
                    var input = box.children("textarea");
                    input.val(result);
                    box.show();
                    input.select();
                    input.focus();
                }
                function rename(oldName) {
                    var result = prompt("请输入新的名字：", oldName);
                    if (result && result != oldName) {
                        $("#Hidden").val(result);
                        return true;
                    }
                    return false;
                }
                var appParser = /^http:\/\/((.*?)@)?(.*?)\/Browse\/(.*)$/;
                function pickApp() {
                    var result = prompt("请输入目标云雀：（请使用“http://[password@]domain/Browse/......”的格式，" +
                                        "如果不输入密码，默认将使用当前的密码）", "http://skylark.apphb.com/Browse/");
                    var match = appParser.exec(result);
                    if (match) {
                        $("#Hidden").val(match[2] ? 'http://' + CryptoJS.SHA512(match[2]) + '@' + match[3]
                                                              + '/Browse/' + match[4] : result);
                        return true;
                    }
                    return false;
                }
                function pickFtp() {
                    return pickCore("请输入目标 FTP 目录：（格式为 ftp://[username:password@]host/dir/file，" +
                                    "你的用户名和密码不会被保留，也不会在上传进度上显示）", "");
                }
                function getUploadThreads() { return localStorage.uploadThreads ? localStorage.uploadThreads : 10; }
                var r = new Resumable({
                    target: '/Upload/<%= RelativePath.Replace("'", @"\'") %>',
                    permanentErrors: [401, 403, 500],
                    simultaneousUploads: getUploadThreads(),
                    minFileSize: 0  // damn you documentation
                });
                var uploadFileTable = $('#upload-file-table');
                var rows = {};
                r.on('fileAdded', function (file) {
                    uploadFileTable.show();
                    uploadFileTable.find('tbody').append(rows[file.relativePath] = $('<tr><td class="nowrap">' + file.relativePath + '</td><td class="nowrap">' + getSize(file.size) + '</td><td class="stretch"><div class="progress-bar" style="height: 26px; margin-bottom: 0;"><div id="upload-progress-bar" class="bg-cyan bar" style="widtd: 0;"></div></div></td><td id="upload-progress-text" class="nowrap">等待上传</td><td class="nowrap"><button id="cancel-upload-button" type="button"><i class="icon-cancel"></i></button></td></tr>'));
                    rows[file.relativePath].find('#cancel-upload-button').click(function () {
                        file.cancel();
                        rows[file.relativePath].remove();
                        delete rows[file.relativePath];
                    });
                    if (!r.isUploading()) r.upload();
                });
                r.on('fileProgress', function (file) {
                    rows[file.relativePath].find('#upload-progress-bar').width(file.progress() * 100 + '%');
                    rows[file.relativePath].find('#upload-progress-text').html((file.progress() * 100).toFixed(2) + '%');
                });
                r.on('fileRetry', function (file) {
                    rows[file.relativePath].find('#upload-progress-text').html('出现了奇怪的事情，重试中');
                });
                r.on('fileError', function (file, message) {
                    rows[file.relativePath].find('#upload-progress-bar').removeClass('bg-cyan');
                    rows[file.relativePath].find('#upload-progress-bar').addClass('bg-red');
                    rows[file.relativePath].find('#upload-progress-text')
                        .html('<span title="' + htmlEncode(message) + '">失败</a>');
                });
                r.on('fileSuccess', function (file) {
                    rows[file.relativePath].find('#upload-progress-bar').width('100%');
                    rows[file.relativePath].find('#upload-progress-text').html('完成');
                });
                r.assignBrowse($('#upload-browse'));
                r.assignBrowse($('#upload-browse-dir'), true);
                r.assignDrop($('#upload-panel'));

                function changeUploadThreads() {
                    var result = prompt("请输入上传线程数：", getUploadThreads());
                    if (result) r.simultaneousUploads = localStorage.uploadThreads = parseInt(result);
                }

                $('.sticky').sticky({ topSpacing: 0 });
            </script>
            <section>
                <table id="file-list" class="hovered bordered table">
                    <asp:Repeater runat="server" ID="DirectoryList" OnItemCommand="DirectoryCommand">
                        <ItemTemplate>
                            <tr>
                                <td class="nowrap input-control checkbox" style="min-width: 50px;">
                                    <label>
                                        <asp:CheckBox ID="Check" runat="server" />
                                        <span class="check"></span>
                                        <img src="/Image/Directory.png" alt="目录" />
                                    </label>
                                </td>
                                <td class="stretch"><a href="<%#Eval("Name") %>/"><%#Eval("Name") %></a></td>
                                <td class="nowrap">&lt;DIR&gt;</td>
                                <td class="nowrap">
                                    <%#File.GetLastWriteTimeUtc(FileHelper.GetFilePath(FileHelper
                                        .Combine(RelativePath, Eval("Name").ToString()))).ToChineseString() %>
                                </td>
                                <td class="nowrap">
                                    <input type="hidden" id="Hidden" runat="server" value='<%#Eval("Name") %>' />
                                    <%  if (CurrentUser.OperateFiles)
                                        { %>
                                    <asp:LinkButton runat="server" Text="[重命名]" CommandName="Rename"
                                                    OnClientClick='<%#
                                                        string.Format("return rename(\"{0}\");", Eval("Name"))%>' />
                                    <%  } %>
                                </td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                    <asp:Repeater runat="server" ID="FileList" OnItemCommand="FileCommand">
                        <ItemTemplate>
                            <tr>
                                <td class="nowrap input-control checkbox" style="min-width: 50px;">
                                    <label>
                                        <asp:CheckBox ID="Check" runat="server" />
                                        <span class="check"></span>
                                        <%# FileHelper.IsReady(FileHelper.GetDataFilePath(
                                                FileHelper.Combine(RelativePath, Eval("Name").ToString())))
                                                    ? " <img src=\"/Image/File.png\" alt=\"文件\" />"
                                                    : " <img src=\"/Image/Busy.png\" alt=\"文件 (处理中)\" />" %>
                                    </label>
                                </td>
                                <td class="stretch"><a href="<%#Eval("Name") %>"><%#Eval("Name") %></a></td>
                                <td class="nowrap">
                                    <%#Mygod.Helper.GetSize(FileHelper.GetFileSize
                                            (FileHelper.Combine(RelativePath, Eval("Name").ToString())), "字节") %>
                                </td>
                                <td class="nowrap">
                                    <%#File.GetLastWriteTimeUtc(FileHelper.GetFilePath(FileHelper
                                        .Combine(RelativePath, Eval("Name").ToString()))).ToChineseString() %>
                                </td>
                                <td class="nowrap">
                                    <input type="hidden" id="Hidden" runat="server" value='<%#Eval("Name") %>' />
                                    <% if (CurrentUser.OperateFiles)
                                       { %>
                                    <asp:LinkButton runat="server" Text="[重命名]" CommandName="Rename"
                                                    OnClientClick='<%#
                                                        string.Format("return rename(\"{0}\");", Eval("Name"))%>' />
                                    <% }
                                       if (CurrentUser.Download)
                                       { %>
                                    <a href="/Download/<%#FileHelper.Combine(RelativePath,
                                                                Eval("Name").ToString()) %>">[下载]</a>
                                    <% } %>
                                </td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </table>
            </section>
        </asp:View>
        <asp:View runat="server" ID="FileView">
            <script type="text/javascript">
                function modifyMime() {
                    var oldValue = "<%=Mime %>";
                    var result = prompt("请输入新的 MIME 类型：", oldValue);
                    if (result && result != oldValue) {
                        $("#Hidden").val(result);
                        return true;
                    }
                    return false;
                }
                function startCustomMime() {
                    window.open("/View/<%=RelativePath %>?Mime=" + $("#custom-mime")[0].value);
                }
                function convert() {
                    $("#convert-form").show();
                }
            </script>
            <section>
                <div>大小：　　<%=Mygod.Helper.GetSize(InfoFile.Length, "字节") %></div>
                <div>修改日期：<%=InfoFile.LastWriteTimeUtc.ToChineseString() %></div>
                <div>
                    默认类型：<%=GetMimeType(Mime) %><% if (CurrentUser.OperateFiles)
                       { %>
                    <button runat="server" OnServerClick="ModifyMime" onclick="return modifyMime() && !">修改</button>
                    <% } %>
                </div>
                <div>FFmpeg 分析结果：<br /><pre><%=FFmpegResult %></pre></div>
                <div>自定义 MIME：<input type="text" id="custom-mime" value="<%=Mime %>" style="width: 200px;" /></div>
            </section>
            <section class="button-set">
                <% if (CurrentUser.Download)
                    { %>
                <a class="button" href="/View/<%= RelativePath %>" target="_blank">使用默认类型查看</a>
                <a class="button" href="/Download/<%= RelativePath %>" target="_blank">下载</a>
                <button type="button" onclick="startCustomMime();">使用自定义 MIME 类型查看</button>
                <% } %>
                <% if (CurrentUser.OperateFiles)
                    { %>
                <a class="button" href="/Edit/<%= RelativePath %>" target="_blank">以纯文本格式编辑</a>
                <%     if (CurrentUser.OperateTasks)
                        { %>
                <button runat="server" OnServerClick="Decompress" onclick="return pickFolderCore(true) && !">
                    解压缩
                </button>
                <button type="button" onclick="convert();">转换媒体文件格式</button>
                <%     }
                    } %>
            </section>
            <section id="convert-form" style="display: none;">
                <div>输出路径：（重名将被忽略）</div>
                <div><asp:TextBox ID="ConvertPathBox" runat="server" Width="100%" ClientIDMode="Static" /></div>
                <div>视频大小：（如640x480，不填表示不变）</div>
                <div><asp:TextBox ID="ConvertSizeBox" runat="server" /></div>
                <div>视频编码：</div>
                <div>
                    <asp:DropDownList ID="ConvertVideoCodecBox" runat="server" ClientIDMode="Static">
                        <asp:ListItem Selected="True" Text="默认编码" Value="" />
                        <asp:ListItem Text="直接复制" Value="copy" />
                    </asp:DropDownList>
                </div>
                <div>音频编码：</div>
                <div>
                    <asp:DropDownList ID="ConvertAudioCodecBox" runat="server" ClientIDMode="Static">
                        <asp:ListItem Selected="True" Text="默认编码" Value="" />
                        <asp:ListItem Text="直接复制" Value="copy" />
                    </asp:DropDownList>
                </div>
                <div>字幕编码：</div>
                <div>
                    <asp:DropDownList ID="ConvertSubtitleCodecBox" runat="server">
                        <asp:ListItem Selected="True" Text="默认编码" Value="" />
                        <asp:ListItem Text="直接复制" Value="copy" />
                    </asp:DropDownList>
                </div>
                <div>起始位置：（秒数，或使用 hh:mm:ss[.xxx] 的形式，不填表示从头开始）</div>
                <div><asp:TextBox ID="ConvertStartBox" runat="server" /></div>
                <div>结束位置：（秒数，或使用 hh:mm:ss[.xxx] 的形式，不填表示到视频结束为止）</div>
                <div><asp:TextBox ID="ConvertEndBox" runat="server" /></div>
                <div>替换音频：（音频路径，用于混流等）</div>
                <div><asp:TextBox ID="ConvertAudioPathBox" runat="server" Width="100%" ClientIDMode="Static" /></div>
                <div class="center">
                    <asp:Button ID="ConvertButton" runat="server" Text="转换" OnClick="Convert" />
                </div>
            </section>
        </asp:View>
        <asp:View runat="server" ID="FileUploadingView">
            <section>
                <% var uploadTask = (UploadTask) Task; %>
                <div>当前状态：　　正在上传中</div>
                <div>开始上传时间：<%=Task == null || !Task.StartTime.HasValue
                                        ? Helper.Unknown : Task.StartTime.Value.ToChineseString() %></div>
                <div>总分块数量：　<%=uploadTask.TotalParts %> (默认分块大小为 1MB)</div>
                <div>已上传数量：　<%=uploadTask.FinishedParts %></div>
                <div class="progress-bar"><div class="bg-cyan bar" style="width: <%=
                    Task == null || !Task.Percentage.HasValue ? 0 : Task.Percentage.Value %>%;"></div></div>
            </section>
        </asp:View>
        <asp:View runat="server" ID="FileProcessingView">
            <section>
                <skylark:TaskViewer runat="server" ID="Viewer" />
            </section>
        </asp:View>
        <asp:View runat="server" ID="GeneralTaskProcessingView">
            <section>
                <% var root = FileHelper.GetElement(FileHelper.GetDataFilePath(RelativePath)); %>
                正在<%=TaskHelper.GetName(root.GetAttributeValue("state")) %>……
                <a href="/Task/Details/<%=root.GetAttributeValue("id") %>">现在去看看吧！</a>
            </section>
        </asp:View>
        <asp:View runat="server" ID="GoneView">
            <section>您要查找的东西已消失！</section>
        </asp:View>
    </asp:MultiView>
</asp:Content>
