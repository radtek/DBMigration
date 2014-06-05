using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;
using System.Data;

namespace PLX.DBMigration.Accessors
{ 
    public class OracleAccessor
    {
        #region Properties

        /***********************************************************************/
        // Properties
        /***********************************************************************/

        public OracleConnection Connection = null;
        public OracleTransaction Transaction = null;
        private XmlAccessor xClass;

        private string connectionString;
        public string ConnectionString
        {
            get { return connectionString; }
            set { connectionString = value; }
        }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public OracleAccessor()
        {
            xClass = new XmlAccessor();
            xClass.ReadXmlFile();
        }

        #region Methods

        /***********************************************************************/
        // Methods
        /***********************************************************************/

        /// <summary>
        /// Setup connection based on XML or default values.
        /// </summary>
        public void ConnectionSetup(string UserId, string Password)
        {
            try
            {
                // Store Builder Values //
                OracleConnectionStringBuilder builder = new OracleConnectionStringBuilder();
                builder.UserID = UserId;
                builder.Password = Password;

                builder.MinPoolSize = Convert.ToInt32(xClass.XmlValues["MinPoolSize"]);
                builder.ConnectionLifeTime = Convert.ToInt32(xClass.XmlValues["ConnectionLifeTime"]);
                builder.ConnectionTimeout = Convert.ToInt32(xClass.XmlValues["ConnectionTimeout"]);
                builder.IncrPoolSize = Convert.ToInt32(xClass.XmlValues["IncrPoolSize"]);
                builder.DecrPoolSize = Convert.ToInt32(xClass.XmlValues["DecrPoolSize"]);
                builder.MaxPoolSize = Convert.ToInt32(xClass.XmlValues["MaxPoolSize"]);
                builder.ValidateConnection = Convert.ToBoolean(xClass.XmlValues["ValidateConnection"]);

                builder.DataSource = string.Format("(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL={0})(HOST={1})(PORT={2})))(CONNECT_DATA=(SERVICE_NAME={3})))",
                                                    xClass.XmlValues["Protocol"],
                                                    xClass.XmlValues["Host"],
                                                    xClass.XmlValues["Port"],
                                                    xClass.XmlValues["ServiceName"]);
                connectionString = builder.ConnectionString;
            }
            catch (Exception e)
            {
                Console.WriteLine("Got an exception");
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Connect with default User ID and Password.
        /// </summary>
        public void ConnectToOracle()
        {
            ConnectToOracle(xClass.XmlValues["UserID"], xClass.XmlValues["Password"]);
        }

        /// <summary>
        /// Connect with Database and return connection.
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name="Password"></param>
        /// <param name="Privilege"></param>
        public void ConnectToOracle(string UserId, string Password, string Privilege = "")
        {
            ConnectionSetup(UserId, Password);

            if (Privilege != "")
                connectionString = "DBA Privilege=" + Privilege + ";" + connectionString;

            try
            {
                Connection = new OracleConnection(connectionString);
                Connection.Open();
                Console.WriteLine("Database Connection Success!");
            }
            catch (OracleException e)
            {
                Console.WriteLine(e);

                if (Connection != null)
                {
                    try
                    {
                        Connection.Dispose();
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// For executing sql scripts that are read into the program.
        /// </summary>
        /// <param name="CommandText"></param>
        public void NonQueryText(string CommandText)
        {
            try
            {
                OracleCommand cmd = new OracleCommand();
                cmd.Connection = Connection;
                cmd.CommandText = CommandText;
                cmd.CommandType = CommandType.Text;
                cmd.Transaction = Transaction;
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Transaction.Rollback();
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// For executing oracle procedures.
        /// </summary>
        /// <param name="pic"></param>
        public void NonQueryProcedure(OracleProcedureInput pic)
        {
            try
            {
                OracleCommand cmd = new OracleCommand();
                cmd.Connection = Connection;

                cmd.CommandText = pic.procedureName;
                cmd.CommandType = CommandType.StoredProcedure;

                //cmd.Parameters.Add("i_section_number", 1);
                for (int i = 0; i < pic.numberOfParameters; i++)
                {
                    cmd.Parameters.Add(pic.parameterName[i], pic.parameterValue[i]);
                }

                cmd.Transaction = Transaction;
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Transaction.Rollback();
                Console.WriteLine(e.ToString());
            }
        }

        #endregion
    }
}
