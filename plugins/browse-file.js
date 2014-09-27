function modifyMime() {
    var oldValue = $('#current-mime').text();
    var result = prompt("请输入新的 MIME 类型：", oldValue);
    if (result && result != oldValue) {
        $("#Hidden").val(result);
        return true;
    }
    return false;
}
function startCustomMime() {
    window.open("/View/" + decodeURIComponent(uriParser[3]) + "?Mime=" + $("#custom-mime")[0].value);
}
function convert() {
    $('#ConvertPathBox').val(decodeURIComponent(uriParser[3]));
    $('#ConvertVideoCodecBox').val('');
    $('#ConvertAudioCodecBox').val('');
    $('#ConvertAudioPathBox').val('');
    $("#convert-form").show();
}
function mergeVA() {
    var path = decodeURIComponent(uriParser[3]),
        result = /^(.*) \[V\]\.(.*)$/i.exec(path) || /^(.*)\.(.*)$/.exec(path);
    $('#ConvertPathBox').val(result ? result[1] + '.' + result[2] : path);
    $('#ConvertVideoCodecBox').val('copy');
    $('#ConvertAudioCodecBox').val('copy');
    $('#ConvertAudioPathBox').val(result ? result[1] + ' [A].' +
                                  (result[2].toLowerCase() == 'mp4' ? 'm4a' : result[2]) : path);
    $("#convert-form").show();
}

$(function () {
    var list = $('#output-paths'), path = decodeURIComponent(uriParser[3]),
        result = /^(.*) \[V\]\.(.*)$/i.exec(path) || /^(.*)\.(.*)$/.exec(path), set = new Set();
    function add(value) {
        if (set.has(value)) return;
        list.append($('<option></option>').attr('value', value));
        set.add(value);
    }
    list.empty();
    add(path);
    if (result) {
        add(result[1] + '.' + result[2]);
        add(result[1] + ' [R].' + result[2]);
    } else add(path + ' [R]');
    (list = $('#audio-paths')).empty();
    set.clear();
    add(path);
    if (result) {
        add(result[1] + ' [A].' + result[2]);
        add(result[1] + ' [A].m4a');
        add(result[1] + ' [A].webm');
        add(result[1] + '.' + result[2]);
        add(result[1] + '.m4a');
        add(result[1] + '.webm');
        add(result[1] + '.mp4');
    } else {
        add(path + ' [A]');
        add(path + ' [A].m4a');
        add(path + ' [A].webm');
    }
});