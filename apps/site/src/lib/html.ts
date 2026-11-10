// The one HTML escaper the site writes into markup. Text and attribute values
// escape the same four characters, so every template and builder calls this.

/** Escapes a string for an HTML text node or a double-quoted attribute value. */
export function escape(value: string): string {
  return value.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
}
