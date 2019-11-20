using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProductStudio;

namespace PSHelper
{
    public class PSBugFiler
    {
        //Details
        //http://bgit/applications/help/productstudiosdk/default.asp?URL=PSSDK_Main.htm

        public static int FileABug(string title, string reproSteps, string expectedResult, string actualResult, string attachmentFolder)
        {
            int bugID = 0;
            bool hasInvalidField = false;
            Directory psDirectory = null;
            Product psProduct = null;
            Datastore psDataStore = null;
            Fields psFields = null;
            DatastoreItemList psDataList = null;
            DatastoreItem psDataItem = null;
            Bug psBug = null;

            // Specify the product database to use and the domain in which
            // the database is located.
            string strProductName = "Office15";
            string strDomain1 = "fareast.corp.microsoft.com";
            string strDomain2 = "redmond.corp.microsoft.com";
            string strDomain3 = "corp.microsoft.com";

            try
            {
                //
                // Connect to the directory with your current domain under your credentials .
                //
                psDirectory = new Directory();

                try
                {
                    //first try with fareast
                    psDirectory.Connect(strDomain1, "", "");
                }
                catch (Exception e1)
                {
                    Console.WriteLine("Fareast Connect Error: {0}", e1.Message);

                    try
                    {
                        //if fareast fails, try redmond
                        psDirectory.Connect(strDomain2, "", "");
                    }
                    catch (Exception e2)
                    {
                        Console.WriteLine("Redmond Connect Error: {0}", e2.Message);

                        //if both fareast & redmond fail, try global domain
                        psDirectory.Connect(strDomain3, "", "");
                    }
                }

                psProduct = psDirectory.GetProductByName(strProductName);
                psDataStore = psProduct.Connect("", "", "");

                //
                // Bind the query and Datastore to our DatastoreItemList.
                //
                psDataList = new DatastoreItemList();
                psDataList.Datastore = psDataStore;

                //
                // Create a blank bug
                //
                psDataList.CreateBlank(PsDatastoreItemTypeEnum.psDatastoreItemTypeBugs);
                psDataItem = psDataList.DatastoreItems.Add(null, PsApplyRulesMask.psApplyRulesAll);
                psBug = psDataItem as Bug;

                //
                // Set fields for the new bug
                //
                psFields = psBug.Fields;

                psFields["Title"].Value = "[LWA]" + title;
                psFields["TreeID"].Value = TreeIDFromPath(psDataStore.RootNode, "Current\\Lync Client\\Lync Web App");
                psFields["Assigned to"].Value = "Active";
                psFields["Severity"].Value = 2;
                psFields["Priority"].Value = 2;
                psFields["Open Build"].Value = "5.0.0000.0000";
                psFields["Ship Cycle"].Value = "O15 Main Wave";
                psFields["Fix By"].Value = "Beta1Refresh";

                psFields["Repro Steps"].Value =
                    "Repro Steps:" + Environment.NewLine +
                    "============" + Environment.NewLine +
                    (string.IsNullOrEmpty(reproSteps) ? "1. Sign in as User A from LWA" : reproSteps) + Environment.NewLine +
                    Environment.NewLine +
                    "Actual Results:" + Environment.NewLine +
                    "===============" + Environment.NewLine +
                    (string.IsNullOrEmpty(actualResult) ? "1. " : actualResult) + Environment.NewLine +
                    Environment.NewLine +
                    "Expected Results:" + Environment.NewLine +
                    "=================" + Environment.NewLine +
                    (string.IsNullOrEmpty(expectedResult) ? "1. " : expectedResult) + Environment.NewLine;

                //
                //  Let's make sure all fields are valid before saving
                //
                foreach (ProductStudio.Field psField in psBug.Fields)
                {
                    if (psField.Validity != PsFieldStatusEnum.psFieldStatusValid)
                    {
                        hasInvalidField = true;
                        Console.WriteLine("Invalid Field '{0}': {1}", psField.Name, psField.Validity.ToString());
                        Console.WriteLine("Current Value: '{0}'", psField.Value);
                        Console.WriteLine();
                    }
                }

                if (hasInvalidField)
                {
                    throw (new ApplicationException("Invalid Field(s) were found.  Could not create."));
                }
                else
                {
                    if (!string.IsNullOrEmpty(attachmentFolder))
                    {
                        string[] files = System.IO.Directory.GetFiles(attachmentFolder);
                        foreach (string file in files)
                            psBug.Files.Add(file, false);
                    }

                    psBug.Save(true);
                    bugID = Convert.ToInt32(psFields["ID"].Value);
                    Console.WriteLine("Bug #{0} Successfully Created.", bugID);
                }
            }
            catch (Exception e3)
            {
                Console.WriteLine("Error: {0}", e3.Message);
            }
            finally
            {
                if (null != psDirectory)
                {
                    try
                    {
                        psDirectory.Disconnect();
                    }
                    catch (Exception e4)
                    {
                        Console.WriteLine("Disconnect Error: {0}", e4.Message);
                    }
                }
            }

            return bugID;
        }

        //
        // Finds the ID from the Product Path.
        //
        internal static int TreeIDFromPath(Node node, string path)
        {
            string[] pathLevelNames;
            char[] separator = { '\\' };
            int pathCount = 0;
            Node currentNode = null;

            pathLevelNames = path.Split(separator);

            currentNode = node;

            if (path.Trim().Length >= 1 && path.Trim() != "\\")
            {
                for (pathCount = 0; pathCount < pathLevelNames.Length; pathCount++)
                {
                    currentNode = currentNode.Nodes[pathLevelNames[pathCount]];
                }
            }

            return currentNode.ID;
        }
    }
}
