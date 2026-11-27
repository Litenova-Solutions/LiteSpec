// A resizable shell panel: the navigation and the rail share this width, drag
// and key logic. Each panel keeps its own storage key, default, min and max.
// A left panel widens when its border moves right; a right panel (invert)
// widens when its border moves left.

export interface PanelWidthOptions {
  /** The localStorage key the width is remembered under. */
  key: string;
  defaultWidth: number;
  min: number;
  max: number;
  /** True when dragging left widens the panel, as the right rail does. */
  invert: boolean;
}

export class PanelWidth {
  width = $state(0);
  dragging = $state(false);
  readonly min: number;
  readonly max: number;
  private readonly key: string;
  private readonly direction: 1 | -1;
  private startX = 0;
  private startWidth = 0;

  constructor(options: PanelWidthOptions) {
    this.key = options.key;
    this.min = options.min;
    this.max = options.max;
    this.direction = options.invert ? -1 : 1;
    let width = options.defaultWidth;
    try {
      const kept = window.localStorage.getItem(this.key);
      const parsed = kept === null ? Number.NaN : Number.parseInt(kept, 10);
      if (Number.isFinite(parsed)) width = Math.min(this.max, Math.max(this.min, parsed));
    } catch {
      /* A reader without storage reads at the default width. */
    }
    this.width = width;
    this.startWidth = width;
  }

  setWidth(next: number): void {
    this.width = Math.min(this.max, Math.max(this.min, Math.round(next)));
    try {
      window.localStorage.setItem(this.key, String(this.width));
    } catch {
      /* A reader without storage still resizes for this visit. */
    }
  }

  dragStart(e: PointerEvent): void {
    if (e.button !== 0 || e.isPrimary === false) return;
    e.preventDefault();
    this.startX = e.clientX;
    this.startWidth = this.width;
    this.dragging = true;
    if (e.currentTarget instanceof HTMLElement) e.currentTarget.setPointerCapture(e.pointerId);
  }

  dragMove(e: PointerEvent): void {
    if (!this.dragging) return;
    this.setWidth(this.startWidth + this.direction * (e.clientX - this.startX));
  }

  dragEnd(): void {
    this.dragging = false;
  }

  keyDown(e: KeyboardEvent): void {
    const step = e.shiftKey ? 32 : 8;
    if (e.key === "ArrowLeft") {
      e.preventDefault();
      this.setWidth(this.width - this.direction * step);
    } else if (e.key === "ArrowRight") {
      e.preventDefault();
      this.setWidth(this.width + this.direction * step);
    } else if (e.key === "Home") {
      e.preventDefault();
      this.setWidth(this.min);
    } else if (e.key === "End") {
      e.preventDefault();
      this.setWidth(this.max);
    }
  }
}
