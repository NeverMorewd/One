using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

using com.cosmosworldwide.ctcs.v53024x.languageutil;
using com.cosmosworldwide.tcsuite.efq;
using com.cosmosworldwide.ctcs.AbstractEntityFacade;
using com.cosmosworldwide.tcsuite.util.filter;
using com.cosmosworldwide.tcsuite.util.facade;
using com.cosmosworldwide.tams.v110000;
using com.cosmosworldwide.ctcs.BasicDataProvider;
using System.Collections.ObjectModel;
using com.cosmosworldwide.eportal.Classes.ProcessLayer.TAMS.CPN;
using com.cosmosworldwide.eportal.Classes.ProcessLayer.TAMS.TruckCall;

namespace Psa.Hnn.Web.ePortal
{
    public class CPNService : ProcessService
    {
        private static void CheckSecurity(ContainerPreNotification containerPreNotification)
        {
            if (!Utilities.UserIsTerminalOperatorManager())
            {
                if (containerPreNotification.TruckingCompany != null &&
                   containerPreNotification.TruckingCompany.Trim().ToUpper() != Utilities.activeCompanyCode())
                {
                    throw new AccessDeniedException("User company code and the prenotifications trucking company do not match");
                }
            }
        }

        public static CPNList getCPNList(string aTruckingCompany, string aContainerId, string anOrderRef, string aHandlingType, string anIsoCode, string aPrecheckStatus, string[] someStatuses, string aTAR, DateTime? fromUpdateDateTime, DateTime? untilDateTime,
                int startRecord, int maxRecords, List<OrderByInfo> orderByInfoList, string culture, bool allowSkipSystems, out List<string> skippedSystems, bool allowSkipErrors, out List<string> erroredSystem)
        {
            return getCPNList(null, null, aTruckingCompany, aContainerId, anOrderRef, aHandlingType, anIsoCode, aPrecheckStatus, someStatuses,
                aTAR, fromUpdateDateTime, untilDateTime, startRecord, maxRecords, orderByInfoList, culture, allowSkipSystems, out skippedSystems, allowSkipErrors, out erroredSystem);
        }

        public static CPNList getCPNList(string environment, string terminal, string aTruckingCompany,
                string aContainerId, string anOrderRef, string aHandlingType, string anIsoCode, string aPrecheckStatus, string[] someStatuses, string aTAR, DateTime? fromUpdateDateTime, DateTime? untilDateTime,
                int startRecord, int maxRecords, List<OrderByInfo> orderByInfoList, string culture, bool allowSkipSystems, out List<string> skippedSystems, bool allowSkipErrors, out List<string> erroredSystem)
        {
            return getCPNList(environment, terminal, aTruckingCompany, aContainerId,
                anOrderRef, aHandlingType, anIsoCode, aPrecheckStatus, someStatuses,
                aTAR, fromUpdateDateTime, untilDateTime, new int[] { }, 0, startRecord,
                maxRecords, orderByInfoList, culture, allowSkipSystems, out skippedSystems, allowSkipErrors, out erroredSystem);
        }

