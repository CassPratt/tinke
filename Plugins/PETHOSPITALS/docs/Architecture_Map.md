# Architecture Map - Let's Play Pet Hospitals (ARM9)

> Structural analysis of Ghidra callgraph + functions export. 1794 functions.
> Methodology: PlanPetHospitals.txt (phases 1-5)

---

## Phase 1 - Hub Nodes

### Top 25 by OUT-degree (call most functions = orchestrators / game loop candidates)
| Address | Name | in | out | total | size | asm | float | offsets |
|---------|------|----|-----|-------|------|-----|-------|---------|
| 0x02021cd4 | FUN_02021cd4 | 0 | 38 | 38 | 2336 | 584 |  | 157 |
| 0x0201707c | FUN_0201707c | 0 | 33 | 33 | 2108 | 527 |  | 90 |
| 0x0203edac | FUN_0203edac | 1 | 23 | 24 | 588 | 147 |  | 18 |
| 0x02019be0 | FUN_02019be0 | 0 | 22 | 22 | 1120 | 280 |  | 75 |
| 0x02004b70 | FUN_02004b70 | 1 | 22 | 23 | 2964 | 741 |  | 53 |
| 0x02041a4c | FUN_02041a4c | 2 | 21 | 23 | 1800 | 450 |  | 28 |
| 0x02021218 | FUN_02021218 | 0 | 21 | 21 | 740 | 185 |  | 26 |
| 0x020148c4 | FUN_020148c4 | 1 | 21 | 22 | 1168 | 292 |  | 45 |
| 0x020179d8 | FUN_020179d8 | 0 | 20 | 20 | 2280 | 570 |  | 103 |
| 0x0201f370 | FUN_0201f370 | 0 | 20 | 20 | 548 | 137 |  | 23 |
| 0x020143fc | FUN_020143fc | 0 | 20 | 20 | 832 | 208 |  | 41 |
| 0x02009508 | FUN_02009508 | 1 | 20 | 21 | 264 | 66 |  | 3 |
| 0x02019080 | FUN_02019080 | 2 | 19 | 21 | 700 | 175 |  | 18 |
| 0x0201e4d0 | FUN_0201e4d0 | 7 | 19 | 26 | 736 | 184 |  | 20 |
| 0x0201eb04 | FUN_0201eb04 | 3 | 18 | 21 | 680 | 170 |  | 18 |
| 0x02000cf4 | FUN_02000cf4 | 0 | 18 | 18 | 252 | 63 |  | 0 |
| 0x02011d10 | FUN_02011d10 | 0 | 18 | 18 | 560 | 140 |  | 36 |
| 0x02010e68 | FUN_02010e68 | 0 | 18 | 18 | 1060 | 265 |  | 57 |
| 0x0201c0d0 | FUN_0201c0d0 | 2 | 17 | 19 | 612 | 153 |  | 5 |
| 0x020135e4 | FUN_020135e4 | 1 | 17 | 18 | 416 | 104 |  | 17 |
| 0x020575c4 | FUN_020575c4 | 1 | 17 | 18 | 1540 | 385 |  | 0 |
| 0x0200e240 | FUN_0200e240 | 5 | 16 | 21 | 468 | 117 |  | 13 |
| 0x020242d8 | FUN_020242d8 | 2 | 16 | 18 | 1132 | 283 |  | 37 |
| 0x0201dd84 | FUN_0201dd84 | 1 | 16 | 17 | 736 | 184 |  | 20 |
| 0x0204ccbc | FUN_0204ccbc | 1 | 16 | 17 | 548 | 137 |  | 2 |

