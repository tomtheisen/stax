export function setClipboard(text: string): boolean {
    let tempArea = document.createElement("textarea");
    tempArea.style.position = "fixed";
    tempArea.style.top = tempArea.style.left = "0";
    tempArea.value = text;

    document.body.appendChild(tempArea);
    tempArea.focus();
    tempArea.select();
    let result = document.execCommand("copy");
    document.body.removeChild(tempArea);

    return result;
}