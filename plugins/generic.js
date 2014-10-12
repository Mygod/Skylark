function isNullOrWhiteSpace(input) {
    if (!input) return true;
    return input.replace(/\s/g, '').length < 1;
}

function getQueryString() {
    var i = location.href.indexOf('?');
    if (i < 0) return null;
    return location.href.substr(i + 1);
};

function getQueryStringRegExp(name) {
    var reg = new RegExp("(^|\\?|&)" + name + "=([^&]*)(\\s|&|$)", "i").exec(location.href);
    return reg ? decodeURIComponent(reg[2].replace(/\+/g, " ")) : "";
};

$.base64 = {
    encode: function (str) {
        return B64.encode(str);
    },
    decode: function (str) {
        return B64.decode(str);
    }
};

$.base64reversed = {
    encode: function (str) {
        return $.base64.encode(str.split("").reverse().join(""));
    },
    decode: function (str) {
        return $.base64.decode(str).split("").reverse().join("");
    }
};

function hideParent() {
    $(event.target).parent().hide();
}

if (typeof String.prototype.startsWith != 'function') {
    // see below for better implementation!
    String.prototype.startsWith = function (str) {
        return this.indexOf(str) == 0;
    };
}

var uriParser = /^(.*)\/(Browse|Download|Offline\/New|Offline\/NiGuan|Offline\/Start|Upload|View)\/(.*)(\?.*)?$/i.exec(location.href);

function changePath() {
    var result = prompt("请输入新的位置：", decodeURIComponent(uriParser[3]));
    if (result) location.href = uriParser[1] + "/" + uriParser[2] + "/" + result + uriParser[4];
}

function showLoginPanel() {
    $('#login-panel').toggle();
}

function login() {
    var box = $('#password-box');
    var psw = box.val();
    if (!psw) return;
    box.val(null);
    showLoginPanel();
    $.cookie('Password', CryptoJS.SHA512(psw), { expires: 365, path: '/' });
    location.reload();
}

function numberWithCommas(x) {
    return x.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
}

var units = ["字节", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB", "BB", "NB", "DB", "CB"];
function getSize(size) {
    var byt = size;
    size = numberWithCommas(size) + ' 字节';
    var i = 0;
    while (byt > 1000)
    {
        byt /= 1024;
        i++;
    }
    return i == 0 ? size : byt.toFixed(2) + " " + units[i] + " (" + size + ")";
}

function htmlEncode(value) {
    return $('<div/>').text(value).html();
}

function htmlDecode(value) {
    return $('<div/>').html(value).text();
}