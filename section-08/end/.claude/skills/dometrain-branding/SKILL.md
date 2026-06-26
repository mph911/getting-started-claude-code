---
name: dometrain-branding
description: Apply Dometrain's brand colors, typography, and styling to UI, CSS, components, slides, diagrams, or any visual asset. Use whenever asked to "make it look like Dometrain", "apply our/Dometrain branding", "use the brand colors", style a page/component on-brand, or build a Dometrain-styled visual.
---

# Dometrain Branding

Dometrain's brand is a **dark, modern, developer-focused** look: deep navy backgrounds, a signature
purple→pink→peach gradient for accents, and clean Poppins typography. When applying branding, default to
the **dark theme** — it is the primary surface across dometrain.com.

## Color palette

### Brand accents (the three signature colors)
| Token       | Hex       | Use                                                        |
| ----------- | --------- | ---------------------------------------------------------- |
| Purple      | `#756AF6` | Primary brand color — links, primary buttons, focus rings  |
| Pink        | `#CC7DDA` | Secondary accent, gradient midpoint                        |
| Peach       | `#F6BE85` | Tertiary accent, gradient endpoint, highlights             |

### Surfaces (dark theme — primary)
| Token         | Hex       | Use                                            |
| ------------- | --------- | ---------------------------------------------- |
| Background    | `#0D1130` | Page background (deep navy)                    |
| Surface       | `#1A1D3A` | Cards, panels, raised surfaces                 |
| Surface alt   | `#262C57` | Borders, dividers, hover states on surfaces    |
| Surface deep  | `#221F41` | Insets, code blocks, wells                     |

### Text
| Token         | Hex       | Use                                            |
| ------------- | --------- | ---------------------------------------------- |
| Text primary  | `#FFFFFF` | Headings and primary copy on dark              |
| Text body     | `#C2CEED` | Body text on dark (soft lavender-blue)         |
| Text muted    | `#8E94B8` | Secondary/muted text, captions                 |
| Text subtle   | `#8B99C8` | Disabled / least-emphasis text                 |

### Functional / brand-adjacent
| Token        | Hex       | Use                                             |
| ------------ | --------- | ----------------------------------------------- |
| .NET purple  | `#512BD4` | When referencing .NET specifically              |
| Success      | `#10B981` | Success / confirmation states                   |
| Error        | `#FF6B6B` | Errors / destructive actions                    |

## The signature gradient

This purple→pink→peach gradient is Dometrain's most recognizable visual element. Use it for hero text,
key headings, primary CTAs, underlines, and decorative accents — sparingly, as a highlight.

```css
/* Canonical 3-stop brand gradient */
background: linear-gradient(135deg, #756AF6, #CC7DDA 50%, #F6BE85);

/* Diagonal hero variant (as used on dometrain.com) */
background: linear-gradient(87.03deg, #756AF6 16.38%, #CC7DDA 57.23%, #F6BE85 97.64%);

/* 2-stop purple→pink for smaller accents / buttons */
background: linear-gradient(90deg, #756AF6, #CC7DDA);
```

Gradient text:
```css
.brand-gradient-text {
  background: linear-gradient(135deg, #756AF6, #CC7DDA 50%, #F6BE85);
  -webkit-background-clip: text;
  background-clip: text;
  color: transparent;
}
```

## Typography

- **UI / headings / body:** `Poppins, sans-serif`
- **Code / monospace:** `JetBrains Mono, monospace`

```css
@import url('https://fonts.googleapis.com/css2?family=Poppins:wght@400;500;600;700&family=JetBrains+Mono:wght@400;500&display=swap');
```

Headings lean bold (600–700); body text is regular (400) in `#C2CEED` on dark surfaces.

## Drop-in CSS variables

When styling a page or component, define these custom properties and reference them rather than hardcoding hex
values:

```css
:root {
  /* Brand accents */
  --dt-purple: #756AF6;
  --dt-pink:   #CC7DDA;
  --dt-peach:  #F6BE85;

  /* Surfaces (dark) */
  --dt-bg:           #0D1130;
  --dt-surface:      #1A1D3A;
  --dt-surface-alt:  #262C57;
  --dt-surface-deep: #221F41;

  /* Text */
  --dt-text:        #FFFFFF;
  --dt-text-body:   #C2CEED;
  --dt-text-muted:  #8E94B8;

  /* Functional */
  --dt-success: #10B981;
  --dt-error:   #FF6B6B;

  /* Signature gradient */
  --dt-gradient: linear-gradient(135deg, #756AF6, #CC7DDA 50%, #F6BE85);

  /* Type */
  --dt-font-sans: 'Poppins', sans-serif;
  --dt-font-mono: 'JetBrains Mono', monospace;
}
```

## Component patterns

```css
body {
  background: var(--dt-bg);
  color: var(--dt-text-body);
  font-family: var(--dt-font-sans);
}

/* Primary CTA */
.dt-btn-primary {
  background: var(--dt-gradient);
  color: #fff;
  border: none;
  border-radius: 10px;
  padding: 0.75rem 1.5rem;
  font-family: var(--dt-font-sans);
  font-weight: 600;
  cursor: pointer;
}

/* Secondary / outline button */
.dt-btn-secondary {
  background: transparent;
  color: var(--dt-purple);
  border: 1px solid var(--dt-purple);
  border-radius: 10px;
  padding: 0.75rem 1.5rem;
}

/* Card */
.dt-card {
  background: var(--dt-surface);
  border: 1px solid var(--dt-surface-alt);
  border-radius: 12px;
  padding: 1.5rem;
}

/* Links */
a { color: var(--dt-purple); }
a:hover { border-color: rgba(117, 106, 246, 0.3); }

/* Code */
code, pre {
  font-family: var(--dt-font-mono);
  background: var(--dt-surface-deep);
}
```

## Guidelines

- **Default to dark theme** (`#0D1130` background). It's the primary brand surface.
- **Use the gradient as an accent, not a fill** — hero headings, primary CTAs, underlines, small decorative
  elements. Don't paint large background areas with it.
- **Body text is `#C2CEED`, not pure white** on dark surfaces; reserve `#FFFFFF` for headings/emphasis.
- **Generous rounding** (10–12px radii) and ample padding match the modern, friendly feel.
- **Honor existing tokens.** If the target project already defines CSS variables or a theme, map these brand
  values onto those tokens instead of introducing a parallel system.
- **Don't invent colors.** Stick to this palette; if a needed shade is missing, derive it from the nearest
  brand color (e.g. tint/shade of `#756AF6`) rather than picking an arbitrary hex.
