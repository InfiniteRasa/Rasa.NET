# Velon Hollow: seam repair and follow-up investigation (v2)

Map 2136 is `adv_foreas_howlingmaw_deathburrow`. This version replaces the earlier six-tile patch and includes the four original seam repairs plus a hospital-ramp height improvement.

## The two flagged sections

| Section | Finding | Action |
|---|---|---|
| West cave, about x=68-77, z=-438 to -442 | No reproduced movement blocker or four-meter drop. The earlier probe chose a lower surface near Y=66.94 beneath the actual cave floor, and fed that adjusted height into subsequent ground queries. | Correct the diagnostic method; retain actual movement-worker regressions. No additional tile outside the existing seam area is needed. |
| Hospital ramp, about 165.65, 76.1, -514.44 | Continuous native ramp and adequate headroom. Coarse navmesh height detail caused visible-height error, not a disconnected path. The old probe's short ray missed a floor more than a meter below its estimated feet. | Include the hospital tile and improve contour/height sampling. |

The actual server scripted-movement worker completed both sections in both directions on the shipped, v1 and v2 meshes. The eight shipped/v1 diagnostic runs and eight shipped/v2 diagnostic runs had no waist/head collision hits or vertical step over 0.9 m. The apparent west drop was a probe artifact, not an observed server movement failure.

Along the measured hospital traversals, the shipped mesh put feet about 1.15 m above collision floor at worst, and v1 about 1.20 m. V2 reduced that to about 0.43 m outbound and 0.53 m inbound. At the originally flagged point, the discrepancy falls from about 1.21 m to about 0.40 m. It is still an approximate voxel/detail mesh, not an exact collision-surface replica.

## Included changes

Seven tiles are replaced: (23,9), (23,10), (21,11), (22,11), (19,10), (20,10), (20,12). Two seams cross tile boundaries; (23,9) covers the hospital ramp. Every other one of the 1,593 tile payloads is byte-for-byte unchanged, including the collision/cover asset and all other maps. The mesh remains at 1,600 tiles.

Physical agent settings remain height 2 m, radius 0.6 m, climb 0.9 m and slope 50 degrees. The map-specific `velon-hollow-seams-v2` profile uses cell 0.2 m, cell height 0.2 m, tile 256 cells (51.2 m), border 10 cells, region minimum/merge 16/40, edge length 2 m, edge error 1.3 cells, detail sample distance 6 cells (1.2 m), and detail maximum error 0.5 cells (0.1 m). This reduces long-edge interpolation error on the curved ramp without raising the climb limit or shrinking the agent.

The builder uses original client geometry: 2,020,633 terrain triangles and 535,057 entity collision triangles. It builds a fine intermediate mesh, then copies only the seven selected tiles into the supplied base while retaining tile references. The full intermediate build retains all 1,600 tiles with no contour failure; Recast reports 151 dropped dangling detail faces. No mission, database or runtime movement-engine code changes are included.

## Verification

97/97 focused tests passed. This includes the original 24 Velon Hollow navigation cases plus 13 movement/height cases and 60 related private/shared instance and navigation checks.

- All four original seams and hospital-to-three-temple landmark paths connect in both directions. All 14 connectivity cases failed on the shipped mesh before the seam repair.
- The real scripted-movement worker completes both investigated sections and the full hospital-to-temple landmark routes in both directions. It checks the actual returned positions, without replacing them using a separate collision-grounding probe.
- In the investigated areas, tests check waist/head collision rays, two meters of overhead clearance, and vertical increments below the original 0.9 m climb limit. Support is checked within 0.6 m on the improved hospital ramp; the west area retains a sub-meter approximation allowance consistent with the shipped mesh.
- Three fixed hospital-ramp support checks run independently of route choice. Two fail on v1 and all three pass on v2.
- The remaining 1,593 tile payloads have a regression fingerprint. All seven selected tile headers retain the original physical agent dimensions.
- The documented CLI command reproduced the checked-in binary exactly. Invalid map/profile/override combinations are rejected.

These are server movement and sampled point/ray checks, not a full swept-capsule test or a live game-client session. Temple destinations are authored map-region landmarks, not a complete playthrough of mission 1976's script. No coordinates were available for the additional mission-1977 seam, which remains unverified. The full suite was not rerun.

Asset SHA256: `F955FCCFACC3CFFD91CC271E8CF4377D9F964935D87A7C205B597F40341A76AE`.

## Regeneration

The git-am patch includes the rebuilt asset. Applying it does not require client data or another navmesh build. For future regeneration, use the specific profile with an existing Velon Hollow mesh as the base:

```powershell
dotnet run --project src\Rasa.NavMesh -- --client 'C:\Users\Atomsk\Desktop\Rasa\Tabula Rasa 1.16.5.0' --map adv_foreas_howlingmaw_deathburrow --profile velon-hollow-seams-v2 --base-nav navmesh\adv_foreas_howlingmaw_deathburrow.nav --out navmesh --threads 4 --no-cover
if ($LASTEXITCODE -ne 0) { throw 'Velon Hollow rebuild failed.' }
```

The base must contain the seven selected tiles with a matching tile grid. Other tiles in the supplied base are preserved. Use this profile when rebuilding Velon Hollow; a generic default rebuild can erase the repairs.
