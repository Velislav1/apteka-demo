// Keys of the складова разписка, as in Apteka Expert 2: F6 edit, F8 delete, F12 unit, Ctrl+Enter приключване.
// Their browser meaning (F5 reload, F6 address bar, F12 developer tools) is blocked on this screen.
const blocked = new Set(["F5", "F6", "F8", "F12"]);

function onKeyDown(event) {
  if (blocked.has(event.key) || (event.key === "Enter" && event.ctrlKey)) {
    event.preventDefault();
  }
}

export function start() {
  document.addEventListener("keydown", onKeyDown, true);
}

export function stop() {
  document.removeEventListener("keydown", onKeyDown, true);
}
