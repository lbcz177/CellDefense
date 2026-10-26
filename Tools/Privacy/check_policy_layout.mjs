// Local Chrome CDP check; no browser libraries or page-side scripts are shipped.
import {writeFile} from 'node:fs/promises';
const pages = await (await fetch('http://127.0.0.1:9229/json')).json();
const page = pages.find(p => p.type === 'page');
const socket = new WebSocket(page.webSocketDebuggerUrl);
await new Promise(resolve => socket.addEventListener('open', resolve, {once:true}));
let sequence = 0;
const pending = new Map();
socket.addEventListener('message', event => {
  const message = JSON.parse(event.data);
  const resolve = pending.get(message.id);
  if (resolve) { pending.delete(message.id); resolve(message); }
});
const call = (method, params = {}) => new Promise((resolve, reject) => {
  const id = ++sequence;
  pending.set(id, result => result.error ? reject(new Error(JSON.stringify(result.error))) : resolve(result.result));
  socket.send(JSON.stringify({id, method, params}));
});
try {
  await call('Page.enable');
  await call('Page.navigate', {url:'http://127.0.0.1:8877/'});
  const results = [];
  for (const width of [390, 1280]) {
    await call('Emulation.setDeviceMetricsOverride', {width,height:960,deviceScaleFactor:1,mobile:width<500});
    await new Promise(resolve => setTimeout(resolve, 400));
    const {result} = await call('Runtime.evaluate', {expression:`JSON.stringify({width:innerWidth,client:document.documentElement.clientWidth,scroll:document.documentElement.scrollWidth,sections:document.querySelectorAll('article section').length,navColumns:getComputedStyle(document.querySelector('nav')).gridTemplateColumns,contact:document.body.textContent.includes('2558057120')})`,returnByValue:true});
    const metrics = JSON.parse(result.value);
    if (metrics.scroll > metrics.client || metrics.width !== width || !metrics.contact || metrics.sections !== 8) throw new Error(JSON.stringify(metrics));
    const capture = await call('Page.captureScreenshot', {format:'png',captureBeyondViewport:false});
    await writeFile(`Assets/_Project/Art/Sprites/细胞绘制/隐私政策站点_2026-10-22/验证~/privacy-${width}-verified.png`, Buffer.from(capture.data, 'base64'));
    results.push(metrics);
  }
  await writeFile('Logs/PrivacyFix20261022/site-layout.json',JSON.stringify(results,null,2));
  console.log(JSON.stringify(results));
} finally { socket.close(); }
