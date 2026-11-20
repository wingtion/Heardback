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
| `scrim-sheet` | `rgba(30,27,22,0.45)` | `rgba(0,0,0,0.65)` | Backdrop behind mobile **bottom sheets** (Move to, Add update) |

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
| `shadow-dialog` | `0 12px 32px rgba(45,35,20,0.18)` | `0 12px 32px rgba(0,0,0,0.55)` | Dialogs |
| `shadow-menu` | `0 8px 24px rgba(45,35,20,0.18)` | `0 8px 24px rgba(0,0,0,0.55)` | "Move to" menu on board cards |
| `shadow-drag` | `0 10px 24px rgba(45,35,20,0.18)` | `0 10px 24px rgba(0,0,0,0.55)` | Board card while it is dragged |

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

**Layout widths:** sidebar 208 px; detail panel 380 px; auth card 400 px (sign in) / 420 px (create account); Add application and Edit dialogs 420 px; Add update dialog 410 px; board "Move to" menu 214 px; invite dialogs 392 px; no-access text max 520 px.

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

Brand marks (also inline SVG, `aria-hidden`): **logo** (double ring with two wave lines, `accent`); **postage stamp** (dotted frame, sun and waves, `accent`); **postmark** (three cancellation waves, `postmark`; the dated circle was removed, §12); **envelope illustration** for empty states (envelope in `ink-muted`, stamp in `accent`); **no-access envelope** (stamp in `ink-muted` with a `danger` ✕).

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
Search, Status filter and Applied via filter on the list page: one 36 px bordered box, label cell on the left (`label`, `ink-2`, divided by a `rule` line), input or select on the right without its own border. Search label includes the search icon. Mobile: 44 px, search label is visually hidden (icon only), filters in a 2-column grid.

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
Container: 1 px `rule` border, radius 4, horizontal scroll inside the box when narrow. Header: `surface`, `label` 600 `ink-2`, 1 px `rule-strong` underline. Rows: 46 px, 1 px `rule` dividers (none on the last row), `cell` text. Company 500 (600 when selected); secondary columns (Applied via, Last update) in `ink-2`; date columns in DM Mono at `label` size; right-aligned "Last update" in tabular numbers; cells don't wrap. Rows are focusable (`tabindex="0"`) and open the detail with Enter/Space as well as click. Footer count "Showing 8 of 8" in `label` `ink-muted`.

### Pagination
Bar inside the table container: `surface` background, 52 px tall. "Showing 1–20 of 34" on the left (13, `ink-2`), "Page 1 of 2" (13, `ink-muted`) plus Previous / Next secondary buttons (32 px, chevron icons) on the right. Previous is disabled on page 1, Next on the last page. Mobile: 44 px icon-only Previous/Next with `aria-label`, "Showing 1–20 of 34" centred.

### Sidebar navigation (desktop)
**Only pages that exist are listed.** Companies and Settings appear in the design but are added in the phase that builds them. Today the list is Applications, CV and Overview (§17) (+ Invite codes for admins).

208 px, `surface`, right border `rule`. Top: logo + wordmark (22 px) with a `rule` divider below. Items 34 px, padding 0 10, radius 3, 14 px `ink-2`; active item per §7. Applications shows its count at the right in DM Mono 12 `ink-muted`. **Admin section** (admins only): divider, small heading "Admin" (12, 600, `ink-muted`), then "Invite codes" with the mail icon. Bottom: divider, user's display name (13.5, 600), role underneath ("Admin" / "Member", 12, `ink-muted`), and a small secondary "Sign out" button.

### Mobile header and menu
Header 60 px, `surface`, bottom `rule`: logo 24 px + wordmark 20 px + 44 px menu button ("Open menu"). The open menu is a full-screen panel (`surface`) with the header's button switched to ✕ ("Close menu", `aria-expanded="true"`), 48 px items at 16 px, the same Admin section, and the user + 44 px Sign out button pinned at the bottom. It traps focus and closes on Escape.

### Envelope detail panel (desktop) — `app-envelope`
380 px column, `surface` background, left border `rule`.
1. **Header bar** (52 px, `base`, bottom `rule`): ✕ icon button "Close details", title "Application" (14, 600), Edit secondary button (32 px) and, in the desktop panel, a danger outline Delete with the trash icon right of it (§12). **Buttons never sit inside the envelope, and no flap line passes behind a button.**
2. **Envelope** (inset 16/20 px): `envelope` background, 1 px `rule-strong`, radius 4, padding 0 20 16. Decorations, all `aria-hidden`: flap V-line (`rule-strong`) across the top; postage stamp top-right (50 × 60); postmark waves (44 × 22) crossing the stamp's left edge. No dated circle (§12).
3. Content, 72 px below the top: "To" (caption, `ink-muted`) → company (`panel-title`) → role (14, `ink-2`) → posting link (13, `link`, underline offset 2).
4. Dashed divider, then a row: postmark status label · "Updated 3 days ago" (12.5, `ink-muted`) · channel + applied date at the right (DM Mono date).
5. **Community section** (Phase 6): replaces the earlier one-line ghost-risk box; see **§18**.
6. **Notes**: heading (12.5, 600, `ink-2`) + text (13.5, `ink`).
7. **Timeline**: heading + "Add update" secondary button (28 px); ordered list of events. Each event = **timeline stamp** + text. The stamp is 42 × 46, `base` fill, 2 px dotted outer + 1 px inner border in the event's status colour (the inner border is dashed for No response), month (8 px caps) above the day number (DM Mono 15/500). Text: status name (13.5, 600, status colour; Applied stays `ink`) + short date (DM Mono 12, `ink-muted`), and a description line (12.5, `ink-2`). The stamp is `aria-hidden`; the date is repeated in the text.

### Full-screen detail sheet (mobile)
`role="dialog"`, `aria-modal`, `surface`. Header 60 px (`base`): 44 px ✕ "Close and go back to applications", title "Application" (15, 600), Edit (36 px). Same envelope inside with 16 px margin (slightly smaller stamp and postmark); the channel/date line wraps to its own row. Sticky footer (`base`, top `rule`): **Delete** (danger outline with trash, 48 px) and **Add update** (primary, 48 px), side by side.

### Dialog — `app-dialog` (CDK Dialog)
`base`, 1 px `rule-strong`, radius 5, `shadow-dialog`. Header: padding 16 20 14, bottom `rule`; title (`dialog-title`), close icon button (30–32 px, bordered). Body: padding 18 20 20, fields 14 px apart, two-column rows where shown. Footer: `surface`, top `rule`, right-aligned Cancel/Done (secondary) + primary action. `aria-modal`, `aria-labelledby` = title, focus trapped, Escape closes, focus returns to the trigger.

