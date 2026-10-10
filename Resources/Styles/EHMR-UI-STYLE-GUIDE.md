# EHMR UI Style Guide (branch rbac)

Use this guide as the visual source of truth for new and updated screens. Keep existing behavior and bindings intact. Do not add new `{StaticResource ...}` color references in this style or in updated controls; use explicit hex literals for color values to avoid missing-resource/XAML-load failures. Existing binding references that carry data or commands are not color resources and remain valid.

## 1. Brand and semantic colors

| Role | Hex | Use |
| --- | --- | --- |
| Primary accent | `#2AEBE7` | Main action, selected menu item, active tab, focus outline, grid selection accent |
| Accent hover | `#17CFCB` | Hover state for primary actions |
| Accent pressed / strong accent | `#0F9693` | Pressed state and small accent text where contrast is needed |
| Accent tint | `#D9FBFA` | Selected-row and selected-control backgrounds |
| Main text | `#0F172A` | Titles and text on the bright accent |
| Body text | `#334155` | Form and grid text |
| Secondary text | `#64748B` | Hints, captions and secondary labels |
| Disabled / placeholder | `#94A3B8` | Disabled values and placeholders |
| Page canvas | `#F1F5F9` | Main content background behind cards |
| Card / control surface | `#FFFFFF` | Cards, inputs and secondary buttons |
| Border / divider | `#D9E0E5` | Thin separators and card borders |
| Alternate row / quiet surface | `#F8FAFC` | Alternate grid rows and nested panels |
| Sidebar | `#202D3D` | Main navigation background |
| Sidebar header / user card | `#182330` | Menu brand strip and bottom user card |
| Sidebar text | `#CBD5E1` | Normal menu labels |
| Sidebar muted text | `#9AA6AD` | Secondary menu labels |
| Success | `#15803D` | Positive completion/approval action only |
| Danger | `#B42318` | Delete, cancel and destructive actions |
| Warning | `#D98E2B` | Warning status only |

Keep success, warning and danger colors semantic; do not recolor them as brand accents.

## 2. Menu / navigation

- Expanded width: 255–260 px; collapsed width: 72 px.
- Background: `#202D3D`; header and user panel: `#182330`.
- Active group: `#2AEBE7` with dark text `#0F172A`.
- Inactive labels: `#CBD5E1`; muted role/help text: `#9AA6AD`.
- Navigation label: 13–14 px; icons: 14–16 px; brand label: 18 px bold.
- Menu items should remain compact, aligned, and without decorative shadows.

## 3. Main page / header

- Canvas: `#F1F5F9`; page content cards: white.
- Horizontal page padding: 24 px; vertical padding: 20 px.
- Page title: 24 px bold, `#0F172A`; subtitle: 13–14 px, `#64748B`.
- Section heading: 16 px bold, `#334155`; section divider/accent: `#2AEBE7`.
- Keep the header actions aligned on the same baseline as the page title/action row.

## 4. Buttons

- Primary (Save/Create/Confirm): `#2AEBE7`, text `#0F172A`, hover `#17CFCB`.
- Secondary (Back/Edit/Preview): white background, `#334155` text, `#CBD5E1` border.
- Success (Approve/Complete): `#15803D`, white text.
- Danger (Delete/destructive cancel): `#B42318`, white text.
- Ghost / clear / close: `#F1F5F9`, `#475569` text, `#CBD5E1` border.
- Standard height: 40 px; compact height: 34 px; large height: 46 px.
- Radius: 6 px; label: 13 px; bold only for primary/destructive actions.
- Keep icon-only buttons visually quiet and use the same border/radius family.

## 5. Grid / tables

- Header: `#33414A` background, `#FFFFFF` text, 12 px bold.
- Rows: white `#FFFFFF`; alternate row: `#F8FAFC`; hover: `#F1F5F9`.
- Selected row: `#D9FBFA`; accent indicators and active pagination: `#2AEBE7`.
- Body text: 12 px `#334155`; secondary cell text: 11–12 px `#64748B`.
- Border: 1 px `#D9E0E5`; row height target: 44–52 px; cell horizontal padding: 12–16 px.
- Preserve readable contrast and never use the bright accent as a large text fill.

## 6. Cards and forms

- Card: white `#FFFFFF`, 1 px `#D9E0E5` border, 8 px radius, 16–20 px padding.
- Form section / nested area: `#F8FAFC` or white; do not introduce another accent hue.
- Input: white, text `#334155`, border `#CBD5E1`; focused border `#2AEBE7`.
- Labels above inputs: 11–12 px `#64748B`; input text: 13 px.
- Standard input height: 40–42 px; compact input: 36 px; large input: 48 px.
- Prefer consistent spacing of 8, 12, 16 and 24 px; no unnecessary shadows.

## 7. Typography

- Body/UI font: use the platform default sans-serif for app text for reliable cross-platform rendering; do not use the icon font for text.
- Icons only: `FASolid`.
- Page title: 24 px bold; section title: 16 px bold; normal text: 13 px; grid text: 12 px; field label/caption: 11–12 px.
- Use 14 px for dense but important secondary headings and 10–11 px only for compact metadata.

## Implementation notes

- `Resources/Styles/EhmrUnifiedStyle.xaml` contains reusable keyed styles with direct hex values and no color `StaticResource` references.
- `Resources/Controls/FFButton.xaml.cs` applies the same button palette to the custom FFButton control.
- Keep existing data bindings, commands, navigation, and status behavior unchanged when applying these styles.
