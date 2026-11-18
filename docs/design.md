# Heardback design system

Everything the Angular code needs to match the approved design. Colors, type, spacing and component rules come from here and nowhere else (see CLAUDE.md, "Visual design rules").

**Source of truth:** the Claude Design handoff (canvas "Heardback — Applications, three directions"), pages **B refined** and **Sign in, account and admin**. The page "Three directions" is the earlier exploration and is not built from. If the two pages disagree, B refined wins.

Handoff link (owner access only): https://claude.ai/code/artifact/b8cef516-80a0-4953-a4f8-79fa1b66dc72

| Artboard | What it defines |
|---|---|
| B refined · Desktop — light and dark | App frame, sidebar, applications table, envelope detail panel, Add application dialog, empty / loading / error |
| B refined · Mobile | Mobile list, full-screen detail sheet |
| B refined · Hover, focus and button states | Buttons, rows, inputs, confirm delete, status labels |
| B refined · Type scale, fonts and colour tokens | Fonts, type scale, colour tokens with contrast |
| Sign in — desktop, states, mobile | Sign in |
| Create account (invite only) | Registration |
| App shell — admin menu, session loading, no access | Admin nav, session restore, 403 page, mobile menu |
| Invite codes (admin) | Table, pagination, create and created dialogs, states, mobile |

---

## 1. Principles

- **Direction: "Correspondence".** A dense, ledger-like tool. The postal theme lives only in the visuals: envelope-shaped detail panel, dated postmark stamps in the timeline, envelope in empty states, postmark double border on the status label in the detail panel.
- **The theme never shows in the words.** Labels stay plain: *Applied, No response · N days, Interview, Rejected, Offer, Delete, Sign in*. No wordplay ("Return to sender" and similar).
- **Dense and left-aligned.** Borders and rules, not shadows. The only shadow is on dialogs.
- **Status colour carries meaning, always with an icon and a text label.** Colour is never the only signal.
- **Avoid** (the generic AI-template look): indigo/violet/purple accents, decorative gradients, glassmorphism, landing/hero sections, eyebrow pills, three-card feature grids, emoji as icons, floating white rounded cards on a pastel page, centered marketing headlines.

---

## 2. Theming

Light and dark themes are both fully specified. Tokens are CSS custom properties, so components use one class (`bg-surface`) and never `dark:` variants.

- **Default: follow the OS.** Each colour token is `light-dark(light, dark)` and `:root` has `color-scheme: light dark`, so the browser picks the OS theme.
- **Manual choice (decided):** a Light / Dark / System setting will live on the Settings page. The tokens already support it: `data-theme="light"` or `data-theme="dark"` on `<html>` overrides the OS setting. The choice is stored on the device. The control itself is built with the Settings page, which still needs its design.

Because the theme runs through `color-scheme`, native controls (scrollbars, date pickers, select menus) match it too.

## 3. Colour tokens

Contrast is WCAG 2.1 against the surface the token is used on. Text ≥ 4.5 : 1, borders/focus/UI marks ≥ 3 : 1. The handoff's own table was rechecked, and the extra pairings the screens use (marked *) were computed for this document. Every text pairing passes in both themes.

### Surfaces

| Token | Light | Dark | Used for |
|---|---|---|---|
| `page` | `#E8E4DC` | `#0F0E0C` | Canvas behind the app frame (outside the app on very wide screens) |
| `base` | `#FCFBF8` | `#1A1815` | Main background, cards, dialogs (handoff name: `bg`) |
| `surface` | `#F5F1E8` | `#201E1A` | Sidebar, table header, dialog footers, mobile bars, auth page backdrop |
| `envelope` | `#F3EDE0` | `#26231E` | Detail panel envelope, confirm-delete box, created-code frame |
| `field` | `#FFFFFF` | `#1D1B18` | Input background |
| `row-hover` | `#F2EDE3` | `#24211C` | Table row hover |
| `row-selected` | `#F7ECE6` | `#2C2420` | Selected row, with a 3 px `accent` bar on the left |
| `nav-active` | `#EAE3D5` | `#2D2A24` | Active sidebar item, sidebar item hover |
| `secondary-hover` | `#EFE9DD` | `#2C2924` | Secondary button hover |
| `skeleton` | `#EAE3D5` | `#2F2C26` | Skeleton bars |
| `skeleton-line` | `#CFC6B5` | `#4E4A41` | Skeleton status-label outline |
| `scrim` | `#CDC6B8` | `#070605` | Dialog backdrop, at **80 % opacity** so the page stays faintly visible |

