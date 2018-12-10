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