using System.Net;
using System.Reflection;
using System.Threading;
using System.Web.Http;
using SecurityScanner.Core;
using SecurityScanner.Core.Interfaces;
using SecurityScanner.Data;
using SecurityScanner.Logging;

namespace SecurityScannerAPI.Controllers
{
    public class ScanController : ApiController
    {
        private const string SecurityToken = "test";

        private readonly IOpenVasWorkflow _workFlow;

        private readonly IRepository _repository;
        private readonly IMessenger _messenger;

        private readonly string _targetOpenVasHost;

   

        public ScanController(IRepository repository, IMessenger messenger)
        {
            _repository = repository;
            _messenger = messenger;
            var targetOpenVasHost = this._repository.GetTargetOpenVasHostAddress();
            _workFlow = new OpenVasWorkflow(repository, targetOpenVasHost);
           
        }

        /// <summary>
        /// Invoke this method to see if there are any completed scans. If there are any completed scans, the report will be created in the default format (HTML) and send to the customer's emailAddress.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public bool RunReportsForCompletedTasks(string token)
        {
            //TODO make this better
            if (token != SecurityToken)
                throw new HttpResponseException(HttpStatusCode.NotFound);

            LogHelper.LogInfo(this, MethodBase.GetCurrentMethod().Name, this.RequestContext.Url.Request.RequestUri.OriginalString);
            var backgroundWorkerThread = new Thread(RunReportsForCompletedTasksStart);
            backgroundWorkerThread.Start();

            return true;
        }

        /// <summary>
        /// Invoke this method to check if any scheduled tasks must be run
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet]
        public bool CheckScheduleTasksToRun(string token)
        {
            //TODO make this better
            if (token != "test")
                throw new HttpResponseException(HttpStatusCode.NotFound);
            LogHelper.LogInfo(this, MethodBase.GetCurrentMethod().Name, this.RequestContext.Url.Request.RequestUri.OriginalString);
            var backgroundWorkerThread = new Thread(CheckScheduleTasksToRunStart);
            backgroundWorkerThread.Start();
            return true;
        }

        private void RunReportsForCompletedTasksStart()
        {
            _workFlow.RunReportsForCompletedTasks();  
        }

        private void CheckScheduleTasksToRunStart()
        {
            var scanOrderIDList = _repository.GetScanOrdersToRunTasksOnSchedule();

            foreach (var scanOrder in scanOrderIDList)
            {
                ScanOrderOccurence occurence = _repository.CreateScanOrderOccurence(System.DateTime.UtcNow, scanOrder.ScanOrderID);
                _workFlow.StartScan(scanOrder, occurence.ScanOrderOccurenceID);
                //Customers can have as many scan orders as they want. For right now, do not limit Customer "A" from purchashing N number of "Daily" scans.
                _messenger.SendInternalEmail("Scheduled Scan Starting", string.Format("Schedule Scan starting for IP Address {0}", scanOrder.TargetIPAddress));
            }
        }
    }
}
