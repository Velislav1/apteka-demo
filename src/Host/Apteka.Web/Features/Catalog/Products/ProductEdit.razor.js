// F11 opens the „Мерки“ tab of the product card, as in Apteka Expert 2 (instead of the browser's full screen).
let handler;

export function start() {
  handler = (event) => {
    if (event.key === "F11") {
      event.preventDefault();
      document.getElementById("tab-Units")?.click();
    }
  };
  document.addEventListener("keydown", handler, true);
}

export function stop() {
  if (handler) {
    document.removeEventListener("keydown", handler, true);
  }
}
