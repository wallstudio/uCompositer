// ==UserScript==
// @name         uPenetrator
// @namespace    https://github.com/wallstudio/uCompositer
// @version      0.2
// @description  
// @author       You
// @match        https://www.youtube.com/*
// @grant        none
// @require      https://raw.githubusercontent.com/wallstudio/UserScriptLibrary/master/xhrFetchInjection.js
// @require      https://raw.githubusercontent.com/wallstudio/UserScriptLibrary/master/download.js
// ==/UserScript==

(function() {
	'use strict';

	console.log('uPenetrator');

	/** @type {Map<string, string>} */
	const media = new Map();
	/** @type {Element} */
	let button;
	const icon = `
		<svg version="1.1" width="100%" height="100%" viewBox="-200 -200 912 912">
			<style type="text/css">
				.st0{fill:#4B4B4B;}
			</style>
			<g>
				<path class="st0" d="M243.591,309.362c3.272,4.317,7.678,6.692,12.409,6.692c4.73,0,9.136-2.376,12.409-6.689l89.594-118.094
					c3.348-4.414,4.274-8.692,2.611-12.042c-1.666-3.35-5.631-5.198-11.168-5.198H315.14c-9.288,0-16.844-7.554-16.844-16.84V59.777
					c0-11.04-8.983-20.027-20.024-20.027h-44.546c-11.04,0-20.022,8.987-20.022,20.027v97.415c0,9.286-7.556,16.84-16.844,16.84
					h-34.305c-5.538,0-9.503,1.848-11.168,5.198c-1.665,3.35-0.738,7.628,2.609,12.046L243.591,309.362z" style="fill: rgb(255, 255, 255);"></path>
				<path class="st0" d="M445.218,294.16v111.304H66.782V294.16H0v152.648c0,14.03,11.413,25.443,25.441,25.443h461.118
					c14.028,0,25.441-11.413,25.441-25.443V294.16H445.218z" style="fill: rgb(255, 255, 255);"></path>
			</g>
		</svg>`;
	
	injectXHR((_url, args) =>
	{
		/** @type {string} */
		const url = _url;

		if(!url.includes("videoplayback"))
		{
			return args;
		}
		if(!location.href.match(/youtube\.com\/watch/))
		{
			return args;
		}

		const filteredUrl = url.split("&").filter(s => !s.match(/(range|rn|rbuf)=/)).join("&");
		const mime = filteredUrl.match(/mime=(\w+)/)[1];
		if(!mime)
		{
			return args;
		}
		media.set(mime, filteredUrl);

		button?.remove();
		const subButton = document.getElementsByClassName('ytp-subtitles-button')[0];
		button = subButton.cloneNode(true);
		button.setAttribute('title', "DL");
		button.innerHTML = icon;
		subButton.parentElement.insertBefore(button, subButton);
		button.addEventListener('click', () =>
		{
			console.log(Array.from(media.entries()).join('\n'))
			const body = Array.from(media.values()).join('\n');
			downloadText(`${document.title}.ucinmeta`, body)
		});

		return args;
	});

})();
