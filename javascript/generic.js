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
    var reg = new RegExp("(^|\\?|&)" + name + "=([^&]*)(\\s|&|$)", "i");
    if (reg.test(location.href)) return unescape(RegExp.$2.replace(/\+/g, " "));
    return "";
};

$.base64 = {
    encode: function (str) {
        return CryptoJS.enc.Base64.stringify(CryptoJS.enc.Utf8.parse(str));
    },
    decode: function (str) {
        return CryptoJS.enc.Base64.parse(str).toString(CryptoJS.enc.Utf8);
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

var uriParser = /^(.*)\/(Browse|Download|Offline\/New|Offline\/NiGuan|Offline\/Start|Upload|View)\/(.*)(\?.*)?$/i;

function changePath() {
    uriParser.exec(location.href);
    var result = prompt("请输入新的位置：", unescape(RegExp.$3));
    if (result) location.href = RegExp.$1 + "/" + RegExp.$2 + "/" + result + RegExp.$4;
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
    $.cookie('Password', CryptoJS.SHA512(psw), { expires: 365 });
    location.reload();
}