if (typeof (loadScript) == 'undefined') {
    var loadScript = function (url, callback) {
        var script = document.createElement("script");
        script.type = "text/javascript";
        script.src = url;
        if (script.readyState) { /* IE */
            script.onreadystatechange = function () {
                if (script.readyState == 'loaded' || script.readyState == 'complete') {
                    script.onreadystatechange = null;
                    callback();
                }
            };
        } else { /* OTHERS */
            script.onload = callback;
        }
        document.getElementsByTagName('head')[0].appendChild(script);
    }
}