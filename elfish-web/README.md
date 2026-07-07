# El-Fish Web

A browser-based homage to **El-Fish** (Maxis / AnimaTek, 1993) — the DOS-era
"software toy" where you caught, bred, and animated fish in a virtual aquarium.

Open `index.html` in any modern browser. No build step, no dependencies,
no server — everything (game logic, sprite generation, UI) lives in the one file.

## What it does

- **640×480 VGA-style tank** (the original's actual resolution) rendered on a
  pixelated canvas: dithered water
  gradient, gravel bed, rocks, swaying plants, bubbles, drifting light rays.
- **Procedurally generated fish.** Every fish has a genome (body shape/size,
  tail type, dorsal/pectoral fins, base/accent/fin colors, pattern, eye size,
  swim speed). Sprites are pre-rendered as 8-frame animation strips with
  ordered (Bayer) dithering for the classic 256-color look — a nod to the
  original's fish "animation compiler".
- **Breeding.** Click two fish to select them, then *Breed Selected*:
  per-gene crossover with a 12% mutation chance produces a fry that starts
  small and grows up in the tank.
- **Fishing** (*Go Fishing*) adds a brand-new random fish.
- **Feeding** drops pellets the fish chase and eat.
- **Critters** (*Add Critter* dropdown): optional bottom-dwelling companions,
  each with natural behavior and randomized coloring. Snails creep across the
  gravel at a true snail's pace and climb the glass walls; freshwater shrimp
  (cherry or translucent ghost) forage with flickering legs, amble toward
  sinking food, and dart backwards when a fish looms; clams sit half-buried,
  slowly gaping and exhaling bubbles; crayfish patrol the floor, snap their
  pincers, and escape with a backward tail-flip. Right-click a critter to
  release it.
- **Save/Load** persists the whole tank to `localStorage`.

## Note on graphics

The original game's art is copyrighted, so no assets from El-Fish are used or
included. All graphics are generated at runtime in the same low-res, dithered
VGA spirit.
