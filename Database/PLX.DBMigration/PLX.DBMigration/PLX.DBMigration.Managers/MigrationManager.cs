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
    public class MigrationManager
    {
        #region Properties

        /***********************************************************************/
        // Properties
        /***********************************************************************/

        private string currentEdition;
        public string CurrentEdition
        {
            get { return currentEdition; }
            set { currentEdition = value; }
        }

        private string nextEdition;
        public string NextEdition
        {
            get { return nextEdition; }
            set { nextEdition = value; }
        }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public MigrationManager()
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
            // Start from rt_upgrade_command table fully populated //
            RunClrDll();
            Console.ForegroundColor = ConsoleColor.Yellow;
            string dumpName = CreateDmpFile();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Run_CopyDumpFile(dumpName);
            Console.ForegroundColor = ConsoleColor.Red;
            Run_RunUnpack(dumpName);
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Run_RunUpgrade();
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
        public string CreateDmpFile()
        {
            try
            {
                Console.WriteLine("Create an Export dmp file...");

                // Get values from XML File and args //
                XmlAccessor xClass = new XmlAccessor();
                xClass.ReadXmlFile();
                string oracleLocation = xClass.XmlValues["OracleLocation"];
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
                return fileName;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Create dmp file Failed!");
                return "";
            }
        }

        /// <summary>
        /// Run CopyDumpFile.bat.
        /// </summary>
        /// <param name="dumpName"></param>
        public void Run_CopyDumpFile(string dumpName)
        {
            Console.WriteLine("Starting CopyDumpFile.bat...");
            
            ProcessStartInfo pInfo = new ProcessStartInfo();
            pInfo.FileName = "CopyDumpFile.bat";
            pInfo.Arguments = dumpName;
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

            Console.WriteLine(dumpName + ".dmp has been copied!");
        }

        /// <summary>
        /// Run RunUnpack.bat.
        /// </summary>
        /// <param name="dumpName"></param>
        public void Run_RunUnpack(string dumpName)
        {
            Console.WriteLine("Starting RunUnpack.bat...");
            
            ProcessStartInfo pInfo = new ProcessStartInfo();
            pInfo.FileName = "RunUnpack.bat";
            pInfo.Arguments = dumpName;
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

            Console.WriteLine("Files were unpacked!");
        }

        /// <summary>
        /// Run RunUpgrade.bat.
        /// </summary>
        public void Run_RunUpgrade()
        {
            Console.WriteLine("Starting RunUpgrade.bat...");

            ProcessStartInfo pInfo = new ProcessStartInfo();
            pInfo.FileName = "RunUpgrade.bat";
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

            Console.WriteLine("Upgrade is finished!");
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
