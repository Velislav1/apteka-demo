let handler;

export function start(dotNet) {
  handler = (event) => {
    if ((event.key === "F5" || event.key === "F6") && !event.ctrlKey && !event.altKey) {
      event.preventDefault();
      dotNet.invokeMethodAsync("OnKey", event.key);
    }
  };
  document.addEventListener("keydown", handler, true);
}

export function stop() {
  if (handler) {
    document.removeEventListener("keydown", handler, true);
  }
}
