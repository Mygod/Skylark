function pickCore(text, defaultText) {
    var result = prompt(text, defaultText);
    if (result) $("#Hidden").val(result);
    return !!result;
}
function pickFolderCore(stripExtension) {
    var target = decodeURIComponent(uriParser[3]);
    if (stripExtension) target = target.replace(/\.[^\.]+?$/, "");
    return pickCore("请输入目标文件夹：（重名文件/文件夹将被跳过）", target);
}
function pickFolder() {
    if ($("input:checked").length == 0) return true;
    return pickFolderCore();
}