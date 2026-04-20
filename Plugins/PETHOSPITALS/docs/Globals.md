# PETHOSPITALS — Global Variable & String Map

Game: **Let's Play Pet Hospitals** (Nintendo DS, game code C2HP)
Source: Ghidra export — `GhidraDumps/globals_full.json` (1860 entries) + `GhidraDumps/all_strings_deep.json`
ARM9 load base: `0x02000000`

---

## Summary

| Metric | Count |
|--------|-------|
| Total mapped globals | 1860 |
| String globals | 1747 |
| Dword globals | 113 |
| Active (read or written at runtime) | 246 |
| Written by functions (state variables) | 25 |

---

## Species Table

Two representations exist side-by-side: a **mixed-case internal name** used by the texture/model
loader (case-sensitive), and an **uppercase enum constant** used by the request/match system.

| Index | Internal name (texture loader) | VA | Enum constant | VA |
|-------|-------------------------------|-----|---------------|-----|
| 0 | `dog` | 0x0206d390 | `DOG` | 0x02070d20 |
| 1 | `cat` | 0x0206d394 | `CAT` | 0x02070d24 |
| 2 | `rabbit` | 0x0206d62c | `RABBIT` | 0x02070d28 |
| 3 | `guineaPig` | 0x0206da78 | `GUINEA_PIG` | 0x02070d30 |
| 4 | `ferret` | 0x0206d5fc | `FERRET` | 0x02070d3c |
| 5 | `horse` | 0x0206b738 | `HORSE` | 0x02070d44 |

> **Note**: The `guineaPig` string must have a capital `P` — Patch 2 fixes the original `guineapig`
> (lowercase p) in the species table so the texture loader can find the correct asset.

---

## Breed Tables

### Dog breeds (6 total)

| Index | Enum constant | VA | Display key |
|-------|---------------|----|-------------|
| 0 | `LABRADOR` | 0x02070db8 | `CL_LABRADOR_DOG` (0x02070eb0) |
| 1 | `HUSKY` | 0x02070dc4 | `CL_HUSKY_DOG` (0x02070ec0) |
| 2 | `DALMATIAN` | 0x02070dcc | `CL_DALMATIAN_DOG` (0x02070ed0) |
| 3 | `SCHNAUZER` | 0x02070dd8 | `CL_SCHNAUZER_DOG` (0x02070ee4) |
| 4 | `DACHSHUND` | — | `CL_DACHSHUND_DOG` (0x02070ef8) |
| 5 | `BEAGLE` | 0x02070df0 | `CL_BEAGLE_DOG` (0x02070f0c) |
| ANY | — | — | `CL_ANY_DOG` (0x020710a0) |

Mixed-case runtime names: `Labrador` (0x0206d9dc), `Husky` (0x0206d48c), `Dalmatian` (0x0206db98),
`Schnauzer` (0x0206da90), `Beagle` (0x0206d814)

### Cat breeds (4 total)

| Index | Enum constant | VA | Display key |
|-------|---------------|----|-------------|
| 0 | `SIAMESE` | 0x02070df8 | `CL_SIAMESE_CAT` (0x02070f1c) |
| 1 | `ABYSSINIAN` | 0x02070e00 | `CL_ABYSSINIAN_CAT` (0x02070f2c) |
| 2 | `EXOTIC` | 0x02070e0c | `CL_EXOTIC_CAT` (0x02070f40) |
| 3 | `SHORTHAIR` | 0x02070e14 | `CL_SHORTHAIR_CAT` (0x02070f50) |
| ANY | — | — | `CL_ANY_CAT` (0x020710ac) |

Runtime names: `Siamese` (0x0206d90c), `Abyssinian` (0x0206de74), `catExotic` (0x0206db5c),
`catShortHair` (0x0206e1f4)

### Rabbit breeds (4 total)

| Index | Enum constant | VA | Display key |
|-------|---------------|----|-------------|
| 0 | `HARLEQUIN` | 0x02070e20 | `CL_HARLEQUIN_RABBIT` (0x02070f64) |
| 1 | `DUTCH` | 0x02070e2c | `CL_DUTCH_RABBIT` (0x02070f78) |
| 2 | `REX` | 0x02070e34 | `CL_REX_RABBIT` (0x02070f88) |
| 3 | `ENGLISH_SPOTS` | — | `CL_ENGLISH_SPOTS_RABBIT` (0x02070f98) |
| ANY | — | — | `CL_ANY_RABBIT` (0x020710b8) |

