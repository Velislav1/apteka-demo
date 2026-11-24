// Global shortcuts, the same as in Apteka Expert 2.
const shortcuts = {
  F1: "purchasing/deliveries/new", // Ctrl+F1 — складова разписка
  F3: "sales/checkout",            // Ctrl+F3 — директна продажба
};

function onKeyDown(event) {
  if (event.ctrlKey && !event.altKey && shortcuts[event.key]) {
    event.preventDefault();
    window.Blazor.navigateTo(shortcuts[event.key]);
  }
}

export function start() {
  document.addEventListener("keydown", onKeyDown, true);
}

export function stop() {
  document.removeEventListener("keydown", onKeyDown, true);
}
