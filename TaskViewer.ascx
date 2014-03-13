<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="TaskViewer.ascx.cs" Inherits="Mygod.Skylark.TaskViewer" %>
<%@ Import Namespace="Mygod.Skylark" %>
<%@ Import Namespace="System.Globalization" %>
<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <asp:Timer runat="server" Interval="1000" />
        <div>当前状态：　　<%=Task == null ? "任务不存在" : Task.GetStatus(TaskHelper.GetName(Task.Type), Never) %></div>
        <%
            var remoteTask = Task as IRemoteTask;
            if (remoteTask != null)
            {
        %>
        <div>目标地址：　　<a href="/Browse/<%=remoteTask.Url %>">/<%=remoteTask.Url %></a></div>
        <%
            }
            var singleSource = Task as ISingleSource; 
            if (singleSource != null)
            {
        %>
        <div>源文件地址：　<a href="/Browse/<%=singleSource.Source %>">/<%=singleSource.Source %></a></div>
        <%  
            }
            var multipleSources = Task as IMultipleSources;
            if (multipleSources != null)
            {
        %>
        <div>
            目标目录：　　<a href="/Browse/<%=multipleSources.BaseFolder %>">/<%=multipleSources.BaseFolder %></a>
        </div>
        <div>
            当前文件：　　<a href="/Browse/<%=multipleSources.CurrentSource %>">/<%=multipleSources.CurrentSource %></a>
        </div>
        <div>源文件数量：　<%=multipleSources.SourceCount %></div>
        <div>已处理数量：　<%=multipleSources.ProcessedSourceCount %></div>
        <%
            }
            var convertTask = Task as ConvertTask;
            if (convertTask != null)
            {
        %>
        <div>总时长：　　　<%=convertTask.Duration.ToString("G") %></div>
        <div>已处理时长：　<%=convertTask.ProcessedDuration.ToString("G") %></div>
        <%
            }
            var crossAppCopyTask = Task as CrossAppCopyTask;
            if (crossAppCopyTask != null)
            {
        %>
        <div>云雀源：　　　<%=string.Format("<a href=\"{0}\">{0}</a>", string.Format("http://{0}/Browse/{1}",
                                            crossAppCopyTask.Domain, crossAppCopyTask.Source)) %></div>
        <%
            }
            var multipleFilesTask = Task as MultipleFilesTask;
            if (multipleFilesTask != null)
            {
        %>
        <div>目标目录：　　<a href="<%=multipleFilesTask.Target %>"><%=multipleFilesTask.Target %></a></div>
        <div>当前文件：　　<%=multipleFilesTask.CurrentFile == null ? "无"
            : string.Format("<a href=\"/Browse/{0}\">{0}</a>", multipleFilesTask.CurrentFile) %></div>
        <div>文件数量：　　<%=multipleFilesTask.FileCount.HasValue
                                ? multipleFilesTask.FileCount.Value.ToString(CultureInfo.InvariantCulture)
                                : Unknown %></div>
        <div>已处理文件数：<%=multipleFilesTask.ProcessedFileCount %></div>
        <%
            }
        %>
        <div>文件总大小：　<%=Task == null || !Task.FileLength.HasValue
                                ? Unknown : Helper.GetSize(Task.FileLength.Value) %></div>
        <div>已处理大小：　<%=Task == null ? Unknown : Helper.GetSize(Task.ProcessedFileLength) %></div>
        <div>平均处理速度：<%=Task == null || !Task.SpeedFileLength.HasValue
                            ? Unknown : Helper.GetSize(Task.SpeedFileLength.Value) %>&nbsp;每秒</div>
        <div>开始时间：　　<%=Task == null || !Task.StartTime.HasValue
                                ? Unknown : Task.StartTime.Value.ToChineseString() %></div>
        <div>花费时间：　　<%=Task == null || !Task.SpentTime.HasValue
                                ? Unknown : Task.SpentTime.Value.ToString("G") %></div>
        <div>预计剩余时间：<%=NeverEnds ? "永远" : Task == null || !Task.PredictedRemainingTime.HasValue
                                ? Unknown : Task.PredictedRemainingTime.Value.ToString("G") %></div>
        <div>预计结束时间：<%=NeverEnds ? "地球毁灭时" : Task == null || !Task.PredictedEndTime.HasValue
                                ? Unknown : Task.PredictedEndTime.Value.ToChineseString() %></div>
        <div class="progress-bar">
            <%-- ReSharper disable UnexpectedValue --%>
            <div class="bar" style="width: <%=Task == null || !Task.Percentage.HasValue
                                                ? 0 : Task.Percentage.Value %>%;"></div>
            <%-- ReSharper restore UnexpectedValue --%>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>