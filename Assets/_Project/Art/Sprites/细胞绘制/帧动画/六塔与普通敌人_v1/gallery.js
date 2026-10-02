"use strict";
const clips = [
  ["01_neutrophil_attack", "中性粒细胞哨兵", "攻击", "tower"],
  ["02_fibrin_attack", "纤维蛋白壁垒", "攻击 / 隔离脉冲", "tower"],
  ["03_lysosome_attack", "溶酶体酸化炮", "攻击", "tower"],
  ["04_neural_attack", "神经脉冲刺突", "攻击", "tower"],
  ["05_phagocyte_attack", "巨噬细胞清道夫", "攻击 / 吞噬", "tower"],
  ["06_antibody_attack", "抗体 B 细胞中继", "攻击", "tower"],
  ["07_staph_move", "葡萄球菌群", "移动 · 已确认版本", "enemy"],
  ["08_staph_attack", "葡萄球菌群", "攻击", "enemy"],
  ["09_diplococcus_move", "鞭毛双球菌", "移动", "enemy"],
  ["10_diplococcus_attack", "鞭毛双球菌", "攻击", "enemy"],
  ["11_bacillus_move", "重甲芽孢杆菌", "移动", "enemy"],
  ["12_bacillus_attack", "重甲芽孢杆菌", "攻击", "enemy"],
];
const gallery = document.querySelector("#gallery");
const play = document.querySelector("#play");
const counter = document.querySelector("#frame");
const status = document.querySelector("#status");
let frame = 0;
let fps = 10;
let playing = true;
let loaded = 0;
let failed = 0;
let previous = 0;
let elapsed = 0;
const views = clips.map(([id, name, action, category], index) => {
  const article = document.createElement("article");
  article.dataset.category = category;
  article.innerHTML = `<div class="stage"><span class="badge">${String(index + 1).padStart(2, "0")} · ${category === "tower" ? "防御塔" : "敌人"}</span><canvas width="128" height="128" aria-label="${name}${action}动画"></canvas></div><div class="details"><h2>${name}</h2><div class="meta">${action} · 8 帧 / 0.8 秒</div><div class="links"><a href="${id}/animation_sheet.png">精灵图 PNG</a><a href="${id}/preview.gif">动画 GIF</a><a href="${id}/animation.aseprite">Aseprite</a></div></div>`;
  gallery.append(article);
  const context = article.querySelector("canvas").getContext("2d");
  context.imageSmoothingEnabled = false;
  const image = new Image();
  const view = { article, context, image, ready: false };
  image.onload = () => {
    view.ready = true;
    loaded += 1;
    updateStatus();
    draw(view);
  };
  image.onerror = () => { failed += 1; updateStatus(); };
  image.src = `${id}/animation_sheet.png`;
  return view;
});
function updateStatus() {
  status.textContent = failed ? `已载入 ${loaded} / 12 套；${failed} 套图片未能读取，请保留预览页与素材文件夹的相对位置。` : `已载入 ${loaded} / 12 套${loaded === 12 ? " · 可离线使用" : "…"}`;
  status.classList.toggle("error", failed > 0);
}
function draw(view) {
  if (!view.ready || view.article.hidden) return;
  view.context.clearRect(0, 0, 128, 128);
  view.context.drawImage(view.image, (frame % 4) * 128, Math.floor(frame / 4) * 128, 128, 128, 0, 0, 128, 128);
}
function render() {
  views.forEach(draw);
  counter.value = `帧 ${frame + 1} / 8`;
}
function setPlaying(value) {
  playing = value;
  elapsed = 0;
  play.textContent = playing ? "暂停" : "播放";
  play.setAttribute("aria-pressed", String(playing));
}
play.addEventListener("click", () => setPlaying(!playing));
document.querySelector("#step").addEventListener("click", () => {
  setPlaying(false);
  frame = (frame + 1) % 8;
  render();
});
document.querySelector("#speed").addEventListener("change", event => { fps = Number(event.target.value); elapsed = 0; });
document.querySelector("#background").addEventListener("change", event => document.documentElement.style.setProperty("--stage", event.target.value));
document.querySelector("#filter").addEventListener("change", event => {
  const value = event.target.value;
  views.forEach(view => { view.article.hidden = value !== "all" && view.article.dataset.category !== value; });
  render();
});
document.addEventListener("visibilitychange", () => { previous = 0; elapsed = 0; });
function tick(now) {
  if (previous && playing && !document.hidden) {
    elapsed += Math.min(now - previous, 250);
    const steps = Math.floor(elapsed / (1000 / fps));
    if (steps) {
      frame = (frame + steps) % 8;
      elapsed %= 1000 / fps;
      render();
    }
  }
  previous = now;
  requestAnimationFrame(tick);
}
requestAnimationFrame(tick);