**Mobile:** dialogs become bottom sheets: full width, top radius 14, top border `rule-strong`, 44 px close button, full-width 48 px buttons side by side, `scrim-sheet` behind. **Exception:** the long forms (Add application, Edit application) become **full-screen sheets** instead (§14).

### Confirm delete — alert dialog
`role="alertdialog"`, `envelope` background, 1 px `rule-strong`, radius 5, padding 16. Title with a `danger` warning icon ("Delete this application?", 14, 600). Body names what will be removed ("Northwind Logistics and its timeline will be removed. You can't undo this."). Buttons: Cancel (secondary) + **solid danger** "Delete application". Initial focus goes to Cancel.

### Empty state
Inside the table container, under the real column header: envelope illustration (72 × 50), title (`empty-title`), one sentence (13, `ink-2`), primary button (34 px, plus icon). Left-aligned.

### Loading state
Inside the table container: a status row with spinner + "Loading applications…" (13, `ink-2`, `role="status"`), then four `aria-hidden` skeleton rows (8–9 px `skeleton` bars and a 22 px `skeleton-line` outline where the status label goes). No shimmer animation.

### Error state
Inside the table container: an error banner with title "Couldn't load …", one reassuring sentence, and a "Try again" secondary button.

### Full-page session loading
Shown while the session is restored on reload (refresh call in flight). Centred on `base`: 40 px logo + wordmark (30 px; mobile 32 / 26), below it spinner + "Signing you in…" (14, `ink-2`). `role="status"`, `aria-live="polite"`.

### Invite code display (created dialog)
Code in DM Mono 500, 30 px (mobile 26), +0.06em, centred in a 72 px box: 1 px `accent` inner border, 2 px dotted `accent` outer frame, `envelope` fill. Below: "Expires 01 Dec 2026 · in 7 days" and "Single use" (12.5, `ink-2`).

---

## 9. Layout and responsive behaviour

- **Desktop app frame:** sidebar (208) | main column (fluid, padding 24 28 20) | detail panel (380, only while an application is selected).
- **Mobile (390 designed):** header + full-screen menu instead of the sidebar; tables become lists; the detail panel becomes a full-screen sheet; the page's primary action moves to a sticky bottom bar (48 px button). **Exception:** on Applications (list and board) the primary action is an "Add" button in the mobile header; there is no sticky bottom bar on that page (§14).
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
List page with heading, summary ("8 applications · 2 with no response"), List / Board toggle, toolbar (Search, Status, Applied via, primary "Add application"), table (Company, Role, Applied via, Applied, Status, Last update), detail panel / sheet, confirm delete, and empty / loading / error states. Exact rules are in §8. The board view, Add application, Edit application and Add update are in **§14** (the Add application form there replaces the earlier version).

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
| Link autofill (Phase 2 handoff) | **Not built.** The server never fetches posting pages (PLAN.md "No scraping"; avoids SSRF). The link is stored and used to find a posting the user already tracks. Filling fields from the posting *text* may come with the Phase 4 LLM work | §14 |
| Editing a shared posting | A shared `JobPosting` is never changed by one user. Saving Edit re-runs the posting dedup and links the application to the matching posting (a new one if none matches) | §14 |
| Board size | Each column shows at most the **200 newest** cards; the column count is the real total; a truncated column ends with "Showing 200 of 340 · see all in the list" | §14 |
| Board card detail (desktop) | Same as the list: 380 px panel beside the board from 1280 px (columns shrink), full-screen sheet below. Revisit if it feels cramped in use | §14 |
| Delete in the desktop panel | In the header bar, right of **Edit**: a 30 px danger outline **Delete** with the trash icon. There is no bottom bar in the desktop panel; **Add update** is the 28 px button beside the Timeline heading. On phones and tablets the sheet footer keeps **Delete** and **Add update** side by side (decided 2026-11-18, after testing; replaces the earlier bottom bar) | §8, §14 |
| Selected row semantics | The selected table row and list item carry `aria-current="true"` (`aria-selected` is only valid inside a grid) | §7 |
| After adding | The new application opens in the detail (panel or sheet) and the list returns to page 1 | §10 |
| Board "see all" for Applied | The Applied column also holds the "No response" cards, while the list's Applied filter leaves them out, so its "see all in the list" opens the list **without a status filter**. The other columns open the list filtered to their status (decided 2026-11-18) | §14 |
| Board with filters and no results | The list's "No matching applications" state with Clear filters, instead of four empty columns (decided 2026-11-18) | §14 |
| Postmark date circle | **Removed** (2026-11-18): its 8 px month was hard to read and the date is already in the timeline. Only the cancellation waves stay, crossing the stamp's left edge | §6, §8 |

**Decided during the Phase 2 UI build** (approved by the author after testing, 2026-11-18). States the design did not cover, built from existing components and copy patterns:

| State | Shown as |
|---|---|
| Filters match nothing | Inside the table container: "No matching applications" (`empty-title`) + "Try another search or clear the filters." + secondary "Clear filters" (30 px). No envelope illustration, so it is not mistaken for "No applications yet" |
| Detail can't load | Error banner "Couldn't load this application" / "The server didn't respond. Try again." + Try again |
| Detail not found (deleted elsewhere) | Info banner "Couldn't find this application" / "It may have been deleted in another tab or on another device." |
| Delete fails | Error banner inside the confirm box: "Couldn't delete the application" / "The server didn't respond. Try again." A 404 counts as deleted |
| Add: same posting text, no link | Error banner "You already added this job" / "Another of your applications has the same job description." (the "already tracked" panel needs a link to look the application up) |
| Applied date field | The browser's native date field, so the picker and keyboard entry are accessible; it shows the date in the browser's own format (e.g. "18.11.2026") rather than "18 Nov 2026" |

**Decided during the board and forms build** (approved by the author, 2026-11-18). Built from existing components and copy; nothing new visually:

