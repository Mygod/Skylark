<%@ Page Title="升级" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Mygod.Skylark.Update.Default" %>
<asp:Content ID="Content1" ContentPlaceHolderID="Head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="Body" runat="server">
    <h2 class="center">升级</h2>
    <% if (string.IsNullOrWhiteSpace(new WebClient()
           .DownloadString("https://mygod.tk/product/update/" + CurrentAssembly.GetName().Version.Revision + '/')))
       { %>
    <div>恭喜您，您的 云雀™ 是最新版！不过你还是可以部署一次最新版。</div>
    <% }
       else
       { %>
    <div>新版 云雀™ 现在已可用，请尽快升级。</div>
    <% } %>
	<small>P.S. 不是所有的更新都会在这里显示，只有重大、稳定的更新才会显示，即不重大的（小 BUG 修复或小改进）或不稳定的（测试版）不会在这里显示。你可以<a href="https://github.com/Mygod/Skylark/commits/master">点击这里看看最近的更新日志</a>，那里通常会有这里不提醒的更新。如果你是个更新狂魔，在 GitHub 上 watch 这个 repo 可在第一时间获得 云雀™ 更新的最新动态。</small>
    <h3>升级须知</h3>
    <ul>
        <li>请在开始前结束一切当前正在执行而且想要继续执行的离线任务，否则这些离线任务在升级过程中可能会被强制终止。</li>
        <li>在最糟糕的情况下您的文件可能会消失，请在升级前备份好最重要的文件。</li>
        <li>升级将不会产生最初部署时产生的演示文件。如果你想要找回他们，<a href="https://github.com/Mygod/Skylark/tree/master/Files">去这里看看</a>。</li>
        <li>如果您的 云雀™ 出现了异常且无法通过升级修复，请尝试通过重新部署的方式重装 云雀™。</li>
        <li>升级过程中会产生一定的临时文件，你需要在升级完成后点击下面的“清理升级文件”来删除它们。</li>
        <li>升级需要半分钟左右，若过程中出现任何错误请忽略，若过了两分钟仍未成功请与我们联系。</li>
    </ul>
    <div class="center">
        <asp:Button ID="UpdateButton" runat="server" Text="云端下载并部署最新版" OnClick="Update" />
        <asp:Button ID="CleanupButton" runat="server" Text="清理升级文件" OnClick="Cleanup" />
    </div>
</asp:Content>