### Text and lines

| Token | Light | Dark | Used for | Contrast (L / D) |
|---|---|---|---|---|
| `ink` | `#1E1B16` | `#EEE9DF` | Primary text | 16.59 / 14.64 on base |
| `ink-2` | `#4F493F` | `#C4BDB0` | Secondary text, labels, column headers | 8.61 / 9.49 on base; 7.90 / 8.92 on surface* |
| `ink-muted` | `#5E5749` | `#ABA494` | Captions, counts, "To", timestamps (handoff: `ink3`) | 6.13 / 6.31 on envelope; 6.35 / 6.71 on surface* |
| `placeholder` | `#6A6356` | `#A39C8D` | Input placeholders | 5.95 / 6.30 on field |
| `disabled` | `#8C8374` | `#7A7366` | Disabled text (exempt from contrast rules) | — |
| `rule` | `#E0D9CB` | `#34312B` | Row dividers, panel/sidebar dividers (decorative) | — |
| `rule-strong` | `#BDB3A1` | `#4E4A41` | Table header underline, card and envelope outline, dashed dividers, envelope flap (decorative) | — |
| `field-border` | `#8C8374` | `#7A7366` | Input and secondary-button borders | 3.74 / 3.66 on field |
| `focus` | `#1E1B16` | `#EEE9DF` | 2 px focus ring | 16.59 / 14.64 on base |
| `postmark` | `#3B372F` | `#C9C2B4` | Postmark cancellation waves and ring (decorative, `aria-hidden`) | — |

### Accent, link, danger, warning

| Token | Light | Dark | Used for | Contrast (L / D) |
|---|---|---|---|---|
| `accent` | `#B23A26` | `#EE8A6E` | Primary button, selection bar, logo, stamps, spinner arc | 5.76 / 7.17 on base |
| `accent-hover` | `#952E1D` | `#F4A28B` | Primary button hover | — |
| `on-accent` | `#FFFFFF` | `#260B04` | Text on primary button | 5.96 / 7.51; hover 7.82 / 9.18 |
| `link` | `#A2321F` | `#F2A089` | Links | 6.74 / 8.58 on base; 5.98 / 7.58 on envelope* |
| `danger` | `#8C1C3F` | `#F08BA9` | Errors, Delete, Rejected, ghost risk High | 8.62 / 7.57 on base |
| `danger-tint` | `#F8E8EE` | `#33171F` | Error banner, danger-button hover | danger on it 7.55 / 6.99; ink-2 on it 7.54 / 8.77* |
| `on-danger` | `#FFFFFF` | `#2A0612` | Text on solid danger button | 8.92 / 7.92 |
| `warn-tint` | `#FBF1E2` | `#2E2414` | "Too many attempts" banner (border and title use `status-no-response`) | title on it 6.06 / 7.73* |

Postal red is the accent only. Never use it for errors. Dark-mode danger pink sits close to the accent coral, so **danger elements always carry their icon** (trash, warning triangle, ✕ glyph).

### Status

| Token | Light | Dark | Status | Contrast on base (L / D) |
|---|---|---|---|---|
| `status-applied` | `#4F493F` | `#C4BDB0` | Applied; invite **Used** | 8.61 / 9.49 |
| `status-no-response` | `#844F00` | `#E8AE62` | No response · N days; invite **Expired**; warning banners | 6.55 / 8.99 |
| `status-interview` | `#24527A` | `#92BCE3` | Interview | 7.92 / 8.88 |
| `status-offer` | `#2D6236` | `#8CC79A` | Offer; invite **Active**; "Copied to clipboard" | 6.97 / 9.07 |
| `danger` | (above) | | Rejected | 8.62 / 7.57 |

All status colours also pass on `row-selected`, `row-hover` and `envelope` (lowest: No response on row-hover, 5.81 light).*

### Shadow

| Token | Light | Dark | Used for |
|---|---|---|---|
| `shadow-dialog` | `0 12px 32px rgba(45,35,20,0.18)` | `0 12px 32px rgba(0,0,0,0.55)` | Dialogs only |

---

## 4. Typography

### Fonts

All three include Latin Extended, so Turkish letters (ğ ş ı İ ü ö ç) render in the font itself. **Self-hosted (decided):** the font files ship with the app (latin + latin-ext subsets, `font-display: swap`). No request goes to Google, so users' IP addresses are not shared and the fonts work offline.

