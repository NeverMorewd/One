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