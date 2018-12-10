using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.cosmosworldwide.tams.corba.v110000;
using com.cosmosworldwide.tams.facade;
using com.cosmosworldwide.tcsuite.util.asynchronousread.corba;
using com.cosmosworldwide.util.facade.planh;

namespace com.cosmosworldwide.ctcs.v53024x.ContainerPreNotification
{
    public class ContainerPreNotifications:PlanHEntities
    {
        ContainerPreNotificationFacade hessianService;
        public ContainerPreNotifications(string environment)
            :base(environment, "")
        {
            hessianService = (ContainerPreNotificationFacade)HessianFactory.Create(typeof(ContainerPreNotificationFacade), this.URL);
        }
    }
}
