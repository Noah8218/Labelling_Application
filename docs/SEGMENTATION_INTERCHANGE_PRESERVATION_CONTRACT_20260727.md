# Segmentation Interchange Preservation Contract

Date: 2026-07-27 KST
Status: Complete
Scope: P1-A preservation/loss contract and P1-B canonical schema v3

## 1. Purpose

This contract prevents structural mask editing from making unsupported
round-trip claims. Before merge, split, hole, multi-component, z-order, or
remove-underlying editing is added, every target format must declare whether
each semantic is preserved, conditionally converted, or lost.

The code authority is
`Yolo\SegmentationInterchangeContractService.cs`. Export services reuse that
authority instead of maintaining separate warning text.

## 2. Preservation levels

- `Preserved`: the current writer retains the semantic without another
  representation.
- `Conditional`: the result depends on a sibling artifact, ordered class
  catalog, rasterization, normalization, or contour conversion.
- `Lost`: the current target has no representation in the implemented path.

## 3. Current format matrix

| Semantic | Segment JSON v3 | Class-index mask PNG | YOLO segmentation | COCO polygon | CVAT polygon |
| --- | --- | --- | --- | --- | --- |
| Class identity | Preserved | Conditional | Conditional | Preserved | Preserved |
| Polygon geometry | Preserved | Conditional | Conditional | Preserved | Preserved |
| Raster pixels | Conditional on sibling PNG | Preserved | Conditional | Conditional | Conditional |
| Holes | Preserved as `Cutouts` | Preserved as background pixels | Lost | Lost | Lost |
| Multiple components | Preserved with component index | Preserved as disconnected pixels | Conditional, separate rows | Conditional, separate annotations | Conditional, separate polygons |
| One-object component grouping | Preserved with object ID | Lost | Lost | Lost | Lost |
| Z-order | Preserved | Lost after compositing | Lost | Lost | Lost; current writer emits `z_order=0` |
| Remove-underlying provenance | Preserved as last structural operation | Lost | Lost | Lost | Lost |

Segment JSON and mask PNG form the current local canonical bundle. JSON v3 owns
class, polygon contour, geometry type, cutout rings, persistent object ID,
component index, z-order, and last structural operation. The PNG owns exact
class-index raster pixels.

Version 1/2 files remain readable. Missing metadata receives deterministic
image-local legacy identity (`legacy-000000`, `legacy-000001`, ...), component
index `0`, z-order `0`, and empty operation provenance. Re-saving promotes the
annotation to v3 without changing polygon/raster interpretation.

## 4. Runtime warning contract

`SegmentationInterchangeProfile.FromAnnotation` detects class, polygon
geometry, raster origin, and holes for every supported version. For v3 it also
detects grouped components, z-order, and structural-operation provenance.

COCO, CVAT, and YOLO segmentation export results now expose a deduplicated
`Warnings` collection. A polygon containing a cutout therefore exports only
with an explicit `Holes: Lost` warning. The export remains available; the
warning prevents the conversion from being presented as lossless.

Example:

```csharp
SegmentationInterchangeAuditResult audit =
    SegmentationInterchangeContractService.AuditAnnotation(
        SegmentationInterchangeTarget.CocoPolygonSegmentation,
        annotation);

if (!audit.IsLossless)
{
    foreach (string warning in audit.Warnings)
    {
        Log(warning);
    }
}
```

## 5. Implementation boundaries

Included:

- one programmatic capability matrix for canonical JSON, mask PNG, YOLO, COCO,
  and CVAT;
- explicit structural-mask fixture covering holes, multiple components,
  instance grouping, z-order, and remove-underlying provenance;
- annotation-derived warnings in COCO/CVAT/YOLO export results;
- existing export files remain backward compatible.

Excluded:

- merge/join, split/slice, or z-order commands;
- changing COCO polygon output to RLE;
- changing CVAT polygon output to mask RLE;
- Viewer/OpenGL, brush, or eraser input paths;
- claims that conversion loss has already been eliminated.

## 6. Verification

Primary focused gate:

```powershell
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --segmentation-interchange-contract
```

The fixture verifies:

- every target has exactly one declared capability for every semantic;
- canonical JSON v3 preserves cutout rings, object/component identity, z-order,
  and structural-operation provenance;
- disconnected raster components retain one object ID and stable component
  indices across save, load, and re-save;
- segment JSON v2 loads with unchanged geometry and deterministic legacy
  metadata;
- history clones retain identity while duplicate commands deliberately start a
  new identity;
- mask PNG preserves raster pixels but loses instance grouping and z-order;
- YOLO, COCO polygon, and CVAT polygon report hole loss;
- COCO/CVAT/YOLO export result objects surface the same deduplicated warning;
- existing segment JSON and mask PNG remain the source artifacts used by
  exporters.

## 7. Next bounded implementation

P1-C can now implement merge/join, split/slice, hole/multi-component editing,
z-order, and remove-underlying warnings with one undo/redo step per operation.
Every command must update v3 identity/provenance and prove save/load/re-save
behavior before it is exposed as complete.
