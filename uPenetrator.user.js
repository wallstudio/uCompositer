// ==UserScript==
// @name         uPenetrator
// @namespace    http://tampermonkey.net/
// @version      0.1
// @description  try to take over the world!
// @author       You
// @match        https://www.youtube.com/*
// @grant        none
// ==/UserScript==

(function() {
    'use strict';

    let video = null;
    let audio = null;

    let insert = function(type, url)
    {
        let _ref = document.getElementById("columns");
        if(_ref == null)
        {
            window.setTimeout(() => insert(type, url), 1);
            return "Retry after 1s";
        }

        // Re-Create
        let _new = document.getElementById("junk_c");
        if(_new != null) _new.remove();
        _new = document.createElement("div");
        _new.id += "junk_c";
        _ref.parentNode.insertBefore(_new, _ref);

        if(type == "VIDEO") video = url;
        if(type == "AUDIO") audio = url;
        if(video != null && audio != null)
        {
            let data = 'data:text/plain,' + encodeURIComponent(`${video}\n${audio}`);
            _new.innerHTML += ` <a id="junk_c_data" href='${data}' download="${document.title}.ucinmeta">${document.title}</a> `;
        }

        return _new;
    }

    let proc = function(url)
    {
        try
        {
            if(!location.href.match(/youtube\.com\/watch/))
            {
                console.log("No watch page");
                return;
            }

            let sp = url.split("&");
            sp = sp
                .map(s => s.startsWith("range=") ? "" : s)
                .map(s => s.startsWith("rn=") ? "" : s)
                .map(s => s.startsWith("rbuf=") ? "" : s);
            let isVideo = sp.some(s => s.startsWith("mime=video"));
            let isAudio = sp.some(s => s.startsWith("mime=audio"));

            if(!isVideo && !isAudio)
            {
                return;
            }

            console.log(insert(isVideo ? "VIDEO" : "AUDIO", sp.join("&")));
        }
        catch(e)
        {
            console.log(e);
        }
    }

    var base_open = XMLHttpRequest.prototype.open;
    XMLHttpRequest.prototype.open = function(method, url, async, user, password){
        if(url.includes("videoplayback"))
        {
            proc(url);
        }
        base_open.apply(this, arguments);
    }

})();
