using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace PLX.DBMigration.Accessors
{
    /// <summary>
    /// XmlAccessor is responsible for creating and reading a xml file that is used for parameters being setup.
    /// </summary>
    public class XmlAccessor
    {
        public Dictionary<string, string> XmlValues;

        /// <summary>
        /// Constructor.
        /// </summary>
        public XmlAccessor()
        {
            Initialize();
        }

        #region Methods

        /***********************************************************************/
        // Methods
        /***********************************************************************/

        /// <summary>
        /// Get default values for the xml file.
        /// </summary>
        public void Initialize()
        {
            XmlValues = new Dictionary<string, string>();
            XmlValues.Add("MinPoolSize", "0");
            XmlValues.Add("ConnectionLifeTime", "120");
            XmlValues.Add("ConnectionTimeout", "15");
            XmlValues.Add("IncrPoolSize", "2");
            XmlValues.Add("DecrPoolSize", "2");
            XmlValues.Add("MaxPoolSize", "15");
            XmlValues.Add("ValidateConnection", "true");
            XmlValues.Add("UserID", "lynx");
            XmlValues.Add("Password", "dang3r");
            //XmlValues.Add("Host", "oracledba-beta");
            XmlValues.Add("Host", "oraclesvr2");
            XmlValues.Add("Protocol", "TCP");
            XmlValues.Add("Port", "1521");
            XmlValues.Add("ServiceName", "pldb");
            XmlValues.Add("StartLocation", @"C:\Work\GitHub\github.penlink.com\X\Database\Oracle\Database");
            XmlValues.Add("OracleLocation", @"C:\app\SScribner\product\11.2.0\client_1\BIN");
            XmlValues.Add("GitLocation", @"C:\Program Files (x86)\Git\bin\git.exe");
            XmlValues.Add("RepoLocation", @"C:\Work\GitHub\github.penlink.com\X");
            XmlValues.Add("FunStatusOn", "true");
        }

        /// <summary>
        /// Create a default XML file for testing.
        /// </summary>
        public void CreateDefaultXml()
        {
            try
            {
                // Setup //
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.IndentChars = "  ";
                settings.NewLineChars = "\r\n";
                settings.NewLineHandling = NewLineHandling.Replace;
                XmlWriter xWriter = XmlWriter.Create("XmlDefault.xml", settings);

                // Write XML File //
                Console.WriteLine("Created Default Xml File");
                xWriter.WriteStartElement("PenlinkDbSetup");
                foreach (var d in XmlValues)
                {
                    xWriter.WriteStartElement(d.Key);
                    xWriter.WriteAttributeString(d.Key, d.Value);
                    xWriter.WriteEndElement();
                }
                xWriter.WriteEndElement();
                xWriter.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Create Default XML Failed!");
            }
        }

        /// <summary>
        /// Read XML File and return values.
        /// </summary>
        /// <param name="XmlValues"></param>
        /// <returns></returns>
        public bool ReadXmlFile()
        {
            try
            {
                // Local Variables //
                bool exists = false;

                // Setup //
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.IgnoreWhitespace = true;

                // Read XML File //
                if (File.Exists("XmlDefault.xml"))
                {
                    XmlReader xReader = XmlReader.Create("XmlDefault.xml", settings);

                    if (xReader != null)
                    {
                        exists = true;
                        while (xReader.Read())
                        {
                            if (xReader.NodeType == XmlNodeType.Element)
                            {
                                if (xReader.Name != "PenlinkDbSetup")
                                {
                                    //Console.WriteLine(xReader.Name + " - " + xReader.GetAttribute(xReader.Name));
                                    XmlValues[xReader.Name] = xReader.GetAttribute(xReader.Name);
                                }
                            }
                        }

                        xReader.Close();
                    }
                }

                return exists;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Read XML File Failed!");
                return false;
            }
        }

        #endregion
    }
}
