import "./app.css";
import { mount } from "svelte";
import App from "./App.svelte";
import { book, startLive } from "$lib/frame";

const target = document.getElementById("app");
if (!target) {
  throw new Error("The book could not start: no #app element.");
}
mount(App, { target });

// A served book follows its rebuilds; a book opened from file:// hears nothing.
startLive((event) => {
  if (event.kind === "building") book.markBuilding();
  else if (event.kind === "failed") book.markFailed(event.error);
  else book.markBuilt(event.generatedOn);
});