| Family | Weights | Role | Fallback |
|---|---|---|---|
| **Newsreader** (optical size axis 6..72) | 500, 600 | Wordmark, page/panel/dialog titles, empty-state titles | Georgia, serif |
| **Public Sans** | 400, 500, 600 | All UI and body text | system-ui, sans-serif |
| **DM Mono** | 400, 500 | Dates, counts, invite codes, stamp numerals | ui-monospace, monospace |

Dates and counts use DM Mono or `tabular-nums` so columns line up.

### Type scale

Smallest readable text is **12 px**. The only exception is the decorative stamp month (7.5–8 px), which is always `aria-hidden` with the date repeated in real text.

| Token | Font · weight | Size / line height | Where |
|---|---|---|---|
| `page-title` | Newsreader 600, −0.01em | 28 / 34 | Desktop page heading; no-access heading |
| `page-title-sm` | Newsreader 600, −0.01em | 24 / 30 | Mobile page heading; Sign in / Create account heading |
| `panel-title` | Newsreader 600, −0.01em | 23 / 28 | Company name in the envelope |
| `dialog-title` | Newsreader 600 | 20 / 26 | Dialog titles, section titles |
| `empty-title` | Newsreader 600 | 18 / 24 | Empty-state title |
| `wordmark` | Newsreader 600, −0.01em | 22 (desktop); mobile header uses `dialog-title` (20); session loading uses `display` / `display-sm` (30 / 26) | "Heardback" next to the logo |
| `body` | Public Sans 400 | 14 / 20 | Notes, descriptions, nav items |
| `cell` | Public Sans 400–600 | 13.5 / 20 | Table cells, inputs, buttons |
| `small` | Public Sans 400 | 13 / 18 | Page subtitles ("8 applications · 2 with no response"), small buttons, banner body |
| `label` | Public Sans 500–600 | 12.5 / 18 | Field labels, column headers, status labels, field errors |
| `caption` | Public Sans 400 | 12 / 16 | Meta text, hints, "To", role under the user's name |
| `label` + `font-mono` | DM Mono 400 | 12.5 / 18 | Applied column, timeline dates, Created/Expires/Used |
| `cell` / `ui-xl` / `display` / `display-sm` | DM Mono 400–500, `tracking-code` (+0.04em), `tracking-code-lg` (+0.06em) in the created dialog | 13.5 (table), 16 (mobile list), 30 / 26 (created dialog desktop / mobile) | Invite codes |
| `ui-lg` + `leading-none` | DM Mono 500 | 15 (14 = `body` on mobile) | Day number in timeline stamps |
| `stamp` / `stamp-sm` | DM Mono 400, +0.1em (`tracking-stamp`), uppercase | 8 / 7.5 | Month in stamps (decorative only) |

**Mobile and auth sizes:** `ui-lg` (15 / 22): mobile list company, mobile full-width buttons, invite-code input (DM Mono, `tracking-code-input` +0.08em, uppercase); `ui-xl` (16 / 24): mobile nav items; `ui-md` (14.5 / 20): auth buttons; `body` (14): auth inputs.

---

## 5. Spacing, sizing, radii, borders

**Spacing** uses Tailwind's default 4 px-based scale (v4 accepts quarter steps, so 18 px = `4.5`, 9 px = `2.25`). Every spacing value in the handoff maps onto it, so no arbitrary values are needed. Common rhythm: 8–14 px between controls, 18–24 px between sections, page padding 24 × 28 px (desktop main column), 16 px (mobile).

**Control heights**

| Control | Desktop | Mobile / auth |
|---|---|---|
| Toolbar input, select, button | 36 | 44 (inputs), 48 (primary buttons) |
| Small button (Sign out, Try again, Edit in panel) | 30–32 | 44 |
| Timeline "Add update" | 28 | 48 (sheet footer) |
| Auth input / button | — | 42 / 44 |
| Table row | 46 (applications), 44 (invite codes) | list item ≈ 76 |
| Table header | ~40 | — |
| Sidebar item | 34 | 48 |
| Icon-only button | 36 (panel close), 30–32 (dialog close), 28 (row copy) | **44 minimum** |
| Status label | 22 | 22 |

Touch targets on mobile are **≥ 44 px**.

**Radii**

| Token | Value | Used for |
|---|---|---|
| `rounded-xs` | 2 px | Postmark status label outer border, skeleton bars |
| `rounded-sm` | 3 px | Status labels, sidebar items, ghost-risk box |
| `rounded-md` | 4 px | Inputs, buttons, table container, envelope, banners |
| `rounded-lg` | 5 px | Dialogs, confirm-delete box |
| `rounded-xl` | 6 px | Auth card, outer app frame |
| `rounded-sheet` (top corners) | 14 px | Top corners of mobile bottom sheets |

