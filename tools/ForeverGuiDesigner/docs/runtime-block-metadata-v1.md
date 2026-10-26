# Forever Studio Runtime Block Metadata v1

Forever Studio discovers ordinary custom blocks directly from the normal Minecraft resource tree. A new block that has a valid `blockstates/*.json` plus model JSON is visible and editable without adding Studio code.

Runtime metadata is only needed when a block has visual state or geometry that does **not** live in the ordinary Minecraft block model, for example geometry emitted by a `BlockEntityRenderer`.

## Location

Create one descriptor per runtime-backed block under:

```text
mod/src/main/resources/assets/forever_skyblock/forever_studio/blocks/<asset-id>.json
```

The filename is only for humans. `assetId` is the stable identity used by Studio and must match the discovered block asset ID.

## Compatibility model

- Normal blockstate/model discovery remains authoritative for the editable base model.
- Existing Forever machines without metadata keep using the built-in source-parity runtime profiles.
- A valid project descriptor takes precedence for the matching `assetId`.
- Invalid metadata is isolated. Studio reports a warning and keeps the ordinary block/model editor available.
- Unknown future `schemaVersion` values fail closed instead of being partially interpreted.
- Project animation files and their stable JSON element TargetIds are unchanged.

## Security and stability rules

Runtime metadata is **data only**. Forever Studio never executes Java, scripts or expressions from a descriptor.

- `previewModel` may only reference local `forever_skyblock:block/...` models.
- Traversal such as `..` and external namespaces are rejected.
- Descriptor files, component counts, state counts, options, tracks and keyframes have hard safety limits.
- IDs are normalized and validated before entering target/state registries.
- Runtime source animation uses bounded numeric samples over normalized phase `0..1`.
- Supported interpolation modes are `linear`, `smooth` and `step`.
- Components hidden by a source state stay in stable topology but are collapsed in preview, preventing neighboring animation targets from changing identity.
- Missing runtime preview geometry does not make the base block uneditable.

## Descriptor example

```json
{
  "schemaVersion": 1,
  "assetId": "example_machine",
  "displayName": "Example Machine",
  "runtimeOwner": "ExampleMachineRenderer.java",
  "coverageNote": "Runtime rotor and machine status are described by project metadata.",
  "components": [
    {
      "key": "rotor",
      "displayName": "Rotor",
      "previewModel": "forever_skyblock:block/studio/example_machine_rotor",
      "pivot": [8, 8, 8],
      "visibleWhen": {
        "status": ["running"]
      }
    }
  ],
  "states": [
    {
      "id": "status",
      "displayName": "Machine status",
      "defaultOptionId": "idle",
      "description": "Visual state read by the renderer.",
      "options": [
        {
          "id": "idle",
          "displayName": "Idle",
          "description": "Rotor is hidden."
        },
        {
          "id": "running",
          "displayName": "Running",
          "description": "Rotor is visible and animated."
        }
      ]
    }
  ],
  "sourceTracks": [
    {
      "id": "rotor-spin",
      "displayName": "Rotor spin",
      "component": "rotor",
      "sourceSymbol": "ExampleMachineRenderer.rotorAngle",
      "sourcePath": "mod/src/main/java/de/tekatz/foreverskyblock/client/ExampleMachineRenderer.java",
      "kind": "Rotation",
      "channel": "rotateY",
      "interpolation": "linear",
      "description": "One complete turn during the visual cycle.",
      "keyframes": [
        { "phase": 0.0, "value": 0.0 },
        { "phase": 1.0, "value": 360.0 }
      ]
    }
  ]
}
```

The runtime component model in this example is an ordinary Minecraft block model JSON under:

```text
mod/src/main/resources/assets/forever_skyblock/models/block/studio/example_machine_rotor.json
```

Its elements are preview/runtime geometry only; they are not merged into or written back to the editable base block model.

## Supported source-track channels

| Channel | Meaning | Identity value |
| --- | --- | ---: |
| `translateX` | X translation in model units | 0 |
| `translateY` | Y translation in model units | 0 |
| `translateZ` | Z translation in model units | 0 |
| `rotateX` | X rotation in degrees | 0 |
| `rotateY` | Y rotation in degrees | 0 |
| `rotateZ` | Z rotation in degrees | 0 |
| `scaleX` | X scale multiplier | 1 |
| `scaleY` | Y scale multiplier | 1 |
| `scaleZ` | Z scale multiplier | 1 |
| `uniformScale` | XYZ scale multiplier | 1 |
| `visibility` | visible when sampled value is >= 0.5 | 1 |

Multiple translation/rotation tracks for one component are additive. Multiple scale tracks multiply. Studio-authored animation remains a separate outer transform layer and is composed after the source/runtime transform.

## Adding a future custom block

1. Add the normal blockstate/model/texture assets. The block is immediately discoverable in Block Editor.
2. Use normal model elements in Animation Studio when all animated geometry exists in model JSON. No runtime descriptor is required.
3. If a renderer adds extra geometry, model those runtime components as local preview model JSON files and add a v1 descriptor.
4. Declare real renderer state axes under `states` when runtime predicates affect visibility/behavior.
5. Declare read-only source behavior under `sourceTracks`. Do not encode Java expressions; sample the visual curve as numeric keyframes.
6. Studio automatically creates stable `runtime:<assetId>:<component>` targets. Authored tracks are persisted through the existing project animation document format.

This separation is intentional: Minecraft assets stay editable as Minecraft assets, project-authored animation stays versioned in the existing animation format, and Java-only renderer behavior is described through a small safe compatibility contract rather than hard-coded Studio switches.
