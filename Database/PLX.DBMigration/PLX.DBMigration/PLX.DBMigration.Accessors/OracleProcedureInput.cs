using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PLX.DBMigration.Accessors
{
    /// <summary>
    /// Oracle Procedure Input is responsible for holding the multiple values that can be enter for an 
    /// Oracle Procedure call.
    /// </summary>
    public class OracleProcedureInput
    {
        public string procedureName;
        public List<string> parameterName;
        public List<int> parameterValue;
        public int numberOfParameters;
    }
}