**Borders:** 1 px everywhere. Exceptions: invalid input 1.5 px `danger`; postmark status label 1.5 px dotted outer + 1 px solid inner; stamps 2 px dotted outer + 1 px solid (or dashed, for No response) inner; dividers inside the envelope and above auth footers are 1 px **dashed** `rule-strong`.

**Layout widths:** sidebar 208 px; detail panel 380 px; auth card 400 px (sign in) / 420 px (create account); Add application dialog 460 px; invite dialogs 392 px; no-access text max 520 px.

---

## 6. Icons

No icon library. Icons are inline stroke SVGs from the handoff (16 × 16 viewBox, 1.4–1.75 stroke, round caps, `currentColor`), collected into one `shared/ui/icon` component. Decorative icons get `aria-hidden="true"`. Icon-only buttons get an `aria-label`.

Set: plus, trash, close (✕), search, chevron-down, chevron-left, chevron-right, calendar, copy, check, warning triangle, info circle, clock, menu, mail (Invite codes nav), spinner (ring in `rule` + arc in `accent` or `currentColor`).

Status glyphs (14 px, drawn in the status colour):

| Status | Glyph |
|---|---|
| Applied | Open circle |
| No response | Dashed circle with clock hands |
| Interview | Circle, right half filled |
| Rejected | Circle with ✕ |
| Offer | Filled circle with a check in `base` |
| Invite Active | Circle with a centre dot |
| Invite Used | Circle with a check |
| Invite Expired | Dashed circle with clock hands |

Brand marks (also inline SVG, `aria-hidden`): **logo** (double ring with two wave lines, `accent`); **postage stamp** (dotted frame, sun and waves, `accent`); **postmark** (three cancellation waves + 3 px double-ring date circle, `postmark`); **envelope illustration** for empty states (envelope in `ink-muted`, stamp in `accent`); **no-access envelope** (stamp in `ink-muted` with a `danger` ✕).

---

## 7. Interaction states

| State | Rule |
|---|---|
| Keyboard focus | `outline: 2px solid focus; outline-offset: 2px` on buttons, links, inputs, nav items (`:focus-visible`). Table rows and mobile list items use offset **−2px**. Grouped toolbar fields show the ring on the group (`:focus-within`) and none on the inner input. An input in focus also turns its border `ink`. |
| Hover | Primary → `accent-hover` background and border. Secondary → `secondary-hover`. Danger → `danger-tint`. Nav item → `nav-active`. Row → `row-hover`. Hover never applies to disabled controls. |
| Selected row | `row-selected` + inset 3 px `accent` bar on the left + company in weight 600 + `aria-selected="true"`. |
| Active nav item | `nav-active`, inset 3 px `accent` bar, `ink`, weight 600, `aria-current="page"`. |
| Disabled | Primary and danger: `surface` background (danger: transparent), `rule`/`rule-strong` border, `disabled` text. Secondary: `base` background, `rule` border. Inputs: `surface` background, `rule` border, `disabled` text. |
| Busy (submitting) | Button keeps its colours, shows the spinner + "…ing" label ("Signing in…", "Creating account…"), `disabled` + `aria-busy="true"`. Form fields are disabled while busy. |

---

## 8. Components

Reusable pieces are standalone components and directives in `web/src/app/shared/ui/` (prefix `app`, OnPush by default).

### Buttons — `appButton` directive on `<button>` / `<a>`
| Variant | Default | Notes |
|---|---|---|
| Primary | `accent` bg + border, `on-accent` text, 600 | One per view. Optional leading plus icon. |
| Secondary | `base` bg (transparent in the sidebar), `field-border` border, `ink` text, 400 | Cancel, Edit, Try again, Sign out, pagination |
| Danger (outline) | transparent bg, `danger` border + text, 600, **trash icon always** | Delete |
| Danger (solid) | `danger` bg, `on-danger` text, 600 | Only the confirm button in a delete confirmation |
| Icon-only | transparent, no border (close in panel/sheet/menu) or `field-border` border (dialog close) | `aria-label` required |

Sizes: `xs` 28 (row copy, timeline "Add update"), `sm` 30–32, `md` 36 (default), `lg` 44 (auth), `xl` 48 (mobile full-width). Padding 12–14 px horizontal. Radius 4.

