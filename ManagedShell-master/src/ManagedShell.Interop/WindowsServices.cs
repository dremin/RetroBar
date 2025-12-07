/*
 * Created by SharpDevelop.
 * User: James
 * Date: 1/3/2009
 * Time: 7:03 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.ServiceProcess;

namespace ManagedShell.Interop
{
    /// <summary>
    /// Basic class for querying the status of Windows Services.
    /// Essentially a wrapper for the ServiceController class.
    /// </summary>
    public class WindowsServices
    {
        private WindowsServices()
        {
        }
        
        /// <summary>
        /// Gets the status of the specified service.
        /// </summary>
        /// <param name="serviceName">Short name of the service to query.</param>
        /// <returns>NotInstalled if the service does not exist on the system.
        /// RetrievalError if it could not get the status of the service.
        /// Running if the service is fully up and running.
        /// Otherwise, NotRunning</returns>
        public static ServiceStatus QueryStatus(string serviceName) {
            ServiceController controller = new ServiceController(serviceName);
            try {
                if (controller.Status == ServiceControllerStatus.Running) {
                    return ServiceStatus.Running;
                } else {
                    return ServiceStatus.NotRunning;
                }
            } catch (InvalidOperationException) {
                return ServiceStatus.NotInstalled;
            } catch {
                return ServiceStatus.RetrievalError;
            }
        }
    }
    
    /// <summary>
    /// Simple Windows service status enumeration.
    /// </summary>
    public enum ServiceStatus {
        Running,
        NotRunning,
        NotInstalled,
        RetrievalError
    }
}