| Topic | Built as |
|---|---|
| Where a moved card lands | At the **top** of the new column, because the board shows the most recently changed first. The drop indicator (accent bar with ring) is therefore always drawn at the top of the target column |
| Move rejected because the board is out of date | If the application was deleted, or already has that status, in another tab or device, the board reloads instead of showing "Couldn't move this application" (Try again could never work) |
| Move announcement | A visually hidden status line reads "Moved Larkspur Labs to Offer." for screen readers, because the card leaves the column the user was in |
| Focus after a move | From the desktop menu: the card's Move to… button in its new column. From the mobile sheet: the selected status tab |
| Mobile board summary | "8 applications · 2 with no response" stays under the title, as on the mobile list (the board handoff shows no summary) |
| Edit: application deleted elsewhere | Error banner with the detail's copy: "Couldn't find this application" / "It may have been deleted in another tab or on another device." |
| Reload latest version | Shows the spinner while the latest version loads; the conflict banner stays if loading fails |
| Tall dialogs | Never taller than the window; the body scrolls between the fixed header and footer |
| 1.5 px borders | Drawn as 1.5 px; browsers round them to 1 px on 1× screens |

## 13. Still to design (before the phase that needs it)

These go back to Claude Design with a brief written from the API that exists at that point. Nothing here is improvised.

- **Phase 2:** nothing left. The board, Edit application, Add update and Add application on mobile came in the "B refined · Board and forms" handoff (§14).
- **Phase 3:** nothing left. The CV page and the match panel were composed from the existing system instead of a Claude Design handoff (§15).
- **Phase 6:** nothing left. Badges, the Community section and the company page came in the "B refined · Community signals" handoff (§18).

---

## 14. Board and forms (Phase 2 handoff)

Source: `project/B4-*.dc.html` in the Claude Design canvas, page "B refined · Board and forms". It uses only the existing tokens plus `scrim-sheet`, `shadow-menu` and `shadow-drag` (§3). Items marked **(decided)** are not in the handoff (or differ from it) and were approved by the author on 2026-11-18.

### New icons and glyphs
Added to the `shared/ui/icon` set: **move** (two opposite arrows), **list** (three lines), **board** (three columns), **clipboard** (Paste), **grip** (2 × 3 dots, drag preview only), **refresh** (conflict banner).
Timeline/chip glyphs: **Note** = circle with two short lines in `ink-2`; **No change** = dotted circle in `ink-muted`.

### View toggle (List / Board)
Segmented control at the right of the page heading: 1 px `field-border`, radius 4, two buttons with `aria-pressed`. Off: `base`, `ink-2`, 400. On: `nav-active`, inset 2 px `accent` bar at the bottom, `ink`, 600. Desktop 34 px with list/board icons; mobile without icons. The handoff shows 40 px on mobile; it is built at **44 px** (touch rule, §5) **(decided)**.

### Board page, desktop
- **Header:** "Applications" (`page-title`), summary "8 applications · 2 with no response" (`small`, `ink-2`, tabular numbers), view toggle (`ml-auto`), primary "Add application" (36 px, plus icon).
- **Toolbar:** Search (320 px, placeholder "Company or role") and Applied via ("All" + the six channels) as toolbar field groups (§8). **No Status filter** on the board, because the columns are the statuses.
- **Columns:** a 4-column grid with 12 px gaps: **Applied, Interview, Offer, Rejected**. "No response · N days" is a label on Applied cards, **not a column**. Each column: `surface`, 1 px `rule`, radius 4, padding 8, gap 8, min height 520. Column heading (`<h2>`): status glyph + name (13.5, 600, `ink`) + count at the right (DM Mono 12.5, `ink-muted`), padding 4 4 8, bottom `rule`. Cards sit in an `<ol>`.
- **Card** (`<article>`, `aria-label` "Company, Role"): `base`, 1 px `rule-strong`, radius 4, padding 10 6 10 12, gap 6, `cursor: grab`; hover border `field-border`. Content: company (14, 600) and role (13, `ink-2`) together as one link that opens the detail; meta "Channel · 12 Nov 2026" (12.5, `ink-2`, date in DM Mono); optional "No response · N days" status label (§8, table form).
- **Move to… button:** 30 px icon-only (move icon), `aria-label` "Move {Company} to…", `aria-haspopup="menu"`; transparent border and background until hover or open (then `field-border` border + `row-hover`).
- **Move to menu** (`role="menu"`, CDK Menu): 214 px, under the button, right-aligned, `base`, 1 px `rule-strong`, radius 4, padding 4, `shadow-menu`. Heading "Move to" (12, 600, `ink-muted`). Items: the **three other statuses** (36 px, glyph + name, 13.5, radius 3; active item `row-hover`). Footer: `rule` divider + "This also adds an entry dated today to the timeline." (12, `ink-muted`). Arrow keys move between items; Escape closes and returns focus to the button.
- **Truncated column (decided):** after the 200th card, one line "Showing 200 of 340 · see all in the list" (12.5, `ink-2`); "see all in the list" is a link-button that switches to the List view filtered to that status.
- **Detail (decided):** a card opens the same detail panel / sheet as the list (§8, §9).

### Drag and drop (desktop only, CDK drag-drop)
| Part | Look |
|---|---|
| Card being dragged | Follows the pointer: `base`, 1.5 px `accent` border, `shadow-drag`, grip icon (`ink-muted`) on the left, shows company, role, channel · date; `cursor: grabbing` |
| Where it came from | 84 px placeholder with a 1.5 px dashed `rule-strong` border |
| Drop target column | 1.5 px dashed `accent` border, `row-selected` background |
| Where it will land | 3 px `accent` bar (radius 2) with a 10 px ring at the left end (2 px `accent`, `base` fill) |

Escape cancels the drag. Keyboard and screen reader users use "Move to…". A move updates the board at once (optimistic) and adds a timeline event dated today.

### Board states
| State | Shown as |
|---|---|
| Loading | Bordered box (`base`, `rule-strong`, padding 16): spinner + "Loading your board…" (`role="status"`), then four `aria-hidden` skeleton columns with 3 / 2 / 1 / 1 skeleton cards (three `skeleton` bars each) |
| Error | The four column names as a plain header row (12.5, 600, `ink-muted`, `rule` underline), then an error banner "Couldn't load your board" / "The server didn't respond. Your applications are safe." + Try again (30 px) |
| No applications at all | The same empty state as the list (envelope, "No applications yet", "Add a job you applied for and Heardback will keep track of who replies.", Add application) |
| Empty column | A box filling the column: 1.5 px dashed `rule-strong`, radius 4, padding 14 12: "No applications in Offer" (13.5, 600) + "Drag a card here, or use Move to… on any card." (12.5, `ink-2`) |
| Move failed | The card goes back to its old column with a 1.5 px `danger` border and an inline alert inside it (`danger-tint`, radius 3, padding 8, `role="alert"`): warning icon + "Couldn't move this application. Try again." (12.5, 500, `danger`) + Try again (30 px secondary). The column counts go back too |