Runtime names: `Harlequin` (0x0206daa8), `Dutch` (0x0206d4ec), `Rex` (0x0206d38c),
`EnglishSpots` (0x0206e1e4)

### Guinea pig breeds (4 total)

| Index | Enum constant | VA | Display key |
|-------|---------------|----|-------------|
| 0 | `AGOUTI` | 0x02070e48 | `CL_AGOUTI_GUINEAPIG` (0x02070fb0) |
| 1 | `DUTCH` | — | `CL_DUTCH_GUINEAPIG` (0x02070fc4) |
| 2 | `HIMALAYAN` | 0x02070e50 | `CL_HIMALAYAN_GUINEAPIG` (0x02070fd8) |
| 3 | `SELF` | 0x02070e5c | `CL_SELF_GUINEAPIG` (0x02070ff0) |
| ANY | — | — | `CL_ANY_GUINEAPIG` (0x020710c8) |

Runtime names: `Agouti` (0x0206d65c), `Himalayan` (0x0206dab4)

### Ferret breeds (4 total)

| Index | Enum constant | VA | Display key |
|-------|---------------|----|-------------|
| 0 | `STANDARD` | 0x02070e64 | `CL_STANDARD_FERRET` (0x02071004) |
| 1 | `ROAN` | 0x02070e70 | `CL_ROAN_FERRET` (0x02071018) |
| 2 | `SIAMESE` | — | `CL_SIAMESE_FERRET` (0x02071028) |
| 3 | `SOLID` | 0x02070e78 | `CL_SOLID_FERRET` (0x0207103c) |
| ANY | — | — | `CL_ANY_FERRET` (0x020710dc) |

Runtime names: `Standard` (no dedicated lowercase entry), `Roan` (0x0206d3a4),
`Solid` (0x0206d44c)

### Horse breeds (4 total)

| Index | Enum constant | VA | Display key |
|-------|---------------|----|-------------|
| 0 | `TRAKEHNER` | 0x02070e80 | `CL_TRAKEHNER_HORSE` (0x0207104c) |
| 1 | `THROUGHBRED` | 0x02070e8c | `CL_THROUGHBRED_HORSE` (0x02071060) |
| 2 | `HANOVERIAN` | 0x02070e98 | `CL_HANOVERIAN_HORSE` (0x02071078) |
| 3 | `ANDALUSIAN` | 0x02070ea4 | `CL_ANDALUSIAN_HORSE` (0x0207108c) |
| ANY | — | — | `CL_ANY_HORSE` (0x020710ec) |

Runtime names: `Trakehner` (0x0206dac0), `Throughbred` (0x0206e098), `Hanoverian` (0x0206de8c),
`Andalusian` — not found as separate lowercase string

---

## Color / Coat Table

Two layers: **enum constants** (uppercase, used for matching) and **runtime names** (mixed-case,
used by texture loader).

### Enum constants (0x02071104 region)

| Constant | VA |
|----------|----|
| `CHOCOLATE` | 0x02071104 |
| `BLACK` | 0x02071110 |
| `GREY` | 0x02071120 |
| `WHITE` | 0x02071128 |
| `CREAM` | 0x02071144 |
| `TRICOLOR` | 0x0207114c |
| `CHOCOLATEPOINT` | 0x02071158 |
| `FAWN` | 0x02071188 |
| `BROWN` | 0x02071190 |
| `DARKGREY` | 0x020711d4 |

### Suffixed coat keys (horse / extended palette, 0x02071204 region)

| Constant | VA |
|----------|----|
| `YELLOW_COAT` | 0x02071204 |
| `CHOCOLATE_COAT` | 0x02071210 |
| `BLACK_COAT` | 0x02071220 |
| `COPPER_COAT` | 0x0207122c |
| `GRAY_COAT` | 0x02071238 |
| `WHITE_COAT` | 0x02071244 |
| `BLUE_COAT` | 0x02071250 |
| `LEMON_COAT` | 0x0207125c |
| `STANDARD_COAT` | 0x02071268 |

### Runtime names (mixed-case, texture lookup)