### Text field — `<app-field>` + `appInput` directive
Label above (`label`, 600, `ink-2`), 5 px gap, input 36 px desktop / 42 px auth / 44 px mobile, `field` bg, `field-border`, radius 4, padding 10–12 px. Optional hint below (`caption`, `ink-muted`). Error: border 1.5 px `danger`, `aria-invalid="true"`, message below with warning icon (`label`, 500, `danger`), linked via `aria-describedby` (hint and error both). Optional "· optional" suffix on the label in `ink-muted`, 400.

Variants: **select** (native `<select>`, `appearance: none`, chevron icon at right); **date** (DM Mono 13 px value, calendar icon at right); **number** (DM Mono, 96 px wide, with trailing unit text "days"); **invite code** (DM Mono 15, +0.08em, uppercase, dashes added while typing, `maxlength` 14, `autocomplete="off"`, `spellcheck="false"`); **password** with a live character counter in DM Mono at the right of the hint ("12–128 characters" · "19 characters").

### Toolbar field group
Search, Status filter and Channel filter on the list page: one 36 px bordered box, label cell on the left (`label`, `ink-2`, divided by a `rule` line), input or select on the right without its own border. Search label includes the search icon. Mobile: 44 px, search label is visually hidden (icon only), filters in a 2-column grid.

### Banners — `app-banner`
Row of icon + text, padding 10–12 px, radius 4, 1 px border.
| Kind | Border / title colour | Background | Icon | Role |
|---|---|---|---|---|
| Error | `danger` | `danger-tint` | warning triangle | `alert` |
| Warning (rate limit) | `status-no-response` | `warn-tint` | clock | `status` |
| Info (session expired) | `field-border` | `surface` | info circle (`ink-2`) | `status` |

Title 13.5 / 600 in the border colour; body 13 in `ink-2` (warning body in `ink`). Error banners in lists can hold a secondary "Try again" button. The rate-limit banner shows the countdown in DM Mono 500 ("Try again in **42** seconds"), and the submit button is disabled until it reaches zero.

### Inline success
"Copied to clipboard": check-in-filled-circle icon + 13 px 600 text in `status-offer`, `role="status"`.

### Status label — `app-status-label`
- **Table / list form:** 22 px tall, padding 0 8 0 6, 1 px border in the status colour, radius 3, glyph + text (`label`, 500), all in the status colour, no fill.
- **Postmark form (detail panel and sheet only):** same label inside a 1.5 px dotted outer border, 2 px gap, radius 2.
- Text is plain: "Applied", "No response · 14 days", "Interview", "Rejected", "Offer". Invite codes: "Active", "Used", "Expired".

### Data table
Container: 1 px `rule` border, radius 4, horizontal scroll inside the box when narrow. Header: `surface`, `label` 600 `ink-2`, 1 px `rule-strong` underline. Rows: 46 px, 1 px `rule` dividers (none on the last row), `cell` text. Company 500 (600 when selected); secondary columns (Channel, Last update) in `ink-2`; date columns in DM Mono at `label` size; right-aligned "Last update" in tabular numbers; cells don't wrap. Rows are focusable (`tabindex="0"`) and open the detail with Enter/Space as well as click. Footer count "Showing 8 of 8" in `label` `ink-muted`.

### Pagination
Bar inside the table container: `surface` background, 52 px tall. "Showing 1–20 of 34" on the left (13, `ink-2`), "Page 1 of 2" (13, `ink-muted`) plus Previous / Next secondary buttons (32 px, chevron icons) on the right. Previous is disabled on page 1, Next on the last page. Mobile: 44 px icon-only Previous/Next with `aria-label`, "Showing 1–20 of 34" centred.

### Sidebar navigation (desktop)
**Only pages that exist are listed.** Dashboard, CV, Companies and Settings appear in the design but are added in the phase that builds them. Until then the list is Applications (+ Invite codes for admins).

208 px, `surface`, right border `rule`. Top: logo + wordmark (22 px) with a `rule` divider below. Items 34 px, padding 0 10, radius 3, 14 px `ink-2`; active item per §7. Applications shows its count at the right in DM Mono 12 `ink-muted`. **Admin section** (admins only): divider, small heading "Admin" (12, 600, `ink-muted`), then "Invite codes" with the mail icon. Bottom: divider, user's display name (13.5, 600), role underneath ("Admin" / "Member", 12, `ink-muted`), and a small secondary "Sign out" button.