### Board, mobile (< 1024 px)
- **Header:** logo + wordmark + primary **"Add"** button (plus icon, `aria-label` "Add application") + menu button. No sticky bottom bar on this page. The handoff's Add button is 40 px tall; it is built at **44 px** **(decided)**.
- Below the header: "Applications" (`page-title-sm`) + view toggle; then Search (icon-only label, flex) and Applied via (150 px, option "All"), both 44 px.
- **Status tabs** (`role="tablist"`, one column at a time, **no dragging**): 4 equal tabs, 54 px, `surface` strip with `rule` lines above and below. Each tab: glyph + name (13) over the count (DM Mono 12, `ink-muted`). Selected: `base`, inset 3 px `accent` bar at the bottom, `ink`, 600, `aria-selected="true"`. Others: transparent, `ink-2`, 500.
- **Tab panel:** padding 12 16, gap 8. Cards: padding 12 10 10 14; company 15 / 600, role 13.5 `ink-2`, meta 12.5; on the right a 44 px secondary **"Move to…"** button (move icon + text, `aria-haspopup="dialog"`).
- **Move to sheet:** bottom sheet with `scrim-sheet`: title "Move to" (`dialog-title`), line "Larkspur Labs · Backend Engineering Intern · now in Applied" (13.5, `ink-2`), a bordered list (`rule`, radius 4) of the three other statuses as 52 px rows (glyph + name, 15), the note "This also adds an entry dated today to the timeline." (12.5, `ink-muted`), and a full-width 48 px Cancel.
- **Empty tab:** dashed box "No applications in Offer" (15, 600) + "Use Move to… on any card to move it here." (13.5, `ink-2`).
- **Move failed:** the same alert as on desktop, with the Try again button on its own line (44 px; the handoff shows 40 px) **(decided)**.

### Form layout shared by Add application and Edit
- **Desktop dialog:** 420 px (§8 dialog). Inputs and buttons 36 px, fields 14 px apart.
- **Mobile:** a **full-screen sheet** (not a bottom sheet): header with title and a 44 px ✕, scrolling body with padding 16, inputs 44 px, **sticky footer** (`surface`, top `rule`) with Cancel and the primary button side by side, 48 px each.
- Fields are disabled while saving; the primary button shows the spinner (§7).
- Field labels use the "· required" / "· optional" suffix (§8 text field).

### Add application
Opens with the cursor in the link field.
1. **Job link · optional**: URL input (`inputmode="url"`, placeholder "Paste the job link") + a secondary **Paste** button (clipboard icon, same height) that pastes from the clipboard. The button is hidden when the browser has no Clipboard API. Error: "Enter a full link that starts with https://". Hint **(decided, replaces the autofill hint)**: "Lets us warn you if you already added this job."
2. **Company · required** ("Enter the company name"), **Role · required** ("Enter the role").
3. **Applied via** (select, the six channels) and **Applied date** (date field, defaults to today) side by side.
4. **Job description · optional** (renamed from "Job posting text" by the author, 2026-11-20): textarea (3 rows, placeholder "Paste the job description"), hint "Paste the job description only. Don't add personal notes here; use Notes for those." (posting text is shared between users; Notes are in Edit).

Footer: Cancel + primary **"Add application"** ("Adding…" while busy).

Not built: the "· optional, fastest" label suffix and the "Company, role and posting text filled in from the link" message (§12, no autofill).

| State | Shown as |
|---|---|
| Validation | Field errors as above |
| Server error | Error banner at the top of the body: "Couldn't add the application" / "The server didn't respond. Your details are still filled in. Try again." |
| Already tracked | Shown when the link matches a posting this user already tracks (checked after the link is entered, and again by the server on submit). The other fields are replaced by a panel (`envelope`, 1 px `field-border`, radius 4, padding 12, `role="status"`): info icon + "You already added this job" (14, 600); a small card (`base`, `rule-strong`, radius 4) with company, role, status label and "Applied 03 Nov 2026"; then "Open it to add an update instead of creating a second copy." (12.5, `ink-2`). Footer: Cancel + primary **"View application"**, which opens that application's detail |

### Edit application
Opens from Edit in the detail panel or sheet header. Title "Edit application" with the line "To change the status, use Add update or move the card on the board." under it (12.5, `ink-muted`).
Fields: **Job link · optional** (full width); **Company · required** | **Role · required**; **Applied via** | **Applied date**; **Job description · optional** (same hint as Add); **Notes · optional, only you see these** (textarea, 3 rows).
Errors: "Enter a full link that starts with https://", "Enter the company name", "Enter the role", "Pick today or an earlier date", "Pick a date after 1 January 2000", and **(decided)** "Pick a date on or before your first update (12 Nov 2026)" when the applied date would move past a later timeline event (that would silently change the status).
Footer: Cancel + primary **"Save"** ("Saving…").
Saving re-links the application to the matching shared posting (§12); the user sees nothing special unless it hits the "already tracked" case below.

| State | Shown as |
|---|---|
| Server error | Error banner "Couldn't save your changes" / "The server didn't respond. Your edits are still here. Try again." |
| Changed elsewhere (409, version check) | Warning banner (`status-no-response` border and title, `warn-tint`, refresh icon): "This application changed since you opened it." / "It was edited in another tab or on another device. Reload to see the latest version. Your edits here will be lost when you reload." + secondary "Reload latest version" (32 px; 44 on mobile). **Save is disabled** |
| Link matches another application you track **(decided; not in the handoff)** | Error banner "You already added this job" / "Another of your applications uses this link. Open that one instead, or change the link." |

