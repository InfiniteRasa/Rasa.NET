# Line of sight in caves

What a line of sight is tested against under the ground (`Managers.Sight`, `appsettings.json`'s `Sight`). Server
only: the retail client already sees, aims and shoots in caves; the server is what refused.

## Why

The server tests a ranged hit, a creature's ranged attack and a selected target against the map's cover file
(`navmesh/<map>.cover`, `Navigation.CoverMesh`): every entity's collision triangles, the caves' shells among them,
and the terrain as a height grid. The grid is the whole heightmap and knows nothing of a cave under it, so two actors
in a cave are both deep in the ground to it and every line between them longer than a grid cell (2 m) is blocked:

- a ranged hit does the least share of its damage, x0.10 (`Cover.Modifier`);
- a creature never uses its ranged attack (`BehaviorManager.Sees`);
- a selected target is "wholly behind cover" (`WeaponChecks`, logged by default, refused if `Sight` is `refuse`).

The Pinhole Falls Caverns under the Wilderness are such a place: "Mama Miasma!" sends players after three egg-layers
there, more than 50 m under the grid.

## The rule

With `UnderTerrain` on, the grid is left out of a line when either end is an actor that is underground, and the
line is tested against the collision alone - the cave's own walls, floor and roof. Underground is the map's word, not
the client's:

1. its feet are more than 2 m under the height grid (`NavMeshFlags.UndergroundDepth`, what the navmesh builder takes
   for under the terrain);
2. the navmesh has floor flagged underground within 2 m across, 3 m below and 1 m above the feet
   (`NavMeshQuery.UndergroundFloor`);
3. nothing of the collision lies between that floor's chest height and the actor's chest.

A place inside a hill, which a client can claim to stand at, has no such floor; a place in the rock beside a cave or
over its roof has the cave's shell between it and the floor; a place on the hill over a cave is not under the grid.
None of them gains anything. On the surface only the grid is asked, so nothing changes there and nothing more is
spent.

There is no roof condition, decided on evidence: on five maps a roof check took most of the fix back (22-38 % of
cave lines then differed from the client's again, against 0.1-1.4 % without), because the places under the grid with a
floor and no roof in the collision are real sunken places where the client leaves its ground out too.

## Settings

```json
"Sight": {
  "UnderTerrain": true
}
```

Without the section it is off and every answer is the cover mesh's own, as before. A config reload applies from the
next line. `.cover` says "(underground: terrain not counted)" and `.los` "An end is underground" when the rule applies.