### Mobile header and menu
Header 60 px, `surface`, bottom `rule`: logo 24 px + wordmark 20 px + 44 px menu button ("Open menu"). The open menu is a full-screen panel (`surface`) with the header's button switched to ✕ ("Close menu", `aria-expanded="true"`), 48 px items at 16 px, the same Admin section, and the user + 44 px Sign out button pinned at the bottom. It traps focus and closes on Escape.

### Envelope detail panel (desktop) — `app-envelope`
380 px column, `surface` background, left border `rule`.
1. **Header bar** (52 px, `base`, bottom `rule`): ✕ icon button "Close details", title "Application" (14, 600), Edit secondary button (32 px). **Buttons never sit inside the envelope, and no flap line passes behind a button.**
2. **Envelope** (inset 16/20 px): `envelope` background, 1 px `rule-strong`, radius 4, padding 0 20 16. Decorations, all `aria-hidden`: flap V-line (`rule-strong`) across the top; postage stamp top-right (50 × 60); postmark (waves + 46 px double-ring date circle) overlapping it.
3. Content, 72 px below the top: "To" (caption, `ink-muted`) → company (`panel-title`) → role (14, `ink-2`) → posting link (13, `link`, underline offset 2).
4. Dashed divider, then a row: postmark status label · "Updated 3 days ago" (12.5, `ink-muted`) · channel + applied date at the right (DM Mono date).
5. **Ghost-risk box** (Phase 6 insight; render only when data exists): `base`, 1 px `rule-strong`, radius 3; "Ghost risk: High" (13.5, 600, `danger` for High), five-wave meter, "4 of 5" (DM Mono 12), one-line explanation (12.5, `ink-2`).
6. **Notes**: heading (12.5, 600, `ink-2`) + text (13.5, `ink`).
7. **Timeline**: heading + "Add update" secondary button (28 px); ordered list of events. Each event = **timeline stamp** + text. The stamp is 42 × 46, `base` fill, 2 px dotted outer + 1 px inner border in the event's status colour (the inner border is dashed for No response), month (8 px caps) above the day number (DM Mono 15/500). Text: status name (13.5, 600, status colour; Applied stays `ink`) + short date (DM Mono 12, `ink-muted`), and a description line (12.5, `ink-2`). The stamp is `aria-hidden`; the date is repeated in the text.

### Full-screen detail sheet (mobile)
`role="dialog"`, `aria-modal`, `surface`. Header 60 px (`base`): 44 px ✕ "Close and go back to applications", title "Application" (15, 600), Edit (36 px). Same envelope inside with 16 px margin (slightly smaller stamp and postmark); the channel/date line wraps to its own row. Sticky footer (`base`, top `rule`): **Delete** (danger outline with trash, 48 px) and **Add update** (primary, 48 px), side by side.

### Dialog — `app-dialog` (CDK Dialog)
`base`, 1 px `rule-strong`, radius 5, `shadow-dialog`. Header: padding 16 20 14, bottom `rule`; optional small stamp mark (Add application), title (`dialog-title`), close icon button (30–32 px, bordered). Body: padding 18 20 20, fields 14 px apart, two-column rows where shown. Footer: `surface`, top `rule`, right-aligned Cancel/Done (secondary) + primary action. `aria-modal`, `aria-labelledby` = title, focus trapped, Escape closes, focus returns to the trigger.

**Mobile:** dialogs become bottom sheets: full width, top radius 14, top border `rule-strong`, 44 px close button, full-width 48 px buttons side by side.

### Confirm delete — alert dialog
`role="alertdialog"`, `envelope` background, 1 px `rule-strong`, radius 5, padding 16. Title with a `danger` warning icon ("Delete this application?", 14, 600). Body names what will be removed ("Northwind Logistics and its timeline will be removed. You can't undo this."). Buttons: Cancel (secondary) + **solid danger** "Delete application". Initial focus goes to Cancel.

### Empty state
Inside the table container, under the real column header: envelope illustration (72 × 50), title (`empty-title`), one sentence (13, `ink-2`), primary button (34 px, plus icon). Left-aligned.

### Loading state
Inside the table container: a status row with spinner + "Loading applications…" (13, `ink-2`, `role="status"`), then four `aria-hidden` skeleton rows (8–9 px `skeleton` bars and a 22 px `skeleton-line` outline where the status label goes). No shimmer animation.

### Error state
Inside the table container: an error banner with title "Couldn't load …", one reassuring sentence, and a "Try again" secondary button.

