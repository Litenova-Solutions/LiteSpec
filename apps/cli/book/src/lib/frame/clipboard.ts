// Copy-to-clipboard with a textarea fallback for file:// where the async API is missing.

/** Copies text to the clipboard, falling back to a textarea; true when it worked. */
export async function copyText(text: string): Promise<boolean> {
  if (typeof navigator !== "undefined" && navigator.clipboard?.writeText) {
    try {
      await navigator.clipboard.writeText(text);
      return true;
    } catch {
      // The async clipboard is unavailable; the textarea fallback below still tries.
    }
  }
  try {
    if (typeof document === "undefined") return false;
    const area = document.createElement("textarea");
    area.value = text;
    area.setAttribute("readonly", "");
    area.style.position = "fixed";
    area.style.opacity = "0";
    document.body.appendChild(area);
    area.select();
    try {
      document.execCommand("copy");
    } catch {
      area.remove();
      return false;
    }
    area.remove();
    return true;
  } catch {
    return false;
  }
}