`Fawn` (0x0206d39c), `Grey` (0x0206d3ac), `Cream` (0x0206d3c4), `White` (0x0206d3dc),
`Brown` (0x0206d3f4), `Black` (0x0206d41c), `DarkGrey` (0x0206d970), `Tricolor` (0x0206d9c4),
`Chocolate` (0x0206dacc), `ChocolatePoint` (0x0206e6b4)

---

## ANY Sentinel Constants

`-1` (0xFF unsigned byte) is the in-memory sentinel. These string constants are used for
matching and display.

| Constant | VA | Usage |
|----------|----|-------|
| `ANY_BREED` | 0x0206a050 | Generic breed sentinel (request matching) |
| `ANY_COAT` | 0x0206b374 | Generic coat/color sentinel |
| `ANY_DOG` | 0x020710a3 | Display: any dog accepted |
| `ANY_CAT` | 0x020710af | Display: any cat accepted |
| `ANY_RABBIT` | 0x020710bb | Display: any rabbit accepted |
| `ANY_GUINEAPIG` | 0x020710cb | Display: any guinea pig accepted |
| `ANY_FERRET` | 0x020710df | Display: any ferret accepted |
| `ANY_HORSE` | 0x020710ef | Display: any horse accepted |

**Patch 3** increases the probability that breed and color are set to ANY when a new request is
generated, by replacing the dynamic range calculation with a fixed value (default: 2 → 50%).

---

## Request Type Constants

| Constant | VA | Notes |
|----------|----|-------|
| `LOST_PET_REQUEST` | 0x0206a010 | Owner lost pet, needs to be found |
| `ADOPTION_REQUEST` | 0x0206a024 | Pet needs a home |
| `REQUEST_DELETE_ASK` | 0x0206a68c | Confirmation before removing a request |
| `MSG_REQUEST_ADDED` | 0x0206a6dc | Notification when request added |
| `REQUEST_HINT` | 0x0206a7fc | In-game hint about requests |
| `(LOST)` | 0x0206a738 | Label tag for lost pet |
| `(ADOPTION)` | 0x0206a740 | Label tag for adoption |

---

## UI / HUD Constants

| Constant | VA | Notes |
|----------|----|-------|
| `healthBar` | 0x02069124 | Health bar GUI element ID |
| `PET_INFO` | 0x02069188 | Pet info panel ID |
| `PET_INFO_EMPTY` | 0x020691d8 | Empty pet slot display |
| `MONEY_FORMAT` | 0x02069404 | Format string for money display |
| `txtMoney` | 0x02069414 | Money text element ID |
| `txtRoom` | 0x02069440 | Room text element ID |
| `LEVEL` | 0x0206baa0 | Level display string |
| `MSG_NO_MONEY` | 0x020693a8 | "Not enough money" message key |

### Species filter buttons (0x0206ae70 region)
`btnDog`, `btnCat`, `btnRabbit`, `btnGuineaPig`, `btnFerret`, `btnHorse`

### Color selection buttons
`btnColor0`–`btnColor3` (0x0206af40 region), `btnColor%i` (0x0206b060, format string)

---

## Gameplay Action Constants (GAMEPLAY_*)

Used by the treatment/interaction system to identify touch gestures.

| Constant | VA |
|----------|----|
| `GAMEPLAY_SCRATCH` | 0x02068e78 |
| `GAMEPLAY_DRAGCIRCULAR` | 0x02068e8c |
| `GAMEPLAY_CLICK` | 0x02068ea4 |
| `GAMEPLAY_CLICKHOLD` | 0x02068eb4 |
| `GAMEPLAY_DRAGRIGHT` | 0x02068ec8 |

---

## Game Mode Strings

| String | VA | Notes |
|--------|----|-------|
| `8GameMode` | 0x0206891c | C++ RTTI-style name for base game mode class |
| `9CondoMode` | 0x0206effd | Condo/home screen mode |
| `11CreditsMode` | 0x02069214 | Credits screen |
| `14DecorationMode` | 0x020692ac | Room decoration mode |
| `9DressMode` | 0x02069460 | Pet dressing/clothing shop |
| `8FoodMode` | 0x02069674 | Food shop/feeding mode |
| `13FurnitureMode` | 0x020697f8 | Furniture placement mode |
| `11SceneObject` | 0x02068240 | Base scene object class name |
| `10GuiElement` | 0x02068f80 | Base GUI element class name |

---

## GUI Resource Paths