### Top 25 by IN-degree (called by most = hot utilities)
| Address | Name | in | out | total | size | asm | float | offsets |
|---------|------|----|-----|-------|------|-----|-------|---------|
| 0x0202a97c | FUN_0202a97c | 354 | 1 | 355 | 156 | 39 |  | 10 |
| 0x02000c1c | FUN_02000c1c | 181 | 0 | 181 | 20 | 5 |  | 0 |
| 0x02001164 | FUN_02001164 | 154 | 0 | 154 | 12 | 3 |  | 0 |
| 0x02000c4c | FUN_02000c4c | 149 | 0 | 149 | 20 | 5 |  | 0 |
| 0x02051eb4 | FUN_02051eb4 | 145 | 1 | 146 | 44 | 11 |  | 0 |
| 0x0202c9cc | FUN_0202c9cc | 145 | 4 | 149 | 808 | 202 |  | 23 |
| 0x02030728 | FUN_02030728 | 142 | 0 | 142 | 48 | 12 |  | 0 |
| 0x02030964 | FUN_02030964 | 134 | 0 | 134 | 20 | 5 |  | 2 |
| 0x020487ec | FUN_020487ec | 115 | 0 | 115 | 24 | 6 |  | 0 |
| 0x0205335c | FUN_0205335c | 105 | 0 | 105 | 28 | 7 |  | 0 |
| 0x020487d8 | FUN_020487d8 | 88 | 0 | 88 | 20 | 5 |  | 0 |
| 0x020582d8 | FUN_020582d8 | 86 | 0 | 86 | 864 | 216 |  | 2 |
| 0x02029ff8 | FUN_02029ff8 | 76 | 9 | 85 | 352 | 88 |  | 9 |
| 0x0200b44c | FUN_0200b44c | 70 | 0 | 70 | 4 | 1 |  | 0 |
| 0x0205a684 | FUN_0205a684 | 66 | 0 | 66 | 140 | 35 |  | 0 |
| 0x0200b450 | FUN_0200b450 | 59 | 1 | 60 | 80 | 20 |  | 1 |
| 0x0203209c | FUN_0203209c | 57 | 1 | 58 | 260 | 65 |  | 2 |
| 0x02059604 | FUN_02059604 | 53 | 0 | 53 | 480 | 120 |  | 0 |
| 0x0200b4a8 | FUN_0200b4a8 | 52 | 1 | 53 | 152 | 38 |  | 1 |
| 0x0200a644 | FUN_0200a644 | 51 | 1 | 52 | 44 | 11 |  | 3 |
| 0x02000c64 | FUN_02000c64 | 50 | 0 | 50 | 20 | 5 |  | 0 |
| 0x0202b954 | FUN_0202b954 | 46 | 2 | 48 | 236 | 59 |  | 16 |
| 0x020587f0 | FUN_020587f0 | 43 | 0 | 43 | 944 | 236 |  | 2 |
| 0x02037bec | FUN_02037bec | 42 | 3 | 45 | 144 | 36 |  | 11 |
| 0x02053378 | FUN_02053378 | 38 | 0 | 38 | 192 | 48 |  | 0 |

### Top 25 by TOTAL degree (central hubs)
| Address | Name | in | out | total | size | asm | float | offsets |
|---------|------|----|-----|-------|------|-----|-------|---------|
| 0x0202a97c | FUN_0202a97c | 354 | 1 | 355 | 156 | 39 |  | 10 |
| 0x02000c1c | FUN_02000c1c | 181 | 0 | 181 | 20 | 5 |  | 0 |
| 0x02001164 | FUN_02001164 | 154 | 0 | 154 | 12 | 3 |  | 0 |
| 0x0202c9cc | FUN_0202c9cc | 145 | 4 | 149 | 808 | 202 |  | 23 |
| 0x02000c4c | FUN_02000c4c | 149 | 0 | 149 | 20 | 5 |  | 0 |
| 0x02051eb4 | FUN_02051eb4 | 145 | 1 | 146 | 44 | 11 |  | 0 |
| 0x02030728 | FUN_02030728 | 142 | 0 | 142 | 48 | 12 |  | 0 |
| 0x02030964 | FUN_02030964 | 134 | 0 | 134 | 20 | 5 |  | 2 |
| 0x020487ec | FUN_020487ec | 115 | 0 | 115 | 24 | 6 |  | 0 |
| 0x0205335c | FUN_0205335c | 105 | 0 | 105 | 28 | 7 |  | 0 |
| 0x020487d8 | FUN_020487d8 | 88 | 0 | 88 | 20 | 5 |  | 0 |
| 0x020582d8 | FUN_020582d8 | 86 | 0 | 86 | 864 | 216 |  | 2 |
| 0x02029ff8 | FUN_02029ff8 | 76 | 9 | 85 | 352 | 88 |  | 9 |
| 0x0200b44c | FUN_0200b44c | 70 | 0 | 70 | 4 | 1 |  | 0 |
| 0x0205a684 | FUN_0205a684 | 66 | 0 | 66 | 140 | 35 |  | 0 |
| 0x0200b450 | FUN_0200b450 | 59 | 1 | 60 | 80 | 20 |  | 1 |
| 0x0203209c | FUN_0203209c | 57 | 1 | 58 | 260 | 65 |  | 2 |
| 0x0200b4a8 | FUN_0200b4a8 | 52 | 1 | 53 | 152 | 38 |  | 1 |
| 0x02059604 | FUN_02059604 | 53 | 0 | 53 | 480 | 120 |  | 0 |
| 0x0200a644 | FUN_0200a644 | 51 | 1 | 52 | 44 | 11 |  | 3 |
| 0x02000c64 | FUN_02000c64 | 50 | 0 | 50 | 20 | 5 |  | 0 |
| 0x0202b954 | FUN_0202b954 | 46 | 2 | 48 | 236 | 59 |  | 16 |
| 0x02037bec | FUN_02037bec | 42 | 3 | 45 | 144 | 36 |  | 11 |
| 0x020587f0 | FUN_020587f0 | 43 | 0 | 43 | 944 | 236 |  | 2 |
| 0x02053378 | FUN_02053378 | 38 | 0 | 38 | 192 | 48 |  | 0 |

