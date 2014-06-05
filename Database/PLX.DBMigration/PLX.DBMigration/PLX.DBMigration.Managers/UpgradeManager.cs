using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using PLX.DBMigration.Accessors;

namespace PLX.DBMigration.Managers
{
    /// <summary>
    /// Upgrade Manager is responsible for executing all of the steps involved in upgrading a database
    /// </summary>
    public class UpgradeManager
    {
        private string startLocation = "";
        private string currentEdition = "";
        private string nextEdition = "";
        private string oracleLocation = "";
        
        /// <summary>
        /// Constructor
        /// </summary>
        public UpgradeManager()
        {
        }

        #region Methods

        /***********************************************************************/
        // Methods
        /***********************************************************************/
        
        /// <summary>
        /// Run each Upgrade step.
        /// </summary>
        public void RunUpgrade()
        {
            // Run 1_UNPACK.bat //

            // Run 2_Run_UPGRADE.bat //

            // Finalize.bat will be in finalize
            
            /*
            GetConnections();
            RevertDbToCurrentEdition();
            RunDropPriorUpgrade();
            ImportDumpFile();
            ExecuteDeployClrDll();
            RunDoGrantsOnNew();
            RunQualifyDatabaseAndUnlock();
            RunUpgradeCommands();
             */ 
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
        /// Revert the DB back to original edition.
        /// </summary>
        public void RevertDbToCurrentEdition()
        {
            // Somehow return database to previous state // 
            // Hope a scheduled task with vm could do this.
        }

        /// <summary>
        /// Open 10_drop_prior_upgrade_objects.pdc and Parse and run each line.
        /// </summary>
        public void RunDropPriorUpgrade()
        {
            string dropLocation = startLocation + "\\10_Structure\\" + currentEdition + "\\05_UPGRADE\\10_drop_prior_upgrade_objects.pdc";

            if (File.Exists(dropLocation))
            {
                string readLine;
                OracleAccessor oc = new OracleAccessor();
                oc.ConnectToOracle();
                oc.Transaction = oc.Connection.BeginTransaction();

                using (StreamReader dpuFile = new StreamReader(dropLocation))
                {
                    while ((readLine = dpuFile.ReadLine()) != null)
                    {
                        if (readLine != "commit;")
                        {
                            readLine = readLine.Replace(";", "");
                            oc.NonQueryText(readLine);
                        }
                    }
                }

                oc.Transaction.Commit();
                oc.Connection.Close();
            }
        }

        /// <summary>
        /// Import the dmp file on the VM.
        /// </summary>
        public void ImportDumpFile()
        {
            try
            {
                Console.WriteLine("Import dmp file...");

                string fileName = "upg_" + currentEdition + "_" + nextEdition;
                ProcessStartInfo pInfo = new ProcessStartInfo();
                pInfo.FileName = "impdp.exe";
                pInfo.WorkingDirectory = @"C:\app\ptmp";
                //pInfo.Arguments = "lynx@beta/dang3r DUMPFILE=" + fileName + ".dmp";
                pInfo.Arguments = "lynx@beta/dang3r DUMPFILE=" + fileName + ".dmp" + " LOGFILE=" + fileName + ".log TABLE_EXISTS_ACTION=REPLACE";

                pInfo.UseShellExecute = false;
                pInfo.CreateNoWindow = true;
                pInfo.RedirectStandardError = true;
                pInfo.RedirectStandardOutput = true;

                Process p = new Process();
                p.StartInfo = pInfo;
                p.OutputDataReceived += new DataReceivedEventHandler(Display);
                p.ErrorDataReceived += new DataReceivedEventHandler(Display);
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                p.WaitForExit();
                p.Close();

                Console.WriteLine("Import dmp file has ran!");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Import Dump File Failed!");
            }
        }

        /// <summary>
        /// Display the process output data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        static void Display(object sender, DataReceivedEventArgs args)
        {
            Console.WriteLine(args.Data);
        }

        /// <summary>
        /// Run Procedure lynx.deploy_clr_dlls.
        /// </summary>
        public void ExecuteDeployClrDll()
        {
            try
            {
                Console.WriteLine("Running lynx.deploy_clr_dlls ...");

                OracleAccessor oc = new OracleAccessor();
                oc.ConnectToOracle();
                oc.Transaction = oc.Connection.BeginTransaction();

                OracleProcedureInput pic = new OracleProcedureInput();
                pic.numberOfParameters = 0;
                pic.procedureName = "lynx.deploy_clr_dlls";
                oc.NonQueryProcedure(pic);

                oc.Transaction.Commit();
                oc.Connection.Close();

                Console.WriteLine("Finished lynx.deploy_clr_dlls.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("lynx.deploy_clr_dlls Failed!");
            }
        }

        /// <summary>
        /// Open 30_do_grants_on_new_objects.pdc and Parse and run each line.
        /// </summary>
        public void RunDoGrantsOnNew()
        {
            try
            {
                string grantLocation = startLocation + "\\10_Structure\\" + currentEdition + "\\05_UPGRADE\\30_do_grants_on_new_objects.pdc";

                if (File.Exists(grantLocation))
                {
                    string readLine;
                    OracleAccessor oc = new OracleAccessor();
                    oc.ConnectToOracle();
                    oc.Transaction = oc.Connection.BeginTransaction();

                    using (StreamReader dpuFile = new StreamReader(grantLocation))
                    {
                        while ((readLine = dpuFile.ReadLine()) != null)
                        {
                            readLine = readLine.Replace(";", "");
                            oc.NonQueryText(readLine);
                        }
                    }

                    oc.Transaction.Commit();
                    oc.Connection.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Do Grants Failed!");
            }
        }

        /// <summary>
        /// Open 40_qualify_database_and_unlock_.pdc and Parse and run each line.
        /// </summary>
        public void RunQualifyDatabaseAndUnlock()
        {
            try
            {
                string qualifyLocation = startLocation + "\\10_Structure\\" + currentEdition + "\\05_UPGRADE\\40_qualify_database_and_unlock_accts.pdc";

                if (File.Exists(qualifyLocation))
                {
                    string readLine;
                    OracleAccessor oc = new OracleAccessor();
                    oc.ConnectToOracle();
                    oc.Transaction = oc.Connection.BeginTransaction();

                    using (StreamReader dpuFile = new StreamReader(qualifyLocation))
                    {
                        while ((readLine = dpuFile.ReadLine()) != null)
                        {
                            readLine = readLine.Replace(";", "");
                            if (readLine.IndexOf("execute", StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                readLine = readLine.Remove(0, 8);
                                OracleProcedureInput pic = new OracleProcedureInput();
                                pic.numberOfParameters = 0;
                                pic.procedureName = readLine;
                                oc.NonQueryProcedure(pic);
                            }
                            else
                            {
                                oc.NonQueryText(readLine);
                            }
                        }
                    }

                    oc.Transaction.Commit();
                    oc.Connection.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Run Qualify Failed!");
            }
        }

        /// <summary>
        /// Open 50_run_upgrade.pdc and Parse and run each line.
        /// </summary>
        public void RunUpgradeCommands()
        {
            try
            {
                //string upgradeLocation = StartLocation + "\\10_Structure\\" + CurrentEdition + "\\05_UPGRADE\\50_run_upgrade.pdc";
                string upgradeLocation = startLocation + "\\10_Structure\\" + currentEdition + "\\05_UPGRADE\\50_run_upgrade_original.pdc";

                if (File.Exists(upgradeLocation))
                {
                    string readLine;
                    OracleAccessor oc = new OracleAccessor();

                    using (StreamReader dpuFile = new StreamReader(upgradeLocation))
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
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Run Upgrade Commands Failed!");
            }
        }

        #endregion
    }
}
