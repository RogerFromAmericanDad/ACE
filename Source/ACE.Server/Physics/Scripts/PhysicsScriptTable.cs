using ACE.Entity.Enum;
using System.Collections.Generic;

namespace ACE.Server.Physics
{
    public class PhysicsScriptTable
    {
        public Dictionary<long, PhysicsScriptTableData> ScriptTable;

        public void Release()
        {

        }

        public int GetScript(PlayScript type, float mod)
        {
            return -1;
        }
    }
}