| Path | VA | Notes |
|------|----|-------|
| `gui/titleTop.ini` | 0x02068808 | Title screen top layout |
| `gui/condoTop.ini` | 0x02069110 | Condo mode top layout |
| `gui/error.ini` | 0x020687f4 | Error screen layout |
| `gui/creditsBot.ini` | 0x02069254 | Credits bottom layout |
| `gui/decorationTop.ini` | 0x020692f4 | Decoration mode top |
| `gui/decorationBot.ini` | 0x0206930c | Decoration mode bottom |
| `gui/shopDressBot.ini` | 0x0206964c | Dress shop bottom |
| `gui/foodTop.ini` | 0x020696a8 | Food mode top |
| `gui/foodBot.ini` | 0x020696b8 | Food mode bottom |
| `gui/shopFoodBot.ini` | 0x02069740 | Food shop bottom |
| `gui/furnitureMenu.ini` | 0x02069954 | Furniture menu layout |
| `gui/furnitureHud.ini` | 0x0206996c | Furniture HUD layout |
| `model/%s.col` | 0x02068a08 | Model colour file format |
| `model/%s.trg` | 0x02068a18 | Model trigger file format |
| `model/%s.ndl` | 0x02068a28 | Model NDL file format |
| `music/menu.raw` | 0x020688e0 | Menu music |
| `music/treatmentComplete.raw` | 0x02068de4 | Treatment complete jingle |

---

## Save System Constants

| Constant | VA |
|----------|----|
| `MSG_SAVE_CORRUPT` | 0x0206881c |
| `SAVE_OK` | 0x02069b54 |
| `SAVE_CONFIRM` | 0x02069b5c |
| `SAVE_DELETE_CONFIRM` | 0x0206a908 |
| `SAVE_DETAILS_PANEL1` | 0x0206a93c |
| `SAVE_DETAILS_PANEL2` | 0x0206a960 |
| `SAVE_ERASE_OK` | 0x0206ab0c |

---

## Active Runtime Variables (read or written, 246 total)

Most-accessed globals are small numeric dwords (float constants and counters). The highest-traffic
string globals are coat/color lookup strings, consistent with the texture system reading them on
every animal load.

### Top accessed (read + write combined)

| VA | Type | r | w | Value (hex) | Notes |
|----|------|---|---|-------------|-------|
| 0x020725b8 | dword | 3 | 0 | 7FFFFFFF | Float: +Inf / NaN mask |
| 0x020725b4 | dword | 2 | 0 | 7F800000 | Float: +Infinity |
| 0x020724cc | dword | 2 | 0 | 027FFE00 | Pointer-range constant |
| 0x020727ea | dword | 2 | 0 | 0000464E | ASCII 'FN' — filename fragment |
| 0x020724a8 | dword | 0 | 2 | 00000001 | Written by FUN_02045e40, FUN_02045e04 |
| 0x020720b8 | string | 1 | 0 | GRAY_COAT | Coat lookup: grey |
| 0x02071210 | string | 1 | 0 | CHOCOLATE_COAT | Coat lookup: chocolate |

### State variables (written by functions)

All 25 written dword globals are written by two functions:

| Writer function | VA | Role (from Architecture_Map) |
|-----------------|-----|------------------------------|
| `FUN_0205d0b8` | 0x0205d0b8 | Largest function (18812 bytes) — likely main init/state table |
| `FUN_0205ce88` | 0x0205ce88 | Related init (writes 2 globals) |
| `FUN_02045e40` | 0x02045e40 | Writes global 0x020724a8 |
| `FUN_02045e04` | 0x02045e04 | Writes global 0x020724a8 |
| `FUN_020476f4` | 0x020476f4 | Writes 0x020724c0 = FFFFFFFF |
| `FUN_02001700` | 0x02001700 | Writes 0x02072a59 = 10E59FE0 (CRT0-area) |

The cluster at `0x0206df94`–`0x0206e010` (18 consecutive dwords all written by `FUN_0205d0b8`)
is likely a large initialization table for scene or entity data.

---

## Notes on Partial String Artifacts

Many entries in `globals_full.json` and `all_strings_deep.json` represent addresses pointing
_into_ the middle of a string (e.g., `NY_BREED` = address of `ANY_BREED` + 1). These are
artifacts of Ghidra's auto-analysis treating every null-terminated substring as a separate
global. The canonical address is always the one pointing to the **first byte** of the full
string.
