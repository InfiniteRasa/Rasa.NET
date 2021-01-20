using Rasa.Data;
using Rasa.Managers;
using Rasa.Structures.Char;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Rasa.Structures
{
    public class Player : Manifestation
    {
        private readonly CharacterEntry character;

        public double Rotation => character.Rotation;

        public uint MapContextId => character.MapContextId;

        public bool IsRunning { get; set; }

        public Player(CharacterEntry character, List<CharacterAppearanceEntry> appearanceData, IEntityManager entityManager)  
            : base(entityManager)
        {
            this.character = character;

            AppearanceData = appearanceData;
            IsRunning = character.RunState == 1;
        }

        protected override Vector3 GetPositionVector()
        {
            return new Vector3((float)character.CoordX, (float)character.CoordY, (float)character.CoordZ);
        }
    }
}
