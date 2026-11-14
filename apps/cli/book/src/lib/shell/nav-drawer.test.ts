import { describe, expect, it } from "vitest";
import { NavDrawer } from "./nav-drawer.svelte";

// The narrow-screen drawer: it opens on the selected record and closes on a
// choice, Escape, the backdrop, or a widen past the breakpoint.
function drawerOn(selected: { id: string | null }, narrow = true): NavDrawer {
  return new NavDrawer(() => selected.id, narrow);
}

describe("the navigation drawer", () => {
  it("starts closed and opens on the record selected", () => {
    const selected = { id: "a" };
    const nav = drawerOn(selected);

    expect(nav.open).toBe(false);
    nav.openOn();
    expect(nav.open).toBe(true);
    expect(nav.drawer).toBe(true);
  });

  it("closes when a different record is chosen", () => {
    const selected = { id: "a" };
    const nav = drawerOn(selected);

    nav.openOn();
    selected.id = "b";
    expect(nav.open).toBe(false);
    expect(nav.drawer).toBe(false);
  });

  it("closes when the record already selected is chosen, and reports that it was open", () => {
    const selected = { id: "a" };
    const nav = drawerOn(selected);

    nav.openOn();
    // Choosing the selected record changes no selection, so the drawer closes through close().
    expect(nav.close()).toBe(true);
    expect(nav.open).toBe(false);
  });

  it("reports that nothing closed when it was already shut, so focus stays put", () => {
    const nav = drawerOn({ id: "a" });

    expect(nav.close()).toBe(false);
  });

  it("closes on Escape only while it covers the page", () => {
    const selected = { id: "a" };
    const nav = drawerOn(selected);

    nav.openOn();
    expect(nav.escape()).toBe(true);
    expect(nav.open).toBe(false);

    const wide = drawerOn(selected, false);
    wide.openOn();
    expect(wide.escape()).toBe(false);
  });

  it("closes from the backdrop through close", () => {
    const nav = drawerOn({ id: "a" });

    nav.openOn();
    expect(nav.close()).toBe(true);
    expect(nav.drawer).toBe(false);
  });

  it("closes when the layout widens while it is open, and a narrow again starts closed", () => {
    const nav = drawerOn({ id: "a" });

    nav.openOn();
    nav.setNarrow(false);
    expect(nav.open).toBe(false);

    nav.setNarrow(true);
    expect(nav.open).toBe(false);
    expect(nav.drawer).toBe(false);
  });

  it("never covers the page on a wide layout, even when open", () => {
    const nav = drawerOn({ id: "a" }, false);

    nav.openOn();
    expect(nav.open).toBe(true);
    expect(nav.drawer).toBe(false);
  });
});
