﻿<%@ Page Title="离线下载你管视频" Language="C#" MasterPageFile="~/FileSystem.Master" AutoEventWireup="true" CodeBehind="YouTube.aspx.cs" Inherits="Mygod.Skylark.Offline.YouTube" %>
<asp:Content ID="Content1" ContentPlaceHolderID="Head" runat="server">
    <style type="text/css">
        #link-box {
            width: 100%;
        }
    </style>
    <script type="text/javascript">
        var opened = false;
        function toggleOpen() {
            $('details').prop('open', opened = !opened);
        }
    </script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="Body" runat="server">
    <section>
        <h2 class="center">离线下载你管视频</h2>
        <p><input type="text" id="link-box" value="http://www.youtube.com/results?search_query=" /></p>
        <p class="center">
            <button type="button" id="magic-button">获取该页上全部视频</button>
            <button type="button" onclick="toggleOpen();">全部展开/合并</button>
        </p>
        <script type="text/javascript">
            var url = getQueryStringRegExp("URL");
            if (url) $("#link-box").val($.base64reversed.decode(url));
            $("#magic-button").click(function () {
                location.href = "?Url=" + $.base64reversed.encode($("#link-box").val());
            });
        </script>
    </section>
    <section>
        <div>想要直接下载你管视频？试试<a href="https://mygod.be/product/hide-ear/" rel="noreferrer">掩耳</a>！提示：显示比特率的是只有视频/音频的，你可以手动/批量合并。</div>
        <% GetEmAll(); %>
    </section>
</asp:Content>
