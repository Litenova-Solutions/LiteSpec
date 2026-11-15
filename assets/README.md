# LiteSpec Brand Assets

The logo files and social images for LiteSpec, a Litenova Solutions product, live in this folder. The site draws the mark from `apps/site/src/components/Mark.astro` and serves copies of the icons and the share image from `apps/site/public/`, while the CLI and analyzer packages use `logo/png/icon-128.png` as their NuGet icon. Nothing else here ships anywhere.

## The Mark

The Litenova Solutions mark is a square frame around an L and a saffron `/>`. LiteSpec keeps the frame and the L, and sets three saffron rules, the lines of a specification, where the `/>` sat. The mark is drawn on the same 240 grid at the same stroke weight of 14:

- The frame runs 14 thick on all four sides.
- The L stands 120 tall and 40 wide, with a stem and foot 14 thick, positioned 43.68 from the left and 60 from the top.
- The three rules run 14 thick and span 97.68 to 196.32, the slot the Litenova `/>` fills, leaving a gap of 14 between the L's foot and the rules.
- The top rule aligns with the L's top, the bottom rule with its baseline, and the middle rule with the center line at 120.

At 48 pixels and below, the icons switch to a heavier cut (stroke 20) that keeps the rules apart.

## Colors

The mark uses the Litenova Solutions colors rather than the site palette:

| Name | Hex | Use |
|---|---|---|
| Kohl navy | `#0F1B33` | Icon background; frame, L and name on light backgrounds |
| Paper | `#F6F7FA` | Frame, L and name on dark backgrounds |
| Saffron | `#EFA73A` | The three rules only |

The mono files render the whole mark in one color: navy for light backgrounds, white for photos and dark backgrounds where saffron would not hold.

## Using the Logo

- Leave clear space of at least a quarter of the mark's width on every side.
- Show the mark at 24 pixels or larger, and the horizontal lockup with the mark at 24 pixels or taller. Below that size, use the icon files.
- Place the `on-light` files on white or paper, and the `on-dark` files on navy or other dark backgrounds.
- Never recolor, stretch, rotate, outline, or add effects to the mark, and never set the name in another typeface. The name is Manrope Bold, converted to outlines in the lockup files.

## Logo Files

All SVGs live in `logo/` and their PNG renders in `logo/png/`. PNG files of the mark and lockups carry transparent backgrounds; icons and avatars are full navy squares.

| File | Use |
|---|---|
| `mark-on-dark.svg`, `mark-on-light.svg` | The mark alone |
| `mark-mono-white.svg`, `mark-mono-navy.svg` | The mark in one color |
| `lockup-on-dark.svg`, `lockup-on-light.svg` | Mark and name side by side: documents, slides, README headers |
| `lockup-stacked-on-dark.svg`, `lockup-stacked-on-light.svg` | Mark over the name, for square or centered placements |
| `lockup-mono-*.svg`, `lockup-stacked-mono-*.svg` | Lockups in one color |
| `icon.svg` | App icon: the mark on a navy square |
| `icon-small.svg`, `icon-rounded-small.svg` | The heavier cut for 48 pixels and below |
| `avatar.svg` | The mark on navy with extra margin, safe for circular crops |
| `favicon.ico` | 16, 32, and 48 pixel favicon |

PNG names state their size: `icon-512.png` is 512 pixels square, `lockup-on-light-96h.png` is 96 pixels tall, and `lockup-stacked-on-dark-1024w.png` is 1024 pixels wide.

## Social Images

The `social/` folder holds images on navy carrying the site's headline, matching the Litenova Solutions images:

| File | Use |
|---|---|
| `og-1200x630.png` | Link previews (Open Graph); the same image the site serves as `og.png` |
| `github-social-preview-1280x640.png` | GitHub repository social preview |

## Where Each File Goes

| Place | File |
|---|---|
| NuGet package icon (`LiteSpec.Cli`, `LiteSpec.Analyzers`) | `logo/png/icon-128.png` |
| Site favicon | `logo/icon-rounded-small.svg` as `favicon.svg`, its 32 pixel render as `favicon-32.png` |
| Apple touch icon | `logo/png/icon-180.png` as `apple-touch-icon.png` |
| Web app icons | `logo/png/icon-192.png`, `logo/png/icon-512.png` |
| GitHub repository social preview | `social/github-social-preview-1280x640.png` |

## Updating

The SVG files are the sources. The PNG files, `favicon.ico`, and the copies in `apps/site/public/` are renders of them produced in a browser. When the mark changes, update `apps/site/src/components/Mark.astro`, the mark in `apps/cli/book/src/lib/components/shell/TopBar.svelte`, and the SVG files together, then render the PNG files again at the sizes their names state. When the site headline changes, render the social images again. The lockups set the name at half the mark's height, cap-centered on it and 96 units to its right, with Manrope's kerning and 0.01 em tracking, matching the Litenova Solutions lockups.