### Add update
Opens from "Add update" on the timeline (desktop) or the sheet footer (mobile). Desktop dialog 410 px; mobile **bottom sheet** over the detail sheet with `scrim-sheet`. Title "Add update" with "Northwind Logistics · Backend Engineer (.NET)" under it, then "Record a reply, an interview, a rejection or just a note." (same style; added in the plain-language pass, 2026-11-20) (12.5, `ink-2`).
1. **New status · optional** (`<fieldset>` + `<legend>`): radio chips (34 px; 44 on mobile), each a `<label>` with a radio, glyph and name: **No change** (default), Applied, Interview, Offer, Rejected. The current status is disabled and labelled "Interview · current" (`ink-muted`). Chip: 1 px `field-border`, radius 4, `base`; selected: 1.5 px `ink` border, `row-selected`, 600. Focus ring on the chip (`:focus-within`). Hint: "Leave “No change” to add a note only."
2. **Date**: date field 180 px, defaults to today, hint "Today or an earlier date.", error "The date can't be in the future. Pick today or earlier."; **(decided)** "Pick a date on or after the applied date (12 Nov 2026)" for a date before the applied date.
3. **Note · optional**: textarea (2 rows, max 280 characters, placeholder "e.g. Recruiter called, interview next week"), live counter "0 / 280" (DM Mono 12, `ink-muted`, right-aligned).

Footer: Cancel + primary **"Save update"** ("Saving…").

| State | Shown as |
|---|---|
| Nothing chosen | Error banner (title only): "Choose a new status or write a note." |
| Server error | Error banner "Couldn't save the update" / "The server didn't respond. Nothing was added to the timeline. Try again." |
| Saved, status changed | The dialog closes. The envelope status label shows the new status and "Updated today". Above the timeline: "Update added" (inline success, §8). The new event is at the top of the timeline on a `row-selected` background (padding 6, radius 4) |
| Saved, note only | Same, but the status stays and the new event is a **Note**: stamp in `ink-2` with a **dashed** inner border, title "Note" in `ink` with the note glyph, line "24 Nov · Recruiter called, second interview next week" |

### Dates
Applied and update dates are calendar dates in the user's own day. "Today or earlier" is checked in the browser against the local date. The server accepts dates up to one day after the UTC date, so users in time zones ahead of UTC are not rejected.
- **Settings page** (including the theme setting), Dashboard, Companies: in their phases.

## 15. CV page and match panel (Phase 3, composed from the existing system)

**How this was designed (decided by the author, 2026-11-18):** no Claude Design handoff, to save its cost. Both screens use only tokens and components already in this document, with no new colours, sizes or patterns. The author approves them from desktop and phone screenshots (light and dark) instead of a canvas.

### CV page (`/cv`, sidebar item "CV" under Applications)
- **Header:** "CV" (`page-title`) and, when a CV exists, "Uploaded 18 Nov 2026 · 2 pages" (`small`, `ink-2`, date in DM Mono), then one line on what the page is for. Content is at most 800 px wide.
- **Privacy note:** info banner (§8), always shown: "Only the text of your CV is kept" / emails, phone numbers, links and address lines are replaced before it is stored; the PDF is never saved; it can be deleted at any time.
- **Upload area:** a dashed box like the board's empty column (1.5 px dashed `rule-strong`, radius 4, `base`, padding 16, 20 from lg). While a file is dragged over it: dashed `accent` on `row-selected`, like a board drop target. Without a CV it holds the envelope empty state ("No CV yet") and a primary **Choose PDF**; with a CV, "Replace your CV" / "The new file replaces the current one." and a secondary **Choose PDF**. Beside the button: "PDF, up to 5 MB and 10 pages · or drop it here" (`label`, `ink-muted`; "or drop it here" only from lg). Buttons are 44 px below lg.
- **Uploading:** the buttons give way to a spinner and "Reading your CV…" (`role="status"`).
- **After an upload:** inline success "CV uploaded" in a persistent status region (§8).
- **Upload problems:** a problem with the file is an error banner with the server's sentence as its title; the hourly limit is a warning banner ("You've uploaded 5 CVs in the last hour. Try again in about N minutes."); no answer is the usual error banner "Couldn't upload your CV" / "The server didn't respond. Try again."
- **The CV** (bordered section, `rule`, radius 4, `base`): "Skills found" heading (`label`, 600, `ink-2`) with the count in DM Mono, then found **skill chips**; a "Show stored text" ghost button (the chevron turns when open) that reveals the text exactly as stored in DM Mono 12.5 on `surface` (at most 384 px high, scrolls), with the markers `[email]` `[phone]` `[link]` `[address]` on `nav-active` in `ink-2`; a bar with the danger outline **Delete CV** (trash icon; full width, 48 px on phones).
- **Skills that are not the user's (decided 2026-11-18, after testing a real CV):** a 28 px secondary **Remove skills** button at the right of "Skills found" (44 px on phones, `aria-pressed`) turns the found chips into buttons with a ✕ ("Remove Rust: not my skill"); its label becomes **Done**, and a caption explains that removed skills count in no match, also after a new upload. Removed skills appear under **Removed · removed by you** as dashed chips with a + ("Count Rust again"), always shown when there are any. Chip buttons are 28 px tall, 44 px below lg. Failure: error banner "Couldn't save your skills" / "The server didn't respond. Try again."
- **Delete:** the same confirm as deleting an application (§8): "Delete your CV?" / "Your CV text is deleted for good. Skill matches will ask you to upload a CV again." + Cancel / **Delete CV**; failure banner "Couldn't delete your CV".
- **Loading / error:** spinner "Loading your CV…"; error banner "Couldn't load your CV" + Try again.

