(()=>{"use strict";var e={895:()=>{try{self["workbox:cacheable-response:6.0.2"]&&_()}catch(e){}},913:()=>{try{self["workbox:core:6.0.2"]&&_()}catch(e){}},550:()=>{try{self["workbox:expiration:6.0.2"]&&_()}catch(e){}},977:()=>{try{self["workbox:precaching:6.0.2"]&&_()}catch(e){}},144:()=>{try{self["workbox:recipes:6.0.2"]&&_()}catch(e){}},80:()=>{try{self["workbox:routing:6.0.2"]&&_()}catch(e){}},873:()=>{try{self["workbox:strategies:6.0.2"]&&_()}catch(e){}}},t={};function s(n){if(t[n])return t[n].exports;var a=t[n]={exports:{}};return e[n](a,a.exports,s),a.exports}(()=>{s(913);class e extends Error{constructor(e,t){super(((e,...t)=>{let s=e;return t.length>0&&(s+=` :: ${JSON.stringify(t)}`),s})(e,t)),this.name=e,this.details=t}}s(80);const t=e=>e&&"object"==typeof e?e:{handle:e};class n{constructor(e,s,n="GET"){this.handler=t(s),this.match=e,this.method=n}}class a extends n{constructor(e,t,s){super((({url:t})=>{const s=e.exec(t.href);if(s&&(t.origin===location.origin||0===s.index))return s.slice(1)}),t,s)}}class r{constructor(){this._routes=new Map,this._defaultHandlerMap=new Map}get routes(){return this._routes}addFetchListener(){self.addEventListener("fetch",(e=>{const{request:t}=e,s=this.handleRequest({request:t,event:e});s&&e.respondWith(s)}))}addCacheListener(){self.addEventListener("message",(e=>{if(e.data&&"CACHE_URLS"===e.data.type){const{payload:t}=e.data,s=Promise.all(t.urlsToCache.map((t=>{"string"==typeof t&&(t=[t]);const s=new Request(...t);return this.handleRequest({request:s,event:e})})));e.waitUntil(s),e.ports&&e.ports[0]&&s.then((()=>e.ports[0].postMessage(!0)))}}))}handleRequest({request:e,event:t}){const s=new URL(e.url,location.href);if(!s.protocol.startsWith("http"))return;const n=s.origin===location.origin,{params:a,route:r}=this.findMatchingRoute({event:t,request:e,sameOrigin:n,url:s});let i=r&&r.handler;const c=e.method;if(!i&&this._defaultHandlerMap.has(c)&&(i=this._defaultHandlerMap.get(c)),!i)return;let o;try{o=i.handle({url:s,request:e,event:t,params:a})}catch(e){o=Promise.reject(e)}return o instanceof Promise&&this._catchHandler&&(o=o.catch((n=>this._catchHandler.handle({url:s,request:e,event:t})))),o}findMatchingRoute({url:e,sameOrigin:t,request:s,event:n}){const a=this._routes.get(s.method)||[];for(const r of a){let a;const i=r.match({url:e,sameOrigin:t,request:s,event:n});if(i)return a=i,(Array.isArray(i)&&0===i.length||i.constructor===Object&&0===Object.keys(i).length||"boolean"==typeof i)&&(a=void 0),{route:r,params:a}}return{}}setDefaultHandler(e,s="GET"){this._defaultHandlerMap.set(s,t(e))}setCatchHandler(e){this._catchHandler=t(e)}registerRoute(e){this._routes.has(e.method)||this._routes.set(e.method,[]),this._routes.get(e.method).push(e)}unregisterRoute(t){if(!this._routes.has(t.method))throw new e("unregister-route-but-not-found-with-method",{method:t.method});const s=this._routes.get(t.method).indexOf(t);if(!(s>-1))throw new e("unregister-route-route-not-registered");this._routes.get(t.method).splice(s,1)}}let i;const c=()=>(i||(i=new r,i.addFetchListener(),i.addCacheListener()),i);function o(t,s,r){let i;if("string"==typeof t){const e=new URL(t,location.href);i=new n((({url:t})=>t.href===e.href),s,r)}else if(t instanceof RegExp)i=new a(t,s,r);else if("function"==typeof t)i=new n(t,s,r);else{if(!(t instanceof n))throw new e("unsupported-route-type",{moduleName:"workbox-routing",funcName:"registerRoute",paramName:"capture"});i=t}return c().registerRoute(i),i}s(873);const h={cacheWillUpdate:async({response:e})=>200===e.status||0===e.status?e:null},l={googleAnalytics:"googleAnalytics",precache:"precache-v2",prefix:"workbox",runtime:"runtime",suffix:"undefined"!=typeof registration?registration.scope:""},u=e=>[l.prefix,e,l.suffix].filter((e=>e&&e.length>0)).join("-"),d=e=>e||u(l.precache),p=e=>e||u(l.runtime);function f(e,t){const s=new URL(e);for(const e of t)s.searchParams.delete(e);return s.href}class w{constructor(){this.promise=new Promise(((e,t)=>{this.resolve=e,this.reject=t}))}}const g=new Set;function m(e){return new Promise((t=>setTimeout(t,e)))}function y(e){return"string"==typeof e?new Request(e):e}class _{constructor(e,t){this._cacheKeys={},Object.assign(this,t),this.event=t.event,this._strategy=e,this._handlerDeferred=new w,this._extendLifetimePromises=[],this._plugins=[...e.plugins],this._pluginStateMap=new Map;for(const e of this._plugins)this._pluginStateMap.set(e,{});this.event.waitUntil(this._handlerDeferred.promise)}fetch(t){return this.waitUntil((async()=>{const{event:s}=this;let n=y(t);if("navigate"===n.mode&&s instanceof FetchEvent&&s.preloadResponse){const e=await s.preloadResponse;if(e)return e}const a=this.hasCallback("fetchDidFail")?n.clone():null;try{for(const e of this.iterateCallbacks("requestWillFetch"))n=await e({request:n.clone(),event:s})}catch(t){throw new e("plugin-error-request-will-fetch",{thrownError:t})}const r=n.clone();try{let e;e=await fetch(n,"navigate"===n.mode?void 0:this._strategy.fetchOptions);for(const t of this.iterateCallbacks("fetchDidSucceed"))e=await t({event:s,request:r,response:e});return e}catch(e){throw a&&await this.runCallbacks("fetchDidFail",{error:e,event:s,originalRequest:a.clone(),request:r.clone()}),e}})())}async fetchAndCachePut(e){const t=await this.fetch(e),s=t.clone();return this.waitUntil(this.cachePut(e,s)),t}cacheMatch(e){return this.waitUntil((async()=>{const t=y(e);let s;const{cacheName:n,matchOptions:a}=this._strategy,r=await this.getCacheKey(t,"read"),i={...a,cacheName:n};s=await caches.match(r,i);for(const e of this.iterateCallbacks("cachedResponseWillBeUsed"))s=await e({cacheName:n,matchOptions:a,cachedResponse:s,request:r,event:this.event})||void 0;return s})())}async cachePut(t,s){const n=y(t);await m(0);const a=await this.getCacheKey(n,"write");if(!s)throw new e("cache-put-with-no-response",{url:(r=a.url,new URL(String(r),location.href).href.replace(new RegExp(`^${location.origin}`),""))});var r;const i=await this._ensureResponseSafeToCache(s);if(!i)return;const{cacheName:c,matchOptions:o}=this._strategy,h=await self.caches.open(c),l=this.hasCallback("cacheDidUpdate"),u=l?await async function(e,t,s,n){const a=f(t.url,s);if(t.url===a)return e.match(t,n);const r={...n,ignoreSearch:!0},i=await e.keys(t,r);for(const t of i)if(a===f(t.url,s))return e.match(t,n)}(h,a.clone(),["__WB_REVISION__"],o):null;try{await h.put(a,l?i.clone():i)}catch(e){throw"QuotaExceededError"===e.name&&await async function(){for(const e of g)await e()}(),e}for(const e of this.iterateCallbacks("cacheDidUpdate"))await e({cacheName:c,oldResponse:u,newResponse:i.clone(),request:a,event:this.event})}async getCacheKey(e,t){if(!this._cacheKeys[t]){let s=e;for(const e of this.iterateCallbacks("cacheKeyWillBeUsed"))s=y(await e({mode:t,request:s,event:this.event,params:this.params}));this._cacheKeys[t]=s}return this._cacheKeys[t]}hasCallback(e){for(const t of this._strategy.plugins)if(e in t)return!0;return!1}async runCallbacks(e,t){for(const s of this.iterateCallbacks(e))await s(t)}*iterateCallbacks(e){for(const t of this._strategy.plugins)if("function"==typeof t[e]){const s=this._pluginStateMap.get(t),n=n=>{const a={...n,state:s};return t[e](a)};yield n}}waitUntil(e){return this._extendLifetimePromises.push(e),e}async doneWaiting(){let e;for(;e=this._extendLifetimePromises.shift();)await e}destroy(){this._handlerDeferred.resolve()}async _ensureResponseSafeToCache(e){let t=e,s=!1;for(const e of this.iterateCallbacks("cacheWillUpdate"))if(t=await e({request:this.request,response:t,event:this.event})||void 0,s=!0,!t)break;return s||t&&200!==t.status&&(t=void 0),t}}class R{constructor(e={}){this.cacheName=p(e.cacheName),this.plugins=e.plugins||[],this.fetchOptions=e.fetchOptions,this.matchOptions=e.matchOptions}handle(e){const[t]=this.handleAll(e);return t}handleAll(e){e instanceof FetchEvent&&(e={event:e,request:e.request});const t=e.event,s="string"==typeof e.request?new Request(e.request):e.request,n="params"in e?e.params:void 0,a=new _(this,{event:t,request:s,params:n}),r=this._getResponse(a,s,t);return[r,this._awaitComplete(r,a,s,t)]}async _getResponse(t,s,n){let a;await t.runCallbacks("handlerWillStart",{event:n,request:s});try{if(a=await this._handle(s,t),!a||"error"===a.type)throw new e("no-response",{url:s.url})}catch(e){for(const r of t.iterateCallbacks("handlerDidError"))if(a=await r({error:e,event:n,request:s}),a)break;if(!a)throw e}for(const e of t.iterateCallbacks("handlerWillRespond"))a=await e({event:n,request:s,response:a});return a}async _awaitComplete(e,t,s,n){let a,r;try{a=await e}catch(r){}try{await t.runCallbacks("handlerDidRespond",{event:n,request:s,response:a}),await t.doneWaiting()}catch(e){r=e}if(await t.runCallbacks("handlerDidComplete",{event:n,request:s,response:a,error:r}),t.destroy(),r)throw r}}class b extends R{constructor(e){super(e),this.plugins.some((e=>"cacheWillUpdate"in e))||this.plugins.unshift(h)}async _handle(t,s){const n=s.fetchAndCachePut(t).catch((()=>{}));let a,r=await s.cacheMatch(t);if(r);else try{r=await n}catch(e){a=e}if(!r)throw new e("no-response",{url:t.url,error:a});return r}}class x extends R{async _handle(t,s){let n,a=await s.cacheMatch(t);if(a);else try{a=await s.fetchAndCachePut(t)}catch(e){n=e}if(!a)throw new e("no-response",{url:t.url,error:n});return a}}s(895);class v{constructor(e={}){this._statuses=e.statuses,this._headers=e.headers}isResponseCacheable(e){let t=!0;return this._statuses&&(t=this._statuses.includes(e.status)),this._headers&&t&&(t=Object.keys(this._headers).some((t=>e.headers.get(t)===this._headers[t]))),t}}class C{constructor(e){this.cacheWillUpdate=async({response:e})=>this._cacheableResponse.isResponseCacheable(e)?e:null,this._cacheableResponse=new v(e)}}function k(e){e.then((()=>{}))}class U{constructor(e,t,{onupgradeneeded:s,onversionchange:n}={}){this._db=null,this._name=e,this._version=t,this._onupgradeneeded=s,this._onversionchange=n||(()=>this.close())}get db(){return this._db}async open(){if(!this._db)return this._db=await new Promise(((e,t)=>{let s=!1;setTimeout((()=>{s=!0,t(new Error("The open request was blocked and timed out"))}),this.OPEN_TIMEOUT);const n=indexedDB.open(this._name,this._version);n.onerror=()=>t(n.error),n.onupgradeneeded=e=>{s?(n.transaction.abort(),n.result.close()):"function"==typeof this._onupgradeneeded&&this._onupgradeneeded(e)},n.onsuccess=()=>{const t=n.result;s?t.close():(t.onversionchange=this._onversionchange.bind(this),e(t))}})),this}async getKey(e,t){return(await this.getAllKeys(e,t,1))[0]}async getAll(e,t,s){return await this.getAllMatching(e,{query:t,count:s})}async getAllKeys(e,t,s){return(await this.getAllMatching(e,{query:t,count:s,includeKeys:!0})).map((e=>e.key))}async getAllMatching(e,{index:t,query:s=null,direction:n="next",count:a,includeKeys:r=!1}={}){return await this.transaction([e],"readonly",((i,c)=>{const o=i.objectStore(e),h=t?o.index(t):o,l=[],u=h.openCursor(s,n);u.onsuccess=()=>{const e=u.result;e?(l.push(r?e:e.value),a&&l.length>=a?c(l):e.continue()):c(l)}}))}async transaction(e,t,s){return await this.open(),await new Promise(((n,a)=>{const r=this._db.transaction(e,t);r.onabort=()=>a(r.error),r.oncomplete=()=>n(),s(r,(e=>n(e)))}))}async _call(e,t,s,...n){return await this.transaction([t],s,((s,a)=>{const r=s.objectStore(t),i=r[e].apply(r,n);i.onsuccess=()=>a(i.result)}))}close(){this._db&&(this._db.close(),this._db=null)}}U.prototype.OPEN_TIMEOUT=2e3;const q={readonly:["get","count","getKey","getAll","getAllKeys"],readwrite:["add","put","clear","delete"]};for(const[e,t]of Object.entries(q))for(const s of t)s in IDBObjectStore.prototype&&(U.prototype[s]=async function(t,...n){return await this._call(s,t,e,...n)});s(550);const T="cache-entries",L=e=>{const t=new URL(e,location.href);return t.hash="",t.href};class E{constructor(e){this._cacheName=e,this._db=new U("workbox-expiration",1,{onupgradeneeded:e=>this._handleUpgrade(e)})}_handleUpgrade(e){const t=e.target.result.createObjectStore(T,{keyPath:"id"});t.createIndex("cacheName","cacheName",{unique:!1}),t.createIndex("timestamp","timestamp",{unique:!1}),(async e=>{await new Promise(((t,s)=>{const n=indexedDB.deleteDatabase(e);n.onerror=()=>{s(n.error)},n.onblocked=()=>{s(new Error("Delete blocked"))},n.onsuccess=()=>{t()}}))})(this._cacheName)}async setTimestamp(e,t){const s={url:e=L(e),timestamp:t,cacheName:this._cacheName,id:this._getId(e)};await this._db.put(T,s)}async getTimestamp(e){return(await this._db.get(T,this._getId(e))).timestamp}async expireEntries(e,t){const s=await this._db.transaction(T,"readwrite",((s,n)=>{const a=s.objectStore(T).index("timestamp").openCursor(null,"prev"),r=[];let i=0;a.onsuccess=()=>{const s=a.result;if(s){const n=s.value;n.cacheName===this._cacheName&&(e&&n.timestamp<e||t&&i>=t?r.push(s.value):i++),s.continue()}else n(r)}})),n=[];for(const e of s)await this._db.delete(T,e.id),n.push(e.url);return n}_getId(e){return this._cacheName+"|"+L(e)}}class N{constructor(e,t={}){this._isRunning=!1,this._rerunRequested=!1,this._maxEntries=t.maxEntries,this._maxAgeSeconds=t.maxAgeSeconds,this._matchOptions=t.matchOptions,this._cacheName=e,this._timestampModel=new E(e)}async expireEntries(){if(this._isRunning)return void(this._rerunRequested=!0);this._isRunning=!0;const e=this._maxAgeSeconds?Date.now()-1e3*this._maxAgeSeconds:0,t=await this._timestampModel.expireEntries(e,this._maxEntries),s=await self.caches.open(this._cacheName);for(const e of t)await s.delete(e,this._matchOptions);this._isRunning=!1,this._rerunRequested&&(this._rerunRequested=!1,k(this.expireEntries()))}async updateTimestamp(e){await this._timestampModel.setTimestamp(e,Date.now())}async isURLExpired(e){return!!this._maxAgeSeconds&&await this._timestampModel.getTimestamp(e)<Date.now()-1e3*this._maxAgeSeconds}async delete(){this._rerunRequested=!1,await this._timestampModel.expireEntries(1/0)}}class K{constructor(e={}){var t;this.cachedResponseWillBeUsed=async({event:e,request:t,cacheName:s,cachedResponse:n})=>{if(!n)return null;const a=this._isResponseDateFresh(n),r=this._getCacheExpiration(s);k(r.expireEntries());const i=r.updateTimestamp(t.url);if(e)try{e.waitUntil(i)}catch(e){}return a?n:null},this.cacheDidUpdate=async({cacheName:e,request:t})=>{const s=this._getCacheExpiration(e);await s.updateTimestamp(t.url),await s.expireEntries()},this._config=e,this._maxAgeSeconds=e.maxAgeSeconds,this._cacheExpirations=new Map,e.purgeOnQuotaError&&(t=()=>this.deleteCacheAndMetadata(),g.add(t))}_getCacheExpiration(t){if(t===p())throw new e("expire-custom-caches-only");let s=this._cacheExpirations.get(t);return s||(s=new N(t,this._config),this._cacheExpirations.set(t,s)),s}_isResponseDateFresh(e){if(!this._maxAgeSeconds)return!0;const t=this._getDateHeaderTimestamp(e);return null===t||t>=Date.now()-1e3*this._maxAgeSeconds}_getDateHeaderTimestamp(e){if(!e.headers.has("date"))return null;const t=e.headers.get("date"),s=new Date(t).getTime();return isNaN(s)?null:s}async deleteCacheAndMetadata(){for(const[e,t]of this._cacheExpirations)await self.caches.delete(e),await t.delete();this._cacheExpirations=new Map}}function A(e,t){const s=t();return e.waitUntil(s),s}function S(t){if(!t)throw new e("add-to-cache-list-unexpected-type",{entry:t});if("string"==typeof t){const e=new URL(t,location.href);return{cacheKey:e.href,url:e.href}}const{revision:s,url:n}=t;if(!n)throw new e("add-to-cache-list-unexpected-type",{entry:t});if(!s){const e=new URL(n,location.href);return{cacheKey:e.href,url:e.href}}const a=new URL(n,location.href),r=new URL(n,location.href);return a.searchParams.set("__WB_REVISION__",s),{cacheKey:a.href,url:r.href}}s(144),s(977);class M{constructor(){this.updatedURLs=[],this.notUpdatedURLs=[],this.handlerWillStart=async({request:e,state:t})=>{t&&(t.originalRequest=e)},this.cachedResponseWillBeUsed=async({event:e,state:t,cachedResponse:s})=>{if("install"===e.type){const e=t.originalRequest.url;s?this.notUpdatedURLs.push(e):this.updatedURLs.push(e)}return s}}}class O{constructor({precacheController:e}){this.cacheKeyWillBeUsed=async({request:e,params:t})=>{const s=t&&t.cacheKey||this._precacheController.getCacheKeyForURL(e.url);return s?new Request(s):e},this._precacheController=e}}let P;const D={cacheWillUpdate:async({response:t})=>t.redirected?await async function(t,s){let n=null;if(t.url&&(n=new URL(t.url).origin),n!==self.location.origin)throw new e("cross-origin-copy-response",{origin:n});const a=t.clone(),r={headers:new Headers(a.headers),status:a.status,statusText:a.statusText},i=s?s(r):r,c=function(){if(void 0===P){const e=new Response("");if("body"in e)try{new Response(e.body),P=!0}catch(e){P=!1}P=!1}return P}()?a.body:await a.blob();return new Response(c,i)}(t):t};class W extends R{constructor(e={}){e.cacheName=d(e.cacheName),super(e),this._fallbackToNetwork=!1!==e.fallbackToNetwork,this.plugins.push(D)}async _handle(e,t){return await t.cacheMatch(e)||(t.event&&"install"===t.event.type?await this._handleInstall(e,t):await this._handleFetch(e,t))}async _handleFetch(t,s){let n;if(!this._fallbackToNetwork)throw new e("missing-precache-entry",{cacheName:this.cacheName,url:t.url});return n=await s.fetch(t),n}async _handleInstall(t,s){const n=await s.fetchAndCachePut(t);let a=Boolean(n);if(n&&n.status>=400&&!this._usesCustomCacheableResponseLogic()&&(a=!1),!a)throw new e("bad-precaching-response",{url:t.url,status:n.status});return n}_usesCustomCacheableResponseLogic(){return this.plugins.some((e=>e.cacheWillUpdate&&e!==D))}}class I{constructor({cacheName:e,plugins:t=[],fallbackToNetwork:s=!0}={}){this._urlsToCacheKeys=new Map,this._urlsToCacheModes=new Map,this._cacheKeysToIntegrities=new Map,this._strategy=new W({cacheName:d(e),plugins:[...t,new O({precacheController:this})],fallbackToNetwork:s}),this.install=this.install.bind(this),this.activate=this.activate.bind(this)}get strategy(){return this._strategy}precache(e){this.addToCacheList(e),this._installAndActiveListenersAdded||(self.addEventListener("install",this.install),self.addEventListener("activate",this.activate),this._installAndActiveListenersAdded=!0)}addToCacheList(t){const s=[];for(const n of t){"string"==typeof n?s.push(n):n&&void 0===n.revision&&s.push(n.url);const{cacheKey:t,url:a}=S(n),r="string"!=typeof n&&n.revision?"reload":"default";if(this._urlsToCacheKeys.has(a)&&this._urlsToCacheKeys.get(a)!==t)throw new e("add-to-cache-list-conflicting-entries",{firstEntry:this._urlsToCacheKeys.get(a),secondEntry:t});if("string"!=typeof n&&n.integrity){if(this._cacheKeysToIntegrities.has(t)&&this._cacheKeysToIntegrities.get(t)!==n.integrity)throw new e("add-to-cache-list-conflicting-integrities",{url:a});this._cacheKeysToIntegrities.set(t,n.integrity)}if(this._urlsToCacheKeys.set(a,t),this._urlsToCacheModes.set(a,r),s.length>0){const e=`Workbox is precaching URLs without revision info: ${s.join(", ")}\nThis is generally NOT safe. Learn more at https://bit.ly/wb-precache`;console.warn(e)}}}install(e){return A(e,(async()=>{const t=new M;this.strategy.plugins.push(t);for(const[t,s]of this._urlsToCacheKeys){const n=this._cacheKeysToIntegrities.get(s),a=this._urlsToCacheModes.get(t),r=new Request(t,{integrity:n,cache:a,credentials:"same-origin"});await Promise.all(this.strategy.handleAll({params:{cacheKey:s},request:r,event:e}))}const{updatedURLs:s,notUpdatedURLs:n}=t;return{updatedURLs:s,notUpdatedURLs:n}}))}activate(e){return A(e,(async()=>{const e=await self.caches.open(this.strategy.cacheName),t=await e.keys(),s=new Set(this._urlsToCacheKeys.values()),n=[];for(const a of t)s.has(a.url)||(await e.delete(a),n.push(a.url));return{deletedURLs:n}}))}getURLsToCacheKeys(){return this._urlsToCacheKeys}getCachedURLs(){return[...this._urlsToCacheKeys.keys()]}getCacheKeyForURL(e){const t=new URL(e,location.href);return this._urlsToCacheKeys.get(t.href)}async matchPrecache(e){const t=e instanceof Request?e.url:e,s=this.getCacheKeyForURL(t);if(s)return(await self.caches.open(this.strategy.cacheName)).match(s)}createHandlerBoundToURL(t){const s=this.getCacheKeyForURL(t);if(!s)throw new e("non-precached-url",{url:t});return e=>(e.request=new Request(t),e.params={cacheKey:s,...e.params},this.strategy.handle(e))}}let F;const j=()=>(F||(F=new I),F);function H(e){return j().matchPrecache(e)}class B extends n{constructor(e,t){super((({request:s})=>{const n=e.getURLsToCacheKeys();for(const e of function*(e,{ignoreURLParametersMatching:t=[/^utm_/,/^fbclid$/],directoryIndex:s="index.html",cleanURLs:n=!0,urlManipulation:a}={}){const r=new URL(e,location.href);r.hash="",yield r.href;const i=function(e,t=[]){for(const s of[...e.searchParams.keys()])t.some((e=>e.test(s)))&&e.searchParams.delete(s);return e}(r,t);if(yield i.href,s&&i.pathname.endsWith("/")){const e=new URL(i.href);e.pathname+=s,yield e.href}if(n){const e=new URL(i.href);e.pathname+=".html",yield e.href}if(a){const e=a({url:r});for(const t of e)yield t.href}}(s.url,t)){const t=n.get(e);if(t)return{cacheKey:t}}}),e.strategy)}}function $(e,t){!function(e){j().precache(e)}(e),function(e){const t=j();o(new B(t,e))}(t)}class G extends R{constructor(e={}){super(e),this._networkTimeoutSeconds=e.networkTimeoutSeconds||0}async _handle(t,s){let n,a;try{const e=[s.fetch(t)];if(this._networkTimeoutSeconds){const t=m(1e3*this._networkTimeoutSeconds);e.push(t)}if(a=await Promise.race(e),!a)throw new Error(`Timed out the network response after ${this._networkTimeoutSeconds} seconds.`)}catch(e){n=e}if(!a)throw new e("no-response",{url:t.url,error:n});return a}}$([{'revision':'52e07ee92b8ef00e85166e86c883e04a','url':'app.bundle.js'},{'revision':'275fe79abee3b697f1673c8bd9c58856','url':'app.bundle.js.LICENSE.txt'}]),function(e={}){const t=`${e.cachePrefix||"google-fonts"}-stylesheets`,s=`${e.cachePrefix||"google-fonts"}-webfonts`,n=e.maxAgeSeconds||31536e3,a=e.maxEntries||30;o((({url:e})=>"https://fonts.googleapis.com"===e.origin),new b({cacheName:t})),o((({url:e})=>"https://fonts.gstatic.com"===e.origin),new x({cacheName:s,plugins:[new C({statuses:[0,200]}),new K({maxAgeSeconds:n,maxEntries:a})]}))}(),function(e={}){const t=e.cacheName||"static-resources";o(e.matchCallback||(({request:e})=>"style"===e.destination||"script"===e.destination||"worker"===e.destination),new b({cacheName:t,plugins:[new C({statuses:[0,200]})]}))}(),function(e={}){const t=e.cacheName||"images",s=e.matchCallback||(({request:e})=>"image"===e.destination),n=e.maxAgeSeconds||2592e3,a=e.maxEntries||60;o(s,new x({cacheName:t,plugins:[new C({statuses:[0,200]}),new K({maxEntries:a,maxAgeSeconds:n})]}))}(),function(e={}){const t=e.pageFallback||"offline.html",s=e.imageFallback||!1,n=e.fontFallback||!1;c().setCatchHandler((async e=>{const a=e.request.destination;return"document"===a?await H(t)||Response.error():"image"===a&&!1!==s?await H(s)||Response.error():"font"===a&&!1!==n&&await H(n)||Response.error()}))}(),self.addEventListener("fetch",(e=>{const{request:t}=e,s=new URL(t.url);s.origin===location.origin&&"/feedback"===s.pathname&&e.respondWith((new G).handle({event:e,request:t}).catch((()=>caches.match("/offline/feedback/index.html"))))})),$([{url:"/offline/feedback/index.html",revision:"1"}])})()})();
//# sourceMappingURL=sw.bundle.js.map