### Full-page session loading
Shown while the session is restored on reload (refresh call in flight). Centred on `base`: 40 px logo + wordmark (30 px; mobile 32 / 26), below it spinner + "Restoring your session…" (14, `ink-2`). `role="status"`, `aria-live="polite"`.

### Invite code display (created dialog)
Code in DM Mono 500, 30 px (mobile 26), +0.06em, centred in a 72 px box: 1 px `accent` inner border, 2 px dotted `accent` outer frame, `envelope` fill. Below: "Expires 01 Dec 2026 · in 7 days" and "Single use" (12.5, `ink-2`).

---

## 9. Layout and responsive behaviour

- **Desktop app frame:** sidebar (208) | main column (fluid, padding 24 28 20) | detail panel (380, only while an application is selected).
- **Mobile (390 designed):** header + full-screen menu instead of the sidebar; tables become lists; the detail panel becomes a full-screen sheet; the page's primary action moves to a sticky bottom bar (48 px button).
- **Breakpoints (decided):**
  | Width | Layout |
  |---|---|
  | < 1024 px (below `lg`) | Mobile layout: header + menu, lists, full-screen detail sheet, sticky bottom action |
  | 1024–1279 px (`lg`) | Sidebar shows; the detail still opens as the full-screen sheet |
  | ≥ 1280 px (`xl`) | Full desktop: sidebar + table + detail panel beside it |
- **Auth pages:** from `sm` (640 px) the card (400/420 px, `base`, 1 px `rule-strong`, radius 6, padding 32) centred on `surface`. Below 640 px: no card; content on `base` with padding 48 24 24, form full width. (The card appears earlier than the app breakpoints because a full-width form on a tablet would be too wide.)
- **No-access page:** inside the normal shell, left-aligned block at max 520 px with 96 × 80 px padding on desktop (56 × 24 on mobile).

---

## 10. Screens and their states

### Sign in (`/login`, signed-out users only)
Logo + wordmark with the postage stamp at the right; heading "Sign in"; Email (`autocomplete="email"`), Password (`current-password`); primary "Sign in" (44 px, full width); dashed divider; "Have an invite? **Create account**".

| State | Shown as |
|---|---|
| Submitting | Button "Signing in…" with spinner; fields disabled |
| Wrong credentials (401 `auth.invalid_credentials`) | Error banner "Email or password is incorrect". Same message for unknown email and wrong password. |
| Too many attempts (429 + `Retry-After`) | Warning banner with live countdown; submit disabled |
| Server/network error | Error banner "Couldn't reach Heardback" + "Check your connection and try again. Your details are still filled in." |
| Session expired (redirected here) | Info banner "Your session has expired. Please sign in again."; email pre-filled when known |
| Session restore gave up (server unreachable on reload, after retries) | Error banner "Couldn't reach Heardback" + "Check your connection and try again." (decided 2026-11-18; reuses the server-error banner) |

### Create account (`/register`, signed-out users only)
Heading "Create account", line "Heardback is invite only. You need a code to join." Fields: Invite code (hint "12 characters from your invite. Dashes are added for you."), Display name (`autocomplete="name"`), Email, Password (`new-password`, 12–128, counter). Primary "Create account"; footer "Already have an account? **Sign in**".

| State | Shown as |
|---|---|
| Per-field validation (client or 400) | Field errors + error banner "Check the highlighted fields" / "N fields need attention before you can continue." Messages: "Enter the full 12-character code", "Enter a display name", "Enter a valid email address", "Use at least 12 characters" |
| Invalid invite (400 `auth.invalid_invite_code`) | Error on the invite field: "This invite code is invalid, expired or already used" |
| Email taken (409 `auth.email_taken`) | Error on the email field: "An account with this email already exists" |
| Too many attempts / server error / submitting | As on sign in ("Couldn't create your account" / "The server didn't respond. Your details are still filled in. Try again.") |

### App shell
Sidebar or mobile header, as §8. Admin section only when `user.isAdmin`. Full-page session loading on reload. Sign out in the sidebar footer / mobile menu.

### No access (admin page opened by a member, 403)
Shell stays. No-access envelope illustration, heading "You don't have access to this page", text "Only admins can manage invite codes. If you think you should have access, ask the person who invited you.", secondary link-button "Go to Applications" (full width on mobile).

