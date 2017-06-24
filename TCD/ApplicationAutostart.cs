using System;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32.TaskScheduler;

namespace TCD.Sys.ApplicationExtensions
{
    public class ApplicationAutostart
    {
        private static bool IsAutostart(string applicationOrTaskName)
        {
            TaskService taskService = new TaskService();
            var present = taskService.RootFolder.TaskService.GetTask(applicationOrTaskName);
            if (present != null)
            {
                foreach (var action in present.Definition.Actions)//check if the path is set right
                    if (action.ActionType == TaskActionType.Execute)
                        if ((action as ExecAction).Path == Application.ExecutablePath)
                            return true;
            }
            return false;
        }

        /// <summary>
        /// Returns a bool indicating wheather the application will autorun.
        /// </summary>
        /// <param name="applicationOrTaskName">The string that identifies the task</param>
        /// <returns>True: the app will autorun
        /// False: the app will not autorun, or at least a different path is specified</returns>
        public static Task<bool> IsAutostartAsync(string applicationOrTaskName)
        {
            Task<bool> t = new Task<bool>(delegate
            {
                return IsAutostart(applicationOrTaskName);
            });
            t.Start();
            return t;
        }

        /// <summary>
        /// Enable or disable autorun.
        /// </summary>
        /// <param name="autostart">True: enable, False: disable</param>
        /// <param name="applicationOrTaskName">The string to identify the task</param>
        /// <param name="description">The description of what the task does (autostarting..)</param>
        /// <param name="startupParameters">Optional command line arguments</param>
        /// <param name="elevatedPermissions">Shall the app start with elevated administrative permissions? NOTE: This requires the calling application to have elevated permissions!!</param>
        /// <returns>True: was successful, False: failed</returns>
        public static Task<bool> SetAutostartAsync(bool autostart, string applicationOrTaskName, string description, string startupParameters = "", bool elevatedPermissions = false)
        {
            Task<bool> t = new Task<bool>(delegate
            {
                TaskService taskService = new TaskService();
                try
                {
                    Version ver = taskService.HighestSupportedVersion;
                    bool newVer = (ver >= new Version(1, 2));
                    string user = WindowsIdentity.GetCurrent().Name;

                    bool present = IsAutostart(applicationOrTaskName);

                    if (autostart && !present)
                    {//create task
                        TaskDefinition td = taskService.NewTask();
                        td.RegistrationInfo.Description = description;
                        td.RegistrationInfo.Author = user;
                        td.Principal.LogonType = TaskLogonType.InteractiveToken;
                        if (elevatedPermissions)
                        {
                            td.Principal.RunLevel = TaskRunLevel.Highest;
                            td.Principal.UserId = user;
                        }
                        LogonTrigger lTrigger = (LogonTrigger)td.Triggers.Add(new LogonTrigger());
                        if (newVer)
                            lTrigger.UserId = user;
                        // Add an action that will launch Notepad whenever the trigger fires
                        td.Actions.Add(new ExecAction(Application.ExecutablePath, startupParameters, Application.StartupPath));
                        taskService.RootFolder.RegisterTaskDefinition(applicationOrTaskName, td);
                    }
                    else if (!autostart && present)
                    {//delete task
                        taskService.RootFolder.DeleteTask(applicationOrTaskName);
                    }
                    return true;
                }
                catch
                {
                    return false;
                }
            });
            t.Start();
            return t;
        }
    }
}
