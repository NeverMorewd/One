using com.cosmosworldwide.tams.corba.v110000;
using com.cosmosworldwide.tcsuite.util.asynchronousread.corba;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.cosmosworldwide.tams.facade
{
    public interface ContainerPreNotificationFacade
    {
        ContainerPreNotificationStruct getContainerPreNotification(string aConnectionHandle, int anId);
        ContainerPreNotificationStruct[] getContainerPreNotificationsByIds(string aConnectionHandle, int[] anIdSeq);
        ContainerPreNotificationStruct[] getContainerPreNotifications(string aConnectionHandle);
        ContainerPreNotificationStruct[] getContainerPreNotificationsWithFilter(string aConnectionHandle, String aFilter);
        void getContainerPreNotificationsAsynchronously(string aConnectionHandle, string aFilter, ReplyListener aListener);

        ContainerPreNotificationStruct createContainerPreNotification(string connectionHandle, bool overrideWarningIndicator, ContainerPreNotificationStruct aContainerPreNotificationStruct);
        ContainerPreNotificationStruct updateContainerPreNotification(string connectionHandle, bool overrideWarningIndicator, ContainerPreNotificationStruct aContainerPreNotificationStructBefore, ContainerPreNotificationStruct aContainerPreNotificationStruct);
        ContainerPreNotificationStruct cancelContainerPreNotification(string connectionHandle, bool overrideWarningIndicator, ContainerPreNotificationStruct aContainerPreNotificationStruct);
        ContainerPreNotificationStruct deleteContainerPreNotification(string connectionHandle, bool overrideWarningIndicator, int anId);

        //<METHOD><CUSTOM>
        void cancelAndUnlinkTruckCall(string aConnectionHandle, bool anOverrideWarningIndicator, ContainerPreNotificationStruct aContainerPreNotificationStruct);
        void validate(string aConnectionHandle, ContainerPreNotificationStruct aCPNStruct, bool anOverruleWarnings);
        //</CUSTOM></METHOD>
    }
}
