# K-menu header image art — 0.24.1 test branch

Branch: `feature/kmenu-header-art`, based on the integrated 0.24.0 `main` baseline. No merge or release is implied by this document.

## Scope

The top K-menu header now uses the six image concepts explicitly approved for this update instead of the previous code-drawn header marks and plain brand text.

The approved image set is used for:

- the Forever Skyblock brand panel;
- Skill Points;
- Perk Points;
- Shop Tier;
- Playtime;
- Island Level.

No alternate icons, substitute artwork or third-party images are introduced. The committed PNGs are scale-only menu derivatives of the six approved generated images. Their visual content is not redrawn or replaced.

## Rendering

The brand image is fitted into the existing left header lane. The five metric images occupy the previous icon lanes for their respective cards while the existing labels, values, Shop Tier progress bar and authoritative server-provided statistics remain unchanged.

The renderer adapts image display size to the existing normal/compact K-menu geometry. Tiny layouts keep the established collapsed header behavior.

This update is presentation-only. It does not alter Skill Points, Perk Points, Shop purchases, tier rules, statistics, Island Level calculation, profile schemas, `skills-9`, challenge state or server transactions.

## Asset identity guard

A JUnit regression locks the six committed menu PNGs by dimensions and SHA-256. This is intentional: changing or regenerating one of these images should fail the test until a new visual is explicitly approved.

Expected committed menu assets:

| Asset | Dimensions | SHA-256 |
| --- | ---: | --- |
| `header_brand.png` | 150×63 | `9860f553d4a6bf69ef850c1924a5274c5b78bc62dcd99faeb4b212244180cc5c` |
| `header_skill_points.png` | 24×24 | `d559c36ea2d0537fd95811a17cbd8fce97be56483e89810de2e0023b9a7fdcaa` |
| `header_perk_points.png` | 24×24 | `cbeb4ad06b39b18f1b6586259019cbaea00d24b8c24818b7013c020b927fd914` |
| `header_shop_tier.png` | 24×24 | `c24e65b33d73342214377e107ca1944eae54e9d03ce4e38f3dc5e171193a4fb1` |
| `header_playtime.png` | 24×24 | `b801422a2f2a4901be464db551b887e1eda82063582685fd04a5f18796f26ed6` |
| `header_island_level.png` | 24×24 | `f0beb871a7eb9225069067b045e7186890e184813f35d3efb06a050ee3afec15` |

## Validation target

The normal CI build must pass the Java/unit suite, client mixin smoke test, native K-menu visual fixture run, dedicated GameTests and NeoForge 21.1.259 integration suite. The visual fixture artifact is used to inspect actual Minecraft scaling rather than relying only on source geometry.

The fixed starter-island NBT is outside this change.
