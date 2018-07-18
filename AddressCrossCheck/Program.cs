using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.esriSystem;
using FileStream = System.IO.FileStream;
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.ConversionTools;

namespace AddressCrossCheck
{
    class Program
    {
        private static LicenseInitializer m_AOLicenseInitializer = new AddressCrossCheck.LicenseInitializer();
        private static string jurisToCheck = "";
        private static string textFilePathName = "";
        private static string countyOrAddressSystem = "";  // AddSystem OR CountyID
        private static IWorkspace workspaceFGDB;
        private static string jurisNameNoSpace;
        private static IFeatureClass addressPointsSGID;

    
        [STAThread()]
        static void Main(string[] args) // args[0] = AddressSystem or CountyID; args[1] = jurisdiction; args[2] = validation search distance (sample parameters: AddSystem LOA 295    __or__  CountyID 49031 295)
        {
            // Set commndline args.
            countyOrAddressSystem = args[0];  // AddSystem OR CountyID

            if (countyOrAddressSystem == "AddSystem" || countyOrAddressSystem == "CountyID")
            {
                //proceed as normal
            }
            else
            {
                // message/log parameter not valid      
                Console.WriteLine(args[0].ToString() + " was not recognized recognized as a valid argument for the first parameter.  Valid args include: AddSystem -OR- CountyID");
                Console.ReadLine();
                return;
            }

            jurisToCheck = args[1];
            int validationSearchDistance = Convert.ToInt32(args[2]); // 240;
            const int sqlQuerySearchDistance = 300;
            
            var intIttrID = 0;

            //ESRI License Initializer generated code.
            m_AOLicenseInitializer.InitializeApplication(new esriLicenseProductCode[] { esriLicenseProductCode.esriLicenseProductCodeAdvanced },
            new esriLicenseExtensionCode[] { });
            //ESRI License Initializer generated code.
            //Do not make any call to ArcObjects after ShutDownApplication()
            m_AOLicenseInitializer.ShutdownApplication();

            try
            {
                // Get access to the date and time for the text file name.
                var strYearMonthDayHourMin = DateTime.Now.ToString("-yyyy-MM-dd-HH-mm");

                // Setup a file stream and a stream writer to write out the road segments that get predirs from address points.
                // Check if c:\temp exists.
                if (!(File.Exists(@"C:\temp")))
                {
                    // It doesn't exist, so create the directory.  
                    Directory.CreateDirectory(@"C:\temp");
                }

                // Set up the file stream for writing to text file.
                textFilePathName = @"C:\temp\AddressXchk" + jurisToCheck + "-" + strYearMonthDayHourMin + ".txt";
                var fileStreamAddr = new FileStream(textFilePathName, FileMode.Create);
                var streamWriterAddr = new StreamWriter(fileStreamAddr);
                // write the first line of the text file - this is the field headings
                streamWriterAddr.WriteLine("ITTR_ID" + "," + "ADDR_UID" + "," + "ROAD_UID" + "," + "DIST_TO_ROAD" + "," + "PREDIR_ISS" + "," + "POSTTYPE_ISS" + "," + "POSTDIR_ISS" + "," + "ADDR_RANGE_ISS" + "," + "NOT_FOUND" + "," + "DIST_ISS");

                //var intIttrID = 0;
                //const int sqlQuerySearchDistance = 250;
                //const int validationSearchDistance = 240;

                // connect to sgid
                // Get the connection string from the current machine's env variables
                var connString = Environment.GetEnvironmentVariable("SGID_SQL_Conn", EnvironmentVariableTarget.User).ToString();
                // example of connString = "Data Source=ServerUrlAddress;Initial Catalog=DatabaseName;User ID=DatabaseUserID;Password=DatabaseUserPassword"

                // Connect to the database.
                using (var sgidConnectionAddressPoints = new SqlConnection(connString))
                using (var sgidConnectionRoads = new SqlConnection(connString))
                {
                    //open the sql connections
                    sgidConnectionAddressPoints.Open();
                    sgidConnectionRoads.Open();

                    // Query Address Points Data
                    using (var addressCommand = new SqlCommand(@"select OBJECTID, StreetName, AddNum,AddNumSuffix,PrefixDir,StreetType,SuffixDir,UTAddPtID from Location.ADDRESSPOINTS where " + countyOrAddressSystem + @" = '" + jurisToCheck + @"'", sgidConnectionAddressPoints))
                    {
                        // Create an address point data reader.
                        using (SqlDataReader addressReader = addressCommand.ExecuteReader())
                        {
                            if (addressReader.HasRows)
                            {
                                while (addressReader.Read())
                                {
                                    // Get the current address point's OID and StreetName
                                    var addressPointOID = addressReader[0];
                                    var addressPointStreetName = addressReader[1];


                                    // Query the Roads data to check for the nearest road based on the current address point
                                    using (var roadsCommand = new SqlCommand(@"DECLARE @g geometry = (select Shape from location.ADDRESSPOINTS where OBJECTID = " + addressPointOID + @");
                                    SELECT TOP(2) Shape.STDistance(@g) as DISTANCE, FROMADDR_L, TOADDR_L, FROMADDR_R, TOADDR_R, OBJECTID, PREDIR, POSTTYPE, POSTDIR, UNIQUE_ID  FROM Transportation.ROADS
                                    WHERE Shape.STDistance(@g) is not null and Shape.STDistance(@g) < " + sqlQuerySearchDistance + @" and NAME = '" + addressPointStreetName + @"'
                                    ORDER BY Shape.STDistance(@g)", sgidConnectionRoads))

                                        // Create a roads data reader
                                    using (SqlDataReader roadsReader = roadsCommand.ExecuteReader())
                                    {
                                        var distIssue = false;
                                        var predirIssue = false;
                                        var posttypeIssue = false;
                                        var postdirIssue = false;
                                        var addrRangeIssue = false;
                                        var notFound = false;
                                        double distFoundToNearestRoad = 0;
                                        string roadOID = "";

                                        // Create variables to preserve the first record found... if also checking the second record, too.
                                        var distIssue_1st = false;
                                        var predirIssue_1st = false;
                                        var posttypeIssue_1st = false;
                                        var postdirIssue_1st = false;
                                        var addrRangeIssue_1st = false;
                                        double distFoundToNearestRoad_1st = 0;
                                        string roadOID_1st = "";

                                        var currentRoadRecord = 0;

                                        // Check if any nearby roads were found.
                                        if (roadsReader.HasRows)
                                        {
                                            // Itterate through them.
                                            while (roadsReader.Read())
                                            {
                                                // Reset these to false, in case it's the 2nd road record.
                                                distIssue = false;
                                                predirIssue = false;
                                                posttypeIssue = false;
                                                postdirIssue = false;
                                                addrRangeIssue = false;
                                                notFound = false;

                                                // Capture the number of the road record we are checking.
                                                currentRoadRecord = currentRoadRecord + 1;

                                                // get the roadoid and distance to the nearest road seg
                                                distFoundToNearestRoad = Math.Ceiling(Convert.ToDouble(roadsReader[0]));
                                                roadOID = roadsReader[9].ToString();


                                                // Check Distance
                                                if (Convert.ToInt32(roadsReader.GetValue(roadsReader.GetOrdinal("DISTANCE"))) > validationSearchDistance)
                                                {
                                                    distIssue = true;
                                                }

                                                // Check Predir
                                                if (roadsReader.GetValue(roadsReader.GetOrdinal("PREDIR")).ToString().ToUpper() != addressReader[4].ToString().ToUpper())
                                                {
                                                    predirIssue = true;
                                                }

                                                // Check PostType
                                                if (roadsReader.GetValue(roadsReader.GetOrdinal("POSTTYPE")).ToString().ToUpper() != addressReader[5].ToString().ToUpper())
                                                {
                                                    posttypeIssue = true;
                                                }

                                                // Check PostDir
                                                if (roadsReader.GetValue(roadsReader.GetOrdinal("POSTDIR")).ToString().ToUpper() != addressReader[6].ToString().ToUpper())
                                                {
                                                    postdirIssue = true;
                                                }

                                                // Check if within Range
                                                // Get lowest and highest values
                                                var lowestVal = 0;
                                                var highestVal = 0;
                                                if (Convert.ToInt32(roadsReader.GetValue(roadsReader.GetOrdinal("FROMADDR_L"))) < Convert.ToInt32(roadsReader.GetValue(roadsReader.GetOrdinal("FROMADDR_R"))))
                                                {
                                                    lowestVal = Convert.ToInt32(
                                                        roadsReader.GetValue(roadsReader.GetOrdinal("FROMADDR_L")));
                                                }
                                                else
                                                {
                                                    lowestVal = Convert.ToInt32(
                                                        roadsReader.GetValue(roadsReader.GetOrdinal("FROMADDR_R")));
                                                }
                                                if (Convert.ToInt32(roadsReader.GetValue(roadsReader.GetOrdinal("TOADDR_L"))) > Convert.ToInt32(roadsReader.GetValue(roadsReader.GetOrdinal("TOADDR_R"))))
                                                {
                                                    highestVal = Convert.ToInt32(
                                                        roadsReader.GetValue(roadsReader.GetOrdinal("TOADDR_L")));
                                                }
                                                else
                                                {
                                                    highestVal = Convert.ToInt32(
                                                        roadsReader.GetValue(roadsReader.GetOrdinal("TOADDR_R")));
                                                }
                                                if (Convert.ToInt32(addressReader[2]) < lowestVal || Convert.ToInt32(addressReader[2]) > highestVal)
                                                {
                                                    addrRangeIssue = true;
                                                }

                                                // If it's a road range issue, maybe check the next returned record (ie: don't break out) to see if that's the right one.  This could happen along a long road with a lot of breaks.
                                                // If rangeIssue is false, then it's not a range issue so we don't need to check the next found road record - at least for this reason, so break out of "Read" so we don't move to the next one.
                                                if (addrRangeIssue || (predirIssue & posttypeIssue))
                                                {
                                                    // then try the next record
                                                    // Preserve road values from 1st found road.
                                                    distIssue_1st = distIssue;
                                                    predirIssue_1st = postdirIssue;
                                                    posttypeIssue_1st = posttypeIssue;
                                                    postdirIssue_1st = postdirIssue;
                                                    addrRangeIssue_1st = addrRangeIssue;
                                                    distFoundToNearestRoad_1st = distFoundToNearestRoad;
                                                    roadOID_1st = roadOID;
                                                }
                                                else
                                                {
                                                    // it's not one of those issues so don't check the next record - break out of while loop
                                                    break;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // road segment within the specified distance was not found
                                            notFound = true;                                               
                                        }

                                        // Write out the text file log here, after all the needed sql-returned road records have been itterated.
                                        if (distIssue || predirIssue || posttypeIssue || postdirIssue || addrRangeIssue || notFound)
                                        {
                                            // Use the values from the 1st found road record, if there were issues also found with the second found road record
                                            // b/c the issues was most likely with the 1st.  We only checked the second to see if there were not issues, and that maybe we were checking the wrong record at 1st.
                                            if (currentRoadRecord > 1)
                                            {
                                                // Use values from the 1st road record found, as the 2nd also had issues
                                                // "ITTR_ID" + "," + "ADDR_OID" + "," + "ROAD_OID" + "," + "DIST_TO_ROAD" + "," + "PREDIR_ISS" + "," + "POSTTYPE_ISS" + "," + "POSTDIR_ISS" + "," + "ADDR_RANGE_ISS" + "," + "NOT_FOUND" + "," + "DIST_ISS" + "," + "ROAD_RECORD"
                                                streamWriterAddr.WriteLine((intIttrID = intIttrID + 1) + "," + addressReader[7].ToString() + "," + roadOID_1st.ToString() + "," + distFoundToNearestRoad_1st.ToString() + "," + predirIssue_1st.ToString() + "," + posttypeIssue_1st.ToString() + "," + postdirIssue_1st.ToString() + "," + addrRangeIssue_1st.ToString() + "," + notFound.ToString() + "," + distIssue_1st.ToString());
                                                Console.WriteLine(addressReader[7].ToString() + "," + roadOID_1st.ToString() + "," + distFoundToNearestRoad_1st.ToString() + "," + predirIssue_1st.ToString() + "," + posttypeIssue_1st.ToString() + "," + postdirIssue_1st.ToString() + "," + addrRangeIssue_1st.ToString() + "," + notFound.ToString() + "," + distIssue_1st.ToString() + "," + currentRoadRecord + " but used 1st");
                                            }
                                            else
                                            {
                                                // We only need to evaluate the 1st found road record, so just use those values.
                                                // "ITTR_ID" + "," + "ADDR_OID" + "," + "ROAD_OID" + "," + "DIST_TO_ROAD" + "," + "PREDIR_ISS" + "," + "POSTTYPE_ISS" + "," + "POSTDIR_ISS" + "," + "ADDR_RANGE_ISS" + "," + "NOT_FOUND" + "," + "DIST_ISS" + "," + "ROAD_RECORD"
                                                streamWriterAddr.WriteLine((intIttrID = intIttrID + 1) + "," + addressReader[7].ToString() + "," + roadOID + "," + distFoundToNearestRoad.ToString() + "," + predirIssue.ToString() + "," + posttypeIssue.ToString() + "," + postdirIssue.ToString() + "," + addrRangeIssue.ToString() + "," + notFound.ToString() + "," + distIssue.ToString());
                                                Console.WriteLine(addressReader[7].ToString() + "," + roadOID + "," + distFoundToNearestRoad.ToString() + "," + predirIssue.ToString() + "," + posttypeIssue.ToString() + "," + postdirIssue.ToString() + "," + addrRangeIssue.ToString() + "," + notFound.ToString() + "," + distIssue.ToString() + "," + currentRoadRecord);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                //close the stream writer
                streamWriterAddr.Close();

                // Create the file geodatabase and import the data for the address system as well as the text file as a table
                CreateFileGeodatabase();

                Console.WriteLine("Done!");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was an error with the AddressPntsRoadsCrossCheck console application, in the Main method." + ex.Message + " " + ex.Source + " " + ex.InnerException + " " + ex.HResult + " " + ex.StackTrace + " " + ex);
                Console.ReadLine();
            }
        }



        // Create the file geodatabase.
        public static void CreateFileGeodatabase()
        {
            string strFgdPath = @"C:\temp\";
            string strFgdName = @"AddressCrossCheck";

            IWorkspaceName workspaceName = null;
            // Instantiate a file geodatabase workspace factory and create a new file geodatabase.
            // The Create method returns a workspace name object.
            IWorkspaceFactory workspaceFactory = new FileGDBWorkspaceFactoryClass();
            Console.WriteLine("Creating File Geodatabase...");

            // check if file geodatabase exists, before creating it
            if (!(workspaceFactory.IsWorkspace(strFgdPath + strFgdName + ".gdb")))
            {
                workspaceName = workspaceFactory.Create(strFgdPath, strFgdName, null, 0);
            }
            else
            {
                IFileNames arcFileNames = new FileNames();
                arcFileNames.Add(strFgdPath + strFgdName + ".gdb");
                workspaceName = workspaceFactory.GetWorkspaceName(strFgdPath, arcFileNames);
            }

            // Cast the workspace name object to the IName interface and open the workspace.
            var name = (IName)workspaceName;
            workspaceFGDB = (IWorkspace)name.Open();

            // Load the data.
            ImportDataToFGDB();
        }



        // Import the table and feature classes.
        public static void ImportDataToFGDB()
        {
            // Import the text file as a table to the newly-created filegeodatabase.
            // Initialize the geoprocessor. 
            Geoprocessor GP = new Geoprocessor();

            // IMPORT THE TEXT FILE
            // Table to Table Tool.
            ESRI.ArcGIS.ConversionTools.TableToTable importTable = new ESRI.ArcGIS.ConversionTools.TableToTable();
            importTable.in_rows = textFilePathName;
            importTable.out_path = @"C:\temp\AddressCrossCheck.gdb";
            // remove space in juris name
            jurisNameNoSpace = jurisToCheck.Replace(" ", "");
            importTable.out_name = "tbl" + jurisNameNoSpace.Trim() + "_Report";

            // Execute the Table to Table Tool
            Console.WriteLine("Importing Text File as Table...");
            GP.Execute(importTable, null);  // if you get an error on this line it might be b/c there already a table in the fgdb with the same name.


            // Import the feature classes (address points and roads).
            // IMPORT THE ADDRESS POINTS
            
            // Re-initialize the geoprocessor
            GP = new Geoprocessor();

            // Connect to sde sgid
            var factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.SdeWorkspaceFactory");
            var workspaceFactory2 = (IWorkspaceFactory2)Activator.CreateInstance(factoryType);
            IWorkspace sgid = workspaceFactory2.OpenFromFile(@"C:\Users\gbunce\AppData\Roaming\ESRI\Desktop10.4\ArcCatalog\DC_agrc@SGID10@sgid.agrc.utah.gov.sde", 0);
            var sgidFeatureWorkspace = (IFeatureWorkspace)sgid;
            addressPointsSGID = sgidFeatureWorkspace.OpenFeatureClass("SGID10.LOCATION.AddressPoints");
            var roadsSGID = sgidFeatureWorkspace.OpenFeatureClass("SGID10.TRANSPORTATION.Roads");

            // FeatureClass to Feature Class Tool
            ESRI.ArcGIS.ConversionTools.FeatureClassToFeatureClass importAddrPnts = new ESRI.ArcGIS.ConversionTools.FeatureClassToFeatureClass();
            importAddrPnts.in_features = addressPointsSGID;
            importAddrPnts.where_clause = countyOrAddressSystem + @" = '" + jurisToCheck + @"'";
            importAddrPnts.out_name = "AddrPnts" + jurisNameNoSpace + "_SGID";
            importAddrPnts.out_path = @"C:\temp\AddressCrossCheck.gdb";

            // Execute the Feature Class to Feature Class Tool
            Console.WriteLine("Importing Address Points as Feature Class...");
            GP.Execute(importAddrPnts,null);


            // IMPORT THE ROADS 
            // Re-initialize the geoprocessor
            GP = new Geoprocessor();

            // FeatureClass to Feature Class Tool
            ESRI.ArcGIS.ConversionTools.FeatureClassToFeatureClass importRoads = new ESRI.ArcGIS.ConversionTools.FeatureClassToFeatureClass();
            importRoads.in_features = roadsSGID;
            string roadsFieldType = "";
            if (countyOrAddressSystem == "AddSystem")
            {
                roadsFieldType = "ADDRSYS";
            }
            else
            {
                roadsFieldType = "COUNTY";
            }
            importRoads.where_clause = roadsFieldType + @"_L = '" + jurisToCheck + @"' or " + roadsFieldType + @"_R = '" + jurisToCheck + @"'";
            importRoads.out_name = "Roads" + jurisNameNoSpace + "_SGID";
            importRoads.out_path = @"C:\temp\AddressCrossCheck.gdb";

            // Execute the Feature Class to Feature Class Tool
            Console.WriteLine("Importing Roads as Feature Class...");
            GP.Execute(importRoads, null);

            // Close the SDE connection (workspace).
            

            // Join the table to the feature class
            JoinTheTableToFeatureClass("AddrPoints");
            JoinTheTableToFeatureClass("Roads");

        }



        // Join the data.
        private static void JoinTheTableToFeatureClass(string dataToJoin)
        {
            string featureClassName = "";
            string featureClassJoinFieldName = "";
            string tableNameJoinFieldName = "";

            // table name.
            if (dataToJoin == "AddrPoints")
            {
                featureClassName = "AddrPnts" + jurisNameNoSpace + "_SGID";
                featureClassJoinFieldName = ".UTAddPtID";
                tableNameJoinFieldName = ".ADDR_UID";

            }
            else if (dataToJoin == "Roads")
            {
                featureClassName = "Roads" + jurisNameNoSpace + "_SGID";
                featureClassJoinFieldName = ".UNIQUE_ID";
                tableNameJoinFieldName = ".ROAD_UID";
            }

            string tableName = "tbl" + jurisNameNoSpace.Trim() + "_Report";
            string queryDefTables = tableName + "," + featureClassName;
            string queryDefWhereClause = tableName + tableNameJoinFieldName + " = " + featureClassName + featureClassJoinFieldName;

            IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspaceFGDB;
            //create query definition
            IQueryDef queryDef = featureWorkspace.CreateQueryDef();
            //provide list of tables to join
            //queryDef.Tables = "ROCKVILLE_Report, AddrPntsROCKVILLE_SGID";
            queryDef.Tables = queryDefTables;
            //retrieve the fields from all tables
            //queryDef.SubFields = "sde.datesjoin.dt_field, sde.dudates.dt_field";
            //set up join
            //queryDef.WhereClause = "ROCKVILLE_Report.ADDR_UID = AddrPntsROCKVILLE_SGID.UTAddPtID";
            queryDef.WhereClause = queryDefWhereClause;


            //Create FeatureDataset. Note the use of .OpenFeatureQuery.
            //The name "MyJoin" is the name of the restult of the query def and
            //is used in place of a feature class name.
            IFeatureDataset featureDataset = featureWorkspace.OpenFeatureQuery("MyJoin", queryDef);
            //open layer to test against
            IFeatureClassContainer featureClassContainer = (IFeatureClassContainer)featureDataset;
            IFeatureClass featureClass = featureClassContainer.get_ClassByName("MyJoin");

            // Export the joined feature class to the file geodatabase (at this point, it's just in memory)
            ExportTheJoinedFeatureClass(featureClass, dataToJoin);
        }



        // Export the joined data. 
        private static void ExportTheJoinedFeatureClass(IFeatureClass featClass, string dataSetName)
        {
            Geoprocessor GP = new Geoprocessor();

            // FeatureClass to Feature Class Tool
            ESRI.ArcGIS.ConversionTools.FeatureClassToFeatureClass importRoads = new ESRI.ArcGIS.ConversionTools.FeatureClassToFeatureClass();
            importRoads.in_features = featClass;
            importRoads.out_name = dataSetName + jurisNameNoSpace + "_ToCheck";
            importRoads.out_path = @"C:\temp\AddressCrossCheck.gdb";

            // Execute the Feature Class to Feature Class Tool
            Console.WriteLine("Exporting the Joined " + dataSetName + "...");
            GP.Execute(importRoads, null);

            // Back to Main function.
        }
    }
}