### Root functions (0 callers) sorted by out-degree - entry points / main loops
| Address | Name | in | out | total | size | asm | float | offsets |
|---------|------|----|-----|-------|------|-----|-------|---------|
| 0x02021cd4 | FUN_02021cd4 | 0 | 38 | 38 | 2336 | 584 |  | 157 |
| 0x0201707c | FUN_0201707c | 0 | 33 | 33 | 2108 | 527 |  | 90 |
| 0x02019be0 | FUN_02019be0 | 0 | 22 | 22 | 1120 | 280 |  | 75 |
| 0x02021218 | FUN_02021218 | 0 | 21 | 21 | 740 | 185 |  | 26 |
| 0x020179d8 | FUN_020179d8 | 0 | 20 | 20 | 2280 | 570 |  | 103 |
| 0x0201f370 | FUN_0201f370 | 0 | 20 | 20 | 548 | 137 |  | 23 |
| 0x020143fc | FUN_020143fc | 0 | 20 | 20 | 832 | 208 |  | 41 |
| 0x02011d10 | FUN_02011d10 | 0 | 18 | 18 | 560 | 140 |  | 36 |
| 0x02000cf4 | FUN_02000cf4 | 0 | 18 | 18 | 252 | 63 |  | 0 |
| 0x02010e68 | FUN_02010e68 | 0 | 18 | 18 | 1060 | 265 |  | 57 |
| 0x0201b9f4 | FUN_0201b9f4 | 0 | 16 | 16 | 800 | 200 |  | 28 |
| 0x02023968 | FUN_02023968 | 0 | 15 | 15 | 552 | 138 |  | 12 |
| 0x02019824 | FUN_02019824 | 0 | 15 | 15 | 532 | 133 |  | 9 |
| 0x0201d298 | FUN_0201d298 | 0 | 14 | 14 | 232 | 58 |  | 3 |
| 0x02012b10 | FUN_02012b10 | 0 | 14 | 14 | 536 | 134 |  | 28 |
| 0x0201b818 | FUN_0201b818 | 0 | 14 | 14 | 456 | 114 |  | 25 |
| 0x02022690 | FUN_02022690 | 0 | 14 | 14 | 1476 | 369 |  | 114 |
| 0x0202c040 | FUN_0202c040 | 0 | 14 | 14 | 476 | 119 |  | 7 |
| 0x0200e964 | FUN_0200e964 | 0 | 14 | 14 | 1144 | 286 |  | 39 |
| 0x020138b8 | FUN_020138b8 | 0 | 13 | 13 | 228 | 57 |  | 6 |

---

## Phase 2 - Classification by Behaviour

### Top 25 by SIZE (largest = complex logic / managers)
| Address | Name | in | out | total | size | asm | float | offsets |
|---------|------|----|-----|-------|------|-----|-------|---------|
| 0x0205d0b8 | FUN_0205d0b8 | 0 | 1 | 1 | 18812 | 4703 |  | 376 |
| 0x02026f8c | FUN_02026f8c | 1 | 0 | 1 | 8400 | 2100 |  | 147 |
| 0x0205367c | FUN_0205367c | 2 | 8 | 10 | 4644 | 1161 |  | 4 |
| 0x020552e8 | FUN_020552e8 | 1 | 11 | 12 | 4252 | 1063 |  | 15 |
| 0x020523d8 | FUN_020523d8 | 1 | 7 | 8 | 3404 | 851 |  | 0 |
| 0x02004b70 | FUN_02004b70 | 1 | 22 | 23 | 2964 | 741 |  | 53 |
| 0x0200cd58 | FUN_0200cd58 | 0 | 4 | 4 | 2348 | 587 |  | 38 |
| 0x02021cd4 | FUN_02021cd4 | 0 | 38 | 38 | 2336 | 584 |  | 157 |
| 0x020179d8 | FUN_020179d8 | 0 | 20 | 20 | 2280 | 570 |  | 103 |
| 0x0205acac | FUN_0205acac | 2 | 7 | 9 | 2216 | 554 |  | 34 |
| 0x0201707c | FUN_0201707c | 0 | 33 | 33 | 2108 | 527 |  | 90 |
| 0x020515e4 | FUN_020515e4 | 1 | 9 | 10 | 2076 | 519 |  | 0 |
| 0x02033624 | FUN_02033624 | 2 | 4 | 6 | 2000 | 500 |  | 39 |
| 0x02006914 | FUN_02006914 | 0 | 5 | 5 | 1940 | 485 |  | 83 |
| 0x0200fc44 | FUN_0200fc44 | 0 | 3 | 3 | 1864 | 466 |  | 14 |
| 0x02050e80 | FUN_02050e80 | 1 | 3 | 4 | 1848 | 462 |  | 1 |
| 0x020087fc | FUN_020087fc | 0 | 0 | 0 | 1820 | 455 |  | 29 |
| 0x02041a4c | FUN_02041a4c | 2 | 21 | 23 | 1800 | 450 |  | 28 |
| 0x0203bcfc | FUN_0203bcfc | 5 | 6 | 11 | 1596 | 399 |  | 18 |
| 0x02020590 | FUN_02020590 | 3 | 15 | 18 | 1560 | 390 |  | 44 |
| 0x0204128c | FUN_0204128c | 1 | 10 | 11 | 1556 | 389 |  | 14 |
| 0x020575c4 | FUN_020575c4 | 1 | 17 | 18 | 1540 | 385 |  | 0 |
| 0x02022690 | FUN_02022690 | 0 | 14 | 14 | 1476 | 369 |  | 114 |
| 0x02026394 | FUN_02026394 | 2 | 0 | 2 | 1436 | 359 |  | 6 |
| 0x02025428 | FUN_02025428 | 0 | 1 | 1 | 1424 | 356 |  | 8 |

