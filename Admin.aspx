<%@ Page Title="董事会" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Admin.aspx.cs" Inherits="Mygod.Skylark.Admin" %>
<asp:Content ID="Content1" ContentPlaceHolderID="Head" runat="server">
    <style type="text/css">
        input.comment, input.password {
            width: 100% !important;
        }
        td {
            text-align: center;
        }
    </style>
    <script type="text/javascript">
        function insert(line) {
            function tf(i) {
                return i == 'T' ? ' checked' : '';
            }
            if (!line) line = CryptoJS.SHA512('New') + ',' + $.base64reversed.encode('New') + ',TTTTF';
            var columns = line.split(',');
            $('#password-list > tbody').append('<tr><td><input type="password" class="password" value="ENCRYPTED'
                + columns[0] + '" /></td><td><input type="text" class="comment" value="' +
                $.base64reversed.decode(columns[1]) + '" /></td><td><input type="checkbox" class="browse"'
                + tf(columns[2][0]) + ' /></td><td><input type="checkbox" class="download"' + tf(columns[2][1])
                + ' /></td><td><input type="checkbox" class="operateFiles"' + tf(columns[2][2])
                + ' /></td><td><input type="checkbox" class="operateTasks"' + tf(columns[2][3])
                + ' /></td><td><input type="checkbox" class="admin"' + tf(columns[2][4]) + ' /></td><td>'
                + '<a href="#" class="delete" onclick="this.parentNode.parentNode.remove();">[删除]</a></td></tr>');
        }
        $(function () {
            decodeURIComponent($('#hidden').val()).split(';').forEach(insert);
        });
        function submitForm() {
            function tf(i) {
                return i.prop('checked') ? 'T' : 'F';
            }
            var result = '';
            $('#password-list > tbody > tr').each(function () {
                var $this = $(this);
                var psw = $this.find('input.password').val();
                if (psw.startsWith('ENCRYPTED')) result += psw.substring(9) + ',';
                else result += CryptoJS.SHA512(psw) + ',';
                result += $.base64reversed.encode($this.find('input.comment').val()) + ','
                       + tf($this.find('input.browse')) + tf($this.find('input.download'))
                       + tf($this.find('input.operateFiles')) + tf($this.find('input.operateTasks'))
                       + tf($this.find('input.admin')) + ';';
            });
            $('#hidden').val(result);
            $('#data').submit();
        }
    </script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="Body" runat="server">
    <h2 class="center">董事会</h2>
    <div>　　欢迎来到您的 云雀™ 董事会！在这里你可以对您的 云雀™ 进行配置。例如在下面进行一些设置，管理各位成员的权限，或者你也可能想去<a href="/Task/">人事部</a>看看义工们的劳动情况。</div>
    
    <h3>配置</h3>
    <div>
        域名自动跳转：<asp:TextBox ID="RootBox" runat="server"></asp:TextBox>
    </div>
    <div class="center"><asp:Button ID="ModifyButton" runat="server" Text="修改" OnClick="UpdateConfig" /></div>

    <h3>管理成员权限</h3>
    <div>
        说明：将密码设为 123456 表示游客密码。所谓游客就是指没输密码或输入的密码为 123456
        或输入的密码不匹配任何已有规则的访问者。
    </div>
    <table id="password-list">
        <thead>
            <tr>
                <th>密码</th>
                <th>备注</th>
                <th class="center">查看目录列表/文件/任务详细信息</th>
                <th class="center">查看/下载文件</th>
                <th class="center">操作文件系统</th>
                <th class="center">操作离线任务</th>
                <th class="center">访问董事会</th>
                <th class="center"><a href="javascript:insert();">[添加]</a></th>
            </tr>
        </thead>
        <tbody></tbody>
    </table>
    <div class="center"><input type="button" onclick="submitForm();" value="提交" /></div>
    
    <h4>我的密码安全吗？</h4>
    <div>Mygod 工作室™ 保证不会窃取您的密码。（如果你不相信，云雀™ 是开源项目，你可以看到所有的源码来证明本工作室的清白）由于您的密码在传输与存储过程中均使用 SHA512 哈希，理论上来说不可能破解，比较安全。但如果可能，为了保险起见，请启用 HTTPS 并使用 HTTPS 操作，这将会更安全。</div>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="OuterForm" runat="server">
    <form id="data" method="post">
        <input type="hidden" id="hidden" name="hidden" value="<%=Data %>" />
    </form>
</asp:Content>
