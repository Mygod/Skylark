<%@ Page Title="人事部" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Mygod.Skylark.Task.Default" %>
<asp:Content ID="Content1" ContentPlaceHolderID="Head" runat="server">
    <style type="text/css">
        .notes {
            font-style: italic;
            color: #888888;
            font-size: 0.9em;
        }
    </style>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="Body" runat="server">
    <h2 class="center">人事部</h2>
    <div>　　欢迎来到您的 云雀™ 人事部！Mygod 工作室™ 一手为您管理这个地方，调遣人力资源来完成你分配的多到没有人性的任务，因此你只需要在这里轻轻地点几下鼠标即可控制大局。</div>
    <div>　　这里的员工主要来自于世界各地为了完成作业而参加社会实践活动的中小学生，他们是廉价而又优秀的义工。Mygod 工作室™从中培养出了许多优秀人才并将它们派往世界各地，包括您 云雀™ 的人事部，一个典型代表就是著名的韩亚航空 214 号班机的四位机长：Sum Ting Wong、Wi Tu Lo、Ho Lee Fuk、Bang Ding Ow。</div>
    
    <h3>人事部记过处分记录</h3>
    <div>　　当您的义工犯了什么该犯或不该犯的错误时就会在记过处分记录上留下痕迹。现在该记录的大小为 <%=LogSize %>。</div>
    <div class="center">
        <input type="button" value="查看记录" onclick="location.href='Log/'" />
        <asp:Button runat="server" Text="销毁记录" OnClick="DestroyLog" />
    </div>
    
    <h3>高级任务管理</h3>
    <div>
        在这里你可以管理高级任务（即非单文件输出的任务，如无文件输出或多文件输出），以下显示了当前正在进行的
        <%=TaskCount %>
        项任务，选中了
        <span id="selected-count">0</span>
        项任务。如果你要处理其他任务（即单文件输出任务），只需点击/删除输出文件即可。
    </div>
    <script type="text/javascript">
        function updateSelectedCount() {
            $('#selected-count').text($("#task-list >>>> input:checkbox:checked").length);
        }
        $(function () {
            $('#task-list >>>> input:checkbox').change(updateSelectedCount);
        });
        var selectAll = false;
        function doSelectAll() {
            $('#task-list >>>> input:checkbox').prop('checked', selectAll = !selectAll);
            updateSelectedCount();
        }
        function invertSelection() {
            var checked = $("#task-list >>>> input:checkbox:checked");
            $("#task-list >>>> input:checkbox:not(:checked)").prop("checked", true);
            checked.prop("checked", false);
            updateSelectedCount();
        }
        function deleteConfirm() {
            return $("#task-list >>>> input:checkbox:checked").length > 0
                && confirm("确定要删除吗？此操作没有后悔药吃。");
        }
    </script>
    <div>
        <a href="javascript:doSelectAll();">[全选]</a>
        <a href="javascript:invertSelection();">[反选]</a>
        <%  if (Request.GetUser().OperateTasks)
            { %>
        <asp:LinkButton runat="server" Text="[删除任务及其相关数据]" OnClick="Delete"
                        OnClientClick="return deleteConfirm();"></asp:LinkButton>
        <%  } %>
    </div>
    <table id="task-list">
        <thead>
            <tr>
                <th>类型</th>
                <th>添加时间</th>
                <th>剩余时间 (估计)</th>
                <th>完成时间 (估计)</th>
                <th>状态</th>
                <th>进度</th>
            </tr>
        </thead>
        <tbody>
            <asp:Repeater runat="server" ID="TaskList">
                <ItemTemplate>
                    <tr>
                        <td class="nowrap">
                            <input type="hidden" id="Hidden" runat="server" value='<%#Eval("ID") %>' />
                            <asp:CheckBox ID="Check" runat="server"
                                          Text='<%#AddSpace(TaskHelper.GetName(Eval("Type").ToString())) %>' />
                        </td>
                        <td>
                            <a href="/Task/Details/<%#Eval("ID") %>">
                                <%#((DateTime?) Eval("StartTime")).ToChineseString() %>
                            </a>
                        </td>
                        <td><%#((TimeSpan?) Eval("PredictedRemainingTime")).ToChineseString() %></td>
                        <td><%#((DateTime?) Eval("PredictedEndTime")).ToChineseString() %></td>
                        <td class="nowrap"><%#((GeneralTask)Container.DataItem).GetStatus() %></td>
                        <td class="nowrap"><%#Eval("Percentage") ?? Helper.Unknown %>%</td>
                    </tr>
                </ItemTemplate>
            </asp:Repeater>
        </tbody>
    </table>
</asp:Content>
