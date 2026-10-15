# Early progression proposals

**Status: proposed, not yet confirmed by TeKatz and not implemented.**

The discussion paused after the following proposal to set up the repository. A request to import previous work does not automatically approve this gameplay idea.

## Latest concrete proposal

| Step | Proposal |
| --- | --- |
| Starting material | New **Stone Fragments** item |
| Mark I | One fixed output, produced slowly without external power |
| Initial test rate | One fragment every two seconds |
| Cobblestone | Four fragments in a 2×2 recipe produce one cobblestone |
| First furnace | Initially use the normal recipe requiring eight cobblestone |
| Stone | Smelt cobblestone in a furnace using wood or charcoal |
| Extraction | Initially manual, from a small, bounded output buffer |
| Full inventory | Production pauses |
| Later expansion | Item transport, further processing and a water/lava cobblestone generator for higher throughput |

At the proposed rate, one cobblestone takes eight seconds and eight cobblestone take 64 seconds, excluding handling time. These are calculated proposal values, not measured production times.

## Decide before implementation

1. Does this opening fit the intended Expert difficulty?
2. Which process provides the first metals without direct iron output from Mark I?
3. Which materials are required for the first new processing machine?
4. How does water become available early enough, and when does lava follow?
5. How can players recover basic resource production after losing or dismantling individual starter items?

New materials and machine behavior belong in `mod/`. TeKatz can create the corresponding crafting recipes in Recipe Editor; the actual generated files are then imported into `pack/config/forever_recipe_editor/generated_pack/`.
