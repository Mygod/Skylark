function getSize(size) {
    var n = size, i = 0;
    while (n > 1000) {
        n /= 1024;
        ++i;
    }
    size = size.toLocaleString() + '  字节';
    return i == 0 ? size : n.toFixed(2) + ' ' +
        ['字节', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB', 'BB', 'NB', 'DB', 'CB'][i] + ' (' + size + ')';
}
function updateSelectedCount() {
    var query = $("#file-list input:checkbox:checked");
    $('#selected-count').text(query.length);
    var size = 0;
    query.each(function () {
        var i = $(this).data('size');
        if (i) size += i;
    });
    $('#selected-size').text(getSize(size));
}
$(function () {
    $('#file-list input:checkbox').change(updateSelectedCount);
});
function newFolder() {
    return pickCore("请输入文件夹名：", "");
}
function deleteConfirm() {
    return $("#file-list input:checkbox:checked").length > 0 && confirm("确定要删除吗？此操作没有后悔药吃。");
}
var selectAll = false;
function doSelectAll() {
    $('#file-list >>>>> input:checkbox').prop('checked', selectAll = !selectAll);
    updateSelectedCount();
}
function invertSelection() {
    var checked = $("#file-list input:checkbox:checked");
    $("#file-list input:checkbox:not(:checked)").prop("checked", true);
    checked.prop("checked", false);
    updateSelectedCount();
}
function showCompressConfig() {
    $("#compress-config").show();
}
function showBatchMergeVAConfig() {
    $("#batch-merge-va-config").show();
}
function getDownloadLink() {
    var array = $("#file-list input:checkbox:checked").parent().parent().parent().find("input:hidden");
    var result = "";
    var prefix = (uriParser[1] + "/Download/" + uriParser[3]).replace("\\", "/");
    while (prefix[prefix.length - 1] == "/") prefix = prefix.substr(0, prefix.length - 1);
    prefix = prefix + "/";
    for (var i = 0; i < array.length; i++) result += prefix + encodeURIComponent(array[i].value) + "\r\n";
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
        $("#Hidden").val(match[2] ? 'http://' + CryptoJS.SHA512(match[2]) + '@' + match[3] + '/Browse/' + match[4]
                                  : result);
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
    target: '/Upload/' + uriParser[3],
    permanentErrors: [401, 403, 500],
    simultaneousUploads: getUploadThreads(),
    minFileSize: 0  // damn you documentation
});
var uploadFileTable = $('#upload-file-table');
var rows = {};
r.on('fileAdded', function (file) {
    uploadFileTable.show();
    uploadFileTable.find('tbody').append(rows[file.relativePath] = $('<tr><td class="nowrap">' + file.relativePath +
        '</td><td class="nowrap">' + getSize(file.size) + '</td><td class="stretch"><div class="progress-bar" ' +
        'style="height: 26px; margin-bottom: 0;"><div id="upload-progress-bar" class="bg-cyan bar" style="widtd: 0;"' +
        '></div></div></td><td id="upload-progress-text" class="nowrap">等待上传</td><td class="nowrap"><button ' +
        'id="cancel-upload-button" type="button"><i class="icon-cancel"></i></button></td></tr>'));
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
    rows[file.relativePath].find('#upload-progress-text').html('<span title="' + htmlEncode(message) + '">失败</a>');
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