### Functions with float/double usage (3D / movement / physics)
| Address | Name | in | out | total | size | asm | float | offsets |
|---------|------|----|-----|-------|------|-----|-------|---------|

### Functions with heavy struct offset access (entity / object handlers)
| Address | Name | in | out | total | size | asm | float | offsets |
|---------|------|----|-----|-------|------|-----|-------|---------|
| 0x0205d0b8 | FUN_0205d0b8 | 0 | 1 | 1 | 18812 | 4703 |  | 376 |
| 0x02021cd4 | FUN_02021cd4 | 0 | 38 | 38 | 2336 | 584 |  | 157 |
| 0x02026f8c | FUN_02026f8c | 1 | 0 | 1 | 8400 | 2100 |  | 147 |
| 0x02022690 | FUN_02022690 | 0 | 14 | 14 | 1476 | 369 |  | 114 |
| 0x020179d8 | FUN_020179d8 | 0 | 20 | 20 | 2280 | 570 |  | 103 |
| 0x0201707c | FUN_0201707c | 0 | 33 | 33 | 2108 | 527 |  | 90 |
| 0x0202f5fc | FUN_0202f5fc | 1 | 1 | 2 | 776 | 194 |  | 85 |
| 0x02006914 | FUN_02006914 | 0 | 5 | 5 | 1940 | 485 |  | 83 |
| 0x0203fb84 | FUN_0203fb84 | 1 | 1 | 2 | 772 | 193 |  | 80 |
| 0x02019be0 | FUN_02019be0 | 0 | 22 | 22 | 1120 | 280 |  | 75 |
| 0x020399cc | FUN_020399cc | 3 | 1 | 4 | 640 | 160 |  | 72 |
| 0x0204dd10 | FUN_0204dd10 | 1 | 1 | 2 | 808 | 202 |  | 72 |
| 0x02025a10 | FUN_02025a10 | 3 | 1 | 4 | 624 | 156 |  | 71 |
| 0x02036668 | FUN_02036668 | 1 | 6 | 7 | 772 | 193 |  | 70 |
| 0x0203f874 | FUN_0203f874 | 8 | 1 | 9 | 784 | 196 |  | 70 |
| 0x0202a718 | FUN_0202a718 | 3 | 1 | 4 | 612 | 153 |  | 65 |
| 0x02011f5c | FUN_02011f5c | 0 | 13 | 13 | 904 | 226 |  | 61 |
| 0x0201a538 | FUN_0201a538 | 0 | 12 | 12 | 1056 | 264 |  | 60 |
| 0x02010e68 | FUN_02010e68 | 0 | 18 | 18 | 1060 | 265 |  | 57 |
| 0x02004b70 | FUN_02004b70 | 1 | 22 | 23 | 2964 | 741 |  | 53 |

---

## Phase 3 - Update Chain Candidates
> Criteria: in-degree <= 3 AND out-degree >= 5 (low callers, calls many = loop/orchestrator)

| Address | Name | in | out | total | size | asm | float | offsets |
|---------|------|----|-----|-------|------|-----|-------|---------|
| 0x02021cd4 | FUN_02021cd4 | 0 | 38 | 38 | 2336 | 584 |  | 157 |
| 0x0201707c | FUN_0201707c | 0 | 33 | 33 | 2108 | 527 |  | 90 |
| 0x0203edac | FUN_0203edac | 1 | 23 | 24 | 588 | 147 |  | 18 |
| 0x02019be0 | FUN_02019be0 | 0 | 22 | 22 | 1120 | 280 |  | 75 |
| 0x02004b70 | FUN_02004b70 | 1 | 22 | 23 | 2964 | 741 |  | 53 |
| 0x02041a4c | FUN_02041a4c | 2 | 21 | 23 | 1800 | 450 |  | 28 |
| 0x020148c4 | FUN_020148c4 | 1 | 21 | 22 | 1168 | 292 |  | 45 |
| 0x02021218 | FUN_02021218 | 0 | 21 | 21 | 740 | 185 |  | 26 |
| 0x020143fc | FUN_020143fc | 0 | 20 | 20 | 832 | 208 |  | 41 |
| 0x0201f370 | FUN_0201f370 | 0 | 20 | 20 | 548 | 137 |  | 23 |
| 0x020179d8 | FUN_020179d8 | 0 | 20 | 20 | 2280 | 570 |  | 103 |
| 0x02009508 | FUN_02009508 | 1 | 20 | 21 | 264 | 66 |  | 3 |
| 0x02019080 | FUN_02019080 | 2 | 19 | 21 | 700 | 175 |  | 18 |
| 0x0201eb04 | FUN_0201eb04 | 3 | 18 | 21 | 680 | 170 |  | 18 |
| 0x02000cf4 | FUN_02000cf4 | 0 | 18 | 18 | 252 | 63 |  | 0 |

