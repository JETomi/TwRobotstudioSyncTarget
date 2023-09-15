using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.RapidDomain;
using ABB.Robotics.Math;
using ABB.Robotics.RobotStudio;
using ABB.Robotics.RobotStudio.Controllers;
using ABB.Robotics.RobotStudio.Environment;
using ABB.Robotics.RobotStudio.Stations;
using System;
using System.Collections.Generic;
using System.Text;

namespace TwRobotstudioSyncTarget
{
    public class Class1
    {
        // This is the entry point which will be called when the Add-in is loaded
        public static void AddinMain()
        {
            // Add context menu button to target datatype
            CommandBarContextPopup ctxPopup = UIEnvironment.GetContextMenu(typeof(RsTarget));
            CommandBarControlCollection cbcc = ctxPopup.Controls;
            // Create button object
            CommandBarButton BtnSyncTarget = new CommandBarButton("TwRobotstudioSyncTarget.SyncTarget", "Sync without path");
            BtnSyncTarget.DefaultEnabled = true;
            BtnSyncTarget.Image = Properties.Resources.icon_16;
            BtnSyncTarget.LargeImage = Properties.Resources.icon_32;
            BtnSyncTarget.ExecuteCommand += BtnSyncTarget_ExecuteCommand;
            //Add button to top. 
            cbcc.Insert(0,BtnSyncTarget);
            //
        }

        private static void BtnSyncTarget_ExecuteCommand(object sender, ExecuteCommandEventArgs e)
        {
            // Get selected objects
            ProjectSelection Selected = Selection.SelectedObjects;
            //
            foreach (var item in Selected)
            {
                //Check if selected object is a target
                if (item is RsTarget target)
                {
                    //Set target to synchronized
                    target.RobTarget.Synchronize = true;
                    //Module to store robtarget in. Default should automaticly be from settings in robotstudio. 
                    if (target.RobTarget.ModuleName == "")
                    {
                        target.RobTarget.ModuleName = "Tw_TargetSync";
                    }
                    //Decleare storage type as constant
                    target.RobTarget.StorageType = RapidStorageType.Constant;
                    //Sync target to rapid. 
                    SynchronizeToRapid(target.RobTarget, target.Parent as RsTask);
                }
            }
            Logger.AddMessage("TwRobotstudioSyncTarget: Sync complete");
        }

        // Copied from documentation, adjusted for Target instead of Path. 
        // https://developercenter.robotstudio.com/api/robotstudio/articles/How-To/Rapid/SynchronizeToRapid.html
        private static bool SynchronizeToRapid(RsRobTarget path, RsTask task)
        {
            bool result = false;
            Project.UndoContext.BeginUndoStep("SynchronizeToRapid");
            try
            {
                path.Synchronize = true;
                //Get reference to instance of RsIrc5Controller   
                RsIrc5Controller rsIrc5Controller = (RsIrc5Controller)task.Parent;
                //Get virtual controller instance from RsIrc5Controller instance
                ABB.Robotics.Controllers.Controller controller =
                    new ABB.Robotics.Controllers.Controller(new Guid(rsIrc5Controller.SystemId.ToString()));

                //Request for Mastership from controller 
                //If granted then call SyncPathProcedure instance method of RsTask   
                using (ABB.Robotics.Controllers.Mastership m =
                    ABB.Robotics.Controllers.Mastership.Request(controller.Rapid))
                {
                    try
                    {
                        List<SyncLogMessage> messages = new List<SyncLogMessage>();
                        task.SyncData(path.ModuleName + "/" + path.Name,
                            SyncDirection.ToController,
                            messages);

                    }
                    catch (Exception ex)
                    {
                        Logger.AddMessage(new LogMessage(ex.Message.ToString()));
                        result = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.AddMessage(new LogMessage(ex.Message.ToString()));
                Project.UndoContext.CancelUndoStep(CancelUndoStepType.Rollback);
            }
            finally
            {
                Project.UndoContext.EndUndoStep();
            }

            return result;
        }
    }
}