### Invite codes (`/admin/invite-codes`, admin)
Heading "Invite codes", summary "34 codes · 9 active" (total from the paged response; the active count is added to the list response in the same phase), primary "Create invite code" at the right (full width under the heading on mobile). Table: Code (DM Mono + copy icon button "Copy code K7QM-…"), Status (Active / Used / Expired label), Created, Expires, Used (date or "—" in `ink-muted`). Newest first, 20 per page, pagination bar. Mobile: list items with code (DM Mono 16) + 44 px copy button, then status label + one meta line ("Expires 01 Dec" / "Used 22 Nov" / "Expired 22 Nov").

- **Create invite code dialog:** text "Anyone with the code can create one account. Each code works once."; field "Expires in (days) · optional", number 1–90, default 7, hint "1–90 days. Leave as 7 if you're not sure."; Cancel + "Create invite code".
- **Invite code created dialog:** code display (§8), expiry line, "Send it to one person. You can find it later in the table."; Done + primary **Copy**. After copying: the line becomes "Copied to clipboard" and the button becomes "Copied" with a check.
- **States:** empty ("No invite codes yet" / "Create a code to let someone join Heardback."), loading ("Loading invite codes…" + skeleton), error ("Couldn't load invite codes" / "The server didn't respond. Existing codes still work." + Try again).

### Applications (Phase 2; designed, not built yet)
List page with heading, summary ("8 applications · 2 with no response"), toolbar (Search, Status, Channel, primary "Add application"), table (Company, Role, Channel, Applied, Status, Last update), detail panel / sheet, Add application dialog (Company, Role, Job posting URL with "Enter a full link that starts with https://", Channel, Applied date), confirm delete, and empty / loading / error states. Exact rules are in §8.

- The toolbar **Delete** button in the desktop artboard was only a style sample. **Delete lives in the detail panel/sheet**, not the toolbar.
- "No response · N days" is **derived from dates** (applied/last event + threshold), not stored by a background job. The timeline row "Marked automatically after 14 days" is a computed row, not a stored event.

---

## 11. Implementing in Tailwind v4

- Tokens live in `web/src/styles/tokens.css` as Tailwind `@theme` variables. Each colour is written once as `light-dark(light, dark)`; `color-scheme` on `:root` follows the OS, and `data-theme="light" | "dark"` on `<html>` overrides it. Utilities read `bg-surface`, `text-ink-2`, `border-rule-strong`, `text-cell`, `font-mono`, `rounded-md`, `shadow-dialog`.
- Tailwind's default colour palette is removed (`--color-*: initial`), so only design tokens exist. A stray `bg-indigo-500` fails to compile instead of slipping in.
- No arbitrary values (`text-[#…]`, `mt-[13px]`) and no inline styles; anything missing becomes a token here first.
- Focus ring, hover and disabled rules live inside the shared components, not repeated in every screen.
- Decorative SVGs (stamps, postmarks, envelope) are components with `aria-hidden="true"`, coloured through `currentColor` / token classes.
- The handoff HTML is a reference only. It is translated into Angular + Tailwind, never copied in.

---

## 12. Decisions made during review (2026-11-18)

Decisions the author made where the handoff was silent or showed only a sample.

| Topic | Decision | Where |
|---|---|---|
| Theme | Follow the OS; a Light / Dark / System setting comes with the Settings page | §2 |
| Invite codes summary | "N codes · M active"; the API adds an active count | §10 |
| Fonts | Self-hosted, no Google Fonts requests | §4 |
| Breakpoints | Mobile < 1024 px; sidebar from 1024 px; detail panel beside the table from 1280 px | §9 |
| Dialog backdrop | `scrim` at 80 % opacity | §3 |
| Sidebar | Only existing pages; items are added in the phase that builds them | §8 |
| Detail panel header | ✕ and Edit in a plain header bar above the envelope; no flap line behind a button | §8 |
| List toolbar Delete | Not built; delete lives in the detail panel/sheet | §10 |
| Auth card breakpoint | Card from 640 px; full-width form only on phones | §9 |
| Invite row copy feedback | After copying from a table row or list item, the copy icon turns into a check in `status-offer` for 2 seconds; screen readers hear "Code copied to clipboard" | §10 |
| Create invite code failure | Error banner inside the dialog: "Couldn't create the invite code" / "The server didn't respond. Try again." | §10 |

## 13. Still to design (before the phase that needs it)

These go back to Claude Design with a brief written from the API that exists at that point. Nothing here is improvised.

- **Phase 2:** kanban board, Edit application form, Add update form, Add application on mobile.
- **Settings page** (including the theme setting), Dashboard, CV, Companies: in their phases.