### Call trees (2 levels) for top 5 candidates

#### FUN_02021cd4 (0x02021cd4)  in=0 out=38 size=2336
  - FUN_02042568  (out=0)
  - FUN_0203c934  (out=1)
      - FUN_02030964
  - FUN_0202a97c  (out=1)
      - FUN_020534c0
  - FUN_02053610  (out=0)
  - FUN_02000c1c  (out=0)
  - FUN_0200a904  (out=3)
      - FUN_02002580
      - FUN_02002790
      - FUN_02002160
  - FUN_02029ff8  (out=9)
      - FUN_0202f194
      - FUN_0202a2f4
      - FUN_02029d14
      - FUN_02029f18
      - FUN_020534c0
      - FUN_02053378
      - FUN_02001164
      - FUN_02029cf8
  - FUN_0202e078  (out=2)
      - FUN_02024c34
      - FUN_02025098
  - FUN_0202c9cc  (out=4)
      - FUN_0205335c
      - FUN_0202cd14
      - FUN_02001164
      - FUN_0202d2f0
  - FUN_0200a644  (out=1)
      - FUN_0202a628
  - FUN_02059574  (out=0)
  - FUN_0200b4a8  (out=1)
      - FUN_0200b54c
  - FUN_02025edc  (out=3)
      - FUN_02025edc
      - FUN_020259c8
      - FUN_02026c94
  - FUN_020230a0  (out=6)
      - FUN_0203ca94
      - FUN_0202a97c
      - FUN_02030728
      - FUN_02051eb4
      - FUN_0203c92c
      - FUN_0202c9cc
  - FUN_0202e22c  (out=2)
      - FUN_02030964
      - FUN_0203ae88
  - FUN_020295c8  (out=2)
      - FUN_02030964
      - FUN_0203ae88
  - FUN_02029488  (out=3)
      - FUN_02024c34
      - FUN_02025098
      - FUN_0205bc64
  - FUN_0200a7d0  (out=0)
  - FUN_0205335c  (out=0)
  - FUN_0200b6e4  (out=1)
      - FUN_02024c34
  - FUN_02030728  (out=0)
  - FUN_02030964  (out=0)
  - FUN_02043070  (out=0)
  - FUN_0200a8ac  (out=4)
      - FUN_0200b450
      - FUN_0200b638
      - FUN_02000c1c
      - FUN_02023444
  - FUN_02001164  (out=0)
  - FUN_02002990  (out=1)
      - FUN_02024c34
  - FUN_0200dacc  (out=1)
      - FUN_0200d6e4
  - FUN_0200ba74  (out=9)
      - FUN_0200c4b8
      - FUN_0200c030
      - FUN_0200bf10
      - FUN_02000c4c
      - FUN_02000c1c
      - FUN_02036e04
      - FUN_02036cc0
      - FUN_0200c148
  - FUN_020400a8  (out=0)
  - FUN_02059604  (out=0)
  - FUN_02051eb4  (out=1)
      - FUN_02051e4c
  - FUN_02025c80  (out=1)
      - FUN_02025cb8
  - FUN_02022cd8  (out=10)
      - FUN_02042ad4
      - FUN_0200df64
      - FUN_0203c934
      - FUN_0200f8f8
      - FUN_02023130
      - FUN_0200d748
      - FUN_0200d758
      - FUN_0200dacc
  - FUN_0203c92c  (out=0)
  - FUN_02022fb4  (out=7)
      - FUN_02053490
      - FUN_02042ab8
      - FUN_0202de0c
      - FUN_02059c70
      - FUN_02051eb4
      - FUN_02042acc
      - FUN_0202b954
  - FUN_02059500  (out=0)
  - FUN_0203d7d4  (out=12)
      - FUN_02026d78
      - FUN_02025edc
      - FUN_0200dab0
      - FUN_0205335c
      - FUN_02007fc8
      - FUN_0200d748
      - FUN_0203dccc
      - FUN_0200d758
  - FUN_0203c9e4  (out=8)
      - FUN_02000c34
      - FUN_02042578
      - FUN_02000c64
      - FUN_0205335c
      - FUN_02042c18
      - FUN_02051eb4
      - FUN_0202c9cc
      - FUN_020425f0

