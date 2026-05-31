# LDraw Licence Attribution

## LDraw Parts Library

BrickForge uses LDraw-compatible file formats to export generated brick models.

The LDraw Parts Library is an open-source collection of LEGO-compatible part definitions
maintained by the LDraw community.

**Licence:** Creative Commons Attribution 2.0 (CC BY 2.0)  
**Source:** https://www.ldraw.org  
**Legal information:** https://www.ldraw.org/legal.html

### Attribution Requirements

As required by CC BY 2.0:

- Attribution to the LDraw Parts Library must be retained in any redistribution.
- Any generated `.mpd`, `.ldr`, or `.ldr`-based file includes attribution comments
  in the file header as required.
- BrickForge does not remove copyright or licence notices from LDraw part definitions.

### LDraw File Header

BrickForge inserts the following attribution into every generated LDraw/MPD file:

```text
0 LDraw Parts used in this file are from the LDraw Parts Library.
0 The LDraw Parts Library is licensed under the Creative Commons Attribution 2.0 license.
0 See https://www.ldraw.org/legal.html for details.
```

### Notes

- Individual LDraw parts may carry additional or different licence notices.
  Users who redistribute modified parts must check each part's own licence header.
- BrickForge does not bundle the LDraw Parts Library itself.
  Part geometry is referenced by part number in export files and must be resolved
  by a compatible LDraw viewer on the user's machine.

---

## LPub3D (Optional)

LPub3D is an open-source WYSIWYG tool for producing LEGO-style building instructions
from LDraw files.

**Website:** https://trevorsandy.github.io/lpub3d/  
**Licence:** GNU General Public License v3 (GPLv3) and other open-source licences.

LPub3D is **not** bundled with BrickForge. It is an optional external tool for
post-processing generated LDraw files. Users who install and use LPub3D must comply
with its own licence terms.

---

## BrickLink Studio (Optional)

BrickLink Studio is a proprietary CAD tool for building and rendering LEGO-compatible models.

**Website:** https://www.bricklink.com/v3/studio/download.page  
**Licence:** BrickLink Studio Software Licence Agreement  
**Reference:** https://studiohelp.bricklink.com/hc/en-us/articles/6606313426711-Studio-Software-License-Agreement

BrickLink Studio is **not** required by BrickForge. It may be used optionally for manual
post-processing of generated `.mpd` files. Users who install and use BrickLink Studio must
comply with its own licence terms.

---

## LEGO Trademark Notice

LEGO® is a registered trademark of the LEGO Group.

BrickForge is an independent fan-made tool and is not affiliated with, endorsed by,
or approved by the LEGO Group.

Generated models and instructions are fanbasierte digitale Bauanleitungen (MOC – My Own Creation)
and are not official LEGO products or instructions.

Neutral terms used in BrickForge output:

- Brick-Modell
- Klemmbaustein-kompatibel
- MOC (My Own Creation)
- digitale Bauanleitung
- fanbasierte Modellbeschreibung
