namespace Rasa.Structures
{
    using Data;
    using Memory;

    /// <summary>
    /// The state of one map-screen marker, in the shape its own kind of marker is read as.
    ///
    /// There is no single shape. <c>client/mapstate.py</c> stores whatever it is sent under the
    /// marker's entity id and then unpacks it differently per marker type, in both the map window
    /// and the minimap:
    ///
    /// <list type="bullet">
    /// <item>WAYPOINT_TELEPORTER: <c>(isFriendly, isKnown)</c></item>
    /// <item>HOSPITAL and SAFE_ZONE: <c>(isFriendly, isKnown, isSafe)</c></item>
    /// <item>CRAFTING_STATION: a bare <c>isActive</c>, not a tuple at all</item>
    /// <item>CONTROL_POINT: <c>(ownerTypeId, ownerId)</c></item>
    /// </list>
    ///
    /// Sending the wrong arity is not a display fault: the client unpacks a fixed number of names
    /// from it, and a tuple of the wrong length raises inside its own UI code, on every marker,
    /// every time the map is opened.
    /// </summary>
    public class MapMarkerState
    {
        public uint MarkerType { get; }

        public bool IsFriendly { get; set; }
        public bool IsKnown { get; set; }
        public bool IsSafe { get; set; }
        public bool IsActive { get; set; }

        /// <summary>A control point's owner under FACTION_OWNED: true for the AFS, false for the Bane.</summary>
        public bool OwnerIsAfs { get; set; }

        public MapMarkerState(uint markerType)
        {
            MarkerType = markerType;
        }

        /// <summary>A waypoint or hospital the player has or has not discovered.</summary>
        public static MapMarkerState Teleporter(uint markerType, bool isKnown, bool isFriendly = true, bool isSafe = true)
        {
            return new MapMarkerState(markerType) { IsKnown = isKnown, IsFriendly = isFriendly, IsSafe = isSafe };
        }

        /// <summary>A crafting station that is there and working.</summary>
        public static MapMarkerState CraftingStation(bool isActive)
        {
            return new MapMarkerState(MapMarkerType.CraftingStation) { IsActive = isActive };
        }

        /// <summary>A control point and the faction that holds it (ControlPoints).</summary>
        public static MapMarkerState ControlPoint(bool ownerIsAfs)
        {
            return new MapMarkerState(MapMarkerType.ControlPoint) { OwnerIsAfs = ownerIsAfs };
        }

        public void Write(PythonWriter pw)
        {
            switch (MarkerType)
            {
                case MapMarkerType.ControlPoint:
                    // (ownerTypeId, ownerId). Under FACTION_OWNED the client tests the owner with
                    // "is True" and "is False": see ControlPointOwnershipType.
                    pw.WriteTuple(2);
                    pw.WriteUInt(ControlPointOwnershipType.FactionOwned);
                    pw.WriteBool(OwnerIsAfs);
                    break;

                case MapMarkerType.WaypointTeleporter:
                    pw.WriteTuple(2);
                    pw.WriteBool(IsFriendly);
                    pw.WriteBool(IsKnown);
                    break;

                case MapMarkerType.Hospital:
                case MapMarkerType.SafeZone:
                    pw.WriteTuple(3);
                    pw.WriteBool(IsFriendly);
                    pw.WriteBool(IsKnown);
                    pw.WriteBool(IsSafe);
                    break;

                case MapMarkerType.CraftingStation:
                    // Not wrapped. The client assigns this straight to isActive and tests its
                    // truth; a one-tuple would be truthy whatever was inside it, so a station
                    // that is off would read as on.
                    pw.WriteBool(IsActive);
                    break;

                default:
                    // Nothing else has a shape this server can fill in yet. Writing a guess
                    // would be worse than writing nothing: the client keeps whatever it is
                    // given and unpacks it by type the next time the map opens.
                    pw.WriteNoneStruct();
                    break;
            }
        }
    }
}