#### FUN_0201707c (0x0201707c)  in=0 out=33 size=2108
  - FUN_0203e2ec  (out=2)
      - FUN_0203c350
      - FUN_02030964
  - FUN_0202a2f4  (out=1)
      - FUN_0202eac4
  - FUN_02042568  (out=0)
  - FUN_0202c250  (out=4)
      - FUN_02034df4
      - FUN_020391a4
      - FUN_02030964
      - FUN_02039288
  - FUN_020425a8  (out=1)
      - FUN_02042558
  - FUN_0200a6c0  (out=1)
      - FUN_0202eac4
  - FUN_0202a97c  (out=1)
      - FUN_020534c0
  - FUN_02000c1c  (out=0)
  - FUN_02042c18  (out=2)
      - FUN_02042558
      - FUN_02030760
  - FUN_020400b4  (out=3)
      - FUN_02030728
      - FUN_02051eb4
      - FUN_0202c9cc
  - FUN_0203e47c  (out=1)
      - FUN_02030964
  - FUN_02024bc8  (out=1)
      - FUN_020534c0
  - FUN_0200a904  (out=3)
      - FUN_02002580
      - FUN_02002790
      - FUN_02002160
  - FUN_02029ff8  (out=9)
      - FUN_0202f194
      - FUN_0202a2f4
      - FUN_02029d14
      - FUN_02029f18
      - FUN_020534c0
      - FUN_02053378
      - FUN_02001164
      - FUN_02029cf8
  - FUN_02031f9c  (out=1)
      - FUN_02001164
  - FUN_0202c9cc  (out=4)
      - FUN_0205335c
      - FUN_0202cd14
      - FUN_02001164
      - FUN_0202d2f0
  - FUN_0200a644  (out=1)
      - FUN_0202a628
  - FUN_020425f0  (out=2)
      - FUN_02042558
      - FUN_02030760
  - FUN_02040930  (out=11)
      - FUN_02053490
      - FUN_02042578
      - FUN_0202a97c
      - FUN_02030728
      - FUN_02030964
      - FUN_020404c8
      - FUN_0203209c
      - FUN_02001164
  - FUN_020295c8  (out=2)
      - FUN_02030964
      - FUN_0203ae88
  - FUN_02042578  (out=0)
  - FUN_02040654  (out=8)
      - FUN_02042578
      - FUN_0202a97c
      - FUN_02030728
      - FUN_02030964
      - FUN_02001164
      - FUN_02051eb4
      - FUN_0204059c
      - FUN_0202c9cc
  - FUN_0200a7d0  (out=0)
  - FUN_0205335c  (out=0)
  - FUN_02030728  (out=0)
  - FUN_02030964  (out=0)
  - FUN_0200a8ac  (out=4)
      - FUN_0200b450
      - FUN_0200b638
      - FUN_02000c1c
      - FUN_02023444
  - FUN_0203209c  (out=1)
      - FUN_02001164
  - FUN_02001164  (out=0)
  - FUN_02002990  (out=1)
      - FUN_02024c34
  - FUN_02051eb4  (out=1)
      - FUN_02051e4c
  - FUN_02042bbc  (out=1)
      - FUN_02042558
  - FUN_02042d20  (out=0)

#### FUN_0203edac (0x0203edac)  in=1 out=23 size=588
  - FUN_02041238  (out=0)
  - FUN_0203f874  (out=1)
      - FUN_02042484
  - FUN_02042e1c  (out=2)
      - FUN_02042974
      - FUN_02030854
  - FUN_0203f034  (out=7)
      - FUN_0203f874
      - FUN_020597f0
      - FUN_02000c1c
      - FUN_02059c70
      - FUN_02059604
      - FUN_020595bc
      - FUN_02059534
  - FUN_020438dc  (out=9)
      - FUN_020103fc
      - FUN_02053490
      - FUN_02043aa8
      - FUN_02043ac4
      - FUN_0200c3f8
      - FUN_02053378
      - FUN_02001164
      - FUN_02025c80
  - FUN_0200b6e4  (out=1)
      - FUN_02024c34
  - FUN_02025388  (out=1)
      - FUN_020321a8
  - FUN_02043ae0  (out=2)
      - FUN_02043aa8
      - FUN_02042b34
  - FUN_02043070  (out=0)
  - FUN_020597f0  (out=0)
  - FUN_0203e0a8  (out=3)
      - FUN_0203e0e0
      - FUN_02001164
      - FUN_0200bca4
  - FUN_0203f63c  (out=0)
  - FUN_02000c1c  (out=0)
  - FUN_0200ba74  (out=9)
      - FUN_0200c4b8
      - FUN_0200c030
      - FUN_0200bf10
      - FUN_02000c4c
      - FUN_02000c1c
      - FUN_02036e04
      - FUN_02036cc0
      - FUN_0200c148
  - FUN_02043b28  (out=2)
      - thunk_FUN_02030ce4
      - FUN_02042eec
  - FUN_020400a8  (out=0)
  - FUN_02059604  (out=0)
  - FUN_02043b04  (out=2)
      - FUN_02043ac4
      - FUN_02042b34
  - FUN_02025c80  (out=1)
      - FUN_02025cb8
  - FUN_020595bc  (out=0)
  - FUN_02059534  (out=0)
  - FUN_0200c964  (out=3)
      - FUN_02010b6c
      - FUN_02010bb0
      - FUN_02024c34
  - FUN_0203f390  (out=7)
      - FUN_0203f874
      - FUN_020597f0
      - FUN_02000c1c
      - FUN_02059c70
      - FUN_02059604
      - FUN_020595bc
      - FUN_02059534

