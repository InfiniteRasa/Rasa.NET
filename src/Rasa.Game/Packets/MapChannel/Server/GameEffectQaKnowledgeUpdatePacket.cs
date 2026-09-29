using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Recv_CallGameEffectMethod(effectId, 'Update', (info,)) on the entity holding a QA_KNOWLEDGE
    /// effect (gameeffectdata 5): a developer readout of the holder and its target, drawn on the
    /// QA build's debug overlay. Wired so that it can be sent, and deliberately sent by nothing:
    /// the 1.16.5 client has no overlay to draw it on.
    ///
    /// Update is a method of the effect, not of the entity: the Update (221) opcode names it in
    /// the client's method table, but no entity class has a Recv_Update to answer one sent to it
    /// directly. The only Recv_Update in the client is this effect's.
    ///
    /// client/actions/abilities/qaknowledge.py:
    ///  - QAKnowledgeAction (action QA_KNOWLEDGE, 84 - ActionId.QaKnowledge) targets anything,
    ///    dead or alive, is usable while dead or swimming and is never interrupted. Its
    ///    CanDoPrecast refuses with PM_QAKNOWLEDGE_UNAVAILABLE (1072) when gameclient has no
    ///    GetQAScreen.
    ///  - QAKnowledgeEffect's announce on the client's own avatar calls SetDeveloper(1), and its
    ///    detach SetDeveloper(0) and clears both panels of the overlay. Nothing in the client
    ///    reads IsDeveloper.
    ///  - Recv_Update(target, info) fills the overlay from info (below), adding each entity's
    ///    animation list (GetAnimationUpdate) itself. Without TargetId it clears the target panel.
    ///
    /// info, a dictionary:
    ///  - ActorId: the holder's entity id; a name of "Unknown-&lt;id&gt;" if the client lacks it.
    ///  - Actor-Attributes: keyed by attribute id; HEALTH, BODY, MIND and SPIRIT are read, each a
    ///    (current, maximum) pair. A missing one is a KeyError.
    ///  - Actor-Weapon: the weapon's name. Actor-WeaponDmg: (min, max); (0, 0) if left out.
    ///  - Actor-PostureState, -ControlState, -MovementState, -CombatState, -ActionState,
    ///    -ToolState: passed to the overlay as they are.
    ///  - TargetId and the same Target- keys, plus Target-Abilities: (abilityId, level, allowed)
    ///    for each ability; those not allowed are left off, the rest shown by name and level.
    ///
    /// tabula_rasa.exe has none of GetQAScreen, SetActorInfo, SetTargetInfo or ClearTargetInfo;
    /// only qaknowledge.pyo in trpython.zip names them. So the retail client cannot start the
    /// ability, and an Update sent to it anyway fails on gameclient.GetQAScreen before it reads
    /// anything - never send this to a retail client. Attaching the effect alone is harmless.
    ///
    /// QAKnowledgeQuery (135) is only a number too - no client or server code uses the name -
    /// and was presumably the QA build's request for these. The C++ server only listed both ids.
    ///
    /// A packet made with no arguments is a well-formed readout of nothing: entity 0, zero
    /// attributes, no weapon, no target.
    /// </summary>
    public class GameEffectQaKnowledgeUpdatePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CallGameEffectMethod;

        /// <summary>The attributes Recv_Update reads, in the order the overlay shows them.</summary>
        private static readonly Attributes[] ReadAttributes = { Attributes.Health, Attributes.Body, Attributes.Mind, Attributes.Spirit };

        /// <summary>One side of the readout: the holder (Actor-) or its target (Target-).</summary>
        public class Subject
        {
            public ulong EntityId { get; set; }

            /// <summary>(current, maximum) by attribute; the four ReadAttributes are always written, (0, 0) when missing here.</summary>
            public Dictionary<Attributes, (int Current, int Max)> Attributes { get; } = new Dictionary<Attributes, (int Current, int Max)>();

            public string Weapon { get; set; } = "";
            public int WeaponDamageMin { get; set; }
            public int WeaponDamageMax { get; set; }

            public int PostureState { get; set; }
            public int ControlState { get; set; }
            public int MovementState { get; set; }
            public int CombatState { get; set; }
            public int ActionState { get; set; }
            public int ToolState { get; set; }

            /// <summary>Target-Abilities only: (abilityId, level, allowed).</summary>
            public List<(int AbilityId, int Level, bool Allowed)> Abilities { get; } = new List<(int AbilityId, int Level, bool Allowed)>();
        }

        public int EffectId { get; set; }
        public Subject Actor { get; set; } = new Subject();

        /// <summary>Null for no target; the client then clears its target panel.</summary>
        public Subject Target { get; set; }

        public GameEffectQaKnowledgeUpdatePacket()
        {
        }

        public GameEffectQaKnowledgeUpdatePacket(int effectId, Subject actor, Subject target = null)
        {
            EffectId = effectId;
            Actor = actor ?? new Subject();
            Target = target;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt(EffectId);
            pw.WriteString("Update");
            pw.WriteTuple(1);                   // args = (info,)

            // Actor: id, attributes, weapon, damage, six states. Target: the same and its abilities.
            pw.WriteDictionary(10 + (Target != null ? 11 : 0));

            pw.WriteString("ActorId");
            pw.WriteULong(Actor.EntityId);
            WriteSubject(pw, "Actor-", Actor);

            if (Target == null)
                return;

            pw.WriteString("TargetId");
            pw.WriteULong(Target.EntityId);
            WriteSubject(pw, "Target-", Target);

            pw.WriteString("Target-Abilities");
            pw.WriteList(Target.Abilities.Count);

            foreach (var (abilityId, level, allowed) in Target.Abilities)
            {
                pw.WriteTuple(3);
                pw.WriteInt(abilityId);
                pw.WriteInt(level);
                pw.WriteBool(allowed);
            }
        }

        private static void WriteSubject(PythonWriter pw, string prefix, Subject subject)
        {
            pw.WriteString(prefix + "Attributes");
            pw.WriteDictionary(ReadAttributes.Length);

            foreach (var attribute in ReadAttributes)
            {
                subject.Attributes.TryGetValue(attribute, out var value);

                pw.WriteInt((int)attribute);
                pw.WriteTuple(2);
                pw.WriteInt(value.Current);
                pw.WriteInt(value.Max);
            }

            pw.WriteString(prefix + "Weapon");
            pw.WriteString(subject.Weapon ?? "");

            pw.WriteString(prefix + "WeaponDmg");
            pw.WriteTuple(2);
            pw.WriteInt(subject.WeaponDamageMin);
            pw.WriteInt(subject.WeaponDamageMax);

            pw.WriteString(prefix + "PostureState");
            pw.WriteInt(subject.PostureState);
            pw.WriteString(prefix + "ControlState");
            pw.WriteInt(subject.ControlState);
            pw.WriteString(prefix + "MovementState");
            pw.WriteInt(subject.MovementState);
            pw.WriteString(prefix + "CombatState");
            pw.WriteInt(subject.CombatState);
            pw.WriteString(prefix + "ActionState");
            pw.WriteInt(subject.ActionState);
            pw.WriteString(prefix + "ToolState");
            pw.WriteInt(subject.ToolState);
        }
    }
}
