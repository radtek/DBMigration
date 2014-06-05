using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using PLX.DBMigration.Accessors;
using System.Threading.Tasks;

namespace PLX.DBMigration.Managers
{
    /// <summary>
    /// Export Manager is responsible for executing all of the steps involved in creating an upgrade export
    /// </summary>
    public class ExportManager
    {        
        /// <summary>
        /// Constructor
        /// </summary>
        public ExportManager()
        {
        }

        #region Methods

        /***********************************************************************/
        // Methods
        /***********************************************************************/

        /// <summary>
        /// Run each export step.
        /// </summary>
        public void RunExport()
        {
            // New way makes these obsolete? //
            //UpdateDatabaseGit();
            //RunParseFiles();
            
            // Start from rt_upgrade_command table fully populated //
            RunClrDll();
            CreateDmpFile();
        }

        /// <summary>
        /// Update Git repo to get latest version of db code.
        /// </summary>
        public void UpdateDatabaseGit()
        {
            try
            {
                Console.WriteLine("Get the latest code from git.exe.");
                XmlAccessor xClass = new XmlAccessor();
                xClass.ReadXmlFile();

                if (File.Exists(xClass.XmlValues["GitLocation"]))
                {
                    ProcessStartInfo pInfo = new ProcessStartInfo();
                    pInfo.FileName = xClass.XmlValues["GitLocation"];
                    pInfo.Arguments = "pull";
                    pInfo.WorkingDirectory = xClass.XmlValues["RepoLocation"];

                    Process p = new Process();
                    p.StartInfo = pInfo;
                    p.Start();
                    p.WaitForExit();
                    p.Close();

                    //pInfo.UseShellExecute = false;
                    //pInfo.CreateNoWindow = true;
                    //pInfo.RedirectStandardError = true;
                    //pInfo.RedirectStandardOutput = true;

                    //Process p = new Process();
                    //p.StartInfo = pInfo;
                    //p.OutputDataReceived += new DataReceivedEventHandler(Display);
                    //p.ErrorDataReceived += new DataReceivedEventHandler(Display);
                    //p.Start();
                    //p.BeginOutputReadLine();
                    //p.BeginErrorReadLine();

                    //p.WaitForExit();
                    //p.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Parse each db code file.
        /// </summary>
        public void RunParseFiles()
        {
            try
            {
                ParseFileManager pfm = new ParseFileManager();
                pfm.ReadFilesToInsert("Structure");
                pfm.ReadFilesToInsert("Code");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Make sure XmlDefault.xml is setup correctly.");
            }
        }

        /// <summary>
        /// Run lynx.store_clr_dlls.
        /// </summary>
        public void RunClrDll()
        {
            try
            {
                Console.WriteLine("Running lynx.store_clr_dlls ...");

                OracleAccessor oc = new OracleAccessor();
                oc.ConnectToOracle();
                oc.Transaction = oc.Connection.BeginTransaction();

                OracleProcedureInput pic = new OracleProcedureInput();
                pic.numberOfParameters = 0;
                pic.procedureName = "lynx.store_clr_dlls";
                oc.NonQueryProcedure(pic);

                oc.Transaction.Commit();
                oc.Connection.Close();

                Console.WriteLine("Finished lynx.store_clr_dlls.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("lynx.store_clr_dlls Failed!");
            }
        }

        /// <summary>
        /// Create the dump file using expdp.exe.
        /// </summary>
        public void CreateDmpFile()
        {
            try
            {
                Console.WriteLine("Create an Export dmp file...");

                string startLocation = "";
                string currentEdition = "";
                string nextEdition = "";
                string oracleLocation = "";

                // Get locations from XML File //
                XmlAccessor xClass = new XmlAccessor();
                xClass.ReadXmlFile();
                startLocation = xClass.XmlValues["StartLocation"];
                oracleLocation = xClass.XmlValues["OracleLocation"];

                /*
                // Get Current and Next Edition from text file //
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
                 */
                currentEdition = "E07";
                nextEdition = "E08";
                string fileName = "upg_" + currentEdition + "_" + nextEdition;

                // Update upg_exp.txt // 
                string[] upgradeParmText = { "DUMPFILE=" + fileName + ".dmp",
                                             "LOGFILE=" + fileName + ".log",
                                             "REUSE_DUMPFILES=YES",
                                             "SCHEMAS=LYNX",
                                             "INCLUDE=SEQUENCE:\"IN ('UPGRADE_COMMAND_SEQ','UPGRADE_MIGRATION_JOB_SEQ')\"",
                                             "INCLUDE=TABLE:\"IN ('RT_NAMESPACE_TYPE','RT_UPGRADE','RT_UPGRADE_COMMAND','RT_UPGRADE_MIGRATION_JOB','RT_UPGRADE_HOST_SCRIPT','RT_CLR_ASSEMBLIES')\"",
                                             "INCLUDE=PACKAGE:\"IN ('UPGRADE_PKG')\""
                                           };

                File.WriteAllLines("upg_exp.txt", upgradeParmText);

                // Get Current Directory //
                string upgFile = Directory.GetCurrentDirectory() + "\\upg_exp.txt";

                ProcessStartInfo pInfo = new ProcessStartInfo();
                pInfo.FileName = "expdp.exe";
                pInfo.WorkingDirectory = oracleLocation;
                //pInfo.Arguments = "lynx@beta/dang3r DUMPFILE=" + fileName + ".dmp LOGFILE=" + fileName + ".log REUSE_DUMPFILES=YES";
                pInfo.Arguments = "lynx_dev@pldb/f3line parfile=" + upgFile;
                pInfo.UseShellExecute = false;
                pInfo.CreateNoWindow = true;
                pInfo.RedirectStandardError = true;
                pInfo.RedirectStandardOutput = true;
                System.Environment.SetEnvironmentVariable("ORACLE_HOME", "C:\\app\\oracle\\product\\11.2.0\\dbhome_2", EnvironmentVariableTarget.Process);
                System.Environment.SetEnvironmentVariable("ORACLE_UNQNAME", "PLDB", EnvironmentVariableTarget.Process);

                Process p = new Process();
                p.StartInfo = pInfo;
                p.OutputDataReceived += new DataReceivedEventHandler(Display);
                p.ErrorDataReceived += new DataReceivedEventHandler(Display);
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                p.WaitForExit();
                p.Close();

                Console.WriteLine("Export dmp file has been created!");

                // dmp file is created in this location //
                // C:\app\oracle\admin\PLDB\dpdump
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Create dmp file Failed!");
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

        #endregion
    }
}