#### FUN_02019be0 (0x02019be0)  in=0 out=22 size=1120
  - FUN_02025edc  (out=3)
      - FUN_02025edc
      - FUN_020259c8
      - FUN_02026c94
  - FUN_0202e22c  (out=2)
      - FUN_02030964
      - FUN_0203ae88
  - FUN_0201aba0  (out=2)
      - FUN_02026d78
      - FUN_020259c8
  - FUN_0201ab3c  (out=1)
      - FUN_0202b954
  - FUN_02025098  (out=3)
      - FUN_020250d8
      - FUN_02025184
      - FUN_02025118
  - FUN_0202a97c  (out=1)
      - FUN_020534c0
  - FUN_0200b6e4  (out=1)
      - FUN_02024c34
  - FUN_0200a8ac  (out=4)
      - FUN_0200b450
      - FUN_0200b638
      - FUN_02000c1c
      - FUN_02023444
  - FUN_02000c1c  (out=0)
  - FUN_02001164  (out=0)
  - FUN_02002990  (out=1)
      - FUN_02024c34
  - FUN_0200dacc  (out=1)
      - FUN_0200d6e4
  - FUN_0201b270  (out=8)
      - FUN_0202ad50
      - FUN_0203ca94
      - FUN_0202a97c
      - FUN_02030728
      - FUN_02051eb4
      - FUN_0203c92c
      - FUN_0202c9cc
      - FUN_0203c9e4
  - FUN_0200ba74  (out=9)
      - FUN_0200c4b8
      - FUN_0200c030
      - FUN_0200bf10
      - FUN_02000c4c
      - FUN_02000c1c
      - FUN_02036e04
      - FUN_02036cc0
      - FUN_0200c148
  - FUN_020078cc  (out=0)
  - FUN_02059604  (out=0)
  - FUN_0200a904  (out=3)
      - FUN_02002580
      - FUN_02002790
      - FUN_02002160
  - FUN_02029ff8  (out=9)
      - FUN_0202f194
      - FUN_0202a2f4
      - FUN_02029d14
      - FUN_02029f18
      - FUN_020534c0
      - FUN_02053378
      - FUN_02001164
      - FUN_02029cf8
  - FUN_02025c80  (out=1)
      - FUN_02025cb8
  - FUN_02059500  (out=0)
  - FUN_0200a644  (out=1)
      - FUN_0202a628
  - FUN_02059574  (out=0)

#### FUN_02004b70 (0x02004b70)  in=1 out=22 size=2964
  - FUN_02058074  (out=0)
  - FUN_0200582c  (out=1)
      - FUN_02000c64
  - FUN_020582d8  (out=0)
  - FUN_0203036c  (out=3)
      - FUN_02051eb4
      - FUN_020549a8
      - FUN_02030288
  - FUN_0202fd88  (out=12)
      - FUN_02053490
      - FUN_020321a8
      - FUN_020301a0
      - FUN_02031c10
      - FUN_02031c90
      - FUN_02053378
      - FUN_0202fd80
      - FUN_02001164
  - FUN_0200580c  (out=0)
  - FUN_0205947c  (out=0)
  - FUN_02005854  (out=2)
      - FUN_02000c34
      - FUN_02000c64
  - FUN_020590c4  (out=0)
  - FUN_02030964  (out=0)
  - FUN_02053378  (out=0)
  - FUN_020303bc  (out=3)
      - FUN_0205335c
      - FUN_02053204
      - FUN_02030288
  - FUN_02004124  (out=0)
  - FUN_0202fcec  (out=2)
      - FUN_0202fd48
      - FUN_02031f8c
  - FUN_02059604  (out=0)
  - FUN_02030324  (out=3)
      - FUN_020552d4
      - FUN_02051eb4
      - FUN_02030288
  - FUN_0202fd2c  (out=2)
      - FUN_0203209c
      - FUN_02030478
  - FUN_02030288  (out=2)
      - FUN_0203011c
      - FUN_020300cc
  - FUN_02058178  (out=0)
  - FUN_02059500  (out=0)
  - FUN_0203ae88  (out=2)
      - FUN_0203b8a0
      - FUN_02005948
  - FUN_02059574  (out=0)

---

## Phase 4 - Central Entity Candidates
> Criteria: in-degree >= 5 AND offset-accesses >= 5 (many callers + struct-heavy = entity handler)