### Skill chip
24 px high, radius 3, padding 0 8, `label` 500, no wrapping (the status label's measurements, §8).
- **Found** (CV page): `rule-strong` border on `surface`, `ink` text.
- **Matched:** `status-offer` border and text, with a check icon.
- **Missing:** dashed `field-border` border, transparent, `ink-2` text.
- **Excluded** (removed by the user): like missing, in `ink-muted`.
- As a **button** (remove or restore): the whole chip is the target, with a 12 px ✕ or + at the end, hover `secondary-hover`, the standard focus ring.

Matched and missing differ by the check and the border style, never by colour alone.

### Match panel ("CV match", in the envelope between Notes and Timeline)
- Heading like Notes and Timeline (`label`, 600, `ink-2`); when ready, "3 of 5 skills · 60%" at the right in DM Mono (`label`, `ink`). **No gauge or ring:** the share is plain text.
- **Ready:** "Your CV shows" + matched chips, "Missing from your CV" + missing chips (captions in `caption`, `ink-muted`), then "Skills named in the role and posting text, compared with your CV."
- **No CV:** "This job asks for" + missing chips, then "Upload your CV to see which of these it shows." with a link to the CV page.
- **No posting text:** "Add the job description to see how your CV matches." + link-button "Edit application" (opens Edit).
- **No known skills:** "No skills Heardback knows were found in this job description." (neutral, not an error).
- **Loading:** spinner "Checking your CV…". **Error:** warning icon and "Couldn't check your CV." in `danger` with a 28 px Try again; the rest of the detail stays usable.
- The panel checks again whenever the application changes (Edit, Add update, a board move).

## 16. AI sections (Phase 4, composed from the existing system)

**How this was designed (decided by the author, 2026-11-19):** like §15, no Claude Design handoff; only tokens and components already in this document. The author approves from desktop and phone screenshots (light and dark). **Every AI section is hidden when AI is off on the server** (no API keys), not shown as "unavailable".

### Groups and headings (decided by the author, 2026-11-19, after testing)
The AI and match sections were hard to tell apart, so the detail groups them under two titles, each a dashed `rule-strong` top border with 10 px above the text, `cell` 600 `ink`: **About this job** (Job summary) and **Your CV for this job** (Skills match, then How to improve your CV). Group titles are `h3`, section headings `h4`. Sections an LLM wrote carry a small **AI tag** at the right of their heading: 20 px high, radius 3, `field-border` border, `caption` 500 `ink-2`, plain text "AI" (screen readers hear "written by AI"); the skills match, worked out exactly, has none.

### Job posting (application detail, after Notes; added at the author's request, 2026-11-19)
The posting text was only visible in Edit. Heading "Job posting" like Notes, then the CV page's stored-text pattern (§15): a ghost "Show job description" / "Hide job description" (renamed from "posting text" by the author, 2026-11-20) with the turning chevron, revealing the text as entered (`small`, `ink`, line breaks kept) on `surface` with a `rule` border, radius 4, at most 384 px high, scrolling. Closed by default and again when another application opens; not shown without posting text.

### Job summary (posting analysis; application detail, "About this job", between Notes and the CV group)
- Heading "Job summary" like Notes (`label`, 600, `ink-2`) with the AI tag; the "About this job" title shows only with it. Hidden while loading and when the application has no posting text (the match panel already asks for it).
- **Not requested:** "AI can summarize this job for you: title, seniority, work mode, location and the skills it asks for." + 28 px secondary **Summarize job** (44 px below lg) and, beside it, the usage line in DM Mono `label` `ink-muted`: "28 of 30 left today · resets 03:00" (the reset in the user's own time zone, 24-hour).
- **Queued / running:** spinner "Summarizing the job…" (`role="status"`); the panel checks again every 3 seconds.
- **Ready:** a two-column list (`small`; labels `ink-muted`, values `ink`; "Not stated" in `ink-muted`): Company, Title, Seniority, Work mode, Location; then the skills as **found** chips (§15); caption "Summarized by AI from the job description on 19 Nov 2026." (date in DM Mono).
- **Waiting for capacity** (app-wide daily cap): clock icon, `status-no-response` text: "AI has reached its daily limit. Your summary will be ready after 03:00."
- **Failed:** warning icon, `danger`: "Couldn’t summarize this job." + 28 px Try again (asks again).

### How to improve your CV (tailoring suggestions; "Your CV for this job", below the skills match)
- Heading "How to improve your CV" with the AI tag. The skills match (§15) is now headed "Skills match". Hidden while loading, when AI is off, without posting text and without a CV (the match panel covers those).
- **Consent needed** (decided 2026-11-19: consent in place, one click): "AI can suggest how to present what’s already in your CV so it fits this job better." then a `caption` `ink-muted` paragraph saying what is sent to whom, with a link to the CV page, then a 28 px **primary** "Allow and get suggestions". The click records consent and asks.
- **Not requested:** the same sentence + 28 px secondary **Get suggestions** + usage line.
- **Ready:** the summary (`small`, `ink`), then an ordered list; each item has a 2 px `rule-strong` left border and 10 px padding: the CV section (`label`, 600, `ink-2`), the quoted CV line in DM Mono `caption` `ink-muted` in quotation marks, the advice (`small`, `ink`). Always ends with an info icon and "Written by AI from your CV and this job description. Check each suggestion before you use it." (`caption`, `ink-muted`).
- **Out of date** (CV or posting changed): above the suggestions, a box like the info banner but compact (`surface`, `field-border`, radius 4): info icon "Your CV or this job description changed since these suggestions were written." + **Update suggestions** + usage line. While new ones are written the old ones stay visible under the spinner "Writing suggestions…".
- **Queued / running, waiting for capacity, failed:** as in Posting analysis ("Writing suggestions…", "AI has reached its daily limit. Your suggestions will be ready after 03:00.", "Couldn’t write suggestions.").

### AI suggestions (CV page, after the CV)
- Bordered section (`rule`, radius 4, `base`), heading "AI suggestions" (`body`, 600). Text on what is sent, to whom, and that it never goes to providers that may train on it.
- **Allowed:** "Allowed since 19 Nov 2026." + danger outline **Stop sending my CV** (opens the confirm below).
- **Not allowed:** "Not allowed. Your CV text is not sent anywhere." + secondary **Allow**.
- **Withdraw confirm** (the delete confirm, §8): "Stop sending your CV to AI?" / "All suggestions made from your CV are deleted. You can allow it again from “How to improve your CV” in any application." + Cancel / danger-solid **Stop and delete suggestions**.

### Limit and failure messages
- Daily limit (429): warning banner, `alert`, "Daily limit reached" / "You’ve used all 30 job summaries for today. You can summarize more after 03:00." and "You’ve used all 10 suggestion requests for today. You can ask for more after 03:00." (wording revised by the author on 2026-11-19).
- Request failed: error banner "Couldn’t start the summary" / "Couldn’t start the suggestions" + "The server didn’t respond. Try again."; consent save failed: "Couldn’t save your choice".
- Load failed: "Couldn’t load the analysis." / "Couldn’t load the suggestions." + Try again, the rest of the detail stays usable.

### Decided (approved by the author from the screenshots and their own test, 2026-11-19)
| Item | Proposal |
|---|---|
| Section names | Decided 2026-11-19: "About this job" › "Job summary"; "Your CV for this job" › "Skills match", "How to improve your CV"; "AI suggestions" (CV page) |
| Consent wording | "To do that, Heardback sends your CV text (emails, phone numbers, links and addresses already removed) and this job description to Groq, an AI provider. You can stop this at any time on the CV page." |
| AI label | "Written by AI from your CV and this job description. Check each suggestion before you use it." |
| Seniority labels | Not stated, Intern, Junior, Mid-level, Senior, Lead; work mode: Not stated, On-site, Hybrid, Remote |
| Server error texts | "Add the job description to analyse it.", "Add the job description to get suggestions.", "AI analysis isn’t available right now.", "Allow Heardback to send your CV text to the AI provider first.", "Upload a CV to get suggestions." (shown only if the UI and server disagree) |

## 17. Overview (Phase 5, composed from the existing system)

**How this was designed (decided by the author, 2026-11-19):** no Claude Design handoff. The author approved an ASCII layout first, then screenshots of the built page. Only existing tokens; the charts are hand-drawn SVG (no chart library). Sidebar item "Overview" after CV (named by the author on 2026-11-19; the page shows the Insights module). URL `/overview`.

- **Page:** title "Overview" (`page-title`) with "19 applications" (`small`, `ink-2`); content at most 960 px wide; sections are bordered boxes (`rule`, radius 4, `base`, padding 12×16), headings `label` 600 `ink-2` (Follow up: `body` 600 `ink`).
- **Follow up** (first: the one thing to act on): "N waiting" in DM Mono at the right. Up to 5 rows (company `cell` 600, role `small` `ink-2`, "No response · N days" `label` in `status-no-response`), each with 28 px secondary **Open** (opens the application in the list) and **Mark followed up** (adds the note "Followed up" dated today, so the row leaves the list); 44 px and full-width pairs below lg. "See all N in the list" link opens the list filtered to No response (`/applications?status=NoResponse`). Caption explains what marking does. Nothing waiting: "Nothing waiting. No application has gone quiet long enough to follow up."
- **Response rate** and **Time until the first reply** side by side from lg: the headline number in DM Mono at `page-title` size, a short word beside it ("heard back", "on average"), and a caption with the counts behind it ("8 of 19 applications got an answer: an interview, an offer or a rejection.", "Median 4 days, from 8 responses."). No responses: "No responses yet."
- **Applications per week:** 12 bars in equal cells, 8 units wide, radius 1; past weeks `rule-strong`, empty weeks a 2-unit stub in `rule`, this week `accent`. Under the bars the count of each week (DM Mono `caption` `ink-2`), then the first week's date and "This week" (`caption` `ink-muted`). Each bar has a native tooltip; a visually hidden table repeats the numbers. One series, so no legend.
- **By how you applied:** per channel the label, "2 of 9 · 22%" (DM Mono `label`) and an 8 px meter (`rule` track, `ink-2` fill, radius 2). **How far they got:** Applied, Interview, Offer with meters relative to Applied and the counts at the right; caption "The furthest stage each application reached. 4 were rejected." Meters are decorative; the numbers carry the meaning.
- **States:** loading spinner "Working out your numbers…"; error banner "Couldn’t load your numbers" + Try again; no applications: envelope empty state "No numbers yet" / "Add your first application to see how your search is going." + primary "Go to Applications"; follow-ups failing alone: inline "Couldn’t load the applications to follow up." + Try again; a failed mark: "Couldn’t save. Try again." on that row.

## 18. Community signals (Phase 6 handoff)

Source: Claude Design canvas page "B refined · Community signals" (artboards *Ghost-risk badge*, *Application detail · Community section and states*, *Company page*), handed off 2026-11-20. Data and wording rules come from ADR-0013. Only existing tokens: `danger`, `status-no-response`, `rule`, `rule-strong`, `envelope`, `base`, `surface`, `danger-tint`, `skeleton`, `link`, `row-hover`, `ink` / `ink-2` / `ink-muted`.

### Ghost-risk badge — `app-ghost-badge`
- **Only Medium and High.** Low gets no badge anywhere in lists.
- 22 px high, padding 0 7 0 6, gap 5, **1 px dashed** border in the level colour, radius 3 (`rounded-sm`), text 12 px `ink-2`, no wrap.
- Content: a 27 × 10 wave meter (five small arcs, stroke 1.4, round caps; the first *score* arcs in the level colour, the rest `rule-strong`; `aria-hidden`), "Ghost risk" and the level in weight 600 and the level colour.
- Level colour: **High** `danger`; **Medium** `status-no-response`.
- Screen readers hear "Ghost risk High, 4 of 5" (", 4 of 5" visually hidden).
- Placement: **table** in the Status cell after the status label (gap 8, wraps); **board card** in a wrapping row under the meta line, after the optional "No response" label (gap 6); **mobile list card** in the status row after the status label (gap 6); **company page rows** after the status label.

### Community section (application detail, desktop panel and mobile sheet)
Inside the envelope, right after the status / channel row, with a 1 px dashed `rule-strong` top border and 12 px above. It replaces the old ghost-risk box (§12 item 5). Content gap 12.
- **Heading** "Stats from other members" (14, 600, `ink`) and the line "Members who applied to the same posting or company." (12, `ink-muted`) **(decided: plain headings that say what the numbers are)**.
- **Ghost-risk box** (Medium and High): `base`, 1 px `rule-strong`, radius 3, padding 10 12, gap 8. Top row: "Ghost risk: High" (13.5, 600, level colour), the 84 × 14 five-wave meter (stroke 1.6), "4 of 5" at the right (DM Mono 12, `ink-muted`). Then the hint (13, `ink`): High "This job is likely inactive. Here is why:", Medium "This job may be inactive. Here is why:". Then **every reason** from the API as a bulleted list (13, `ink-2`, gap 4, largest point first).
- **Low** (numbers exist, at most one point): no box, one quiet line: `base`, 1 px `rule`, radius 3, padding 8 12; the meter with the first wave `ink-muted` and the rest `rule-strong`; "**Ghost risk: Low.** No warning signs from other members so far." (13, `ink-2`; the first part 600 `ink`).
- **Numbers table**: a label column and one right-aligned column per group, headers "This job" / "This company" (12, 600, `ink-2`, 1 px `rule-strong` underline). A real `<table>` with row and column headers. Rows (13; labels `ink-2`; values DM Mono 12.5 `ink`; 1 px `rule` dividers; padding 6 0): **Members who applied**, **Applications**, **Heard back** ("3 · 21%"), **Average days to first answer** ("23.5"), **Median days to first answer** ("19"). An empty time is "—", with the caption "“—” means nobody has heard back yet, so there is no answer time." (12, `ink-muted`). Value cells give screen readers a version that reads well ("3, 21 percent", "23.5 days", "No answers yet"). Two 88 px columns when both groups have numbers (the handoff's 76 px wrapped "This company" onto two lines; headers stay on one line); one 96 px column when only one group has (headed "This job" or "This company"). **(decided)** When only the company has numbers, the line "Fewer than 3 other members applied to this exact job, so only company numbers are shown." (13, `ink-2`) sits above the table; when only the posting has, "Fewer than 3 other members applied to this company, so only job numbers are shown."; the ghost-risk box still shows whatever reasons exist.
- **Privacy line** (12, `ink-2`, 14 px info icon): "Based on other members only. You are not counted. Numbers appear once at least 3 other members have applied."
- **"All your applications at {Company}"** link (13.5, 600, `link`, chevron; replaces the handoff's "View company", which did not say where it goes) to `/companies/:key` when the application has a company key.
- **No company key** (older application): instead of the link, under a dashed `rule-strong` divider: "Company stats and the list of your applications at this company aren’t available here, because this application was added before companies were linked." (12.5, `ink-2`).
- **States:** *loading*: `base`, 1 px `rule`, radius 3, padding 12; spinner + "Loading community numbers…" (13, `ink-2`, `role="status"`) and three `skeleton` bars (70 / 90 / 55 %). *Error*: the error banner (§9) "Couldn’t load community numbers" / "Your own application details are fine." + secondary **Try again** (30 px desktop, 44 px mobile). *Not enough data*: 1.5 px dashed `rule-strong`, radius 4, padding 12; "Not enough data yet" (13.5, 600) / "Fewer than 3 other members have applied to this job. To keep everyone anonymous, no numbers are shown until at least 3 have." (13, `ink-2`); the link to your applications at the company still shows under it.
- Mobile sheet: the same content; buttons 44 px.

### Company page (`/applications/companies/:key`)
Reached only from the "All your applications at {Company}" link; inside the normal app shell. The URL sits under `/applications` (the handoff says `/companies/:key`) so **Applications** stays the active nav item. No search, no list of companies.
- **Desktop:** "Back to Applications" with a left chevron (13, `ink-2`, no underline), which opens the application the page was opened from again (`?from=` in the URL; decided by the author, 2026-11-20), then the company name (Newsreader 28 / 34, 600, −0.01em) (no caption under it). Two columns, gap 24, from the xl breakpoint; between lg and xl the stats box sits under the list:
  - **Your applications at {Company}** (16, 600) + "Only your own applications are listed. Select one to open it." (12.5, `ink-2`). Table (columns sized by their content; dates, "Last update" and headers never wrap): 1 px `rule`, radius 4; header 40 px `surface`, 12.5 / 600 `ink-2`, `rule-strong` underline; rows at least 48 px with `row-hover`: **Role** (link, 600, `ink`, opens the application), **Status** (status label + ghost badge, wrapping), **Applied** (DM Mono 12.5), **Last update** (right-aligned, `ink-2`, relative such as "14 days ago").
  - **Stats box** (440 px from lg, so its heading fits on one line; the handoff's 360 px wrapped it; `envelope`, 1 px `rule-strong`, radius 4, padding 16, gap 10): heading "Stats from other members who applied to this company" (15, 600; no separate description line), the same five rows in one 96 px column, and the privacy line ending "Their postings and roles are never shown."
- **Mobile:** the same "Back to Applications" link at 44 px height (the app header is already on screen, so no second header bar). Name (Newsreader 24 / 30), then "Your applications at {Company}" as a list (role 15 / 600 with the short applied date at the right in DM Mono 12 `ink-muted`; status label + badge below; `rule` dividers), then the stats box (96 px value column; privacy line "Based on other members only. You are not counted. Shown once at least 3 others applied.").
- **States:** *loading*: spinner "Loading company…" with a skeleton title, three 40 px row bars and a box outline. *Not enough data*: your applications still show; the box holds the dashed "Not enough data yet" card ("…applied to this company…"). *Error*: back link + error banner "Couldn’t load this company" / "The server didn’t respond. Your applications are safe." + Try again. *Not found*: envelope illustration (§9 empty state), "You have no applications at this company" (Newsreader 22, mobile 24), "Company pages only open from your own applications. The link may be old, or the application was deleted." and secondary "Go to Applications" (36 px; full-width 48 px on mobile).

### Aligned with the API (decided by the author, 2026-11-20: no new design round; the rules above already include these)
1. **Only one group has numbers.** The handoff draws "both groups" and "posting only"; with about 15 members the most common case is the company passing the minimum while the posting does not. That case uses the one-column table headed "This company" with "Not enough members for this posting yet." above it (and the mirror for posting only).
2. **Badge ranges follow ADR-0013:** Low 0–1, Medium 2–3, High 4–5, so a 2-point posting shows a Medium badge. The handoff's "Low · 1–2 of 5" label is not used.
3. **"Last update" on the company page** comes from a new `updatedAt` on the company page items (relative time, like the list).
4. **Plain headings** (decided by the author after testing the demo, 2026-11-20): the same day an app-wide plain-language pass followed (one word for the listing, "job"; "Channel" became "Applied via"; clearer CV, AI and Edit hints), keeping "Ghost risk" as designed. Here: headings say what the numbers are instead of "Community" / "Community numbers", the link says where it goes ("All your applications at {Company}"), "Other members" became "Members who applied", the one-column case explains itself, and captions that repeat a heading (or only said "Company name as you saved it") are gone.
5. The handoff's sample sidebar (Dashboard / CV / Settings) and sample numbers ("Medium · 3 of 5" from two one-point reasons) are mock data; the real shell and API values are used.

## 19. UI terms and code names

The UI uses plain words for the people who use Heardback; the code, API and database keep the domain names
from PLAN.md and the ADRs. Both are deliberate (decided by the author, 2026-11-20): UI copy can change without
renaming API fields, columns and migrations. When the two differ:

| On screen | In code, API and database |
| --- | --- |
| Job description | `postingText` (`posting_text`) |
| Job link | `url` |
| Applied via | `channel` (`ApplicationChannel`) |
| Job (the listing several members can share) | `JobPosting` in the Postings module (ADR-0004) |
| Add update / timeline entry | `ApplicationEvent` |
| No response · N days | derived from `NoResponseRule`, never stored |
| Stats from other members | community outcomes, `ICommunityOutcomes`, `/api/community` (ADR-0013) |
| Members who applied | `CommunityOutcome.Members` (other members only) |
| Ghost risk, warning signs | `GhostSignal`, `GhostReason` (ADR-0013) |
| All your applications at {Company} | company page, `/api/community/companies/{key}` (`CompanyKey`) |
| Removed (CV skills) | excluded skills, `excludedSkills` |
