// Keeps the till keyboard-only: the till input always has the focus, and the till's keys
// (F2, F4, F8, F10, F12, Ctrl+Enter, Esc, arrows) never trigger browser actions.
const handledKeys = new Set(["F2", "F4", "F9", "F8", "F10", "F12", "Escape", "ArrowUp", "ArrowDown"]);

function onKeyDown(event) {
  if (handledKeys.has(event.key) || (event.key === "Enter" && event.ctrlKey)) {
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