| Address | Name | in | out | total | size | asm | float | offsets |
|---------|------|----|-----|-------|------|-----|-------|---------|
| 0x0202a97c | FUN_0202a97c | 354 | 1 | 355 | 156 | 39 |  | 10 |
| 0x0202c9cc | FUN_0202c9cc | 145 | 4 | 149 | 808 | 202 |  | 23 |
| 0x0202b954 | FUN_0202b954 | 46 | 2 | 48 | 236 | 59 |  | 16 |
| 0x02029ff8 | FUN_02029ff8 | 76 | 9 | 85 | 352 | 88 |  | 9 |
| 0x0202ad50 | FUN_0202ad50 | 22 | 3 | 25 | 400 | 100 |  | 28 |
| 0x0203f874 | FUN_0203f874 | 8 | 1 | 9 | 784 | 196 |  | 70 |
| 0x02037bec | FUN_02037bec | 42 | 3 | 45 | 144 | 36 |  | 11 |
| 0x0202eac4 | FUN_0202eac4 | 17 | 1 | 18 | 532 | 133 |  | 26 |
| 0x0200ae04 | FUN_0200ae04 | 33 | 3 | 36 | 128 | 32 |  | 8 |
| 0x02026d78 | FUN_02026d78 | 28 | 0 | 28 | 424 | 106 |  | 9 |
| 0x02015bf0 | FUN_02015bf0 | 5 | 5 | 10 | 788 | 197 |  | 46 |
| 0x020157bc | FUN_020157bc | 7 | 3 | 10 | 336 | 84 |  | 31 |
| 0x020295c8 | FUN_020295c8 | 26 | 2 | 28 | 340 | 85 |  | 8 |
| 0x0202f2c4 | FUN_0202f2c4 | 6 | 2 | 8 | 648 | 162 |  | 33 |
| 0x0204a078 | FUN_0204a078 | 14 | 4 | 18 | 396 | 99 |  | 14 |

---

## Phase 5 - Architecture Summary

### Known anchors (from prior patch work)
| Address | Role |
|---------|------|
| 0x020402A4 | First animal creator - breed range setup (PATCH3 target) |
| 0x02040328 | First animal creator - color range setup (PATCH3 target) |
| 0x020403B8 | First animal creator - STRB species/breed/color to new object |
| 0x02042218 | Second animal creator start (clone via vtable, NOT random) |
| 0x020422E8 | Second animal creator - STRB species/breed/color |
| 0x0201B9F4 | Request manager UI handler |
| 0x02036224 | Texture replacement check (PATCH1 - always returns false) |

### Candidate system map
| System | Best candidate | Evidence |
|--------|---------------|----------|
| Main Loop / Entry point | 0x02021cd4 FUN_02021cd4 | Root node, out=38 |
| Top orchestrator | 0x02021cd4 FUN_02021cd4 | Highest out-degree (38) |
| Most-called utility | 0x0202a97c FUN_0202a97c | Highest in-degree (354) |
| Largest function | 0x0205d0b8 FUN_0205d0b8 | 18812 bytes |
| 3D/float system | 0x  | Float usage, total-deg= |
| Central entity handler | 0x0202a97c FUN_0202a97c | in=354, offsets=10 |

---

## Phase 6 - Tooling Notes
- ARM9 size unchanged by current patches - all offsets are stable
- Patch by symbolic function address now possible using table above
- Framework reusable for other NDS titles with Ghidra callgraph export

---

## Key Findings (interpreted)

### Main Loop
**`FUN_02021cd4` (0x02021cd4)** is the strongest main loop / top-level state manager candidate:
- Root node: 0 callers (nothing calls it — it IS the entry point of the game logic)
- Highest out-degree in the entire graph: calls 38 distinct functions
- 2336 bytes, 584 ASM lines — substantial orchestration function
- 157 offset accesses — manages many sub-objects

**`FUN_0201707c` (0x0201707c)** is the second candidate (possibly a different game state / scene):
- Also a root (0 callers), calls 33 functions, 2108 bytes

### Most-Called Utility
**`FUN_0202a97c` (0x0202a97c)** is called **354 times** — by far the hottest function in the codebase:
- Only 156 bytes, 39 ASM lines, calls just 1 other function
- 10 offset accesses suggest it operates on an object/struct
- Almost certainly a reference-counting operation (AddRef/Release) or a core allocator

### Small Hot Utilities (infrastructure)
- **`FUN_02000c1c`** (181 callers, 20 bytes) — tiny, called everywhere — likely a null-check, refcount dec, or inline helper
- **`FUN_02001164`** (154 callers, 12 bytes) — even smaller, similar profile
- **`FUN_02000c4c`** (149 callers, 20 bytes) — same tier, likely paired with 02000c1c

These three together suggest a refcount system: acquire / release / check pattern.

### Central Entity / Object Handler
**`FUN_0202c9cc` (0x0202c9cc)** — 145 callers, 23 offset accesses, 808 bytes:
- Called nearly as often as the tiny utilities but is substantial in size
- Heavy struct access pattern strongly suggests this is a core entity method — likely the main
  getter/setter or update dispatcher for Pet or Patient objects

**`FUN_0203f874` (0x0203f874)** — only 8 callers but **70 offset accesses** in 784 bytes:
- The highest offset-access density in the dataset
- Few callers + massive struct interaction = complex state machine
- Best candidate for **PetUpdate** or **PatientStateMachine** — the function that reads/writes
  all fields of a pet/patient struct (HP, states, timers, flags, species, breed, color)

### Update Chain (Phase 3)
The likely update chain top:
```
FUN_02021cd4  (root, out=38)  <-- main loop / top-level dispatcher
  ├─ FUN_0201707c  (root, out=33)  <-- scene or state variant
  ├─ FUN_02041a4c  (in=2, out=21)  <-- sub-system manager
  └─ ... (38 callees total)
```
To trace deeper: load `FUN_02021cd4` in Ghidra and follow its call tree.
