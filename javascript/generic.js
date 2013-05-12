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