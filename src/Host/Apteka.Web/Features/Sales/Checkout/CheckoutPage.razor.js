// Keeps the till keyboard-only: the till input always has the focus, and F-keys do not trigger browser actions.
const handledKeys = new Set(["F2", "F3", "F9", "Escape"]);

function onKeyDown(event) {
  if (handledKeys.has(event.key)) {
    event.preventDefault();
  }
}

function keepFocus() {
  const input = document.getElementById("till-input");
  if (input && document.activeElement !== input) {
    input.focus();
  }
}

let timer;

export function start() {
  document.addEventListener("keydown", onKeyDown, true);
  document.addEventListener("focusout", () => setTimeout(keepFocus, 0));
  timer = setInterval(keepFocus, 500);
  keepFocus();
}

export function stop() {
  document.removeEventListener("keydown", onKeyDown, true);
  clearInterval(timer);
}

export function focus() {
  keepFocus();
}