        public static CPNList getCPNList(string environment, string terminal, string aTruckingCompany,
                string aContainerId, string anOrderRef, string aHandlingType, string anIsoCode, string aPrecheckStatus, string[] someStatuses, string aTAR, DateTime? fromUpdateDateTime, DateTime? untilDateTime, int[] aExceptIds,
                int startRecord, int maxRecords, List<OrderByInfo> orderByInfoList, string culture, bool allowSkipSystems, out List<string> skippedSystems, bool allowSkipErrors, out List<string> erroredSystems)
        {
            return getCPNList(environment, terminal, aTruckingCompany,
                aContainerId, anOrderRef, aHandlingType, anIsoCode, aPrecheckStatus, someStatuses, aTAR, fromUpdateDateTime, untilDateTime, aExceptIds, 0,
                startRecord, maxRecords, orderByInfoList, culture, allowSkipSystems, out skippedSystems, allowSkipErrors, out erroredSystems);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="environment"></param>
        /// <param name="terminal"></param>
        /// <param name="aTruckingCompany"></param>
        /// <param name="aContainerId"></param>
        /// <param name="anOrderRef"></param>
        /// <param name="aHandlingType"></param>
        /// <param name="anIsoCode"></param>
        /// <param name="aPrecheckStatus"></param>
        /// <param name="someStatuses"></param>
        /// <param name="aTAR"></param>
        /// <param name="fromUpdateDateTime"></param>
        /// <param name="untilDateTime"></param>
        /// <param name="aExceptIds"></param>
        /// <param name="aTruckCallId"></param>
        /// <param name="startRecord"></param>
        /// <param name="maxRecords"></param>
        /// <param name="orderByInfoList"></param>
        /// <param name="culture"></param>
        /// <param name="allowSkipSystems"></param>
        /// <param name="skippedSystems"></param>
        /// <param name="allowSkipErrors"></param>
        /// <param name="erroredSystems"></param>
        /// <returns></returns>
        public static CPNList getCPNList(string environment, string terminal, string aTruckingCompany,
                string aContainerId, string anOrderRef, string aHandlingType, string anIsoCode, string aPrecheckStatus, string[] someStatuses, string aTAR, DateTime? fromUpdateDateTime, DateTime? untilDateTime, int[] aExceptIds, int aTruckCallId,
                int startRecord, int maxRecords, List<OrderByInfo> orderByInfoList, string culture, bool allowSkipSystems, out List<string> skippedSystems, bool allowSkipErrors, out List<string> erroredSystems)
        {
            skippedSystems = new List<string>();
            erroredSystems = new List<string>();

            //orderByInfoList.Add(new OrderByInfo("Id", false));
            orderByInfoList.Add(new OrderByInfo("CreationDateTime", false));


            #region Create filter object

            // IMPORTANT
            // In order to use the indexes of CTCS, we always need to send the terminalcode first, followed by the status code.

            List<Filter> filtersToAnd = new List<Filter>();
            if (someStatuses.Length > 0)
            {
                List<Filter> statusfiltersToOr = new List<Filter>();
                foreach (string someStatus in someStatuses)
                {
                    statusfiltersToOr.Add(new FilterLeaf(
                        ContainerPreNotification.ENTITY_NAME, ContainerPreNotification.ATTRIBUTE_CONTAINERPRENOTIFICATIONSTS,
                        Condition.IsEqual, someStatus));
                }
                Filter statusesFilter = OrFilters(statusfiltersToOr);
                filtersToAnd.Add(statusesFilter);
            }


            andWildCardStringFilter(filtersToAnd,
                ContainerPreNotification.ENTITY_NAME,
                ContainerPreNotification.ATTRIBUTE_CONTAINERID,
                aContainerId);

            andWildCardStringFilter(filtersToAnd,
                ContainerPreNotification.ENTITY_NAME,
                ContainerPreNotification.ATTRIBUTE_ORDERREFERENCE,
                anOrderRef);

            andWildCardStringFilter(filtersToAnd,
                TruckCall.ENTITY_NAME,
                TruckCall.ATTRIBUTE_TAR,
                aTAR);

            if (aTruckCallId > 0)
            {
                andWildCardStringFilter(filtersToAnd,
                    TruckCall.ENTITY_NAME,
                    TruckCall.ATTRIBUTE_TRUCKCALLID,
                    aTruckCallId.ToString());
            }

            andExactStringFilter(filtersToAnd,
                ContainerPreNotification.ENTITY_NAME,
                ContainerPreNotification.ATTRIBUTE_HANDLINGTYPE,
                aHandlingType);

            andExactStringFilter(filtersToAnd,
                ContainerPreNotification.ENTITY_NAME,
                ContainerPreNotification.ATTRIBUTE_ISOCODE,
                anIsoCode);

            if (!aTruckingCompany.Equals(string.Empty))
            {
                andWildCardStringFilter(filtersToAnd,
                ContainerPreNotification.ENTITY_NAME,
                ContainerPreNotification.ATTRIBUTE_TRUCKINGCOMPANY,
                aTruckingCompany);
            }

            if (!aPrecheckStatus.ToUpper().Equals(InternalConstants.ALL))
            {
                andExactStringFilter(filtersToAnd,
                   ContainerPreNotification.ENTITY_NAME,
                   ContainerPreNotification.ATTRIBUTE_PREGATECHECKSTATUS,
                   aPrecheckStatus);
            }


            if (fromUpdateDateTime.HasValue)
            {
                filtersToAnd.Add(new FilterLeaf(ContainerPreNotification.ENTITY_NAME,
                    ContainerPreNotification.ATTRIBUTE_CREATIONDATETIME,
                    Condition.IsGreaterThan,
                    fromUpdateDateTime.Value));
            }

            if (untilDateTime.HasValue)
            {
                filtersToAnd.Add(new FilterLeaf(ContainerPreNotification.ENTITY_NAME,
                    ContainerPreNotification.ATTRIBUTE_CREATIONDATETIME,
                    Condition.IsLessThan,
                    untilDateTime.Value));
            }

            if (aExceptIds.Length > 0)
            {
                foreach (int except in aExceptIds)
                {
                    filtersToAnd.Add(new FilterLeaf(ContainerPreNotification.ENTITY_NAME,
                        ContainerPreNotification.ATTRIBUTE_CONTAINERPRENOTIFICATIONID,
                        Condition.IsNotEqual,
                        except));
                }
            }

            Filter filter = AndFilters(filtersToAnd);
            #endregion

            CPNList totalList = new CPNList();
            Dictionary<string, EDCDICColumns> orderByEBCDICColumns = new Dictionary<string, EDCDICColumns>();
            OrderBy orderby = new OrderBy();
            StringBuilder orderbySb = new StringBuilder();
            foreach (OrderByInfo orderByInfo in orderByInfoList)
            {
                OrderByElement orderbyElement = null;
                SortOrder order = orderByInfo.Ascending ? SortOrder.ASCENDING : SortOrder.DESCENDING;
                switch (orderByInfo.OrderByColumn)
                {
                    case "Id": orderbyElement = new OrderByElement(ContainerPreNotification.ENTITY_NAME, ContainerPreNotification.ATTRIBUTE_CONTAINERPRENOTIFICATIONID, order); break;
                    case "Terminal": orderbyElement = new OrderByElement(ContainerPreNotification.ENTITY_NAME, ContainerPreNotification.ATTRIBUTE_TERMINAL, order); break;
                    case "Status": orderbyElement = new OrderByElement(ContainerPreNotification.ENTITY_NAME, ContainerPreNotification.ATTRIBUTE_CONTAINERPRENOTIFICATIONSTS, order); break;
                    case "OrderReference": orderbyElement = new OrderByElement(ContainerPreNotification.ENTITY_NAME, ContainerPreNotification.ATTRIBUTE_ORDERREFERENCE, order); break;
                    case "ContainerId": orderbyElement = new OrderByElement(ContainerPreNotification.ENTITY_NAME, ContainerPreNotification.ATTRIBUTE_CONTAINERID, order); break;
                    case "ISOCode": orderbyElement = new OrderByElement(ContainerPreNotification.ENTITY_NAME, ContainerPreNotification.ATTRIBUTE_ISOCODE, order); break;
                    case "LoadStatus": orderbyElement = new OrderByElement(ContainerPreNotification.ENTITY_NAME, ContainerPreNotification.ATTRIBUTE_LOADSTATUS, order); break;
                    case "HandlingType": orderbyElement = new OrderByElement(ContainerPreNotification.ENTITY_NAME, ContainerPreNotification.ATTRIBUTE_HANDLINGTYPE, order); break;
                    case "TruckCallId": orderbyElement = new OrderByElement(ContainerPreNotification.ENTITY_NAME, ContainerPreNotification.ATTRIBUTE_TRUCKCALLID, order); break;
                    case "TruckingCompany": orderbyElement = new OrderByElement(ContainerPreNotification.ENTITY_NAME, ContainerPreNotification.ATTRIBUTE_TRUCKINGCOMPANY, order); break;
                    case "CreationDateTime": orderbyElement = new OrderByElement(ContainerPreNotification.ENTITY_NAME, ContainerPreNotification.ATTRIBUTE_CREATIONDATETIME, order); break;
                    case "UpdateDateTime": orderbyElement = new OrderByElement(ContainerPreNotification.ENTITY_NAME, ContainerPreNotification.ATTRIBUTE_CHANGEDATETIME, order); break;
                    case "PreGateCheckStatus": orderbyElement = new OrderByElement(ContainerPreNotification.ENTITY_NAME, ContainerPreNotification.ATTRIBUTE_PREGATECHECKSTATUS, order); break;
                    //case "PreGateCheckError": orderbyElement = new OrderByElement(ContainerPreNotification.ENTITY_NAME, ContainerPreNotification.ATTRIBUTE_PREGATECHECKERROR, order); break;
                    case "PreGateCheckDateTime": orderbyElement = new OrderByElement(ContainerPreNotification.ENTITY_NAME, ContainerPreNotification.ATTRIBUTE_PREGATECHECKDATETIME, order); break;
                    case "TAR": orderbyElement = new OrderByElement(TruckCall.ENTITY_NAME, TruckCall.ATTRIBUTE_TAR, order); break;
                    case "CallCardNbr": orderbyElement = new OrderByElement(TruckCall.ENTITY_NAME, TruckCall.ATTRIBUTE_CALLCARDNUMBER, order); break;
                    default: orderbyElement = null; break;
                }
                addToOrderByClause(orderByEBCDICColumns, orderby, orderbySb, orderByInfo, orderbyElement, totalList.CPN);
            }

            CPNList result = new CPNList();
            OrderingSeqNbrInfo orderingSeqNbrInfo = new OrderingSeqNbrInfo(result.CPN);

            #region Init Logging vars
            // Execute request with logging
            DateTime aStartTime = DateTime.Now;
            DateTime anEndtime = DateTime.MinValue;
            Exception anExceptionDuringRequest = null;
            #endregion

            Dictionary<string, IAsyncResult> callsByEnvironment = new Dictionary<string, IAsyncResult>();
            Dictionary<string, ContainerPreNotifications> entitiesByEnvironment = new Dictionary<string, ContainerPreNotifications>();
            Collection<string> environments = com.cosmosworldwide.ctcs.BasicDataProvider.SystemConfiguration.GetEnvironmentNames(InternalConstants.Modules.TAMS.ToString());
            if (environment != null && terminal != null)
            {
                environments = new Collection<string>(new string[] { environment });
            }

            if (allowSkipSystems)
            {
                Dictionary<string, DateTime> queryLockedSystems = Psa.Hnn.Web.Global.QueryLockDown.GetLockedSystems();

                skippedSystems.AddRange(environments.Intersect(queryLockedSystems.Keys));

                environments = new Collection<string>(new List<string>(environments
                    .Except(skippedSystems)));

                Utilities.SendMailForQueryLockedDownSystems(queryLockedSystems);
            }

            foreach (string anEnvironment in environments)
            {
                List<Filter> envFiltersToOr = new List<Filter>();
                Collection<string> terminals = com.cosmosworldwide.ctcs.BasicDataProvider.SystemConfiguration.GetTerminalsForEnv(anEnvironment);
                if (environment != null && terminal != null)
                {
                    terminals = new Collection<string>(new string[] { terminal });
                }
                foreach (string anEnvTerminal in terminals)
                {
                    envFiltersToOr.Add(new FilterLeaf(ContainerPreNotification.ENTITY_NAME, ContainerPreNotification.ATTRIBUTE_TERMINAL,
                        Condition.IsEqual, anEnvTerminal));
                }

                Filter envFilter = OrFilters(envFiltersToOr);
                Filter filterWithTerminal = AndFilters(new List<Filter>(new Filter[] { envFilter, filter }));

                //ContainerPreNotifications containerPreNotifications = ContainerPreNotifications.GetInstance(JavaConnection.getConnectionHandle(anEnvironment), anEnvironment);
                ContainerPreNotifications containerPreNotifications = new ContainerPreNotifications(null, anEnvironment);
                // Get all records (not only from startrecord) until maxRecords, because final ordering will be done over all environments
                IAsyncResult asyncResult =
                    containerPreNotifications.BeginGet(ContainerPreNotifications.GETTERTYPE_ALLWITHTARNBR, filterWithTerminal, orderby, 0, startRecord + maxRecords);
                callsByEnvironment.Add(anEnvironment, asyncResult);
                entitiesByEnvironment.Add(anEnvironment, containerPreNotifications);
            }

            foreach (string anEnvironment in environments)
            {
                try
                {
                    ContainerPreNotifications containerPreNotifications = entitiesByEnvironment[anEnvironment];
                    List<ContainerPreNotification> aCPNCollectionPerEnvironment = containerPreNotifications.EndGet(callsByEnvironment[anEnvironment]);
                    //TODO remove this line and translate the loadstatus in the TAMS facade to english
                    string ctcsLanguage = LanguageConversion.ConvertCTCSSystemLanguage(SystemConfiguration.GetSystemLanguage(anEnvironment));

                    foreach (ContainerPreNotification cpn in aCPNCollectionPerEnvironment)
                    {
                        string aConvertedLoadStatus = Utilities.ConvertFromEnglish(LanguageConversion.LanguageValueSet.LoadStatus, LanguageConversion.ConvertLV(LanguageConversion.LanguageValueSet.LoadStatus, cpn.LoadStatus, ctcsLanguage, LanguageConversion.ENGLISH));
                        string aConvertedHandlingType = Utilities.ConvertFromEnglish(LanguageConversion.LanguageValueSet.HandlingType, cpn.HandlingType);

                        CPNList.CPNRow cpnRow = totalList.CPN.AddCPNRow(
                                        cpn.Id,
                                        anEnvironment.Trim(),
                                        TerminalCodeTranslator.Encode(cpn.Terminal.Trim()),
                                        cpn.GateZone.Trim(),
                                        cpn.Status.Trim(),
                                        cpn.OrderReference.Trim(),
                                        cpn.ContainerId.Trim(),
                                        cpn.ISOCode.Trim(),
                                        aConvertedLoadStatus,
                                        aConvertedHandlingType,
                                        cpn.TruckCallId,
                                        cpn.TruckingCompany.Trim(),
                                        DateTime.MinValue,
                                        DateTime.MinValue,
                                        cpn.PreGateCheckStatus.Trim(),
                                        string.Empty,
                                        DateTime.MinValue,
                                        "",
                                        "",
                                        cpn.ExecutedByTruckCallId,
                                        "");

                        DateTime? creationDateTime = Utilities.ConvertLongToDateTime(cpn.CreationDateTime);
                        if (creationDateTime.HasValue) cpnRow.CreationDateTime = creationDateTime.Value; else cpnRow.SetCreationDateTimeNull();
                        DateTime? updateDateTime = Utilities.ConvertLongToDateTime(cpn.ChangeDateTime);
                        if (updateDateTime.HasValue) cpnRow.UpdateDateTime = updateDateTime.Value; else cpnRow.SetUpdateDateTimeNull();
                        DateTime? preGateCheckDateTime = Utilities.ConvertLongToDateTime(cpn.PreGateCheckDateTime);
                        if (preGateCheckDateTime.HasValue) cpnRow.PreGateCheckDateTime = preGateCheckDateTime.Value; else cpnRow.SetPreGateCheckDateTimeNull();

                        if (cpn.TruckCall == null)
                        {
                            cpnRow.SetTARNull();
                            cpnRow.SetCallCardNbrNull();
                        }
                        else
                        {
                            cpnRow.TAR = cpn.TruckCall.TAR.Trim();
                            cpnRow.CallCardNbr = cpn.TruckCall.CallCardNumber;
                        }

                        if (cpn.ExecutedByTruckCallId != 0)
                        {
                            //TruckCalls tc = TruckCalls.GetInstance(JavaConnection.getConnectionHandle(anEnvironment), anEnvironment);
                            TruckCalls tc = new TruckCalls(null, anEnvironment);
                            TruckCall tcExecutedBy = (TruckCall)tc.Get(cpn.ExecutedByTruckCallId);
                            cpnRow.ExecutedByCallCardNbr = tcExecutedBy.CallCardNumber;
                        }

                        // check the parent status. if the status is NOT ANC and NOT PRV --> do not show the precheck status/error
                        cpnRow.PreGateCheckStatus = Utilities.GetValidPreCheckValue(cpn.Status, cpn.PreGateCheckStatus);
                    }

                    containerPreNotifications.Dispose();
                }
                catch (Exception ex)
                {
                    anExceptionDuringRequest = ex;
                    if (allowSkipErrors && HttpContext.Current.ApplicationInstance is Global)
                    {
                        Global b = HttpContext.Current.ApplicationInstance as Global;
                        if (b != null)
                        {
                            erroredSystems.Add(anEnvironment);
                            b.HandleException(HttpContext.Current.ApplicationInstance, new EventArgs(), ex);
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
                finally
                {
                    anEndtime = DateTime.Now;
                    StringBuilder sParameters = new StringBuilder();
                    sParameters.Append(Utilities.LogToString(environment, terminal, aTruckingCompany,
                        aContainerId, anOrderRef, aHandlingType, anIsoCode, aPrecheckStatus, someStatuses, aTAR, fromUpdateDateTime, untilDateTime, aExceptIds,
                        startRecord, maxRecords));
                    Utilities.LogToLoggingDatabase(anEnvironment, Utilities.LoggingRequestCode.TamsGetPrenotificationList, sParameters.ToString(), anExceptionDuringRequest, aStartTime, anEndtime);
                }
            }

            //Reorder en get the right page
            prepareDatatableForEBCDICSorting(totalList.CPN, orderByEBCDICColumns);
            DataRow[] allRows = totalList.CPN.Select("", orderbySb.ToString());

            orderingSeqNbrInfo.Init();
            int nrOfRowsToKeep = allRows.Length;
            if (maxRecords > 0)
            {
                if (startRecord + maxRecords < nrOfRowsToKeep)
                {
                    nrOfRowsToKeep = startRecord + maxRecords;
                }
            }
            for (int i = startRecord; i < nrOfRowsToKeep; i++)
            {
                CPNList.CPNRow cpnRow = (CPNList.CPNRow)allRows[i];
                CPNList.CPNRow newCpnRow =
                    result.CPN.AddCPNRow(
                                    cpnRow.Id,
                                    cpnRow.Environment.Trim(),
                                    cpnRow.Terminal.Trim(),
                                    cpnRow.GateZone.Trim(),
                                    cpnRow.Status.Trim(),
                                    cpnRow.OrderReference.Trim(),
                                    cpnRow.ContainerId.Trim(),
                                    cpnRow.ISOCode.Trim(),
                                    cpnRow.LoadStatus.Trim(),
                                    cpnRow.HandlingType.Trim(),
                                    cpnRow.TruckCallId,
                                    cpnRow.TruckingCompany.Trim(),
                                    DateTime.MinValue,
                                    DateTime.MinValue,
                                    cpnRow.PreGateCheckStatus.Trim(),
                                    cpnRow.PreGateCheckMessage.Trim(),
                                    DateTime.MinValue,
                                    "",
                                    "",
                                    cpnRow.ExecutedByTruckCallId,
                                    cpnRow.ExecutedByCallCardNbr);

                if (!cpnRow.IsCreationDateTimeNull()) newCpnRow.CreationDateTime = cpnRow.CreationDateTime;
                else newCpnRow.SetCreationDateTimeNull();
                if (!cpnRow.IsUpdateDateTimeNull()) newCpnRow.UpdateDateTime = cpnRow.UpdateDateTime;
                else newCpnRow.SetUpdateDateTimeNull();
                if (!cpnRow.IsPreGateCheckDateTimeNull()) newCpnRow.PreGateCheckDateTime = cpnRow.PreGateCheckDateTime;
                else newCpnRow.SetPreGateCheckDateTimeNull();
                //if (cpnRow.IsTARNull()) newCpnRow.SetTARNull(); else newCpnRow.TAR = cpnRow.TAR.Trim();
                if (cpnRow.IsTARNull())
                {
                    newCpnRow.TAR = "";
                }
                else
                {
                    newCpnRow.TAR = cpnRow.TAR.Trim();
                }

                if (cpnRow.IsCallCardNbrNull() || cpnRow.Status != "ACT" || cpnRow.CallCardNbr.Equals("0"))
                {
                    newCpnRow.SetCallCardNbrNull();
                }
                else
                {
                    newCpnRow.CallCardNbr = cpnRow.CallCardNbr;
                }

                // check the parent status. if the status is NOT ANC and NOT PRV --> do not show the precheck status/error
                newCpnRow.PreGateCheckStatus = Utilities.GetValidPreCheckValue(cpnRow.Status, cpnRow.PreGateCheckStatus);
                newCpnRow.PreGateCheckMessage = Utilities.GetValidPreCheckValue(cpnRow.Status, cpnRow.PreGateCheckMessage);

                orderingSeqNbrInfo.addSeqNbrToRow(newCpnRow);
            }

            //Add pre gate check last logging messages
            Dictionary<string, List<int>> cpnIDsPerEnvironment = new Dictionary<string, List<int>>();
            foreach (CPNList.CPNRow resultRow in result.CPN.Rows)
            {
                if (!cpnIDsPerEnvironment.ContainsKey(resultRow.Environment))
                    cpnIDsPerEnvironment.Add(resultRow.Environment, new List<int>());
                cpnIDsPerEnvironment[resultRow.Environment].Add(resultRow.Id);
            }
            Dictionary<string, IAsyncResult> loggingCallsByEnvironment = new Dictionary<string, IAsyncResult>();
            Dictionary<string, PreGateCheckLoggings> loggingEntitiesByEnvironment = new Dictionary<string, PreGateCheckLoggings>();
            foreach (string env in cpnIDsPerEnvironment.Keys)
            {
                List<Filter> filtersToOr = new List<Filter>();
                Filter cpnFilter = null;
                OrderBy cpnOrderby = null;

                PreGateCheckLoggings preGateCheckLoggings = null;
                string applicationName = SystemConfiguration.GetAceApplNameForEnv(env);
                if (String.IsNullOrEmpty(applicationName))
                {
                    string cpnIds = "<LIST>";
                    foreach (int tId in cpnIDsPerEnvironment[env])
                    {
                        cpnIds += tId + ";";
                    }
                    cpnFilter = new FilterLeaf(PreGateCheckLogging.ENTITY_NAME_OLD, PreGateCheckLogging.ATTRIBUTE_CONTAINERPRENOTIFICATIONID, Condition.IsEqual, cpnIds.TrimEnd(';'));

                    // WNKLRB: Because we need only the loggings with a datetime that is not existing we add a month to the current date.
                    // We cant use 99991231235959 because tcsuite class CosmosDateTime messes with this date (> 2100).
                    cpnFilter = new And(cpnFilter, new FilterLeaf(PreGateCheckLogging.ENTITY_NAME_OLD,
                        PreGateCheckLogging.ATTRIBUTE_ENDDATETIME,
                        Condition.IsGreaterThan,
                        DateTime.Now.AddMonths(1)));

                    cpnOrderby = new OrderBy(PreGateCheckLogging.ENTITY_NAME_OLD,
                                                    PreGateCheckLogging.ATTRIBUTE_STARTDATETIME,
                                                    SortOrder.DESCENDING);

                    //PreGateCheckLoggings preGateCheckLoggings = PreGateCheckLoggings.GetInstance(JavaConnection.getConnectionHandle(env), env);
                    preGateCheckLoggings = new PreGateCheckLoggings(null, env);
                }
                else
                {
                    string cpnIds = "<LIST>";
                    foreach (int tId in cpnIDsPerEnvironment[env])
                    {
                        cpnIds += tId + ";";
                    }
                    cpnFilter = new FilterLeaf(PreGateCheckLogging.ENTITY_NAME, PreGateCheckLogging.ATTRIBUTE_CONTAINERPRENOTIFICATIONID, Condition.IsEqual, cpnIds.TrimEnd(';'));

                    // WNKLRB: Because we need only the loggings with a datetime that is not existing we add a month to the current date.
                    // We cant use 99991231235959 because tcsuite class CosmosDateTime messes with this date (> 2100).
                    cpnFilter = new And(cpnFilter, new FilterLeaf(PreGateCheckLogging.ENTITY_NAME,
                        PreGateCheckLogging.ATTRIBUTE_ENDDATETIME,
                        Condition.IsGreaterThan,
                        DateTime.Now.AddMonths(1)));

                    cpnFilter = new And(cpnFilter, new FilterLeaf(
                        PreGateCheckLogging.ENTITY_NAME, PreGateCheckLogging.ATTRIBUTE_LOGGINGTYPE,
                        Condition.IsEqual, InternalConstants.CPNLOG_PREGATECHECKLOGGING));

                    cpnOrderby = new OrderBy(PreGateCheckLogging.ENTITY_NAME,
                                                    PreGateCheckLogging.ATTRIBUTE_STARTDATETIME,
                                                    SortOrder.DESCENDING);

                    if (!Utilities.UserIsTerminalOperatorManager())
                    {
                        cpnFilter = new And(cpnFilter, new FilterLeaf(PreGateCheckLogging.ENTITY_NAME,
                                                            PreGateCheckLogging.ATTRIBUTE_ADDRESSEETYPE,
                                                            Condition.IsEqual,
                                                            "External"));
                    }

                    string[] tables = new string[2];
                    tables[0] = applicationName;
                    tables[1] = SystemConfiguration.GetCtcsApplNameForEnv(env);
                    preGateCheckLoggings = new PreGateCheckLoggings(null, env, tables);
                }
                IAsyncResult asyncResult = preGateCheckLoggings.BeginGet(PreGateCheckLoggings.GETTERTYPE_ALLWITHPARAMS_ONLYONEPERCPNID,
                        cpnFilter, cpnOrderby, 0, 0);
                loggingCallsByEnvironment.Add(env, asyncResult);
                loggingEntitiesByEnvironment.Add(env, preGateCheckLoggings);
            }
            foreach (string env in cpnIDsPerEnvironment.Keys)
            {
                PreGateCheckLoggings preGateCheckLoggings = loggingEntitiesByEnvironment[env];
                List<PreGateCheckLogging> loggingList = preGateCheckLoggings.EndGet(loggingCallsByEnvironment[env]);
                List<int> cpnIDsAlreadyHandled = new List<int>(); // we only want the first one, which will be the latest, since the list is ordered by the startDateTime descending
                foreach (PreGateCheckLogging logging in loggingList)
                {
                    if (Utilities.ConvertLongToDateTime(logging.EndDateTime).HasValue)
                    {
                        // Error is resolved, skip
                        continue;
                    }
                    if (!cpnIDsAlreadyHandled.Contains(logging.ContainerPrenotificationID))
                    {
                        CPNList.CPNRow cpnRow = result.CPN.FindByIdEnvironment(logging.ContainerPrenotificationID, env);

                        // The pregate check error should only be visible when the status is ANC or PRV. In all other cases, the pregate check error should be blanc
                        if (cpnRow.Status.Trim().ToUpper().Equals(InternalConstants.ANC) || cpnRow.Status.Trim().ToUpper().Equals(InternalConstants.PRV))
                        {
                            // Show message when NOK & when (OK + INFO)
                            if (cpnRow.PreGateCheckStatus.ToUpper().Equals(InternalConstants.NOK) || (cpnRow.PreGateCheckStatus.ToUpper().Equals(InternalConstants.OK) && logging.SeverityLevel != null && logging.SeverityLevel.Equals(InternalConstants.TAMS_PREGATE_SEVERITY_INFO, StringComparison.InvariantCultureIgnoreCase)))
                            {
                                cpnRow.PreGateCheckMessage = logging.ErrorID + ": " + Utilities.makeErrorMessage(logging.ErrorID, logging.Parameters, culture, logging.MessageFile);
                            }
                            else
                            {
                                cpnRow.PreGateCheckMessage = string.Empty;
                            }
                        }
                        else
                        {
                            cpnRow.PreGateCheckMessage = string.Empty;
                        }

                        cpnIDsAlreadyHandled.Add(logging.ContainerPrenotificationID);
                    }
                }

                preGateCheckLoggings.Dispose();
            }

            //Utilities.StartWriteLogging("CPNist");
            //Utilities.WriteCollection2Logging(result.CPN.Rows);
            //Utilities.EndWriteLogging();

            return result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="containerPrenotificationId"></param>
        /// <param name="aCPNStatus"></param>
        /// <param name="aCPNPrecheckStatus"></param>
        /// <param name="anEnvironment"></param>
        /// <returns></returns>
        public static string GetPreGateCheckError(int containerPrenotificationId, string aCPNStatus, string aCPNPrecheckStatus, string anEnvironment)
        {
            if (aCPNStatus.Trim().ToUpper().Equals(InternalConstants.ANC) || aCPNStatus.Trim().ToUpper().Equals(InternalConstants.PRV))
            {
                CPNLoggingList aLoggingList = null;
                string applicationName = SystemConfiguration.GetAceApplNameForEnv(anEnvironment);
                if (String.IsNullOrEmpty(applicationName))
                {
                    aLoggingList = getCPNLoggingList(containerPrenotificationId, anEnvironment, HttpContext.Current.Profile["Culture"].ToString());
                }
                else
                {
                    aLoggingList = GetCPNLoggingList(anEnvironment, HttpContext.Current.Profile["Culture"].ToString(), containerPrenotificationId, 0, 0, null, HttpContext.Current.Server);
                }
                if (aLoggingList != null && aLoggingList.Logging != null && aLoggingList.Logging.Count > 0)
                {
                    for(int i = 0; i < aLoggingList.Logging.Count; i++)
                    {
                        // Show message when NOK & when (OK + INFO)
                        if (aCPNPrecheckStatus.ToUpper().Equals(InternalConstants.NOK) || (aCPNPrecheckStatus.ToUpper().Equals(InternalConstants.OK) && aLoggingList.Logging[i].SeverityLevel != null && aLoggingList.Logging[i].SeverityLevel.Equals(InternalConstants.TAMS_PREGATE_SEVERITY_INFO, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            if (aLoggingList.Logging[i].IsEndDateTimeNull())
                                return aLoggingList.Logging[i].ErrorMessage.Trim();
                        }
                    }
                }

                return string.Empty;
            }
            else
            {
                return string.Empty;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="containerPrenotificationId"></param>
        /// <param name="anEnvironment"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static CPNLoggingList getCPNLoggingList(int containerPrenotificationId, string anEnvironment, string culture)
        {
            Filter filter = new FilterLeaf(PreGateCheckLogging.ENTITY_NAME_OLD,
                                            PreGateCheckLogging.ATTRIBUTE_CONTAINERPRENOTIFICATIONID,
                                            Condition.IsEqual, containerPrenotificationId);
            OrderBy orderby = new OrderBy(PreGateCheckLogging.ENTITY_NAME_OLD,
                                            PreGateCheckLogging.ATTRIBUTE_STARTDATETIME,
                                            SortOrder.DESCENDING);
            CPNLoggingList result = new CPNLoggingList();
            //PreGateCheckLoggings preGateCheckLoggings = PreGateCheckLoggings.GetInstance(JavaConnection.getConnectionHandle(anEnvironment), anEnvironment);
            PreGateCheckLoggings preGateCheckLoggings = new PreGateCheckLoggings(null, anEnvironment);
            List<PreGateCheckLogging> loggingList =
                preGateCheckLoggings.Get_Old(PreGateCheckLoggings.GETTERTYPE_ALLWITHPARAMSANDTARNBR,
                    filter, orderby, 0, 0);
            foreach (PreGateCheckLogging logging in loggingList)
            {
                CPNLoggingList.LoggingRow loggingRow = result.Logging.AddLoggingRow(
                                logging.Id,
                                logging.ContainerPrenotificationID,
                                logging.TruckCallId,
                                logging.ErrorID,
                                logging.ErrorDescription.Trim(),
                                logging.LoggingType.Trim(),
                                DateTime.MinValue,
                                DateTime.MinValue,
                                "",
                                "",
                                "",
                                logging.SeverityLevel);
                DateTime? startDateTime = Utilities.ConvertLongToDateTime(logging.StartDateTime);
                if (startDateTime.HasValue) loggingRow.StartDateTime = startDateTime.Value;
                else loggingRow.SetStartDateTimeNull();
                DateTime? endDateTime = Utilities.ConvertLongToDateTime(logging.EndDateTime);
                if (endDateTime.HasValue) loggingRow.EndDateTime = endDateTime.Value;
                else loggingRow.SetEndDateTimeNull();
                if (logging.TruckCall == null) loggingRow.SetTARNull(); else loggingRow.TAR = logging.TruckCall.TAR.Trim();

                loggingRow.ErrorMessage = Utilities.makeErrorMessage(logging.ErrorID, logging.Parameters, culture, logging.MessageFile);
            }
            return result;
        }

        public static void AreThereUnsolvedErrorsBeforeEndDateTime(List<PreGateCheckLogging> CPNLogs, string anEnvironment)
        {
            Dictionary<PreGateCheckLogging, IAsyncResult> callsByCPN = new Dictionary<PreGateCheckLogging, IAsyncResult>();
            Dictionary<PreGateCheckLogging, PreGateCheckLoggings> entitiesByCPN = new Dictionary<PreGateCheckLogging, PreGateCheckLoggings>();
            string[] tables = new string[2];
            tables[0] = SystemConfiguration.GetAceApplNameForEnv(anEnvironment);
            tables[1] = SystemConfiguration.GetCtcsApplNameForEnv(anEnvironment);

            foreach (PreGateCheckLogging log in CPNLogs)
            {
                List<Filter> filtersToAnd = new List<Filter>();
                filtersToAnd.Add(new FilterLeaf(PreGateCheckLogging.ENTITY_NAME,
                                                PreGateCheckLogging.ATTRIBUTE_CONTAINERPRENOTIFICATIONID,
                                                Condition.IsEqual, log.ContainerPrenotificationID));
                filtersToAnd.Add(new FilterLeaf(PreGateCheckLogging.ENTITY_NAME,
                                                PreGateCheckLogging.ATTRIBUTE_LOGGINGTYPE,
                                                Condition.IsEqual, InternalConstants.CPNLOG_PREGATECHECKLOGGING));
                filtersToAnd.Add(new FilterLeaf(PreGateCheckLogging.ENTITY_NAME,
                                                PreGateCheckLogging.ATTRIBUTE_STARTDATETIME,
                                                Condition.IsLessThanOrEqualTo, Utilities.ConvertLongToDateTime(log.EndDateTime)));
                filtersToAnd.Add(new FilterLeaf(PreGateCheckLogging.ENTITY_NAME,
                                                PreGateCheckLogging.ATTRIBUTE_ENDDATETIME,
                                                Condition.IsGreaterThan, Utilities.ConvertLongToDateTime(log.EndDateTime)));
                filtersToAnd.Add(new FilterLeaf(PreGateCheckLogging.ENTITY_NAME,
                                                PreGateCheckLogging.ATTRIBUTE_SEVERITYLEVEL,
                                                Condition.IsNotEqual, InternalConstants.TAMS_PREGATE_SEVERITY_INFO.ToUpper())); // This place needs toupper as the BDStoredProcedure is also doing a UPPER()
                Filter filter = AndFilters(filtersToAnd);


                PreGateCheckLoggings preGateCheckLoggings = new PreGateCheckLoggings(null, anEnvironment, tables);

                IAsyncResult asyncResult = preGateCheckLoggings.BeginGet(PreGateCheckLoggings.GETTERTYPE_TOTALNROFROWS, filter, null, 0, 0);
                callsByCPN.Add(log, asyncResult);
                entitiesByCPN.Add(log, preGateCheckLoggings);
            }

            foreach (PreGateCheckLogging log in CPNLogs)
            {
                PreGateCheckLoggings preGateCheckLogging = entitiesByCPN[log];
                List<PreGateCheckLogging> loggingList = preGateCheckLogging.EndGet(callsByCPN[log]);

                log.TotalNrRows = loggingList[0].TotalNrRows;

                preGateCheckLogging.Dispose();
            }
        }

        public static DateTime? GetFirstUnsolvedErrorDateTimeAfterEndDateTime(int cpnId, DateTime dateTime, string anEnvironment)
        {
            Dictionary<PreGateCheckLogging, IAsyncResult> callsByCPN = new Dictionary<PreGateCheckLogging, IAsyncResult>();
            Dictionary<PreGateCheckLogging, PreGateCheckLoggings> entitiesByCPN = new Dictionary<PreGateCheckLogging, PreGateCheckLoggings>();
            string[] tables = new string[2];
            tables[0] = SystemConfiguration.GetAceApplNameForEnv(anEnvironment);
            tables[1] = SystemConfiguration.GetCtcsApplNameForEnv(anEnvironment);

            List<Filter> filtersToAnd = new List<Filter>();
            filtersToAnd.Add(new FilterLeaf(PreGateCheckLogging.ENTITY_NAME,
                                            PreGateCheckLogging.ATTRIBUTE_CONTAINERPRENOTIFICATIONID,
                                            Condition.IsEqual, cpnId));
            filtersToAnd.Add(new FilterLeaf(PreGateCheckLogging.ENTITY_NAME,
                                            PreGateCheckLogging.ATTRIBUTE_LOGGINGTYPE,
                                            Condition.IsEqual, InternalConstants.CPNLOG_PREGATECHECKLOGGING));
            filtersToAnd.Add(new FilterLeaf(PreGateCheckLogging.ENTITY_NAME,
                                            PreGateCheckLogging.ATTRIBUTE_STARTDATETIME,
                                            Condition.IsGreaterThan, dateTime));
            filtersToAnd.Add(new FilterLeaf(PreGateCheckLogging.ENTITY_NAME,
                                            PreGateCheckLogging.ATTRIBUTE_SEVERITYLEVEL,
                                            Condition.IsNotEqual, InternalConstants.TAMS_PREGATE_SEVERITY_INFO));
            Filter filter = AndFilters(filtersToAnd);


            PreGateCheckLoggings preGateCheckLoggings = new PreGateCheckLoggings(null, anEnvironment, tables);

            List<PreGateCheckLogging> loggingList = preGateCheckLoggings.Get(PreGateCheckLoggings.GETTERTYPE_ALLWITHPARAMS, filter, null, 0, 1);
            if (loggingList.Count > 0)
            {
                return Utilities.ConvertLongToDateTime(loggingList[0].StartDateTime);
            }

            return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="environment"></param>
        /// <param name="culture"></param>
        /// <param name="containerPrenotificationId"></param>
        /// <param name="startRecord"></param>
        /// <param name="maxRecords"></param>
        /// <param name="orderByInfoList"></param>
        /// <param name="aServer"></param>
        /// <param name="internalLog"></param>
        /// <param name="externalLog"></param>
        /// <param name="types"></param>
        /// <param name="nextPage"></param>
        /// <param name="fullOffset"></param>
        /// <param name="orderByEBCDICColumns"></param>
        /// <param name="orderbySb"></param>
        /// <returns></returns>
        private static CPNLoggingList GetCPNLoggingList(string environment, string culture, int containerPrenotificationId, int startRecord, int maxRecords, List<OrderByInfo> orderByInfoList, HttpServerUtility aServer, bool internalLog, bool externalLog, string[] types, bool nextPage, int fullOffset, out Dictionary<string, EDCDICColumns> orderByEBCDICColumns, out StringBuilder orderbySb)
        {
            EnsureJavaConnection();

            if (!Utilities.UserIsTerminalOperatorManager())
            {
                ContainerPreNotifications containerPreNotifications = ContainerPreNotifications.GetInstance(JavaConnection.getTamsServer(environment).ConnectionHandle, environment);
                ContainerPreNotification containerPreNotification = (ContainerPreNotification)containerPreNotifications.Get(containerPrenotificationId);
                CheckSecurity(containerPreNotification);
            }

            #region Add default order by fields
            if (orderByInfoList == null) orderByInfoList = new List<OrderByInfo>();
            if (orderByInfoList.Count == 0)
            {
                orderByInfoList.Add(new OrderByInfo(PreGateCheckLogging.ATTRIBUTE_STARTDATETIME, false));
                orderByInfoList.Add(new OrderByInfo(PreGateCheckLogging.ATTRIBUTE_ENDDATETIME, false));
            }
            #endregion

            #region Create filter object
            List<Filter> filtersToAnd = new List<Filter>();
            filtersToAnd.Add(new FilterLeaf(PreGateCheckLogging.ENTITY_NAME, PreGateCheckLogging.ATTRIBUTE_CONTAINERPRENOTIFICATIONID, containerPrenotificationId));
            filtersToAnd.Add(new FilterLeaf(PreGateCheckLogging.ENTITY_NAME, PreGateCheckLogging.ATTRIBUTE_ENDDATETIME,Condition.IsGreaterThan, DateTime.Now));

            if (!Utilities.UserIsTerminalOperatorManager())
            {
                // Always add the blanks (everybody can see these)
                filtersToAnd.Add(new FilterLeaf(PreGateCheckLogging.ENTITY_NAME, PreGateCheckLogging.ATTRIBUTE_ADDRESSEETYPE, Condition.IsEqual, "<LIST>EXTERNAL;"));
            }
            else if (!(internalLog && externalLog))
            {
                // Always add the blanks (everybody can see these)
                filtersToAnd.Add(new FilterLeaf(PreGateCheckLogging.ENTITY_NAME, PreGateCheckLogging.ATTRIBUTE_ADDRESSEETYPE, Condition.IsEqual, String.Format("<LIST>{0};", (internalLog ? "INTERNAL" : "EXTERNAL"))));
            }

            if (types.Length > 0)
            {
                filtersToAnd.Add(new FilterLeaf(PreGateCheckLogging.ENTITY_NAME, PreGateCheckLogging.ATTRIBUTE_LOGGINGTYPE, Condition.IsEqual, (types.Length > 1) ? "<LIST>" + String.Join(";", types) : types[0]));
            }

            if (!Utilities.UserIsTerminalOperatorManager())
            {
                filtersToAnd.Add(OrFilters(new List<Filter>() {
                    new FilterLeaf(TruckCall.ENTITY_NAME, TruckCall.ATTRIBUTE_TRUCKINGCOMPANY, Condition.IsEqual, Utilities.activeCompanyCode()),
                    new FilterLeaf(TruckCall.ENTITY_NAME, TruckCall.ATTRIBUTE_TRUCKINGCOMPANY, Condition.IsEqual, "NULL")
                    }
                ));
            }

            Filter filter = null;
            if (filtersToAnd.Count > 0)
                filter = AndFilters(filtersToAnd);
            #endregion

            CPNLoggingList totalList = new CPNLoggingList();

            #region order by
            orderByEBCDICColumns = new Dictionary<string, EDCDICColumns>();
            OrderBy orderby = new OrderBy();
            orderbySb = new StringBuilder();
            foreach (OrderByInfo orderByInfo in orderByInfoList)
            {
                OrderByElement orderbyElement = null;
                SortOrder order = orderByInfo.Ascending ? SortOrder.ASCENDING : SortOrder.DESCENDING;
                switch (orderByInfo.OrderByColumn)
                {
                    case PreGateCheckLogging.ATTRIBUTE_ERRORID: orderbyElement = new OrderByElement(PreGateCheckLogging.ENTITY_NAME, PreGateCheckLogging.ATTRIBUTE_ERRORID, order); break;
                    case PreGateCheckLogging.ATTRIBUTE_ERRORDESCRIPTION: orderbyElement = new OrderByElement(PreGateCheckLogging.ENTITY_NAME, PreGateCheckLogging.ATTRIBUTE_ERRORDESCRIPTION, order); break;
                    case PreGateCheckLogging.ATTRIBUTE_STARTDATETIME: orderbyElement = new OrderByElement(PreGateCheckLogging.ENTITY_NAME, PreGateCheckLogging.ATTRIBUTE_STARTDATETIME, order); break;
                    case PreGateCheckLogging.ATTRIBUTE_ENDDATETIME: orderbyElement = new OrderByElement(PreGateCheckLogging.ENTITY_NAME, PreGateCheckLogging.ATTRIBUTE_ENDDATETIME, order); break;
                    default: orderbyElement = null; break;
                }
                addToOrderByClause(orderByEBCDICColumns, orderby, orderbySb, orderByInfo, orderbyElement, totalList.Logging);
            }
            #endregion

            #region retrieve data
            string[] tables = new string[2];
            tables[0] = SystemConfiguration.GetAceApplNameForEnv(environment);
            tables[1] = SystemConfiguration.GetCtcsApplNameForEnv(environment);
            PreGateCheckLoggings PreGateCheckLog = new PreGateCheckLoggings(JavaConnection.getTamsServer(environment).ConnectionHandle, environment, tables);

            int nrRowsToRetrieve = maxRecords + startRecord;
            if (nrRowsToRetrieve > 0 && !nextPage)
                nrRowsToRetrieve += (1 - fullOffset); // the row of the (previous in time)/(next in rowcount) page must be retrieved to know the last row must be an OK line
            List<PreGateCheckLogging> aPreGateCheckLoggingList = PreGateCheckLog.Get(PreGateCheckLoggings.GETTERTYPE_ALLWITHPARAMSANDTARNBR, filter, orderby, 0, nrRowsToRetrieve);

            foreach (PreGateCheckLogging logLine in aPreGateCheckLoggingList)
            {
                CPNLoggingList.LoggingRow CPNLoggingRow =
                totalList.Logging.AddLoggingRow(
                    logLine.Id,
                    logLine.ContainerPrenotificationID,
                    logLine.TruckCallId,
                    logLine.ErrorID,
                    logLine.ErrorDescription.Trim(),
                    logLine.LoggingType.Trim(),
                    DateTime.MinValue,
                    DateTime.MinValue,
                    "",
                    "",
                    logLine.AddresseeType,
                    logLine.SeverityLevel);

                DateTime? startDateTime = Utilities.ConvertLongToDateTime(logLine.StartDateTime);
                if (startDateTime.HasValue) CPNLoggingRow.StartDateTime = startDateTime.Value; else CPNLoggingRow.SetStartDateTimeNull();
                DateTime? endDateTime = Utilities.ConvertLongToDateTime(logLine.EndDateTime);
                if (endDateTime.HasValue) CPNLoggingRow.EndDateTime = endDateTime.Value; else CPNLoggingRow.SetEndDateTimeNull();
                if (logLine.TruckCall == null) CPNLoggingRow.SetTARNull(); else CPNLoggingRow.TAR = logLine.TruckCall.TAR.Trim();

                CPNLoggingRow.ErrorMessage = Utilities.makeErrorMessage(logLine.ErrorID, logLine.Parameters, culture, logLine.MessageFile);
            }
            #endregion

            return totalList;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="environment"></param>
        /// <param name="culture"></param>
        /// <param name="containerPrenotificationId"></param>
        /// <param name="startRecord"></param>
        /// <param name="maxRecords"></param>
        /// <param name="orderByInfoList"></param>
        /// <param name="aServer"></param>
        /// <returns></returns>
        public static CPNLoggingList GetCPNLoggingList(string environment, string culture, int containerPrenotificationId, int startRecord, int maxRecords, List<OrderByInfo> orderByInfoList, HttpServerUtility aServer)
        {
            Dictionary<string, EDCDICColumns> orderByEBCDICColumns = null;
            StringBuilder orderbySb = null;
            CPNLoggingList totalList = GetCPNLoggingList(environment, culture, containerPrenotificationId, startRecord, maxRecords, orderByInfoList, aServer, true, true, new string[] { "PGC" }, true, 0, out orderByEBCDICColumns, out orderbySb);

            #region prepare datatable
            CPNLoggingList result = new CPNLoggingList();
            OrderingSeqNbrInfo orderingSeqNbrInfo = new OrderingSeqNbrInfo(result.Logging);

            prepareDatatableForEBCDICSorting(totalList.Logging, orderByEBCDICColumns);
            DataRow[] allRows = totalList.Logging.Select("", orderbySb.ToString());

            orderingSeqNbrInfo.Init();
            int nrOfRowsToKeep = allRows.Length;

            if (nrOfRowsToKeep > 0)
            {
                if (maxRecords > 0)
                {
                    if (startRecord + maxRecords < nrOfRowsToKeep)
                    {
                        nrOfRowsToKeep = startRecord + maxRecords;
                    }
                }

                for (int i = startRecord; i < nrOfRowsToKeep; i++)
                {
                    CPNLoggingList.LoggingRow PreGateCheckLoggingLineRow = (CPNLoggingList.LoggingRow)allRows[i];

                    //Insert logging row
                    CPNLoggingList.LoggingRow newPreGateCheckLoggingLineRow =
                        result.Logging.AddLoggingRow(
                        PreGateCheckLoggingLineRow.Id,
                        PreGateCheckLoggingLineRow.ContainerPrenotificationID,
                        PreGateCheckLoggingLineRow.TruckCallId,
                        PreGateCheckLoggingLineRow.ErrorID,
                        PreGateCheckLoggingLineRow.ErrorDescription.Trim(),
                        PreGateCheckLoggingLineRow.LoggingType.Trim(),
                        DateTime.MinValue,
                        DateTime.MinValue,
                        "",
                        PreGateCheckLoggingLineRow.ErrorMessage,
                        PreGateCheckLoggingLineRow.AddresseeType,
                        PreGateCheckLoggingLineRow.SeverityLevel);

                    if (!PreGateCheckLoggingLineRow.IsStartDateTimeNull()) newPreGateCheckLoggingLineRow.StartDateTime = PreGateCheckLoggingLineRow.StartDateTime; else newPreGateCheckLoggingLineRow.SetStartDateTimeNull();
                    if (!PreGateCheckLoggingLineRow.IsEndDateTimeNull()) newPreGateCheckLoggingLineRow.EndDateTime = PreGateCheckLoggingLineRow.EndDateTime; else newPreGateCheckLoggingLineRow.SetEndDateTimeNull();
                    if (!PreGateCheckLoggingLineRow.IsTARNull()) newPreGateCheckLoggingLineRow.TAR = PreGateCheckLoggingLineRow.TAR; else newPreGateCheckLoggingLineRow.SetTARNull();

                    orderingSeqNbrInfo.addSeqNbrToRow(newPreGateCheckLoggingLineRow);
                }
            }
            #endregion

            return result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="environment"></param>
        /// <param name="culture"></param>
        /// <param name="containerPrenotificationId"></param>
        /// <param name="startRecord"></param>
        /// <param name="maxRecords"></param>
        /// <param name="orderByInfoList"></param>
        /// <param name="aServer"></param>
        /// <param name="internalLog"></param>
        /// <param name="externalLog"></param>
        /// <param name="types"></param>
        /// <param name="addOKLines"></param>
        /// <param name="fullOffset"></param>
        /// <param name="nextPage"></param>
        /// <param name="endWithOKLine"></param>
        /// <param name="endDateTimeOKLine"></param>
        /// <param name="OKLineAtEnd"></param>
        /// <param name="OKLineAtBegin"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static CPNLoggingList GetCPNLoggingList(string environment, string culture, int containerPrenotificationId, int startRecord, int maxRecords, List<OrderByInfo> orderByInfoList, HttpServerUtility aServer, bool internalLog, bool externalLog, string[] types, bool addOKLines, int fullOffset, bool nextPage, bool endWithOKLine, DateTime endDateTimeOKLine, out DateTime OKLineAtEnd, out bool OKLineAtBegin, out int offset)
        {
            Dictionary<string, EDCDICColumns> orderByEBCDICColumns = null;
            StringBuilder orderbySb = null;
            CPNLoggingList totalList = GetCPNLoggingList(environment, culture, containerPrenotificationId, startRecord, maxRecords, orderByInfoList, aServer, internalLog, externalLog, types, nextPage, fullOffset, out orderByEBCDICColumns, out orderbySb);

            if (Array.IndexOf(types, InternalConstants.CPNLOG_PREGATECHECKLOGGING) < 0)
            {
                addOKLines = false;
            }

            #region prepare datatable
            CPNLoggingList result = new CPNLoggingList();
            OrderingSeqNbrInfo orderingSeqNbrInfo = new OrderingSeqNbrInfo(result.Logging);

            prepareDatatableForEBCDICSorting(totalList.Logging, orderByEBCDICColumns);
            DataRow[] allRows = totalList.Logging.Select("", orderbySb.ToString());

            orderingSeqNbrInfo.Init();
            int nrOfRowsToKeep = allRows.Length;
            //offset is also used as unique row key that's why it's -1 and not 0
            offset = -1;
            OKLineAtBegin = false;
            OKLineAtEnd = DateTime.MaxValue;

            if (nrOfRowsToKeep > 0)
            {
                //we retrieved more rows then necessary, because we don't now the startrecords for sure with previous. 
                //If we use the nrOfRowsToKeep from next, we loose our extra rows (+1 is to eliminate the -1 of the offset)
                int previousNrOfRowsToKeep = (nrOfRowsToKeep > (startRecord + maxRecords) ? nrOfRowsToKeep : startRecord + maxRecords) + 1;

                if (maxRecords > 0)
                {
                    if (startRecord + maxRecords < nrOfRowsToKeep)
                    {
                        nrOfRowsToKeep = startRecord + maxRecords;
                    }
                }

                #region Retrieve the number or unsolved errors per enddate
                //IMPORTANT this is a dummy PreGateCheckLog last parameter is empty
                PreGateCheckLoggings PreGateCheckLog = new PreGateCheckLoggings(JavaConnection.getTamsServer(environment).ConnectionHandle, environment, new string[2]);

                //Make PreGateCheckLogging element to store the enddate, id, prenotificationId
                List<PreGateCheckLogging> CPNLogs = new List<PreGateCheckLogging>();
                for (int i = startRecord; (i < allRows.Length) && ((nextPage && (i < nrOfRowsToKeep)) || (!nextPage && (i < (previousNrOfRowsToKeep + offset)))); i++)
                {
                    CPNLoggingList.LoggingRow PreGateCheckLoggingLineRow = (CPNLoggingList.LoggingRow)allRows[i];
                    if (PreGateCheckLoggingLineRow.LoggingType == InternalConstants.CPNLOG_PREGATECHECKLOGGING && !PreGateCheckLoggingLineRow.IsEndDateTimeNull())
                    {
                        PreGateCheckLogging CPNitem = new PreGateCheckLogging(PreGateCheckLog);
                        CPNitem.SetTheId(PreGateCheckLoggingLineRow.Id);
                        CPNitem.ContainerPrenotificationID = PreGateCheckLoggingLineRow.ContainerPrenotificationID;
                        CPNitem.EndDateTime = Utilities.ConvertDateTimeToLong(PreGateCheckLoggingLineRow.EndDateTime);
                        CPNLogs.Add(CPNitem);
                    }
                }
                //Update CPNLogs with the number of unsolved errors (rows) on the enddatetime
                AreThereUnsolvedErrorsBeforeEndDateTime(CPNLogs, environment);
                #endregion

                List<DateTime> dateTimesAllErrorsFixed = new List<DateTime>();
                Dictionary<int, DateTime> rowIndexOfOKLine = new Dictionary<int, DateTime>();
                DateTime previousStartDateTimeError = DateTime.MaxValue;

                for (int i = startRecord; (i < allRows.Length) && ((nextPage && (i < nrOfRowsToKeep)) || (!nextPage && (i <= (previousNrOfRowsToKeep + offset)))); i++)
                {
                    CPNLoggingList.LoggingRow PreGateCheckLoggingLineRow = (CPNLoggingList.LoggingRow)allRows[i];

                    if (addOKLines)
                    {
                        #region ok line
                        if (!nextPage && i == startRecord)
                        {
                            //when previous is clicked we don't no which enddates are reported yet, so we will investigate this using the previous records
                            //if an other row before this one (which belong to the same error time frame) is found, then no OK line can be inserted before 
                            //this row. If no such row is found we don't know if an OK line must be displayed so futher investigation is needed.
                            if (!PreGateCheckLoggingLineRow.IsEndDateTimeNull() && CPNLogs.Find(delegate(PreGateCheckLogging l) { return l.Id.Equals(PreGateCheckLoggingLineRow.Id); }).TotalNrRows == 0)
                            {
                                if ((from r in totalList.Logging.AsEnumerable()
                                     where PreGateCheckLoggingLineRow.StartDateTime < r.Field<DateTime>("StartDateTime")
                                     && PreGateCheckLoggingLineRow.EndDateTime > r.Field<DateTime>("StartDateTime")
                                     select r).AsDataView<CPNLoggingList.LoggingRow>().Count > 0)
                                {
                                    //this is not a OK line because a previous row has the OK line already
                                    endDateTimeOKLine = PreGateCheckLoggingLineRow.EndDateTime;
                                }
                                else
                                {
                                    endDateTimeOKLine = DateTime.MaxValue;
                                }
                            }
                        }

                        //find the rows with end datetimes forwhich an OK line should be inserted (have enddate, not info and have no open errors)
                        if (!PreGateCheckLoggingLineRow.IsEndDateTimeNull() && !PreGateCheckLoggingLineRow.SeverityLevel.Equals(InternalConstants.TAMS_PREGATE_SEVERITY_INFO, StringComparison.InvariantCultureIgnoreCase) && CPNLogs.Find(delegate(PreGateCheckLogging l) { return l.Id.Equals(PreGateCheckLoggingLineRow.Id); }).TotalNrRows == 0)
                        {
                            if (!dateTimesAllErrorsFixed.Contains(PreGateCheckLoggingLineRow.EndDateTime))
                            {
                                dateTimesAllErrorsFixed.Add(PreGateCheckLoggingLineRow.EndDateTime);
                            }
                        }

                        //Positions the OK line according to the startdatetime (main sorting column)
                        foreach (DateTime edt in dateTimesAllErrorsFixed)
                        {
                            if (edt < previousStartDateTimeError && !PreGateCheckLoggingLineRow.IsStartDateTimeNull() && edt > PreGateCheckLoggingLineRow.StartDateTime)
                            {
                                if (!edt.Equals(endDateTimeOKLine) && (nextPage ||
                                    (!nextPage && ((i - 1 != (nrOfRowsToKeep + offset + 1)) || endWithOKLine))))//previous && (not last row || allow to end with OK line)
                                {
                                    CPNLoggingList.LoggingRow addPreGateCheckLoggingLineRow = addPreGateCheckLoggingLineRow =
                                    result.Logging.AddLoggingRow(offset--, 0, 0, "", "", InternalConstants.CPNLOG_PREGATECHECKLOGGING, edt, previousStartDateTimeError, "", Resources.TAMS.LoggingLineOK, "", "");
                                    if (previousStartDateTimeError == DateTime.MaxValue)
                                    {
                                        addPreGateCheckLoggingLineRow.SetEndDateTimeNull();

                                        DateTime? previousErrorStartDate = GetFirstUnsolvedErrorDateTimeAfterEndDateTime(containerPrenotificationId, edt, environment);
                                        if (previousErrorStartDate.HasValue)
                                        {
                                            addPreGateCheckLoggingLineRow.EndDateTime = previousErrorStartDate.Value;
                                        }
                                    }
                                    if (!PreGateCheckLoggingLineRow.IsTARNull()) addPreGateCheckLoggingLineRow.TAR = PreGateCheckLoggingLineRow.TAR; else addPreGateCheckLoggingLineRow.SetTARNull();

                                    orderingSeqNbrInfo.addSeqNbrToRow(addPreGateCheckLoggingLineRow);
                                    rowIndexOfOKLine.Add(i + rowIndexOfOKLine.Count, edt);
                                }
                            }
                        }
                        #endregion
                    }

                    //Insert logging row
                    CPNLoggingList.LoggingRow newPreGateCheckLoggingLineRow =
                        result.Logging.AddLoggingRow(
                        PreGateCheckLoggingLineRow.Id,
                        PreGateCheckLoggingLineRow.ContainerPrenotificationID,
                        PreGateCheckLoggingLineRow.TruckCallId,
                        PreGateCheckLoggingLineRow.ErrorID,
                        PreGateCheckLoggingLineRow.ErrorDescription.Trim(),
                        PreGateCheckLoggingLineRow.LoggingType.Trim(),
                        DateTime.MinValue,
                        DateTime.MinValue,
                        "",
                        PreGateCheckLoggingLineRow.ErrorMessage,
                        PreGateCheckLoggingLineRow.AddresseeType,
                        PreGateCheckLoggingLineRow.SeverityLevel);

                    if (!PreGateCheckLoggingLineRow.IsStartDateTimeNull())
                    {
                        newPreGateCheckLoggingLineRow.StartDateTime = PreGateCheckLoggingLineRow.StartDateTime;
                        if (PreGateCheckLoggingLineRow.LoggingType == InternalConstants.CPNLOG_PREGATECHECKLOGGING && !PreGateCheckLoggingLineRow.SeverityLevel.Equals(InternalConstants.TAMS_PREGATE_SEVERITY_INFO, StringComparison.InvariantCultureIgnoreCase))
                        {
                            previousStartDateTimeError = PreGateCheckLoggingLineRow.StartDateTime;
                        }
                    }
                    else newPreGateCheckLoggingLineRow.SetStartDateTimeNull();
                    if (!PreGateCheckLoggingLineRow.IsEndDateTimeNull()) newPreGateCheckLoggingLineRow.EndDateTime = PreGateCheckLoggingLineRow.EndDateTime; else newPreGateCheckLoggingLineRow.SetEndDateTimeNull();
                    if (!PreGateCheckLoggingLineRow.IsTARNull()) newPreGateCheckLoggingLineRow.TAR = PreGateCheckLoggingLineRow.TAR; else newPreGateCheckLoggingLineRow.SetTARNull();

                    orderingSeqNbrInfo.addSeqNbrToRow(newPreGateCheckLoggingLineRow);
                }

                if (addOKLines)
                {
                    #region OK Line when not PGC lines
                    // WNKLRB: Not sure of the following, but it seems logical to just add the row at the end and have it deleted afterwards if there is too many rows...
                    // We need to add a OK line, when we only have e.g. DEV lines, PGC cause errors, the rest is informative
                    // Also, if only Informative PGC, an ok line is added
                    if (totalList.Logging.Select(totalList.Logging.LoggingTypeColumn.ColumnName + " = '" + InternalConstants.CPNLOG_PREGATECHECKLOGGING + "' AND " + totalList.Logging.SeverityLevelColumn.ColumnName + " <> '" + InternalConstants.TAMS_PREGATE_SEVERITY_INFO + "'", orderbySb.ToString()).Length == 0)
                    {
                        CPNLoggingList.LoggingRow addPreGateCheckLoggingLineRow = addPreGateCheckLoggingLineRow =
                                result.Logging.AddLoggingRow(0, 0, 0, "", "", InternalConstants.CPNLOG_PREGATECHECKLOGGING, DateTime.MinValue, DateTime.MinValue, "", Resources.TAMS.LoggingLineOK, "", "");
                        addPreGateCheckLoggingLineRow.SetStartDateTimeNull();
                        addPreGateCheckLoggingLineRow.SetEndDateTimeNull();
                        orderingSeqNbrInfo.addSeqNbrToRow(addPreGateCheckLoggingLineRow);
                    }
                    #endregion
                }

                #region remove extra rows (because we inserted extra OK lines)
                int resultRowCount = result.Tables[0].Rows.Count;
                if (!nextPage)
                {
                    //we always retrieve one row to much to check if the second to last row is an OK line
                    if (result.Tables[0].Rows.Count > maxRecords)
                        result.Tables[0].Rows.RemoveAt(result.Tables[0].Rows.Count - 1);

                    //plus the extra rows from the ok lines
                    for (int r = -1; r > offset; r--)
                    {
                        result.Tables[0].Rows.RemoveAt(0);
                    }
                }
                //remove rows that fall of this page because we inserted ok lines
                while (result.Tables[0].Rows.Count > maxRecords)
                {
                    result.Tables[0].Rows.RemoveAt(result.Tables[0].Rows.Count - 1);
                }

                //We kept track of the inserted ok lines, some of them may have fallen of the page
                //if this is the case the offset, OKLineAtEnd and OKLineAtBegin must be adjusted
                if (rowIndexOfOKLine.Count > 0)
                {
                    int previousKey = -1;
                    foreach (var kvp in rowIndexOfOKLine)
                    {
                        //new StartRecord = startRecord + resultRowCount -1 - maxRecords where -1 is the extra row we retrieved
                        if ((nextPage && kvp.Key >= (startRecord + maxRecords)) || (!nextPage && kvp.Key < (startRecord + resultRowCount - 1 - maxRecords)))
                        {
                            offset++;
                        }
                        else
                        {
                            if (kvp.Key > previousKey)
                            {
                                previousKey = kvp.Key;
                                OKLineAtEnd = kvp.Value;
                            }
                            if ((nextPage && kvp.Key == startRecord) || (!nextPage && kvp.Key == (startRecord + resultRowCount - 1 - maxRecords)))
                            {
                                OKLineAtBegin = true;
                            }
                        }
                    }
                }

                if (OKLineAtEnd == DateTime.MaxValue)
                {
                    OKLineAtEnd = endDateTimeOKLine;
                }
                #endregion
            }
            else
            {
                if (addOKLines)
                {
                    CPNLoggingList.LoggingRow addPreGateCheckLoggingLineRow = addPreGateCheckLoggingLineRow =
                                result.Logging.AddLoggingRow(0, 0, 0, "", "", InternalConstants.CPNLOG_PREGATECHECKLOGGING, DateTime.MinValue, DateTime.MinValue, "", Resources.TAMS.LoggingLineOK, "", "");
                    addPreGateCheckLoggingLineRow.SetStartDateTimeNull();
                    addPreGateCheckLoggingLineRow.SetEndDateTimeNull();
                    orderingSeqNbrInfo.addSeqNbrToRow(addPreGateCheckLoggingLineRow);
                }
            }
            #endregion

            return result;
        }

        public static void deleteCPN(int cpnID, string anEnvironment, bool anOverrideWarningIndicator)
        {
            EnsureJavaConnection();

            ContainerPreNotifications containerPreNotifications = ContainerPreNotifications.GetInstance(JavaConnection.getTamsServer(anEnvironment).ConnectionHandle, anEnvironment);

            #region Init Logging vars
            // Execute request with logging
            DateTime aStartTime = DateTime.Now;
            DateTime anEndtime = DateTime.MinValue;
            Exception anExceptionDuringRequest = null;
            #endregion

            try
            {
                ContainerPreNotification containerPreNotification =
                    (ContainerPreNotification)containerPreNotifications.Get(cpnID);

                CheckSecurity(containerPreNotification);

                containerPreNotifications.Cancel(anOverrideWarningIndicator, containerPreNotification);
            }
            catch (Exception ex)
            {
                anExceptionDuringRequest = ex;
                throw;
            }
            finally
            {
                anEndtime = DateTime.Now;
                StringBuilder sParameters = new StringBuilder();
                sParameters.Append(Utilities.LogToString(cpnID));
                Utilities.LogToLoggingDatabase(anEnvironment, Utilities.LoggingRequestCode.TamsDeletePrenotification, sParameters.ToString(), anExceptionDuringRequest, aStartTime, anEndtime);
            }
        }

        public static CPNEdit loadEmptyCPN(string anEnvironment)
        {
            return loadCPN(null, anEnvironment);
        }

        public static CPNEdit loadExistingCPN(int cpnID, string anEnvironment)
        {
            return loadCPN(cpnID, anEnvironment);
        }

        private static CPNEdit loadCPN(int? cpnID, string anEnvironment)
        {
            EnsureJavaConnection();

            ContainerPreNotifications containerPreNotifications = ContainerPreNotifications.GetInstance(JavaConnection.getTamsServer(anEnvironment).ConnectionHandle, anEnvironment);

            #region Init Logging vars
            // Execute request with logging
            DateTime aStartTime = DateTime.Now;
            DateTime anEndtime = DateTime.MinValue;
            Exception anExceptionDuringRequest = null;
            #endregion

            try
            {
                ContainerPreNotification containerPreNotification =
                    cpnID.HasValue ?
                    (ContainerPreNotification)containerPreNotifications.Get(cpnID.Value) :
                    (ContainerPreNotification)containerPreNotifications.GetEmpty();

                if (cpnID.HasValue)
                {
                    CheckSecurity(containerPreNotification);
                }

                string originalObjectSerialized = EntityFacadeHelper.Serialize(containerPreNotification);
                CPNEdit result = new CPNEdit();

                string aConvertedLoadStatus = Utilities.ConvertFromEnglish(LanguageConversion.LanguageValueSet.LoadStatus, containerPreNotification.LoadStatus);
                string aConvertedHandlingType = Utilities.ConvertFromEnglish(LanguageConversion.LanguageValueSet.HandlingType, containerPreNotification.HandlingType);

                CPNEdit.CPNRow row = result.CPN.AddCPNRow(
                            containerPreNotification.Id,
                            anEnvironment,
                            originalObjectSerialized,
                            containerPreNotification.Terminal.Trim(),
                            containerPreNotification.GateZone.Trim(),
                            containerPreNotification.Status,
                            containerPreNotification.OrderReference.Trim(),
                            containerPreNotification.ContainerId.Trim(),
                            containerPreNotification.ISOCode.Trim(),
                            aConvertedLoadStatus,
                            aConvertedHandlingType,
                            containerPreNotification.TruckingCompany.Trim(),
                            containerPreNotification.PreGateCheckStatus.Trim(),
                            string.Empty,
                            DateTime.MinValue,
                            containerPreNotification.PODCountry.Trim(),
                            containerPreNotification.PODPlace.Trim(),
                            containerPreNotification.POLCountry.Trim(),
                            containerPreNotification.POLPlace.Trim(),
                            DateTime.MinValue,
                            DateTime.MinValue,
                            containerPreNotification.CreatedByUser,
                            containerPreNotification.ChangedByUser,
                            containerPreNotification.CreatedByExternalUser,
                            containerPreNotification.ChangedByExternalUser,
                            containerPreNotification.DriveThroughIndicator,
                            containerPreNotification.OriginDestination.Trim(),
                            containerPreNotification.GrossWeight,
                            containerPreNotification.ReleaseReference.Trim(),
                            containerPreNotification.ContainerLength,
                            containerPreNotification.ContainerType.Trim(),
                            containerPreNotification.ContainerHeight,
                            containerPreNotification.ContainerCSCWeight,
                            containerPreNotification.ContainerCondition.Trim(),
                            containerPreNotification.ContainerMaterial.Trim(),
                            containerPreNotification.ContainerLeaseType.Trim(),
                            containerPreNotification.ContainerQuarantineStatus.Trim(),
                            containerPreNotification.ContainerOperationalReeferIndication,
                            containerPreNotification.ContainerIMDGClass.Trim(),
                            containerPreNotification.BillOfLadingNumber.Trim(),
                            containerPreNotification.TruckCallId,
                            String.IsNullOrEmpty(containerPreNotification.HandlingPIN.Trim()) ? "" : InternalConstants.FAKE_PINCODE, // Do not retrieve PIN for security (ViewState)
                            (containerPreNotification.TruckCall != null ? containerPreNotification.TruckCall.TAR : ""));

                DateTime? preGateCheckDateTime = Utilities.ConvertLongToDateTime(containerPreNotification.PreGateCheckDateTime);
                if (preGateCheckDateTime.HasValue) row.PreGateCheckDateTime = preGateCheckDateTime.Value;
                else row.SetPreGateCheckDateTimeNull();

                DateTime? creationDateTime = Utilities.ConvertLongToDateTime(containerPreNotification.CreationDateTime);
                if (creationDateTime.HasValue) row.CreationDateTime = creationDateTime.Value;
                else row.SetCreationDateTimeNull();

                DateTime? changeDateTime = Utilities.ConvertLongToDateTime(containerPreNotification.ChangeDateTime);
                if (changeDateTime.HasValue) row.ChangeDateTime = changeDateTime.Value;
                else row.SetChangeDateTimeNull();

                // check the parent status. if the status is NOT ANC and NOT PRV --> do not show the precheck status/error
                if (containerPreNotification.PreGateCheckStatus.Equals(InternalConstants.NOK) || (containerPreNotification.PreGateCheckStatus.ToUpper().Equals(InternalConstants.OK)))
                {
                    row.PreGateCheckMessage = GetPreGateCheckError(containerPreNotification.Id, containerPreNotification.Status, containerPreNotification.PreGateCheckStatus, anEnvironment);
                }
                else
                {
                    row.PreGateCheckMessage = string.Empty;
                }

                return result;
            }
            catch (Exception ex)
            {
                anExceptionDuringRequest = ex;
                throw;
            }
            finally
            {
                anEndtime = DateTime.Now;
                StringBuilder sParameters = new StringBuilder();
                sParameters.Append(Utilities.LogToString(cpnID));
                Utilities.LogToLoggingDatabase(anEnvironment, Utilities.LoggingRequestCode.TamsGetPrenotification, sParameters.ToString(), anExceptionDuringRequest, aStartTime, anEndtime);
            }
        }

        public static void saveCPN(ref CPNEdit cpn, bool createTruckCall, string anEnvironment, bool anOverrideWarningIndicator)
        {
            EnsureJavaConnection();
            CPNEdit.CPNRow cpnRow = (CPNEdit.CPNRow)cpn.CPN.Rows[0];
            TAMSServer tamsServer = JavaConnection.getTamsServer(cpnRow.Environment);
            ContainerPreNotifications containerPreNotifications = ContainerPreNotifications.GetInstance(tamsServer.ConnectionHandle, cpnRow.Environment);

            ContainerPreNotification original = (ContainerPreNotification)EntityFacadeHelper.DeSerialize(containerPreNotifications, cpnRow.OriginalObjectSerialized);

            ContainerPreNotification containerPreNotification = (ContainerPreNotification)original.Clone(true);

            containerPreNotification.Terminal = cpnRow.Terminal.Trim();
            containerPreNotification.OrderReference = cpnRow.OrderReference.Trim();
            containerPreNotification.ContainerId = cpnRow.ContainerId.Trim();
            containerPreNotification.ISOCode = cpnRow.ISOCode.Trim();
            containerPreNotification.LoadStatus = Utilities.ConvertToEnglish(LanguageConversion.LanguageValueSet.LoadStatus, cpnRow.LoadStatus);
            containerPreNotification.HandlingType = Utilities.ConvertToEnglish(LanguageConversion.LanguageValueSet.HandlingType, cpnRow.HandlingType.Trim());
            containerPreNotification.TruckingCompany = cpnRow.TruckingCompany.Trim();
            containerPreNotification.PreGateCheckStatus = cpnRow.PreGateCheckStatus.Trim();
            //containerPreNotification.PreGateCheckError = cpnRow.PreGateCheckError;
            containerPreNotification.PreGateCheckDateTime = Utilities.ConvertDateTimeToLong(
                cpnRow.IsPreGateCheckDateTimeNull() ? (DateTime?)null : (DateTime?)cpnRow.PreGateCheckDateTime);
            containerPreNotification.PODCountry = cpnRow.PODCountry.Trim();
            containerPreNotification.PODPlace = cpnRow.PODPlace.Trim();
            containerPreNotification.POLCountry = cpnRow.POLCountry.Trim();
            containerPreNotification.POLPlace = cpnRow.POLPlace.Trim();

            containerPreNotification.DriveThroughIndicator = cpnRow.DriveThroughIndicator;
            containerPreNotification.OriginDestination = cpnRow.OriginDestination.Trim();
            containerPreNotification.GrossWeight = cpnRow.GrossWeight;
            containerPreNotification.ReleaseReference = cpnRow.ReleaseInstruction.Trim();
            if (!String.IsNullOrEmpty(cpnRow.HandlingPin.Trim()) && (cpnRow.HandlingPin.Trim() != InternalConstants.FAKE_PINCODE))
            {
                containerPreNotification.HandlingPIN = cpnRow.HandlingPin.Trim();
            }
            containerPreNotification.ContainerLength = cpnRow.ContainerLength;
            containerPreNotification.ContainerType = cpnRow.ContainerType.Trim();
            containerPreNotification.ContainerHeight = cpnRow.ContainerHeight;
            containerPreNotification.ContainerCSCWeight = cpnRow.ContainerCSCWeight;
            containerPreNotification.ContainerCondition = cpnRow.ContainerCondition.Trim();
            containerPreNotification.ContainerMaterial = cpnRow.ContainerMaterial.Trim();
            containerPreNotification.ContainerLeaseType = cpnRow.ContainerLeaseType.Trim();
            containerPreNotification.ContainerQuarantineStatus = cpnRow.ContainerQuarantineStatus.Trim();
            containerPreNotification.ContainerOperationalReeferIndication = cpnRow.ContainerOperationalReeferIndication;
            containerPreNotification.ContainerIMDGClass = cpnRow.ContainerIMDGClass.Trim();
            containerPreNotification.BillOfLadingNumber = cpnRow.BillOfLadingNumber.Trim();

            bool isUpdate = cpnRow.Id > 0;

            #region Init Logging vars
            // Execute request with logging
            DateTime aStartTime = DateTime.Now;
            DateTime anEndtime = DateTime.MinValue;
            Exception anExceptionDuringRequest = null;
            #endregion

            CheckSecurity(containerPreNotification);

            string username = Utilities.activeUser().MembershipInfo.UserName.Trim();
            if (isUpdate)
            {
                containerPreNotification.ChangedByExternalUser = username;

                try
                {
                    containerPreNotifications.Update(anOverrideWarningIndicator, original, containerPreNotification);
                }
                catch (Exception ex)
                {
                    anExceptionDuringRequest = ex;
                    throw;
                }
                finally
                {
                    anEndtime = DateTime.Now;
                    StringBuilder sParameters = new StringBuilder();
                    sParameters.Append(Utilities.LogToString(containerPreNotification.Id));
                    Utilities.LogToLoggingDatabase(anEnvironment, Utilities.LoggingRequestCode.TamsEditPrenotification, sParameters.ToString(), anExceptionDuringRequest, aStartTime, anEndtime);
                }

                // After an update request, there is no FillWithStruct, so the returned CPN does NOT contain the updated data !
                containerPreNotification = (ContainerPreNotification)containerPreNotifications.Get(containerPreNotification.Id);
            }
            else
            {
                containerPreNotification.CreatedByExternalUser = username;

                try
                {
                    if (createTruckCall)
                    {
                        TruckCalls truckCalls = TruckCalls.GetInstance(tamsServer.ConnectionHandle, cpnRow.Environment);
                        TruckCall truckCall = (TruckCall)truckCalls.GetEmpty();
                        truckCall.AppointmentTruckingCompany = cpnRow.TruckingCompany;
                        truckCall.TruckingCompany = cpnRow.TruckingCompany;
                        truckCall.Terminal = cpnRow.Terminal;
                        truckCall.CreatedByExternalUser = username;
                        ContainerPreNotification[] cpns = new ContainerPreNotification[1] { containerPreNotification };
                        tamsServer.CreateTruckCall(truckCall, cpns, anOverrideWarningIndicator);
                        containerPreNotification.TruckCall = truckCall;
                    }
                    else
                    {
                        containerPreNotifications.Create(anOverrideWarningIndicator, containerPreNotification);
                    }
                }
                catch (Exception ex)
                {
                    anExceptionDuringRequest = ex;
                    throw;
                }
                finally
                {
                    anEndtime = DateTime.Now;
                    StringBuilder sParameters = new StringBuilder();
                    sParameters.Append(Utilities.LogToString(containerPreNotification.ContainerId));
                    Utilities.LogToLoggingDatabase(anEnvironment, Utilities.LoggingRequestCode.TamsCreatePrenotification, sParameters.ToString(), anExceptionDuringRequest, aStartTime, anEndtime);
                }
            }

            ConvertCPNEdit(containerPreNotification, anEnvironment, ref cpn);
            if (createTruckCall)
            {
                cpn.CPN[0].TruckCallId = containerPreNotification.TruckCall.Id;
                cpn.CPN[0].TAR = containerPreNotification.TruckCall.TAR;
            }
        }

        public static void ConvertCPNEdit(ContainerPreNotification aContainerPrenotification, string anEnvironment, ref CPNEdit aCpnEdit)
        {
            aCpnEdit.CPN[0].Terminal = aContainerPrenotification.Terminal.Trim();
            aCpnEdit.CPN[0].GateZone = aContainerPrenotification.GateZone.Trim();
            aCpnEdit.CPN[0].OrderReference = aContainerPrenotification.OrderReference.Trim();
            aCpnEdit.CPN[0].ContainerId = aContainerPrenotification.ContainerId.Trim();
            aCpnEdit.CPN[0].ISOCode = aContainerPrenotification.ISOCode.Trim();

            aCpnEdit.CPN[0].LoadStatus = Utilities.ConvertFromEnglish(LanguageConversion.LanguageValueSet.LoadStatus, aContainerPrenotification.LoadStatus);
            aCpnEdit.CPN[0].HandlingType = Utilities.ConvertFromEnglish(LanguageConversion.LanguageValueSet.HandlingType, aContainerPrenotification.HandlingType.Trim());
            aCpnEdit.CPN[0].TruckingCompany = aContainerPrenotification.TruckingCompany.Trim();
            aCpnEdit.CPN[0].PreGateCheckStatus = aContainerPrenotification.PreGateCheckStatus.Trim();
            //containerPreNotification.PreGateCheckError = cpnRow.PreGateCheckError;

            DateTime? aDateTime = Utilities.ConvertLongToDateTime(aContainerPrenotification.PreGateCheckDateTime);
            if (aDateTime.HasValue)
            {
                aCpnEdit.CPN[0].PreGateCheckDateTime = aDateTime.Value;
            }
            else
            {
                aCpnEdit.CPN[0].SetPreGateCheckDateTimeNull();
            }

            aCpnEdit.CPN[0].PODCountry = aContainerPrenotification.PODCountry.Trim();
            aCpnEdit.CPN[0].PODPlace = aContainerPrenotification.PODPlace.Trim();
            aCpnEdit.CPN[0].POLCountry = aContainerPrenotification.POLCountry.Trim();
            aCpnEdit.CPN[0].POLPlace = aContainerPrenotification.POLPlace.Trim();
            aCpnEdit.CPN[0].Id = aContainerPrenotification.Id;
            aCpnEdit.CPN[0].Status = aContainerPrenotification.Status;

            aCpnEdit.CPN[0].DriveThroughIndicator = aContainerPrenotification.DriveThroughIndicator;
            aCpnEdit.CPN[0].OriginDestination = aContainerPrenotification.OriginDestination.Trim();
            aCpnEdit.CPN[0].GrossWeight = aContainerPrenotification.GrossWeight;
            aCpnEdit.CPN[0].ReleaseInstruction = aContainerPrenotification.ReleaseReference.Trim();
            aCpnEdit.CPN[0].ContainerLength = aContainerPrenotification.ContainerLength;
            aCpnEdit.CPN[0].ContainerType = aContainerPrenotification.ContainerType.Trim();
            aCpnEdit.CPN[0].ContainerHeight = aContainerPrenotification.ContainerHeight;
            aCpnEdit.CPN[0].ContainerCSCWeight = aContainerPrenotification.ContainerCSCWeight;
            aCpnEdit.CPN[0].ContainerCondition = aContainerPrenotification.ContainerCondition.Trim();
            aCpnEdit.CPN[0].ContainerMaterial = aContainerPrenotification.ContainerMaterial.Trim();
            aCpnEdit.CPN[0].ContainerLeaseType = aContainerPrenotification.ContainerLeaseType.Trim();
            aCpnEdit.CPN[0].ContainerQuarantineStatus = aContainerPrenotification.ContainerQuarantineStatus.Trim();
            aCpnEdit.CPN[0].ContainerOperationalReeferIndication = aContainerPrenotification.ContainerOperationalReeferIndication;
            aCpnEdit.CPN[0].ContainerIMDGClass = aContainerPrenotification.ContainerIMDGClass.Trim();
            aCpnEdit.CPN[0].BillOfLadingNumber = aContainerPrenotification.BillOfLadingNumber.Trim();
        }
        public static void ConvertCPNEdit(CPNEdit aCPN, ref TruckCallEdit.CPNRow aTruckCallCpnRow)
        {
            aTruckCallCpnRow.ContainerId = aCPN.CPN[0].ContainerId;
            aTruckCallCpnRow.HandlingType = aCPN.CPN[0].HandlingType;
            aTruckCallCpnRow.Id = aCPN.CPN[0].Id;
            aTruckCallCpnRow.ISOCode = aCPN.CPN[0].ISOCode;
            aTruckCallCpnRow.LoadStatus = aCPN.CPN[0].LoadStatus;
            aTruckCallCpnRow.OrderReference = aCPN.CPN[0].OrderReference;
            aTruckCallCpnRow.PODCountry = aCPN.CPN[0].PODCountry;
            aTruckCallCpnRow.PODPlace = aCPN.CPN[0].PODPlace;
            aTruckCallCpnRow.POLCountry = aCPN.CPN[0].POLCountry;
            aTruckCallCpnRow.POLPlace = aCPN.CPN[0].POLPlace;

            if (aCPN.CPN[0].IsPreGateCheckDateTimeNull())
            {
                aTruckCallCpnRow.SetPreGateCheckDateTimeNull();
            }
            else
            {
                aTruckCallCpnRow.PreGateCheckDateTime = aCPN.CPN[0].PreGateCheckDateTime;
            }

            aTruckCallCpnRow.PreGateCheckMessage = aCPN.CPN[0].PreGateCheckMessage;
            aTruckCallCpnRow.PreGateCheckStatus = aCPN.CPN[0].PreGateCheckStatus;
            aTruckCallCpnRow.Status = aCPN.CPN[0].Status;
            aTruckCallCpnRow.TruckingCompany = aCPN.CPN[0].TruckingCompany;
            aTruckCallCpnRow.Terminal = aCPN.CPN[0].Terminal;
            aTruckCallCpnRow.GateZone = aCPN.CPN[0].GateZone;
        }

    }
}
