using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using PLX.DBMigration.Accessors;

namespace PLX.DBMigration.Managers
{
    /// <summary>
    /// Finalize Manager is responsible for executing all of the steps involved in finalizing an upgraded database
    /// </summary>
    class FinalizeManager
    {
        private string startLocation = "";
        private string currentEdition = "";
        private string nextEdition = "";
        private string oracleLocation = "";
        
        /// <summary>
        /// Constructor
        /// </summary>
        public FinalizeManager()
        {
        }

        #region Methods

        /***********************************************************************/
        // Methods
        /***********************************************************************/

        /// <summary>
        /// Run each finalization step.
        /// </summary>
        public void RunFinal()
        {
            GetConnections();
            RunQualifyFinalization();
            RunFinalization();
        }

        /// <summary>
        /// Get connections from xml.
        /// </summary>
        public void GetConnections()
        {
            try
            {
                XmlAccessor xClass = new XmlAccessor();
                xClass.ReadXmlFile();
                startLocation = xClass.XmlValues["StartLocation"];
                oracleLocation = xClass.XmlValues["OracleLocation"];

                if (File.Exists(startLocation + "\\Current Edition.txt"))
                {
                    using (var sr = new StreamReader(startLocation + "\\Current Edition.txt"))
                    {
                        currentEdition = sr.ReadLine();
                        nextEdition = sr.ReadLine();
                    }
                }
                else
                {
                    Console.WriteLine("File Missing " + startLocation + "\\Current Edition.txt");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Get Connections Failed!");
            }
        }

        /// <summary>
        /// Run the finalization package.
        /// </summary>
        public void RunQualifyFinalization()
        {
            Console.WriteLine("Running lynx.upgrade_pkg.qualify_database ...");

            OracleAccessor oc = new OracleAccessor();
            oc.ConnectToOracle();
            oc.Transaction = oc.Connection.BeginTransaction();

            OracleProcedureInput pic = new OracleProcedureInput();
            pic.numberOfParameters = 0;
            pic.procedureName = "lynx.upgrade_pkg.qualify_database";
            oc.NonQueryProcedure(pic);

            oc.Transaction.Commit();
            oc.Connection.Close();

            Console.WriteLine("Finished lynx.upgrade_pkg.qualify_database.");
        }

        /// <summary>
        /// Open 60_run_finalization_original.pdc and Parse and run each line.
        /// </summary>
        public void RunFinalization()
        {
            // since this is the same as run upgrade should be able to combine them and just pass the file location.

            //string dropLocation = StartLocation + "\\10_Structure\\" + CurrentEdition + "\\05_UPGRADE\\60_run_finalization.pdc";
            string finalLocation = startLocation + "\\10_Structure\\" + currentEdition + "\\05_UPGRADE\\60_run_finalization_original.pdc";

            if (File.Exists(finalLocation))
            {
                string readLine;
                OracleAccessor oc = new OracleAccessor();

                using (StreamReader dpuFile = new StreamReader(finalLocation))
                {
                    while ((readLine = dpuFile.ReadLine()) != null)
                    {
                        readLine = readLine.Replace(";", "");

                        if (readLine.IndexOf("connect", StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            // Connect with edition or normal //
                            readLine = readLine.Remove(0, 8);
                            string UserId = readLine.Remove(readLine.IndexOf("/"));
                            string Password = readLine.Remove(0, readLine.IndexOf("/") + 1);
                            string Edition = "";
                            if (Password.IndexOf("edition") != -1)
                            {
                                // Parse out edition //
                                Edition = Password.Remove(0, Password.IndexOf("edition"));
                                Password = Password.Remove(Password.IndexOf("edition") - 1);
                            }

                            // Now try to connect //
                            //oc.Connect(UserId, Password, Edition);
                        }
                        else if (readLine.IndexOf("execute", StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            //oc.tran = oc.conn.BeginTransaction();

                            // execute with a parameter (x) //
                            readLine = readLine.Remove(0, 8);

                            string proName = readLine.Remove(readLine.IndexOf("("));
                            string paramNum = readLine.Remove(0, readLine.IndexOf("(") + 1);
                            paramNum = paramNum.Remove(1);

                            OracleProcedureInput pic = new OracleProcedureInput();
                            pic.parameterName = new List<string>();
                            pic.parameterValue = new List<int>();
                            pic.numberOfParameters = 1;
                            pic.parameterName.Add("i_section_number");
                            pic.parameterValue.Add(Convert.ToInt32(paramNum));
                            pic.procedureName = proName;

                            oc.NonQueryProcedure(pic);

                            //oc.tran.Commit();
                            //oc.conn.Close();
                        }

                    }
                }
            }
        }

        #endregion
    }
}
